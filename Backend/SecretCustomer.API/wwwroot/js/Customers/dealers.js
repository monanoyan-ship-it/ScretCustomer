// Dealers ViewModel - Müşteri Bayileri
function DealersViewModel() {
    var self = this;

    // Config
    self.customerId = window.dealersConfig ? window.dealersConfig.customerId : null;

    // State
    self.isLoading = ko.observable(false);
    self.isSaving = ko.observable(false);
    self.customerName = ko.observable('');

    // Data
    self.dealers = ko.observableArray([]);

    // Filter
    self.filter = {
        searchTerm: ko.observable(''),
        dealerTypeId: ko.observable(''),
        isActive: ko.observable('')
    };

    // Editing dealer
    self.editingDealer = ko.observable(null);
    self.modal = null;

    // Dealer type helpers
    self.getDealerTypeName = function(typeId) {
        var types = {
            1: 'Perakende',
            2: 'Toptan',
            3: 'Franchise',
            4: 'Yetkili Bayi'
        };
        return types[typeId] || typeId;
    };

    self.getDealerTypeBadgeClass = function(typeId) {
        var classes = {
            1: 'bg-primary',
            2: 'bg-info',
            3: 'bg-warning text-dark',
            4: 'bg-success'
        };
        return classes[typeId] || 'bg-secondary';
    };

    // Load customer info
    self.loadCustomer = function() {
        if (!self.customerId) return;

        ApiService.get('/customers/' + self.customerId)
            .then(function(data) {
                self.customerName(data.companyName || '');
            })
            .catch(function(error) {
                console.error('Error loading customer:', error);
            });
    };

    // Load dealers
    self.loadDealers = function() {
        if (!self.customerId) {
            toastr.error('Müşteri ID bulunamadı.');
            return;
        }

        self.isLoading(true);

        var params = {
            customerId: self.customerId
        };

        if (self.filter.searchTerm()) params.searchTerm = self.filter.searchTerm();
        if (self.filter.dealerTypeId()) params.dealerTypeId = self.filter.dealerTypeId();
        if (self.filter.isActive()) params.isActive = self.filter.isActive();

        var queryString = Object.keys(params).map(function(key) {
            return encodeURIComponent(key) + '=' + encodeURIComponent(params[key]);
        }).join('&');

        ApiService.get('/dealers?' + queryString)
            .then(function(data) {
                self.dealers(data.items || []);
            })
            .catch(function(error) {
                console.error('Error:', error);
                toastr.error(error.message || 'Bayi listesi yüklenirken bir hata oluştu.');
            })
            .finally(function() {
                self.isLoading(false);
            });
    };

    // Apply filters
    self.applyFilters = function() {
        self.loadDealers();
    };

    // Clear filters
    self.clearFilters = function() {
        self.filter.searchTerm('');
        self.filter.dealerTypeId('');
        self.filter.isActive('');
        self.loadDealers();
    };

    // Create new dealer
    self.createNew = function() {
        self.editingDealer({
            id: null,
            name: ko.observable(''),
            dealerTypeId: ko.observable(1),
            address: ko.observable(''),
            city: ko.observable(''),
            district: ko.observable(''),
            phone: ko.observable(''),
            email: ko.observable(''),
            contactPerson: ko.observable(''),
            latitude: ko.observable(null),
            longitude: ko.observable(null),
            notes: ko.observable(''),
            isActive: ko.observable(true)
        });
        self.showModal();
    };

    // Edit dealer
    self.editDealer = function(dealer) {
        self.editingDealer({
            id: dealer.id,
            name: ko.observable(dealer.name),
            dealerTypeId: ko.observable(dealer.dealerTypeId),
            address: ko.observable(dealer.address || ''),
            city: ko.observable(dealer.city || ''),
            district: ko.observable(dealer.district || ''),
            phone: ko.observable(dealer.phone || ''),
            email: ko.observable(dealer.email || ''),
            contactPerson: ko.observable(dealer.contactPerson || ''),
            latitude: ko.observable(dealer.latitude),
            longitude: ko.observable(dealer.longitude),
            notes: ko.observable(dealer.notes || ''),
            isActive: ko.observable(dealer.isActive)
        });
        self.showModal();
    };

    // Save dealer
    self.saveDealer = function() {
        var editing = self.editingDealer();
        if (!editing) return;

        // Validation
        var name = typeof editing.name === 'function' ? editing.name() : editing.name;
        if (!name || name.trim() === '') {
            toastr.error('Bayi adı zorunludur.');
            return;
        }

        self.isSaving(true);

        var data = {
            name: name.trim(),
            dealerTypeId: parseInt(typeof editing.dealerTypeId === 'function' ? editing.dealerTypeId() : editing.dealerTypeId),
            address: (typeof editing.address === 'function' ? editing.address() : editing.address) || null,
            city: (typeof editing.city === 'function' ? editing.city() : editing.city) || null,
            district: (typeof editing.district === 'function' ? editing.district() : editing.district) || null,
            phone: (typeof editing.phone === 'function' ? editing.phone() : editing.phone) || null,
            email: (typeof editing.email === 'function' ? editing.email() : editing.email) || null,
            contactPerson: (typeof editing.contactPerson === 'function' ? editing.contactPerson() : editing.contactPerson) || null,
            latitude: (typeof editing.latitude === 'function' ? editing.latitude() : editing.latitude) || null,
            longitude: (typeof editing.longitude === 'function' ? editing.longitude() : editing.longitude) || null,
            notes: (typeof editing.notes === 'function' ? editing.notes() : editing.notes) || null,
            isActive: typeof editing.isActive === 'function' ? editing.isActive() : editing.isActive,
            customerId: self.customerId
        };

        var isNew = !editing.id;
        var promise = isNew
            ? ApiService.post('/dealers', data)
            : ApiService.put('/dealers/' + editing.id, data);

        promise
            .then(function() {
                toastr.success(isNew ? 'Bayi başarıyla oluşturuldu.' : 'Bayi başarıyla güncellendi.');
                self.hideModal();
                self.loadDealers();
                self.notifyParent();
            })
            .catch(function(error) {
                console.error('Error saving dealer:', error);
                toastr.error(error.message || 'Bayi kaydedilirken bir hata oluştu.');
            })
            .finally(function() {
                self.isSaving(false);
            });
    };

    // Delete dealer
    self.deleteDealer = function(dealer) {
        deleteConfirmation.show(
            '<strong>' + dealer.name + '</strong> bayisini silmek istediğinize emin misiniz?',
            function() {
                ApiService.delete('/dealers/' + dealer.id)
                    .then(function() {
                        toastr.success('Bayi başarıyla silindi.');
                        self.loadDealers();
                        self.notifyParent();
                    })
                    .catch(function(error) {
                        console.error('Error deleting dealer:', error);
                        toastr.error(error.message || 'Bayi silinirken bir hata oluştu.');
                    });
            }
        );
    };

    // Show modal
    self.showModal = function() {
        if (!self.modal) {
            var modalEl = document.getElementById('dealerModal');
            if (modalEl) {
                self.modal = new bootstrap.Modal(modalEl);
            }
        }
        if (self.modal) {
            self.modal.show();
        }
    };

    // Hide modal
    self.hideModal = function() {
        if (self.modal) {
            self.modal.hide();
        }
        self.editingDealer(null);
    };

    // Notify parent window to refresh
    self.notifyParent = function() {
        if (window.opener && !window.opener.closed) {
            try {
                if (window.opener.vm && typeof window.opener.vm.loadCustomers === 'function') {
                    window.opener.vm.loadCustomers();
                }
            } catch (e) {
                console.log('Could not notify parent:', e);
            }
        }
    };

    // Initialize
    self.init = function() {
        self.loadCustomer();
        self.loadDealers();
    };

    // Start
    self.init();
}

// Initialize when document is ready
$(document).ready(function() {
    var app = document.getElementById('dealers-app');
    if (app) {
        ko.applyBindings(new DealersViewModel(), app);
    }
});
