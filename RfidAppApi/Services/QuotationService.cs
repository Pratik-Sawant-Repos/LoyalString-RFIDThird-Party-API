using Microsoft.EntityFrameworkCore;
using RfidAppApi.Data;
using RfidAppApi.DTOs;
using RfidAppApi.Models;

namespace RfidAppApi.Services
{
    /// <summary>
    /// Service implementation for quotation management
    /// </summary>
    public class QuotationService : IQuotationService
    {
        private readonly ClientDbContextFactory _dbContextFactory;
        private readonly IEmailService _emailService;
        private readonly IWebhookService _webhookService;
        private readonly ILogger<QuotationService> _logger;

        public QuotationService(
            ClientDbContextFactory dbContextFactory,
            IEmailService emailService,
            IWebhookService webhookService,
            ILogger<QuotationService> logger)
        {
            _dbContextFactory = dbContextFactory;
            _emailService = emailService;
            _webhookService = webhookService;
            _logger = logger;
        }

        public async Task<IEnumerable<QuotationDto>> GetAllQuotationsAsync(string clientCode)
        {
            using var context = await _dbContextFactory.CreateAsync(clientCode);

            var quotations = await context.Quotations
                .Include(q => q.Customer)
                .Include(q => q.QuotationItems)
                .Where(q => q.IsActive)
                .OrderByDescending(q => q.QuotationDate)
                .ToListAsync();

            return quotations.Select(q => MapToDto(q));
        }

        public async Task<QuotationDto?> GetQuotationByIdAsync(int id, string clientCode)
        {
            using var context = await _dbContextFactory.CreateAsync(clientCode);

            var quotation = await context.Quotations
                .Include(q => q.Customer)
                .Include(q => q.QuotationItems)
                .ThenInclude(qi => qi.Product)
                .FirstOrDefaultAsync(q => q.Id == id && q.IsActive);

            return quotation != null ? MapToDto(quotation) : null;
        }

        public async Task<IEnumerable<QuotationDto>> GetQuotationsByCustomerAsync(int customerId, string clientCode)
        {
            using var context = await _dbContextFactory.CreateAsync(clientCode);

            var quotations = await context.Quotations
                .Include(q => q.Customer)
                .Include(q => q.QuotationItems)
                .Where(q => q.CustomerId == customerId && q.IsActive)
                .OrderByDescending(q => q.QuotationDate)
                .ToListAsync();

            return quotations.Select(q => MapToDto(q));
        }

        public async Task<QuotationDto> CreateQuotationAsync(CreateQuotationDto createDto, string clientCode)
        {
            using var context = await _dbContextFactory.CreateAsync(clientCode);

            // Validate customer exists
            var customer = await context.Customers
                .FirstOrDefaultAsync(c => c.Id == createDto.CustomerId && c.IsActive);

            if (customer == null)
            {
                throw new KeyNotFoundException($"Customer with ID {createDto.CustomerId} not found");
            }

            // Validate items
            if (createDto.Items == null || !createDto.Items.Any())
            {
                throw new ArgumentException("Quotation must have at least one item");
            }

            // Generate quotation number
            var quotationNumber = await GenerateQuotationNumberAsync(context, clientCode);

            // Create quotation
            var quotation = new Quotation
            {
                ClientCode = clientCode,
                QuotationNumber = quotationNumber,
                CustomerId = createDto.CustomerId,
                OldMetalWeight = createDto.OldMetalWeight,
                OldMetalRate = createDto.OldMetalRate,
                OldMetalAmount = createDto.OldMetalAmount,
                PaymentMode = createDto.PaymentMode,
                IsGstApplied = createDto.IsGstApplied,
                GstPercentage = createDto.GstPercentage,
                ValidUntil = createDto.ValidUntil,
                Remarks = createDto.Remarks,
                Status = "Draft",
                QuotationDate = DateTime.UtcNow,
                CreatedOn = DateTime.UtcNow,
                IsActive = true
            };

            // Process items and calculate amounts
            decimal totalGrossWeight = 0;
            decimal totalItemAmount = 0;
            int totalQuantity = 0;

            foreach (var itemDto in createDto.Items)
            {
                // Validate product exists
                var product = await context.ProductDetails
                    .Include(p => p.Design)
                    .Include(p => p.Purity)
                    .FirstOrDefaultAsync(p => p.Id == itemDto.ProductId);

                if (product == null)
                {
                    throw new KeyNotFoundException($"Product with ID {itemDto.ProductId} not found");
                }

                // Get RFID code if not provided
                string? rfidCode = itemDto.RfidCode;
                if (string.IsNullOrEmpty(rfidCode))
                {
                    var assignment = await context.ProductRfidAssignments
                        .FirstOrDefaultAsync(a => a.ProductId == product.Id && a.IsActive);
                    rfidCode = assignment?.RFIDCode;
                }

                // Calculate item amounts
                var (makingAmount, metalAmount, itemAmount) = CalculateItemAmounts(
                    itemDto.NetWeight,
                    itemDto.GoldRate,
                    itemDto.Making,
                    itemDto.MakingType,
                    itemDto.StoneAmount);

                var quotationItem = new QuotationItem
                {
                    ClientCode = clientCode,
                    ProductId = itemDto.ProductId,
                    ItemCode = itemDto.ItemCode,
                    RfidCode = rfidCode,
                    DesignName = product.Design?.DesignName ?? itemDto.ItemCode,
                    Purity = product.Purity?.PurityName ?? "",
                    GrossWeight = itemDto.GrossWeight,
                    StoneWeight = itemDto.StoneWeight,
                    NetWeight = itemDto.NetWeight,
                    GoldRate = itemDto.GoldRate,
                    Making = itemDto.Making,
                    MakingType = itemDto.MakingType,
                    StoneAmount = itemDto.StoneAmount,
                    MakingAmount = makingAmount,
                    MetalAmount = metalAmount,
                    ItemAmount = itemAmount,
                    Quantity = itemDto.Quantity,
                    Remarks = itemDto.Remarks,
                    CreatedOn = DateTime.UtcNow
                };

                quotation.QuotationItems.Add(quotationItem);

                totalGrossWeight += itemDto.GrossWeight * itemDto.Quantity;
                totalItemAmount += itemAmount * itemDto.Quantity;
                totalQuantity += itemDto.Quantity;
            }

            // Set summary values
            quotation.Quantity = totalQuantity;
            quotation.TotalGrossWeight = totalGrossWeight;
            quotation.SubTotalAmount = totalItemAmount - (createDto.OldMetalAmount ?? 0);

            // Calculate GST
            if (createDto.IsGstApplied)
            {
                quotation.GstAmount = quotation.SubTotalAmount * (createDto.GstPercentage / 100);
                quotation.TotalAmount = quotation.SubTotalAmount + quotation.GstAmount;
            }
            else
            {
                quotation.GstAmount = 0;
                quotation.TotalAmount = quotation.SubTotalAmount;
            }

            context.Quotations.Add(quotation);
            await context.SaveChangesAsync();

            // Set QuotationId for all items after quotation is saved
            foreach (var item in quotation.QuotationItems)
            {
                item.QuotationId = quotation.Id;
            }
            await context.SaveChangesAsync();

            _logger.LogInformation("Quotation created: {QuotationNumber} for customer: {CustomerId}", 
                quotation.QuotationNumber, customer.Id);

            // Trigger webhook for quotation created (async, non-blocking)
            _ = Task.Run(async () =>
            {
                try
                {
                    await _webhookService.TriggerWebhookAsync("quotation.created", new
                    {
                        quotationId = quotation.Id,
                        quotationNumber = quotation.QuotationNumber,
                        customerId = customer.Id,
                        customerName = customer.CustomerName,
                        totalAmount = quotation.TotalAmount,
                        quantity = quotation.Quantity,
                        status = quotation.Status,
                        createdAt = quotation.CreatedOn
                    }, clientCode);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to trigger webhook for quotation created: {QuotationId}", quotation.Id);
                }
            });

            // Send email if requested
            if (createDto.SendEmail)
            {
                try
                {
                    await SendQuotationEmailAsync(quotation.Id, null, null, clientCode);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to send quotation email for quotation: {QuotationId}", quotation.Id);
                }
            }

            return await GetQuotationByIdAsync(quotation.Id, clientCode) ?? throw new Exception("Failed to retrieve created quotation");
        }

        public async Task<QuotationDto> UpdateQuotationAsync(int id, UpdateQuotationDto updateDto, string clientCode)
        {
            using var context = await _dbContextFactory.CreateAsync(clientCode);

            var quotation = await context.Quotations
                .Include(q => q.QuotationItems)
                .FirstOrDefaultAsync(q => q.Id == id && q.ClientCode == clientCode);

            if (quotation == null)
            {
                throw new KeyNotFoundException($"Quotation with ID {id} not found");
            }

            // Update customer if provided
            if (updateDto.CustomerId.HasValue)
            {
                var customer = await context.Customers
                    .FirstOrDefaultAsync(c => c.Id == updateDto.CustomerId.Value && c.IsActive);
                
                if (customer == null)
                {
                    throw new KeyNotFoundException($"Customer with ID {updateDto.CustomerId.Value} not found");
                }
                
                quotation.CustomerId = updateDto.CustomerId.Value;
            }

            // Update items if provided
            if (updateDto.Items != null && updateDto.Items.Any())
            {
                // Remove existing items
                context.QuotationItems.RemoveRange(quotation.QuotationItems);

                // Add new items
                decimal totalGrossWeight = 0;
                decimal totalItemAmount = 0;
                int totalQuantity = 0;

                foreach (var itemDto in updateDto.Items)
                {
                    var product = await context.ProductDetails
                        .Include(p => p.Design)
                        .Include(p => p.Purity)
                        .FirstOrDefaultAsync(p => p.Id == itemDto.ProductId);

                    if (product == null)
                    {
                        throw new KeyNotFoundException($"Product with ID {itemDto.ProductId} not found");
                    }

                    string? rfidCode = itemDto.RfidCode;
                    if (string.IsNullOrEmpty(rfidCode))
                    {
                        var assignment = await context.ProductRfidAssignments
                            .FirstOrDefaultAsync(a => a.ProductId == product.Id && a.IsActive);
                        rfidCode = assignment?.RFIDCode;
                    }

                    var (makingAmount, metalAmount, itemAmount) = CalculateItemAmounts(
                        itemDto.NetWeight,
                        itemDto.GoldRate,
                        itemDto.Making,
                        itemDto.MakingType,
                        itemDto.StoneAmount);

                    var quotationItem = new QuotationItem
                    {
                        QuotationId = quotation.Id,
                        ClientCode = clientCode,
                        ProductId = itemDto.ProductId,
                        ItemCode = itemDto.ItemCode,
                        RfidCode = rfidCode,
                        DesignName = product.Design?.DesignName ?? itemDto.ItemCode,
                        Purity = product.Purity?.PurityName ?? "",
                        GrossWeight = itemDto.GrossWeight,
                        StoneWeight = itemDto.StoneWeight,
                        NetWeight = itemDto.NetWeight,
                        GoldRate = itemDto.GoldRate,
                        Making = itemDto.Making,
                        MakingType = itemDto.MakingType,
                        StoneAmount = itemDto.StoneAmount,
                        MakingAmount = makingAmount,
                        MetalAmount = metalAmount,
                        ItemAmount = itemAmount,
                        Quantity = itemDto.Quantity,
                        Remarks = itemDto.Remarks,
                        CreatedOn = DateTime.UtcNow
                    };

                    quotation.QuotationItems.Add(quotationItem);

                    totalGrossWeight += itemDto.GrossWeight * itemDto.Quantity;
                    totalItemAmount += itemAmount * itemDto.Quantity;
                    totalQuantity += itemDto.Quantity;
                }

                quotation.Quantity = totalQuantity;
                quotation.TotalGrossWeight = totalGrossWeight;
                quotation.SubTotalAmount = totalItemAmount - (updateDto.OldMetalAmount ?? quotation.OldMetalAmount ?? 0);
            }

            // Update other fields
            if (updateDto.OldMetalWeight.HasValue)
                quotation.OldMetalWeight = updateDto.OldMetalWeight;

            if (updateDto.OldMetalRate.HasValue)
                quotation.OldMetalRate = updateDto.OldMetalRate;

            if (updateDto.OldMetalAmount.HasValue)
                quotation.OldMetalAmount = updateDto.OldMetalAmount;

            if (!string.IsNullOrEmpty(updateDto.PaymentMode))
                quotation.PaymentMode = updateDto.PaymentMode;

            if (updateDto.IsGstApplied.HasValue)
                quotation.IsGstApplied = updateDto.IsGstApplied.Value;

            if (updateDto.GstPercentage.HasValue)
                quotation.GstPercentage = updateDto.GstPercentage.Value;

            if (updateDto.ValidUntil.HasValue)
                quotation.ValidUntil = updateDto.ValidUntil;

            if (!string.IsNullOrEmpty(updateDto.Status))
                quotation.Status = updateDto.Status;

            if (updateDto.Remarks != null)
                quotation.Remarks = updateDto.Remarks;

            // Recalculate GST and total
            if (quotation.IsGstApplied)
            {
                quotation.GstAmount = quotation.SubTotalAmount * (quotation.GstPercentage / 100);
                quotation.TotalAmount = quotation.SubTotalAmount + quotation.GstAmount;
            }
            else
            {
                quotation.GstAmount = 0;
                quotation.TotalAmount = quotation.SubTotalAmount;
            }

            quotation.UpdatedOn = DateTime.UtcNow;
            await context.SaveChangesAsync();

            _logger.LogInformation("Quotation updated: {QuotationNumber}", quotation.QuotationNumber);

            return await GetQuotationByIdAsync(quotation.Id, clientCode) ?? throw new Exception("Failed to retrieve updated quotation");
        }

        public async Task<bool> DeleteQuotationAsync(int id, string clientCode)
        {
            using var context = await _dbContextFactory.CreateAsync(clientCode);

            var quotation = await context.Quotations
                .FirstOrDefaultAsync(q => q.Id == id && q.ClientCode == clientCode);

            if (quotation == null)
            {
                return false;
            }

            // Soft delete
            quotation.IsActive = false;
            quotation.UpdatedOn = DateTime.UtcNow;

            await context.SaveChangesAsync();

            _logger.LogInformation("Quotation deleted (soft): {QuotationNumber}", quotation.QuotationNumber);

            return true;
        }

        public async Task<bool> SendQuotationEmailAsync(int quotationId, string? toEmail, string? additionalMessage, string clientCode)
        {
            using var context = await _dbContextFactory.CreateAsync(clientCode);

            var quotation = await context.Quotations
                .Include(q => q.Customer)
                .Include(q => q.QuotationItems)
                .ThenInclude(qi => qi.Product)
                .FirstOrDefaultAsync(q => q.Id == quotationId && q.ClientCode == clientCode);

            if (quotation == null)
            {
                throw new KeyNotFoundException($"Quotation with ID {quotationId} not found");
            }

            var emailAddress = toEmail ?? quotation.Customer.Email;
            if (string.IsNullOrEmpty(emailAddress))
            {
                throw new InvalidOperationException("No email address available for customer");
            }

            // Generate email body
            var emailBody = GenerateQuotationEmailBody(quotation, additionalMessage);

            // Send email
            var result = await _emailService.SendEmailAsync(
                emailAddress,
                $"Quotation #{quotation.QuotationNumber} - {quotation.Customer.CustomerName}",
                emailBody,
                true);

            if (result)
            {
                quotation.Status = "Sent";
                quotation.UpdatedOn = DateTime.UtcNow;
                await context.SaveChangesAsync();

                _logger.LogInformation("Quotation email sent: {QuotationNumber} to {Email}", 
                    quotation.QuotationNumber, emailAddress);

                // Trigger webhook for email sent (async, non-blocking)
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await _webhookService.TriggerWebhookAsync("quotation.email.sent", new
                        {
                            quotationId = quotation.Id,
                            quotationNumber = quotation.QuotationNumber,
                            customerEmail = emailAddress,
                            status = "sent",
                            sentAt = DateTime.UtcNow
                        }, clientCode);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to trigger webhook for quotation email sent: {QuotationId}", quotation.Id);
                    }
                });
            }
            else
            {
                // Trigger webhook for email failed
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await _webhookService.TriggerWebhookAsync("quotation.email.failed", new
                        {
                            quotationId = quotation.Id,
                            quotationNumber = quotation.QuotationNumber,
                            customerEmail = emailAddress,
                            status = "failed"
                        }, clientCode);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to trigger webhook for quotation email failed: {QuotationId}", quotation.Id);
                    }
                });
            }

            return result;
        }

        private static (decimal makingAmount, decimal metalAmount, decimal itemAmount) CalculateItemAmounts(
            decimal netWeight,
            decimal goldRate,
            decimal making,
            string makingType,
            decimal stoneAmount)
        {
            // Calculate metal amount
            decimal metalAmount = netWeight * goldRate;

            // Calculate making amount based on type
            decimal makingAmount = 0;
            switch (makingType.ToLower())
            {
                case "pergram":
                    makingAmount = netWeight * making;
                    break;
                case "percentage":
                    makingAmount = metalAmount * (making / 100);
                    break;
                case "fixed":
                default:
                    makingAmount = making;
                    break;
            }

            // Calculate total item amount
            decimal itemAmount = metalAmount + makingAmount + stoneAmount;

            return (makingAmount, metalAmount, itemAmount);
        }

        private async Task<string> GenerateQuotationNumberAsync(ClientDbContext context, string clientCode)
        {
            var year = DateTime.UtcNow.Year;
            var prefix = $"QTN-{year}-";

            var lastQuotation = await context.Quotations
                .Where(q => q.QuotationNumber.StartsWith(prefix))
                .OrderByDescending(q => q.QuotationNumber)
                .FirstOrDefaultAsync();

            int sequence = 1;
            if (lastQuotation != null)
            {
                var lastSequence = lastQuotation.QuotationNumber.Replace(prefix, "");
                if (int.TryParse(lastSequence, out int lastNum))
                {
                    sequence = lastNum + 1;
                }
            }

            return $"{prefix}{sequence:D6}";
        }

        private static QuotationDto MapToDto(Quotation quotation)
        {
            return new QuotationDto
            {
                Id = quotation.Id,
                ClientCode = quotation.ClientCode,
                QuotationNumber = quotation.QuotationNumber,
                CustomerId = quotation.CustomerId,
                Customer = quotation.Customer != null ? new CustomerDto
                {
                    Id = quotation.Customer.Id,
                    ClientCode = quotation.Customer.ClientCode,
                    CustomerName = quotation.Customer.CustomerName,
                    Email = quotation.Customer.Email,
                    MobileNumber = quotation.Customer.MobileNumber,
                    AlternatePhone = quotation.Customer.AlternatePhone,
                    Address = quotation.Customer.Address,
                    City = quotation.Customer.City,
                    State = quotation.Customer.State,
                    PinCode = quotation.Customer.PinCode,
                    Country = quotation.Customer.Country,
                    CustomerType = quotation.Customer.CustomerType,
                    CompanyName = quotation.Customer.CompanyName,
                    GSTNumber = quotation.Customer.GSTNumber,
                    Notes = quotation.Customer.Notes,
                    CreatedOn = quotation.Customer.CreatedOn,
                    UpdatedOn = quotation.Customer.UpdatedOn,
                    IsActive = quotation.Customer.IsActive
                } : null,
                Quantity = quotation.Quantity,
                TotalGrossWeight = quotation.TotalGrossWeight,
                OldMetalWeight = quotation.OldMetalWeight,
                OldMetalRate = quotation.OldMetalRate,
                OldMetalAmount = quotation.OldMetalAmount,
                PaymentMode = quotation.PaymentMode,
                IsGstApplied = quotation.IsGstApplied,
                GstPercentage = quotation.GstPercentage,
                GstAmount = quotation.GstAmount,
                SubTotalAmount = quotation.SubTotalAmount,
                TotalAmount = quotation.TotalAmount,
                Status = quotation.Status,
                QuotationDate = quotation.QuotationDate,
                ValidUntil = quotation.ValidUntil,
                CreatedOn = quotation.CreatedOn,
                UpdatedOn = quotation.UpdatedOn,
                Remarks = quotation.Remarks,
                IsActive = quotation.IsActive,
                Items = quotation.QuotationItems.Select(qi => new QuotationItemDto
                {
                    Id = qi.Id,
                    QuotationId = qi.QuotationId,
                    ProductId = qi.ProductId,
                    ItemCode = qi.ItemCode,
                    RfidCode = qi.RfidCode,
                    DesignName = qi.DesignName,
                    Purity = qi.Purity,
                    GrossWeight = qi.GrossWeight,
                    StoneWeight = qi.StoneWeight,
                    NetWeight = qi.NetWeight,
                    GoldRate = qi.GoldRate,
                    Making = qi.Making,
                    MakingType = qi.MakingType,
                    StoneAmount = qi.StoneAmount,
                    MakingAmount = qi.MakingAmount,
                    MetalAmount = qi.MetalAmount,
                    ItemAmount = qi.ItemAmount,
                    Quantity = qi.Quantity,
                    Remarks = qi.Remarks
                }).ToList()
            };
        }

        private static string GenerateQuotationEmailBody(Quotation quotation, string? additionalMessage)
        {
            var itemsHtml = string.Join("", quotation.QuotationItems.Select((item, index) => $@"
                <tr>
                    <td style=""padding: 10px; border-bottom: 1px solid #e0e0e0;"">{index + 1}</td>
                    <td style=""padding: 10px; border-bottom: 1px solid #e0e0e0;"">{item.ItemCode}</td>
                    <td style=""padding: 10px; border-bottom: 1px solid #e0e0e0;"">{item.DesignName}</td>
                    <td style=""padding: 10px; border-bottom: 1px solid #e0e0e0;"">{item.Purity}</td>
                    <td style=""padding: 10px; border-bottom: 1px solid #e0e0e0; text-align: right;"">{item.GrossWeight:F3} g</td>
                    <td style=""padding: 10px; border-bottom: 1px solid #e0e0e0; text-align: right;"">{item.NetWeight:F3} g</td>
                    <td style=""padding: 10px; border-bottom: 1px solid #e0e0e0; text-align: right;"">₹{item.ItemAmount:F2}</td>
                </tr>"));

            return $@"
<!DOCTYPE html>
<html lang=""en"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>Quotation #{quotation.QuotationNumber}</title>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 800px; margin: 0 auto; padding: 20px; }}
        .header {{ background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; padding: 30px; text-align: center; border-radius: 10px 10px 0 0; }}
        .content {{ background: #f9f9f9; padding: 30px; border-radius: 0 0 10px 10px; }}
        .info-box {{ background: white; padding: 20px; margin: 20px 0; border-radius: 5px; border-left: 4px solid #667eea; }}
        table {{ width: 100%; border-collapse: collapse; margin: 20px 0; background: white; }}
        th {{ background: #667eea; color: white; padding: 12px; text-align: left; }}
        .total-box {{ background: white; padding: 20px; margin: 20px 0; border-radius: 5px; }}
        .total-row {{ display: flex; justify-content: space-between; padding: 10px 0; border-bottom: 1px solid #e0e0e0; }}
        .total-row:last-child {{ border-bottom: none; font-weight: bold; font-size: 1.2em; }}
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""header"">
            <h1>Quotation #{quotation.QuotationNumber}</h1>
            <p>Date: {quotation.QuotationDate:dd MMM yyyy}</p>
        </div>
        <div class=""content"">
            <div class=""info-box"">
                <h3>Customer Details</h3>
                <p><strong>Name:</strong> {quotation.Customer.CustomerName}</p>
                <p><strong>Email:</strong> {quotation.Customer.Email}</p>
                {(string.IsNullOrEmpty(quotation.Customer.MobileNumber) ? "" : $"<p><strong>Phone:</strong> {quotation.Customer.MobileNumber}</p>")}
                {(string.IsNullOrEmpty(quotation.Customer.Address) ? "" : $"<p><strong>Address:</strong> {quotation.Customer.Address}</p>")}
            </div>

            <h3>Items</h3>
            <table>
                <thead>
                    <tr>
                        <th>#</th>
                        <th>Item Code</th>
                        <th>Design</th>
                        <th>Purity</th>
                        <th>Gross Weight</th>
                        <th>Net Weight</th>
                        <th>Amount</th>
                    </tr>
                </thead>
                <tbody>
                    {itemsHtml}
                </tbody>
            </table>

            <div class=""total-box"">
                <div class=""total-row"">
                    <span>Subtotal:</span>
                    <span>₹{quotation.SubTotalAmount:F2}</span>
                </div>
                {(quotation.IsGstApplied ? $@"
                <div class=""total-row"">
                    <span>GST ({quotation.GstPercentage}%):</span>
                    <span>₹{quotation.GstAmount:F2}</span>
                </div>" : "")}
                <div class=""total-row"">
                    <span><strong>Total Amount:</strong></span>
                    <span><strong>₹{quotation.TotalAmount:F2}</strong></span>
                </div>
            </div>

            {(string.IsNullOrEmpty(additionalMessage) ? "" : $"<div class=\"info-box\"><p>{additionalMessage}</p></div>")}
            {(quotation.ValidUntil.HasValue ? $"<p><strong>Valid Until:</strong> {quotation.ValidUntil.Value:dd MMM yyyy}</p>" : "")}
        </div>
    </div>
</body>
</html>";
        }
    }
}

