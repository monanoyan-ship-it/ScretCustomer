// CustomerPortal Survey Results ViewModel
function CustomerSurveyResultsViewModel() {
    var self = this;

    // State
    self.isLoading = ko.observable(true);
    self.isLoadingPopup = ko.observable(false);
    self.isLoadingResponseDetail = ko.observable(false);
    self.isLoadingDistribution = ko.observable(false);
    self.isLoadingScoreDetail = ko.observable(false);

    // Data
    self.recentResponses = ko.observableArray([]);
    self.surveyProjects = ko.observableArray([]);
    self.allResponses = ko.observableArray([]);
    self.projectDetail = ko.observable(null);
    self.responseDetail = ko.observable(null);
    self.questionDistribution = ko.observable(null);
    self.scoreDetail = ko.observable(null);

    // Project Search
    self.projectSearchTerm = ko.observable('');
    self.filteredProjects = ko.computed(function() {
        var term = (self.projectSearchTerm() || '').toLowerCase().trim();
        var projects = self.surveyProjects();
        if (!term) return projects;
        return projects.filter(function(p) {
            return (p.projectName || '').toLowerCase().indexOf(term) !== -1;
        });
    });

    // Popup Filters
    self.popupFilter = {
        projectId: ko.observable(null),
        startDate: ko.observable(''),
        endDate: ko.observable('')
    };

    // Question Distribution Filters (sadece proje filtresi)
    self.distributionFilter = {
        projectId: ko.observable(null)
    };

    // Modals
    self.allResponsesModal = null;
    self.projectDetailModal = null;
    self.responseDetailModal = null;
    self.scoreDetailModal = null;

    // Initialize modals
    self.initModals = function() {
        self.allResponsesModal = new bootstrap.Modal(document.getElementById('allResponsesModal'));
        self.projectDetailModal = new bootstrap.Modal(document.getElementById('projectDetailModal'));
        self.responseDetailModal = new bootstrap.Modal(document.getElementById('responseDetailModal'));
        self.scoreDetailModal = new bootstrap.Modal(document.getElementById('scoreDetailModal'));
    };

    // Load initial data
    self.loadData = function() {
        self.isLoading(true);

        // Load both recent responses and projects in parallel
        Promise.all([
            customerApiFetch('/api/customer/portal/reports/survey-responses/recent?count=10').then(function(r) { return r.json(); }),
            customerApiFetch('/api/customer/portal/reports/survey-projects').then(function(r) { return r.json(); })
        ])
        .then(function(results) {
            self.recentResponses(results[0] || []);
            self.surveyProjects(results[1] || []);
        })
        .catch(function(error) {
            console.error('Error loading survey data:', error);
            toastr.error('Veriler yuklenirken hata olustu.');
        })
        .finally(function() {
            self.isLoading(false);
        });
    };

    // Show All Responses Popup
    self.showAllResponsesPopup = function() {
        self.popupFilter.projectId(null);
        self.popupFilter.startDate('');
        self.popupFilter.endDate('');
        self.loadAllResponses();
        self.allResponsesModal.show();
    };

    // Load All Responses (with filter)
    self.loadAllResponses = function() {
        self.isLoadingPopup(true);
        self.allResponses([]);

        var params = ['count=100'];
        if (self.popupFilter.projectId()) {
            params.push('projectId=' + self.popupFilter.projectId());
        }
        if (self.popupFilter.startDate()) {
            params.push('startDate=' + self.popupFilter.startDate());
        }
        if (self.popupFilter.endDate()) {
            params.push('endDate=' + self.popupFilter.endDate());
        }

        customerApiFetch('/api/customer/portal/reports/survey-responses/recent?' + params.join('&'))
            .then(function(r) { return r.json(); })
            .then(function(data) {
                self.allResponses(data || []);
            })
            .catch(function(error) {
                console.error('Error loading all responses:', error);
                toastr.error('Yanitlar yuklenirken hata olustu.');
            })
            .finally(function() {
                self.isLoadingPopup(false);
            });
    };

    // Show Project Detail Modal
    self.showProjectDetail = function(project) {
        self.projectDetail(null);
        self.projectDetailModal.show();

        customerApiFetch('/api/customer/portal/reports/survey-projects/' + project.projectId + '/detail')
            .then(function(r) { return r.json(); })
            .then(function(data) {
                self.projectDetail(data);
            })
            .catch(function(error) {
                console.error('Error loading project detail:', error);
                toastr.error('Proje detayi yuklenirken hata olustu.');
            });
    };

    // Show Response Detail Modal
    self.showResponseDetail = function(response) {
        self.responseDetail(null);
        self.isLoadingResponseDetail(true);
        self.responseDetailModal.show();

        customerApiFetch('/api/customer/portal/evaluations/' + response.evaluationId)
            .then(function(r) { return r.json(); })
            .then(function(data) {
                var detail = {
                    respondentName: response.respondentName,
                    respondentEmail: response.respondentEmail,
                    projectName: response.projectName,
                    score: response.score,
                    completedAt: response.completedAt,
                    answers: (data.answers || []).map(function(qa) {
                        return {
                            questionText: qa.questionText,
                            groupName: qa.groupName,
                            score: qa.earnedPoints || qa.givenPoints,
                            maxPoints: qa.weightPoints || qa.maxPoints,
                            selectedSubCriteria: qa.selectedSubCriteria || [],
                            comment: qa.notes
                        };
                    })
                };
                self.responseDetail(detail);
            })
            .catch(function(error) {
                console.error('Error loading response detail:', error);
                toastr.error('Yanit detayi yuklenirken hata olustu.');
            })
            .finally(function() {
                self.isLoadingResponseDetail(false);
            });
    };

    // Show Response Detail from Project Modal
    self.showResponseDetailFromProject = function(respondent) {
        var response = {
            evaluationId: respondent.evaluationId,
            respondentName: respondent.fullName,
            respondentEmail: respondent.email,
            projectName: self.projectDetail() ? self.projectDetail().projectName : '',
            score: respondent.score,
            completedAt: respondent.completedAt
        };
        self.showResponseDetail(response);
    };

    // Export Group Scores Report
    self.exportGroupScores = function() {
        if (!self.projectDetail()) return;
        var projectId = self.projectDetail().projectId;
        window.location.href = '/api/customer/portal/reports/survey-results/' + projectId + '/export/group-scores';
    };

    // Export Question Statistics Report
    self.exportQuestionStats = function() {
        if (!self.projectDetail()) return;
        var projectId = self.projectDetail().projectId;
        window.location.href = '/api/customer/portal/reports/survey-results/' + projectId + '/export/question-stats';
    };

    // Export Detail Report
    self.exportDetailReport = function() {
        if (!self.projectDetail()) return;
        var projectId = self.projectDetail().projectId;
        window.location.href = '/api/customer/portal/reports/survey-results/' + projectId + '/export/detail';
    };

    // Export Full Detail Report
    self.exportFullDetailReport = function() {
        if (!self.projectDetail()) return;
        var projectId = self.projectDetail().projectId;
        window.location.href = '/api/customer/portal/reports/survey-results/' + projectId + '/export/full-detail';
    };

    // Export Project Report (direct from table row)
    self.exportProjectReport = function(projectId, reportType) {
        var urlMap = {
            'group': '/api/customer/portal/reports/survey-results/' + projectId + '/export/group-scores',
            'question': '/api/customer/portal/reports/survey-results/' + projectId + '/export/question-stats',
            'detail': '/api/customer/portal/reports/survey-results/' + projectId + '/export/detail',
            'full': '/api/customer/portal/reports/survey-results/' + projectId + '/export/full-detail'
        };
        var url = urlMap[reportType];
        if (url) {
            window.location.href = url;
        }
    };

    // Show Score Detail Modal
    self.showScoreDetail = function(project) {
        self.scoreDetail(null);
        self.isLoadingScoreDetail(true);
        self.scoreDetailModal.show();

        customerApiFetch('/api/customer/portal/reports/survey-projects/' + project.projectId + '/score-detail')
            .then(function(r) { return r.json(); })
            .then(function(data) {
                self.scoreDetail(data);
            })
            .catch(function(error) {
                console.error('Error loading score detail:', error);
                toastr.error('Puan detayi yuklenirken hata olustu.');
            })
            .finally(function() {
                self.isLoadingScoreDetail(false);
            });
    };

    // Export Score Detail Report
    self.exportScoreDetail = function() {
        if (!self.scoreDetail()) return;
        var projectId = self.scoreDetail().projectId;
        window.location.href = '/api/customer/portal/reports/survey-results/' + projectId + '/export/score-detail';
    };

    // Load Question Score Distribution
    self.loadQuestionDistribution = function() {
        // Proje seçilmeden yükleme yapma
        if (!self.distributionFilter.projectId()) {
            toastr.warning('Lütfen bir proje seçin.');
            return;
        }

        self.isLoadingDistribution(true);
        self.questionDistribution(null);

        var url = '/api/customer/portal/reports/survey-question-distribution?projectId=' + self.distributionFilter.projectId();

        customerApiFetch(url)
            .then(function(r) { return r.json(); })
            .then(function(data) {
                self.questionDistribution(data);
            })
            .catch(function(error) {
                console.error('Error loading question distribution:', error);
                toastr.error('Soru puan dagilimi yuklenirken hata olustu.');
            })
            .finally(function() {
                self.isLoadingDistribution(false);
            });
    };

    // Initialize
    self.init = function() {
        self.initModals();
        self.loadData();
        // Proje seçilmeden soru dağılımı yüklenmesin
    };

    self.init();
}

// Apply bindings
$(document).ready(function() {
    var app = document.getElementById('survey-results-app');
    if (app) {
        ko.applyBindings(new CustomerSurveyResultsViewModel(), app);
    }
});
