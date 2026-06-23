using System.ComponentModel.DataAnnotations;

namespace Product_Management.Models
{
    public class Product
    {
        public int Id { get; set; }

        [Required]
        public required string Name { get; set; }

        [Required]
        public decimal Price { get; set; }

        [Required]
        public int Quantity { get; set; }

        public string? ImageUrl { get; set; }
        

        // FOREIGN KEY (CATEGORY)
        [Required]
        public int CategoryId { get; set; }

        // Navigation property
        public Category? Category { get; set; }

        public List<OrderItem> OrderItems { get; set; }
    = new List<OrderItem>();

       // public List<OrderItem> OrderItems { get; set; }
   // = new();

        public ICollection<Review>? Reviews { get; set; }


    }
}