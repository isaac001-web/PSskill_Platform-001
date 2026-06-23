using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Product_Management.Data;
using Product_Management.Models;

namespace Product_Management.Controllers
{
    //[Authorize(Roles = "Admin")]
    public class ProductsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly UserManager<ApplicationUser> _userManager; // ✅ ADDED

        // =========================================
        // CONSTRUCTOR
        // =========================================

        public ProductsController(
            ApplicationDbContext context,
            IWebHostEnvironment webHostEnvironment,
            UserManager<ApplicationUser> userManager) // ✅ ADDED
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
            _userManager = userManager; // ✅ ADDED
        }

        // =========================================
        // INDEX (SEARCH + PAGINATION)
        // =========================================
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Index(string search, int page = 1)
        {
            int pageSize = 5;

            var query = _context.Products
                .Include(p => p.Category)
                .AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(p => p.Name.Contains(search));
            }

            int totalProducts = await query.CountAsync();

            var products = await query
                .OrderBy(p => p.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling(totalProducts / (double)pageSize);
            ViewBag.Search = search;

            return View(products);
        }

        // =========================================
        // CREATE (GET)
        // =========================================
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public IActionResult Create()
        {
            ViewBag.Categories = _context.Categories.ToList();
            return View();
        }

        // =========================================
        // CREATE (POST)
        // =========================================
        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> Create(Product product, IFormFile imageFile)
        {
            if (ModelState.IsValid)
            {
                if (imageFile != null)
                {
                    string fileName =
                        Guid.NewGuid().ToString() +
                        Path.GetExtension(imageFile.FileName);

                    string uploadFolder =
                        Path.Combine(_webHostEnvironment.WebRootPath, "images");

                    string filePath =
                        Path.Combine(uploadFolder, fileName);

                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await imageFile.CopyToAsync(fileStream);
                    }

                    product.ImageUrl = fileName;
                }

                _context.Products.Add(product);
                await _context.SaveChangesAsync();

                return RedirectToAction("Index");
            }

            ViewBag.Categories = _context.Categories.ToList();
            return View(product);
        }

        // =========================================
        // DETAILS ✅ UPDATED
        // =========================================
       // [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Details(int id)
        {
            var product = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Reviews)          // ✅ ADDED
                    .ThenInclude(r => r.User)      // ✅ ADDED
                .FirstOrDefaultAsync(x => x.Id == id);

            if (product == null)
            {
                return NotFound();
            }

            return View(product);
        }

        // =========================================
        // EDIT (GET)
        // =========================================
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var product = await _context.Products.FindAsync(id);

            if (product == null)
            {
                return NotFound();
            }

            ViewBag.Categories = _context.Categories.ToList();
            return View(product);
        }

        // =========================================
        // EDIT (POST)
        // =========================================
        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Product product, IFormFile? imageFile)
        {
            if (id != product.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                var existingProduct = await _context.Products
                    .FirstOrDefaultAsync(p => p.Id == id);

                if (existingProduct == null)
                {
                    return NotFound();
                }

                existingProduct.Name = product.Name;
                existingProduct.Price = product.Price;
                existingProduct.Quantity = product.Quantity;
                existingProduct.CategoryId = product.CategoryId;

                if (imageFile != null && imageFile.Length > 0)
                {
                    string fileName =
                        Guid.NewGuid().ToString() +
                        Path.GetExtension(imageFile.FileName);

                    string uploadFolder =
                        Path.Combine(_webHostEnvironment.WebRootPath, "images");

                    if (!Directory.Exists(uploadFolder))
                    {
                        Directory.CreateDirectory(uploadFolder);
                    }

                    string filePath =
                        Path.Combine(uploadFolder, fileName);

                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await imageFile.CopyToAsync(fileStream);
                    }

                    existingProduct.ImageUrl = fileName;
                }

                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            ViewBag.Categories = _context.Categories.ToList();
            return View(product);
        }

        // =========================================
        // DELETE (GET)
        // =========================================
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var product = await _context.Products.FindAsync(id);

            if (product == null)
            {
                return NotFound();
            }

            return View(product);
        }

        // =========================================
        // DELETE (POST)
        // =========================================
        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var product = await _context.Products.FindAsync(id);

            if (product != null)
            {
                if (!string.IsNullOrEmpty(product.ImageUrl))
                {
                    string imagePath =
                        Path.Combine(_webHostEnvironment.WebRootPath, "images", product.ImageUrl);

                    if (System.IO.File.Exists(imagePath))
                    {
                        System.IO.File.Delete(imagePath);
                    }
                }

                _context.Products.Remove(product);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction("Index");
        }

        // =========================================
        // ADD REVIEW (POST) ✅ NEW
        // =========================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AllowAnonymous] // overrides the Admin-only restriction on the controller
        [Authorize]      // but still requires login
        public async Task<IActionResult> AddReview(int ProductId, int Rating, string Comment)
        {
            var product = await _context.Products.FindAsync(ProductId);

            if (product == null)
            {
                return NotFound();
            }

            var userId = _userManager.GetUserId(User);

            var review = new Review
            {
                ProductId = ProductId,
                Rating = Rating,
                Comment = Comment,
                UserId = userId,
                ReviewDate = DateTime.Now
            };

            _context.Reviews.Add(review);
            await _context.SaveChangesAsync();

            return RedirectToAction("Details", new { id = ProductId });
        }
    }
}