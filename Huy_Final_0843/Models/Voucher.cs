using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Huy_Final_0843.Models
{
    public class Voucher
    {
        public int Id { get; set; }

        [Required, StringLength(50)]
        public string Code { get; set; } // Mã text, ví dụ: TET2024

        [Required, StringLength(20)]
        public string DiscountType { get; set; } = "Percent";

        [NotMapped]
        public int DiscountValue
        {
            get => DiscountPercent;
            set => DiscountPercent = value;
        }

        [Range(1, 100)]
        public int DiscountPercent { get; set; } // Giảm giá %

        public decimal MinOrderAmount { get; set; }

        public int MaxUsage { get; set; } // Maximum số lượng người dùng

        [NotMapped]
        public int UsageLimit
        {
            get => MaxUsage;
            set => MaxUsage = value;
        }

        public int UsedCount { get; set; } = 0; // Đã xài bấy nhiêu lần

        public DateTime ExpiryDate { get; set; } // Ngày hết hạn

        public bool IsActive { get; set; } = true;

        // Public: hiện trong danh sách. Private: ẩn, chỉ nhập tay.
        public bool IsPrivate { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
