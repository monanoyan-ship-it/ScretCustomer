// CustomerPortal Survey Results ViewModel
function CustomerSurveyResultsViewModel() {
    var self = this;

    // State
    self.isLoading = ko.observable(true);
    self.isLoadingPopup = ko.observable(false);
    self.isLoadingResponseDetail = ko.observable(false);
    self.isLoadingDistribution = ko.observable(false);
    self.isExportingDistribution = ko.observable(false);
    self.isExportingAllResponses = ko.observable(false);

    // Data
    self.recentResponses = ko.observableArray([]);
    self.surveyProjects = ko.observableArray([]);
    self.allResponses = ko.observableArray([]);
    self.projectDetail = ko.observable(null);
    self.responseDetail = ko.observable(null);
    self.questionDistribution = ko.observable(null);
    self.scoreDetail = ko.observable(null);
    self.isLoadingScoreDetail = ko.observable(false);

    // Flat cevap dagilimi tablosu icin (Soru | Cevap | Secim | Toplam | Oran)
    self.answerDistributionFlat = ko.observableArray([]);
    self.isLoadingAnswerDistribution = ko.observable(false);

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

    // Popup Filters (CP advantage: date filters)
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

    // Load All Responses (with filter - CP advantage: date filters)
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

    // Export All Responses to Excel
    self.exportAllResponses = function() {
        if (!self.popupFilter.projectId()) {
            toastr.warning('Lutfen bir proje secin.');
            return;
        }

        self.isExportingAllResponses(true);
        var url = '/api/customer/portal/reports/survey-responses/export?projectId=' + self.popupFilter.projectId();
        window.location.href = url;

        setTimeout(function() {
            self.isExportingAllResponses(false);
        }, 1000);
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

    // Show Response Detail Modal (CP endpoint + field mapping)
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

    // Export Detail Report (scores + selections)
    self.exportDetailReport = function() {
        if (!self.projectDetail()) return;
        var projectId = self.projectDetail().projectId;
        window.location.href = '/api/customer/portal/reports/survey-results/' + projectId + '/export/detail';
    };

    // Export Full Detail Report (scores + selections + comments)
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

    // Load Question Score Distribution (parallel: question-distribution + score-detail)
    self.loadQuestionDistribution = function() {
        // Proje secilmeden yukleme yapma
        if (!self.distributionFilter.projectId()) {
            toastr.warning('Lutfen bir proje secin.');
            return;
        }

        self.isLoadingDistribution(true);
        self.isLoadingAnswerDistribution(true);
        self.questionDistribution(null);
        self.answerDistributionFlat([]);

        var projectId = self.distributionFilter.projectId();

        // Hem soru dagilimi hem de cevap dagilimi yukle
        Promise.all([
            customerApiFetch('/api/customer/portal/reports/survey-question-distribution?projectId=' + projectId).then(function(r) { return r.json(); }),
            customerApiFetch('/api/customer/portal/reports/survey-projects/' + projectId + '/score-detail').then(function(r) { return r.json(); })
        ])
        .then(function(results) {
            self.questionDistribution(results[0]);

            // scoreDetail verisini flat tabloya donustur
            var scoreData = results[1];
            if (scoreData && scoreData.questions) {
                var flatData = [];
                scoreData.questions.forEach(function(q) {
                    if (q.answerDistributions && q.answerDistributions.length > 0) {
                        q.answerDistributions.forEach(function(ans) {
                            flatData.push({
                                questionText: q.questionText,
                                groupName: q.groupName,
                                answerText: ans.answerText,
                                selectionCount: ans.selectionCount,
                                totalCount: q.responseCount,
                                percentage: ans.percentage
                            });
                        });
                    }
                });
                self.answerDistributionFlat(flatData);
            }
        })
        .catch(function(error) {
            console.error('Error loading question distribution:', error);
            toastr.error('Soru puan dagilimi yuklenirken hata olustu.');
        })
        .finally(function() {
            self.isLoadingDistribution(false);
            self.isLoadingAnswerDistribution(false);
        });
    };

    // Export Question Distribution to Excel
    self.exportQuestionDistribution = function() {
        if (!self.distributionFilter.projectId()) {
            toastr.warning('Lutfen bir proje secin.');
            return;
        }

        self.isExportingDistribution(true);
        window.location.href = '/api/customer/portal/reports/survey-question-distribution/export?projectId=' + self.distributionFilter.projectId();

        // Reset loading state after a short delay
        setTimeout(function() {
            self.isExportingDistribution(false);
        }, 1000);
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
        ko.applyBindings(new CustomerSurveyResultsViewModel(), app);
    }
});
