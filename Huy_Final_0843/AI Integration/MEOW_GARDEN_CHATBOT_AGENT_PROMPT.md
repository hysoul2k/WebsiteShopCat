# 🐾 AGENT TASK — Tích hợp AI Chatbot vào Meow Garden (ASP.NET Core MVC .NET 8)

## MỤC TIÊU
Thêm 2 AI chatbot (MeowBot tư vấn mua hàng & DrPaws sức khỏe mèo) vào web Meow Garden
dưới dạng floating widget góc phải màn hình, gọi Anthropic Claude API qua backend.

---

## BƯỚC 1 — Tạo file Controllers/ChatController.cs

Tạo file mới tại `Controllers/ChatController.cs` với nội dung sau:

```csharp
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using System.Text;
using System.Text.Json;

namespace MeowGarden.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ChatController : ControllerBase
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly IMemoryCache _cache;

        private const string SHOP_SYSTEM = @"Bạn là MeowBot 🐱 — trợ lý tư vấn mua hàng của shop Meow Garden — shop mèo cao cấp phong cách Earthy ấm áp.
Bạn thân thiện, am hiểu về mèo và các sản phẩm cho mèo.
Hãy tư vấn thức ăn, đồ chơi, dụng cụ chăm sóc, và giới thiệu các giống mèo phù hợp.
Luôn gợi ý sản phẩm cụ thể và upsell tự nhiên. Trả lời ngắn gọn, vui vẻ, dùng emoji mèo.
Trả lời bằng tiếng Việt.";

        private const string HEALTH_SYSTEM = @"Bạn là DrPaws 🩺 — trợ lý sức khỏe mèo của shop Meow Garden.
Bạn chuyên nghiệp, đáng tin cậy, hiểu biết về thú y.
Giải đáp triệu chứng, nhắc lịch tiêm phòng, hướng dẫn chăm sóc sức khỏe mèo.
LUÔN khuyến khích gặp bác sĩ thú y khi có dấu hiệu nghiêm trọng.
Trả lời ngắn gọn, dễ hiểu, dùng emoji phù hợp. Trả lời bằng tiếng Việt.";

        public ChatController(IHttpClientFactory httpClientFactory, IConfiguration configuration, IMemoryCache cache)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _cache = cache;
        }

        [HttpPost("send")]
        public async Task<IActionResult> Send([FromBody] ChatRequest request)
        {
            // Rate limiting: 30 requests/hour per IP
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            var cacheKey = $"chat_ratelimit_{ip}";
            var count = _cache.GetOrCreate(cacheKey, entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1);
                return 0;
            });
            if (count >= 30)
                return StatusCode(429, new { error = "Bạn đã gửi quá nhiều tin nhắn. Vui lòng thử lại sau 1 tiếng." });
            _cache.Set(cacheKey, count + 1, TimeSpan.FromHours(1));

            if (request?.Messages == null || request.Messages.Count == 0)
                return BadRequest(new { error = "Tin nhắn không hợp lệ." });

            var systemPrompt = request.Mode == "health" ? HEALTH_SYSTEM : SHOP_SYSTEM;

            try
            {
                var client = _httpClientFactory.CreateClient("AnthropicClient");

                var body = new
                {
                    model = "claude-sonnet-4-20250514",
                    max_tokens = 1024,
                    system = systemPrompt,
                    messages = request.Messages.Select(m => new { role = m.Role, content = m.Content }).ToList()
                };

                var json = JsonSerializer.Serialize(body);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await client.PostAsync("/v1/messages", content);

                if (!response.IsSuccessStatusCode)
                {
                    var err = await response.Content.ReadAsStringAsync();
                    return StatusCode(500, new { error = "Lỗi từ AI service.", detail = err });
                }

                var responseJson = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(responseJson);
                var reply = doc.RootElement
                    .GetProperty("content")[0]
                    .GetProperty("text")
                    .GetString();

                return Ok(new { reply });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Đã xảy ra lỗi.", detail = ex.Message });
            }
        }
    }

    public class ChatRequest
    {
        public string Mode { get; set; } = "shop";
        public List<ChatMessage> Messages { get; set; } = new();
    }

    public class ChatMessage
    {
        public string Role { get; set; } = "user";
        public string Content { get; set; } = "";
    }
}
```

---

## BƯỚC 2 — Sửa Program.cs

Thêm các dòng sau vào `Program.cs`, **trước** dòng `var app = builder.Build();`:

```csharp
// Rate limiting cache
builder.Services.AddMemoryCache();

// Anthropic HttpClient (API key bảo mật ở backend)
builder.Services.AddHttpClient("AnthropicClient", client =>
{
    client.BaseAddress = new Uri("https://api.anthropic.com");
    client.DefaultRequestHeaders.Add("x-api-key", builder.Configuration["Anthropic:ApiKey"]);
    client.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");
    client.Timeout = TimeSpan.FromSeconds(30);
});
```

Kiểm tra sau `var app = builder.Build();` đã có dòng:
```csharp
app.MapControllers(); // Nếu chưa có thì thêm
```

---

## BƯỚC 3 — Sửa appsettings.json

Thêm section `Anthropic` vào `appsettings.json`:

```json
{
  "ConnectionStrings": { },
  "Anthropic": {
    "ApiKey": "PASTE_YOUR_API_KEY_HERE"
  }
}
```

⚠️ Lấy API key tại: https://console.anthropic.com/settings/keys
⚠️ Thêm `appsettings.Production.json` vào `.gitignore` để tránh lộ key!

---

## BƯỚC 4 — Tạo file wwwroot/css/meow-chat-widget.css

Tạo file `wwwroot/css/meow-chat-widget.css` với nội dung sau:

```css
:root {
    --meow-gold: #C8A96E;
    --meow-green: #4A6741;
    --meow-cream: #FBF7F0;
    --meow-orange: #E07B39;
    --meow-teal: #3A8C7E;
    --meow-shadow: 0 8px 40px rgba(0,0,0,0.18);
    --meow-radius: 20px;
}
#meow-chat-toggle {
    position: fixed; bottom: 28px; right: 28px;
    width: 62px; height: 62px; border-radius: 50%;
    background: linear-gradient(135deg, var(--meow-gold), #a8813e);
    border: none; cursor: pointer; font-size: 28px;
    box-shadow: 0 4px 20px rgba(200,169,110,0.5);
    z-index: 9999; transition: transform 0.2s ease, box-shadow 0.2s ease;
    display: flex; align-items: center; justify-content: center;
}
#meow-chat-toggle:hover { transform: scale(1.1); box-shadow: 0 6px 28px rgba(200,169,110,0.65); }
#meow-chat-toggle.open { transform: rotate(15deg) scale(1.05); }
#meow-chat-badge {
    position: absolute; top: -2px; right: -2px;
    width: 18px; height: 18px; background: #e74c3c;
    border-radius: 50%; border: 2px solid #fff; display: none;
}
#meow-chat-badge.show { display: block; }
#meow-chat-popup {
    position: fixed; bottom: 104px; right: 28px;
    width: 400px; height: 540px;
    background: var(--meow-cream); border-radius: var(--meow-radius);
    box-shadow: var(--meow-shadow); z-index: 9998;
    display: none; flex-direction: column; overflow: hidden;
    animation: meowSlideUp 0.3s cubic-bezier(0.34, 1.56, 0.64, 1);
    font-family: 'DM Sans', sans-serif;
}
#meow-chat-popup.show { display: flex; }
@keyframes meowSlideUp {
    from { opacity: 0; transform: translateY(20px) scale(0.95); }
    to   { opacity: 1; transform: translateY(0) scale(1); }
}
.meow-header { padding: 16px 20px 0; background: var(--meow-cream); border-bottom: 1px solid rgba(0,0,0,0.07); }
.meow-header-top { display: flex; align-items: center; justify-content: space-between; margin-bottom: 12px; }
.meow-brand { font-family: 'Cormorant Garamond', serif; font-size: 20px; font-weight: 700; color: var(--meow-green); letter-spacing: 0.5px; }
.meow-close { background: none; border: none; font-size: 20px; cursor: pointer; color: #999; padding: 0 4px; line-height: 1; transition: color 0.2s; }
.meow-close:hover { color: #333; }
.meow-tabs { display: flex; gap: 8px; padding-bottom: 14px; }
.meow-tab { flex: 1; padding: 9px 12px; border-radius: 12px; border: 2px solid transparent; background: #f0ece4; cursor: pointer; font-family: 'DM Sans', sans-serif; font-size: 13px; font-weight: 600; color: #888; transition: all 0.2s ease; text-align: center; }
.meow-tab:hover { background: #e8e2d6; color: #555; }
.meow-tab.active-shop { background: linear-gradient(135deg, #f5e6c8, #ead4a0); border-color: var(--meow-gold); color: #7a5c1e; box-shadow: 0 2px 10px rgba(200,169,110,0.3); }
.meow-tab.active-health { background: linear-gradient(135deg, #d4ede9, #a8d5cd); border-color: var(--meow-teal); color: #1e5a50; box-shadow: 0 2px 10px rgba(58,140,126,0.25); }
.meow-bot-bar { display: flex; align-items: center; gap: 10px; padding: 10px 20px; border-bottom: 1px solid rgba(0,0,0,0.06); }
.meow-bot-avatar { width: 36px; height: 36px; border-radius: 50%; display: flex; align-items: center; justify-content: center; font-size: 18px; flex-shrink: 0; }
.meow-bot-avatar.shop { background: linear-gradient(135deg, #f5e6c8, #e8c97e); }
.meow-bot-avatar.health { background: linear-gradient(135deg, #d4ede9, #7ccfc3); }
.meow-bot-name { font-family: 'Cormorant Garamond', serif; font-size: 16px; font-weight: 700; color: #333; }
.meow-bot-status { font-size: 11px; color: #888; font-weight: 500; }
.meow-online-dot { width: 8px; height: 8px; background: #27ae60; border-radius: 50%; margin-left: auto; box-shadow: 0 0 6px #27ae60; flex-shrink: 0; }
.meow-messages { flex: 1; overflow-y: auto; padding: 16px; display: flex; flex-direction: column; gap: 12px; scroll-behavior: smooth; }
.meow-messages::-webkit-scrollbar { width: 4px; }
.meow-messages::-webkit-scrollbar-thumb { background: #ddd; border-radius: 4px; }
.meow-msg { display: flex; align-items: flex-end; gap: 8px; }
.meow-msg.user { flex-direction: row-reverse; }
.meow-bubble { max-width: 78%; padding: 11px 15px; border-radius: 18px; font-size: 13.5px; line-height: 1.55; font-weight: 500; white-space: pre-wrap; word-break: break-word; }
.meow-msg.user .meow-bubble { background: var(--meow-green); color: #fff; border-radius: 18px 18px 4px 18px; box-shadow: 0 2px 10px rgba(74,103,65,0.3); }
.meow-msg.bot .meow-bubble { background: #fff; color: #333; border-radius: 18px 18px 18px 4px; box-shadow: 0 2px 10px rgba(0,0,0,0.08); }
.meow-typing .meow-bubble { padding: 14px 18px; }
.meow-dots { display: flex; gap: 5px; align-items: center; }
.meow-dots span { width: 7px; height: 7px; border-radius: 50%; background: #bbb; animation: meowBounce 1.2s ease-in-out infinite; }
.meow-dots span:nth-child(2) { animation-delay: 0.2s; }
.meow-dots span:nth-child(3) { animation-delay: 0.4s; }
@keyframes meowBounce { 0%, 80%, 100% { transform: translateY(0); opacity: 0.5; } 40% { transform: translateY(-6px); opacity: 1; } }
.meow-quick { padding: 10px 16px 6px; display: flex; flex-wrap: wrap; gap: 6px; border-top: 1px solid rgba(0,0,0,0.06); }
.meow-quick-btn { padding: 6px 12px; border-radius: 20px; border: 1.5px solid #ddd; background: #fff; font-family: 'DM Sans', sans-serif; font-size: 11.5px; font-weight: 600; color: #666; cursor: pointer; transition: all 0.18s ease; white-space: nowrap; }
.meow-quick-btn:hover { border-color: var(--meow-gold); color: #7a5c1e; background: #fdf8ef; transform: translateY(-1px); }
.meow-input-area { padding: 10px 14px 14px; display: flex; gap: 8px; align-items: flex-end; background: var(--meow-cream); }
#meow-input { flex: 1; border: 2px solid #e8e2d6; border-radius: 14px; padding: 10px 14px; font-family: 'DM Sans', sans-serif; font-size: 13.5px; font-weight: 500; color: #333; resize: none; background: #fff; transition: border-color 0.2s; line-height: 1.4; max-height: 100px; overflow-y: auto; }
#meow-input:focus { outline: none; border-color: var(--meow-gold); }
#meow-input::placeholder { color: #bbb; }
#meow-send { width: 44px; height: 44px; border-radius: 50%; border: none; background: var(--meow-green); color: #fff; font-size: 16px; cursor: pointer; display: flex; align-items: center; justify-content: center; flex-shrink: 0; transition: all 0.2s ease; box-shadow: 0 3px 12px rgba(74,103,65,0.35); }
#meow-send:hover { transform: scale(1.08); background: #3a5433; }
#meow-send:disabled { background: #ccc; cursor: not-allowed; box-shadow: none; transform: none; }
@media (max-width: 480px) {
    #meow-chat-popup { bottom: 0; right: 0; width: 100%; height: 85vh; border-radius: var(--meow-radius) var(--meow-radius) 0 0; }
    #meow-chat-toggle { bottom: 20px; right: 20px; }
}
```

---

## BƯỚC 5 — Tạo file wwwroot/js/meow-chat-widget.js

Tạo file `wwwroot/js/meow-chat-widget.js` với nội dung sau:

```javascript
(function () {
    'use strict';

    const QUICK_SHOP = ['Mèo 3 tháng ăn gì?','Tư vấn giống mèo chung cư','Đồ dùng cơ bản cho mèo mới','Sản phẩm tắm cho mèo'];
    const QUICK_HEALTH = ['Mèo bỏ ăn phải làm gì?','Lịch tiêm vaccine mèo con','Mèo hay nôn có sao không?','Cách phòng bệnh cho mèo'];
    const WELCOME = {
        shop: 'Chào bạn! Mình là MeowBot 🐱 Bạn đang tìm gì cho bé mèo hôm nay?',
        health: 'Xin chào! Mình là DrPaws 🩺 Bé mèo nhà bạn có vấn đề gì cần tư vấn không?',
    };

    let currentMode = 'shop';
    let isLoading = false;
    const history = { shop: [], health: [] };

    function buildWidget() {
        const html = `
        <button id="meow-chat-toggle" aria-label="Mở chat hỗ trợ">🐾<span id="meow-chat-badge"></span></button>
        <div id="meow-chat-popup" role="dialog" aria-label="Chat hỗ trợ Meow Garden">
            <div class="meow-header">
                <div class="meow-header-top">
                    <span class="meow-brand">Meow Garden AI</span>
                    <button class="meow-close" id="meow-close-btn" aria-label="Đóng">✕</button>
                </div>
                <div class="meow-tabs">
                    <button class="meow-tab active-shop" data-mode="shop">🐱 MeowBot<br><small style="font-weight:500;opacity:.8">Mua hàng & tư vấn</small></button>
                    <button class="meow-tab" data-mode="health">🩺 DrPaws<br><small style="font-weight:500;opacity:.8">Sức khỏe mèo</small></button>
                </div>
            </div>
            <div class="meow-bot-bar">
                <div class="meow-bot-avatar shop" id="meow-bot-avatar">🐱</div>
                <div>
                    <div class="meow-bot-name" id="meow-bot-name">MeowBot</div>
                    <div class="meow-bot-status" id="meow-bot-status">Tư vấn mua hàng & sản phẩm mèo</div>
                </div>
                <div class="meow-online-dot"></div>
            </div>
            <div class="meow-messages" id="meow-messages"></div>
            <div class="meow-quick" id="meow-quick"></div>
            <div class="meow-input-area">
                <textarea id="meow-input" rows="1" placeholder="Nhắn tin với MeowBot..."></textarea>
                <button id="meow-send" disabled>➤</button>
            </div>
        </div>`;
        const container = document.createElement('div');
        container.innerHTML = html;
        document.body.appendChild(container);
    }

    function renderMessages() {
        const box = document.getElementById('meow-messages');
        const msgs = history[currentMode];
        box.innerHTML = '';
        if (msgs.length === 0) { appendBotBubble(WELCOME[currentMode], box); }
        else { msgs.forEach(m => { if (m.role === 'user') appendUserBubble(m.content, box); else appendBotBubble(m.content, box); }); }
        scrollBottom();
    }

    function appendUserBubble(text, box) {
        const div = document.createElement('div');
        div.className = 'meow-msg user';
        div.innerHTML = `<div class="meow-bubble">${escapeHtml(text)}</div>`;
        box.appendChild(div);
    }

    function appendBotBubble(text, box) {
        const avatar = currentMode === 'shop' ? '🐱' : '🩺';
        const div = document.createElement('div');
        div.className = 'meow-msg bot';
        div.innerHTML = `<div class="meow-bot-avatar ${currentMode}" style="width:30px;height:30px;font-size:15px;flex-shrink:0">${avatar}</div><div class="meow-bubble">${escapeHtml(text)}</div>`;
        box.appendChild(div);
    }

    function showTyping(box) {
        const div = document.createElement('div');
        div.className = 'meow-msg bot meow-typing';
        div.id = 'meow-typing-indicator';
        const avatar = currentMode === 'shop' ? '🐱' : '🩺';
        div.innerHTML = `<div class="meow-bot-avatar ${currentMode}" style="width:30px;height:30px;font-size:15px;flex-shrink:0">${avatar}</div><div class="meow-bubble"><div class="meow-dots"><span></span><span></span><span></span></div></div>`;
        box.appendChild(div);
        scrollBottom();
    }

    function hideTyping() { const el = document.getElementById('meow-typing-indicator'); if (el) el.remove(); }

    function renderQuickReplies() {
        const container = document.getElementById('meow-quick');
        const list = currentMode === 'shop' ? QUICK_SHOP : QUICK_HEALTH;
        container.innerHTML = list.map(q => `<button class="meow-quick-btn" data-q="${escapeAttr(q)}">${escapeHtml(q)}</button>`).join('');
        container.querySelectorAll('.meow-quick-btn').forEach(btn => {
            btn.addEventListener('click', () => { if (!isLoading) sendMessage(btn.dataset.q); });
        });
    }

    function switchMode(mode) {
        currentMode = mode;
        document.querySelectorAll('.meow-tab').forEach(t => {
            t.classList.remove('active-shop', 'active-health');
            if (t.dataset.mode === mode) t.classList.add(`active-${mode}`);
        });
        const avatar = document.getElementById('meow-bot-avatar');
        const name = document.getElementById('meow-bot-name');
        const status = document.getElementById('meow-bot-status');
        const input = document.getElementById('meow-input');
        if (mode === 'shop') { avatar.textContent='🐱'; avatar.className='meow-bot-avatar shop'; name.textContent='MeowBot'; status.textContent='Tư vấn mua hàng & sản phẩm mèo'; input.placeholder='Hỏi về sản phẩm, giống mèo...'; }
        else { avatar.textContent='🩺'; avatar.className='meow-bot-avatar health'; name.textContent='DrPaws'; status.textContent='Hỗ trợ sức khỏe thú cưng'; input.placeholder='Mô tả triệu chứng của bé mèo...'; }
        renderMessages();
        renderQuickReplies();
    }

    async function sendMessage(text) {
        text = (text || '').trim();
        if (!text || isLoading) return;
        isLoading = true; setInputDisabled(true);
        history[currentMode].push({ role: 'user', content: text });
        const box = document.getElementById('meow-messages');
        appendUserBubble(text, box);
        showTyping(box);
        document.getElementById('meow-input').value = '';
        try {
            const response = await fetch('/api/chat/send', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ mode: currentMode, messages: history[currentMode] }),
            });
            hideTyping();
            if (response.status === 429) { const err = await response.json(); appendBotBubble(err.error || 'Bạn gửi quá nhiều tin nhắn rồi! Thử lại sau nhé 😅', box); }
            else if (!response.ok) { appendBotBubble('Ôi, mình gặp sự cố rồi! Thử lại nhé 🙏', box); }
            else { const data = await response.json(); history[currentMode].push({ role: 'assistant', content: data.reply }); appendBotBubble(data.reply, box); }
        } catch (err) { hideTyping(); appendBotBubble('Không kết nối được. Vui lòng thử lại sau! 🙏', box); console.error('[MeowChat] Error:', err); }
        isLoading = false; setInputDisabled(false); scrollBottom();
        document.getElementById('meow-input').focus();
    }

    function scrollBottom() { const box = document.getElementById('meow-messages'); if (box) box.scrollTop = box.scrollHeight; }
    function setInputDisabled(disabled) { document.getElementById('meow-input').disabled = disabled; document.getElementById('meow-send').disabled = disabled; }
    function escapeHtml(str) { return str.replace(/&/g,'&amp;').replace(/</g,'&lt;').replace(/>/g,'&gt;').replace(/"/g,'&quot;').replace(/\n/g,'<br>'); }
    function escapeAttr(str) { return str.replace(/"/g,'&quot;'); }

    function togglePopup() {
        const popup = document.getElementById('meow-chat-popup');
        const btn = document.getElementById('meow-chat-toggle');
        const badge = document.getElementById('meow-chat-badge');
        const isOpen = popup.classList.contains('show');
        if (isOpen) { popup.classList.remove('show'); btn.classList.remove('open'); }
        else { popup.classList.add('show'); btn.classList.add('open'); badge.classList.remove('show'); renderMessages(); renderQuickReplies(); setTimeout(() => document.getElementById('meow-input').focus(), 300); }
    }

    function init() {
        buildWidget();
        document.getElementById('meow-chat-toggle').addEventListener('click', togglePopup);
        document.getElementById('meow-close-btn').addEventListener('click', togglePopup);
        document.querySelectorAll('.meow-tab').forEach(tab => { tab.addEventListener('click', () => switchMode(tab.dataset.mode)); });
        document.getElementById('meow-send').addEventListener('click', () => { sendMessage(document.getElementById('meow-input').value); });
        document.getElementById('meow-input').addEventListener('keydown', e => { if (e.key === 'Enter' && !e.shiftKey) { e.preventDefault(); sendMessage(e.target.value); } });
        document.getElementById('meow-input').addEventListener('input', e => { document.getElementById('meow-send').disabled = !e.target.value.trim() || isLoading; });
        document.addEventListener('click', e => {
            const popup = document.getElementById('meow-chat-popup');
            const toggle = document.getElementById('meow-chat-toggle');
            if (popup.classList.contains('show') && !popup.contains(e.target) && !toggle.contains(e.target)) { popup.classList.remove('show'); toggle.classList.remove('open'); }
        });
        setTimeout(() => { if (!document.getElementById('meow-chat-popup').classList.contains('show')) { document.getElementById('meow-chat-badge').classList.add('show'); } }, 3000);
    }

    if (document.readyState === 'loading') { document.addEventListener('DOMContentLoaded', init); } else { init(); }
})();
```

---

## BƯỚC 6 — Sửa Views/Shared/_Layout.cshtml

Thêm đoạn sau ngay TRƯỚC thẻ đóng `</body>`:

```razor
@if (!(ViewContext.RouteData.Values["area"]?.ToString()
        ?.Equals("Admin", StringComparison.OrdinalIgnoreCase) ?? false))
{
    <link rel="stylesheet" href="~/css/meow-chat-widget.css" asp-append-version="true" />
    <script src="~/js/meow-chat-widget.js" asp-append-version="true"></script>
}
```

---

## CHECKLIST HOÀN THÀNH

```
□ Controllers/ChatController.cs — tạo xong
□ Program.cs — thêm AddMemoryCache() và AddHttpClient("AnthropicClient")
□ appsettings.json — thêm section "Anthropic": { "ApiKey": "..." }
□ .gitignore — thêm appsettings.Production.json
□ wwwroot/css/meow-chat-widget.css — tạo xong
□ wwwroot/js/meow-chat-widget.js — tạo xong
□ Views/Shared/_Layout.cshtml — nhúng link + script trước </body>
□ dotnet build — không lỗi
□ Test POST /api/chat/send bằng Postman
□ Test widget trên trình duyệt (desktop + mobile)
```

---

## TEST NHANH VỚI POSTMAN / HTTP FILE

```http
POST https://localhost:7xxx/api/chat/send
Content-Type: application/json

{
  "mode": "shop",
  "messages": [
    { "role": "user", "content": "Mèo 3 tháng tuổi nên ăn gì?" }
  ]
}
```

Expected response:
```json
{ "reply": "Mèo 3 tháng tuổi nên ăn..." }
```
