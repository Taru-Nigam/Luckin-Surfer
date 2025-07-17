namespace GameCraft.Models
{
    public class OrderSummaryViewModel
    {
        public int OrderId { get; set; }
        public decimal TotalAmount { get; set; }
        public string ShippingName { get; set; } = "";
        public string ShippingEmail { get; set; } = "";
        public string ShippingPhone { get; set; } = "";
        public string ShippingPostCode { get; set; } = "";
        public string ShippingAddress { get; set; } = "";
        public string ShippingCity { get; set; } = "";
    }
}