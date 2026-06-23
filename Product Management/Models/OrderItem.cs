namespace Product_Management.Models
{
    public class OrderItem
    {
        public int Id { get; set; }

        // Foreign Key → Order
        public int OrderId { get; set; }

        public Order? Order { get; set; }          // ← FIXED

        // Foreign Key → Product
        public int ProductId { get; set; }

        public Product? Product { get; set; }      // ← FIXED

        // Quantity purchased
        public int Quantity { get; set; }

        // Product price at time of purchase
        public decimal Price { get; set; }
    }
}