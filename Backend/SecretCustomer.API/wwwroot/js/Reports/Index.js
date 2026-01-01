// Reports ViewModel
function ReportsViewModel() {
    var self = this;

    // State
    self.isLoading = ko.observable(false);
    self.isExporting = ko.observable(false);
    self.errorMessage = ko.observable('');

    // Filter
    self.filter = {
        projectId: ko.observable(''),
        branchId: ko.observable(''),
        evaluatorId: ko.observable(''),
        checklistId: ko.observable(''),
        region: ko.observable(''),
        status: ko.observable(''),
        startDate: ko.observable(''),
        endDate: ko.observable(''),
        page: ko.observable(1),
        pageSize: ko.observable(20)
    };

    // Data
    self.evaluations = ko.observableArray([]);
    self.projects = ko.observableArray([]);
    self.branches = ko.observableArray([]);
    self.evaluators = ko.observableArray([]);
    self.checklists = ko.observableArray([]);
    self.regions = ko.observableArray([]);
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
        var current = self.filter.page();
        var pages = [];

        var start = Math.max(1, current - 2);
        var end = Math.min(total, current + 2);

        for (var i = start; i <= end; i++) {
            pages.push(i);
        }

        return pages;
    });

    // Build filter DTO
    self.buildFilterDto = function() {
        return {
            projectId: self.filter.projectId() || null,
            branchId: self.filter.branchId() || null,
            evaluatorId: self.filter.evaluatorId() || null,
            checklistId: self.filter.checklistId() || null,
            region: self.filter.region() || null,
            status: self.filter.status() || null,
            startDate: self.filter.startDate() || null,
            endDate: self.filter.endDate() || null,
            page: self.filter.page(),
            pageSize: self.filter.pageSize()
        };
    };

    // Load evaluations
    self.loadEvaluations = function() {
        self.isLoading(true);
        self.errorMessage('');

        var filterDto = self.buildFilterDto();

        fetch('/api/reports/evaluations', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            credentials: 'include',
            body: JSON.stringify(filterDto)
        })
            .then(function(response) {
                if (!response.ok) throw new Error(T('Report.LoadError', 'Veriler yüklenemedi'));
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
                self.errorMessage(error.message || T('Report.LoadErrorMessage', 'Veriler yüklenirken bir hata oluştu.'));
            })
            .finally(function() {
                self.isLoading(false);
            });
    };

    // Load summary
    self.loadSummary = function() {
        var filterDto = self.buildFilterDto();

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

        // Branches modülü kaldırıldı - CustomerOrganizations kullanılıyor
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

    // Apply filters
    self.applyFilters = function() {
        self.filter.page(1);
        self.loadEvaluations();
        self.loadSummary();
    };

    // Clear filters
    self.clearFilters = function() {
        self.filter.projectId('');
        self.filter.branchId('');
        self.filter.evaluatorId('');
        self.filter.checklistId('');
        self.filter.region('');
        self.filter.status('');
        self.filter.startDate('');
        self.filter.endDate('');
        self.filter.page(1);
        self.loadEvaluations();
        self.loadSummary();
    };

    // Pagination
    self.previousPage = function() {
        if (self.paginationInfo().hasPreviousPage) {
            self.filter.page(self.filter.page() - 1);
            self.loadEvaluations();
        }
    };

    self.nextPage = function() {
        if (self.paginationInfo().hasNextPage) {
            self.filter.page(self.filter.page() + 1);
            self.loadEvaluations();
        }
    };

    self.goToPage = function(page) {
        self.filter.page(page);
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
                self.errorMessage(T('Report.DetailLoadError', 'Detay yüklenirken bir hata oluştu.'));
            });
    };

    // Export to Excel
    self.exportToExcel = function() {
        self.isExporting(true);

        var filterDto = self.buildFilterDto();
        filterDto.page = 1;
        filterDto.pageSize = 10000;

        fetch('/api/reports/export/excel', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            credentials: 'include',
            body: JSON.stringify(filterDto)
        })
            .then(function(response) {
                if (!response.ok) throw new Error(T('Report.ExportError', 'Export başarısız'));
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
                self.errorMessage(T('Report.ExcelDownloadError', 'Excel dosyası indirilemedi.'));
            })
            .finally(function() {
                self.isExporting(false);
            });
    };

    // Export detailed to Excel
    self.exportDetailedToExcel = function() {
        self.isExporting(true);

        var filterDto = self.buildFilterDto();

        fetch('/api/reports/export/excel/detailed', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            credentials: 'include',
            body: JSON.stringify(filterDto)
        })
            .then(function(response) {
                if (!response.ok) throw new Error(T('Report.ExportError', 'Export başarısız'));
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
                self.errorMessage(T('Report.DetailedExcelDownloadError', 'Detaylı Excel dosyası indirilemedi.'));
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
        switch (status) {
            case 'Completed': return T('Status.Completed', 'Tamamlandı');
            case 'Draft': return T('Status.Draft', 'Taslak');
            case 'InProgress': return T('Status.InProgress', 'Devam Ediyor');
            default: return status || T('Common.Unknown', 'Bilinmiyor');
        }
    };

    // Initialize
    self.loadFilterOptions();
    self.loadEvaluations();
    self.loadSummary();
}

// Apply bindings when document is ready
$(document).ready(function() {
    ko.applyBindings(new ReportsViewModel(), document.getElementById('reports-app'));
});
