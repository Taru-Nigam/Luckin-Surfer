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
        public string Phone { get; set; }

        [StringLength(200)]
        public string Address { get; set; }

        [StringLength(100)]
        public string City { get; set; }

        [StringLength(20)]
        public string PostCode { get; set; }

        [StringLength(100)]
        public int UserType { get; set; }
        
        [Required]
        public string HashedPassword { get; set; }

        [Required]
        public string Salt { get; set; }
    }
}
