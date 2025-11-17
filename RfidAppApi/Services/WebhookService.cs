using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using RfidAppApi.Data;
using RfidAppApi.DTOs;
using RfidAppApi.Models;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace RfidAppApi.Services
{
    /// <summary>
    /// Service for webhook management and async delivery
    /// Handles webhook subscriptions, event triggering, and retry logic
    /// </summary>
    public class WebhookService : IWebhookService
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly ILogger<WebhookService> _logger;
        private readonly HttpClient _httpClient;

        public WebhookService(
            AppDbContext context,
            IConfiguration configuration,
            ILogger<WebhookService> logger,
            IHttpClientFactory httpClientFactory)
        {
            _context = context;
            _configuration = configuration;
            _logger = logger;
            _httpClient = httpClientFactory.CreateClient();
            _httpClient.Timeout = TimeSpan.FromSeconds(30);
        }

        public async Task<WebhookSubscriptionDto> SubscribeAsync(CreateWebhookSubscriptionDto subscription, string clientCode)
        {
            var webhookSubscription = new WebhookSubscription
            {
                ClientCode = clientCode,
                EventType = subscription.EventType,
                WebhookUrl = subscription.WebhookUrl,
                SecretKey = subscription.SecretKey,
                Description = subscription.Description,
                IsActive = true,
                CreatedOn = DateTime.UtcNow
            };

            _context.WebhookSubscriptions.Add(webhookSubscription);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Webhook subscription created: {SubscriptionId} for client: {ClientCode}, Event: {EventType}",
                webhookSubscription.Id, clientCode, subscription.EventType);

            return MapToDto(webhookSubscription);
        }

        public async Task<bool> UnsubscribeAsync(int subscriptionId, string clientCode)
        {
            var subscription = await _context.WebhookSubscriptions
                .FirstOrDefaultAsync(s => s.Id == subscriptionId && s.ClientCode == clientCode);

            if (subscription == null)
                return false;

            subscription.IsActive = false;
            subscription.UpdatedOn = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Webhook subscription deactivated: {SubscriptionId}", subscriptionId);
            return true;
        }

        public async Task<IEnumerable<WebhookSubscriptionDto>> GetSubscriptionsAsync(string clientCode, string? eventType = null)
        {
            var query = _context.WebhookSubscriptions
                .Where(s => s.ClientCode == clientCode && s.IsActive);

            if (!string.IsNullOrEmpty(eventType))
            {
                query = query.Where(s => s.EventType == eventType || s.EventType.EndsWith(".*"));
            }

            var subscriptions = await query.ToListAsync();
            return subscriptions.Select(MapToDto);
        }

        public async Task TriggerWebhookAsync(string eventType, object payload, string clientCode, string? specificUrl = null)
        {
            // Get all active subscriptions for this event type
            var subscriptions = await _context.WebhookSubscriptions
                .Where(s => s.ClientCode == clientCode && s.IsActive && 
                    (s.EventType == eventType || 
                     s.EventType == "*" || 
                     (s.EventType.EndsWith(".*") && eventType.StartsWith(s.EventType.Replace(".*", "")))))
                .ToListAsync();

            if (!subscriptions.Any() && string.IsNullOrEmpty(specificUrl))
            {
                _logger.LogDebug("No webhook subscriptions found for event: {EventType}, client: {ClientCode}", eventType, clientCode);
                return;
            }

            // Create webhook payload
            var webhookPayload = new WebhookPayload
            {
                EventType = eventType,
                ClientCode = clientCode,
                Timestamp = DateTime.UtcNow,
                Data = payload
            };

            var payloadJson = JsonSerializer.Serialize(webhookPayload, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            // If specific URL provided, add it to the list
            var urlsToNotify = subscriptions.Select(s => new { s.WebhookUrl, s.SecretKey, Id = (int?)s.Id }).ToList();
            if (!string.IsNullOrEmpty(specificUrl))
            {
                urlsToNotify.Add(new { WebhookUrl = specificUrl, SecretKey = (string?)null, Id = (int?)null });
            }

            // Deliver webhooks asynchronously (fire and forget)
            _ = Task.Run(async () =>
            {
                foreach (var urlInfo in urlsToNotify)
                {
                    try
                    {
                        // Generate signature if secret key is provided
                        if (!string.IsNullOrEmpty(urlInfo.SecretKey))
                        {
                            webhookPayload.Signature = GenerateSignature(payloadJson, urlInfo.SecretKey);
                            payloadJson = JsonSerializer.Serialize(webhookPayload, new JsonSerializerOptions
                            {
                                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                            });
                        }

                        await DeliverWebhookAsync(eventType, urlInfo.WebhookUrl, payloadJson, clientCode, urlInfo.Id);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error triggering webhook for event: {EventType}, URL: {Url}", eventType, urlInfo.WebhookUrl);
                    }
                }
            });
        }

        private async Task DeliverWebhookAsync(string eventType, string webhookUrl, string payloadJson, string clientCode, int? subscriptionId)
        {
            var webhookEvent = new WebhookEvent
            {
                ClientCode = clientCode,
                EventType = eventType,
                WebhookUrl = webhookUrl,
                Status = "Pending",
                Payload = payloadJson,
                CreatedOn = DateTime.UtcNow,
                MaxRetries = 3
            };

            _context.WebhookEvents.Add(webhookEvent);
            await _context.SaveChangesAsync();

            try
            {
                var content = new StringContent(payloadJson, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync(webhookUrl, content);

                webhookEvent.Status = response.IsSuccessStatusCode ? "Delivered" : "Failed";
                webhookEvent.ResponseStatusCode = (int)response.StatusCode;
                webhookEvent.DeliveredOn = DateTime.UtcNow;

                if (response.IsSuccessStatusCode)
                {
                    webhookEvent.ResponseBody = await response.Content.ReadAsStringAsync();
                    _logger.LogInformation("Webhook delivered successfully: {EventType} to {Url}", eventType, webhookUrl);
                }
                else
                {
                    webhookEvent.ErrorMessage = $"HTTP {response.StatusCode}: {await response.Content.ReadAsStringAsync()}";
                    _logger.LogWarning("Webhook delivery failed: {EventType} to {Url}, Status: {Status}", 
                        eventType, webhookUrl, response.StatusCode);
                }

                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                webhookEvent.Status = "Failed";
                webhookEvent.ErrorMessage = ex.Message;
                webhookEvent.RetryCount = 0;
                webhookEvent.NextRetryAt = DateTime.UtcNow.AddMinutes(5); // Retry after 5 minutes

                await _context.SaveChangesAsync();

                _logger.LogError(ex, "Error delivering webhook: {EventType} to {Url}", eventType, webhookUrl);
            }
        }

        public async Task<IEnumerable<WebhookEventDto>> GetWebhookEventsAsync(string clientCode, string? eventType = null, int page = 1, int pageSize = 50)
        {
            IQueryable<WebhookEvent> query = _context.WebhookEvents
                .Where(e => e.ClientCode == clientCode);

            if (!string.IsNullOrEmpty(eventType))
            {
                query = query.Where(e => e.EventType == eventType);
            }

            query = query.OrderByDescending(e => e.CreatedOn);

            var events = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return events.Select(e => new WebhookEventDto
            {
                Id = e.Id,
                ClientCode = e.ClientCode,
                EventType = e.EventType,
                WebhookUrl = e.WebhookUrl,
                Status = e.Status,
                ResponseStatusCode = e.ResponseStatusCode,
                ErrorMessage = e.ErrorMessage,
                RetryCount = e.RetryCount,
                CreatedOn = e.CreatedOn,
                DeliveredOn = e.DeliveredOn
            });
        }

        public async Task<int> RetryFailedWebhooksAsync(string clientCode)
        {
            var failedEvents = await _context.WebhookEvents
                .Where(e => e.ClientCode == clientCode && 
                    e.Status == "Failed" && 
                    e.RetryCount < e.MaxRetries &&
                    (e.NextRetryAt == null || e.NextRetryAt <= DateTime.UtcNow))
                .ToListAsync();

            int retriedCount = 0;

            foreach (var webhookEvent in failedEvents)
            {
                try
                {
                    webhookEvent.RetryCount++;
                    webhookEvent.Status = "Retrying";
                    webhookEvent.NextRetryAt = DateTime.UtcNow.AddMinutes(Math.Pow(2, webhookEvent.RetryCount)); // Exponential backoff

                    await _context.SaveChangesAsync();

                    var content = new StringContent(webhookEvent.Payload, Encoding.UTF8, "application/json");
                    var response = await _httpClient.PostAsync(webhookEvent.WebhookUrl, content);

                    webhookEvent.Status = response.IsSuccessStatusCode ? "Delivered" : "Failed";
                    webhookEvent.ResponseStatusCode = (int)response.StatusCode;
                    webhookEvent.DeliveredOn = DateTime.UtcNow;

                    if (response.IsSuccessStatusCode)
                    {
                        webhookEvent.ResponseBody = await response.Content.ReadAsStringAsync();
                        retriedCount++;
                    }
                    else
                    {
                        webhookEvent.ErrorMessage = $"HTTP {response.StatusCode}";
                    }

                    await _context.SaveChangesAsync();
                }
                catch (Exception ex)
                {
                    webhookEvent.Status = "Failed";
                    webhookEvent.ErrorMessage = ex.Message;
                    await _context.SaveChangesAsync();
                }
            }

            return retriedCount;
        }

        private static string GenerateSignature(string payload, string secretKey)
        {
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secretKey));
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
            return Convert.ToBase64String(hash);
        }

        private static WebhookSubscriptionDto MapToDto(WebhookSubscription subscription)
        {
            return new WebhookSubscriptionDto
            {
                Id = subscription.Id,
                ClientCode = subscription.ClientCode,
                EventType = subscription.EventType,
                WebhookUrl = subscription.WebhookUrl,
                IsActive = subscription.IsActive,
                Description = subscription.Description,
                CreatedOn = subscription.CreatedOn,
                UpdatedOn = subscription.UpdatedOn
            };
        }
    }
}

