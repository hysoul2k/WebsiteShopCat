using Huy_Final_0843.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Huy_Final_0843.Areas.Admin.Controllers
{
    // Cấp quyền Admin tuyệt đối không cho Staff
    [Area("Admin")]
    [Authorize(Roles = SD.Role_Admin)]
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DashboardController(ApplicationDbContext context)
        {
            _context = context;
        }

        // --- BÁO CÁO DOANH THU & BIỂU ĐỒ (SHOWCASE) ---
        public async Task<IActionResult> RevenueReport(int? month, int? year)
        {
            var vnTime = DateTime.UtcNow.AddHours(7);
            int selectedMonth = month ?? vnTime.Month;
            int selectedYear = year ?? vnTime.Year;

            // 1. Lọc đơn hàng ĐÃ HOÀN THÀNH trong tháng/năm chỉ định
            var filteredOrdersQuery = _context.Orders
                .AsNoTracking()
                .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Product)
                .Where(o => o.Status == OrderStatus.Completed && 
                            o.OrderDate.Month == selectedMonth && 
                            o.OrderDate.Year == selectedYear);

            var filteredOrders = await filteredOrdersQuery.ToListAsync();

            // 2. Tính Tổng doanh thu và Số đơn
            decimal totalRevenue = filteredOrders.Sum(o => o.TotalPrice);
            int ordersThisMonth = filteredOrders.Count;

            // 3. Xử lý Dữ liệu Biểu đồ (Daily Revenue)
            int daysInMonth = DateTime.DaysInMonth(selectedYear, selectedMonth);
            var chartLabels = new List<string>();
            var chartData = new List<decimal>();

            for (int day = 1; day <= daysInMonth; day++)
            {
                chartLabels.Add($"Ngày {day:D2}");
                // Tính tổng doanh thu của ngày này
                decimal dayRevenue = filteredOrders
                    .Where(o => o.OrderDate.Day == day)
                    .Sum(o => o.TotalPrice);
                chartData.Add(dayRevenue);
            }

            // 4. Danh sách Năm hiển thị (Lấy từ dữ liệu thực tế)
            var availableYears = await _context.Orders
                .Select(o => o.OrderDate.Year)
                .Distinct()
                .OrderByDescending(y => y)
                .ToListAsync();
            if (!availableYears.Contains(vnTime.Year)) availableYears.Add(vnTime.Year);

            // 5. Top 5 Sản phẩm bán chạy trong tháng đó
            var topProducts = filteredOrders
                .SelectMany(o => o.OrderDetails)
                .Where(od => od.Product != null)
                .GroupBy(od => new { od.ProductId, od.Product.Name, od.Product.ImageUrl })
                .Select(g => new TopProductViewModel
                {
                    ProductName = g.Key.Name,
                    ProductImage = g.Key.ImageUrl,
                    TotalSold = g.Sum(od => od.Quantity),
                    TotalRevenueGenerated = g.Sum(od => od.Quantity * od.Price)
                })
                .OrderByDescending(x => x.TotalSold)
                .Take(5)
                .ToList();

            var viewModel = new RevenueReportViewModel
            {
                TotalRevenue = totalRevenue,
                OrdersThisMonth = ordersThisMonth,
                TopSellingProducts = topProducts,
                SelectedMonth = selectedMonth,
                SelectedYear = selectedYear,
                AvailableYears = availableYears,
                ChartLabels = chartLabels,
                ChartData = chartData
            };

            return View(viewModel);
        }

        // Báo cáo Tra Cứu theo tuỳ chọn Tháng / Năm
        public async Task<IActionResult> RevenueStats(int? month, int? year)
        {
            var vnTime = DateTime.UtcNow.AddHours(7);
            var currentMonth = month ?? vnTime.Month;
            var currentYear = year ?? vnTime.Year;

            // Xương máu số 1: Kép chặt Include để không bị hụt Data!
            var successOrdersQuery = _context.Orders
                .AsNoTracking()
                .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Product)
                .ThenInclude(p => p.Category)
                .Where(o => o.Status == OrderStatus.Completed);

            // Lấy danh sách các Năm có trong cơ sở dữ liệu để build giao diện cho Menu Droplist
            var availableYears = await successOrdersQuery
                .Select(o => o.OrderDate.Year)
                .Distinct()
                .OrderByDescending(y => y)
                .ToListAsync();

            if (!availableYears.Contains(vnTime.Year))
            {
                availableYears.Add(vnTime.Year);
                availableYears = availableYears.OrderByDescending(y => y).ToList();
            }

            // Tiến hành Query theo mốc thời gian đã lọc
            var filteredOrders = await successOrdersQuery
                .Where(o => o.OrderDate.Month == currentMonth && o.OrderDate.Year == currentYear)
                .ToListAsync();

            decimal totalRevenue = filteredOrders.Sum(o => o.TotalPrice);
            int totalOrders = filteredOrders.Count;
            // Bẫy lỗi không được chia cho mốc 0
            decimal averageOrderValue = totalOrders > 0 ? (totalRevenue / totalOrders) : 0;

            // Bóc tách làm 3 Tab chuyên biệt
            var catGroup = new RevenueGroupData { GroupName = "Mèo" };
            var foodGroup = new RevenueGroupData { GroupName = "Thức Ăn" };
            var accessoryGroup = new RevenueGroupData { GroupName = "Dụng Cụ" };

            // FIX: Lấy OrderDetails từ những Order đã lọc theo tháng/năm!
            var allOrderDetails = filteredOrders.SelectMany(o => o.OrderDetails.Select(od => new {
                OrderDate = o.OrderDate,
                OrderId = o.Id,
                Detail = od
            }));

            foreach (var item in allOrderDetails)
            {
                if (item.Detail.Product == null || item.Detail.Product.Category == null) continue;
                
                var product = item.Detail.Product!;
                var category = product.Category!;

                string categoryNameSearch = category.Name.ToLower();
                var detailViewModel = new CategoryOrderDetailViewModel
                {
                    OrderId = item.OrderId,
                    OrderDate = item.OrderDate,
                    ProductName = product.Name,
                    CategoryName = category.Name,
                    ImageUrl = product.ImageUrl,
                    Quantity = item.Detail.Quantity,
                    Price = item.Detail.Price
                };

                // Phân tích Mapping chuẩn tiếng Việt & Tối ưu hóa tính chính xác (Dứt điểm Category 0 VNĐ)
                if (categoryNameSearch.Contains("thức ăn") || categoryNameSearch.Contains("hạt") || categoryNameSearch.Contains("pate") || categoryNameSearch.Contains("súp"))
                {
                    foodGroup.Orders.Add(detailViewModel);
                    foodGroup.TotalRevenue += detailViewModel.Total;
                }
                else if (categoryNameSearch.Contains("dụng cụ") || categoryNameSearch.Contains("phụ kiện") || categoryNameSearch.Contains("cát") || categoryNameSearch.Contains("khay") || categoryNameSearch.Contains("đồ chơi"))
                {
                    accessoryGroup.Orders.Add(detailViewModel);
                    accessoryGroup.TotalRevenue += detailViewModel.Total;
                }
                else if (categoryNameSearch.Contains("mèo") || categoryNameSearch.Contains("anh lông") || categoryNameSearch.Contains("scottish") || categoryNameSearch.Contains("persian"))
                {
                    catGroup.Orders.Add(detailViewModel);
                    catGroup.TotalRevenue += detailViewModel.Total;
                }
                else
                {
                    // Trường hợp mặc định cho các sản phẩm khác (vẫn đưa vào Cat cho an toàn)
                    catGroup.Orders.Add(detailViewModel);
                    catGroup.TotalRevenue += detailViewModel.Total;
                }
            }

            // Descending Sort mớ Order Detail cho gọn gàng (Theo mới nhất)
            catGroup.Orders = catGroup.Orders.OrderByDescending(x => x.OrderDate).ToList();
            foodGroup.Orders = foodGroup.Orders.OrderByDescending(x => x.OrderDate).ToList();
            accessoryGroup.Orders = accessoryGroup.Orders.OrderByDescending(x => x.OrderDate).ToList();

            var model = new RevenueStatsViewModel
            {
                SelectedMonth = currentMonth,
                SelectedYear = currentYear,
                TotalRevenue = totalRevenue,
                TotalOrders = totalOrders,
                AverageOrderValue = averageOrderValue,
                AvailableYears = availableYears,
                CatGroup = catGroup,
                FoodGroup = foodGroup,
                AccessoryGroup = accessoryGroup
            };

            return View(model);
        }

        // --- HÀM DỌN SẠCH DỮ LIỆU ĐỂ TRÌNH DIỄN (SHOWCASE ONLY) ---
        [HttpPost]
        [Authorize(Roles = SD.Role_Admin)]
        public async Task<IActionResult> ClearAllData()
        {
            return await ClearRevenue();
        }

        [HttpPost]
        [Authorize(Roles = SD.Role_Admin)]
        public async Task<IActionResult> ClearRevenue()
        {
            try
            {
                await _context.Database.ExecuteSqlRawAsync("DELETE FROM OrderDetails");
                await _context.Database.ExecuteSqlRawAsync("DELETE FROM Orders");
                await _context.Database.ExecuteSqlRawAsync("DBCC CHECKIDENT ('Orders', RESEED, 0)");
                await _context.Database.ExecuteSqlRawAsync("DBCC CHECKIDENT ('OrderDetails', RESEED, 0)");
                
                TempData["Success"] = "Hệ thống đã dọn sạch toàn bộ doanh thu và đưa về 0 thành công!";
            }
            catch (System.Exception ex)
            {
                TempData["Error"] = "Lỗi khi dọn dẹp: " + ex.Message;
            }

            return RedirectToAction(nameof(RevenueStats));
        }
    }
}
