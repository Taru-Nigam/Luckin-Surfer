using System.ComponentModel.DataAnnotations;

namespace GameCraft.Models
{
    public class Icon
    {
        [Key]
        public int IconId { get; set; }

        [Required]
        public string Name { get; set; } // e.g., "Play & Earn"

        [Required]
        public byte[] ImageData { get; set; } // Store image as binary data

        [Required]
        public string Description { get; set; } // e.g., "Win Tickets playing games!"

        public int Order { get; set; } // To define the display order
    }
}
