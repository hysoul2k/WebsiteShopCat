using System.ComponentModel.DataAnnotations;

namespace Huy_Final_0843.Models
{
    public class ChatLog
    {
        public int Id { get; set; }

        [Required, MaxLength(100)]
        public string SessionId { get; set; } = "";

        public string? AccountId { get; set; }
        public ApplicationUser? Account { get; set; }

        [Required, MaxLength(10)]
        public string MessageFrom { get; set; } = "user"; // user | bot

        [Required]
        public string MessageContent { get; set; } = "";

        [MaxLength(50)]
        public string? Intent { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
