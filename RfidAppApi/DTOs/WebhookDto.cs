using System.ComponentModel.DataAnnotations;

namespace RfidAppApi.DTOs
{
    /// <summary>
    /// DTO for creating a webhook subscription
    /// </summary>
    public class CreateWebhookSubscriptionDto
    {
        [Required]
        [StringLength(100)]
        public string EventType { get; set; } = string.Empty; // e.g., "product.bulk_upload.completed", "quotation.*", "invoice.created"

        [Required]
        [StringLength(500)]
        [Url]
        public string WebhookUrl { get; set; } = string.Empty;

        [StringLength(500)]
        public string? SecretKey { get; set; }

        [StringLength(50)]
        public string? Description { get; set; }
    }

    /// <summary>
    /// DTO for webhook subscription response
    /// </summary>
    public class WebhookSubscriptionDto
    {
        public int Id { get; set; }
        public string ClientCode { get; set; } = string.Empty;
        public string EventType { get; set; } = string.Empty;
        public string WebhookUrl { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public string? Description { get; set; }
        public DateTime CreatedOn { get; set; }
        public DateTime? UpdatedOn { get; set; }
    }

    /// <summary>
    /// DTO for webhook event history
    /// </summary>
    public class WebhookEventDto
    {
        public int Id { get; set; }
        public string ClientCode { get; set; } = string.Empty;
        public string EventType { get; set; } = string.Empty;
        public string WebhookUrl { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public int? ResponseStatusCode { get; set; }
        public string? ErrorMessage { get; set; }
        public int RetryCount { get; set; }
        public DateTime CreatedOn { get; set; }
        public DateTime? DeliveredOn { get; set; }
    }

    /// <summary>
    /// Standard webhook payload structure
    /// </summary>
    public class WebhookPayload
    {
        public string EventType { get; set; } = string.Empty;
        public string ClientCode { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public object Data { get; set; } = new();
        public string? Signature { get; set; } // HMAC signature for verification
    }
}

