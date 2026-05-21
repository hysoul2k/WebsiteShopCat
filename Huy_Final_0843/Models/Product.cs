using System.ComponentModel.DataAnnotations;

namespace Huy_Final_0843.Models
{
    public class Product
    {
        public int Id { get; set; }
        [Required, StringLength(100)]
        public string Name { get; set; } = default!;
        [Range(0, 2000000000, ErrorMessage = "Giá trị phải từ 0 đến 2 tỷ")]
        public decimal Price { get; set; }
        public string Description { get; set; } = default!;
        public string? ImageUrl { get; set; }
        public List<ProductImage>? Images { get; set; }
        public int CategoryId { get; set; }
        public Category? Category { get; set; }

        // Tính năng Tồn kho
        [Range(0, 100000)]
        public int StockQuantity { get; set; } = 50;

        // Tính năng bình luận, đánh giá (Rating/Reviews)
        public List<Review>? Reviews { get; set; }
    }
}
