// FieldWorker Dashboard ViewModel
function FieldWorkerDashboardViewModel() {
    var self = this;

    // State
    self.isLoading = ko.observable(true);
    self.userName = ko.observable('');

    // Stats
    self.totalVisits = ko.observable(0);
    self.todayVisits = ko.observable(0);
    self.thisWeekVisits = ko.observable(0);
    self.pendingRequests = ko.observable(0);

    // Data
    self.assignedProjects = ko.observableArray([]);
    self.recentVisits = ko.observableArray([]);

    // Status helpers
    self.getStatusBadgeClass = function(statusId) {
        var classes = {
            1: 'bg-warning text-dark', // Draft
            2: 'bg-success',            // Completed
            3: 'bg-info',               // Submitted
            4: 'bg-danger'              // Rejected
        };
        return classes[statusId] || 'bg-secondary';
    };

    self.formatDate = function(dateStr) {
        if (!dateStr) return '-';
        var date = new Date(dateStr);
        return date.toLocaleDateString('tr-TR');
    };

    // Load dashboard data
    self.loadDashboard = function() {
        self.isLoading(true);

        ApiService.get('/fieldworker/dashboard')
            .then(function(data) {
                self.userName(data.userName || '');
                self.totalVisits(data.totalVisits || 0);
                self.todayVisits(data.todayVisits || 0);
                self.thisWeekVisits(data.thisWeekVisits || 0);
                self.pendingRequests(data.pendingRequests || 0);
                self.assignedProjects(data.assignedProjects || []);
                self.recentVisits(data.recentVisits || []);
            })
            .catch(function(error) {
                console.error('Error loading dashboard:', error);
                toastr.error(error.message || 'Dashboard yuklenirken hata olustu.');
            })
            .finally(function() {
                self.isLoading(false);
            });
    };

    // Initialize
    self.loadDashboard();
}

// Initialize when document is ready
$(document).ready(function() {
    var app = document.getElementById('fieldworker-dashboard-app');
    if (app) {
        ko.applyBindings(new FieldWorkerDashboardViewModel(), app);
    }
});
