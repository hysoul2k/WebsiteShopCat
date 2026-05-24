using System.ComponentModel.DataAnnotations;
using System.Text;
using Huy_Final_0843.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;

namespace Huy_Final_0843.Areas.Identity.Pages.Account
{
    [AllowAnonymous]
    public class ResetPasswordModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public ResetPasswordModel(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        [BindProperty]
        public InputModel Input { get; set; } = default!;

        public class InputModel
        {
            [Required(ErrorMessage = "Vui lòng nhập email")]
            [EmailAddress]
            public string Email { get; set; } = default!;

            [Required(ErrorMessage = "Vui lòng nhập mật khẩu mới")]
            [MinLength(6, ErrorMessage = "Mật khẩu tối thiểu 6 ký tự")]
            [DataType(DataType.Password)]
            public string Password { get; set; } = default!;

            [DataType(DataType.Password)]
            [Compare("Password", ErrorMessage = "Mật khẩu xác nhận không khớp")]
            public string ConfirmPassword { get; set; } = default!;

            public string Code { get; set; } = default!;
        }

        public IActionResult OnGet(string? code, string? email)
        {
            if (code == null) return BadRequest("Thiếu mã xác nhận.");
            Input = new InputModel
            {
                Code  = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(code)),
                Email = email ?? ""
            };
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid) return Page();

            var user = await _userManager.FindByEmailAsync(Input.Email);
            if (user == null) return RedirectToPage("./ResetPasswordConfirmation");

            var result = await _userManager.ResetPasswordAsync(user, Input.Code, Input.Password);
            if (result.Succeeded) return RedirectToPage("./ResetPasswordConfirmation");

            foreach (var error in result.Errors)
                ModelState.AddModelError(string.Empty, error.Description);

            return Page();
        }
    }
}
