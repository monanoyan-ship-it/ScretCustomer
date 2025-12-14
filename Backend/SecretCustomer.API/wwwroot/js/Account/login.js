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
            self.errorMessage(T('Account.LoginRequired', 'Kullanıcı adı ve şifre gereklidir.'));
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
                self.errorMessage(error.message || T('Account.LoginFailed', 'Giriş başarısız. Lütfen bilgilerinizi kontrol edin.'));
            })
            .finally(function() {
                self.isLoading(false);
            });
    };
}
