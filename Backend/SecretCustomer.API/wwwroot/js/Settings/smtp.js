function SmtpProfilesViewModel() {
    var self = this;

    // State
    self.isLoading = ko.observable(true);
    self.isSaving = ko.observable(false);
    self.isTesting = ko.observable(false);
    self.isSendingTest = ko.observable(false);
    self.isEditing = ko.observable(false);
    self.showPassword = ko.observable(false);
    self.showClientSecret = ko.observable(false);

    // Data
    self.profiles = ko.observableArray([]);
    self.editingProfileId = ko.observable(null);

    // Test email
    self.testEmail = ko.observable('');
    self.selectedTestProfileId = ko.observable(null);

    // Form
    self.form = {
        name: ko.observable(''),
        host: ko.observable(''),
        port: ko.observable('587'),
        username: ko.observable(''),
        password: ko.observable(''),
        useSsl: ko.observable(true),
        fromEmail: ko.observable(''),
        fromName: ko.observable('Secret Customer'),
        enabled: ko.observable(true),
        useOAuth: ko.observable(false),
        tenantId: ko.observable(''),
        clientId: ko.observable(''),
        clientSecret: ko.observable(''),
        useGraphApi: ko.observable(true)
    };

    // Reset form
    self.resetForm = function() {
        self.form.name('');
        self.form.host('');
        self.form.port('587');
        self.form.username('');
        self.form.password('');
        self.form.useSsl(true);
        self.form.fromEmail('');
        self.form.fromName('Secret Customer');
        self.form.enabled(true);
        self.form.useOAuth(false);
        self.form.tenantId('');
        self.form.clientId('');
        self.form.clientSecret('');
        self.form.useGraphApi(true);
        self.showPassword(false);
        self.showClientSecret(false);
    };

    // Load profiles
    self.loadProfiles = function() {
        self.isLoading(true);
        fetch('/api/smtp/profiles', { credentials: 'include' })
            .then(function(r) { return r.json(); })
            .then(function(data) {
                self.profiles(data);
                // İlk profili test için varsayılan seç
                if (data.length > 0 && !self.selectedTestProfileId()) {
                    var defaultProfile = data.find(function(p) { return p.isDefault; });
                    self.selectedTestProfileId(defaultProfile ? defaultProfile.id : data[0].id);
                }
            })
            .catch(function() {
                toastr.error('Profiller yüklenirken hata oluştu.');
            })
            .finally(function() {
                self.isLoading(false);
            });
    };

    // Open create modal
    self.openCreateModal = function() {
        self.isEditing(false);
        self.editingProfileId(null);
        self.resetForm();
        bootstrap.Modal.getOrCreateInstance(document.getElementById('profileModal')).show();
    };

    // Open edit modal
    self.openEditModal = function(profile) {
        self.isEditing(true);
        self.editingProfileId(profile.id);
        self.form.name(profile.name || '');
        self.form.host(profile.host || '');
        self.form.port(String(profile.port || 587));
        self.form.username(profile.username || '');
        self.form.password(profile.password || '');
        self.form.useSsl(profile.useSsl !== false);
        self.form.fromEmail(profile.fromEmail || '');
        self.form.fromName(profile.fromName || 'Secret Customer');
        self.form.enabled(profile.enabled !== false);
        self.form.useOAuth(profile.useOAuth === true);
        self.form.tenantId(profile.tenantId || '');
        self.form.clientId(profile.clientId || '');
        self.form.clientSecret(profile.clientSecret || '');
        self.form.useGraphApi(profile.useGraphApi !== false);
        self.showPassword(false);
        self.showClientSecret(false);
        bootstrap.Modal.getOrCreateInstance(document.getElementById('profileModal')).show();
    };

    // Save profile
    self.saveProfile = function() {
        var isNew = !self.isEditing();
        var url = isNew ? '/api/smtp/profiles' : '/api/smtp/profiles/' + self.editingProfileId();
        var method = isNew ? 'POST' : 'PUT';

        var data = {
            name: self.form.name(),
            host: self.form.host(),
            port: parseInt(self.form.port()),
            username: self.form.username(),
            password: self.form.password(),
            useSsl: self.form.useSsl(),
            fromEmail: self.form.fromEmail(),
            fromName: self.form.fromName(),
            enabled: self.form.enabled(),
            useOAuth: self.form.useOAuth(),
            tenantId: self.form.tenantId(),
            clientId: self.form.clientId(),
            clientSecret: self.form.clientSecret(),
            useGraphApi: self.form.useGraphApi()
        };

        self.isSaving(true);
        fetch(url, {
            method: method,
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(data),
            credentials: 'include'
        })
        .then(function(r) { return r.json(); })
        .then(function(result) {
            if (result.success) {
                bootstrap.Modal.getInstance(document.getElementById('profileModal')).hide();
                toastr.success(result.message);
                self.loadProfiles();
            } else {
                toastr.error(result.message);
            }
        })
        .catch(function() {
            toastr.error('Kaydetme sırasında hata oluştu.');
        })
        .finally(function() {
            self.isSaving(false);
        });
    };

    // Delete profile
    self.deleteProfile = function(profile) {
        showDeleteConfirm(profile.name, function() {
            fetch('/api/smtp/profiles/' + profile.id, {
                method: 'DELETE',
                credentials: 'include'
            })
            .then(function(r) { return r.json(); })
            .then(function(result) {
                if (result.success) {
                    toastr.success(result.message);
                    self.loadProfiles();
                } else {
                    toastr.error(result.message);
                }
            })
            .catch(function() {
                toastr.error('Silme sırasında hata oluştu.');
            });
        });
    };

    // Set default
    self.setDefault = function(profile) {
        fetch('/api/smtp/profiles/' + profile.id + '/set-default', {
            method: 'POST',
            credentials: 'include'
        })
        .then(function(r) { return r.json(); })
        .then(function(result) {
            if (result.success) {
                toastr.success(result.message);
                self.loadProfiles();
            } else {
                toastr.error(result.message);
            }
        })
        .catch(function() {
            toastr.error('Varsayılan yapma sırasında hata oluştu.');
        });
    };

    // Test connection
    self.testConnection = function(profile) {
        self.isTesting(true);
        fetch('/api/smtp/profiles/' + profile.id + '/test-connection', {
            method: 'POST',
            credentials: 'include'
        })
        .then(function(r) { return r.json(); })
        .then(function(result) {
            if (result.success) {
                toastr.success(result.message);
            } else {
                toastr.error(result.message);
            }
        })
        .catch(function() {
            toastr.error('Bağlantı testi sırasında hata oluştu.');
        })
        .finally(function() {
            self.isTesting(false);
        });
    };

    // Send test email
    self.sendTestEmail = function() {
        if (!self.selectedTestProfileId()) {
            toastr.warning('Lütfen bir profil seçin.');
            return;
        }
        if (!self.testEmail()) {
            toastr.warning('Lütfen bir email adresi girin.');
            return;
        }

        self.isSendingTest(true);
        fetch('/api/smtp/profiles/' + self.selectedTestProfileId() + '/send-test', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ toEmail: self.testEmail() }),
            credentials: 'include'
        })
        .then(function(r) { return r.json(); })
        .then(function(result) {
            if (result.success) {
                toastr.success(result.message);
            } else {
                toastr.error(result.message);
            }
        })
        .catch(function() {
            toastr.error('Test maili gönderilirken hata oluştu.');
        })
        .finally(function() {
            self.isSendingTest(false);
        });
    };

    // Toggle password visibility
    self.togglePassword = function() {
        self.showPassword(!self.showPassword());
    };

    self.toggleClientSecret = function() {
        self.showClientSecret(!self.showClientSecret());
    };

    // Initialize
    self.loadProfiles();
}

$(document).ready(function() {
    ko.applyBindings(new SmtpProfilesViewModel(), document.getElementById('smtp-profiles-app'));
});
