//using Microsoft.AspNetCore.Authorization;
//using Microsoft.AspNetCore.Identity;
//using Microsoft.AspNetCore.Mvc;
//using Microsoft.EntityFrameworkCore;
//using Product_Management.Data;
//using Product_Management.Models;
//using Product_Management.Helpers;

//namespace Product_Management.Controllers
//{
//    [Authorize]
//    public class CheckoutController : Controller
//    {
//        private readonly ApplicationDbContext _context;
//        private readonly UserManager<ApplicationUser> _userManager;  // ← FIXED
//        private const string CartSession = "Cart";

//        public CheckoutController(
//            ApplicationDbContext context,
//            UserManager<ApplicationUser> userManager)  // ← FIXED
//        {
//            _context = context;
//            _userManager = userManager;
//        }

//        [HttpGet]
//        public async Task<IActionResult> Index()
//        {
//            var cart = SessionHelper.GetObject<List<CartItem>>(HttpContext, CartSession)
//                ?? new List<CartItem>();

//            if (!cart.Any()) return RedirectToAction("Index", "Cart");

//            var user = await _userManager.GetUserAsync(User);

//            var order = new Order
//            {
//                CustomerName = user.FullName,
//                Email = user.Email,
//                PhoneNumber = user.PhoneNumber,
//                Address = user.Address
//            };

//            return View(order);
//        }

//        [HttpPost]
//        public async Task<IActionResult> Index(Order order)
//        {
//            var cart = SessionHelper.GetObject<List<CartItem>>(HttpContext, CartSession)
//                ?? new List<CartItem>();

//            if (!cart.Any()) return RedirectToAction("Index", "Cart");

//            if (ModelState.IsValid)
//            {
//                var user = await _userManager.GetUserAsync(User);

//                order.UserId = user.Id;
//                order.TotalAmount = cart.Sum(x => x.Price * x.Quantity);
//                order.OrderDate = DateTime.Now;

//                _context.Orders.Add(order);
//                await _context.SaveChangesAsync();

//                foreach (var item in cart)
//                {
//                    var product = await _context.Products.FindAsync(item.ProductId);

//                    if (product != null)
//                    {
//                        if (product.Quantity < item.Quantity)
//                        {
//                            ModelState.AddModelError("", $"Not enough stock for {product.Name}");
//                            return View(order);
//                        }

//                        product.Quantity -= item.Quantity;

//                        var orderItem = new OrderItem
//                        {
//                            OrderId = order.Id,
//                            ProductId = product.Id,
//                            Quantity = item.Quantity,
//                            Price = item.Price
//                        };

//                        _context.OrderItems.Add(orderItem);
//                    }
//                }

//                await _context.SaveChangesAsync();
//                HttpContext.Session.Remove(CartSession);
//                return RedirectToAction("Success");
//            }

//            return View(order);
//        }

//        public IActionResult Success()
//        {
//            return View();
//        }

//        public async Task<IActionResult> MyOrders()
//        {
//            var user = await _userManager.GetUserAsync(User);

//            var orders = await _context.Orders
//                .Where(x => x.UserId == user.Id)
//                .OrderByDescending(x => x.OrderDate)
//                .ToListAsync();

//            return View(orders);
//        }
//    }
//}



using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Product_Management.Data;
using Product_Management.Models;
using Product_Management.Helpers;

namespace Product_Management.Controllers
{
    [Authorize]
    public class CheckoutController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private const string CartSession = "Cart";

        public CheckoutController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var cart = SessionHelper.GetObject<List<CartItem>>(
                HttpContext, CartSession)
                ?? new List<CartItem>();

            if (!cart.Any())
                return RedirectToAction("Index", "Cart");

            var user = await _userManager.GetUserAsync(User);

            var order = new Order
            {
                CustomerName = user!.FullName ?? string.Empty,
                Email = user.Email ?? string.Empty,
                PhoneNumber = user.PhoneNumber ?? string.Empty,
                Address = user.Address ?? string.Empty
            };

            return View(order);
        }

        [HttpPost]
        public async Task<IActionResult> Index(Order order)
        {
            var cart = SessionHelper.GetObject<List<CartItem>>(
                HttpContext, CartSession)
                ?? new List<CartItem>();

            if (!cart.Any())
                return RedirectToAction("Index", "Cart");

            if (ModelState.IsValid)
            {
                var user = await _userManager.GetUserAsync(User);

                order.UserId = user!.Id;
                order.TotalAmount = cart.Sum(x => x.Price * x.Quantity);
                order.OrderDate = DateTime.Now;
                order.Status = OrderStatus.Pending;  // ← set default

                _context.Orders.Add(order);
                await _context.SaveChangesAsync();

                foreach (var item in cart)
                {
                    var product = await _context.Products
                        .FindAsync(item.ProductId);

                    if (product != null)
                    {
                        if (product.Quantity < item.Quantity)
                        {
                            ModelState.AddModelError("",
                                $"Not enough stock for {product.Name}");
                            return View(order);
                        }

                        product.Quantity -= item.Quantity;

                        _context.OrderItems.Add(new OrderItem
                        {
                            OrderId = order.Id,
                            ProductId = product.Id,
                            Quantity = item.Quantity,
                            Price = item.Price
                        });
                    }
                }

                await _context.SaveChangesAsync();
                HttpContext.Session.Remove(CartSession);

                // NOTIFY USER — order placed
                _context.Notifications.Add(new Notification
                {
                    UserId = user.Id,
                    Title = "Order Placed Successfully",
                    Message = $"Your order #{order.Id} has been placed and is now pending.",
                    IsRead = false,
                    CreatedAt = DateTime.Now
                });
                await _context.SaveChangesAsync();

                return RedirectToAction("Success",
                    new { orderId = order.Id });
            }

            return View(order);
        }

        public IActionResult Success(int orderId)
        {
            ViewBag.OrderId = orderId;
            return View();
        }

        // =========================================
        // MY ORDERS
        // =========================================

        public async Task<IActionResult> MyOrders()
        {
            var user = await _userManager.GetUserAsync(User);

            var orders = await _context.Orders
                .Include(x => x.OrderItems)
                    .ThenInclude(x => x.Product)
                .Where(x => x.UserId == user!.Id)
                .OrderByDescending(x => x.OrderDate)
                .ToListAsync();

            return View(orders);
        }

        // =========================================
        // ORDER DETAILS
        // =========================================

        public async Task<IActionResult> OrderDetails(int id)
        {
            var user = await _userManager.GetUserAsync(User);

            var order = await _context.Orders
                .Include(x => x.OrderItems)
                    .ThenInclude(x => x.Product)
                .FirstOrDefaultAsync(x =>
                    x.Id == id && x.UserId == user!.Id);

            if (order == null) return NotFound();

            return View(order);
        }

        // =========================================
        // CANCEL ORDER — user can only cancel Pending
        // =========================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelOrder(
            int orderId, string reason)
        {
            var user = await _userManager.GetUserAsync(User);

            var order = await _context.Orders
                .FirstOrDefaultAsync(x =>
                    x.Id == orderId && x.UserId == user!.Id);

            if (order == null) return NotFound();

            // USERS CAN ONLY CANCEL PENDING ORDERS
            if (order.Status != OrderStatus.Pending)
            {
                TempData["Error"] =
                    "You can only cancel orders that are still pending.";
                return RedirectToAction("MyOrders");
            }

            order.Status = OrderStatus.Cancelled;
            order.CancelReason = reason;
            order.LastUpdated = DateTime.Now;

            // NOTIFY USER
            _context.Notifications.Add(new Notification
            {
                UserId = user!.Id,
                Title = "Order Cancelled",
                Message = $"Your order #{order.Id} has been cancelled.",
                IsRead = false,
                CreatedAt = DateTime.Now
            });

            await _context.SaveChangesAsync();

            TempData["Success"] = "Order cancelled successfully.";
            return RedirectToAction("MyOrders");
        }
    }
}