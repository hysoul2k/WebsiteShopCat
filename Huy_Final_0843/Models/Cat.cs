using System.ComponentModel.DataAnnotations;

namespace Huy_Final_0843.Models
{
    public class Cat
    {
        public int Id { get; set; }

        [Required, StringLength(100)]
        public string Name { get; set; } = default!;

        [Range(0, 2000000000, ErrorMessage = "Giá trị phải từ 0 đến 2 tỷ")]
        public decimal Price { get; set; }

        public string? Description { get; set; }

        public string? ImageUrl { get; set; }

        [Required]
        public string Gender { get; set; } = "Không rõ";

        [Range(0, 30, ErrorMessage = "Tuổi phải từ 0 đến 30")]
        public int Age { get; set; } = 0;
    }
}
