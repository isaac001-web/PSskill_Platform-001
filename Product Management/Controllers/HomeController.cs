// Import MVC
using Microsoft.AspNetCore.Mvc;
using Product_Management.Data;
// Import Entity Framework Core
using Microsoft.EntityFrameworkCore;

// Import database context
using Product_Management.Models;

namespace Product_Management.Controllers
{
    // Home Controller (Storefront)
    public class HomeController : Controller
    {
        // Database context variable
        private readonly ApplicationDbContext _context;

        // Constructor
        public HomeController(ApplicationDbContext context)
        {
            _context = context;
        }

        // =========================================
        // HOME PAGE (PRODUCTS + CATEGORY FILTER)
        // =========================================

        public async Task<IActionResult> Index(int? categoryId)
        {
            // Load all categories (for dropdown filter)
            var categories = await _context.Categories.ToListAsync();

            // Start product query (not executed yet)
            var productsQuery = _context.Products
                .Include(p => p.Category)
                 .Include(p => p.Reviews)  // ← ADD THIS
                .AsQueryable();

            // FILTER BY CATEGORY (if selected)
            if (categoryId != null)
            {
                productsQuery = productsQuery
                    .Where(p => p.CategoryId == categoryId);
            }

            // Execute query
            var products = await productsQuery.ToListAsync();

            // Send data to View
            ViewBag.Categories = categories;
            ViewBag.SelectedCategory = categoryId;

            return View(products);
        }
    }
}