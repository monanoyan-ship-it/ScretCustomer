// FieldWorker Requests ViewModel
function FieldWorkerRequestsViewModel() {
    var self = this;

    // State
    self.isLoading = ko.observable(true);
    self.isSaving = ko.observable(false);

    // Stats
    self.pendingCount = ko.observable(0);
    self.approvedCount = ko.observable(0);
    self.rejectedCount = ko.observable(0);

    // Data
    self.requests = ko.observableArray([]);
    self.customers = ko.observableArray([]);

    // New request modal
    self.newRequest = {
        customerId: ko.observable(''),
        dealerTypeId: ko.observable(1),
        name: ko.observable(''),
        address: ko.observable(''),
        city: ko.observable(''),
        district: ko.observable(''),
        phone: ko.observable(''),
        contactPerson: ko.observable(''),
        notes: ko.observable('')
    };

    self.modal = null;

    // Helpers
    self.getRequestTypeName = function(typeId) {
        var types = {
            1: 'Yeni Bayi',
            2: 'Bayi Guncelleme',
            3: 'Yeni Personel'
        };
        return types[typeId] || typeId;
    };

    self.getRequestTypeBadge = function(typeId) {
        var classes = {
            1: 'bg-primary',
            2: 'bg-info',
            3: 'bg-success'
        };
        return classes[typeId] || 'bg-secondary';
    };

    self.getStatusText = function(statusId) {
        var statuses = {
            1: 'Beklemede',
            2: 'Onaylandi',
            3: 'Reddedildi'
        };
        return statuses[statusId] || statusId;
    };

    self.getStatusBadgeClass = function(statusId) {
        var classes = {
            1: 'bg-warning text-dark',
            2: 'bg-success',
            3: 'bg-danger'
        };
        return classes[statusId] || 'bg-secondary';
    };

    self.getDealerNameFromJson = function(json) {
        try {
            var data = JSON.parse(json);
            return data.name || data.Name || '-';
        } catch (e) {
            return '-';
        }
    };

    self.formatDate = function(dateStr) {
        if (!dateStr) return '-';
        var date = new Date(dateStr);
        return date.toLocaleDateString('tr-TR');
    };

    // Load customers from assigned projects
    self.loadCustomers = function() {
        return ApiService.get('/fieldworker/projects')
            .then(function(data) {
                // Get unique customers
                var uniqueCustomers = [];
                var seen = {};
                (data || []).forEach(function(p) {
                    if (!seen[p.customerId]) {
                        seen[p.customerId] = true;
                        uniqueCustomers.push({
                            customerId: p.customerId,
                            customerName: p.customerName
                        });
                    }
                });
                self.customers(uniqueCustomers);
            });
    };

    // Load my requests
    self.loadRequests = function() {
        self.isLoading(true);

        ApiService.get('/dealer-requests/my')
            .then(function(data) {
                self.requests(data || []);

                // Calculate stats
                var pending = 0, approved = 0, rejected = 0;
                (data || []).forEach(function(r) {
                    if (r.statusId === 1) pending++;
                    else if (r.statusId === 2) approved++;
                    else if (r.statusId === 3) rejected++;
                });
                self.pendingCount(pending);
                self.approvedCount(approved);
                self.rejectedCount(rejected);
            })
            .catch(function(error) {
                console.error('Error loading requests:', error);
                toastr.error(error.message || 'Talepler yuklenirken hata olustu.');
            })
            .finally(function() {
                self.isLoading(false);
            });
    };

    // Show new request modal
    self.showNewRequestModal = function() {
        self.newRequest.customerId('');
        self.newRequest.dealerTypeId(1);
        self.newRequest.name('');
        self.newRequest.address('');
        self.newRequest.city('');
        self.newRequest.district('');
        self.newRequest.phone('');
        self.newRequest.contactPerson('');
        self.newRequest.notes('');

        if (!self.modal) {
            var modalEl = document.getElementById('newRequestModal');
            if (modalEl) {
                self.modal = new bootstrap.Modal(modalEl);
            }
        }
        if (self.modal) {
            self.modal.show();
        }
    };

    // Submit request
    self.submitRequest = function() {
        // Validation
        if (!self.newRequest.customerId()) {
            toastr.warning('Firma secimi zorunludur.');
            return;
        }
        if (!self.newRequest.name()) {
            toastr.warning('Bayi adi zorunludur.');
            return;
        }

        self.isSaving(true);

        var requestData = {
            name: self.newRequest.name(),
            dealerTypeId: parseInt(self.newRequest.dealerTypeId()),
            address: self.newRequest.address() || null,
            city: self.newRequest.city() || null,
            district: self.newRequest.district() || null,
            phone: self.newRequest.phone() || null,
            contactPerson: self.newRequest.contactPerson() || null,
            notes: self.newRequest.notes() || null
        };

        var data = {
            customerId: parseInt(self.newRequest.customerId()),
            requestTypeId: 1, // NewDealer
            requestDataJson: JSON.stringify(requestData)
        };

        ApiService.post('/dealer-requests', data)
            .then(function() {
                toastr.success('Bayi talebi basariyla gonderildi.');
                if (self.modal) {
                    self.modal.hide();
                }
                self.loadRequests();
            })
            .catch(function(error) {
                console.error('Error submitting request:', error);
                toastr.error(error.message || 'Talep gonderilirken hata olustu.');
            })
            .finally(function() {
                self.isSaving(false);
            });
    };

    // Initialize
    self.init = function() {
        Promise.all([
            self.loadCustomers()
        ]).then(function() {
            self.loadRequests();
        });
    };

    self.init();
}

// Initialize when document is ready
$(document).ready(function() {
    var app = document.getElementById('fieldworker-requests-app');
    if (app) {
        ko.applyBindings(new FieldWorkerRequestsViewModel(), app);
    }
});
