using Microsoft.EntityFrameworkCore;
using RfidAppApi.Models;

namespace RfidAppApi.Data
{
    public class ClientDbContext : DbContext
    {
        private readonly string _clientCode;

        public ClientDbContext(DbContextOptions<ClientDbContext> options, string clientCode) : base(options)
        {
            _clientCode = clientCode;
        }

        // Client Database - All Product and RFID Tables
        public DbSet<CategoryMaster> CategoryMasters { get; set; }
        public DbSet<ProductMaster> ProductMasters { get; set; }
        public DbSet<DesignMaster> DesignMasters { get; set; }
        public DbSet<PurityMaster> PurityMasters { get; set; }
        public DbSet<BranchMaster> BranchMasters { get; set; }
        public DbSet<CounterMaster> CounterMasters { get; set; }
        public DbSet<BoxMaster> BoxMasters { get; set; }
        public DbSet<Rfid> Rfids { get; set; }
        public DbSet<ProductDetails> ProductDetails { get; set; }
        public DbSet<ProductRfidAssignment> ProductRfidAssignments { get; set; }
        public DbSet<Invoice> Invoices { get; set; }
        public DbSet<InvoicePayment> InvoicePayments { get; set; }
        public DbSet<ProductImage> ProductImages { get; set; }
        public DbSet<StockMovement> StockMovements { get; set; }
        public DbSet<DailyStockBalance> DailyStockBalances { get; set; }
        public DbSet<StockVerification> StockVerifications { get; set; }
        public DbSet<StockVerificationDetail> StockVerificationDetails { get; set; }
        public DbSet<StockTransfer> StockTransfers { get; set; }
        public DbSet<ProductCustomField> ProductCustomFields { get; set; } = null!;
        
        // Customer Management (separate table for future modules)
        public DbSet<Customer> Customers { get; set; }
        
        // Quotation Management
        public DbSet<Quotation> Quotations { get; set; }
        public DbSet<QuotationItem> QuotationItems { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure table names
            modelBuilder.Entity<CategoryMaster>().ToTable("tblCategoryMaster");
            modelBuilder.Entity<ProductMaster>().ToTable("tblProductMaster");
            modelBuilder.Entity<DesignMaster>().ToTable("tblDesignMaster");
            modelBuilder.Entity<PurityMaster>().ToTable("tblPurityMaster");
            modelBuilder.Entity<BranchMaster>().ToTable("tblBranchMaster");
            modelBuilder.Entity<CounterMaster>().ToTable("tblCounterMaster");
            modelBuilder.Entity<BoxMaster>().ToTable("tblBoxMaster");
            modelBuilder.Entity<Rfid>().ToTable("tblRFID");
            modelBuilder.Entity<ProductDetails>().ToTable("tblProductDetails");
            modelBuilder.Entity<ProductRfidAssignment>().ToTable("tblProductRFIDAssignment");
            modelBuilder.Entity<Invoice>().ToTable("tblInvoice");
            modelBuilder.Entity<InvoicePayment>().ToTable("tblInvoicePayment");
            modelBuilder.Entity<ProductImage>().ToTable("tblProductImage");
            modelBuilder.Entity<StockMovement>().ToTable("tblStockMovement");
            modelBuilder.Entity<DailyStockBalance>().ToTable("tblDailyStockBalance");
            modelBuilder.Entity<StockVerification>().ToTable("tblStockVerification");
            modelBuilder.Entity<StockVerificationDetail>().ToTable("tblStockVerificationDetail");
            modelBuilder.Entity<StockTransfer>().ToTable("tblStockTransfer");
            modelBuilder.Entity<ProductCustomField>().ToTable("tblProductCustomFields");
            modelBuilder.Entity<Customer>().ToTable("tblCustomer");
            modelBuilder.Entity<Quotation>().ToTable("tblQuotation");
            modelBuilder.Entity<QuotationItem>().ToTable("tblQuotationItem");

            // Configure relationships
            modelBuilder.Entity<CounterMaster>()
                .HasOne(c => c.Branch)
                .WithMany()
                .HasForeignKey(c => c.BranchId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ProductDetails>()
                .HasOne(p => p.Category)
                .WithMany()
                .HasForeignKey(p => p.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ProductDetails>()
                .HasOne(p => p.Product)
                .WithMany()
                .HasForeignKey(p => p.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ProductDetails>()
                .HasOne(p => p.Design)
                .WithMany()
                .HasForeignKey(p => p.DesignId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ProductDetails>()
                .HasOne(p => p.Purity)
                .WithMany()
                .HasForeignKey(p => p.PurityId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ProductDetails>()
                .HasOne(p => p.Branch)
                .WithMany()
                .HasForeignKey(p => p.BranchId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ProductDetails>()
                .HasOne(p => p.Counter)
                .WithMany()
                .HasForeignKey(p => p.CounterId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ProductDetails>()
                .HasOne(p => p.Box)
                .WithMany()
                .HasForeignKey(p => p.BoxId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ProductCustomField>()
                .HasOne(pcf => pcf.ProductDetails)
                .WithMany(p => p.CustomFields)
                .HasForeignKey(pcf => pcf.ProductDetailsId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ProductRfidAssignment>()
                .HasOne(pr => pr.Product)
                .WithMany()
                .HasForeignKey(pr => pr.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ProductRfidAssignment>()
                .HasOne(pr => pr.Rfid)
                .WithMany()
                .HasForeignKey(pr => pr.RFIDCode)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Invoice>()
                .HasOne(i => i.Product)
                .WithMany()
                .HasForeignKey(i => i.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<InvoicePayment>()
                .HasOne(ip => ip.Invoice)
                .WithMany(i => i.Payments)
                .HasForeignKey(ip => ip.InvoiceId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ProductImage>()
                .HasOne(pi => pi.Product)
                .WithMany()
                .HasForeignKey(pi => pi.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<StockMovement>()
                .HasOne(sm => sm.Product)
                .WithMany()
                .HasForeignKey(sm => sm.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<StockMovement>()
                .HasOne(sm => sm.Branch)
                .WithMany()
                .HasForeignKey(sm => sm.BranchId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<StockMovement>()
                .HasOne(sm => sm.Counter)
                .WithMany()
                .HasForeignKey(sm => sm.CounterId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<StockMovement>()
                .HasOne(sm => sm.Category)
                .WithMany()
                .HasForeignKey(sm => sm.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<DailyStockBalance>()
                .HasOne(dsb => dsb.Product)
                .WithMany()
                .HasForeignKey(dsb => dsb.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<DailyStockBalance>()
                .HasOne(dsb => dsb.Branch)
                .WithMany()
                .HasForeignKey(dsb => dsb.BranchId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<DailyStockBalance>()
                .HasOne(dsb => dsb.Counter)
                .WithMany()
                .HasForeignKey(dsb => dsb.CounterId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<DailyStockBalance>()
                .HasOne(dsb => dsb.Category)
                .WithMany()
                .HasForeignKey(dsb => dsb.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            // Stock Verification Relationships
            modelBuilder.Entity<StockVerification>()
                .HasOne(sv => sv.Branch)
                .WithMany()
                .HasForeignKey(sv => sv.BranchId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<StockVerification>()
                .HasOne(sv => sv.Counter)
                .WithMany()
                .HasForeignKey(sv => sv.CounterId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<StockVerification>()
                .HasOne(sv => sv.Category)
                .WithMany()
                .HasForeignKey(sv => sv.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<StockVerificationDetail>()
                .HasOne(svd => svd.StockVerification)
                .WithMany(sv => sv.VerificationDetails)
                .HasForeignKey(svd => svd.StockVerificationId)
                .OnDelete(DeleteBehavior.Cascade);

            // Stock Transfer Relationships
            modelBuilder.Entity<StockTransfer>()
                .HasOne(st => st.Product)
                .WithMany()
                .HasForeignKey(st => st.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<StockTransfer>()
                .HasOne(st => st.SourceBranch)
                .WithMany()
                .HasForeignKey(st => st.SourceBranchId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<StockTransfer>()
                .HasOne(st => st.SourceCounter)
                .WithMany()
                .HasForeignKey(st => st.SourceCounterId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<StockTransfer>()
                .HasOne(st => st.SourceBox)
                .WithMany()
                .HasForeignKey(st => st.SourceBoxId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<StockTransfer>()
                .HasOne(st => st.DestinationBranch)
                .WithMany()
                .HasForeignKey(st => st.DestinationBranchId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<StockTransfer>()
                .HasOne(st => st.DestinationCounter)
                .WithMany()
                .HasForeignKey(st => st.DestinationCounterId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<StockTransfer>()
                .HasOne(st => st.DestinationBox)
                .WithMany()
                .HasForeignKey(st => st.DestinationBoxId)
                .OnDelete(DeleteBehavior.Restrict);

            // Customer Relationships
            modelBuilder.Entity<Quotation>()
                .HasOne(q => q.Customer)
                .WithMany(c => c.Quotations)
                .HasForeignKey(q => q.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);

            // Quotation Relationships
            modelBuilder.Entity<QuotationItem>()
                .HasOne(qi => qi.Quotation)
                .WithMany(q => q.QuotationItems)
                .HasForeignKey(qi => qi.QuotationId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<QuotationItem>()
                .HasOne(qi => qi.Product)
                .WithMany()
                .HasForeignKey(qi => qi.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            // Global Query Filter for Client Code
            modelBuilder.Entity<Rfid>().HasQueryFilter(e => e.ClientCode == _clientCode);
            modelBuilder.Entity<ProductDetails>().HasQueryFilter(e => e.ClientCode == _clientCode);
            modelBuilder.Entity<ProductCustomField>().HasQueryFilter(e => e.ProductDetails.ClientCode == _clientCode);
            modelBuilder.Entity<ProductRfidAssignment>().HasQueryFilter(e => e.Product.ClientCode == _clientCode);
            modelBuilder.Entity<Invoice>().HasQueryFilter(e => e.ClientCode == _clientCode);
            modelBuilder.Entity<InvoicePayment>().HasQueryFilter(e => e.Invoice.ClientCode == _clientCode);
            modelBuilder.Entity<ProductImage>().HasQueryFilter(e => e.ClientCode == _clientCode);
            modelBuilder.Entity<StockMovement>().HasQueryFilter(e => e.ClientCode == _clientCode);
            modelBuilder.Entity<DailyStockBalance>().HasQueryFilter(e => e.ClientCode == _clientCode);
            modelBuilder.Entity<StockVerification>().HasQueryFilter(e => e.ClientCode == _clientCode);
            modelBuilder.Entity<StockVerificationDetail>().HasQueryFilter(e => e.ClientCode == _clientCode);
            modelBuilder.Entity<StockTransfer>().HasQueryFilter(e => e.ClientCode == _clientCode);
            modelBuilder.Entity<Customer>().HasQueryFilter(e => e.ClientCode == _clientCode);
            modelBuilder.Entity<Quotation>().HasQueryFilter(e => e.ClientCode == _clientCode);
            modelBuilder.Entity<QuotationItem>().HasQueryFilter(e => e.ClientCode == _clientCode);

            // HIGH PERFORMANCE INDEXES FOR LAKHS OF RECORDS
            // Master Data Indexes
            modelBuilder.Entity<CategoryMaster>()
                .HasIndex(c => c.CategoryName)
                .IsUnique();

            modelBuilder.Entity<ProductMaster>()
                .HasIndex(p => p.ProductName)
                .IsUnique();

            modelBuilder.Entity<DesignMaster>()
                .HasIndex(d => d.DesignName)
                .IsUnique();

            modelBuilder.Entity<PurityMaster>()
                .HasIndex(p => p.PurityName)
                .IsUnique();

            modelBuilder.Entity<BranchMaster>()
                .HasIndex(b => b.BranchName)
                .IsUnique();

            modelBuilder.Entity<BoxMaster>()
                .HasIndex(b => b.BoxName)
                .IsUnique();

            // RFID Table - High Performance Indexes
            modelBuilder.Entity<Rfid>()
                .HasIndex(r => r.RFIDCode)
                .IsUnique();

            modelBuilder.Entity<Rfid>()
                .HasIndex(r => r.IsActive);

            modelBuilder.Entity<Rfid>()
                .HasIndex(r => r.CreatedOn);

            modelBuilder.Entity<Rfid>()
                .HasIndex(r => new { r.RFIDCode, r.IsActive });

            // Product Details - High Performance Indexes
            modelBuilder.Entity<ProductDetails>()
                .HasIndex(p => p.ItemCode)
                .IsUnique();

            modelBuilder.Entity<ProductDetails>()
                .HasIndex(p => p.CategoryId);

            modelBuilder.Entity<ProductDetails>()
                .HasIndex(p => p.ProductId);

            modelBuilder.Entity<ProductDetails>()
                .HasIndex(p => p.BranchId);

            modelBuilder.Entity<ProductDetails>()
                .HasIndex(p => p.CounterId);

            modelBuilder.Entity<ProductDetails>()
                .HasIndex(p => p.BoxId);

            modelBuilder.Entity<ProductDetails>()
                .HasIndex(p => p.CreatedOn);

            modelBuilder.Entity<ProductDetails>()
                .HasIndex(p => new { p.CategoryId, p.BranchId });

            modelBuilder.Entity<ProductDetails>()
                .HasIndex(p => new { p.ItemCode, p.Status });

            // Product RFID Assignment - High Performance Indexes
            modelBuilder.Entity<ProductRfidAssignment>()
                .HasIndex(pr => pr.ProductId);

            modelBuilder.Entity<ProductRfidAssignment>()
                .HasIndex(pr => pr.RFIDCode);

            modelBuilder.Entity<ProductRfidAssignment>()
                .HasIndex(pr => pr.AssignedOn);

            modelBuilder.Entity<ProductRfidAssignment>()
                .HasIndex(pr => new { pr.ProductId, pr.RFIDCode })
                .IsUnique();

            // Composite Indexes for Complex Queries
            modelBuilder.Entity<ProductDetails>()
                .HasIndex(p => new { p.CategoryId, p.BranchId, p.Status });

            modelBuilder.Entity<Rfid>()
                .HasIndex(r => new { r.IsActive, r.CreatedOn });

            // Include Indexes for Covering Queries
            modelBuilder.Entity<ProductDetails>()
                .HasIndex(p => new { p.CategoryId, p.ItemCode, p.Status, p.CreatedOn });

            // Stock Transfer - High Performance Indexes
            modelBuilder.Entity<StockTransfer>()
                .HasIndex(st => st.TransferNumber)
                .IsUnique();

            modelBuilder.Entity<StockTransfer>()
                .HasIndex(st => st.ProductId);

            modelBuilder.Entity<StockTransfer>()
                .HasIndex(st => st.RfidCode);

            modelBuilder.Entity<StockTransfer>()
                .HasIndex(st => st.Status);

            modelBuilder.Entity<StockTransfer>()
                .HasIndex(st => st.TransferDate);

            modelBuilder.Entity<StockTransfer>()
                .HasIndex(st => st.SourceBranchId);

            modelBuilder.Entity<StockTransfer>()
                .HasIndex(st => st.SourceCounterId);

            modelBuilder.Entity<StockTransfer>()
                .HasIndex(st => st.DestinationBranchId);

            modelBuilder.Entity<StockTransfer>()
                .HasIndex(st => st.DestinationCounterId);

            // Composite Indexes for Stock Transfer Queries
            modelBuilder.Entity<StockTransfer>()
                .HasIndex(st => new { st.Status, st.TransferDate });

            modelBuilder.Entity<StockTransfer>()
                .HasIndex(st => new { st.SourceBranchId, st.SourceCounterId, st.Status });

            modelBuilder.Entity<StockTransfer>()
                .HasIndex(st => new { st.DestinationBranchId, st.DestinationCounterId, st.Status });

            modelBuilder.Entity<StockTransfer>()
                .HasIndex(st => new { st.ProductId, st.Status });

            modelBuilder.Entity<StockTransfer>()
                .HasIndex(st => new { st.RfidCode, st.Status });

            modelBuilder.Entity<Rfid>()
                .HasIndex(r => new { r.IsActive, r.RFIDCode, r.CreatedOn });

            // Invoice Table - High Performance Indexes
            modelBuilder.Entity<Invoice>()
                .HasIndex(i => i.InvoiceNumber)
                .IsUnique();

            modelBuilder.Entity<Invoice>()
                .HasIndex(i => i.ProductId);

            modelBuilder.Entity<Invoice>()
                .HasIndex(i => i.SoldOn);

            modelBuilder.Entity<Invoice>()
                .HasIndex(i => i.CreatedOn);

            modelBuilder.Entity<Invoice>()
                .HasIndex(i => i.InvoiceType);

            modelBuilder.Entity<Invoice>()
                .HasIndex(i => i.RfidCode);

            modelBuilder.Entity<Invoice>()
                .HasIndex(i => new { i.ClientCode, i.SoldOn });

            modelBuilder.Entity<Invoice>()
                .HasIndex(i => new { i.InvoiceType, i.SoldOn });

            modelBuilder.Entity<Invoice>()
                .HasIndex(i => new { i.ProductId, i.SoldOn });

            // Invoice Payment Table - High Performance Indexes
            modelBuilder.Entity<InvoicePayment>()
                .HasIndex(ip => ip.InvoiceId);

            modelBuilder.Entity<InvoicePayment>()
                .HasIndex(ip => ip.PaymentMethod);

            modelBuilder.Entity<InvoicePayment>()
                .HasIndex(ip => ip.CreatedOn);

            modelBuilder.Entity<InvoicePayment>()
                .HasIndex(ip => new { ip.InvoiceId, ip.PaymentMethod });

            modelBuilder.Entity<InvoicePayment>()
                .HasIndex(ip => new { ip.PaymentMethod, ip.CreatedOn });

            // Product Image Table - High Performance Indexes
            modelBuilder.Entity<ProductImage>()
                .HasIndex(pi => pi.ProductId);

            modelBuilder.Entity<ProductImage>()
                .HasIndex(pi => pi.ImageType);

            modelBuilder.Entity<ProductImage>()
                .HasIndex(pi => pi.IsActive);

            modelBuilder.Entity<ProductImage>()
                .HasIndex(pi => pi.CreatedOn);

            modelBuilder.Entity<ProductImage>()
                .HasIndex(pi => new { pi.ProductId, pi.IsActive });

            modelBuilder.Entity<ProductImage>()
                .HasIndex(pi => new { pi.ProductId, pi.ImageType });

            modelBuilder.Entity<ProductImage>()
                .HasIndex(pi => new { pi.ProductId, pi.DisplayOrder });

            modelBuilder.Entity<ProductImage>()
                .HasIndex(pi => new { pi.IsActive, pi.CreatedOn });

            // Stock Movement Table - High Performance Indexes
            modelBuilder.Entity<StockMovement>()
                .HasIndex(sm => sm.ProductId);

            modelBuilder.Entity<StockMovement>()
                .HasIndex(sm => sm.MovementType);

            modelBuilder.Entity<StockMovement>()
                .HasIndex(sm => sm.MovementDate);

            modelBuilder.Entity<StockMovement>()
                .HasIndex(sm => sm.CreatedOn);

            modelBuilder.Entity<StockMovement>()
                .HasIndex(sm => sm.BranchId);

            modelBuilder.Entity<StockMovement>()
                .HasIndex(sm => sm.CounterId);

            modelBuilder.Entity<StockMovement>()
                .HasIndex(sm => sm.CategoryId);

            modelBuilder.Entity<StockMovement>()
                .HasIndex(sm => sm.RfidCode);

            modelBuilder.Entity<StockMovement>()
                .HasIndex(sm => sm.ReferenceNumber);

            modelBuilder.Entity<StockMovement>()
                .HasIndex(sm => new { sm.ProductId, sm.MovementDate });

            modelBuilder.Entity<StockMovement>()
                .HasIndex(sm => new { sm.MovementType, sm.MovementDate });

            modelBuilder.Entity<StockMovement>()
                .HasIndex(sm => new { sm.BranchId, sm.MovementDate });

            modelBuilder.Entity<StockMovement>()
                .HasIndex(sm => new { sm.CounterId, sm.MovementDate });

            modelBuilder.Entity<StockMovement>()
                .HasIndex(sm => new { sm.CategoryId, sm.MovementDate });

            modelBuilder.Entity<StockMovement>()
                .HasIndex(sm => new { sm.ClientCode, sm.MovementDate });

            // Daily Stock Balance Table - High Performance Indexes
            modelBuilder.Entity<DailyStockBalance>()
                .HasIndex(dsb => dsb.ProductId);

            modelBuilder.Entity<DailyStockBalance>()
                .HasIndex(dsb => dsb.BalanceDate);

            modelBuilder.Entity<DailyStockBalance>()
                .HasIndex(dsb => dsb.CreatedOn);

            modelBuilder.Entity<DailyStockBalance>()
                .HasIndex(dsb => dsb.BranchId);

            modelBuilder.Entity<DailyStockBalance>()
                .HasIndex(dsb => dsb.CounterId);

            modelBuilder.Entity<DailyStockBalance>()
                .HasIndex(dsb => dsb.CategoryId);

            modelBuilder.Entity<DailyStockBalance>()
                .HasIndex(dsb => dsb.RfidCode);

            modelBuilder.Entity<DailyStockBalance>()
                .HasIndex(dsb => new { dsb.ProductId, dsb.BalanceDate })
                .IsUnique();

            modelBuilder.Entity<DailyStockBalance>()
                .HasIndex(dsb => new { dsb.BranchId, dsb.BalanceDate });

            modelBuilder.Entity<DailyStockBalance>()
                .HasIndex(dsb => new { dsb.CounterId, dsb.BalanceDate });

            modelBuilder.Entity<DailyStockBalance>()
                .HasIndex(dsb => new { dsb.CategoryId, dsb.BalanceDate });

            modelBuilder.Entity<DailyStockBalance>()
                .HasIndex(dsb => new { dsb.ClientCode, dsb.BalanceDate });

            modelBuilder.Entity<DailyStockBalance>()
                .HasIndex(dsb => new { dsb.ProductId, dsb.BranchId, dsb.BalanceDate });

            modelBuilder.Entity<DailyStockBalance>()
                .HasIndex(dsb => new { dsb.ProductId, dsb.CounterId, dsb.BalanceDate });

            // Stock Verification Table - High Performance Indexes
            modelBuilder.Entity<StockVerification>()
                .HasIndex(sv => sv.VerificationDate);

            modelBuilder.Entity<StockVerification>()
                .HasIndex(sv => sv.CreatedOn);

            modelBuilder.Entity<StockVerification>()
                .HasIndex(sv => sv.Status);

            modelBuilder.Entity<StockVerification>()
                .HasIndex(sv => sv.BranchId);

            modelBuilder.Entity<StockVerification>()
                .HasIndex(sv => sv.CounterId);

            modelBuilder.Entity<StockVerification>()
                .HasIndex(sv => sv.CategoryId);

            modelBuilder.Entity<StockVerification>()
                .HasIndex(sv => sv.VerifiedBy);

            modelBuilder.Entity<StockVerification>()
                .HasIndex(sv => new { sv.VerificationDate, sv.Status });

            modelBuilder.Entity<StockVerification>()
                .HasIndex(sv => new { sv.BranchId, sv.VerificationDate });

            modelBuilder.Entity<StockVerification>()
                .HasIndex(sv => new { sv.CounterId, sv.VerificationDate });

            modelBuilder.Entity<StockVerification>()
                .HasIndex(sv => new { sv.CategoryId, sv.VerificationDate });

            // Stock Verification Detail Table - High Performance Indexes
            modelBuilder.Entity<StockVerificationDetail>()
                .HasIndex(svd => svd.StockVerificationId);

            modelBuilder.Entity<StockVerificationDetail>()
                .HasIndex(svd => svd.ItemCode);

            modelBuilder.Entity<StockVerificationDetail>()
                .HasIndex(svd => svd.VerificationStatus);

            modelBuilder.Entity<StockVerificationDetail>()
                .HasIndex(svd => svd.ScannedAt);

            modelBuilder.Entity<StockVerificationDetail>()
                .HasIndex(svd => svd.ScannedBy);

            modelBuilder.Entity<StockVerificationDetail>()
                .HasIndex(svd => new { svd.StockVerificationId, svd.VerificationStatus });

            modelBuilder.Entity<StockVerificationDetail>()
                .HasIndex(svd => new { svd.ItemCode, svd.VerificationStatus });

            // Product Custom Fields - High Performance Indexes
            modelBuilder.Entity<ProductCustomField>()
                .HasIndex(pcf => pcf.ProductDetailsId);

            modelBuilder.Entity<ProductCustomField>()
                .HasIndex(pcf => pcf.FieldName);

            modelBuilder.Entity<ProductCustomField>()
                .HasIndex(pcf => pcf.FieldValue);

            modelBuilder.Entity<ProductCustomField>()
                .HasIndex(pcf => new { pcf.ProductDetailsId, pcf.FieldName });

            modelBuilder.Entity<ProductCustomField>()
                .HasIndex(pcf => new { pcf.FieldName, pcf.FieldValue });

            modelBuilder.Entity<ProductCustomField>()
                .HasIndex(pcf => new { pcf.ProductDetailsId, pcf.FieldName, pcf.FieldValue });

            // Customer Table - High Performance Indexes
            modelBuilder.Entity<Customer>()
                .HasIndex(c => new { c.ClientCode, c.Email })
                .IsUnique();

            modelBuilder.Entity<Customer>()
                .HasIndex(c => c.ClientCode);

            modelBuilder.Entity<Customer>()
                .HasIndex(c => c.CustomerName);

            modelBuilder.Entity<Customer>()
                .HasIndex(c => c.MobileNumber);

            modelBuilder.Entity<Customer>()
                .HasIndex(c => new { c.ClientCode, c.CustomerName });

            // Quotation Table - High Performance Indexes
            modelBuilder.Entity<Quotation>()
                .HasIndex(q => q.QuotationNumber)
                .IsUnique();

            modelBuilder.Entity<Quotation>()
                .HasIndex(q => q.CustomerId);

            modelBuilder.Entity<Quotation>()
                .HasIndex(q => q.QuotationDate);

            modelBuilder.Entity<Quotation>()
                .HasIndex(q => q.Status);

            modelBuilder.Entity<Quotation>()
                .HasIndex(q => q.CreatedOn);

            modelBuilder.Entity<Quotation>()
                .HasIndex(q => new { q.ClientCode, q.QuotationDate });

            modelBuilder.Entity<Quotation>()
                .HasIndex(q => new { q.CustomerId, q.Status });

            modelBuilder.Entity<Quotation>()
                .HasIndex(q => new { q.Status, q.QuotationDate });

            // Quotation Item Table - High Performance Indexes
            modelBuilder.Entity<QuotationItem>()
                .HasIndex(qi => qi.QuotationId);

            modelBuilder.Entity<QuotationItem>()
                .HasIndex(qi => qi.ProductId);

            modelBuilder.Entity<QuotationItem>()
                .HasIndex(qi => qi.ItemCode);

            modelBuilder.Entity<QuotationItem>()
                .HasIndex(qi => qi.RfidCode);

            modelBuilder.Entity<QuotationItem>()
                .HasIndex(qi => new { qi.QuotationId, qi.ProductId });
        }
    }
} 