// Survey Form ViewModel
function SurveyFormViewModel() {
    var self = this;

    // Config
    self.token = window.surveyConfig ? window.surveyConfig.token : '';

    // State
    self.isLoading = ko.observable(true);
    self.isSubmitting = ko.observable(false);
    self.errorMessage = ko.observable('');

    // Data
    self.surveyInfo = ko.observable(null);
    self.questions = ko.observableArray([]);

    // Computed: Question groups
    self.questionGroups = ko.computed(function() {
        var questions = self.questions();
        var groups = {};
        var groupOrder = [];

        questions.forEach(function(q) {
            var groupName = q.groupName || 'Genel';
            if (!groups[groupName]) {
                groups[groupName] = {
                    name: groupName,
                    order: q.groupOrder || 0,
                    questions: []
                };
                groupOrder.push(groupName);
            }
            groups[groupName].questions.push(q);
        });

        // Sort groups by order
        return groupOrder
            .map(function(name) { return groups[name]; })
            .sort(function(a, b) { return a.order - b.order; });
    });

    // Soru cevaplanmış mı kontrolü
    self.isQuestionAnswered = function(q) {
        var answer = q.answer();
        var hasSubCriteriaSelection = answer.selectedSubCriteriaIds && answer.selectedSubCriteriaIds().length > 0;
        if (q.scoringType === 'Descriptive') {
            return answer.comment() && answer.comment().trim() !== '';
        }
        return answer.score() !== null || hasSubCriteriaSelection;
    };

    // Computed: Answered count
    self.answeredCount = ko.computed(function() {
        var count = 0;
        self.questions().forEach(function(q) {
            if (self.isQuestionAnswered(q)) {
                count++;
            }
        });
        return count;
    });

    // Helper: Get score options for a question
    self.getScoreOptions = function(maxPoints) {
        var options = [];
        for (var i = 0; i <= maxPoints; i++) {
            options.push(i);
        }
        return options;
    };

    // Load survey info
    self.loadSurveyInfo = function() {
        if (!self.token) {
            self.errorMessage('Geçersiz anket linki. Token bulunamadı.');
            self.isLoading(false);
            return;
        }

        fetch('/api/surveys/validate/' + encodeURIComponent(self.token))
            .then(function(res) {
                if (!res.ok) {
                    return res.json().then(function(data) {
                        throw new Error(data.message || 'Anket doğrulanamadı');
                    });
                }
                return res.json();
            })
            .then(function(data) {
                if (data.valid && data.invitation) {
                    self.surveyInfo(data.invitation);
                    // Load questions from response
                    self.loadQuestions(data.questions || []);
                } else {
                    self.errorMessage('Geçersiz anket daveti.');
                }
            })
            .catch(function(error) {
                console.error('Error validating survey:', error);
                self.errorMessage(error.message || 'Anket yüklenirken bir hata oluştu.');
            })
            .finally(function() {
                self.isLoading(false);
            });
    };

    // Load questions from API response
    self.loadQuestions = function(questionsData) {
        var questions = (questionsData || []).map(function(q, index) {
            // SubCriteria observables
            var subCriteriaList = (q.subCriteria || []).map(function(sc) {
                return {
                    id: sc.id,
                    description: sc.description,
                    order: sc.order || 0,
                    isSelected: ko.observable(false)
                };
            });

            return {
                id: q.id,
                text: q.text,
                description: q.description,
                groupName: q.groupName,
                groupOrder: q.groupOrder || 0,
                order: q.order || (index + 1),
                scoringType: q.scoringType || 'Scored',
                maxPoints: q.maxPoints || 10,
                weightPoints: q.weightPoints || 1,
                // SubCriteria settings
                selectionTypeId: parseInt(q.selectionTypeId) || 2, // 1=Single, 2=Multiple
                showScoreInput: q.showScoreInput !== false, // Default true
                isRequired: q.isRequired === true, // Zorunlu mu?
                subCriteria: subCriteriaList,
                hasSubCriteria: subCriteriaList.length > 0,
                // Answer model
                answer: ko.observable({
                    score: ko.observable(null),
                    comment: ko.observable(''),
                    selectedSubCriteriaIds: ko.observableArray([])
                })
            };
        });
        self.questions(questions);
    };

    // SubCriteria selection toggle (for Single selection type)
    self.selectSubCriteria = function(question, subCriteria) {
        if (question.selectionTypeId === 1) {
            // Single selection: unselect all others first
            question.subCriteria.forEach(function(sc) {
                sc.isSelected(sc.id === subCriteria.id);
            });
            // Update answer
            var answer = question.answer();
            answer.selectedSubCriteriaIds([subCriteria.id]);
        } else {
            // Multiple selection: toggle
            subCriteria.isSelected(!subCriteria.isSelected());
            // Update answer
            var answer = question.answer();
            var selectedIds = question.subCriteria
                .filter(function(sc) { return sc.isSelected(); })
                .map(function(sc) { return sc.id; });
            answer.selectedSubCriteriaIds(selectedIds);
        }
    };

    // Submit survey
    self.submitSurvey = function() {
        if (self.answeredCount() === 0) {
            toastr.warning('Lütfen en az bir soruyu cevaplayın.');
            return;
        }

        // Zorunlu soru kontrolü
        var unansweredRequired = self.questions().filter(function(q) {
            return q.isRequired && !self.isQuestionAnswered(q);
        });
        if (unansweredRequired.length > 0) {
            var questionNumbers = unansweredRequired.map(function(q) { return '#' + q.order; }).join(', ');
            toastr.error('Zorunlu sorular cevaplanmalıdır: ' + questionNumbers);
            return;
        }

        // Prepare answers
        var answers = [];
        self.questions().forEach(function(q) {
            var answer = q.answer();
            var score = answer.score();
            var comment = answer.comment();
            var selectedSubCriteriaIds = answer.selectedSubCriteriaIds ? answer.selectedSubCriteriaIds() : [];

            // Skip unanswered questions (except descriptive which only need comment)
            if (q.scoringType === 'Descriptive') {
                if (comment && comment.trim()) {
                    answers.push({
                        questionId: q.id,
                        score: null,
                        comment: comment,
                        selectedSubCriteriaIds: selectedSubCriteriaIds
                    });
                }
            } else if (score !== null || selectedSubCriteriaIds.length > 0) {
                answers.push({
                    questionId: q.id,
                    score: score,
                    comment: comment || null,
                    selectedSubCriteriaIds: selectedSubCriteriaIds
                });
            }
        });

        if (answers.length === 0) {
            toastr.warning('Lütfen en az bir soruyu cevaplayın.');
            return;
        }

        // Confirm
        if (!confirm('Anketi göndermek istediğinize emin misiniz? Bu işlem geri alınamaz.')) {
            return;
        }

        self.isSubmitting(true);

        fetch('/api/surveys/submit/' + encodeURIComponent(self.token), {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ answers: answers })
        })
        .then(function(res) {
            if (!res.ok) {
                return res.json().then(function(data) {
                    throw new Error(data.message || 'Gönderim başarısız');
                });
            }
            return res.json();
        })
        .then(function(result) {
            if (result.success) {
                toastr.success('Anket başarıyla gönderildi!');
                // Redirect to completed page
                setTimeout(function() {
                    window.location.href = '/Survey/Completed';
                }, 1500);
            } else {
                toastr.error(result.message || 'Anket gönderilemedi.');
            }
        })
        .catch(function(error) {
            console.error('Error submitting survey:', error);
            toastr.error(error.message || 'Anket gönderilirken bir hata oluştu.');
        })
        .finally(function() {
            self.isSubmitting(false);
        });
    };

    // Initialize
    self.init = function() {
        self.loadSurveyInfo();
    };

    self.init();
}

// Apply bindings
$(document).ready(function() {
    var app = document.getElementById('survey-app');
    if (app) {
        ko.applyBindings(new SurveyFormViewModel(), app);
    }
});
