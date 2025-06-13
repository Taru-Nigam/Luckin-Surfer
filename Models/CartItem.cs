using System.ComponentModel.DataAnnotations;

namespace GameCraft.Models
{
    public class CartItem
    {
        [Key]
        public int CartItemId { get; set; }

        [Required]
        public int ProductId { get; set; }  // Reference to the product/item ID

        [Required]
        [StringLength(200)]
        public string Name { get; set; }  // Name of the product/item

        [Required]
        [Range(0.0, double.MaxValue)]
        public decimal Price { get; set; }  // Price per single unit

        [Required]
        [Range(1, int.MaxValue)]
        public int Quantity { get; set; }  // Number of units of this item in the cart

        // Optional: You can add more properties like image URL, description etc. if needed
        [StringLength(500)]
        public string ImageUrl { get; set; }
    }
}
