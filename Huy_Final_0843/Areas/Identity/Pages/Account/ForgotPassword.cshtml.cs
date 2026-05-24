// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.Encodings.Web;
using Huy_Final_0843.Helpers;
using Huy_Final_0843.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;

namespace Huy_Final_0843.Areas.Identity.Pages.Account
{
    public class ForgotPasswordModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IEmailSender _emailSender;

        public ForgotPasswordModel(UserManager<ApplicationUser> userManager, IEmailSender emailSender)
        {
            _userManager = userManager;
            _emailSender = emailSender;
        }

        [BindProperty]
        public InputModel Input { get; set; } = default!;

        public class InputModel
        {
            [Required(ErrorMessage = "Vui lòng nhập email")]
            [EmailAddress(ErrorMessage = "Email không hợp lệ")]
            public string Email { get; set; } = default!;
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid) return Page();

            var user = await _userManager.FindByEmailAsync(Input.Email);
            if (user == null)
            {
                // Không tiết lộ email có tồn tại hay không
                return RedirectToPage("./ForgotPasswordConfirmation");
            }

            var code = await _userManager.GeneratePasswordResetTokenAsync(user);
            code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
            var callbackUrl = Url.Page(
                "/Account/ResetPassword",
                pageHandler: null,
                values: new { area = "Identity", code, email = Input.Email },
                protocol: Request.Scheme);

            var content = $@"
                <p>Xin chào,</p>
                <p>Bạn vừa yêu cầu đặt lại mật khẩu cho tài khoản <b>Meow Garden</b>. Nhấn nút bên dưới để tiếp tục:</p>
                <div style='text-align:center; margin: 30px 0;'>
                    <a href='{HtmlEncoder.Default.Encode(callbackUrl)}' class='btn'>ĐẶT LẠI MẬT KHẨU</a>
                </div>
                <p style='color:#888; font-size:13px;'>Link có hiệu lực trong 1 giờ. Nếu bạn không yêu cầu điều này, hãy bỏ qua email này.</p>";

            var html = EmailTemplateHelper.GetBaseTemplate("Đặt lại mật khẩu", content);
            await _emailSender.SendEmailAsync(Input.Email, "Đặt lại mật khẩu - Meow Garden", html);

            return RedirectToPage("./ForgotPasswordConfirmation");
        }
    }
}
