using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace GameCraft.Models
{
    public class Order
    {
        public int OrderId { get; set; }

        public int CustomerId { get; set; }

        public decimal TotalAmount { get; set; }

        public DateTime OrderDate { get; set; }

        [StringLength(100)]
        public string ShippingName { get; set; } = "";

        [StringLength(100)]
        public string ShippingEmail { get; set; } = "";

        [StringLength(50)]
        public string ShippingPhone { get; set; } = "";

        [StringLength(20)]
        public string ShippingPostCode { get; set; } = "";

        [StringLength(200)]
        public string ShippingAddress { get; set; } = "";

        [StringLength(100)]
        public string ShippingCity { get; set; } = "";

        public List<OrderDetail> OrderDetails { get; set; }
        public List<CardOrderDetail> CardOrderDetails { get; set; }
    }
}