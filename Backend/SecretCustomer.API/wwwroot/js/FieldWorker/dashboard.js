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
    self.pendingDealers = ko.observableArray([]);

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

    // Google Maps'te aç
    self.openInMaps = function(dealer) {
        var lat = dealer.latitude || 0;
        var lng = dealer.longitude || 0;

        if (dealer.googleMapsUrl) {
            window.open(dealer.googleMapsUrl, '_blank');
        } else {
            var url = 'https://www.google.com/maps?q=' + lat + ',' + lng;
            window.open(url, '_blank');
        }
    };

    // Ziyaret popup'ını aç
    self.startVisit = function(dealer) {
        var url = '/FieldWorker/VisitPopup?assignmentId=' + dealer.assignmentId + '&dealerId=' + dealer.dealerId;
        var width = 1200;
        var height = 800;
        var left = (screen.width - width) / 2;
        var top = (screen.height - height) / 2;
        window.open(url, 'FieldWorkerVisitPopup',
            'width=' + width + ',height=' + height + ',left=' + left + ',top=' + top + ',scrollbars=yes,resizable=yes');
    };

    // Load dashboard data
    self.loadDashboard = function() {
        self.isLoading(true);

        // Dashboard ve pending dealers'ı paralel yükle
        Promise.all([
            ApiService.get('/fieldworker/dashboard'),
            ApiService.get('/fieldworker/pending-dealers')
        ])
        .then(function(results) {
            var dashboardData = results[0];
            var pendingData = results[1];

            self.userName(dashboardData.userName || '');
            self.totalVisits(dashboardData.totalVisits || 0);
            self.todayVisits(dashboardData.todayVisits || 0);
            self.thisWeekVisits(dashboardData.thisWeekVisits || 0);
            self.pendingRequests(dashboardData.pendingRequests || 0);
            self.assignedProjects(dashboardData.assignedProjects || []);
            self.recentVisits(dashboardData.recentVisits || []);
            self.pendingDealers(pendingData || []);
        })
        .catch(function(error) {
            console.error('Error loading dashboard:', error);
            toastr.error(error.message || T('FieldWorker.LoadDashboardError', 'Dashboard yuklenirken hata olustu.'));
        })
        .finally(function() {
            self.isLoading(false);
        });
    };

    // Popup kapandığında yenile
    window.addEventListener('message', function(event) {
        if (event.data === 'visitSaved' || event.data === 'evaluationSaved') {
            self.loadDashboard();
        }
    });

    // Initialize
    self.loadDashboard();
}

// Translation keys used in this module
var TRANSLATION_KEYS = [
    'FieldWorker.LoadDashboardError',
    'FieldWorker.NoLocationData'
];

// Initialize when document is ready
$(document).ready(function() {
    var app = document.getElementById('fieldworker-dashboard-app');
    if (app) {
        Localization.loadKeys(TRANSLATION_KEYS).then(function() {
            ko.applyBindings(new FieldWorkerDashboardViewModel(), app);
        });
    }
});
