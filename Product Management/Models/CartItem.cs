namespace Product_Management.Models
{
    // Represents one product inside the cart
    public class CartItem
    {
        public int ProductId { get; set; }

        public required string Name { get; set; }

        public decimal Price { get; set; }

        public required string ImageUrl { get; set; }

        public int Quantity { get; set; }

        // Total price for this item
        public decimal Total => Price * Quantity;
    }
}