using System.ComponentModel.DataAnnotations;

namespace RfidAppApi.DTOs
{
    /// <summary>
    /// DTO for creating a quotation item
    /// </summary>
    public class CreateQuotationItemDto
    {
        [Required]
        public int ProductId { get; set; }

        [Required]
        [StringLength(50)]
        public string ItemCode { get; set; } = string.Empty;

        [StringLength(50)]
        public string? RfidCode { get; set; }

        [Required]
        public decimal GrossWeight { get; set; }

        public decimal StoneWeight { get; set; } = 0;

        [Required]
        public decimal NetWeight { get; set; }

        public decimal GoldRate { get; set; } = 0;

        public decimal Making { get; set; } = 0;

        [StringLength(20)]
        public string MakingType { get; set; } = "Fixed"; // PerGram, Percentage, Fixed

        public decimal StoneAmount { get; set; } = 0;

        public int Quantity { get; set; } = 1;

        [StringLength(500)]
        public string? Remarks { get; set; }
    }

    /// <summary>
    /// DTO for creating a quotation
    /// </summary>
    public class CreateQuotationDto
    {
        [Required]
        public int CustomerId { get; set; }

        [Required]
        public List<CreateQuotationItemDto> Items { get; set; } = new List<CreateQuotationItemDto>();

        // Old Metal Information (optional)
        public decimal? OldMetalWeight { get; set; }

        public decimal? OldMetalRate { get; set; }

        public decimal? OldMetalAmount { get; set; }

        // Payment Mode
        [StringLength(50)]
        public string? PaymentMode { get; set; }

        // GST Information
        public bool IsGstApplied { get; set; } = false;

        public decimal GstPercentage { get; set; } = 3.00m;

        public DateTime? ValidUntil { get; set; }

        [StringLength(500)]
        public string? Remarks { get; set; }

        // Email sending option
        public bool SendEmail { get; set; } = false;
    }

    /// <summary>
    /// DTO for updating a quotation
    /// </summary>
    public class UpdateQuotationDto
    {
        public int? CustomerId { get; set; }

        public List<CreateQuotationItemDto>? Items { get; set; }

        public decimal? OldMetalWeight { get; set; }

        public decimal? OldMetalRate { get; set; }

        public decimal? OldMetalAmount { get; set; }

        [StringLength(50)]
        public string? PaymentMode { get; set; }

        public bool? IsGstApplied { get; set; }

        public decimal? GstPercentage { get; set; }

        public DateTime? ValidUntil { get; set; }

        [StringLength(20)]
        public string? Status { get; set; }

        [StringLength(500)]
        public string? Remarks { get; set; }
    }

    /// <summary>
    /// DTO for quotation item response
    /// </summary>
    public class QuotationItemDto
    {
        public int Id { get; set; }
        public int QuotationId { get; set; }
        public int ProductId { get; set; }
        public string ItemCode { get; set; } = string.Empty;
        public string? RfidCode { get; set; }
        public string DesignName { get; set; } = string.Empty;
        public string Purity { get; set; } = string.Empty;
        public decimal GrossWeight { get; set; }
        public decimal StoneWeight { get; set; }
        public decimal NetWeight { get; set; }
        public decimal GoldRate { get; set; }
        public decimal Making { get; set; }
        public string MakingType { get; set; } = string.Empty;
        public decimal StoneAmount { get; set; }
        public decimal MakingAmount { get; set; }
        public decimal MetalAmount { get; set; }
        public decimal ItemAmount { get; set; }
        public int Quantity { get; set; }
        public string? Remarks { get; set; }
    }

    /// <summary>
    /// DTO for quotation response
    /// </summary>
    public class QuotationDto
    {
        public int Id { get; set; }
        public string ClientCode { get; set; } = string.Empty;
        public string QuotationNumber { get; set; } = string.Empty;
        public int CustomerId { get; set; }
        public CustomerDto? Customer { get; set; }
        public int Quantity { get; set; }
        public decimal TotalGrossWeight { get; set; }
        public decimal? OldMetalWeight { get; set; }
        public decimal? OldMetalRate { get; set; }
        public decimal? OldMetalAmount { get; set; }
        public string? PaymentMode { get; set; }
        public bool IsGstApplied { get; set; }
        public decimal GstPercentage { get; set; }
        public decimal GstAmount { get; set; }
        public decimal SubTotalAmount { get; set; }
        public decimal TotalAmount { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime QuotationDate { get; set; }
        public DateTime? ValidUntil { get; set; }
        public DateTime CreatedOn { get; set; }
        public DateTime? UpdatedOn { get; set; }
        public string? Remarks { get; set; }
        public bool IsActive { get; set; }
        public List<QuotationItemDto> Items { get; set; } = new List<QuotationItemDto>();
    }

    /// <summary>
    /// DTO for sending quotation via email
    /// </summary>
    public class SendQuotationEmailDto
    {
        [Required]
        public int QuotationId { get; set; }

        [StringLength(100)]
        [EmailAddress]
        public string? ToEmail { get; set; } // Optional, will use customer email if not provided

        [StringLength(500)]
        public string? AdditionalMessage { get; set; }
    }
}

