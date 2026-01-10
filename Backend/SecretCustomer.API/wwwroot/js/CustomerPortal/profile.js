// CustomerPortal Profile ViewModel
function ProfileViewModel() {
    var self = this;

    // State
    self.isLoading = ko.observable(true);
    self.isUpdating = ko.observable(false);

    // Profile fields
    self.username = ko.observable('');
    self.firstName = ko.observable('');
    self.lastName = ko.observable('');
    self.email = ko.observable('');
    self.phoneNumber = ko.observable('');

    // Password change fields
    self.currentPassword = ko.observable('');
    self.newPassword = ko.observable('');
    self.confirmPassword = ko.observable('');
    self.isChangingPassword = ko.observable(false);

    // Password visibility toggles
    self.showCurrentPassword = ko.observable(false);
    self.showNewPassword = ko.observable(false);
    self.showConfirmPassword = ko.observable(false);

    self.toggleCurrentPassword = function() {
        self.showCurrentPassword(!self.showCurrentPassword());
    };

    self.toggleNewPassword = function() {
        self.showNewPassword(!self.showNewPassword());
    };

    self.toggleConfirmPassword = function() {
        self.showConfirmPassword(!self.showConfirmPassword());
    };

    // Validation
    self.canUpdateProfile = ko.computed(function() {
        return self.firstName() && self.firstName().trim() &&
               self.lastName() && self.lastName().trim();
    });

    self.canSubmitPassword = ko.computed(function() {
        return self.currentPassword() &&
               self.newPassword() &&
               self.newPassword().length >= 6 &&
               self.confirmPassword() &&
               self.newPassword() === self.confirmPassword();
    });

    // Load profile
    self.loadProfile = function() {
        self.isLoading(true);

        fetch('/api/customer-portal/profile', { credentials: 'include' })
            .then(function(response) {
                if (!response.ok) throw new Error('Profil yüklenemedi');
                return response.json();
            })
            .then(function(data) {
                self.username(data.username || '');
                self.firstName(data.firstName || '');
                self.lastName(data.lastName || '');
                self.email(data.email || '');
                self.phoneNumber(data.phoneNumber || '');
            })
            .catch(function(error) {
                console.error('Profile load error:', error);
                toastr.error(T('Profile.LoadError', 'Profil bilgileri yüklenirken bir hata oluştu.'));
            })
            .finally(function() {
                self.isLoading(false);
            });
    };

    // Update profile
    self.updateProfile = function() {
        if (!self.canUpdateProfile()) {
            toastr.warning(T('Profile.NameRequired', 'Ad ve Soyad alanları zorunludur.'));
            return;
        }

        self.isUpdating(true);

        fetch('/api/customer-portal/profile', {
            method: 'PUT',
            headers: { 'Content-Type': 'application/json' },
            credentials: 'include',
            body: JSON.stringify({
                firstName: self.firstName(),
                lastName: self.lastName(),
                email: self.email() || null,
                phoneNumber: self.phoneNumber() || null
            })
        })
        .then(function(response) {
            return response.json().then(function(data) {
                if (!response.ok) {
                    throw new Error(data.message || T('Profile.UpdateFailed', 'Profil güncellenemedi'));
                }
                return data;
            });
        })
        .then(function(result) {
            toastr.success(T('Profile.UpdateSuccess', 'Profil bilgileriniz başarıyla güncellendi.'));
            // Update fields with response
            if (result.firstName) self.firstName(result.firstName);
            if (result.lastName) self.lastName(result.lastName);
            if (result.email !== undefined) self.email(result.email || '');
            if (result.phoneNumber !== undefined) self.phoneNumber(result.phoneNumber || '');
        })
        .catch(function(error) {
            console.error('Profile update error:', error);
            toastr.error(error.message || T('Profile.UpdateError', 'Profil güncellenirken bir hata oluştu.'));
        })
        .finally(function() {
            self.isUpdating(false);
        });
    };

    // Change password
    self.changePassword = function() {
        if (!self.canSubmitPassword()) {
            toastr.warning(T('Profile.FillAllFields', 'Lütfen tüm alanları doldurun ve şifrelerin eşleştiğinden emin olun.'));
            return;
        }

        self.isChangingPassword(true);

        fetch('/api/customer-portal/profile/change-password', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            credentials: 'include',
            body: JSON.stringify({
                currentPassword: self.currentPassword(),
                newPassword: self.newPassword(),
                confirmPassword: self.confirmPassword()
            })
        })
        .then(function(response) {
            return response.json().then(function(data) {
                if (!response.ok) {
                    throw new Error(data.message || T('Profile.PasswordChangeFailed', 'Şifre değiştirilemedi'));
                }
                return data;
            });
        })
        .then(function(result) {
            toastr.success(T('Profile.PasswordChanged', 'Şifreniz başarıyla değiştirildi.'));
            // Clear form
            self.currentPassword('');
            self.newPassword('');
            self.confirmPassword('');
        })
        .catch(function(error) {
            console.error('Password change error:', error);
            toastr.error(error.message || T('Profile.PasswordChangeError', 'Şifre değiştirilirken bir hata oluştu.'));
        })
        .finally(function() {
            self.isChangingPassword(false);
        });
    };

    // Initialize
    self.loadProfile();
}

// Translation keys
var TRANSLATION_KEYS = [
    'Profile.LoadError',
    'Profile.NameRequired',
    'Profile.UpdateFailed',
    'Profile.UpdateSuccess',
    'Profile.UpdateError',
    'Profile.FillAllFields',
    'Profile.PasswordChangeFailed',
    'Profile.PasswordChanged',
    'Profile.PasswordChangeError'
];

// Apply bindings when DOM is ready
document.addEventListener('DOMContentLoaded', function() {
    if (typeof Localization !== 'undefined') {
        Localization.loadKeys(TRANSLATION_KEYS).then(function() {
            ko.applyBindings(new ProfileViewModel(), document.getElementById('profile-app'));
        });
    } else {
        ko.applyBindings(new ProfileViewModel(), document.getElementById('profile-app'));
    }
});
