using System.Collections.Generic;
using GameCraft.Models; // Ensure this is correctly referenced

namespace GameCraft.ViewModels
{
    public class UserManagementViewModel
    {
        public List<Customer> Users { get; set; }
        public string SearchQuery { get; set; }
        public int? FilterUserType { get; set; } // Nullable to allow "All"
        public bool? FilterIsEmailVerified { get; set; } // Nullable to allow "All"
        public int? MinPrizePoints { get; set; }
        public int? MaxPrizePoints { get; set; }
    }
}
