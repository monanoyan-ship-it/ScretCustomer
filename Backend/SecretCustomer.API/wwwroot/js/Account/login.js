// Login ViewModel
function LoginViewModel() {
    var self = this;

    self.username = ko.observable('');
    self.password = ko.observable('');
    self.isLoading = ko.observable(false);
    self.errorMessage = ko.observable('');

    self.login = function() {
        self.errorMessage('');

        if (!self.username() || !self.password()) {
            toastr.error(T('Account.LoginRequired', 'Kullanıcı adı ve şifre gereklidir.'));
            return;
        }

        self.isLoading(true);

        authService.login(self.username(), self.password())
            .then(function(user) {
                console.log('Login successful:', user);
                window.location.hash = '#/dashboard';
            })
            .catch(function(error) {
                console.error('Login error:', error);
                toastr.error(error.message || T('Account.LoginFailed', 'Giriş başarısız. Lütfen bilgilerinizi kontrol edin.'));
            })
            .finally(function() {
                self.isLoading(false);
            });
    };
}

// Translation keys
var TRANSLATION_KEYS = [
    'Account.LoginRequired',
    'Account.LoginFailed'
];

// Apply bindings when DOM is ready
$(document).ready(function() {
    Localization.loadKeys(TRANSLATION_KEYS).then(function() {
        ko.applyBindings(new LoginViewModel(), document.getElementById('login-app'));
    });
});
