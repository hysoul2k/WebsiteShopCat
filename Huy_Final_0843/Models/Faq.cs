using System.ComponentModel.DataAnnotations;

namespace Huy_Final_0843.Models
{
    public class Faq
    {
        [Key]
        public int FaqId { get; set; }

        [Required]
        public string Question { get; set; } = "";

        [Required]
        public string Answer { get; set; } = "";

        // policy, shipping, cat_care, order, general
        [Required]
        [MaxLength(50)]
        public string Category { get; set; } = "general";

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
