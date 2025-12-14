// Personnel ViewModel
function PersonnelViewModel() {
    var self = this;

    // State
    self.isLoading = ko.observable(false);
    self.isSaving = ko.observable(false);
    self.errorMessage = ko.observable('');
    self.successMessage = ko.observable('');

    // Data
    self.personnel = ko.observableArray([]);
    self.branches = ko.observableArray([]);
    self.customers = ko.observableArray([]);

    // Filter
    self.filter = {
        searchTerm: ko.observable(''),
        branchId: ko.observable(''),
        customerId: ko.observable(''),
        isActive: ko.observable(''),
        page: ko.observable(1),
        pageSize: ko.observable(20)
    };

    // Pagination info
    self.paginationInfo = ko.observable({
        totalCount: 0,
        totalPages: 0,
        hasNextPage: false,
        hasPreviousPage: false
    });

    // Page numbers
    self.pageNumbers = ko.computed(function() {
        var total = self.paginationInfo().totalPages;
        var current = self.filter.page();
        var pages = [];
        var start = Math.max(1, current - 2);
        var end = Math.min(total, current + 2);
        for (var i = start; i <= end; i++) {
            pages.push(i);
        }
        return pages;
    });

    // Editing personnel
    self.editingPersonnel = ko.observable(null);
    self.modal = null;

    // Load personnel
    self.loadPersonnel = function() {
        self.isLoading(true);
        self.errorMessage('');

        var params = new URLSearchParams();
        if (self.filter.searchTerm()) params.append('searchTerm', self.filter.searchTerm());
        if (self.filter.branchId()) params.append('branchId', self.filter.branchId());
        if (self.filter.customerId()) params.append('customerId', self.filter.customerId());
        if (self.filter.isActive() !== '') params.append('isActive', self.filter.isActive());
        params.append('page', self.filter.page());
        params.append('pageSize', self.filter.pageSize());

        fetch('/api/personnel?' + params.toString(), { credentials: 'include' })
            .then(function(response) {
                if (!response.ok) throw new Error('Veriler yüklenemedi');
                return response.json();
            })
            .then(function(data) {
                self.personnel(data.items);
                self.paginationInfo({
                    totalCount: data.totalCount,
                    totalPages: data.totalPages,
                    hasNextPage: data.hasNextPage,
                    hasPreviousPage: data.hasPreviousPage
                });
            })
            .catch(function(error) {
                console.error('Error:', error);
                self.errorMessage(error.message || 'Personel listesi yüklenirken bir hata oluştu.');
            })
            .finally(function() {
                self.isLoading(false);
            });
    };

    // Load filter options
    self.loadFilterOptions = function() {
        // Branches
        fetch('/api/branches', { credentials: 'include' })
            .then(function(res) { return res.json(); })
            .then(function(data) { self.branches(data); })
            .catch(function(error) { console.error('Error loading branches:', error); });

        // Customers
        fetch('/api/customers', { credentials: 'include' })
            .then(function(res) { return res.json(); })
            .then(function(data) { self.customers(data); })
            .catch(function(error) { console.error('Error loading customers:', error); });
    };

    // Apply filters
    self.applyFilters = function() {
        self.filter.page(1);
        self.loadPersonnel();
    };

    // Clear filters
    self.clearFilters = function() {
        self.filter.searchTerm('');
        self.filter.branchId('');
        self.filter.customerId('');
        self.filter.isActive('');
        self.filter.page(1);
        self.loadPersonnel();
    };

    // Pagination
    self.previousPage = function() {
        if (self.paginationInfo().hasPreviousPage) {
            self.filter.page(self.filter.page() - 1);
            self.loadPersonnel();
        }
    };

    self.nextPage = function() {
        if (self.paginationInfo().hasNextPage) {
            self.filter.page(self.filter.page() + 1);
            self.loadPersonnel();
        }
    };

    self.goToPage = function(page) {
        self.filter.page(page);
        self.loadPersonnel();
    };

    // Create new
    self.createNew = function() {
        self.editingPersonnel({
            id: null,
            firstName: ko.observable(''),
            lastName: ko.observable(''),
            tcKimlikNo: ko.observable(''),
            erpNo: ko.observable(''),
            sicilNo: ko.observable(''),
            title: ko.observable(''),
            gender: ko.observable(0),
            birthDate: ko.observable(''),
            hireDate: ko.observable(''),
            email: ko.observable(''),
            phoneNumber: ko.observable(''),
            department: ko.observable(''),
            isActive: ko.observable(true),
            notes: ko.observable(''),
            branchId: ko.observable(''),
            customerId: ko.observable('')
        });
        self.showModal();
    };

    // Edit personnel
    self.editPersonnel = function(personnel) {
        self.editingPersonnel({
            id: personnel.id,
            firstName: ko.observable(personnel.firstName),
            lastName: ko.observable(personnel.lastName),
            tcKimlikNo: ko.observable(personnel.tcKimlikNo || ''),
            erpNo: ko.observable(personnel.erpNo || ''),
            sicilNo: ko.observable(personnel.sicilNo || ''),
            title: ko.observable(personnel.title || ''),
            gender: ko.observable(personnel.gender),
            birthDate: ko.observable(personnel.birthDate ? personnel.birthDate.split('T')[0] : ''),
            hireDate: ko.observable(personnel.hireDate ? personnel.hireDate.split('T')[0] : ''),
            email: ko.observable(personnel.email || ''),
            phoneNumber: ko.observable(personnel.phoneNumber || ''),
            department: ko.observable(personnel.department || ''),
            isActive: ko.observable(personnel.isActive),
            notes: ko.observable(personnel.notes || ''),
            branchId: ko.observable(personnel.branchId || ''),
            customerId: ko.observable(personnel.customerId || '')
        });
        self.showModal();
    };

    // Save personnel
    self.savePersonnel = function() {
        var editing = self.editingPersonnel();
        if (!editing) return;

        // Validation
        if (!editing.firstName() || !editing.lastName()) {
            self.errorMessage('Ad ve Soyad alanları zorunludur.');
            return;
        }

        self.isSaving(true);
        self.errorMessage('');

        var dto = {
            firstName: editing.firstName(),
            lastName: editing.lastName(),
            tcKimlikNo: editing.tcKimlikNo() || null,
            erpNo: editing.erpNo() || null,
            sicilNo: editing.sicilNo() || null,
            title: editing.title() || null,
            gender: parseInt(editing.gender()) || 0,
            birthDate: editing.birthDate() || null,
            hireDate: editing.hireDate() || null,
            email: editing.email() || null,
            phoneNumber: editing.phoneNumber() || null,
            department: editing.department() || null,
            isActive: editing.isActive(),
            notes: editing.notes() || null,
            branchId: editing.branchId() || null,
            customerId: editing.customerId() || null
        };

        var url = editing.id ? '/api/personnel/' + editing.id : '/api/personnel';
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
                self.successMessage(editing.id ? 'Personel başarıyla güncellendi.' : 'Personel başarıyla oluşturuldu.');
                self.hideModal();
                self.loadPersonnel();
            })
            .catch(function(error) {
                console.error('Error:', error);
                self.errorMessage(error.message || 'Personel kaydedilirken bir hata oluştu.');
            })
            .finally(function() {
                self.isSaving(false);
            });
    };

    // Delete personnel
    self.deletePersonnel = function(personnel) {
        showDeleteConfirm(personnel.fullName, function() {
            fetch('/api/personnel/' + personnel.id, {
                method: 'DELETE',
                credentials: 'include'
            })
                .then(function(response) {
                    if (!response.ok) throw new Error('Silme işlemi başarısız');
                    return response.json();
                })
                .then(function(data) {
                    self.successMessage('Personel başarıyla silindi.');
                    self.loadPersonnel();
                })
                .catch(function(error) {
                    console.error('Error:', error);
                    self.errorMessage(error.message || 'Personel silinirken bir hata oluştu.');
                });
        });
    };

    // Modal helpers
    self.showModal = function() {
        if (!self.modal) {
            self.modal = new bootstrap.Modal(document.getElementById('personnelModal'));
        }
        self.modal.show();
    };

    self.hideModal = function() {
        if (self.modal) {
            self.modal.hide();
        }
    };

    // Initialize
    self.loadFilterOptions();
    self.loadPersonnel();

    // Auto-search on typing (with debounce)
    var searchTimeout;
    self.filter.searchTerm.subscribe(function(value) {
        clearTimeout(searchTimeout);
        searchTimeout = setTimeout(function() {
            self.applyFilters();
        }, 500);
    });
}

// Apply bindings when document is ready
$(document).ready(function() {
    ko.applyBindings(new PersonnelViewModel(), document.getElementById('personnel-app'));
});
