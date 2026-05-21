/* =====================================================
   Meow Garden — Chat Widget JS
   File: wwwroot/js/meow-chat-widget.js
   Vanilla JS + jQuery compatible, gọi /api/chat/send
   ===================================================== */

(function () {
    'use strict';

    // ── Config ──────────────────────────────────────────
    const QUICK_SHOP = [
        'Mèo 3 tháng ăn gì?',
        'Tư vấn giống mèo chung cư',
        'Đồ dùng cơ bản cho mèo mới',
        'Sản phẩm tắm cho mèo',
    ];
    const QUICK_HEALTH = [
        'Mèo bỏ ăn phải làm gì?',
        'Lịch tiêm vaccine mèo con',
        'Mèo hay nôn có sao không?',
        'Cách phòng bệnh cho mèo',
    ];
    const WELCOME = {
        shop: 'Chào bạn! Mình là MeowBot 🐱 Bạn đang tìm gì cho bé mèo hôm nay?',
        health: 'Xin chào! Mình là DrPaws 🩺 Bé mèo nhà bạn có vấn đề gì cần tư vấn không?',
    };

    // ── State ────────────────────────────────────────────
    let currentMode = 'shop';
    let isLoading = false;
    const history = { shop: [], health: [] };

    // ── Build HTML ───────────────────────────────────────
    function buildWidget() {
        const html = `
        <!-- Toggle button -->
        <button id="meow-chat-toggle" aria-label="Mở chat hỗ trợ">
            🐾
            <span id="meow-chat-badge"></span>
        </button>

        <!-- Popup -->
        <div id="meow-chat-popup" role="dialog" aria-label="Chat hỗ trợ Meow Garden">
            <!-- Header -->
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

            <!-- Bot info bar -->
            <div class="meow-bot-bar">
                <div class="meow-bot-avatar shop" id="meow-bot-avatar">🐱</div>
                <div>
                    <div class="meow-bot-name" id="meow-bot-name">MeowBot</div>
                    <div class="meow-bot-status" id="meow-bot-status">Tư vấn mua hàng & sản phẩm mèo</div>
                </div>
                <div class="meow-online-dot"></div>
            </div>

            <!-- Messages -->
            <div class="meow-messages" id="meow-messages"></div>

            <!-- Quick replies -->
            <div class="meow-quick" id="meow-quick"></div>

            <!-- Input -->
            <div class="meow-input-area">
                <textarea id="meow-input" rows="1" placeholder="Nhắn tin với MeowBot..."></textarea>
                <button id="meow-send" disabled>➤</button>
            </div>
        </div>`;

        const container = document.createElement('div');
        container.innerHTML = html;
        document.body.appendChild(container);
    }

    // ── Render messages ──────────────────────────────────
    function renderMessages() {
        const box = document.getElementById('meow-messages');
        const msgs = history[currentMode];
        box.innerHTML = '';

        if (msgs.length === 0) {
            appendBotBubble(WELCOME[currentMode], box);
        } else {
            msgs.forEach(m => {
                if (m.role === 'user') appendUserBubble(m.content, box);
                else appendBotBubble(m.content, box);
            });
        }
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
        div.innerHTML = `
            <div class="meow-bot-avatar ${currentMode}" style="width:30px;height:30px;font-size:15px;flex-shrink:0">${avatar}</div>
            <div class="meow-bubble">${escapeHtml(text)}</div>`;
        box.appendChild(div);
    }

    function showTyping(box) {
        const div = document.createElement('div');
        div.className = 'meow-msg bot meow-typing';
        div.id = 'meow-typing-indicator';
        const avatar = currentMode === 'shop' ? '🐱' : '🩺';
        div.innerHTML = `
            <div class="meow-bot-avatar ${currentMode}" style="width:30px;height:30px;font-size:15px;flex-shrink:0">${avatar}</div>
            <div class="meow-bubble">
                <div class="meow-dots"><span></span><span></span><span></span></div>
            </div>`;
        box.appendChild(div);
        scrollBottom();
    }

    function hideTyping() {
        const el = document.getElementById('meow-typing-indicator');
        if (el) el.remove();
    }

    // ── Quick replies ────────────────────────────────────
    function renderQuickReplies() {
        const container = document.getElementById('meow-quick');
        const list = currentMode === 'shop' ? QUICK_SHOP : QUICK_HEALTH;
        container.innerHTML = list.map(q =>
            `<button class="meow-quick-btn" data-q="${escapeAttr(q)}">${escapeHtml(q)}</button>`
        ).join('');

        container.querySelectorAll('.meow-quick-btn').forEach(btn => {
            btn.addEventListener('click', () => {
                if (!isLoading) sendMessage(btn.dataset.q);
            });
        });
    }

    // ── Switch mode ──────────────────────────────────────
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

        if (mode === 'shop') {
            avatar.textContent = '🐱';
            avatar.className = 'meow-bot-avatar shop';
            name.textContent = 'MeowBot';
            status.textContent = 'Tư vấn mua hàng & sản phẩm mèo';
            input.placeholder = 'Hỏi về sản phẩm, giống mèo...';
        } else {
            avatar.textContent = '🩺';
            avatar.className = 'meow-bot-avatar health';
            name.textContent = 'DrPaws';
            status.textContent = 'Hỗ trợ sức khỏe thú cưng';
            input.placeholder = 'Mô tả triệu chứng của bé mèo...';
        }

        renderMessages();
        renderQuickReplies();
    }

    // ── Send message ─────────────────────────────────────
    async function sendMessage(text) {
        text = (text || '').trim();
        if (!text || isLoading) return;

        isLoading = true;
        setInputDisabled(true);

        // Add to history & render
        history[currentMode].push({ role: 'user', content: text });
        const box = document.getElementById('meow-messages');
        appendUserBubble(text, box);
        showTyping(box);
        document.getElementById('meow-input').value = '';

        try {
            const response = await fetch('/api/chat/send', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({
                    mode: currentMode,
                    messages: history[currentMode],
                }),
            });

            hideTyping();

            if (response.status === 429) {
                const err = await response.json();
                appendBotBubble(err.error || 'Bạn gửi quá nhiều tin nhắn rồi! Thử lại sau nhé 😅', box);
            } else if (!response.ok) {
                appendBotBubble('Ôi, mình gặp sự cố rồi! Thử lại nhé 🙏', box);
            } else {
                const data = await response.json();
                history[currentMode].push({ role: 'assistant', content: data.reply });
                appendBotBubble(data.reply, box);
            }
        } catch (err) {
            hideTyping();
            appendBotBubble('Không kết nối được. Vui lòng thử lại sau! 🙏', box);
            console.error('[MeowChat] Error:', err);
        }

        isLoading = false;
        setInputDisabled(false);
        scrollBottom();
        document.getElementById('meow-input').focus();
    }

    // ── Helpers ──────────────────────────────────────────
    function scrollBottom() {
        const box = document.getElementById('meow-messages');
        if (box) box.scrollTop = box.scrollHeight;
    }

    function setInputDisabled(disabled) {
        document.getElementById('meow-input').disabled = disabled;
        document.getElementById('meow-send').disabled = disabled;
    }

    function escapeHtml(str) {
        return str
            .replace(/&/g, '&amp;')
            .replace(/</g, '&lt;')
            .replace(/>/g, '&gt;')
            .replace(/"/g, '&quot;')
            .replace(/\n/g, '<br>');
    }

    function escapeAttr(str) {
        return str.replace(/"/g, '&quot;');
    }

    // ── Toggle popup ─────────────────────────────────────
    function togglePopup() {
        const popup = document.getElementById('meow-chat-popup');
        const btn = document.getElementById('meow-chat-toggle');
        const badge = document.getElementById('meow-chat-badge');
        const isOpen = popup.classList.contains('show');

        if (isOpen) {
            popup.classList.remove('show');
            btn.classList.remove('open');
        } else {
            popup.classList.add('show');
            btn.classList.add('open');
            badge.classList.remove('show');
            renderMessages();
            renderQuickReplies();
            setTimeout(() => document.getElementById('meow-input').focus(), 300);
        }
    }

    // ── Init ─────────────────────────────────────────────
    function init() {
        buildWidget();

        // Toggle open/close
        document.getElementById('meow-chat-toggle').addEventListener('click', togglePopup);
        document.getElementById('meow-close-btn').addEventListener('click', togglePopup);

        // Tab switching
        document.querySelectorAll('.meow-tab').forEach(tab => {
            tab.addEventListener('click', () => switchMode(tab.dataset.mode));
        });

        // Send on button click
        document.getElementById('meow-send').addEventListener('click', () => {
            sendMessage(document.getElementById('meow-input').value);
        });

        // Send on Enter (Shift+Enter = newline)
        document.getElementById('meow-input').addEventListener('keydown', e => {
            if (e.key === 'Enter' && !e.shiftKey) {
                e.preventDefault();
                sendMessage(e.target.value);
            }
        });

        // Enable/disable send button based on input
        document.getElementById('meow-input').addEventListener('input', e => {
            document.getElementById('meow-send').disabled = !e.target.value.trim() || isLoading;
        });

        // Close popup when clicking outside
        document.addEventListener('click', e => {
            const popup = document.getElementById('meow-chat-popup');
            const toggle = document.getElementById('meow-chat-toggle');
            if (popup.classList.contains('show') &&
                !popup.contains(e.target) &&
                !toggle.contains(e.target)) {
                popup.classList.remove('show');
                toggle.classList.remove('open');
            }
        });

        // Show badge after 3s if not opened (subtle nudge)
        setTimeout(() => {
            const popup = document.getElementById('meow-chat-popup');
            if (!popup.classList.contains('show')) {
                document.getElementById('meow-chat-badge').classList.add('show');
            }
        }, 3000);
    }

    // Run after DOM ready
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', init);
    } else {
        init();
    }
})();
