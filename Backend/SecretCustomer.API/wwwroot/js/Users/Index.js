// ===== User Edit ViewModel =====
function UserEditViewModel(data) {
    var self = this;
    data = data || {};

    self.id = data.id || null;
    self.username = ko.observable(data.username || '');
    self.firstName = ko.observable(data.firstName || '');
    self.lastName = ko.observable(data.lastName || '');
    self.email = ko.observable(data.email || '');
    self.password = ko.observable('');
    self.role = ko.observable(data.role !== undefined ? data.role.toString() : '2');
    self.isActive = ko.observable(data.isActive !== undefined ? data.isActive : true);
}

// ===== Customer Personnel Edit ViewModel =====
function CustomerPersonnelEditViewModel(data) {
    var self = this;
    data = data || {};

    self.id = data.id || null;
    self.customerId = ko.observable(data.customerId || '');
    self.username = ko.observable(data.username || '');
    self.firstName = ko.observable(data.firstName || '');
    self.lastName = ko.observable(data.lastName || '');
    self.email = ko.observable(data.email || '');
    self.phoneNumber = ko.observable(data.phoneNumber || '');
    self.department = ko.observable(data.department || '');
    self.password = ko.observable('');
    self.role = ko.observable(data.role !== undefined ? data.role.toString() : '1');
    self.isActive = ko.observable(data.isActive !== undefined ? data.isActive : true);
}

// ===== Password Change ViewModel =====
function PasswordChangeViewModel(user, isCustomerPersonnel) {
    var self = this;

    self.userId = user.id;
    self.username = user.username;
    self.isCustomerPersonnel = isCustomerPersonnel || false;
    self.newPassword = ko.observable('');
    self.newPasswordConfirm = ko.observable('');
}

// ===== Main ViewModel =====
function UsersViewModel() {
    var self = this;

    // ===== Tab State =====
    self.activeTab = ko.observable('users');

    // ===== State =====
    self.isLoading = ko.observable(false);
    self.isSaving = ko.observable(false);
    self.errorMessage = ko.observable('');
    self.successMessage = ko.observable('');
    self.modalErrorMessage = ko.observable('');

    // ===== Data =====
    self.users = ko.observableArray([]);
    self.customerPersonnel = ko.observableArray([]);
    self.customers = ko.observableArray([]);

    // ===== Filter =====
    self.selectedCustomerId = ko.observable('');
    self.searchText = ko.observable('');

    // ===== Editing State =====
    self.editingUser = ko.observable(null);
    self.editingCustomerPersonnel = ko.observable(null);
    self.passwordChangeUser = ko.observable(null);

    // ===== Modal State =====
    self.isModalOpen = ko.observable(false);
    self.isCustomerPersonnelModalOpen = ko.observable(false);
    self.isPasswordModalOpen = ko.observable(false);

    // ===== Filtered Customer Personnel =====
    self.filteredCustomerPersonnel = ko.computed(function() {
        var list = self.customerPersonnel();
        var customerId = self.selectedCustomerId();
        var search = (self.searchText() || '').toLowerCase();

        if (customerId) {
            list = list.filter(function(p) {
                return p.customerId == customerId;
            });
        }

        if (search) {
            list = list.filter(function(p) {
                return (p.username || '').toLowerCase().indexOf(search) >= 0 ||
                       (p.firstName || '').toLowerCase().indexOf(search) >= 0 ||
                       (p.lastName || '').toLowerCase().indexOf(search) >= 0 ||
                       (p.email || '').toLowerCase().indexOf(search) >= 0;
            });
        }

        return list;
    });

    // ===== Tab Switch =====
    self.switchTab = function(tab) {
        self.activeTab(tab);
        self.errorMessage('');
        self.successMessage('');
    };

    // ===== Role Display Helpers (System Users) =====
    self.getRoleDisplayName = function(role) {
        var roleNames = {
            1: 'Admin', 2: 'Kalite Uzmanı', 3: 'Saha Çalışanı',
            'Admin': 'Admin', 'QualitySpecialist': 'Kalite Uzmanı', 'FieldWorker': 'Saha Çalışanı'
        };
        return roleNames[role] || 'Bilinmiyor';
    };

    self.getRoleBadgeClass = function(role) {
        var roleClasses = {
            1: 'bg-danger', 2: 'bg-primary', 3: 'bg-success',
            'Admin': 'bg-danger', 'QualitySpecialist': 'bg-primary', 'FieldWorker': 'bg-success'
        };
        return roleClasses[role] || 'bg-secondary';
    };

    // ===== Role Display Helpers (Customer Personnel) =====
    self.getCustomerRoleDisplayName = function(role) {
        var roleNames = {
            1: 'Müşteri Yöneticisi', 2: 'Müşteri Süpervizörü', 3: 'Müşteri Operatörü',
            'CustomerManager': 'Müşteri Yöneticisi', 'CustomerSupervisor': 'Müşteri Süpervizörü', 'CustomerOperator': 'Müşteri Operatörü'
        };
        return roleNames[role] || 'Bilinmiyor';
    };

    self.getCustomerRoleBadgeClass = function(role) {
        var roleClasses = {
            1: 'bg-info', 2: 'bg-warning', 3: 'bg-secondary',
            'CustomerManager': 'bg-info', 'CustomerSupervisor': 'bg-warning', 'CustomerOperator': 'bg-secondary'
        };
        return roleClasses[role] || 'bg-secondary';
    };

    // ===== Load Data =====
    self.loadUsers = function() {
        fetch('/api/users', { credentials: 'include' })
            .then(function(res) { return res.json(); })
            .then(function(data) { self.users(data || []); })
            .catch(function(err) { console.error('Error loading users:', err); });
    };

    self.loadCustomerPersonnel = function() {
        fetch('/api/customer-personnel', { credentials: 'include' })
            .then(function(res) { return res.json(); })
            .then(function(data) { self.customerPersonnel(data || []); })
            .catch(function(err) { console.error('Error loading customer personnel:', err); });
    };

    self.loadCustomers = function() {
        fetch('/api/customers', { credentials: 'include' })
            .then(function(res) { return res.json(); })
            .then(function(data) { self.customers(data || []); })
            .catch(function(err) { console.error('Error loading customers:', err); });
    };

    self.loadAll = function() {
        self.isLoading(true);
        Promise.all([
            fetch('/api/users', { credentials: 'include' }).then(function(r) { return r.json(); }),
            fetch('/api/customer-personnel', { credentials: 'include' }).then(function(r) { return r.json(); }),
            fetch('/api/customers', { credentials: 'include' }).then(function(r) { return r.json(); })
        ])
        .then(function(results) {
            self.users(results[0] || []);
            self.customerPersonnel(results[1] || []);
            self.customers(results[2] || []);
        })
        .catch(function(err) {
            console.error('Error loading data:', err);
            self.errorMessage('Veriler yüklenirken bir hata oluştu.');
        })
        .finally(function() {
            self.isLoading(false);
        });
    };

    // ===== Create New =====
    self.createNew = function() {
        self.modalErrorMessage('');
        if (self.activeTab() === 'users') {
            self.editingUser(new UserEditViewModel());
            self.isModalOpen(true);
        } else {
            self.editingCustomerPersonnel(new CustomerPersonnelEditViewModel());
            self.isCustomerPersonnelModalOpen(true);
        }
    };

    // ===== User CRUD =====
    self.editUser = function(user) {
        self.modalErrorMessage('');
        self.editingUser(new UserEditViewModel(user));
        self.isModalOpen(true);
    };

    self.saveUser = function() {
        var u = self.editingUser();

        if (!u.id && (!u.username() || u.username().trim() === '')) {
            toastr.warning('Kullanıcı adı zorunludur!');
            return;
        }
        if (!u.firstName() || !u.lastName() || !u.email()) {
            toastr.warning('Ad, Soyad ve E-posta zorunludur!');
            return;
        }
        if (!u.id && (!u.password() || u.password().length < 6)) {
            toastr.warning('Şifre en az 6 karakter olmalıdır!');
            return;
        }

        var dto = {
            firstName: u.firstName(),
            lastName: u.lastName(),
            email: u.email(),
            role: parseInt(u.role()),
            isActive: u.isActive()
        };

        if (!u.id) {
            dto.username = u.username();
            dto.password = u.password();
        }

        self.isSaving(true);
        var endpoint = u.id ? '/api/users/' + u.id : '/api/users';
        var method = u.id ? 'PUT' : 'POST';

        fetch(endpoint, {
            method: method,
            headers: { 'Content-Type': 'application/json' },
            credentials: 'include',
            body: JSON.stringify(dto)
        })
        .then(function(res) {
            if (!res.ok) return res.json().then(function(e) { throw new Error(e.message || 'Hata'); });
            return res.json();
        })
        .then(function() {
            toastr.success('Kullanıcı başarıyla kaydedildi.');
            self.closeModal();
            self.loadUsers();
        })
        .catch(function(err) {
            self.modalErrorMessage(err.message);
        })
        .finally(function() {
            self.isSaving(false);
        });
    };

    self.deleteUser = function(user) {
        showDeleteConfirm(user.firstName + ' ' + user.lastName, function() {
            fetch('/api/users/' + user.id, { method: 'DELETE', credentials: 'include' })
                .then(function() {
                    toastr.success('Kullanıcı silindi.');
                    self.users.remove(user);
                })
                .catch(function(err) {
                    toastr.error('Silme hatası.');
                });
        });
    };

    self.closeModal = function() {
        self.isModalOpen(false);
        self.editingUser(null);
        self.modalErrorMessage('');
    };

    // ===== Customer Personnel CRUD =====
    self.editCustomerPersonnel = function(cp) {
        self.modalErrorMessage('');
        self.editingCustomerPersonnel(new CustomerPersonnelEditViewModel(cp));
        self.isCustomerPersonnelModalOpen(true);
    };

    self.saveCustomerPersonnel = function() {
        var cp = self.editingCustomerPersonnel();

        if (!cp.customerId()) {
            toastr.warning('Müşteri seçimi zorunludur!');
            return;
        }
        if (!cp.id && (!cp.username() || cp.username().trim() === '')) {
            toastr.warning('Kullanıcı adı zorunludur!');
            return;
        }
        if (!cp.firstName() || !cp.lastName() || !cp.email()) {
            toastr.warning('Ad, Soyad ve E-posta zorunludur!');
            return;
        }
        if (!cp.id && (!cp.password() || cp.password().length < 6)) {
            toastr.warning('Şifre en az 6 karakter olmalıdır!');
            return;
        }

        var dto = {
            customerId: parseInt(cp.customerId()),
            firstName: cp.firstName(),
            lastName: cp.lastName(),
            email: cp.email(),
            phoneNumber: cp.phoneNumber(),
            department: cp.department(),
            role: parseInt(cp.role()),
            isActive: cp.isActive()
        };

        if (!cp.id) {
            dto.username = cp.username();
            dto.password = cp.password();
        }

        self.isSaving(true);
        var endpoint = cp.id ? '/api/customer-personnel/' + cp.id : '/api/customer-personnel';
        var method = cp.id ? 'PUT' : 'POST';

        fetch(endpoint, {
            method: method,
            headers: { 'Content-Type': 'application/json' },
            credentials: 'include',
            body: JSON.stringify(dto)
        })
        .then(function(res) {
            if (!res.ok) return res.json().then(function(e) { throw new Error(e.message || 'Hata'); });
            return res.json();
        })
        .then(function() {
            toastr.success('Müşteri personeli başarıyla kaydedildi.');
            self.closeCustomerPersonnelModal();
            self.loadCustomerPersonnel();
        })
        .catch(function(err) {
            self.modalErrorMessage(err.message);
        })
        .finally(function() {
            self.isSaving(false);
        });
    };

    self.deleteCustomerPersonnel = function(cp) {
        showDeleteConfirm(cp.firstName + ' ' + cp.lastName, function() {
            fetch('/api/customer-personnel/' + cp.id, { method: 'DELETE', credentials: 'include' })
                .then(function() {
                    toastr.success('Müşteri personeli silindi.');
                    self.customerPersonnel.remove(cp);
                })
                .catch(function(err) {
                    toastr.error('Silme hatası.');
                });
        });
    };

    self.closeCustomerPersonnelModal = function() {
        self.isCustomerPersonnelModalOpen(false);
        self.editingCustomerPersonnel(null);
        self.modalErrorMessage('');
    };

    // ===== Password Change =====
    self.changePassword = function(user) {
        self.modalErrorMessage('');
        self.passwordChangeUser(new PasswordChangeViewModel(user, false));
        self.isPasswordModalOpen(true);
    };

    self.changeCustomerPersonnelPassword = function(cp) {
        self.modalErrorMessage('');
        self.passwordChangeUser(new PasswordChangeViewModel(cp, true));
        self.isPasswordModalOpen(true);
    };

    self.savePassword = function() {
        var pwdUser = self.passwordChangeUser();

        if (!pwdUser.newPassword() || pwdUser.newPassword().length < 6) {
            toastr.warning('Şifre en az 6 karakter olmalıdır!');
            return;
        }
        if (pwdUser.newPassword() !== pwdUser.newPasswordConfirm()) {
            toastr.warning('Şifreler eşleşmiyor!');
            return;
        }

        self.isSaving(true);

        var endpoint = pwdUser.isCustomerPersonnel
            ? '/api/customer-personnel/' + pwdUser.userId + '/change-password'
            : '/api/users/' + pwdUser.userId + '/change-password';

        fetch(endpoint, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            credentials: 'include',
            body: JSON.stringify({ newPassword: pwdUser.newPassword() })
        })
        .then(function(res) {
            if (!res.ok) return res.json().then(function(e) { throw new Error(e.message || 'Hata'); });
            return res.json();
        })
        .then(function() {
            toastr.success('Şifre başarıyla değiştirildi.');
            self.closePasswordModal();
        })
        .catch(function(err) {
            self.modalErrorMessage(err.message);
        })
        .finally(function() {
            self.isSaving(false);
        });
    };

    self.closePasswordModal = function() {
        self.isPasswordModalOpen(false);
        self.passwordChangeUser(null);
        self.modalErrorMessage('');
    };

    // ===== Initialize =====
    self.loadAll();
}

// ===== Apply Bindings =====
$(document).ready(function() {
    ko.applyBindings(new UsersViewModel(), document.getElementById('users-app'));
});
