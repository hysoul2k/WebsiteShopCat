using Huy_Final_0843.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.IO;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity.UI.Services;
using Huy_Final_0843.Helpers;

namespace Huy_Final_0843.Controllers
{
    [Authorize]
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IWebHostEnvironment _hostEnvironment;
        private readonly IEmailSender _emailSender;

        public AccountController(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, IWebHostEnvironment hostEnvironment, IEmailSender emailSender)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _hostEnvironment = hostEnvironment;
            _emailSender = emailSender;
        }

        // Trang Cá nhân (Profile)
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            return View(user);
        }

        // Cập nhật Thông tin
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateProfile(string fullName, string? address, string? phoneNumber, IFormFile? avatarFile)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            user.FullName = fullName;
            user.Address = address;
            user.PhoneNumber = phoneNumber;

            // Xử lý Upload Avatar
            if (avatarFile != null && avatarFile.Length > 0)
            {
                string wwwRootPath = _hostEnvironment.WebRootPath;
                string fileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(avatarFile.FileName);
                string path = Path.Combine(wwwRootPath, @"images\avatars");

                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }

                // Xóa ảnh cũ nếu có (không xóa ảnh mặc định nếu bạn có)
                if (!string.IsNullOrEmpty(user.ProfileImageUrl))
                {
                    var oldImagePath = Path.Combine(wwwRootPath, user.ProfileImageUrl.TrimStart('\\'));
                    if (System.IO.File.Exists(oldImagePath))
                    {
                        System.IO.File.Delete(oldImagePath);
                    }
                }

                using (var fileStream = new FileStream(Path.Combine(path, fileName), FileMode.Create))
                {
                    await avatarFile.CopyToAsync(fileStream);
                }
                user.ProfileImageUrl = @"\images\avatars\" + fileName;
            }

            var result = await _userManager.UpdateAsync(user);
            if (result.Succeeded)
            {
                // QUAN TRỌNG: Làm mới đăng nhập để cập nhật Claims (Lấy tên mới hiển thị trên Navbar ngay)
                await _signInManager.RefreshSignInAsync(user);
                TempData["Message"] = "Cập nhật thông tin cá nhân thành công!";
                return RedirectToAction(nameof(Index));
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error.Description);
            }

            return View("Index", user);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RequestEmailChange(string newEmail)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            var otp = new Random().Next(100000, 999999).ToString();
            HttpContext.Session.SetString("EmailChangeOTP", otp);
            HttpContext.Session.SetString("NewEmail", newEmail);
            HttpContext.Session.SetString("OTPExpiry", DateTime.Now.AddMinutes(5).ToString());

            var subject = "Mã xác thực đổi Email - Meow Garden";
            var message = EmailTemplateHelper.GetOTPTemplate(otp);

            await _emailSender.SendEmailAsync(newEmail, subject, message);

            return RedirectToAction(nameof(VerifyOTP));
        }

        [HttpGet]
        public IActionResult VerifyOTP()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VerifyOTP(string otp)
        {
            var sessionOTP = HttpContext.Session.GetString("EmailChangeOTP");
            var newEmail = HttpContext.Session.GetString("NewEmail");
            var expiryStr = HttpContext.Session.GetString("OTPExpiry");

            if (sessionOTP == null || newEmail == null || expiryStr == null)
            {
                TempData["Error"] = "Yêu cầu đã hết hạn hoặc không tồn tại.";
                return RedirectToAction(nameof(Index));
            }

            var expiry = DateTime.Parse(expiryStr);
            if (DateTime.Now > expiry)
            {
                TempData["Error"] = "Mã OTP đã hết hạn.";
                return RedirectToAction(nameof(Index));
            }

            if (otp == sessionOTP)
            {
                var user = await _userManager.GetUserAsync(User);
                if (user != null)
                {
                    var token = await _userManager.GenerateChangeEmailTokenAsync(user, newEmail);
                    var result = await _userManager.ChangeEmailAsync(user, newEmail, token);
                    if (result.Succeeded)
                    {
                        await _userManager.SetUserNameAsync(user, newEmail);
                        await _signInManager.RefreshSignInAsync(user);
                        TempData["Message"] = "Đã cập nhật Email thành công!";
                        HttpContext.Session.Remove("EmailChangeOTP");
                        HttpContext.Session.Remove("NewEmail");
                        HttpContext.Session.Remove("OTPExpiry");
                        return RedirectToAction(nameof(Index));
                    }
                }
            }

            ModelState.AddModelError("", "Mã OTP không chính xác.");
            return View();
        }

        // --- QUÊN MẬT KHẨU (FORGOT PASSWORD) ---
        [AllowAnonymous]
        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(string email)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                // Không báo là email không tồn tại để tránh rò rỉ thông tin
                TempData["Message"] = "Nếu email tồn tại, mã xác thực đã được gửi đi.";
                return RedirectToAction(nameof(ResetPassword), new { email = email });
            }

            var otp = new Random().Next(100000, 999999).ToString();
            var resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);

            HttpContext.Session.SetString("ResetOTP", otp);
            HttpContext.Session.SetString("ResetEmail", email);
            HttpContext.Session.SetString("ResetToken", resetToken);
            HttpContext.Session.SetString("ResetOTPExpiry", DateTime.Now.AddMinutes(10).ToString());

            var subject = "Khôi phục mật khẩu - Meow Garden";
            var message = EmailTemplateHelper.GetForgotPasswordOTPTemplate(otp);

            await _emailSender.SendEmailAsync(email, subject, message);

            TempData["Message"] = "Mã xác thực đã được gửi tới email của bạn.";
            return RedirectToAction(nameof(ResetPassword), new { email = email });
        }

        [AllowAnonymous]
        [HttpGet]
        public IActionResult ResetPassword(string email)
        {
            return View(new ResetPasswordViewModel { Email = email });
        }

        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var sessionOTP = HttpContext.Session.GetString("ResetOTP");
            var sessionEmail = HttpContext.Session.GetString("ResetEmail");
            var sessionToken = HttpContext.Session.GetString("ResetToken");
            var expiryStr = HttpContext.Session.GetString("ResetOTPExpiry");

            if (sessionOTP == null || sessionEmail != model.Email || expiryStr == null || sessionToken == null)
            {
                TempData["Error"] = "Yêu cầu đã hết hạn hoặc không hợp lệ.";
                return RedirectToAction(nameof(ForgotPassword));
            }

            var expiry = DateTime.Parse(expiryStr);
            if (DateTime.Now > expiry)
            {
                TempData["Error"] = "Mã xác thực đã hết hạn.";
                return RedirectToAction(nameof(ForgotPassword));
            }

            if (model.OTP != sessionOTP)
            {
                ModelState.AddModelError("", "Mã xác thực không chính xác.");
                return View(model);
            }

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user != null)
            {
                var result = await _userManager.ResetPasswordAsync(user, sessionToken, model.NewPassword);
                if (result.Succeeded)
                {
                    TempData["Message"] = "Đặt lại mật khẩu thành công! Vui lòng đăng nhập lại.";
                    HttpContext.Session.Remove("ResetOTP");
                    HttpContext.Session.Remove("ResetEmail");
                    HttpContext.Session.Remove("ResetToken");
                    HttpContext.Session.Remove("ResetOTPExpiry");
                    return RedirectToPage("/Account/Login", new { area = "Identity" });
                }
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError("", error.Description);
                }
            }

            return View(model);
        }
    }
}
