using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RfidAppApi.DTOs;
using RfidAppApi.Services;
using System.Security.Claims;

namespace RfidAppApi.Controllers
{
    /// <summary>
    /// Controller for webhook subscription and management
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class WebhookController : ControllerBase
    {
        private readonly IWebhookService _webhookService;
        private readonly ILogger<WebhookController> _logger;

        public WebhookController(
            IWebhookService webhookService,
            ILogger<WebhookController> logger)
        {
            _webhookService = webhookService;
            _logger = logger;
        }

        /// <summary>
        /// Subscribe to webhook events
        /// </summary>
        [HttpPost("subscribe")]
        [ProducesResponseType(typeof(WebhookSubscriptionDto), 200)]
        public async Task<ActionResult<WebhookSubscriptionDto>> Subscribe([FromBody] CreateWebhookSubscriptionDto subscription)
        {
            try
            {
                var clientCode = GetClientCodeFromToken();
                if (string.IsNullOrEmpty(clientCode))
                {
                    return BadRequest(new { success = false, message = "Client code not found in token" });
                }

                var result = await _webhookService.SubscribeAsync(subscription, clientCode);
                return Ok(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error subscribing to webhook");
                return StatusCode(500, new { success = false, message = "Error subscribing to webhook", error = ex.Message });
            }
        }

        /// <summary>
        /// Get all webhook subscriptions
        /// </summary>
        [HttpGet("subscriptions")]
        [ProducesResponseType(typeof(IEnumerable<WebhookSubscriptionDto>), 200)]
        public async Task<ActionResult<IEnumerable<WebhookSubscriptionDto>>> GetSubscriptions([FromQuery] string? eventType = null)
        {
            try
            {
                var clientCode = GetClientCodeFromToken();
                if (string.IsNullOrEmpty(clientCode))
                {
                    return BadRequest(new { success = false, message = "Client code not found in token" });
                }

                var subscriptions = await _webhookService.GetSubscriptionsAsync(clientCode, eventType);
                return Ok(new { success = true, data = subscriptions });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting webhook subscriptions");
                return StatusCode(500, new { success = false, message = "Error retrieving subscriptions", error = ex.Message });
            }
        }

        /// <summary>
        /// Unsubscribe from webhook events
        /// </summary>
        [HttpDelete("subscriptions/{id}")]
        [ProducesResponseType(200)]
        public async Task<ActionResult> Unsubscribe(int id)
        {
            try
            {
                var clientCode = GetClientCodeFromToken();
                if (string.IsNullOrEmpty(clientCode))
                {
                    return BadRequest(new { success = false, message = "Client code not found in token" });
                }

                var result = await _webhookService.UnsubscribeAsync(id, clientCode);
                if (!result)
                {
                    return NotFound(new { success = false, message = "Subscription not found" });
                }

                return Ok(new { success = true, message = "Unsubscribed successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error unsubscribing from webhook");
                return StatusCode(500, new { success = false, message = "Error unsubscribing", error = ex.Message });
            }
        }

        /// <summary>
        /// Get webhook event history
        /// </summary>
        [HttpGet("events")]
        [ProducesResponseType(typeof(IEnumerable<WebhookEventDto>), 200)]
        public async Task<ActionResult<IEnumerable<WebhookEventDto>>> GetEvents(
            [FromQuery] string? eventType = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50)
        {
            try
            {
                var clientCode = GetClientCodeFromToken();
                if (string.IsNullOrEmpty(clientCode))
                {
                    return BadRequest(new { success = false, message = "Client code not found in token" });
                }

                var events = await _webhookService.GetWebhookEventsAsync(clientCode, eventType, page, pageSize);
                return Ok(new { success = true, data = events });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting webhook events");
                return StatusCode(500, new { success = false, message = "Error retrieving events", error = ex.Message });
            }
        }

        /// <summary>
        /// Retry failed webhook deliveries
        /// </summary>
        [HttpPost("retry-failed")]
        [ProducesResponseType(200)]
        public async Task<ActionResult> RetryFailed()
        {
            try
            {
                var clientCode = GetClientCodeFromToken();
                if (string.IsNullOrEmpty(clientCode))
                {
                    return BadRequest(new { success = false, message = "Client code not found in token" });
                }

                var retriedCount = await _webhookService.RetryFailedWebhooksAsync(clientCode);
                return Ok(new { success = true, message = $"Retried {retriedCount} webhook(s)" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrying failed webhooks");
                return StatusCode(500, new { success = false, message = "Error retrying webhooks", error = ex.Message });
            }
        }

        private string? GetClientCodeFromToken()
        {
            return User.FindFirst("ClientCode")?.Value;
        }
    }
}

