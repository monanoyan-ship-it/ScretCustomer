// Organizations ViewModel - Organizasyon Yönetimi Popup
function OrganizationsViewModel() {
    var self = this;

    // Config
    self.customerId = window.organizationsConfig ? window.organizationsConfig.customerId : null;

    // State
    self.isLoading = ko.observable(false);
    self.isSaving = ko.observable(false);
    self.isLoadingPersonnel = ko.observable(false);

    // Data
    self.organizations = ko.observableArray([]);
    self.selectedOrganization = ko.observable(null);
    self.orgPersonnel = ko.observable({ supervisors: [], operators: [] });
    self.personnelPool = ko.observableArray([]);

    // Managers computed from personnelPool (role 1 = CustomerManager)
    self.managers = ko.computed(function() {
        return self.personnelPool().filter(function(p) {
            return p.role === 1;
        });
    });

    // Search
    self.orgSearchText = ko.observable('');

    // ========== CHIP-BASED FILTER SYSTEM ==========
    self.selectedFilterType = ko.observable('');
    self.activeFilters = ko.observableArray([]);

    // Temp filter values
    self.tempFilter = {
        name: ko.observable(''),
        code: ko.observable(''),
        isActive: ko.observable(null)
    };

    // Filter labels
    self.filterLabels = {
        name: 'Organizasyon Adı',
        code: 'Kod',
        isActive: 'Durum'
    };

    self.statusLabels = {
        'true': 'Aktif',
        'false': 'Pasif'
    };

    // Can add filter
    self.canAddFilter = ko.computed(function() {
        var type = self.selectedFilterType();
        if (!type) return false;

        switch (type) {
            case 'name': return self.tempFilter.name().trim() !== '';
            case 'code': return self.tempFilter.code().trim() !== '';
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
            case 'name':
                var name = self.tempFilter.name().trim();
                if (!name) return;
                filter.value = name;
                filter.displayValue = name;
                self.tempFilter.name('');
                break;

            case 'code':
                var code = self.tempFilter.code().trim();
                if (!code) return;
                filter.value = code;
                filter.displayValue = code;
                self.tempFilter.code('');
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
    };

    // Remove filter
    self.removeFilter = function(filter) {
        self.activeFilters.remove(filter);
    };

    // Clear all filters
    self.clearFilters = function() {
        self.activeFilters([]);
        self.orgSearchText('');
    };

    // Filtered organizations
    self.filteredOrganizations = ko.computed(function() {
        var search = (self.orgSearchText() || '').toLowerCase().trim();
        var orgs = self.organizations();
        var filters = self.activeFilters();

        // Global search filter
        if (search) {
            orgs = orgs.filter(function(o) {
                return (o.name || '').toLowerCase().indexOf(search) >= 0 ||
                       (o.code || '').toLowerCase().indexOf(search) >= 0;
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
                orgs = orgs.filter(function(o) {
                    return typeFilters.some(function(f) {
                        switch (f.type) {
                            case 'name':
                                return (o.name || '').toLowerCase().indexOf(f.value.toLowerCase()) >= 0;
                            case 'code':
                                return (o.code || '').toLowerCase().indexOf(f.value.toLowerCase()) >= 0;
                            case 'isActive':
                                return o.isActive === f.value;
                            default:
                                return true;
                        }
                    });
                });
            });
        }

        return orgs;
    });

    // Personnel pool for supervisor (role 2 = Supervisor, unassigned)
    self.personnelPoolForSupervisor = ko.computed(function() {
        return self.personnelPool().filter(function(p) {
            return p.role === 2 && (p.organizationCount === 0 || !p.organizationId);
        });
    });

    // Unassigned operators (role 3 = Operator, not assigned to any org)
    self.unassignedOperators = ko.computed(function() {
        return self.personnelPool().filter(function(p) {
            return p.role === 3 && (p.organizationCount === 0 || !p.organizationId);
        });
    });

    // Modals
    self.editingOrganization = ko.observable(null);
    self.orgModal = null;
    self.addOperatorModal = null;
    self.delegateModal = null;

    // Manager form
    self.showNewManagerForm = ko.observable(false);
    self.isSavingManager = ko.observable(false);
    self.newManager = {
        firstName: ko.observable(''),
        lastName: ko.observable(''),
        username: ko.observable(''),
        email: ko.observable(''),
        password: ko.observable('')
    };

    // Supervisor form
    self.showNewSupervisorForm = ko.observable(false);
    self.isSavingSupervisor = ko.observable(false);
    self.newSupervisor = {
        firstName: ko.observable(''),
        lastName: ko.observable(''),
        username: ko.observable(''),
        email: ko.observable(''),
        password: ko.observable('')
    };

    // Operator form
    self.selectedSupervisorForOperator = ko.observable(null);
    self.isSavingOperator = ko.observable(false);
    self.newOperator = {
        firstName: ko.observable(''),
        lastName: ko.observable(''),
        username: ko.observable(''),
        email: ko.observable(''),
        password: ko.observable('')
    };

    // Pool selection
    self.selectedPoolPersonnelId = ko.observable(null);
    self.selectedPoolOperatorId = ko.observable(null);
    self.selectedPoolOperatorForSupervisorId = ko.observable(null);

    // Delegate modal
    self.personnelToRemove = ko.observable(null);
    self.availableDelegates = ko.observableArray([]);
    self.selectedDelegateId = ko.observable(null);
    self.isRemovingWithDelegate = ko.observable(false);

    // ==================== LOAD DATA ====================

    self.loadOrganizations = function() {
        if (!self.customerId) return;

        self.isLoading(true);

        ApiService.get('/customer-organizations/by-customer/' + self.customerId)
            .then(function(data) {
                self.organizations(data || []);
            })
            .catch(function(error) {
                console.error('Error loading organizations:', error);
                toastr.error('Organizasyonlar yüklenirken hata oluştu.');
            })
            .finally(function() {
                self.isLoading(false);
            });
    };

    self.loadPersonnelPool = function() {
        if (!self.customerId) return;

        ApiService.get('/customer-organizations/personnel-pool/' + self.customerId)
            .then(function(data) {
                self.personnelPool(data || []);
            })
            .catch(function(error) {
                console.error('Error loading personnel pool:', error);
            });
    };


    self.loadOrgPersonnel = function(orgId) {
        self.isLoadingPersonnel(true);

        ApiService.get('/customer-organizations/' + orgId + '/personnel')
            .then(function(data) {
                self.orgPersonnel(data || { supervisors: [], operators: [] });
            })
            .catch(function(error) {
                console.error('Error loading org personnel:', error);
                toastr.error('Personeller yüklenirken hata oluştu.');
            })
            .finally(function() {
                self.isLoadingPersonnel(false);
            });
    };

    // ==================== ORGANIZATION CRUD ====================

    self.selectOrganization = function(org) {
        self.selectedOrganization(org);
        self.loadOrgPersonnel(org.id);
    };

    self.createOrganization = function() {
        self.editingOrganization({
            id: null,
            name: ko.observable(''),
            code: ko.observable(''),
            description: ko.observable(''),
            isActive: ko.observable(true)
        });
        self.showOrgModal();
    };

    self.editOrganization = function(org) {
        self.editingOrganization({
            id: org.id,
            name: ko.observable(org.name),
            code: ko.observable(org.code || ''),
            description: ko.observable(org.description || ''),
            isActive: ko.observable(org.isActive)
        });
        self.showOrgModal();
    };

    self.saveOrganization = function() {
        var org = self.editingOrganization();
        if (!org) return;

        var name = ko.unwrap(org.name);
        if (!name) {
            toastr.warning('Organizasyon adı zorunludur.');
            return;
        }

        self.isSaving(true);

        var data = {
            name: name,
            code: ko.unwrap(org.code),
            description: ko.unwrap(org.description),
            isActive: ko.unwrap(org.isActive),
            customerId: self.customerId
        };

        var promise = org.id
            ? ApiService.put('/customer-organizations/' + org.id, data)
            : ApiService.post('/customer-organizations', data);

        promise
            .then(function(savedOrg) {
                var isNew = !org.id;
                if (isNew) {
                    self.organizations.push(savedOrg);
                } else {
                    var list = self.organizations();
                    for (var i = 0; i < list.length; i++) {
                        if (list[i].id === savedOrg.id) {
                            self.organizations.splice(i, 1, savedOrg);
                            if (self.selectedOrganization() && self.selectedOrganization().id === savedOrg.id) {
                                self.selectedOrganization(savedOrg);
                            }
                            break;
                        }
                    }
                }
                toastr.success(isNew ? 'Organizasyon oluşturuldu.' : 'Organizasyon güncellendi.');
                self.hideOrgModal();
                self.notifyParent();
            })
            .catch(function(error) {
                console.error('Error saving organization:', error);
                toastr.error('Organizasyon kaydedilirken hata oluştu.');
            })
            .finally(function() {
                self.isSaving(false);
            });
    };

    self.deleteOrganization = function(org) {
        deleteConfirmation.show(
            '<strong>' + org.name + '</strong> organizasyonunu silmek istediğinize emin misiniz?',
            function() {
                ApiService.delete('/customer-organizations/' + org.id)
                    .then(function() {
                        self.organizations.remove(function(o) { return o.id === org.id; });
                        if (self.selectedOrganization() && self.selectedOrganization().id === org.id) {
                            self.selectedOrganization(null);
                            self.orgPersonnel({ supervisors: [], operators: [] });
                        }
                        toastr.success('Organizasyon silindi.');
                        self.notifyParent();
                    })
                    .catch(function(error) {
                        console.error('Error deleting organization:', error);
                        toastr.error('Organizasyon silinirken hata oluştu.');
                    });
            }
        );
    };

    // ==================== MANAGER CRUD ====================

    self.toggleNewManagerForm = function() {
        if (self.showNewManagerForm()) {
            self.showNewManagerForm(false);
        } else {
            self.resetNewManager();
            self.showNewManagerForm(true);
        }
    };

    self.resetNewManager = function() {
        self.newManager.firstName('');
        self.newManager.lastName('');
        self.newManager.username('');
        self.newManager.email('');
        self.newManager.password('');
    };

    self.saveNewManager = function() {
        var firstName = self.newManager.firstName();
        var lastName = self.newManager.lastName();
        var username = self.newManager.username();
        var email = self.newManager.email();
        var password = self.newManager.password();

        if (!firstName || !lastName || !username || !email || !password) {
            toastr.warning('Tüm alanları doldurun.');
            return;
        }

        var usernameRegex = /^[a-zA-Z0-9_.-]+$/;
        if (!usernameRegex.test(username)) {
            toastr.error('Kullanıcı adı sadece İngilizce harf, rakam, alt çizgi, nokta ve tire içerebilir.');
            return;
        }

        if (password.length < 6) {
            toastr.error('Şifre en az 6 karakter olmalıdır.');
            return;
        }

        self.isSavingManager(true);

        ApiService.post('/customer-personnel', {
            customerId: self.customerId,
            firstName: firstName,
            lastName: lastName,
            username: username,
            email: email,
            password: password,
            role: 1, // CustomerManager
            isActive: true
        })
        .then(function() {
            toastr.success('Yönetici oluşturuldu.');
            self.showNewManagerForm(false);
            self.loadPersonnelPool();
            self.notifyParent();
        })
        .catch(function(error) {
            console.error('Error creating manager:', error);
            toastr.error(error.message || 'Yönetici oluşturulurken hata oluştu.');
        })
        .finally(function() {
            self.isSavingManager(false);
        });
    };

    self.removeManager = function(manager) {
        deleteConfirmation.show(
            '<strong>' + manager.fullName + '</strong> yöneticisini silmek istediğinize emin misiniz?',
            function() {
                ApiService.delete('/customer-personnel/' + manager.id)
                    .then(function() {
                        toastr.success('Yönetici silindi.');
                        self.loadPersonnelPool();
                        self.notifyParent();
                    })
                    .catch(function(error) {
                        console.error('Error deleting manager:', error);
                        toastr.error('Yönetici silinirken hata oluştu.');
                    });
            }
        );
    };

    // ==================== SUPERVISOR CRUD ====================

    self.toggleNewSupervisorForm = function() {
        if (self.showNewSupervisorForm()) {
            self.showNewSupervisorForm(false);
        } else {
            self.resetNewSupervisor();
            self.showNewSupervisorForm(true);
        }
    };

    self.resetNewSupervisor = function() {
        self.newSupervisor.firstName('');
        self.newSupervisor.lastName('');
        self.newSupervisor.username('');
        self.newSupervisor.email('');
        self.newSupervisor.password('');
    };

    self.saveNewSupervisor = function() {
        var org = self.selectedOrganization();
        if (!org) return;

        var firstName = self.newSupervisor.firstName();
        var lastName = self.newSupervisor.lastName();
        var username = self.newSupervisor.username();
        var email = self.newSupervisor.email();
        var password = self.newSupervisor.password();

        if (!firstName || !lastName || !username || !email || !password) {
            toastr.warning('Tüm alanları doldurun.');
            return;
        }

        var usernameRegex = /^[a-zA-Z0-9_.-]+$/;
        if (!usernameRegex.test(username)) {
            toastr.error('Kullanıcı adı sadece İngilizce harf, rakam, alt çizgi, nokta ve tire içerebilir.');
            return;
        }

        if (password.length < 6) {
            toastr.error('Şifre en az 6 karakter olmalıdır.');
            return;
        }

        self.isSavingSupervisor(true);

        // Create personnel then assign to org
        ApiService.post('/customer-personnel', {
            customerId: self.customerId,
            firstName: firstName,
            lastName: lastName,
            username: username,
            email: email,
            password: password,
            role: 2, // CustomerSupervisor
            isActive: true
        })
        .then(function(newPersonnel) {
            return ApiService.post('/customer-organizations/assign-personnel', {
                personnelId: newPersonnel.id,
                organizationId: org.id
            });
        })
        .then(function() {
            toastr.success('Süpervizör oluşturuldu ve organizasyona eklendi.');
            self.showNewSupervisorForm(false);
            self.loadOrgPersonnel(org.id);
            self.loadOrganizations();
            self.loadPersonnelPool();
            self.notifyParent();
        })
        .catch(function(error) {
            console.error('Error creating supervisor:', error);
            toastr.error(error.message || 'Süpervizör oluşturulurken hata oluştu.');
        })
        .finally(function() {
            self.isSavingSupervisor(false);
        });
    };

    self.assignFromPool = function() {
        var personnelId = self.selectedPoolPersonnelId();
        var org = self.selectedOrganization();
        if (!personnelId || !org) return;

        ApiService.post('/customer-organizations/assign-personnel', {
            personnelId: personnelId,
            organizationId: org.id
        })
        .then(function() {
            toastr.success('Personel organizasyona eklendi.');
            self.selectedPoolPersonnelId(null);
            self.loadOrgPersonnel(org.id);
            self.loadOrganizations();
            self.loadPersonnelPool();
            self.notifyParent();
        })
        .catch(function(error) {
            console.error('Error assigning personnel:', error);
            toastr.error('Personel eklenirken hata oluştu.');
        });
    };

    // Havuzdan bağımsız operatör ata
    self.assignOperatorFromPool = function() {
        var personnelId = self.selectedPoolOperatorId();
        var org = self.selectedOrganization();
        if (!personnelId || !org) return;

        ApiService.post('/customer-organizations/assign-personnel', {
            personnelId: personnelId,
            organizationId: org.id,
            supervisorId: null
        })
        .then(function() {
            toastr.success('Operatör organizasyona eklendi.');
            self.selectedPoolOperatorId(null);
            self.loadOrgPersonnel(org.id);
            self.loadOrganizations();
            self.loadPersonnelPool();
            self.notifyParent();
        })
        .catch(function(error) {
            console.error('Error assigning operator:', error);
            toastr.error('Operatör eklenirken hata oluştu.');
        });
    };

    // Havuzdan süpervizör ekibine operatör ata
    self.assignOperatorToSupervisor = function(supervisor) {
        var personnelId = self.selectedPoolOperatorForSupervisorId();
        var org = self.selectedOrganization();
        if (!personnelId || !org || !supervisor) return;

        ApiService.post('/customer-organizations/assign-personnel', {
            personnelId: personnelId,
            organizationId: org.id,
            supervisorId: supervisor.id
        })
        .then(function() {
            toastr.success('Operatör ekibe eklendi.');
            self.selectedPoolOperatorForSupervisorId(null);
            self.loadOrgPersonnel(org.id);
            self.loadOrganizations();
            self.loadPersonnelPool();
            self.notifyParent();
        })
        .catch(function(error) {
            console.error('Error assigning operator to supervisor:', error);
            toastr.error('Operatör eklenirken hata oluştu.');
        });
    };

    // ==================== OPERATOR CRUD ====================

    self.openAddOperatorModal = function(supervisor) {
        self.selectedSupervisorForOperator(supervisor);
        self.resetNewOperator();
        self.showAddOperatorModal();
    };

    self.addIndependentOperator = function() {
        self.selectedSupervisorForOperator(null);
        self.resetNewOperator();
        self.showAddOperatorModal();
    };

    self.resetNewOperator = function() {
        self.newOperator.firstName('');
        self.newOperator.lastName('');
        self.newOperator.username('');
        self.newOperator.email('');
        self.newOperator.password('');
    };

    self.saveNewOperator = function() {
        var org = self.selectedOrganization();
        if (!org) return;

        var firstName = self.newOperator.firstName();
        var lastName = self.newOperator.lastName();
        var username = self.newOperator.username();
        var email = self.newOperator.email();
        var password = self.newOperator.password();

        if (!firstName || !lastName || !username || !email || !password) {
            toastr.warning('Tüm alanları doldurun.');
            return;
        }

        var usernameRegex = /^[a-zA-Z0-9_.-]+$/;
        if (!usernameRegex.test(username)) {
            toastr.error('Kullanıcı adı sadece İngilizce harf, rakam, alt çizgi, nokta ve tire içerebilir.');
            return;
        }

        if (password.length < 6) {
            toastr.error('Şifre en az 6 karakter olmalıdır.');
            return;
        }

        self.isSavingOperator(true);

        var supervisor = self.selectedSupervisorForOperator();

        // Create personnel then assign to org
        ApiService.post('/customer-personnel', {
            customerId: self.customerId,
            firstName: firstName,
            lastName: lastName,
            username: username,
            email: email,
            password: password,
            role: "CustomerOperator",
            isActive: true
        })
        .then(function(newPersonnel) {
            return ApiService.post('/customer-organizations/assign-personnel', {
                personnelId: newPersonnel.id,
                organizationId: org.id,
                supervisorId: supervisor ? supervisor.id : null
            });
        })
        .then(function() {
            toastr.success('Operatör oluşturuldu ve organizasyona eklendi.');
            self.hideAddOperatorModal();
            self.loadOrgPersonnel(org.id);
            self.loadOrganizations();
            self.loadPersonnelPool();
            self.notifyParent();
        })
        .catch(function(error) {
            console.error('Error creating operator:', error);
            toastr.error(error.message || 'Operatör oluşturulurken hata oluştu.');
        })
        .finally(function() {
            self.isSavingOperator(false);
        });
    };

    // ==================== PERSONNEL ACTIONS ====================

    self.makeIndependent = function(personnel) {
        var org = self.selectedOrganization();
        if (!org) return;

        ApiService.post('/customer-organizations/' + org.id + '/personnel/' + personnel.id + '/make-independent')
            .then(function() {
                toastr.success('Personel bağımsız yapıldı.');
                self.loadOrgPersonnel(org.id);
                self.loadOrganizations();
                self.loadPersonnelPool();
            })
            .catch(function(error) {
                console.error('Error making independent:', error);
                toastr.error('İşlem sırasında hata oluştu.');
            });
    };

    self.removePersonnelFromOrg = function(personnel) {
        var org = self.selectedOrganization();
        if (!org) return;

        // Check if supervisor has team members
        if (personnel.teamMembers && personnel.teamMembers.length > 0) {
            // Show delegate modal
            self.personnelToRemove(personnel);
            self.selectedDelegateId(null);

            // Get available delegates (other supervisors)
            var delegates = self.orgPersonnel().supervisors.filter(function(s) {
                return s.id !== personnel.id;
            });
            self.availableDelegates(delegates);

            self.showDelegateModal();
            return;
        }

        deleteConfirmation.show(
            '<strong>' + personnel.fullName + '</strong> personelini organizasyondan çıkarmak istediğinize emin misiniz?',
            function() {
                ApiService.delete('/customer-organizations/' + org.id + '/personnel/' + personnel.id)
                    .then(function() {
                        toastr.success('Personel organizasyondan çıkarıldı.');
                        self.loadOrgPersonnel(org.id);
                        self.loadOrganizations();
                        self.loadPersonnelPool();
                        self.notifyParent();
                    })
                    .catch(function(error) {
                        console.error('Error removing personnel:', error);
                        toastr.error('İşlem sırasında hata oluştu.');
                    });
            }
        );
    };

    self.confirmRemoveWithDelegate = function() {
        var org = self.selectedOrganization();
        var personnel = self.personnelToRemove();
        var delegateId = self.selectedDelegateId();

        if (!org || !personnel || !delegateId) return;

        self.isRemovingWithDelegate(true);

        ApiService.post('/customer-organizations/' + org.id + '/personnel/' + personnel.id + '/transfer-and-remove', {
            newSupervisorId: delegateId
        })
        .then(function() {
            toastr.success('Ekip devredildi ve personel çıkarıldı.');
            self.hideDelegateModal();
            self.loadOrgPersonnel(org.id);
            self.loadOrganizations();
            self.loadPersonnelPool();
            self.notifyParent();
        })
        .catch(function(error) {
            console.error('Error transferring:', error);
            toastr.error('İşlem sırasında hata oluştu.');
        })
        .finally(function() {
            self.isRemovingWithDelegate(false);
        });
    };

    // ==================== MODAL HELPERS ====================

    self.showOrgModal = function() {
        if (!self.orgModal) {
            var el = document.getElementById('orgModal');
            if (el) self.orgModal = new bootstrap.Modal(el);
        }
        if (self.orgModal) self.orgModal.show();
    };

    self.hideOrgModal = function() {
        if (self.orgModal) self.orgModal.hide();
        self.editingOrganization(null);
    };

    self.showAddOperatorModal = function() {
        if (!self.addOperatorModal) {
            var el = document.getElementById('addOperatorModal');
            if (el) self.addOperatorModal = new bootstrap.Modal(el);
        }
        if (self.addOperatorModal) self.addOperatorModal.show();
    };

    self.hideAddOperatorModal = function() {
        if (self.addOperatorModal) self.addOperatorModal.hide();
    };

    self.showDelegateModal = function() {
        if (!self.delegateModal) {
            var el = document.getElementById('delegateModal');
            if (el) self.delegateModal = new bootstrap.Modal(el);
        }
        if (self.delegateModal) self.delegateModal.show();
    };

    self.hideDelegateModal = function() {
        if (self.delegateModal) self.delegateModal.hide();
        self.personnelToRemove(null);
        self.selectedDelegateId(null);
    };

    // ==================== NOTIFY PARENT ====================

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

    // ==================== INIT ====================

    self.init = function() {
        self.loadOrganizations();
        self.loadPersonnelPool();
    };

    self.init();
}

// Apply bindings
$(document).ready(function() {
    var app = document.getElementById('organizations-app');
    if (app) {
        window.vm = new OrganizationsViewModel();
        ko.applyBindings(window.vm, app);
    }
});
