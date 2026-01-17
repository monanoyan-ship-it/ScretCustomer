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

    // ========== CHIP-BASED FILTER SYSTEM ==========
    self.selectedFilterType = ko.observable('');
    self.activeFilters = ko.observableArray([]);

    // Temp filter values
    self.tempFilter = {
        approvalType: ko.observable(null),
        status: ko.observable(null),
        priority: ko.observable(null),
        searchTerm: ko.observable('')
    };

    // Filter labels
    self.filterLabels = {
        approvalType: 'Onay Tipi',
        status: 'Durum',
        priority: 'Öncelik',
        searchTerm: 'Arama'
    };

    self.approvalTypes = [
        { id: 'Evaluation', name: 'Değerlendirme' },
        { id: 'Assignment', name: 'Atama' },
        { id: 'Project', name: 'Proje' },
        { id: 'Meeting', name: 'Toplantı' },
        { id: 'Training', name: 'Eğitim' },
        { id: 'Delegation', name: 'Vekalet' },
        { id: 'General', name: 'Genel' }
    ];

    self.statusOptions = [
        { id: 'Pending', name: 'Bekliyor' },
        { id: 'Approved', name: 'Onaylandı' },
        { id: 'Rejected', name: 'Reddedildi' },
        { id: 'Cancelled', name: 'İptal Edildi' }
    ];

    self.priorityOptions = [
        { id: 'Low', name: 'Düşük' },
        { id: 'Normal', name: 'Normal' },
        { id: 'High', name: 'Yüksek' },
        { id: 'Urgent', name: 'Acil' }
    ];

    // Can add filter
    self.canAddFilter = ko.computed(function() {
        var type = self.selectedFilterType();
        if (!type) return false;

        switch (type) {
            case 'approvalType': return self.tempFilter.approvalType() !== null;
            case 'status': return self.tempFilter.status() !== null;
            case 'priority': return self.tempFilter.priority() !== null;
            case 'searchTerm': return self.tempFilter.searchTerm().trim() !== '';
            default: return false;
        }
    });

    // Add filter
    self.addFilter = function() {
        var type = self.selectedFilterType();
        if (!type) return;

        var filter = {
            type: type,
            label: self.filterLabels[type],
            value: null,
            displayValue: ''
        };

        switch (type) {
            case 'approvalType':
                var approvalTypeId = self.tempFilter.approvalType();
                if (!approvalTypeId) return;
                var approvalType = self.approvalTypes.find(function(t) { return t.id === approvalTypeId; });
                filter.value = approvalTypeId;
                filter.displayValue = approvalType ? approvalType.name : approvalTypeId;
                self.tempFilter.approvalType(null);
                break;

            case 'status':
                var statusId = self.tempFilter.status();
                if (!statusId) return;
                var status = self.statusOptions.find(function(s) { return s.id === statusId; });
                filter.value = statusId;
                filter.displayValue = status ? status.name : statusId;
                self.tempFilter.status(null);
                break;

            case 'priority':
                var priorityId = self.tempFilter.priority();
                if (!priorityId) return;
                var priority = self.priorityOptions.find(function(p) { return p.id === priorityId; });
                filter.value = priorityId;
                filter.displayValue = priority ? priority.name : priorityId;
                self.tempFilter.priority(null);
                break;

            case 'searchTerm':
                var searchTerm = self.tempFilter.searchTerm().trim();
                if (!searchTerm) return;
                filter.value = searchTerm;
                filter.displayValue = searchTerm;
                self.tempFilter.searchTerm('');
                break;

            default:
                return;
        }

        self.activeFilters.push(filter);
        self.selectedFilterType('');
        self.currentPage(1);
        self.loadApprovals();
    };

    // Remove filter
    self.removeFilter = function(filter) {
        self.activeFilters.remove(filter);
        self.currentPage(1);
        self.loadApprovals();
    };

    // Clear all filters
    self.clearFilters = function() {
        self.activeFilters([]);
        self.currentPage(1);
        self.loadApprovals();
    };

    // Build filter params
    self.buildFilterParams = function(params) {
        self.activeFilters().forEach(function(filter) {
            switch (filter.type) {
                case 'approvalType':
                    params.append('approvalType', filter.value);
                    break;
                case 'status':
                    params.append('status', filter.value);
                    break;
                case 'priority':
                    params.append('priority', filter.value);
                    break;
                case 'searchTerm':
                    params.append('search', filter.value);
                    break;
            }
        });
    };

    // Sorting
    self.sorting = TableSorting.createSortState('requestedAt', 'desc');

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

    // Sorting subscriptions
    self.sorting.sortBy.subscribe(function() {
        self.currentPage(1);
        self.loadApprovals();
    });
    self.sorting.sortDirection.subscribe(function() {
        self.currentPage(1);
        self.loadApprovals();
    });

    // Load approvals
    self.loadApprovals = function() {
        self.isLoading(true);
        var params = new URLSearchParams({
            page: self.currentPage(),
            pageSize: self.pageSize()
        });

        // Add chip-based filters
        self.buildFilterParams(params);

        if (self.sorting.sortBy()) params.append('sortBy', self.sorting.sortBy());
        if (self.sorting.sortDirection()) params.append('sortDirection', self.sorting.sortDirection());

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

// Translation keys
var TRANSLATION_KEYS = [
    'Common.Total',
    'Approval.LoadError',
    'Approval.DetailLoadError',
    'Approval.Approve',
    'Approval.ConfirmApprove',
    'Approval.ApproveSuccess',
    'Approval.ApproveError',
    'Approval.Reject',
    'Approval.ConfirmReject',
    'Approval.RejectSuccess',
    'Approval.RejectError',
    'Common.Cancel',
    'Approval.ConfirmCancel',
    'Approval.CancelRequest',
    'Approval.CancelSuccess',
    'Approval.CancelError',
    'Approval.Type.Evaluation',
    'Approval.Type.Assignment',
    'Approval.Type.Project',
    'Approval.Type.Meeting',
    'Approval.Type.Training',
    'Approval.Type.Delegation',
    'Approval.Type.General',
    'Priority.Low',
    'Priority.Normal',
    'Priority.High',
    'Priority.Urgent',
    // Confirm modal keys
    'Confirm.Title',
    'Confirm.Message',
    'Common.Confirm'
];

// Initialize ViewModel
$(document).ready(function() {
    Localization.loadKeys(TRANSLATION_KEYS).then(function() {
        ko.applyBindings(new ApprovalsViewModel(), document.getElementById('approvals-app'));
    });
});
