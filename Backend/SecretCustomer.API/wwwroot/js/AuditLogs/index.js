(function () {
    'use strict';

    function AuditLogsViewModel() {
        var self = this;

        // Data
        self.logs = ko.observableArray([]);
        self.logTypes = ko.observableArray([]);
        self.selectedLog = ko.observable(null);
        self.isLoading = ko.observable(false);

        // Pagination
        self.currentPage = ko.observable(1);
        self.totalPages = ko.observable(1);
        self.totalCount = ko.observable(0);
        self.pageSize = ko.observable(50);

        // Stats
        self.errorCount = ko.observable(0);
        self.warningCount = ko.observable(0);
        self.loginCount = ko.observable(0);

        // Filters
        self.filter = {
            logType: ko.observable(''),
            category: ko.observable(''),
            userId: ko.observable(''),
            fromDate: ko.observable(''),
            toDate: ko.observable('')
        };

        // Computed: visible pages for pagination
        self.visiblePages = ko.computed(function () {
            var pages = [];
            var current = self.currentPage();
            var total = self.totalPages();
            var start = Math.max(1, current - 2);
            var end = Math.min(total, current + 2);

            for (var i = start; i <= end; i++) {
                pages.push(i);
            }
            return pages;
        });

        // Load log types
        self.loadLogTypes = function () {
            $.get('/api/auditlogs/types', function (data) {
                self.logTypes(data);
            });
        };

        // Load logs
        self.loadLogs = function () {
            self.isLoading(true);

            var params = {
                page: self.currentPage(),
                pageSize: self.pageSize()
            };

            if (self.filter.logType()) params.logType = self.filter.logType();
            if (self.filter.category()) params.category = self.filter.category();
            if (self.filter.userId()) params.userId = self.filter.userId();
            if (self.filter.fromDate()) params.fromDate = self.filter.fromDate();
            if (self.filter.toDate()) params.toDate = self.filter.toDate();

            $.get('/api/auditlogs', params, function (result) {
                self.logs(result.data);
                self.totalCount(result.totalCount);
                self.totalPages(result.totalPages);
                self.isLoading(false);

                // Calculate stats from current data
                self.calculateStats(result.data);
            }).fail(function () {
                self.isLoading(false);
                toastr.error('Loglar yüklenirken hata oluştu');
            });
        };

        // Calculate stats
        self.calculateStats = function (data) {
            var errors = 0, warnings = 0, logins = 0;
            data.forEach(function (log) {
                if (log.logTypeId === 2) errors++;
                if (log.logTypeId === 1) warnings++;
                if (log.logTypeId === 20) logins++;
            });
            self.errorCount(errors);
            self.warningCount(warnings);
            self.loginCount(logins);
        };

        // Show detail modal
        self.showDetail = function (log) {
            self.selectedLog(log);
            var modal = new bootstrap.Modal(document.getElementById('detailModal'));
            modal.show();
        };

        // Get badge class for log type
        self.getLogTypeBadgeClass = function (logType) {
            switch (logType) {
                case 0: return 'bg-info';           // Info
                case 1: return 'bg-warning';        // Warning
                case 2: return 'bg-danger';         // Error
                case 10: return 'bg-success';       // DataCreate
                case 11: return 'bg-primary';       // DataUpdate
                case 12: return 'bg-secondary';     // DataDelete
                case 20: return 'bg-success';       // Login
                case 21: return 'bg-secondary';     // Logout
                case 22: return 'bg-danger';        // LoginFailed
                case 23: return 'bg-danger';        // AccessDenied
                default: return 'bg-secondary';
            }
        };

        // Format date
        self.formatDate = function (dateStr) {
            if (!dateStr) return '-';
            var date = new Date(dateStr);
            return date.toLocaleString('tr-TR', {
                year: 'numeric',
                month: '2-digit',
                day: '2-digit',
                hour: '2-digit',
                minute: '2-digit',
                second: '2-digit'
            });
        };

        // Pagination
        self.prevPage = function () {
            if (self.currentPage() > 1) {
                self.currentPage(self.currentPage() - 1);
                self.loadLogs();
            }
        };

        self.nextPage = function () {
            if (self.currentPage() < self.totalPages()) {
                self.currentPage(self.currentPage() + 1);
                self.loadLogs();
            }
        };

        self.goToPage = function (page) {
            self.currentPage(page);
            self.loadLogs();
        };

        // Cleanup old logs
        self.cleanupLogs = function () {
            var days = prompt('Kaç günden eski loglar silinsin?', '90');
            if (days && !isNaN(days)) {
                showConfirmModal({
                    title: T('AuditLog.CleanupTitle', 'Log Temizleme'),
                    message: days + T('AuditLog.CleanupConfirm', ' günden eski tüm loglar silinecek. Emin misiniz?'),
                    confirmText: T('Common.Delete', 'Sil'),
                    confirmClass: 'btn-danger',
                    onConfirm: function() {
                        $.ajax({
                            url: '/api/auditlogs/cleanup?daysToKeep=' + days,
                            type: 'DELETE',
                            success: function (result) {
                                toastr.success(result.message);
                                self.loadLogs();
                            },
                            error: function () {
                                toastr.error(T('AuditLog.CleanupError', 'Silme işlemi başarısız'));
                            }
                        });
                    }
                });
            }
        };

        // Initialize
        self.init = function () {
            self.loadLogTypes();
            self.loadLogs();
        };

        self.init();
    }

    // Apply bindings
    ko.applyBindings(new AuditLogsViewModel(), document.getElementById('auditlogs-app'));
})();
