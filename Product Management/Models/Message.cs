using System.ComponentModel.DataAnnotations.Schema;

namespace Product_Management.Models
{
    public class Message
    {
        public int Id { get; set; }
        public string? UserId { get; set; }

        public string Subject { get; set; }
            = string.Empty;

        public string Content { get; set; }
            = string.Empty;

        public DateTime SentAt { get; set; }
            = DateTime.Now;

        // =========================
        // SENDER
        // =========================

        public string SenderId { get; set; }
            = string.Empty;

        [ForeignKey("SenderId")]
        public ApplicationUser? Sender { get; set; }

        // =========================
        // RECEIVER
        // =========================

        public string ReceiverId { get; set; }
            = string.Empty;

        [ForeignKey("ReceiverId")]
        public ApplicationUser? Receiver { get; set; }

        // =========================
        // ADMIN REPLY
        // =========================

        public string? AdminReply { get; set; }

        public bool IsRead { get; set; }
            = false;
    }
}