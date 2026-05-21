using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation; // Cho [ValidateNever]
using System.ComponentModel.DataAnnotations.Schema;      // Cho [ForeignKey]
namespace Huy_Final_0843.Models
{
    public enum OrderStatus
    {
        Pending,    // Chờ xử lý
        Shipping,   // Đang giao hàng
        Completed,  // Hoàn thành
        Cancelled   // Đã hủy
    }

    public class Order
    {
        public int Id { get; set; }
        public string UserId { get; set; } = default!;
        public DateTime OrderDate { get; set; } = DateTime.UtcNow.AddHours(7);
        public decimal TotalPrice { get; set; }
        public string ShippingAddress { get; set; } = default!;
        public string Notes { get; set; } = default!;
        public OrderStatus Status { get; set; } = OrderStatus.Pending;
        public string? CancellationReason { get; set; }
        public string PaymentStatus { get; set; } = "Pending";
        public string PaymentMethod { get; set; } = "COD"; // COD or BankTransfer
        // Tính năng Mã Giảm Giá Voucher
        public int? VoucherId { get; set; }
        [ForeignKey("VoucherId")]
        public Voucher? Voucher { get; set; }
        public decimal DiscountAmount { get; set; } = 0;

        [ForeignKey("UserId")] 
        [ValidateNever]
        public ApplicationUser ApplicationUser { get; set; } = default!;
        public List<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();
    }
}
