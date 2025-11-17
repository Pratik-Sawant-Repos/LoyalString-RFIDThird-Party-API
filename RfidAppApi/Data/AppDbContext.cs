using Microsoft.EntityFrameworkCore;
using RfidAppApi.Models;

namespace RfidAppApi.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        // Master Database - User Management and Activity Tracking Tables
        public DbSet<User> Users { get; set; }
        public DbSet<UserProfile> UserProfiles { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<UserRole> UserRoles { get; set; }
        public DbSet<Permission> Permissions { get; set; }
        public DbSet<UserActivity> UserActivities { get; set; }
        public DbSet<UserPermission> UserPermissions { get; set; }
        
        // Webhook Management
        public DbSet<WebhookSubscription> WebhookSubscriptions { get; set; }
        public DbSet<WebhookEvent> WebhookEvents { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure table names to match the original design
            modelBuilder.Entity<User>().ToTable("tblUser");
            modelBuilder.Entity<UserProfile>().ToTable("tblUserProfile");
            modelBuilder.Entity<Role>().ToTable("tblRole");
            modelBuilder.Entity<UserRole>().ToTable("tblUserRole");
            modelBuilder.Entity<Permission>().ToTable("tblPermission");
            modelBuilder.Entity<UserActivity>().ToTable("tblUserActivity");
            modelBuilder.Entity<UserPermission>().ToTable("tblUserPermission");
            modelBuilder.Entity<WebhookSubscription>().ToTable("tblWebhookSubscription");
            modelBuilder.Entity<WebhookEvent>().ToTable("tblWebhookEvent");

            // Configure relationships
            modelBuilder.Entity<UserRole>()
                .HasOne(ur => ur.User)
                .WithMany()
                .HasForeignKey(ur => ur.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<UserRole>()
                .HasOne(ur => ur.Role)
                .WithMany()
                .HasForeignKey(ur => ur.RoleId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Permission>()
                .HasOne(p => p.Role)
                .WithMany()
                .HasForeignKey(p => p.RoleId)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure unique constraints
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();

            // Note: ClientCode is not unique as multiple users can belong to the same organization
            modelBuilder.Entity<User>()
                .HasIndex(u => u.ClientCode);

            modelBuilder.Entity<Role>()
                .HasIndex(r => r.RoleName)
                .IsUnique();

            // Configure User hierarchy relationships
            modelBuilder.Entity<User>()
                .HasOne(u => u.AdminUser)
                .WithMany()
                .HasForeignKey(u => u.AdminUserId)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure indexes for Branch and Counter
            modelBuilder.Entity<User>()
                .HasIndex(u => u.BranchId);

            modelBuilder.Entity<User>()
                .HasIndex(u => u.CounterId);

            // Configure UserActivity relationships
            modelBuilder.Entity<UserActivity>()
                .HasOne(ua => ua.User)
                .WithMany()
                .HasForeignKey(ua => ua.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure UserPermission relationships
            modelBuilder.Entity<UserPermission>()
                .HasOne(up => up.User)
                .WithMany()
                .HasForeignKey(up => up.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<UserPermission>()
                .HasOne(up => up.CreatedByUser)
                .WithMany()
                .HasForeignKey(up => up.CreatedBy)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure indexes for better performance
            modelBuilder.Entity<UserActivity>()
                .HasIndex(ua => new { ua.UserId, ua.CreatedOn });

            modelBuilder.Entity<UserActivity>()
                .HasIndex(ua => ua.ActivityType);

            modelBuilder.Entity<UserPermission>()
                .HasIndex(up => new { up.UserId, up.Module })
                .IsUnique();

            // Configure UserProfile relationships
            modelBuilder.Entity<UserProfile>()
                .HasOne(up => up.User)
                .WithMany()
                .HasForeignKey(up => up.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure unique constraint - one profile per user
            modelBuilder.Entity<UserProfile>()
                .HasIndex(up => up.UserId)
                .IsUnique();

            // Configure Webhook indexes
            modelBuilder.Entity<WebhookSubscription>()
                .HasIndex(w => new { w.ClientCode, w.EventType });

            modelBuilder.Entity<WebhookSubscription>()
                .HasIndex(w => w.ClientCode);

            modelBuilder.Entity<WebhookEvent>()
                .HasIndex(w => new { w.ClientCode, w.Status });

            modelBuilder.Entity<WebhookEvent>()
                .HasIndex(w => new { w.ClientCode, w.EventType, w.CreatedOn });

            modelBuilder.Entity<WebhookEvent>()
                .HasIndex(w => w.NextRetryAt);
        }
    }
} 