// Penalties Report ViewModel
function PenaltiesViewModel() {
    var self = this;

    // Page name for saved filters
    self.pageName = '/Reports/Penalties';

    // State
    self.isLoading = ko.observable(false);
    self.isExporting = ko.observable(false);
    self.errorMessage = ko.observable('');

    // Lookup data
    self.customers = ko.observableArray([]);
    self.projects = ko.observableArray([]);
    self.organizations = ko.observableArray([]);
    self.checklists = ko.observableArray([]);
    self.evaluators = ko.observableArray([]);
    self.dateRanges = ko.observableArray([
        { systemName: 'today', name: 'Bugun' },
        { systemName: 'yesterday', name: 'Dun' },
        { systemName: 'thisWeek', name: 'Bu Hafta' },
        { systemName: 'lastWeek', name: 'Gecen Hafta' },
        { systemName: 'thisMonth', name: 'Bu Ay' },
        { systemName: 'lastMonth', name: 'Gecen Ay' },
        { systemName: 'last7Days', name: 'Son 7 Gun' },
        { systemName: 'last30Days', name: 'Son 30 Gun' }
    ]);

    // Filter system
    self.selectedFilterType = ko.observable('');
    self.activeFilters = ko.observableArray([]);

    // Temp filter values
    self.tempFilter = {
        customerIdForFilter: ko.observable(null),
        projectId: ko.observable(null),
        organizationId: ko.observable(null),
        checklistId: ko.observable(null),
        evaluatorId: ko.observable(null),
        penaltyType: ko.observable(''),
        startDate: ko.observable(''),
        endDate: ko.observable(''),
        selectedDateRangeType: ko.observable(null)
    };

    // Tarih manuel degistirildiginde dateRangeType'i temizle
    self.tempFilter.startDate.subscribe(function(newVal) {
        if (self._manualDateChange) {
            self.tempFilter.selectedDateRangeType(null);
        }
    });
    self.tempFilter.endDate.subscribe(function(newVal) {
        if (self._manualDateChange) {
            self.tempFilter.selectedDateRangeType(null);
        }
    });
    self._manualDateChange = true;

    // Filtered options based on customer selection (for temp filter)
    self.filteredProjectsForFilter = ko.computed(function() {
        var customerId = self.tempFilter.customerIdForFilter();
        if (!customerId) return self.projects();
        return self.projects().filter(function(p) {
            return p.customerId == customerId;
        });
    });

    self.filteredOrganizationsForFilter = ko.computed(function() {
        var customerId = self.tempFilter.customerIdForFilter();
        if (!customerId) return self.organizations();
        return self.organizations().filter(function(o) {
            return o.customerId == customerId;
        });
    });

    // Pagination
    self.page = ko.observable(1);
    self.pageSize = ko.observable(50);

    // Filter labels
    self.filterLabels = {
        customerOrganization: 'Musteri/Org.',
        project: 'Proje',
        checklist: 'Kontrol Listesi',
        evaluator: 'Degerlendirici',
        penaltyType: 'Ceza Tipi',
        dateRange: 'Tarih'
    };

    self.penaltyTypeLabels = {
        'YellowCard': 'Sari Kart',
        'RedCard': 'Kirmizi Kart'
    };

    // Summary
    self.summary = ko.observable({
        totalPenalties: 0,
        totalYellowCards: 0,
        totalRedCards: 0,
        affectedEvaluations: 0
    });

    // Data
    self.penalties = ko.observableArray([]);
    self.topPenaltyQuestions = ko.observableArray([]);
    self.topPenaltyOrganizations = ko.observableArray([]);
    self.topPenaltyPersonnel = ko.observableArray([]);
    self.monthlyTrend = ko.observableArray([]);

    // Pagination
    self.totalCount = ko.observable(0);
    self.totalPages = ko.observable(0);

    // Chart instance
    var penaltyTrendChart = null;

    // Customer change handler for temp filter
    self.onCustomerChangeForFilter = function() {
        self.tempFilter.projectId(null);
        self.tempFilter.organizationId(null);
    };

    // Can add filter computed
    self.canAddFilter = ko.computed(function() {
        var type = self.selectedFilterType();
        if (!type) return false;

        switch (type) {
            case 'customerOrganization': return self.tempFilter.customerIdForFilter();
            case 'project': return self.tempFilter.projectId();
            case 'checklist': return self.tempFilter.checklistId();
            case 'evaluator': return self.tempFilter.evaluatorId();
            case 'penaltyType': return self.tempFilter.penaltyType() !== '';
            case 'dateRange': return self.tempFilter.startDate() || self.tempFilter.endDate();
            default: return false;
        }
    });

    // Add filter
    self.addFilter = function() {
        var type = self.selectedFilterType();
        if (!type) return;

        var filter = {
            type: type,
            label: self.filterLabels[type],
            value: null,
            displayValue: ''
        };

        switch (type) {
            case 'customerOrganization':
                var customerId = self.tempFilter.customerIdForFilter();
                var customer = self.customers().find(function(c) { return c.id === customerId; });
                if (!customer) return;

                var orgId = self.tempFilter.organizationId();
                var org = orgId ? self.organizations().find(function(o) { return o.id === orgId; }) : null;

                filter.value = { customerId: customerId, organizationId: orgId };
                filter.displayValue = org ? customer.name + ' / ' + org.name : customer.name;

                self.tempFilter.customerIdForFilter(null);
                self.tempFilter.organizationId(null);
                break;

            case 'project':
                var projectId = self.tempFilter.projectId();
                var project = self.projects().find(function(p) { return p.id === projectId; });
                if (!project) return;
                filter.value = projectId;
                filter.displayValue = project.name;
                self.tempFilter.projectId(null);
                break;

            case 'checklist':
                var checklistId = self.tempFilter.checklistId();
                var checklist = self.checklists().find(function(c) { return c.id === checklistId; });
                if (!checklist) return;
                filter.value = checklistId;
                filter.displayValue = checklist.name;
                self.tempFilter.checklistId(null);
                break;

            case 'evaluator':
                var evaluatorId = self.tempFilter.evaluatorId();
                var evaluator = self.evaluators().find(function(e) { return e.id === evaluatorId; });
                if (!evaluator) return;
                filter.value = evaluatorId;
                filter.displayValue = evaluator.fullName;
                self.tempFilter.evaluatorId(null);
                break;

            case 'penaltyType':
                var penaltyType = self.tempFilter.penaltyType();
                if (!penaltyType) return;
                filter.value = penaltyType;
                filter.displayValue = self.penaltyTypeLabels[penaltyType] || penaltyType;
                self.tempFilter.penaltyType('');
                break;

            case 'dateRange':
                var startDate = self.tempFilter.startDate();
                var endDate = self.tempFilter.endDate();
                var dateRangeType = self.tempFilter.selectedDateRangeType();
                if (!startDate && !endDate) return;

                filter.value = {
                    startDate: startDate,
                    endDate: endDate,
                    dateRangeType: dateRangeType
                };

                if (dateRangeType) {
                    var rangeInfo = self.dateRanges().find(function(r) { return r.systemName === dateRangeType; });
                    filter.displayValue = rangeInfo ? rangeInfo.name : dateRangeType;
                } else {
                    filter.displayValue = (startDate || '...') + ' - ' + (endDate || '...');
                }

                self.tempFilter.startDate('');
                self.tempFilter.endDate('');
                self.tempFilter.selectedDateRangeType(null);
                break;

            default:
                return;
        }

        self.activeFilters.push(filter);
        self.selectedFilterType('');
        self.search(); // Filtre eklenince otomatik ara
    };

    // Remove filter
    self.removeFilter = function(filter) {
        self.activeFilters.remove(filter);
        self.search(); // Filtre kaldirilinca otomatik ara
    };

    // Clear all filters
    self.clearFilters = function() {
        self.activeFilters([]);
        self.search();
    };

    // Set temp date range (quick select buttons)
    self.setTempDateRange = function(range) {
        var today = new Date();
        today.setHours(0, 0, 0, 0);
        var startDate = null;
        var endDate = null;

        var formatDate = function(date) {
            var year = date.getFullYear();
            var month = String(date.getMonth() + 1).padStart(2, '0');
            var day = String(date.getDate()).padStart(2, '0');
            return year + '-' + month + '-' + day;
        };

        var getMonday = function(d) {
            var date = new Date(d.getTime());
            var day = date.getDay();
            var diff = date.getDate() - day + (day === 0 ? -6 : 1);
            date.setDate(diff);
            return date;
        };

        switch (range) {
            case 'today':
                startDate = new Date(today.getTime());
                endDate = new Date(today.getTime());
                break;
            case 'yesterday':
                var yesterday = new Date(today.getTime());
                yesterday.setDate(yesterday.getDate() - 1);
                startDate = yesterday;
                endDate = new Date(yesterday.getTime());
                break;
            case 'thisWeek':
                startDate = getMonday(today);
                endDate = new Date(today.getTime());
                break;
            case 'lastWeek':
                var lastWeekStart = getMonday(today);
                lastWeekStart.setDate(lastWeekStart.getDate() - 7);
                var lastWeekEnd = new Date(lastWeekStart.getTime());
                lastWeekEnd.setDate(lastWeekEnd.getDate() + 6);
                startDate = lastWeekStart;
                endDate = lastWeekEnd;
                break;
            case 'thisMonth':
                startDate = new Date(today.getFullYear(), today.getMonth(), 1);
                endDate = new Date(today.getTime());
                break;
            case 'lastMonth':
                startDate = new Date(today.getFullYear(), today.getMonth() - 1, 1);
                endDate = new Date(today.getFullYear(), today.getMonth(), 0);
                break;
            case 'last7Days':
                startDate = new Date(today.getTime());
                startDate.setDate(startDate.getDate() - 6);
                endDate = new Date(today.getTime());
                break;
            case 'last30Days':
                startDate = new Date(today.getTime());
                startDate.setDate(startDate.getDate() - 29);
                endDate = new Date(today.getTime());
                break;
        }

        self._manualDateChange = false;
        if (startDate) self.tempFilter.startDate(formatDate(startDate));
        if (endDate) self.tempFilter.endDate(formatDate(endDate));
        self.tempFilter.selectedDateRangeType(range);
        self._manualDateChange = true;
    };

    // Build query params from active filters
    self.buildQueryParams = function() {
        var params = [];

        // Coklu deger icin array'ler
        var customerIds = [];
        var organizationIds = [];
        var projectIds = [];
        var checklistIds = [];
        var evaluatorIds = [];
        var penaltyTypes = [];
        var dateRanges = [];

        self.activeFilters().forEach(function(filter) {
            switch (filter.type) {
                case 'customerOrganization':
                    if (filter.value.customerId) customerIds.push(filter.value.customerId);
                    if (filter.value.organizationId) organizationIds.push(filter.value.organizationId);
                    break;
                case 'project':
                    projectIds.push(filter.value);
                    break;
                case 'checklist':
                    checklistIds.push(filter.value);
                    break;
                case 'evaluator':
                    evaluatorIds.push(filter.value);
                    break;
                case 'penaltyType':
                    penaltyTypes.push(filter.value);
                    break;
                case 'dateRange':
                    dateRanges.push({
                        startDate: filter.value.startDate,
                        endDate: filter.value.endDate
                    });
                    break;
            }
        });

        // Query string olustur (çoklu değer desteği)
        if (customerIds.length === 1) params.push('customerId=' + customerIds[0]);
        else if (customerIds.length > 1) customerIds.forEach(function(id) { params.push('customerIds=' + id); });

        if (organizationIds.length === 1) params.push('organizationId=' + organizationIds[0]);
        else if (organizationIds.length > 1) organizationIds.forEach(function(id) { params.push('organizationIds=' + id); });

        if (projectIds.length === 1) params.push('projectId=' + projectIds[0]);
        else if (projectIds.length > 1) projectIds.forEach(function(id) { params.push('projectIds=' + id); });

        if (checklistIds.length === 1) params.push('checklistId=' + checklistIds[0]);
        else if (checklistIds.length > 1) checklistIds.forEach(function(id) { params.push('checklistIds=' + id); });

        if (evaluatorIds.length === 1) params.push('evaluatorId=' + evaluatorIds[0]);
        else if (evaluatorIds.length > 1) evaluatorIds.forEach(function(id) { params.push('evaluatorIds=' + id); });

        if (penaltyTypes.length === 1) params.push('penaltyType=' + penaltyTypes[0]);
        else if (penaltyTypes.length > 1) penaltyTypes.forEach(function(t) { params.push('penaltyTypes=' + t); });

        if (dateRanges.length > 0) {
            if (dateRanges[0].startDate) params.push('startDate=' + dateRanges[0].startDate);
            if (dateRanges[0].endDate) params.push('endDate=' + dateRanges[0].endDate);
        }

        params.push('page=' + self.page());
        params.push('pageSize=' + self.pageSize());

        return params;
    };

    // Search
    self.search = function() {
        self.page(1);
        self.loadReport();
    };

    // Load filter options
    self.loadFilterOptions = function() {
        // Load customers
        fetch('/api/customers', { credentials: 'include' })
            .then(function(response) { return response.json(); })
            .then(function(data) {
                self.customers(data || []);
            })
            .catch(function(error) {
                console.error('Error loading customers:', error);
            });

        // Load projects
        fetch('/api/projects', { credentials: 'include' })
            .then(function(response) { return response.json(); })
            .then(function(data) {
                self.projects(data || []);
            })
            .catch(function(error) {
                console.error('Error loading projects:', error);
            });

        // Load organizations
        fetch('/api/customer-organizations', { credentials: 'include' })
            .then(function(response) { return response.json(); })
            .then(function(data) {
                self.organizations(data || []);
            })
            .catch(function(error) {
                console.error('Error loading organizations:', error);
            });

        // Load checklists
        fetch('/api/checklists', { credentials: 'include' })
            .then(function(response) { return response.json(); })
            .then(function(data) {
                self.checklists(data || []);
            })
            .catch(function(error) {
                console.error('Error loading checklists:', error);
            });

        // Load evaluators (users)
        fetch('/api/users', { credentials: 'include' })
            .then(function(response) { return response.json(); })
            .then(function(data) {
                var users = (data || []).map(function(u) {
                    return {
                        id: u.id,
                        fullName: u.firstName + ' ' + u.lastName
                    };
                });
                self.evaluators(users);
            })
            .catch(function(error) {
                console.error('Error loading evaluators:', error);
            });
    };

    // Load penalty report
    self.loadReport = function() {
        self.isLoading(true);
        self.errorMessage('');

        var params = self.buildQueryParams();
        var url = '/api/reports/penalties' + (params.length > 0 ? '?' + params.join('&') : '');

        fetch(url, { credentials: 'include' })
            .then(function(response) {
                if (!response.ok) throw new Error(T('Report.LoadError', 'Rapor yuklenemedi'));
                return response.json();
            })
            .then(function(data) {
                self.summary(data.summary || {
                    totalPenalties: 0,
                    totalYellowCards: 0,
                    totalRedCards: 0,
                    affectedEvaluations: 0
                });
                self.penalties(data.penalties || []);
                self.topPenaltyQuestions(data.topPenaltyQuestions || []);
                self.topPenaltyOrganizations(data.topPenaltyOrganizations || []);
                self.topPenaltyPersonnel(data.topPenaltyPersonnel || []);
                self.monthlyTrend(data.monthlyTrend || []);
                self.totalCount(data.totalCount || 0);
                self.totalPages(data.totalPages || 0);
                self.updateChart(data.monthlyTrend || []);
            })
            .catch(function(error) {
                console.error('Penalties report error:', error);
                toastr.error(error.message || T('Report.LoadErrorMessage', 'Rapor yuklenirken bir hata olustu.'));
            })
            .finally(function() {
                self.isLoading(false);
            });
    };

    // Update chart
    self.updateChart = function(monthlyTrend) {
        var ctx = document.getElementById('penaltyTrendChart');
        if (!ctx) return;

        if (penaltyTrendChart) {
            penaltyTrendChart.destroy();
        }

        var labels = monthlyTrend.map(function(m) { return m.monthName; });
        var yellowData = monthlyTrend.map(function(m) { return m.yellowCardCount; });
        var redData = monthlyTrend.map(function(m) { return m.redCardCount; });

        penaltyTrendChart = new Chart(ctx, {
            type: 'bar',
            data: {
                labels: labels,
                datasets: [
                    {
                        label: T('Penalty.YellowCard', 'Sari Kart'),
                        data: yellowData,
                        backgroundColor: 'rgba(255, 193, 7, 0.7)',
                        borderColor: 'rgb(255, 193, 7)',
                        borderWidth: 1
                    },
                    {
                        label: T('Penalty.RedCard', 'Kirmizi Kart'),
                        data: redData,
                        backgroundColor: 'rgba(220, 53, 69, 0.7)',
                        borderColor: 'rgb(220, 53, 69)',
                        borderWidth: 1
                    }
                ]
            },
            options: {
                responsive: true,
                maintainAspectRatio: true,
                plugins: {
                    legend: {
                        position: 'top'
                    }
                },
                scales: {
                    y: {
                        beginAtZero: true,
                        ticks: {
                            stepSize: 1
                        }
                    }
                }
            }
        });
    };

    // Pagination
    self.previousPage = function() {
        if (self.page() > 1) {
            self.page(self.page() - 1);
            self.loadReport();
        }
    };

    self.nextPage = function() {
        if (self.page() < self.totalPages()) {
            self.page(self.page() + 1);
            self.loadReport();
        }
    };

    // Export to Excel
    self.exportToExcel = function() {
        self.isExporting(true);

        var params = self.buildQueryParams();
        // Pagination parametrelerini kaldir
        params = params.filter(function(p) {
            return !p.startsWith('page=') && !p.startsWith('pageSize=');
        });

        var url = '/api/reports/penalties/export' + (params.length > 0 ? '?' + params.join('&') : '');

        fetch(url, { credentials: 'include' })
            .then(function(response) {
                if (!response.ok) throw new Error(T('Report.ExportError', 'Export basarisiz'));
                return response.blob();
            })
            .then(function(blob) {
                var url = window.URL.createObjectURL(blob);
                var a = document.createElement('a');
                a.href = url;
                a.download = T('File.PenaltyReport', 'CezaliKLRaporu') + '_' + new Date().toISOString().split('T')[0] + '.xlsx';
                document.body.appendChild(a);
                a.click();
                window.URL.revokeObjectURL(url);
                a.remove();
            })
            .catch(function(error) {
                console.error('Export error:', error);
                toastr.error(T('Report.ExcelExportError', 'Excel export basarisiz') + ': ' + error.message);
            })
            .finally(function() {
                self.isExporting(false);
            });
    };

    // Initialize
    self.loadFilterOptions();
    self.loadReport();
}

// Translation keys
var TRANSLATION_KEYS = [
    'Report.LoadError',
    'Report.LoadErrorMessage',
    'Report.ExportError',
    'Report.ExcelExportError',
    'Penalty.YellowCard',
    'Penalty.RedCard',
    'File.PenaltyReport'
];

// Apply bindings
$(document).ready(function() {
    Localization.loadKeys(TRANSLATION_KEYS).then(function() {
        ko.applyBindings(new PenaltiesViewModel(), document.getElementById('penalties-app'));
    });
});
