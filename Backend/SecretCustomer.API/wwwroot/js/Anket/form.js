// Online Anket Form ViewModel
function SurveyFormViewModel() {
    var self = this;

    // State
    self.isLoading = ko.observable(true);
    self.hasError = ko.observable(false);
    self.errorTitle = ko.observable('');
    self.errorMessage = ko.observable('');
    self.isSubmitting = ko.observable(false);

    // Survey Data
    self.token = ko.observable(SURVEY_TOKEN);
    self.surveyName = ko.observable('');
    self.customerName = ko.observable('');
    self.organizationName = ko.observable('');
    self.isAnonymous = ko.observable(true);
    self.personnelName = ko.observable('');
    self.personnelId = ko.observable(null);
    self.projectId = ko.observable(null);
    self.checklistId = ko.observable(null);

    // Questions
    self.questions = ko.observableArray([]);
    self.groupedQuestions = ko.observableArray([]);

    // Computed
    self.totalQuestions = ko.computed(function() {
        return self.questions().length;
    });

    self.answeredCount = ko.computed(function() {
        return self.questions().filter(function(q) {
            return q.selectedScore() !== null;
        }).length;
    });

    self.progressPercent = ko.computed(function() {
        var total = self.totalQuestions();
        if (total === 0) return 0;
        return Math.round((self.answeredCount() / total) * 100);
    });

    self.canSubmit = ko.computed(function() {
        // Zorunlu soruların hepsi cevaplandı mı?
        var requiredQuestions = self.questions().filter(function(q) {
            return q.isRequired;
        });
        return requiredQuestions.every(function(q) {
            return q.selectedScore() !== null;
        });
    });

    // Load Survey
    self.loadSurvey = function() {
        self.isLoading(true);
        self.hasError(false);

        fetch('/api/surveys/validate/' + self.token())
            .then(function(response) {
                if (!response.ok) {
                    return response.json().then(function(data) {
                        throw new Error(data.message || 'Anket yüklenemedi.');
                    });
                }
                return response.json();
            })
            .then(function(data) {
                var inv = data.invitation;
                self.projectId(inv.projectId);
                self.checklistId(inv.checklistId);
                self.surveyName(inv.checklistName || inv.projectName);
                self.customerName(inv.customerName || '');
                self.organizationName(inv.organizationName || '');
                self.isAnonymous(inv.isAnonymous);

                if (inv.personnel) {
                    self.personnelId(inv.personnel.id);
                    self.personnelName(inv.personnel.fullName);
                }

                // Load checklist questions
                self.loadQuestions(inv.checklistId);
            })
            .catch(function(error) {
                self.hasError(true);
                self.errorTitle('Anket Yüklenemedi');
                self.errorMessage(error.message);
                self.isLoading(false);
            });
    };

    // Load Questions
    self.loadQuestions = function(checklistId) {
        fetch('/api/checklists/' + checklistId)
            .then(function(response) {
                if (!response.ok) {
                    throw new Error('Sorular yüklenemedi.');
                }
                return response.json();
            })
            .then(function(data) {
                var questions = (data.questions || []).map(function(q, index) {
                    var maxPoints = q.maxPoints || 5;
                    var ratingOptions = [];
                    for (var i = 1; i <= maxPoints; i++) {
                        ratingOptions.push(i);
                    }

                    return {
                        id: q.id,
                        order: index + 1,
                        text: q.text,
                        groupName: q.groupName || null,
                        helpText: q.helpText || null,
                        isRequired: q.isRequired,
                        maxPoints: maxPoints,
                        weightPoints: q.weightPoints || 1,
                        scoringType: q.scoringType || 'Scored',
                        ratingOptions: ratingOptions,
                        selectedScore: ko.observable(null),
                        comment: ko.observable('')
                    };
                });

                self.questions(questions);
                self.groupQuestions(questions);
                self.isLoading(false);
            })
            .catch(function(error) {
                self.hasError(true);
                self.errorTitle('Sorular Yüklenemedi');
                self.errorMessage(error.message);
                self.isLoading(false);
            });
    };

    // Group questions by groupName
    self.groupQuestions = function(questions) {
        var groups = {};
        var groupOrder = [];

        questions.forEach(function(q) {
            var groupName = q.groupName || '';
            if (!groups[groupName]) {
                groups[groupName] = [];
                groupOrder.push(groupName);
            }
            groups[groupName].push(q);
        });

        var result = groupOrder.map(function(groupName) {
            return {
                groupName: groupName || null,
                questions: groups[groupName]
            };
        });

        self.groupedQuestions(result);
    };

    // Submit Survey
    self.submitSurvey = function() {
        if (!self.canSubmit() || self.isSubmitting()) {
            return;
        }

        self.isSubmitting(true);

        // Prepare answers
        var answers = self.questions().map(function(q) {
            var score = q.selectedScore();
            return {
                questionId: q.id,
                score: score,
                comment: q.comment() || null
            };
        });

        // Submit survey with token
        fetch('/api/surveys/submit/' + self.token(), {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({
                answers: answers
            })
        })
        .then(function(response) {
            if (!response.ok) {
                return response.json().then(function(data) {
                    throw new Error(data.message || 'Anket gönderilemedi.');
                });
            }
            return response.json();
        })
        .then(function(data) {
            // Redirect to complete page
            window.location.href = '/Anket/Complete';
        })
        .catch(function(error) {
            alert('Hata: ' + error.message);
            self.isSubmitting(false);
        });
    };

    // Initialize
    self.loadSurvey();
}

// Start
document.addEventListener('DOMContentLoaded', function() {
    ko.applyBindings(new SurveyFormViewModel(), document.getElementById('survey-app'));
});
