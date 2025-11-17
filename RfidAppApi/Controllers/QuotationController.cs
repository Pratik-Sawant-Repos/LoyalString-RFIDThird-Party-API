using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RfidAppApi.DTOs;
using RfidAppApi.Services;
using System.Security.Claims;

namespace RfidAppApi.Controllers
{
    /// <summary>
    /// Controller for quotation management
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class QuotationController : ControllerBase
    {
        private readonly IQuotationService _quotationService;
        private readonly ILogger<QuotationController> _logger;

        public QuotationController(
            IQuotationService quotationService,
            ILogger<QuotationController> logger)
        {
            _quotationService = quotationService;
            _logger = logger;
        }

        /// <summary>
        /// Get all quotations
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<QuotationDto>), 200)]
        public async Task<ActionResult<IEnumerable<QuotationDto>>> GetAllQuotations()
        {
            try
            {
                var clientCode = GetClientCodeFromToken();
                if (string.IsNullOrEmpty(clientCode))
                {
                    return BadRequest(new { success = false, message = "Client code not found in token" });
                }

                var quotations = await _quotationService.GetAllQuotationsAsync(clientCode);
                return Ok(new { success = true, data = quotations });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all quotations");
                return StatusCode(500, new { success = false, message = "Error retrieving quotations", error = ex.Message });
            }
        }

        /// <summary>
        /// Get quotation by ID
        /// </summary>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(QuotationDto), 200)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<QuotationDto>> GetQuotationById(int id)
        {
            try
            {
                var clientCode = GetClientCodeFromToken();
                if (string.IsNullOrEmpty(clientCode))
                {
                    return BadRequest(new { success = false, message = "Client code not found in token" });
                }

                var quotation = await _quotationService.GetQuotationByIdAsync(id, clientCode);
                if (quotation == null)
                {
                    return NotFound(new { success = false, message = $"Quotation with ID {id} not found" });
                }

                return Ok(new { success = true, data = quotation });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting quotation by ID: {QuotationId}", id);
                return StatusCode(500, new { success = false, message = "Error retrieving quotation", error = ex.Message });
            }
        }

        /// <summary>
        /// Get quotations by customer ID
        /// </summary>
        [HttpGet("customer/{customerId}")]
        [ProducesResponseType(typeof(IEnumerable<QuotationDto>), 200)]
        public async Task<ActionResult<IEnumerable<QuotationDto>>> GetQuotationsByCustomer(int customerId)
        {
            try
            {
                var clientCode = GetClientCodeFromToken();
                if (string.IsNullOrEmpty(clientCode))
                {
                    return BadRequest(new { success = false, message = "Client code not found in token" });
                }

                var quotations = await _quotationService.GetQuotationsByCustomerAsync(customerId, clientCode);
                return Ok(new { success = true, data = quotations });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting quotations for customer: {CustomerId}", customerId);
                return StatusCode(500, new { success = false, message = "Error retrieving quotations", error = ex.Message });
            }
        }

        /// <summary>
        /// Create a new quotation
        /// </summary>
        [HttpPost]
        [ProducesResponseType(typeof(QuotationDto), 201)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<QuotationDto>> CreateQuotation([FromBody] CreateQuotationDto createDto)
        {
            try
            {
                var clientCode = GetClientCodeFromToken();
                if (string.IsNullOrEmpty(clientCode))
                {
                    return BadRequest(new { success = false, message = "Client code not found in token" });
                }

                var quotation = await _quotationService.CreateQuotationAsync(createDto, clientCode);
                return CreatedAtAction(nameof(GetQuotationById), new { id = quotation.Id }, 
                    new { success = true, message = "Quotation created successfully", data = quotation });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { success = false, message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating quotation");
                return StatusCode(500, new { success = false, message = "Error creating quotation", error = ex.Message });
            }
        }

        /// <summary>
        /// Update a quotation
        /// </summary>
        [HttpPut("{id}")]
        [ProducesResponseType(typeof(QuotationDto), 200)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<QuotationDto>> UpdateQuotation(int id, [FromBody] UpdateQuotationDto updateDto)
        {
            try
            {
                var clientCode = GetClientCodeFromToken();
                if (string.IsNullOrEmpty(clientCode))
                {
                    return BadRequest(new { success = false, message = "Client code not found in token" });
                }

                var quotation = await _quotationService.UpdateQuotationAsync(id, updateDto, clientCode);
                return Ok(new { success = true, message = "Quotation updated successfully", data = quotation });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating quotation: {QuotationId}", id);
                return StatusCode(500, new { success = false, message = "Error updating quotation", error = ex.Message });
            }
        }

        /// <summary>
        /// Delete a quotation (soft delete)
        /// </summary>
        [HttpDelete("{id}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        public async Task<ActionResult> DeleteQuotation(int id)
        {
            try
            {
                var clientCode = GetClientCodeFromToken();
                if (string.IsNullOrEmpty(clientCode))
                {
                    return BadRequest(new { success = false, message = "Client code not found in token" });
                }

                var result = await _quotationService.DeleteQuotationAsync(id, clientCode);
                if (!result)
                {
                    return NotFound(new { success = false, message = $"Quotation with ID {id} not found" });
                }

                return Ok(new { success = true, message = "Quotation deleted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting quotation: {QuotationId}", id);
                return StatusCode(500, new { success = false, message = "Error deleting quotation", error = ex.Message });
            }
        }

        /// <summary>
        /// Send quotation via email
        /// </summary>
        [HttpPost("{id}/send-email")]
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        public async Task<ActionResult> SendQuotationEmail(int id, [FromBody] SendQuotationEmailDto? emailDto = null)
        {
            try
            {
                var clientCode = GetClientCodeFromToken();
                if (string.IsNullOrEmpty(clientCode))
                {
                    return BadRequest(new { success = false, message = "Client code not found in token" });
                }

                var result = await _quotationService.SendQuotationEmailAsync(
                    id, 
                    emailDto?.ToEmail, 
                    emailDto?.AdditionalMessage, 
                    clientCode);

                if (!result)
                {
                    return StatusCode(500, new { success = false, message = "Failed to send quotation email" });
                }

                return Ok(new { success = true, message = "Quotation email sent successfully" });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { success = false, message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending quotation email: {QuotationId}", id);
                return StatusCode(500, new { success = false, message = "Error sending quotation email", error = ex.Message });
            }
        }

        private string? GetClientCodeFromToken()
        {
            return User.FindFirst("ClientCode")?.Value;
        }
    }
}

