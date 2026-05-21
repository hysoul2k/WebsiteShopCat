using System.ComponentModel.DataAnnotations;

namespace Huy_Final_0843.Models
{
    public class Category
    {
        [Key]
        public int Id { get; set; }

        [Required, StringLength(100)]
        public string Name { get; set; } = default!;

        public List<Product>? Products { get; set; }
    }
}
