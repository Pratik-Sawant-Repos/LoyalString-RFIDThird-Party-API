using System.ComponentModel.DataAnnotations;

namespace RfidAppApi.DTOs
{
    /// <summary>
    /// DTO for creating a new customer
    /// </summary>
    public class CreateCustomerDto
    {
        [Required]
        [StringLength(150)]
        public string CustomerName { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [StringLength(15)]
        public string? MobileNumber { get; set; }

        [StringLength(15)]
        public string? AlternatePhone { get; set; }

        [StringLength(255)]
        public string? Address { get; set; }

        [StringLength(100)]
        public string? City { get; set; }

        [StringLength(100)]
        public string? State { get; set; }

        [StringLength(10)]
        public string? PinCode { get; set; }

        [StringLength(100)]
        public string? Country { get; set; }

        [StringLength(50)]
        public string? CustomerType { get; set; }

        [StringLength(100)]
        public string? CompanyName { get; set; }

        [StringLength(50)]
        public string? GSTNumber { get; set; }

        [StringLength(500)]
        public string? Notes { get; set; }
    }

    /// <summary>
    /// DTO for updating customer information
    /// </summary>
    public class UpdateCustomerDto
    {
        [StringLength(150)]
        public string? CustomerName { get; set; }

        [StringLength(100)]
        [EmailAddress]
        public string? Email { get; set; }

        [StringLength(15)]
        public string? MobileNumber { get; set; }

        [StringLength(15)]
        public string? AlternatePhone { get; set; }

        [StringLength(255)]
        public string? Address { get; set; }

        [StringLength(100)]
        public string? City { get; set; }

        [StringLength(100)]
        public string? State { get; set; }

        [StringLength(10)]
        public string? PinCode { get; set; }

        [StringLength(100)]
        public string? Country { get; set; }

        [StringLength(50)]
        public string? CustomerType { get; set; }

        [StringLength(100)]
        public string? CompanyName { get; set; }

        [StringLength(50)]
        public string? GSTNumber { get; set; }

        [StringLength(500)]
        public string? Notes { get; set; }

        public bool? IsActive { get; set; }
    }

    /// <summary>
    /// DTO for customer response
    /// </summary>
    public class CustomerDto
    {
        public int Id { get; set; }
        public string ClientCode { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? MobileNumber { get; set; }
        public string? AlternatePhone { get; set; }
        public string? Address { get; set; }
        public string? City { get; set; }
        public string? State { get; set; }
        public string? PinCode { get; set; }
        public string? Country { get; set; }
        public string? CustomerType { get; set; }
        public string? CompanyName { get; set; }
        public string? GSTNumber { get; set; }
        public string? Notes { get; set; }
        public DateTime CreatedOn { get; set; }
        public DateTime? UpdatedOn { get; set; }
        public bool IsActive { get; set; }
    }
}

