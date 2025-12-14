function BranchEditViewModel(data) {
    var self = this;
    data = data || {};

    self.id = data.id || null;
    self.name = ko.observable(data.name || '');
    self.code = ko.observable(data.code || '');
    self.address = ko.observable(data.address || '');
    self.city = ko.observable(data.city || '');
    self.region = ko.observable(data.region || '');
    self.isActive = ko.observable(data.isActive !== undefined ? data.isActive : true);
}

function BranchesViewModel() {
    var self = this;

    // State
    self.isLoading = ko.observable(false);
    self.isSaving = ko.observable(false);
    self.errorMessage = ko.observable('');
    self.successMessage = ko.observable('');

    // Data
    self.branches = ko.observableArray([]);
    self.editingBranch = ko.observable(null);

    // Modal state
    self.isModalOpen = ko.observable(false);

    // Load branches
    self.loadBranches = function() {
        self.isLoading(true);
        self.errorMessage('');

        fetch('/api/branches', { credentials: 'include' })
            .then(response => {
                if (!response.ok) throw new Error(T('Message.LoadError', 'Yükleme başarısız'));
                return response.json();
            })
            .then(data => {
                self.branches(data);
            })
            .catch(error => {
                console.error('Error:', error);
                self.errorMessage(T('Branch.LoadError', 'Şubeler yüklenirken bir hata oluştu.'));
            })
            .finally(() => {
                self.isLoading(false);
            });
    };

    // Create new branch
    self.createNew = function() {
        self.editingBranch(new BranchEditViewModel());
        self.isModalOpen(true);
    };

    // Edit existing branch
    self.editBranch = function(branch) {
        self.editingBranch(new BranchEditViewModel(branch));
        self.isModalOpen(true);
    };

    // Save branch
    self.saveBranch = function() {
        var br = self.editingBranch();

        // Validation
        if (!br.name() || br.name().trim() === '') {
            toastr.warning(T('Branch.NameRequired', 'Şube adı zorunludur!'));
            return;
        }

        if (!br.code() || br.code().trim() === '') {
            toastr.warning(T('Branch.CodeRequired', 'Şube kodu zorunludur!'));
            return;
        }

        // Prepare DTO
        var dto = {
            name: br.name(),
            code: br.code(),
            address: br.address() || null,
            city: br.city() || null,
            region: br.region() || null,
            isActive: br.isActive()
        };

        self.isSaving(true);
        var endpoint = br.id ? '/api/branches/' + br.id : '/api/branches';
        var method = br.id ? 'PUT' : 'POST';

        fetch(endpoint, {
            method: method,
            headers: { 'Content-Type': 'application/json' },
            credentials: 'include',
            body: JSON.stringify(dto)
        })
        .then(response => {
            if (!response.ok) {
                return response.json().then(err => {
                    throw new Error(err.message || T('Message.SaveError', 'Kayıt başarısız'));
                });
            }
            return response.json();
        })
        .then(data => {
            self.successMessage(T('Branch.SaveSuccess', 'Şube başarıyla kaydedildi.'));
            self.closeModal();
            self.loadBranches();
        })
        .catch(error => {
            console.error('Error:', error);
            self.errorMessage(error.message || T('Branch.SaveError', 'Şube kaydedilirken bir hata oluştu.'));
        })
        .finally(() => {
            self.isSaving(false);
        });
    };

    // Delete branch
    self.deleteBranch = function(branch) {
        deleteConfirmation.show(T('Branch.DeleteConfirm', 'Bu şubeyi silmek istediğinizden emin misiniz?'), function() {

        fetch('/api/branches/' + branch.id, {
            method: 'DELETE',
            credentials: 'include'
        })
        .then(response => {
            if (!response.ok) throw new Error(T('Message.DeleteError', 'Silme başarısız'));
            self.successMessage(T('Branch.DeleteSuccess', 'Şube başarıyla silindi.'));
            self.branches.remove(branch);
        })
        .catch(error => {
            console.error('Error:', error);
            self.errorMessage(T('Branch.DeleteError', 'Şube silinirken bir hata oluştu.'));
        });
        });
    };

    // Close modal
    self.closeModal = function() {
        self.isModalOpen(false);
        self.editingBranch(null);
    };

    // Initialize
    self.loadBranches();
}

// Apply bindings
$(document).ready(function() {
    ko.applyBindings(new BranchesViewModel(), document.getElementById('branches-app'));
});
