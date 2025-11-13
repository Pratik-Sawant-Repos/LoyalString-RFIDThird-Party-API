using RfidAppApi.DTOs;

namespace RfidAppApi.Services
{
    /// <summary>
    /// Interface for dashboard analytics service
    /// </summary>
    public interface IDashboardService
    {
        /// <summary>
        /// Get comprehensive dashboard data
        /// </summary>
        Task<DashboardDto> GetDashboardDataAsync(string clientCode);

        /// <summary>
        /// Get total product count
        /// </summary>
        Task<int> GetTotalProductCountAsync(string clientCode);

        /// <summary>
        /// Get total sold products count
        /// </summary>
        Task<int> GetTotalSoldProductsAsync(string clientCode);

        /// <summary>
        /// Get recently added products
        /// </summary>
        Task<List<RecentlyAddedProductDto>> GetRecentlyAddedProductsAsync(string clientCode, int count = 10);

        /// <summary>
        /// Get sales data over months
        /// </summary>
        Task<List<MonthlySalesDto>> GetSalesOverMonthAsync(string clientCode, int months = 12);

        /// <summary>
        /// Get sales month growth percentage
        /// </summary>
        Task<decimal> GetSalesMonthGrowthAsync(string clientCode);

        /// <summary>
        /// Get which user added which products
        /// </summary>
        Task<List<UserProductCreationDto>> GetUserProductCreationsAsync(string clientCode);

        /// <summary>
        /// Get top selling products
        /// </summary>
        Task<List<TopSellingProductDto>> GetTopSellingProductsAsync(string clientCode, int topCount = 10);

        /// <summary>
        /// Get total weight of all products by category
        /// </summary>
        Task<List<CategoryWeightDto>> GetTotalWeightByCategoryAsync(string clientCode);

        /// <summary>
        /// Get customer summary information
        /// </summary>
        Task<CustomerSummaryDto> GetCustomerSummaryAsync(string clientCode);

        /// <summary>
        /// Export all dashboard data to Excel
        /// </summary>
        Task<byte[]> ExportDashboardDataToExcelAsync(string clientCode);

        /// <summary>
        /// Get comprehensive dashboard summary with all important metrics in one call
        /// </summary>
        Task<DashboardSummaryDto> GetDashboardSummaryAsync(string clientCode);
    }
}

