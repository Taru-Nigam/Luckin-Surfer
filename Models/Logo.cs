// File: Models/Logo.cs
using System.ComponentModel.DataAnnotations;
namespace GameCraft.Models
{
    public class Logo
    {
        [Key]
        public int LogoId { get; set; }
        [Required]
        public string Name { get; set; } // e.g., "GameCraft Logo"
        [Required]
        public byte[] ImageData { get; set; } // Store image as binary data
        [Required]
        public string Description { get; set; } // e.g., "Main website logo"
    }
}