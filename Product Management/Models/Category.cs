using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;

namespace Product_Management.Models
{
    public class Category
    {
        public int Id { get; set; }

        [Required]
        public required string Name { get; set; }

        // One category → many products
        public List<Product>? Products { get; set; }
    }
}