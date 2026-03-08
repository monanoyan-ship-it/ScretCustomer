// Performance Tracking Report ViewModel
function PerformanceTrackingViewModel() {
    var self = this;

    // Observables
    self.isLoading = ko.observable(false);
    self.evaluatorPerformances = ko.observableArray([]);
    self.customerQuotaStatuses = ko.observableArray([]);
    self.projectTypePerformances = ko.observableArray([]);
    self.summary = ko.observable({
        totalEvaluators: 0,
        totalActiveCustomers: 0,
        totalTodayEvaluations: 0,
        totalWeekEvaluations: 0,
        totalMonthEvaluations: 0,
        totalYearEvaluations: 0
    });

    // Filter data sources
    self.customers = ko.observableArray([]);
    self.evaluators = ko.observableArray([]);
    self.projects = ko.observableArray([]);

    // Filter state
    self.selectedFilterType = ko.observable('');
    self.activeFilters = ko.observableArray([]);
    self.tempFilter = {
        customerId: ko.observable(null),
        evaluatorId: ko.observable(null),
        projectId: ko.observable(null),
        startDate: ko.observable(''),
        endDate: ko.observable(''),
        dateRangeType: ko.observable('')
    };

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

    // Calculate date range from type
    self.calculateDateRange = function(rangeType) {
        var today = new Date();
        var start, end;

        if (rangeType === 'today') {
            start = end = formatLocalDate(today);
        } else if (rangeType === 'yesterday') {
            var yesterday = new Date(today);
            yesterday.setDate(yesterday.getDate() - 1);
            start = end = formatLocalDate(yesterday);
        } else if (rangeType === 'thisWeek') {
            var dayOfWeek = today.getDay();
            var diff = today.getDate() - dayOfWeek + (dayOfWeek === 0 ? -6 : 1);
            var weekStart = new Date(today);
            weekStart.setDate(diff);
            start = formatLocalDate(weekStart);
            end = formatLocalDate(today);
        } else if (rangeType === 'lastWeek') {
            var dayOfWeek = today.getDay();
            var diff = today.getDate() - dayOfWeek + (dayOfWeek === 0 ? -6 : 1);
            var lastWeekEnd = new Date(today);
            lastWeekEnd.setDate(diff - 1);
            var lastWeekStart = new Date(lastWeekEnd);
            lastWeekStart.setDate(lastWeekEnd.getDate() - 6);
            start = formatLocalDate(lastWeekStart);
            end = formatLocalDate(lastWeekEnd);
        } else if (rangeType === 'thisMonth') {
            start = formatLocalDate(new Date(today.getFullYear(), today.getMonth(), 1));
            end = formatLocalDate(today);
        } else if (rangeType === 'lastMonth') {
            start = formatLocalDate(new Date(today.getFullYear(), today.getMonth() - 1, 1));
            end = formatLocalDate(new Date(today.getFullYear(), today.getMonth(), 0));
        } else if (rangeType === 'last3Months') {
            start = formatLocalDate(new Date(today.getFullYear(), today.getMonth() - 2, 1));
            end = formatLocalDate(today);
        } else if (rangeType === 'last6Months') {
            start = formatLocalDate(new Date(today.getFullYear(), today.getMonth() - 5, 1));
            end = formatLocalDate(today);
        } else if (rangeType === 'thisYear') {
            start = formatLocalDate(new Date(today.getFullYear(), 0, 1));
            end = formatLocalDate(today);
        } else if (rangeType === 'lastYear') {
            start = formatLocalDate(new Date(today.getFullYear() - 1, 0, 1));
            end = formatLocalDate(new Date(today.getFullYear() - 1, 11, 31));
        }

        return { start: start, end: end };
    };

    // Set date range in dropdown
    self.setDateRange = function(rangeType) {
        var range = self.calculateDateRange(rangeType);
        self.tempFilter.startDate(range.start);
        self.tempFilter.endDate(range.end);
        self.tempFilter.dateRangeType(rangeType);
    };

    // Can add filter
    self.canAddFilter = ko.computed(function() {
        var type = self.selectedFilterType();
        if (!type) return false;

        if (type === 'customer') return !!self.tempFilter.customerId();
        if (type === 'evaluator') return !!self.tempFilter.evaluatorId();
        if (type === 'project') return !!self.tempFilter.projectId();
        if (type === 'dateRange') return !!self.tempFilter.startDate() && !!self.tempFilter.endDate();

        return false;
    });

    // Add filter
    self.addFilter = function() {
        var type = self.selectedFilterType();
        if (!type) return;

        var filter = { type: type };

        if (type === 'customer') {
            var id = self.tempFilter.customerId();
            var item = self.customers().find(function(c) { return c.id == id; });
            if (!item) return;
            filter.value = id;
            filter.label = T('Report.Customer', 'Firma');
            filter.displayValue = item.companyName;
            self.tempFilter.customerId(null);
        } else if (type === 'evaluator') {
            var id = self.tempFilter.evaluatorId();
            var item = self.evaluators().find(function(e) { return e.id == id; });
            if (!item) return;
            filter.value = id;
            filter.label = T('Report.Evaluator', 'Dinleyici');
            filter.displayValue = item.fullName;
            self.tempFilter.evaluatorId(null);
        } else if (type === 'project') {
            var id = self.tempFilter.projectId();
            var item = self.projects().find(function(p) { return p.id == id; });
            if (!item) return;
            filter.value = id;
            filter.label = T('Common.Project', 'Proje');
            filter.displayValue = item.name;
            self.tempFilter.projectId(null);
        } else if (type === 'dateRange') {
            filter.startDate = self.tempFilter.startDate();
            filter.endDate = self.tempFilter.endDate();
            filter.dateRangeType = self.tempFilter.dateRangeType();
            filter.label = T('Common.DateRange', 'Tarih');
            filter.displayValue = self.dateRangeLabels[filter.dateRangeType] || (filter.startDate + ' - ' + filter.endDate);

            self.tempFilter.startDate('');
            self.tempFilter.endDate('');
            self.tempFilter.dateRangeType('');
        }

        self.activeFilters.push(filter);
        self.selectedFilterType('');
        self.search(); // Otomatik ara
    };

    // Remove filter
    self.removeFilter = function(filter) {
        self.activeFilters.remove(filter);
        self.search(); // Otomatik ara
    };

    // Clear all filters
    self.clearFilters = function() {
        self.activeFilters.removeAll();
        self.search(); // Otomatik ara
    };

    // Search with filters
    self.search = function() {
        self.loadData();
    };

    // Build query params from active filters
    self.buildQueryParams = function() {
        var params = new URLSearchParams();

        self.activeFilters().forEach(function(filter) {
            if (filter.type === 'customer') {
                params.append('customerIds', filter.value);
            } else if (filter.type === 'evaluator') {
                params.append('evaluatorIds', filter.value);
            } else if (filter.type === 'project') {
                params.append('projectIds', filter.value);
            } else if (filter.type === 'dateRange') {
                params.append('startDate', filter.startDate);
                params.append('endDate', filter.endDate);
            }
        });

        return params.toString();
    };

    // Load filter options
    self.loadFilterOptions = function() {
        // Load customers
        ApiService.get('/customers?pageSize=1000')
            .then(function(data) {
                var items = data.items || data || [];
                self.customers(items);
            })
            .catch(function(error) {
                console.error('Error loading customers:', error);
            });

        // Load evaluators (users with evaluator role)
        ApiService.get('/users?pageSize=1000')
            .then(function(data) {
                var items = data.items || data || [];
                self.evaluators(items.map(function(u) {
                    return {
                        id: u.id,
                        fullName: (u.firstName || '') + ' ' + (u.lastName || u.userName || '')
                    };
                }));
            })
            .catch(function(error) {
                console.error('Error loading evaluators:', error);
            });

        // Load projects
        ApiService.get('/projects?pageSize=1000')
            .then(function(data) {
                var items = data.items || data || [];
                self.projects(items);
            })
            .catch(function(error) {
                console.error('Error loading projects:', error);
            });
    };

    // Load data
    self.loadData = function() {
        self.isLoading(true);

        var queryParams = self.buildQueryParams();
        var url = '/reports/performance-tracking' + (queryParams ? '?' + queryParams : '');

        ApiService.get(url)
            .then(function(data) {
                self.evaluatorPerformances(data.evaluatorPerformances || []);
                self.customerQuotaStatuses(data.customerQuotaStatuses || []);
                self.projectTypePerformances(data.projectTypePerformances || []);
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
    self.loadFilterOptions();
    self.loadData();
}

// Translation keys
var TRANSLATION_KEYS = [
    'Report.LoadError',
    'Report.Customer',
    'Report.Evaluator',
    'Common.Project',
    'Common.DateRange'
];

// Initialize when DOM ready
$(document).ready(function() {
    Localization.loadKeys(TRANSLATION_KEYS).then(function() {
        ko.applyBindings(new PerformanceTrackingViewModel(), document.getElementById('performance-tracking-app'));
    });
});
