// Listenings (Dinlemeler) Page ViewModel
function ListeningsViewModel() {
    var self = this;

    // State
    self.isLoading = ko.observable(false);
    self.isExporting = ko.observable(false);
    self.isLoadingDetail = ko.observable(false);
    self.evaluations = ko.observableArray([]);
    self.projects = ko.observableArray([]);
    self.totalCount = ko.observable(0);
    self.showDetailModal = ko.observable(false);
    self.selectedEvaluation = ko.observable(null);

    // Sorting
    self.sortField = ko.observable('callDate');
    self.sortDirection = ko.observable('desc');

    // Filter
    self.filter = {
        projectId: ko.observable(null),
        startDate: ko.observable(''),
        endDate: ko.observable(''),
        status: ko.observable(''),
        page: ko.observable(1),
        pageSize: ko.observable(25)
    };

    // Computed
    self.totalPages = ko.computed(function() {
        return Math.ceil(self.totalCount() / self.filter.pageSize()) || 1;
    });

    self.visiblePages = ko.computed(function() {
        var current = self.filter.page();
        var total = self.totalPages();
        var pages = [];
        var start = Math.max(1, current - 2);
        var end = Math.min(total, current + 2);

        for (var i = start; i <= end; i++) {
            pages.push(i);
        }
        return pages;
    });

    // Init
    self.init = function() {
        self.loadProjects();
        self.search();
    };

    // Load projects for filter dropdown
    self.loadProjects = function() {
        ApiService.get('/projects')
            .then(function(response) {
                self.projects(response || []);
            })
            .catch(function(error) {
                console.error('Error loading projects:', error);
            });
    };

    // Search evaluations
    self.search = function() {
        self.filter.page(1);
        self.loadEvaluations();
    };

    // Load evaluations
    self.loadEvaluations = function() {
        self.isLoading(true);

        var params = {
            page: self.filter.page(),
            pageSize: self.filter.pageSize(),
            sortField: self.sortField(),
            sortDirection: self.sortDirection()
        };

        if (self.filter.projectId()) {
            params.projectId = self.filter.projectId();
        }
        if (self.filter.startDate()) {
            params.startDate = self.filter.startDate();
        }
        if (self.filter.endDate()) {
            params.endDate = self.filter.endDate();
        }
        if (self.filter.status()) {
            params.status = self.filter.status();
        }

        ApiService.post('/reports/evaluations', params)
            .then(function(response) {
                self.evaluations(response.items || []);
                self.totalCount(response.totalCount || 0);
            })
            .catch(function(error) {
                console.error('Error loading evaluations:', error);
                toastr.error('Veriler yüklenirken hata oluştu');
            })
            .finally(function() {
                self.isLoading(false);
            });
    };

    // Clear filters
    self.clearFilters = function() {
        self.filter.projectId(null);
        self.filter.startDate('');
        self.filter.endDate('');
        self.filter.status('');
        self.search();
    };

    // Sorting
    self.toggleSort = function(field) {
        if (self.sortField() === field) {
            self.sortDirection(self.sortDirection() === 'asc' ? 'desc' : 'asc');
        } else {
            self.sortField(field);
            self.sortDirection('asc');
        }
        self.loadEvaluations();
    };

    self.getSortIcon = function(field) {
        if (self.sortField() !== field) return '';
        return self.sortDirection() === 'asc' ? 'bi-sort-up' : 'bi-sort-down';
    };

    // Pagination
    self.prevPage = function() {
        if (self.filter.page() > 1) {
            self.filter.page(self.filter.page() - 1);
            self.loadEvaluations();
        }
    };

    self.nextPage = function() {
        if (self.filter.page() < self.totalPages()) {
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
        self.selectedEvaluation(evaluation);
        self.showDetailModal(true);
        self.isLoadingDetail(true);

        ApiService.get('/reports/evaluations/' + evaluation.evaluationId)
            .then(function(response) {
                self.selectedEvaluation(response);
            })
            .catch(function(error) {
                console.error('Error loading evaluation detail:', error);
                toastr.error('Detay yüklenirken hata oluştu');
            })
            .finally(function() {
                self.isLoadingDetail(false);
            });
    };

    self.closeDetailModal = function() {
        self.showDetailModal(false);
        self.selectedEvaluation(null);
    };

    // Export to Excel
    self.exportToExcel = function() {
        self.isExporting(true);

        var params = {};
        if (self.filter.projectId()) {
            params.projectId = self.filter.projectId();
        }
        if (self.filter.startDate()) {
            params.startDate = self.filter.startDate();
        }
        if (self.filter.endDate()) {
            params.endDate = self.filter.endDate();
        }
        if (self.filter.status()) {
            params.status = self.filter.status();
        }

        ApiService.downloadPost('/reports/export/excel', params, 'Dinlemeler.xlsx')
            .then(function() {
                toastr.success('Excel dosyası indirildi');
            })
            .catch(function(error) {
                console.error('Error exporting:', error);
                toastr.error('Excel export sırasında hata oluştu');
            })
            .finally(function() {
                self.isExporting(false);
            });
    };

    // Helpers
    self.formatDate = function(dateStr) {
        if (!dateStr) return '-';
        var date = new Date(dateStr);
        return date.toLocaleDateString('tr-TR');
    };

    self.formatTime = function(timeStr) {
        if (!timeStr) return '';
        // timeStr could be "14:30:00" or "14:30"
        if (typeof timeStr === 'string') {
            return timeStr.substring(0, 5);
        }
        return '';
    };

    self.getScoreClass = function(score) {
        if (score === null || score === undefined) return 'bg-secondary';
        if (score >= 90) return 'bg-success';
        if (score >= 70) return 'bg-warning text-dark';
        return 'bg-danger';
    };

    self.getStatusText = function(status) {
        var statusTexts = {
            'Completed': 'Tamamlandı',
            'InProgress': 'Devam Ediyor',
            'Draft': 'Taslak',
            'Pending': 'Bekliyor',
            'Cancelled': 'İptal'
        };
        return statusTexts[status] || status;
    };

    self.getStatusClass = function(status) {
        var statusClasses = {
            'Completed': 'bg-success',
            'InProgress': 'bg-info',
            'Draft': 'bg-secondary',
            'Pending': 'bg-warning text-dark',
            'Cancelled': 'bg-danger'
        };
        return statusClasses[status] || 'bg-secondary';
    };

    // Initialize
    self.init();
}

// Apply bindings when DOM is ready
$(document).ready(function() {
    if (document.getElementById('listenings-app')) {
        ko.applyBindings(new ListeningsViewModel(), document.getElementById('listenings-app'));
    }
});
