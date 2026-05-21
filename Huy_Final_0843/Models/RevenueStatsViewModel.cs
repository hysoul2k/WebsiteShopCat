using System.Collections.Generic;

namespace Huy_Final_0843.Models
{
    public class RevenueStatsViewModel
    {
        public int SelectedMonth { get; set; }
        public int SelectedYear { get; set; }
        public decimal TotalRevenue { get; set; }
        public int TotalOrders { get; set; }
        public decimal AverageOrderValue { get; set; }
        
        // Chứa danh sách các năm đang có dữ liệu hoá đơn thực tế để thả vào Dropdown filter.
        public List<int> AvailableYears { get; set; } = new List<int>();

        // 3 Tab Data
        public RevenueGroupData CatGroup { get; set; } = new RevenueGroupData();
        public RevenueGroupData FoodGroup { get; set; } = new RevenueGroupData();
        public RevenueGroupData AccessoryGroup { get; set; } = new RevenueGroupData();
    }

    public class RevenueGroupData
    {
        public string GroupName { get; set; }
        public decimal TotalRevenue { get; set; }
        public List<CategoryOrderDetailViewModel> Orders { get; set; } = new List<CategoryOrderDetailViewModel>();
    }

    public class CategoryOrderDetailViewModel
    {
        public int OrderId { get; set; }
        public DateTime OrderDate { get; set; }
        public string ProductName { get; set; }
        public string CategoryName { get; set; }
        public string ImageUrl { get; set; }
        public int Quantity { get; set; }
        public decimal Price { get; set; }
        public decimal Total => Quantity * Price;
    }
}
