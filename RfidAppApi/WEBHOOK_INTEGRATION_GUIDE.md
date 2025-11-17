# Webhook Integration Guide & Performance Optimization

## 🎯 Overview
This document outlines where webhooks should be integrated to make the API faster and more responsive, and provides performance optimization recommendations.

## 📋 Webhook Integration Points

### 1. **Bulk Operations (HIGH IMPACT)**
These operations are long-running and should trigger webhooks when complete:

#### Product Excel Bulk Upload
- **Event**: `product.bulk_upload.completed`
- **Trigger**: After Excel file processing completes
- **Payload**: `{ totalProducts, successfullyCreated, failed, errors, processingTime }`
- **Location**: `Services/ProductExcelService.cs` → `UploadProductsFromExcelAsync`
- **Impact**: ⚡ **CRITICAL** - Prevents API timeout on large uploads (100k+ products)

#### RFID Excel Bulk Upload
- **Event**: `rfid.bulk_upload.completed`
- **Trigger**: After RFID Excel processing completes
- **Payload**: `{ totalRfids, successfullyCreated, failed, errors }`
- **Location**: `Services/RfidExcelService.cs`
- **Impact**: ⚡ **HIGH** - Large RFID imports can take minutes

### 2. **Email Operations (MEDIUM IMPACT)**
Make email sending non-blocking with webhook notifications:

#### Quotation Email Sent
- **Event**: `quotation.email.sent`
- **Trigger**: After quotation email is successfully sent
- **Payload**: `{ quotationId, quotationNumber, customerEmail, status }`
- **Location**: `Services/QuotationService.cs` → `SendQuotationEmailAsync`
- **Impact**: ⚡ **MEDIUM** - Email sending can take 2-5 seconds

#### Invoice Email (if implemented)
- **Event**: `invoice.email.sent`
- **Trigger**: After invoice email sent
- **Payload**: `{ invoiceId, invoiceNumber, customerEmail }`
- **Location**: `Services/InvoiceService.cs` (if email feature added)

#### User Registration Emails
- **Event**: `user.registration.email.sent`
- **Trigger**: After welcome email sent
- **Payload**: `{ userId, email, emailSent }`
- **Location**: `Services/UserService.cs` → `Register`
- **Impact**: ⚡ **LOW** - Already async, but webhook provides status

### 3. **Business Operations (MEDIUM IMPACT)**

#### Quotation Created
- **Event**: `quotation.created`
- **Trigger**: After quotation is successfully created
- **Payload**: `{ quotationId, quotationNumber, customerId, totalAmount, status }`
- **Location**: `Services/QuotationService.cs` → `CreateQuotationAsync`
- **Impact**: ⚡ **MEDIUM** - Enables real-time notifications to external systems

#### Quotation Updated
- **Event**: `quotation.updated`
- **Trigger**: After quotation is updated
- **Payload**: `{ quotationId, quotationNumber, changes }`
- **Location**: `Services/QuotationService.cs` → `UpdateQuotationAsync`

#### Invoice Created
- **Event**: `invoice.created`
- **Trigger**: After invoice is successfully created
- **Payload**: `{ invoiceId, invoiceNumber, productId, totalAmount, customerName }`
- **Location**: `Services/InvoiceService.cs` → `CreateInvoiceAsync`
- **Impact**: ⚡ **MEDIUM** - Enables integration with accounting systems

#### Stock Transfer Completed
- **Event**: `stock.transfer.completed`
- **Trigger**: After stock transfer is completed
- **Payload**: `{ transferId, transferNumber, sourceBranch, destinationBranch, status }`
- **Location**: `Services/StockTransferService.cs`
- **Impact**: ⚡ **MEDIUM** - Real-time inventory updates

### 4. **Reporting & Analytics (HIGH IMPACT)**

#### Report Generation Completed
- **Event**: `report.generated`
- **Trigger**: After complex report generation completes
- **Payload**: `{ reportType, reportId, downloadUrl, generatedAt }`
- **Location**: `Services/ReportingService.cs`
- **Impact**: ⚡ **HIGH** - Large reports can take 30+ seconds

#### Dashboard Data Export
- **Event**: `dashboard.export.completed`
- **Trigger**: After Excel export completes
- **Payload**: `{ exportId, downloadUrl, recordCount }`
- **Location**: `Services/DashboardService.cs` → `ExportDashboardDataToExcelAsync`
- **Impact**: ⚡ **HIGH** - Excel generation can be slow

### 5. **Stock Verification (HIGH IMPACT)**

#### Stock Verification Completed
- **Event**: `stock.verification.completed`
- **Trigger**: After stock verification session completes
- **Payload**: `{ verificationId, totalScanned, matched, unmatched, missing }`
- **Location**: `Services/StockVerificationService.cs`
- **Impact**: ⚡ **CRITICAL** - Stock verification can take 10+ minutes

### 6. **Database Operations (MEDIUM IMPACT)**

#### Database Migration Completed
- **Event**: `database.migration.completed`
- **Trigger**: After migration completes
- **Payload**: `{ clientCode, migrationType, tablesCreated, status }`
- **Location**: `Services/DatabaseMigrationService.cs`
- **Impact**: ⚡ **MEDIUM** - Migrations can take several minutes

## 🚀 Performance Optimization Recommendations

### 1. **Caching Strategy**

#### Master Data Caching
- **Location**: Already implemented in `UserFriendlyProductService`
- **Enhancement**: Add Redis caching for:
  - CategoryMaster, ProductMaster, DesignMaster, PurityMaster
  - BranchMaster, CounterMaster, BoxMaster
- **Impact**: ⚡ **HIGH** - Reduces database queries by 80%+

#### Query Result Caching
- Cache frequently accessed data:
  - Dashboard statistics (5-minute TTL)
  - Product lists (1-minute TTL)
  - Customer lists (2-minute TTL)
- **Implementation**: Use `IMemoryCache` or Redis

### 2. **Database Query Optimization**

#### Use Projection Instead of Full Entities
```csharp
// ❌ BAD - Loads full entity
var products = await context.ProductDetails.ToListAsync();

// ✅ GOOD - Only loads needed fields
var products = await context.ProductDetails
    .Select(p => new { p.Id, p.ItemCode, p.GrossWeight })
    .ToListAsync();
```

#### Batch Operations
- Use `AddRange` instead of multiple `Add` calls
- Process in batches of 1000 records
- **Location**: Already implemented in bulk operations

#### Index Optimization
- ✅ Already well-indexed in `ClientDbContext`
- Consider adding composite indexes for common query patterns

### 3. **Async/Await Best Practices**

#### Fire-and-Forget for Non-Critical Operations
```csharp
// Email sending (already implemented)
_ = Task.Run(async () => {
    await _emailService.SendEmailAsync(...);
});

// Webhook triggering (recommended)
_ = Task.Run(async () => {
    await _webhookService.TriggerWebhookAsync(...);
});
```

### 4. **Background Job Processing**

#### Implement Hangfire or Quartz.NET for:
- Retry failed webhooks
- Generate reports in background
- Process bulk uploads asynchronously
- Clean up old webhook events

### 5. **Response Compression**

#### Enable Response Compression
```csharp
// In Program.cs
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
    options.Providers.Add<GzipCompressionProvider>();
});
```

### 6. **API Response Pagination**

#### All List Endpoints Should Support Pagination
- ✅ Already implemented in some endpoints
- ⚠️ Add to: Customer, Quotation, Product lists
- **Default**: pageSize = 50, maxPageSize = 1000

### 7. **Connection Pooling**

#### Optimize Database Connections
```json
// appsettings.json
"ConnectionStrings": {
  "DefaultConnection": "Server=...;Database=...;Max Pool Size=100;Min Pool Size=10;..."
}
```

## 📊 Expected Performance Improvements

| Operation | Current | With Webhooks | Improvement |
|-----------|---------|---------------|-------------|
| Bulk Product Upload (100k) | 5-10 min (blocking) | 5-10 min (async) | ⚡ API responds in <1s |
| Quotation Email | 2-5s (blocking) | <100ms (async) | ⚡ 20-50x faster |
| Stock Verification | 10+ min (blocking) | <1s (async) | ⚡ 600x faster |
| Report Generation | 30s (blocking) | <1s (async) | ⚡ 30x faster |
| Dashboard Export | 15s (blocking) | <1s (async) | ⚡ 15x faster |

## 🔧 Implementation Priority

### Phase 1 (Immediate - High Impact)
1. ✅ Webhook infrastructure (DONE)
2. ⚠️ Integrate webhooks in bulk uploads
3. ⚠️ Integrate webhooks in email operations
4. ⚠️ Integrate webhooks in stock verification

### Phase 2 (Short-term - Medium Impact)
1. Integrate webhooks in quotation/invoice operations
2. Add caching layer (Redis)
3. Implement background job processing
4. Add response compression

### Phase 3 (Long-term - Optimization)
1. Query optimization audit
2. Database connection pooling tuning
3. API response pagination everywhere
4. Monitoring and alerting

## 📝 Webhook Event Types Reference

### Product Events
- `product.bulk_upload.started`
- `product.bulk_upload.completed`
- `product.bulk_upload.failed`
- `product.created`
- `product.updated`
- `product.deleted`

### Quotation Events
- `quotation.created`
- `quotation.updated`
- `quotation.deleted`
- `quotation.email.sent`
- `quotation.email.failed`
- `quotation.status.changed`

### Invoice Events
- `invoice.created`
- `invoice.updated`
- `invoice.payment.received`
- `invoice.email.sent`

### Stock Events
- `stock.transfer.completed`
- `stock.verification.started`
- `stock.verification.completed`
- `stock.movement.recorded`

### Customer Events
- `customer.created`
- `customer.updated`
- `customer.deleted`

### User Events
- `user.registered`
- `user.registration.email.sent`
- `user.activated`
- `user.deactivated`

### System Events
- `database.migration.completed`
- `report.generated`
- `dashboard.export.completed`

## 🔐 Webhook Security

1. **HMAC Signature**: Use secret key to sign payloads
2. **HTTPS Only**: Require HTTPS for webhook URLs
3. **Retry Logic**: Exponential backoff (5min, 10min, 20min)
4. **Rate Limiting**: Max 100 webhooks per minute per client
5. **Event Filtering**: Clients can subscribe to specific events only

## 📈 Monitoring

Track these metrics:
- Webhook delivery success rate
- Average delivery time
- Failed webhook retry rate
- Webhook queue size
- API response times (before/after webhooks)

