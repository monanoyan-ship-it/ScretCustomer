// Stats Component ViewModel
function DashboardStatsViewModel() {
    var self = this;

    self.isLoading = ko.observable(true);
    self.errorMessage = ko.observable('');
    self.totalEvaluations = ko.observable(0);
    self.averageScore = ko.observable(0);
    self.percentageChange = ko.observable(0);

    self.loadStats = function() {
        self.isLoading(true);
        self.errorMessage('');

        fetch('/api/dashboard/admin', {
            credentials: 'include'
        })
        .then(response => {
            if (!response.ok) throw new Error('Yükleme başarısız');
            return response.json();
        })
        .then(data => {
            self.totalEvaluations(data.totalEvaluations || 0);
            self.averageScore(data.averageScore || 0);
            self.percentageChange(data.percentageChange || 0);
        })
        .catch(error => {
            console.error('Error:', error);
            self.errorMessage('Dashboard verileri yüklenirken bir hata oluştu.');
        })
        .finally(() => {
            self.isLoading(false);
        });
    };

    self.loadStats();
}

// Top Branches Component ViewModel
function DashboardTopBranchesViewModel() {
    var self = this;

    self.isLoading = ko.observable(true);
    self.topBranches = ko.observableArray([]);

    self.loadBranches = function() {
        self.isLoading(true);

        fetch('/api/dashboard/admin', {
            credentials: 'include'
        })
        .then(res => res.json())
        .then(data => {
            self.topBranches(data.topBranches || []);
        })
        .catch(error => {
            console.error('Error:', error);
        })
        .finally(() => {
            self.isLoading(false);
        });
    };

    self.loadBranches();
}

// Bottom Branches Component ViewModel
function DashboardBottomBranchesViewModel() {
    var self = this;

    self.isLoading = ko.observable(true);
    self.bottomBranches = ko.observableArray([]);

    self.loadBranches = function() {
        self.isLoading(true);

        fetch('/api/dashboard/admin', {
            credentials: 'include'
        })
        .then(res => res.json())
        .then(data => {
            self.bottomBranches(data.bottomBranches || []);
        })
        .catch(error => {
            console.error('Error:', error);
        })
        .finally(() => {
            self.isLoading(false);
        });
    };

    self.loadBranches();
}

// Apply bindings to each component
$(document).ready(function() {
    ko.applyBindings(
        new DashboardStatsViewModel(),
        document.getElementById('dashboard-stats-component')
    );

    ko.applyBindings(
        new DashboardTopBranchesViewModel(),
        document.getElementById('dashboard-top-branches-component')
    );

    ko.applyBindings(
        new DashboardBottomBranchesViewModel(),
        document.getElementById('dashboard-bottom-branches-component')
    );
});
