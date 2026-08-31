/* Ortak arayüz davranışları. Sunucu tarafı sözleşmeleri (form alan adları,
   action'lar, endpoint'ler) değişmez; burada yalnızca kullanım kolaylığı vardır. */
(function () {
    "use strict";

    var App = window.App || {};

    /* ---------- Toast ---------- */
    function toastStack() {
        var stack = document.querySelector(".toast-stack");
        if (!stack) {
            stack = document.createElement("div");
            stack.className = "toast-stack";
            document.body.appendChild(stack);
        }
        return stack;
    }

    var ICONS = {
        ok: '<svg width="17" height="17" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.2" stroke-linecap="round" stroke-linejoin="round"><path d="M20 6 9 17l-5-5"/></svg>',
        err: '<svg width="17" height="17" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.2" stroke-linecap="round" stroke-linejoin="round"><circle cx="12" cy="12" r="9"/><path d="M12 8v5M12 16h.01"/></svg>'
    };

    App.toast = function (message, type, timeout) {
        if (!message) { return; }
        var kind = type === "err" || type === "error" || type === "danger" ? "err" : "ok";
        var el = document.createElement("div");
        el.className = "toast-item " + kind;
        el.setAttribute("role", "status");
        el.innerHTML =
            '<span class="toast-icon">' + ICONS[kind] + "</span>" +
            '<div class="toast-body"></div>' +
            '<button type="button" class="toast-close" aria-label="Kapat">&times;</button>';
        el.querySelector(".toast-body").textContent = message;

        var close = function () {
            if (!el.parentNode) { return; }
            el.classList.add("is-hiding");
            setTimeout(function () { el.remove(); }, 200);
        };

        el.querySelector(".toast-close").addEventListener("click", close);
        toastStack().appendChild(el);
        setTimeout(close, timeout || 4500);
    };

    function flushServerMessages() {
        document.querySelectorAll("[data-toast]").forEach(function (node) {
            App.toast(node.getAttribute("data-toast"), node.getAttribute("data-toast-type"));
            node.remove();
        });
    }

    /* ---------- Adet kontrolü ---------- */
    function stepQuantity(scope, delta) {
        var input = scope.querySelector("input[type=number]");
        if (!input) { return; }
        var min = parseInt(input.getAttribute("min") || "1", 10);
        var max = parseInt(input.getAttribute("max") || "99", 10);
        var next = (parseInt(input.value, 10) || min) + delta;
        input.value = Math.min(max, Math.max(min, next));
        input.dispatchEvent(new Event("change", { bubbles: true }));
    }

    function bindQuantity() {
        document.addEventListener("click", function (e) {
            var btn = e.target.closest(".qty-btn");
            if (!btn) { return; }
            e.preventDefault();
            stepQuantity(btn.closest(".qty"), btn.dataset.step === "up" ? 1 : -1);
        });
    }

    /* ---------- Not alanı aç/kapat ---------- */
    function bindNoteToggle() {
        document.addEventListener("click", function (e) {
            var btn = e.target.closest("[data-note-toggle]");
            if (!btn) { return; }
            e.preventDefault();
            var field = document.getElementById(btn.getAttribute("data-note-toggle"));
            if (!field) { return; }
            var hidden = field.hasAttribute("hidden");
            field.toggleAttribute("hidden", !hidden);
            if (hidden) { field.querySelector("input, textarea").focus(); }
        });
    }

    /* ---------- Gönderim sırasında yükleme durumu ---------- */
    function bindSubmitState() {
        // Doğrulama gönderimi iptal ettiyse (jQuery unobtrusive veya tarayıcı) buton kilitlenmez.
        document.addEventListener("submit", function (e) {
            var form = e.target;
            if (e.defaultPrevented || form.hasAttribute("data-no-loading")) { return; }
            var btn = form.querySelector("button[type=submit], button:not([type=button])");
            if (btn && !btn.classList.contains("is-loading")) { btn.classList.add("is-loading"); }
        });
    }

    /* ---------- Admin kenar çubuğu ---------- */
    function bindSidebar() {
        var toggle = document.querySelector("[data-sidebar-toggle]");
        var backdrop = document.querySelector(".sidebar-backdrop");
        if (toggle) {
            toggle.addEventListener("click", function () { document.body.classList.toggle("nav-open"); });
        }
        if (backdrop) {
            backdrop.addEventListener("click", function () { document.body.classList.remove("nav-open"); });
        }
    }

    /* ---------- Geçen süre etiketleri ---------- */
    function humanize(from) {
        var mins = Math.max(0, Math.floor((Date.now() - from) / 60000));
        if (mins < 1) { return "az önce"; }
        if (mins < 60) { return mins + " dk"; }
        var hours = Math.floor(mins / 60);
        return hours + " sa " + (mins % 60) + " dk";
    }

    function refreshElapsed() {
        document.querySelectorAll("[data-elapsed]").forEach(function (node) {
            var ts = Date.parse(node.getAttribute("data-elapsed"));
            if (!isNaN(ts)) { node.textContent = humanize(ts); }
        });
    }

    /* ---------- DataTables ---------- */
    function bindDataTables() {
        if (typeof DataTable === "undefined") { return; }
        document.querySelectorAll("table.js-datatable").forEach(function (table) {
            if (table.dataset.dtReady === "1") { return; }
            table.dataset.dtReady = "1";
            table.style.width = "100%";
            var nosort = Array.prototype.map.call(table.querySelectorAll("thead th"), function (th, i) {
                return th.classList.contains("no-sort") ? i : null;
            }).filter(function (i) { return i !== null; });
            var options = {
                pageLength: 10,
                lengthMenu: [10, 25, 50, 100],
                order: [],
                autoWidth: false,
                deferRender: true,
                searching: true,
                layout: {
                    topStart: "search",
                    topEnd: "pageLength",
                    bottomStart: "info",
                    bottomEnd: "paging"
                },
                language: {
                    search: "",
                    searchPlaceholder: "İsim, numara veya metin ara",
                    lengthMenu: "_MENU_ kayıt",
                    info: "_START_–_END_ / _TOTAL_ kayıt",
                    infoEmpty: "Kayıt yok",
                    infoFiltered: "(_MAX_ kayıt içinde)",
                    zeroRecords: "Eşleşen kayıt yok",
                    emptyTable: "Tabloda veri yok",
                    paginate: { first: "İlk", last: "Son", next: "›", previous: "‹" }
                }
            };
            if (nosort.length) {
                options.columnDefs = [{ orderable: false, targets: nosort }];
            }
            enhanceDataTableSearch(new DataTable(table, options));
        });
    }

    function enhanceDataTableSearch(api) {
        var search = api.table().container().querySelector(".dt-search");
        if (!search || search.querySelector(".dt-search-btn")) { return; }
        var input = search.querySelector("input[type='search'], input");
        if (!input) { return; }
        input.setAttribute("aria-label", "Tabloda ara");
        var button = document.createElement("button");
        button.type = "button";
        button.className = "btn btn-primary btn-sm dt-search-btn";
        button.textContent = "Ara";
        search.appendChild(button);
        function runSearch() {
            api.search(input.value).draw();
        }
        button.addEventListener("click", runSearch);
        input.addEventListener("keydown", function (e) {
            if (e.key === "Enter") {
                e.preventDefault();
                runSearch();
            }
        });
    }

    /* ---------- Sayfa yenileme sayacı ---------- */
    function bindRefreshMeter() {
        var meter = document.querySelector("[data-refresh]");
        if (!meter) { return; }
        var seconds = parseInt(meter.getAttribute("data-refresh"), 10) || 10;
        var bar = meter.querySelector(".bar span");
        if (bar) {
            bar.style.transition = "transform " + seconds + "s linear";
            requestAnimationFrame(function () { bar.style.transform = "scaleX(0)"; });
        }
        setTimeout(function () { window.location.reload(); }, seconds * 1000);
    }

    document.addEventListener("DOMContentLoaded", function () {
        flushServerMessages();
        bindQuantity();
        bindNoteToggle();
        bindSubmitState();
        bindSidebar();
        bindRefreshMeter();
        refreshElapsed();
        setInterval(refreshElapsed, 30000);
        bindDataTables();
    });

    window.App = App;
})();
