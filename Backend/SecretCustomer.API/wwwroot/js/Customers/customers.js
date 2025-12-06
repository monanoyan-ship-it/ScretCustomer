// Customers ViewModel
function CustomersViewModel() {
    var self = this;

    // Observables
    self.customers = ko.observableArray([]);
    self.isLoading = ko.observable(false);
    self.errorMessage = ko.observable('');
    self.successMessage = ko.observable('');
    self.showInactive = ko.observable(false);

    // Form observables
    self.isEditing = ko.observable(false);
    self.showForm = ko.observable(false);
    self.currentCustomer = ko.observable({
        id: null,
        companyName: '',
        taxNumber: '',
        phone: '',
        email: '',
        address: '',
        city: '',
        isActive: true,
        contractStartDate: null,
        contractEndDate: null,
        notes: ''
    });

    // Computed
    self.filteredCustomers = ko.computed(function() {
        if (self.showInactive()) {
            return self.customers();
        }
        return self.customers().filter(function(c) { return c.isActive; });
    });

    // Load customers
    self.loadCustomers = function() {
        self.isLoading(true);
        self.errorMessage('');

        customerApiService.getAllCustomers(self.showInactive())
            .then(function(data) {
                self.customers(data || []);
            })
            .catch(function(error) {
                console.error('Error loading customers:', error);
                self.errorMessage('Müşteriler yüklenirken bir hata oluştu: ' + (error.message || ''));
            })
            .finally(function() {
                self.isLoading(false);
            });
    };

    // Show create form
    self.showCreateForm = function() {
        self.isEditing(false);
        self.currentCustomer({
            id: null,
            companyName: '',
            taxNumber: '',
            phone: '',
            email: '',
            address: '',
            city: '',
            isActive: true,
            contractStartDate: null,
            contractEndDate: null,
            notes: ''
        });
        self.showForm(true);
    };

    // Show edit form
    self.editCustomer = function(customer) {
        self.isEditing(true);
        self.currentCustomer({
            id: customer.id,
            companyName: customer.companyName,
            taxNumber: customer.taxNumber,
            phone: customer.phone || '',
            email: customer.email || '',
            address: customer.address || '',
            city: customer.city || '',
            isActive: customer.isActive,
            contractStartDate: customer.contractStartDate,
            contractEndDate: customer.contractEndDate,
            notes: customer.notes || ''
        });
        self.showForm(true);
    };

    // Save customer
    self.saveCustomer = function() {
        self.errorMessage('');
        self.successMessage('');

        var customer = self.currentCustomer();

        // Validation
        if (!customer.companyName || !customer.taxNumber) {
            self.errorMessage('Firma adı ve vergi numarası zorunludur.');
            return;
        }

        var promise = self.isEditing() 
            ? customerApiService.updateCustomer(customer.id, customer)
            : customerApiService.createCustomer(customer);

        promise
            .then(function() {
                self.successMessage(self.isEditing() ? 'Müşteri başarıyla güncellendi.' : 'Müşteri başarıyla oluşturuldu.');
                self.showForm(false);
                self.loadCustomers();
            })
            .catch(function(error) {
                console.error('Error saving customer:', error);
                self.errorMessage('Müşteri kaydedilirken bir hata oluştu: ' + (error.message || ''));
            });
    };

    // Cancel form
    self.cancelForm = function() {
        self.showForm(false);
        self.errorMessage('');
        self.successMessage('');
    };

    // Delete customer
    self.deleteCustomer = function(customer) {
        if (!confirm('Bu müşteriyi silmek istediğinizden emin misiniz?\n\n' + customer.companyName)) {
            return;
        }

        customerApiService.deleteCustomer(customer.id)
            .then(function() {
                self.successMessage('Müşteri başarıyla silindi.');
                self.loadCustomers();
            })
            .catch(function(error) {
                console.error('Error deleting customer:', error);
                self.errorMessage('Müşteri silinirken bir hata oluştu: ' + (error.message || ''));
            });
    };

    // View customer details (navigate to personnel management)
    self.viewCustomerDetails = function(customer) {
        window.location.hash = '#/customers/' + customer.id + '/personnel';
    };

    // Toggle inactive customers
    self.toggleShowInactive = function() {
        self.showInactive(!self.showInactive());
        self.loadCustomers();
    };

    // Initialize
    self.loadCustomers();
}
