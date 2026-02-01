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

    // Modal state - KnockoutJS pattern
    self.isNewVisitModalOpen = ko.observable(false);
    self.isDetailModalOpen = ko.observable(false);
    self.selectedVisit = ko.observable(null);
    self.visitEvaluation = ko.observable(null);

    // New visit form
    self.newVisit = {
        projectId: ko.observable(''),
        dealerId: ko.observable(''),
        controlDate: ko.observable(''),
        controlTime: ko.observable(''),
        auditorComment: ko.observable('')
    };

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

    self.formatDateTime = function(dateStr) {
        if (!dateStr) return '-';
        var date = new Date(dateStr);
        return date.toLocaleDateString('tr-TR') + ' ' + date.toLocaleTimeString('tr-TR', { hour: '2-digit', minute: '2-digit' });
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

        var params = new URLSearchParams();
        params.append('page', self.currentPage());
        params.append('pageSize', self.pageSize);

        if (self.filter.searchTerm()) params.append('searchTerm', self.filter.searchTerm());
        if (self.filter.projectId()) params.append('projectId', self.filter.projectId());
        if (self.filter.statusId()) params.append('statusId', self.filter.statusId());

        ApiService.get('/fieldworker/visits?' + params.toString())
            .then(function(data) {
                self.visits(data.items || []);
                self.totalCount(data.totalCount || 0);
            })
            .catch(function(error) {
                console.error('Error loading visits:', error);
                toastr.error(error.message || T('FieldWorker.LoadVisitsError', 'Ziyaretler yuklenirken hata olustu.'));
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

    // ==================== NEW VISIT MODAL ====================
    self.showNewVisitModal = function() {
        self.newVisit.projectId('');
        self.newVisit.dealerId('');
        self.newVisit.controlDate(new Date().toISOString().split('T')[0]);
        self.newVisit.controlTime('');
        self.newVisit.auditorComment('');
        self.isNewVisitModalOpen(true);
    };

    self.closeNewVisitModal = function() {
        self.isNewVisitModalOpen(false);
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
            toastr.warning(T('FieldWorker.ProjectRequired', 'Proje secimi zorunludur.'));
            return;
        }
        if (!self.newVisit.dealerId()) {
            toastr.warning(T('FieldWorker.DealerRequired', 'Bayi secimi zorunludur.'));
            return;
        }

        self.isSaving(true);

        var data = {
            projectId: parseInt(self.newVisit.projectId()),
            dealerId: parseInt(self.newVisit.dealerId()),
            checklistId: 0,
            controlDate: self.newVisit.controlDate() ? new Date(self.newVisit.controlDate()).toISOString() : null,
            controlTime: self.newVisit.controlTime() || null,
            auditorComment: self.newVisit.auditorComment() || null,
            isDraft: isDraft,
            answers: []
        };

        ApiService.post('/fieldworker/visits', data)
            .then(function(response) {
                var message = isDraft
                    ? T('FieldWorker.VisitSavedAsDraft', 'Ziyaret taslak olarak kaydedildi.')
                    : T('FieldWorker.VisitSaved', 'Ziyaret basariyla kaydedildi.');
                toastr.success(message);
                self.closeNewVisitModal();
                self.loadVisits();

                // Show detail modal for the new visit
                if (response.evaluationId) {
                    self.showVisitDetail({ evaluationId: response.evaluationId });
                }
            })
            .catch(function(error) {
                console.error('Error saving visit:', error);
                toastr.error(error.message || T('FieldWorker.SaveVisitError', 'Ziyaret kaydedilirken hata olustu.'));
            })
            .finally(function() {
                self.isSaving(false);
            });
    };

    // ==================== DETAIL MODAL ====================
    self.showVisitDetail = function(visit) {
        self.selectedVisit(visit);
        self.visitEvaluation(null);
        self.isDetailModalOpen(true);

        // Load evaluation details
        if (visit.evaluationId) {
            ApiService.get('/evaluations/' + visit.evaluationId)
                .then(function(evaluation) {
                    self.visitEvaluation(evaluation);
                })
                .catch(function(error) {
                    console.error('Error loading evaluation:', error);
                });
        }
    };

    self.closeDetailModal = function() {
        self.isDetailModalOpen(false);
        self.selectedVisit(null);
        self.visitEvaluation(null);
    };

    // Edit visit - Open evaluation form in new window
    self.editVisit = function(visit) {
        if (visit.evaluationId) {
            window.open('/Evaluations?id=' + visit.evaluationId, '_blank');
        }
    };

    // Continue evaluation from detail modal
    self.continueEvaluation = function() {
        var visit = self.selectedVisit();
        if (visit && visit.evaluationId) {
            window.open('/Evaluations?id=' + visit.evaluationId, '_blank');
            self.closeDetailModal();
        }
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

// Translation keys used in this module
var TRANSLATION_KEYS = [
    'FieldWorker.LoadVisitsError',
    'FieldWorker.ProjectRequired',
    'FieldWorker.DealerRequired',
    'FieldWorker.VisitSavedAsDraft',
    'FieldWorker.VisitSaved',
    'FieldWorker.SaveVisitError',
    'Common.Confirm',
    'Common.Cancel'
];

// Initialize when document is ready
$(document).ready(function() {
    var app = document.getElementById('fieldworker-visits-app');
    if (app) {
        Localization.loadKeys(TRANSLATION_KEYS).then(function() {
            ko.applyBindings(new FieldWorkerVisitsViewModel(), app);
        });
    }
});
