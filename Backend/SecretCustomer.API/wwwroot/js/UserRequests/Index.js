// UserRequests Index ViewModel
// Taslaga alma talepleri + Personel talepleri yonetimi (Admin)
// Tab yapisi ile iki talep turunu ayni sayfada yonetir

function UserRequestsViewModel() {
    var self = this;

    // Tab State
    self.activeTab = ko.observable('draft');

    // Loading
    self.isLoading = ko.observable(false);

    // ==================== DRAFT REQUESTS STATE ====================
    self.draftRequests = ko.observableArray([]);
    self.draftCounts = ko.observable({ pending: 0, approved: 0, rejected: 0, total: 0 });

    // ==================== PERSONNEL REQUESTS STATE ====================
    self.personnelRequests = ko.observableArray([]);
    self.personnelCounts = ko.observable({ pending: 0, approved: 0, rejected: 0, total: 0 });

    // Draft Detail Modal
    self.isDraftDetailModalOpen = ko.observable(false);
    self.selectedDraftRequest = ko.observable(null);
    self.draftRequestDetail = ko.observable(null);
    self.draftEvaluationDetail = ko.observable(null);

    // ==================== SORTING ====================
    self.draftSorting = TableSorting.createSortState('requestedAt', 'desc');
    self.personnelSorting = TableSorting.createSortState('requestedAt', 'desc');
    // Sorted Draft Requests
    self.sortedDraftRequests = ko.computed(function() {
        var items = self.draftRequests();
        var sortBy = self.draftSorting.sortBy();
        var sortDir = self.draftSorting.sortDirection();
        if (!sortBy || items.length === 0) return items;
        return TableSorting.clientSort(items, sortBy, sortDir);
    });

    // Sorted Personnel Requests
    self.sortedPersonnelRequests = ko.computed(function() {
        var items = self.personnelRequests();
        var sortBy = self.personnelSorting.sortBy();
        var sortDir = self.personnelSorting.sortDirection();
        if (!sortBy || items.length === 0) return items;
        return TableSorting.clientSort(items, sortBy, sortDir);
    });

    // Personnel Detail Modal
    self.isPersonnelDetailModalOpen = ko.observable(false);
    self.selectedPersonnelRequest = ko.observable(null);

    // Personnel Approve Modal
    self.isApproveModalOpen = ko.observable(false);
    self.approvingRequest = ko.observable(null);
    self.approveFirstName = ko.observable('');
    self.approveLastName = ko.observable('');
    self.approveUsername = ko.observable('');
    self.approveEmail = ko.observable('');
    self.approveOrganizationId = ko.observable(null);
    self.availableOrganizations = ko.observableArray([]);
    self.approveSupervisorId = ko.observable(null);
    self.availableSupervisors = ko.observableArray([]);

    // Personnel Reject Modal
    self.isRejectModalOpen = ko.observable(false);
    self.rejectingRequest = ko.observable(null);
    self.rejectReason = ko.observable('');
    self.rejectSelectedPersonnelId = ko.observable(null);
    self.availablePersonnelForReject = ko.observableArray([]);

    // Current counts based on active tab
    self.currentCounts = ko.computed(function() {
        var tab = self.activeTab();
        if (tab === 'draft') return self.draftCounts();
        return self.personnelCounts();
    });

    // ==================== FILTERS ====================
    self.filterStatus = ko.observable('');
    self.searchTerm = ko.observable('');

    // Filter subscriptions
    self.filterStatus.subscribe(function() {
        self.loadCurrentTab();
    });

    self.searchTerm.subscribe(function() {
        clearTimeout(self.searchTimeout);
        self.searchTimeout = setTimeout(function() {
            self.loadCurrentTab();
        }, 300);
    });

    self.clearFilters = function() {
        self.filterStatus('');
        self.searchTerm('');
    };

    // ==================== TAB SWITCHING ====================
    self.setActiveTab = function(tab) {
        if (self.activeTab() !== tab) {
            self.activeTab(tab);
            self.clearFilters();
            self.loadCurrentTab();
        }
    };

    self.loadCurrentTab = function() {
        var tab = self.activeTab();
        if (tab === 'draft') {
            self.loadDraftRequests();
        } else if (tab === 'personnel') {
            self.loadPersonnelRequests();
        }
    };

    // ==================== DRAFT REQUESTS ====================
    self.loadDraftRequests = function() {
        self.isLoading(true);
        var params = new URLSearchParams({
            page: 1,
            pageSize: 100,
            approvalType: 'Evaluation'
        });
        if (self.filterStatus()) params.append('status', self.filterStatus() === '1' ? 'Pending' : self.filterStatus() === '2' ? 'Approved' : 'Rejected');
        if (self.searchTerm()) params.append('searchTerm', self.searchTerm());

        $.get('/api/approvals?' + params.toString())
            .done(function(response) {
                self.draftRequests(response.items || []);
            })
            .fail(function() {
                toastr.error(T('DraftRequest.LoadError', 'Talepler yuklenirken hata olustu'));
            })
            .always(function() {
                self.isLoading(false);
            });
    };

    self.loadDraftCounts = function() {
        $.get('/api/approvals?approvalType=Evaluation&status=Pending&pageSize=1')
            .done(function(response) {
                var counts = self.draftCounts();
                counts.pending = response.totalCount || 0;
                self.draftCounts(counts);
                self.updateDraftTotal();
            });

        $.get('/api/approvals?approvalType=Evaluation&status=Approved&pageSize=1')
            .done(function(response) {
                var counts = self.draftCounts();
                counts.approved = response.totalCount || 0;
                self.draftCounts(counts);
                self.updateDraftTotal();
            });

        $.get('/api/approvals?approvalType=Evaluation&status=Rejected&pageSize=1')
            .done(function(response) {
                var counts = self.draftCounts();
                counts.rejected = response.totalCount || 0;
                self.draftCounts(counts);
                self.updateDraftTotal();
            });
    };

    self.updateDraftTotal = function() {
        var counts = self.draftCounts();
        counts.total = counts.pending + counts.approved + counts.rejected;
        self.draftCounts({ ...counts });
    };

    self.showDraftDetail = function(request) {
        self.selectedDraftRequest(request);
        self.draftRequestDetail(null);
        self.draftEvaluationDetail(null);
        self.isDraftDetailModalOpen(true);

        // Load approval detail for description
        $.get('/api/approvals/' + request.id)
            .done(function(detail) {
                self.draftRequestDetail(detail);
            });

        // Load evaluation detail if available
        if (request.relatedEntityId) {
            $.get('/api/evaluations/' + request.relatedEntityId)
                .done(function(evaluation) {
                    self.draftEvaluationDetail(evaluation);
                })
                .fail(function() {
                    self.draftEvaluationDetail(null);
                });
        }
    };

    self.closeDraftDetailModal = function() {
        self.isDraftDetailModalOpen(false);
        self.selectedDraftRequest(null);
        self.draftRequestDetail(null);
        self.draftEvaluationDetail(null);
    };

    self.approveDraftRequest = function(request) {
        showConfirmModal({
            title: T('Approval.Approve', 'Onayla'),
            message: T('DraftRequest.ConfirmApprove', 'Bu talebi onaylamak istediginizden emin misiniz? Degerlendirme taslak durumuna alinacaktir.'),
            type: 'success',
            confirmText: T('Approval.Approve', 'Onayla'),
            confirmIcon: 'bi-check-circle',
            onConfirm: function() {
                self.doDraftApprove(request.id);
            }
        });
    };

    self.rejectDraftRequest = function(request) {
        showConfirmModal({
            title: T('Approval.Reject', 'Reddet'),
            message: T('DraftRequest.ConfirmReject', 'Bu talebi reddetmek istediginizden emin misiniz?'),
            type: 'danger',
            confirmText: T('Approval.Reject', 'Reddet'),
            confirmIcon: 'bi-x-circle',
            onConfirm: function() {
                self.doDraftReject(request.id);
            }
        });
    };

    self.doDraftApprove = function(id) {
        $.ajax({
            url: '/api/approvals/' + id + '/respond',
            type: 'POST',
            contentType: 'application/json',
            data: JSON.stringify({ approved: true, note: '' })
        })
        .done(function() {
            toastr.success(T('DraftRequest.ApproveSuccess', 'Talep onaylandi. Degerlendirme taslak durumuna alindi.'));
            self.refreshAll();
        })
        .fail(function(xhr) {
            var message = xhr.responseJSON?.message || T('Approval.ApproveError', 'Onay islemi basarisiz');
            toastr.error(message);
        });
    };

    self.doDraftReject = function(id) {
        $.ajax({
            url: '/api/approvals/' + id + '/respond',
            type: 'POST',
            contentType: 'application/json',
            data: JSON.stringify({ approved: false, note: '' })
        })
        .done(function() {
            toastr.success(T('DraftRequest.RejectSuccess', 'Talep reddedildi.'));
            self.refreshAll();
        })
        .fail(function(xhr) {
            var message = xhr.responseJSON?.message || T('Approval.RejectError', 'Red islemi basarisiz');
            toastr.error(message);
        });
    };

    // ==================== PERSONNEL REQUESTS ====================
    self.loadPersonnelRequests = function() {
        self.isLoading(true);
        var params = new URLSearchParams({
            page: 1,
            pageSize: 100
        });
        if (self.filterStatus()) params.append('status', self.filterStatus());
        if (self.searchTerm()) params.append('searchTerm', self.searchTerm());

        $.get('/api/personnel-requests?' + params.toString())
            .done(function(response) {
                var items = (response.items || []).map(function(item) {
                    item.fullName = item.firstName + ' ' + item.lastName;
                    return item;
                });
                self.personnelRequests(items);
            })
            .fail(function() {
                toastr.error(T('PersonnelRequest.LoadError', 'Personel talepleri yuklenirken hata olustu'));
            })
            .always(function() {
                self.isLoading(false);
            });
    };

    self.loadPersonnelCounts = function() {
        $.get('/api/personnel-requests/counts')
            .done(function(response) {
                self.personnelCounts({
                    pending: response.pending || 0,
                    approved: response.approved || 0,
                    rejected: response.rejected || 0,
                    total: response.total || 0
                });
            });
    };

    self.showPersonnelDetail = function(request) {
        self.selectedPersonnelRequest(request);
        self.isPersonnelDetailModalOpen(true);
    };

    self.closePersonnelDetailModal = function() {
        self.isPersonnelDetailModalOpen(false);
        self.selectedPersonnelRequest(null);
    };

    // ==================== PERSONNEL APPROVE MODAL ====================
    self.showApprovePersonnelModal = function(request) {
        self.approvingRequest(request);
        self.approveFirstName(request.firstName || '');
        self.approveLastName(request.lastName || '');
        self.approveUsername('');
        self.approveEmail('');
        self.approveOrganizationId(request.customerOrganizationId ? String(request.customerOrganizationId) : null);
        self.availableOrganizations([]);
        self.approveSupervisorId(null);
        self.availableSupervisors([]);

        // Load organizations for the customer
        if (request.customerId) {
            $.get('/api/customer-organizations/by-customer/' + request.customerId)
                .done(function(orgs) {
                    self.availableOrganizations(orgs || []);
                });
        }

        // Load supervisors for the organization
        self.loadSupervisorsForOrg(request.customerOrganizationId);

        self.isApproveModalOpen(true);
    };

    self.loadSupervisorsForOrg = function(orgId) {
        self.approveSupervisorId(null);
        self.availableSupervisors([]);
        if (orgId) {
            $.get('/api/personnel-requests/supervisors/' + orgId)
                .done(function(supervisors) {
                    self.availableSupervisors(supervisors || []);
                });
        }
    };

    // Organizasyon değişince süpervizörleri yeniden yükle
    self.approveOrganizationId.subscribe(function(newOrgId) {
        self.loadSupervisorsForOrg(newOrgId);
    });

    self.closeApproveModal = function() {
        self.isApproveModalOpen(false);
        self.approvingRequest(null);
    };

    self.confirmApprove = function() {
        var request = self.approvingRequest();
        if (!request || !self.approveUsername()) {
            toastr.warning(T('PersonnelRequest.UsernameRequired', 'Kullanici adi zorunludur'));
            return;
        }

        $.ajax({
            url: '/api/personnel-requests/' + request.id + '/approve',
            type: 'POST',
            contentType: 'application/json',
            data: JSON.stringify({
                username: self.approveUsername(),
                email: self.approveEmail() || null,
                firstName: self.approveFirstName() || null,
                lastName: self.approveLastName() || null,
                customerOrganizationId: self.approveOrganizationId() ? parseInt(self.approveOrganizationId(), 10) : null,
                supervisorId: self.approveSupervisorId() || null
            })
        })
        .done(function() {
            toastr.success(T('PersonnelRequest.ApproveSuccess', 'Personel talebi onaylandi ve kullanici olusturuldu.'));
            self.closeApproveModal();
            self.refreshAll();
        })
        .fail(function(xhr) {
            var message = xhr.responseJSON?.message || T('Approval.ApproveError', 'Onay islemi basarisiz');
            toastr.error(message);
        });
    };

    // ==================== PERSONNEL REJECT MODAL ====================
    self.showRejectPersonnelModal = function(request) {
        self.rejectingRequest(request);
        self.rejectReason('');
        self.rejectSelectedPersonnelId(null);
        self.availablePersonnelForReject([]);

        // Load personnel for the organization
        if (request.customerOrganizationId) {
            $.get('/api/customer-personnel/by-organization/' + request.customerOrganizationId)
                .done(function(personnel) {
                    self.availablePersonnelForReject(personnel || []);
                })
                .fail(function() {
                    // Fallback: try loading by customer
                    if (request.customerId) {
                        $.get('/api/customer-personnel/by-customer/' + request.customerId)
                            .done(function(personnel) {
                                self.availablePersonnelForReject(personnel || []);
                            });
                    }
                });
        }

        self.isRejectModalOpen(true);
    };

    self.closeRejectModal = function() {
        self.isRejectModalOpen(false);
        self.rejectingRequest(null);
    };

    self.confirmReject = function() {
        var request = self.rejectingRequest();
        if (!request || !self.rejectReason()) {
            toastr.warning(T('PersonnelRequest.RejectReasonRequired', 'Red sebebi zorunludur'));
            return;
        }

        var data = {
            rejectReason: self.rejectReason()
        };

        // Eğer personel seçildiyse ekle
        if (self.rejectSelectedPersonnelId()) {
            data.correctPersonnelId = self.rejectSelectedPersonnelId();
        }

        $.ajax({
            url: '/api/personnel-requests/' + request.id + '/reject',
            type: 'POST',
            contentType: 'application/json',
            data: JSON.stringify(data)
        })
        .done(function() {
            var message = self.rejectSelectedPersonnelId()
                ? T('PersonnelRequest.RejectSuccessWithPersonnel', 'Talep reddedildi. Degerlendirme secilen personele atandi.')
                : T('PersonnelRequest.RejectSuccess', 'Personel talebi reddedildi. Ilgili degerlendirme taslaga alindi.');
            toastr.success(message);
            self.closeRejectModal();
            self.refreshAll();
        })
        .fail(function(xhr) {
            var message = xhr.responseJSON?.message || T('Approval.RejectError', 'Red islemi basarisiz');
            toastr.error(message);
        });
    };

    // ==================== HELPERS ====================
    self.getStatusText = function(status) {
        // Personnel uses int (0=Pending, 1=Approved, 2=Rejected)
        // Draft uses string (Pending, Approved, Rejected)
        var statusMap = {
            0: T('Project.Pending', 'Beklemede'),
            1: T('Status.Approved', 'Onaylandi'),
            2: T('Status.Rejected', 'Reddedildi'),
            'Pending': T('Project.Pending', 'Beklemede'),
            'Approved': T('Status.Approved', 'Onaylandi'),
            'Rejected': T('Status.Rejected', 'Reddedildi'),
            'Cancelled': T('Approval.Status.Cancelled', 'Iptal')
        };
        return statusMap[status] || status;
    };

    self.getStatusBadgeClass = function(status) {
        var classMap = {
            0: 'bg-warning text-dark',
            1: 'bg-success',
            2: 'bg-danger',
            'Pending': 'bg-warning text-dark',
            'Approved': 'bg-success',
            'Rejected': 'bg-danger',
            'Cancelled': 'bg-secondary'
        };
        return classMap[status] || 'bg-secondary';
    };

    self.formatDate = function(dateStr) {
        if (!dateStr) return '-';
        var date = new Date(dateStr);
        return date.toLocaleDateString('tr-TR') + ' ' + date.toLocaleTimeString('tr-TR', { hour: '2-digit', minute: '2-digit' });
    };

    // ==================== REFRESH ====================
    self.refreshAll = function() {
        self.loadDraftRequests();
        self.loadDraftCounts();
        self.loadPersonnelRequests();
        self.loadPersonnelCounts();
    };

    // ==================== INITIALIZE ====================
    self.init = function() {
        // Check URL for tab parameter
        var urlParams = new URLSearchParams(window.location.search);
        var tab = urlParams.get('tab');
        if (tab === 'personnel') {
            self.activeTab('personnel');
        }

        self.refreshAll();
    };

    self.init();
}

// Translation keys
var TRANSLATION_KEYS = [
    'Common.Total',
    'DraftRequest.LoadError',
    'DraftRequest.ConfirmApprove',
    'DraftRequest.ConfirmReject',
    'DraftRequest.ApproveSuccess',
    'Approval.ApproveError',
    'DraftRequest.RejectSuccess',
    'Approval.RejectError',
    'DraftRequest.NoEvaluationLinked',
    'PersonnelRequest.LoadError',
    'PersonnelRequest.ApproveSuccess',
    'Approval.ApproveError',
    'PersonnelRequest.RejectSuccess',
    'Approval.RejectError',
    'PersonnelRequest.UsernameRequired',
    'PersonnelRequest.RejectReasonRequired',
    'PersonnelRequest.NoEvaluationLinked',
    'Approval.Approve',
    'Approval.Reject',
    'Project.Pending',
    'Status.Approved',
    'Status.Rejected',
    'Approval.Status.Cancelled'
];

// Initialize ViewModel
$(document).ready(function() {
    Localization.loadKeys(TRANSLATION_KEYS).then(function() {
        ko.applyBindings(new UserRequestsViewModel(), document.getElementById('user-requests-app'));
    });
});
