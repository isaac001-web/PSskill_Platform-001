using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Product_Management.Data;
using Product_Management.Models;

namespace Product_Management.Controllers
{
    [Authorize]
    public class MessageController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public MessageController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // =========================================
        // INBOX — shows all messages
        // =========================================

        public async Task<IActionResult> Inbox()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            var admins = await _userManager.GetUsersInRoleAsync("Admin");
            var admin = admins.FirstOrDefault();

            ViewBag.Admin = admin;

            // ONLY show messages where this user is involved
            // AND exclude messages sent by admin to themselves
            var messages = await _context.Messages
                .Include(x => x.Sender)
                .Include(x => x.Receiver)
                .Where(x =>
                    x.SenderId == user.Id ||
                    x.ReceiverId == user.Id)
                .Where(x =>
                    // exclude broadcast messages showing as "sent by admin"
                    // only show messages relevant to this user's conversation
                    x.SenderId != x.ReceiverId)
                .OrderByDescending(x => x.SentAt)
                .ToListAsync();

            return View(messages);
        }

        // =========================================
        // CONVERSATION — chat between user & admin
        // =========================================

        public async Task<IActionResult> Conversation()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            // GET ADMIN
            var admins = await _userManager.GetUsersInRoleAsync("Admin");
            var admin = admins.FirstOrDefault();
            if (admin == null)
            {
                TempData["Error"] = "No admin found.";
                return RedirectToAction("Inbox");
            }

            ViewBag.Admin = admin;

            // GET CONVERSATION — only between this user and admin
            var messages = await _context.Messages
                .Include(x => x.Sender)
                .Include(x => x.Receiver)
                .Where(x =>
                    (x.SenderId == user.Id && x.ReceiverId == admin.Id) ||
                    (x.SenderId == admin.Id && x.ReceiverId == user.Id))
                .OrderBy(x => x.SentAt)
                .ToListAsync();

            // MARK ADMIN MESSAGES AS READ
            var unread = messages
                .Where(x => x.SenderId == admin.Id && !x.IsRead)
                .ToList();

            foreach (var msg in unread)
                msg.IsRead = true;

            if (unread.Any())
                await _context.SaveChangesAsync();

            return View(messages);
        }

        // =========================================
        // SEND — user sends message to admin
        // =========================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Send(string receiverId, string content)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            if (string.IsNullOrWhiteSpace(content))
            {
                TempData["Error"] = "Message cannot be empty.";
                return RedirectToAction("Conversation");
            }

            var message = new Message
            {
                UserId = user.Id,
                Subject = "Chat",
                Content = content,
                SenderId = user.Id,
                ReceiverId = receiverId,
                SentAt = DateTime.Now,
                IsRead = false
            };
            _context.Messages.Add(message);

            // NOTIFY ADMIN
            var notification = new Notification
            {
                UserId = receiverId,
                Title = "New Message",
                Message = $"{user.FullName ?? user.Email} sent you a message.",
                IsRead = false,
                CreatedAt = DateTime.Now
            };
            _context.Notifications.Add(notification);

            await _context.SaveChangesAsync();

            return RedirectToAction("Conversation");
        }

        // =========================================
        // CONTACT ADMIN FORM
        // =========================================

        [HttpGet]
        public IActionResult ContactAdmin()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ContactAdmin(
            string subject, string content)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            if (string.IsNullOrWhiteSpace(subject) ||
                string.IsNullOrWhiteSpace(content))
            {
                TempData["Error"] = "All fields are required.";
                return RedirectToAction("ContactAdmin");
            }

            var admins = await _userManager.GetUsersInRoleAsync("Admin");
            var adminUser = admins.FirstOrDefault();

            if (adminUser == null)
            {
                TempData["Error"] = "Admin account not found.";
                return RedirectToAction("ContactAdmin");
            }

            var message = new Message
            {
                UserId = user.Id,
                Subject = subject,
                Content = content,
                SenderId = user.Id,
                ReceiverId = adminUser.Id,
                SentAt = DateTime.Now,
                IsRead = false
            };
            _context.Messages.Add(message);

            var notification = new Notification
            {
                UserId = adminUser.Id,
                Title = "New User Message",
                Message = $"{user.FullName ?? user.Email} sent a message.",
                IsRead = false,
                CreatedAt = DateTime.Now
            };
            _context.Notifications.Add(notification);

            await _context.SaveChangesAsync();

            TempData["Success"] = "Message sent successfully.";
            return RedirectToAction("Conversation");
        }
    }
}