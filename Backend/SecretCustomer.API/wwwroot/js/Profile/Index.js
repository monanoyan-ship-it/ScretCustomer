// Profile ViewModel
function ProfileViewModel() {
    var self = this;

    // State
    self.isLoading = ko.observable(true);
    self.isSaving = ko.observable(false);
    self.isChangingPassword = ko.observable(false);
    self.errorMessage = ko.observable('');
    self.successMessage = ko.observable('');

    // Password visibility
    self.showCurrentPassword = ko.observable(false);
    self.showNewPassword = ko.observable(false);
    self.showConfirmPassword = ko.observable(false);

    // Profile data
    self.profile = ko.observable({
        id: '',
        username: '',
        email: '',
        firstName: '',
        lastName: '',
        fullName: '',
        phoneNumber: '',
        role: '',
        createdAt: null,
        lastLoginAt: null
    });

    // Edit form
    self.editForm = {
        firstName: ko.observable(''),
        lastName: ko.observable(''),
        email: ko.observable(''),
        phoneNumber: ko.observable('')
    };

    // Password form
    self.passwordForm = {
        currentPassword: ko.observable(''),
        newPassword: ko.observable(''),
        confirmPassword: ko.observable('')
    };

    // Load profile
    self.loadProfile = function() {
        self.isLoading(true);
        self.errorMessage('');

        fetch('/api/profile', { credentials: 'include' })
            .then(function(response) {
                if (!response.ok) throw new Error(T('Profile.LoadError', 'Profil yüklenemedi'));
                return response.json();
            })
            .then(function(data) {
                self.profile(data);

                // Populate edit form
                self.editForm.firstName(data.firstName);
                self.editForm.lastName(data.lastName);
                self.editForm.email(data.email || '');
                self.editForm.phoneNumber(data.phoneNumber || '');
            })
            .catch(function(error) {
                console.error('Error:', error);
                toastr.error(error.message || T('Profile.LoadErrorMessage', 'Profil yüklenirken bir hata oluştu.'));
            })
            .finally(function() {
                self.isLoading(false);
            });
    };

    // Update profile
    self.updateProfile = function() {
        self.isSaving(true);
        self.errorMessage('');
        self.successMessage('');

        var dto = {
            firstName: self.editForm.firstName(),
            lastName: self.editForm.lastName(),
            email: self.editForm.email() || null,
            phoneNumber: self.editForm.phoneNumber() || null
        };

        fetch('/api/profile', {
            method: 'PUT',
            headers: { 'Content-Type': 'application/json' },
            credentials: 'include',
            body: JSON.stringify(dto)
        })
            .then(function(response) {
                if (!response.ok) {
                    return response.json().then(function(err) {
                        throw new Error(err.message || T('Profile.UpdateError', 'Güncelleme başarısız'));
                    });
                }
                return response.json();
            })
            .then(function(data) {
                self.profile(data);
                toastr.success(T('Profile.UpdateSuccess', 'Profil bilgileriniz başarıyla güncellendi.'));
            })
            .catch(function(error) {
                console.error('Error:', error);
                toastr.error(error.message || T('Profile.UpdateErrorMessage', 'Profil güncellenirken bir hata oluştu.'));
            })
            .finally(function() {
                self.isSaving(false);
            });
    };

    // Change password
    self.changePassword = function() {
        // Validate
        if (self.passwordForm.newPassword() !== self.passwordForm.confirmPassword()) {
            toastr.error(T('Profile.PasswordMismatch', 'Yeni şifreler eşleşmiyor.'));
            return;
        }

        if (self.passwordForm.newPassword().length < 6) {
            toastr.error(T('Validation.PasswordMinLength', 'Yeni şifre en az 6 karakter olmalıdır.'));
            return;
        }

        self.isChangingPassword(true);
        self.errorMessage('');
        self.successMessage('');

        var dto = {
            currentPassword: self.passwordForm.currentPassword(),
            newPassword: self.passwordForm.newPassword(),
            confirmPassword: self.passwordForm.confirmPassword()
        };

        fetch('/api/profile/change-password', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            credentials: 'include',
            body: JSON.stringify(dto)
        })
            .then(function(response) {
                if (!response.ok) {
                    return response.json().then(function(err) {
                        throw new Error(err.message || T('Account.PasswordChangeError', 'Şifre değiştirme başarısız'));
                    });
                }
                return response.json();
            })
            .then(function(data) {
                toastr.success(data.message || T('Account.PasswordChanged', 'Şifreniz başarıyla değiştirildi.'));
                // Clear password form
                self.passwordForm.currentPassword('');
                self.passwordForm.newPassword('');
                self.passwordForm.confirmPassword('');
            })
            .catch(function(error) {
                console.error('Error:', error);
                toastr.error(error.message || T('Account.PasswordChangeErrorMessage', 'Şifre değiştirilirken bir hata oluştu.'));
            })
            .finally(function() {
                self.isChangingPassword(false);
            });
    };

    // Helper: Get role text
    self.getRoleText = function(role) {
        var roles = {
            'Admin': T('Role.Admin', 'Yönetici'),
            'QualitySpecialist': T('Role.QualitySpecialist', 'Kalite Uzmanı'),
            'FieldWorker': T('Role.FieldWorker', 'Saha Çalışanı')
        };
        return roles[role] || role;
    };

    // Helper: Format date
    self.formatDate = function(dateStr) {
        if (!dateStr) return '-';
        var date = new Date(dateStr);
        return date.toLocaleDateString('tr-TR', {
            year: 'numeric',
            month: 'long',
            day: 'numeric'
        });
    };

    // Initialize
    self.loadProfile();
}

// Translation keys
var TRANSLATION_KEYS = [
    'Profile.LoadError',
    'Profile.LoadErrorMessage',
    'Profile.UpdateError',
    'Profile.UpdateSuccess',
    'Profile.UpdateErrorMessage',
    'Profile.PasswordMismatch',
    'Validation.PasswordMinLength',
    'Account.PasswordChangeError',
    'Account.PasswordChanged',
    'Account.PasswordChangeErrorMessage',
    'Role.Admin',
    'Role.QualitySpecialist',
    'Role.FieldWorker'
];

// Apply bindings when document is ready
$(document).ready(function() {
    Localization.loadKeys(TRANSLATION_KEYS).then(function() {
        ko.applyBindings(new ProfileViewModel(), document.getElementById('profile-app'));
    });
});
