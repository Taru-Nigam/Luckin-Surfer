using System.ComponentModel.DataAnnotations;

namespace GameCraft.Models
{
    public class CheckoutViewModel
    {
        [Required(ErrorMessage = "Name is required.")]
        [StringLength(100)]
        [Display(Name = "Full Name")]
        public string ShippingName { get; set; }

        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress]
        [StringLength(100)]
        [Display(Name = "Email Address")]
        public string ShippingEmail { get; set; }

        [Required(ErrorMessage = "Phone number is required.")]
        [Phone]
        [StringLength(20)]
        [Display(Name = "Phone Number")]
        public string ShippingPhone { get; set; }

        [Required(ErrorMessage = "Address is required.")]
        [StringLength(200)]
        [Display(Name = "Street Address")]
        public string ShippingAddress { get; set; }

        [Required(ErrorMessage = "City is required.")]
        [StringLength(100)]
        [Display(Name = "City")]
        public string ShippingCity { get; set; }

        [Required(ErrorMessage = "Post Code is required.")]
        [StringLength(10)]
        [Display(Name = "Post Code")]
        public string ShippingPostCode { get; set; }
    }
}