// DraftRequests Index ViewModel
// Taslaga alma talepleri yonetimi (Admin)
// Uses existing Approval API with Evaluation filter

function DraftRequestsViewModel() {
    var self = this;

    // State
    self.requests = ko.observableArray([]);
    self.viewingRequest = ko.observable(null);
    self.isDetailModalOpen = ko.observable(false);
    self.responseNote = ko.observable('');
    self.isLoading = ko.observable(false);

    // Summary counts
    self.pendingCount = ko.observable(0);
    self.approvedCount = ko.observable(0);
    self.rejectedCount = ko.observable(0);
    self.totalCount = ko.observable(0);

    // Filters
    self.filterStatus = ko.observable('');
    self.searchTerm = ko.observable('');

    // Pagination
    self.currentPage = ko.observable(1);
    self.pageSize = ko.observable(20);

    self.totalPages = ko.computed(function() {
        return Math.ceil(self.totalCount() / self.pageSize()) || 1;
    });

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
    self.filterStatus.subscribe(function() { self.currentPage(1); self.loadRequests(); });
    self.searchTerm.subscribe(function() {
        clearTimeout(self.searchTimeout);
        self.searchTimeout = setTimeout(function() {
            self.currentPage(1);
            self.loadRequests();
        }, 300);
    });

    // Load requests - filtered to EvaluationRevert only
    self.loadRequests = function() {
        self.isLoading(true);
        var params = new URLSearchParams({
            page: self.currentPage(),
            pageSize: self.pageSize(),
            approvalType: 'Evaluation' // Filter: only Evaluation approvals
        });
        if (self.filterStatus()) params.append('status', self.filterStatus());
        if (self.searchTerm()) params.append('searchTerm', self.searchTerm());

        $.get('/api/approvals?' + params.toString())
            .done(function(response) {
                // Further filter to only EvaluationRevert (RelatedEntityType check is done on backend)
                // Backend returns all Evaluation approvals, filter will work
                self.requests(response.items || []);
                self.totalCount(response.totalCount || 0);
            })
            .fail(function(xhr) {
                toastr.error(T('DraftRequest.LoadError', 'Talepler yuklenirken hata olustu'));
            })
            .always(function() {
                self.isLoading(false);
            });
    };

    // Load summary counts
    self.loadSummary = function() {
        // Count pending
        $.get('/api/approvals?approvalType=Evaluation&status=Pending&pageSize=1')
            .done(function(response) {
                self.pendingCount(response.totalCount || 0);
            });

        // Count approved
        $.get('/api/approvals?approvalType=Evaluation&status=Approved&pageSize=1')
            .done(function(response) {
                self.approvedCount(response.totalCount || 0);
            });

        // Count rejected
        $.get('/api/approvals?approvalType=Evaluation&status=Rejected&pageSize=1')
            .done(function(response) {
                self.rejectedCount(response.totalCount || 0);
            });
    };

    // Clear filters
    self.clearFilters = function() {
        self.filterStatus('');
        self.searchTerm('');
    };

    // Pagination
    self.previousPage = function() {
        if (self.currentPage() > 1) {
            self.currentPage(self.currentPage() - 1);
            self.loadRequests();
        }
    };

    self.nextPage = function() {
        if (self.currentPage() < self.totalPages()) {
            self.currentPage(self.currentPage() + 1);
            self.loadRequests();
        }
    };

    self.goToPage = function(page) {
        if (page >= 1 && page <= self.totalPages()) {
            self.currentPage(page);
            self.loadRequests();
        }
    };

    // Show detail modal
    self.showDetail = function(request) {
        $.get('/api/approvals/' + request.id)
            .done(function(response) {
                self.viewingRequest(response);
                self.responseNote('');
                self.isDetailModalOpen(true);
            })
            .fail(function() {
                toastr.error(T('DraftRequest.DetailLoadError', 'Talep detayi yuklenemedi'));
            });
    };

    // Close detail modal
    self.closeDetailModal = function() {
        self.isDetailModalOpen(false);
        self.viewingRequest(null);
        self.responseNote('');
    };

    // Approve request (quick action)
    self.approveRequest = function(request) {
        showConfirmModal({
            title: T('Approval.Approve', 'Onayla'),
            message: T('DraftRequest.ConfirmApprove', 'Bu talebi onaylamak istediginizden emin misiniz? Degerlendirme taslak durumuna alinacaktir.'),
            type: 'success',
            confirmText: T('Approval.Approve', 'Onayla'),
            confirmIcon: 'bi-check-circle',
            onConfirm: function() {
                self.doApprove(request.id, '');
            }
        });
    };

    // Reject request (quick action)
    self.rejectRequest = function(request) {
        showConfirmModal({
            title: T('Approval.Reject', 'Reddet'),
            message: T('DraftRequest.ConfirmReject', 'Bu talebi reddetmek istediginizden emin misiniz?'),
            type: 'danger',
            confirmText: T('Approval.Reject', 'Reddet'),
            confirmIcon: 'bi-x-circle',
            onConfirm: function() {
                self.doReject(request.id, '');
            }
        });
    };

    // Approve from detail modal
    self.approveFromDetail = function(request) {
        showConfirmModal({
            title: T('Approval.Approve', 'Onayla'),
            message: T('DraftRequest.ConfirmApprove', 'Bu talebi onaylamak istediginizden emin misiniz? Degerlendirme taslak durumuna alinacaktir.'),
            type: 'success',
            confirmText: T('Approval.Approve', 'Onayla'),
            confirmIcon: 'bi-check-circle',
            onConfirm: function() {
                self.doApprove(request.id, self.responseNote());
            }
        });
    };

    // Reject from detail modal
    self.rejectFromDetail = function(request) {
        showConfirmModal({
            title: T('Approval.Reject', 'Reddet'),
            message: T('DraftRequest.ConfirmReject', 'Bu talebi reddetmek istediginizden emin misiniz?'),
            type: 'danger',
            confirmText: T('Approval.Reject', 'Reddet'),
            confirmIcon: 'bi-x-circle',
            onConfirm: function() {
                self.doReject(request.id, self.responseNote());
            }
        });
    };

    // Do approve
    self.doApprove = function(id, note) {
        $.ajax({
            url: '/api/approvals/' + id + '/respond',
            type: 'POST',
            contentType: 'application/json',
            data: JSON.stringify({ approved: true, note: note })
        })
        .done(function() {
            toastr.success(T('DraftRequest.ApproveSuccess', 'Talep onaylandi. Degerlendirme taslak durumuna alindi.'));
            self.closeDetailModal();
            self.refreshAll();
        })
        .fail(function(xhr) {
            var message = xhr.responseJSON?.message || T('DraftRequest.ApproveError', 'Onay islemi basarisiz');
            toastr.error(message);
        });
    };

    // Do reject
    self.doReject = function(id, note) {
        $.ajax({
            url: '/api/approvals/' + id + '/respond',
            type: 'POST',
            contentType: 'application/json',
            data: JSON.stringify({ approved: false, note: note })
        })
        .done(function() {
            toastr.success(T('DraftRequest.RejectSuccess', 'Talep reddedildi.'));
            self.closeDetailModal();
            self.refreshAll();
        })
        .fail(function(xhr) {
            var message = xhr.responseJSON?.message || T('DraftRequest.RejectError', 'Red islemi basarisiz');
            toastr.error(message);
        });
    };

    // Helper functions
    self.getStatusText = function(status) {
        var statuses = {
            'Pending': T('Approval.Status.Pending', 'Beklemede'),
            'Approved': T('Approval.Status.Approved', 'Onaylandi'),
            'Rejected': T('Approval.Status.Rejected', 'Reddedildi'),
            'Cancelled': T('Approval.Status.Cancelled', 'Iptal')
        };
        return statuses[status] || status;
    };

    self.getStatusBadgeClass = function(status) {
        var classes = {
            'Pending': 'bg-warning text-dark',
            'Approved': 'bg-success',
            'Rejected': 'bg-danger',
            'Cancelled': 'bg-secondary'
        };
        return classes[status] || 'bg-secondary';
    };

    self.formatDate = function(dateStr) {
        if (!dateStr) return '-';
        var date = new Date(dateStr);
        return date.toLocaleDateString('tr-TR') + ' ' + date.toLocaleTimeString('tr-TR', { hour: '2-digit', minute: '2-digit' });
    };

    // Refresh all data
    self.refreshAll = function() {
        self.loadRequests();
        self.loadSummary();
    };

    // Initialize
    self.init = function() {
        self.refreshAll();
    };

    self.init();
}

// Initialize ViewModel
$(document).ready(function() {
    ko.applyBindings(new DraftRequestsViewModel(), document.getElementById('draft-requests-app'));
});
