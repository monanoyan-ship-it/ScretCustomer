// Customer Organizations Page ViewModel
function CustomerOrganizationsViewModel() {
    var self = this;

    // Observables
    self.isLoading = ko.observable(true);
    self.errorMessage = ko.observable('');
    self.modalErrorMessage = ko.observable('');

    // Customers
    self.customers = ko.observableArray([]);
    self.customerSearchText = ko.observable('');
    self.selectedCustomer = ko.observable(null);

    // Organizations (flat list)
    self.organizations = ko.observableArray([]);
    self.isLoadingOrganizations = ko.observable(false);

    // Selected Organization & Personnel
    self.selectedOrganization = ko.observable(null);
    self.personnelData = ko.observable({ supervisors: [], operators: [] });
    self.isLoadingPersonnel = ko.observable(false);

    // Unassigned Personnel (OrganizationId = null)
    self.unassignedPersonnel = ko.observableArray([]);
    self.isLoadingUnassigned = ko.observable(false);
    self.showUnassigned = ko.observable(false);

    // Modal
    self.isModalOpen = ko.observable(false);
    self.isSaving = ko.observable(false);
    self.editingOrganization = ko.observable(null);

    // Personnel Modal
    self.isPersonnelModalOpen = ko.observable(false);
    self.isSavingPersonnel = ko.observable(false);
    self.editingPersonnel = ko.observable(null);
    self.customerPersonnelRoles = ko.observableArray([]);

    // Move Modal
    self.isMoveModalOpen = ko.observable(false);
    self.movingPerson = ko.observable(null);
    self.selectedTargetOrg = ko.observable(null);
    self.targetOrgPersonnel = ko.observable({ supervisors: [], operators: [] });
    self.selectedTargetSupervisor = ko.observable(null); // null = bağımsız
    self.moveWithTeam = ko.observable(false);
    self.isMoving = ko.observable(false);
    self.isLoadingTargetPersonnel = ko.observable(false);

    // Available target organizations (exclude current)
    self.availableTargetOrgs = ko.computed(function() {
        var currentOrg = self.selectedOrganization();
        if (!currentOrg) return [];
        return self.organizations().filter(function(org) {
            return org.id !== currentOrg.id;
        });
    });

    // Filtered customers
    self.filteredCustomers = ko.computed(function() {
        var search = self.customerSearchText().toLowerCase();
        if (!search) return self.customers();
        return self.customers().filter(function(c) {
            return c.companyName.toLowerCase().indexOf(search) > -1;
        });
    });

    // Load customers
    self.loadCustomers = function() {
        self.isLoading(true);
        self.errorMessage('');

        ApiService.get('/customers')
            .then(function(data) {
                var customersWithCount = data.map(function(c) {
                    c.organizationCount = c.organizationCount || 0;
                    return c;
                });
                self.customers(customersWithCount);
            })
            .catch(function(error) {
                console.error('Error loading customers:', error);
                toastr.error('Müşteriler yüklenirken bir hata oluştu.');
            })
            .finally(function() {
                self.isLoading(false);
            });
    };

    // Select customer
    self.selectCustomer = function(customer) {
        self.selectedCustomer(customer);
        self.selectedOrganization(null);
        self.personnelData({ supervisors: [], operators: [] });
        self.loadOrganizations(customer.id);
    };

    // Load organizations for customer
    self.loadOrganizations = function(customerId) {
        self.isLoadingOrganizations(true);
        self.organizations([]);

        ApiService.get('/customer-organizations/by-customer/' + customerId + '?includeInactive=true')
            .then(function(data) {
                var orgs = (data || []).map(function(org) {
                    org.isSelected = ko.observable(false);
                    return org;
                });
                self.organizations(orgs);
                if (self.selectedCustomer()) {
                    self.selectedCustomer().organizationCount = data.length;
                }
                // Atanmamış personelleri de yükle
                self.loadUnassignedPersonnel(customerId);
            })
            .catch(function(error) {
                console.error('Error loading organizations:', error);
                toastr.error('Organizasyonlar yüklenirken bir hata oluştu.');
            })
            .finally(function() {
                self.isLoadingOrganizations(false);
            });
    };

    // Load unassigned personnel for customer
    self.loadUnassignedPersonnel = function(customerId) {
        self.isLoadingUnassigned(true);
        self.unassignedPersonnel([]);

        ApiService.get('/customer-personnel/by-customer/' + customerId + '?includeInactive=true')
            .then(function(data) {
                // Multi-org destekli: Hiçbir organizasyona atanmamış olanları filtrele
                // organizationAssignmentCount = 0 veya (organizationId null VE organizationIds boş)
                var unassigned = (data || []).filter(function(p) {
                    var hasNoOrg = !p.organizationId && (!p.organizationIds || p.organizationIds.length === 0);
                    var noAssignments = p.organizationAssignmentCount === 0;
                    return hasNoOrg || noAssignments;
                });
                self.unassignedPersonnel(unassigned);
            })
            .catch(function(error) {
                console.error('Error loading unassigned personnel:', error);
            })
            .finally(function() {
                self.isLoadingUnassigned(false);
            });
    };

    // Toggle unassigned panel
    self.toggleUnassigned = function() {
        self.showUnassigned(!self.showUnassigned());
        // Diğer organizasyonları kapat
        if (self.showUnassigned()) {
            self.organizations().forEach(function(o) { o.isSelected(false); });
            self.selectedOrganization(null);
            self.personnelData({ supervisors: [], operators: [] });
        }
    };

    // Toggle organization - show/hide personnel
    self.toggleOrganization = function(org) {
        var wasSelected = org.isSelected();

        // Önce tüm seçimleri kapat
        self.organizations().forEach(function(o) {
            o.isSelected(false);
        });
        self.showUnassigned(false); // Atanmamış panelini kapat

        if (!wasSelected) {
            org.isSelected(true);
            self.selectedOrganization(org);
            self.loadPersonnel(org.id);
        } else {
            self.selectedOrganization(null);
            self.personnelData({ supervisors: [], operators: [] });
        }
    };

    // Load personnel for organization
    self.loadPersonnel = function(organizationId) {
        self.isLoadingPersonnel(true);
        self.personnelData({ supervisors: [], operators: [] });

        ApiService.get('/customer-organizations/' + organizationId + '/personnel')
            .then(function(data) {
                self.personnelData({
                    supervisors: data.supervisors || [],
                    operators: data.operators || []
                });
            })
            .catch(function(error) {
                console.error('Error loading personnel:', error);
                toastr.error('Personeller yüklenirken hata oluştu.');
            })
            .finally(function() {
                self.isLoadingPersonnel(false);
            });
    };

    // ========== MOVE MODAL ==========

    self.openMoveModal = function(person) {
        self.movingPerson(person);
        self.selectedTargetOrg(null);
        self.targetOrgPersonnel({ supervisors: [], operators: [] });
        self.selectedTargetSupervisor(null);
        self.moveWithTeam(false);
        self.isMoveModalOpen(true);
    };

    self.closeMoveModal = function() {
        self.isMoveModalOpen(false);
        self.movingPerson(null);
        self.selectedTargetOrg(null);
        self.targetOrgPersonnel({ supervisors: [], operators: [] });
        self.selectedTargetSupervisor(null);
    };

    self.selectTargetOrg = function(org) {
        self.selectedTargetOrg(org);
        self.selectedTargetSupervisor(null);
        self.loadTargetOrgPersonnel(org.id);
    };

    self.loadTargetOrgPersonnel = function(orgId) {
        self.isLoadingTargetPersonnel(true);
        self.targetOrgPersonnel({ supervisors: [], operators: [] });

        ApiService.get('/customer-organizations/' + orgId + '/personnel')
            .then(function(data) {
                self.targetOrgPersonnel({
                    supervisors: data.supervisors || [],
                    operators: data.operators || []
                });
            })
            .catch(function(error) {
                console.error('Error loading target personnel:', error);
            })
            .finally(function() {
                self.isLoadingTargetPersonnel(false);
            });
    };

    self.selectTargetSupervisor = function(supervisor) {
        // null means independent
        self.selectedTargetSupervisor(supervisor);
    };

    // Tasima: Mevcut org'dan cikarir, yeni org'a ekler
    self.executeMove = function() {
        var person = self.movingPerson();
        var targetOrg = self.selectedTargetOrg();
        var targetSupervisor = self.selectedTargetSupervisor();
        var includeTeam = self.moveWithTeam();

        if (!person || !targetOrg) {
            toastr.error('Lutfen hedef organizasyon secin.');
            return;
        }

        self.isMoving(true);

        var teamMemberIds = [];
        if (includeTeam && person.teamMembers && person.teamMembers.length > 0) {
            teamMemberIds = person.teamMembers.map(function(tm) { return tm.id; });
        }

        var targetOrgName = targetOrg.name;
        var targetSupervisorName = targetSupervisor ? targetSupervisor.fullName : 'Bagimsiz';

        // Klasik tasima: Eski API'yi kullan
        var movePromise = ApiService.put('/customer-personnel/' + person.id + '/change-organization', {
            newOrganizationId: targetOrg.id
        });

        // Set supervisor
        movePromise = movePromise.then(function() {
            var supervisorId = targetSupervisor ? targetSupervisor.id : null;
            return ApiService.put('/customer-organizations/personnel/' + person.id + '/supervisor', supervisorId);
        });

        // Move team members
        if (includeTeam && teamMemberIds.length > 0) {
            movePromise = movePromise.then(function() {
                var chain = Promise.resolve();
                teamMemberIds.forEach(function(tmId) {
                    chain = chain.then(function() {
                        return ApiService.put('/customer-personnel/' + tmId + '/change-organization', {
                            newOrganizationId: targetOrg.id
                        });
                    });
                });
                return chain;
            });
        }

        movePromise
            .then(function() {
                var movedText = includeTeam && teamMemberIds.length > 0
                    ? person.fullName + ' ve ' + teamMemberIds.length + ' ekip uyesi'
                    : person.fullName;

                var message = '<strong>' + movedText + '</strong> tasindi<br>' +
                    '<small>Hedef: ' + targetOrgName + '</small><br>' +
                    '<small>Supervizor: ' + targetSupervisorName + '</small>';

                toastr.success(message, 'Tasima Basarili');
                self.closeMoveModal();

                if (self.selectedOrganization()) {
                    self.loadPersonnel(self.selectedOrganization().id);
                }
                self.loadOrganizations(self.selectedCustomer().id);
            })
            .catch(function(error) {
                console.error('Error moving personnel:', error);
                toastr.error(error.message || 'Tasima sirasinda hata olustu.');
            })
            .finally(function() {
                self.isMoving(false);
            });
    };

    // Kopyalama: Mevcut org'da kalir, yeni org'a da eklenir (multi-org)
    // Ayni organizasyonda farkli supervizore de atanabilir
    self.executeCopy = function() {
        var person = self.movingPerson();
        var targetOrg = self.selectedTargetOrg();
        var targetSupervisor = self.selectedTargetSupervisor();
        var includeTeam = self.moveWithTeam();

        if (!person || !targetOrg) {
            toastr.error('Lutfen hedef organizasyon secin.');
            return;
        }

        // Ayni organizasyonda ayni supervizore atama yapilamaz
        var currentOrg = self.selectedOrganization();
        if (currentOrg && targetOrg.id === currentOrg.id) {
            // Ayni organizasyona kopyalama - farkli supervizor secilmeli
            var currentSupervisorId = person.supervisorId || null;
            var targetSupervisorId = targetSupervisor ? targetSupervisor.id : null;

            if (currentSupervisorId === targetSupervisorId) {
                toastr.error('Ayni organizasyonda ayni supervizore atama yapilamaz. Lutfen farkli bir supervizor secin.');
                return;
            }
        }

        self.isMoving(true);

        var teamMemberIds = [];
        if (person.teamMembers && person.teamMembers.length > 0) {
            teamMemberIds = person.teamMembers.map(function(tm) { return tm.id; });
        }

        var targetOrgName = targetOrg.name;
        var targetSupervisorName = targetSupervisor ? targetSupervisor.fullName : 'Bagimsiz';

        // Süpervizör başka bir süpervizörün altına kopyalanıyorsa:
        // Kendisi kopyalanmaz, sadece ekip üyeleri kopyalanır
        var isSupervisorToSupervisor = targetSupervisor && person.teamMembers && person.teamMembers.length > 0;

        var copyPromise;

        if (isSupervisorToSupervisor) {
            // Sadece ekip üyelerini hedef süpervizörün altına ekle
            copyPromise = Promise.resolve();
            teamMemberIds.forEach(function(tmId) {
                copyPromise = copyPromise.then(function() {
                    return ApiService.post('/customer-organizations/' + targetOrg.id + '/personnel/' + tmId, {
                        supervisorId: targetSupervisor.id
                    });
                });
            });
        } else {
            // Normal kopyalama: kişiyi ekle
            copyPromise = ApiService.post('/customer-organizations/' + targetOrg.id + '/personnel/' + person.id, {
                supervisorId: targetSupervisor ? targetSupervisor.id : null
            });

            // Ekip uyelerini de ekle (ekiple birlikte taşı seçiliyse)
            if (includeTeam && teamMemberIds.length > 0) {
                copyPromise = copyPromise.then(function() {
                    var chain = Promise.resolve();
                    teamMemberIds.forEach(function(tmId) {
                        chain = chain.then(function() {
                            return ApiService.post('/customer-organizations/' + targetOrg.id + '/personnel/' + tmId, {
                                supervisorId: targetSupervisor ? targetSupervisor.id : null
                            });
                        });
                    });
                    return chain;
                });
            }
        }

        copyPromise
            .then(function() {
                var copiedText;
                var message;

                if (isSupervisorToSupervisor) {
                    // Süpervizörden süpervizöre: sadece ekip üyeleri kopyalandı
                    copiedText = teamMemberIds.length + ' ekip üyesi';
                    message = '<strong>' + copiedText + '</strong> kopyalandı<br>' +
                        '<small>Kaynak: ' + person.fullName + ' ekibi</small><br>' +
                        '<small>Hedef: ' + targetSupervisorName + ' ekibi</small>';
                } else {
                    copiedText = includeTeam && teamMemberIds.length > 0
                        ? person.fullName + ' ve ' + teamMemberIds.length + ' ekip uyesi'
                        : person.fullName;
                    message = '<strong>' + copiedText + '</strong> eklendi<br>' +
                        '<small>Hedef: ' + targetOrgName + '</small><br>' +
                        '<small>Supervizor: ' + targetSupervisorName + '</small><br>' +
                        '<small class="text-info"><i class="bi bi-info-circle"></i> Mevcut organizasyonda da kalmaya devam ediyor</small>';
                }

                toastr.success(message, 'Kopyalama Basarili');
                self.closeMoveModal();

                if (self.selectedOrganization()) {
                    self.loadPersonnel(self.selectedOrganization().id);
                }
                self.loadOrganizations(self.selectedCustomer().id);
            })
            .catch(function(error) {
                console.error('Error copying personnel:', error);
                toastr.error(error.message || 'Kopyalama sirasinda hata olustu.');
            })
            .finally(function() {
                self.isMoving(false);
            });
    };

    // ========== EKİPTEN ÇIKARMA ==========

    self.removeFromTeam = function(person, supervisor) {
        var orgId = self.selectedOrganization().id;
        var personName = person.fullName;
        var supervisorName = supervisor.fullName;

        showConfirmModal({
            title: 'Ekipten Çıkar',
            message: personName + ' kişisini ' + supervisorName + ' ekibinden çıkarmak istediğinize emin misiniz?',
            type: 'danger',
            confirmText: 'Evet, Çıkar',
            confirmIcon: 'bi-box-arrow-right',
            onConfirm: function() {
                ApiService.delete('/customer-organizations/' + orgId + '/personnel/' + person.id + '/supervisor/' + supervisor.id)
                    .then(function() {
                        toastr.success(personName + ' ekipten çıkarıldı.');
                        self.loadPersonnel(orgId);
                        self.loadOrganizations(self.selectedCustomer().id);
                    })
                    .catch(function(error) {
                        console.error('Error removing from team:', error);
                        toastr.error(error.message || 'Ekipten çıkarma sırasında hata oluştu.');
                    });
            }
        });
    };

    self.removeFromOrganization = function(person) {
        var orgId = self.selectedOrganization().id;
        var orgName = self.selectedOrganization().name;
        var personName = person.fullName;

        showConfirmModal({
            title: 'Organizasyondan Çıkar',
            message: personName + ' kişisini ' + orgName + ' organizasyonundan çıkarmak istediğinize emin misiniz?',
            type: 'danger',
            confirmText: 'Evet, Çıkar',
            confirmIcon: 'bi-box-arrow-right',
            onConfirm: function() {
                ApiService.delete('/customer-organizations/' + orgId + '/personnel/' + person.id + '/v2')
                    .then(function() {
                        toastr.success(personName + ' organizasyondan çıkarıldı.');
                        self.loadPersonnel(orgId);
                        self.loadOrganizations(self.selectedCustomer().id);
                    })
                    .catch(function(error) {
                        console.error('Error removing from organization:', error);
                        toastr.error(error.message || 'Organizasyondan çıkarma sırasında hata oluştu.');
                    });
            }
        });
    };

    // ========== ORGANIZATION CRUD ==========

    self.createNewOrganization = function() {
        if (!self.selectedCustomer()) {
            toastr.error('Lütfen önce bir müşteri seçin.');
            return;
        }

        self.editingOrganization({
            id: null,
            name: ko.observable(''),
            code: ko.observable(''),
            description: ko.observable(''),
            order: ko.observable(0),
            isActive: ko.observable(true),
            customerId: self.selectedCustomer().id
        });
        self.isModalOpen(true);
    };

    self.editOrganization = function(org, event) {
        if (event) event.stopPropagation();

        ApiService.get('/customer-organizations/' + org.id)
            .then(function(data) {
                self.editingOrganization({
                    id: data.id,
                    name: ko.observable(data.name),
                    code: ko.observable(data.code || ''),
                    description: ko.observable(data.description || ''),
                    order: ko.observable(data.order || 0),
                    isActive: ko.observable(data.isActive),
                    customerId: data.customerId
                });
                self.isModalOpen(true);
            })
            .catch(function(error) {
                console.error('Error loading organization:', error);
                toastr.error('Organizasyon yüklenirken hata oluştu.');
            });
    };

    self.saveOrganization = function() {
        var org = self.editingOrganization();
        if (!org) return;

        var name = ko.unwrap(org.name);
        if (!name || name.trim() === '') {
            toastr.error('Organizasyon adı zorunludur.');
            return;
        }

        self.isSaving(true);
        var data = {
            name: name.trim(),
            code: ko.unwrap(org.code) || null,
            description: ko.unwrap(org.description) || null,
            order: parseInt(ko.unwrap(org.order)) || 0,
            isActive: ko.unwrap(org.isActive),
            customerId: org.customerId
        };

        var isNew = !org.id;
        var promise = isNew
            ? ApiService.post('/customer-organizations', data)
            : ApiService.put('/customer-organizations/' + org.id, data);

        promise
            .then(function(savedOrg) {
                self.closeModal();

                if (isNew) {
                    savedOrg.isSelected = ko.observable(false);
                    self.organizations.push(savedOrg);
                    if (self.selectedCustomer()) {
                        self.selectedCustomer().organizationCount++;
                    }
                } else {
                    var list = self.organizations();
                    for (var i = 0; i < list.length; i++) {
                        if (list[i].id === savedOrg.id) {
                            savedOrg.isSelected = list[i].isSelected;
                            self.organizations.splice(i, 1, savedOrg);
                            break;
                        }
                    }
                }

                toastr.success(isNew ? 'Organizasyon oluşturuldu.' : 'Organizasyon güncellendi.');
            })
            .catch(function(error) {
                console.error('Error saving organization:', error);
                toastr.error(error.message || 'Organizasyon kaydedilirken hata oluştu.');
            })
            .finally(function() {
                self.isSaving(false);
            });
    };

    self.deleteOrganization = function(org, event) {
        if (event) event.stopPropagation();

        showDeleteConfirm('"' + org.name + '" organizasyonu', function() {
                ApiService.delete('/customer-organizations/' + org.id)
                    .then(function() {
                        self.organizations.remove(org);
                        if (self.selectedCustomer() && self.selectedCustomer().organizationCount > 0) {
                            self.selectedCustomer().organizationCount--;
                        }
                        if (self.selectedOrganization() && self.selectedOrganization().id === org.id) {
                            self.selectedOrganization(null);
                            self.personnelData({ supervisors: [], operators: [] });
                        }
                        toastr.success('Organizasyon silindi.');
                    })
                    .catch(function(error) {
                        console.error('Error deleting organization:', error);
                        toastr.error(error.message || 'Organizasyon silinirken hata oluştu.');
                    });
            }
        );
    };

    self.closeModal = function() {
        self.isModalOpen(false);
        self.editingOrganization(null);
    };

    // ========== PERSONNEL CRUD (No Organization) ==========

    self.createNewPersonnel = function() {
        if (!self.selectedCustomer()) {
            toastr.error('Lütfen önce bir müşteri seçin.');
            return;
        }

        self.editingPersonnel({
            customerId: self.selectedCustomer().id,
            username: '',
            email: '',
            password: '',
            firstName: '',
            lastName: '',
            phoneNumber: '',
            department: '',
            title: '',
            role: '',
            isActive: true,
            notes: ''
        });
        self.isPersonnelModalOpen(true);
    };

    self.closePersonnelModal = function() {
        self.isPersonnelModalOpen(false);
        self.editingPersonnel(null);
    };

    self.savePersonnel = function() {
        var personnel = self.editingPersonnel();
        if (!personnel) return;

        // Validation
        if (!personnel.username || !personnel.email || !personnel.firstName || !personnel.lastName) {
            toastr.error('Kullanıcı adı, e-posta, ad ve soyad zorunludur.');
            return;
        }

        // Username format validation
        var usernameRegex = /^[a-zA-Z0-9_.-]+$/;
        if (!usernameRegex.test(personnel.username)) {
            toastr.error('Kullanıcı adı sadece İngilizce harf, rakam, alt çizgi, nokta ve tire içerebilir.');
            return;
        }

        if (!personnel.password || personnel.password.length < 6) {
            toastr.error('Şifre en az 6 karakter olmalıdır.');
            return;
        }

        if (!personnel.role) {
            toastr.error('Rol seçimi zorunludur.');
            return;
        }

        self.isSavingPersonnel(true);

        // Check username uniqueness
        fetch('/api/customer-personnel/check-username/' + encodeURIComponent(personnel.username), { credentials: 'include' })
            .then(function(response) { return response.json(); })
            .then(function(data) {
                if (data.exists) {
                    self.isSavingPersonnel(false);
                    toastr.error('Bu kullanıcı adı zaten kullanılıyor.');
                    return Promise.reject('username_exists');
                }
                // Check email
                return fetch('/api/customer-personnel/check-email/' + encodeURIComponent(personnel.email), { credentials: 'include' });
            })
            .then(function(response) { return response.json(); })
            .then(function(data) {
                if (data.exists) {
                    self.isSavingPersonnel(false);
                    toastr.error('Bu e-posta adresi zaten kullanılıyor.');
                    return Promise.reject('email_exists');
                }
                // All checks passed, proceed with save
                return self.doSavePersonnel(personnel);
            })
            .catch(function(error) {
                if (error !== 'username_exists' && error !== 'email_exists') {
                    console.error('Error checking uniqueness:', error);
                    self.isSavingPersonnel(false);
                }
            });
    };

    self.doSavePersonnel = function(personnel) {
        var dataToSend = {
            customerId: personnel.customerId,
            username: personnel.username,
            email: personnel.email,
            password: personnel.password,
            firstName: personnel.firstName,
            lastName: personnel.lastName,
            phoneNumber: personnel.phoneNumber || null,
            department: personnel.department || null,
            title: personnel.title || null,
            role: parseInt(personnel.role, 10),
            isActive: personnel.isActive,
            notes: personnel.notes || null
            // organizationId is intentionally NOT sent - personnel will have no organization
        };

        return ApiService.post('/customer-personnel', dataToSend)
            .then(function() {
                toastr.success('Personel başarıyla oluşturuldu.');
                self.closePersonnelModal();
                // Reload unassigned personnel
                self.loadUnassignedPersonnel(self.selectedCustomer().id);
            })
            .catch(function(error) {
                console.error('Error saving personnel:', error);
                toastr.error('Personel kaydedilirken bir hata oluştu: ' + (error.message || ''));
            })
            .finally(function() {
                self.isSavingPersonnel(false);
            });
    };

    // Initialize
    EnumsService.load().then(function() {
        // Load customer personnel roles for dropdown
        if (EnumsService.cache && EnumsService.cache.customerPersonnelRoles) {
            self.customerPersonnelRoles(EnumsService.toSelectOptions(EnumsService.cache.customerPersonnelRoles));
        }
        self.loadCustomers();
    });
}

// Initialize on page load
$(document).ready(function() {
    var appElement = document.getElementById('customer-orgs-app');
    if (appElement) {
        ko.applyBindings(new CustomerOrganizationsViewModel(), appElement);
    }
});
