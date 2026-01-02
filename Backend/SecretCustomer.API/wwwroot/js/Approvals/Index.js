// Approvals Index ViewModel - Consolidated
// Pattern: Single Index.cshtml + Index.js with modals

function ApprovalsViewModel() {
    var self = this;

    // State
    self.approvals = ko.observableArray([]);
    self.myPendingApprovals = ko.observableArray([]);
    self.viewingApproval = ko.observable(null);
    self.isDetailModalOpen = ko.observable(false);
    self.responseNote = ko.observable('');
    self.isLoading = ko.observable(false);

    // Summary
    self.summary = {
        totalApprovals: ko.observable(0),
        pendingApprovals: ko.observable(0),
        approvedCount: ko.observable(0),
        rejectedCount: ko.observable(0),
        overdueCount: ko.observable(0),
        todayApprovals: ko.observable(0)
    };

    // Filters
    self.filter = {
        approvalType: ko.observable(''),
        status: ko.observable(''),
        priority: ko.observable(''),
        searchTerm: ko.observable('')
    };

    // Pagination
    self.currentPage = ko.observable(1);
    self.pageSize = ko.observable(20);
    self.totalCount = ko.observable(0);

    self.totalPages = ko.computed(function() {
        return Math.ceil(self.totalCount() / self.pageSize()) || 1;
    });

    // Total count text for display
    self.totalCountText = ko.computed(function() {
        return T('Common.Total', 'Toplam') + ': ' + self.totalCount();
    });

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

    // Filter subscriptions
    self.filter.approvalType.subscribe(function() { self.loadApprovals(); });
    self.filter.status.subscribe(function() { self.loadApprovals(); });
    self.filter.priority.subscribe(function() { self.loadApprovals(); });
    self.filter.searchTerm.subscribe(function() {
        clearTimeout(self.searchTimeout);
        self.searchTimeout = setTimeout(function() {
            self.loadApprovals();
        }, 300);
    });

    // Load approvals
    self.loadApprovals = function() {
        self.isLoading(true);
        var params = new URLSearchParams({
            page: self.currentPage(),
            pageSize: self.pageSize()
        });
        if (self.filter.approvalType()) params.append('approvalType', self.filter.approvalType());
        if (self.filter.status()) params.append('status', self.filter.status());
        if (self.filter.priority()) params.append('priority', self.filter.priority());
        if (self.filter.searchTerm()) params.append('search', self.filter.searchTerm());

        $.get('/api/approvals?' + params.toString())
            .done(function(response) {
                self.approvals(response.items || []);
                self.totalCount(response.totalCount || 0);
            })
            .fail(function(xhr) {
                toastr.error(T('Approval.LoadError', 'Onaylar yüklenirken hata oluştu'));
            })
            .always(function() {
                self.isLoading(false);
            });
    };

    // Load my pending approvals
    self.loadMyPendingApprovals = function() {
        $.get('/api/approvals/my-pending')
            .done(function(response) {
                self.myPendingApprovals(response || []);
            })
            .fail(function() {
                console.error('My pending approvals could not be loaded');
            });
    };

    // Load summary
    self.loadSummary = function() {
        $.get('/api/approvals/summary')
            .done(function(response) {
                self.summary.totalApprovals(response.totalApprovals || 0);
                self.summary.pendingApprovals(response.pendingApprovals || 0);
                self.summary.approvedCount(response.approvedCount || 0);
                self.summary.rejectedCount(response.rejectedCount || 0);
                self.summary.overdueCount(response.overdueCount || 0);
                self.summary.todayApprovals(response.todayApprovals || 0);
            })
            .fail(function() {
                console.error('Summary could not be loaded');
            });
    };

    // Clear filters
    self.clearFilters = function() {
        self.filter.approvalType('');
        self.filter.status('');
        self.filter.priority('');
        self.filter.searchTerm('');
    };

    // Pagination
    self.previousPage = function() {
        if (self.currentPage() > 1) {
            self.currentPage(self.currentPage() - 1);
            self.loadApprovals();
        }
    };

    self.nextPage = function() {
        if (self.currentPage() < self.totalPages()) {
            self.currentPage(self.currentPage() + 1);
            self.loadApprovals();
        }
    };

    self.goToPage = function(page) {
        if (page >= 1 && page <= self.totalPages()) {
            self.currentPage(page);
            self.loadApprovals();
        }
    };

    // Show detail modal
    self.showDetail = function(approval) {
        $.get('/api/approvals/' + approval.id)
            .done(function(response) {
                self.viewingApproval(response);
                self.responseNote('');
                self.isDetailModalOpen(true);
            })
            .fail(function() {
                toastr.error(T('Approval.DetailLoadError', 'Onay detayı yüklenemedi'));
            });
    };

    // Close detail modal
    self.closeDetailModal = function() {
        self.isDetailModalOpen(false);
        self.viewingApproval(null);
        self.responseNote('');
    };

    // Approve request (quick action from list)
    self.approveRequest = function(approval) {
        showConfirmModal({
            title: T('Approval.Approve', 'Onay'),
            message: T('Approval.ConfirmApprove', 'Bu onay talebini onaylamak istediğinizden emin misiniz?'),
            type: 'success',
            confirmText: T('Approval.Approve', 'Onayla'),
            confirmIcon: 'bi-check-circle',
            onConfirm: function() {
                $.ajax({
                    url: '/api/approvals/' + approval.id + '/approve',
                    type: 'POST',
                    contentType: 'application/json',
                    data: JSON.stringify({ note: '' })
                })
                .done(function() {
                    toastr.success(T('Approval.ApproveSuccess', 'Onay işlemi başarıyla tamamlandı'));
                    self.refreshAll();
                })
                .fail(function(xhr) {
                    toastr.error(xhr.responseJSON?.message || T('Approval.ApproveError', 'Onay işlemi başarısız'));
                });
            }
        });
    };

    // Reject request (quick action from list)
    self.rejectRequest = function(approval) {
        showConfirmModal({
            title: T('Approval.Reject', 'Reddet'),
            message: T('Approval.ConfirmReject', 'Bu onay talebini reddetmek istediğinizden emin misiniz?'),
            type: 'danger',
            confirmText: T('Approval.Reject', 'Reddet'),
            confirmIcon: 'bi-x-circle',
            onConfirm: function() {
                $.ajax({
                    url: '/api/approvals/' + approval.id + '/reject',
                    type: 'POST',
                    contentType: 'application/json',
                    data: JSON.stringify({ note: '' })
                })
                .done(function() {
                    toastr.success(T('Approval.RejectSuccess', 'Red işlemi başarıyla tamamlandı'));
                    self.refreshAll();
                })
                .fail(function(xhr) {
                    toastr.error(xhr.responseJSON?.message || T('Approval.RejectError', 'Red işlemi başarısız'));
                });
            }
        });
    };

    // Approve from detail modal
    self.approveFromDetail = function(approval) {
        showConfirmModal({
            title: T('Approval.Approve', 'Onay'),
            message: T('Approval.ConfirmApprove', 'Bu onay talebini onaylamak istediğinizden emin misiniz?'),
            type: 'success',
            confirmText: T('Approval.Approve', 'Onayla'),
            confirmIcon: 'bi-check-circle',
            onConfirm: function() {
                $.ajax({
                    url: '/api/approvals/' + approval.id + '/approve',
                    type: 'POST',
                    contentType: 'application/json',
                    data: JSON.stringify({ note: self.responseNote() })
                })
                .done(function() {
                    toastr.success(T('Approval.ApproveSuccess', 'Onay işlemi başarıyla tamamlandı'));
                    self.closeDetailModal();
                    self.refreshAll();
                })
                .fail(function(xhr) {
                    toastr.error(xhr.responseJSON?.message || T('Approval.ApproveError', 'Onay işlemi başarısız'));
                });
            }
        });
    };

    // Reject from detail modal
    self.rejectFromDetail = function(approval) {
        showConfirmModal({
            title: T('Approval.Reject', 'Reddet'),
            message: T('Approval.ConfirmReject', 'Bu onay talebini reddetmek istediğinizden emin misiniz?'),
            type: 'danger',
            confirmText: T('Approval.Reject', 'Reddet'),
            confirmIcon: 'bi-x-circle',
            onConfirm: function() {
                $.ajax({
                    url: '/api/approvals/' + approval.id + '/reject',
                    type: 'POST',
                    contentType: 'application/json',
                    data: JSON.stringify({ note: self.responseNote() })
                })
                .done(function() {
                    toastr.success(T('Approval.RejectSuccess', 'Red işlemi başarıyla tamamlandı'));
                    self.closeDetailModal();
                    self.refreshAll();
                })
                .fail(function(xhr) {
                    toastr.error(xhr.responseJSON?.message || T('Approval.RejectError', 'Red işlemi başarısız'));
                });
            }
        });
    };

    // Cancel approval (by requester)
    self.cancelApproval = function(approval) {
        showConfirmModal({
            title: T('Common.Cancel', 'İptal'),
            message: T('Approval.ConfirmCancel', 'Onay talebinizi iptal etmek istediğinizden emin misiniz?'),
            type: 'warning',
            confirmText: T('Approval.CancelRequest', 'İptal Et'),
            confirmIcon: 'bi-x-circle',
            onConfirm: function() {
                $.ajax({
                    url: '/api/approvals/' + approval.id + '/cancel',
                    type: 'POST'
                })
                .done(function() {
                    toastr.success(T('Approval.CancelSuccess', 'Onay talebi iptal edildi'));
                    self.closeDetailModal();
                    self.refreshAll();
                })
                .fail(function(xhr) {
                    toastr.error(xhr.responseJSON?.message || T('Approval.CancelError', 'İptal işlemi başarısız'));
                });
            }
        });
    };

    // Permission checks
    self.canRespondToApproval = function(approval) {
        // This would check if current user is the approver
        return approval && approval.status === 'Pending';
    };

    self.canCancelApproval = function(approval) {
        // This would check if current user is the requester
        return approval && approval.status === 'Pending';
    };

    // Helper functions
    self.getApprovalTypeText = function(type) {
        var types = {
            'Evaluation': T('Approval.Type.Evaluation', 'Değerlendirme'),
            'Assignment': T('Approval.Type.Assignment', 'Atama'),
            'Project': T('Approval.Type.Project', 'Proje'),
            'Meeting': T('Approval.Type.Meeting', 'Toplantı'),
            'Training': T('Approval.Type.Training', 'Eğitim'),
            'Delegation': T('Approval.Type.Delegation', 'Vekalet'),
            'General': T('Approval.Type.General', 'Genel')
        };
        return types[type] || type;
    };

    // Status helpers - EnumsService kullanir
    self.getStatusText = function(status) {
        return EnumsService.getApprovalStatusDisplay(status);
    };

    self.getStatusBadgeClass = function(status) {
        return EnumsService.getApprovalStatusCss(status);
    };

    self.getPriorityText = function(priority) {
        var priorities = {
            'Low': T('Priority.Low', 'Düşük'),
            'Normal': T('Priority.Normal', 'Normal'),
            'High': T('Priority.High', 'Yüksek'),
            'Urgent': T('Priority.Urgent', 'Acil')
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
        return date.toLocaleDateString('tr-TR') + ' ' + date.toLocaleTimeString('tr-TR', { hour: '2-digit', minute: '2-digit' });
    };

    self.isOverdue = function(approval) {
        if (!approval.dueDate || approval.status !== 'Pending') return false;
        return new Date(approval.dueDate) < new Date();
    };

    // Refresh all data
    self.refreshAll = function() {
        self.loadApprovals();
        self.loadMyPendingApprovals();
        self.loadSummary();
    };

    // Initialize
    self.init = function() {
        // Once EnumsService'i yukle, sonra verileri cek
        EnumsService.load().then(function() {
            self.refreshAll();
        });
    };

    self.init();
}

// Initialize ViewModel
$(document).ready(function() {
    ko.applyBindings(new ApprovalsViewModel(), document.getElementById('approvals-app'));
});
