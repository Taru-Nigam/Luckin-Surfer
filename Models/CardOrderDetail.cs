using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GameCraft.Models
{
    public class CardOrderDetail
    {
        [Key]
        public int CardOrderDetailId { get; set; }

        [Required]
        public int OrderId { get; set; }

        [Required]
        public int CardId { get; set; } // Reference to Card

        public Card Card { get; set; } // Navigation property

        [Required]
        [Range(1, int.MaxValue)]
        public int Quantity { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal UnitPrice { get; set; }
    }
}
