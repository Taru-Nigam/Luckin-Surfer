using System.ComponentModel.DataAnnotations;

namespace GameCraft.Models
{
    public class AdminLoginViewModel
    {
        [Required(ErrorMessage = "Admin Key is required.")]
        [DataType(DataType.Password)]
        public string AdminKey { get; set; }
    }
}
