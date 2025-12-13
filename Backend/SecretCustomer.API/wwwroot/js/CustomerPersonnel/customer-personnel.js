// Customer Personnel ViewModel
function CustomerPersonnelViewModel(customerId) {
    var self = this;

    // Observables
    self.customerId = ko.observable(customerId);
    self.customer = ko.observable(null);
    self.personnel = ko.observableArray([]);
    self.availableCustomers = ko.observableArray([]);
    self.isLoading = ko.observable(false);
    self.isSaving = ko.observable(false);
    self.errorMessage = ko.observable('');
    self.successMessage = ko.observable('');
    self.showInactive = ko.observable(false);

    // Modal
    self.isModalOpen = ko.observable(false);
    self.editingPersonnel = ko.observable(null);

    // Reset password modal (Admin)
    self.showChangePasswordModal = ko.observable(false);
    self.selectedPersonnelForPassword = ko.observable(null);
    self.isResettingPassword = ko.observable(false);
    self.newPassword = ko.observable('');
    self.confirmPassword = ko.observable('');

    // Personnel roles
    self.personnelRoles = [
        { value: 1, text: 'Müşteri Yöneticisi' },
        { value: 2, text: 'Müşteri Süpervizörü' },
        { value: 3, text: 'Müşteri Operatörü' },
        { value: 4, text: 'Müşteri Görüntüleyici' }
    ];

    // Computed
    self.filteredPersonnel = ko.computed(function() {
        if (self.showInactive()) {
            return self.personnel();
        }
        return self.personnel().filter(function(p) { return p.isActive; });
    });

    self.getRoleName = function(role) {
        var roleObj = self.personnelRoles.find(function(r) { return r.value === role; });
        return roleObj ? roleObj.text : 'Bilinmeyen';
    };

    // Load customer info
    self.loadCustomer = function() {
        if (!customerId) return;

        customerApiService.getCustomerById(customerId)
            .then(function(data) {
                self.customer(data);
            })
            .catch(function(error) {
                console.error('Error loading customer:', error);
                self.errorMessage('Müşteri bilgileri yüklenirken bir hata oluştu.');
            });
    };

    // Load all customers (for dropdown)
    self.loadCustomers = function() {
        customerApiService.getActiveCustomers()
            .then(function(data) {
                self.availableCustomers(data || []);
            })
            .catch(function(error) {
                console.error('Error loading customers:', error);
            });
    };

    // Load personnel
    self.loadPersonnel = function() {
        self.isLoading(true);
        self.errorMessage('');

        var promise = customerId 
            ? customerApiService.getPersonnelByCustomerId(customerId, self.showInactive())
            : customerApiService.getAllPersonnel(self.showInactive());

        promise
            .then(function(data) {
                self.personnel(data || []);
            })
            .catch(function(error) {
                console.error('Error loading personnel:', error);
                self.errorMessage('Personeller yüklenirken bir hata oluştu: ' + (error.message || ''));
            })
            .finally(function() {
                self.isLoading(false);
            });
    };

    // Create new personnel
    self.createNew = function() {
        self.editingPersonnel({
            id: null,
            customerId: customerId || '',
            username: '',
            email: '',
            password: '',
            firstName: '',
            lastName: '',
            phoneNumber: '',
            department: '',
            title: '',
            role: 1,
            isActive: true,
            notes: ''
        });
        self.isModalOpen(true);
    };

    // Edit personnel
    self.editPersonnel = function(personnel) {
        self.editingPersonnel({
            id: personnel.id,
            customerId: personnel.customerId,
            username: personnel.username,
            email: personnel.email,
            password: '',
            firstName: personnel.firstName,
            lastName: personnel.lastName,
            phoneNumber: personnel.phoneNumber || '',
            department: personnel.department || '',
            title: personnel.title || '',
            role: personnel.role,
            isActive: personnel.isActive,
            notes: personnel.notes || ''
        });
        self.isModalOpen(true);
    };

    // Save personnel
    self.savePersonnel = function() {
        self.errorMessage('');
        self.successMessage('');

        var personnel = self.editingPersonnel();
        if (!personnel) return;

        // Validation
        if (!personnel.customerId) {
            self.errorMessage('Müşteri seçimi zorunludur.');
            return;
        }

        if (!personnel.username || !personnel.email || !personnel.firstName || !personnel.lastName) {
            self.errorMessage('Kullanıcı adı, e-posta, ad ve soyad zorunludur.');
            return;
        }

        if (!personnel.id && !personnel.password) {
            self.errorMessage('Yeni personel için şifre zorunludur.');
            return;
        }

        self.isSaving(true);

        var promise = personnel.id 
            ? customerApiService.updatePersonnel(personnel.id, personnel)
            : customerApiService.createPersonnel(personnel);

        promise
            .then(function() {
                self.successMessage(personnel.id ? 'Personel başarıyla güncellendi.' : 'Personel başarıyla oluşturuldu.');
                self.isModalOpen(false);
                self.loadPersonnel();
            })
            .catch(function(error) {
                console.error('Error saving personnel:', error);
                self.errorMessage('Personel kaydedilirken bir hata oluştu: ' + (error.message || ''));
            })
            .finally(function() {
                self.isSaving(false);
            });
    };

    // Close modal
    self.closeModal = function() {
        self.isModalOpen(false);
        self.editingPersonnel(null);
        self.errorMessage('');
    };

    // Delete personnel
    self.deletePersonnel = function(personnel) {
        deleteConfirmation.show('Bu personeli silmek istediğinizden emin misiniz?\n\n' + personnel.fullName, function() {
            customerApiService.deletePersonnel(personnel.id)
                .then(function() {
                    self.successMessage('Personel başarıyla silindi.');
                    self.loadPersonnel();
                })
                .catch(function(error) {
                    console.error('Error deleting personnel:', error);
                    self.errorMessage('Personel silinirken bir hata oluştu: ' + (error.message || ''));
                });
        });
    };

    // Show reset password modal (Admin)
    self.showChangePassword = function(personnel) {
        self.selectedPersonnelForPassword(personnel);
        self.newPassword('');
        self.confirmPassword('');
        self.showChangePasswordModal(true);
    };

    // Reset password (Admin)
    self.resetPassword = function() {
        self.errorMessage('');
        self.successMessage('');

        var newPass = self.newPassword();
        var confirmPass = self.confirmPassword();

        if (!newPass || !confirmPass) {
            self.errorMessage('Tüm alanlar zorunludur.');
            return;
        }

        if (newPass.length < 6) {
            self.errorMessage('Yeni şifre en az 6 karakter olmalıdır.');
            return;
        }

        if (newPass !== confirmPass) {
            self.errorMessage('Yeni şifre ve onay eşleşmiyor.');
            return;
        }

        var personnelId = self.selectedPersonnelForPassword().id;

        self.isResettingPassword(true);

        customerApiService.resetPersonnelPassword(personnelId, newPass)
            .then(function(response) {
                self.successMessage(response.message || 'Şifre başarıyla sıfırlandı.');
                self.showChangePasswordModal(false);
                self.newPassword('');
                self.confirmPassword('');
            })
            .catch(function(error) {
                console.error('Error resetting password:', error);
                self.errorMessage('Şifre sıfırlanırken bir hata oluştu: ' + (error.message || ''));
            })
            .finally(function() {
                self.isResettingPassword(false);
            });
    };

    // Cancel reset password
    self.cancelChangePassword = function() {
        self.showChangePasswordModal(false);
        self.newPassword('');
        self.confirmPassword('');
    };

    // Toggle inactive personnel
    self.toggleShowInactive = function() {
        self.showInactive(!self.showInactive());
        self.loadPersonnel();
    };

    // Initialize
    self.loadCustomer();
    self.loadCustomers();
    self.loadPersonnel();
}

// Apply bindings when DOM is ready
if (typeof ko !== 'undefined') {
    var customerPersonnelApp = document.getElementById('customer-personnel-app');
    if (customerPersonnelApp) {
        var customerId = customerPersonnelApp.getAttribute('data-customer-id') || null;
        ko.applyBindings(new CustomerPersonnelViewModel(customerId), customerPersonnelApp);
    }
}
