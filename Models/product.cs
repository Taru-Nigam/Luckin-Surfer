using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GameCraft.Models
{
    public class Product
    {
        [Key]
        public int ProductId { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; }

        [StringLength(1000)]
        public string Description { get; set; }

        [Required]
        [Range(0.01, 100000)]
        public decimal Price { get; set; }

        [Required]
        public int CategoryId { get; set; }

        public int Quantity { get; set; }

        public byte[] ImageData { get; set; }

        // Navigation property to the Category
        [ForeignKey("CategoryId")]
        public virtual Category Category { get; set; } // This will allow you to access the category directly
    }
}
