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
    self.modalErrorMessage = ko.observable('');
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
        { value: 1, text: T('Role.CustomerManager', 'Müşteri Yöneticisi') },
        { value: 2, text: T('Role.CustomerSupervisor', 'Müşteri Süpervizörü') },
        { value: 3, text: T('Role.CustomerOperator', 'Müşteri Operatörü') }
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
        return roleObj ? roleObj.text : T('Common.Unknown', 'Bilinmeyen');
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
                toastr.error(T('Customer.LoadError', 'Müşteri bilgileri yüklenirken bir hata oluştu.'));
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
                toastr.error(T('Personnel.LoadError', 'Personeller yüklenirken bir hata oluştu:') + ' ' + (error.message || ''));
            })
            .finally(function() {
                self.isLoading(false);
            });
    };

    // Create new personnel
    self.createNew = function() {
        self.editingPersonnel({
            id: null,
            customerId: customerId || null,
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
        self.modalErrorMessage('');
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
        self.modalErrorMessage('');
        self.isModalOpen(true);
    };

    // Save personnel
    self.savePersonnel = function() {
        self.modalErrorMessage('');
        self.successMessage('');

        var personnel = self.editingPersonnel();
        if (!personnel) return;

        // Validation
        if (!personnel.customerId) {
            toastr.error(T('Validation.CustomerRequired', 'Müşteri seçimi zorunludur.'));
            return;
        }

        if (!personnel.username || !personnel.email || !personnel.firstName || !personnel.lastName) {
            toastr.error(T('Personnel.RequiredFields', 'Kullanıcı adı, e-posta, ad ve soyad zorunludur.'));
            return;
        }

        // Username format validation (no spaces, no Turkish characters)
        var usernameRegex = /^[a-zA-Z0-9_.-]+$/;
        if (!usernameRegex.test(personnel.username)) {
            toastr.error(T('User.UsernameInvalid', 'Kullanıcı adı sadece İngilizce harf, rakam, alt çizgi, nokta ve tire içerebilir. Boşluk ve Türkçe karakter kullanılamaz.'));
            return;
        }

        if (!personnel.id && !personnel.password) {
            toastr.error(T('Personnel.PasswordRequired', 'Yeni personel için şifre zorunludur.'));
            return;
        }

        if (!personnel.role) {
            toastr.error(T('Personnel.RoleRequired', 'Rol seçimi zorunludur.'));
            return;
        }

        self.isSaving(true);
        self.modalErrorMessage('');

        // Check username/email uniqueness before saving
        var excludeIdParam = personnel.id ? '?excludeId=' + personnel.id : '';

        // Check username
        fetch('/api/customer-personnel/check-username/' + encodeURIComponent(personnel.username) + excludeIdParam, { credentials: 'include' })
            .then(function(response) { return response.json(); })
            .then(function(data) {
                if (data.exists) {
                    self.isSaving(false);
                    toastr.error(T('User.UsernameExists', 'Bu kullanıcı adı zaten kullanılıyor.'));
                    return Promise.reject('username_exists');
                }
                // Check email
                return fetch('/api/customer-personnel/check-email/' + encodeURIComponent(personnel.email) + excludeIdParam, { credentials: 'include' });
            })
            .then(function(response) { return response.json(); })
            .then(function(data) {
                if (data.exists) {
                    self.isSaving(false);
                    toastr.error(T('User.EmailExists', 'Bu e-posta adresi zaten kullanılıyor.'));
                    return Promise.reject('email_exists');
                }
                // All checks passed, proceed with save
                self.doSavePersonnel(personnel);
            })
            .catch(function(error) {
                if (error !== 'username_exists' && error !== 'email_exists') {
                    console.error('Error checking uniqueness:', error);
                    self.isSaving(false);
                }
            });
    };

    // Actual save operation
    self.doSavePersonnel = function(personnel) {
        // Backend'e gönderilecek veriyi hazırla
        var dataToSend = {
            customerId: personnel.customerId,
            username: personnel.username,
            email: personnel.email,
            firstName: personnel.firstName,
            lastName: personnel.lastName,
            phoneNumber: personnel.phoneNumber || null,
            department: personnel.department || null,
            title: personnel.title || null,
            role: parseInt(personnel.role, 10),
            isActive: personnel.isActive,
            notes: personnel.notes || null
        };

        // Yeni personel için şifre ekle
        if (!personnel.id) {
            dataToSend.password = personnel.password;
        }

        console.log('Sending personnel data:', JSON.stringify(dataToSend, null, 2));

        var promise = personnel.id
            ? customerApiService.updatePersonnel(personnel.id, dataToSend)
            : customerApiService.createPersonnel(dataToSend);

        promise
            .then(function() {
                toastr.success(personnel.id ? T('Personnel.UpdateSuccess', 'Personel başarıyla güncellendi.') : T('Personnel.SaveSuccess', 'Personel başarıyla oluşturuldu.'));
                self.isModalOpen(false);
                self.loadPersonnel();
            })
            .catch(function(error) {
                console.error('Error saving personnel:', error);
                toastr.error(T('Personnel.SaveError', 'Personel kaydedilirken bir hata oluştu:') + ' ' + (error.message || ''));
            })
            .finally(function() {
                self.isSaving(false);
            });
    };

    // Close modal
    self.closeModal = function() {
        self.isModalOpen(false);
        self.editingPersonnel(null);
        self.modalErrorMessage('');
    };

    // Delete personnel
    self.deletePersonnel = function(personnel) {
        deleteConfirmation.show(T('Personnel.DeleteConfirm', 'Bu personeli silmek istediğinizden emin misiniz?') + '\n\n' + personnel.fullName, function() {
            customerApiService.deletePersonnel(personnel.id)
                .then(function() {
                    toastr.success(T('Personnel.DeleteSuccess', 'Personel başarıyla silindi.'));
                    self.loadPersonnel();
                })
                .catch(function(error) {
                    console.error('Error deleting personnel:', error);
                    toastr.error(T('Personnel.DeleteError', 'Personel silinirken bir hata oluştu:') + ' ' + (error.message || ''));
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
            toastr.error(T('Validation.AllFieldsRequired', 'Tüm alanlar zorunludur.'));
            return;
        }

        if (newPass.length < 6) {
            toastr.error(T('Validation.PasswordMinLength', 'Yeni şifre en az 6 karakter olmalıdır.'));
            return;
        }

        if (newPass !== confirmPass) {
            toastr.error(T('Validation.PasswordMismatch', 'Yeni şifre ve onay eşleşmiyor.'));
            return;
        }

        var personnelId = self.selectedPersonnelForPassword().id;

        self.isResettingPassword(true);

        customerApiService.resetPersonnelPassword(personnelId, newPass)
            .then(function(response) {
                toastr.success(response.message || T('Password.ResetSuccess', 'Şifre başarıyla sıfırlandı.'));
                self.showChangePasswordModal(false);
                self.newPassword('');
                self.confirmPassword('');
            })
            .catch(function(error) {
                console.error('Error resetting password:', error);
                toastr.error(T('Password.ResetError', 'Şifre sıfırlanırken bir hata oluştu:') + ' ' + (error.message || ''));
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
