// Evaluation Form ViewModel - Cagri Denetleme
function EvaluationFormViewModel() {
    var self = this;

    // State
    self.isLoading = ko.observable(true);
    self.isSaving = ko.observable(false);
    self.errorMessage = ko.observable('');
    self.successMessage = ko.observable('');
    self.formData = ko.observable(null);

    // Form fields
    self.callId = ko.observable('');
    self.callDate = ko.observable('');
    self.durationMinutes = ko.observable(null);
    self.controlTime = ko.observable('');
    self.evaluatedPersonnelId = ko.observable(null);
    self.evaluatedUnknownPersonnel = ko.observable('');
    self.evaluationComment = ko.observable('');

    // Answers dictionary (questionId -> answer observable)
    self.answers = {};

    // Computed scores
    self.totalScore = ko.observable(0);
    self.maxScore = ko.observable(0);
    self.scorePercentage = ko.observable(0);
    self.yellowCardCount = ko.observable(0);
    self.redCardCount = ko.observable(0);

    // Get or create answer for a question
    self.getAnswer = function(questionId) {
        if (!self.answers[questionId]) {
            self.answers[questionId] = {
                questionId: questionId,
                answerText: ko.observable(''),
                answerNumeric: ko.observable(null),
                isNA: ko.observable(false),
                givenPoints: ko.observable(null),
                notes: ko.observable(''),
                recommendationNotes: ko.observable(''),
                applyPenalty: ko.observable(false),
                selectedPenaltyType: ko.observable('')
            };

            // Subscribe to changes to recalculate scores
            self.answers[questionId].answerNumeric.subscribe(function() { self.calculateScores(); });
            self.answers[questionId].answerText.subscribe(function() { self.calculateScores(); });
            self.answers[questionId].isNA.subscribe(function() { self.calculateScores(); });
            self.answers[questionId].givenPoints.subscribe(function() { self.calculateScores(); });
            self.answers[questionId].applyPenalty.subscribe(function() { self.calculateScores(); });
            self.answers[questionId].selectedPenaltyType.subscribe(function() { self.calculateScores(); });
        }
        return self.answers[questionId];
    };

    // Load form data
    self.loadForm = function() {
        self.isLoading(true);
        self.errorMessage('');

        var url = '';
        if (initialAssignmentId) {
            url = '/api/evaluations/form/' + initialAssignmentId;
        } else if (initialEvaluationId) {
            url = '/api/evaluations/form/edit/' + initialEvaluationId;
        } else {
            self.errorMessage('Gecersiz parametreler');
            self.isLoading(false);
            return;
        }

        fetch(url, { credentials: 'include' })
            .then(function(response) {
                if (!response.ok) throw new Error('Form yuklenemedi');
                return response.json();
            })
            .then(function(data) {
                self.formData(data);

                // Load existing values if any
                if (data.callId) self.callId(data.callId);
                if (data.callDate) self.callDate(data.callDate.split('T')[0]);
                if (data.durationMinutes) self.durationMinutes(data.durationMinutes);
                if (data.evaluatedPersonnelId) self.evaluatedPersonnelId(data.evaluatedPersonnelId);
                if (data.evaluatedUnknownPersonnel) self.evaluatedUnknownPersonnel(data.evaluatedUnknownPersonnel);
                if (data.evaluationComment) self.evaluationComment(data.evaluationComment);

                // Load existing answers
                if (data.existingAnswers && data.existingAnswers.length > 0) {
                    data.existingAnswers.forEach(function(a) {
                        var answer = self.getAnswer(a.questionId);
                        if (a.answerText) answer.answerText(a.answerText);
                        if (a.answerNumeric) answer.answerNumeric(a.answerNumeric);
                        answer.isNA(a.isNA || false);
                        if (a.givenPoints) answer.givenPoints(a.givenPoints);
                        if (a.notes) answer.notes(a.notes);
                        if (a.recommendationNotes) answer.recommendationNotes(a.recommendationNotes);
                        answer.applyPenalty(a.isPenaltyApplied || false);
                        if (a.appliedPenaltyType && a.appliedPenaltyType !== 'None') {
                            answer.selectedPenaltyType(a.appliedPenaltyType);
                        }
                    });
                }

                // Initialize answers for all questions
                data.sections.forEach(function(section) {
                    section.questions.forEach(function(q) {
                        self.getAnswer(q.id);
                    });
                });

                self.calculateScores();
            })
            .catch(function(error) {
                console.error('Form loading error:', error);
                self.errorMessage('Form yuklenirken bir hata olustu.');
            })
            .finally(function() {
                self.isLoading(false);
            });
    };

    // Calculate scores
    self.calculateScores = function() {
        if (!self.formData()) return;

        var total = 0;
        var max = 0;
        var yellowCards = 0;
        var redCards = 0;

        self.formData().sections.forEach(function(section) {
            section.questions.forEach(function(q) {
                var answer = self.answers[q.id];
                if (!answer) return;

                // Skip N/A questions
                if (answer.isNA()) return;

                // Skip unscored questions
                if (q.scoringType === 'Unscored') return;

                // Handle penalty questions
                if (q.scoringType === 'Penalty' || q.penaltyType !== 'None') {
                    if (answer.applyPenalty()) {
                        if (answer.selectedPenaltyType() === 'YellowCard') {
                            yellowCards++;
                        } else if (answer.selectedPenaltyType() === 'RedCard') {
                            redCards++;
                        }
                        total -= q.penaltyValue || 0;
                    }
                    return;
                }

                // Normal scored questions
                var qMax = q.maxPoints || q.points || 0;
                max += qMax;

                // Use given points if available
                if (answer.givenPoints() !== null && answer.givenPoints() !== '') {
                    total += parseFloat(answer.givenPoints()) || 0;
                } else if (answer.answerNumeric() !== null) {
                    // Calculate based on Likert/Star scale
                    total += (answer.answerNumeric() / 5) * qMax;
                } else if (answer.answerText()) {
                    // YesNo type
                    if (answer.answerText().toLowerCase() === 'evet' || answer.answerText().toLowerCase() === 'yes') {
                        total += qMax;
                    }
                }
            });
        });

        self.totalScore(Math.max(0, total));
        self.maxScore(max);
        self.scorePercentage(max > 0 ? (total / max) * 100 : 0);
        self.yellowCardCount(yellowCards);
        self.redCardCount(redCards);
    };

    // Prepare submission data
    self.prepareData = function() {
        var answers = [];

        Object.keys(self.answers).forEach(function(questionId) {
            var a = self.answers[questionId];
            answers.push({
                questionId: questionId,
                answerText: a.answerText() || null,
                answerNumeric: a.answerNumeric() || null,
                isNA: a.isNA(),
                givenPoints: a.givenPoints() ? parseFloat(a.givenPoints()) : null,
                notes: a.notes() || null,
                recommendationNotes: a.recommendationNotes() || null,
                applyPenalty: a.applyPenalty(),
                selectedPenaltyType: a.selectedPenaltyType() || null
            });
        });

        return {
            assignmentId: self.formData().assignmentId,
            answers: answers,
            notes: '',
            evaluationComment: self.evaluationComment(),
            callId: self.callId() || null,
            callDate: self.callDate() || null,
            durationMinutes: self.durationMinutes() ? parseInt(self.durationMinutes()) : null,
            evaluatedPersonnelId: self.evaluatedPersonnelId() || null,
            evaluatedUnknownPersonnel: self.evaluatedUnknownPersonnel() || null,
            controlDate: new Date().toISOString().split('T')[0],
            controlTime: self.controlTime() || null,
            formOpenedAt: new Date().toISOString()
        };
    };

    // Save as draft
    self.saveDraft = function() {
        self.isSaving(true);
        self.errorMessage('');
        self.successMessage('');

        var data = self.prepareData();

        fetch('/api/evaluations/draft', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            credentials: 'include',
            body: JSON.stringify(data)
        })
        .then(function(response) {
            if (!response.ok) throw new Error('Taslak kaydedilemedi');
            return response.json();
        })
        .then(function(result) {
            self.successMessage('Taslak basariyla kaydedildi.');
        })
        .catch(function(error) {
            console.error('Draft save error:', error);
            self.errorMessage('Taslak kaydedilirken bir hata olustu.');
        })
        .finally(function() {
            self.isSaving(false);
        });
    };

    // Submit evaluation
    self.submitEvaluation = function() {
        // Validate required questions
        var hasError = false;
        self.formData().sections.forEach(function(section) {
            section.questions.forEach(function(q) {
                if (q.isRequired) {
                    var answer = self.answers[q.id];
                    if (!answer) {
                        hasError = true;
                        return;
                    }
                    if (answer.isNA()) return; // N/A is acceptable

                    var hasAnswer = answer.answerText() || answer.answerNumeric() !== null || answer.givenPoints() !== null;
                    if (!hasAnswer) {
                        hasError = true;
                    }
                }
            });
        });

        if (hasError) {
            self.errorMessage('Lutfen tum zorunlu sorulari cevaplayin.');
            return;
        }

        self.isSaving(true);
        self.errorMessage('');
        self.successMessage('');

        var data = self.prepareData();

        fetch('/api/evaluations/submit', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            credentials: 'include',
            body: JSON.stringify(data)
        })
        .then(function(response) {
            if (!response.ok) throw new Error('Degerlendirme gonderilemedi');
            return response.json();
        })
        .then(function(result) {
            self.successMessage('Degerlendirme basariyla tamamlandi.');
            // Redirect after 2 seconds
            setTimeout(function() {
                window.location.href = '/Evaluations';
            }, 2000);
        })
        .catch(function(error) {
            console.error('Submit error:', error);
            self.errorMessage('Degerlendirme gonderilirken bir hata olustu.');
        })
        .finally(function() {
            self.isSaving(false);
        });
    };

    // Initialize
    self.loadForm();
}

// Apply bindings when DOM is ready
document.addEventListener('DOMContentLoaded', function() {
    ko.applyBindings(new EvaluationFormViewModel(), document.getElementById('evaluation-app'));

    // Initialize tooltips
    var tooltipTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="tooltip"]'));
    tooltipTriggerList.map(function (tooltipTriggerEl) {
        return new bootstrap.Tooltip(tooltipTriggerEl);
    });
});
