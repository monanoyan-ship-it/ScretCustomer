// Personnel Question Performance Report ViewModel
function PersonnelQuestionPerformanceViewModel() {
    var self = this;

    // State
    self.isLoading = ko.observable(false);
    self.isExporting = ko.observable(false);

    // Lookup Data
    self.customers = ko.observableArray([]);
    self.projects = ko.observableArray([]);
    self.organizations = ko.observableArray([]);
    self.periods = ko.observableArray([]);

    // Filters
    self.filter = {
        customerId: ko.observable(null),
        projectId: ko.observable(null),
        organizationId: ko.observable(null),
        periodId: ko.observable(null),
        startDate: ko.observable(''),
        endDate: ko.observable('')
    };

    // Computed: Filtered Projects by Customer
    self.filteredProjects = ko.computed(function() {
        var customerId = self.filter.customerId();
        if (!customerId) {
            return self.projects();
        }
        return self.projects().filter(function(p) {
            return p.customerId === customerId;
        });
    });

    // Computed: Filtered Organizations by Customer
    self.filteredOrganizations = ko.computed(function() {
        var customerId = self.filter.customerId();
        if (!customerId) {
            return self.organizations();
        }
        return self.organizations().filter(function(o) {
            return o.customerId === customerId;
        });
    });

    // On Customer Change - Clear dependent filters
    self.onCustomerChange = function() {
        self.filter.projectId(null);
        self.filter.organizationId(null);
    };

    // Load Lookup Data
    self.loadLookupData = function() {
        self.isLoading(true);

        Promise.all([
            apiService.get('/lookup/customers'),
            apiService.get('/lookup/projects'),
            apiService.get('/lookup/organizations'),
            apiService.get('/lookup/periods')
        ])
        .then(function(results) {
            self.customers(results[0] || []);
            self.projects(results[1] || []);
            self.organizations(results[2] || []);
            self.periods(results[3] || []);
        })
        .catch(function(error) {
            console.error('Error loading lookup data:', error);
            toastr.error('Filtre verileri yüklenirken hata oluştu.');
        })
        .finally(function() {
            self.isLoading(false);
        });
    };

    // Clear Filters
    self.clearFilters = function() {
        self.filter.customerId(null);
        self.filter.projectId(null);
        self.filter.organizationId(null);
        self.filter.periodId(null);
        self.filter.startDate('');
        self.filter.endDate('');
    };

    // Export Report
    self.exportReport = function() {
        self.isExporting(true);

        var params = [];
        if (self.filter.customerId()) {
            params.push('customerId=' + self.filter.customerId());
        }
        if (self.filter.projectId()) {
            params.push('projectId=' + self.filter.projectId());
        }
        if (self.filter.organizationId()) {
            params.push('organizationId=' + self.filter.organizationId());
        }
        if (self.filter.periodId()) {
            params.push('periodId=' + self.filter.periodId());
        }
        if (self.filter.startDate()) {
            params.push('startDate=' + self.filter.startDate());
        }
        if (self.filter.endDate()) {
            params.push('endDate=' + self.filter.endDate());
        }

        var url = '/api/reports/personnel-question-performance/export';
        if (params.length > 0) {
            url += '?' + params.join('&');
        }

        // Download file
        window.location.href = url;

        // Reset exporting state after a delay
        setTimeout(function() {
            self.isExporting(false);
        }, 2000);
    };

    // Initialize
    self.init = function() {
        self.loadLookupData();
    };

    self.init();
}

// Apply bindings
$(document).ready(function() {
    var app = document.getElementById('personnel-question-performance-app');
    if (app) {
        ko.applyBindings(new PersonnelQuestionPerformanceViewModel(), app);
    }
});
