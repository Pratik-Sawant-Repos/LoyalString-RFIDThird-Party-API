using RfidAppApi.DTOs;

namespace RfidAppApi.Services
{
    /// <summary>
    /// Service interface for customer management
    /// </summary>
    public interface ICustomerService
    {
        /// <summary>
        /// Get all customers for a client
        /// </summary>
        Task<IEnumerable<CustomerDto>> GetAllCustomersAsync(string clientCode);

        /// <summary>
        /// Get customer by ID
        /// </summary>
        Task<CustomerDto?> GetCustomerByIdAsync(int id, string clientCode);

        /// <summary>
        /// Get customer by email
        /// </summary>
        Task<CustomerDto?> GetCustomerByEmailAsync(string email, string clientCode);

        /// <summary>
        /// Search customers by name or email
        /// </summary>
        Task<IEnumerable<CustomerDto>> SearchCustomersAsync(string searchTerm, string clientCode);

        /// <summary>
        /// Create a new customer
        /// </summary>
        Task<CustomerDto> CreateCustomerAsync(CreateCustomerDto createDto, string clientCode);

        /// <summary>
        /// Update customer information
        /// </summary>
        Task<CustomerDto> UpdateCustomerAsync(int id, UpdateCustomerDto updateDto, string clientCode);

        /// <summary>
        /// Delete a customer (soft delete)
        /// </summary>
        Task<bool> DeleteCustomerAsync(int id, string clientCode);
    }
}

