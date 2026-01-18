// Personnel ViewModel - Müşteri Personeli Popup
function PersonnelViewModel() {
    var self = this;

    // Config
    self.customerId = window.personnelConfig ? window.personnelConfig.customerId : null;

    // State
    self.isLoading = ko.observable(false);
    self.isSaving = ko.observable(false);
    self.allPersonnel = ko.observableArray([]);
    self.searchText = ko.observable('');

    // ========== CHIP-BASED FILTER SYSTEM ==========
    self.selectedFilterType = ko.observable('');
    self.activeFilters = ko.observableArray([]);

    // Temp filter values
    self.tempFilter = {
        fullName: ko.observable(''),
        username: ko.observable(''),
        email: ko.observable(''),
        role: ko.observable(null),
        isActive: ko.observable(null)
    };

    // Filter labels
    self.filterLabels = {
        fullName: 'Ad Soyad',
        username: 'Kullanıcı Adı',
        email: 'E-posta',
        role: 'Rol',
        isActive: 'Durum'
    };

    self.statusLabels = {
        'true': 'Aktif',
        'false': 'Pasif'
    };

    self.roles = [
        { id: '1', name: 'Yönetici' },
        { id: '2', name: 'Süpervizör' },
        { id: '3', name: 'Operatör' }
    ];

    // Can add filter
    self.canAddFilter = ko.computed(function() {
        var type = self.selectedFilterType();
        if (!type) return false;

        switch (type) {
            case 'fullName': return self.tempFilter.fullName().trim() !== '';
            case 'username': return self.tempFilter.username().trim() !== '';
            case 'email': return self.tempFilter.email().trim() !== '';
            case 'role': return self.tempFilter.role() !== null;
            case 'isActive': return self.tempFilter.isActive() !== null;
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
            case 'fullName':
                var fullName = self.tempFilter.fullName().trim();
                if (!fullName) return;
                filter.value = fullName;
                filter.displayValue = fullName;
                self.tempFilter.fullName('');
                break;

            case 'username':
                var username = self.tempFilter.username().trim();
                if (!username) return;
                filter.value = username;
                filter.displayValue = username;
                self.tempFilter.username('');
                break;

            case 'email':
                var email = self.tempFilter.email().trim();
                if (!email) return;
                filter.value = email;
                filter.displayValue = email;
                self.tempFilter.email('');
                break;

            case 'role':
                var roleId = self.tempFilter.role();
                if (!roleId) return;
                var role = self.roles.find(function(r) { return r.id === roleId; });
                filter.value = roleId;
                filter.displayValue = role ? role.name : roleId;
                self.tempFilter.role(null);
                break;

            case 'isActive':
                var isActive = self.tempFilter.isActive();
                if (isActive === null) return;
                filter.value = isActive;
                filter.displayValue = self.statusLabels[String(isActive)];
                self.tempFilter.isActive(null);
                break;

            default:
                return;
        }

        self.activeFilters.push(filter);
        self.selectedFilterType('');
        self.currentPage(1);
    };

    // Remove filter
    self.removeFilter = function(filter) {
        self.activeFilters.remove(filter);
        self.currentPage(1);
    };

    // Clear all filters
    self.clearFilters = function() {
        self.activeFilters([]);
        self.searchText('');
        self.currentPage(1);
    };

    // Pagination
    self.currentPage = ko.observable(1);
    self.pageSize = ko.observable(20);

    // Modals
    self.editingPersonnel = ko.observable(null);
    self.personnelModal = null;
    self.resetPasswordModal = null;

    // Reset password
    self.selectedPersonnelId = ko.observable(null);
    self.selectedPersonnelName = ko.observable('');
    self.newPassword = ko.observable('');
    self.confirmPassword = ko.observable('');
    self.isResettingPassword = ko.observable(false);

    // Filtered personnel
    self.filteredPersonnel = ko.computed(function() {
        var list = self.allPersonnel();
        var search = (self.searchText() || '').toLowerCase();
        var filters = self.activeFilters();

        // Global search filter
        if (search) {
            list = list.filter(function(p) {
                return (p.firstName || '').toLowerCase().indexOf(search) >= 0 ||
                       (p.lastName || '').toLowerCase().indexOf(search) >= 0 ||
                       (p.fullName || '').toLowerCase().indexOf(search) >= 0 ||
                       (p.username || '').toLowerCase().indexOf(search) >= 0 ||
                       (p.email || '').toLowerCase().indexOf(search) >= 0;
            });
        }

        // Apply chip-based filters
        if (filters.length > 0) {
            var filtersByType = {};
            filters.forEach(function(f) {
                if (!filtersByType[f.type]) filtersByType[f.type] = [];
                filtersByType[f.type].push(f);
            });

            Object.keys(filtersByType).forEach(function(type) {
                var typeFilters = filtersByType[type];
                list = list.filter(function(p) {
                    return typeFilters.some(function(f) {
                        switch (f.type) {
                            case 'fullName':
                                var fullName = (p.fullName || (p.firstName + ' ' + p.lastName) || '').toLowerCase();
                                return fullName.indexOf(f.value.toLowerCase()) >= 0;
                            case 'username':
                                return (p.username || '').toLowerCase().indexOf(f.value.toLowerCase()) >= 0;
                            case 'email':
                                return (p.email || '').toLowerCase().indexOf(f.value.toLowerCase()) >= 0;
                            case 'role':
                                return String(p.role) === f.value;
                            case 'isActive':
                                return p.isActive === f.value;
                            default:
                                return true;
                        }
                    });
                });
            });
        }

        return list;
    });

    // Paginated personnel
    self.paginatedPersonnel = ko.computed(function() {
        var list = self.filteredPersonnel();
        var page = parseInt(self.currentPage(), 10);
        var pageSize = parseInt(self.pageSize(), 10);
        var start = (page - 1) * pageSize;
        return list.slice(start, start + pageSize);
    });

    self.totalCount = ko.computed(function() {
        return self.filteredPersonnel().length;
    });

    self.totalPages = ko.computed(function() {
        return Math.ceil(self.totalCount() / parseInt(self.pageSize(), 10)) || 1;
    });

    // Reset page when filters change
    self.searchText.subscribe(function() { self.currentPage(1); });
    self.pageSize.subscribe(function() { self.currentPage(1); });

    self.goToPage = function(page) {
        if (page >= 1 && page <= self.totalPages()) {
            self.currentPage(page);
        }
    };

    self.previousPage = function() {
        if (self.currentPage() > 1) self.currentPage(self.currentPage() - 1);
    };

    self.nextPage = function() {
        if (self.currentPage() < self.totalPages()) self.currentPage(self.currentPage() + 1);
    };

    // Role helpers
    self.getRoleName = function(role) {
        var roles = { '1': 'Yönetici', '2': 'Süpervizör', '3': 'Operatör' };
        return roles[String(role)] || role;
    };

    self.getRoleBadgeClass = function(role) {
        var classes = { '1': 'bg-danger', '2': 'bg-warning text-dark', '3': 'bg-info' };
        return classes[String(role)] || 'bg-secondary';
    };

    // Load personnel
    self.loadPersonnel = function() {
        if (!self.customerId) {
            toastr.error('Müşteri ID bulunamadı.');
            return;
        }

        self.isLoading(true);

        ApiService.get('/customer-personnel/by-customer/' + self.customerId + '?includeInactive=true')
            .then(function(data) {
                self.allPersonnel(data || []);
                self.currentPage(1);
            })
            .catch(function(error) {
                console.error('Error loading personnel:', error);
                toastr.error('Personeller yüklenirken bir hata oluştu.');
            })
            .finally(function() {
                self.isLoading(false);
            });
    };

    // Create new personnel
    self.createNew = function() {
        self.editingPersonnel({
            id: ko.observable(null),
            customerId: ko.observable(self.customerId),
            firstName: ko.observable(''),
            lastName: ko.observable(''),
            username: ko.observable(''),
            email: ko.observable(''),
            password: ko.observable(''),
            phoneNumber: ko.observable(''),
            department: ko.observable(''),
            title: ko.observable(''),
            role: ko.observable('3'),
            isActive: ko.observable(true)
        });
        self.showModal();
    };

    // Edit personnel
    self.editPersonnel = function(personnel) {
        self.editingPersonnel({
            id: ko.observable(personnel.id),
            customerId: ko.observable(personnel.customerId),
            firstName: ko.observable(personnel.firstName),
            lastName: ko.observable(personnel.lastName),
            username: ko.observable(personnel.username),
            email: ko.observable(personnel.email),
            password: ko.observable(''),
            phoneNumber: ko.observable(personnel.phoneNumber || ''),
            department: ko.observable(personnel.department || ''),
            title: ko.observable(personnel.title || ''),
            role: ko.observable(String(personnel.role)),
            isActive: ko.observable(personnel.isActive)
        });
        self.showModal();
    };

    // Save personnel
    self.savePersonnel = function() {
        var p = self.editingPersonnel();
        if (!p) return;

        var id = ko.unwrap(p.id);
        var firstName = ko.unwrap(p.firstName);
        var lastName = ko.unwrap(p.lastName);
        var username = ko.unwrap(p.username);
        var email = ko.unwrap(p.email);
        var password = ko.unwrap(p.password);

        // Validation
        if (!firstName || !lastName || !username || !email) {
            toastr.warning('Ad, soyad, kullanıcı adı ve e-posta zorunludur.');
            return;
        }

        if (!id && !password) {
            toastr.warning('Yeni personel için şifre zorunludur.');
            return;
        }

        // Username validation
        var usernameRegex = /^[a-zA-Z0-9_.-]+$/;
        if (!usernameRegex.test(username)) {
            toastr.error('Kullanıcı adı sadece İngilizce harf, rakam, alt çizgi, nokta ve tire içerebilir.');
            return;
        }

        self.isSaving(true);

        var data = {
            customerId: ko.unwrap(p.customerId),
            firstName: firstName,
            lastName: lastName,
            username: username,
            email: email,
            password: password || null,
            phoneNumber: ko.unwrap(p.phoneNumber) || null,
            department: ko.unwrap(p.department) || null,
            title: ko.unwrap(p.title) || null,
            role: String(ko.unwrap(p.role)),
            isActive: ko.unwrap(p.isActive)
        };

        var promise = id
            ? ApiService.put('/customer-personnel/' + id, data)
            : ApiService.post('/customer-personnel', data);

        promise
            .then(function(savedPersonnel) {
                var isNew = !id;
                if (isNew) {
                    self.allPersonnel.push(savedPersonnel);
                } else {
                    var list = self.allPersonnel();
                    for (var i = 0; i < list.length; i++) {
                        if (list[i].id === savedPersonnel.id) {
                            self.allPersonnel.splice(i, 1, savedPersonnel);
                            break;
                        }
                    }
                }
                toastr.success(isNew ? 'Personel oluşturuldu.' : 'Personel güncellendi.');
                self.hideModal();
                self.notifyParent();
            })
            .catch(function(error) {
                console.error('Error saving personnel:', error);
                toastr.error('Personel kaydedilirken bir hata oluştu.');
            })
            .finally(function() {
                self.isSaving(false);
            });
    };

    // Delete personnel
    self.deletePersonnel = function(personnel) {
        deleteConfirmation.show(
            '<strong>' + (personnel.fullName || personnel.firstName + ' ' + personnel.lastName) + '</strong> personelini silmek istediğinize emin misiniz?',
            function() {
                ApiService.delete('/customer-personnel/' + personnel.id)
                    .then(function() {
                        self.allPersonnel.remove(personnel);
                        toastr.success('Personel silindi.');
                        self.notifyParent();
                    })
                    .catch(function(error) {
                        console.error('Error deleting personnel:', error);
                        toastr.error('Personel silinirken bir hata oluştu.');
                    });
            }
        );
    };

    // Show reset password modal
    self.showResetPassword = function(personnel) {
        self.selectedPersonnelId(personnel.id);
        self.selectedPersonnelName(personnel.fullName || personnel.firstName + ' ' + personnel.lastName);
        self.newPassword('');
        self.confirmPassword('');

        if (!self.resetPasswordModal) {
            var el = document.getElementById('resetPasswordModal');
            if (el) self.resetPasswordModal = new bootstrap.Modal(el);
        }
        if (self.resetPasswordModal) self.resetPasswordModal.show();
    };

    // Reset password
    self.resetPassword = function() {
        var newPass = self.newPassword();
        var confirmPass = self.confirmPassword();

        if (!newPass || !confirmPass) {
            toastr.warning('Tüm alanları doldurun.');
            return;
        }

        if (newPass.length < 6) {
            toastr.warning('Şifre en az 6 karakter olmalıdır.');
            return;
        }

        if (newPass !== confirmPass) {
            toastr.warning('Şifreler eşleşmiyor.');
            return;
        }

        self.isResettingPassword(true);

        ApiService.post('/customer-personnel/' + self.selectedPersonnelId() + '/reset-password', { newPassword: newPass })
            .then(function() {
                toastr.success('Şifre sıfırlandı.');
                if (self.resetPasswordModal) self.resetPasswordModal.hide();
            })
            .catch(function(error) {
                console.error('Error resetting password:', error);
                toastr.error('Şifre sıfırlanırken bir hata oluştu.');
            })
            .finally(function() {
                self.isResettingPassword(false);
            });
    };

    // Export to Excel
    self.exportToExcel = function() {
        window.open('/api/customers/' + self.customerId + '/personnel/export/excel', '_blank');
    };

    // Modal helpers
    self.showModal = function() {
        if (!self.personnelModal) {
            var el = document.getElementById('personnelModal');
            if (el) self.personnelModal = new bootstrap.Modal(el);
        }
        if (self.personnelModal) self.personnelModal.show();
    };

    self.hideModal = function() {
        if (self.personnelModal) self.personnelModal.hide();
        self.editingPersonnel(null);
    };

    // Notify parent window (opener) to refresh
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
        self.loadPersonnel();
    };

    self.init();
}

// Apply bindings
$(document).ready(function() {
    var app = document.getElementById('personnel-app');
    if (app) {
        ko.applyBindings(new PersonnelViewModel(), app);
    }
});
