using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RfidAppApi.DTOs;
using RfidAppApi.Extensions;
using RfidAppApi.Services;
using System.Security.Claims;

namespace RfidAppApi.Controllers
{
    /// <summary>
    /// Controller for dashboard analytics and reporting
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class DashboardController : ControllerBase
    {
        private readonly IDashboardService _dashboardService;
        private readonly ILogger<DashboardController> _logger;

        public DashboardController(
            IDashboardService dashboardService,
            ILogger<DashboardController> logger)
        {
            _dashboardService = dashboardService;
            _logger = logger;
        }

        /// <summary>
        /// Get comprehensive dashboard summary with all important metrics in one call
        /// Returns: Total products, sold products, weights (gross/net/stone), sales metrics, customer metrics, category/branch metrics, inventory value, and RFID metrics
        /// </summary>
        [HttpGet("summary")]
        [ProducesResponseType(typeof(DashboardSummaryDto), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> GetDashboardSummary()
        {
            try
            {
                var clientCode = GetClientCodeFromToken();
                if (string.IsNullOrEmpty(clientCode))
                    return BadRequest(new { success = false, message = "Client code not found in token" });

                var summary = await _dashboardService.GetDashboardSummaryAsync(clientCode);
                return SuccessResponse(summary, "Dashboard summary retrieved successfully");
            }
            catch (Exception ex)
            {
                return ErrorResponse(500, "Failed to retrieve dashboard summary", ex);
            }
        }

        /// <summary>
        /// Get comprehensive dashboard data
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(DashboardDto), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> GetDashboard()
        {
            try
            {
                var clientCode = GetClientCodeFromToken();
                if (string.IsNullOrEmpty(clientCode))
                    return BadRequest(new { success = false, message = "Client code not found in token" });

                var dashboard = await _dashboardService.GetDashboardDataAsync(clientCode);
                return SuccessResponse(dashboard, "Dashboard data retrieved successfully");
            }
            catch (Exception ex)
            {
                return ErrorResponse(500, "Failed to retrieve dashboard data", ex);
            }
        }

        /// <summary>
        /// Get total product count
        /// </summary>
        [HttpGet("total-products")]
        [ProducesResponseType(typeof(int), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> GetTotalProductCount()
        {
            try
            {
                var clientCode = GetClientCodeFromToken();
                if (string.IsNullOrEmpty(clientCode))
                    return BadRequest(new { success = false, message = "Client code not found in token" });

                var count = await _dashboardService.GetTotalProductCountAsync(clientCode);
                return SuccessResponse(count, $"Total products: {count}");
            }
            catch (Exception ex)
            {
                return ErrorResponse(500, "Failed to retrieve total product count", ex);
            }
        }

        /// <summary>
        /// Get total sold products count
        /// </summary>
        [HttpGet("total-sold-products")]
        [ProducesResponseType(typeof(int), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> GetTotalSoldProducts()
        {
            try
            {
                var clientCode = GetClientCodeFromToken();
                if (string.IsNullOrEmpty(clientCode))
                    return BadRequest(new { success = false, message = "Client code not found in token" });

                var count = await _dashboardService.GetTotalSoldProductsAsync(clientCode);
                return SuccessResponse(count, $"Total sold products: {count}");
            }
            catch (Exception ex)
            {
                return ErrorResponse(500, "Failed to retrieve total sold products count", ex);
            }
        }

        /// <summary>
        /// Get recently added products
        /// </summary>
        [HttpGet("recently-added")]
        [ProducesResponseType(typeof(List<RecentlyAddedProductDto>), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> GetRecentlyAddedProducts([FromQuery] int count = 10)
        {
            try
            {
                var clientCode = GetClientCodeFromToken();
                if (string.IsNullOrEmpty(clientCode))
                    return BadRequest(new { success = false, message = "Client code not found in token" });

                var products = await _dashboardService.GetRecentlyAddedProductsAsync(clientCode, count);
                return SuccessResponse(products, $"Found {products.Count} recently added product(s)");
            }
            catch (Exception ex)
            {
                return ErrorResponse(500, "Failed to retrieve recently added products", ex);
            }
        }

        /// <summary>
        /// Get sales data over months
        /// </summary>
        [HttpGet("sales-over-month")]
        [ProducesResponseType(typeof(List<MonthlySalesDto>), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> GetSalesOverMonth([FromQuery] int months = 12)
        {
            try
            {
                var clientCode = GetClientCodeFromToken();
                if (string.IsNullOrEmpty(clientCode))
                    return BadRequest(new { success = false, message = "Client code not found in token" });

                var sales = await _dashboardService.GetSalesOverMonthAsync(clientCode, months);
                return SuccessResponse(sales, $"Found sales data for {sales.Count} month(s)");
            }
            catch (Exception ex)
            {
                return ErrorResponse(500, "Failed to retrieve sales over month data", ex);
            }
        }

        /// <summary>
        /// Get sales month growth percentage
        /// </summary>
        [HttpGet("sales-growth")]
        [ProducesResponseType(typeof(decimal), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> GetSalesMonthGrowth()
        {
            try
            {
                var clientCode = GetClientCodeFromToken();
                if (string.IsNullOrEmpty(clientCode))
                    return BadRequest(new { success = false, message = "Client code not found in token" });

                var growth = await _dashboardService.GetSalesMonthGrowthAsync(clientCode);
                return SuccessResponse(growth, $"Sales month growth: {growth:F2}%");
            }
            catch (Exception ex)
            {
                return ErrorResponse(500, "Failed to retrieve sales month growth", ex);
            }
        }

        /// <summary>
        /// Get which user added which products
        /// </summary>
        [HttpGet("user-product-creations")]
        [ProducesResponseType(typeof(List<UserProductCreationDto>), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> GetUserProductCreations()
        {
            try
            {
                var clientCode = GetClientCodeFromToken();
                if (string.IsNullOrEmpty(clientCode))
                    return BadRequest(new { success = false, message = "Client code not found in token" });

                var creations = await _dashboardService.GetUserProductCreationsAsync(clientCode);
                return SuccessResponse(creations, $"Found product creations for {creations.Count} user(s)");
            }
            catch (Exception ex)
            {
                return ErrorResponse(500, "Failed to retrieve user product creations", ex);
            }
        }

        /// <summary>
        /// Get top selling products
        /// </summary>
        [HttpGet("top-selling")]
        [ProducesResponseType(typeof(List<TopSellingProductDto>), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> GetTopSellingProducts([FromQuery] int topCount = 10)
        {
            try
            {
                var clientCode = GetClientCodeFromToken();
                if (string.IsNullOrEmpty(clientCode))
                    return BadRequest(new { success = false, message = "Client code not found in token" });

                var products = await _dashboardService.GetTopSellingProductsAsync(clientCode, topCount);
                return SuccessResponse(products, $"Found {products.Count} top selling product(s)");
            }
            catch (Exception ex)
            {
                return ErrorResponse(500, "Failed to retrieve top selling products", ex);
            }
        }

        /// <summary>
        /// Get total weight of all products by category
        /// </summary>
        [HttpGet("weight-by-category")]
        [ProducesResponseType(typeof(List<CategoryWeightDto>), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> GetTotalWeightByCategory()
        {
            try
            {
                var clientCode = GetClientCodeFromToken();
                if (string.IsNullOrEmpty(clientCode))
                    return BadRequest(new { success = false, message = "Client code not found in token" });

                var weights = await _dashboardService.GetTotalWeightByCategoryAsync(clientCode);
                return SuccessResponse(weights, $"Found weight data for {weights.Count} categor(ies)");
            }
            catch (Exception ex)
            {
                return ErrorResponse(500, "Failed to retrieve weight by category data", ex);
            }
        }

        /// <summary>
        /// Get customer summary information
        /// </summary>
        [HttpGet("customer-summary")]
        [ProducesResponseType(typeof(CustomerSummaryDto), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> GetCustomerSummary()
        {
            try
            {
                var clientCode = GetClientCodeFromToken();
                if (string.IsNullOrEmpty(clientCode))
                    return BadRequest(new { success = false, message = "Client code not found in token" });

                var summary = await _dashboardService.GetCustomerSummaryAsync(clientCode);
                return SuccessResponse(summary, "Customer summary retrieved successfully");
            }
            catch (Exception ex)
            {
                return ErrorResponse(500, "Failed to retrieve customer summary", ex);
            }
        }

        /// <summary>
        /// Download all dashboard data in Excel format
        /// </summary>
        [HttpGet("export-excel")]
        [ProducesResponseType(typeof(FileResult), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> ExportDashboardDataToExcel()
        {
            try
            {
                var clientCode = GetClientCodeFromToken();
                if (string.IsNullOrEmpty(clientCode))
                    return BadRequest(new { success = false, message = "Client code not found in token" });

                var excelData = await _dashboardService.ExportDashboardDataToExcelAsync(clientCode);
                var fileName = $"Dashboard_Export_{clientCode}_{DateTime.UtcNow:yyyyMMdd_HHmmss}.xlsx";

                return File(excelData, 
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", 
                    fileName);
            }
            catch (Exception ex)
            {
                return ErrorResponse(500, "Failed to export dashboard data to Excel", ex);
            }
        }

        #region Helper Methods

        private string? GetClientCodeFromToken()
        {
            return User.FindFirst("ClientCode")?.Value;
        }

        private IActionResult SuccessResponse(object? data, string message)
        {
            return Ok(new
            {
                success = true,
                message = message,
                data = data
            });
        }

        private IActionResult ErrorResponse(int statusCode, string message, Exception ex)
        {
            _logger.LogError(ex, message);
            return StatusCode(statusCode, new
            {
                success = false,
                message = message,
                error = ex.Message
            });
        }

        #endregion
    }
}

