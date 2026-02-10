// Customers ViewModel
function CustomersViewModel() {
    var self = this;

    // Observables
    self.allCustomers = ko.observableArray([]);
    self.isLoading = ko.observable(false);
    self.isSaving = ko.observable(false);
    self.errorMessage = ko.observable('');
    self.successMessage = ko.observable('');
    self.showInactive = ko.observable(false);
    self.searchText = ko.observable('');

    // ========== CHIP-BASED FILTER SYSTEM ==========
    self.selectedFilterType = ko.observable('');
    self.activeFilters = ko.observableArray([]);

    // Temp filter values
    self.tempFilter = {
        companyName: ko.observable(''),
        code: ko.observable(''),
        email: ko.observable(''),
        city: ko.observable(''),
        taxNumber: ko.observable(''),
        isActive: ko.observable(null)
    };

    // Filter labels (for display)
    self.filterLabels = {
        companyName: 'Firma Adı',
        code: 'Kod',
        email: 'E-posta',
        city: 'Şehir',
        taxNumber: 'Vergi No',
        isActive: 'Durum'
    };

    self.statusLabels = {
        'true': 'Aktif',
        'false': 'Pasif'
    };

    // Can add filter check
    self.canAddFilter = ko.computed(function() {
        var type = self.selectedFilterType();
        if (!type) return false;

        switch (type) {
            case 'companyName': return self.tempFilter.companyName().trim() !== '';
            case 'code': return self.tempFilter.code().trim() !== '';
            case 'email': return self.tempFilter.email().trim() !== '';
            case 'city': return self.tempFilter.city().trim() !== '';
            case 'taxNumber': return self.tempFilter.taxNumber().trim() !== '';
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
            case 'companyName':
                var companyName = self.tempFilter.companyName().trim();
                if (!companyName) return;
                filter.value = companyName;
                filter.displayValue = companyName;
                self.tempFilter.companyName('');
                break;

            case 'code':
                var code = self.tempFilter.code().trim();
                if (!code) return;
                filter.value = code;
                filter.displayValue = code;
                self.tempFilter.code('');
                break;

            case 'email':
                var email = self.tempFilter.email().trim();
                if (!email) return;
                filter.value = email;
                filter.displayValue = email;
                self.tempFilter.email('');
                break;

            case 'city':
                var city = self.tempFilter.city().trim();
                if (!city) return;
                filter.value = city;
                filter.displayValue = city;
                self.tempFilter.city('');
                break;

            case 'taxNumber':
                var taxNumber = self.tempFilter.taxNumber().trim();
                if (!taxNumber) return;
                filter.value = taxNumber;
                filter.displayValue = taxNumber;
                self.tempFilter.taxNumber('');
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
        self.loadCustomers();
    };

    // Remove filter
    self.removeFilter = function(filter) {
        self.activeFilters.remove(filter);
        self.currentPage(1);
        self.loadCustomers();
    };

    // Clear all filters
    self.clearFilters = function() {
        self.activeFilters([]);
        self.searchText('');
        self.currentPage(1);
        self.loadCustomers();
    };

    // Build filter params for API (çoklu değer desteği)
    self.buildFilterParams = function() {
        var companyNames = [];
        var codes = [];
        var emails = [];
        var cities = [];
        var taxNumbers = [];
        var isActiveFilter = null;

        self.activeFilters().forEach(function(filter) {
            switch (filter.type) {
                case 'companyName':
                    companyNames.push(filter.value);
                    break;
                case 'code':
                    codes.push(filter.value);
                    break;
                case 'email':
                    emails.push(filter.value);
                    break;
                case 'city':
                    cities.push(filter.value);
                    break;
                case 'taxNumber':
                    taxNumbers.push(filter.value);
                    break;
                case 'isActive':
                    isActiveFilter = filter.value;
                    break;
            }
        });

        var params = new URLSearchParams();

        // Çoklu değerler (array olarak gönderilir)
        companyNames.forEach(function(name) {
            params.append('companyNames', name);
        });
        codes.forEach(function(code) {
            params.append('codes', code);
        });
        emails.forEach(function(email) {
            params.append('emails', email);
        });
        cities.forEach(function(city) {
            params.append('cities', city);
        });
        taxNumbers.forEach(function(taxNumber) {
            params.append('taxNumbers', taxNumber);
        });

        // Durum filtresi (tekil)
        if (isActiveFilter !== null) {
            params.append('isActive', isActiveFilter);
        }

        // Global arama
        var searchText = self.searchText();
        if (searchText) {
            params.append('searchTerm', searchText);
        }

        // Include inactive toggle
        params.append('includeInactive', self.showInactive());

        // Pagination
        params.append('page', self.currentPage());
        params.append('pageSize', self.pageSize());

        // Sorting
        params.append('sortBy', self.sorting.sortBy() || 'companyName');
        params.append('sortDirection', self.sorting.sortDirection() || 'asc');

        return params.toString();
    };

    // Sorting
    self.sorting = TableSorting.createSortState('companyName', 'asc');

    // Pagination
    self.currentPage = ko.observable(1);
    self.pageSize = ko.observable(20);

    // Modal
    self.isModalOpen = ko.observable(false);
    self.editingCustomer = ko.observable(null);
    self.modalErrorMessage = ko.observable('');

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
    self.personnelModalErrorMessage = ko.observable('');
    
    // Reset Password (Admin)
    self.showChangePasswordModal = ko.observable(false);
    self.selectedPersonnelForPassword = ko.observable(null);
    self.isResettingPassword = ko.observable(false);
    self.newPassword = ko.observable('');
    self.confirmPassword = ko.observable('');
    self.passwordModalErrorMessage = ko.observable('');

    // Notification Settings (dropdown options)
    self.notificationFrequencies = ko.observableArray([]);
    self.notificationTemplates = ko.observableArray([]);

    // Bildirim kuralları için yardımcı veriler (EnumsService'den dolduruluyor, init'te)
    self.ruleFrequencies = ko.observableArray([]);

    self.daysOfWeek = ko.observableArray([]);

    self.daysOfMonth = (function() {
        var arr = [];
        for (var i = 1; i <= 28; i++) arr.push({ id: i, name: String(i) });
        return arr;
    })();

    // Kural objesi oluştur (observable alanlarla)
    self.createRuleObservable = function(rule) {
        return {
            id: ko.observable(rule ? rule.id : 0),
            frequencyId: ko.observable(rule ? String(rule.frequencyId) : '1'),
            dayOfWeek: ko.observable(rule && rule.dayOfWeek ? String(rule.dayOfWeek) : null),
            dayOfMonth: ko.observable(rule && rule.dayOfMonth ? String(rule.dayOfMonth) : null),
            emails: ko.observable(rule ? (rule.emails || '') : ''),
            sendToPersonnel: ko.observable(rule ? rule.sendToPersonnel : false),
            emailTemplateId: ko.observable(rule && rule.emailTemplateId ? String(rule.emailTemplateId) : null),
            tokenExpirationDays: ko.observable(rule ? (rule.tokenExpirationDays || 30) : 30),
            isActive: ko.observable(rule ? rule.isActive : true)
        };
    };

    // Kural ekle
    self.addNotificationRule = function() {
        var customer = self.editingCustomer();
        if (customer && customer.notificationRules) {
            customer.notificationRules.push(self.createRuleObservable());
        }
    };

    // Kural sil
    self.removeNotificationRule = function(rule) {
        var customer = self.editingCustomer();
        if (customer && customer.notificationRules) {
            customer.notificationRules.remove(rule);
        }
    };

    // Show personnel actions (hide when password reset is open)
    self.showPersonnelActions = ko.computed(function() {
        return !self.showChangePasswordModal();
    });

    // Show personnel loading
    self.showPersonnelLoading = ko.computed(function() {
        return self.isLoadingPersonnel() && !self.showChangePasswordModal();
    });

    // Show personnel table
    self.showPersonnelTable = ko.computed(function() {
        return !self.isLoadingPersonnel() && !self.showChangePasswordModal();
    });

    // Server-side filtered customers (artık filtreleme server'da yapılıyor)
    // allCustomers zaten server'dan filtrelenmiş veri geliyor

    // Backwards compatibility - sortedCustomers direkt allCustomers'ı kullanır
    self.sortedCustomers = ko.computed(function() {
        return self.allCustomers();
    });

    // Backwards compatibility
    self.customers = self.allCustomers;

    // Pagination - server'dan gelen değerler
    self.totalCount = ko.observable(0);

    self.totalPages = ko.computed(function() {
        return Math.ceil(self.totalCount() / parseInt(self.pageSize(), 10)) || 1;
    });

    // Reset to page 1 and reload when pageSize changes
    self.pageSize.subscribe(function() {
        self.currentPage(1);
        self.loadCustomers();
    });

    // Search with debounce
    var searchTimeout = null;
    self.searchText.subscribe(function() {
        clearTimeout(searchTimeout);
        searchTimeout = setTimeout(function() {
            self.currentPage(1);
            self.loadCustomers();
        }, 300);
    });

    // Reload when showInactive changes
    self.showInactive.subscribe(function() {
        self.currentPage(1);
        self.loadCustomers();
    });

    // Reload when sorting changes
    self.sorting.sortBy.subscribe(function() {
        self.loadCustomers();
    });
    self.sorting.sortDirection.subscribe(function() {
        self.loadCustomers();
    });

    self.goToPage = function(page) {
        if (page >= 1 && page <= self.totalPages()) {
            self.currentPage(page);
            self.loadCustomers();
        }
    };

    self.previousPage = function() {
        if (self.currentPage() > 1) {
            self.currentPage(self.currentPage() - 1);
            self.loadCustomers();
        }
    };

    self.nextPage = function() {
        if (self.currentPage() < self.totalPages()) {
            self.currentPage(self.currentPage() + 1);
            self.loadCustomers();
        }
    };

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

    // Date input için YYYY-MM-DD formatı
    self.formatDateForInput = function(dateString) {
        if (!dateString) return '';
        var date = new Date(dateString);
        if (isNaN(date.getTime())) return '';
        var year = date.getFullYear();
        var month = ('0' + (date.getMonth() + 1)).slice(-2);
        var day = ('0' + date.getDate()).slice(-2);
        return year + '-' + month + '-' + day;
    };

    // Load customers (server-side filtering)
    self.loadCustomers = function() {
        self.isLoading(true);
        self.errorMessage('');

        var queryString = self.buildFilterParams();

        ApiService.get('/customers?' + queryString)
            .then(function(result) {
                // Server'dan PagedCustomerResult formatında veri geliyor
                self.allCustomers(result.items || []);
                self.totalCount(result.totalCount || 0);
            })
            .catch(function(error) {
                console.error('Error loading customers:', error);
                toastr.error(T('Customer.LoadError', 'Müşteriler yüklenirken bir hata oluştu.'));
            })
            .finally(function() {
                self.isLoading(false);
            });
    };

    // Create new customer
    self.createNew = function() {        self.editingCustomer({
            id: null,
            code: '',
            companyName: '',
            taxNumber: '',
            phone: '',
            email: '',
            address: '',
            city: '',
            isActive: true,
            contractStartDate: null,
            contractEndDate: null,
            notes: '',
            targetCount: null,
            dailyQuota: null,
            weeklyQuota: null,
            monthlyQuota: null,
            evaluationNotificationFrequencyId: 0,
            evaluationNotificationTemplateId: null,
            notificationEmails: '',
            notificationRules: ko.observableArray([])
        });
        self.isModalOpen(true);
    };

    // Edit customer (fetch full detail for notification rules)
    self.editCustomer = function(customer) {
        customerApiService.getCustomerById(customer.id)
            .then(function(detail) {
                var rules = (detail.notificationRules || []).map(function(r) {
                    return self.createRuleObservable(r);
                });

                self.editingCustomer({
                    id: detail.id,
                    code: detail.code || '',
                    companyName: detail.companyName,
                    taxNumber: detail.taxNumber || '',
                    phone: detail.phone || '',
                    email: detail.email || '',
                    address: detail.address || '',
                    city: detail.city || '',
                    isActive: detail.isActive,
                    contractStartDate: self.formatDateForInput(detail.contractStartDate),
                    contractEndDate: self.formatDateForInput(detail.contractEndDate),
                    notes: detail.notes || '',
                    targetCount: detail.targetCount || null,
                    dailyQuota: detail.dailyQuota || null,
                    weeklyQuota: detail.weeklyQuota || null,
                    monthlyQuota: detail.monthlyQuota || null,
                    evaluationNotificationFrequencyId: detail.evaluationNotificationFrequencyId || 0,
                    evaluationNotificationTemplateId: detail.evaluationNotificationTemplateId || null,
                    notificationEmails: detail.notificationEmails || '',
                    notificationRules: ko.observableArray(rules)
                });
                self.isModalOpen(true);
            })
            .catch(function(error) {
                console.error('Error loading customer details:', error);
                toastr.error('Müşteri detayları yüklenirken hata oluştu.');
            });
    };

    // Save customer
    self.saveCustomer = function() {        self.successMessage('');

        var customer = self.editingCustomer();
        if (!customer) return;

        // Validation
        if (!customer.companyName) {
            toastr.error(T('Customer.CompanyNameRequired', 'Şirket adı zorunludur.'));
            return;
        }

        self.isSaving(true);

        // Prepare data - unwrap notificationRules observables for JSON serialization
        var customerData = {};
        for (var key in customer) {
            if (key !== 'notificationRules' && customer.hasOwnProperty(key)) {
                customerData[key] = customer[key];
            }
        }
        if (customer.notificationRules) {
            customerData.notificationRules = customer.notificationRules().map(function(rule) {
                return {
                    id: ko.unwrap(rule.id) || 0,
                    frequencyId: parseInt(ko.unwrap(rule.frequencyId), 10),
                    dayOfWeek: ko.unwrap(rule.dayOfWeek) ? parseInt(ko.unwrap(rule.dayOfWeek), 10) : null,
                    dayOfMonth: ko.unwrap(rule.dayOfMonth) ? parseInt(ko.unwrap(rule.dayOfMonth), 10) : null,
                    emails: ko.unwrap(rule.emails) || null,
                    sendToPersonnel: ko.unwrap(rule.sendToPersonnel) || false,
                    emailTemplateId: ko.unwrap(rule.emailTemplateId) ? parseInt(ko.unwrap(rule.emailTemplateId), 10) : null,
                    tokenExpirationDays: parseInt(ko.unwrap(rule.tokenExpirationDays), 10) || 30,
                    isActive: ko.unwrap(rule.isActive)
                };
            });
        }

        var promise = customer.id
            ? customerApiService.updateCustomer(customer.id, customerData)
            : customerApiService.createCustomer(customerData);

        promise
            .then(function(savedCustomer) {
                var isNew = !customer.id;
                if (isNew) {
                    // Yeni kayıt: array'e ekle
                    self.allCustomers.push(savedCustomer);
                } else {
                    // Güncelleme: array'de bul ve güncelle
                    var list = self.allCustomers();
                    for (var i = 0; i < list.length; i++) {
                        if (list[i].id === savedCustomer.id) {
                            self.allCustomers.splice(i, 1, savedCustomer);
                            break;
                        }
                    }
                }
                toastr.success(isNew ? T('Customer.SaveSuccess', 'Müşteri başarıyla oluşturuldu.') : T('Customer.UpdateSuccess', 'Müşteri başarıyla güncellendi.'));
                self.isModalOpen(false);
            })
            .catch(function(error) {
                console.error('Error saving customer:', error);
                toastr.error(T('Customer.SaveError', 'Müşteri kaydedilirken bir hata oluştu.') + ' ' + (error.message || ''));
            })
            .finally(function() {
                self.isSaving(false);
            });
    };

    // Close modal
    self.closeModal = function() {
        self.isModalOpen(false);
        self.editingCustomer(null);    };

    // Delete customer
    self.deleteCustomer = function(customer) {
        deleteConfirmation.show(
            '"' + customer.companyName + '" ' + T('Customer.DeleteConfirm', 'Bu müşteriyi silmek istediğinizden emin misiniz?'),
            function() {
                customerApiService.deleteCustomer(customer.id)
                    .then(function() {
                        // Array'den sil
                        self.allCustomers.remove(function(c) { return c.id === customer.id; });
                        toastr.success(T('Customer.DeleteSuccess', 'Müşteri başarıyla silindi.'));
                    })
                    .catch(function(error) {
                        console.error('Error deleting customer:', error);
                        toastr.error(T('Customer.DeleteError', 'Müşteri silinirken bir hata oluştu.'));
                    });
            }
        );
    };

    // ========== PERSONNEL MANAGEMENT ==========
    
    // Show personnel popup
    self.showPersonnel = function(customer) {
        var url = '/Customers/Personnel/' + customer.id;
        var popup = window.open(url, 'personnel_' + customer.id, 'width=1100,height=700,scrollbars=yes,resizable=yes');
        if (popup) popup.focus();
    };

    // Show dealers popup
    self.showDealers = function(customer) {
        var url = '/Customers/Dealers/' + customer.id;
        var popup = window.open(url, 'dealers_' + customer.id, 'width=1200,height=750,scrollbars=yes,resizable=yes');
        if (popup) popup.focus();
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
                toastr.error(T('Personnel.LoadError', 'Personeller yüklenirken bir hata oluştu.'));
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

    // Export personnel to Excel
    self.exportPersonnelToExcel = function() {
        var customer = self.selectedCustomerForPersonnel();
        if (!customer) return;

        // Open download in new tab
        window.open('/api/customers/' + customer.id + '/personnel/export/excel', '_blank');
    };

    // Create new personnel
    self.createNewPersonnel = function() {
        var customer = self.selectedCustomerForPersonnel();
        if (!customer) return;

        self.personnelModalErrorMessage('');
        self.editingPersonnel({
            id: ko.observable(null),
            customerId: ko.observable(customer.id),
            username: ko.observable(''),
            email: ko.observable(''),
            password: ko.observable(''),
            firstName: ko.observable(''),
            lastName: ko.observable(''),
            phoneNumber: ko.observable(''),
            department: ko.observable(''),
            title: ko.observable(''),
            role: ko.observable('3'), // Default to operator
            isActive: ko.observable(true)
        });
        self.showPersonnelFormModal(true);
    };

    // Edit personnel
    self.editPersonnel = function(personnel) {
        self.personnelModalErrorMessage('');
        self.editingPersonnel({
            id: ko.observable(personnel.id),
            customerId: ko.observable(personnel.customerId),
            username: ko.observable(personnel.username),
            email: ko.observable(personnel.email),
            password: ko.observable(''),
            firstName: ko.observable(personnel.firstName),
            lastName: ko.observable(personnel.lastName),
            phoneNumber: ko.observable(personnel.phoneNumber || ''),
            department: ko.observable(personnel.department || ''),
            title: ko.observable(personnel.title || ''),
            role: ko.observable(String(personnel.role)), // Convert to string for select binding
            isActive: ko.observable(personnel.isActive)
        });
        self.showPersonnelFormModal(true);
    };

    // Save personnel
    self.savePersonnel = function() {
        self.personnelModalErrorMessage('');
        self.successMessage('');

        var personnel = self.editingPersonnel();
        if (!personnel) return;

        // Unwrap observables
        var id = ko.unwrap(personnel.id);
        var username = ko.unwrap(personnel.username);
        var email = ko.unwrap(personnel.email);
        var password = ko.unwrap(personnel.password);
        var firstName = ko.unwrap(personnel.firstName);
        var lastName = ko.unwrap(personnel.lastName);

        // Validation
        if (!username || !email || !firstName || !lastName) {
            toastr.warning(T('Personnel.RequiredFields', 'Kullanıcı adı, e-posta, ad ve soyad zorunludur.'));
            return;
        }

        if (!id && !password) {
            toastr.warning(T('Personnel.PasswordRequired', 'Yeni personel için şifre zorunludur.'));
            return;
        }

        self.isSavingPersonnel(true);

        // Prepare data - convert empty password to null and role to integer
        var data = {
            customerId: ko.unwrap(personnel.customerId),
            username: username,
            email: email,
            password: password || null,
            firstName: firstName,
            lastName: lastName,
            phoneNumber: ko.unwrap(personnel.phoneNumber) || null,
            department: ko.unwrap(personnel.department) || null,
            title: ko.unwrap(personnel.title) || null,
            role: String(ko.unwrap(personnel.role)),
            isActive: ko.unwrap(personnel.isActive)
        };

        var promise = id
            ? customerApiService.updatePersonnel(id, data)
            : customerApiService.createPersonnel(data);

        promise
            .then(function(response) {
                var isNew = !id;
                toastr.success(isNew ? T('Personnel.SaveSuccess', 'Personel başarıyla oluşturuldu.') : T('Personnel.UpdateSuccess', 'Personel başarıyla güncellendi.'));
                self.showPersonnelFormModal(false);

                if (isNew) {
                    // Yeni: ID'yi al, nesneyi oluştur, listeye ekle
                    var newItem = {
                        id: response.id || response,
                        customerId: data.customerId,
                        username: data.username,
                        email: data.email,
                        firstName: data.firstName,
                        lastName: data.lastName,
                        fullName: data.firstName + ' ' + data.lastName,
                        phoneNumber: data.phoneNumber,
                        department: data.department,
                        title: data.title,
                        role: data.role,
                        isActive: data.isActive
                    };
                    self.personnel.push(newItem);
                } else {
                    // Güncelleme: mevcut kaydı bul ve güncelle
                    var list = self.personnel();
                    for (var j = 0; j < list.length; j++) {
                        if (list[j].id === id) {
                            list[j].username = data.username;
                            list[j].email = data.email;
                            list[j].firstName = data.firstName;
                            list[j].lastName = data.lastName;
                            list[j].fullName = data.firstName + ' ' + data.lastName;
                            list[j].phoneNumber = data.phoneNumber;
                            list[j].department = data.department;
                            list[j].title = data.title;
                            list[j].role = data.role;
                            list[j].isActive = data.isActive;
                            self.personnel.valueHasMutated();
                            break;
                        }
                    }
                }
            })
            .catch(function(error) {
                console.error('Error saving personnel:', error);
                toastr.error(T('Personnel.SaveError', 'Personel kaydedilirken bir hata oluştu.'));
            })
            .finally(function() {
                self.isSavingPersonnel(false);
            });
    };

    // Close personnel form modal
    self.closePersonnelFormModal = function() {
        self.showPersonnelFormModal(false);
        self.editingPersonnel(null);
        self.personnelModalErrorMessage('');
    };

    // Delete personnel
    self.deletePersonnel = function(personnel) {
        deleteConfirmation.show(
            '"' + personnel.fullName + '" ' + T('Personnel.DeleteConfirm', 'Bu personeli silmek istediğinizden emin misiniz?'),
            function() {
                customerApiService.deletePersonnel(personnel.id)
                    .then(function() {
                        // Listeden sil (loadPersonnel yerine)
                        self.personnel.remove(function(p) { return p.id === personnel.id; });
                        toastr.success(T('Personnel.DeleteSuccess', 'Personel başarıyla silindi.'));
                    })
                    .catch(function(error) {
                        console.error('Error deleting personnel:', error);
                        toastr.error(T('Personnel.DeleteError', 'Personel silinirken bir hata oluştu.') + ' ' + (error.message || ''));
                    });
            }
        );
    };

    // Show reset password modal (Admin)
    self.showChangePasswordForPersonnel = function(personnel) {
        self.passwordModalErrorMessage('');
        self.selectedPersonnelForPassword(personnel);
        self.newPassword('');
        self.confirmPassword('');
        self.showChangePasswordModal(true);
    };

    // Reset password (Admin)
    self.resetPassword = function() {
        self.passwordModalErrorMessage('');
        self.successMessage('');

        var newPass = self.newPassword();
        var confirmPass = self.confirmPassword();

        if (!newPass || !confirmPass) {
            self.passwordModalErrorMessage(T('Common.AllFieldsRequired', 'Tüm alanlar zorunludur.'));
            return;
        }

        if (newPass.length < 6) {
            self.passwordModalErrorMessage(T('Password.MinLength', 'Yeni şifre en az 6 karakter olmalıdır.'));
            return;
        }

        if (newPass !== confirmPass) {
            self.passwordModalErrorMessage(T('Password.Mismatch', 'Yeni şifre ve onay eşleşmiyor.'));
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
                self.passwordModalErrorMessage(T('Password.ResetError', 'Şifre sıfırlanırken bir hata oluştu.') + ' ' + (error.message || ''));
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
        self.passwordModalErrorMessage('');
    };

    // Toggle inactive customers
    self.toggleShowInactive = function() {
        self.showInactive(!self.showInactive());
        // No need to reload - client-side filtering handles it
        // showInactive.subscribe already resets currentPage
    };

    // ========== ORGANIZATION MANAGEMENT ==========

    // Organization Modal State
    self.showOrganizationModal = ko.observable(false);
    self.selectedCustomerForOrg = ko.observable(null);
    self.organizations = ko.observableArray([]);
    self.selectedOrganization = ko.observable(null);
    self.isLoadingOrganizations = ko.observable(false);
    self.orgModalErrorMessage = ko.observable('');
    self.orgModalSuccessMessage = ko.observable('');
    self.orgSearchText = ko.observable('');

    // Filtered organizations based on search
    self.filteredOrganizations = ko.computed(function() {
        var search = (self.orgSearchText() || '').toLowerCase().trim();
        var orgs = self.organizations();

        if (!search) return orgs;

        return orgs.filter(function(org) {
            return (org.name && org.name.toLowerCase().indexOf(search) >= 0) ||
                   (org.code && org.code.toLowerCase().indexOf(search) >= 0);
        });
    });

    // Organization Form Modal
    self.showOrgFormModal = ko.observable(false);
    self.editingOrganization = ko.observable(null);
    self.isSavingOrg = ko.observable(false);

    // Personnel in Organization
    self.orgPersonnelList = ko.observable({ supervisors: [], operators: [] });
    self.isLoadingOrgPersonnel = ko.observable(false);
    self.personnelPool = ko.observableArray([]);
    self.selectedPoolPersonnelId = ko.observable(null);

    // Personnel Pool for Supervisor (filter out operators and already assigned)
    self.personnelPoolForSupervisor = ko.computed(function() {
        var pool = self.personnelPool();
        var selectedOrg = self.selectedOrganization();
        if (!selectedOrg) return [];

        // Filter: only managers and supervisors, not already in this organization
        return pool.filter(function(p) {
            // Role 1 = Manager, Role 2 = Supervisor
            return (p.role === 1 || p.role === 2) && p.organizationId !== selectedOrg.id;
        });
    });

    // Personnel Pool for Operator (only operators not in this organization)
    self.selectedPoolOperatorId = ko.observable(null);
    self.personnelPoolForOperator = ko.computed(function() {
        var pool = self.personnelPool();
        var selectedOrg = self.selectedOrganization();
        if (!selectedOrg) return [];

        // Filter: only operators, not already in this organization
        return pool.filter(function(p) {
            // Role 3 = Operator
            return p.role === 3 && p.organizationId !== selectedOrg.id;
        });
    });

    // Firma Yöneticileri (CustomerManager)
    self.customerManagers = ko.observableArray([]);
    self.showNewManagerForm = ko.observable(false);
    self.newManager = ko.observable({
        firstName: ko.observable(''),
        lastName: ko.observable(''),
        username: ko.observable(''),
        email: ko.observable(''),
        password: ko.observable('')
    });
    self.isSavingNewManager = ko.observable(false);

    // Inline New Supervisor Form
    self.showNewSupervisorForm = ko.observable(false);
    self.newSupervisor = ko.observable({
        firstName: ko.observable(''),
        lastName: ko.observable(''),
        username: ko.observable(''),
        email: ko.observable(''),
        password: ko.observable('')
    });
    self.isSavingNewSupervisor = ko.observable(false);

    // Add Operator Modal
    self.showAddOperatorModal = ko.observable(false);
    self.selectedSupervisorForOperator = ko.observable(null);
    self.newOperator = ko.observable({
        firstName: ko.observable(''),
        lastName: ko.observable(''),
        username: ko.observable(''),
        email: ko.observable(''),
        password: ko.observable('')
    });
    self.isSavingOperator = ko.observable(false);

    // Delegate Modal (for removing supervisor with team members)
    self.showDelegateModal = ko.observable(false);
    self.personnelToRemove = ko.observable(null);
    self.availableDelegates = ko.observableArray([]);
    self.selectedDelegateId = ko.observable(null);
    self.isRemovingWithDelegate = ko.observable(false);

    // Show organizations popup
    self.showOrganizations = function(customer) {
        var url = '/Customers/Organizations/' + customer.id;
        var popup = window.open(url, 'organizations_' + customer.id, 'width=1400,height=850,scrollbars=yes,resizable=yes');
        if (popup) popup.focus();
    };

    // Load customer managers (role = 1)
    self.loadCustomerManagers = function(customerId) {
        self.customerManagers([]);
        // Filter from pool (role 1 = CustomerManager)
        ApiService.get('/customer-organizations/personnel-pool/' + customerId)
            .then(function(data) {
                var managers = (data || []).filter(function(p) { return p.role === 1; });
                self.customerManagers(managers);
            });
    };

    // Toggle new manager form
    self.toggleNewManagerForm = function() {
        if (self.showNewManagerForm()) {
            self.showNewManagerForm(false);
        } else {
            self.newManager({
                firstName: ko.observable(''),
                lastName: ko.observable(''),
                username: ko.observable(''),
                email: ko.observable(''),
                password: ko.observable('')
            });
            self.showNewManagerForm(true);
        }
    };

    // Save new manager
    self.saveNewManager = function() {
        var mgr = self.newManager();
        var customer = self.selectedCustomerForOrg();
        if (!customer) return;

        var firstName = ko.unwrap(mgr.firstName);
        var lastName = ko.unwrap(mgr.lastName);
        var username = ko.unwrap(mgr.username);
        var email = ko.unwrap(mgr.email);
        var password = ko.unwrap(mgr.password);

        if (!firstName || !lastName || !username || !email || !password) {
            toastr.error(T('Common.AllFieldsRequired', 'Tüm alanları doldurun.'));
            return;
        }

        // Kullanıcı adı formatı kontrolü (sadece İngilizce harf, rakam, alt çizgi, nokta, tire)
        var usernameRegex = /^[a-zA-Z0-9_.-]+$/;
        if (!usernameRegex.test(username)) {
            toastr.error(T('User.UsernameInvalid', 'Kullanıcı adı sadece İngilizce harf, rakam, alt çizgi, nokta ve tire içerebilir. Boşluk ve Türkçe karakter kullanılamaz.'));
            return;
        }

        // Şifre uzunluğu kontrolü
        if (password.length < 6) {
            toastr.error(T('Validation.PasswordMinLength', 'Şifre en az 6 karakter olmalıdır.'));
            return;
        }

        self.isSavingNewManager(true);        customerApiService.createPersonnel({
            customerId: customer.id,
            firstName: firstName,
            lastName: lastName,
            username: username,
            email: email,
            password: password,
            role: '1', // CustomerManager
            isActive: true
        })
        .then(function() {
            toastr.success(T('Personnel.ManagerCreated', 'Firma yöneticisi oluşturuldu.'));
            self.showNewManagerForm(false);
            self.loadCustomerManagers(customer.id);
            self.loadPersonnelPool(customer.id);
        })
        .catch(function(error) {
            console.error('Error creating manager:', error);
            // Validation hatalarını detaylı göster
            var errorMsg = '';
            if (error.errors) {
                var errorDetails = [];
                for (var field in error.errors) {
                    errorDetails.push(field + ': ' + error.errors[field].join(', '));
                }
                errorMsg = errorDetails.join('; ');
            } else if (error.validationErrors) {
                var errorDetails = [];
                for (var field in error.validationErrors) {
                    errorDetails.push(field + ': ' + error.validationErrors[field].join(', '));
                }
                errorMsg = errorDetails.join('; ');
            } else {
                errorMsg = error.message || error.title || JSON.stringify(error);
            }
            toastr.error(T('Personnel.CreateError', 'Yönetici oluşturulurken bir hata oluştu.') + ' ' + errorMsg);
        })
        .finally(function() {
            self.isSavingNewManager(false);
        });
    };

    // Remove manager
    self.removeManager = function(manager) {
        deleteConfirmation.show(
            '"' + manager.fullName + '" yöneticisini silmek istediğinizden emin misiniz?',
            function() {
                customerApiService.deletePersonnel(manager.id)
                    .then(function() {
                        toastr.success('Yönetici silindi.');
                        self.loadCustomerManagers(self.selectedCustomerForOrg().id);
                        self.loadPersonnelPool(self.selectedCustomerForOrg().id);
                    })
                    .catch(function(error) {
                        toastr.error('Yönetici silinirken hata: ' + (error.message || ''));
                    });
            }
        );
    };

    // Close organizations modal
    self.closeOrganizationModal = function() {
        // Modalı kapatmadan önce müşteri sayılarını güncelle (loadCustomers yerine)
        var customer = self.selectedCustomerForOrg();
        if (customer) {
            var orgCount = self.organizations().length;
            var customers = self.allCustomers();
            for (var i = 0; i < customers.length; i++) {
                if (customers[i].id === customer.id) {
                    customers[i].organizationCount = orgCount;
                    self.allCustomers.valueHasMutated();
                    break;
                }
            }
        }
        self.showOrganizationModal(false);
        self.selectedCustomerForOrg(null);
        self.organizations([]);
        self.selectedOrganization(null);
    };

    // Load organizations for customer
    self.loadOrganizations = function(customerId) {
        self.isLoadingOrganizations(true);        ApiService.get('/customer-organizations/by-customer/' + customerId)
            .then(function(data) {
                self.organizations(data || []);
            })
            .catch(function(error) {
                console.error('Error loading organizations:', error);
                toastr.error(T('Organization.LoadError', 'Organizasyonlar yüklenirken bir hata oluştu.'));
            })
            .finally(function() {
                self.isLoadingOrganizations(false);
            });
    };

    // Load personnel pool for customer
    self.loadPersonnelPool = function(customerId) {
        ApiService.get('/customer-organizations/personnel-pool/' + customerId)
            .then(function(data) {
                self.personnelPool(data || []);
            })
            .catch(function(error) {
                console.error('Error loading personnel pool:', error);
            });
    };

    // Select organization
    self.selectOrganization = function(org) {
        self.selectedOrganization(org);
        self.selectedPoolPersonnelId(null);
        self.loadOrgPersonnel(org.id);
    };

    // Load personnel for organization
    self.loadOrgPersonnel = function(organizationId) {
        self.isLoadingOrgPersonnel(true);

        ApiService.get('/customer-organizations/' + organizationId + '/personnel')
            .then(function(data) {
                self.orgPersonnelList(data || { supervisors: [], operators: [] });
            })
            .catch(function(error) {
                console.error('Error loading organization personnel:', error);
                toastr.error(T('Personnel.LoadError', 'Personeller yüklenirken bir hata oluştu.'));
            })
            .finally(function() {
                self.isLoadingOrgPersonnel(false);
            });
    };

    // Create new organization
    self.createNewOrganization = function() {
        var customer = self.selectedCustomerForOrg();
        if (!customer) return;

        self.editingOrganization({
            id: null,
            name: ko.observable(''),
            code: ko.observable(''),
            description: ko.observable(''),
            isActive: ko.observable(true),
            customerId: customer.id
        });
        self.showOrgFormModal(true);
    };

    // Edit organization
    self.editOrganization = function(org) {
        self.editingOrganization({
            id: org.id,
            name: ko.observable(org.name),
            code: ko.observable(org.code || ''),
            description: ko.observable(org.description || ''),
            isActive: ko.observable(org.isActive),
            customerId: org.customerId
        });
        self.showOrgFormModal(true);
    };

    // Close organization form modal
    self.closeOrgFormModal = function() {
        self.showOrgFormModal(false);
        self.editingOrganization(null);
    };

    // Save organization
    self.saveOrganization = function() {
        var org = self.editingOrganization();
        if (!org) return;

        var name = ko.unwrap(org.name);
        if (!name) {
            toastr.error(T('Organization.NameRequired', 'Organizasyon adı zorunludur.'));
            return;
        }

        self.isSavingOrg(true);        var data = {
            name: name,
            code: ko.unwrap(org.code),
            description: ko.unwrap(org.description),
            isActive: ko.unwrap(org.isActive),
            customerId: org.customerId
        };

        var promise;
        if (org.id) {
            promise = ApiService.put('/customer-organizations/' + org.id, data);
        } else {
            promise = ApiService.post('/customer-organizations', data);
        }

        promise
            .then(function(savedOrg) {
                var isNew = !org.id;
                if (isNew) {
                    // Yeni kayıt: array'e ekle
                    self.organizations.push(savedOrg);
                } else {
                    // Güncelleme: array'de bul ve güncelle
                    var list = self.organizations();
                    for (var i = 0; i < list.length; i++) {
                        if (list[i].id === savedOrg.id) {
                            self.organizations.splice(i, 1, savedOrg);
                            break;
                        }
                    }
                }
                toastr.success(isNew ? T('Organization.CreateSuccess', 'Organizasyon oluşturuldu.') : T('Organization.UpdateSuccess', 'Organizasyon güncellendi.'));
                self.closeOrgFormModal();
            })
            .catch(function(error) {
                console.error('Error saving organization:', error);
                toastr.error(T('Organization.SaveError', 'Organizasyon kaydedilirken bir hata oluştu.') + ' ' + (error.message || ''));
            })
            .finally(function() {
                self.isSavingOrg(false);
            });
    };

    // Delete organization
    self.deleteOrganization = function(org) {
        deleteConfirmation.show(
            '"' + org.name + '" ' + T('Organization.DeleteConfirm', 'organizasyonunu silmek istediğinizden emin misiniz?'),
            function() {
                ApiService.delete('/customer-organizations/' + org.id)
                    .then(function() {
                        // Array'den sil
                        self.organizations.remove(function(o) { return o.id === org.id; });
                        toastr.success(T('Organization.DeleteSuccess', 'Organizasyon silindi.'));
                        if (self.selectedOrganization() && self.selectedOrganization().id === org.id) {
                            self.selectedOrganization(null);
                            self.orgPersonnelList({ supervisors: [], operators: [] });
                        }
                    })
                    .catch(function(error) {
                        console.error('Error deleting organization:', error);
                        toastr.error(T('Organization.DeleteError', 'Organizasyon silinirken bir hata oluştu.') + ' ' + (error.message || ''));
                    });
            }
        );
    };

    // Assign pool personnel to organization (Supervisor/Manager)
    self.assignPoolPersonnelToOrg = function() {
        var personnelId = self.selectedPoolPersonnelId();
        var org = self.selectedOrganization();
        if (!personnelId || !org) return;

        ApiService.post('/customer-organizations/assign-personnel', {
            personnelId: personnelId,
            organizationId: org.id
        })
        .then(function() {
            toastr.success(T('Personnel.AssignSuccess', 'Personel organizasyona atandı.'));
            self.selectedPoolPersonnelId(null);
            self.loadOrgPersonnel(org.id);
            self.loadOrganizations(self.selectedCustomerForOrg().id);
            self.loadPersonnelPool(self.selectedCustomerForOrg().id);
        })
        .catch(function(error) {
            console.error('Error assigning personnel:', error);
            toastr.error(T('Personnel.AssignError', 'Personel atanırken bir hata oluştu.') + ' ' + (error.message || ''));
        });
    };

    // Assign pool operator to organization (Independent Operator)
    self.assignPoolOperatorToOrg = function() {
        var personnelId = self.selectedPoolOperatorId();
        var org = self.selectedOrganization();
        if (!personnelId || !org) return;

        ApiService.post('/customer-organizations/assign-personnel', {
            personnelId: personnelId,
            organizationId: org.id
        })
        .then(function() {
            toastr.success(T('Personnel.OperatorAssignSuccess', 'Operatör organizasyona atandı.'));
            self.selectedPoolOperatorId(null);
            self.loadOrgPersonnel(org.id);
            self.loadOrganizations(self.selectedCustomerForOrg().id);
            self.loadPersonnelPool(self.selectedCustomerForOrg().id);
        })
        .catch(function(error) {
            console.error('Error assigning operator:', error);
            toastr.error(T('Personnel.AssignError', 'Operatör atanırken bir hata oluştu.') + ' ' + (error.message || ''));
        });
    };

    // Edit organization personnel (opens form modal)
    self.editOrgPersonnel = function(personnel) {        self.editingPersonnel({
            id: ko.observable(personnel.id),
            customerId: ko.observable(self.selectedCustomerForOrg().id),
            username: ko.observable(personnel.username),
            email: ko.observable(personnel.email),
            password: ko.observable(''),
            firstName: ko.observable(personnel.firstName),
            lastName: ko.observable(personnel.lastName),
            phoneNumber: ko.observable(personnel.phoneNumber || ''),
            department: ko.observable(personnel.department || ''),
            title: ko.observable(personnel.title || ''),
            role: ko.observable(String(personnel.role)), // Convert to string for select binding
            isActive: ko.observable(personnel.isActive !== false)
        });
        self.showPersonnelFormModal(true);
    };

    // Override savePersonnel to also refresh org data
    var originalSavePersonnel = self.savePersonnel;
    self.savePersonnel = function() {
        self.personnelModalErrorMessage('');
        self.successMessage('');

        var personnel = self.editingPersonnel();
        if (!personnel) return;

        // Unwrap observables
        var id = ko.unwrap(personnel.id);
        var username = ko.unwrap(personnel.username);
        var email = ko.unwrap(personnel.email);
        var password = ko.unwrap(personnel.password);
        var firstName = ko.unwrap(personnel.firstName);
        var lastName = ko.unwrap(personnel.lastName);

        // Validation
        if (!username || !email || !firstName || !lastName) {
            toastr.warning(T('Personnel.RequiredFields', 'Kullanıcı adı, e-posta, ad ve soyad zorunludur.'));
            return;
        }

        if (!id && !password) {
            toastr.warning(T('Personnel.PasswordRequired', 'Yeni personel için şifre zorunludur.'));
            return;
        }

        self.isSavingPersonnel(true);

        // Prepare data - convert empty password to null and role to integer
        var data = {
            customerId: ko.unwrap(personnel.customerId),
            username: username,
            email: email,
            password: password || null,
            firstName: firstName,
            lastName: lastName,
            phoneNumber: ko.unwrap(personnel.phoneNumber) || null,
            department: ko.unwrap(personnel.department) || null,
            title: ko.unwrap(personnel.title) || null,
            role: String(ko.unwrap(personnel.role)),
            isActive: ko.unwrap(personnel.isActive)
        };

        var promise = id
            ? customerApiService.updatePersonnel(id, data)
            : customerApiService.createPersonnel(data);

        promise
            .then(function(response) {
                var isNew = !id;
                toastr.success(isNew ? T('Personnel.SaveSuccess', 'Personel başarıyla oluşturuldu.') : T('Personnel.UpdateSuccess', 'Personel başarıyla güncellendi.'));
                self.showPersonnelFormModal(false);

                // Organization modalı açıksa, org personnel listesini güncelle
                if (self.showOrganizationModal() && self.selectedOrganization()) {
                    // OrgPersonnel güncelleme - yapısı karmaşık, sadece listeyi yenile
                    self.loadOrgPersonnel(self.selectedOrganization().id);
                } else if (self.selectedCustomerForPersonnel()) {
                    // Normal personnel modalı - local güncelle
                    if (isNew) {
                        var newItem = {
                            id: response.id || response,
                            customerId: data.customerId,
                            username: data.username,
                            email: data.email,
                            firstName: data.firstName,
                            lastName: data.lastName,
                            fullName: data.firstName + ' ' + data.lastName,
                            phoneNumber: data.phoneNumber,
                            department: data.department,
                            title: data.title,
                            role: data.role,
                            isActive: data.isActive
                        };
                        self.personnel.push(newItem);
                    } else {
                        var list = self.personnel();
                        for (var j = 0; j < list.length; j++) {
                            if (list[j].id === id) {
                                list[j].username = data.username;
                                list[j].email = data.email;
                                list[j].firstName = data.firstName;
                                list[j].lastName = data.lastName;
                                list[j].fullName = data.firstName + ' ' + data.lastName;
                                list[j].role = data.role;
                                list[j].isActive = data.isActive;
                                self.personnel.valueHasMutated();
                                break;
                            }
                        }
                    }
                }
            })
            .catch(function(error) {
                console.error('Error saving personnel:', error);
                toastr.error(T('Personnel.SaveError', 'Personel kaydedilirken bir hata oluştu.'));
            })
            .finally(function() {
                self.isSavingPersonnel(false);
            });
    };

    // Make operator independent (remove from supervisor but keep in organization)
    self.makeIndependent = function(personnel) {
        var org = self.selectedOrganization();
        if (!org) return;

        deleteConfirmation.show(
            '"' + personnel.fullName + '" ' + T('Customer.MakeIndependentConfirm', 'personelini bağımsız yapmak istediğinizden emin misiniz? Süpervizörden ayrılacak ama organizasyonda kalacak.'),
            function() {
                ApiService.put('/customer-organizations/personnel/' + personnel.id + '/supervisor', null)
                    .then(function() {
                        toastr.success(T('Customer.MakeIndependentSuccess', 'Personel bağımsız yapıldı.'));
                        self.loadOrgPersonnel(org.id);
                        self.loadOrganizations(self.selectedCustomerForOrg().id);
                        self.loadPersonnelPool(self.selectedCustomerForOrg().id);
                    })
                    .catch(function(error) {
                        console.error('Error making personnel independent:', error);
                        toastr.error(T('Customer.MakeIndependentError', 'Personel bağımsız yapılırken bir hata oluştu.') + ' ' + (error.message || ''));
                    });
            }
        );
    };

    // Remove personnel from organization
    self.removePersonnelFromOrg = function(personnel) {
        var org = self.selectedOrganization();
        if (!org) return;

        // Check if supervisor has team members
        if (personnel.teamMembers && personnel.teamMembers.length > 0) {
            // Show delegate modal
            self.personnelToRemove(personnel);

            // Get available delegates (other supervisors in same organization)
            var supervisors = self.orgPersonnelList().supervisors || [];
            var delegates = supervisors.filter(function(s) {
                return s.id !== personnel.id;
            });
            self.availableDelegates(delegates);
            self.selectedDelegateId(null);
            self.showDelegateModal(true);
            return;
        }

        // No team members, proceed with normal removal
        deleteConfirmation.show(
            '"' + personnel.fullName + '" ' + T('Personnel.RemoveFromOrgConfirm', 'personelini organizasyondan çıkarmak istediğinizden emin misiniz?'),
            function() {
                ApiService.delete('/customer-organizations/' + org.id + '/personnel/' + personnel.id)
                    .then(function() {
                        toastr.success(T('Personnel.RemoveSuccess', 'Personel organizasyondan çıkarıldı.'));
                        self.loadOrgPersonnel(org.id);
                        self.loadOrganizations(self.selectedCustomerForOrg().id);
                        self.loadPersonnelPool(self.selectedCustomerForOrg().id);
                    })
                    .catch(function(error) {
                        console.error('Error removing personnel:', error);
                        toastr.error(T('Personnel.RemoveError', 'Personel çıkarılırken bir hata oluştu.') + ' ' + (error.message || ''));
                    });
            }
        );
    };

    // Close delegate modal
    self.closeDelegateModal = function() {
        self.showDelegateModal(false);
        self.personnelToRemove(null);
        self.availableDelegates([]);
        self.selectedDelegateId(null);
    };

    // Confirm removal with delegate (transfer team members)
    self.confirmRemoveWithDelegate = function() {
        var personnel = self.personnelToRemove();
        var delegateId = self.selectedDelegateId();
        var org = self.selectedOrganization();

        if (!personnel || !delegateId || !org) return;

        self.isRemovingWithDelegate(true);        // API call to transfer and remove
        ApiService.post('/customer-organizations/' + org.id + '/transfer-and-remove', {
            personnelIdToRemove: personnel.id,
            newSupervisorId: delegateId
        })
        .then(function() {
            toastr.success(T('Personnel.TransferSuccess', 'Ekip üyeleri devredildi ve personel organizasyondan çıkarıldı.'));
            self.closeDelegateModal();
            self.loadOrgPersonnel(org.id);
            self.loadOrganizations(self.selectedCustomerForOrg().id);
            self.loadPersonnelPool(self.selectedCustomerForOrg().id);
        })
        .catch(function(error) {
            console.error('Error transferring and removing:', error);
            toastr.error(T('Personnel.TransferError', 'Transfer işlemi sırasında bir hata oluştu.') + ' ' + (error.message || ''));
        })
        .finally(function() {
            self.isRemovingWithDelegate(false);
        });
    };

    // Toggle inline new supervisor form
    self.toggleNewSupervisorForm = function() {
        if (self.showNewSupervisorForm()) {
            self.showNewSupervisorForm(false);
        } else {
            // Reset form
            self.newSupervisor({
                firstName: ko.observable(''),
                lastName: ko.observable(''),
                username: ko.observable(''),
                email: ko.observable(''),
                password: ko.observable('')
            });
            self.showNewSupervisorForm(true);
        }
    };

    // Save new supervisor (inline form)
    self.saveNewSupervisor = function() {
        var sup = self.newSupervisor();
        var customer = self.selectedCustomerForOrg();
        var org = self.selectedOrganization();

        if (!customer || !org) return;

        var firstName = ko.unwrap(sup.firstName);
        var lastName = ko.unwrap(sup.lastName);
        var username = ko.unwrap(sup.username);
        var email = ko.unwrap(sup.email);
        var password = ko.unwrap(sup.password);
        var role = '2'; // Süpervizör (sabit)

        if (!firstName || !lastName || !username || !email || !password) {
            toastr.error(T('Common.AllFieldsRequired', 'Tüm alanları doldurun.'));
            return;
        }

        // Kullanıcı adı formatı kontrolü (sadece İngilizce harf, rakam, alt çizgi, nokta, tire)
        var usernameRegex = /^[a-zA-Z0-9_.-]+$/;
        if (!usernameRegex.test(username)) {
            toastr.error(T('User.UsernameInvalid', 'Kullanıcı adı sadece İngilizce harf, rakam, alt çizgi, nokta ve tire içerebilir. Boşluk ve Türkçe karakter kullanılamaz.'));
            return;
        }

        // Şifre uzunluğu kontrolü
        if (password.length < 6) {
            toastr.error(T('Validation.PasswordMinLength', 'Şifre en az 6 karakter olmalıdır.'));
            return;
        }

        self.isSavingNewSupervisor(true);        // First create the personnel
        customerApiService.createPersonnel({
            customerId: customer.id,
            firstName: firstName,
            lastName: lastName,
            username: username,
            email: email,
            password: password,
            role: role,
            isActive: true
        })
        .then(function(newPersonnel) {
            // Then assign to organization
            return ApiService.post('/customer-organizations/assign-personnel', {
                personnelId: newPersonnel.id,
                organizationId: org.id
            });
        })
        .then(function() {
            toastr.success(T('Personnel.SupervisorCreated', 'Yönetici/Süpervizör oluşturuldu ve atandı.'));
            self.showNewSupervisorForm(false);
            self.loadOrgPersonnel(org.id);
            self.loadOrganizations(customer.id);
            self.loadPersonnelPool(customer.id);
        })
        .catch(function(error) {
            console.error('Error creating supervisor:', error);
            // Validation hatalarını detaylı göster
            var errorMsg = '';
            if (error.errors) {
                // ASP.NET validation errors
                var errorDetails = [];
                for (var field in error.errors) {
                    errorDetails.push(field + ': ' + error.errors[field].join(', '));
                }
                errorMsg = errorDetails.join('; ');
            } else if (error.validationErrors) {
                // Custom validation errors
                var errorDetails = [];
                for (var field in error.validationErrors) {
                    errorDetails.push(field + ': ' + error.validationErrors[field].join(', '));
                }
                errorMsg = errorDetails.join('; ');
            } else {
                errorMsg = error.message || error.title || JSON.stringify(error);
            }
            toastr.error(T('Personnel.CreateError', 'Personel oluşturulurken bir hata oluştu.') + ' ' + errorMsg);
        })
        .finally(function() {
            self.isSavingNewSupervisor(false);
        });
    };

    // Add operator to supervisor
    self.addOperatorToSupervisor = function(supervisor) {
        self.selectedSupervisorForOperator(supervisor);
        self.newOperator({
            firstName: ko.observable(''),
            lastName: ko.observable(''),
            username: ko.observable(''),
            email: ko.observable(''),
            password: ko.observable('')
        });
        self.showAddOperatorModal(true);
    };

    // Add independent operator (no supervisor)
    self.addIndependentOperator = function() {
        self.selectedSupervisorForOperator(null); // No supervisor
        self.newOperator({
            firstName: ko.observable(''),
            lastName: ko.observable(''),
            username: ko.observable(''),
            email: ko.observable(''),
            password: ko.observable('')
        });
        self.showAddOperatorModal(true);
    };

    // Close add operator modal
    self.closeAddOperatorModal = function() {
        self.showAddOperatorModal(false);
        self.selectedSupervisorForOperator(null);
    };

    // Save new operator
    self.saveNewOperator = function() {
        var op = self.newOperator();
        var supervisor = self.selectedSupervisorForOperator(); // Can be null for independent operators
        var customer = self.selectedCustomerForOrg();
        var org = self.selectedOrganization();

        if (!customer || !org) return;

        var firstName = ko.unwrap(op.firstName);
        var lastName = ko.unwrap(op.lastName);
        var username = ko.unwrap(op.username);
        var email = ko.unwrap(op.email);
        var password = ko.unwrap(op.password);

        if (!firstName || !lastName || !username || !email || !password) {
            toastr.error(T('Common.AllFieldsRequired', 'Tüm zorunlu alanları doldurun.'));
            return;
        }

        // Kullanıcı adı formatı kontrolü (sadece İngilizce harf, rakam, alt çizgi, nokta, tire)
        var usernameRegex = /^[a-zA-Z0-9_.-]+$/;
        if (!usernameRegex.test(username)) {
            toastr.error(T('User.UsernameInvalid', 'Kullanıcı adı sadece İngilizce harf, rakam, alt çizgi, nokta ve tire içerebilir. Boşluk ve Türkçe karakter kullanılamaz.'));
            return;
        }

        // Şifre uzunluğu kontrolü
        if (password.length < 6) {
            toastr.error(T('Validation.PasswordMinLength', 'Şifre en az 6 karakter olmalıdır.'));
            return;
        }

        self.isSavingOperator(true);        // First create the personnel
        customerApiService.createPersonnel({
            customerId: customer.id,
            firstName: firstName,
            lastName: lastName,
            username: username,
            email: email,
            password: password,
            role: '3', // Operator
            isActive: true
        })
        .then(function(newPersonnel) {
            // Then assign to organization (with or without supervisor)
            return ApiService.post('/customer-organizations/assign-personnel', {
                personnelId: newPersonnel.id,
                organizationId: org.id,
                supervisorId: supervisor ? supervisor.id : null
            });
        })
        .then(function() {
            toastr.success(T('Personnel.OperatorCreated', 'Operatör oluşturuldu ve atandı.'));
            self.closeAddOperatorModal();
            self.loadOrgPersonnel(org.id);
            self.loadOrganizations(customer.id);
            self.loadPersonnelPool(customer.id);
        })
        .catch(function(error) {
            console.error('Error creating operator:', error);
            // Validation hatalarını detaylı göster
            var errorMsg = '';
            if (error.errors) {
                var errorDetails = [];
                for (var field in error.errors) {
                    errorDetails.push(field + ': ' + error.errors[field].join(', '));
                }
                errorMsg = errorDetails.join('; ');
            } else if (error.validationErrors) {
                var errorDetails = [];
                for (var field in error.validationErrors) {
                    errorDetails.push(field + ': ' + error.validationErrors[field].join(', '));
                }
                errorMsg = errorDetails.join('; ');
            } else {
                errorMsg = error.message || error.title || JSON.stringify(error);
            }
            toastr.error(T('Personnel.CreateError', 'Operatör oluşturulurken bir hata oluştu.') + ' ' + errorMsg);
        })
        .finally(function() {
            self.isSavingOperator(false);
        });
    };

    // Handle ESC key to close modals
    self.handleEscapeKey = function(e) {
        if (e.key === 'Escape' || e.keyCode === 27) {
            // Close modals in order (innermost first)
            if (self.showAddOperatorModal()) {
                self.closeAddOperatorModal();
            } else if (self.showOrgFormModal()) {
                self.closeOrgFormModal();
            } else if (self.showNewSupervisorForm()) {
                self.showNewSupervisorForm(false);
            } else if (self.showNewManagerForm()) {
                self.showNewManagerForm(false);
            } else if (self.showOrganizationModal()) {
                self.closeOrganizationModal();
            } else if (self.showPersonnelFormModal()) {
                self.closePersonnelFormModal();
            } else if (self.showChangePasswordModal()) {
                self.cancelChangePassword();
            } else if (self.showPersonnelModal()) {
                self.closePersonnelModal();
            } else if (self.showCustomerModal()) {
                self.closeModal();
            }
        }
    };

    // Add ESC key listener
    document.addEventListener('keydown', self.handleEscapeKey);

    // Load notification frequencies + days from EnumsService
    self.loadNotificationFrequencies = function() {
        var frequencies = EnumsService.getByType('evaluationNotificationFrequency');
        if (frequencies && frequencies.length > 0) {
            self.notificationFrequencies(frequencies);
            // Kural dropdown'u için id=0 (None) hariç olanları al
            self.ruleFrequencies(frequencies.filter(function(f) { return f.id > 0; }));
        }

        var days = EnumsService.getByType('daysOfWeek');
        if (days && days.length > 0) {
            self.daysOfWeek(days);
        }
    };

    // Load notification templates
    self.loadNotificationTemplates = function() {
        // templateTypeId=8 is EvaluationNotification
        ApiService.get('/email-templates?templateTypeId=8')
            .then(function(data) {
                self.notificationTemplates(data || []);
            })
            .catch(function(error) {
                console.error('Error loading notification templates:', error);
                self.notificationTemplates([]);
            });
    };

    // Initialize
    self.init = function() {
        // Önce EnumsService'i yükle, sonra diğer verileri çek
        EnumsService.load().then(function() {
            self.loadNotificationFrequencies();
            self.loadNotificationTemplates();
            self.loadCustomers();
        });
    };

    self.init();
}

// Translation keys
var TRANSLATION_KEYS = [
    'Customer.LoadError',
    'Customer.CompanyNameRequired',
    'Customer.SaveSuccess',
    'Customer.UpdateSuccess',
    'Customer.SaveError',
    'Customer.DeleteConfirm',
    'Customer.DeleteSuccess',
    'Customer.DeleteError',
    'Personnel.LoadError',
    'Personnel.RequiredFields',
    'Personnel.PasswordRequired',
    'Personnel.SaveSuccess',
    'Personnel.UpdateSuccess',
    'Personnel.SaveError',
    'Personnel.DeleteConfirm',
    'Personnel.DeleteSuccess',
    'Personnel.DeleteError',
    'Common.AllFieldsRequired',
    'Password.MinLength',
    'Password.Mismatch',
    'Password.ResetSuccess',
    'Password.ResetError',
    'User.UsernameInvalid',
    'Validation.PasswordMinLength',
    'Personnel.ManagerCreated',
    'Personnel.CreateError',
    'Organization.LoadError',
    'Organization.NameRequired',
    'Organization.CreateSuccess',
    'Organization.UpdateSuccess',
    'Organization.SaveError',
    'Organization.DeleteConfirm',
    'Organization.DeleteSuccess',
    'Organization.DeleteError',
    'Personnel.AssignSuccess',
    'Personnel.AssignError',
    'Personnel.OperatorAssignSuccess',
    'Personnel.SupervisorCreated',
    'Personnel.OperatorCreated',
    'Customer.MakeIndependentConfirm',
    'Customer.MakeIndependentSuccess',
    'Customer.MakeIndependentError',
    'Personnel.RemoveFromOrgConfirm',
    'Personnel.RemoveSuccess',
    'Personnel.RemoveError',
    'Personnel.TransferSuccess',
    'Personnel.TransferError',
    // Confirm modal keys
    'Common.Confirmation',
    'Confirm.Message',
    'Common.DeleteConfirmation',
    'Common.DeleteConfirmationMessage',
    'Common.YesDelete',
    'Common.Confirm'
];

// Apply bindings when DOM is ready
// Global ViewModel reference for popup windows
var vm = null;

$(document).ready(function() {
    Localization.loadKeys(TRANSLATION_KEYS).then(function() {
        vm = new CustomersViewModel();
        ko.applyBindings(vm, document.getElementById('customers-app'));
    });
});
