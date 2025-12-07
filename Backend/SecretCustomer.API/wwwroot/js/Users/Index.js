function UserEditViewModel(data) {
    var self = this;
    data = data || {};

    self.id = data.id || null;
    self.username = ko.observable(data.username || '');
    self.firstName = ko.observable(data.firstName || '');
    self.lastName = ko.observable(data.lastName || '');
    self.email = ko.observable(data.email || '');
    self.password = ko.observable('');
    self.role = ko.observable(data.role !== undefined ? data.role.toString() : '3'); // Default: Evaluator
    self.branchId = ko.observable(data.branchId || null);
    self.isActive = ko.observable(data.isActive !== undefined ? data.isActive : true);
}

function PasswordChangeViewModel(user) {
    var self = this;
    
    self.userId = user.id;
    self.username = user.username;
    self.newPassword = ko.observable('');
    self.newPasswordConfirm = ko.observable('');
}

function UsersViewModel() {
    var self = this;

    // State
    self.isLoading = ko.observable(false);
    self.isSaving = ko.observable(false);
    self.errorMessage = ko.observable('');
    self.successMessage = ko.observable('');

    // Data
    self.users = ko.observableArray([]);
    self.branches = ko.observableArray([]);
    self.editingUser = ko.observable(null);
    self.passwordChangeUser = ko.observable(null);

    // Modal state
    self.isModalOpen = ko.observable(false);
    self.isPasswordModalOpen = ko.observable(false);

    // Role display helpers
    self.getRoleDisplayName = function(role) {
        const roleNames = {
            1: 'Admin',
            2: 'Team Leader',
            3: 'Evaluator',
            4: 'Customer Representative'
        };
        return roleNames[role] || 'Unknown';
    };

    self.getRoleBadgeClass = function(role) {
        const roleClasses = {
            1: 'bg-danger',
            2: 'bg-primary',
            3: 'bg-success',
            4: 'bg-info'
        };
        return roleClasses[role] || 'bg-secondary';
    };

    // Load users
    self.loadUsers = function() {
        self.isLoading(true);
        self.errorMessage('');

        fetch('/api/users', { credentials: 'include' })
            .then(response => {
                if (!response.ok) throw new Error('Yükleme başarısız');
                return response.json();
            })
            .then(data => {
                self.users(data);
            })
            .catch(error => {
                console.error('Error:', error);
                self.errorMessage('Kullanıcılar yüklenirken bir hata oluştu.');
            })
            .finally(() => {
                self.isLoading(false);
            });
    };

    // Load branches
    self.loadBranches = function() {
        fetch('/api/branches/active', { credentials: 'include' })
            .then(response => {
                if (!response.ok) throw new Error('Şubeler yüklenemedi');
                return response.json();
            })
            .then(data => {
                self.branches(data);
            })
            .catch(error => {
                console.error('Error loading branches:', error);
            });
    };

    // Create new user
    self.createNew = function() {
        self.editingUser(new UserEditViewModel());
        self.isModalOpen(true);
    };

    // Edit existing user
    self.editUser = function(user) {
        self.editingUser(new UserEditViewModel(user));
        self.isModalOpen(true);
    };

    // Save user
    self.saveUser = function() {
        var u = self.editingUser();

        // Validation
        if (!u.id && (!u.username() || u.username().trim() === '')) {
            alert('Kullanıcı adı zorunludur!');
            return;
        }

        if (!u.firstName() || u.firstName().trim() === '') {
            alert('Ad alanı zorunludur!');
            return;
        }

        if (!u.lastName() || u.lastName().trim() === '') {
            alert('Soyad alanı zorunludur!');
            return;
        }

        if (!u.email() || u.email().trim() === '') {
            alert('E-posta alanı zorunludur!');
            return;
        }

        if (!u.id && (!u.password() || u.password().trim().length < 6)) {
            alert('Şifre en az 6 karakter olmalıdır!');
            return;
        }

        // Prepare DTO
        var dto = {
            firstName: u.firstName(),
            lastName: u.lastName(),
            email: u.email(),
            role: parseInt(u.role()),
            branchId: u.branchId() || null,
            isActive: u.isActive()
        };

        if (!u.id) {
            dto.username = u.username();
            dto.password = u.password();
        }

        self.isSaving(true);
        var endpoint = u.id ? '/api/users/' + u.id : '/api/users';
        var method = u.id ? 'PUT' : 'POST';

        fetch(endpoint, {
            method: method,
            headers: { 'Content-Type': 'application/json' },
            credentials: 'include',
            body: JSON.stringify(dto)
        })
        .then(response => {
            if (!response.ok) {
                return response.json().then(err => {
                    throw new Error(err.message || 'Kayıt başarısız');
                });
            }
            return response.json();
        })
        .then(data => {
            self.successMessage('Kullanıcı başarıyla kaydedildi.');
            self.closeModal();
            self.loadUsers();
        })
        .catch(error => {
            console.error('Error:', error);
            self.errorMessage(error.message || 'Kullanıcı kaydedilirken bir hata oluştu.');
        })
        .finally(() => {
            self.isSaving(false);
        });
    };

    // Delete user
    self.deleteUser = function(user) {
        deleteConfirmation.show('Bu kullanıcıyı silmek istediğinizden emin misiniz?', function() {

        fetch('/api/users/' + user.id, {
            method: 'DELETE',
            credentials: 'include'
        })
        .then(response => {
            if (!response.ok) throw new Error('Silme başarısız');
            self.successMessage('Kullanıcı başarıyla silindi.');
            self.users.remove(user);
        })
        .catch(error => {
            console.error('Error:', error);
            self.errorMessage('Kullanıcı silinirken bir hata oluştu.');
        });
        });
    };

    // Change password
    self.changePassword = function(user) {
        self.passwordChangeUser(new PasswordChangeViewModel(user));
        self.isPasswordModalOpen(true);
    };

    // Save password
    self.savePassword = function() {
        var pwdUser = self.passwordChangeUser();
        
        // Validation
        if (!pwdUser.newPassword() || pwdUser.newPassword().trim() === '') {
            alert('Yeni şifre boş olamaz!');
            return;
        }
        
        if (pwdUser.newPassword().length < 6) {
            alert('Şifre en az 6 karakter olmalıdır!');
            return;
        }
        
        if (pwdUser.newPassword() !== pwdUser.newPasswordConfirm()) {
            alert('Şifreler eşleşmiyor!');
            return;
        }
        
        self.isSaving(true);
        self.errorMessage('');
        
        const dto = {
            userId: pwdUser.userId,
            newPassword: pwdUser.newPassword()
        };
        
        fetch('/api/users/' + pwdUser.userId + '/change-password', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            credentials: 'include',
            body: JSON.stringify(dto)
        })
        .then(response => {
            if (!response.ok) {
                return response.json().then(err => {
                    throw new Error(err.message || 'Şifre değiştirilemedi');
                });
            }
            return response.json();
        })
        .then(data => {
            self.successMessage('Şifre başarıyla değiştirildi.');
            self.closePasswordModal();
        })
        .catch(error => {
            console.error('Error:', error);
            self.errorMessage(error.message || 'Şifre değiştirilirken bir hata oluştu.');
        })
        .finally(() => {
            self.isSaving(false);
        });
    };

    // Close password modal
    self.closePasswordModal = function() {
        self.isPasswordModalOpen(false);
        self.passwordChangeUser(null);
    };

    // Close modal
    self.closeModal = function() {
        self.isModalOpen(false);
        self.editingUser(null);
    };

    // Initialize
    self.loadUsers();
    self.loadBranches();
}

// Apply bindings
$(document).ready(function() {
    ko.applyBindings(new UsersViewModel(), document.getElementById('users-app'));
});
