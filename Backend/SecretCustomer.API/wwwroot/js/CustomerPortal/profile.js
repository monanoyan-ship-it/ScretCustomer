// CustomerPortal Profile ViewModel
function ProfileViewModel() {
    var self = this;

    // State
    self.isLoading = ko.observable(true);
    self.profile = ko.observable({});

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

        fetch('/api/auth/me', { credentials: 'include' })
            .then(function(response) {
                if (!response.ok) throw new Error('Profil yüklenemedi');
                return response.json();
            })
            .then(function(data) {
                self.profile(data);
            })
            .catch(function(error) {
                console.error('Profile load error:', error);
                toastr.error(T('Profile.LoadError', 'Profil bilgileri yüklenirken bir hata oluştu.'));
            })
            .finally(function() {
                self.isLoading(false);
            });
    };

    // Change password
    self.changePassword = function() {
        if (!self.canSubmitPassword()) {
            toastr.warning(T('Profile.FillAllFields', 'Lütfen tüm alanları doldurun ve şifrelerin eşleştiğinden emin olun.'));
            return;
        }

        self.isChangingPassword(true);

        fetch('/api/profile/change-password', {
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
