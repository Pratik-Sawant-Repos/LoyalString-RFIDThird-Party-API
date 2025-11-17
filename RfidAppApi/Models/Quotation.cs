using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RfidAppApi.Models
{
    /// <summary>
    /// Model for storing quotation header information
    /// </summary>
    public class Quotation
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string ClientCode { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string QuotationNumber { get; set; } = string.Empty;

        [Required]
        public int CustomerId { get; set; }

        // Summary Information
        [Required]
        public int Quantity { get; set; } = 0;

        [Column(TypeName = "decimal(18,3)")]
        public decimal TotalGrossWeight { get; set; } = 0;

        // Old Metal Information (optional)
        [Column(TypeName = "decimal(18,3)")]
        public decimal? OldMetalWeight { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? OldMetalRate { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? OldMetalAmount { get; set; }

        // Payment Mode
        [StringLength(50)]
        public string? PaymentMode { get; set; } // Cash, Card, UPI, Cheque, etc.

        // GST Information
        public bool IsGstApplied { get; set; } = false;

        [Column(TypeName = "decimal(5,2)")]
        public decimal GstPercentage { get; set; } = 3.00m; // Default 3% GST

        [Column(TypeName = "decimal(18,2)")]
        public decimal GstAmount { get; set; } = 0;

        [Column(TypeName = "decimal(18,2)")]
        public decimal SubTotalAmount { get; set; } = 0; // Amount before GST

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmount { get; set; } = 0; // Final amount including GST

        // Status and Dates
        [StringLength(20)]
        public string Status { get; set; } = "Draft"; // Draft, Sent, Accepted, Rejected, Expired

        public DateTime QuotationDate { get; set; } = DateTime.UtcNow;

        public DateTime? ValidUntil { get; set; } // Quotation validity period

        public DateTime CreatedOn { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedOn { get; set; }

        [StringLength(500)]
        public string? Remarks { get; set; }

        public bool IsActive { get; set; } = true;

        // Navigation properties
        [ForeignKey("CustomerId")]
        public virtual Customer Customer { get; set; } = null!;

        public virtual ICollection<QuotationItem> QuotationItems { get; set; } = new List<QuotationItem>();
    }

    /// <summary>
    /// Model for storing individual items in a quotation
    /// Links to products from inventory (ProductDetails)
    /// </summary>
    public class QuotationItem
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int QuotationId { get; set; }

        [Required]
        [StringLength(50)]
        public string ClientCode { get; set; } = string.Empty;

        // Product Reference (from inventory)
        [Required]
        public int ProductId { get; set; } // Reference to ProductDetails

        [Required]
        [StringLength(50)]
        public string ItemCode { get; set; } = string.Empty;

        [StringLength(50)]
        public string? RfidCode { get; set; }

        // Product Details (stored for historical reference)
        [Required]
        [StringLength(100)]
        public string DesignName { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string Purity { get; set; } = string.Empty;

        // Weight Details
        [Required]
        [Column(TypeName = "decimal(18,3)")]
        public decimal GrossWeight { get; set; }

        [Column(TypeName = "decimal(18,3)")]
        public decimal StoneWeight { get; set; } = 0;

        [Required]
        [Column(TypeName = "decimal(18,3)")]
        public decimal NetWeight { get; set; }

        // Rate and Amount Details
        [Column(TypeName = "decimal(18,2)")]
        public decimal GoldRate { get; set; } = 0; // Per gram rate

        [Column(TypeName = "decimal(18,2)")]
        public decimal Making { get; set; } = 0; // Making charge (can be per gram, percentage, or fixed)

        [StringLength(20)]
        public string MakingType { get; set; } = "Fixed"; // PerGram, Percentage, Fixed

        [Column(TypeName = "decimal(18,2)")]
        public decimal StoneAmount { get; set; } = 0;

        // Calculated Amounts
        [Column(TypeName = "decimal(18,2)")]
        public decimal MakingAmount { get; set; } = 0;

        [Column(TypeName = "decimal(18,2)")]
        public decimal MetalAmount { get; set; } = 0; // (NetWeight * GoldRate)

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal ItemAmount { get; set; } = 0; // MetalAmount + MakingAmount + StoneAmount

        public int Quantity { get; set; } = 1;

        [StringLength(500)]
        public string? Remarks { get; set; }

        public DateTime CreatedOn { get; set; } = DateTime.UtcNow;

        // Navigation properties
        [ForeignKey("QuotationId")]
        public virtual Quotation Quotation { get; set; } = null!;

        [ForeignKey("ProductId")]
        public virtual ProductDetails? Product { get; set; }
    }
}

