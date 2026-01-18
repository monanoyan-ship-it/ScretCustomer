// PersonnelQuestionPerformance.js - Personel Soru Bazli Performans Raporu

function PersonnelQuestionPerformanceViewModel() {
    var self = this;

    // State
    self.isLoading = ko.observable(false);
    self.isExporting = ko.observable(false);
    self.report = ko.observable(null);

    // Lookups
    self.customers = ko.observableArray([]);
    self.organizations = ko.observableArray([]);
    self.projects = ko.observableArray([]);

    // Stage 1: Hierarchical selection
    self.selectedCustomerId = ko.observable('');
    self.selectedOrganizationId = ko.observable('');
    self.selectedProjectId = ko.observable('');

    // Filtered lists
    self.filteredProjects = ko.computed(function() {
        var customerId = self.selectedCustomerId();
        if (!customerId) return self.projects();
        return self.projects().filter(function(p) { return p.customerId == customerId; });
    });

    // Stage 2: Chip-based filters
    self.activeFilters = ko.observableArray([]);
    self.selectedFilterType = ko.observable('');
    self.tempFilter = {
        startDate: ko.observable(''),
        endDate: ko.observable(''),
        dateRangeType: ko.observable('')
    };

    // Date range labels
    self.dateRangeLabels = {
        'today': 'Bugun',
        'yesterday': 'Dun',
        'thisWeek': 'Bu Hafta',
        'lastWeek': 'Gecen Hafta',
        'thisMonth': 'Bu Ay',
        'lastMonth': 'Gecen Ay',
        'last3Months': 'Son 3 Ay',
        'last6Months': 'Son 6 Ay',
        'thisYear': 'Bu Yil',
        'lastYear': 'Gecen Yil'
    };

    // Can add filter computed
    self.canAddFilter = ko.computed(function() {
        var type = self.selectedFilterType();
        if (type === 'dateRange') {
            return self.tempFilter.startDate() || self.tempFilter.endDate();
        }
        return false;
    });

    // Hierarchical change handlers
    self.onCustomerChange = function() {
        self.selectedOrganizationId('');
        self.organizations([]);
        self.report(null);

        var customerId = self.selectedCustomerId();
        if (customerId) {
            // Load organizations for selected customer
            fetch('/api/reports/organizations/' + customerId, { credentials: 'include' })
                .then(function(response) { return response.json(); })
                .then(function(data) {
                    self.organizations(data || []);
                })
                .catch(function(error) {
                    console.error('Error loading organizations:', error);
                });
        }
    };

    self.onOrganizationChange = function() {
        self.report(null);
    };

    // Calculate date range
    self.calculateDateRange = function(rangeType) {
        var today = new Date();
        var start, end;

        switch (rangeType) {
            case 'today':
                start = end = today;
                break;
            case 'yesterday':
                start = end = new Date(today.getTime() - 86400000);
                break;
            case 'thisWeek':
                var dayOfWeek = today.getDay();
                var diff = dayOfWeek === 0 ? 6 : dayOfWeek - 1;
                start = new Date(today.getTime() - diff * 86400000);
                end = today;
                break;
            case 'lastWeek':
                var dayOfWeek = today.getDay();
                var diff = dayOfWeek === 0 ? 6 : dayOfWeek - 1;
                var thisWeekStart = new Date(today.getTime() - diff * 86400000);
                start = new Date(thisWeekStart.getTime() - 7 * 86400000);
                end = new Date(thisWeekStart.getTime() - 86400000);
                break;
            case 'thisMonth':
                start = new Date(today.getFullYear(), today.getMonth(), 1);
                end = today;
                break;
            case 'lastMonth':
                start = new Date(today.getFullYear(), today.getMonth() - 1, 1);
                end = new Date(today.getFullYear(), today.getMonth(), 0);
                break;
            case 'last3Months':
                start = new Date(today.getFullYear(), today.getMonth() - 3, 1);
                end = today;
                break;
            case 'last6Months':
                start = new Date(today.getFullYear(), today.getMonth() - 6, 1);
                end = today;
                break;
            case 'thisYear':
                start = new Date(today.getFullYear(), 0, 1);
                end = today;
                break;
            case 'lastYear':
                start = new Date(today.getFullYear() - 1, 0, 1);
                end = new Date(today.getFullYear() - 1, 11, 31);
                break;
            default:
                return null;
        }

        var formatDate = function(d) {
            return d.getFullYear() + '-' + String(d.getMonth() + 1).padStart(2, '0') + '-' + String(d.getDate()).padStart(2, '0');
        };

        return {
            start: formatDate(start),
            end: formatDate(end)
        };
    };

    // Set temp date range
    self.setTempDateRange = function(rangeType) {
        var range = self.calculateDateRange(rangeType);
        if (range) {
            self.tempFilter.startDate(range.start);
            self.tempFilter.endDate(range.end);
            self.tempFilter.dateRangeType(rangeType);
        }
    };

    // Add filter
    self.addFilter = function() {
        var type = self.selectedFilterType();

        if (type === 'dateRange') {
            var startDate = self.tempFilter.startDate();
            var endDate = self.tempFilter.endDate();
            var rangeType = self.tempFilter.dateRangeType();

            if (startDate || endDate) {
                // Remove existing date range filter
                self.activeFilters.remove(function(f) { return f.type === 'dateRange'; });

                var displayValue = rangeType ? self.dateRangeLabels[rangeType] : (startDate + ' - ' + endDate);

                self.activeFilters.push({
                    type: 'dateRange',
                    startDate: startDate,
                    endDate: endDate,
                    dateRangeType: rangeType,
                    label: 'Tarih',
                    displayValue: displayValue
                });

                // Clear temp
                self.tempFilter.startDate('');
                self.tempFilter.endDate('');
                self.tempFilter.dateRangeType('');
                self.selectedFilterType('');
            }
        }
    };

    // Remove filter
    self.removeFilter = function(filter) {
        self.activeFilters.remove(filter);
    };

    // Clear all filters
    self.clearAllFilters = function() {
        self.activeFilters.removeAll();
    };

    // Clear all (including hierarchical)
    self.clearFilters = function() {
        self.selectedCustomerId('');
        self.selectedOrganizationId('');
        self.selectedProjectId('');
        self.activeFilters.removeAll();
        self.report(null);
    };

    // Build filter params using URLSearchParams
    self.buildFilterParams = function() {
        var params = new URLSearchParams();

        // Hierarchical filters
        if (self.selectedCustomerId()) {
            params.append('customerIds', self.selectedCustomerId());
        }
        if (self.selectedOrganizationId()) {
            params.append('organizationIds', self.selectedOrganizationId());
        }
        if (self.selectedProjectId()) {
            params.append('projectIds', self.selectedProjectId());
        }

        // Active filters
        self.activeFilters().forEach(function(filter) {
            if (filter.type === 'dateRange') {
                if (filter.startDate) params.append('startDate', filter.startDate);
                if (filter.endDate) params.append('endDate', filter.endDate);
            }
        });

        return params.toString();
    };

    // Load report
    self.loadReport = function() {
        self.isLoading(true);
        self.report(null);

        var params = self.buildFilterParams();
        var url = '/api/reports/personnel-question-performance';
        if (params) url += '?' + params;

        fetch(url, { credentials: 'include' })
            .then(function(response) {
                if (!response.ok) throw new Error('Network response was not ok');
                return response.json();
            })
            .then(function(data) {
                self.report(data);
            })
            .catch(function(error) {
                console.error('Error loading report:', error);
                toastr.error('Rapor yuklenirken hata olustu.');
            })
            .finally(function() {
                self.isLoading(false);
            });
    };

    // Export to Excel
    self.exportToExcel = function() {
        self.isExporting(true);

        var params = self.buildFilterParams();
        var url = '/api/reports/personnel-question-performance/export';
        if (params) url += '?' + params;

        fetch(url, { credentials: 'include' })
            .then(function(response) {
                if (!response.ok) throw new Error('Network response was not ok');
                return response.blob();
            })
            .then(function(blob) {
                var filename = 'PersonelSoruPerformansi_' + new Date().toISOString().split('T')[0] + '.xlsx';
                var a = document.createElement('a');
                a.href = URL.createObjectURL(blob);
                a.download = filename;
                a.click();
                URL.revokeObjectURL(a.href);
                toastr.success('Excel dosyasi indirildi.');
            })
            .catch(function(error) {
                console.error('Error exporting:', error);
                toastr.error('Excel export sirasinda hata olustu.');
            })
            .finally(function() {
                self.isExporting(false);
            });
    };

    // Get score cell CSS class
    self.getScoreCellClass = function(score) {
        if (score === null || score === undefined) return '';
        if (score >= 80) return 'table-success-light';
        if (score >= 60) return 'table-warning-light';
        return 'table-danger-light';
    };

    // Load lookups
    self.loadLookups = function() {
        fetch('/api/reports/lookups', { credentials: 'include' })
            .then(function(response) { return response.json(); })
            .then(function(data) {
                self.customers(data.customers || []);
                self.projects(data.projects || []);
            })
            .catch(function(error) {
                console.error('Error loading lookups:', error);
            });
        // Organizations are loaded when customer is selected (onCustomerChange)
    };

    // Initialize
    self.loadLookups();
}

// Apply bindings
$(document).ready(function() {
    var app = document.getElementById('personnel-question-performance-app');
    if (app) {
        ko.applyBindings(new PersonnelQuestionPerformanceViewModel(), app);
    }
});
