using System.ComponentModel.DataAnnotations;

namespace Huy_Final_0843.Models
{
    public class VoucherUsage
    {
        public int Id { get; set; }

        public int VoucherId { get; set; }
        public Voucher Voucher { get; set; } = default!;

        [Required]
        public string UserId { get; set; } = default!;
        public ApplicationUser User { get; set; } = default!;

        public DateTime UsedAt { get; set; } = DateTime.UtcNow;
    }
}
