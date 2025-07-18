using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GameCraft.Models
{
    public class RedeemedPrize
    {
        [Key]
        public int Id { get; set; } // Primary Key for the redemption record

        [Required]
        public int ProductId { get; set; } // Foreign key to the Product (Prize)
        [ForeignKey("ProductId")]
        public Product Product { get; set; } // Navigation property to Product

        [Required]
        public int CustomerId { get; set; } // Foreign key to the Customer (Player who redeemed)
        [ForeignKey("CustomerId")]
        public Customer Customer { get; set; } // Navigation property to Customer

        [Required]
        public int TicketsSpent { get; set; } // How many tickets were spent for this prize

        public DateTime RedemptionDate { get; set; } = DateTime.Now; // When it was redeemed

        [StringLength(255)]
        public string? EmployeeName { get; set; } // Optional: Employee who processed the redemption
    }
}