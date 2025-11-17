using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RfidAppApi.Models
{
    /// <summary>
    /// Model for storing webhook events and their delivery status
    /// </summary>
    public class WebhookEvent
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string ClientCode { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string EventType { get; set; } = string.Empty; // e.g., "product.bulk_upload.completed", "quotation.email.sent"

        [Required]
        [StringLength(500)]
        public string WebhookUrl { get; set; } = string.Empty;

        [Required]
        [StringLength(20)]
        public string Status { get; set; } = "Pending"; // Pending, Delivered, Failed, Retrying

        [Column(TypeName = "nvarchar(max)")]
        public string Payload { get; set; } = string.Empty; // JSON payload

        [Column(TypeName = "nvarchar(max)")]
        public string? ResponseBody { get; set; } // Response from webhook endpoint

        public int? ResponseStatusCode { get; set; }

        [StringLength(1000)]
        public string? ErrorMessage { get; set; }

        public int RetryCount { get; set; } = 0;

        public int MaxRetries { get; set; } = 3;

        public DateTime CreatedOn { get; set; } = DateTime.UtcNow;

        public DateTime? DeliveredOn { get; set; }

        public DateTime? NextRetryAt { get; set; }

        public bool IsActive { get; set; } = true;
    }

    /// <summary>
    /// Model for webhook subscriptions (client can subscribe to specific events)
    /// </summary>
    public class WebhookSubscription
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string ClientCode { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string EventType { get; set; } = string.Empty; // e.g., "product.*", "quotation.created", "invoice.*"

        [Required]
        [StringLength(500)]
        public string WebhookUrl { get; set; } = string.Empty;

        [StringLength(500)]
        public string? SecretKey { get; set; } // For webhook signature verification

        [StringLength(50)]
        public string? Description { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedOn { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedOn { get; set; }
    }
}

