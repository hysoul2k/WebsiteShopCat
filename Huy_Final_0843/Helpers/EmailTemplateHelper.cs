using System.Text;

namespace Huy_Final_0843.Helpers
{
    public static class EmailTemplateHelper
    {
        public static string GetBaseTemplate(string title, string content)
        {
            var sb = new StringBuilder();
            sb.Append("<!DOCTYPE html>");
            sb.Append("<html>");
            sb.Append("<head>");
            sb.Append("<meta charset='utf-8'>");
            sb.Append("<style>");
            sb.Append("body { font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; margin: 0; padding: 0; background-color: #f4f4f4; color: #333; }");
            sb.Append(".container { max-width: 600px; margin: 20px auto; background-color: #ffffff; border-radius: 8px; overflow: hidden; box-shadow: 0 4px 10px rgba(0,0,0,0.1); }");
            sb.Append(".header { background-color: #bc8f8f; padding: 30px; text-align: center; color: #ffffff; }");
            sb.Append(".header h1 { margin: 0; font-size: 28px; letter-spacing: 2px; text-transform: uppercase; }");
            sb.Append(".content { padding: 40px; line-height: 1.6; }");
            sb.Append(".footer { background-color: #1a1a1a; padding: 20px; text-align: center; color: #888; font-size: 12px; }");
            sb.Append(".btn { display: inline-block; padding: 12px 25px; background-color: #bc8f8f; color: #ffffff !important; text-decoration: none; border-radius: 5px; font-weight: bold; margin-top: 20px; }");
            sb.Append("</style>");
            sb.Append("</head>");
            sb.Append("<body>");
            sb.Append("<div class='container'>");
            sb.Append("<div class='header'><h1>MEOW GARDEN</h1><p style='margin:5px 0 0; font-size:12px; opacity:0.8;'>Thiên đường thú cưng Professional</p></div>");
            sb.Append("<div class='content'>");
            sb.Append($"<h2 style='color: #bc8f8f; margin-top: 0;'>{title}</h2>");
            sb.Append(content);
            sb.Append("</div>");
            sb.Append("<div class='footer'>");
            sb.Append("<p><b>Meow Garden - Nơi tình yêu nở rộ cùng các bé mèo</b></p>");
            sb.Append("<p>Địa chỉ: Thành phố Hồ Chí Minh, Việt Nam</p>");
            sb.Append("<p>&copy; 2026 Meow Garden. All rights reserved.</p>");
            sb.Append("</div>");
            sb.Append("</div>");
            sb.Append("</body></html>");
            return sb.ToString();
        }

        public static string GetOTPTemplate(string otp)
        {
            var content = $@"
                <p>Xin chào,</p>
                <p>Bạn đang thực hiện thay đổi email tại <b>Meow Garden</b>. Để bảo mật tài khoản, vui lòng sử dụng mã OTP dưới đây để xác thực:</p>
                <div style='background-color: #fdf2f2; border: 2px dashed #bc8f8f; padding: 20px; text-align: center; margin: 30px 0; border-radius: 10px;'>
                    <span style='font-size: 40px; font-weight: 800; color: #bc8f8f; letter-spacing: 15px;'>{otp}</span>
                </div>
                <p style='color: #d9534f; font-weight: bold;'>Lưu ý: Mã này sẽ hết hạn sau 5 phút. Vui lòng không chia sẻ mã này cho bất kỳ ai.</p>
                <p>Nếu bạn không thực hiện yêu cầu này, vui lòng bỏ qua email này hoặc liên hệ hỗ trợ.</p>";
            
            return GetBaseTemplate("Xác thực thay đổi Email", content);
        }

        public static string GetForgotPasswordOTPTemplate(string otp)
        {
            var content = $@"
                <p>Xin chào,</p>
                <p>Chúng tôi nhận được yêu cầu khôi phục mật khẩu cho tài khoản của bạn tại <b>Meow Garden</b>. Vui lòng sử dụng mã xác nhận dưới đây:</p>
                <div style='background-color: #f0f7ff; border: 2px dashed #3a7bd5; padding: 20px; text-align: center; margin: 30px 0; border-radius: 10px;'>
                    <span style='font-size: 40px; font-weight: 800; color: #3a7bd5; letter-spacing: 15px;'>{otp}</span>
                </div>
                <p style='color: #d9534f; font-weight: bold;'>Lưu ý: Mã này sẽ hết hạn sau 10 phút. Nếu bạn không thực hiện yêu cầu này, hãy đổi mật khẩu ngay để bảo mật tài khoản.</p>
                <p>Trân trọng,<br/>Đội ngũ Meow Garden</p>";

            return GetBaseTemplate("Khôi phục mật khẩu", content);
        }

        public static string GetOrderConfirmationTemplate(int orderId, decimal total, string paymentMethod, string viewOrderUrl)
        {
            var paymentNote = paymentMethod == "BankTransfer"
                ? "<p style='background:#fff8e1;border-left:4px solid #ffc107;padding:12px 16px;border-radius:4px;'>💳 <b>Vui lòng hoàn tất chuyển khoản</b> để đơn hàng được xử lý. Sau khi admin xác nhận, bạn sẽ nhận thêm email cập nhật.</p>"
                : "<p style='background:#e8f5e9;border-left:4px solid #4caf50;padding:12px 16px;border-radius:4px;'>✅ Phương thức thanh toán: <b>Thanh toán khi nhận hàng (COD)</b></p>";

            var content = $@"
                <p>Xin chào!</p>
                <p>Cảm ơn bạn đã đặt hàng tại <b>Meow Garden</b>. Chúng mình đã nhận được đơn hàng của bạn và đang chuẩn bị xử lý! 🐱</p>
                <div style='background-color: #f9f9f9; padding: 20px; border-radius: 8px; margin: 20px 0; border-left: 4px solid #bc8f8f;'>
                    <table style='width: 100%; border-collapse: collapse;'>
                        <tr><td style='padding: 5px 0; color: #666;'>Mã đơn hàng:</td><td style='text-align: right; font-weight: bold;'>#{orderId}</td></tr>
                        <tr><td style='padding: 5px 0; color: #666;'>Tổng thanh toán:</td><td style='text-align: right; font-weight: bold; color: #bc8f8f;'>{total:N0} đ</td></tr>
                        <tr><td style='padding: 5px 0; color: #666;'>Trạng thái:</td><td style='text-align: right;'><span style='background-color: #ffc107; color: #333; padding: 2px 10px; border-radius: 15px; font-size: 12px;'>Chờ xử lý</span></td></tr>
                    </table>
                </div>
                {paymentNote}
                <div style='text-align: center; margin-top: 24px;'>
                    <a href='{viewOrderUrl}' class='btn'>XEM ĐƠN HÀNG CỦA TÔI</a>
                </div>
                <p style='margin-top: 30px; font-size: 13px; color: #888;'>Nếu bạn có thắc mắc, hãy liên hệ chúng mình qua email. Cảm ơn bạn đã tin tưởng Meow Garden! 🐾</p>";

            return GetBaseTemplate("Xác nhận đặt hàng thành công!", content);
        }

        public static string GetBankTransferPendingTemplate(int orderId, decimal total, string viewOrderUrl)
        {
            var content = $@"
                <p>Xin chào!</p>
                <p>Chúng mình đã nhận được thông tin bạn vừa chuyển khoản cho đơn hàng <b>#{orderId}</b>.</p>
                <div style='background-color: #f9f9f9; padding: 20px; border-radius: 8px; margin: 20px 0; border-left: 4px solid #ffc107;'>
                    <table style='width: 100%; border-collapse: collapse;'>
                        <tr><td style='padding: 5px 0; color: #666;'>Mã đơn hàng:</td><td style='text-align: right; font-weight: bold;'>#{orderId}</td></tr>
                        <tr><td style='padding: 5px 0; color: #666;'>Số tiền:</td><td style='text-align: right; font-weight: bold; color: #bc8f8f;'>{total:N0} đ</td></tr>
                        <tr><td style='padding: 5px 0; color: #666;'>Trạng thái:</td><td style='text-align: right;'><span style='background-color: #ffc107; color: #333; padding: 2px 10px; border-radius: 15px; font-size: 12px;'>Chờ xác nhận</span></td></tr>
                    </table>
                </div>
                <p>Admin sẽ xác nhận giao dịch trong vòng <b>1-2 giờ làm việc</b>. Sau khi xác nhận, bạn sẽ nhận thêm email thông báo.</p>
                <div style='text-align: center; margin-top: 24px;'>
                    <a href='{viewOrderUrl}' class='btn'>THEO DÕI ĐƠN HÀNG</a>
                </div>
                <p style='margin-top: 30px; font-size: 13px; color: #888;'>Cảm ơn bạn đã tin tưởng Meow Garden! 🐾</p>";

            return GetBaseTemplate("Đã nhận thông tin chuyển khoản", content);
        }

        public static string GetOrderUpdateTemplate(int orderId, decimal total, string status, string viewOrderUrl)
        {
            var content = $@"
                <p>Chào mừng bạn đã quay lại!</p>
                <p>Đơn hàng của bạn tại <b>Meow Garden</b> vừa có cập nhật mới về trạng thái xử lý.</p>
                <div style='background-color: #f9f9f9; padding: 20px; border-radius: 8px; margin: 20px 0; border-left: 4px solid #bc8f8f;'>
                    <table style='width: 100%; border-collapse: collapse;'>
                        <tr><td style='padding: 5px 0; color: #666;'>Mã đơn hàng:</td><td style='text-align: right; font-weight: bold;'>#{orderId}</td></tr>
                        <tr><td style='padding: 5px 0; color: #666;'>Tổng thanh toán:</td><td style='text-align: right; font-weight: bold; color: #bc8f8f;'>{total:N0} đ</td></tr>
                        <tr><td style='padding: 5px 0; color: #666;'>Trạng thái mới:</td><td style='text-align: right;'><span style='background-color: #bc8f8f; color: white; padding: 2px 10px; border-radius: 15px; font-size: 12px;'>{status}</span></td></tr>
                    </table>
                </div>
                <p>Boss của bạn đang rất háo hức chờ đợi! Chúng tôi sẽ giao hàng nhanh nhất có thể.</p>
                <div style='text-align: center;'>
                    <a href='{viewOrderUrl}' class='btn'>XEM CHI TIẾT ĐƠN HÀNG</a>
                </div>
                <p style='margin-top: 30px; font-size: 13px; color: #888;'>Cảm ơn bạn đã tin tưởng Meow Garden!</p>";

            return GetBaseTemplate("Cập nhật trạng thái đơn hàng", content);
        }
    }
}
