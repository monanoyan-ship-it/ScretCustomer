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
    self.modalErrorMessage = ko.observable('');
    self.passwordModalErrorMessage = ko.observable('');

    // Role display helpers - handle both numeric and string enum values
    self.getRoleDisplayName = function(role) {
        const roleNames = {
            // Numeric values
            1: T('Role.Admin', 'Admin'),
            2: T('Role.TeamLeader', 'Takım Lideri'),
            3: T('Role.Evaluator', 'Değerlendirici'),
            4: T('Role.CustomerRepresentative', 'Müşteri Temsilcisi'),
            5: T('Role.FieldWorker', 'Saha Çalışanı'),
            // String values (enum names)
            'Admin': T('Role.Admin', 'Admin'),
            'TeamLeader': T('Role.TeamLeader', 'Takım Lideri'),
            'Evaluator': T('Role.Evaluator', 'Değerlendirici'),
            'CustomerRepresentative': T('Role.CustomerRepresentative', 'Müşteri Temsilcisi'),
            'FieldWorker': T('Role.FieldWorker', 'Saha Çalışanı')
        };
        return roleNames[role] || T('Common.Unknown', 'Bilinmiyor');
    };

    self.getRoleBadgeClass = function(role) {
        const roleClasses = {
            // Numeric values
            1: 'bg-danger',
            2: 'bg-primary',
            3: 'bg-success',
            4: 'bg-info',
            5: 'bg-warning text-dark',
            // String values (enum names)
            'Admin': 'bg-danger',
            'TeamLeader': 'bg-primary',
            'Evaluator': 'bg-success',
            'CustomerRepresentative': 'bg-info',
            'FieldWorker': 'bg-warning text-dark'
        };
        return roleClasses[role] || 'bg-secondary';
    };

    // Load users
    self.loadUsers = function() {
        self.isLoading(true);
        self.errorMessage('');

        fetch('/api/users', { credentials: 'include' })
            .then(response => {
                if (!response.ok) throw new Error(T('Message.LoadError', 'Yükleme başarısız'));
                return response.json();
            })
            .then(data => {
                self.users(data);
            })
            .catch(error => {
                console.error('Error:', error);
                self.errorMessage(T('User.LoadError', 'Kullanıcılar yüklenirken bir hata oluştu.'));
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
        self.modalErrorMessage('');
        self.editingUser(new UserEditViewModel());
        self.isModalOpen(true);
    };

    // Edit existing user
    self.editUser = function(user) {
        self.modalErrorMessage('');
        self.editingUser(new UserEditViewModel(user));
        self.isModalOpen(true);
    };

    // Save user
    self.saveUser = function() {
        var u = self.editingUser();

        // Validation
        if (!u.id && (!u.username() || u.username().trim() === '')) {
            toastr.warning(T('User.UsernameRequired', 'Kullanıcı adı zorunludur!'));
            return;
        }

        // Username format validation (no spaces, no Turkish characters)
        if (!u.id && u.username()) {
            var usernameRegex = /^[a-zA-Z0-9_.-]+$/;
            if (!usernameRegex.test(u.username())) {
                toastr.warning(T('User.UsernameInvalid', 'Kullanıcı adı sadece İngilizce harf, rakam, alt çizgi, nokta ve tire içerebilir. Boşluk ve Türkçe karakter kullanılamaz.'));
                return;
            }
        }

        if (!u.firstName() || u.firstName().trim() === '') {
            toastr.warning(T('User.FirstNameRequired', 'Ad alanı zorunludur!'));
            return;
        }

        if (!u.lastName() || u.lastName().trim() === '') {
            toastr.warning(T('User.LastNameRequired', 'Soyad alanı zorunludur!'));
            return;
        }

        if (!u.email() || u.email().trim() === '') {
            toastr.warning(T('User.EmailRequired', 'E-posta alanı zorunludur!'));
            return;
        }

        if (!u.id && (!u.password() || u.password().trim().length < 6)) {
            toastr.warning(T('User.PasswordMinLength', 'Şifre en az 6 karakter olmalıdır!'));
            return;
        }

        // Check username/email uniqueness before saving (only for new users)
        if (!u.id) {
            self.isSaving(true);

            // Check username
            fetch('/api/users/check-username/' + encodeURIComponent(u.username()), { credentials: 'include' })
                .then(function(response) { return response.json(); })
                .then(function(data) {
                    if (data.exists) {
                        self.isSaving(false);
                        self.modalErrorMessage(T('User.UsernameExists', 'Bu kullanıcı adı zaten kullanılıyor.'));
                        return Promise.reject('username_exists');
                    }
                    // Check email
                    return fetch('/api/users/check-email/' + encodeURIComponent(u.email()), { credentials: 'include' });
                })
                .then(function(response) { return response.json(); })
                .then(function(data) {
                    if (data.exists) {
                        self.isSaving(false);
                        self.modalErrorMessage(T('User.EmailExists', 'Bu e-posta adresi zaten kullanılıyor.'));
                        return Promise.reject('email_exists');
                    }
                    // All checks passed, proceed with save
                    self.doSaveUser(u);
                })
                .catch(function(error) {
                    if (error !== 'username_exists' && error !== 'email_exists') {
                        console.error('Error checking uniqueness:', error);
                        self.isSaving(false);
                    }
                });
            return;
        }

        // For existing users, save directly
        self.doSaveUser(u);
    };

    // Actual save operation
    self.doSaveUser = function(u) {
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
            self.successMessage(T('User.SaveSuccess', 'Kullanıcı başarıyla kaydedildi.'));
            self.closeModal();
            self.loadUsers();
        })
        .catch(error => {
            console.error('Error:', error);
            self.modalErrorMessage(error.message || T('User.SaveError', 'Kullanıcı kaydedilirken bir hata oluştu.'));
        })
        .finally(() => {
            self.isSaving(false);
        });
    };

    // Delete user
    self.deleteUser = function(user) {
        showDeleteConfirm(user.firstName + ' ' + user.lastName, function() {
            fetch('/api/users/' + user.id, {
                method: 'DELETE',
                credentials: 'include'
            })
            .then(response => {
                if (!response.ok) throw new Error(T('Message.DeleteError', 'Silme başarısız'));
                self.successMessage(T('User.DeleteSuccess', 'Kullanıcı başarıyla silindi.'));
                self.users.remove(user);
            })
            .catch(error => {
                console.error('Error:', error);
                self.errorMessage(T('User.DeleteError', 'Kullanıcı silinirken bir hata oluştu.'));
            });
        });
    };

    // Change password
    self.changePassword = function(user) {
        self.passwordModalErrorMessage('');
        self.passwordChangeUser(new PasswordChangeViewModel(user));
        self.isPasswordModalOpen(true);
    };

    // Save password
    self.savePassword = function() {
        var pwdUser = self.passwordChangeUser();

        // Validation
        if (!pwdUser.newPassword() || pwdUser.newPassword().trim() === '') {
            toastr.warning(T('Validation.Required', 'Yeni şifre boş olamaz!'));
            return;
        }

        if (pwdUser.newPassword().length < 6) {
            toastr.warning(T('User.PasswordMinLength', 'Şifre en az 6 karakter olmalıdır!'));
            return;
        }

        if (pwdUser.newPassword() !== pwdUser.newPasswordConfirm()) {
            toastr.warning(T('Validation.PasswordMismatch', 'Şifreler eşleşmiyor!'));
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
            self.successMessage(T('Account.PasswordChanged', 'Şifre başarıyla değiştirildi.'));
            self.closePasswordModal();
        })
        .catch(error => {
            console.error('Error:', error);
            self.passwordModalErrorMessage(error.message || T('Account.PasswordChangeError', 'Şifre değiştirilirken bir hata oluştu.'));
        })
        .finally(() => {
            self.isSaving(false);
        });
    };

    // Close password modal
    self.closePasswordModal = function() {
        self.isPasswordModalOpen(false);
        self.passwordChangeUser(null);
        self.passwordModalErrorMessage('');
    };

    // Close modal
    self.closeModal = function() {
        self.isModalOpen(false);
        self.editingUser(null);
        self.modalErrorMessage('');
    };

    // Initialize
    self.loadUsers();
    self.loadBranches();
}

// Apply bindings
$(document).ready(function() {
    ko.applyBindings(new UsersViewModel(), document.getElementById('users-app'));
});
