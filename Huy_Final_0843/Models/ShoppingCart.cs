using System.Collections.Generic;
using System.Linq;

namespace Huy_Final_0843.Models
{
    public class ShoppingCart
    {
        // Danh sách các món hàng trong giỏ
        public List<CartItem> Items { get; set; } = new List<CartItem>();

        // Phương thức thêm hàng vào giỏ
        public void AddItem(CartItem item)
        {
            // Kiểm tra xem sản phẩm này đã có trong giỏ chưa
            var existingItem = Items.FirstOrDefault(i => i.ProductId == item.ProductId);

            if (existingItem != null)
            {
                // Nếu có rồi thì cộng dồn số lượng
                existingItem.Quantity += item.Quantity;
            }
            else
            {
                // Nếu chưa có thì thêm mới vào danh sách
                Items.Add(item);
            }
        }

        // Phương thức xóa sản phẩm khỏi giỏ
        public void RemoveItem(int productId)
        {
            Items.RemoveAll(i => i.ProductId == productId);
        }

        // --- CÁC PHƯƠNG THỨC BỔ TRỢ QUAN TRỌNG ---

        // Tính tổng tiền của toàn bộ giỏ hàng (Để hiện ở Footer bảng giỏ hàng)
        public decimal GetTotalValue()
        {
            return Items.Sum(i => i.Price * i.Quantity);
        }

        // Đếm tổng số lượng item (Để hiện trên Badge icon Cart ở Layout)
        public int GetTotalQuantity()
        {
            return Items.Sum(i => i.Quantity);
        }

        // Làm trống giỏ hàng sau khi đặt hàng thành công
        public void Clear()
        {
            Items.Clear();
        }
    }
}