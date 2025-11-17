using RfidAppApi.DTOs;

namespace RfidAppApi.Services
{
    /// <summary>
    /// Service interface for quotation management
    /// </summary>
    public interface IQuotationService
    {
        /// <summary>
        /// Get all quotations for a client
        /// </summary>
        Task<IEnumerable<QuotationDto>> GetAllQuotationsAsync(string clientCode);

        /// <summary>
        /// Get quotation by ID with all details
        /// </summary>
        Task<QuotationDto?> GetQuotationByIdAsync(int id, string clientCode);

        /// <summary>
        /// Get quotations by customer ID
        /// </summary>
        Task<IEnumerable<QuotationDto>> GetQuotationsByCustomerAsync(int customerId, string clientCode);

        /// <summary>
        /// Create a new quotation
        /// </summary>
        Task<QuotationDto> CreateQuotationAsync(CreateQuotationDto createDto, string clientCode);

        /// <summary>
        /// Update a quotation
        /// </summary>
        Task<QuotationDto> UpdateQuotationAsync(int id, UpdateQuotationDto updateDto, string clientCode);

        /// <summary>
        /// Delete a quotation (soft delete)
        /// </summary>
        Task<bool> DeleteQuotationAsync(int id, string clientCode);

        /// <summary>
        /// Send quotation via email
        /// </summary>
        Task<bool> SendQuotationEmailAsync(int quotationId, string? toEmail, string? additionalMessage, string clientCode);
    }
}

