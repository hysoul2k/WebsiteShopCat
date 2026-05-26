#nullable disable
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Huy_Final_0843.Models;

namespace Huy_Final_0843.Areas.Identity.Pages.Account
{
    public class LoginModel : PageModel
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _db;
        private readonly ILogger<LoginModel> _logger;

        public LoginModel(
            SignInManager<ApplicationUser> signInManager,
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext db,
            ILogger<LoginModel> logger)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _db = db;
            _logger = logger;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public IList<AuthenticationScheme> ExternalLogins { get; set; }
        public string ReturnUrl { get; set; }

        [TempData]
        public string ErrorMessage { get; set; }

        // Truyền xuống View
        public bool IsLocked { get; set; }
        public bool IsPermanentlyLocked { get; set; }
        public int LockRemainingSeconds { get; set; }
        public int AttemptsRemaining { get; set; }
        public string LockMessage { get; set; }

        public class InputModel
        {
            [Required]
            [EmailAddress]
            public string Email { get; set; }

            [Required]
            [DataType(DataType.Password)]
            public string Password { get; set; }

            [Display(Name = "Remember me?")]
            public bool RememberMe { get; set; }
        }

        // Thời gian khóa theo số lần (null = khóa vĩnh viễn)
        private static TimeSpan? GetLockDuration(int totalLockCount) => totalLockCount switch
        {
            1 => TimeSpan.FromMinutes(5),
            2 => TimeSpan.FromMinutes(15),
            3 => TimeSpan.FromMinutes(30),
            4 => TimeSpan.FromHours(1),
            _ => null
        };

        private static string FormatDuration(TimeSpan? duration)
        {
            if (duration == null) return "vĩnh viễn";
            if (duration.Value.TotalHours >= 1) return $"{(int)duration.Value.TotalHours} giờ";
            return $"{(int)duration.Value.TotalMinutes} phút";
        }

        public async Task OnGetAsync(string returnUrl = null)
        {
            if (!string.IsNullOrEmpty(ErrorMessage))
                ModelState.AddModelError(string.Empty, ErrorMessage);

            returnUrl ??= Url.Content("~/");
            await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);
            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();
            ReturnUrl = returnUrl;
        }

        public async Task<IActionResult> OnPostAsync(string returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");
            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();

            if (!ModelState.IsValid) return Page();

            // ── TRANSACTION với UPDLOCK để tránh race condition ──
            await using var tx = await _db.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead);
            try
            {
                var normalizedEmail = Input.Email.Trim().ToUpperInvariant();

                // Pessimistic lock: đọc trực tiếp từ DB, không qua cache
                var user = await _db.Users
                    .FromSqlRaw("SELECT * FROM AspNetUsers WITH (UPDLOCK, ROWLOCK) WHERE NormalizedEmail = {0}", normalizedEmail)
                    .FirstOrDefaultAsync();

                if (user == null)
                {
                    ModelState.AddModelError(string.Empty, "Email hoặc mật khẩu không đúng.");
                    await tx.RollbackAsync();
                    return Page();
                }

                // ── KIỂM TRA KHÓA VĨNH VIỄN ──
                if (user.IsPermanentlyLocked)
                {
                    IsPermanentlyLocked = true;
                    IsLocked = true;
                    LockMessage = "Tài khoản đã bị khóa vĩnh viễn, vui lòng liên hệ admin.";
                    await tx.RollbackAsync();
                    return Page();
                }

                // ── KIỂM TRA KHÓA TẠM THỜI ──
                if (user.LockedUntil.HasValue && user.LockedUntil.Value > DateTime.UtcNow)
                {
                    var remaining = (int)(user.LockedUntil.Value - DateTime.UtcNow).TotalSeconds;
                    IsLocked = true;
                    LockRemainingSeconds = remaining;
                    LockMessage = $"Tài khoản đang bị khóa. Vui lòng thử lại sau {remaining / 60} phút {remaining % 60} giây.";
                    await tx.RollbackAsync();
                    return Page();
                }

                // Nếu hết thời gian khóa → xóa LockedUntil
                if (user.LockedUntil.HasValue && user.LockedUntil.Value <= DateTime.UtcNow)
                    user.LockedUntil = null;

                // ── TỰ RESET NẾU QUÁ 24H KHÔNG ĐĂng NHẬP SAI ──
                if (user.LastFailedAt.HasValue && (DateTime.UtcNow - user.LastFailedAt.Value).TotalHours > 24)
                    user.FailedLoginCount = 0;

                // ── XÁC THỰC MẬT KHẨU ──
                bool passwordOk = await _userManager.CheckPasswordAsync(user, Input.Password);

                if (passwordOk)
                {
                    // Reset toàn bộ lockout state
                    user.FailedLoginCount = 0;
                    user.TotalLockCount = 0;
                    user.LockedUntil = null;
                    user.IsPermanentlyLocked = false;
                    user.LastFailedAt = null;

                    await _db.SaveChangesAsync();
                    await tx.CommitAsync();

                    await _signInManager.SignInAsync(user, Input.RememberMe);
                    _logger.LogInformation("User logged in.");
                    return LocalRedirect(returnUrl);
                }
                else
                {
                    // Đăng nhập sai
                    user.FailedLoginCount++;
                    user.LastFailedAt = DateTime.UtcNow;

                    if (user.FailedLoginCount >= 3)
                    {
                        user.TotalLockCount++;
                        user.FailedLoginCount = 0;

                        var duration = GetLockDuration(user.TotalLockCount);
                        if (duration == null)
                        {
                            user.IsPermanentlyLocked = true;
                            IsPermanentlyLocked = true;
                            IsLocked = true;
                            LockMessage = "Tài khoản đã bị khóa vĩnh viễn do đăng nhập sai quá nhiều lần, vui lòng liên hệ admin.";
                        }
                        else
                        {
                            user.LockedUntil = DateTime.UtcNow.Add(duration.Value);
                            var secs = (int)duration.Value.TotalSeconds;
                            IsLocked = true;
                            LockRemainingSeconds = secs;
                            LockMessage = $"Tài khoản bị khóa {FormatDuration(duration)} do đăng nhập sai 3 lần liên tiếp.";
                        }
                    }
                    else
                    {
                        int remaining = 3 - user.FailedLoginCount;
                        var nextDuration = GetLockDuration(user.TotalLockCount + 1);
                        AttemptsRemaining = remaining;
                        ModelState.AddModelError(string.Empty,
                            $"Sai mật khẩu, còn {remaining} lần thử trước khi tài khoản bị khóa {FormatDuration(nextDuration)}.");
                    }

                    await _db.SaveChangesAsync();
                    await tx.CommitAsync();
                    return Page();
                }
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }
    }
}
