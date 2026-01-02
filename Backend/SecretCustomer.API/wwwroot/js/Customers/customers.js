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
                self.errorMessage(T('Customer.LoadError', 'Müşteriler yüklenirken bir hata oluştu.') + ' ' + (error.message || ''));
            })
            .finally(function() {
                self.isLoading(false);
            });
    };

    // Create new customer
    self.createNew = function() {
        self.modalErrorMessage('');
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
        self.modalErrorMessage('');
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
        self.modalErrorMessage('');
        self.successMessage('');

        var customer = self.editingCustomer();
        if (!customer) return;

        // Validation
        if (!customer.companyName) {
            self.modalErrorMessage(T('Customer.CompanyNameRequired', 'Şirket adı zorunludur.'));
            return;
        }

        self.isSaving(true);

        var promise = customer.id
            ? customerApiService.updateCustomer(customer.id, customer)
            : customerApiService.createCustomer(customer);

        promise
            .then(function(savedCustomer) {
                var isNew = !customer.id;
                if (isNew) {
                    // Yeni kayıt: array'e ekle
                    self.customers.push(savedCustomer);
                } else {
                    // Güncelleme: array'de bul ve güncelle
                    var list = self.customers();
                    for (var i = 0; i < list.length; i++) {
                        if (list[i].id === savedCustomer.id) {
                            self.customers.splice(i, 1, savedCustomer);
                            break;
                        }
                    }
                }
                self.successMessage(isNew ? T('Customer.SaveSuccess', 'Müşteri başarıyla oluşturuldu.') : T('Customer.UpdateSuccess', 'Müşteri başarıyla güncellendi.'));
                self.isModalOpen(false);
            })
            .catch(function(error) {
                console.error('Error saving customer:', error);
                self.modalErrorMessage(T('Customer.SaveError', 'Müşteri kaydedilirken bir hata oluştu.') + ' ' + (error.message || ''));
            })
            .finally(function() {
                self.isSaving(false);
            });
    };

    // Close modal
    self.closeModal = function() {
        self.isModalOpen(false);
        self.editingCustomer(null);
        self.modalErrorMessage('');
    };

    // Delete customer
    self.deleteCustomer = function(customer) {
        deleteConfirmation.show(
            '"' + customer.companyName + '" ' + T('Customer.DeleteConfirm', 'Bu müşteriyi silmek istediğinizden emin misiniz?'),
            function() {
                customerApiService.deleteCustomer(customer.id)
                    .then(function() {
                        // Array'den sil
                        self.customers.remove(function(c) { return c.id === customer.id; });
                        self.successMessage(T('Customer.DeleteSuccess', 'Müşteri başarıyla silindi.'));
                    })
                    .catch(function(error) {
                        console.error('Error deleting customer:', error);
                        self.errorMessage(T('Customer.DeleteError', 'Müşteri silinirken bir hata oluştu.') + ' ' + (error.message || ''));
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
                self.errorMessage(T('Personnel.LoadError', 'Personeller yüklenirken bir hata oluştu.') + ' ' + (error.message || ''));
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

        self.personnelModalErrorMessage('');
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
        self.personnelModalErrorMessage('');
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
        self.personnelModalErrorMessage('');
        self.successMessage('');

        var personnel = self.editingPersonnel();
        if (!personnel) return;

        // Validation
        if (!personnel.username || !personnel.email || !personnel.firstName || !personnel.lastName) {
            self.personnelModalErrorMessage(T('Personnel.RequiredFields', 'Kullanıcı adı, e-posta, ad ve soyad zorunludur.'));
            return;
        }

        if (!personnel.id && !personnel.password) {
            self.personnelModalErrorMessage(T('Personnel.PasswordRequired', 'Yeni personel için şifre zorunludur.'));
            return;
        }

        self.isSavingPersonnel(true);

        var promise = personnel.id
            ? customerApiService.updatePersonnel(personnel.id, personnel)
            : customerApiService.createPersonnel(personnel);

        promise
            .then(function() {
                self.successMessage(personnel.id ? T('Personnel.UpdateSuccess', 'Personel başarıyla güncellendi.') : T('Personnel.SaveSuccess', 'Personel başarıyla oluşturuldu.'));
                self.showPersonnelFormModal(false);
                self.loadPersonnel(self.selectedCustomerForPersonnel().id);
                self.loadCustomers(); // Refresh personnel count
            })
            .catch(function(error) {
                console.error('Error saving personnel:', error);
                self.personnelModalErrorMessage(T('Personnel.SaveError', 'Personel kaydedilirken bir hata oluştu.') + ' ' + (error.message || ''));
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
                        self.successMessage(T('Personnel.DeleteSuccess', 'Personel başarıyla silindi.'));
                        self.loadPersonnel(self.selectedCustomerForPersonnel().id);
                        self.loadCustomers(); // Refresh personnel count
                    })
                    .catch(function(error) {
                        console.error('Error deleting personnel:', error);
                        self.errorMessage(T('Personnel.DeleteError', 'Personel silinirken bir hata oluştu.') + ' ' + (error.message || ''));
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
                self.successMessage(response.message || T('Password.ResetSuccess', 'Şifre başarıyla sıfırlandı.'));
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
        self.loadCustomers();
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

    // Show organizations modal
    self.showOrganizations = function(customer) {
        self.selectedCustomerForOrg(customer);
        self.selectedOrganization(null);
        self.orgPersonnelList({ supervisors: [], operators: [] });
        self.orgModalErrorMessage('');
        self.orgModalSuccessMessage('');
        self.orgSearchText('');
        self.showOrganizationModal(true);
        self.showNewManagerForm(false);
        self.showNewSupervisorForm(false);
        self.loadOrganizations(customer.id);
        self.loadPersonnelPool(customer.id);
        self.loadCustomerManagers(customer.id);
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
            self.orgModalErrorMessage(T('Common.AllFieldsRequired', 'Tüm alanları doldurun.'));
            return;
        }

        self.isSavingNewManager(true);
        self.orgModalErrorMessage('');

        customerApiService.createPersonnel({
            customerId: customer.id,
            firstName: firstName,
            lastName: lastName,
            username: username,
            email: email,
            password: password,
            role: 1, // CustomerManager
            isActive: true
        })
        .then(function() {
            self.orgModalSuccessMessage(T('Personnel.ManagerCreated', 'Firma yöneticisi oluşturuldu.'));
            self.showNewManagerForm(false);
            self.loadCustomerManagers(customer.id);
            self.loadPersonnelPool(customer.id);
        })
        .catch(function(error) {
            console.error('Error creating manager:', error);
            self.orgModalErrorMessage(T('Personnel.CreateError', 'Yönetici oluşturulurken bir hata oluştu.') + ' ' + (error.message || ''));
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
                        self.orgModalSuccessMessage('Yönetici silindi.');
                        self.loadCustomerManagers(self.selectedCustomerForOrg().id);
                        self.loadPersonnelPool(self.selectedCustomerForOrg().id);
                    })
                    .catch(function(error) {
                        self.orgModalErrorMessage('Yönetici silinirken hata: ' + (error.message || ''));
                    });
            }
        );
    };

    // Close organizations modal
    self.closeOrganizationModal = function() {
        self.showOrganizationModal(false);
        self.selectedCustomerForOrg(null);
        self.organizations([]);
        self.selectedOrganization(null);
        self.loadCustomers(); // Refresh to update counts
    };

    // Load organizations for customer
    self.loadOrganizations = function(customerId) {
        self.isLoadingOrganizations(true);
        self.orgModalErrorMessage('');

        ApiService.get('/customer-organizations/by-customer/' + customerId)
            .then(function(data) {
                self.organizations(data || []);
            })
            .catch(function(error) {
                console.error('Error loading organizations:', error);
                self.orgModalErrorMessage(T('Organization.LoadError', 'Organizasyonlar yüklenirken bir hata oluştu.'));
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
                self.orgModalErrorMessage(T('Personnel.LoadError', 'Personeller yüklenirken bir hata oluştu.'));
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
            self.orgModalErrorMessage(T('Organization.NameRequired', 'Organizasyon adı zorunludur.'));
            return;
        }

        self.isSavingOrg(true);
        self.orgModalErrorMessage('');

        var data = {
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
                self.orgModalSuccessMessage(isNew ? T('Organization.CreateSuccess', 'Organizasyon oluşturuldu.') : T('Organization.UpdateSuccess', 'Organizasyon güncellendi.'));
                self.closeOrgFormModal();
            })
            .catch(function(error) {
                console.error('Error saving organization:', error);
                self.orgModalErrorMessage(T('Organization.SaveError', 'Organizasyon kaydedilirken bir hata oluştu.') + ' ' + (error.message || ''));
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
                        self.orgModalSuccessMessage(T('Organization.DeleteSuccess', 'Organizasyon silindi.'));
                        if (self.selectedOrganization() && self.selectedOrganization().id === org.id) {
                            self.selectedOrganization(null);
                            self.orgPersonnelList({ supervisors: [], operators: [] });
                        }
                    })
                    .catch(function(error) {
                        console.error('Error deleting organization:', error);
                        self.orgModalErrorMessage(T('Organization.DeleteError', 'Organizasyon silinirken bir hata oluştu.') + ' ' + (error.message || ''));
                    });
            }
        );
    };

    // Assign pool personnel to organization
    self.assignPoolPersonnelToOrg = function() {
        var personnelId = self.selectedPoolPersonnelId();
        var org = self.selectedOrganization();
        if (!personnelId || !org) return;

        ApiService.post('/customer-organizations/assign-personnel', {
            personnelId: personnelId,
            organizationId: org.id
        })
        .then(function() {
            self.orgModalSuccessMessage(T('Personnel.AssignSuccess', 'Personel organizasyona atandı.'));
            self.selectedPoolPersonnelId(null);
            self.loadOrgPersonnel(org.id);
            self.loadOrganizations(self.selectedCustomerForOrg().id);
            self.loadPersonnelPool(self.selectedCustomerForOrg().id);
        })
        .catch(function(error) {
            console.error('Error assigning personnel:', error);
            self.orgModalErrorMessage(T('Personnel.AssignError', 'Personel atanırken bir hata oluştu.') + ' ' + (error.message || ''));
        });
    };

    // Edit organization personnel (opens form modal)
    self.editOrgPersonnel = function(personnel) {
        self.modalErrorMessage('');
        self.editingPersonnel({
            id: personnel.id,
            customerId: self.selectedCustomerForOrg().id,
            username: personnel.username,
            email: personnel.email,
            password: '',
            firstName: personnel.firstName,
            lastName: personnel.lastName,
            phoneNumber: personnel.phoneNumber || '',
            department: personnel.department || '',
            title: personnel.title || '',
            role: personnel.role,
            isActive: personnel.isActive !== false
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

        // Validation
        if (!personnel.username || !personnel.email || !personnel.firstName || !personnel.lastName) {
            self.personnelModalErrorMessage(T('Personnel.RequiredFields', 'Kullanıcı adı, e-posta, ad ve soyad zorunludur.'));
            return;
        }

        if (!personnel.id && !personnel.password) {
            self.personnelModalErrorMessage(T('Personnel.PasswordRequired', 'Yeni personel için şifre zorunludur.'));
            return;
        }

        self.isSavingPersonnel(true);

        var promise = personnel.id
            ? customerApiService.updatePersonnel(personnel.id, personnel)
            : customerApiService.createPersonnel(personnel);

        promise
            .then(function() {
                self.successMessage(personnel.id ? T('Personnel.UpdateSuccess', 'Personel başarıyla güncellendi.') : T('Personnel.SaveSuccess', 'Personel başarıyla oluşturuldu.'));
                self.showPersonnelFormModal(false);

                // If organization modal is open, refresh org data
                if (self.showOrganizationModal() && self.selectedOrganization()) {
                    self.loadOrgPersonnel(self.selectedOrganization().id);
                    self.loadOrganizations(self.selectedCustomerForOrg().id);
                    self.loadPersonnelPool(self.selectedCustomerForOrg().id);
                } else if (self.selectedCustomerForPersonnel()) {
                    self.loadPersonnel(self.selectedCustomerForPersonnel().id);
                }
                self.loadCustomers(); // Refresh personnel count
            })
            .catch(function(error) {
                console.error('Error saving personnel:', error);
                self.personnelModalErrorMessage(T('Personnel.SaveError', 'Personel kaydedilirken bir hata oluştu.') + ' ' + (error.message || ''));
            })
            .finally(function() {
                self.isSavingPersonnel(false);
            });
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
                        self.orgModalSuccessMessage(T('Personnel.RemoveSuccess', 'Personel organizasyondan çıkarıldı.'));
                        self.loadOrgPersonnel(org.id);
                        self.loadOrganizations(self.selectedCustomerForOrg().id);
                        self.loadPersonnelPool(self.selectedCustomerForOrg().id);
                    })
                    .catch(function(error) {
                        console.error('Error removing personnel:', error);
                        self.orgModalErrorMessage(T('Personnel.RemoveError', 'Personel çıkarılırken bir hata oluştu.') + ' ' + (error.message || ''));
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

        self.isRemovingWithDelegate(true);
        self.orgModalErrorMessage('');

        // API call to transfer and remove
        ApiService.post('/customer-organizations/' + org.id + '/transfer-and-remove', {
            personnelIdToRemove: personnel.id,
            newSupervisorId: delegateId
        })
        .then(function() {
            self.orgModalSuccessMessage(T('Personnel.TransferSuccess', 'Ekip üyeleri devredildi ve personel organizasyondan çıkarıldı.'));
            self.closeDelegateModal();
            self.loadOrgPersonnel(org.id);
            self.loadOrganizations(self.selectedCustomerForOrg().id);
            self.loadPersonnelPool(self.selectedCustomerForOrg().id);
        })
        .catch(function(error) {
            console.error('Error transferring and removing:', error);
            self.orgModalErrorMessage(T('Personnel.TransferError', 'Transfer işlemi sırasında bir hata oluştu.') + ' ' + (error.message || ''));
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
        var role = 2; // Süpervizör (sabit)

        if (!firstName || !lastName || !username || !email || !password) {
            self.orgModalErrorMessage(T('Common.AllFieldsRequired', 'Tüm alanları doldurun.'));
            return;
        }

        self.isSavingNewSupervisor(true);
        self.orgModalErrorMessage('');

        // First create the personnel
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
            self.orgModalSuccessMessage(T('Personnel.SupervisorCreated', 'Yönetici/Süpervizör oluşturuldu ve atandı.'));
            self.showNewSupervisorForm(false);
            self.loadOrgPersonnel(org.id);
            self.loadOrganizations(customer.id);
            self.loadPersonnelPool(customer.id);
        })
        .catch(function(error) {
            console.error('Error creating supervisor:', error);
            self.orgModalErrorMessage(T('Personnel.CreateError', 'Personel oluşturulurken bir hata oluştu.') + ' ' + (error.message || ''));
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

    // Close add operator modal
    self.closeAddOperatorModal = function() {
        self.showAddOperatorModal(false);
        self.selectedSupervisorForOperator(null);
    };

    // Save new operator
    self.saveNewOperator = function() {
        var op = self.newOperator();
        var supervisor = self.selectedSupervisorForOperator();
        var customer = self.selectedCustomerForOrg();
        var org = self.selectedOrganization();

        if (!supervisor || !customer || !org) return;

        var firstName = ko.unwrap(op.firstName);
        var lastName = ko.unwrap(op.lastName);
        var username = ko.unwrap(op.username);
        var email = ko.unwrap(op.email);
        var password = ko.unwrap(op.password);

        if (!firstName || !lastName || !username || !email || !password) {
            self.orgModalErrorMessage(T('Common.AllFieldsRequired', 'Tüm zorunlu alanları doldurun.'));
            return;
        }

        self.isSavingOperator(true);
        self.orgModalErrorMessage('');

        // First create the personnel
        customerApiService.createPersonnel({
            customerId: customer.id,
            firstName: firstName,
            lastName: lastName,
            username: username,
            email: email,
            password: password,
            role: 3, // Operator
            isActive: true
        })
        .then(function(newPersonnel) {
            // Then assign to organization with supervisor
            return ApiService.post('/customer-organizations/assign-personnel', {
                personnelId: newPersonnel.id,
                organizationId: org.id,
                supervisorId: supervisor.id
            });
        })
        .then(function() {
            self.orgModalSuccessMessage(T('Personnel.OperatorCreated', 'Operatör oluşturuldu ve atandı.'));
            self.closeAddOperatorModal();
            self.loadOrgPersonnel(org.id);
            self.loadOrganizations(customer.id);
            self.loadPersonnelPool(customer.id);
        })
        .catch(function(error) {
            console.error('Error creating operator:', error);
            self.orgModalErrorMessage(T('Personnel.CreateError', 'Operatör oluşturulurken bir hata oluştu.') + ' ' + (error.message || ''));
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

    // Initialize
    self.init = function() {
        // Önce EnumsService'i yükle, sonra diğer verileri çek
        EnumsService.load().then(function() {
            self.loadCustomers();
        });
    };

    self.init();
}

// Apply bindings when DOM is ready
if (typeof ko !== 'undefined') {
    ko.applyBindings(new CustomersViewModel(), document.getElementById('customers-app'));
}
