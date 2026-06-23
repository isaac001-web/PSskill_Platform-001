namespace Product_Management.Models
{
    public class Notification
    {
        public int Id { get; set; }

        public string Title { get; set; } = string.Empty;      // ← FIXED

        public string Message { get; set; } = string.Empty;    // ← FIXED

        public bool IsRead { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // =========================
        // USER RELATIONSHIP
        // =========================

        public string UserId { get; set; } = string.Empty;     // ← FIXED

        public ApplicationUser? User { get; set; }              // ← FIXED
    }
}