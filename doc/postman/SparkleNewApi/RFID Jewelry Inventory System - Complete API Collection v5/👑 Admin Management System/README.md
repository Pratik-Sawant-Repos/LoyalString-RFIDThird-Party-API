Comprehensive admin management APIs with perfect validation, error handling, and permission management.

**✨ Key Features:**
- ✅ **Standardized Response Format**: All endpoints return `{ success: true/false, message: "...", data: ... }`
- ✅ **Comprehensive Validation**: ModelState validation, parameter validation (user IDs, dates, pagination), and business rules
- ✅ **Perfect Error Handling**: Consistent error responses with detailed validation error messages
- ✅ **Permission Management**: Proper MainAdmin vs Admin vs User permission checks
- ✅ **Self-Protection**: Prevents self-deletion and self-deactivation
- ✅ **Input Validation**: All DTOs have proper validation attributes ([Required], [StringLength], [Range], etc.)

**Response Format Examples:**

**Success Response:**
```json
{
  "success": true,
  "message": "User updated successfully.",
  "data": { ... }
}
```

**Error Response:**
```json
{
  "success": false,
  "message": "Invalid user ID.",
  "errors": ["User ID must be greater than 0"]
}
```

**Validation Rules:**
- User IDs must be > 0
- Page numbers must be >= 1
- Page size must be between 1 and 100
- Start date cannot be greater than end date
- Password must be at least 6 characters
- All required fields are validated via ModelState