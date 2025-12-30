// Dashboard ViewModel
function DashboardViewModel() {
    var self = this;

    self.user = ko.observable(authService.getUser());
    self.isLoading = ko.observable(true);
    self.errorMessage = ko.observable('');

    // Stats
    self.totalEvaluations = ko.observable(0);
    self.averageScore = ko.observable(0);
    self.percentageChange = ko.observable(0);
    self.topBranches = ko.observableArray([]);
    self.bottomBranches = ko.observableArray([]);

    // Computed
    self.isAdmin = ko.computed(function() {
        return authService.isAdmin();
    });

    self.isQualitySpecialist = ko.computed(function() {
        return authService.isQualitySpecialist();
    });

    self.formattedAverageScore = ko.computed(function() {
        return self.averageScore().toFixed(1);
    });

    self.percentageChangeClass = ko.computed(function() {
        return self.percentageChange() >= 0 ? 'text-success' : 'text-danger';
    });

    self.percentageChangeIcon = ko.computed(function() {
        return self.percentageChange() >= 0 ? 'bi-arrow-up' : 'bi-arrow-down';
    });

    // Load dashboard data
    self.loadDashboard = function() {
        self.isLoading(true);
        self.errorMessage('');

        var endpoint = self.isAdmin() ? '/dashboard/admin' :
                       self.isQualitySpecialist() ? '/dashboard/representative' :
                       '/dashboard/representative';

        apiService.get(endpoint)
            .then(function(data) {
                if (self.isAdmin()) {
                    self.totalEvaluations(data.totalEvaluations || 0);
                    self.averageScore(data.averageScore || 0);
                    self.percentageChange(data.percentageChange || 0);
                    self.topBranches(data.topBranches || []);
                    self.bottomBranches(data.bottomBranches || []);
                } else {
                    // Representative view
                    console.log('Representative data:', data);
                }
            })
            .catch(function(error) {
                console.error('Dashboard error:', error);
                self.errorMessage('Dashboard verileri yüklenirken bir hata oluştu.');
            })
            .finally(function() {
                self.isLoading(false);
            });
    };

    self.logout = function() {
        authService.logout();
        window.location.hash = '#/login';
    };

    // Initialize
    self.loadDashboard();
}
