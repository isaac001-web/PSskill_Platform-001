using Microsoft.AspNetCore.Identity;

namespace Product_Management.Models
{
    // Custom Identity User
    public class ApplicationUser : IdentityUser
    {
        // =========================================
        // PROFILE INFORMATION
        // =========================================

        public string? FullName { get; set; }

        public string? ProfileImage { get; set; }

        public string? Address { get; set; }

        public string? City { get; set; }

        public string? State { get; set; }

        public string? Country { get; set; }

        public string? Bio { get; set; }

        public DateTime CreatedAt { get; set; }
            = DateTime.Now;

        // =========================================
        // RELATIONSHIPS
        // =========================================

        // USER ORDERS
        public ICollection<Order>? Orders { get; set; }

        // USER NOTIFICATIONS
        public ICollection<Notification>? Notifications
        {
            get; set;
        }

        // =========================
        // SENT MESSAGES
        // =========================
        public ICollection<Message>? SentMessages
        {
            get; set;
        }

        // =========================
        // RECEIVED MESSAGES
        // =========================
        public ICollection<Message>? ReceivedMessages
        {
            get; set;
        }



    }
}