// Performance Settings ViewModel
function PerformanceSettingsViewModel() {
    var self = this;

    // Observables
    self.isLoading = ko.observable(false);
    self.isSaving = ko.observable(false);
    self.settings = ko.observableArray([]);

    // Load settings
    self.loadSettings = function() {
        self.isLoading(true);

        ApiService.get('/performance-settings')
            .then(function(data) {
                var mappedSettings = data.map(function(item) {
                    return {
                        id: ko.observable(item.id),
                        projectTypeId: item.projectTypeId,
                        projectTypeName: item.projectTypeName,
                        projectTypeIcon: item.projectTypeIcon,
                        projectTypeColor: item.projectTypeColor,
                        dailyTarget: ko.observable(item.dailyTarget),
                        weeklyTarget: ko.observable(item.weeklyTarget),
                        monthlyTarget: ko.observable(item.monthlyTarget),
                        yearlyTarget: ko.observable(item.yearlyTarget),
                        successThreshold: ko.observable(item.successThreshold || 80),
                        warningThreshold: ko.observable(item.warningThreshold || 60),
                        isActive: ko.observable(item.isActive !== false)
                    };
                });
                self.settings(mappedSettings);
            })
            .catch(function(error) {
                console.error('Error loading performance settings:', error);
                toastr.error(T('Setting.LoadError', 'Ayarlar yüklenirken hata oluştu.'));
            })
            .finally(function() {
                self.isLoading(false);
            });
    };

    // Save all settings
    self.saveAll = function() {
        self.isSaving(true);

        var settingsData = self.settings().map(function(s) {
            return {
                projectTypeId: s.projectTypeId,
                dailyTarget: s.dailyTarget() ? parseInt(s.dailyTarget()) : null,
                weeklyTarget: s.weeklyTarget() ? parseInt(s.weeklyTarget()) : null,
                monthlyTarget: s.monthlyTarget() ? parseInt(s.monthlyTarget()) : null,
                yearlyTarget: s.yearlyTarget() ? parseInt(s.yearlyTarget()) : null,
                successThreshold: s.successThreshold() ? parseFloat(s.successThreshold()) : 80,
                warningThreshold: s.warningThreshold() ? parseFloat(s.warningThreshold()) : 60,
                isActive: s.isActive()
            };
        });

        ApiService.post('/performance-settings/bulk', { settings: settingsData })
            .then(function(data) {
                toastr.success(T('Setting.SaveSuccess', 'Performans ayarları başarıyla kaydedildi.'));
                // Update IDs from response
                if (data && data.length > 0) {
                    data.forEach(function(saved) {
                        var setting = self.settings().find(function(s) {
                            return s.projectTypeId === saved.projectTypeId;
                        });
                        if (setting) {
                            setting.id(saved.id);
                        }
                    });
                }
            })
            .catch(function(error) {
                console.error('Error saving performance settings:', error);
                toastr.error(T('Setting.SaveError', 'Ayarlar kaydedilirken hata oluştu.'));
            })
            .finally(function() {
                self.isSaving(false);
            });
    };

    // Initialize
    self.loadSettings();
}

// Translation keys
var TRANSLATION_KEYS = [
    'Setting.LoadError',
    'Setting.SaveSuccess',
    'Setting.SaveError'
];

// Initialize when DOM ready
$(document).ready(function() {
    Localization.loadKeys(TRANSLATION_KEYS).then(function() {
        ko.applyBindings(new PerformanceSettingsViewModel(), document.getElementById('performance-settings-app'));
    });
});
