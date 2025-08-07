using System.ComponentModel.DataAnnotations;

namespace GameCraft.Models
{
    public class Card
    {
        [Key]
        public int CardId { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; }

        [StringLength(1000)]
        public string Description { get; set; }

        [Required]
        [Range(0.01, 100000)]
        public decimal Price { get; set; }

        public byte[] ImageData { get; set; }

        // Additional properties specific to cards can be added here
    }
}
