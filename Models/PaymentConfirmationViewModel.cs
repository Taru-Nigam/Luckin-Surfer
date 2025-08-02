using GameCraft.Models;
using System;
using System.Collections.Generic;

namespace GameCraft.ViewModels
{
    public class PaymentConfirmationViewModel
    {
        public int OrderId { get; set; }
        public DateTime OrderDate { get; set; }
        public decimal TotalAmount { get; set; }
        public string CustomerName { get; set; }
        public string GameCardNumber { get; set; }
        public Promotion Promotion { get; set; }
        public Product PurchasedProduct { get; set; }
    }
}
