using GameCraft.Models;
using System.ComponentModel.DataAnnotations;

namespace GameCraft.ViewModels // Ensure this namespace matches your project structure
{
    public class DebitCardViewModel
    {
        // Properties to carry promotion details
        // Product details
        public Product Product { get; set; }

        // Card details
        [Required(ErrorMessage = "Card Number is required.")]
        [Display(Name = "Card Number")]
        public string CardNumber { get; set; }

        [Required(ErrorMessage = "Cardholder Name is required.")]
        [Display(Name = "Cardholder Name")]
        public string CardholderName { get; set; }

        [Required(ErrorMessage = "Expiry Month is required.")]
        [Display(Name = "Expiry Month")]
        public int ExpiryMonth { get; set; }

        [Required(ErrorMessage = "Expiry Year is required.")]
        [Display(Name = "Expiry Year")]
        public int ExpiryYear { get; set; }

        [Required(ErrorMessage = "CVV is required.")]
        [Display(Name = "CVV")]
        public string CVV { get; set; }
    }
}
