using System.ComponentModel.DataAnnotations;

namespace Product_Management.Models
{
    public class Review
    {
        public int Id { get; set; }

        // Foreign key to Product
        [Required]
        public int ProductId { get; set; }

        // Navigation property to Product
        public Product? Product { get; set; }

        // Foreign key to User (ASP.NET Identity)
        public string? UserId { get; set; }

        // Navigation property to User
        public ApplicationUser? User { get; set; }

        [Required]
        [Range(1, 5)]
        public int Rating { get; set; }

        [Required]
        [StringLength(1000)]
        public required string Comment { get; set; }

        public DateTime ReviewDate { get; set; } = DateTime.Now;
    }
}