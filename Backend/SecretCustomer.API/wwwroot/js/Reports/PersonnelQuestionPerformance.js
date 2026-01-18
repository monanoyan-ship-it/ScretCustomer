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

    // Filter UI state
    self.selectedFilterType = ko.observable('');
    self.tempFilter = {
        customerId: ko.observable(''),
        projectId: ko.observable(''),
        organizationId: ko.observable(''),
        startDate: ko.observable(''),
        endDate: ko.observable(''),
        dateRangeType: ko.observable('')
    };

    // Active filters (chip-based)
    self.activeFilters = ko.observableArray([]);

    // Date range labels
    self.dateRangeLabels = {
        'today': 'Bugün',
        'yesterday': 'Dün',
        'thisWeek': 'Bu Hafta',
        'lastWeek': 'Geçen Hafta',
        'thisMonth': 'Bu Ay',
        'lastMonth': 'Geçen Ay',
        'last3Months': 'Son 3 Ay',
        'last6Months': 'Son 6 Ay',
        'thisYear': 'Bu Yıl',
        'lastYear': 'Geçen Yıl'
    };

    // Computed: Filtered Projects by selected customer in temp filter
    self.filteredProjects = ko.computed(function() {
        var customerId = self.tempFilter.customerId();
        if (!customerId) {
            return self.projects();
        }
        return self.projects().filter(function(p) {
            return p.customerId == customerId;
        });
    });

    // Computed: Filtered Organizations by selected customer in temp filter
    self.filteredOrganizations = ko.computed(function() {
        var customerId = self.tempFilter.customerId();
        if (!customerId) {
            return self.organizations();
        }
        return self.organizations().filter(function(o) {
            return o.customerId == customerId;
        });
    });

    // Can add filter check
    self.canAddFilter = ko.computed(function() {
        var type = self.selectedFilterType();
        if (!type) return false;
        if (type === 'customer') return self.tempFilter.customerId();
        if (type === 'project') return self.tempFilter.projectId();
        if (type === 'organization') return self.tempFilter.organizationId();
        if (type === 'dateRange') return self.tempFilter.startDate() || self.tempFilter.endDate() || self.tempFilter.dateRangeType();
        return false;
    });

    // Load Lookup Data
    self.loadLookupData = function() {
        self.isLoading(true);

        Promise.all([
            fetch('/api/customers/active', { credentials: 'include' }).then(r => r.json()),
            fetch('/api/projects', { credentials: 'include' }).then(r => r.json()).then(d => d.items || d),
            fetch('/api/customer-organizations', { credentials: 'include' }).then(r => r.json()).then(d => d.items || d)
        ])
        .then(function(results) {
            self.customers(results[0] || []);
            self.projects(results[1] || []);
            self.organizations(results[2] || []);
        })
        .catch(function(error) {
            console.error('Error loading lookup data:', error);
        })
        .finally(function() {
            self.isLoading(false);
        });
    };

    // Calculate date range from type
    self.calculateDateRange = function(rangeType) {
        var today = new Date();
        var start, end;

        if (rangeType === 'today') {
            start = end = today.toISOString().split('T')[0];
        } else if (rangeType === 'yesterday') {
            var yesterday = new Date(today);
            yesterday.setDate(yesterday.getDate() - 1);
            start = end = yesterday.toISOString().split('T')[0];
        } else if (rangeType === 'thisWeek') {
            var dayOfWeek = today.getDay();
            var diff = today.getDate() - dayOfWeek + (dayOfWeek === 0 ? -6 : 1);
            var weekStart = new Date(today);
            weekStart.setDate(diff);
            start = weekStart.toISOString().split('T')[0];
            end = today.toISOString().split('T')[0];
        } else if (rangeType === 'lastWeek') {
            var dayOfWeek = today.getDay();
            var diff = today.getDate() - dayOfWeek + (dayOfWeek === 0 ? -6 : 1);
            var lastWeekEnd = new Date(today);
            lastWeekEnd.setDate(diff - 1);
            var lastWeekStart = new Date(lastWeekEnd);
            lastWeekStart.setDate(lastWeekEnd.getDate() - 6);
            start = lastWeekStart.toISOString().split('T')[0];
            end = lastWeekEnd.toISOString().split('T')[0];
        } else if (rangeType === 'thisMonth') {
            start = new Date(today.getFullYear(), today.getMonth(), 1).toISOString().split('T')[0];
            end = today.toISOString().split('T')[0];
        } else if (rangeType === 'lastMonth') {
            start = new Date(today.getFullYear(), today.getMonth() - 1, 1).toISOString().split('T')[0];
            end = new Date(today.getFullYear(), today.getMonth(), 0).toISOString().split('T')[0];
        } else if (rangeType === 'last3Months') {
            start = new Date(today.getFullYear(), today.getMonth() - 2, 1).toISOString().split('T')[0];
            end = today.toISOString().split('T')[0];
        } else if (rangeType === 'last6Months') {
            start = new Date(today.getFullYear(), today.getMonth() - 5, 1).toISOString().split('T')[0];
            end = today.toISOString().split('T')[0];
        } else if (rangeType === 'thisYear') {
            start = new Date(today.getFullYear(), 0, 1).toISOString().split('T')[0];
            end = today.toISOString().split('T')[0];
        } else if (rangeType === 'lastYear') {
            start = new Date(today.getFullYear() - 1, 0, 1).toISOString().split('T')[0];
            end = new Date(today.getFullYear() - 1, 11, 31).toISOString().split('T')[0];
        }

        return { start: start, end: end };
    };

    // Set date range in dropdown (sadece değer atar)
    self.setDateRange = function(rangeType) {
        var range = self.calculateDateRange(rangeType);
        self.tempFilter.startDate(range.start);
        self.tempFilter.endDate(range.end);
        self.tempFilter.dateRangeType(rangeType);
    };

    // Quick date range filter - direkt uygula (hızlı erişim butonları için)
    self.setQuickDateRange = function(rangeType) {
        var range = self.calculateDateRange(rangeType);
        var displayValue = self.dateRangeLabels[rangeType] || (range.start + ' - ' + range.end);

        self.activeFilters.push({
            type: 'dateRange',
            value: null,
            startDate: range.start,
            endDate: range.end,
            dateRangeType: rangeType,
            label: 'Tarih',
            displayValue: displayValue
        });
    };

    // Add filter
    self.addFilter = function() {
        var type = self.selectedFilterType();
        if (!type) return;

        var filter = { type: type };
        var label = '';
        var displayValue = '';

        if (type === 'customer') {
            filter.value = self.tempFilter.customerId();
            var customer = self.customers().find(function(c) { return c.id == filter.value; });
            label = 'Müşteri';
            displayValue = customer ? customer.companyName : filter.value;
        } else if (type === 'project') {
            filter.value = self.tempFilter.projectId();
            var project = self.projects().find(function(p) { return p.id == filter.value; });
            label = 'Proje';
            displayValue = project ? project.name : filter.value;
        } else if (type === 'organization') {
            filter.value = self.tempFilter.organizationId();
            var org = self.organizations().find(function(o) { return o.id == filter.value; });
            label = 'Organizasyon';
            displayValue = org ? org.name : filter.value;
        } else if (type === 'dateRange') {
            filter.dateRangeType = self.tempFilter.dateRangeType();
            filter.startDate = self.tempFilter.startDate();
            filter.endDate = self.tempFilter.endDate();
            label = 'Tarih';
            if (filter.dateRangeType && self.dateRangeLabels[filter.dateRangeType]) {
                displayValue = self.dateRangeLabels[filter.dateRangeType];
            } else {
                displayValue = (filter.startDate || '...') + ' - ' + (filter.endDate || '...');
            }
        }

        self.activeFilters.push({
            type: type,
            value: filter.value,
            startDate: filter.startDate,
            endDate: filter.endDate,
            dateRangeType: filter.dateRangeType,
            label: label,
            displayValue: displayValue
        });

        // Reset temp filter
        self.resetTempFilter();
        self.selectedFilterType('');
    };

    self.resetTempFilter = function() {
        self.tempFilter.customerId('');
        self.tempFilter.projectId('');
        self.tempFilter.organizationId('');
        self.tempFilter.startDate('');
        self.tempFilter.endDate('');
        self.tempFilter.dateRangeType('');
    };

    // Remove single filter
    self.removeFilter = function(filter) {
        self.activeFilters.remove(filter);
    };

    // Clear all filters
    self.clearFilters = function() {
        self.activeFilters.removeAll();
    };

    // Build query params from active filters
    self.buildQueryParams = function() {
        var params = {};
        var customerIds = [];
        var projectIds = [];
        var organizationIds = [];
        var startDate = null;
        var endDate = null;

        self.activeFilters().forEach(function(f) {
            switch (f.type) {
                case 'customer':
                    customerIds.push(f.value);
                    break;
                case 'project':
                    projectIds.push(f.value);
                    break;
                case 'organization':
                    organizationIds.push(f.value);
                    break;
                case 'dateRange':
                    if (f.dateRangeType && self.dateRangeLabels[f.dateRangeType]) {
                        var range = self.calculateDateRange(f.dateRangeType);
                        startDate = range.start;
                        endDate = range.end;
                    } else {
                        if (f.startDate) startDate = f.startDate;
                        if (f.endDate) endDate = f.endDate;
                    }
                    break;
            }
        });

        if (customerIds.length > 0) params.customerIds = customerIds;
        if (projectIds.length > 0) params.projectIds = projectIds;
        if (organizationIds.length > 0) params.organizationIds = organizationIds;
        if (startDate) params.startDate = startDate;
        if (endDate) params.endDate = endDate;

        return params;
    };

    // Export Report
    self.exportReport = function() {
        self.isExporting(true);

        var params = self.buildQueryParams();
        var queryParts = [];

        Object.keys(params).forEach(function(key) {
            var value = params[key];
            if (Array.isArray(value)) {
                value.forEach(function(v) {
                    queryParts.push(key + '=' + encodeURIComponent(v));
                });
            } else {
                queryParts.push(key + '=' + encodeURIComponent(value));
            }
        });

        var url = '/api/reports/personnel-question-performance/export';
        if (queryParts.length > 0) {
            url += '?' + queryParts.join('&');
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
