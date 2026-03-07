// Notifications Index ViewModel
function NotificationsIndexViewModel() {
    var self = this;

    // Summary
    self.summary = ko.observable({
        totalNotifications: 0,
        unreadCount: 0,
        todayCount: 0
    });

    // Notifications list
    self.notifications = ko.observableArray([]);
    self.totalCount = ko.observable(0);
    self.currentPage = ko.observable(1);
    self.pageSize = ko.observable(10);
    self.totalPages = ko.computed(function() {
        return Math.ceil(self.totalCount() / self.pageSize());
    });

    // Filters
    self.filter = {
        notificationType: ko.observable(''),
        priority: ko.observable(''),
        isRead: ko.observable(''),
        searchTerm: ko.observable('')
    };

    // Subscribe to filter changes
    self.filter.notificationType.subscribe(function() { self.currentPage(1); self.loadNotifications(); });
    self.filter.priority.subscribe(function() { self.currentPage(1); self.loadNotifications(); });
    self.filter.isRead.subscribe(function() { self.currentPage(1); self.loadNotifications(); });
    self.filter.searchTerm.subscribe(function() { self.currentPage(1); self.loadNotifications(); });

    // Visible pages for pagination
    self.visiblePages = ko.computed(function() {
        var pages = [];
        var total = self.totalPages();
        var current = self.currentPage();
        var start = Math.max(1, current - 2);
        var end = Math.min(total, current + 2);

        for (var i = start; i <= end; i++) {
            pages.push(i);
        }
        return pages;
    });

    // Load summary
    self.loadSummary = function() {
        $.get('/api/notifications/summary', function(data) {
            self.summary(data);
        });
    };

    // Load notifications
    self.loadNotifications = function() {
        var params = {
            page: self.currentPage(),
            pageSize: self.pageSize(),
            notificationType: self.filter.notificationType(),
            priority: self.filter.priority(),
            searchTerm: self.filter.searchTerm()
        };

        var isReadValue = self.filter.isRead();
        if (isReadValue !== '') {
            params.isRead = isReadValue === 'true';
        }

        $.get('/api/notifications', params, function(data) {
            self.notifications(data.items);
            self.totalCount(data.totalCount);
        });
    };

    // Clear filters
    self.clearFilters = function() {
        self.filter.notificationType('');
        self.filter.priority('');
        self.filter.isRead('');
        self.filter.searchTerm('');
    };

    // Pagination
    self.goToPage = function(page) {
        if (page >= 1 && page <= self.totalPages()) {
            self.currentPage(page);
            self.loadNotifications();
        }
    };

    self.previousPage = function() {
        if (self.currentPage() > 1) {
            self.goToPage(self.currentPage() - 1);
        }
    };

    self.nextPage = function() {
        if (self.currentPage() < self.totalPages()) {
            self.goToPage(self.currentPage() + 1);
        }
    };

    // Mark as read
    self.markAsRead = function(notification) {
        $.ajax({
            url: '/api/notifications/' + notification.id + '/read',
            method: 'POST',
            success: function() {
                notification.isRead = true;
                self.loadNotifications();
                self.loadSummary();
            },
            error: function() {
                toastr.error('Bir hata oluştu');
            }
        });
    };

    // Mark all as read
    self.markAllAsRead = function() {
        showConfirmModal({
            title: 'Tümünü Okundu İşaretle',
            message: 'Tüm bildirimleri okundu olarak işaretlemek istiyor musunuz?',
            type: 'info',
            confirmText: 'Okundu İşaretle',
            confirmIcon: 'bi-check-all',
            onConfirm: function() {
                $.ajax({
                    url: '/api/notifications/read-all',
                    method: 'POST',
                    success: function(data) {
                        toastr.success(data.count + ' bildirim okundu olarak işaretlendi');
                        self.loadNotifications();
                        self.loadSummary();
                    },
                    error: function() {
                        toastr.error('Bir hata oluştu');
                    }
                });
            }
        });
    };

    // Delete notification
    self.deleteNotification = function(notification) {
        showDeleteConfirm('Bu bildirim', function() {
            $.ajax({
                url: '/api/notifications/' + notification.id,
                method: 'DELETE',
                success: function() {
                    toastr.success('Bildirim silindi');
                    self.loadNotifications();
                    self.loadSummary();
                },
                error: function() {
                    toastr.error('Bir hata oluştu');
                }
            });
        });
    };

    // Detail modal
    self.selectedNotification = ko.observable(null);

    self.showDetail = function(notification) {
        $.get('/api/notifications/' + notification.id, function(data) {
            self.selectedNotification(data);
            $('#notificationDetailModal').modal('show');
            if (!notification.isRead) {
                self.markAsRead(notification);
            }
        }).fail(function() {
            toastr.error('Bildirim detayı yüklenemedi.');
        });
    };

    // Helper functions
    self.getNotificationTypeIcon = function(type) {
        var icons = {
            'Info': 'bi bi-info-circle',
            'Success': 'bi bi-check-circle',
            'Warning': 'bi bi-exclamation-triangle',
            'Error': 'bi bi-x-circle',
            'ApprovalRequest': 'bi bi-clipboard-check',
            'Assignment': 'bi bi-list-task',
            'MeetingInvite': 'bi bi-calendar-event',
            'TrainingInvite': 'bi bi-mortarboard',
            'Reminder': 'bi bi-bell',
            'System': 'bi bi-gear'
        };
        return icons[type] || 'bi bi-bell';
    };

    self.getNotificationTypeIconClass = function(type) {
        var classes = {
            'Info': 'bg-info text-white',
            'Success': 'bg-success text-white',
            'Warning': 'bg-warning text-dark',
            'Error': 'bg-danger text-white',
            'ApprovalRequest': 'bg-primary text-white',
            'Assignment': 'bg-secondary text-white',
            'MeetingInvite': 'bg-purple text-white',
            'TrainingInvite': 'bg-teal text-white',
            'Reminder': 'bg-orange text-white',
            'System': 'bg-dark text-white'
        };
        return classes[type] || 'bg-secondary text-white';
    };

    self.getPriorityText = function(priority) {
        var priorities = {
            'Low': 'Düşük',
            'Normal': 'Normal',
            'High': 'Yüksek',
            'Urgent': 'Acil'
        };
        return priorities[priority] || priority;
    };

    self.getPriorityBadgeClass = function(priority) {
        var classes = {
            'Low': 'bg-secondary',
            'Normal': 'bg-info',
            'High': 'bg-warning text-dark',
            'Urgent': 'bg-danger'
        };
        return classes[priority] || 'bg-secondary';
    };

    self.formatDate = function(dateStr) {
        if (!dateStr) return '-';
        var date = new Date(dateStr);
        var now = new Date();
        var diff = now - date;
        var minutes = Math.floor(diff / 60000);
        var hours = Math.floor(diff / 3600000);
        var days = Math.floor(diff / 86400000);

        if (minutes < 1) return 'Az önce';
        if (minutes < 60) return minutes + ' dakika önce';
        if (hours < 24) return hours + ' saat önce';
        if (days < 7) return days + ' gün önce';

        return date.toLocaleDateString('tr-TR');
    };

    self.formatDateFull = function(dateStr) {
        if (!dateStr) return '-';
        var date = new Date(dateStr);
        return date.toLocaleDateString('tr-TR') + ' ' + date.toLocaleTimeString('tr-TR', { hour: '2-digit', minute: '2-digit' });
    };

    // Initialize
    self.init = function() {
        self.loadSummary();
        self.loadNotifications();
    };

    self.init();
}

// Initialize when document is ready
$(document).ready(function() {
    ko.applyBindings(new NotificationsIndexViewModel(), document.getElementById('notifications-app'));
});
