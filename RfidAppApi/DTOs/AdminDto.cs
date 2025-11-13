using System.ComponentModel.DataAnnotations;

namespace RfidAppApi.DTOs
{
    /// <summary>
    /// DTO for admin user management
    /// </summary>
    public class AdminUserDto
    {
        public int UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? FullName { get; set; }
        public string? MobileNumber { get; set; }
        public string? City { get; set; }
        public string? Address { get; set; }
        public string OrganisationName { get; set; } = string.Empty;
        public string? ShowroomType { get; set; }
        public string ClientCode { get; set; } = string.Empty;
        public string? DatabaseName { get; set; }
        public bool IsAdmin { get; set; }
        public int? AdminUserId { get; set; }
        public string UserType { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime CreatedOn { get; set; }
        public DateTime? LastLoginDate { get; set; }
        
        // Branch and Counter information
        public int? BranchId { get; set; }
        public string? BranchName { get; set; }
        public int? CounterId { get; set; }
        public string? CounterName { get; set; }
        
        public List<UserPermissionDto> Permissions { get; set; } = new List<UserPermissionDto>();
    }

    /// <summary>
    /// DTO for creating a new user by admin
    /// </summary>
    public class CreateUserByAdminDto
    {
        [Required]
        [StringLength(100)]
        public string UserName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [StringLength(100)]
        public string Email { get; set; } = string.Empty;

        [Required]
        [StringLength(100, MinimumLength = 6)]
        public string Password { get; set; } = string.Empty;

        [StringLength(150)]
        public string? FullName { get; set; }

        [StringLength(20)]
        public string? MobileNumber { get; set; }

        [StringLength(100)]
        public string? City { get; set; }

        [StringLength(255)]
        public string? Address { get; set; }

        [StringLength(100)]
        public string? ShowroomType { get; set; }

        public bool IsAdmin { get; set; } = false;

        // Branch and Counter assignment for sub-users
        public int? BranchId { get; set; }
        public int? CounterId { get; set; }

        public List<UserPermissionCreateDto> Permissions { get; set; } = new List<UserPermissionCreateDto>();
    }

    /// <summary>
    /// DTO for updating user by admin
    /// </summary>
    public class UpdateUserByAdminDto
    {
        [StringLength(150)]
        public string? FullName { get; set; }

        [StringLength(20)]
        public string? MobileNumber { get; set; }

        [StringLength(100)]
        public string? City { get; set; }

        [StringLength(255)]
        public string? Address { get; set; }

        [StringLength(100)]
        public string? ShowroomType { get; set; }

        public bool? IsActive { get; set; }
        public bool? IsAdmin { get; set; }
        
        // Branch and Counter assignment for sub-users
        public int? BranchId { get; set; }
        public int? CounterId { get; set; }
        
        public List<UserPermissionCreateDto>? Permissions { get; set; }
    }

    /// <summary>
    /// DTO for user permissions
    /// </summary>
    public class UserPermissionDto
    {
        public int UserPermissionId { get; set; }
        public int UserId { get; set; }
        public string ClientCode { get; set; } = string.Empty;
        public string Module { get; set; } = string.Empty;
        public bool CanView { get; set; }
        public bool CanCreate { get; set; }
        public bool CanEdit { get; set; }
        public bool CanDelete { get; set; }
        public bool CanExport { get; set; }
        public bool CanImport { get; set; }
        public DateTime CreatedOn { get; set; }
        public int CreatedBy { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string UserEmail { get; set; } = string.Empty;
    }

    /// <summary>
    /// DTO for creating user permissions
    /// </summary>
    public class UserPermissionCreateDto
    {
        [Required]
        public string Module { get; set; } = string.Empty;
        public bool CanView { get; set; } = false;
        public bool CanCreate { get; set; } = false;
        public bool CanEdit { get; set; } = false;
        public bool CanDelete { get; set; } = false;
        public bool CanExport { get; set; } = false;
        public bool CanImport { get; set; } = false;
    }

    /// <summary>
    /// DTO for user activity tracking
    /// </summary>
    public class UserActivityDto
    {
        public int ActivityId { get; set; }
        public int UserId { get; set; }
        public string ClientCode { get; set; } = string.Empty;
        public string ActivityType { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? TableName { get; set; }
        public int? RecordId { get; set; }
        public string? OldValues { get; set; }
        public string? NewValues { get; set; }
        public string? IpAddress { get; set; }
        public string? UserAgent { get; set; }
        public DateTime CreatedOn { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string UserEmail { get; set; } = string.Empty;
    }

    /// <summary>
    /// DTO for admin dashboard statistics
    /// </summary>
    public class AdminDashboardDto
    {
        public int TotalUsers { get; set; }
        public int ActiveUsers { get; set; }
        public int TotalAdmins { get; set; }
        public int TotalProducts { get; set; }
        public int TotalRFIDs { get; set; }
        public int TotalInvoices { get; set; }
        public int TodayActivities { get; set; }
        public List<UserActivityDto> RecentActivities { get; set; } = new List<UserActivityDto>();
        public List<AdminUserDto> RecentUsers { get; set; } = new List<AdminUserDto>();
    }

    /// <summary>
    /// DTO for activity filter
    /// </summary>
    public class ActivityFilterDto
    {
        public int? UserId { get; set; }
        public string? ActivityType { get; set; }
        public string? Action { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }

    /// <summary>
    /// DTO for bulk user permission update
    /// </summary>
    public class BulkPermissionUpdateDto
    {
        public List<int> UserIds { get; set; } = new List<int>();
        public List<UserPermissionCreateDto> Permissions { get; set; } = new List<UserPermissionCreateDto>();
    }

    /// <summary>
    /// DTO for bulk user permission removal
    /// </summary>
    public class BulkPermissionRemoveDto
    {
        public List<int> UserIds { get; set; } = new List<int>();
        public List<string> Modules { get; set; } = new List<string>();
        public bool RemoveAll { get; set; } = false;
    }

    /// <summary>
    /// DTO for user permission summary
    /// </summary>
    public class UserPermissionSummaryDto
    {
        public int UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string UserEmail { get; set; } = string.Empty;
        public int TotalPermissions { get; set; }
        public int ActivePermissions { get; set; }
        public List<ModulePermissionSummary> ModuleSummaries { get; set; } = new List<ModulePermissionSummary>();
    }

    /// <summary>
    /// DTO for module permission summary
    /// </summary>
    public class ModulePermissionSummary
    {
        public string Module { get; set; } = string.Empty;
        public bool CanView { get; set; }
        public bool CanCreate { get; set; }
        public bool CanEdit { get; set; }
        public bool CanDelete { get; set; }
        public bool CanExport { get; set; }
        public bool CanImport { get; set; }
        public int PermissionCount { get; set; }
    }

    /// <summary>
    /// DTO for activity summary
    /// </summary>
    public class ActivitySummaryDto
    {
        public int TotalActivities { get; set; }
        public List<ModuleActivitySummary> ActivitiesByModule { get; set; } = new List<ModuleActivitySummary>();
        public List<UserActivitySummary> ActivitiesByUser { get; set; } = new List<UserActivitySummary>();
        public List<RecentActivity> RecentActivities { get; set; } = new List<RecentActivity>();
    }

    /// <summary>
    /// DTO for module activity summary
    /// </summary>
    public class ModuleActivitySummary
    {
        public string Module { get; set; } = string.Empty;
        public int Count { get; set; }
        public DateTime LastActivity { get; set; }
    }

    /// <summary>
    /// DTO for user activity summary
    /// </summary>
    public class UserActivitySummary
    {
        public int UserId { get; set; }
        public int Count { get; set; }
        public DateTime LastActivity { get; set; }
    }

    /// <summary>
    /// DTO for recent activity
    /// </summary>
    public class RecentActivity
    {
        public int UserId { get; set; }
        public string ActivityType { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime CreatedOn { get; set; }
    }

    /// <summary>
    /// DTO for user hierarchy
    /// </summary>
    public class UserHierarchyDto
    {
        public int AdminUserId { get; set; }
        public string AdminUserName { get; set; } = string.Empty;
        public List<UserHierarchyItem> Users { get; set; } = new List<UserHierarchyItem>();
    }

    /// <summary>
    /// DTO for user hierarchy item
    /// </summary>
    public class UserHierarchyItem
    {
        public int UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string UserType { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime CreatedOn { get; set; }
    }

}
