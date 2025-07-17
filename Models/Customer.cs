using System.ComponentModel.DataAnnotations;

namespace GameCraft.Models
{
    public class Customer
    {
        public int CustomerId { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [StringLength(100)]
        public string? Phone { get; set; }

        [StringLength(200)]
        public string? Address { get; set; }

        [StringLength(100)]
        public string? City { get; set; }

        [StringLength(20)]
        public string? PostCode { get; set; }

        [Range(0, 2)]
        public int UserType { get; set; } // 0 = Admin, 1 = Customer, 2 = Employee

        [Required]
        public string PasswordHash { get; set; }

        [Required]
        public string Salt { get; set; }

        public byte[]? AvatarImageData { get; set; }

        [Range(0, 1000000)]
        public int PrizePoints { get; set; } = 0;

        public string? GameCraftCardNumber { get; set; }

        // New property for Admin Key
        public string? AdminKey { get; set; }
    }
}
