$(function () {
    SF.init();
});

var SF = (function () {

    function init() {
        initSidebar();
        initSignalR();
        initToasts();
        initConfirmDialogs();
        initSearchTypeahead();
        highlightActiveNav();
    }

    function initSidebar() {
        $('#sf-toggle-btn').on('click', function () {
            $('#sf-sidebar').toggleClass('collapsed');
            var collapsed = $('#sf-sidebar').hasClass('collapsed');
            localStorage.setItem('sf_sidebar_collapsed', collapsed);
        });

        var savedState = localStorage.getItem('sf_sidebar_collapsed');
        if (savedState === 'true') {
            $('#sf-sidebar').addClass('collapsed');
        }
    }

    function highlightActiveNav() {
        var path = window.location.pathname.toLowerCase();
        $('.sf-nav-item').removeClass('active');
        $('.sf-nav-item').each(function () {
            var href = $(this).attr('href');
            if (href && path.startsWith(href.toLowerCase()) && href !== '/') {
                $(this).addClass('active');
            }
        });
    }

    function initSignalR() {
        if (typeof signalR === 'undefined') return;

        var connection = new signalR.HubConnectionBuilder()
            .withUrl('/hubs/stockflow')
            .withAutomaticReconnect([0, 2000, 5000, 10000])
            .configureLogging(signalR.LogLevel.Warning)
            .build();

        connection.on('ProcessingComplete', function (data) {
            showToast('Shipment #' + data.shipmentId + ' processed successfully.', 'success');
            refreshNotifBadge();
        });

        connection.on('ApprovalRequired', function (data) {
            showToast('Item #' + data.processedItemId + ' awaiting your approval.', 'info');
            refreshNotifBadge();
        });

        connection.on('ApprovalDecision', function (data) {
            var type = data.decision === 'Approved' ? 'success' : 'danger';
            showToast('Item #' + data.processedItemId + ' ' + data.decision.toLowerCase() + '.', type);
        });

        connection.on('StaleShipment', function (data) {
            showToast('Shipment #' + data.shipmentId + ' has been pending too long.', 'warning');
        });

        connection.on('UserNotification', function (data) {
            showToast(data.message, 'info');
        });

        connection.start()
            .then(function () {
                console.log('SignalR connected.');
            })
            .catch(function (err) {
                console.warn('SignalR connection failed:', err);
            });

        connection.onreconnecting(function () {
            showToast('Reconnecting to server...', 'warning');
        });

        connection.onreconnected(function () {
            showToast('Reconnected.', 'success');
        });

        window.sfConnection = connection;
    }

    function initToasts() {
        if ($('#sf-toast-container').length === 0) {
            $('body').append('<div id="sf-toast-container" class="sf-toast-container"></div>');
        }

        var successMsg = $('#sf-temp-success').val();
        var errorMsg = $('#sf-temp-error').val();

        if (successMsg) showToast(successMsg, 'success');
        if (errorMsg) showToast(errorMsg, 'danger');
    }

    function showToast(message, type) {
        type = type || 'info';
        var icons = { success: '✓', danger: '✕', warning: '!', info: 'i' };
        var toast = $(
            '<div class="sf-toast ' + type + '">' +
            '<span style="font-size:11px;font-weight:700;opacity:0.7">' + (icons[type] || 'i') + '</span>' +
            '<span class="sf-toast-msg">' + escapeHtml(message) + '</span>' +
            '<span class="sf-toast-close">✕</span>' +
            '</div>'
        );

        toast.find('.sf-toast-close').on('click', function () {
            dismissToast(toast);
        });

        $('#sf-toast-container').append(toast);

        setTimeout(function () {
            dismissToast(toast);
        }, 4500);
    }

    function dismissToast(toast) {
        toast.css({ opacity: 0, transform: 'translateX(40px)', transition: 'all 0.25s ease' });
        setTimeout(function () { toast.remove(); }, 260);
    }

    function initConfirmDialogs() {
        $(document).on('click', '[data-sf-confirm]', function (e) {
            var message = $(this).data('sf-confirm') || 'Are you sure?';
            if (!confirm(message)) {
                e.preventDefault();
                e.stopImmediatePropagation();
                return false;
            }
        });

        $(document).on('submit', 'form[data-sf-confirm]', function (e) {
            var message = $(this).data('sf-confirm') || 'Are you sure?';
            if (!confirm(message)) {
                e.preventDefault();
                return false;
            }
        });
    }

    function initSearchTypeahead() {
        var timer;
        var $input = $('#sf-global-search');
        var $dropdown = $('#sf-search-dropdown');

        if (!$input.length) return;

        $input.on('input', function () {
            clearTimeout(timer);
            var q = $(this).val().trim();
            if (q.length < 2) { $dropdown.hide(); return; }

            timer = setTimeout(function () {
                $.get('/Search/Quick', { q: q }, function (res) {
                    if (!res.success) return;
                    renderSearchDropdown(res.data, $dropdown);
                });
            }, 280);
        });

        $input.on('keydown', function (e) {
            if (e.key === 'Enter') {
                window.location.href = '/Search?Query=' + encodeURIComponent($(this).val());
            }
            if (e.key === 'Escape') { $dropdown.hide(); }
        });

        $(document).on('click', function (e) {
            if (!$(e.target).closest('.sf-search-wrap').length) {
                $dropdown.hide();
            }
        });
    }

    function renderSearchDropdown(data, $dropdown) {
        if (!data) { $dropdown.hide(); return; }
        var total = (data.items ? data.items.length : 0) +
                    (data.shipments ? data.shipments.length : 0) +
                    (data.processedItems ? data.processedItems.length : 0);

        if (total === 0) { $dropdown.hide(); return; }

        var html = '<div style="padding:6px 0">';

        if (data.items && data.items.length) {
            html += '<div style="padding:3px 10px;font-size:10px;color:var(--sf-text-muted);font-weight:600;text-transform:uppercase">Items</div>';
            data.items.slice(0, 3).forEach(function (i) {
                html += '<a href="/Item/Detail/' + i.itemId + '" class="sf-search-result-item">' +
                    '<span>' + escapeHtml(i.itemName) + '</span>' +
                    '<span style="font-size:11px;color:var(--sf-text-muted)">' + escapeHtml(i.sku) + '</span></a>';
            });
        }

        if (data.shipments && data.shipments.length) {
            html += '<div style="padding:3px 10px;font-size:10px;color:var(--sf-text-muted);font-weight:600;text-transform:uppercase">Shipments</div>';
            data.shipments.slice(0, 3).forEach(function (s) {
                html += '<a href="/Shipment/Detail/' + s.shipmentId + '" class="sf-search-result-item">' +
                    '<span>SHP-' + String(s.shipmentId).padStart(4,'0') + ' — ' + escapeHtml(s.itemName) + '</span>' +
                    '<span class="sf-status ' + s.status.charAt(0) + '">' + s.status.charAt(0) + '</span></a>';
            });
        }

        html += '<a href="/Search?Query=' + encodeURIComponent($('#sf-global-search').val()) + '" ' +
            'style="display:block;padding:7px 10px;font-size:11.5px;color:var(--sf-primary);border-top:0.5px solid var(--sf-border);text-align:center">See all results →</a>';
        html += '</div>';

        $dropdown.html(html).show();
    }

    function refreshNotifBadge() {
        var $dot = $('.sf-notif-dot');
        $dot.show();
        $dot.addClass('sf-notif-pulse');
        setTimeout(function () { $dot.removeClass('sf-notif-pulse'); }, 600);
    }

    function escapeHtml(str) {
        if (!str) return '';
        return String(str)
            .replace(/&/g, '&amp;')
            .replace(/</g, '&lt;')
            .replace(/>/g, '&gt;')
            .replace(/"/g, '&quot;');
    }

    function ajaxPost(url, data, onSuccess, onError) {
        var token = $('input[name="__RequestVerificationToken"]').val() ||
                    $('meta[name="csrf-token"]').attr('content');
        $.ajax({
            url: url,
            method: 'POST',
            data: $.extend({}, data, { __RequestVerificationToken: token }),
            success: function (res) {
                if (onSuccess) onSuccess(res);
            },
            error: function (xhr) {
                var msg = 'An error occurred. Please try again.';
                try { msg = JSON.parse(xhr.responseText).message || msg; } catch (e) {}
                if (onError) onError(msg);
                else showToast(msg, 'danger');
            }
        });
    }

    function formatWeight(val, unit) {
        return parseFloat(val).toFixed(2) + ' ' + (unit || 'kg');
    }

    function formatDate(dateStr) {
        var d = new Date(dateStr);
        return d.toLocaleDateString('en-IN', { day: '2-digit', month: 'short', year: 'numeric' });
    }

    return {
        init: init,
        showToast: showToast,
        ajaxPost: ajaxPost,
        formatWeight: formatWeight,
        formatDate: formatDate,
        escapeHtml: escapeHtml
    };

})();