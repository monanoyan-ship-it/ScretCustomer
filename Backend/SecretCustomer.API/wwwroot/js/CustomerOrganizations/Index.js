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
    self.personnel = ko.observableArray([]);
    self.isLoadingPersonnel = ko.observable(false);

    // Drag & Drop
    self.draggedPersonnel = ko.observable(null);
    self.dropTargetOrgId = ko.observable(null);

    // Modal
    self.isModalOpen = ko.observable(false);
    self.isSaving = ko.observable(false);
    self.editingOrganization = ko.observable(null);

    // Filtered customers
    self.filteredCustomers = ko.computed(function() {
        var search = self.customerSearchText().toLowerCase();
        if (!search) {
            return self.customers();
        }
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
        self.personnel([]);
        self.loadOrganizations(customer.id);
    };

    // Load organizations for customer
    self.loadOrganizations = function(customerId) {
        self.isLoadingOrganizations(true);
        self.organizations([]);

        ApiService.get('/customer-organizations/by-customer/' + customerId + '?includeInactive=true')
            .then(function(data) {
                // Add isSelected observable to each org
                var orgs = (data || []).map(function(org) {
                    org.isSelected = ko.observable(false);
                    return org;
                });
                self.organizations(orgs);
                if (self.selectedCustomer()) {
                    self.selectedCustomer().organizationCount = data.length;
                }
            })
            .catch(function(error) {
                console.error('Error loading organizations:', error);
                toastr.error('Organizasyonlar yüklenirken bir hata oluştu.');
            })
            .finally(function() {
                self.isLoadingOrganizations(false);
            });
    };

    // Toggle organization - show/hide personnel
    self.toggleOrganization = function(org) {
        var wasSelected = org.isSelected();

        // Deselect all organizations
        self.organizations().forEach(function(o) {
            o.isSelected(false);
        });

        if (!wasSelected) {
            org.isSelected(true);
            self.selectedOrganization(org);
            self.loadPersonnel(org.id);
        } else {
            self.selectedOrganization(null);
            self.personnel([]);
        }
    };

    // Load personnel for organization (same endpoint as Customers page)
    self.loadPersonnel = function(organizationId) {
        self.isLoadingPersonnel(true);
        self.personnel([]);

        ApiService.get('/customer-organizations/' + organizationId + '/personnel')
            .then(function(data) {
                // Flatten hierarchical data to flat list
                var allPersonnel = [];

                // Add supervisors and their team members
                if (data && data.supervisors) {
                    data.supervisors.forEach(function(sup) {
                        allPersonnel.push({
                            id: sup.id,
                            fullName: sup.fullName,
                            title: sup.title || '',
                            roleName: sup.roleName,
                            isActive: sup.isActive !== false
                        });
                        // Add team members
                        if (sup.teamMembers) {
                            sup.teamMembers.forEach(function(tm) {
                                allPersonnel.push({
                                    id: tm.id,
                                    fullName: tm.fullName,
                                    title: tm.title || '',
                                    roleName: tm.roleName,
                                    isActive: tm.isActive !== false
                                });
                            });
                        }
                    });
                }

                // Add independent operators
                if (data && data.operators) {
                    data.operators.forEach(function(op) {
                        allPersonnel.push({
                            id: op.id,
                            fullName: op.fullName,
                            title: op.title || '',
                            roleName: op.roleName,
                            isActive: op.isActive !== false
                        });
                    });
                }

                self.personnel(allPersonnel);
            })
            .catch(function(error) {
                console.error('Error loading personnel:', error);
                toastr.error('Personeller yüklenirken hata oluştu.');
            })
            .finally(function() {
                self.isLoadingPersonnel(false);
            });
    };

    // ========== DRAG & DROP FOR PERSONNEL ==========

    self.onPersonnelDragStart = function(person, event) {
        var e = event.originalEvent || event;
        self.draggedPersonnel(person);
        e.target.classList.add('dragging');
        e.dataTransfer.setData('text/plain', person.id);
        e.dataTransfer.effectAllowed = 'move';
    };

    self.onOrgDragOver = function(org, event) {
        var e = event.originalEvent || event;
        e.preventDefault();
        e.dataTransfer.dropEffect = 'move';
        if (self.draggedPersonnel() && self.selectedOrganization() && self.selectedOrganization().id !== org.id) {
            self.dropTargetOrgId(org.id);
        }
    };

    self.onOrgDragLeave = function(org, event) {
        self.dropTargetOrgId(null);
    };

    self.onOrgDrop = function(org, event) {
        var e = event.originalEvent || event;
        e.preventDefault();
        self.dropTargetOrgId(null);

        var draggedPerson = self.draggedPersonnel();
        if (!draggedPerson || !self.selectedOrganization() || self.selectedOrganization().id === org.id) {
            self.draggedPersonnel(null);
            return;
        }

        // Change organization
        self.changePersonnelOrganization(draggedPerson.id, org.id, org.name);
        self.draggedPersonnel(null);
    };

    self.changePersonnelOrganization = function(personnelId, newOrgId, newOrgName) {
        ApiService.put('/customer-personnel/' + personnelId + '/change-organization', { newOrganizationId: newOrgId })
            .then(function() {
                toastr.success('Personel "' + newOrgName + '" organizasyonuna taşındı.');
                // Reload current org's personnel
                if (self.selectedOrganization()) {
                    self.loadPersonnel(self.selectedOrganization().id);
                }
                // Update personnel counts
                self.loadOrganizations(self.selectedCustomer().id);
            })
            .catch(function(error) {
                console.error('Error changing organization:', error);
                toastr.error(error.message || 'Organizasyon değiştirme sırasında hata oluştu.');
            });
    };

    // Create new organization
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
        self.modalErrorMessage('');
        self.isModalOpen(true);
    };

    // Edit organization
    self.editOrganization = function(org, event) {
        if (event) {
            event.stopPropagation();
        }

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
                self.modalErrorMessage('');
                self.isModalOpen(true);
            })
            .catch(function(error) {
                console.error('Error loading organization:', error);
                toastr.error('Organizasyon yüklenirken hata oluştu.');
            });
    };

    // Save organization
    self.saveOrganization = function() {
        var org = self.editingOrganization();
        if (!org) return;

        var name = ko.unwrap(org.name);
        if (!name || name.trim() === '') {
            toastr.error('Organizasyon adı zorunludur.');
            return;
        }

        self.isSaving(true);
        self.modalErrorMessage('');

        var data = {
            name: name.trim(),
            code: ko.unwrap(org.code) || null,
            description: ko.unwrap(org.description) || null,
            order: parseInt(ko.unwrap(org.order)) || 0,
            isActive: ko.unwrap(org.isActive),
            customerId: org.customerId
        };

        var isNew = !org.id;
        var promise;
        if (isNew) {
            promise = ApiService.post('/customer-organizations', data);
        } else {
            promise = ApiService.put('/customer-organizations/' + org.id, data);
        }

        promise
            .then(function(savedOrg) {
                self.closeModal();

                if (isNew) {
                    // Yeni kayit: array'e ekle
                    savedOrg.isSelected = ko.observable(false);
                    self.organizations.push(savedOrg);
                    // Update customer's organization count
                    if (self.selectedCustomer()) {
                        self.selectedCustomer().organizationCount++;
                    }
                } else {
                    // Guncelleme: array'de bul ve guncelle
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
                var errorMsg = 'Organizasyon kaydedilirken bir hata oluştu.';
                if (error && error.message) {
                    errorMsg = error.message;
                }
                toastr.error(errorMsg);
            })
            .finally(function() {
                self.isSaving(false);
            });
    };

    // Delete organization
    self.deleteOrganization = function(org, event) {
        if (event) {
            event.stopPropagation();
        }

        showDeleteConfirmation(
            'Organizasyon Sil',
            '"' + org.name + '" organizasyonunu silmek istediğinize emin misiniz?',
            function() {
                ApiService.delete('/customer-organizations/' + org.id)
                    .then(function() {
                        // Array'den sil
                        self.organizations.remove(org);

                        // Update customer's organization count
                        if (self.selectedCustomer() && self.selectedCustomer().organizationCount > 0) {
                            self.selectedCustomer().organizationCount--;
                        }

                        if (self.selectedOrganization() && self.selectedOrganization().id === org.id) {
                            self.selectedOrganization(null);
                            self.personnel([]);
                        }
                        toastr.success('Organizasyon silindi.');
                    })
                    .catch(function(error) {
                        console.error('Error deleting organization:', error);
                        var errorMsg = 'Organizasyon silinirken bir hata oluştu.';
                        if (error && error.message) {
                            errorMsg = error.message;
                        }
                        toastr.error(errorMsg);
                    });
            }
        );
    };

    // Close modal
    self.closeModal = function() {
        self.isModalOpen(false);
        self.editingOrganization(null);
        self.modalErrorMessage('');
    };

    // Initialize
    // EnumsService'i yukle, sonra diger verileri cek
    EnumsService.load().then(function() {
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
