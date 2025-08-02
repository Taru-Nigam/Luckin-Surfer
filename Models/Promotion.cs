using System.ComponentModel.DataAnnotations;

namespace GameCraft.Models
{
    public class Promotion
    {
        [Key]
        public int PromotionId { get; set; }

        [Required]
        public string Title { get; set; }

        public string Description { get; set; }

        [Required]
        public byte[] ImageData { get; set; } // Store image as binary data
        public string ButtonText { get; set; }
        public string ButtonUrl { get; set; }
        public string BackgroundColor { get; set; } // For carousel display
        public string TextColor { get; set; }       // For carousel display

        public decimal Price { get; set; } // Example: price for a card promotion
    }
}
