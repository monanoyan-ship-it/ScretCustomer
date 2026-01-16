// CustomerPortal Performance By Period Report ViewModel
function PerformanceByPeriodViewModel() {
    var self = this;

    // State
    self.isLoading = ko.observable(false);
    self.isExporting = ko.observable(false);

    // Filter options
    self.projects = ko.observableArray([]);
    self.organizations = ko.observableArray([]);

    // Filters
    self.filter = {
        projectId: ko.observable(''),
        organizationId: ko.observable('')
    };

    // Report data
    self.periods = ko.observableArray([]);
    self.reportData = ko.observableArray([]);

    // Computed
    self.hasData = ko.computed(function() {
        return self.reportData().length > 0;
    });

    // Load filter options
    self.loadFilterOptions = function() {
        // Load projects
        customerApiFetch('/api/customer/portal/projects')
            .then(function(response) { return response.json(); })
            .then(function(data) {
                self.projects(data || []);
            })
            .catch(function(error) {
                console.error('Error loading projects:', error);
            });

        // Load organizations
        customerApiFetch('/api/customer/portal/organizations')
            .then(function(response) { return response.json(); })
            .then(function(data) {
                var orgs = [];
                (data || []).forEach(function(group) {
                    (group.organizations || []).forEach(function(org) {
                        orgs.push(org);
                    });
                });
                self.organizations(orgs);
            })
            .catch(function(error) {
                console.error('Error loading organizations:', error);
            });
    };

    // Build query params
    self.buildQueryParams = function() {
        var params = [];
        if (self.filter.projectId()) params.push('projectId=' + self.filter.projectId());
        if (self.filter.organizationId()) params.push('organizationId=' + self.filter.organizationId());
        return params;
    };

    // Load report
    self.loadReport = function() {
        self.isLoading(true);

        var params = self.buildQueryParams();
        var url = '/api/customer/portal/reports/performance-by-period' + (params.length > 0 ? '?' + params.join('&') : '');

        customerApiFetch(url)
            .then(function(response) {
                if (!response.ok) throw new Error('Rapor yuklenemedi');
                return response.json();
            })
            .then(function(data) {
                self.periods(data.periods || []);
                self.reportData(data.data || []);
            })
            .catch(function(error) {
                console.error('Performance by period report error:', error);
                toastr.error(error.message || 'Rapor yuklenirken bir hata olustu.');
            })
            .finally(function() {
                self.isLoading(false);
            });
    };

    // Clear filters
    self.clearFilters = function() {
        self.filter.projectId('');
        self.filter.organizationId('');
        self.loadReport();
    };

    // Export to Excel
    self.exportToExcel = function() {
        self.isExporting(true);

        var params = self.buildQueryParams();
        var url = '/api/customer/portal/reports/performance-by-period/export' + (params.length > 0 ? '?' + params.join('&') : '');

        customerApiFetch(url)
            .then(function(response) {
                if (!response.ok) throw new Error('Export basarisiz');
                return response.blob();
            })
            .then(function(blob) {
                var url = window.URL.createObjectURL(blob);
                var a = document.createElement('a');
                a.href = url;
                a.download = 'DonemBazliBasari_' + new Date().toISOString().split('T')[0] + '.xlsx';
                document.body.appendChild(a);
                a.click();
                window.URL.revokeObjectURL(url);
                a.remove();
            })
            .catch(function(error) {
                console.error('Export error:', error);
                toastr.error('Excel export basarisiz: ' + error.message);
            })
            .finally(function() {
                self.isExporting(false);
            });
    };

    // Score cell class helper
    self.getScoreCellClass = function(score) {
        if (score === null || score === undefined) return '';
        if (score >= 80) return 'table-success-light';
        if (score >= 60) return 'table-warning-light';
        return 'table-danger-light';
    };

    // Initialize
    self.loadFilterOptions();
    self.loadReport();
}

// Apply bindings when DOM is ready
$(document).ready(function() {
    ko.applyBindings(new PerformanceByPeriodViewModel(), document.getElementById('performance-by-period-app'));
});
