// Models/Prize.cs
using System.ComponentModel.DataAnnotations;

namespace GameCraft.Models 
{
    public class Prize 
    {
        [Key] // Primary key for the database
        public int Id { get; set; }

        [Required] // Name is mandatory
        [StringLength(100, ErrorMessage = "Prize Name cannot exceed 100 characters.")]
        public string Name { get; set; }

        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Ticket Cost must be a positive number.")]
        public int TicketCost { get; set; }

        [StringLength(255, ErrorMessage = "Image URL cannot exceed 255 characters.")]
        public string ImageUrl { get; set; }

        [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters.")]
        public string Description { get; set; }

        [Required(ErrorMessage = "Category is required.")] 
        [StringLength(50, ErrorMessage = "Category cannot exceed 50 characters.")]
        public string Category { get; set; }

    }
}