// ===== Evaluation Form ViewModel =====

function AnswerViewModel(questionId, maxPoints) {
    var self = this;

    self.questionId = questionId;
    self.score = ko.observable(null);
    self.note = ko.observable('');
    self.isNA = ko.observable(false);
    self.selectedSubCriteria = ko.observableArray([]);
    self.applyPenalty = ko.observable(false);
    self.selectedPenaltyType = ko.observable('None');

    // When score is -1, it means N/A
    self.score.subscribe(function(val) {
        if (val === -1) {
            self.isNA(true);
        } else {
            self.isNA(false);
        }
    });
}

function EvaluationFormViewModel() {
    var self = this;

    // ===== State =====
    self.isLoading = ko.observable(true);
    self.isSaving = ko.observable(false);
    self.errorMessage = ko.observable('');

    // ===== Form Data =====
    self.formData = ko.observable(null);
    self.answers = {};

    // ===== Evaluation Info =====
    self.selectedPeriodId = ko.observable('');
    self.selectedPersonnelId = ko.observable('');
    self.selectedOrganizationId = ko.observable('');
    self.evaluationDate = ko.observable(new Date().toISOString().split('T')[0]);
    self.generalNotes = ko.observable('');
    self.callId = ko.observable('');
    self.evaluationComment = ko.observable('');

    // ===== Get Assignment ID from page =====
    self.assignmentId = ko.observable(null);

    // ===== Helper Functions =====
    self.getAnswer = function(questionId) {
        if (!self.answers[questionId]) {
            self.answers[questionId] = new AnswerViewModel(questionId);
        }
        return self.answers[questionId];
    };

    self.getScoreOptions = function(maxPoints) {
        var options = [];
        var steps = maxPoints || 5;
        for (var i = 0; i <= steps; i++) {
            options.push({
                value: i,
                label: i.toString()
            });
        }
        return options;
    };

    self.isSubCriteriaSelected = function(questionId, subCriteriaId) {
        var answer = self.getAnswer(questionId);
        return ko.computed({
            read: function() {
                return answer.selectedSubCriteria().indexOf(subCriteriaId) >= 0;
            },
            write: function(checked) {
                var arr = answer.selectedSubCriteria();
                if (checked && arr.indexOf(subCriteriaId) < 0) {
                    answer.selectedSubCriteria.push(subCriteriaId);
                } else if (!checked) {
                    answer.selectedSubCriteria.remove(subCriteriaId);
                }
            }
        });
    };

    // ===== Calculated Score =====
    self.calculatedScore = ko.computed(function() {
        var data = self.formData();
        if (!data || !data.sections) return 0;

        var totalMaxPoints = 0;
        var totalEarnedPoints = 0;

        data.sections.forEach(function(section) {
            section.questions.forEach(function(question) {
                var answer = self.answers[question.id];
                if (!answer) return;

                var score = answer.score();
                var maxPoints = question.maxPoints || 5;

                // Skip N/A answers
                if (score === -1 || answer.isNA()) return;

                // Scored questions
                if (question.scoringType === 'Scored' && score !== null) {
                    totalMaxPoints += question.weightPoints;
                    var earnedPoints = (score / maxPoints) * question.weightPoints;
                    totalEarnedPoints += earnedPoints;
                }
            });
        });

        if (totalMaxPoints === 0) return 0;
        return Math.round((totalEarnedPoints / totalMaxPoints) * 100);
    });

    // ===== Load Form Data =====
    self.loadForm = function() {
        self.isLoading(true);
        self.errorMessage('');

        var assignId = self.assignmentId();

        // Load evaluation form by assignment ID
        var url = '/api/evaluations/form/' + assignId;

        fetch(url, { credentials: 'include' })
            .then(function(res) {
                if (!res.ok) {
                    throw new Error('Form yüklenemedi: ' + res.status);
                }
                return res.json();
            })
            .then(function(data) {
                console.log('Form data loaded:', data);
                self.formData(data);

                // Set selected values from existing data
                if (data.selectedPeriodId) {
                    self.selectedPeriodId(data.selectedPeriodId);
                }
                if (data.selectedOrganizationId) {
                    self.selectedOrganizationId(data.selectedOrganizationId);
                }
                if (data.evaluatedPersonnelId) {
                    self.selectedPersonnelId(data.evaluatedPersonnelId);
                }
                if (data.callId) {
                    self.callId(data.callId);
                }
                if (data.evaluationComment) {
                    self.evaluationComment(data.evaluationComment);
                }

                // Load existing answers if any
                if (data.existingAnswers && data.existingAnswers.length > 0) {
                    data.existingAnswers.forEach(function(ans) {
                        var answer = self.getAnswer(ans.questionId);
                        if (ans.isNA) {
                            answer.score(-1);
                        } else if (ans.answerNumeric !== null) {
                            answer.score(ans.answerNumeric);
                        }
                        if (ans.answerText) {
                            answer.note(ans.answerText);
                        }
                    });
                }
            })
            .catch(function(error) {
                console.error('Error loading form:', error);
                self.errorMessage(error.message || 'Form yüklenirken bir hata oluştu.');
            })
            .finally(function() {
                self.isLoading(false);
            });
    };

    // ===== Load personnel by organization =====
    self.loadPersonnelByOrganization = function(organizationId) {
        if (!organizationId) return;

        fetch('/api/evaluations/personnel-by-org/' + organizationId, { credentials: 'include' })
            .then(function(res) { return res.json(); })
            .then(function(data) {
                var formData = self.formData();
                if (formData) {
                    formData.availablePersonnel = data;
                    self.formData(formData);
                    self.formData.valueHasMutated();
                }
            })
            .catch(function(error) {
                console.error('Error loading personnel:', error);
            });
    };

    // Watch organization change
    self.selectedOrganizationId.subscribe(function(orgId) {
        if (orgId) {
            self.loadPersonnelByOrganization(orgId);
        }
    });

    // ===== Build Submit DTO =====
    self.buildSubmitDto = function() {
        var data = self.formData();
        var answers = [];

        if (data && data.sections) {
            data.sections.forEach(function(section) {
                section.questions.forEach(function(question) {
                    var answer = self.answers[question.id];
                    if (!answer) return;

                    var score = answer.score();
                    if (score === null && !answer.note()) return;

                    var answerDto = {
                        questionId: question.id,
                        answerText: answer.note() || null,
                        answerNumeric: score === -1 ? null : score,
                        isNA: score === -1,
                        givenPoints: score === -1 ? 0 : (score !== null ? (score / (question.maxPoints || 5)) * question.weightPoints : null),
                        notes: answer.note() || null,
                        applyPenalty: answer.applyPenalty(),
                        selectedPenaltyType: answer.selectedPenaltyType()
                    };

                    answers.push(answerDto);
                });
            });
        }

        return {
            assignmentId: data?.assignmentId || self.assignmentId(),
            assignmentPeriodId: self.selectedPeriodId() ? parseInt(self.selectedPeriodId()) : null,
            evaluatedOrganizationId: self.selectedOrganizationId() ? parseInt(self.selectedOrganizationId()) : null,
            evaluatedPersonnelId: self.selectedPersonnelId() ? parseInt(self.selectedPersonnelId()) : null,
            callId: self.callId() || null,
            controlDate: self.evaluationDate() || null,
            evaluationComment: self.evaluationComment() || null,
            notes: self.generalNotes() || null,
            answers: answers
        };
    };

    // ===== Save Draft =====
    self.saveDraft = function() {
        var dto = self.buildSubmitDto();
        dto.saveAsDraft = true;

        self.isSaving(true);

        fetch('/api/evaluations/draft', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            credentials: 'include',
            body: JSON.stringify(dto)
        })
            .then(function(res) {
                if (!res.ok) {
                    return res.json().then(function(err) {
                        throw new Error(err.message || 'Taslak kaydedilemedi');
                    });
                }
                return res.json();
            })
            .then(function(result) {
                toastr.success('Taslak başarıyla kaydedildi.');
            })
            .catch(function(error) {
                console.error('Error saving draft:', error);
                toastr.error(error.message || 'Taslak kaydedilirken bir hata oluştu.');
            })
            .finally(function() {
                self.isSaving(false);
            });
    };

    // ===== Submit Evaluation =====
    self.submitEvaluation = function() {
        var dto = self.buildSubmitDto();

        // Validation
        if (!dto.assignmentId) {
            toastr.warning('Atama bilgisi bulunamadı.');
            return;
        }

        if (dto.answers.length === 0) {
            toastr.warning('Lütfen en az bir soruyu yanıtlayın.');
            return;
        }

        self.isSaving(true);

        fetch('/api/evaluations/submit', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            credentials: 'include',
            body: JSON.stringify(dto)
        })
            .then(function(res) {
                if (!res.ok) {
                    return res.json().then(function(err) {
                        throw new Error(err.message || 'Değerlendirme gönderilemedi');
                    });
                }
                return res.json();
            })
            .then(function(result) {
                toastr.success('Değerlendirme başarıyla gönderildi.');
                // Redirect to assignments
                setTimeout(function() {
                    window.location.href = '/Assignments/Index';
                }, 1500);
            })
            .catch(function(error) {
                console.error('Error submitting evaluation:', error);
                toastr.error(error.message || 'Değerlendirme gönderilirken bir hata oluştu.');
            })
            .finally(function() {
                self.isSaving(false);
            });
    };

    // ===== Initialize =====
    self.init = function() {
        var appElement = document.getElementById('evaluation-form-app');
        if (appElement) {
            var assignId = appElement.getAttribute('data-assignment-id');
            if (assignId) {
                self.assignmentId(parseInt(assignId));
                self.loadForm();
            } else {
                self.errorMessage('Atama ID bulunamadı.');
                self.isLoading(false);
            }
        }
    };
}

// ===== Apply Bindings =====
$(document).ready(function() {
    var vm = new EvaluationFormViewModel();
    ko.applyBindings(vm, document.getElementById('evaluation-form-app'));
    vm.init();
});
