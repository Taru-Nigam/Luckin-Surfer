using System;
using System.ComponentModel.DataAnnotations;

namespace GameCraft.Models
{
    public class AuditLog
    {
        [Key]
        public int Id { get; set; } // Primary Key for the log record

        [Required]
        [StringLength(500)]
        public string Action { get; set; } // Description of the action (e.g., "Logged in", "Updated stock for X")

        [Required]
        [StringLength(100)]
        public string EmployeeName { get; set; } // Name of the employee performing the action

        public DateTime Timestamp { get; set; } = DateTime.Now; // When the action occurred
    }
}