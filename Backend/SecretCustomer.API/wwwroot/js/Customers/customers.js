// Customers ViewModel
function CustomersViewModel() {
    var self = this;

    // Observables
    self.customers = ko.observableArray([]);
    self.isLoading = ko.observable(false);
    self.isSaving = ko.observable(false);
    self.errorMessage = ko.observable('');
    self.successMessage = ko.observable('');
    self.showInactive = ko.observable(false);

    // Modal
    self.isModalOpen = ko.observable(false);
    self.editingCustomer = ko.observable(null);

    // Personnel Management
    self.showPersonnelModal = ko.observable(false);
    self.selectedCustomerForPersonnel = ko.observable(null);
    self.personnel = ko.observableArray([]);
    self.isLoadingPersonnel = ko.observable(false);
    self.personnelSearchText = ko.observable('');
    
    // Personnel Form
    self.showPersonnelFormModal = ko.observable(false);
    self.editingPersonnel = ko.observable(null);
    self.isSavingPersonnel = ko.observable(false);
    
    // Reset Password (Admin)
    self.showChangePasswordModal = ko.observable(false);
    self.selectedPersonnelForPassword = ko.observable(null);
    self.isResettingPassword = ko.observable(false);
    self.newPassword = ko.observable('');
    self.confirmPassword = ko.observable('');

    // Computed
    self.filteredCustomers = ko.computed(function() {
        if (self.showInactive()) {
            return self.customers();
        }
        return self.customers().filter(function(c) { return c.isActive; });
    });

    self.filteredPersonnel = ko.computed(function() {
        var search = self.personnelSearchText().toLowerCase();
        if (!search) return self.personnel();
        
        return self.personnel().filter(function(p) {
            return (p.fullName && p.fullName.toLowerCase().indexOf(search) >= 0) ||
                   (p.username && p.username.toLowerCase().indexOf(search) >= 0) ||
                   (p.email && p.email.toLowerCase().indexOf(search) >= 0);
        });
    });

    // Utility functions
    self.formatDate = function(dateString) {
        if (!dateString) return '-';
        var date = new Date(dateString);
        return date.toLocaleDateString('tr-TR');
    };

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

    // Create new customer
    self.createNew = function() {
        self.editingCustomer({
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
        self.isModalOpen(true);
    };

    // Edit customer
    self.editCustomer = function(customer) {
        self.editingCustomer({
            id: customer.id,
            companyName: customer.companyName,
            taxNumber: customer.taxNumber || '',
            phone: customer.phone || '',
            email: customer.email || '',
            address: customer.address || '',
            city: customer.city || '',
            isActive: customer.isActive,
            contractStartDate: customer.contractStartDate,
            contractEndDate: customer.contractEndDate,
            notes: customer.notes || ''
        });
        self.isModalOpen(true);
    };

    // Save customer
    self.saveCustomer = function() {
        self.errorMessage('');
        self.successMessage('');

        var customer = self.editingCustomer();
        if (!customer) return;

        // Validation
        if (!customer.companyName) {
            self.errorMessage('Şirket adı zorunludur.');
            return;
        }

        self.isSaving(true);

        var promise = customer.id 
            ? customerApiService.updateCustomer(customer.id, customer)
            : customerApiService.createCustomer(customer);

        promise
            .then(function() {
                self.successMessage(customer.id ? 'Müşteri başarıyla güncellendi.' : 'Müşteri başarıyla oluşturuldu.');
                self.isModalOpen(false);
                self.loadCustomers();
            })
            .catch(function(error) {
                console.error('Error saving customer:', error);
                self.errorMessage('Müşteri kaydedilirken bir hata oluştu: ' + (error.message || ''));
            })
            .finally(function() {
                self.isSaving(false);
            });
    };

    // Close modal
    self.closeModal = function() {
        self.isModalOpen(false);
        self.editingCustomer(null);
        self.errorMessage('');
    };

    // Delete customer
    self.deleteCustomer = function(customer) {
        deleteConfirmation.show(
            '"' + customer.companyName + '" müşterisini silmek istediğinizden emin misiniz? Bu işlem geri alınamaz.',
            function() {
                customerApiService.deleteCustomer(customer.id)
                    .then(function() {
                        self.successMessage('Müşteri başarıyla silindi.');
                        self.loadCustomers();
                    })
                    .catch(function(error) {
                        console.error('Error deleting customer:', error);
                        self.errorMessage('Müşteri silinirken bir hata oluştu: ' + (error.message || ''));
                    });
            }
        );
    };

    // ========== PERSONNEL MANAGEMENT ==========
    
    // Show personnel modal
    self.showPersonnel = function(customer) {
        self.selectedCustomerForPersonnel(customer);
        self.personnelSearchText('');
        self.showPersonnelModal(true);
        self.loadPersonnel(customer.id);
    };

    // Load personnel for customer
    self.loadPersonnel = function(customerId) {
        self.isLoadingPersonnel(true);
        self.errorMessage('');

        customerApiService.getPersonnelByCustomerId(customerId, false)
            .then(function(data) {
                self.personnel(data || []);
            })
            .catch(function(error) {
                console.error('Error loading personnel:', error);
                self.errorMessage('Personeller yüklenirken bir hata oluştu: ' + (error.message || ''));
            })
            .finally(function() {
                self.isLoadingPersonnel(false);
            });
    };

    // Close personnel modal
    self.closePersonnelModal = function() {
        self.showPersonnelModal(false);
        self.selectedCustomerForPersonnel(null);
        self.personnel([]);
        self.personnelSearchText('');
    };

    // Create new personnel
    self.createNewPersonnel = function() {
        var customer = self.selectedCustomerForPersonnel();
        if (!customer) return;

        self.editingPersonnel({
            id: null,
            customerId: customer.id,
            username: '',
            email: '',
            password: '',
            firstName: '',
            lastName: '',
            phoneNumber: '',
            department: '',
            title: '',
            role: 1,
            isActive: true
        });
        self.showPersonnelFormModal(true);
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
            isActive: personnel.isActive
        });
        self.showPersonnelFormModal(true);
    };

    // Save personnel
    self.savePersonnel = function() {
        self.errorMessage('');
        self.successMessage('');

        var personnel = self.editingPersonnel();
        if (!personnel) return;

        // Validation
        if (!personnel.username || !personnel.email || !personnel.firstName || !personnel.lastName) {
            self.errorMessage('Kullanıcı adı, e-posta, ad ve soyad zorunludur.');
            return;
        }

        if (!personnel.id && !personnel.password) {
            self.errorMessage('Yeni personel için şifre zorunludur.');
            return;
        }

        self.isSavingPersonnel(true);

        var promise = personnel.id 
            ? customerApiService.updatePersonnel(personnel.id, personnel)
            : customerApiService.createPersonnel(personnel);

        promise
            .then(function() {
                self.successMessage(personnel.id ? 'Personel başarıyla güncellendi.' : 'Personel başarıyla oluşturuldu.');
                self.showPersonnelFormModal(false);
                self.loadPersonnel(self.selectedCustomerForPersonnel().id);
                self.loadCustomers(); // Refresh personnel count
            })
            .catch(function(error) {
                console.error('Error saving personnel:', error);
                self.errorMessage('Personel kaydedilirken bir hata oluştu: ' + (error.message || ''));
            })
            .finally(function() {
                self.isSavingPersonnel(false);
            });
    };

    // Close personnel form modal
    self.closePersonnelFormModal = function() {
        self.showPersonnelFormModal(false);
        self.editingPersonnel(null);
    };

    // Delete personnel
    self.deletePersonnel = function(personnel) {
        deleteConfirmation.show(
            '"' + personnel.fullName + '" personelini silmek istediğinizden emin misiniz? Bu işlem geri alınamaz.',
            function() {
                customerApiService.deletePersonnel(personnel.id)
                    .then(function() {
                        self.successMessage('Personel başarıyla silindi.');
                        self.loadPersonnel(self.selectedCustomerForPersonnel().id);
                        self.loadCustomers(); // Refresh personnel count
                    })
                    .catch(function(error) {
                        console.error('Error deleting personnel:', error);
                        self.errorMessage('Personel silinirken bir hata oluştu: ' + (error.message || ''));
                    });
            }
        );
    };

    // Show reset password modal (Admin)
    self.showChangePasswordForPersonnel = function(personnel) {
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
        self.selectedPersonnelForPassword(null);
        self.newPassword('');
        self.confirmPassword('');
    };

    // Toggle inactive customers
    self.toggleShowInactive = function() {
        self.showInactive(!self.showInactive());
        self.loadCustomers();
    };

    // Initialize
    self.loadCustomers();
}

// Apply bindings when DOM is ready
if (typeof ko !== 'undefined') {
    ko.applyBindings(new CustomersViewModel(), document.getElementById('customers-app'));
}
