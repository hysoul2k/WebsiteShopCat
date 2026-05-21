using Microsoft.AspNetCore.SignalR;

namespace Huy_Final_0843.Hubs
{
    public class OrderHub : Hub
    {
        // 1. Khi khách hàng đặt đơn -> Thông báo Admin
        public async Task SendNewOrderNotification(string orderId, string customerName)
        {
            await Clients.All.SendAsync("ReceiveNewOrder", orderId, customerName);
        }

        // 2. Khi Admin cập nhật trạng thái -> Thông báo đúng User ID đó
        public async Task SendStatusUpdate(string userId, string orderId, string status)
        {
            await Clients.User(userId).SendAsync("ReceiveStatusUpdate", orderId, status);
        }
        
        // 3. Thông báo chung khi trạng thái thay đổi (Dành cho demo hoặc log)
        public async Task BroadcastStatusUpdate(string orderId, string status)
        {
            await Clients.All.SendAsync("ReceiveGlobalUpdate", orderId, status);
        }
    }
}
