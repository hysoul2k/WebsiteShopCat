using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Huy_Final_0843.Models
{
    public class AuditLog
    {
        [Key]
        public int Id { get; set; }

        public string UserId { get; set; }
        [ForeignKey("UserId")]
        public virtual ApplicationUser User { get; set; }

        [Required]
        public string Action { get; set; } // Ví dụ: "Thêm mới sản phẩm", "Cập nhật trạng thái đơn hàng"

        [Required]
        public string TableName { get; set; } // Ví dụ: "Products", "Orders"

        public string EntityId { get; set; } // ID của đối tượng bị tác động

        public string Details { get; set; } // Chi tiết thay đổi (JSON hoặc Text)

        public DateTime Timestamp { get; set; } = DateTime.Now;
    }
}
