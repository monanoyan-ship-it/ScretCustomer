// Evaluations ViewModel - Çağrı Denetleme (Birleştirilmiş: Liste + Detay + Değerlendirme Formu)
function EvaluationsViewModel() {
    var self = this;

    // ========================
    // LIST STATE
    // ========================
    self.isLoading = ko.observable(true);
    self.errorMessage = ko.observable('');
    self.activeTab = ko.observable('assignments');
    self.currentUserRole = ko.observable(''); // Kullanıcı rolü (Admin kontrolü için)
    self.filterStatus = ko.observable('');
    // Her tab için ayrı search
    self.assignmentsSearch = ko.observable('');
    self.evaluationsSearch = ko.observable('');
    self.expiredSearch = ko.observable('');

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
    self.duration = ko.observable('');
    self.controlTime = ko.observable('');
    self.availablePersonnel = ko.observableArray([]);
    self.isLoadingPersonnel = ko.observable(false);
    self.evaluatedPersonnelId = ko.observable(null);
    self.evaluatedUnknownPersonnel = ko.observable('');
    self.evaluationComment = ko.observable('');

    // Dönem seçimi
    self.selectedPeriodId = ko.observable(null);
    self.availablePeriods = ko.observableArray([]);

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

    // Sekme 1: Aktif Atamalar (tarihi geçmemiş, tamamlanmamış)
    self.activeAssignments = ko.computed(function() {
        var assignments = self.allAssignments();
        var search = self.assignmentsSearch().toLowerCase();
        var today = new Date();
        today.setHours(0, 0, 0, 0);

        return assignments.filter(function(a) {
            if (a.isCompleted) return false;
            var dueDate = new Date(a.dueDate);
            if (dueDate < today) return false;
            if (search) {
                var matchesSearch = (a.projectName || '').toLowerCase().indexOf(search) >= 0 ||
                                   (a.checklistName || '').toLowerCase().indexOf(search) >= 0;
                if (!matchesSearch) return false;
            }
            return true;
        });
    });

    // Sekme 2: Tüm Dinlemeler (yapılmış evaluation'lar)
    self.allEvaluationsList = ko.computed(function() {
        var search = self.evaluationsSearch().toLowerCase();
        return self.allEvaluations().filter(function(e) {
            if (search) {
                var matchesSearch = (e.projectName || '').toLowerCase().indexOf(search) >= 0 ||
                                   (e.checklistName || '').toLowerCase().indexOf(search) >= 0 ||
                                   (e.evaluatedPersonnelName || '').toLowerCase().indexOf(search) >= 0 ||
                                   (e.evaluatedUnknownPersonnel || '').toLowerCase().indexOf(search) >= 0;
                if (!matchesSearch) return false;
            }
            return true;
        });
    });

    // Sekme 3: Tarihi Geçmiş Atamalar (hala dinleme eklenebilir)
    self.expiredAssignments = ko.computed(function() {
        var assignments = self.allAssignments();
        var search = self.expiredSearch().toLowerCase();
        var today = new Date();
        today.setHours(0, 0, 0, 0);

        return assignments.filter(function(a) {
            if (a.isCompleted) return false;
            var dueDate = new Date(a.dueDate);
            if (dueDate >= today) return false;
            if (search) {
                var matchesSearch = (a.projectName || '').toLowerCase().indexOf(search) >= 0 ||
                                   (a.checklistName || '').toLowerCase().indexOf(search) >= 0;
                if (!matchesSearch) return false;
            }
            return true;
        });
    });

    // Admin mi kontrolü
    self.isAdmin = ko.computed(function() {
        return self.currentUserRole() === 'Admin';
    });

    // Sadece taslak (Draft) durumundaki evaluation düzenlenebilir
    self.canEditEvaluation = function(evaluation) {
        return evaluation.status === 'Draft';
    };

    // ========================
    // LIST FUNCTIONS
    // ========================

    self.loadEvaluations = function() {
        self.isLoading(true);
        self.errorMessage('');

        Promise.all([
            fetch('/api/assignments/my-assignments', { credentials: 'include' }).then(function(r) { return r.json(); }),
            fetch('/api/evaluations/evaluator', { credentials: 'include' }).then(function(r) { return r.json(); }),
            fetch('/api/auth/me', { credentials: 'include' }).then(function(r) { return r.json(); })
        ])
        .then(function(results) {
            self.allAssignments(results[0] || []);
            self.allEvaluations(results[1] || []);
            if (results[2] && results[2].role) {
                self.currentUserRole(results[2].role);
            }
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
        self.duration('');
        self.controlTime('');
        self.availablePersonnel([]);
        self.evaluatedPersonnelId(null);
        self.evaluatedUnknownPersonnel('');
        self.evaluationComment('');
        self.selectedPeriodId(null);
        self.availablePeriods([]);
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
                if (data.duration) self.duration(data.duration);
                if (data.evaluatedUnknownPersonnel) self.evaluatedUnknownPersonnel(data.evaluatedUnknownPersonnel);
                if (data.evaluationComment) self.evaluationComment(data.evaluationComment);

                // Dönemleri yükle
                self.availablePeriods(data.availablePeriods || []);
                if (data.selectedPeriodId) {
                    self.selectedPeriodId(data.selectedPeriodId);
                } else if (data.availablePeriods && data.availablePeriods.length > 0) {
                    // Aktif dönemi otomatik seç
                    var activePeriod = data.availablePeriods.find(function(p) { return p.status === 'Open'; });
                    if (activePeriod) {
                        self.selectedPeriodId(activePeriod.id);
                    }
                }

                // Personel listesini yükle (Checklist'in organizasyonuna göre API'den geliyor)
                self.availablePersonnel(data.availablePersonnel || []);
                if (data.evaluatedPersonnelId) {
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
                // Soru zaten YellowCard/RedCard tanımlıysa otomatik set et
                data.sections.forEach(function(section) {
                    section.questions.forEach(function(q) {
                        var answer = self.getAnswer(q.id);
                        if (q.penaltyType === 'YellowCard' || q.penaltyType === 'RedCard') {
                            answer.selectedPenaltyType(q.penaltyType);
                        }
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

                // Handle penalty questions - penaltyType sorudan geliyor (checklist'te belirlendi)
                if (q.scoringType === 'Penalty') {
                    // Cevaplanmadıysa etkisi yok
                    if (answer.answerNumeric() === null || answer.answerNumeric() === '') return;

                    var numericValue = parseFloat(answer.answerNumeric()) || 0;
                    var maxScore = q.maxPoints || 2;
                    var weight = q.weightPoints || 0;

                    // 0 seçilirse ceza yok, maxScore seçilirse tam ceza
                    if (numericValue > 0) {
                        var penaltyAmount = (numericValue / maxScore) * weight;
                        total -= penaltyAmount;

                        // Kart sayısını tut
                        if (q.penaltyType === 'YellowCard') yellowCards++;
                        else if (q.penaltyType === 'RedCard') redCards++;
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
        var percentage = max > 0 ? (Math.max(0, total) / max) * 100 : 0;
        self.scorePercentageCalc(Math.min(100, percentage));
        self.yellowCardCountCalc(yellowCards);
        self.redCardCountCalc(redCards);
    };

    // Prepare submission data
    self.prepareData = function() {
        var answers = [];

        // Soruları map'e al (penaltyType için)
        var questionMap = {};
        if (self.formData()) {
            self.formData().sections.forEach(function(section) {
                section.questions.forEach(function(q) {
                    questionMap[q.id] = q;
                });
            });
        }

        Object.keys(self.answers).forEach(function(questionId) {
            var a = self.answers[questionId];
            var q = questionMap[questionId];
            // penaltyType sorudan geliyor (checklist'te belirlendi)
            var penaltyType = q && q.penaltyType && q.penaltyType !== 'None' ? q.penaltyType : null;
            // Cezalı sorularda: değer > 0 ise ceza uygula
            var shouldApplyPenalty = q && q.scoringType === 'Penalty' &&
                a.answerNumeric() !== null && a.answerNumeric() !== '' &&
                parseFloat(a.answerNumeric()) > 0;

            answers.push({
                questionId: questionId,
                answerText: a.answerText() || null,
                answerNumeric: a.answerNumeric() || null,
                isNA: a.isNA(),
                givenPoints: a.givenPoints() ? parseFloat(a.givenPoints()) : null,
                notes: a.notes() || null,
                recommendationNotes: a.recommendationNotes() || null,
                applyPenalty: shouldApplyPenalty,
                selectedPenaltyType: shouldApplyPenalty ? penaltyType : null
            });
        });

        return {
            assignmentId: self.formData().assignmentId,
            assignmentPeriodId: self.selectedPeriodId() || null,
            answers: answers,
            notes: '',
            evaluationComment: self.evaluationComment(),
            callId: self.callId() || null,
            callDate: self.callDate() || null,
            duration: self.duration() || null,
            evaluatedOrganizationId: self.formData().checklistOrganizationId || null,
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
        // Not: Zorunluluk kontrolü kaldırıldı
        // Puanlanmamış sorular zaten puana etki etmiyor

        self.isSavingForm(true);
        self.modalErrorMessage('');
        self.formSuccessMessage('');

        var data = self.prepareData();
        var assignmentId = data.assignmentId;

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
        .then(function(newEvaluation) {
            self.formSuccessMessage(T('Evaluation.SubmitSuccess', 'Değerlendirme başarıyla tamamlandı.'));

            // Yeni degerlendirmeyi ekle veya mevcut olani guncelle
            var existingIndex = -1;
            var evaluations = self.allEvaluations();
            for (var i = 0; i < evaluations.length; i++) {
                if (evaluations[i].id === newEvaluation.id) {
                    existingIndex = i;
                    break;
                }
            }

            if (existingIndex >= 0) {
                // Mevcut degerlendirmeyi guncelle
                self.allEvaluations.splice(existingIndex, 1, newEvaluation);
            } else {
                // Yeni degerlendirme ekle
                self.allEvaluations.push(newEvaluation);
            }

            // Assignment'i tamamlandi olarak isaretle
            var assignments = self.allAssignments();
            for (var j = 0; j < assignments.length; j++) {
                if (assignments[j].id === assignmentId) {
                    assignments[j].isCompleted = true;
                    self.allAssignments.splice(j, 1, assignments[j]);
                    break;
                }
            }

            // Close modal after 1.5 seconds
            setTimeout(function() {
                self.closeEvaluateModal();
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
    // Once EnumsService'i yukle, sonra diger verileri cek
    EnumsService.load().then(function() {
        self.loadEvaluations();
    });
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
