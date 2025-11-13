using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RfidAppApi.DTOs;
using RfidAppApi.Services;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace RfidAppApi.Controllers
{
    /// <summary>
    /// Admin controller for user management and activity tracking
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class AdminController : ControllerBase
    {
        private readonly IAdminService _adminService;
        private readonly IUserService _userService;

        public AdminController(IAdminService adminService, IUserService userService)
        {
            _adminService = adminService;
            _userService = userService;
        }

        #region User Management

        /// <summary>
        /// Register a new admin user (Main Admin only)
        /// </summary>
        /// <param name="createUserDto">Admin user creation details</param>
        /// <returns>Created admin user information</returns>
        [HttpPost("register-admin")]
        [ProducesResponseType(typeof(AdminUserDto), 201)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<AdminUserDto>> RegisterAdmin([FromBody] CreateUserByAdminDto createUserDto)
        {
            try
            {
                // Validate model state
                if (!ModelState.IsValid)
                {
                    return BadRequest(new { success = false, message = "Invalid input data.", errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage) });
                }

                // Validate admin user ID
                var adminUserId = GetCurrentUserId();
                if (adminUserId <= 0)
                {
                    return Unauthorized(new { success = false, message = "Invalid user authentication." });
                }

                // Check if user is MainAdmin
                if (!await IsMainAdminAsync(adminUserId))
                {
                    return StatusCode(403, new { success = false, message = "Only main admins can register admin users." });
                }

                var result = await _adminService.CreateUserAsync(createUserDto, adminUserId);
                return CreatedAtAction(nameof(GetUser), new { userId = result.UserId }, new { success = true, message = "Admin user created successfully.", data = result });
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new { success = false, message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "An error occurred while creating the admin user.", error = ex.Message });
            }
        }

        /// <summary>
        /// Register a new sub-user under admin
        /// </summary>
        /// <param name="createUserDto">Sub-user creation details</param>
        /// <returns>Created sub-user information</returns>
        [HttpPost("register-sub-user")]
        [ProducesResponseType(typeof(AdminUserDto), 201)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<AdminUserDto>> RegisterSubUser([FromBody] CreateUserByAdminDto createUserDto)
        {
            try
            {
                // Validate model state
                if (!ModelState.IsValid)
                {
                    return BadRequest(new { success = false, message = "Invalid input data.", errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage) });
                }

                // Validate admin user ID
                var adminUserId = GetCurrentUserId();
                if (adminUserId <= 0)
                {
                    return Unauthorized(new { success = false, message = "Invalid user authentication." });
                }

                // Check if user is Admin
                if (!await IsAdminAsync(adminUserId))
                {
                    return StatusCode(403, new { success = false, message = "Only admins can register sub-users." });
                }

                var result = await _adminService.CreateUserAsync(createUserDto, adminUserId);
                return CreatedAtAction(nameof(GetUser), new { userId = result.UserId }, new { success = true, message = "Sub-user created successfully.", data = result });
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new { success = false, message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "An error occurred while creating the sub-user.", error = ex.Message });
            }
        }

        /// <summary>
        /// Get user by ID
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <returns>User information</returns>
        [HttpGet("users/{userId}")]
        [ProducesResponseType(typeof(AdminUserDto), 200)]
        [ProducesResponseType(404)]
        [ProducesResponseType(401)]
        public async Task<ActionResult<AdminUserDto>> GetUser(int userId)
        {
            try
            {
                // Validate parameters
                if (userId <= 0)
                {
                    return BadRequest(new { success = false, message = "Invalid user ID." });
                }

                // Validate admin user ID
                var adminUserId = GetCurrentUserId();
                if (adminUserId <= 0)
                {
                    return Unauthorized(new { success = false, message = "Invalid user authentication." });
                }

                var user = await _adminService.GetUserByIdAsync(userId, adminUserId);
                
                if (user == null)
                {
                    return NotFound(new { success = false, message = "User not found or access denied." });
                }

                return Ok(new { success = true, message = "User retrieved successfully.", data = user });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "An error occurred while retrieving the user.", error = ex.Message });
            }
        }

        /// <summary>
        /// Get all users managed by the current admin
        /// </summary>
        /// <returns>List of users</returns>
        [HttpGet("users-under-admin")]
        [ProducesResponseType(typeof(IEnumerable<AdminUserDto>), 200)]
        [ProducesResponseType(401)]
        public async Task<ActionResult<IEnumerable<AdminUserDto>>> GetUsersUnderAdmin(
            [FromQuery] int page = 1, 
            [FromQuery] int pageSize = 10, 
            [FromQuery] string? searchTerm = null,
            [FromQuery] string? sortBy = null,
            [FromQuery] string? sortOrder = null)
        {
            try
            {
                // Validate parameters
                if (page < 1)
                {
                    return BadRequest(new { success = false, message = "Page number must be greater than 0." });
                }

                if (pageSize < 1 || pageSize > 100)
                {
                    return BadRequest(new { success = false, message = "Page size must be between 1 and 100." });
                }

                // Validate admin user ID
                var adminUserId = GetCurrentUserId();
                if (adminUserId <= 0)
                {
                    return Unauthorized(new { success = false, message = "Invalid user authentication." });
                }

                var users = await _adminService.GetUsersByAdminAsync(adminUserId);
                return Ok(new { success = true, message = "Users retrieved successfully.", data = users, count = users.Count() });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "An error occurred while retrieving users.", error = ex.Message });
            }
        }

        /// <summary>
        /// Update user information (Admin only)
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="updateUserDto">Update information</param>
        /// <returns>Updated user information</returns>
        [HttpPut("users/{userId}")]
        [ProducesResponseType(typeof(AdminUserDto), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<AdminUserDto>> UpdateUser(int userId, [FromBody] UpdateUserByAdminDto updateUserDto)
        {
            try
            {
                // Validate model state
                if (!ModelState.IsValid)
                {
                    return BadRequest(new { success = false, message = "Invalid input data.", errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage) });
                }

                // Validate parameters
                if (userId <= 0)
                {
                    return BadRequest(new { success = false, message = "Invalid user ID." });
                }

                // Validate admin user ID
                var adminUserId = GetCurrentUserId();
                if (adminUserId <= 0)
                {
                    return Unauthorized(new { success = false, message = "Invalid user authentication." });
                }

                // Check if user is Admin
                if (!await IsAdminAsync(adminUserId))
                {
                    return StatusCode(403, new { success = false, message = "Only admins can update users." });
                }

                var result = await _adminService.UpdateUserAsync(userId, updateUserDto, adminUserId);
                
                if (result == null)
                {
                    return NotFound(new { success = false, message = "User not found or access denied." });
                }

                return Ok(new { success = true, message = "User updated successfully.", data = result });
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new { success = false, message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "An error occurred while updating the user.", error = ex.Message });
            }
        }

        /// <summary>
        /// Delete user (Admin only)
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <returns>Success status</returns>
        [HttpDelete("users/{userId}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(404)]
        public async Task<ActionResult> DeleteUser(int userId)
        {
            try
            {
                // Validate parameters
                if (userId <= 0)
                {
                    return BadRequest(new { success = false, message = "Invalid user ID." });
                }

                // Validate admin user ID
                var adminUserId = GetCurrentUserId();
                if (adminUserId <= 0)
                {
                    return Unauthorized(new { success = false, message = "Invalid user authentication." });
                }

                // Check if user is Admin
                if (!await IsAdminAsync(adminUserId))
                {
                    return StatusCode(403, new { success = false, message = "Only admins can delete users." });
                }

                // Prevent self-deletion
                if (userId == adminUserId)
                {
                    return BadRequest(new { success = false, message = "You cannot delete your own account." });
                }

                var result = await _adminService.DeleteUserAsync(userId, adminUserId);
                
                if (!result)
                {
                    return NotFound(new { success = false, message = "User not found or access denied." });
                }

                return Ok(new { success = true, message = "User deleted successfully." });
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "An error occurred while deleting the user.", error = ex.Message });
            }
        }

        /// <summary>
        /// Activate user (Admin only)
        /// </summary>
        /// <param name="activateUserDto">Activation details</param>
        /// <returns>Success status</returns>
        [HttpPut("activate-user")]
        [ProducesResponseType(200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(404)]
        public async Task<ActionResult> ActivateUser([FromBody] ActivateUserDto activateUserDto)
        {
            try
            {
                // Validate model state
                if (!ModelState.IsValid)
                {
                    return BadRequest(new { success = false, message = "Invalid input data.", errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage) });
                }

                // Validate parameters
                if (activateUserDto.UserId <= 0)
                {
                    return BadRequest(new { success = false, message = "Invalid user ID." });
                }

                // Validate admin user ID
                var adminUserId = GetCurrentUserId();
                if (adminUserId <= 0)
                {
                    return Unauthorized(new { success = false, message = "Invalid user authentication." });
                }

                // Check if user is Admin
                if (!await IsAdminAsync(adminUserId))
                {
                    return StatusCode(403, new { success = false, message = "Only admins can activate users." });
                }

                var result = await _adminService.ActivateUserAsync(activateUserDto.UserId, adminUserId);
                
                if (!result)
                {
                    return NotFound(new { success = false, message = "User not found or access denied." });
                }

                return Ok(new { success = true, message = "User activated successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "An error occurred while activating the user.", error = ex.Message });
            }
        }

        /// <summary>
        /// Deactivate user (Admin only)
        /// </summary>
        /// <param name="deactivateUserDto">Deactivation details</param>
        /// <returns>Success status</returns>
        [HttpPut("deactivate-user")]
        [ProducesResponseType(200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(404)]
        public async Task<ActionResult> DeactivateUser([FromBody] DeactivateUserDto deactivateUserDto)
        {
            try
            {
                // Validate model state
                if (!ModelState.IsValid)
                {
                    return BadRequest(new { success = false, message = "Invalid input data.", errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage) });
                }

                // Validate parameters
                if (deactivateUserDto.UserId <= 0)
                {
                    return BadRequest(new { success = false, message = "Invalid user ID." });
                }

                // Validate admin user ID
                var adminUserId = GetCurrentUserId();
                if (adminUserId <= 0)
                {
                    return Unauthorized(new { success = false, message = "Invalid user authentication." });
                }

                // Check if user is Admin
                if (!await IsAdminAsync(adminUserId))
                {
                    return StatusCode(403, new { success = false, message = "Only admins can deactivate users." });
                }

                // Prevent self-deactivation
                if (deactivateUserDto.UserId == adminUserId)
                {
                    return BadRequest(new { success = false, message = "You cannot deactivate your own account." });
                }

                var result = await _adminService.DeactivateUserAsync(deactivateUserDto.UserId, adminUserId);
                
                if (!result)
                {
                    return NotFound(new { success = false, message = "User not found or access denied." });
                }

                return Ok(new { success = true, message = "User deactivated successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "An error occurred while deactivating the user.", error = ex.Message });
            }
        }

        /// <summary>
        /// Reset user password (Admin only)
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="resetPasswordDto">New password</param>
        /// <returns>Success status</returns>
        [HttpPost("users/{userId}/reset-password")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(404)]
        public async Task<ActionResult> ResetUserPassword(int userId, [FromBody] AdminResetPasswordDto resetPasswordDto)
        {
            try
            {
                // Validate model state
                if (!ModelState.IsValid)
                {
                    return BadRequest(new { success = false, message = "Invalid input data.", errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage) });
                }

                // Validate parameters
                if (userId <= 0)
                {
                    return BadRequest(new { success = false, message = "Invalid user ID." });
                }

                if (string.IsNullOrWhiteSpace(resetPasswordDto?.NewPassword) || resetPasswordDto.NewPassword.Length < 6)
                {
                    return BadRequest(new { success = false, message = "Password must be at least 6 characters long." });
                }

                // Validate admin user ID
                var adminUserId = GetCurrentUserId();
                if (adminUserId <= 0)
                {
                    return Unauthorized(new { success = false, message = "Invalid user authentication." });
                }

                // Check if user is Admin
                if (!await IsAdminAsync(adminUserId))
                {
                    return StatusCode(403, new { success = false, message = "Only admins can reset passwords." });
                }

                var result = await _adminService.ResetUserPasswordAsync(userId, resetPasswordDto.NewPassword, adminUserId);
                
                if (!result)
                {
                    return NotFound(new { success = false, message = "User not found or access denied." });
                }

                return Ok(new { success = true, message = "Password reset successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "An error occurred while resetting the password.", error = ex.Message });
            }
        }

        #endregion

        #region Permission Management

        /// <summary>
        /// Get user permissions by user ID
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <returns>User permissions</returns>
        [HttpGet("users/{userId}/permissions")]
        [ProducesResponseType(typeof(IEnumerable<UserPermissionDto>), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<IEnumerable<UserPermissionDto>>> GetUserPermissions(int userId)
        {
            try
            {
                // Validate parameters
                if (userId <= 0)
                {
                    return BadRequest(new { success = false, message = "Invalid user ID." });
                }

                // Validate admin user ID
                var adminUserId = GetCurrentUserId();
                if (adminUserId <= 0)
                {
                    return Unauthorized(new { success = false, message = "Invalid user authentication." });
                }

                if (!await _adminService.CanUserAccessUserAsync(adminUserId, userId))
                {
                    return StatusCode(403, new { success = false, message = "Access denied." });
                }

                var permissions = await _adminService.GetUserPermissionsAsync(userId);
                return Ok(new { success = true, message = "Permissions retrieved successfully.", data = permissions });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "An error occurred while retrieving permissions.", error = ex.Message });
            }
        }

        /// <summary>
        /// Get all user permissions in organization
        /// </summary>
        /// <returns>All user permissions</returns>
        [HttpGet("permissions")]
        [ProducesResponseType(typeof(IEnumerable<UserPermissionDto>), 200)]
        [ProducesResponseType(401)]
        public async Task<ActionResult<IEnumerable<UserPermissionDto>>> GetAllUserPermissions()
        {
            try
            {
                // Validate admin user ID
                var adminUserId = GetCurrentUserId();
                if (adminUserId <= 0)
                {
                    return Unauthorized(new { success = false, message = "Invalid user authentication." });
                }

                if (!await IsAdminAsync(adminUserId))
                {
                    return StatusCode(403, new { success = false, message = "Only admins can view all permissions." });
                }

                var clientCode = GetClientCodeFromToken();
                if (string.IsNullOrEmpty(clientCode))
                {
                    return BadRequest(new { success = false, message = "Client code not found in token." });
                }

                var permissions = await _adminService.GetAllUserPermissionsAsync(clientCode);
                return Ok(new { success = true, message = "Permissions retrieved successfully.", data = permissions });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "An error occurred while retrieving permissions.", error = ex.Message });
            }
        }

        /// <summary>
        /// Create/Assign permissions to user
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="permissions">Permission details</param>
        /// <returns>Success status</returns>
        [HttpPost("users/{userId}/permissions")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(404)]
        public async Task<ActionResult> CreateUserPermissions(int userId, [FromBody] List<UserPermissionCreateDto> permissions)
        {
            try
            {
                // Validate model state
                if (!ModelState.IsValid)
                {
                    return BadRequest(new { success = false, message = "Invalid input data.", errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage) });
                }

                // Validate parameters
                if (userId <= 0)
                {
                    return BadRequest(new { success = false, message = "Invalid user ID." });
                }

                if (permissions == null || permissions.Count == 0)
                {
                    return BadRequest(new { success = false, message = "Permissions list cannot be empty." });
                }

                // Validate admin user ID
                var adminUserId = GetCurrentUserId();
                if (adminUserId <= 0)
                {
                    return Unauthorized(new { success = false, message = "Invalid user authentication." });
                }

                if (!await IsAdminAsync(adminUserId))
                {
                    return StatusCode(403, new { success = false, message = "Only admins can assign permissions." });
                }

                if (!await _adminService.CanUserAccessUserAsync(adminUserId, userId))
                {
                    return StatusCode(403, new { success = false, message = "Access denied to this user." });
                }

                var result = await _adminService.UpdateUserPermissionsAsync(userId, permissions, adminUserId);
                
                if (!result)
                {
                    return BadRequest(new { success = false, message = "Failed to assign permissions." });
                }

                return Ok(new { success = true, message = "Permissions assigned successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "An error occurred while assigning permissions.", error = ex.Message });
            }
        }

        /// <summary>
        /// Update user permissions
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="permissions">Updated permission details</param>
        /// <returns>Success status</returns>
        [HttpPut("users/{userId}/permissions")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(404)]
        public async Task<ActionResult> UpdateUserPermissions(int userId, [FromBody] List<UserPermissionCreateDto> permissions)
        {
            try
            {
                // Validate model state
                if (!ModelState.IsValid)
                {
                    return BadRequest(new { success = false, message = "Invalid input data.", errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage) });
                }

                // Validate parameters
                if (userId <= 0)
                {
                    return BadRequest(new { success = false, message = "Invalid user ID." });
                }

                if (permissions == null || permissions.Count == 0)
                {
                    return BadRequest(new { success = false, message = "Permissions list cannot be empty." });
                }

                // Validate admin user ID
                var adminUserId = GetCurrentUserId();
                if (adminUserId <= 0)
                {
                    return Unauthorized(new { success = false, message = "Invalid user authentication." });
                }

                if (!await IsAdminAsync(adminUserId))
                {
                    return StatusCode(403, new { success = false, message = "Only admins can update permissions." });
                }

                if (!await _adminService.CanUserAccessUserAsync(adminUserId, userId))
                {
                    return StatusCode(403, new { success = false, message = "Access denied to this user." });
                }

                var result = await _adminService.UpdateUserPermissionsAsync(userId, permissions, adminUserId);
                
                if (!result)
                {
                    return BadRequest(new { success = false, message = "Failed to update permissions." });
                }

                return Ok(new { success = true, message = "Permissions updated successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "An error occurred while updating permissions.", error = ex.Message });
            }
        }

        /// <summary>
        /// Remove all permissions from user
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <returns>Success status</returns>
        [HttpDelete("users/{userId}/permissions")]
        [ProducesResponseType(200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(404)]
        public async Task<ActionResult> RemoveUserPermissions(int userId)
        {
            try
            {
                // Validate parameters
                if (userId <= 0)
                {
                    return BadRequest(new { success = false, message = "Invalid user ID." });
                }

                // Validate admin user ID
                var adminUserId = GetCurrentUserId();
                if (adminUserId <= 0)
                {
                    return Unauthorized(new { success = false, message = "Invalid user authentication." });
                }

                if (!await IsAdminAsync(adminUserId))
                {
                    return StatusCode(403, new { success = false, message = "Only admins can remove permissions." });
                }

                if (!await _adminService.CanUserAccessUserAsync(adminUserId, userId))
                {
                    return StatusCode(403, new { success = false, message = "Access denied to this user." });
                }

                var result = await _adminService.RemoveUserPermissionsAsync(userId, adminUserId);
                
                if (!result)
                {
                    return BadRequest(new { success = false, message = "Failed to remove permissions." });
                }

                return Ok(new { success = true, message = "All permissions removed successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "An error occurred while removing permissions.", error = ex.Message });
            }
        }

        /// <summary>
        /// Remove specific permission from user
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="module">Module name</param>
        /// <returns>Success status</returns>
        [HttpDelete("users/{userId}/permissions/{module}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(404)]
        public async Task<ActionResult> RemoveUserPermission(int userId, string module)
        {
            try
            {
                // Validate parameters
                if (userId <= 0)
                {
                    return BadRequest(new { success = false, message = "Invalid user ID." });
                }

                if (string.IsNullOrWhiteSpace(module))
                {
                    return BadRequest(new { success = false, message = "Module name is required." });
                }

                // Validate admin user ID
                var adminUserId = GetCurrentUserId();
                if (adminUserId <= 0)
                {
                    return Unauthorized(new { success = false, message = "Invalid user authentication." });
                }

                if (!await IsAdminAsync(adminUserId))
                {
                    return StatusCode(403, new { success = false, message = "Only admins can remove permissions." });
                }

                if (!await _adminService.CanUserAccessUserAsync(adminUserId, userId))
                {
                    return StatusCode(403, new { success = false, message = "Access denied to this user." });
                }

                var result = await _adminService.RemoveUserPermissionAsync(userId, module, adminUserId);
                
                if (!result)
                {
                    return BadRequest(new { success = false, message = "Failed to remove permission." });
                }

                return Ok(new { success = true, message = $"Permission for module '{module}' removed successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "An error occurred while removing permission.", error = ex.Message });
            }
        }

        /// <summary>
        /// Bulk update permissions for multiple users
        /// </summary>
        /// <param name="bulkUpdate">Bulk permission update details</param>
        /// <returns>Success status</returns>
        [HttpPost("permissions/bulk-update")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        public async Task<ActionResult> BulkUpdatePermissions([FromBody] BulkPermissionUpdateDto bulkUpdate)
        {
            try
            {
                // Validate model state
                if (!ModelState.IsValid)
                {
                    return BadRequest(new { success = false, message = "Invalid input data.", errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage) });
                }

                // Validate parameters
                if (bulkUpdate == null)
                {
                    return BadRequest(new { success = false, message = "Bulk update data is required." });
                }

                if (bulkUpdate.UserIds == null || bulkUpdate.UserIds.Count == 0)
                {
                    return BadRequest(new { success = false, message = "User IDs list cannot be empty." });
                }

                if (bulkUpdate.Permissions == null || bulkUpdate.Permissions.Count == 0)
                {
                    return BadRequest(new { success = false, message = "Permissions list cannot be empty." });
                }

                // Validate admin user ID
                var adminUserId = GetCurrentUserId();
                if (adminUserId <= 0)
                {
                    return Unauthorized(new { success = false, message = "Invalid user authentication." });
                }

                if (!await IsAdminAsync(adminUserId))
                {
                    return StatusCode(403, new { success = false, message = "Only admins can update permissions." });
                }

                var result = await _adminService.BulkUpdatePermissionsAsync(bulkUpdate, adminUserId);
                
                if (!result)
                {
                    return BadRequest(new { success = false, message = "Failed to update permissions." });
                }

                return Ok(new { success = true, message = "Permissions updated successfully for all users." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "An error occurred while updating permissions.", error = ex.Message });
            }
        }

        /// <summary>
        /// Bulk remove permissions for multiple users
        /// </summary>
        /// <param name="bulkRemove">Bulk permission removal details</param>
        /// <returns>Success status</returns>
        [HttpPost("permissions/bulk-remove")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        public async Task<ActionResult> BulkRemovePermissions([FromBody] BulkPermissionRemoveDto bulkRemove)
        {
            try
            {
                // Validate model state
                if (!ModelState.IsValid)
                {
                    return BadRequest(new { success = false, message = "Invalid input data.", errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage) });
                }

                // Validate parameters
                if (bulkRemove == null)
                {
                    return BadRequest(new { success = false, message = "Bulk remove data is required." });
                }

                if (bulkRemove.UserIds == null || bulkRemove.UserIds.Count == 0)
                {
                    return BadRequest(new { success = false, message = "User IDs list cannot be empty." });
                }

                if (!bulkRemove.RemoveAll && (bulkRemove.Modules == null || bulkRemove.Modules.Count == 0))
                {
                    return BadRequest(new { success = false, message = "Modules list cannot be empty when RemoveAll is false." });
                }

                // Validate admin user ID
                var adminUserId = GetCurrentUserId();
                if (adminUserId <= 0)
                {
                    return Unauthorized(new { success = false, message = "Invalid user authentication." });
                }

                if (!await IsAdminAsync(adminUserId))
                {
                    return StatusCode(403, new { success = false, message = "Only admins can remove permissions." });
                }

                var result = await _adminService.BulkRemovePermissionsAsync(bulkRemove, adminUserId);
                
                if (!result)
                {
                    return BadRequest(new { success = false, message = "Failed to remove permissions." });
                }

                return Ok(new { success = true, message = "Permissions removed successfully for all users." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "An error occurred while removing permissions.", error = ex.Message });
            }
        }

        /// <summary>
        /// Get available permission modules
        /// </summary>
        /// <returns>List of available modules</returns>
        [HttpGet("permissions/modules")]
        [ProducesResponseType(typeof(IEnumerable<string>), 200)]
        [ProducesResponseType(401)]
        public async Task<ActionResult<IEnumerable<string>>> GetAvailableModules()
        {
            try
            {
                // Validate admin user ID
                var adminUserId = GetCurrentUserId();
                if (adminUserId <= 0)
                {
                    return Unauthorized(new { success = false, message = "Invalid user authentication." });
                }

                if (!await IsAdminAsync(adminUserId))
                {
                    return StatusCode(403, new { success = false, message = "Only admins can view available modules." });
                }

                var modules = new[]
                {
                    "Product",
                    "RFID", 
                    "Invoice",
                    "Reports",
                    "StockTransfer",
                    "StockVerification",
                    "ProductImage",
                    "User",
                    "Admin"
                };

                return Ok(new { success = true, message = "Modules retrieved successfully.", data = modules });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "An error occurred while retrieving modules.", error = ex.Message });
            }
        }

        /// <summary>
        /// Get permission summary for user
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <returns>Permission summary</returns>
        [HttpGet("users/{userId}/permissions/summary")]
        [ProducesResponseType(typeof(UserPermissionSummaryDto), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<UserPermissionSummaryDto>> GetUserPermissionSummary(int userId)
        {
            try
            {
                // Validate parameters
                if (userId <= 0)
                {
                    return BadRequest(new { success = false, message = "Invalid user ID." });
                }

                // Validate admin user ID
                var adminUserId = GetCurrentUserId();
                if (adminUserId <= 0)
                {
                    return Unauthorized(new { success = false, message = "Invalid user authentication." });
                }

                if (!await _adminService.CanUserAccessUserAsync(adminUserId, userId))
                {
                    return StatusCode(403, new { success = false, message = "Access denied." });
                }

                var summary = await _adminService.GetUserPermissionSummaryAsync(userId);
                return Ok(new { success = true, message = "Permission summary retrieved successfully.", data = summary });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "An error occurred while retrieving permission summary.", error = ex.Message });
            }
        }

        #endregion

        #region Activity Tracking

        /// <summary>
        /// Get user activities with filters
        /// </summary>
        /// <param name="startDate">Start date for filtering activities</param>
        /// <param name="endDate">End date for filtering activities</param>
        /// <param name="userId">User ID to filter activities (optional)</param>
        /// <param name="activityType">Activity type to filter (optional)</param>
        /// <param name="action">Action name to filter (optional)</param>
        /// <param name="page">Page number for pagination (default: 1)</param>
        /// <param name="pageSize">Page size for pagination (default: 20, max: 100)</param>
        /// <returns>List of user activities</returns>
        [HttpGet("user-activities")]
        [ProducesResponseType(typeof(IEnumerable<UserActivityDto>), 200)]
        [ProducesResponseType(401)]
        public async Task<ActionResult<IEnumerable<UserActivityDto>>> GetUserActivities(
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null,
            [FromQuery] int? userId = null,
            [FromQuery] string? activityType = null,
            [FromQuery] string? action = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            try
            {
                // Validate parameters
                if (page < 1)
                {
                    return BadRequest(new { success = false, message = "Page number must be greater than 0." });
                }

                if (pageSize < 1 || pageSize > 100)
                {
                    return BadRequest(new { success = false, message = "Page size must be between 1 and 100." });
                }

                if (startDate.HasValue && endDate.HasValue && startDate > endDate)
                {
                    return BadRequest(new { success = false, message = "Start date cannot be greater than end date." });
                }

                if (userId.HasValue && userId <= 0)
                {
                    return BadRequest(new { success = false, message = "Invalid user ID." });
                }

                // Validate admin user ID
                var adminUserId = GetCurrentUserId();
                if (adminUserId <= 0)
                {
                    return Unauthorized(new { success = false, message = "Invalid user authentication." });
                }

                var filter = new ActivityFilterDto
                {
                    StartDate = startDate,
                    EndDate = endDate,
                    UserId = userId,
                    ActivityType = activityType,
                    Action = action,
                    Page = page,
                    PageSize = pageSize
                };
                var activities = await _adminService.GetAllActivitiesAsync(filter, adminUserId);
                return Ok(new { success = true, message = "Activities retrieved successfully.", data = activities, count = activities.Count() });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "An error occurred while retrieving activities.", error = ex.Message });
            }
        }

        /// <summary>
        /// Get activities for a specific user
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <returns>List of user activities</returns>
        [HttpGet("user-activities/user/{userId}")]
        [ProducesResponseType(typeof(IEnumerable<UserActivityDto>), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<IEnumerable<UserActivityDto>>> GetUserActivitiesByUser(int userId)
        {
            try
            {
                // Validate parameters
                if (userId <= 0)
                {
                    return BadRequest(new { success = false, message = "Invalid user ID." });
                }

                // Validate admin user ID
                var adminUserId = GetCurrentUserId();
                if (adminUserId <= 0)
                {
                    return Unauthorized(new { success = false, message = "Invalid user authentication." });
                }

                if (!await _adminService.CanUserAccessUserAsync(adminUserId, userId))
                {
                    return StatusCode(403, new { success = false, message = "Access denied." });
                }

                var filter = new ActivityFilterDto { UserId = userId };
                var clientCode = GetClientCodeFromToken();
                if (string.IsNullOrEmpty(clientCode))
                {
                    return BadRequest(new { success = false, message = "Client code not found in token." });
                }

                var activities = await _adminService.GetUserActivitiesAsync(filter, clientCode);
                return Ok(new { success = true, message = "User activities retrieved successfully.", data = activities, count = activities.Count() });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "An error occurred while retrieving user activities.", error = ex.Message });
            }
        }

        /// <summary>
        /// Get user activities by date range
        /// </summary>
        /// <param name="startDate">Start date</param>
        /// <param name="endDate">End date</param>
        /// <returns>List of user activities</returns>
        [HttpGet("user-activities/date-range")]
        [ProducesResponseType(typeof(IEnumerable<UserActivityDto>), 200)]
        [ProducesResponseType(401)]
        public async Task<ActionResult<IEnumerable<UserActivityDto>>> GetUserActivitiesByDateRange(
            [FromQuery] DateTime startDate,
            [FromQuery] DateTime endDate)
        {
            try
            {
                // Validate parameters
                if (startDate > endDate)
                {
                    return BadRequest(new { success = false, message = "Start date cannot be greater than end date." });
                }

                // Validate admin user ID
                var adminUserId = GetCurrentUserId();
                if (adminUserId <= 0)
                {
                    return Unauthorized(new { success = false, message = "Invalid user authentication." });
                }

                var filter = new ActivityFilterDto
                {
                    StartDate = startDate,
                    EndDate = endDate
                };
                var activities = await _adminService.GetAllActivitiesAsync(filter, adminUserId);
                return Ok(new { success = true, message = "Activities retrieved successfully.", data = activities, count = activities.Count() });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "An error occurred while retrieving activities.", error = ex.Message });
            }
        }

        /// <summary>
        /// Get user activities by module
        /// </summary>
        /// <param name="module">Module name</param>
        /// <returns>List of user activities</returns>
        [HttpGet("user-activities/module/{module}")]
        [ProducesResponseType(typeof(IEnumerable<UserActivityDto>), 200)]
        [ProducesResponseType(401)]
        public async Task<ActionResult<IEnumerable<UserActivityDto>>> GetUserActivitiesByModule(string module)
        {
            try
            {
                // Validate parameters
                if (string.IsNullOrWhiteSpace(module))
                {
                    return BadRequest(new { success = false, message = "Module name is required." });
                }

                // Validate admin user ID
                var adminUserId = GetCurrentUserId();
                if (adminUserId <= 0)
                {
                    return Unauthorized(new { success = false, message = "Invalid user authentication." });
                }

                var filter = new ActivityFilterDto { ActivityType = module };
                var activities = await _adminService.GetAllActivitiesAsync(filter, adminUserId);
                return Ok(new { success = true, message = "Activities retrieved successfully.", data = activities, count = activities.Count() });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "An error occurred while retrieving activities.", error = ex.Message });
            }
        }

        /// <summary>
        /// Get user activities by action
        /// </summary>
        /// <param name="action">Action name</param>
        /// <returns>List of user activities</returns>
        [HttpGet("user-activities/action/{action}")]
        [ProducesResponseType(typeof(IEnumerable<UserActivityDto>), 200)]
        [ProducesResponseType(401)]
        public async Task<ActionResult<IEnumerable<UserActivityDto>>> GetUserActivitiesByAction(string action)
        {
            try
            {
                // Validate parameters
                if (string.IsNullOrWhiteSpace(action))
                {
                    return BadRequest(new { success = false, message = "Action name is required." });
                }

                // Validate admin user ID
                var adminUserId = GetCurrentUserId();
                if (adminUserId <= 0)
                {
                    return Unauthorized(new { success = false, message = "Invalid user authentication." });
                }

                var filter = new ActivityFilterDto { Action = action };
                var activities = await _adminService.GetAllActivitiesAsync(filter, adminUserId);
                return Ok(new { success = true, message = "Activities retrieved successfully.", data = activities, count = activities.Count() });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "An error occurred while retrieving activities.", error = ex.Message });
            }
        }

        /// <summary>
        /// Get activities for a specific user
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="filter">Activity filter parameters</param>
        /// <returns>List of user activities</returns>
        [HttpGet("users/{userId}/activities")]
        [ProducesResponseType(typeof(IEnumerable<UserActivityDto>), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<IEnumerable<UserActivityDto>>> GetUserActivities(int userId, [FromQuery] ActivityFilterDto filter)
        {
            try
            {
                // Validate parameters
                if (userId <= 0)
                {
                    return BadRequest(new { success = false, message = "Invalid user ID." });
                }

                if (filter == null)
                {
                    filter = new ActivityFilterDto();
                }

                // Validate date range if provided
                if (filter.StartDate.HasValue && filter.EndDate.HasValue && filter.StartDate > filter.EndDate)
                {
                    return BadRequest(new { success = false, message = "Start date cannot be greater than end date." });
                }

                // Validate pagination
                if (filter.Page < 1)
                {
                    filter.Page = 1;
                }

                if (filter.PageSize < 1 || filter.PageSize > 100)
                {
                    filter.PageSize = 20;
                }

                // Validate admin user ID
                var adminUserId = GetCurrentUserId();
                if (adminUserId <= 0)
                {
                    return Unauthorized(new { success = false, message = "Invalid user authentication." });
                }

                if (!await _adminService.CanUserAccessUserAsync(adminUserId, userId))
                {
                    return StatusCode(403, new { success = false, message = "Access denied." });
                }

                filter.UserId = userId;
                var clientCode = GetClientCodeFromToken();
                if (string.IsNullOrEmpty(clientCode))
                {
                    return BadRequest(new { success = false, message = "Client code not found in token." });
                }

                var activities = await _adminService.GetUserActivitiesAsync(filter, clientCode);
                return Ok(new { success = true, message = "User activities retrieved successfully.", data = activities, count = activities.Count() });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "An error occurred while retrieving user activities.", error = ex.Message });
            }
        }

        #endregion

        #region Activity Analytics

        /// <summary>
        /// Get activity summary
        /// </summary>
        /// <returns>Activity summary</returns>
        [HttpGet("activity-summary")]
        [ProducesResponseType(typeof(ActivitySummaryDto), 200)]
        [ProducesResponseType(401)]
        public async Task<ActionResult<ActivitySummaryDto>> GetActivitySummary()
        {
            try
            {
                // Validate admin user ID
                var adminUserId = GetCurrentUserId();
                if (adminUserId <= 0)
                {
                    return Unauthorized(new { success = false, message = "Invalid user authentication." });
                }

                var summary = await _adminService.GetActivitySummaryAsync(adminUserId);
                return Ok(new { success = true, message = "Activity summary retrieved successfully.", data = summary });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "An error occurred while retrieving activity summary.", error = ex.Message });
            }
        }

        /// <summary>
        /// Get activity summary by date range
        /// </summary>
        /// <param name="startDate">Start date</param>
        /// <param name="endDate">End date</param>
        /// <returns>Activity summary</returns>
        [HttpGet("activity-summary/date-range")]
        [ProducesResponseType(typeof(ActivitySummaryDto), 200)]
        [ProducesResponseType(401)]
        public async Task<ActionResult<ActivitySummaryDto>> GetActivitySummaryByDateRange(
            [FromQuery] DateTime startDate,
            [FromQuery] DateTime endDate)
        {
            try
            {
                // Validate parameters
                if (startDate > endDate)
                {
                    return BadRequest(new { success = false, message = "Start date cannot be greater than end date." });
                }

                // Validate admin user ID
                var adminUserId = GetCurrentUserId();
                if (adminUserId <= 0)
                {
                    return Unauthorized(new { success = false, message = "Invalid user authentication." });
                }

                var summary = await _adminService.GetActivitySummaryByDateRangeAsync(adminUserId, startDate, endDate);
                return Ok(new { success = true, message = "Activity summary retrieved successfully.", data = summary });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "An error occurred while retrieving activity summary.", error = ex.Message });
            }
        }

        /// <summary>
        /// Get activity summary by user
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <returns>Activity summary</returns>
        [HttpGet("activity-summary/user/{userId}")]
        [ProducesResponseType(typeof(ActivitySummaryDto), 200)]
        [ProducesResponseType(401)]
        public async Task<ActionResult<ActivitySummaryDto>> GetActivitySummaryByUser(int userId)
        {
            try
            {
                // Validate parameters
                if (userId <= 0)
                {
                    return BadRequest(new { success = false, message = "Invalid user ID." });
                }

                // Validate admin user ID
                var adminUserId = GetCurrentUserId();
                if (adminUserId <= 0)
                {
                    return Unauthorized(new { success = false, message = "Invalid user authentication." });
                }

                if (!await _adminService.CanUserAccessUserAsync(adminUserId, userId))
                {
                    return StatusCode(403, new { success = false, message = "Access denied." });
                }

                var summary = await _adminService.GetActivitySummaryByUserAsync(adminUserId, userId);
                return Ok(new { success = true, message = "Activity summary retrieved successfully.", data = summary });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "An error occurred while retrieving activity summary.", error = ex.Message });
            }
        }

        /// <summary>
        /// Get activity summary by module
        /// </summary>
        /// <param name="module">Module name</param>
        /// <returns>Activity summary</returns>
        [HttpGet("activity-summary/module/{module}")]
        [ProducesResponseType(typeof(ActivitySummaryDto), 200)]
        [ProducesResponseType(401)]
        public async Task<ActionResult<ActivitySummaryDto>> GetActivitySummaryByModule(string module)
        {
            try
            {
                // Validate parameters
                if (string.IsNullOrWhiteSpace(module))
                {
                    return BadRequest(new { success = false, message = "Module name is required." });
                }

                // Validate admin user ID
                var adminUserId = GetCurrentUserId();
                if (adminUserId <= 0)
                {
                    return Unauthorized(new { success = false, message = "Invalid user authentication." });
                }

                var summary = await _adminService.GetActivitySummaryByModuleAsync(adminUserId, module);
                return Ok(new { success = true, message = "Activity summary retrieved successfully.", data = summary });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "An error occurred while retrieving activity summary.", error = ex.Message });
            }
        }

        #endregion

        #region User Hierarchy

        /// <summary>
        /// Get user hierarchy
        /// </summary>
        /// <returns>User hierarchy</returns>
        [HttpGet("user-hierarchy")]
        [ProducesResponseType(typeof(UserHierarchyDto), 200)]
        [ProducesResponseType(401)]
        public async Task<ActionResult<UserHierarchyDto>> GetUserHierarchy()
        {
            try
            {
                // Validate admin user ID
                var adminUserId = GetCurrentUserId();
                if (adminUserId <= 0)
                {
                    return Unauthorized(new { success = false, message = "Invalid user authentication." });
                }

                var hierarchy = await _adminService.GetUserHierarchyAsync(adminUserId);
                return Ok(new { success = true, message = "User hierarchy retrieved successfully.", data = hierarchy });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "An error occurred while retrieving user hierarchy.", error = ex.Message });
            }
        }

        /// <summary>
        /// Get user hierarchy by admin
        /// </summary>
        /// <param name="adminUserId">Admin user ID</param>
        /// <returns>User hierarchy</returns>
        [HttpGet("user-hierarchy/admin/{adminUserId}")]
        [ProducesResponseType(typeof(UserHierarchyDto), 200)]
        [ProducesResponseType(401)]
        public async Task<ActionResult<UserHierarchyDto>> GetUserHierarchyByAdmin(int adminUserId)
        {
            try
            {
                // Validate parameters
                if (adminUserId <= 0)
                {
                    return BadRequest(new { success = false, message = "Invalid admin user ID." });
                }

                // Validate current user ID
                var currentUserId = GetCurrentUserId();
                if (currentUserId <= 0)
                {
                    return Unauthorized(new { success = false, message = "Invalid user authentication." });
                }

                if (!await IsMainAdminAsync(currentUserId))
                {
                    return StatusCode(403, new { success = false, message = "Only main admins can view other admin hierarchies." });
                }

                var hierarchy = await _adminService.GetUserHierarchyByAdminAsync(adminUserId);
                return Ok(new { success = true, message = "User hierarchy retrieved successfully.", data = hierarchy });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "An error occurred while retrieving user hierarchy.", error = ex.Message });
            }
        }

        #endregion

        #region Data Export

        /// <summary>
        /// Export user activities to CSV
        /// </summary>
        /// <param name="startDate">Start date</param>
        /// <param name="endDate">End date</param>
        /// <param name="userId">User ID (optional)</param>
        /// <param name="module">Module (optional)</param>
        /// <returns>CSV file</returns>
        [HttpGet("export-activities/csv")]
        [ProducesResponseType(200)]
        [ProducesResponseType(401)]
        public async Task<IActionResult> ExportActivitiesToCsv(
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null,
            [FromQuery] int? userId = null,
            [FromQuery] string? module = null)
        {
            try
            {
                // Validate parameters
                if (startDate.HasValue && endDate.HasValue && startDate > endDate)
                {
                    return BadRequest(new { success = false, message = "Start date cannot be greater than end date." });
                }

                if (userId.HasValue && userId <= 0)
                {
                    return BadRequest(new { success = false, message = "Invalid user ID." });
                }

                // Validate admin user ID
                var adminUserId = GetCurrentUserId();
                if (adminUserId <= 0)
                {
                    return Unauthorized(new { success = false, message = "Invalid user authentication." });
                }

                var filter = new ActivityFilterDto
                {
                    StartDate = startDate,
                    EndDate = endDate,
                    UserId = userId,
                    ActivityType = module
                };
                var csvData = await _adminService.ExportActivitiesToCsvAsync(filter, adminUserId);
                
                var fileName = $"user-activities-{DateTime.Now:yyyyMMdd-HHmmss}.csv";
                return File(System.Text.Encoding.UTF8.GetBytes(csvData), "text/csv", fileName);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "An error occurred while exporting activities.", error = ex.Message });
            }
        }

        /// <summary>
        /// Export user permissions to CSV
        /// </summary>
        /// <returns>CSV file</returns>
        [HttpGet("export-permissions/csv")]
        [ProducesResponseType(200)]
        [ProducesResponseType(401)]
        public async Task<IActionResult> ExportPermissionsToCsv()
        {
            try
            {
                // Validate admin user ID
                var adminUserId = GetCurrentUserId();
                if (adminUserId <= 0)
                {
                    return Unauthorized(new { success = false, message = "Invalid user authentication." });
                }

                if (!await IsAdminAsync(adminUserId))
                {
                    return StatusCode(403, new { success = false, message = "Only admins can export permissions." });
                }

                var clientCode = GetClientCodeFromToken();
                if (string.IsNullOrEmpty(clientCode))
                {
                    return BadRequest(new { success = false, message = "Client code not found in token." });
                }

                var csvData = await _adminService.ExportPermissionsToCsvAsync(clientCode);
                
                var fileName = $"user-permissions-{DateTime.Now:yyyyMMdd-HHmmss}.csv";
                return File(System.Text.Encoding.UTF8.GetBytes(csvData), "text/csv", fileName);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "An error occurred while exporting permissions.", error = ex.Message });
            }
        }

        #endregion

        #region Dashboard and Analytics

        /// <summary>
        /// Get admin dashboard with statistics and recent activities
        /// </summary>
        /// <returns>Admin dashboard data</returns>
        [HttpGet("dashboard")]
        [ProducesResponseType(typeof(AdminDashboardDto), 200)]
        [ProducesResponseType(401)]
        public async Task<ActionResult<AdminDashboardDto>> GetDashboard()
        {
            try
            {
                // Validate admin user ID
                var adminUserId = GetCurrentUserId();
                if (adminUserId <= 0)
                {
                    return Unauthorized(new { success = false, message = "Invalid user authentication." });
                }

                var dashboard = await _adminService.GetAdminDashboardAsync(adminUserId);
                return Ok(new { success = true, message = "Dashboard data retrieved successfully.", data = dashboard });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "An error occurred while retrieving dashboard data.", error = ex.Message });
            }
        }

        /// <summary>
        /// Get organization dashboard (Main Admin only)
        /// </summary>
        /// <returns>Organization dashboard data</returns>
        [HttpGet("organization/dashboard")]
        [ProducesResponseType(typeof(AdminDashboardDto), 200)]
        [ProducesResponseType(401)]
        public async Task<ActionResult<AdminDashboardDto>> GetOrganizationDashboard()
        {
            try
            {
                // Validate admin user ID
                var adminUserId = GetCurrentUserId();
                if (adminUserId <= 0)
                {
                    return Unauthorized(new { success = false, message = "Invalid user authentication." });
                }

                if (!await IsMainAdminAsync(adminUserId))
                {
                    return StatusCode(403, new { success = false, message = "Only main admins can view organization dashboard." });
                }

                var clientCode = GetClientCodeFromToken();
                if (string.IsNullOrEmpty(clientCode))
                {
                    return BadRequest(new { success = false, message = "Client code not found in token." });
                }

                var dashboard = await _adminService.GetOrganizationDashboardAsync(clientCode);
                return Ok(new { success = true, message = "Organization dashboard retrieved successfully.", data = dashboard });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "An error occurred while retrieving organization dashboard.", error = ex.Message });
            }
        }

        /// <summary>
        /// Get all users in organization (Main Admin only)
        /// </summary>
        /// <returns>List of all users in organization</returns>
        [HttpGet("organization/users")]
        [ProducesResponseType(typeof(IEnumerable<AdminUserDto>), 200)]
        [ProducesResponseType(401)]
        public async Task<ActionResult<IEnumerable<AdminUserDto>>> GetOrganizationUsers()
        {
            try
            {
                // Validate admin user ID
                var adminUserId = GetCurrentUserId();
                if (adminUserId <= 0)
                {
                    return Unauthorized(new { success = false, message = "Invalid user authentication." });
                }

                if (!await IsMainAdminAsync(adminUserId))
                {
                    return StatusCode(403, new { success = false, message = "Only main admins can view all organization users." });
                }

                var clientCode = GetClientCodeFromToken();
                if (string.IsNullOrEmpty(clientCode))
                {
                    return BadRequest(new { success = false, message = "Client code not found in token." });
                }

                var users = await _adminService.GetAllUsersInOrganizationAsync(clientCode);
                return Ok(new { success = true, message = "Organization users retrieved successfully.", data = users, count = users.Count() });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "An error occurred while retrieving organization users.", error = ex.Message });
            }
        }

        #endregion

        #region Branch and Counter Management

        /// <summary>
        /// Get all branches for the current client
        /// </summary>
        /// <returns>List of branches</returns>
        [HttpGet("branches")]
        [ProducesResponseType(typeof(IEnumerable<BranchMasterDto>), 200)]
        [ProducesResponseType(401)]
        public async Task<ActionResult<IEnumerable<BranchMasterDto>>> GetBranches()
        {
            try
            {
                // Validate admin user ID
                var adminUserId = GetCurrentUserId();
                if (adminUserId <= 0)
                {
                    return Unauthorized(new { success = false, message = "Invalid user authentication." });
                }

                var clientCode = GetClientCodeFromToken();
                if (string.IsNullOrEmpty(clientCode))
                {
                    return BadRequest(new { success = false, message = "Client code not found in token." });
                }
                
                var branches = await _adminService.GetBranchesAsync(clientCode);
                return Ok(new { success = true, message = "Branches retrieved successfully.", data = branches, count = branches.Count() });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "An error occurred while retrieving branches.", error = ex.Message });
            }
        }

        /// <summary>
        /// Get counters for a specific branch
        /// </summary>
        /// <param name="branchId">Branch ID</param>
        /// <returns>List of counters</returns>
        [HttpGet("branches/{branchId}/counters")]
        [ProducesResponseType(typeof(IEnumerable<CounterMasterDto>), 200)]
        [ProducesResponseType(401)]
        public async Task<ActionResult<IEnumerable<CounterMasterDto>>> GetCountersByBranch(int branchId)
        {
            try
            {
                // Validate parameters
                if (branchId <= 0)
                {
                    return BadRequest(new { success = false, message = "Invalid branch ID." });
                }

                // Validate admin user ID
                var adminUserId = GetCurrentUserId();
                if (adminUserId <= 0)
                {
                    return Unauthorized(new { success = false, message = "Invalid user authentication." });
                }

                var clientCode = GetClientCodeFromToken();
                if (string.IsNullOrEmpty(clientCode))
                {
                    return BadRequest(new { success = false, message = "Client code not found in token." });
                }
                
                var counters = await _adminService.GetCountersByBranchAsync(branchId, clientCode);
                return Ok(new { success = true, message = "Counters retrieved successfully.", data = counters, count = counters.Count() });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "An error occurred while retrieving counters.", error = ex.Message });
            }
        }

        /// <summary>
        /// Get users by branch and counter
        /// </summary>
        /// <param name="branchId">Branch ID (optional)</param>
        /// <param name="counterId">Counter ID (optional)</param>
        /// <returns>List of users</returns>
        [HttpGet("users/by-location")]
        [ProducesResponseType(typeof(IEnumerable<AdminUserDto>), 200)]
        [ProducesResponseType(401)]
        public async Task<ActionResult<IEnumerable<AdminUserDto>>> GetUsersByLocation(
            [FromQuery] int? branchId = null, 
            [FromQuery] int? counterId = null)
        {
            try
            {
                // Validate parameters
                if (branchId.HasValue && branchId <= 0)
                {
                    return BadRequest(new { success = false, message = "Invalid branch ID." });
                }

                if (counterId.HasValue && counterId <= 0)
                {
                    return BadRequest(new { success = false, message = "Invalid counter ID." });
                }

                // Validate admin user ID
                var adminUserId = GetCurrentUserId();
                if (adminUserId <= 0)
                {
                    return Unauthorized(new { success = false, message = "Invalid user authentication." });
                }

                var users = await _adminService.GetUsersByLocationAsync(adminUserId, branchId, counterId);
                return Ok(new { success = true, message = "Users retrieved successfully.", data = users, count = users.Count() });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "An error occurred while retrieving users by location.", error = ex.Message });
            }
        }

        #endregion

        #region Helper Methods

        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            return userIdClaim != null ? int.Parse(userIdClaim.Value) : 0;
        }

        private string GetClientCodeFromToken()
        {
            return User.FindFirst("ClientCode")?.Value ?? string.Empty;
        }

        private async Task<bool> IsAdminAsync(int userId)
        {
            var user = await _userService.GetUserByIdAsync(userId);
            return user != null && (user.IsAdmin || user.UserType == "MainAdmin" || user.UserType == "Admin");
        }

        private async Task<bool> IsMainAdminAsync(int userId)
        {
            var user = await _userService.GetUserByIdAsync(userId);
            return user != null && user.UserType == "MainAdmin";
        }

        #endregion
    }

    /// <summary>
    /// DTO for admin resetting user password (different from user self-reset password flow)
    /// </summary>
    public class AdminResetPasswordDto
    {
        [Required(ErrorMessage = "New password is required.")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be between 6 and 100 characters.")]
        public string NewPassword { get; set; } = string.Empty;
    }

    /// <summary>
    /// DTO for activating user
    /// </summary>
    public class ActivateUserDto
    {
        [Required(ErrorMessage = "User ID is required.")]
        [Range(1, int.MaxValue, ErrorMessage = "User ID must be greater than 0.")]
        public int UserId { get; set; }
        public bool IsActive { get; set; }
        [StringLength(500)]
        public string? Remarks { get; set; }
    }

    /// <summary>
    /// DTO for deactivating user
    /// </summary>
    public class DeactivateUserDto
    {
        [Required(ErrorMessage = "User ID is required.")]
        [Range(1, int.MaxValue, ErrorMessage = "User ID must be greater than 0.")]
        public int UserId { get; set; }
        public bool IsActive { get; set; }
        [StringLength(500)]
        public string? Remarks { get; set; }
    }

}
