using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Product_Management.Data;
using Product_Management.Models;

namespace Product_Management.Controllers
{
    [Authorize]
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;  // ← FIXED

        public DashboardController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)  // ← FIXED
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);

            var orders = await _context.Orders
                .Where(x => x.UserId == user.Id)
                .ToListAsync();

            var totalSpent = orders.Sum(x => x.TotalAmount);

            ViewBag.TotalOrders = orders.Count;
            ViewBag.TotalSpent = totalSpent;
            ViewBag.UserName = user.FullName;

            return View(orders.OrderByDescending(x => x.OrderDate).Take(5));
        }
    }
}