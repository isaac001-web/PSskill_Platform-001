//using System.ComponentModel.DataAnnotations;

//namespace Product_Management.Models
//{
//    public class Order
//    {
//        public int Id { get; set; }

//        // Logged-in user ID
//        public string UserId { get; set; } = string.Empty;

//        // Navigation property
//        public ApplicationUser? User { get; set; }

//        // Customer full name
//        [Required]
//        public string CustomerName { get; set; }
//            = string.Empty;

//        // Customer email
//        [Required]
//        public string Email { get; set; }
//            = string.Empty;

//        // Phone number
//        [Required]
//        public string PhoneNumber { get; set; }
//            = string.Empty;

//        // Address
//        [Required]
//        public string Address { get; set; }
//            = string.Empty;

//        // Total order amount
//        public decimal TotalAmount { get; set; }

//        // Order date
//        public DateTime OrderDate { get; set; }
//            = DateTime.Now;

//        // Order items
//        public List<OrderItem> OrderItems { get; set; }
//            = new List<OrderItem>();
//    }
//}



using System.ComponentModel.DataAnnotations;

namespace Product_Management.Models
{
    // ORDER STATUS ENUM
    public enum OrderStatus
    {
        Pending = 0,
        Processing = 1,
        Shipped = 2,
        Delivered = 3,
        Cancelled = 4
    }

    public class Order
    {
        public int Id { get; set; }

        public string UserId { get; set; } = string.Empty;
        public ApplicationUser? User { get; set; }

        [Required]
        public string CustomerName { get; set; } = string.Empty;

        [Required]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string PhoneNumber { get; set; } = string.Empty;

        [Required]
        public string Address { get; set; } = string.Empty;

        public decimal TotalAmount { get; set; }

        public DateTime OrderDate { get; set; } = DateTime.Now;

        // ← NEW
        public OrderStatus Status { get; set; } = OrderStatus.Pending;

        // ← NEW
        public string? CancelReason { get; set; }

        // ← NEW
        public DateTime? LastUpdated { get; set; }

        public List<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
    }
}