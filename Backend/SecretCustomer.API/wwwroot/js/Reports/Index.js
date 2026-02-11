// Reports ViewModel
function ReportsViewModel() {
    var self = this;

    // Page name for saved filters (URL path)
    self.pageName = '/Reports';

    // State
    self.isLoading = ko.observable(false);
    self.isExporting = ko.observable(false);
    self.errorMessage = ko.observable('');

    // Lookup data
    self.projects = ko.observableArray([]);
    self.branches = ko.observableArray([]); // CustomerOrganizations
    self.evaluators = ko.observableArray([]);
    self.checklists = ko.observableArray([]);
    self.regions = ko.observableArray([]);
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

    // Temp filter values (for adding new filter)
    self.tempFilter = {
        projectId: ko.observable(null),
        branchId: ko.observable(null),
        evaluatorId: ko.observable(null),
        checklistId: ko.observable(null),
        region: ko.observable(''),
        status: ko.observable(''),
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

    // Pagination
    self.page = ko.observable(1);
    self.pageSize = ko.observable(20);

    // Filter labels
    self.filterLabels = {
        project: 'Proje',
        branch: 'Sube/Org.',
        evaluator: 'Degerlendirici',
        checklist: 'Kontrol Listesi',
        region: 'Bolge',
        status: 'Durum',
        dateRange: 'Tarih'
    };

    self.statusLabels = {
        'Completed': 'Tamamlandi',
        'InProgress': 'Devam Ediyor',
        'Draft': 'Taslak'
    };

    // Data
    self.evaluations = ko.observableArray([]);
    self.selectedDetail = ko.observable(null);

    // Summary
    self.summary = ko.observable({
        totalEvaluations: 0,
        completedEvaluations: 0,
        pendingEvaluations: 0,
        averageScore: 0,
        totalYellowCards: 0,
        totalRedCards: 0
    });

    // Pagination info
    self.paginationInfo = ko.observable({
        totalCount: 0,
        totalPages: 0,
        hasNextPage: false,
        hasPreviousPage: false
    });

    // Page numbers for pagination
    self.pageNumbers = ko.computed(function() {
        var total = self.paginationInfo().totalPages;
        var current = self.page();
        var pages = [];

        var start = Math.max(1, current - 2);
        var end = Math.min(total, current + 2);

        for (var i = start; i <= end; i++) {
            pages.push(i);
        }

        return pages;
    });

    // Can add filter computed
    self.canAddFilter = ko.computed(function() {
        var type = self.selectedFilterType();
        if (!type) return false;

        switch (type) {
            case 'project': return self.tempFilter.projectId();
            case 'branch': return self.tempFilter.branchId();
            case 'evaluator': return self.tempFilter.evaluatorId();
            case 'checklist': return self.tempFilter.checklistId();
            case 'region': return self.tempFilter.region();
            case 'status': return self.tempFilter.status() !== '';
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
            case 'project':
                var projectId = self.tempFilter.projectId();
                var project = self.projects().find(function(p) { return p.id === projectId; });
                if (!project) return;
                filter.value = projectId;
                filter.displayValue = project.code ? project.code + ' - ' + project.name : project.name;
                self.tempFilter.projectId(null);
                break;

            case 'branch':
                var branchId = self.tempFilter.branchId();
                var branch = self.branches().find(function(b) { return b.id === branchId; });
                if (!branch) return;
                filter.value = branchId;
                filter.displayValue = branch.name;
                self.tempFilter.branchId(null);
                break;

            case 'evaluator':
                var evaluatorId = self.tempFilter.evaluatorId();
                var evaluator = self.evaluators().find(function(e) { return e.id === evaluatorId; });
                if (!evaluator) return;
                filter.value = evaluatorId;
                filter.displayValue = evaluator.fullName;
                self.tempFilter.evaluatorId(null);
                break;

            case 'checklist':
                var checklistId = self.tempFilter.checklistId();
                var checklist = self.checklists().find(function(c) { return c.id === checklistId; });
                if (!checklist) return;
                filter.value = checklistId;
                filter.displayValue = checklist.name;
                self.tempFilter.checklistId(null);
                break;

            case 'region':
                var region = self.tempFilter.region();
                if (!region) return;
                filter.value = region;
                filter.displayValue = region;
                self.tempFilter.region('');
                break;

            case 'status':
                var status = self.tempFilter.status();
                if (!status) return;
                filter.value = status;
                filter.displayValue = self.statusLabels[status] || status;
                self.tempFilter.status('');
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

    // Build filter params from active filters (coklu deger destegi)
    self.buildFilterParams = function() {
        var params = {
            page: self.page(),
            pageSize: self.pageSize()
        };

        // Coklu deger icin array'ler
        var projectIds = [];
        var branchIds = [];
        var evaluatorIds = [];
        var checklistIds = [];
        var regions = [];
        var statuses = [];
        var dateRanges = [];

        self.activeFilters().forEach(function(filter) {
            switch (filter.type) {
                case 'project':
                    projectIds.push(filter.value);
                    break;
                case 'branch':
                    branchIds.push(filter.value);
                    break;
                case 'evaluator':
                    evaluatorIds.push(filter.value);
                    break;
                case 'checklist':
                    checklistIds.push(filter.value);
                    break;
                case 'region':
                    regions.push(filter.value);
                    break;
                case 'status':
                    statuses.push(filter.value);
                    break;
                case 'dateRange':
                    dateRanges.push({
                        startDate: filter.value.startDate,
                        endDate: filter.value.endDate
                    });
                    break;
            }
        });

        // Array'leri params'a ekle - HER ZAMAN ÇOĞUL KULLAN
        if (projectIds.length > 0) params.projectIds = projectIds;
        if (branchIds.length > 0) params.branchIds = branchIds;
        if (evaluatorIds.length > 0) params.evaluatorIds = evaluatorIds;
        if (checklistIds.length > 0) params.checklistIds = checklistIds;
        if (regions.length > 0) params.regions = regions;
        if (statuses.length > 0) params.statuses = statuses;

        if (dateRanges.length > 0) {
            params.startDate = dateRanges[0].startDate;
            params.endDate = dateRanges[0].endDate;
        }

        return params;
    };

    // Search
    self.search = function() {
        self.page(1);
        self.loadEvaluations();
        self.loadSummary();
    };

    // Load evaluations
    self.loadEvaluations = function() {
        self.isLoading(true);
        self.errorMessage('');

        var filterDto = self.buildFilterParams();

        fetch('/api/reports/evaluations', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            credentials: 'include',
            body: JSON.stringify(filterDto)
        })
            .then(function(response) {
                if (!response.ok) throw new Error(T('Report.LoadError', 'Veriler yuklenemedi'));
                return response.json();
            })
            .then(function(data) {
                self.evaluations(data.items);
                self.paginationInfo({
                    totalCount: data.totalCount,
                    totalPages: data.totalPages,
                    hasNextPage: data.hasNextPage,
                    hasPreviousPage: data.hasPreviousPage
                });
            })
            .catch(function(error) {
                console.error('Error:', error);
                toastr.error(error.message || T('Report.LoadErrorMessage', 'Veriler yuklenirken bir hata olustu.'));
            })
            .finally(function() {
                self.isLoading(false);
            });
    };

    // Load summary
    self.loadSummary = function() {
        var filterDto = self.buildFilterParams();

        fetch('/api/reports/summary', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            credentials: 'include',
            body: JSON.stringify(filterDto)
        })
            .then(function(response) { return response.json(); })
            .then(function(data) {
                self.summary(data);
            })
            .catch(function(error) {
                console.error('Error loading summary:', error);
            });
    };

    // Load filter options
    self.loadFilterOptions = function() {
        // Projects
        fetch('/api/projects', { credentials: 'include' })
            .then(function(res) { return res.json(); })
            .then(function(data) {
                self.projects(data.filter(function(p) { return p.isActive; }));
            })
            .catch(function(error) { console.error('Error loading projects:', error); });

        // Branches (CustomerOrganizations)
        fetch('/api/customer-organizations', { credentials: 'include' })
            .then(function(res) { return res.json(); })
            .then(function(data) {
                self.branches(data || []);
                self.regions([]);
            })
            .catch(function(error) {
                console.error('Error loading organizations:', error);
                self.branches([]);
                self.regions([]);
            });

        // Evaluators (role 3)
        fetch('/api/users/role/3', { credentials: 'include' })
            .then(function(res) { return res.json(); })
            .then(function(data) {
                self.evaluators(data);
            })
            .catch(function(error) { console.error('Error loading evaluators:', error); });

        // Checklists
        fetch('/api/checklists', { credentials: 'include' })
            .then(function(res) { return res.json(); })
            .then(function(data) {
                self.checklists(data.filter(function(c) { return c.isActive; }));
            })
            .catch(function(error) { console.error('Error loading checklists:', error); });
    };

    // Pagination
    self.previousPage = function() {
        if (self.paginationInfo().hasPreviousPage) {
            self.page(self.page() - 1);
            self.loadEvaluations();
        }
    };

    self.nextPage = function() {
        if (self.paginationInfo().hasNextPage) {
            self.page(self.page() + 1);
            self.loadEvaluations();
        }
    };

    self.goToPage = function(page) {
        self.page(page);
        self.loadEvaluations();
    };

    // View detail
    self.viewDetail = function(evaluation) {
        fetch('/api/reports/evaluations/' + evaluation.evaluationId, { credentials: 'include' })
            .then(function(response) { return response.json(); })
            .then(function(data) {
                self.selectedDetail(data);
                var modal = new bootstrap.Modal(document.getElementById('detailModal'));
                modal.show();
            })
            .catch(function(error) {
                console.error('Error:', error);
                toastr.error(T('Report.DetailLoadError', 'Detay yuklenirken bir hata olustu.'));
            });
    };

    // Export to Excel
    self.exportToExcel = function() {
        self.isExporting(true);

        var filterDto = self.buildFilterParams();
        filterDto.page = 1;
        filterDto.pageSize = 10000;

        fetch('/api/reports/export/excel', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            credentials: 'include',
            body: JSON.stringify(filterDto)
        })
            .then(function(response) {
                if (!response.ok) throw new Error(T('Report.ExportError', 'Export basarisiz'));
                return response.blob();
            })
            .then(function(blob) {
                var url = window.URL.createObjectURL(blob);
                var a = document.createElement('a');
                a.href = url;
                a.download = T('File.Evaluations', 'Degerlendirmeler') + '_' + new Date().toISOString().slice(0, 10) + '.xlsx';
                document.body.appendChild(a);
                a.click();
                window.URL.revokeObjectURL(url);
                a.remove();
            })
            .catch(function(error) {
                console.error('Error:', error);
                toastr.error(T('Report.ExcelDownloadError', 'Excel dosyasi indirilemedi.'));
            })
            .finally(function() {
                self.isExporting(false);
            });
    };

    // Export detailed to Excel
    self.exportDetailedToExcel = function() {
        self.isExporting(true);

        var filterDto = self.buildFilterParams();

        fetch('/api/reports/export/excel/detailed', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            credentials: 'include',
            body: JSON.stringify(filterDto)
        })
            .then(function(response) {
                if (!response.ok) throw new Error(T('Report.ExportError', 'Export basarisiz'));
                return response.blob();
            })
            .then(function(blob) {
                var url = window.URL.createObjectURL(blob);
                var a = document.createElement('a');
                a.href = url;
                a.download = T('File.DetailedEvaluations', 'Detayli_Degerlendirmeler') + '_' + new Date().toISOString().slice(0, 10) + '.xlsx';
                document.body.appendChild(a);
                a.click();
                window.URL.revokeObjectURL(url);
                a.remove();
            })
            .catch(function(error) {
                console.error('Error:', error);
                toastr.error(T('Report.DetailedExcelDownloadError', 'Detayli Excel dosyasi indirilemedi.'));
            })
            .finally(function() {
                self.isExporting(false);
            });
    };

    // Status helpers
    self.getStatusBadgeClass = function(status) {
        switch (status) {
            case 'Completed': return 'bg-success';
            case 'Draft': return 'bg-secondary';
            case 'InProgress': return 'bg-info';
            default: return 'bg-light text-dark';
        }
    };

    self.getStatusText = function(status) {
        return self.statusLabels[status] || status || T('Common.Unknown', 'Bilinmiyor');
    };

    // Initialize
    self.loadFilterOptions();
    self.loadEvaluations();
    self.loadSummary();
}

// Translation keys
var TRANSLATION_KEYS = [
    'Report.LoadError',
    'Report.LoadErrorMessage',
    'Report.DetailLoadError',
    'Report.ExportError',
    'Report.ExcelDownloadError',
    'Report.DetailedExcelDownloadError',
    'File.Evaluations',
    'File.DetailedEvaluations',
    'Status.Completed',
    'Common.Status.Draft',
    'Status.InProgress',
    'Common.Unknown'
];

// Apply bindings when document is ready
$(document).ready(function() {
    Localization.loadKeys(TRANSLATION_KEYS).then(function() {
        ko.applyBindings(new ReportsViewModel(), document.getElementById('reports-app'));
    });
});
