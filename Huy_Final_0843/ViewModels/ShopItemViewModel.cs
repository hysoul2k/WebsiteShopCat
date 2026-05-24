namespace Huy_Final_0843.ViewModels
{
    public class ShopItemViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = default!;
        public decimal Price { get; set; }
        public string? ImageUrl { get; set; }
        public bool IsCat { get; set; }

        // Cat-only
        public string? Gender { get; set; }
        public int Age { get; set; }

        // Product-only
        public int StockQuantity { get; set; }
        public double AvgRating { get; set; }
        public int TotalReviews { get; set; }
        public string? CategoryName { get; set; }
    }
}
