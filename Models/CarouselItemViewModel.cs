namespace GameCraft.Models
{
    public class CarouselItemViewModel
    {
        public string Type { get; set; } // "Product" or "Promotion"
        public string Title { get; set; }
        public string Description { get; set; }
        public string ImageUrl { get; set; }
        public string ButtonText { get; set; }
        public string ButtonUrl { get; set; }
        public string BackgroundColor { get; set; } // For promotions
        public string TextColor { get; set; }       // For promotions
    }
}
