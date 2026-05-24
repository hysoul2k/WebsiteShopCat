using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Huy_Final_0843.Models;
using Huy_Final_0843.Repositories;
using Huy_Final_0843.Hubs;
using Microsoft.AspNetCore.Identity.UI.Services;
using Huy_Final_0843.Services;
using Huy_Final_0843.Services.AI;

var builder = WebApplication.CreateBuilder(args);

// 1. Cấu hình Database
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    if (builder.Configuration.GetValue<bool>("UseInMemoryDatabase"))
    {
        options.UseInMemoryDatabase("ChatbotTestInMemoryDb");
    }
    else
    {
        options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
    }
});

// 2. Cấu hình Identity
builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
    .AddDefaultTokenProviders()
    .AddDefaultUI()
    .AddEntityFrameworkStores<ApplicationDbContext>();


// --- BỔ SUNG CẤU HÌNH SESSION (Theo tài liệu) ---
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(8);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.MaxAge = TimeSpan.FromHours(8);
});
// -----------------------------------------------

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = $"/Identity/Account/Login";
    options.LogoutPath = $"/Identity/Account/Logout";
    options.AccessDeniedPath = $"/Identity/Account/AccessDenied";
});

// Kiểm tra SecurityStamp mỗi 30 giây — kick user ngay khi bị đổi role/xóa
builder.Services.Configure<SecurityStampValidatorOptions>(options =>
{
    options.ValidationInterval = TimeSpan.FromSeconds(30);
});

builder.Services.AddRazorPages();
builder.Services.AddControllersWithViews();
builder.Services.AddSignalR();

// 3. Đăng ký Repository
builder.Services.AddScoped<IProductRepository, EFProductRepository>();
builder.Services.AddScoped<ICategoryRepository, EFCategoryRepository>();
builder.Services.AddScoped<ICatRepository, EFCatRepository>();

builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("EmailSettings"));
builder.Services.AddTransient<IEmailSender, EmailSender>();
builder.Services.AddScoped<IAuditLogService, AuditLogService>();
builder.Services.AddScoped<ProductKnowledgeService>();
builder.Services.AddScoped<ICatRagChatService, CatRagChatService>();

// Memory cache cho rate limiting
builder.Services.AddMemoryCache();

// HttpClient cho Anthropic API (API key được bảo mật ở backend)
builder.Services.AddHttpClient("AnthropicClient", client =>
{
    client.BaseAddress = new Uri("https://api.anthropic.com");
    client.DefaultRequestHeaders.Add("x-api-key", builder.Configuration["Anthropic:ApiKey"]);
    client.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");
    client.Timeout = TimeSpan.FromSeconds(30);
});

var app = builder.Build();

// THÊM: Gọi khối lệnh Seed Dữ liệu khi Ứng dụng Bắt đầu
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    SeedData.Initialize(services);
}

// THÊM: Tạo user dev tạm thời (EmailConfirmed) để kiểm tra các endpoint yêu cầu đăng nhập
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var email = "dev@meow.local";
        var user = userManager.FindByEmailAsync(email).GetAwaiter().GetResult();
        if (user == null)
        {
            user = new ApplicationUser { UserName = email, Email = email, EmailConfirmed = true, FullName = "Dev Tester" };
            var res = userManager.CreateAsync(user, "Dev@1234").GetAwaiter().GetResult();
        }
    }
    catch { /* ignore in seed if Identity not configured properly */ }
}

// THÊM: Đảm bảo có một số Voucher mẫu (trường hợp SeedData không chạy vì đã có Products)
using (var scope2 = app.Services.CreateScope())
{
    var services2 = scope2.ServiceProvider;
    try
    {
        var db = services2.GetRequiredService<ApplicationDbContext>();
        var now = DateTime.UtcNow;
        var seedVouchers = new[] {
            new Voucher { Code = "MEOW10", DiscountType = "Percent", DiscountPercent = 10, MinOrderAmount = 0, MaxUsage = 100, UsedCount = 0, ExpiryDate = new DateTime(2026, 12, 31), IsActive = true },
            new Voucher { Code = "MEOW50K", DiscountType = "Fixed", DiscountPercent = 50000, MinOrderAmount = 200000, MaxUsage = 50, UsedCount = 0, ExpiryDate = new DateTime(2026, 12, 31), IsActive = true },
            new Voucher { Code = "WELCOME15", DiscountType = "Percent", DiscountPercent = 15, MinOrderAmount = 100000, MaxUsage = 200, UsedCount = 0, ExpiryDate = new DateTime(2026, 9, 30), IsActive = true },
            new Voucher { Code = "KITTY20", DiscountType = "Percent", DiscountPercent = 20, MinOrderAmount = 300000, MaxUsage = 30, UsedCount = 0, ExpiryDate = new DateTime(2026, 8, 1), IsActive = true },
            new Voucher { Code = "FREESHIP", DiscountType = "Fixed", DiscountPercent = 30000, MinOrderAmount = 150000, MaxUsage = 0, UsedCount = 0, ExpiryDate = new DateTime(2026, 6, 30), IsActive = true }
        };

        foreach (var voucher in seedVouchers)
        {
            if (!db.Vouchers.Any(v => v.Code == voucher.Code))
            {
                db.Vouchers.Add(voucher);
            }
        }

        if (db.ChangeTracker.HasChanges())
        {
            db.SaveChanges();
        }
    }
    catch { }
}

// 4. Middleware
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// --- BỔ SUNG USE SESSION (Phải đặt trước Authentication và sau Routing) ---
app.UseSession();
// -----------------------------------------------------------------------

app.UseAuthentication();
app.UseAuthorization();

// 5. Cấu hình Route
app.MapControllerRoute(
    name: "Admin",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapHub<OrderHub>("/orderHub");

app.MapRazorPages();

app.MapControllers();

app.Run();

public partial class Program { }