// Organizations ViewModel - Şube/Organizasyon Popup
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

    // Modals
    self.editingOrganization = ko.observable(null);
    self.orgModal = null;
    self.addPersonnelModal = null;

    // Add personnel
    self.addPersonnelTitle = ko.observable('Personel Ekle');
    self.addPersonnelRole = ko.observable('2'); // 2=Supervisor, 3=Operator
    self.selectedPoolPersonnelId = ko.observable(null);
    self.newPersonnel = {
        firstName: ko.observable(''),
        lastName: ko.observable(''),
        username: ko.observable(''),
        email: ko.observable(''),
        password: ko.observable('')
    };

    // Load organizations
    self.loadOrganizations = function() {
        if (!self.customerId) return;

        self.isLoading(true);

        ApiService.get('/customer-organizations/by-customer/' + self.customerId)
            .then(function(data) {
                self.organizations(data || []);
            })
            .catch(function(error) {
                console.error('Error loading organizations:', error);
                toastr.error('Şubeler yüklenirken bir hata oluştu.');
            })
            .finally(function() {
                self.isLoading(false);
            });
    };

    // Load personnel pool
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

    // Select organization
    self.selectOrganization = function(org) {
        self.selectedOrganization(org);
        self.loadOrgPersonnel(org.id);
    };

    // Load organization personnel
    self.loadOrgPersonnel = function(orgId) {
        self.isLoadingPersonnel(true);

        ApiService.get('/customer-organizations/' + orgId + '/personnel')
            .then(function(data) {
                self.orgPersonnel(data || { supervisors: [], operators: [] });
            })
            .catch(function(error) {
                console.error('Error loading org personnel:', error);
                toastr.error('Personeller yüklenirken bir hata oluştu.');
            })
            .finally(function() {
                self.isLoadingPersonnel(false);
            });
    };

    // Create organization
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

    // Edit organization
    self.editOrganization = function() {
        var org = self.selectedOrganization();
        if (!org) return;

        self.editingOrganization({
            id: org.id,
            name: ko.observable(org.name),
            code: ko.observable(org.code || ''),
            description: ko.observable(org.description || ''),
            isActive: ko.observable(org.isActive)
        });
        self.showOrgModal();
    };

    // Save organization
    self.saveOrganization = function() {
        var org = self.editingOrganization();
        if (!org) return;

        var name = ko.unwrap(org.name);
        if (!name) {
            toastr.warning('Şube adı zorunludur.');
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
                toastr.success(isNew ? 'Şube oluşturuldu.' : 'Şube güncellendi.');
                self.hideOrgModal();
                self.notifyParent();
            })
            .catch(function(error) {
                console.error('Error saving organization:', error);
                toastr.error('Şube kaydedilirken bir hata oluştu.');
            })
            .finally(function() {
                self.isSaving(false);
            });
    };

    // Delete organization
    self.deleteOrganization = function() {
        var org = self.selectedOrganization();
        if (!org) return;

        deleteConfirmation.show(
            '<strong>' + org.name + '</strong> şubesini silmek istediğinize emin misiniz?',
            function() {
                ApiService.delete('/customer-organizations/' + org.id)
                    .then(function() {
                        self.organizations.remove(function(o) { return o.id === org.id; });
                        self.selectedOrganization(null);
                        self.orgPersonnel({ supervisors: [], operators: [] });
                        toastr.success('Şube silindi.');
                        self.notifyParent();
                    })
                    .catch(function(error) {
                        console.error('Error deleting organization:', error);
                        toastr.error('Şube silinirken bir hata oluştu.');
                    });
            }
        );
    };

    // Add supervisor
    self.addSupervisor = function() {
        self.addPersonnelTitle('Süpervizör Ekle');
        self.addPersonnelRole('2');
        self.resetNewPersonnel();
        self.showAddPersonnelModal();
    };

    // Add operator
    self.addOperator = function() {
        self.addPersonnelTitle('Operatör Ekle');
        self.addPersonnelRole('3');
        self.resetNewPersonnel();
        self.showAddPersonnelModal();
    };

    // Reset new personnel form
    self.resetNewPersonnel = function() {
        self.selectedPoolPersonnelId(null);
        self.newPersonnel.firstName('');
        self.newPersonnel.lastName('');
        self.newPersonnel.username('');
        self.newPersonnel.email('');
        self.newPersonnel.password('');
    };

    // Assign from pool
    self.assignFromPool = function() {
        var personnelId = self.selectedPoolPersonnelId();
        var org = self.selectedOrganization();
        if (!personnelId || !org) return;

        ApiService.post('/customer-organizations/assign-personnel', {
            personnelId: personnelId,
            organizationId: org.id
        })
        .then(function() {
            toastr.success('Personel şubeye eklendi.');
            self.hideAddPersonnelModal();
            self.loadOrgPersonnel(org.id);
            self.loadOrganizations();
            self.loadPersonnelPool();
            self.notifyParent();
        })
        .catch(function(error) {
            console.error('Error assigning personnel:', error);
            toastr.error('Personel eklenirken bir hata oluştu.');
        });
    };

    // Create and assign
    self.createAndAssign = function() {
        var org = self.selectedOrganization();
        if (!org) return;

        var firstName = self.newPersonnel.firstName();
        var lastName = self.newPersonnel.lastName();
        var username = self.newPersonnel.username();
        var email = self.newPersonnel.email();
        var password = self.newPersonnel.password();

        if (!firstName || !lastName || !username || !email || !password) {
            toastr.warning('Tüm alanları doldurun.');
            return;
        }

        // Username validation
        var usernameRegex = /^[a-zA-Z0-9_.-]+$/;
        if (!usernameRegex.test(username)) {
            toastr.error('Kullanıcı adı sadece İngilizce harf, rakam, alt çizgi, nokta ve tire içerebilir.');
            return;
        }

        if (password.length < 6) {
            toastr.error('Şifre en az 6 karakter olmalıdır.');
            return;
        }

        self.isSaving(true);

        // First create personnel
        ApiService.post('/customer-personnel', {
            customerId: self.customerId,
            firstName: firstName,
            lastName: lastName,
            username: username,
            email: email,
            password: password,
            role: self.addPersonnelRole(),
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
            toastr.success('Personel oluşturuldu ve şubeye eklendi.');
            self.hideAddPersonnelModal();
            self.loadOrgPersonnel(org.id);
            self.loadOrganizations();
            self.loadPersonnelPool();
            self.notifyParent();
        })
        .catch(function(error) {
            console.error('Error creating personnel:', error);
            var msg = error.message || 'Personel oluşturulurken bir hata oluştu.';
            toastr.error(msg);
        })
        .finally(function() {
            self.isSaving(false);
        });
    };

    // Remove from organization
    self.removeFromOrg = function(personnel) {
        var org = self.selectedOrganization();
        if (!org) return;

        deleteConfirmation.show(
            '<strong>' + personnel.fullName + '</strong> personelini şubeden çıkarmak istediğinize emin misiniz?',
            function() {
                ApiService.delete('/customer-organizations/' + org.id + '/personnel/' + personnel.id)
                    .then(function() {
                        toastr.success('Personel şubeden çıkarıldı.');
                        self.loadOrgPersonnel(org.id);
                        self.loadOrganizations();
                        self.loadPersonnelPool();
                        self.notifyParent();
                    })
                    .catch(function(error) {
                        console.error('Error removing personnel:', error);
                        toastr.error('Personel çıkarılırken bir hata oluştu.');
                    });
            }
        );
    };

    // Modal helpers
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

    self.showAddPersonnelModal = function() {
        if (!self.addPersonnelModal) {
            var el = document.getElementById('addPersonnelModal');
            if (el) self.addPersonnelModal = new bootstrap.Modal(el);
        }
        if (self.addPersonnelModal) self.addPersonnelModal.show();
    };

    self.hideAddPersonnelModal = function() {
        if (self.addPersonnelModal) self.addPersonnelModal.hide();
    };

    // Notify parent
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
        self.loadOrganizations();
        self.loadPersonnelPool();
    };

    self.init();
}

// Apply bindings
$(document).ready(function() {
    var app = document.getElementById('organizations-app');
    if (app) {
        ko.applyBindings(new OrganizationsViewModel(), app);
    }
});
