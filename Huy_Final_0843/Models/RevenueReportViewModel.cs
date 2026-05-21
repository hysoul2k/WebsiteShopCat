using System.Collections.Generic;

namespace Huy_Final_0843.Models
{
    public class RevenueReportViewModel
    {
        public decimal TotalRevenue { get; set; }
        public int OrdersThisMonth { get; set; }
        public List<TopProductViewModel> TopSellingProducts { get; set; } = new List<TopProductViewModel>();

        // --- BỔ SUNG: DỮ LIỆU LỌC & BIỂU ĐỒ ---
        public int SelectedMonth { get; set; }
        public int SelectedYear { get; set; }
        public List<int> AvailableYears { get; set; } = new List<int>();
        public List<string> ChartLabels { get; set; } = new List<string>();
        public List<decimal> ChartData { get; set; } = new List<decimal>();
    }

    public class TopProductViewModel
    {
        public string ProductName { get; set; }
        public string ProductImage { get; set; }
        public int TotalSold { get; set; }
        public decimal TotalRevenueGenerated { get; set; }
    }
}
