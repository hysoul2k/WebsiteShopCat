// =====================================================
// THÊM VÀO Program.cs — đặt trước var app = builder.Build();
// =====================================================

// 1. Memory cache cho rate limiting
builder.Services.AddMemoryCache();

// 2. HttpClient cho Anthropic API (API key được bảo mật ở backend)
builder.Services.AddHttpClient("AnthropicClient", client =>
{
    client.BaseAddress = new Uri("https://api.anthropic.com");
    client.DefaultRequestHeaders.Add("x-api-key", builder.Configuration["Anthropic:ApiKey"]);
    client.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");
    client.Timeout = TimeSpan.FromSeconds(30);
});

// 3. Đảm bảo có AddControllers() nếu chưa có (project MVC thường đã có)
// builder.Services.AddControllers(); // bỏ comment nếu chưa có dòng này

// =====================================================
// THÊM VÀO sau var app = builder.Build();
// =====================================================

// Map API routes (nếu chưa có MapControllers)
// app.MapControllers(); // bỏ comment nếu chưa có dòng này
