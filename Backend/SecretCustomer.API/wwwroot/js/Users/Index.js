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

    // ========== USERS TAB CHIP-BASED FILTER SYSTEM ==========
    self.usersSelectedFilterType = ko.observable('');
    self.usersActiveFilters = ko.observableArray([]);

    self.usersTempFilter = {
        username: ko.observable(''),
        fullName: ko.observable(''),
        email: ko.observable(''),
        role: ko.observable(null),
        isActive: ko.observable(null)
    };

    self.usersFilterLabels = {
        username: 'Kullanıcı Adı',
        fullName: 'Ad Soyad',
        email: 'E-posta',
        role: 'Rol',
        isActive: 'Durum'
    };

    self.usersStatusLabels = {
        'true': 'Aktif',
        'false': 'Pasif'
    };

    self.usersCanAddFilter = ko.computed(function() {
        var type = self.usersSelectedFilterType();
        if (!type) return false;

        switch (type) {
            case 'username': return self.usersTempFilter.username().trim() !== '';
            case 'fullName': return self.usersTempFilter.fullName().trim() !== '';
            case 'email': return self.usersTempFilter.email().trim() !== '';
            case 'role': return self.usersTempFilter.role() !== null;
            case 'isActive': return self.usersTempFilter.isActive() !== null;
            default: return false;
        }
    });

    self.usersAddFilter = function() {
        var type = self.usersSelectedFilterType();
        if (!type) return;

        var filter = {
            type: type,
            label: self.usersFilterLabels[type],
            value: null,
            displayValue: ''
        };

        switch (type) {
            case 'username':
                var username = self.usersTempFilter.username().trim();
                if (!username) return;
                filter.value = username;
                filter.displayValue = username;
                self.usersTempFilter.username('');
                break;

            case 'fullName':
                var fullName = self.usersTempFilter.fullName().trim();
                if (!fullName) return;
                filter.value = fullName;
                filter.displayValue = fullName;
                self.usersTempFilter.fullName('');
                break;

            case 'email':
                var email = self.usersTempFilter.email().trim();
                if (!email) return;
                filter.value = email;
                filter.displayValue = email;
                self.usersTempFilter.email('');
                break;

            case 'role':
                var roleId = self.usersTempFilter.role();
                if (!roleId) return;
                var role = self.userRoles().find(function(r) { return r.id === roleId; });
                filter.value = roleId;
                filter.displayValue = role ? role.name : roleId;
                self.usersTempFilter.role(null);
                break;

            case 'isActive':
                var isActive = self.usersTempFilter.isActive();
                if (isActive === null) return;
                filter.value = isActive;
                filter.displayValue = self.usersStatusLabels[String(isActive)];
                self.usersTempFilter.isActive(null);
                break;

            default:
                return;
        }

        self.usersActiveFilters.push(filter);
        self.usersSelectedFilterType('');
        self.usersPage(1);
    };

    self.usersRemoveFilter = function(filter) {
        self.usersActiveFilters.remove(filter);
        self.usersPage(1);
    };

    self.usersClearFilters = function() {
        self.usersActiveFilters([]);
        self.usersSearchText('');
        self.usersPage(1);
    };

    // ========== CUSTOMER PERSONNEL TAB CHIP-BASED FILTER SYSTEM ==========
    self.cpSelectedFilterType = ko.observable('');
    self.cpActiveFilters = ko.observableArray([]);

    self.cpTempFilter = {
        customerId: ko.observable(null),
        username: ko.observable(''),
        fullName: ko.observable(''),
        email: ko.observable(''),
        role: ko.observable(null),
        isActive: ko.observable(null)
    };

    self.cpFilterLabels = {
        customer: 'Müşteri',
        username: 'Kullanıcı Adı',
        fullName: 'Ad Soyad',
        email: 'E-posta',
        role: 'Rol',
        isActive: 'Durum'
    };

    self.cpStatusLabels = {
        'true': 'Aktif',
        'false': 'Pasif'
    };

    self.cpCanAddFilter = ko.computed(function() {
        var type = self.cpSelectedFilterType();
        if (!type) return false;

        switch (type) {
            case 'customer': return self.cpTempFilter.customerId() !== null;
            case 'username': return self.cpTempFilter.username().trim() !== '';
            case 'fullName': return self.cpTempFilter.fullName().trim() !== '';
            case 'email': return self.cpTempFilter.email().trim() !== '';
            case 'role': return self.cpTempFilter.role() !== null;
            case 'isActive': return self.cpTempFilter.isActive() !== null;
            default: return false;
        }
    });

    // ===== Sorting =====
    self.sorting = TableSorting.createSortState('username', 'asc');
    self.cpSorting = TableSorting.createSortState('username', 'asc');

    // ===== Users Pagination =====
    self.usersPage = ko.observable(1);
    self.usersPageSize = ko.observable(20);

    // Filtered and sorted users (before pagination)
    self.filteredAndSortedUsers = ko.computed(function() {
        var items = self.allUsers();
        var search = (self.usersSearchText() || '').toLocaleLowerCase('tr-TR');
        var filters = self.usersActiveFilters();

        // Global search
        if (search) {
            items = items.filter(function(u) {
                return (u.username || '').toLocaleLowerCase('tr-TR').indexOf(search) >= 0 ||
                       (u.firstName || '').toLocaleLowerCase('tr-TR').indexOf(search) >= 0 ||
                       (u.lastName || '').toLocaleLowerCase('tr-TR').indexOf(search) >= 0 ||
                       (u.email || '').toLocaleLowerCase('tr-TR').indexOf(search) >= 0;
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
                items = items.filter(function(u) {
                    return typeFilters.some(function(f) {
                        switch (f.type) {
                            case 'username':
                                return (u.username || '').toLocaleLowerCase('tr-TR').indexOf(f.value.toLocaleLowerCase('tr-TR')) >= 0;
                            case 'fullName':
                                var fullName = ((u.firstName || '') + ' ' + (u.lastName || '')).toLocaleLowerCase('tr-TR');
                                return fullName.indexOf(f.value.toLocaleLowerCase('tr-TR')) >= 0;
                            case 'email':
                                return (u.email || '').toLocaleLowerCase('tr-TR').indexOf(f.value.toLocaleLowerCase('tr-TR')) >= 0;
                            case 'role':
                                return String(u.roleId) === f.value;
                            case 'isActive':
                                return u.isActive === f.value;
                            default:
                                return true;
                        }
                    });
                });
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
    self.cpPageSize = ko.observable(50);
    self.cpTotalCountServer = ko.observable(0);
    self.cpIsLoading = ko.observable(false);

    // ===== Editing State =====
    self.editingUser = ko.observable(null);
    self.editingCustomerPersonnel = ko.observable(null);
    self.passwordChangeUser = ko.observable(null);

    // ===== Modal State =====
    self.isModalOpen = ko.observable(false);
    self.isCustomerPersonnelModalOpen = ko.observable(false);
    self.isPasswordModalOpen = ko.observable(false);

    // ===== Server-side Customer Personnel Search =====
    self.searchCustomerPersonnel = function() {
        self.cpIsLoading(true);

        var params = new URLSearchParams();
        params.append('Page', self.cpPage());
        params.append('PageSize', self.cpPageSize());

        // Global search
        var search = (self.searchText() || '').trim();
        if (search) {
            params.append('SearchTerm', search);
        }

        // Legacy dropdown filter
        var customerId = self.selectedCustomerId();
        if (customerId) {
            params.append('CustomerIds', customerId);
        }

        // Chip-based filters
        var filters = self.cpActiveFilters();
        filters.forEach(function(f) {
            switch (f.type) {
                case 'customer':
                    params.append('CustomerIds', f.value);
                    break;
                case 'fullName':
                    params.append('FullNames', f.value);
                    break;
                case 'email':
                    params.append('Emails', f.value);
                    break;
                case 'isActive':
                    params.append('IsActive', f.value);
                    break;
            }
        });

        // Include inactive based on filter
        var hasActiveFilter = filters.some(function(f) { return f.type === 'isActive'; });
        if (!hasActiveFilter) {
            params.append('IncludeInactive', 'true');
        }

        fetch('/api/customer-personnel?' + params.toString(), { credentials: 'include' })
            .then(function(res) { return res.json(); })
            .then(function(data) {
                self.allCustomerPersonnel(data.items || []);
                self.cpTotalCountServer(data.totalCount || 0);
            })
            .catch(function(err) {
                console.error('Error searching customer personnel:', err);
                toastr.error('Arama sırasında hata oluştu');
            })
            .finally(function() {
                self.cpIsLoading(false);
            });
    };

    // Customer Personnel list (directly from server)
    self.filteredCustomerPersonnel = ko.computed(function() {
        return self.allCustomerPersonnel();
    });

    // Backwards compatibility
    self.customerPersonnel = self.filteredCustomerPersonnel;

    self.cpTotalCount = ko.computed(function() {
        return self.cpTotalCountServer();
    });

    self.cpTotalPages = ko.computed(function() {
        return Math.ceil(self.cpTotalCount() / parseInt(self.cpPageSize(), 10)) || 1;
    });

    // Debounced search for text input
    var cpSearchTimeout = null;
    self.searchText.subscribe(function() {
        clearTimeout(cpSearchTimeout);
        cpSearchTimeout = setTimeout(function() {
            self.cpPage(1);
            self.searchCustomerPersonnel();
        }, 300);
    });

    self.selectedCustomerId.subscribe(function() {
        self.cpPage(1);
        self.searchCustomerPersonnel();
    });

    self.cpPageSize.subscribe(function() {
        self.cpPage(1);
        self.searchCustomerPersonnel();
    });

    // Override filter functions to trigger search
    var originalCpAddFilter = self.cpAddFilter;
    self.cpAddFilter = function() {
        var type = self.cpSelectedFilterType();
        if (!type) return;

        var filter = {
            type: type,
            label: self.cpFilterLabels[type],
            value: null,
            displayValue: ''
        };

        switch (type) {
            case 'customer':
                var customerId = self.cpTempFilter.customerId();
                if (!customerId) return;
                var customer = self.customers().find(function(c) { return c.id === customerId; });
                filter.value = customerId;
                filter.displayValue = customer ? customer.companyName : customerId;
                self.cpTempFilter.customerId(null);
                break;

            case 'username':
                var username = self.cpTempFilter.username().trim();
                if (!username) return;
                filter.value = username;
                filter.displayValue = username;
                self.cpTempFilter.username('');
                break;

            case 'fullName':
                var fullName = self.cpTempFilter.fullName().trim();
                if (!fullName) return;
                filter.value = fullName;
                filter.displayValue = fullName;
                self.cpTempFilter.fullName('');
                break;

            case 'email':
                var email = self.cpTempFilter.email().trim();
                if (!email) return;
                filter.value = email;
                filter.displayValue = email;
                self.cpTempFilter.email('');
                break;

            case 'role':
                var roleId = self.cpTempFilter.role();
                if (!roleId) return;
                var role = self.customerPersonnelRoles().find(function(r) { return r.id === roleId; });
                filter.value = roleId;
                filter.displayValue = role ? role.name : roleId;
                self.cpTempFilter.role(null);
                break;

            case 'isActive':
                var isActive = self.cpTempFilter.isActive();
                if (isActive === null) return;
                filter.value = isActive;
                filter.displayValue = self.cpStatusLabels[String(isActive)];
                self.cpTempFilter.isActive(null);
                break;

            default:
                return;
        }

        self.cpActiveFilters.push(filter);
        self.cpSelectedFilterType('');
        self.cpPage(1);
        self.searchCustomerPersonnel();
    };

    self.cpRemoveFilter = function(filter) {
        self.cpActiveFilters.remove(filter);
        self.cpPage(1);
        self.searchCustomerPersonnel();
    };

    self.cpClearFilters = function() {
        self.cpActiveFilters([]);
        self.searchText('');
        self.selectedCustomerId('');
        self.cpPage(1);
        self.searchCustomerPersonnel();
    };

    self.cpGoToPage = function(page) {
        if (page >= 1 && page <= self.cpTotalPages()) {
            self.cpPage(page);
            self.searchCustomerPersonnel();
        }
    };

    self.cpPreviousPage = function() {
        if (self.cpPage() > 1) {
            self.cpPage(self.cpPage() - 1);
            self.searchCustomerPersonnel();
        }
    };

    self.cpNextPage = function() {
        if (self.cpPage() < self.cpTotalPages()) {
            self.cpPage(self.cpPage() + 1);
            self.searchCustomerPersonnel();
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
        fetch('/api/customers?pageSize=1000', { credentials: 'include' })
            .then(function(res) { return res.json(); })
            .then(function(data) { self.customers(data.items || []); })
            .catch(function(err) { console.error('Error loading customers:', err); });
    };

    self.loadAll = function() {
        self.isLoading(true);
        // Once EnumsService'i yukle, sonra verileri cek
        EnumsService.load()
            .then(function() {
                // Rol listelerini EnumsService'den al (displayName artik API'den lokalize gelir)
                var cache = EnumsService.cache;
                if (cache && cache.userRoles) {
                    self.userRoles(cache.userRoles.map(function(r) {
                        return { id: r.id.toString(), name: r.displayName || r.nameKey.split('.').pop() };
                    }));
                }
                if (cache && cache.customerPersonnelRoles) {
                    self.customerPersonnelRoles(cache.customerPersonnelRoles.map(function(r) {
                        return { id: r.systemName, name: r.displayName || r.nameKey.split('.').pop() };
                    }));
                }
                return Promise.all([
                    fetch('/api/users', { credentials: 'include' }).then(function(r) { return r.json(); }),
                    fetch('/api/customers?pageSize=1000', { credentials: 'include' }).then(function(r) { return r.json(); })
                ]);
            })
            .then(function(results) {
                self.allUsers(results[0] || []);
                // customers API returns PagedCustomerResult { items: [], totalCount: X }
                self.customers(results[1].items || []);
                self.usersPage(1);
                // Customer personnel server-side arama ile yüklenecek
                self.searchCustomerPersonnel();
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
