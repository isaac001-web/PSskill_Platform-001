using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Product_Management.Models;
using Product_Management.Helpers;
using Product_Management.Data;

namespace Product_Management.Controllers
{
    public class CartController : Controller
    {
        private readonly ApplicationDbContext _context;
        private const string CartSession = "Cart";

        public CartController(ApplicationDbContext context)
        {
            _context = context;
        }

        // =========================================
        // VIEW CART
        // =========================================

        public IActionResult Index()
        {
            var cart = SessionHelper.GetObject<List<CartItem>>(
                HttpContext, CartSession)
                ?? new List<CartItem>();

            UpdateCartCount(cart);
            return View(cart);
        }

        // =========================================
        // ADD TO CART
        // =========================================

        public async Task<IActionResult> Add(int id)
        {
            // ← REDIRECT TO LOGIN IF NOT AUTHENTICATED
            if (!User.Identity!.IsAuthenticated)
            {
                var returnUrl = Url.Action("Add", "Cart",
                    new { id = id });

                return RedirectToPage("/Account/Login",
                    new { area = "Identity", returnUrl });
            }

            var product = await _context.Products
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null) return NotFound();

            var cart = SessionHelper.GetObject<List<CartItem>>(
                HttpContext, CartSession)
                ?? new List<CartItem>();

            var existingItem = cart
                .FirstOrDefault(x => x.ProductId == id);

            if (existingItem != null)
            {
                if (existingItem.Quantity < product.Quantity)
                    existingItem.Quantity++;
            }
            else
            {
                cart.Add(new CartItem
                {
                    ProductId = product.Id,
                    Name = product.Name,
                    Price = product.Price,
                    Quantity = 1,
                    ImageUrl = product.ImageUrl
                });
            }

            SessionHelper.SetObject(HttpContext, CartSession, cart);
            UpdateCartCount(cart);

            TempData["Success"] = $"{product.Name} added to cart";

            return Redirect(Request.Headers["Referer"].ToString());
        }

        // =========================================
        // INCREASE QUANTITY
        // =========================================

        public async Task<IActionResult> Increase(int id)
        {
            var cart = SessionHelper.GetObject<List<CartItem>>(
                HttpContext, CartSession)
                ?? new List<CartItem>();

            var item = cart.FirstOrDefault(x => x.ProductId == id);

            if (item != null)
            {
                var product = await _context.Products.FindAsync(id);

                if (product != null &&
                    item.Quantity < product.Quantity)
                {
                    item.Quantity++;
                }
            }

            SessionHelper.SetObject(HttpContext, CartSession, cart);
            UpdateCartCount(cart);

            return RedirectToAction("Index");
        }

        // =========================================
        // DECREASE QUANTITY
        // =========================================

        public IActionResult Decrease(int id)
        {
            var cart = SessionHelper.GetObject<List<CartItem>>(
                HttpContext, CartSession)
                ?? new List<CartItem>();

            var item = cart.FirstOrDefault(x => x.ProductId == id);

            if (item != null)
            {
                item.Quantity--;

                if (item.Quantity <= 0)
                    cart.Remove(item);
            }

            SessionHelper.SetObject(HttpContext, CartSession, cart);
            UpdateCartCount(cart);

            return RedirectToAction("Index");
        }

        // =========================================
        // REMOVE ITEM
        // =========================================

        public IActionResult Remove(int id)
        {
            var cart = SessionHelper.GetObject<List<CartItem>>(
                HttpContext, CartSession)
                ?? new List<CartItem>();

            var item = cart.FirstOrDefault(x => x.ProductId == id);

            if (item != null)
                cart.Remove(item);

            SessionHelper.SetObject(HttpContext, CartSession, cart);
            UpdateCartCount(cart);

            return RedirectToAction("Index");
        }

        // =========================================
        // CLEAR CART
        // =========================================

        public IActionResult Clear()
        {
            HttpContext.Session.Remove(CartSession);
            HttpContext.Session.SetInt32("CartCount", 0);

            TempData["Success"] = "Cart cleared successfully";

            return RedirectToAction("Index");
        }

        // =========================================
        // UPDATE CART COUNT
        // =========================================

        private void UpdateCartCount(List<CartItem> cart)
        {
            int count = cart.Sum(x => x.Quantity);
            HttpContext.Session.SetInt32("CartCount", count);
        }
    }
}