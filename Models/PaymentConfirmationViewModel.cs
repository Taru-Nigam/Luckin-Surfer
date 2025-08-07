using System;
using System.Collections.Generic;

namespace GameCraft.Models
{
    public class PaymentConfirmationViewModel
    {
        public int OrderId { get; set; }
        public DateTime OrderDate { get; set; }
        public decimal TotalAmount { get; set; }
        public string CustomerName { get; set; }
        public string GameCardNumber { get; set; }
        public Promotion Promotion { get; set; }
        public Card PurchasedProduct { get; set; }
    }
}
