namespace RfidAppApi.DTOs
{
    /// <summary>
    /// DTO for comprehensive dashboard data
    /// </summary>
    public class DashboardDto
    {
        public int TotalProductCount { get; set; }
        public int TotalSoldProducts { get; set; }
        public List<RecentlyAddedProductDto> RecentlyAddedProducts { get; set; } = new();
        public List<MonthlySalesDto> SalesOverMonth { get; set; } = new();
        public decimal SalesMonthGrowth { get; set; }
        public List<UserProductCreationDto> UserProductCreations { get; set; } = new();
        public List<TopSellingProductDto> TopSellingProducts { get; set; } = new();
        public List<CategoryWeightDto> TotalWeightByCategory { get; set; } = new();
        public CustomerSummaryDto CustomerSummary { get; set; } = new();
    }

    /// <summary>
    /// DTO for recently added products
    /// </summary>
    public class RecentlyAddedProductDto
    {
        public int ProductId { get; set; }
        public string ItemCode { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public string BranchName { get; set; } = string.Empty;
        public string CounterName { get; set; } = string.Empty;
        public decimal? Mrp { get; set; }
        public float? GrossWeight { get; set; }
        public DateTime CreatedOn { get; set; }
    }

    /// <summary>
    /// DTO for monthly sales data
    /// </summary>
    public class MonthlySalesDto
    {
        public string Month { get; set; } = string.Empty; // Format: "YYYY-MM" or "January 2024"
        public int Year { get; set; }
        public int MonthNumber { get; set; }
        public int TotalSalesCount { get; set; }
        public decimal TotalSalesAmount { get; set; }
    }

    /// <summary>
    /// DTO for user product creation tracking
    /// </summary>
    public class UserProductCreationDto
    {
        public int UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string UserEmail { get; set; } = string.Empty;
        public int ProductsCreated { get; set; }
        public List<ProductCreationDetailDto> ProductDetails { get; set; } = new();
    }

    /// <summary>
    /// DTO for product creation detail
    /// </summary>
    public class ProductCreationDetailDto
    {
        public int ProductId { get; set; }
        public string ItemCode { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public DateTime CreatedOn { get; set; }
    }

    /// <summary>
    /// DTO for top selling products
    /// </summary>
    public class TopSellingProductDto
    {
        public int ProductId { get; set; }
        public string ItemCode { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public int TotalSold { get; set; }
        public decimal TotalSalesAmount { get; set; }
        public decimal AverageSellingPrice { get; set; }
    }

    /// <summary>
    /// DTO for category weight summary
    /// </summary>
    public class CategoryWeightDto
    {
        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public int ProductCount { get; set; }
        public float TotalGrossWeight { get; set; }
        public float TotalNetWeight { get; set; }
        public float TotalStoneWeight { get; set; }
    }

    /// <summary>
    /// DTO for customer summary
    /// </summary>
    public class CustomerSummaryDto
    {
        public int TotalCustomers { get; set; }
        public int TotalInvoices { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal AverageOrderValue { get; set; }
        public List<TopCustomerDto> TopCustomers { get; set; } = new();
    }

    /// <summary>
    /// DTO for top customers
    /// </summary>
    public class TopCustomerDto
    {
        public string CustomerName { get; set; } = string.Empty;
        public string? CustomerPhone { get; set; }
        public int TotalPurchases { get; set; }
        public decimal TotalSpent { get; set; }
        public DateTime LastPurchaseDate { get; set; }
    }

    /// <summary>
    /// Comprehensive dashboard summary with all important metrics in one response
    /// </summary>
    public class DashboardSummaryDto
    {
        // Product Metrics
        public int TotalProductCount { get; set; }
        public int ActiveProductCount { get; set; }
        public int TotalSoldProducts { get; set; }
        public int RecentlyAddedProductsCount { get; set; } // Last 30 days

        // Weight Metrics
        public float TotalGrossWeight { get; set; }
        public float TotalNetWeight { get; set; }
        public float TotalStoneWeight { get; set; }
        public float AverageGrossWeight { get; set; }
        public float AverageNetWeight { get; set; }

        // Sales Metrics
        public decimal TotalRevenue { get; set; }
        public decimal TotalSalesAmount { get; set; }
        public int TotalSalesCount { get; set; }
        public decimal AverageOrderValue { get; set; }
        public decimal SalesMonthGrowth { get; set; }
        public decimal CurrentMonthSales { get; set; }
        public decimal PreviousMonthSales { get; set; }

        // Customer Metrics
        public int TotalCustomers { get; set; }
        public int TotalInvoices { get; set; }
        public int ActiveCustomers { get; set; } // Customers with purchases in last 30 days

        // Category & Branch Metrics
        public int TotalCategories { get; set; }
        public int TotalBranches { get; set; }
        public int TotalCounters { get; set; }
        public string? TopCategoryName { get; set; }
        public int TopCategoryProductCount { get; set; }

        // Inventory Value
        public decimal TotalInventoryValue { get; set; } // Sum of all product MRP
        public decimal AverageProductPrice { get; set; }

        // Additional Metrics
        public int TotalRfidTags { get; set; }
        public int UsedRfidTags { get; set; }
        public int UnusedRfidTags { get; set; }
    }
}

