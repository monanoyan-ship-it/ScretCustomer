// Survey Results Report ViewModel
function SurveyResultsViewModel() {
    var self = this;

    // State
    self.isLoading = ko.observable(true);
    self.isLoadingPopup = ko.observable(false);
    self.isLoadingResponseDetail = ko.observable(false);

    // Data
    self.recentResponses = ko.observableArray([]);
    self.surveyProjects = ko.observableArray([]);
    self.allResponses = ko.observableArray([]);
    self.projectDetail = ko.observable(null);
    self.responseDetail = ko.observable(null);

    // Popup Filters
    self.popupFilter = {
        projectId: ko.observable(null),
        startDate: ko.observable(''),
        endDate: ko.observable('')
    };

    // Modals
    self.allResponsesModal = null;
    self.projectDetailModal = null;
    self.responseDetailModal = null;

    // Initialize modals
    self.initModals = function() {
        self.allResponsesModal = new bootstrap.Modal(document.getElementById('allResponsesModal'));
        self.projectDetailModal = new bootstrap.Modal(document.getElementById('projectDetailModal'));
        self.responseDetailModal = new bootstrap.Modal(document.getElementById('responseDetailModal'));
    };

    // Load initial data
    self.loadData = function() {
        self.isLoading(true);

        // Load both recent responses and projects in parallel
        Promise.all([
            apiService.get('/reports/survey-responses/recent?count=10'),
            apiService.get('/reports/survey-projects')
        ])
        .then(function(results) {
            self.recentResponses(results[0] || []);
            self.surveyProjects(results[1] || []);
        })
        .catch(function(error) {
            console.error('Error loading survey data:', error);
            toastr.error('Veriler yüklenirken hata oluştu.');
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

        var params = ['count=100']; // Max 100 for popup
        if (self.popupFilter.projectId()) {
            params.push('projectId=' + self.popupFilter.projectId());
        }
        if (self.popupFilter.startDate()) {
            params.push('startDate=' + self.popupFilter.startDate());
        }
        if (self.popupFilter.endDate()) {
            params.push('endDate=' + self.popupFilter.endDate());
        }

        apiService.get('/reports/survey-responses/recent?' + params.join('&'))
            .then(function(data) {
                self.allResponses(data || []);
            })
            .catch(function(error) {
                console.error('Error loading all responses:', error);
                toastr.error('Yanıtlar yüklenirken hata oluştu.');
            })
            .finally(function() {
                self.isLoadingPopup(false);
            });
    };

    // Show Project Detail Modal
    self.showProjectDetail = function(project) {
        self.projectDetail(null);
        self.projectDetailModal.show();

        apiService.get('/reports/survey-projects/' + project.projectId + '/detail')
            .then(function(data) {
                self.projectDetail(data);
            })
            .catch(function(error) {
                console.error('Error loading project detail:', error);
                toastr.error('Proje detayı yüklenirken hata oluştu.');
            });
    };

    // Show Response Detail Modal
    self.showResponseDetail = function(response) {
        self.responseDetail(null);
        self.isLoadingResponseDetail(true);
        self.responseDetailModal.show();

        apiService.get('/reports/evaluations/' + response.evaluationId)
            .then(function(data) {
                // Transform data for display
                var detail = {
                    respondentName: response.respondentName,
                    respondentEmail: response.respondentEmail,
                    projectName: response.projectName,
                    score: response.score,
                    completedAt: response.completedAt,
                    answers: (data.questionAnswers || []).map(function(qa) {
                        return {
                            questionText: qa.questionText,
                            groupName: qa.groupName,
                            score: qa.score,
                            maxPoints: qa.maxPoints,
                            selectedSubCriteria: qa.selectedSubCriteria || [],
                            comment: qa.comment
                        };
                    })
                };
                self.responseDetail(detail);
            })
            .catch(function(error) {
                console.error('Error loading response detail:', error);
                toastr.error('Yanıt detayı yüklenirken hata oluştu.');
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
        window.location.href = '/api/reports/survey-results/' + projectId + '/export/group-scores';
    };

    // Export Question Statistics Report
    self.exportQuestionStats = function() {
        if (!self.projectDetail()) return;
        var projectId = self.projectDetail().projectId;
        window.location.href = '/api/reports/survey-results/' + projectId + '/export/question-stats';
    };

    // Export Detail Report (scores + selections)
    self.exportDetailReport = function() {
        if (!self.projectDetail()) return;
        var projectId = self.projectDetail().projectId;
        window.location.href = '/api/reports/survey-results/' + projectId + '/export/detail';
    };

    // Export Full Detail Report (scores + selections + comments)
    self.exportFullDetailReport = function() {
        if (!self.projectDetail()) return;
        var projectId = self.projectDetail().projectId;
        window.location.href = '/api/reports/survey-results/' + projectId + '/export/full-detail';
    };

    // Initialize
    self.init = function() {
        self.initModals();
        self.loadData();
    };

    self.init();
}

// Apply bindings
$(document).ready(function() {
    var app = document.getElementById('survey-results-app');
    if (app) {
        ko.applyBindings(new SurveyResultsViewModel(), app);
    }
});
