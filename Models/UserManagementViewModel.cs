using System.Collections.Generic;

namespace GameCraft.Models
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
