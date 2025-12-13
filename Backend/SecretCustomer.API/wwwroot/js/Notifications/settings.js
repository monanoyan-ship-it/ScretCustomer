// Notifications Settings ViewModel
function NotificationsSettingsViewModel() {
    var self = this;

    self.loading = ko.observable(true);
    self.settings = ko.observableArray([]);

    // Notification types
    self.notificationTypes = [
        'Info', 'Success', 'Warning', 'Error',
        'ApprovalRequest', 'Assignment', 'MeetingInvite',
        'TrainingInvite', 'Reminder', 'System'
    ];

    // Load settings
    self.loadSettings = function() {
        self.loading(true);
        $.get('/api/notifications/settings', function(data) {
            // Convert to observable objects
            var observableSettings = data.map(function(s) {
                return {
                    id: s.id,
                    userId: s.userId,
                    notificationType: s.notificationType,
                    inAppEnabled: ko.observable(s.inAppEnabled),
                    emailEnabled: ko.observable(s.emailEnabled),
                    smsEnabled: ko.observable(s.smsEnabled),
                    pushEnabled: ko.observable(s.pushEnabled)
                };
            });
            self.settings(observableSettings);
            self.loading(false);
        }).fail(function() {
            toastr.error('Ayarlar yüklenemedi');
            self.loading(false);
        });
    };

    // Initialize default settings
    self.initializeSettings = function() {
        self.loading(true);
        var promises = self.notificationTypes.map(function(type) {
            return $.ajax({
                url: '/api/notifications/settings',
                method: 'PUT',
                contentType: 'application/json',
                data: JSON.stringify({
                    notificationType: type,
                    inAppEnabled: true,
                    emailEnabled: type !== 'Info',
                    smsEnabled: type === 'Urgent' || type === 'ApprovalRequest',
                    pushEnabled: true
                })
            });
        });

        Promise.all(promises).then(function() {
            toastr.success('Varsayılan ayarlar oluşturuldu');
            self.loadSettings();
        }).catch(function() {
            toastr.error('Ayarlar oluşturulamadı');
            self.loading(false);
        });
    };

    // Update setting
    self.updateSetting = function(setting) {
        $.ajax({
            url: '/api/notifications/settings',
            method: 'PUT',
            contentType: 'application/json',
            data: JSON.stringify({
                notificationType: setting.notificationType,
                inAppEnabled: setting.inAppEnabled(),
                emailEnabled: setting.emailEnabled(),
                smsEnabled: setting.smsEnabled(),
                pushEnabled: setting.pushEnabled()
            }),
            success: function() {
                // Silent success - no toast for each change
            },
            error: function() {
                toastr.error('Ayar kaydedilemedi');
            }
        });
    };

    // Helper functions
    self.getNotificationTypeText = function(type) {
        var types = {
            'Info': 'Bilgi',
            'Success': 'Başarılı İşlem',
            'Warning': 'Uyarı',
            'Error': 'Hata',
            'ApprovalRequest': 'Onay Talebi',
            'Assignment': 'Görev Ataması',
            'MeetingInvite': 'Toplantı Daveti',
            'TrainingInvite': 'Eğitim Daveti',
            'Reminder': 'Hatırlatma',
            'System': 'Sistem Bildirimi'
        };
        return types[type] || type;
    };

    // Initialize
    self.init = function() {
        self.loadSettings();
    };

    self.init();
}

// Initialize when document is ready
$(document).ready(function() {
    ko.applyBindings(new NotificationsSettingsViewModel());
});
