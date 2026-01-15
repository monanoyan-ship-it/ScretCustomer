// Performance Tracking Report ViewModel
function PerformanceTrackingViewModel() {
    var self = this;

    // Observables
    self.isLoading = ko.observable(false);
    self.evaluatorPerformances = ko.observableArray([]);
    self.customerQuotaStatuses = ko.observableArray([]);
    self.summary = ko.observable({
        totalEvaluators: 0,
        totalActiveCustomers: 0,
        totalTodayEvaluations: 0,
        totalWeekEvaluations: 0,
        totalMonthEvaluations: 0,
        totalYearEvaluations: 0
    });

    // Load data
    self.loadData = function() {
        self.isLoading(true);

        ApiService.get('/reports/performance-tracking')
            .then(function(data) {
                self.evaluatorPerformances(data.evaluatorPerformances || []);
                self.customerQuotaStatuses(data.customerQuotaStatuses || []);
                self.summary(data.summary || {
                    totalEvaluators: 0,
                    totalActiveCustomers: 0,
                    totalTodayEvaluations: 0,
                    totalWeekEvaluations: 0,
                    totalMonthEvaluations: 0,
                    totalYearEvaluations: 0
                });
            })
            .catch(function(error) {
                console.error('Error loading performance tracking data:', error);
                toastr.error(T('Report.LoadError', 'Veriler yüklenirken hata oluştu.'));
            })
            .finally(function() {
                self.isLoading(false);
            });
    };

    // Refresh data
    self.refresh = function() {
        self.loadData();
    };

    // Initialize
    self.loadData();
}

// Translation keys
var TRANSLATION_KEYS = [
    'Report.LoadError'
];

// Initialize when DOM ready
$(document).ready(function() {
    Localization.loadKeys(TRANSLATION_KEYS).then(function() {
        ko.applyBindings(new PerformanceTrackingViewModel(), document.getElementById('performance-tracking-app'));
    });
});
