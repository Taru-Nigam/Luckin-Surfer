using System.ComponentModel.DataAnnotations;

namespace GameCraft.Models
{
    public class VerifyOtpViewModel
    {
        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Invalid email address.")]
        public string Email { get; set; }

        [Required(ErrorMessage = "OTP is required.")]
        [StringLength(6, MinimumLength = 6, ErrorMessage = "OTP must be 6 digits.")]
        [RegularExpression("^[0-9]*$", ErrorMessage = "OTP must be numeric.")]
        public string Otp { get; set; }
    }
}
