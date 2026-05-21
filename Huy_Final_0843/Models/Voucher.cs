using System.ComponentModel.DataAnnotations;

namespace Huy_Final_0843.Models
{
    public class Voucher
    {
        public int Id { get; set; }
        
        [Required, StringLength(50)]
        public string Code { get; set; } // Mã text, ví dụ: TET2024
        
        [Range(1, 100)]
        public int DiscountPercent { get; set; } // Giảm giá %
        
        public int MaxUsage { get; set; } // Maximum số lượng người dùng 
        
        public int UsedCount { get; set; } = 0; // Đã xài bấy nhiêu lần
        
        public DateTime ExpiryDate { get; set; } // Ngày hết hạn
    }
}
