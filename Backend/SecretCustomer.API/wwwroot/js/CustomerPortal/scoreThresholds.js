// Customer Portal - Score Thresholds ViewModel
function ScoreThresholdsViewModel() {
    var self = this;

    // API helper - fallback if customerApiFetch is not available
    function apiFetch(url, options) {
        if (typeof customerApiFetch === 'function') {
            return customerApiFetch(url, options);
        }
        options = options || {};
        options.credentials = 'include';
        return fetch(url, options).then(function (response) {
            if (response.status === 401) {
                window.location.href = '/Account/Login';
                throw new Error('Unauthorized');
            }
            return response;
        });
    }

    // Observables
    self.isLoading = ko.observable(false);
    self.isSaving = ko.observable(false);
    self.settings = ko.observableArray([]);

    // Load settings
    self.loadSettings = function () {
        self.isLoading(true);

        apiFetch('/api/customer/portal/score-thresholds')
            .then(function (r) { return r.json(); })
            .then(function (data) {
                var mappedSettings = data.map(function (item) {
                    return {
                        id: ko.observable(item.id),
                        projectTypeId: item.projectTypeId,
                        projectTypeName: item.projectTypeName,
                        projectTypeIcon: item.projectTypeIcon,
                        projectTypeColor: item.projectTypeColor,
                        successThreshold: ko.observable(item.successThreshold || 80),
                        warningThreshold: ko.observable(item.warningThreshold || 60),
                        isActive: ko.observable(item.isActive !== false)
                    };
                });
                self.settings(mappedSettings);
            })
            .catch(function (error) {
                console.error('Error loading score thresholds:', error);
                toastr.error(T('CustomerPortal.ScoreThresholds.LoadError', 'Puan eşikleri yüklenirken hata oluştu.'));
            })
            .finally(function () {
                self.isLoading(false);
            });
    };

    // Save all settings
    self.saveAll = function () {
        self.isSaving(true);

        var settingsData = self.settings().map(function (s) {
            return {
                projectTypeId: s.projectTypeId,
                successThreshold: s.successThreshold() ? parseFloat(s.successThreshold()) : 80,
                warningThreshold: s.warningThreshold() ? parseFloat(s.warningThreshold()) : 60,
                isActive: s.isActive()
            };
        });

        apiFetch('/api/customer/portal/score-thresholds/bulk', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ settings: settingsData })
        })
            .then(function (r) { return r.json(); })
            .then(function (data) {
                toastr.success(T('CustomerPortal.ScoreThresholds.SaveSuccess', 'Puan eşikleri başarıyla kaydedildi.'));
                // Update IDs from response
                if (data && data.length > 0) {
                    data.forEach(function (saved) {
                        var setting = self.settings().find(function (s) {
                            return s.projectTypeId === saved.projectTypeId;
                        });
                        if (setting) {
                            setting.id(saved.id);
                        }
                    });
                }
            })
            .catch(function (error) {
                console.error('Error saving score thresholds:', error);
                toastr.error(T('CustomerPortal.ScoreThresholds.SaveError', 'Puan eşikleri kaydedilirken hata oluştu.'));
            })
            .finally(function () {
                self.isSaving(false);
            });
    };

    // Initialize
    self.loadSettings();
}

// Translation keys
var TRANSLATION_KEYS = [
    'CustomerPortal.ScoreThresholds.LoadError',
    'CustomerPortal.ScoreThresholds.SaveSuccess',
    'CustomerPortal.ScoreThresholds.SaveError'
];

// Initialize when DOM ready
$(document).ready(function () {
    Localization.loadKeys(TRANSLATION_KEYS).then(function () {
        ko.applyBindings(new ScoreThresholdsViewModel(), document.getElementById('score-thresholds-app'));
    });
});
