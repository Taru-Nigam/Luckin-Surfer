using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace GameCraft.Models
{
    public class Order
    {
        public int OrderId { get; set; }

        [Required]
        public int CustomerId { get; set; }

        [Required]
        public DateTime OrderDate { get; set; }

        public decimal TotalAmount { get; set; }

    }
}