using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Product_Management.Models;
using Product_Management.Services;

namespace Product_Management.Controllers
{
    public class ForgotPasswordController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IEmailService _emailService;

        public ForgotPasswordController(
            UserManager<ApplicationUser> userManager,
            IEmailService emailService)
        {
            _userManager = userManager;
            _emailService = emailService;
        }

        // =========================================
        // STEP 1 — SHOW VERIFY FORM
        // =========================================

        [HttpGet]
        public IActionResult Index() => View();

        // =========================================
        // STEP 2 — VERIFY EMAIL + NAME, SEND OTP
        // =========================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Verify(
            string email, string fullName)
        {
            if (string.IsNullOrWhiteSpace(email) ||
                string.IsNullOrWhiteSpace(fullName))
            {
                TempData["Error"] = "Please fill in all fields.";
                return View("Index");
            }

            var user = await _userManager
                .FindByEmailAsync(email.Trim());

            if (user == null ||
                !string.Equals(
                    user.FullName?.Trim(),
                    fullName.Trim(),
                    StringComparison.OrdinalIgnoreCase))
            {
                TempData["Error"] =
                    "No account found matching those details.";
                return View("Index");
            }

            // GENERATE 6-DIGIT OTP
            var otp = new Random()
                .Next(100000, 999999).ToString();

            // STORE IN SESSION (10 minutes)
            HttpContext.Session.SetString("OtpCode", otp);
            HttpContext.Session.SetString("OtpEmail", email.Trim());
            HttpContext.Session.SetString("OtpExpiry",
                DateTime.UtcNow.AddMinutes(10).ToString("o"));

            // BUILD EMAIL
            var htmlBody = $@"
<!DOCTYPE html>
<html>
<body style='font-family:Segoe UI,sans-serif;
             background:#f8fafc;margin:0;padding:0;'>
  <div style='max-width:480px;margin:40px auto;
              background:#fff;border-radius:16px;
              box-shadow:0 4px 20px rgba(0,0,0,0.08);
              overflow:hidden;'>

    <div style='background:linear-gradient(135deg,#0f172a,#1e3a5f);
                padding:32px;text-align:center;'>
      <h2 style='color:#fff;margin:0;font-weight:900;
                 font-size:1.5rem;'>PSskill</h2>
      <p style='color:#94a3b8;margin:6px 0 0;
                font-size:0.88rem;'>
        Password Reset Request
      </p>
    </div>

    <div style='padding:36px 32px;'>
      <p style='color:#374151;font-size:0.95rem;
                margin-bottom:8px;'>
        Hi <strong>{user.FullName}</strong>,
      </p>
      <p style='color:#6b7280;font-size:0.88rem;
                line-height:1.6;margin-bottom:24px;'>
        We received a request to reset your PSskill password.
        Use the code below to continue. This code expires in
        <strong>10 minutes</strong>.
      </p>

      <div style='background:#f1f5f9;border-radius:12px;
                  padding:24px;text-align:center;
                  margin-bottom:24px;'>
        <p style='color:#6b7280;font-size:0.78rem;
                  text-transform:uppercase;
                  letter-spacing:0.08em;margin:0 0 10px;'>
          Your Reset Code
        </p>
        <div style='font-size:2.4rem;font-weight:900;
                    color:#0f172a;letter-spacing:0.18em;'>
          {otp}
        </div>
      </div>

      <p style='color:#9ca3af;font-size:0.80rem;
                line-height:1.6;'>
        If you did not request a password reset, you can
        safely ignore this email.
      </p>
    </div>

    <div style='background:#f8fafc;padding:20px 32px;
                text-align:center;
                border-top:1px solid #e2e8f0;'>
      <p style='color:#9ca3af;font-size:0.75rem;margin:0;'>
        &copy; 2026 PSskill. All rights reserved.
      </p>
    </div>

  </div>
</body>
</html>";

            try
            {
                await _emailService.SendAsync(
                    toEmail: email.Trim(),
                    toName: user.FullName ?? email,
                    subject: "PSskill — Your Password Reset Code",
                    htmlBody: htmlBody
                );
            }
            catch (Exception ex)
            {
                TempData["Error"] =
                    "Failed to send email. Please try again. " +
                    ex.Message;
                return View("Index");
            }

            TempData["OtpSentTo"] = email.Trim();
            return RedirectToAction("VerifyOtp");
        }

        // =========================================
        // STEP 3 — SHOW OTP ENTRY FORM
        // =========================================

        [HttpGet]
        public IActionResult VerifyOtp()
        {
            ViewBag.SentTo = TempData["OtpSentTo"]?.ToString();
            return View();
        }

        // =========================================
        // STEP 4 — VALIDATE OTP
        // =========================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult VerifyOtp(string otp)
        {
            var storedOtp =
                HttpContext.Session.GetString("OtpCode");
            var expiryStr =
                HttpContext.Session.GetString("OtpExpiry");

            if (string.IsNullOrEmpty(storedOtp) ||
                string.IsNullOrEmpty(expiryStr))
            {
                TempData["Error"] =
                    "Session expired. Please start again.";
                return RedirectToAction("Index");
            }

            var expiry = DateTime.Parse(expiryStr, null,
                System.Globalization.DateTimeStyles
                    .RoundtripKind);

            if (DateTime.UtcNow > expiry)
            {
                TempData["Error"] =
                    "Your code has expired. " +
                    "Please request a new one.";
                HttpContext.Session.Remove("OtpCode");
                HttpContext.Session.Remove("OtpEmail");
                HttpContext.Session.Remove("OtpExpiry");
                return RedirectToAction("Index");
            }

            if (otp?.Trim() != storedOtp)
            {
                TempData["Error"] =
                    "Invalid code. Please try again.";
                return View();
            }

            // OTP VALID
            HttpContext.Session.SetString("OtpVerified", "true");
            return RedirectToAction("ResetPassword");
        }

        // =========================================
        // STEP 5 — SHOW RESET PASSWORD FORM
        // =========================================

        [HttpGet]
        public IActionResult ResetPassword()
        {
            if (HttpContext.Session.GetString("OtpVerified")
                != "true")
            {
                TempData["Error"] =
                    "Please verify your code first.";
                return RedirectToAction("Index");
            }

            return View();
        }

        // =========================================
        // STEP 6 — SAVE NEW PASSWORD
        // =========================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(
            string newPassword, string confirmPassword)
        {
            if (HttpContext.Session.GetString("OtpVerified")
                != "true")
            {
                TempData["Error"] =
                    "Session expired. Please start again.";
                return RedirectToAction("Index");
            }

            if (string.IsNullOrWhiteSpace(newPassword) ||
                string.IsNullOrWhiteSpace(confirmPassword))
            {
                TempData["Error"] = "Please fill in all fields.";
                return View();
            }

            if (newPassword != confirmPassword)
            {
                TempData["Error"] = "Passwords do not match.";
                return View();
            }

            if (newPassword.Length < 6)
            {
                TempData["Error"] =
                    "Password must be at least 6 characters.";
                return View();
            }

            var email = HttpContext.Session
                .GetString("OtpEmail");

            if (string.IsNullOrEmpty(email))
            {
                TempData["Error"] =
                    "Session expired. Please start again.";
                return RedirectToAction("Index");
            }

            var user = await _userManager
                .FindByEmailAsync(email);

            if (user == null)
            {
                TempData["Error"] = "User not found.";
                return RedirectToAction("Index");
            }

            var token = await _userManager
                .GeneratePasswordResetTokenAsync(user);

            var result = await _userManager
                .ResetPasswordAsync(user, token, newPassword);

            if (!result.Succeeded)
            {
                TempData["Error"] = string.Join(" ",
                    result.Errors.Select(e => e.Description));
                return View();
            }

            // CLEAR SESSION
            HttpContext.Session.Remove("OtpCode");
            HttpContext.Session.Remove("OtpEmail");
            HttpContext.Session.Remove("OtpExpiry");
            HttpContext.Session.Remove("OtpVerified");

            TempData["Success"] =
                "Password reset successfully! Please log in.";

            return RedirectToPage("/Account/Login",
                new { area = "Identity" });
        }

        // =========================================
        // RESEND OTP
        // =========================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResendOtp()
        {
            var email = HttpContext.Session
                .GetString("OtpEmail");

            if (string.IsNullOrEmpty(email))
            {
                TempData["Error"] =
                    "Session expired. Please start again.";
                return RedirectToAction("Index");
            }

            var user = await _userManager
                .FindByEmailAsync(email);

            if (user == null)
                return RedirectToAction("Index");

            var otp = new Random()
                .Next(100000, 999999).ToString();

            HttpContext.Session.SetString("OtpCode", otp);
            HttpContext.Session.SetString("OtpExpiry",
                DateTime.UtcNow.AddMinutes(10).ToString("o"));

            var htmlBody = $@"
<div style='font-family:Segoe UI,sans-serif;
            max-width:480px;margin:0 auto;
            background:#fff;border-radius:16px;
            padding:32px;'>
  <h2 style='color:#0f172a;'>New Reset Code</h2>
  <p style='color:#6b7280;'>
    Hi <strong>{user.FullName}</strong>, here is your new code:
  </p>
  <div style='background:#f1f5f9;border-radius:12px;
              padding:24px;text-align:center;margin:20px 0;'>
    <div style='font-size:2.4rem;font-weight:900;
                color:#0f172a;letter-spacing:0.18em;'>
      {otp}
    </div>
  </div>
  <p style='color:#9ca3af;font-size:0.82rem;'>
    This code expires in 10 minutes.
  </p>
</div>";

            try
            {
                await _emailService.SendAsync(
                    toEmail: email,
                    toName: user.FullName ?? email,
                    subject: "PSskill — New Reset Code",
                    htmlBody: htmlBody
                );
                TempData["Success"] =
                    "A new code has been sent to your email.";
            }
            catch
            {
                TempData["Error"] =
                    "Failed to send email. Please try again.";
            }

            return RedirectToAction("VerifyOtp");
        }
    }
}