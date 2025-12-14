// Organization ViewModel
function OrganizationViewModel() {
    var self = this;

    // State
    self.isLoading = ko.observable(false);
    self.isSaving = ko.observable(false);
    self.errorMessage = ko.observable('');
    self.successMessage = ko.observable('');
    self.viewMode = ko.observable('tree'); // 'tree' or 'list'

    // Data
    self.tree = ko.observableArray([]);
    self.units = ko.observableArray([]);
    self.allUnitsForSelect = ko.observableArray([]);
    self.users = ko.observableArray([]);
    self.branches = ko.observableArray([]);

    // Filter
    self.filter = {
        searchTerm: ko.observable(''),
        unitType: ko.observable(''),
        isActive: ko.observable(''),
        page: ko.observable(1),
        pageSize: ko.observable(50)
    };

    // Editing unit
    self.editingUnit = ko.observable(null);
    self.modal = null;

    // Toggle view mode
    self.toggleView = function() {
        self.viewMode(self.viewMode() === 'tree' ? 'list' : 'tree');
        if (self.viewMode() === 'list') {
            self.loadUnits();
        }
    };

    // Load tree view
    self.loadTree = function() {
        self.isLoading(true);
        self.errorMessage('');

        fetch('/api/organizationunits/tree', { credentials: 'include' })
            .then(function(response) {
                if (!response.ok) throw new Error('Veriler yüklenemedi');
                return response.json();
            })
            .then(function(data) {
                self.tree(data);
            })
            .catch(function(error) {
                console.error('Error:', error);
                self.errorMessage(error.message || 'Organizasyon yapısı yüklenirken bir hata oluştu.');
            })
            .finally(function() {
                self.isLoading(false);
            });
    };

    // Load list view
    self.loadUnits = function() {
        self.isLoading(true);
        self.errorMessage('');

        var params = new URLSearchParams();
        if (self.filter.searchTerm()) params.append('searchTerm', self.filter.searchTerm());
        if (self.filter.unitType() !== '') params.append('unitType', self.filter.unitType());
        if (self.filter.isActive() !== '') params.append('isActive', self.filter.isActive());
        params.append('page', self.filter.page());
        params.append('pageSize', self.filter.pageSize());

        fetch('/api/organizationunits?' + params.toString(), { credentials: 'include' })
            .then(function(response) {
                if (!response.ok) throw new Error('Veriler yüklenemedi');
                return response.json();
            })
            .then(function(data) {
                self.units(data);
            })
            .catch(function(error) {
                console.error('Error:', error);
                self.errorMessage(error.message || 'Birimler yüklenirken bir hata oluştu.');
            })
            .finally(function() {
                self.isLoading(false);
            });
    };

    // Load all units for select dropdown
    self.loadAllUnitsForSelect = function() {
        fetch('/api/organizationunits/all', { credentials: 'include' })
            .then(function(res) { return res.json(); })
            .then(function(data) { self.allUnitsForSelect(data); })
            .catch(function(error) { console.error('Error loading units:', error); });
    };

    // Load users for manager selection
    self.loadUsers = function() {
        fetch('/api/users', { credentials: 'include' })
            .then(function(res) { return res.json(); })
            .then(function(data) {
                var users = data.map(function(u) {
                    return {
                        id: u.id,
                        fullName: u.firstName + ' ' + u.lastName
                    };
                });
                self.users(users);
            })
            .catch(function(error) { console.error('Error loading users:', error); });
    };

    // Load branches
    self.loadBranches = function() {
        fetch('/api/branches', { credentials: 'include' })
            .then(function(res) { return res.json(); })
            .then(function(data) { self.branches(data); })
            .catch(function(error) { console.error('Error loading branches:', error); });
    };

    // Clear filters
    self.clearFilters = function() {
        self.filter.searchTerm('');
        self.filter.unitType('');
        self.filter.isActive('');
        self.filter.page(1);
        self.loadUnits();
    };

    // Create new unit
    self.createNew = function() {
        self.editingUnit({
            id: null,
            name: ko.observable(''),
            code: ko.observable(''),
            description: ko.observable(''),
            unitType: ko.observable(1), // Department default
            order: ko.observable(0),
            isActive: ko.observable(true),
            parentId: ko.observable(null),
            managerId: ko.observable(null),
            branchId: ko.observable(null)
        });
        self.showModal();
    };

    // Create child unit
    self.createChild = function(parent) {
        self.editingUnit({
            id: null,
            name: ko.observable(''),
            code: ko.observable(''),
            description: ko.observable(''),
            unitType: ko.observable(parent.unitType + 1 > 4 ? 4 : parent.unitType + 1),
            order: ko.observable(0),
            isActive: ko.observable(true),
            parentId: ko.observable(parent.id),
            managerId: ko.observable(null),
            branchId: ko.observable(null)
        });
        self.showModal();
    };

    // Edit unit
    self.editUnit = function(unit) {
        self.editingUnit({
            id: unit.id,
            name: ko.observable(unit.name),
            code: ko.observable(unit.code || ''),
            description: ko.observable(unit.description || ''),
            unitType: ko.observable(unit.unitType),
            order: ko.observable(unit.order || 0),
            isActive: ko.observable(unit.isActive),
            parentId: ko.observable(unit.parentId || null),
            managerId: ko.observable(unit.managerId || null),
            branchId: ko.observable(unit.branchId || null)
        });
        self.showModal();
    };

    // Save unit
    self.saveUnit = function() {
        var editing = self.editingUnit();
        if (!editing) return;

        // Validation
        if (!editing.name()) {
            self.errorMessage('Birim adı zorunludur.');
            return;
        }

        self.isSaving(true);
        self.errorMessage('');

        var dto = {
            name: editing.name(),
            code: editing.code() || null,
            description: editing.description() || null,
            unitType: parseInt(editing.unitType()),
            order: parseInt(editing.order()) || 0,
            isActive: editing.isActive(),
            parentId: editing.parentId() || null,
            managerId: editing.managerId() || null,
            branchId: editing.branchId() || null
        };

        var url = editing.id ? '/api/organizationunits/' + editing.id : '/api/organizationunits';
        var method = editing.id ? 'PUT' : 'POST';

        fetch(url, {
            method: method,
            headers: { 'Content-Type': 'application/json' },
            credentials: 'include',
            body: JSON.stringify(dto)
        })
            .then(function(response) {
                if (!response.ok) {
                    return response.json().then(function(err) {
                        throw new Error(err.message || 'İşlem başarısız');
                    });
                }
                return response.json();
            })
            .then(function(data) {
                self.successMessage(editing.id ? 'Birim başarıyla güncellendi.' : 'Birim başarıyla oluşturuldu.');
                self.hideModal();
                self.loadTree();
                self.loadAllUnitsForSelect();
                if (self.viewMode() === 'list') {
                    self.loadUnits();
                }
            })
            .catch(function(error) {
                console.error('Error:', error);
                self.errorMessage(error.message || 'Birim kaydedilirken bir hata oluştu.');
            })
            .finally(function() {
                self.isSaving(false);
            });
    };

    // Delete unit
    self.deleteUnit = function(unit) {
        showDeleteConfirm(unit.name + ' birimi', function() {
            fetch('/api/organizationunits/' + unit.id, {
            method: 'DELETE',
            credentials: 'include'
        })
            .then(function(response) {
                if (!response.ok) {
                    return response.json().then(function(err) {
                        throw new Error(err.message || 'Silme işlemi başarısız');
                    });
                }
                return response.json();
            })
            .then(function(data) {
                self.successMessage('Birim başarıyla silindi.');
                self.loadTree();
                self.loadAllUnitsForSelect();
                if (self.viewMode() === 'list') {
                    self.loadUnits();
                }
            })
            .catch(function(error) {
                console.error('Error:', error);
                self.errorMessage(error.message || 'Birim silinirken bir hata oluştu.');
            });
        });
    };

    // Modal helpers
    self.showModal = function() {
        if (!self.modal) {
            self.modal = new bootstrap.Modal(document.getElementById('unitModal'));
        }
        self.modal.show();
    };

    self.hideModal = function() {
        if (self.modal) {
            self.modal.hide();
        }
    };

    // Initialize
    self.loadTree();
    self.loadAllUnitsForSelect();
    self.loadUsers();
    self.loadBranches();

    // Auto-search on typing (with debounce)
    var searchTimeout;
    self.filter.searchTerm.subscribe(function(value) {
        if (self.viewMode() === 'list') {
            clearTimeout(searchTimeout);
            searchTimeout = setTimeout(function() {
                self.loadUnits();
            }, 500);
        }
    });
}

// Apply bindings when document is ready
$(document).ready(function() {
    ko.applyBindings(new OrganizationViewModel(), document.getElementById('organization-app'));
});
