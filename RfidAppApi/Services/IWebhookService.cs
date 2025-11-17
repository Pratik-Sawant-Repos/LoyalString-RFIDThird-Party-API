using RfidAppApi.DTOs;

namespace RfidAppApi.Services
{
    /// <summary>
    /// Service interface for webhook management and delivery
    /// </summary>
    public interface IWebhookService
    {
        /// <summary>
        /// Subscribe to webhook events
        /// </summary>
        Task<WebhookSubscriptionDto> SubscribeAsync(CreateWebhookSubscriptionDto subscription, string clientCode);

        /// <summary>
        /// Unsubscribe from webhook events
        /// </summary>
        Task<bool> UnsubscribeAsync(int subscriptionId, string clientCode);

        /// <summary>
        /// Get all subscriptions for a client
        /// </summary>
        Task<IEnumerable<WebhookSubscriptionDto>> GetSubscriptionsAsync(string clientCode, string? eventType = null);

        /// <summary>
        /// Trigger a webhook event (async delivery)
        /// </summary>
        Task TriggerWebhookAsync(string eventType, object payload, string clientCode, string? specificUrl = null);

        /// <summary>
        /// Get webhook event history
        /// </summary>
        Task<IEnumerable<WebhookEventDto>> GetWebhookEventsAsync(string clientCode, string? eventType = null, int page = 1, int pageSize = 50);

        /// <summary>
        /// Retry failed webhook deliveries
        /// </summary>
        Task<int> RetryFailedWebhooksAsync(string clientCode);
    }
}

