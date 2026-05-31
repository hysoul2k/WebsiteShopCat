/* =====================================================
   Meow Garden — Chat Widget JS (v2.0)
   File: wwwroot/js/meow-chat-widget.js
   Vanilla JS, gọi /api/chat/send
   Features: AbortController timeout, retry, better UX
   ===================================================== */

(function () {
    'use strict';

    // ── Config ──────────────────────────────────────────
    const API_URL = '/api/chat/send';
    const REQUEST_TIMEOUT_MS = 28000;  // 28s (server has 25s)
    const MAX_RETRIES = 1;

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
        shop: 'Chào bạn! Mình là MeowSales 🐱 Bạn đang tìm gì cho bé mèo hôm nay? Mình biết hết sản phẩm trên shop luôn nè!',
        health: 'Xin chào! Mình là MeowHealth 🩺 Bé mèo nhà bạn có vấn đề gì cần tư vấn không?',
    };
    const ERROR_MESSAGES = {
        timeout: 'Mình đang nghĩ hơi lâu, bạn thử gửi lại nhé! ⏳',
        network: 'Không kết nối được. Kiểm tra mạng và thử lại nhé! 📡',
        rateLimit: 'Bạn gửi quá nhiều tin nhắn rồi! Thử lại sau nhé 😅',
        server: 'Ôi, mình gặp sự cố rồi! Thử lại nhé 🙏',
    };

    // ── State ────────────────────────────────────────────
    let currentMode = 'shop';
    let isLoading = false;
    let currentAbortController = null;
    const history = { shop: [], health: [] };
    const sessionId = 'meow_' + Date.now() + '_' + Math.random().toString(36).slice(2, 8);

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
                    <button class="meow-tab active-shop" data-mode="shop">🐱 MeowSales<br><small style="font-weight:500;opacity:.8">Mua hàng & tư vấn</small></button>
                    <button class="meow-tab" data-mode="health">🩺 MeowHealth<br><small style="font-weight:500;opacity:.8">Sức khỏe mèo</small></button>
                </div>
            </div>

            <!-- Bot info bar -->
            <div class="meow-bot-bar">
                <div class="meow-bot-avatar shop" id="meow-bot-avatar">🐱</div>
                <div>
                    <div class="meow-bot-name" id="meow-bot-name">MeowSales</div>
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
                <textarea id="meow-input" rows="1" placeholder="Nhắn tin với MeowSales..."></textarea>
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

    // ── Escape helpers ──────────────────────────────────
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
        if (isLoading) return; // Prevent switching during loading
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
            name.textContent = 'MeowSales';
            status.textContent = 'Tư vấn mua hàng & sản phẩm mèo';
            input.placeholder = 'Hỏi về sản phẩm, giống mèo...';
        } else {
            avatar.textContent = '🩺';
            avatar.className = 'meow-bot-avatar health';
            name.textContent = 'MeowHealth';
            status.textContent = 'Hỗ trợ sức khỏe thú cưng';
            input.placeholder = 'Mô tả triệu chứng của bé mèo...';
        }

        renderMessages();
        renderQuickReplies();
    }

    // ── API Call with timeout and retry ──────────────────
    async function callApi(payload, retryCount = 0) {
        // Abort any previous pending request
        if (currentAbortController) {
            currentAbortController.abort();
        }
        currentAbortController = new AbortController();
        const signal = currentAbortController.signal;

        // Set timeout
        const timeoutId = setTimeout(() => {
            currentAbortController.abort();
        }, REQUEST_TIMEOUT_MS);

        try {
            const response = await fetch(API_URL, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(payload),
                signal: signal,
            });

            clearTimeout(timeoutId);

            if (response.status === 429) {
                const err = await response.json();
                return { error: err.error || ERROR_MESSAGES.rateLimit, type: 'rateLimit' };
            }

            if (response.status === 504) {
                // Server timeout — retry if possible
                if (retryCount < MAX_RETRIES) {
                    console.log(`[MeowChat] Server timeout, retrying (${retryCount + 1}/${MAX_RETRIES})...`);
                    return callApi(payload, retryCount + 1);
                }
                return { error: ERROR_MESSAGES.timeout, type: 'timeout' };
            }

            if (!response.ok) {
                if (retryCount < MAX_RETRIES) {
                    console.log(`[MeowChat] Error ${response.status}, retrying (${retryCount + 1}/${MAX_RETRIES})...`);
                    await new Promise(r => setTimeout(r, 500));
                    return callApi(payload, retryCount + 1);
                }
                return { error: ERROR_MESSAGES.server, type: 'server' };
            }

            const data = await response.json();
            return { reply: data.reply, type: 'success' };

        } catch (err) {
            clearTimeout(timeoutId);

            if (err.name === 'AbortError') {
                if (retryCount < MAX_RETRIES) {
                    console.log(`[MeowChat] Timeout, retrying (${retryCount + 1}/${MAX_RETRIES})...`);
                    return callApi(payload, retryCount + 1);
                }
                return { error: ERROR_MESSAGES.timeout, type: 'timeout' };
            }

            // Network error
            if (retryCount < MAX_RETRIES) {
                console.log(`[MeowChat] Network error, retrying (${retryCount + 1}/${MAX_RETRIES})...`);
                await new Promise(r => setTimeout(r, 1000));
                return callApi(payload, retryCount + 1);
            }

            console.error('[MeowChat] Error:', err);
            return { error: ERROR_MESSAGES.network, type: 'network' };
        }
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

        // Update status to "đang trả lời..."
        const statusEl = document.getElementById('meow-bot-status');
        const prevStatus = statusEl.textContent;
        statusEl.textContent = 'Đang trả lời...';

        // Call API with retry — gửi tối đa 6 turns gần nhất để Gemini có conversation context
        // history[currentMode] đã có current message (đã push ở trên)
        const recentHistory = history[currentMode].slice(-6);
        const result = await callApi({
            mode: currentMode,
            sessionId: sessionId,
            messages: recentHistory
        });

        hideTyping();
        statusEl.textContent = prevStatus;

        if (result.type === 'success' && result.reply) {
            history[currentMode].push({ role: 'assistant', content: result.reply });
            appendBotBubble(result.reply, box);
        } else {
            appendBotBubble(result.error || ERROR_MESSAGES.server, box);
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
            if (popup && popup.classList.contains('show') &&
                !popup.contains(e.target) &&
                !toggle.contains(e.target)) {
                popup.classList.remove('show');
                toggle.classList.remove('open');
            }
        });

        // Show badge after 3s if not opened (subtle nudge)
        setTimeout(() => {
            const popup = document.getElementById('meow-chat-popup');
            if (popup && !popup.classList.contains('show')) {
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
