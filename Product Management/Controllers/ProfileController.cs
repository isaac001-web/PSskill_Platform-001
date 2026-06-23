using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Product_Management.Models;

namespace Product_Management.Controllers
{
    [Authorize]
    public class ProfileController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;

        private readonly IWebHostEnvironment _webHostEnvironment;

        public ProfileController(
            UserManager<ApplicationUser> userManager,
            IWebHostEnvironment webHostEnvironment)
        {
            _userManager = userManager;

            _webHostEnvironment = webHostEnvironment;
        }

        // =========================================
        // PROFILE PAGE
        // =========================================

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            // Get logged-in user
            var user =
                await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return RedirectToAction(
                    "Login",
                    "Account"
                );
            }

            return View(user);
        }

        // =========================================
        // UPDATE PROFILE
        // =========================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Update(
            ApplicationUser model,
            IFormFile? imageFile)
        {
            // Get logged-in user
            var user =
                await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return NotFound();
            }

            // =====================================
            // UPDATE USER DETAILS
            // =====================================

            user.FullName =
                model.FullName ?? string.Empty;

            user.PhoneNumber =
                model.PhoneNumber;

            user.Address =
                model.Address;

            user.City =
                model.City;

            user.State =
                model.State;

            user.Country =
                model.Country;

            // =====================================
            // IMAGE UPLOAD
            // =====================================

            if (imageFile != null &&
                imageFile.Length > 0)
            {
                // Create unique filename
                string fileName =
                    Guid.NewGuid().ToString() +
                    Path.GetExtension(imageFile.FileName);

                // Folder path
                string uploadFolder =
                    Path.Combine(
                        _webHostEnvironment.WebRootPath,
                        "profiles"
                    );

                // Create folder if it doesn't exist
                if (!Directory.Exists(uploadFolder))
                {
                    Directory.CreateDirectory(uploadFolder);
                }

                // Delete old image if exists
                if (!string.IsNullOrEmpty(user.ProfileImage))
                {
                    string oldImagePath =
                        Path.Combine(
                            uploadFolder,
                            user.ProfileImage
                        );

                    if (System.IO.File.Exists(oldImagePath))
                    {
                        System.IO.File.Delete(oldImagePath);
                    }
                }

                // Full file path
                string filePath =
                    Path.Combine(
                        uploadFolder,
                        fileName
                    );

                // Save image
                using (var stream =
                    new FileStream(filePath, FileMode.Create))
                {
                    await imageFile.CopyToAsync(stream);
                }

                // Save filename to database
                user.ProfileImage = fileName;
            }

            // =====================================
            // SAVE USER
            // =====================================

            var result =
                await _userManager.UpdateAsync(user);

            if (result.Succeeded)
            {
                TempData["Success"] =
                    "Profile Updated Successfully";
            }
            else
            {
                TempData["Error"] =
                    "Failed to update profile";
            }

            return RedirectToAction("Index");
        }
    }
}