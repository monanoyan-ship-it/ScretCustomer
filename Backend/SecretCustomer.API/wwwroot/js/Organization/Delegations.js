// Delegations ViewModel
function DelegationsViewModel() {
    var self = this;

    // State
    self.isLoading = ko.observable(false);
    self.isSaving = ko.observable(false);
    self.errorMessage = ko.observable('');
    self.successMessage = ko.observable('');

    // Data
    self.delegations = ko.observableArray([]);
    self.users = ko.observableArray([]);

    // Filter
    self.filter = {
        searchTerm: ko.observable(''),
        delegationType: ko.observable(''),
        isApproved: ko.observable(''),
        isCurrent: ko.observable(''),
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

    // Editing delegation
    self.editingDelegation = ko.observable(null);
    self.modal = null;

    // Load delegations
    self.loadDelegations = function() {
        self.isLoading(true);
        self.errorMessage('');

        var params = new URLSearchParams();
        if (self.filter.searchTerm()) params.append('searchTerm', self.filter.searchTerm());
        if (self.filter.delegationType() !== '') params.append('delegationType', self.filter.delegationType());
        if (self.filter.isApproved() !== '') params.append('isApproved', self.filter.isApproved());
        if (self.filter.isCurrent() !== '') params.append('isCurrent', self.filter.isCurrent());
        params.append('page', self.filter.page());
        params.append('pageSize', self.filter.pageSize());

        fetch('/api/delegations?' + params.toString(), { credentials: 'include' })
            .then(function(response) {
                if (!response.ok) throw new Error('Veriler yüklenemedi');
                return response.json();
            })
            .then(function(data) {
                self.delegations(data.items);
                self.paginationInfo({
                    totalCount: data.totalCount,
                    totalPages: data.totalPages,
                    hasNextPage: data.hasNextPage,
                    hasPreviousPage: data.hasPreviousPage
                });
            })
            .catch(function(error) {
                console.error('Error:', error);
                self.errorMessage(error.message || 'Vekaletler yüklenirken bir hata oluştu.');
            })
            .finally(function() {
                self.isLoading(false);
            });
    };

    // Load users
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

    // Clear filters
    self.clearFilters = function() {
        self.filter.searchTerm('');
        self.filter.delegationType('');
        self.filter.isApproved('');
        self.filter.isCurrent('');
        self.filter.page(1);
        self.loadDelegations();
    };

    // Pagination
    self.previousPage = function() {
        if (self.paginationInfo().hasPreviousPage) {
            self.filter.page(self.filter.page() - 1);
            self.loadDelegations();
        }
    };

    self.nextPage = function() {
        if (self.paginationInfo().hasNextPage) {
            self.filter.page(self.filter.page() + 1);
            self.loadDelegations();
        }
    };

    // Create new delegation
    self.createNew = function() {
        var today = new Date().toISOString().split('T')[0];
        self.editingDelegation({
            id: null,
            title: ko.observable(''),
            description: ko.observable(''),
            delegatorUserId: ko.observable(''),
            delegateeUserId: ko.observable(''),
            startDate: ko.observable(today),
            endDate: ko.observable(''),
            delegationType: ko.observable(0),
            reason: ko.observable(4),
            isActive: ko.observable(true),
            notes: ko.observable('')
        });
        self.showModal();
    };

    // Edit delegation
    self.editDelegation = function(delegation) {
        self.editingDelegation({
            id: delegation.id,
            title: ko.observable(delegation.title || ''),
            description: ko.observable(delegation.description || ''),
            delegatorUserId: ko.observable(delegation.delegatorUserId),
            delegateeUserId: ko.observable(delegation.delegateeUserId),
            startDate: ko.observable(delegation.startDate ? delegation.startDate.split('T')[0] : ''),
            endDate: ko.observable(delegation.endDate ? delegation.endDate.split('T')[0] : ''),
            delegationType: ko.observable(delegation.delegationType),
            reason: ko.observable(delegation.reason),
            isActive: ko.observable(delegation.isActive),
            notes: ko.observable(delegation.notes || '')
        });
        self.showModal();
    };

    // Save delegation
    self.saveDelegation = function() {
        var editing = self.editingDelegation();
        if (!editing) return;

        // Validation
        if (!editing.delegatorUserId() || !editing.delegateeUserId()) {
            self.errorMessage('Vekalet veren ve alan seçilmelidir.');
            return;
        }

        if (!editing.startDate()) {
            self.errorMessage('Başlangıç tarihi zorunludur.');
            return;
        }

        self.isSaving(true);
        self.errorMessage('');

        var dto = {
            title: editing.title() || null,
            description: editing.description() || null,
            delegatorUserId: editing.delegatorUserId(),
            delegateeUserId: editing.delegateeUserId(),
            startDate: editing.startDate(),
            endDate: editing.endDate() || null,
            delegationType: parseInt(editing.delegationType()),
            reason: parseInt(editing.reason()),
            isActive: editing.isActive(),
            notes: editing.notes() || null
        };

        var url = editing.id ? '/api/delegations/' + editing.id : '/api/delegations';
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
                self.successMessage(editing.id ? 'Vekalet başarıyla güncellendi.' : 'Vekalet başarıyla oluşturuldu.');
                self.hideModal();
                self.loadDelegations();
            })
            .catch(function(error) {
                console.error('Error:', error);
                self.errorMessage(error.message || 'Vekalet kaydedilirken bir hata oluştu.');
            })
            .finally(function() {
                self.isSaving(false);
            });
    };

    // Approve delegation
    self.approveDelegation = function(delegation) {
        if (!confirm('Bu vekaleti onaylamak istediğinizden emin misiniz?')) {
            return;
        }

        fetch('/api/delegations/' + delegation.id + '/approve', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            credentials: 'include',
            body: JSON.stringify({ isApproved: true, notes: '' })
        })
            .then(function(response) {
                if (!response.ok) {
                    return response.json().then(function(err) {
                        throw new Error(err.message || 'Onay işlemi başarısız');
                    });
                }
                return response.json();
            })
            .then(function(data) {
                self.successMessage('Vekalet başarıyla onaylandı.');
                self.loadDelegations();
            })
            .catch(function(error) {
                console.error('Error:', error);
                self.errorMessage(error.message || 'Vekalet onaylanırken bir hata oluştu.');
            });
    };

    // Delete delegation
    self.deleteDelegation = function(delegation) {
        if (!confirm('Bu vekaleti silmek istediğinizden emin misiniz?')) {
            return;
        }

        fetch('/api/delegations/' + delegation.id, {
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
                self.successMessage('Vekalet başarıyla silindi.');
                self.loadDelegations();
            })
            .catch(function(error) {
                console.error('Error:', error);
                self.errorMessage(error.message || 'Vekalet silinirken bir hata oluştu.');
            });
    };

    // Modal helpers
    self.showModal = function() {
        if (!self.modal) {
            self.modal = new bootstrap.Modal(document.getElementById('delegationModal'));
        }
        self.modal.show();
    };

    self.hideModal = function() {
        if (self.modal) {
            self.modal.hide();
        }
    };

    // Initialize
    self.loadDelegations();
    self.loadUsers();

    // Auto-search on typing (with debounce)
    var searchTimeout;
    self.filter.searchTerm.subscribe(function(value) {
        clearTimeout(searchTimeout);
        searchTimeout = setTimeout(function() {
            self.filter.page(1);
            self.loadDelegations();
        }, 500);
    });
}

// Helper functions
function formatDate(dateString) {
    if (!dateString) return '';
    var date = new Date(dateString);
    return date.toLocaleDateString('tr-TR');
}

function getStatusClass(status) {
    switch (status) {
        case 'Aktif': return 'bg-success';
        case 'Onay Bekliyor': return 'bg-warning';
        case 'Süresi Dolmuş': return 'bg-secondary';
        default: return 'bg-secondary';
    }
}

// Apply bindings when document is ready
$(document).ready(function() {
    ko.applyBindings(new DelegationsViewModel(), document.getElementById('delegations-app'));
});
