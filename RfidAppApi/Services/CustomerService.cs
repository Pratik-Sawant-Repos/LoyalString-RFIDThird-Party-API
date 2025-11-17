using Microsoft.EntityFrameworkCore;
using RfidAppApi.Data;
using RfidAppApi.DTOs;
using RfidAppApi.Models;

namespace RfidAppApi.Services
{
    /// <summary>
    /// Service implementation for customer management
    /// </summary>
    public class CustomerService : ICustomerService
    {
        private readonly ClientDbContextFactory _dbContextFactory;
        private readonly ILogger<CustomerService> _logger;

        public CustomerService(
            ClientDbContextFactory dbContextFactory,
            ILogger<CustomerService> logger)
        {
            _dbContextFactory = dbContextFactory;
            _logger = logger;
        }

        public async Task<IEnumerable<CustomerDto>> GetAllCustomersAsync(string clientCode)
        {
            using var context = await _dbContextFactory.CreateAsync(clientCode);
            
            var customers = await context.Customers
                .Where(c => c.IsActive)
                .OrderBy(c => c.CustomerName)
                .ToListAsync();

            return customers.Select(MapToDto);
        }

        public async Task<CustomerDto?> GetCustomerByIdAsync(int id, string clientCode)
        {
            using var context = await _dbContextFactory.CreateAsync(clientCode);
            
            var customer = await context.Customers
                .FirstOrDefaultAsync(c => c.Id == id && c.IsActive);

            return customer != null ? MapToDto(customer) : null;
        }

        public async Task<CustomerDto?> GetCustomerByEmailAsync(string email, string clientCode)
        {
            using var context = await _dbContextFactory.CreateAsync(clientCode);
            
            var customer = await context.Customers
                .FirstOrDefaultAsync(c => c.Email == email && c.IsActive);

            return customer != null ? MapToDto(customer) : null;
        }

        public async Task<IEnumerable<CustomerDto>> SearchCustomersAsync(string searchTerm, string clientCode)
        {
            using var context = await _dbContextFactory.CreateAsync(clientCode);
            
            var customers = await context.Customers
                .Where(c => c.IsActive && 
                    (c.CustomerName.Contains(searchTerm) || 
                     c.Email.Contains(searchTerm) ||
                     (c.MobileNumber != null && c.MobileNumber.Contains(searchTerm))))
                .OrderBy(c => c.CustomerName)
                .ToListAsync();

            return customers.Select(MapToDto);
        }

        public async Task<CustomerDto> CreateCustomerAsync(CreateCustomerDto createDto, string clientCode)
        {
            using var context = await _dbContextFactory.CreateAsync(clientCode);

            // Check if customer with same email already exists
            var existingCustomer = await context.Customers
                .FirstOrDefaultAsync(c => c.Email == createDto.Email && c.ClientCode == clientCode);

            if (existingCustomer != null)
            {
                throw new InvalidOperationException($"Customer with email {createDto.Email} already exists");
            }

            var customer = new Customer
            {
                ClientCode = clientCode,
                CustomerName = createDto.CustomerName,
                Email = createDto.Email,
                MobileNumber = createDto.MobileNumber,
                AlternatePhone = createDto.AlternatePhone,
                Address = createDto.Address,
                City = createDto.City,
                State = createDto.State,
                PinCode = createDto.PinCode,
                Country = createDto.Country,
                CustomerType = createDto.CustomerType,
                CompanyName = createDto.CompanyName,
                GSTNumber = createDto.GSTNumber,
                Notes = createDto.Notes,
                CreatedOn = DateTime.UtcNow,
                IsActive = true
            };

            context.Customers.Add(customer);
            await context.SaveChangesAsync();

            _logger.LogInformation("Customer created: {CustomerId} - {CustomerName}", customer.Id, customer.CustomerName);

            return MapToDto(customer);
        }

        public async Task<CustomerDto> UpdateCustomerAsync(int id, UpdateCustomerDto updateDto, string clientCode)
        {
            using var context = await _dbContextFactory.CreateAsync(clientCode);

            var customer = await context.Customers
                .FirstOrDefaultAsync(c => c.Id == id && c.ClientCode == clientCode);

            if (customer == null)
            {
                throw new KeyNotFoundException($"Customer with ID {id} not found");
            }

            // Update fields if provided
            if (!string.IsNullOrEmpty(updateDto.CustomerName))
                customer.CustomerName = updateDto.CustomerName;

            if (!string.IsNullOrEmpty(updateDto.Email))
                customer.Email = updateDto.Email;

            if (updateDto.MobileNumber != null)
                customer.MobileNumber = updateDto.MobileNumber;

            if (updateDto.AlternatePhone != null)
                customer.AlternatePhone = updateDto.AlternatePhone;

            if (updateDto.Address != null)
                customer.Address = updateDto.Address;

            if (updateDto.City != null)
                customer.City = updateDto.City;

            if (updateDto.State != null)
                customer.State = updateDto.State;

            if (updateDto.PinCode != null)
                customer.PinCode = updateDto.PinCode;

            if (updateDto.Country != null)
                customer.Country = updateDto.Country;

            if (updateDto.CustomerType != null)
                customer.CustomerType = updateDto.CustomerType;

            if (updateDto.CompanyName != null)
                customer.CompanyName = updateDto.CompanyName;

            if (updateDto.GSTNumber != null)
                customer.GSTNumber = updateDto.GSTNumber;

            if (updateDto.Notes != null)
                customer.Notes = updateDto.Notes;

            if (updateDto.IsActive.HasValue)
                customer.IsActive = updateDto.IsActive.Value;

            customer.UpdatedOn = DateTime.UtcNow;

            await context.SaveChangesAsync();

            _logger.LogInformation("Customer updated: {CustomerId} - {CustomerName}", customer.Id, customer.CustomerName);

            return MapToDto(customer);
        }

        public async Task<bool> DeleteCustomerAsync(int id, string clientCode)
        {
            using var context = await _dbContextFactory.CreateAsync(clientCode);

            var customer = await context.Customers
                .FirstOrDefaultAsync(c => c.Id == id && c.ClientCode == clientCode);

            if (customer == null)
            {
                return false;
            }

            // Soft delete
            customer.IsActive = false;
            customer.UpdatedOn = DateTime.UtcNow;

            await context.SaveChangesAsync();

            _logger.LogInformation("Customer deleted (soft): {CustomerId} - {CustomerName}", customer.Id, customer.CustomerName);

            return true;
        }

        private static CustomerDto MapToDto(Customer customer)
        {
            return new CustomerDto
            {
                Id = customer.Id,
                ClientCode = customer.ClientCode,
                CustomerName = customer.CustomerName,
                Email = customer.Email,
                MobileNumber = customer.MobileNumber,
                AlternatePhone = customer.AlternatePhone,
                Address = customer.Address,
                City = customer.City,
                State = customer.State,
                PinCode = customer.PinCode,
                Country = customer.Country,
                CustomerType = customer.CustomerType,
                CompanyName = customer.CompanyName,
                GSTNumber = customer.GSTNumber,
                Notes = customer.Notes,
                CreatedOn = customer.CreatedOn,
                UpdatedOn = customer.UpdatedOn,
                IsActive = customer.IsActive
            };
        }
    }
}

