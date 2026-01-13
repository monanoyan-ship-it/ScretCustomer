// FieldWorker Visits ViewModel
function FieldWorkerVisitsViewModel() {
    var self = this;

    // State
    self.isLoading = ko.observable(true);
    self.isSaving = ko.observable(false);

    // Filter
    self.filter = {
        searchTerm: ko.observable(''),
        projectId: ko.observable(''),
        statusId: ko.observable('')
    };

    // Pagination
    self.currentPage = ko.observable(1);
    self.pageSize = 20;
    self.totalCount = ko.observable(0);
    self.totalPages = ko.computed(function() {
        return Math.ceil(self.totalCount() / self.pageSize) || 1;
    });

    // Data
    self.visits = ko.observableArray([]);
    self.projects = ko.observableArray([]);
    self.dealers = ko.observableArray([]);

    // New visit modal
    self.newVisit = {
        projectId: ko.observable(''),
        dealerId: ko.observable(''),
        controlDate: ko.observable(''),
        controlTime: ko.observable(''),
        auditorComment: ko.observable('')
    };

    self.modal = null;

    // Filtered dealers based on selected project
    self.filteredDealers = ko.computed(function() {
        var projectId = self.newVisit.projectId();
        if (!projectId) return [];

        var project = self.projects().find(function(p) { return p.projectId === parseInt(projectId); });
        if (!project) return [];

        return self.dealers().filter(function(d) {
            return d.customerId === project.customerId;
        });
    });

    // Status helpers
    self.getStatusBadgeClass = function(statusId) {
        var classes = {
            1: 'bg-warning text-dark',
            2: 'bg-success',
            3: 'bg-info',
            4: 'bg-danger'
        };
        return classes[statusId] || 'bg-secondary';
    };

    self.formatDate = function(dateStr) {
        if (!dateStr) return '-';
        var date = new Date(dateStr);
        return date.toLocaleDateString('tr-TR');
    };

    // Load projects
    self.loadProjects = function() {
        return ApiService.get('/fieldworker/projects')
            .then(function(data) {
                self.projects(data || []);
            });
    };

    // Load dealers
    self.loadDealers = function() {
        return ApiService.get('/fieldworker/dealers')
            .then(function(data) {
                self.dealers(data || []);
            });
    };

    // Load visits
    self.loadVisits = function() {
        self.isLoading(true);

        var params = {
            page: self.currentPage(),
            pageSize: self.pageSize
        };

        if (self.filter.searchTerm()) params.searchTerm = self.filter.searchTerm();
        if (self.filter.projectId()) params.projectId = self.filter.projectId();
        if (self.filter.statusId()) params.statusId = self.filter.statusId();

        var queryString = Object.keys(params).map(function(key) {
            return encodeURIComponent(key) + '=' + encodeURIComponent(params[key]);
        }).join('&');

        ApiService.get('/fieldworker/visits?' + queryString)
            .then(function(data) {
                self.visits(data.items || []);
                self.totalCount(data.totalCount || 0);
            })
            .catch(function(error) {
                console.error('Error loading visits:', error);
                toastr.error(error.message || 'Ziyaretler yuklenirken hata olustu.');
            })
            .finally(function() {
                self.isLoading(false);
            });
    };

    // Apply filters
    self.applyFilters = function() {
        self.currentPage(1);
        self.loadVisits();
    };

    // Clear filters
    self.clearFilters = function() {
        self.filter.searchTerm('');
        self.filter.projectId('');
        self.filter.statusId('');
        self.currentPage(1);
        self.loadVisits();
    };

    // Pagination
    self.prevPage = function() {
        if (self.currentPage() > 1) {
            self.currentPage(self.currentPage() - 1);
            self.loadVisits();
        }
    };

    self.nextPage = function() {
        if (self.currentPage() < self.totalPages()) {
            self.currentPage(self.currentPage() + 1);
            self.loadVisits();
        }
    };

    // Show new visit modal
    self.showNewVisitModal = function() {
        self.newVisit.projectId('');
        self.newVisit.dealerId('');
        self.newVisit.controlDate(new Date().toISOString().split('T')[0]);
        self.newVisit.controlTime('');
        self.newVisit.auditorComment('');

        if (!self.modal) {
            var modalEl = document.getElementById('newVisitModal');
            if (modalEl) {
                self.modal = new bootstrap.Modal(modalEl);
            }
        }
        if (self.modal) {
            self.modal.show();
        }
    };

    // Save visit
    self.saveVisit = function() {
        self.doSaveVisit(false);
    };

    // Save as draft
    self.saveAsDraft = function() {
        self.doSaveVisit(true);
    };

    self.doSaveVisit = function(isDraft) {
        // Validation
        if (!self.newVisit.projectId()) {
            toastr.warning('Proje secimi zorunludur.');
            return;
        }
        if (!self.newVisit.dealerId()) {
            toastr.warning('Bayi secimi zorunludur.');
            return;
        }

        self.isSaving(true);

        // Find checklist from project's first assignment
        var project = self.projects().find(function(p) { return p.projectId === parseInt(self.newVisit.projectId()); });

        var data = {
            projectId: parseInt(self.newVisit.projectId()),
            dealerId: parseInt(self.newVisit.dealerId()),
            checklistId: 0, // Will be determined by backend
            controlDate: self.newVisit.controlDate() ? new Date(self.newVisit.controlDate()).toISOString() : null,
            controlTime: self.newVisit.controlTime() || null,
            auditorComment: self.newVisit.auditorComment() || null,
            isDraft: isDraft,
            answers: []
        };

        ApiService.post('/fieldworker/visits', data)
            .then(function(response) {
                toastr.success(isDraft ? 'Ziyaret taslak olarak kaydedildi.' : 'Ziyaret basariyla kaydedildi.');
                if (self.modal) {
                    self.modal.hide();
                }
                self.loadVisits();

                // Redirect to evaluation form
                if (response.evaluationId) {
                    setTimeout(function() {
                        window.location.href = '/Evaluations?id=' + response.evaluationId;
                    }, 1000);
                }
            })
            .catch(function(error) {
                console.error('Error saving visit:', error);
                toastr.error(error.message || 'Ziyaret kaydedilirken hata olustu.');
            })
            .finally(function() {
                self.isSaving(false);
            });
    };

    // Edit visit (redirect to evaluation)
    self.editVisit = function(visit) {
        window.location.href = '/Evaluations?id=' + visit.evaluationId;
    };

    // Initialize
    self.init = function() {
        // Check URL params
        var urlParams = new URLSearchParams(window.location.search);
        var status = urlParams.get('status');
        if (status) {
            self.filter.statusId(status);
        }

        // Load data
        Promise.all([
            self.loadProjects(),
            self.loadDealers()
        ]).then(function() {
            self.loadVisits();
        });
    };

    self.init();
}

// Initialize when document is ready
$(document).ready(function() {
    var app = document.getElementById('fieldworker-visits-app');
    if (app) {
        ko.applyBindings(new FieldWorkerVisitsViewModel(), app);
    }
});
