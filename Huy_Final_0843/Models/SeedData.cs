using Microsoft.EntityFrameworkCore;
using Huy_Final_0843.Models;

namespace Huy_Final_0843.Models
{
    public static class SeedData
    {
        public static void Initialize(IServiceProvider serviceProvider)
        {
            using (var context = new ApplicationDbContext(
                serviceProvider.GetRequiredService<DbContextOptions<ApplicationDbContext>>()))
            {
                // Kiểm tra xem DB đã có Product nào chưa, nếu có rồi thì ngưng
                if (context.Products.Any())
                {
                    return; 
                }

                // ==========================================
                // 1. KHỞI TẠO HOẶC LẤY DANH MỤC TỪ DATABASE
                // ==========================================
                var catMeo = context.Categories.FirstOrDefault(c => c.Name == "Mèo Cảnh");
                if (catMeo == null)
                {
                    catMeo = new Category { Name = "Mèo Cảnh" };
                    context.Categories.Add(catMeo);
                }

                var catThucAn = context.Categories.FirstOrDefault(c => c.Name == "Thức ăn cho Mèo");
                if (catThucAn == null)
                {
                    catThucAn = new Category { Name = "Thức ăn cho Mèo" };
                    context.Categories.Add(catThucAn);
                }

                var catPhuKien = context.Categories.FirstOrDefault(c => c.Name == "Dụng cụ & Phụ kiện");
                if (catPhuKien == null)
                {
                    catPhuKien = new Category { Name = "Dụng cụ & Phụ kiện" };
                    context.Categories.Add(catPhuKien);
                }

                // Lưu lại ngay để SQL Server cấp phát ID (Id = 1, 2, 3...) cho các Category này
                context.SaveChanges();

                // ==========================================
                // 2. BƠM 60 SẢN PHẨM MẪU VÀO DATABASE
                // ==========================================
                var products = new List<Product>
                {
                    // DANH MỤC 1: CÁC GIỐNG MÈO (20 Bé)
                    new Product { Name = "Mèo Anh Lông Ngắn (ALN) Xám Xanh - Meow Garden", Price = 4500000, CategoryId = catMeo.Id, StockQuantity = 3, ImageUrl = "https://i.ibb.co/3sWJ5zj/default-cat.jpg", Description = "Bé mèo chuẩn chủng viện Meow Garden, mặt nọng, béo tròn, tính tình hiền lành và rất quấn chủ." },
                    new Product { Name = "Mèo Anh Lông Ngắn (ALN) Bicolor - Meow Garden", Price = 6500000, CategoryId = catMeo.Id, StockQuantity = 2, ImageUrl = "https://i.ibb.co/3sWJ5zj/default-cat.jpg", Description = "Bé mèo từ khu vườn Meow Garden, màu lông chia hai mảng trắng và xám/đen cực kỳ đối xứng." },
                    // ... (giữ nguyên các sản phẩm khác nhưng có thể cập nhật mô tả nếu cần)
                    new Product { Name = "Mèo Anh Lông Ngắn (ALN) Golden", Price = 8000000, CategoryId = catMeo.Id, StockQuantity = 1, ImageUrl = "https://i.ibb.co/3sWJ5zj/default-cat.jpg", Description = "Bộ lông vàng rực rỡ mang lại tài lộc, dòng cao cấp đang rất được săn đón." },
                    new Product { Name = "Mèo Anh Lông Dài (ALD) Trắng", Price = 5000000, CategoryId = catMeo.Id, StockQuantity = 2, ImageUrl = "https://i.ibb.co/3sWJ5zj/default-cat.jpg", Description = "Bộ lông xù bồng bềnh như bông tuyết, đôi mắt to tròn, cần chải lông thường xuyên." },
                    new Product { Name = "Mèo Tai Cụp (Scottish Fold) Xám", Price = 7500000, CategoryId = catMeo.Id, StockQuantity = 1, ImageUrl = "https://i.ibb.co/3sWJ5zj/default-cat.jpg", Description = "Đôi tai cụp sát đầu đáng yêu như cú mèo, ngoan ngoãn và thân thiện." },
                    new Product { Name = "Mèo Tai Cụp (Scottish Fold) Tabby", Price = 7000000, CategoryId = catMeo.Id, StockQuantity = 2, ImageUrl = "https://i.ibb.co/3sWJ5zj/default-cat.jpg", Description = "Màu vằn hổ khỏe khoắn kết hợp với đôi tai cụp tạo nên vẻ đẹp độc lạ." },
                    new Product { Name = "Mèo Scottish Straight (Tai thẳng)", Price = 5500000, CategoryId = catMeo.Id, StockQuantity = 3, ImageUrl = "https://i.ibb.co/3sWJ5zj/default-cat.jpg", Description = "Anh em cùng bầy với tai cụp nhưng tai thẳng, gen khỏe, ít bệnh vặt." },
                    new Product { Name = "Mèo Ba Tư (Persian) Lông Xù", Price = 6000000, CategoryId = catMeo.Id, StockQuantity = 2, ImageUrl = "https://i.ibb.co/3sWJ5zj/default-cat.jpg", Description = "Giống mèo quý tộc với khuôn mặt tịt, mũi ngắn và bộ lông dài thướt tha." },
                    new Product { Name = "Mèo Exotic (Ba Tư Lông Ngắn)", Price = 8500000, CategoryId = catMeo.Id, StockQuantity = 1, ImageUrl = "https://i.ibb.co/3sWJ5zj/default-cat.jpg", Description = "Dành cho ai thích mặt tịt của Ba Tư nhưng lười chải lông, phiên bản lông ngắn mượt mà." },
                    new Product { Name = "Mèo Bengal Vằn Báo", Price = 12000000, CategoryId = catMeo.Id, StockQuantity = 1, ImageUrl = "https://i.ibb.co/3sWJ5zj/default-cat.jpg", Description = "Mang vẻ đẹp hoang dã với bộ lông vằn đốm, rất năng động và thích nghịch nước." },
                    new Product { Name = "Mèo Munchkin Chân Ngắn", Price = 9000000, CategoryId = catMeo.Id, StockQuantity = 3, ImageUrl = "https://i.ibb.co/3sWJ5zj/default-cat.jpg", Description = "Corgi của thế giới loài mèo với 4 chân ngắn ngủn nhưng di chuyển cực lanh lợi." },
                    new Product { Name = "Mèo Sphynx (Mèo Ai Cập)", Price = 15000000, CategoryId = catMeo.Id, StockQuantity = 1, ImageUrl = "https://i.ibb.co/3sWJ5zj/default-cat.jpg", Description = "Giống mèo không lông, da nhăn nheo, đòi hỏi chế độ chăm sóc da đặc biệt." },
                    new Product { Name = "Mèo Ragdoll Bicolor", Price = 14000000, CategoryId = catMeo.Id, StockQuantity = 2, ImageUrl = "https://i.ibb.co/3sWJ5zj/default-cat.jpg", Description = "Khi được bế lên sẽ nhũn ra như búp bê vải, kích thước lớn, mắt xanh biển." },
                    new Product { Name = "Mèo Xiêm (Siamese)", Price = 2500000, CategoryId = catMeo.Id, StockQuantity = 4, ImageUrl = "https://i.ibb.co/3sWJ5zj/default-cat.jpg", Description = "Đến từ Thái Lan, lông ngắn, màu sắc độc đáo ở mặt và đuôi, khá thông minh." },
                    new Product { Name = "Mèo Maine Coon Khổng Lồ", Price = 18000000, CategoryId = catMeo.Id, StockQuantity = 1, ImageUrl = "https://i.ibb.co/3sWJ5zj/default-cat.jpg", Description = "Giống mèo lớn nhất thế giới, oai vệ như sư tử nhưng tính tình lại dịu dàng." },
                    new Product { Name = "Mèo Nga Mắt Xanh (Russian Blue)", Price = 9000000, CategoryId = catMeo.Id, StockQuantity = 2, ImageUrl = "https://i.ibb.co/3sWJ5zj/default-cat.jpg", Description = "Bộ lông xám xanh ánh bạc, đôi mắt xanh ngọc bích hút hồn, tính cách độc lập." },
                    new Product { Name = "Mèo Mỹ Lông Ngắn (American Shorthair)", Price = 5000000, CategoryId = catMeo.Id, StockQuantity = 3, ImageUrl = "https://i.ibb.co/3sWJ5zj/default-cat.jpg", Description = "Cơ bắp săn chắc, vằn xoáy cổ điển, kỹ năng bắt chuột cực đỉnh." },
                    new Product { Name = "Mèo Rừng Na Uy (Norwegian Forest)", Price = 16000000, CategoryId = catMeo.Id, StockQuantity = 1, ImageUrl = "https://i.ibb.co/3sWJ5zj/default-cat.jpg", Description = "Bộ lông 2 lớp chống nước, chịu lạnh tốt, xuất thân là thợ săn lão luyện." },
                    new Product { Name = "Mèo Abyssinian", Price = 11000000, CategoryId = catMeo.Id, StockQuantity = 1, ImageUrl = "https://i.ibb.co/3sWJ5zj/default-cat.jpg", Description = "Được ví như siêu mẫu của loài mèo với vóc dáng mảnh mai, thanh thoát." },
                    new Product { Name = "Mèo Mướp Ta (Nhận Nuôi)", Price = 0, CategoryId = catMeo.Id, StockQuantity = 5, ImageUrl = "https://i.ibb.co/3sWJ5zj/default-cat.jpg", Description = "Các bé mèo ta khỏe mạnh, đã được tiêm phòng và tẩy giun, tìm mái ấm yêu thương." },

                    // DANH MỤC 2: THỨC ĂN CHO MÈO (20 Sản phẩm)
                    new Product { Name = "Hạt Royal Canin Kitten (2kg)", Price = 380000, CategoryId = catThucAn.Id, StockQuantity = 50, ImageUrl = "https://i.ibb.co/3sWJ5zj/default-cat.jpg", Description = "Thức ăn hạt cao cấp cho mèo con dưới 12 tháng, hỗ trợ tiêu hóa." },
                    new Product { Name = "Hạt Royal Canin Indoor (2kg)", Price = 400000, CategoryId = catThucAn.Id, StockQuantity = 40, ImageUrl = "https://i.ibb.co/3sWJ5zj/default-cat.jpg", Description = "Dành cho mèo sống trong nhà, giúp giảm mùi hôi phân hiệu quả." },
                    new Product { Name = "Hạt Royal Canin Hairball (2kg)", Price = 420000, CategoryId = catThucAn.Id, StockQuantity = 30, ImageUrl = "https://i.ibb.co/3sWJ5zj/default-cat.jpg", Description = "Công thức đặc biệt giúp mèo tiêu hóa và đào thải búi lông trong ruột." },
                    new Product { Name = "Hạt Whiskas Vị Cá Ngừ (1.2kg)", Price = 120000, CategoryId = catThucAn.Id, StockQuantity = 100, ImageUrl = "https://i.ibb.co/3sWJ5zj/default-cat.jpg", Description = "Hạt phổ thông vị cá ngừ thơm ngon, kích thích vị giác mèo trưởng thành." },
                    new Product { Name = "Hạt Whiskas Kitten (1.1kg)", Price = 130000, CategoryId = catThucAn.Id, StockQuantity = 80, ImageUrl = "https://i.ibb.co/3sWJ5zj/default-cat.jpg", Description = "Bổ sung canxi và phốt pho cho mèo con đang giai đoạn phát triển xương." },
                    new Product { Name = "Hạt Me-O Vị Hải Sản (1.2kg)", Price = 110000, CategoryId = catThucAn.Id, StockQuantity = 120, ImageUrl = "https://i.ibb.co/3sWJ5zj/default-cat.jpg", Description = "Giòn rụm, cung cấp đầy đủ dinh dưỡng cơ bản với mức giá tiết kiệm." },
                    new Product { Name = "Hạt Catsrang Hàn Quốc (5kg)", Price = 450000, CategoryId = catThucAn.Id, StockQuantity = 60, ImageUrl = "https://i.ibb.co/3sWJ5zj/default-cat.jpg", Description = "Thương hiệu quốc dân, hạt nhỏ dễ nhai, không sử dụng chất bảo quản tổng hợp." },
                    new Product { Name = "Hạt Minino Yum Vị Hải Sản (1.5kg)", Price = 145000, CategoryId = catThucAn.Id, StockQuantity = 90, ImageUrl = "https://i.ibb.co/3sWJ5zj/default-cat.jpg", Description = "Hình dạng hạt ngôi sao độc đáo, giúp làm sạch răng khi nhai." },
                    new Product { Name = "Hạt Orijen Cat & Kitten (1.8kg)", Price = 1100000, CategoryId = catThucAn.Id, StockQuantity = 15, ImageUrl = "https://i.ibb.co/3sWJ5zj/default-cat.jpg", Description = "Dòng hạt siêu cấp với 85% là thịt tươi sống, không chứa ngũ cốc." },
                    new Product { Name = "Hạt Hữu Cơ Nutrience Vị Gà (2.5kg)", Price = 650000, CategoryId = catThucAn.Id, StockQuantity = 20, ImageUrl = "https://i.ibb.co/3sWJ5zj/default-cat.jpg", Description = "Hạt Grain-free dành cho mèo có hệ tiêu hóa nhạy cảm, nguyên liệu thịt gà tươi." },
                    new Product { Name = "Pate Nekko Vị Cá Ngừ Trẻ em (70g)", Price = 18000, CategoryId = catThucAn.Id, StockQuantity = 200, ImageUrl = "https://i.ibb.co/3sWJ5zj/default-cat.jpg", Description = "Pate tươi dạng sệt dễ tiêu hóa cho mèo con, bổ sung độ ẩm." },
                    new Product { Name = "Pate Nekko Vị Gà & Phô Mai (70g)", Price = 18000, CategoryId = catThucAn.Id, StockQuantity = 250, ImageUrl = "https://i.ibb.co/3sWJ5zj/default-cat.jpg", Description = "Sự kết hợp béo ngậy giữa thịt gà xé và phô mai, mèo nào cũng mê." },
                    new Product { Name = "Pate Snappy Tom Lon (400g)", Price = 45000, CategoryId = catThucAn.Id, StockQuantity = 100, ImageUrl = "https://i.ibb.co/3sWJ5zj/default-cat.jpg", Description = "Pate dạng lon lớn siêu tiết kiệm, thịt cá nguyên miếng sực sực." },
                    new Product { Name = "Pate Whiskas Dạng Gói (85g)", Price = 15000, CategoryId = catThucAn.Id, StockQuantity = 300, ImageUrl = "https://i.ibb.co/3sWJ5zj/default-cat.jpg", Description = "Pate phổ thông nhiều sốt, dùng trộn với hạt khô để tăng độ ngon miệng." },
                    new Product { Name = "Súp Thưởng Ciao Churu Vị Cá Hồi (Túi 4 thanh)", Price = 45000, CategoryId = catThucAn.Id, StockQuantity = 150, ImageUrl = "https://i.ibb.co/3sWJ5zj/default-cat.jpg", Description = "Món ăn vặt gây nghiện số 1, dạng súp lỏng dễ liếm, bổ sung nước." },
                    new Product { Name = "Súp Thưởng Ciao Churu Vị Gà (Túi 4 thanh)", Price = 45000, CategoryId = catThucAn.Id, StockQuantity = 150, ImageUrl = "https://i.ibb.co/3sWJ5zj/default-cat.jpg", Description = "Làm từ thịt gà xé tơi, ít béo, thích hợp làm phần thưởng huấn luyện." },
                    new Product { Name = "Thịt Sấy Lạnh (Freeze Dried) Hỗn Hợp (100g)", Price = 120000, CategoryId = catThucAn.Id, StockQuantity = 40, ImageUrl = "https://i.ibb.co/3sWJ5zj/default-cat.jpg", Description = "Giữ nguyên 100% dinh dưỡng từ thịt tươi bằng công nghệ sấy thăng hoa." },
                    new Product { Name = "Cỏ Mèo Hữu Cơ (Catnip)", Price = 35000, CategoryId = catThucAn.Id, StockQuantity = 80, ImageUrl = "https://i.ibb.co/3sWJ5zj/default-cat.jpg", Description = "Giúp mèo xả stress, hưng phấn và hỗ trợ tiêu hóa búi lông." },
                    new Product { Name = "Sữa Bột Bio Milk Cho Mèo Con (100g)", Price = 55000, CategoryId = catThucAn.Id, StockQuantity = 50, ImageUrl = "https://i.ibb.co/3sWJ5zj/default-cat.jpg", Description = "Thay thế sữa mẹ cho mèo sơ sinh hoặc mèo mẹ bị mất sữa." },
                    new Product { Name = "Gel Dinh Dưỡng Nutri-Plus (120g)", Price = 180000, CategoryId = catThucAn.Id, StockQuantity = 30, ImageUrl = "https://i.ibb.co/3sWJ5zj/default-cat.jpg", Description = "Bổ sung năng lượng tức thì cho mèo ốm dậy, mèo biếng ăn, mèo mang thai." },

                    // DANH MỤC 3: DỤNG CỤ & PHỤ KIỆN (20 Sản phẩm)
                    new Product { Name = "Cát Vệ Sinh Đất Sét Moon Cat (8L)", Price = 65000, CategoryId = catPhuKien.Id, StockQuantity = 80, ImageUrl = "https://i.ibb.co/3sWJ5zj/default-cat.jpg", Description = "Siêu vón cục, khử mùi hương chanh cực tốt, ít bụi." },
                    new Product { Name = "Cát Đậu Nành Tofu Sạch Cature (6L)", Price = 120000, CategoryId = catPhuKien.Id, StockQuantity = 60, ImageUrl = "https://i.ibb.co/3sWJ5zj/default-cat.jpg", Description = "Cát hữu cơ thân thiện môi trường, có thể xả trực tiếp vào bồn cầu không lo tắc." },
                    new Product { Name = "Cát Thủy Tinh Khử Mùi Cao Cấp (5L)", Price = 150000, CategoryId = catPhuKien.Id, StockQuantity = 40, ImageUrl = "https://i.ibb.co/3sWJ5zj/default-cat.jpg", Description = "Hút ẩm cực mạnh, không bụi, 1 túi dùng được cả tháng cho 1 bé mèo." },
                    new Product { Name = "Khay Vệ Sinh Thành Cao Chống Văng", Price = 180000, CategoryId = catPhuKien.Id, StockQuantity = 50, ImageUrl = "https://i.ibb.co/3sWJ5zj/default-cat.jpg", Description = "Thiết kế hở rộng rãi, thành cao giúp mèo bới cát không bị văng ra sàn nhà." },
                    new Product { Name = "Nhà Vệ Sinh Tàu Vũ Trụ Kín", Price = 450000, CategoryId = catPhuKien.Id, StockQuantity = 15, ImageUrl = "https://i.ibb.co/3sWJ5zj/default-cat.jpg", Description = "Kín đáo, ngăn mùi tuyệt đối bay ra phòng, có cửa lật qua lại dễ dàng." },
                    new Product { Name = "Xẻng Xúc Cát Vệ Sinh Mèo", Price = 20000, CategoryId = catPhuKien.Id, StockQuantity = 200, ImageUrl = "https://i.ibb.co/3sWJ5zj/default-cat.jpg", Description = "Lỗ lọc kích thước chuẩn, giữ lại phần cục vón và làm rớt phần cát sạch xuống." },
                    new Product { Name = "Cây Cào Móng 1 Cột Thừng", Price = 150000, CategoryId = catPhuKien.Id, StockQuantity = 40, ImageUrl = "https://i.ibb.co/3sWJ5zj/default-cat.jpg", Description = "Thiết kế nhỏ gọn, bọc dây thừng sisal tự nhiên giúp mèo xả stress." },
                    new Product { Name = "Cat Tree 3 Tầng Kèm Võng", Price = 850000, CategoryId = catPhuKien.Id, StockQuantity = 10, ImageUrl = "https://i.ibb.co/3sWJ5zj/default-cat.jpg", Description = "Khu vui chơi tổng hợp cho mèo, có võng nằm ngủ và đồ chơi treo lủng lẳng." },
                    new Product { Name = "Balo Vận Chuyển Phi Hành Gia", Price = 320000, CategoryId = catPhuKien.Id, StockQuantity = 30, ImageUrl = "https://i.ibb.co/3sWJ5zj/default-cat.jpg", Description = "Có mặt kính trong suốt để bé ngắm cảnh khi được chở đi dạo bằng xe máy." },
                    new Product { Name = "Túi Vận Chuyển Vải Thoáng Khí", Price = 150000, CategoryId = catPhuKien.Id, StockQuantity = 50, ImageUrl = "https://i.ibb.co/3sWJ5zj/default-cat.jpg", Description = "Nhẹ nhàng, dễ gấp gọn, phù hợp cho những chuyến đi ngắn ra phòng khám thú y." },
                    new Product { Name = "Bát Ăn Đôi Nhựa Kèm Bình Nước", Price = 95000, CategoryId = catPhuKien.Id, StockQuantity = 100, ImageUrl = "https://i.ibb.co/3sWJ5zj/default-cat.jpg", Description = "Tích hợp sẵn bình cấp nước tự động khi nước trong khay vơi đi." },
                    new Product { Name = "Bát Ăn Gốm Sứ Chống Gù Cổ", Price = 180000, CategoryId = catPhuKien.Id, StockQuantity = 40, ImageUrl = "https://i.ibb.co/3sWJ5zj/default-cat.jpg", Description = "Thiết kế độ cao chuẩn giúp mèo không bị gập cổ khi ăn, chống nôn trớ." },
                    new Product { Name = "Máy Cho Ăn Tự Động Kết Nối WiFi", Price = 1200000, CategoryId = catPhuKien.Id, StockQuantity = 10, ImageUrl = "https://i.ibb.co/3sWJ5zj/default-cat.jpg", Description = "Điều khiển qua App điện thoại, tự động nhả hạt theo giờ dù chủ đi du lịch." },
                    new Product { Name = "Máy Lọc Nước Vòi Phun Cho Mèo", Price = 350000, CategoryId = catPhuKien.Id, StockQuantity = 25, ImageUrl = "https://i.ibb.co/3sWJ5zj/default-cat.jpg", Description = "Tạo dòng nước chảy róc rách kích thích mèo uống nhiều nước hơn, ngăn ngừa bệnh thận." },
                    new Product { Name = "Sữa Tắm SOS Chuyên Dụng (530ml)", Price = 120000, CategoryId = catPhuKien.Id, StockQuantity = 70, ImageUrl = "https://i.ibb.co/3sWJ5zj/default-cat.jpg", Description = "Khử mùi hôi, làm mượt lông, an toàn cho làn da nhạy cảm của mèo." },
                    new Product { Name = "Lược Chải Lông Rụng Nút Bấm", Price = 85000, CategoryId = catPhuKien.Id, StockQuantity = 120, ImageUrl = "https://i.ibb.co/3sWJ5zj/default-cat.jpg", Description = "Gỡ rối lông cực tốt, bấm nút là tự động đẩy phần lông rụng ra khỏi lược." },
                    new Product { Name = "Kềm Cắt Móng Thép Chống Gỉ", Price = 45000, CategoryId = catPhuKien.Id, StockQuantity = 150, ImageUrl = "https://i.ibb.co/3sWJ5zj/default-cat.jpg", Description = "Lưỡi cắt sắc bén, có thanh chắn an toàn để không cắt phạm vào tủy móng." },
                    new Product { Name = "Vòng Cổ Có Chuông Lục Lạc", Price = 25000, CategoryId = catPhuKien.Id, StockQuantity = 200, ImageUrl = "https://i.ibb.co/3sWJ5zj/default-cat.jpg", Description = "Nhiều màu sắc, có thể điều chỉnh kích cỡ, chuông kêu leng keng vui tai." },
                    new Product { Name = "Đồ Chơi Cần Câu Mèo Gắn Lông Vũ", Price = 35000, CategoryId = catPhuKien.Id, StockQuantity = 300, ImageUrl = "https://i.ibb.co/3sWJ5zj/default-cat.jpg", Description = "Đồ chơi tương tác giúp tiêu hao năng lượng, rèn luyện phản xạ săn mồi." },
                    new Product { Name = "Đèn Laser Đồ Chơi Bỏ Túi", Price = 40000, CategoryId = catPhuKien.Id, StockQuantity = 150, ImageUrl = "https://i.ibb.co/3sWJ5zj/default-cat.jpg", Description = "Phát ra đốm sáng đỏ khiến mọi bé mèo đều phát cuồng chạy theo bắt." }
                };

                context.Products.AddRange(products);
                context.SaveChanges();

                // ==========================================
                // 3. Seed some sample Vouchers for testing
                // ==========================================
                var now = DateTime.UtcNow;
                var seedVouchers = new List<Voucher>
                {
                    new Voucher { Code = "MEOW10", DiscountType = "Percent", DiscountPercent = 10, MinOrderAmount = 0, MaxUsage = 100, UsedCount = 0, ExpiryDate = new DateTime(2026, 12, 31), IsActive = true },
                    new Voucher { Code = "MEOW50K", DiscountType = "Fixed", DiscountPercent = 50000, MinOrderAmount = 200000, MaxUsage = 50, UsedCount = 0, ExpiryDate = new DateTime(2026, 12, 31), IsActive = true },
                    new Voucher { Code = "WELCOME15", DiscountType = "Percent", DiscountPercent = 15, MinOrderAmount = 100000, MaxUsage = 200, UsedCount = 0, ExpiryDate = new DateTime(2026, 9, 30), IsActive = true },
                    new Voucher { Code = "KITTY20", DiscountType = "Percent", DiscountPercent = 20, MinOrderAmount = 300000, MaxUsage = 30, UsedCount = 0, ExpiryDate = new DateTime(2026, 8, 1), IsActive = true },
                    new Voucher { Code = "FREESHIP", DiscountType = "Fixed", DiscountPercent = 30000, MinOrderAmount = 150000, MaxUsage = 0, UsedCount = 0, ExpiryDate = new DateTime(2026, 6, 30), IsActive = true }
                };

                foreach (var voucher in seedVouchers)
                {
                    if (!context.Vouchers.Any(v => v.Code == voucher.Code))
                    {
                        context.Vouchers.Add(voucher);
                    }
                }

                if (context.ChangeTracker.HasChanges())
                {
                    context.SaveChanges();
                }
            }
        }
    }
}