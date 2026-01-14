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
    self.role = ko.observable(data.roleId !== undefined ? data.roleId.toString() : '2');
    self.isActive = ko.observable(data.isActive !== undefined ? data.isActive : true);

    // Mevcut data'dan ViewModel olustur (guncelleme icin)
    self.toData = function() {
        return {
            id: self.id,
            username: self.username(),
            firstName: self.firstName(),
            lastName: self.lastName(),
            email: self.email(),
            role: parseInt(self.role()),
            isActive: self.isActive()
        };
    };
}

// ===== Customer Personnel Edit ViewModel =====
function CustomerPersonnelEditViewModel(data) {
    var self = this;
    data = data || {};

    self.id = data.id || data.Id || null;
    self.customerId = ko.observable(data.customerId || data.CustomerId || '');
    self.customerName = data.customerName || data.CustomerName || '';
    self.username = ko.observable(data.username || data.Username || '');
    self.firstName = ko.observable(data.firstName || data.FirstName || '');
    self.lastName = ko.observable(data.lastName || data.LastName || '');
    self.email = ko.observable(data.email || data.Email || '');
    self.phoneNumber = ko.observable(data.phoneNumber || data.PhoneNumber || '');
    self.department = ko.observable(data.department || data.Department || '');
    self.password = ko.observable('');
    self.role = ko.observable(data.role || data.Role || 'CustomerOperator');
    self.isActive = ko.observable(data.isActive !== undefined ? data.isActive : (data.IsActive !== undefined ? data.IsActive : true));

    // Mevcut data'dan ViewModel olustur (guncelleme icin)
    self.toData = function() {
        return {
            id: self.id,
            customerId: parseInt(self.customerId()),
            customerName: self.customerName,
            username: self.username(),
            firstName: self.firstName(),
            lastName: self.lastName(),
            email: self.email(),
            phoneNumber: self.phoneNumber(),
            department: self.department(),
            role: self.role(),
            isActive: self.isActive()
        };
    };
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
    self.allUsers = ko.observableArray([]);
    self.allCustomerPersonnel = ko.observableArray([]);
    self.customers = ko.observableArray([]);

    // ===== Role Lists (EnumsService'den) =====
    self.userRoles = ko.observableArray([]);
    self.customerPersonnelRoles = ko.observableArray([]);

    // ===== Filter =====
    self.selectedCustomerId = ko.observable('');
    self.searchText = ko.observable('');
    self.usersSearchText = ko.observable('');

    // ===== Sorting =====
    self.sorting = TableSorting.createSortState('username', 'asc');
    self.cpSorting = TableSorting.createSortState('username', 'asc');

    // ===== Users Pagination =====
    self.usersPage = ko.observable(1);
    self.usersPageSize = ko.observable(20);

    // Filtered and sorted users (before pagination)
    self.filteredAndSortedUsers = ko.computed(function() {
        var items = self.allUsers();
        var search = (self.usersSearchText() || '').toLowerCase();

        if (search) {
            items = items.filter(function(u) {
                return (u.username || '').toLowerCase().indexOf(search) >= 0 ||
                       (u.firstName || '').toLowerCase().indexOf(search) >= 0 ||
                       (u.lastName || '').toLowerCase().indexOf(search) >= 0 ||
                       (u.email || '').toLowerCase().indexOf(search) >= 0;
            });
        }

        var sortBy = self.sorting.sortBy();
        var sortDir = self.sorting.sortDirection();
        if (sortBy && items.length > 0) {
            items = TableSorting.clientSort(items, sortBy, sortDir);
        }
        return items;
    });

    // Paginated users
    self.sortedUsers = ko.computed(function() {
        var list = self.filteredAndSortedUsers();
        var page = parseInt(self.usersPage(), 10);
        var pageSize = parseInt(self.usersPageSize(), 10);
        var start = (page - 1) * pageSize;
        return list.slice(start, start + pageSize);
    });

    // Backwards compatibility
    self.users = self.sortedUsers;

    self.usersTotalCount = ko.computed(function() {
        return self.filteredAndSortedUsers().length;
    });

    self.usersTotalPages = ko.computed(function() {
        return Math.ceil(self.usersTotalCount() / parseInt(self.usersPageSize(), 10)) || 1;
    });

    self.usersPageSize.subscribe(function() {
        self.usersPage(1);
    });

    self.usersSearchText.subscribe(function() {
        self.usersPage(1);
    });

    self.usersGoToPage = function(page) {
        if (page >= 1 && page <= self.usersTotalPages()) {
            self.usersPage(page);
        }
    };

    self.usersPreviousPage = function() {
        if (self.usersPage() > 1) {
            self.usersPage(self.usersPage() - 1);
        }
    };

    self.usersNextPage = function() {
        if (self.usersPage() < self.usersTotalPages()) {
            self.usersPage(self.usersPage() + 1);
        }
    };

    // ===== Customer Personnel Pagination =====
    self.cpPage = ko.observable(1);
    self.cpPageSize = ko.observable(20);

    // ===== Editing State =====
    self.editingUser = ko.observable(null);
    self.editingCustomerPersonnel = ko.observable(null);
    self.passwordChangeUser = ko.observable(null);

    // ===== Modal State =====
    self.isModalOpen = ko.observable(false);
    self.isCustomerPersonnelModalOpen = ko.observable(false);
    self.isPasswordModalOpen = ko.observable(false);

    // ===== Filtered Customer Personnel (before pagination) =====
    self.allFilteredCustomerPersonnel = ko.computed(function() {
        var list = self.allCustomerPersonnel();
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

        // Sıralama uygula
        var sortBy = self.cpSorting.sortBy();
        var sortDir = self.cpSorting.sortDirection();
        if (sortBy && list.length > 0) {
            list = TableSorting.clientSort(list, sortBy, sortDir);
        }

        return list;
    });

    // Paginated customer personnel
    self.filteredCustomerPersonnel = ko.computed(function() {
        var list = self.allFilteredCustomerPersonnel();
        var page = parseInt(self.cpPage(), 10);
        var pageSize = parseInt(self.cpPageSize(), 10);
        var start = (page - 1) * pageSize;
        return list.slice(start, start + pageSize);
    });

    // Backwards compatibility
    self.customerPersonnel = self.filteredCustomerPersonnel;

    self.cpTotalCount = ko.computed(function() {
        return self.allFilteredCustomerPersonnel().length;
    });

    self.cpTotalPages = ko.computed(function() {
        return Math.ceil(self.cpTotalCount() / parseInt(self.cpPageSize(), 10)) || 1;
    });

    self.cpPageSize.subscribe(function() {
        self.cpPage(1);
    });

    self.selectedCustomerId.subscribe(function() {
        self.cpPage(1);
    });

    self.searchText.subscribe(function() {
        self.cpPage(1);
    });

    self.cpGoToPage = function(page) {
        if (page >= 1 && page <= self.cpTotalPages()) {
            self.cpPage(page);
        }
    };

    self.cpPreviousPage = function() {
        if (self.cpPage() > 1) {
            self.cpPage(self.cpPage() - 1);
        }
    };

    self.cpNextPage = function() {
        if (self.cpPage() < self.cpTotalPages()) {
            self.cpPage(self.cpPage() + 1);
        }
    };

    // ===== Tab Switch =====
    self.switchTab = function(tab) {
        self.activeTab(tab);
        self.errorMessage('');
        self.successMessage('');
    };

    // ===== Role Display Helpers (System Users) - EnumsService kullanir =====
    self.getRoleDisplayName = function(role) {
        return EnumsService.getUserRoleDisplay(role);
    };

    self.getRoleBadgeClass = function(role) {
        return EnumsService.getUserRoleCss(role);
    };

    // ===== Role Display Helpers (Customer Personnel) - EnumsService kullanir =====
    self.getCustomerRoleDisplayName = function(role) {
        return EnumsService.getCustomerRoleDisplay(role);
    };

    self.getCustomerRoleBadgeClass = function(role) {
        return EnumsService.getCustomerRoleCss(role);
    };

    // ===== Load Data =====
    self.loadUsers = function() {
        fetch('/api/users', { credentials: 'include' })
            .then(function(res) { return res.json(); })
            .then(function(data) {
                self.allUsers(data || []);
                self.usersPage(1);
            })
            .catch(function(err) { console.error('Error loading users:', err); });
    };

    self.loadCustomerPersonnel = function() {
        fetch('/api/customer-personnel', { credentials: 'include' })
            .then(function(res) { return res.json(); })
            .then(function(data) {
                self.allCustomerPersonnel(data || []);
                self.cpPage(1);
            })
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
        // Once EnumsService'i yukle, sonra verileri cek
        EnumsService.load()
            .then(function() {
                // Rol listelerini EnumsService'den al
                var cache = EnumsService.cache;
                if (cache && cache.userRoles) {
                    self.userRoles(cache.userRoles.map(function(r) {
                        return { id: r.id.toString(), name: T(r.nameKey, r.nameKey.split('.').pop()) };
                    }));
                }
                if (cache && cache.customerPersonnelRoles) {
                    self.customerPersonnelRoles(cache.customerPersonnelRoles.map(function(r) {
                        return { id: r.systemName, name: T(r.nameKey, r.nameKey.split('.').pop()) };
                    }));
                }
                return Promise.all([
                    fetch('/api/users', { credentials: 'include' }).then(function(r) { return r.json(); }),
                    fetch('/api/customer-personnel', { credentials: 'include' }).then(function(r) { return r.json(); }),
                    fetch('/api/customers', { credentials: 'include' }).then(function(r) { return r.json(); })
                ]);
            })
            .then(function(results) {
                self.allUsers(results[0] || []);
                self.allCustomerPersonnel(results[1] || []);
                self.customers(results[2] || []);
                self.usersPage(1);
                self.cpPage(1);
            })
            .catch(function(err) {
                console.error('Error loading data:', err);
                toastr.error('Veriler yüklenirken bir hata oluştu.');
            })
            .finally(function() {
                self.isLoading(false);
            });
    };

    // ===== Create New =====
    self.createNew = function() {        if (self.activeTab() === 'users') {
            self.editingUser(new UserEditViewModel());
            self.isModalOpen(true);
        } else {
            self.editingCustomerPersonnel(new CustomerPersonnelEditViewModel());
            self.isCustomerPersonnelModalOpen(true);
        }
    };

    // ===== User CRUD =====
    self.editUser = function(user) {        self.editingUser(new UserEditViewModel(user));
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
            roleId: parseInt(u.role()),
            isActive: u.isActive()
        };

        if (!u.id) {
            dto.username = u.username();
            dto.password = u.password();
        }

        self.isSaving(true);
        var isNew = !u.id;
        var endpoint = isNew ? '/api/users' : '/api/users/' + u.id;
        var method = isNew ? 'POST' : 'PUT';

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
        .then(function(savedUser) {
            if (isNew) {
                // Yeni kayit: array'e ekle
                self.allUsers.push(savedUser);
            } else {
                // Guncelleme: array'de bul ve guncelle
                var users = self.allUsers();
                for (var i = 0; i < users.length; i++) {
                    if (users[i].id === savedUser.id) {
                        self.allUsers.splice(i, 1, savedUser);
                        break;
                    }
                }
            }
            toastr.success('Kullanıcı başarıyla kaydedildi.');
            self.closeModal();
        })
        .catch(function(err) {
            toastr.error(err.message);
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
                    self.allUsers.remove(user);
                })
                .catch(function(err) {
                    toastr.error('Silme hatası.');
                });
        });
    };

    self.closeModal = function() {
        self.isModalOpen(false);
        self.editingUser(null);    };

    // ===== Customer Personnel CRUD =====
    self.editCustomerPersonnel = function(cp) {        self.editingCustomerPersonnel(new CustomerPersonnelEditViewModel(cp));
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
            username: cp.username(),
            firstName: cp.firstName(),
            lastName: cp.lastName(),
            email: cp.email(),
            phoneNumber: cp.phoneNumber(),
            department: cp.department(),
            role: cp.role(),
            isActive: cp.isActive()
        };

        if (!cp.id) {
            dto.password = cp.password();
        }

        self.isSaving(true);
        var isNew = !cp.id;
        var endpoint = isNew ? '/api/customer-personnel' : '/api/customer-personnel/' + cp.id;
        var method = isNew ? 'POST' : 'PUT';

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
        .then(function(savedCp) {
            if (isNew) {
                // Yeni kayit: array'e ekle
                self.allCustomerPersonnel.push(savedCp);
            } else {
                // Guncelleme: array'de bul ve guncelle
                var list = self.allCustomerPersonnel();
                for (var i = 0; i < list.length; i++) {
                    if (list[i].id === savedCp.id) {
                        self.allCustomerPersonnel.splice(i, 1, savedCp);
                        break;
                    }
                }
            }
            toastr.success('Müşteri personeli başarıyla kaydedildi.');
            self.closeCustomerPersonnelModal();
        })
        .catch(function(err) {
            toastr.error(err.message);
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
                    self.allCustomerPersonnel.remove(cp);
                })
                .catch(function(err) {
                    toastr.error('Silme hatası.');
                });
        });
    };

    self.closeCustomerPersonnelModal = function() {
        self.isCustomerPersonnelModalOpen(false);
        self.editingCustomerPersonnel(null);    };

    // ===== Password Change =====
    self.changePassword = function(user) {        self.passwordChangeUser(new PasswordChangeViewModel(user, false));
        self.isPasswordModalOpen(true);
    };

    self.changeCustomerPersonnelPassword = function(cp) {        self.passwordChangeUser(new PasswordChangeViewModel(cp, true));
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

        // Admin şifre sıfırlama - reset-password endpoint'i kullan
        var endpoint = pwdUser.isCustomerPersonnel
            ? '/api/customer-personnel/' + pwdUser.userId + '/reset-password'
            : '/api/users/' + pwdUser.userId + '/change-password';

        fetch(endpoint, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            credentials: 'include',
            body: JSON.stringify({
                userId: pwdUser.userId,
                newPassword: pwdUser.newPassword()
            })
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
            toastr.error(err.message);
        })
        .finally(function() {
            self.isSaving(false);
        });
    };

    self.closePasswordModal = function() {
        self.isPasswordModalOpen(false);
        self.passwordChangeUser(null);    };

    // ===== Initialize =====
    self.loadAll();
}

// ===== Apply Bindings =====
$(document).ready(function() {
    ko.applyBindings(new UsersViewModel(), document.getElementById('users-app'));
});
