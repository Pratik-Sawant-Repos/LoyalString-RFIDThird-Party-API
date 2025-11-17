using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RfidAppApi.DTOs;
using RfidAppApi.Services;
using System.Security.Claims;

namespace RfidAppApi.Controllers
{
    /// <summary>
    /// Controller for customer management
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class CustomerController : ControllerBase
    {
        private readonly ICustomerService _customerService;
        private readonly ILogger<CustomerController> _logger;

        public CustomerController(
            ICustomerService customerService,
            ILogger<CustomerController> logger)
        {
            _customerService = customerService;
            _logger = logger;
        }

        /// <summary>
        /// Get all customers
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<CustomerDto>), 200)]
        public async Task<ActionResult<IEnumerable<CustomerDto>>> GetAllCustomers()
        {
            try
            {
                var clientCode = GetClientCodeFromToken();
                if (string.IsNullOrEmpty(clientCode))
                {
                    return BadRequest(new { success = false, message = "Client code not found in token" });
                }

                var customers = await _customerService.GetAllCustomersAsync(clientCode);
                return Ok(new { success = true, data = customers });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all customers");
                return StatusCode(500, new { success = false, message = "Error retrieving customers", error = ex.Message });
            }
        }

        /// <summary>
        /// Get customer by ID
        /// </summary>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(CustomerDto), 200)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<CustomerDto>> GetCustomerById(int id)
        {
            try
            {
                var clientCode = GetClientCodeFromToken();
                if (string.IsNullOrEmpty(clientCode))
                {
                    return BadRequest(new { success = false, message = "Client code not found in token" });
                }

                var customer = await _customerService.GetCustomerByIdAsync(id, clientCode);
                if (customer == null)
                {
                    return NotFound(new { success = false, message = $"Customer with ID {id} not found" });
                }

                return Ok(new { success = true, data = customer });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting customer by ID: {CustomerId}", id);
                return StatusCode(500, new { success = false, message = "Error retrieving customer", error = ex.Message });
            }
        }

        /// <summary>
        /// Search customers by name or email
        /// </summary>
        [HttpGet("search")]
        [ProducesResponseType(typeof(IEnumerable<CustomerDto>), 200)]
        public async Task<ActionResult<IEnumerable<CustomerDto>>> SearchCustomers([FromQuery] string searchTerm)
        {
            try
            {
                var clientCode = GetClientCodeFromToken();
                if (string.IsNullOrEmpty(clientCode))
                {
                    return BadRequest(new { success = false, message = "Client code not found in token" });
                }

                if (string.IsNullOrWhiteSpace(searchTerm))
                {
                    return BadRequest(new { success = false, message = "Search term is required" });
                }

                var customers = await _customerService.SearchCustomersAsync(searchTerm, clientCode);
                return Ok(new { success = true, data = customers });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching customers: {SearchTerm}", searchTerm);
                return StatusCode(500, new { success = false, message = "Error searching customers", error = ex.Message });
            }
        }

        /// <summary>
        /// Create a new customer
        /// </summary>
        [HttpPost]
        [ProducesResponseType(typeof(CustomerDto), 201)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<CustomerDto>> CreateCustomer([FromBody] CreateCustomerDto createDto)
        {
            try
            {
                var clientCode = GetClientCodeFromToken();
                if (string.IsNullOrEmpty(clientCode))
                {
                    return BadRequest(new { success = false, message = "Client code not found in token" });
                }

                var customer = await _customerService.CreateCustomerAsync(createDto, clientCode);
                return CreatedAtAction(nameof(GetCustomerById), new { id = customer.Id }, 
                    new { success = true, message = "Customer created successfully", data = customer });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating customer");
                return StatusCode(500, new { success = false, message = "Error creating customer", error = ex.Message });
            }
        }

        /// <summary>
        /// Update customer information
        /// </summary>
        [HttpPut("{id}")]
        [ProducesResponseType(typeof(CustomerDto), 200)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<CustomerDto>> UpdateCustomer(int id, [FromBody] UpdateCustomerDto updateDto)
        {
            try
            {
                var clientCode = GetClientCodeFromToken();
                if (string.IsNullOrEmpty(clientCode))
                {
                    return BadRequest(new { success = false, message = "Client code not found in token" });
                }

                var customer = await _customerService.UpdateCustomerAsync(id, updateDto, clientCode);
                return Ok(new { success = true, message = "Customer updated successfully", data = customer });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating customer: {CustomerId}", id);
                return StatusCode(500, new { success = false, message = "Error updating customer", error = ex.Message });
            }
        }

        /// <summary>
        /// Delete a customer (soft delete)
        /// </summary>
        [HttpDelete("{id}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        public async Task<ActionResult> DeleteCustomer(int id)
        {
            try
            {
                var clientCode = GetClientCodeFromToken();
                if (string.IsNullOrEmpty(clientCode))
                {
                    return BadRequest(new { success = false, message = "Client code not found in token" });
                }

                var result = await _customerService.DeleteCustomerAsync(id, clientCode);
                if (!result)
                {
                    return NotFound(new { success = false, message = $"Customer with ID {id} not found" });
                }

                return Ok(new { success = true, message = "Customer deleted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting customer: {CustomerId}", id);
                return StatusCode(500, new { success = false, message = "Error deleting customer", error = ex.Message });
            }
        }

        private string? GetClientCodeFromToken()
        {
            return User.FindFirst("ClientCode")?.Value;
        }
    }
}

