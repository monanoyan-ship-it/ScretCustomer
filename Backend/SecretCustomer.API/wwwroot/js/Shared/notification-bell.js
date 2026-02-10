// Notification Bell - SignalR real-time bildirim sistemi
(function () {
    'use strict';

    var connection = null;
    var unreadCount = 0;

    function init() {
        if (!window.userId) return;

        loadUnreadCount();
        loadUnreadNotifications();
        initSignalR();
        bindEvents();
    }

    function loadUnreadCount() {
        fetch('/api/notifications/unread-count', { credentials: 'include' })
            .then(function (res) { return res.json(); })
            .then(function (data) {
                updateBadge(data.count);
            })
            .catch(function (err) {
                console.error('Bildirim sayısı yüklenemedi:', err);
            });
    }

    function loadUnreadNotifications() {
        fetch('/api/notifications/unread', { credentials: 'include' })
            .then(function (res) { return res.json(); })
            .then(function (notifications) {
                renderDropdownItems(notifications);
            })
            .catch(function (err) {
                console.error('Bildirimler yüklenemedi:', err);
            });
    }

    function initSignalR() {
        if (typeof signalR === 'undefined') {
            console.warn('SignalR kütüphanesi yüklenmedi');
            return;
        }

        connection = new signalR.HubConnectionBuilder()
            .withUrl('/hubs/notifications')
            .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
            .build();

        connection.on('ReceiveNotification', function (notification) {
            addNotificationToDropdown(notification);
            showToast(notification);
        });

        connection.on('UpdateUnreadCount', function (count) {
            updateBadge(count);
        });

        connection.onreconnecting(function () {
            console.log('SignalR yeniden bağlanıyor...');
        });

        connection.onreconnected(function () {
            console.log('SignalR yeniden bağlandı');
            loadUnreadCount();
            loadUnreadNotifications();
        });

        connection.start()
            .then(function () {
                console.log('SignalR connected');
            })
            .catch(function (err) {
                console.error('SignalR bağlantı hatası:', err);
            });
    }

    function bindEvents() {
        // Tümünü oku butonu
        var markAllBtn = document.getElementById('notification-mark-all-read');
        if (markAllBtn) {
            markAllBtn.addEventListener('click', function (e) {
                e.preventDefault();
                e.stopPropagation();
                markAllRead();
            });
        }
    }

    function updateBadge(count) {
        unreadCount = count;
        var badge = document.getElementById('notification-badge');
        if (badge) {
            if (count > 0) {
                badge.textContent = count > 99 ? '99+' : count;
                badge.style.display = '';
            } else {
                badge.style.display = 'none';
            }
        }
    }

    function renderDropdownItems(notifications) {
        var container = document.getElementById('notification-dropdown-items');
        if (!container) return;

        if (!notifications || notifications.length === 0) {
            container.innerHTML = '<div class="text-center text-muted py-3 px-2"><i class="bi bi-bell-slash me-1"></i> Yeni bildirim yok</div>';
            return;
        }

        var html = '';
        notifications.forEach(function (n) {
            html += buildNotificationItem(n);
        });
        container.innerHTML = html;

        // Click event'leri bağla
        container.querySelectorAll('.notification-item').forEach(function (el) {
            el.addEventListener('click', function () {
                var id = this.getAttribute('data-id');
                var url = this.getAttribute('data-url');
                markRead(id, url);
            });
        });
    }

    function addNotificationToDropdown(notification) {
        var container = document.getElementById('notification-dropdown-items');
        if (!container) return;

        // "Yeni bildirim yok" mesajını temizle
        var emptyMsg = container.querySelector('.text-muted');
        if (emptyMsg) container.innerHTML = '';

        var temp = document.createElement('div');
        temp.innerHTML = buildNotificationItem(notification);
        var item = temp.firstElementChild;

        item.addEventListener('click', function () {
            var id = this.getAttribute('data-id');
            var url = this.getAttribute('data-url');
            markRead(id, url);
        });

        container.insertBefore(item, container.firstChild);

        // Max 10 item tut
        while (container.children.length > 10) {
            container.removeChild(container.lastChild);
        }
    }

    function buildNotificationItem(n) {
        var iconClass = getTypeIcon(n.notificationType);
        var priorityClass = getPriorityClass(n.priority);
        var timeText = n.timeAgo || formatTimeAgo(n.createdAt);

        return '<a href="#" class="dropdown-item notification-item d-flex align-items-start py-2 ' + (n.isRead ? '' : 'bg-light') + '" data-id="' + n.id + '" data-url="' + (n.actionUrl || '') + '">'
            + '<div class="me-2 mt-1"><i class="bi ' + iconClass + ' ' + priorityClass + '"></i></div>'
            + '<div class="flex-grow-1 overflow-hidden">'
            + '<div class="fw-semibold text-truncate" style="font-size: 0.85rem;">' + escapeHtml(n.title) + '</div>'
            + '<div class="text-muted text-truncate" style="font-size: 0.78rem;">' + escapeHtml(n.message) + '</div>'
            + '<small class="text-muted">' + timeText + '</small>'
            + '</div>'
            + '</a>';
    }

    function getTypeIcon(type) {
        var icons = {
            'Info': 'bi-info-circle text-info',
            'Success': 'bi-check-circle text-success',
            'Warning': 'bi-exclamation-triangle text-warning',
            'Error': 'bi-x-circle text-danger',
            'ApprovalRequest': 'bi-check2-square text-primary',
            'Assignment': 'bi-person-plus text-secondary',
            'Reminder': 'bi-bell text-warning',
            'System': 'bi-gear text-dark'
        };
        return icons[type] || 'bi-bell text-primary';
    }

    function getPriorityClass(priority) {
        if (priority === 'Urgent') return 'text-danger';
        if (priority === 'High') return 'text-warning';
        return '';
    }

    function formatTimeAgo(dateStr) {
        if (!dateStr) return '';
        var date = new Date(dateStr);
        var now = new Date();
        var diff = Math.floor((now - date) / 1000);

        if (diff < 60) return 'Az önce';
        if (diff < 3600) return Math.floor(diff / 60) + ' dk önce';
        if (diff < 86400) return Math.floor(diff / 3600) + ' saat önce';
        if (diff < 604800) return Math.floor(diff / 86400) + ' gün önce';
        return date.toLocaleDateString('tr-TR');
    }

    function escapeHtml(str) {
        if (!str) return '';
        var div = document.createElement('div');
        div.textContent = str;
        return div.innerHTML;
    }

    function markRead(id, url) {
        fetch('/api/notifications/' + id + '/read', {
            method: 'POST',
            credentials: 'include'
        }).then(function () {
            // Badge güncelle
            if (unreadCount > 0) {
                updateBadge(unreadCount - 1);
            }
            // Okundu stilini uygula
            var item = document.querySelector('.notification-item[data-id="' + id + '"]');
            if (item) item.classList.remove('bg-light');

            // URL varsa yönlendir
            if (url) {
                window.location.href = url;
            }
        }).catch(function (err) {
            console.error('Bildirim okundu işaretlenemedi:', err);
        });
    }

    function markAllRead() {
        fetch('/api/notifications/read-all', {
            method: 'POST',
            credentials: 'include'
        }).then(function () {
            updateBadge(0);
            // Tüm item'ları okundu yap
            document.querySelectorAll('.notification-item.bg-light').forEach(function (el) {
                el.classList.remove('bg-light');
            });
        }).catch(function (err) {
            console.error('Bildirimler okundu işaretlenemedi:', err);
        });
    }

    function showToast(notification) {
        if (typeof toastr !== 'undefined') {
            var toastType = 'info';
            if (notification.notificationType === 'Success') toastType = 'success';
            else if (notification.notificationType === 'Warning') toastType = 'warning';
            else if (notification.notificationType === 'Error') toastType = 'error';

            toastr[toastType](notification.message, notification.title, {
                timeOut: 5000,
                onclick: function () {
                    if (notification.actionUrl) {
                        window.location.href = notification.actionUrl;
                    }
                }
            });
        }
    }

    // DOM ready olduğunda başlat
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', init);
    } else {
        init();
    }
})();
