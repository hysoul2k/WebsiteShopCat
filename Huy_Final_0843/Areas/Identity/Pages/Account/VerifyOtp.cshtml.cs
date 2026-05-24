#nullable disable
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Caching.Memory;
using Huy_Final_0843.Models;

namespace Huy_Final_0843.Areas.Identity.Pages.Account
{
    [AllowAnonymous]
    public class VerifyOtpModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IMemoryCache _cache;
        private readonly IEmailSender _emailSender;

        public VerifyOtpModel(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IMemoryCache cache,
            IEmailSender emailSender)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _cache = cache;
            _emailSender = emailSender;
        }

        [BindProperty(SupportsGet = true)]
        public string Email { get; set; }

        [BindProperty(SupportsGet = true)]
        public string ReturnUrl { get; set; }

        [BindProperty]
        [Required(ErrorMessage = "Vui lòng nhập mã OTP.")]
        [StringLength(6, MinimumLength = 6, ErrorMessage = "Mã OTP gồm 6 chữ số.")]
        public string Otp { get; set; }

        [TempData]
        public string StatusMessage { get; set; }

        public IActionResult OnGet()
        {
            if (string.IsNullOrEmpty(Email)) return RedirectToPage("./Register");
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid) return Page();

            var cached = _cache.Get<string>($"register_otp_{Email}");
            if (cached == null || cached != Otp)
            {
                ModelState.AddModelError(nameof(Otp), "Mã OTP không đúng hoặc đã hết hạn.");
                return Page();
            }

            var user = await _userManager.FindByEmailAsync(Email);
            if (user == null) return RedirectToPage("./Register");

            user.EmailConfirmed = true;
            await _userManager.UpdateAsync(user);
            _cache.Remove($"register_otp_{Email}");

            await _signInManager.SignInAsync(user, isPersistent: false);
            return LocalRedirect(ReturnUrl ?? Url.Content("~/"));
        }

        public async Task<IActionResult> OnPostResendAsync()
        {
            var user = await _userManager.FindByEmailAsync(Email);
            if (user == null) return RedirectToPage("./Register");

            var otp = new Random().Next(100000, 999999).ToString();
            _cache.Set($"register_otp_{Email}", otp, TimeSpan.FromMinutes(10));

            await _emailSender.SendEmailAsync(Email, "🐾 Xác nhận tài khoản Meow Garden",
                $@"<div style='font-family:sans-serif;max-width:480px;margin:auto;padding:32px;border:1px solid #e0e0e0;border-radius:12px;'>
                    <h2 style='color:#2D5016;'>Meow Garden 🐱</h2>
                    <p>Mã OTP mới xác nhận tài khoản của bạn:</p>
                    <div style='font-size:36px;font-weight:bold;letter-spacing:8px;color:#2D5016;text-align:center;padding:16px;background:#f0f4ec;border-radius:8px;'>{otp}</div>
                    <p style='color:#888;font-size:13px;margin-top:16px;'>Mã có hiệu lực trong <strong>10 phút</strong>.</p>
                </div>");

            StatusMessage = "Đã gửi lại mã OTP. Kiểm tra email của bạn.";
            return RedirectToPage(new { Email, ReturnUrl });
        }
    }
}
