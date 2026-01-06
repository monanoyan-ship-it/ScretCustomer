// Suggestions Report ViewModel
function SuggestionsViewModel() {
    var self = this;

    // State
    self.isLoading = ko.observable(false);
    self.isExporting = ko.observable(false);
    self.errorMessage = ko.observable('');

    // Filter options
    self.projects = ko.observableArray([]);
    self.branches = ko.observableArray([]);
    self.checklists = ko.observableArray([]);

    // Filters
    self.filter = {
        projectId: ko.observable(''),
        branchId: ko.observable(''),
        checklistId: ko.observable(''),
        searchText: ko.observable(''),
        startDate: ko.observable(''),
        endDate: ko.observable('')
    };

    // Summary
    self.summary = ko.observable({
        totalSuggestions: 0,
        totalEvaluationsWithSuggestions: 0,
        uniqueEvaluators: 0,
        uniquePersonnel: 0
    });

    // Pagination
    self.currentPage = ko.observable(1);
    self.pageSize = ko.observable(50);
    self.totalCount = ko.observable(0);
    self.totalPages = ko.computed(function() {
        return Math.ceil(self.totalCount() / self.pageSize()) || 1;
    });

    // Visible pages for pagination
    self.visiblePages = ko.computed(function() {
        var pages = [];
        var current = self.currentPage();
        var total = self.totalPages();
        var start = Math.max(1, current - 2);
        var end = Math.min(total, current + 2);

        for (var i = start; i <= end; i++) {
            pages.push(i);
        }
        return pages;
    });

    // Data
    self.suggestions = ko.observableArray([]);
    self.topSuggestedQuestions = ko.observableArray([]);

    // Load filter options
    self.loadFilterOptions = function() {
        // Load projects
        fetch('/api/projects', { credentials: 'include' })
            .then(function(response) { return response.json(); })
            .then(function(data) {
                self.projects(data);
            })
            .catch(function(error) {
                console.error('Error loading projects:', error);
            });

        // Branches modülü kaldırıldı - CustomerOrganizations kullanılıyor
        fetch('/api/customer-organizations', { credentials: 'include' })
            .then(function(response) { return response.json(); })
            .then(function(data) {
                self.branches(data || []);
            })
            .catch(function(error) {
                console.error('Error loading organizations:', error);
                self.branches([]);
            });

        // Load checklists
        fetch('/api/checklists', { credentials: 'include' })
            .then(function(response) { return response.json(); })
            .then(function(data) {
                self.checklists(data);
            })
            .catch(function(error) {
                console.error('Error loading checklists:', error);
            });
    };

    // Build query params
    self.buildQueryParams = function(includePagination) {
        var params = [];
        if (self.filter.projectId()) params.push('projectId=' + self.filter.projectId());
        if (self.filter.branchId()) params.push('branchId=' + self.filter.branchId());
        if (self.filter.checklistId()) params.push('checklistId=' + self.filter.checklistId());
        if (self.filter.searchText()) params.push('searchText=' + encodeURIComponent(self.filter.searchText()));
        if (self.filter.startDate()) params.push('startDate=' + self.filter.startDate());
        if (self.filter.endDate()) params.push('endDate=' + self.filter.endDate());
        if (includePagination) {
            params.push('page=' + self.currentPage());
            params.push('pageSize=' + self.pageSize());
        }
        return params.length > 0 ? '?' + params.join('&') : '';
    };

    // Load suggestions report
    self.loadReport = function() {
        self.isLoading(true);
        self.errorMessage('');

        var url = '/api/reports/suggestions' + self.buildQueryParams(true);

        // Load main report and top questions in parallel
        Promise.all([
            fetch(url, { credentials: 'include' }).then(function(r) { return r.json(); }),
            fetch('/api/reports/suggestions/top-questions' + self.buildQueryParams(false), { credentials: 'include' }).then(function(r) { return r.json(); })
        ])
        .then(function(results) {
            var data = results[0];
            var topQuestions = results[1];

            self.summary(data.summary || {
                totalSuggestions: 0,
                totalEvaluationsWithSuggestions: 0,
                uniqueEvaluators: 0,
                uniquePersonnel: 0
            });
            self.suggestions(data.suggestions || []);
            self.totalCount(data.totalCount || 0);
            self.topSuggestedQuestions(topQuestions || []);
        })
        .catch(function(error) {
            console.error('Suggestions report error:', error);
            toastr.error(error.message || T('Report.LoadErrorMessage', 'Rapor yüklenirken bir hata oluştu.'));
        })
        .finally(function() {
            self.isLoading(false);
        });
    };

    // Apply filters
    self.applyFilters = function() {
        self.currentPage(1);
        self.loadReport();
    };

    // Clear filters
    self.clearFilters = function() {
        self.filter.projectId('');
        self.filter.branchId('');
        self.filter.checklistId('');
        self.filter.searchText('');
        self.filter.startDate('');
        self.filter.endDate('');
        self.currentPage(1);
        self.loadReport();
    };

    // Pagination
    self.goToPage = function(page) {
        if (page >= 1 && page <= self.totalPages()) {
            self.currentPage(page);
            self.loadReport();
        }
    };

    self.previousPage = function() {
        if (self.currentPage() > 1) {
            self.goToPage(self.currentPage() - 1);
        }
    };

    self.nextPage = function() {
        if (self.currentPage() < self.totalPages()) {
            self.goToPage(self.currentPage() + 1);
        }
    };

    // Export to Excel
    self.exportToExcel = function() {
        self.isExporting(true);

        var url = '/api/reports/suggestions/export' + self.buildQueryParams(false);

        fetch(url, { credentials: 'include' })
            .then(function(response) {
                if (!response.ok) throw new Error(T('Report.ExportError', 'Export başarısız'));
                return response.blob();
            })
            .then(function(blob) {
                var url = window.URL.createObjectURL(blob);
                var a = document.createElement('a');
                a.href = url;
                a.download = T('File.SuggestionsReport', 'OnerilerRaporu') + '_' + new Date().toISOString().split('T')[0] + '.xlsx';
                document.body.appendChild(a);
                a.click();
                window.URL.revokeObjectURL(url);
                a.remove();
            })
            .catch(function(error) {
                console.error('Export error:', error);
                toastr.error(T('Report.ExcelExportError', 'Excel export başarısız') + ': ' + error.message);
            })
            .finally(function() {
                self.isExporting(false);
            });
    };

    // Initialize
    self.loadFilterOptions();
    self.loadReport();
}

// Apply bindings
$(document).ready(function() {
    ko.applyBindings(new SuggestionsViewModel(), document.getElementById('suggestions-app'));
});
