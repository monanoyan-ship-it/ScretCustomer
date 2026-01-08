function CustomerLoginViewModel() {
    var self = this;

    // State
    self.username = ko.observable('');
    self.password = ko.observable('');
    self.isLoading = ko.observable(false);
    self.errorMessage = ko.observable('');

    // Login
    self.login = function() {
        self.errorMessage('');

        if (!self.username() || !self.password()) {
            toastr.error(T('Account.LoginRequired', 'Kullanıcı adı ve şifre gereklidir.'));
            return;
        }

        self.isLoading(true);

        fetch('/api/customer/CustomerPortalAuth/login', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({
                username: self.username(),
                password: self.password()
            })
        })
        .then(function(response) {
            if (!response.ok) {
                return response.json().then(function(err) {
                    throw new Error(err.message || T('Account.LoginFailed', 'Giriş başarısız.'));
                });
            }
            return response.json();
        })
        .then(function(data) {
            // Store token in cookie
            document.cookie = 'CustomerToken=' + data.token + '; path=/; max-age=86400; SameSite=Strict';

            // Store user info in localStorage
            localStorage.setItem('customerUser', JSON.stringify(data.user));

            // Redirect to dashboard
            var returnUrl = new URLSearchParams(window.location.search).get('returnUrl');
            window.location.href = returnUrl || '/CustomerPortal/Dashboard';
        })
        .catch(function(error) {
            toastr.error(error.message || T('Account.LoginError', 'Giriş sırasında bir hata oluştu.'));
        })
        .finally(function() {
            self.isLoading(false);
        });
    };
}

// Translation keys
var TRANSLATION_KEYS = [
    'Account.LoginRequired',
    'Account.LoginFailed',
    'Account.LoginError'
];

// Apply bindings when DOM is ready
$(document).ready(function() {
    Localization.loadKeys(TRANSLATION_KEYS).then(function() {
        ko.applyBindings(new CustomerLoginViewModel(), document.getElementById('customer-login-app'));
    });
});
