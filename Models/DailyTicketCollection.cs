using System;
using System.ComponentModel.DataAnnotations;

namespace GameCraft.Models
{
    public class DailyTicketCollection
    {
        [Key]
        public int Id { get; set; } // Primary Key

        [Required]
        public DateTime CollectionDate { get; set; } // Date for which tickets were collected (e.g., only date part)

        [Required]
        public long TotalTicketsCollected { get; set; } // Total tickets collected for this specific day
    }
}