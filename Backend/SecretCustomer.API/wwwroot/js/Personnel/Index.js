// Personnel ViewModel - Bizim Personelimiz (Users)
function PersonnelViewModel() {
    var self = this;

    // State
    self.isLoading = ko.observable(false);
    self.isSaving = ko.observable(false);
    self.errorMessage = ko.observable('');
    self.successMessage = ko.observable('');
    self.modalErrorMessage = ko.observable('');

    // Data
    self.personnel = ko.observableArray([]);

    // Filter
    self.filter = {
        searchTerm: ko.observable(''),
        role: ko.observable(''),
        isActive: ko.observable('')
    };

    // Filtered personnel
    self.filteredPersonnel = ko.computed(function() {
        var list = self.personnel();
        var search = (self.filter.searchTerm() || '').toLowerCase();
        var role = self.filter.role();
        var isActive = self.filter.isActive();

        return list.filter(function(p) {
            // Search filter
            if (search) {
                var match = (p.firstName || '').toLowerCase().indexOf(search) > -1 ||
                           (p.lastName || '').toLowerCase().indexOf(search) > -1 ||
                           (p.username || '').toLowerCase().indexOf(search) > -1 ||
                           (p.email || '').toLowerCase().indexOf(search) > -1;
                if (!match) return false;
            }

            // Role filter
            if (role !== '' && p.role !== parseInt(role)) {
                return false;
            }

            // Status filter
            if (isActive === 'true' && !p.isActive) return false;
            if (isActive === 'false' && p.isActive) return false;

            return true;
        });
    });

    // Editing personnel
    self.editingPersonnel = ko.observable(null);
    self.modal = null;

    // Password change
    self.passwordUserId = ko.observable(null);
    self.newPassword = ko.observable('');
    self.confirmPassword = ko.observable('');
    self.passwordError = ko.observable('');
    self.isSavingPassword = ko.observable(false);
    self.passwordModal = null;

    // Role helpers
    self.getRoleName = function(role) {
        var roles = {
            0: 'Admin',
            1: 'Manager',
            2: 'Kalite Uzmanı',
            3: 'Saha Çalışanı'
        };
        return roles[role] || role;
    };

    self.getRoleBadgeClass = function(role) {
        var classes = {
            0: 'bg-danger',      // Admin - kırmızı
            1: 'bg-primary',     // Manager - mavi
            2: 'bg-info',        // QualitySpecialist - açık mavi
            3: 'bg-secondary'    // FieldWorker - gri
        };
        return classes[role] || 'bg-secondary';
    };

    // Load personnel
    self.loadPersonnel = function() {
        self.isLoading(true);
        self.errorMessage('');

        ApiService.get('/users')
            .then(function(data) {
                self.personnel(data || []);
            })
            .catch(function(error) {
                console.error('Error:', error);
                toastr.error(error.message || 'Personel listesi yüklenirken bir hata oluştu.');
            })
            .finally(function() {
                self.isLoading(false);
            });
    };

    // Apply filters
    self.applyFilters = function() {
        // Filters are applied via computed, just trigger update
        self.filter.searchTerm.valueHasMutated();
    };

    // Clear filters
    self.clearFilters = function() {
        self.filter.searchTerm('');
        self.filter.role('');
        self.filter.isActive('');
    };

    // Create new
    self.createNew = function() {        self.editingPersonnel({
            id: null,
            username: ko.observable(''),
            firstName: ko.observable(''),
            lastName: ko.observable(''),
            email: ko.observable(''),
            phoneNumber: ko.observable(''),
            role: ko.observable(3), // Default: FieldWorker
            password: ko.observable(''),
            isActive: ko.observable(true)
        });
        self.showModal();
    };

    // Edit personnel
    self.editPersonnel = function(person) {        self.editingPersonnel({
            id: person.id,
            username: ko.observable(person.username),
            firstName: ko.observable(person.firstName),
            lastName: ko.observable(person.lastName),
            email: ko.observable(person.email),
            phoneNumber: ko.observable(person.phoneNumber || ''),
            role: ko.observable(person.role),
            password: ko.observable(''),
            isActive: ko.observable(person.isActive)
        });
        self.showModal();
    };

    // Save personnel
    self.savePersonnel = function() {
        var editing = self.editingPersonnel();
        if (!editing) return;

        // Validation
        if (!editing.firstName() || !editing.lastName()) {
            toastr.error('Ad ve Soyad alanları zorunludur.');
            return;
        }
        if (!editing.email()) {
            toastr.error('E-posta alanı zorunludur.');
            return;
        }
        if (!editing.id && !editing.username()) {
            toastr.error('Kullanıcı adı zorunludur.');
            return;
        }
        if (!editing.id && !editing.password()) {
            toastr.error('Şifre zorunludur.');
            return;
        }
        if (!editing.id && editing.password().length < 6) {
            toastr.error('Şifre en az 6 karakter olmalıdır.');
            return;
        }

        self.isSaving(true);        var promise;
        if (editing.id) {
            // Update
            var updateDto = {
                email: editing.email(),
                firstName: editing.firstName(),
                lastName: editing.lastName(),
                phoneNumber: editing.phoneNumber() || null,
                role: parseInt(editing.role()),
                isActive: editing.isActive()
            };
            promise = ApiService.put('/users/' + editing.id, updateDto);
        } else {
            // Create
            var createDto = {
                username: editing.username(),
                email: editing.email(),
                password: editing.password(),
                firstName: editing.firstName(),
                lastName: editing.lastName(),
                role: parseInt(editing.role()),
                isActive: editing.isActive()
            };
            promise = ApiService.post('/users', createDto);
        }

        promise
            .then(function(data) {
                toastr.success(editing.id ? 'Personel başarıyla güncellendi.' : 'Personel başarıyla oluşturuldu.');
                self.hideModal();
                self.loadPersonnel();
            })
            .catch(function(error) {
                console.error('Error:', error);
                toastr.error(error.message || 'Personel kaydedilirken bir hata oluştu.');
            })
            .finally(function() {
                self.isSaving(false);
            });
    };

    // Delete personnel
    self.deletePersonnel = function(person) {
        showDeleteConfirm('"' + person.fullName + '" personeli', function() {
            ApiService.delete('/users/' + person.id)
                .then(function() {
                    toastr.success('Personel başarıyla silindi.');
                    self.loadPersonnel();
                })
                .catch(function(error) {
                    console.error('Error:', error);
                    toastr.error(error.message || 'Personel silinirken bir hata oluştu.');
                });
        });
    };

    // Change password
    self.changePassword = function(person) {
        self.passwordUserId(person.id);
        self.newPassword('');
        self.confirmPassword('');
        self.passwordError('');
        self.showPasswordModal();
    };

    self.savePassword = function() {
        var userId = self.passwordUserId();
        var newPass = self.newPassword();
        var confirmPass = self.confirmPassword();

        if (!newPass || newPass.length < 6) {
            toastr.error('Şifre en az 6 karakter olmalıdır.');
            return;
        }
        if (newPass !== confirmPass) {
            toastr.error('Şifreler eşleşmiyor.');
            return;
        }

        self.isSavingPassword(true);
        self.passwordError('');

        ApiService.post('/users/' + userId + '/change-password', {
            userId: userId,
            newPassword: newPass
        })
            .then(function() {
                toastr.success('Şifre başarıyla değiştirildi.');
                self.hidePasswordModal();
            })
            .catch(function(error) {
                console.error('Error:', error);
                toastr.error(error.message || 'Şifre değiştirilemedi.');
            })
            .finally(function() {
                self.isSavingPassword(false);
            });
    };

    // Modal helpers
    self.showModal = function() {
        if (!self.modal) {
            self.modal = new bootstrap.Modal(document.getElementById('personnelModal'));
        }
        self.modal.show();
    };

    self.hideModal = function() {
        if (self.modal) {
            self.modal.hide();
        }
    };

    self.showPasswordModal = function() {
        if (!self.passwordModal) {
            self.passwordModal = new bootstrap.Modal(document.getElementById('passwordModal'));
        }
        self.passwordModal.show();
    };

    self.hidePasswordModal = function() {
        if (self.passwordModal) {
            self.passwordModal.hide();
        }
    };

    // Initialize
    self.loadPersonnel();

    // Auto-search on typing (with debounce)
    var searchTimeout;
    self.filter.searchTerm.subscribe(function(value) {
        clearTimeout(searchTimeout);
        searchTimeout = setTimeout(function() {
            // Computed handles filtering automatically
        }, 300);
    });
}

// Apply bindings when document is ready
$(document).ready(function() {
    ko.applyBindings(new PersonnelViewModel(), document.getElementById('personnel-app'));
});
