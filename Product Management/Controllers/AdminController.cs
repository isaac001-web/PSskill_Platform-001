using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Product_Management.Models;
using Product_Management.Data;

namespace Product_Management.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public AdminController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // =========================================
        // DASHBOARD
        // =========================================

        public async Task<IActionResult> Index()
        {
            ViewBag.TotalProducts =
                await _context.Products.CountAsync();
            ViewBag.TotalOrders =
                await _context.Orders.CountAsync();
            ViewBag.TotalUsers =
                await _userManager.Users.CountAsync();
            ViewBag.TotalRevenue =
                await _context.Orders
                .SumAsync(x => (decimal?)x.TotalAmount) ?? 0;
            ViewBag.TotalMessages =
                await _context.Messages.CountAsync();

            var lowStockProducts = await _context.Products
                .Where(x => x.Quantity < 5)
                .OrderBy(x => x.Quantity)
                .ToListAsync();

            return View(lowStockProducts);
        }

        // =========================================
        // ADMIN INBOX
        // =========================================

        public async Task<IActionResult> Inbox()
        {
            var admin = await _userManager.GetUserAsync(User);
            if (admin == null) return NotFound();

            var userIds = await _context.Messages
                .Where(x =>
                    (x.SenderId == admin.Id || x.ReceiverId == admin.Id)
                    && x.SenderId != admin.Id
                    || x.ReceiverId == admin.Id)
                .Select(x =>
                    x.SenderId == admin.Id
                    ? x.ReceiverId
                    : x.SenderId)
                .Where(x => x != admin.Id)
                .Distinct()
                .ToListAsync();

            var users = new List<ApplicationUser>();
            foreach (var uid in userIds)
            {
                var u = await _userManager.FindByIdAsync(uid);
                if (u != null &&
                    !await _userManager.IsInRoleAsync(u, "Admin"))
                    users.Add(u);
            }

            var unreadCounts = new Dictionary<string, int>();
            foreach (var u in users)
            {
                unreadCounts[u.Id] = await _context.Messages
                    .CountAsync(x =>
                        x.SenderId == u.Id &&
                        x.ReceiverId == admin.Id &&
                        !x.IsRead);
            }

            ViewBag.UnreadCounts = unreadCounts;

            return View(users);
        }

        // =========================================
        // CONVERSATION
        // =========================================

        public async Task<IActionResult> Conversation(string userId)
        {
            if (string.IsNullOrEmpty(userId))
                return NotFound();

            var admin = await _userManager.GetUserAsync(User);
            if (admin == null) return NotFound();

            var user = await _userManager.Users
                .FirstOrDefaultAsync(x => x.Id == userId);
            if (user == null) return NotFound();

            ViewBag.User = user;

            var messages = await _context.Messages
                .Include(x => x.Sender)
                .Include(x => x.Receiver)
                .Where(x =>
                    (x.SenderId == admin.Id &&
                     x.ReceiverId == user.Id) ||
                    (x.SenderId == user.Id &&
                     x.ReceiverId == admin.Id))
                .OrderBy(x => x.SentAt)
                .ToListAsync();

            var unread = messages
                .Where(x => x.SenderId == user.Id && !x.IsRead)
                .ToList();

            foreach (var msg in unread)
                msg.IsRead = true;

            if (unread.Any())
                await _context.SaveChangesAsync();

            return View(messages);
        }

        // =========================================
        // REPLY
        // =========================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reply(
            string receiverId, string content)
        {
            var admin = await _userManager.GetUserAsync(User);
            if (admin == null) return NotFound();

            if (string.IsNullOrWhiteSpace(content))
            {
                TempData["Error"] = "Message cannot be empty.";
                return RedirectToAction("Conversation",
                    new { userId = receiverId });
            }

            var receiver = await _userManager.Users
                .FirstOrDefaultAsync(x => x.Id == receiverId);
            if (receiver == null) return NotFound();

            _context.Messages.Add(new Message
            {
                Subject = "Admin Reply",
                Content = content,
                SenderId = admin.Id,
                ReceiverId = receiverId,
                UserId = receiverId,
                SentAt = DateTime.Now,
                IsRead = false
            });

            _context.Notifications.Add(new Notification
            {
                UserId = receiverId,
                Title = "New Message from Admin",
                Message = "Admin replied to your message.",
                IsRead = false,
                CreatedAt = DateTime.Now
            });

            await _context.SaveChangesAsync();

            TempData["Success"] = "Reply sent.";
            return RedirectToAction("Conversation",
                new { userId = receiverId });
        }

        // =========================================
        // BROADCAST GET  ← THIS WAS MISSING
        // =========================================

        [HttpGet]
        public IActionResult Broadcast()
        {
            return View();
        }




        // =========================================
        // ALL ORDERS
        // =========================================

        public async Task<IActionResult> Orders(string status = "")
        {
            var query = _context.Orders
                .Include(x => x.User)
                .Include(x => x.OrderItems).ThenInclude(oi => oi.Product)
                .AsQueryable();

            if (!string.IsNullOrEmpty(status) &&
                Enum.TryParse<OrderStatus>(status, out var parsedStatus))
            {
                query = query.Where(x => x.Status == parsedStatus);
            }

            var orders = await query
               // .OrderByDescending(x => x.OrderDate)
               .OrderBy(x => x.OrderDate.Year)
                .ToListAsync();

            ViewBag.CurrentStatus = status;
            return View(orders);
        }

        // =========================================
        // ORDER DETAILS — ADMIN
        // =========================================

        public async Task<IActionResult> OrderDetails(int id)
        {
            var order = await _context.Orders
                .Include(x => x.User)
                .Include(x => x.OrderItems)
                    .ThenInclude(x => x.Product)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (order == null) return NotFound();

            return View(order);
        }

        // =========================================
        // UPDATE ORDER STATUS — ADMIN
        // =========================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateOrderStatus(
            int orderId, OrderStatus status, string? reason)
        {
            var order = await _context.Orders
                .FirstOrDefaultAsync(x => x.Id == orderId);

            if (order == null) return NotFound();

            var oldStatus = order.Status;
            order.Status = status;
            order.LastUpdated = DateTime.Now;

            if (status == OrderStatus.Cancelled && !string.IsNullOrEmpty(reason))
                order.CancelReason = reason;

            // NOTIFY USER of status change
            var statusMessages = new Dictionary<OrderStatus, string>
    {
        { OrderStatus.Processing,
            $"Your order #{order.Id} is now being processed." },
        { OrderStatus.Shipped,
            $"Your order #{order.Id} has been shipped and is on the way!" },
        { OrderStatus.Delivered,
            $"Your order #{order.Id} has been delivered. Enjoy!" },
        { OrderStatus.Cancelled,
            $"Your order #{order.Id} has been cancelled by admin. Reason: {reason ?? "N/A"}" }
    };

            if (statusMessages.ContainsKey(status))
            {
                _context.Notifications.Add(new Notification
                {
                    UserId = order.UserId,
                    Title = $"Order #{order.Id} — {status}",
                    Message = statusMessages[status],
                    IsRead = false,
                    CreatedAt = DateTime.Now
                });
            }

            await _context.SaveChangesAsync();

            TempData["Success"] =
                $"Order #{orderId} status updated to {status}.";
            return RedirectToAction("OrderDetails", new { id = orderId });
        }

        // =========================================
        // USER MANAGEMENT
        // =========================================

        public async Task<IActionResult> Users(string search = "")
        {
            var users = _userManager.Users.AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                users = users.Where(x =>
                    x.FullName.Contains(search) ||
                    x.Email.Contains(search));
            }

            var userList = await users.ToListAsync();

            // GET ROLES FOR EACH USER
            var userRoles = new Dictionary<string, IList<string>>();
            foreach (var u in userList)
            {
                userRoles[u.Id] = await _userManager.GetRolesAsync(u);
            }

            ViewBag.UserRoles = userRoles;
            ViewBag.Search = search;

            return View(userList);
        }

        // =========================================
        // USER DETAILS — ADMIN
        // =========================================

        public async Task<IActionResult> UserDetails(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            var roles = await _userManager.GetRolesAsync(user);
            var orders = await _context.Orders
                .Where(x => x.UserId == id)
                .OrderByDescending(x => x.OrderDate)
                .ToListAsync();

            ViewBag.Roles = roles;
            ViewBag.Orders = orders;
            ViewBag.AllRoles = new List<string> { "Admin", "User" };

            return View(user);
        }

        // =========================================
        // ASSIGN ROLE
        // =========================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignRole(
            string userId, string role)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound();

            if (!await _userManager.IsInRoleAsync(user, role))
            {
                await _userManager.AddToRoleAsync(user, role);
                TempData["Success"] =
                    $"Role '{role}' assigned to {user.FullName ?? user.Email}.";
            }
            else
            {
                TempData["Error"] = "User already has this role.";
            }

            return RedirectToAction("UserDetails", new { id = userId });
        }

        // =========================================
        // REMOVE ROLE
        // =========================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveRole(
            string userId, string role)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound();

            if (await _userManager.IsInRoleAsync(user, role))
            {
                await _userManager.RemoveFromRoleAsync(user, role);
                TempData["Success"] =
                    $"Role '{role}' removed from {user.FullName ?? user.Email}.";
            }

            return RedirectToAction("UserDetails", new { id = userId });
        }

        // =========================================
        // DELETE USER
        // =========================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteUser(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound();

            // PREVENT DELETING SELF
            var currentAdmin = await _userManager.GetUserAsync(User);
            if (user.Id == currentAdmin?.Id)
            {
                TempData["Error"] = "You cannot delete your own account.";
                return RedirectToAction("Users");
            }

            await _userManager.DeleteAsync(user);
            TempData["Success"] =
                $"User {user.FullName ?? user.Email} deleted successfully.";

            return RedirectToAction("Users");
        }

        // =========================================
        // BROADCAST POST
        // =========================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Broadcast(
            string subject, string content)
        {
            var admin = await _userManager.GetUserAsync(User);
            if (admin == null) return NotFound();

            if (string.IsNullOrWhiteSpace(subject) ||
                string.IsNullOrWhiteSpace(content))
            {
                TempData["Error"] =
                    "Subject and message are required.";
                return View();
            }

            var allUsers = await _userManager.Users
                .Where(x => x.Id != admin.Id)
                .ToListAsync();

            var nonAdmins = new List<ApplicationUser>();
            foreach (var u in allUsers)
            {
                if (!await _userManager.IsInRoleAsync(u, "Admin"))
                    nonAdmins.Add(u);
            }

            if (!nonAdmins.Any())
            {
                TempData["Error"] =
                    "No users found to broadcast to.";
                return View();
            }

            foreach (var user in nonAdmins)
            {
                _context.Messages.Add(new Message
                {
                    Subject = subject,
                    Content = content,
                    SenderId = admin.Id,
                    ReceiverId = user.Id,
                    UserId = user.Id,
                    SentAt = DateTime.Now,
                    IsRead = false
                });

                _context.Notifications.Add(new Notification
                {
                    UserId = user.Id,
                    Title = subject,
                    Message = content,
                    IsRead = false,
                    CreatedAt = DateTime.Now
                });
            }

            await _context.SaveChangesAsync();

            TempData["Success"] =
                $"Broadcast sent to {nonAdmins.Count} users successfully.";

            return RedirectToAction("Inbox");
        }
    }
}