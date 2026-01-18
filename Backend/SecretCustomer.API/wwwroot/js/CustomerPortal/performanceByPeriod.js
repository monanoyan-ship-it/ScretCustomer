// CustomerPortal Performance By Period Report ViewModel
function PerformanceByPeriodViewModel() {
    var self = this;

    // State
    self.isLoading = ko.observable(false);
    self.isExporting = ko.observable(false);

    // Filter options
    self.projects = ko.observableArray([]);
    self.organizations = ko.observableArray([]);

    // Filter UI - Chip-based filtre sistemi
    self.selectedFilterType = ko.observable('');
    self.tempFilter = {
        projectId: ko.observable(''),
        organizationId: ko.observable('')
    };

    // Active filters (chip-based)
    self.activeFilters = ko.observableArray([]);

    // Can add filter check
    self.canAddFilter = ko.computed(function() {
        var type = self.selectedFilterType();
        if (!type) return false;
        if (type === 'project') return self.tempFilter.projectId();
        if (type === 'organization') return self.tempFilter.organizationId();
        return false;
    });

    // Add filter
    self.addFilter = function() {
        var type = self.selectedFilterType();
        if (!type) return;

        var filter = { type: type };
        var label = '';
        var displayValue = '';

        if (type === 'project') {
            filter.value = self.tempFilter.projectId();
            var project = self.projects().find(function(p) { return p.id == filter.value; });
            label = 'Proje';
            displayValue = project ? project.name : filter.value;
        } else if (type === 'organization') {
            filter.value = self.tempFilter.organizationId();
            var org = self.organizations().find(function(o) { return o.id == filter.value; });
            label = 'Organizasyon';
            displayValue = org ? org.name : filter.value;
        }

        // Tüm filtre tipleri çoklu değer destekler
        self.activeFilters.push({
            type: type,
            value: filter.value,
            label: label,
            displayValue: displayValue
        });

        // Reset temp
        self.resetTempFilter();
        self.selectedFilterType('');
        self.search(); // Filtre eklenince otomatik ara
    };

    self.resetTempFilter = function() {
        self.tempFilter.projectId('');
        self.tempFilter.organizationId('');
    };

    self.removeFilter = function(filter) {
        self.activeFilters.remove(filter);
        self.search(); // Filtre kaldırılınca otomatik ara
    };

    self.clearFilters = function() {
        self.activeFilters.removeAll();
        self.search(); // Tüm filtreler temizlenince otomatik ara
    };

    // Search
    self.search = function() {
        self.loadReport();
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

    // Build query params from active filters
    self.buildQueryParams = function() {
        var params = [];

        // Çoklu değer desteği için array'ler
        var projectIds = [];
        var organizationIds = [];

        self.activeFilters().forEach(function(f) {
            if (f.type === 'project') {
                projectIds.push(f.value);
            } else if (f.type === 'organization') {
                organizationIds.push(f.value);
            }
        });

        // Çoklu değerleri query string'e ekle (API çoğul parametre bekliyor)
        projectIds.forEach(function(id) { params.push('projectIds=' + id); });
        organizationIds.forEach(function(id) { params.push('organizationIds=' + id); });

        return params.length > 0 ? '?' + params.join('&') : '';
    };

    // Load report
    self.loadReport = function() {
        self.isLoading(true);

        var url = '/api/customer/portal/reports/performance-by-period' + self.buildQueryParams();

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

    // Export to Excel
    self.exportToExcel = function() {
        self.isExporting(true);

        var url = '/api/customer/portal/reports/performance-by-period/export' + self.buildQueryParams();

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
