// Example AuditLog.cs
using System;
using System.ComponentModel.DataAnnotations;

namespace GameCraft.Models
{
    public class AuditLog
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } // Or int if your User ID is int

        [Required]
        public string UserName { get; set; } // To easily display who performed the action

        [Required]
        public string Action { get; set; } // e.g., "Change Password", "Add User", "Logged In"

        public string Details { get; set; } // Additional details, e.g., "Old value: X, New value: Y"

        [Required]
        public DateTime Timestamp { get; set; }

        [Required]
        public string UserRole { get; set; } // To distinguish between customer and admin activities
    }
}
