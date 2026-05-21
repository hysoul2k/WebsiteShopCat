using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace Huy_Final_0843.Models
{
    public class ApplicationUser : IdentityUser
    {
        [Required(ErrorMessage = "Vui lòng nhập họ tên")]
        [StringLength(100)]
        public string FullName { get; set; } = string.Empty;

        [StringLength(200)]
        public string? Address { get; set; }

        [Range(1, 120, ErrorMessage = "Tuổi không hợp lệ")]
        public int? Age { get; set; }

        public string? ProfileImageUrl { get; set; }
    }
}