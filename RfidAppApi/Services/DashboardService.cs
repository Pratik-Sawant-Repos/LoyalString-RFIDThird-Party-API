using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using RfidAppApi.Data;
using RfidAppApi.DTOs;
using RfidAppApi.Models;

namespace RfidAppApi.Services
{
    /// <summary>
    /// Service for dashboard analytics and reporting
    /// </summary>
    public class DashboardService : IDashboardService
    {
        private readonly IClientService _clientService;
        private readonly AppDbContext _appContext;
        private readonly ILogger<DashboardService> _logger;

        public DashboardService(
            IClientService clientService,
            AppDbContext appContext,
            ILogger<DashboardService> logger)
        {
            _clientService = clientService;
            _appContext = appContext;
            _logger = logger;
            
            // Set EPPlus license context for non-commercial use
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        }

        public async Task<DashboardDto> GetDashboardDataAsync(string clientCode)
        {
            return new DashboardDto
            {
                TotalProductCount = await GetTotalProductCountAsync(clientCode),
                TotalSoldProducts = await GetTotalSoldProductsAsync(clientCode),
                RecentlyAddedProducts = await GetRecentlyAddedProductsAsync(clientCode, 10),
                SalesOverMonth = await GetSalesOverMonthAsync(clientCode, 12),
                SalesMonthGrowth = await GetSalesMonthGrowthAsync(clientCode),
                UserProductCreations = await GetUserProductCreationsAsync(clientCode),
                TopSellingProducts = await GetTopSellingProductsAsync(clientCode, 10),
                TotalWeightByCategory = await GetTotalWeightByCategoryAsync(clientCode),
                CustomerSummary = await GetCustomerSummaryAsync(clientCode)
            };
        }

        public async Task<int> GetTotalProductCountAsync(string clientCode)
        {
            using var context = await _clientService.GetClientDbContextAsync(clientCode);
            return await context.ProductDetails.CountAsync();
        }

        public async Task<int> GetTotalSoldProductsAsync(string clientCode)
        {
            using var context = await _clientService.GetClientDbContextAsync(clientCode);
            
            // Count products that have been sold (either through Invoice or StockMovement with Sale type)
            var soldFromInvoices = await context.Invoices
                .Where(i => i.ClientCode == clientCode && i.IsActive && i.InvoiceType == "Sale")
                .Select(i => i.ProductId)
                .Distinct()
                .CountAsync();

            var soldFromMovements = await context.StockMovements
                .Where(sm => sm.ClientCode == clientCode && sm.IsActive && sm.MovementType == "Sale")
                .Select(sm => sm.ProductId)
                .Distinct()
                .CountAsync();

            // Return the maximum count (some products might be in both, but we want unique count)
            var allSoldProductIds = await context.Invoices
                .Where(i => i.ClientCode == clientCode && i.IsActive && i.InvoiceType == "Sale")
                .Select(i => i.ProductId)
                .Union(context.StockMovements
                    .Where(sm => sm.ClientCode == clientCode && sm.IsActive && sm.MovementType == "Sale")
                    .Select(sm => sm.ProductId))
                .Distinct()
                .CountAsync();

            return allSoldProductIds;
        }

        public async Task<List<RecentlyAddedProductDto>> GetRecentlyAddedProductsAsync(string clientCode, int count = 10)
        {
            using var context = await _clientService.GetClientDbContextAsync(clientCode);
            
            return await context.ProductDetails
                .Include(p => p.Category)
                .Include(p => p.Branch)
                .Include(p => p.Counter)
                .Include(p => p.Product)
                .OrderByDescending(p => p.CreatedOn)
                .Take(count)
                .Select(p => new RecentlyAddedProductDto
                {
                    ProductId = p.Id,
                    ItemCode = p.ItemCode,
                    ProductName = p.Product.ProductName,
                    CategoryName = p.Category.CategoryName,
                    BranchName = p.Branch.BranchName,
                    CounterName = p.Counter.CounterName,
                    Mrp = p.Mrp,
                    GrossWeight = p.GrossWeight,
                    CreatedOn = p.CreatedOn
                })
                .ToListAsync();
        }

        public async Task<List<MonthlySalesDto>> GetSalesOverMonthAsync(string clientCode, int months = 12)
        {
            using var context = await _clientService.GetClientDbContextAsync(clientCode);
            
            var endDate = DateTime.UtcNow;
            var startDate = endDate.AddMonths(-months);

            // Get sales from invoices
            var invoiceSales = await context.Invoices
                .Where(i => i.ClientCode == clientCode && 
                           i.IsActive && 
                           i.InvoiceType == "Sale" &&
                           i.SoldOn >= startDate && 
                           i.SoldOn <= endDate)
                .GroupBy(i => new { Year = i.SoldOn.Year, Month = i.SoldOn.Month })
                .Select(g => new
                {
                    Year = g.Key.Year,
                    Month = g.Key.Month,
                    Count = g.Count(),
                    TotalAmount = g.Sum(i => i.FinalAmount)
                })
                .ToListAsync();

            // Get sales from stock movements
            var movementSales = await context.StockMovements
                .Where(sm => sm.ClientCode == clientCode && 
                           sm.IsActive && 
                           sm.MovementType == "Sale" &&
                           sm.MovementDate >= startDate && 
                           sm.MovementDate <= endDate)
                .GroupBy(sm => new { Year = sm.MovementDate.Year, Month = sm.MovementDate.Month })
                .Select(g => new
                {
                    Year = g.Key.Year,
                    Month = g.Key.Month,
                    Count = g.Count(),
                    TotalAmount = g.Sum(sm => sm.TotalAmount ?? 0)
                })
                .ToListAsync();

            // Combine and aggregate
            var combinedSales = invoiceSales
                .Concat(movementSales)
                .GroupBy(s => new { s.Year, s.Month })
                .Select(g => new MonthlySalesDto
                {
                    Year = g.Key.Year,
                    MonthNumber = g.Key.Month,
                    Month = new DateTime(g.Key.Year, g.Key.Month, 1).ToString("MMMM yyyy"),
                    TotalSalesCount = g.Sum(s => s.Count),
                    TotalSalesAmount = g.Sum(s => s.TotalAmount)
                })
                .OrderBy(s => s.Year)
                .ThenBy(s => s.MonthNumber)
                .ToList();

            return combinedSales;
        }

        public async Task<decimal> GetSalesMonthGrowthAsync(string clientCode)
        {
            using var context = await _clientService.GetClientDbContextAsync(clientCode);
            
            var now = DateTime.UtcNow;
            var currentMonthStart = new DateTime(now.Year, now.Month, 1);
            var lastMonthStart = currentMonthStart.AddMonths(-1);
            var lastMonthEnd = currentMonthStart.AddDays(-1);

            // Current month sales
            var currentMonthSales = await context.Invoices
                .Where(i => i.ClientCode == clientCode && 
                           i.IsActive && 
                           i.InvoiceType == "Sale" &&
                           i.SoldOn >= currentMonthStart)
                .SumAsync(i => (decimal?)i.FinalAmount) ?? 0;

            var currentMonthSalesFromMovements = await context.StockMovements
                .Where(sm => sm.ClientCode == clientCode && 
                           sm.IsActive && 
                           sm.MovementType == "Sale" &&
                           sm.MovementDate >= currentMonthStart)
                .SumAsync(sm => sm.TotalAmount ?? 0);

            currentMonthSales += currentMonthSalesFromMovements;

            // Last month sales
            var lastMonthSales = await context.Invoices
                .Where(i => i.ClientCode == clientCode && 
                           i.IsActive && 
                           i.InvoiceType == "Sale" &&
                           i.SoldOn >= lastMonthStart && 
                           i.SoldOn <= lastMonthEnd)
                .SumAsync(i => (decimal?)i.FinalAmount) ?? 0;

            var lastMonthSalesFromMovements = await context.StockMovements
                .Where(sm => sm.ClientCode == clientCode && 
                           sm.IsActive && 
                           sm.MovementType == "Sale" &&
                           sm.MovementDate >= lastMonthStart && 
                           sm.MovementDate <= lastMonthEnd)
                .SumAsync(sm => sm.TotalAmount ?? 0);

            lastMonthSales += lastMonthSalesFromMovements;

            // Calculate growth percentage
            if (lastMonthSales == 0)
                return currentMonthSales > 0 ? 100 : 0;

            return ((currentMonthSales - lastMonthSales) / lastMonthSales) * 100;
        }

        public async Task<List<UserProductCreationDto>> GetUserProductCreationsAsync(string clientCode)
        {
            // Get product creation activities from UserActivity
            var activities = await _appContext.UserActivities
                .Include(ua => ua.User)
                .Where(ua => ua.ClientCode == clientCode && 
                           ua.ActivityType == "Product" && 
                           ua.Action == "Create" &&
                           ua.RecordId.HasValue)
                .ToListAsync();

            // Group by user
            var productCreations = activities
                .GroupBy(ua => new { ua.UserId, ua.User.UserName, ua.User.Email })
                .Select(g => new
                {
                    UserId = g.Key.UserId,
                    UserName = g.Key.UserName,
                    UserEmail = g.Key.Email,
                    ProductIds = g.Select(x => x.RecordId!.Value).ToList()
                })
                .ToList();

            using var context = await _clientService.GetClientDbContextAsync(clientCode);

            var result = new List<UserProductCreationDto>();

            foreach (var creation in productCreations)
            {
                if (!creation.ProductIds.Any())
                    continue;

                var products = await context.ProductDetails
                    .Include(p => p.Product)
                    .Where(p => creation.ProductIds.Contains(p.Id))
                    .Select(p => new ProductCreationDetailDto
                    {
                        ProductId = p.Id,
                        ItemCode = p.ItemCode,
                        ProductName = p.Product.ProductName,
                        CreatedOn = p.CreatedOn
                    })
                    .OrderByDescending(p => p.CreatedOn)
                    .ToListAsync();

                result.Add(new UserProductCreationDto
                {
                    UserId = creation.UserId,
                    UserName = creation.UserName,
                    UserEmail = creation.UserEmail,
                    ProductsCreated = products.Count,
                    ProductDetails = products
                });
            }

            return result.OrderByDescending(r => r.ProductsCreated).ToList();
        }

        public async Task<List<TopSellingProductDto>> GetTopSellingProductsAsync(string clientCode, int topCount = 10)
        {
            using var context = await _clientService.GetClientDbContextAsync(clientCode);

            // Get sales from invoices
            var invoiceSales = await context.Invoices
                .Include(i => i.Product)
                    .ThenInclude(p => p.Product)
                .Include(i => i.Product)
                    .ThenInclude(p => p.Category)
                .Where(i => i.ClientCode == clientCode && 
                           i.IsActive && 
                           i.InvoiceType == "Sale")
                .GroupBy(i => new
                {
                    ProductId = i.ProductId,
                    ItemCode = i.Product.ItemCode,
                    ProductName = i.Product.Product.ProductName,
                    CategoryName = i.Product.Category.CategoryName
                })
                .Select(g => new
                {
                    g.Key.ProductId,
                    g.Key.ItemCode,
                    g.Key.ProductName,
                    g.Key.CategoryName,
                    TotalSold = g.Count(),
                    TotalSalesAmount = g.Sum(i => i.FinalAmount),
                    AverageSellingPrice = g.Average(i => i.FinalAmount)
                })
                .ToListAsync();

            // Get sales from stock movements
            var movementSales = await context.StockMovements
                .Include(sm => sm.Product)
                    .ThenInclude(p => p.Product)
                .Include(sm => sm.Product)
                    .ThenInclude(p => p.Category)
                .Where(sm => sm.ClientCode == clientCode && 
                           sm.IsActive && 
                           sm.MovementType == "Sale")
                .GroupBy(sm => new
                {
                    ProductId = sm.ProductId,
                    ItemCode = sm.Product!.ItemCode,
                    ProductName = sm.Product.Product.ProductName,
                    CategoryName = sm.Product.Category.CategoryName
                })
                .Select(g => new
                {
                    g.Key.ProductId,
                    g.Key.ItemCode,
                    g.Key.ProductName,
                    g.Key.CategoryName,
                    TotalSold = g.Count(),
                    TotalSalesAmount = g.Sum(sm => sm.TotalAmount ?? 0),
                    AverageSellingPrice = g.Average(sm => sm.TotalAmount ?? 0)
                })
                .ToListAsync();

            // Combine and aggregate
            var combinedSales = invoiceSales
                .Concat(movementSales)
                .GroupBy(s => s.ProductId)
                .Select(g => new TopSellingProductDto
                {
                    ProductId = g.Key,
                    ItemCode = g.First().ItemCode,
                    ProductName = g.First().ProductName,
                    CategoryName = g.First().CategoryName,
                    TotalSold = g.Sum(s => s.TotalSold),
                    TotalSalesAmount = g.Sum(s => s.TotalSalesAmount),
                    AverageSellingPrice = g.Average(s => s.AverageSellingPrice)
                })
                .OrderByDescending(s => s.TotalSold)
                .Take(topCount)
                .ToList();

            return combinedSales;
        }

        public async Task<List<CategoryWeightDto>> GetTotalWeightByCategoryAsync(string clientCode)
        {
            using var context = await _clientService.GetClientDbContextAsync(clientCode);

            return await context.ProductDetails
                .Include(p => p.Category)
                .GroupBy(p => new { p.CategoryId, p.Category.CategoryName })
                .Select(g => new CategoryWeightDto
                {
                    CategoryId = g.Key.CategoryId,
                    CategoryName = g.Key.CategoryName,
                    ProductCount = g.Count(),
                    TotalGrossWeight = g.Sum(p => p.GrossWeight ?? 0),
                    TotalNetWeight = g.Sum(p => p.NetWeight ?? 0),
                    TotalStoneWeight = g.Sum(p => p.StoneWeight ?? 0)
                })
                .OrderByDescending(c => c.TotalGrossWeight)
                .ToListAsync();
        }

        public async Task<CustomerSummaryDto> GetCustomerSummaryAsync(string clientCode)
        {
            using var context = await _clientService.GetClientDbContextAsync(clientCode);

            var invoices = await context.Invoices
                .Where(i => i.ClientCode == clientCode && i.IsActive && i.InvoiceType == "Sale")
                .ToListAsync();

            var totalInvoices = invoices.Count;
            var totalRevenue = invoices.Sum(i => i.FinalAmount);
            var averageOrderValue = totalInvoices > 0 ? totalRevenue / totalInvoices : 0;

            // Get unique customers (by phone or name)
            var uniqueCustomers = invoices
                .Where(i => !string.IsNullOrWhiteSpace(i.CustomerName) || !string.IsNullOrWhiteSpace(i.CustomerPhone))
                .GroupBy(i => i.CustomerPhone ?? i.CustomerName ?? "Unknown")
                .Count();

            // Get top customers
            var topCustomers = invoices
                .Where(i => !string.IsNullOrWhiteSpace(i.CustomerName) || !string.IsNullOrWhiteSpace(i.CustomerPhone))
                .GroupBy(i => new
                {
                    Name = i.CustomerName ?? "Unknown",
                    Phone = i.CustomerPhone ?? ""
                })
                .Select(g => new TopCustomerDto
                {
                    CustomerName = g.Key.Name,
                    CustomerPhone = g.Key.Phone,
                    TotalPurchases = g.Count(),
                    TotalSpent = g.Sum(i => i.FinalAmount),
                    LastPurchaseDate = g.Max(i => i.SoldOn)
                })
                .OrderByDescending(c => c.TotalSpent)
                .Take(10)
                .ToList();

            return new CustomerSummaryDto
            {
                TotalCustomers = uniqueCustomers,
                TotalInvoices = totalInvoices,
                TotalRevenue = totalRevenue,
                AverageOrderValue = averageOrderValue,
                TopCustomers = topCustomers
            };
        }

        public async Task<byte[]> ExportDashboardDataToExcelAsync(string clientCode)
        {
            var dashboardData = await GetDashboardDataAsync(clientCode);

            using var package = new ExcelPackage();
            
            // Summary Sheet
            var summarySheet = package.Workbook.Worksheets.Add("Dashboard Summary");
            summarySheet.Cells[1, 1].Value = "Metric";
            summarySheet.Cells[1, 2].Value = "Value";
            summarySheet.Cells[2, 1].Value = "Total Products";
            summarySheet.Cells[2, 2].Value = dashboardData.TotalProductCount;
            summarySheet.Cells[3, 1].Value = "Total Sold Products";
            summarySheet.Cells[3, 2].Value = dashboardData.TotalSoldProducts;
            summarySheet.Cells[4, 1].Value = "Sales Month Growth (%)";
            summarySheet.Cells[4, 2].Value = dashboardData.SalesMonthGrowth;
            summarySheet.Cells[5, 1].Value = "Total Customers";
            summarySheet.Cells[5, 2].Value = dashboardData.CustomerSummary.TotalCustomers;
            summarySheet.Cells[6, 1].Value = "Total Revenue";
            summarySheet.Cells[6, 2].Value = dashboardData.CustomerSummary.TotalRevenue;
            summarySheet.Cells[1, 1, 1, 2].Style.Font.Bold = true;
            summarySheet.Columns.AutoFit();

            // Recently Added Products
            var recentSheet = package.Workbook.Worksheets.Add("Recently Added Products");
            recentSheet.Cells[1, 1].Value = "Item Code";
            recentSheet.Cells[1, 2].Value = "Product Name";
            recentSheet.Cells[1, 3].Value = "Category";
            recentSheet.Cells[1, 4].Value = "Branch";
            recentSheet.Cells[1, 5].Value = "Counter";
            recentSheet.Cells[1, 6].Value = "MRP";
            recentSheet.Cells[1, 7].Value = "Gross Weight";
            recentSheet.Cells[1, 8].Value = "Created On";
            for (int i = 0; i < dashboardData.RecentlyAddedProducts.Count; i++)
            {
                var product = dashboardData.RecentlyAddedProducts[i];
                recentSheet.Cells[i + 2, 1].Value = product.ItemCode;
                recentSheet.Cells[i + 2, 2].Value = product.ProductName;
                recentSheet.Cells[i + 2, 3].Value = product.CategoryName;
                recentSheet.Cells[i + 2, 4].Value = product.BranchName;
                recentSheet.Cells[i + 2, 5].Value = product.CounterName;
                recentSheet.Cells[i + 2, 6].Value = product.Mrp;
                recentSheet.Cells[i + 2, 7].Value = product.GrossWeight;
                recentSheet.Cells[i + 2, 8].Value = product.CreatedOn;
            }
            recentSheet.Cells[1, 1, 1, 8].Style.Font.Bold = true;
            recentSheet.Columns.AutoFit();

            // Monthly Sales
            var monthlySheet = package.Workbook.Worksheets.Add("Monthly Sales");
            monthlySheet.Cells[1, 1].Value = "Month";
            monthlySheet.Cells[1, 2].Value = "Year";
            monthlySheet.Cells[1, 3].Value = "Sales Count";
            monthlySheet.Cells[1, 4].Value = "Total Amount";
            for (int i = 0; i < dashboardData.SalesOverMonth.Count; i++)
            {
                var sales = dashboardData.SalesOverMonth[i];
                monthlySheet.Cells[i + 2, 1].Value = sales.Month;
                monthlySheet.Cells[i + 2, 2].Value = sales.Year;
                monthlySheet.Cells[i + 2, 3].Value = sales.TotalSalesCount;
                monthlySheet.Cells[i + 2, 4].Value = sales.TotalSalesAmount;
            }
            monthlySheet.Cells[1, 1, 1, 4].Style.Font.Bold = true;
            monthlySheet.Columns.AutoFit();

            // User Product Creations
            var userSheet = package.Workbook.Worksheets.Add("User Product Creations");
            userSheet.Cells[1, 1].Value = "User Name";
            userSheet.Cells[1, 2].Value = "User Email";
            userSheet.Cells[1, 3].Value = "Products Created";
            int row = 2;
            foreach (var user in dashboardData.UserProductCreations)
            {
                userSheet.Cells[row, 1].Value = user.UserName;
                userSheet.Cells[row, 2].Value = user.UserEmail;
                userSheet.Cells[row, 3].Value = user.ProductsCreated;
                row++;
            }
            userSheet.Cells[1, 1, 1, 3].Style.Font.Bold = true;
            userSheet.Columns.AutoFit();

            // Top Selling Products
            var topSellingSheet = package.Workbook.Worksheets.Add("Top Selling Products");
            topSellingSheet.Cells[1, 1].Value = "Item Code";
            topSellingSheet.Cells[1, 2].Value = "Product Name";
            topSellingSheet.Cells[1, 3].Value = "Category";
            topSellingSheet.Cells[1, 4].Value = "Total Sold";
            topSellingSheet.Cells[1, 5].Value = "Total Sales Amount";
            topSellingSheet.Cells[1, 6].Value = "Average Price";
            for (int i = 0; i < dashboardData.TopSellingProducts.Count; i++)
            {
                var product = dashboardData.TopSellingProducts[i];
                topSellingSheet.Cells[i + 2, 1].Value = product.ItemCode;
                topSellingSheet.Cells[i + 2, 2].Value = product.ProductName;
                topSellingSheet.Cells[i + 2, 3].Value = product.CategoryName;
                topSellingSheet.Cells[i + 2, 4].Value = product.TotalSold;
                topSellingSheet.Cells[i + 2, 5].Value = product.TotalSalesAmount;
                topSellingSheet.Cells[i + 2, 6].Value = product.AverageSellingPrice;
            }
            topSellingSheet.Cells[1, 1, 1, 6].Style.Font.Bold = true;
            topSellingSheet.Columns.AutoFit();

            // Category Weight
            var weightSheet = package.Workbook.Worksheets.Add("Weight by Category");
            weightSheet.Cells[1, 1].Value = "Category";
            weightSheet.Cells[1, 2].Value = "Product Count";
            weightSheet.Cells[1, 3].Value = "Total Gross Weight";
            weightSheet.Cells[1, 4].Value = "Total Net Weight";
            weightSheet.Cells[1, 5].Value = "Total Stone Weight";
            for (int i = 0; i < dashboardData.TotalWeightByCategory.Count; i++)
            {
                var category = dashboardData.TotalWeightByCategory[i];
                weightSheet.Cells[i + 2, 1].Value = category.CategoryName;
                weightSheet.Cells[i + 2, 2].Value = category.ProductCount;
                weightSheet.Cells[i + 2, 3].Value = category.TotalGrossWeight;
                weightSheet.Cells[i + 2, 4].Value = category.TotalNetWeight;
                weightSheet.Cells[i + 2, 5].Value = category.TotalStoneWeight;
            }
            weightSheet.Cells[1, 1, 1, 5].Style.Font.Bold = true;
            weightSheet.Columns.AutoFit();

            // Customer Summary
            var customerSheet = package.Workbook.Worksheets.Add("Customer Summary");
            customerSheet.Cells[1, 1].Value = "Customer Name";
            customerSheet.Cells[1, 2].Value = "Phone";
            customerSheet.Cells[1, 3].Value = "Total Purchases";
            customerSheet.Cells[1, 4].Value = "Total Spent";
            customerSheet.Cells[1, 5].Value = "Last Purchase";
            for (int i = 0; i < dashboardData.CustomerSummary.TopCustomers.Count; i++)
            {
                var customer = dashboardData.CustomerSummary.TopCustomers[i];
                customerSheet.Cells[i + 2, 1].Value = customer.CustomerName;
                customerSheet.Cells[i + 2, 2].Value = customer.CustomerPhone;
                customerSheet.Cells[i + 2, 3].Value = customer.TotalPurchases;
                customerSheet.Cells[i + 2, 4].Value = customer.TotalSpent;
                customerSheet.Cells[i + 2, 5].Value = customer.LastPurchaseDate;
            }
            customerSheet.Cells[1, 1, 1, 5].Style.Font.Bold = true;
            customerSheet.Columns.AutoFit();

            return package.GetAsByteArray();
        }

        public async Task<DashboardSummaryDto> GetDashboardSummaryAsync(string clientCode)
        {
            using var context = await _clientService.GetClientDbContextAsync(clientCode);

            var now = DateTime.UtcNow;
            var currentMonthStart = new DateTime(now.Year, now.Month, 1);
            var lastMonthStart = currentMonthStart.AddMonths(-1);
            var lastMonthEnd = currentMonthStart.AddDays(-1);
            var thirtyDaysAgo = now.AddDays(-30);

            // Product Metrics
            var totalProducts = await context.ProductDetails.CountAsync();
            var activeProducts = await context.ProductDetails
                .CountAsync(p => p.Status == null || p.Status.ToLower() == "active");
            
            var soldProductIds = await context.Invoices
                .Where(i => i.ClientCode == clientCode && i.IsActive && i.InvoiceType == "Sale")
                .Select(i => i.ProductId)
                .Union(context.StockMovements
                    .Where(sm => sm.ClientCode == clientCode && sm.IsActive && sm.MovementType == "Sale")
                    .Select(sm => sm.ProductId))
                .Distinct()
                .CountAsync();

            var recentlyAddedCount = await context.ProductDetails
                .CountAsync(p => p.CreatedOn >= thirtyDaysAgo);

            // Weight Metrics
            var weightStats = await context.ProductDetails
                .Select(p => new
                {
                    GrossWeight = p.GrossWeight ?? 0,
                    NetWeight = p.NetWeight ?? 0,
                    StoneWeight = p.StoneWeight ?? 0
                })
                .ToListAsync();

            var totalGrossWeight = weightStats.Sum(w => w.GrossWeight);
            var totalNetWeight = weightStats.Sum(w => w.NetWeight);
            var totalStoneWeight = weightStats.Sum(w => w.StoneWeight);
            var productCountForAvg = weightStats.Count(w => w.GrossWeight > 0);
            var averageGrossWeight = productCountForAvg > 0 ? totalGrossWeight / productCountForAvg : 0;
            var netWeightCount = weightStats.Count(w => w.NetWeight > 0);
            var averageNetWeight = netWeightCount > 0 ? totalNetWeight / netWeightCount : 0;

            // Sales Metrics
            var invoices = await context.Invoices
                .Where(i => i.ClientCode == clientCode && i.IsActive && i.InvoiceType == "Sale")
                .ToListAsync();

            var totalRevenue = invoices.Sum(i => i.FinalAmount);
            var totalInvoices = invoices.Count;
            var averageOrderValue = totalInvoices > 0 ? totalRevenue / totalInvoices : 0;

            // Current month sales
            var currentMonthSales = invoices
                .Where(i => i.SoldOn >= currentMonthStart)
                .Sum(i => i.FinalAmount);

            var currentMonthSalesFromMovements = await context.StockMovements
                .Where(sm => sm.ClientCode == clientCode && 
                           sm.IsActive && 
                           sm.MovementType == "Sale" &&
                           sm.MovementDate >= currentMonthStart)
                .SumAsync(sm => sm.TotalAmount ?? 0);

            currentMonthSales += currentMonthSalesFromMovements;

            // Previous month sales
            var previousMonthSales = invoices
                .Where(i => i.SoldOn >= lastMonthStart && i.SoldOn <= lastMonthEnd)
                .Sum(i => i.FinalAmount);

            var previousMonthSalesFromMovements = await context.StockMovements
                .Where(sm => sm.ClientCode == clientCode && 
                           sm.IsActive && 
                           sm.MovementType == "Sale" &&
                           sm.MovementDate >= lastMonthStart && 
                           sm.MovementDate <= lastMonthEnd)
                .SumAsync(sm => sm.TotalAmount ?? 0);

            previousMonthSales += previousMonthSalesFromMovements;

            // Sales growth
            var salesGrowth = previousMonthSales == 0 
                ? (currentMonthSales > 0 ? 100 : 0)
                : ((currentMonthSales - previousMonthSales) / previousMonthSales) * 100;

            // Total sales count
            var totalSalesCount = invoices.Count;
            var totalSalesCountFromMovements = await context.StockMovements
                .Where(sm => sm.ClientCode == clientCode && sm.IsActive && sm.MovementType == "Sale")
                .CountAsync();
            totalSalesCount += totalSalesCountFromMovements;

            // Customer Metrics
            var uniqueCustomers = invoices
                .Where(i => !string.IsNullOrWhiteSpace(i.CustomerName) || !string.IsNullOrWhiteSpace(i.CustomerPhone))
                .GroupBy(i => i.CustomerPhone ?? i.CustomerName ?? "Unknown")
                .Count();

            var activeCustomers = invoices
                .Where(i => i.SoldOn >= thirtyDaysAgo && 
                          (!string.IsNullOrWhiteSpace(i.CustomerName) || !string.IsNullOrWhiteSpace(i.CustomerPhone)))
                .GroupBy(i => i.CustomerPhone ?? i.CustomerName ?? "Unknown")
                .Count();

            // Category & Branch Metrics
            var totalCategories = await context.CategoryMasters.CountAsync();
            var totalBranches = await context.BranchMasters.CountAsync();
            var totalCounters = await context.CounterMasters.CountAsync();

            // Top category
            var topCategory = await context.ProductDetails
                .Include(p => p.Category)
                .GroupBy(p => new { p.CategoryId, p.Category.CategoryName })
                .Select(g => new
                {
                    CategoryName = g.Key.CategoryName,
                    ProductCount = g.Count()
                })
                .OrderByDescending(g => g.ProductCount)
                .FirstOrDefaultAsync();

            var topCategoryName = topCategory?.CategoryName;
            var topCategoryProductCount = topCategory?.ProductCount ?? 0;

            // Inventory Value
            var inventoryValue = await context.ProductDetails
                .SumAsync(p => p.Mrp ?? 0);
            
            var averageProductPrice = totalProducts > 0 ? inventoryValue / totalProducts : 0;

            // RFID Metrics
            var totalRfidTags = await context.Rfids.CountAsync();
            var usedRfidTags = await context.ProductRfidAssignments
                .Where(pa => pa.IsActive)
                .Select(pa => pa.RFIDCode)
                .Distinct()
                .CountAsync();
            var unusedRfidTags = totalRfidTags - usedRfidTags;

            return new DashboardSummaryDto
            {
                // Product Metrics
                TotalProductCount = totalProducts,
                ActiveProductCount = activeProducts,
                TotalSoldProducts = soldProductIds,
                RecentlyAddedProductsCount = recentlyAddedCount,

                // Weight Metrics
                TotalGrossWeight = totalGrossWeight,
                TotalNetWeight = totalNetWeight,
                TotalStoneWeight = totalStoneWeight,
                AverageGrossWeight = averageGrossWeight,
                AverageNetWeight = averageNetWeight,

                // Sales Metrics
                TotalRevenue = totalRevenue,
                TotalSalesAmount = totalRevenue,
                TotalSalesCount = totalSalesCount,
                AverageOrderValue = averageOrderValue,
                SalesMonthGrowth = salesGrowth,
                CurrentMonthSales = currentMonthSales,
                PreviousMonthSales = previousMonthSales,

                // Customer Metrics
                TotalCustomers = uniqueCustomers,
                TotalInvoices = totalInvoices,
                ActiveCustomers = activeCustomers,

                // Category & Branch Metrics
                TotalCategories = totalCategories,
                TotalBranches = totalBranches,
                TotalCounters = totalCounters,
                TopCategoryName = topCategoryName,
                TopCategoryProductCount = topCategoryProductCount,

                // Inventory Value
                TotalInventoryValue = inventoryValue,
                AverageProductPrice = averageProductPrice,

                // Additional Metrics
                TotalRfidTags = totalRfidTags,
                UsedRfidTags = usedRfidTags,
                UnusedRfidTags = unusedRfidTags
            };
        }
    }
}

