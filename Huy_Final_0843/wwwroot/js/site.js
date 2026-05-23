// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

const connection = new signalR.HubConnectionBuilder()
    .withUrl("/stockHub")
    .withAutomaticReconnect()
    .build();

connection.on("UpdateStock", (productId, newStock) => {
    // Cập nhật badge stock trên tất cả card/nút đang hiển thị
    document.querySelectorAll(`[data-product-id="${productId}"]`)
        .forEach(el => {
            const stockBadge = el.querySelector(".stock-badge");
            const addBtn = el.querySelector(".btn-add-cart");

            if (stockBadge) stockBadge.textContent = newStock + " còn lại";

            if (newStock <= 0) {
                if (addBtn) {
                    addBtn.disabled = true;
                    addBtn.textContent = "Hết hàng";
                    addBtn.classList.add("btn-disabled");
                }
            }
        });
});

connection.start().catch(err => console.error("SignalR Error:", err));

// --- TOAST NOTIFICATIONS NHẤT QUÁN ---
window.showToast = function(message, type = 'success') {
    if (typeof Swal !== 'undefined') {
        const Toast = Swal.mixin({
            toast: true,
            position: 'top-end',
            showConfirmButton: false,
            timer: 3000,
            timerProgressBar: true
        });

        Toast.fire({
            icon: type,
            title: message
        });
    } else {
        alert(message);
    }
}

// --- GLOBAL LOADING STATES CHO FORM SUBMIT ---
document.addEventListener("DOMContentLoaded", function () {
    const forms = document.querySelectorAll('form');
    forms.forEach(form => {
        form.addEventListener('submit', function (e) {
            // Không áp dụng cho các form có class 'no-spinner'
            if (this.classList.contains('no-spinner')) return;

            const submitBtn = this.querySelector('button[type="submit"], input[type="submit"]');
            if (submitBtn) {
                // Tránh disable nếu form chưa hợp lệ HTML5
                if (!this.checkValidity()) return;
                
                const originalText = submitBtn.innerHTML || submitBtn.value;
                submitBtn.disabled = true;
                submitBtn.dataset.originalText = originalText;
                
                if (submitBtn.tagName === 'INPUT') {
                    submitBtn.value = 'Đang xử lý...';
                } else {
                    submitBtn.innerHTML = '<span class="spinner-border spinner-border-sm" role="status" aria-hidden="true"></span> Đang xử lý...';
                }
            }
        });
    });
});
