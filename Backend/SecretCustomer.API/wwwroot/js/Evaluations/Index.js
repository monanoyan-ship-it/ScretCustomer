// Evaluations ViewModel - Çağrı Denetleme (Birleştirilmiş: Liste + Detay + Değerlendirme Formu)
function EvaluationsViewModel() {
    var self = this;

    // ========================
    // LIST STATE
    // ========================
    self.isLoading = ko.observable(true);
    self.errorMessage = ko.observable('');
    self.activeTab = ko.observable('pending');
    self.filterStatus = ko.observable('');
    self.searchTerm = ko.observable('');

    // List Data
    self.allAssignments = ko.observableArray([]);
    self.allEvaluations = ko.observableArray([]);

    // ========================
    // DETAILS MODAL STATE
    // ========================
    self.isDetailsModalOpen = ko.observable(false);
    self.isDetailsLoading = ko.observable(false);
    self.detailsData = ko.observable(null);

    // ========================
    // EVALUATE MODAL STATE
    // ========================
    self.isEvaluateModalOpen = ko.observable(false);
    self.isFormLoading = ko.observable(false);
    self.isSavingForm = ko.observable(false);
    self.modalErrorMessage = ko.observable('');
    self.formSuccessMessage = ko.observable('');
    self.formData = ko.observable(null);
    self.currentAssignmentId = null;
    self.currentEvaluationId = null;

    // Form fields
    self.callId = ko.observable('');
    self.callDate = ko.observable('');
    self.durationMinutes = ko.observable(null);
    self.controlTime = ko.observable('');
    self.selectedOrganizationId = ko.observable(null);
    self.availablePersonnel = ko.observableArray([]);
    self.isLoadingPersonnel = ko.observable(false);
    self.evaluatedPersonnelId = ko.observable(null);
    self.evaluatedUnknownPersonnel = ko.observable('');
    self.evaluationComment = ko.observable('');

    // Organizasyon seçildiğinde personel listesini yükle
    self.selectedOrganizationId.subscribe(function(orgId) {
        if (!orgId) {
            self.availablePersonnel([]);
            self.evaluatedPersonnelId(null);
            return;
        }
        self.loadPersonnelByOrganization(orgId);
    });

    // Organizasyona göre personel listesi yükle
    self.loadPersonnelByOrganization = function(organizationId) {
        self.isLoadingPersonnel(true);
        self.availablePersonnel([]);
        self.evaluatedPersonnelId(null);

        fetch('/api/evaluations/personnel-by-org/' + organizationId, { credentials: 'include' })
            .then(function(response) {
                if (!response.ok) throw new Error('Personel yüklenemedi');
                return response.json();
            })
            .then(function(data) {
                self.availablePersonnel(data || []);
            })
            .catch(function(error) {
                console.error('Personnel loading error:', error);
                self.availablePersonnel([]);
            })
            .finally(function() {
                self.isLoadingPersonnel(false);
            });
    };

    // Answers dictionary (questionId -> answer observable)
    self.answers = {};

    // Computed scores
    self.totalScoreCalc = ko.observable(0);
    self.maxScoreCalc = ko.observable(0);
    self.scorePercentageCalc = ko.observable(0);
    self.yellowCardCountCalc = ko.observable(0);
    self.redCardCountCalc = ko.observable(0);

    // Helper: Generate score options array [0, 1, 2, ..., maxPoints]
    // Müşteri isteği: ağırlık=15, max=2 ise → 0,1,2 seçenekleri
    self.getScoreOptions = function(maxPoints) {
        var max = parseInt(maxPoints) || 5;
        if (max > 10) max = 10; // Max 10 seçenek göster (UI için)
        var options = [];
        for (var i = 0; i <= max; i++) {
            options.push(i);
        }
        return options;
    };

    // ========================
    // LIST COMPUTED
    // ========================

    // Pending Assignments (no evaluation yet)
    self.pendingAssignments = ko.computed(function() {
        var assignments = self.allAssignments();
        var evaluationAssignmentIds = self.allEvaluations().map(function(e) { return e.assignmentId; });
        var search = self.searchTerm().toLowerCase();

        return assignments.filter(function(a) {
            if (a.isCompleted) return false;
            if (evaluationAssignmentIds.indexOf(a.id) >= 0) return false;
            if (search) {
                var matchesSearch = (a.projectName || '').toLowerCase().indexOf(search) >= 0 ||
                                   (a.branchName || '').toLowerCase().indexOf(search) >= 0 ||
                                   (a.checklistName || '').toLowerCase().indexOf(search) >= 0;
                if (!matchesSearch) return false;
            }
            return true;
        });
    });

    // Completed Evaluations
    self.completedEvaluations = ko.computed(function() {
        var search = self.searchTerm().toLowerCase();
        return self.allEvaluations().filter(function(e) {
            if (e.status !== 'Completed') return false;
            if (search) {
                var matchesSearch = (e.projectName || '').toLowerCase().indexOf(search) >= 0 ||
                                   (e.branchName || '').toLowerCase().indexOf(search) >= 0;
                if (!matchesSearch) return false;
            }
            return true;
        });
    });

    // Draft Evaluations
    self.draftEvaluations = ko.computed(function() {
        var search = self.searchTerm().toLowerCase();
        return self.allEvaluations().filter(function(e) {
            if (e.status !== 'Draft' && e.status !== 'InProgress') return false;
            if (search) {
                var matchesSearch = (e.projectName || '').toLowerCase().indexOf(search) >= 0 ||
                                   (e.branchName || '').toLowerCase().indexOf(search) >= 0;
                if (!matchesSearch) return false;
            }
            return true;
        });
    });

    // ========================
    // LIST FUNCTIONS
    // ========================

    self.loadEvaluations = function() {
        self.isLoading(true);
        self.errorMessage('');

        Promise.all([
            fetch('/api/assignments/my-assignments', { credentials: 'include' }).then(function(r) { return r.json(); }),
            fetch('/api/evaluations/evaluator', { credentials: 'include' }).then(function(r) { return r.json(); })
        ])
        .then(function(results) {
            self.allAssignments(results[0] || []);
            self.allEvaluations(results[1] || []);
        })
        .catch(function(error) {
            console.error('Load error:', error);
            self.errorMessage(T('Evaluation.LoadError', 'Veriler yüklenirken bir hata oluştu.'));
        })
        .finally(function() {
            self.isLoading(false);
        });
    };

    // ========================
    // DETAILS MODAL FUNCTIONS
    // ========================

    self.showDetails = function(evaluation) {
        self.isDetailsModalOpen(true);
        self.isDetailsLoading(true);
        self.detailsData(null);

        fetch('/api/evaluations/' + evaluation.id, { credentials: 'include' })
            .then(function(response) {
                if (!response.ok) throw new Error(T('Evaluation.NotFound', 'Değerlendirme bulunamadı'));
                return response.json();
            })
            .then(function(data) {
                self.detailsData(data);
            })
            .catch(function(error) {
                console.error('Details load error:', error);
                self.closeDetailsModal();
                self.errorMessage(T('Evaluation.DetailsLoadError', 'Değerlendirme detayları yüklenirken bir hata oluştu.'));
            })
            .finally(function() {
                self.isDetailsLoading(false);
            });
    };

    self.closeDetailsModal = function() {
        self.isDetailsModalOpen(false);
        self.detailsData(null);
    };

    // ========================
    // EVALUATE MODAL FUNCTIONS
    // ========================

    self.startEvaluation = function(assignment) {
        self.currentAssignmentId = assignment.id;
        self.currentEvaluationId = null;
        self.openEvaluateModal();
    };

    self.continueEvaluation = function(evaluation) {
        self.currentAssignmentId = null;
        self.currentEvaluationId = evaluation.id;
        self.openEvaluateModal();
    };

    self.openEvaluateModal = function() {
        self.isEvaluateModalOpen(true);
        self.isFormLoading(true);
        self.modalErrorMessage('');
        self.formSuccessMessage('');
        self.formData(null);
        self.answers = {};
        self.resetFormFields();
        self.loadForm();
    };

    self.resetFormFields = function() {
        self.callId('');
        self.callDate('');
        self.durationMinutes(null);
        self.controlTime('');
        self.selectedOrganizationId(null);
        self.availablePersonnel([]);
        self.evaluatedPersonnelId(null);
        self.evaluatedUnknownPersonnel('');
        self.evaluationComment('');
        self.totalScoreCalc(0);
        self.maxScoreCalc(0);
        self.scorePercentageCalc(0);
        self.yellowCardCountCalc(0);
        self.redCardCountCalc(0);
    };

    self.closeEvaluateModal = function() {
        self.isEvaluateModalOpen(false);
        self.formData(null);
        self.currentAssignmentId = null;
        self.currentEvaluationId = null;
    };

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
        self.isFormLoading(true);
        self.modalErrorMessage('');

        var url = '';
        if (self.currentAssignmentId) {
            url = '/api/evaluations/form/' + self.currentAssignmentId;
        } else if (self.currentEvaluationId) {
            url = '/api/evaluations/form/edit/' + self.currentEvaluationId;
        } else {
            self.modalErrorMessage(T('Evaluation.InvalidParams', 'Geçersiz parametreler'));
            self.isFormLoading(false);
            return;
        }

        fetch(url, { credentials: 'include' })
            .then(function(response) {
                if (!response.ok) throw new Error(T('Evaluation.FormLoadError', 'Form yüklenemedi'));
                return response.json();
            })
            .then(function(data) {
                self.formData(data);

                // Load existing values if any
                if (data.callId) self.callId(data.callId);
                if (data.callDate) self.callDate(data.callDate.split('T')[0]);
                if (data.durationMinutes) self.durationMinutes(data.durationMinutes);
                if (data.evaluatedUnknownPersonnel) self.evaluatedUnknownPersonnel(data.evaluatedUnknownPersonnel);
                if (data.evaluationComment) self.evaluationComment(data.evaluationComment);

                // Mevcut seçili organizasyonu ve personeli yükle
                if (data.selectedOrganizationId) {
                    // Önce personel listesini yükle, sonra organizasyonu set et
                    self.availablePersonnel(data.availablePersonnel || []);
                    self.selectedOrganizationId(data.selectedOrganizationId);
                    if (data.evaluatedPersonnelId) {
                        self.evaluatedPersonnelId(data.evaluatedPersonnelId);
                    }
                } else if (data.evaluatedPersonnelId) {
                    // Eski kayıtlar için (organizasyon seçilmemiş)
                    self.availablePersonnel(data.availablePersonnel || []);
                    self.evaluatedPersonnelId(data.evaluatedPersonnelId);
                }

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
                self.modalErrorMessage(T('Evaluation.FormLoadErrorMessage', 'Form yüklenirken bir hata oluştu.'));
            })
            .finally(function() {
                self.isFormLoading(false);
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
                // Müşteri isteği: ağırlık puanı (weightPoints) ve max skor (maxPoints) sistemi
                // Örnek: ağırlık=15, max=2 → 0 seçilirse 0 puan, 1 seçilirse 7.5 puan, 2 seçilirse 15 puan
                var weight = q.weightPoints || q.points || 0;
                var maxScore = q.maxPoints || 5;
                max += weight;  // Toplam maksimum puan = ağırlık puanları toplamı

                // Use given points if available (manual override)
                if (answer.givenPoints() !== null && answer.givenPoints() !== '') {
                    total += parseFloat(answer.givenPoints()) || 0;
                } else if (answer.answerNumeric() !== null && answer.answerNumeric() !== '') {
                    // Likert/Rating hesaplaması: (cevap / maxScore) * ağırlık
                    var numericValue = parseFloat(answer.answerNumeric()) || 0;
                    total += (numericValue / maxScore) * weight;
                } else if (answer.answerText()) {
                    // YesNo type - Evet = tam puan, Hayır = 0 puan
                    var answerLower = answer.answerText().toLowerCase();
                    if (answerLower === 'evet' || answerLower === 'yes') {
                        total += weight;
                    }
                }
            });
        });

        self.totalScoreCalc(Math.max(0, total));
        self.maxScoreCalc(max);
        self.scorePercentageCalc(max > 0 ? (total / max) * 100 : 0);
        self.yellowCardCountCalc(yellowCards);
        self.redCardCountCalc(redCards);
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
            evaluatedOrganizationId: self.selectedOrganizationId() || null,
            evaluatedPersonnelId: self.evaluatedPersonnelId() || null,
            evaluatedUnknownPersonnel: self.evaluatedUnknownPersonnel() || null,
            controlDate: new Date().toISOString().split('T')[0],
            controlTime: self.controlTime() || null,
            formOpenedAt: new Date().toISOString()
        };
    };

    // Save as draft
    self.saveDraft = function() {
        self.isSavingForm(true);
        self.modalErrorMessage('');
        self.formSuccessMessage('');

        var data = self.prepareData();

        fetch('/api/evaluations/draft', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            credentials: 'include',
            body: JSON.stringify(data)
        })
        .then(function(response) {
            if (!response.ok) throw new Error(T('Evaluation.DraftSaveError', 'Taslak kaydedilemedi'));
            return response.json();
        })
        .then(function(result) {
            self.formSuccessMessage(T('Evaluation.DraftSaved', 'Taslak başarıyla kaydedildi.'));
        })
        .catch(function(error) {
            console.error('Draft save error:', error);
            self.modalErrorMessage(T('Evaluation.DraftSaveErrorMessage', 'Taslak kaydedilirken bir hata oluştu.'));
        })
        .finally(function() {
            self.isSavingForm(false);
        });
    };

    // Submit evaluation
    self.submitEvaluation = function() {
        // Organizasyon seçimi zorunlu kontrolü
        if (!self.selectedOrganizationId()) {
            self.modalErrorMessage(T('Evaluation.OrganizationRequired', 'Lütfen bir organizasyon seçin.'));
            return;
        }

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
            self.modalErrorMessage(T('Evaluation.AnswerAllRequired', 'Lütfen tüm zorunlu soruları cevaplayın.'));
            return;
        }

        self.isSavingForm(true);
        self.modalErrorMessage('');
        self.formSuccessMessage('');

        var data = self.prepareData();

        fetch('/api/evaluations/submit', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            credentials: 'include',
            body: JSON.stringify(data)
        })
        .then(function(response) {
            if (!response.ok) throw new Error(T('Evaluation.SubmitError', 'Değerlendirme gönderilemedi'));
            return response.json();
        })
        .then(function(result) {
            self.formSuccessMessage(T('Evaluation.SubmitSuccess', 'Değerlendirme başarıyla tamamlandı.'));
            // Close modal and refresh list after 1.5 seconds
            setTimeout(function() {
                self.closeEvaluateModal();
                self.loadEvaluations();
            }, 1500);
        })
        .catch(function(error) {
            console.error('Submit error:', error);
            self.modalErrorMessage(T('Evaluation.SubmitErrorMessage', 'Değerlendirme gönderilirken bir hata oluştu.'));
        })
        .finally(function() {
            self.isSavingForm(false);
        });
    };

    // ========================
    // INITIALIZE
    // ========================
    self.loadEvaluations();
}

// Apply bindings when DOM is ready
document.addEventListener('DOMContentLoaded', function() {
    ko.applyBindings(new EvaluationsViewModel(), document.getElementById('evaluations-app'));

    // Initialize tooltips
    var tooltipTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="tooltip"]'));
    tooltipTriggerList.map(function (tooltipTriggerEl) {
        return new bootstrap.Tooltip(tooltipTriggerEl);
    });
});
