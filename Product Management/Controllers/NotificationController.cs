using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Product_Management.Models;
using Product_Management.Data;

namespace Product_Management.Controllers
{
    [Authorize]
    public class NotificationController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;  // ← FIXED

        public NotificationController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)  // ← FIXED
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            var notifications = await _context.Notifications
                .Where(x => x.UserId == user.Id)
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync();

            foreach (var item in notifications)
                item.IsRead = true;

            await _context.SaveChangesAsync();

            HttpContext.Session.SetInt32("NotificationCount", 0);

            return View(notifications);
        }

        public async Task<int> GetUnreadCount()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return 0;

            return await _context.Notifications
                .CountAsync(x => x.UserId == user.Id && !x.IsRead);
        }

        public async Task<IActionResult> MarkAsRead(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Index");

            var notification = await _context.Notifications
                .FirstOrDefaultAsync(x => x.Id == id && x.UserId == user.Id);

            if (notification != null)
            {
                notification.IsRead = true;
                await _context.SaveChangesAsync();
            }

            int unreadCount = await _context.Notifications
                .CountAsync(x => x.UserId == user.Id && !x.IsRead);

            HttpContext.Session.SetInt32("NotificationCount", unreadCount);

            return RedirectToAction("Index");
        }

        public async Task<IActionResult> Delete(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Index");

            var notification = await _context.Notifications
                .FirstOrDefaultAsync(x => x.Id == id && x.UserId == user.Id);

            if (notification != null)
            {
                _context.Notifications.Remove(notification);
                await _context.SaveChangesAsync();
            }

            int unreadCount = await _context.Notifications
                .CountAsync(x => x.UserId == user.Id && !x.IsRead);

            HttpContext.Session.SetInt32("NotificationCount", unreadCount);

            return RedirectToAction("Index");
        }
    }
}