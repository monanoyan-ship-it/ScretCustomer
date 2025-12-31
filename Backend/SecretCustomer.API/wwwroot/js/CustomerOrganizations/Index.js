// Customer Organizations Page ViewModel
function CustomerOrganizationsViewModel() {
    var self = this;

    // Observables
    self.isLoading = ko.observable(true);
    self.errorMessage = ko.observable('');
    self.modalErrorMessage = ko.observable('');

    // Customers
    self.customers = ko.observableArray([]);
    self.customerSearchText = ko.observable('');
    self.selectedCustomer = ko.observable(null);

    // Organizations
    self.organizations = ko.observableArray([]);
    self.isLoadingOrganizations = ko.observable(false);

    // Modal
    self.isModalOpen = ko.observable(false);
    self.isSaving = ko.observable(false);
    self.editingOrganization = ko.observable(null);

    // Filtered customers
    self.filteredCustomers = ko.computed(function() {
        var search = self.customerSearchText().toLowerCase();
        if (!search) {
            return self.customers();
        }
        return self.customers().filter(function(c) {
            return c.companyName.toLowerCase().indexOf(search) > -1;
        });
    });

    // Load customers
    self.loadCustomers = function() {
        self.isLoading(true);
        self.errorMessage('');

        ApiService.get('/api/customers')
            .then(function(data) {
                // Add organization count to each customer
                var customersWithCount = data.map(function(c) {
                    c.organizationCount = c.organizationCount || 0;
                    return c;
                });
                self.customers(customersWithCount);
            })
            .catch(function(error) {
                console.error('Error loading customers:', error);
                self.errorMessage('Müşteriler yüklenirken bir hata oluştu.');
            })
            .finally(function() {
                self.isLoading(false);
            });
    };

    // Select customer
    self.selectCustomer = function(customer) {
        self.selectedCustomer(customer);
        self.loadOrganizations(customer.id);
    };

    // Load organizations for customer
    self.loadOrganizations = function(customerId) {
        self.isLoadingOrganizations(true);
        self.organizations([]);

        ApiService.get('/api/customer-organizations/by-customer/' + customerId + '?includeInactive=true')
            .then(function(data) {
                self.organizations(data || []);
                // Update customer's organization count
                if (self.selectedCustomer()) {
                    self.selectedCustomer().organizationCount = data.length;
                }
            })
            .catch(function(error) {
                console.error('Error loading organizations:', error);
                self.errorMessage('Organizasyonlar yüklenirken bir hata oluştu.');
            })
            .finally(function() {
                self.isLoadingOrganizations(false);
            });
    };

    // Create new organization
    self.createNewOrganization = function() {
        if (!self.selectedCustomer()) {
            self.errorMessage('Lütfen önce bir müşteri seçin.');
            return;
        }

        self.editingOrganization({
            id: null,
            name: ko.observable(''),
            code: ko.observable(''),
            description: ko.observable(''),
            order: ko.observable(0),
            isActive: ko.observable(true),
            customerId: self.selectedCustomer().id
        });
        self.modalErrorMessage('');
        self.isModalOpen(true);
    };

    // Edit organization
    self.editOrganization = function(org) {
        self.editingOrganization({
            id: org.id,
            name: ko.observable(org.name),
            code: ko.observable(org.code || ''),
            description: ko.observable(org.description || ''),
            order: ko.observable(org.order || 0),
            isActive: ko.observable(org.isActive),
            customerId: org.customerId
        });
        self.modalErrorMessage('');
        self.isModalOpen(true);
    };

    // Save organization
    self.saveOrganization = function() {
        var org = self.editingOrganization();
        if (!org) return;

        var name = ko.unwrap(org.name);
        if (!name || name.trim() === '') {
            self.modalErrorMessage('Organizasyon adı zorunludur.');
            return;
        }

        self.isSaving(true);
        self.modalErrorMessage('');

        var data = {
            name: name.trim(),
            code: ko.unwrap(org.code) || null,
            description: ko.unwrap(org.description) || null,
            order: parseInt(ko.unwrap(org.order)) || 0,
            isActive: ko.unwrap(org.isActive),
            customerId: org.customerId
        };

        var promise;
        if (org.id) {
            // Update
            promise = ApiService.put('/api/customer-organizations/' + org.id, data);
        } else {
            // Create
            promise = ApiService.post('/api/customer-organizations', data);
        }

        promise
            .then(function(result) {
                self.closeModal();
                self.loadOrganizations(self.selectedCustomer().id);
                toastr.success(org.id ? 'Organizasyon güncellendi.' : 'Organizasyon oluşturuldu.');
            })
            .catch(function(error) {
                console.error('Error saving organization:', error);
                var errorMsg = 'Organizasyon kaydedilirken bir hata oluştu.';
                if (error && error.message) {
                    errorMsg = error.message;
                }
                self.modalErrorMessage(errorMsg);
            })
            .finally(function() {
                self.isSaving(false);
            });
    };

    // Delete organization
    self.deleteOrganization = function(org) {
        showDeleteConfirmation(
            'Organizasyon Sil',
            '"' + org.name + '" organizasyonunu silmek istediğinize emin misiniz?',
            function() {
                ApiService.delete('/api/customer-organizations/' + org.id)
                    .then(function() {
                        self.loadOrganizations(self.selectedCustomer().id);
                        toastr.success('Organizasyon silindi.');
                    })
                    .catch(function(error) {
                        console.error('Error deleting organization:', error);
                        var errorMsg = 'Organizasyon silinirken bir hata oluştu.';
                        if (error && error.message) {
                            errorMsg = error.message;
                        }
                        toastr.error(errorMsg);
                    });
            }
        );
    };

    // Close modal
    self.closeModal = function() {
        self.isModalOpen(false);
        self.editingOrganization(null);
        self.modalErrorMessage('');
    };

    // Initialize
    self.loadCustomers();
}

// Initialize on page load
$(document).ready(function() {
    var appElement = document.getElementById('customer-orgs-app');
    if (appElement) {
        ko.applyBindings(new CustomerOrganizationsViewModel(), appElement);
    }
});
