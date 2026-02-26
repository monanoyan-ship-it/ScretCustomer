// GM Dinleme Popup ViewModel
// PopupMaximum.js'den adapte edildi - çağrı/personel alanları kaldırıldı

var GmDinlemePopupViewModel = function() {
    var self = this;
    var config = window.popupConfig || {};

    // ========================
    // STATE
    // ========================
    self.isLoading = ko.observable(true);
    self.isSavingForm = ko.observable(false);
    self.formData = ko.observable(null);

    // Ozet gorunumu
    self.isShowingSummary = ko.observable(false);
    self.summaryData = ko.observable(null);

    // Form fields
    self.evaluationComment = ko.observable('');

    // Answers dictionary (questionId -> answer observable)
    self.answers = {};

    // Computed scores
    self.totalScoreCalc = ko.observable(0);
    self.maxScoreCalc = ko.observable(0);
    self.scorePercentageCalc = ko.observable(0);
    self.yellowCardCountCalc = ko.observable(0);
    self.redCardCountCalc = ko.observable(0);
    self.scoredWeightCalc = ko.observable(0);
    self.yellowCardWeightCalc = ko.observable(0);
    self.redCardWeightCalc = ko.observable(0);

    // Helper: Generate score options array [0, 1, 2, ..., maxPoints]
    self.getScoreOptions = function(maxPoints) {
        var max = parseInt(maxPoints) || 5;
        if (max > 10) max = 10;
        var options = [];
        for (var i = 0; i <= max; i++) {
            options.push(i);
        }
        return options;
    };

    // ========================
    // ANSWER SYSTEM
    // ========================

    self.getAnswer = function(questionId, isRequired) {
        if (!self.answers[questionId]) {
            self.answers[questionId] = {
                questionId: questionId,
                answerId: ko.observable(null),
                answerText: ko.observable(''),
                answerNumeric: ko.observable(null),
                givenPoints: ko.observable(null),
                notes: ko.observable(''),
                applyPenalty: ko.observable(false),
                selectedPenaltyType: ko.observable(''),
                selectedSubCriteria: ko.observableArray([]),
                isIncluded: ko.observable(isRequired !== false)
            };

            self.answers[questionId].answerNumeric.subscribe(function(newValue) {
                if (newValue !== null && newValue !== '') {
                    self.answers[questionId].isIncluded(true);
                }
                self.calculateScores();
            });
            self.answers[questionId].answerText.subscribe(function() { self.calculateScores(); });
            self.answers[questionId].givenPoints.subscribe(function(newValue) {
                if (newValue !== null && newValue !== '') {
                    self.answers[questionId].isIncluded(true);
                }
                self.calculateScores();
            });
            self.answers[questionId].applyPenalty.subscribe(function() { self.calculateScores(); });
            self.answers[questionId].selectedPenaltyType.subscribe(function() { self.calculateScores(); });
            self.answers[questionId].isIncluded.subscribe(function(newValue) {
                if (!newValue) {
                    self.answers[questionId].answerNumeric(null);
                    self.answers[questionId].givenPoints(null);
                }
                self.calculateScores();
            });
        }
        return self.answers[questionId];
    };

    self.toggleSubCriteria = function(questionId, subCriteriaId) {
        var answer = self.getAnswer(questionId);
        var arr = answer.selectedSubCriteria();
        var idx = arr.indexOf(subCriteriaId);
        if (idx >= 0) {
            answer.selectedSubCriteria.splice(idx, 1);
        } else {
            answer.selectedSubCriteria.push(subCriteriaId);
        }
    };

    self.isSubCriteriaSelected = function(questionId, subCriteriaId) {
        var answer = self.getAnswer(questionId);
        return answer.selectedSubCriteria().indexOf(subCriteriaId) >= 0;
    };

    // ========================
    // LOAD FORM
    // ========================

    self.loadForm = function() {
        self.isLoading(true);
        self.formData(null);
        self.answers = {};
        self.isShowingSummary(false);
        self.summaryData(null);
        self.evaluationComment('');
        self.totalScoreCalc(0);
        self.maxScoreCalc(0);
        self.scorePercentageCalc(0);
        self.yellowCardCountCalc(0);
        self.redCardCountCalc(0);
        self.scoredWeightCalc(0);
        self.yellowCardWeightCalc(0);
        self.redCardWeightCalc(0);

        var url = '';
        if (config.gmAtamaId) {
            url = '/api/gm/dinleme/form/' + config.gmAtamaId;
        } else if (config.dinlemeId) {
            url = '/api/gm/dinleme/form/edit/' + config.dinlemeId;
        } else {
            toastr.error(T('Evaluation.InvalidParams', 'Geçersiz parametreler'));
            self.isLoading(false);
            return;
        }

        fetch(url, { credentials: 'include' })
            .then(function(response) {
                if (!response.ok) throw new Error(T('Evaluation.FormLoadError', 'Form yüklenemedi'));
                return response.json();
            })
            .then(function(data) {
                self.formData(data);

                // Mevcut yorum varsa yükle
                if (data.comment) self.evaluationComment(data.comment);

                // Mevcut cevapları map'le
                var existingAnswerMap = {};
                if (data.existingAnswers && data.existingAnswers.length > 0) {
                    data.existingAnswers.forEach(function(a) {
                        existingAnswerMap[a.questionId] = a;
                    });
                }

                // Soruları initialize et
                data.penaltyGroups.forEach(function(section) {
                    section.questions.forEach(function(q) {
                        var answer = self.getAnswer(q.id, q.isRequired);

                        // Penalty type otomatik set
                        if (q.penaltyType === 'YellowCard' || q.penaltyType === 'RedCard') {
                            answer.selectedPenaltyType(q.penaltyType);
                        }

                        // Mevcut cevap varsa yükle
                        var existingAnswer = existingAnswerMap[q.id];
                        if (existingAnswer) {
                            if (existingAnswer.id) answer.answerId(existingAnswer.id);
                            if (existingAnswer.answerText) answer.answerText(existingAnswer.answerText);
                            if (existingAnswer.answerNumeric !== null && existingAnswer.answerNumeric !== undefined) {
                                answer.answerNumeric(existingAnswer.answerNumeric);
                            }
                            if (existingAnswer.givenPoints) answer.givenPoints(existingAnswer.givenPoints);
                            if (existingAnswer.notes) answer.notes(existingAnswer.notes);
                            answer.applyPenalty(existingAnswer.isPenaltyApplied || false);
                            if (existingAnswer.appliedPenaltyType && existingAnswer.appliedPenaltyType !== 'None') {
                                answer.selectedPenaltyType(existingAnswer.appliedPenaltyType);
                            }
                            if (existingAnswer.selectedSubCriteriaIds && existingAnswer.selectedSubCriteriaIds.length > 0) {
                                answer.selectedSubCriteria(existingAnswer.selectedSubCriteriaIds);
                            }
                            answer.isIncluded(true);
                        } else {
                            // Yeni: Puanli ve dahil edilen sorular için varsayılan max puan
                            if (q.scoringType === 'Scored' && answer.answerNumeric() === null && answer.isIncluded()) {
                                answer.answerNumeric(q.maxPoints || 5);
                            }
                        }
                    });
                });

                self.calculateScores();
            })
            .catch(function(error) {
                console.error('Form loading error:', error);
                toastr.error(T('Evaluation.FormLoadError', 'Form yüklenirken bir hata oluştu.'));
            })
            .finally(function() {
                self.isLoading(false);
            });
    };

    // ========================
    // CALCULATE SCORES
    // ========================

    self.calculateScores = function() {
        if (!self.formData()) return;

        var total = 0;
        var max = 0;
        var yellowCards = 0;
        var redCards = 0;
        var scoredWeight = 0;
        var yellowCardWeight = 0;
        var redCardWeight = 0;

        self.formData().penaltyGroups.forEach(function(section) {
            section.questions.forEach(function(q) {
                var weight = q.weightPoints || q.points || 0;
                var answer = self.answers[q.id];

                if (q.scoringType === 'Unscored') return;

                if (q.scoringType === 'Penalty') {
                    if (q.penaltyType === 'YellowCard') {
                        yellowCardWeight += weight;
                    } else if (q.penaltyType === 'RedCard') {
                        redCardWeight += weight;
                    }
                } else {
                    if (!q.isRequired && (!answer || !answer.isIncluded())) {
                        return;
                    }
                    scoredWeight += weight;
                }

                if (!answer) return;
                if (!q.isRequired && !answer.isIncluded()) return;

                if (q.scoringType === 'Penalty') {
                    if (answer.answerNumeric() === null || answer.answerNumeric() === '') return;

                    var numericValue = parseFloat(answer.answerNumeric()) || 0;
                    var maxScore = q.maxPoints || 2;

                    if (numericValue > 0) {
                        var penaltyAmount = (numericValue / maxScore) * weight;
                        total -= penaltyAmount;
                        if (q.penaltyType === 'YellowCard') yellowCards++;
                        else if (q.penaltyType === 'RedCard') redCards++;
                    }
                    return;
                }

                // Normal scored questions
                var maxScore = q.maxPoints || 5;
                max += weight;

                if (answer.givenPoints() !== null && answer.givenPoints() !== '') {
                    total += parseFloat(answer.givenPoints()) || 0;
                } else if (answer.answerNumeric() !== null && answer.answerNumeric() !== '') {
                    var numericValue = parseFloat(answer.answerNumeric()) || 0;
                    total += (numericValue / maxScore) * weight;
                } else if (answer.answerText()) {
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
        self.scoredWeightCalc(scoredWeight);
        self.yellowCardWeightCalc(yellowCardWeight);
        self.redCardWeightCalc(redCardWeight);
    };

    // ========================
    // PREPARE DATA
    // ========================

    self.prepareData = function() {
        var answers = [];

        var questionMap = {};
        if (self.formData()) {
            self.formData().penaltyGroups.forEach(function(section) {
                section.questions.forEach(function(q) {
                    questionMap[q.id] = q;
                });
            });
        }

        Object.keys(self.answers).forEach(function(questionId) {
            var a = self.answers[questionId];
            var q = questionMap[questionId];

            var penaltyType = q && q.penaltyType && q.penaltyType !== 'None' ? q.penaltyType : null;
            var shouldApplyPenalty = q && q.scoringType === 'Penalty' &&
                a.answerNumeric() !== null && a.answerNumeric() !== '' &&
                parseFloat(a.answerNumeric()) > 0;

            var answerNumericVal = a.answerNumeric() !== null && a.answerNumeric() !== '' ? parseFloat(a.answerNumeric()) : null;
            var givenPointsVal = a.givenPoints() !== null && a.givenPoints() !== '' ? parseFloat(a.givenPoints()) : null;

            var isIncludedVal = a.isIncluded ? a.isIncluded() : true;
            if (answerNumericVal !== null || givenPointsVal !== null) {
                isIncludedVal = true;
            }

            answers.push({
                questionId: parseInt(questionId),
                answerText: a.answerText() || null,
                answerNumeric: answerNumericVal !== null ? Math.round(answerNumericVal) : null,
                givenPoints: givenPointsVal,
                notes: a.notes() || null,
                applyPenalty: shouldApplyPenalty,
                selectedPenaltyType: shouldApplyPenalty ? penaltyType : null,
                selectedSubCriteriaIds: a.selectedSubCriteria ? a.selectedSubCriteria() : [],
                isIncluded: isIncludedVal
            });
        });

        return {
            dinlemeId: self.formData().dinlemeId || null,
            gmAtamaId: self.formData().gmAtamaId,
            comment: self.evaluationComment() || null,
            answers: answers
        };
    };

    // ========================
    // SAVE DRAFT
    // ========================

    self.saveDraft = function() {
        self.isSavingForm(true);
        var data = self.prepareData();

        fetch('/api/gm/dinleme/draft', {
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
            if (result.answers) {
                result.answers.forEach(function(a) {
                    var qId = String(a.questionId);
                    if (self.answers[qId]) {
                        self.answers[qId].answerId(a.id);
                    }
                });
            }
            toastr.success(T('Evaluation.DraftSaved', 'Taslak başarıyla kaydedildi.'));
            self.notifyOpener();
            window.close();
        })
        .catch(function(error) {
            console.error('Draft save error:', error);
            toastr.error(T('Evaluation.DraftSaveError', 'Taslak kaydedilirken bir hata oluştu.'));
        })
        .finally(function() {
            self.isSavingForm(false);
        });
    };

    // ========================
    // SHOW SUMMARY
    // ========================

    self.showSummary = function() {
        var answersForSummary = [];
        if (self.formData()) {
            self.formData().penaltyGroups.forEach(function(section) {
                section.questions.forEach(function(q) {
                    var answer = self.answers[q.id];
                    if (!answer) return;

                    var answerNumeric = answer.answerNumeric();
                    var maxPoints = q.maxPoints || 5;
                    var weightPoints = q.weightPoints || 0;
                    var earnedPoints = 0;

                    if (q.scoringType === 'Scored' && answerNumeric !== null && answerNumeric !== '') {
                        earnedPoints = (parseFloat(answerNumeric) / maxPoints) * weightPoints;
                    } else if (q.scoringType === 'Penalty' && answerNumeric !== null && answerNumeric !== '' && parseFloat(answerNumeric) > 0) {
                        earnedPoints = -((parseFloat(answerNumeric) / maxPoints) * weightPoints);
                    }

                    var selectedSubCriteriaNames = [];
                    if (answer.selectedSubCriteria && answer.selectedSubCriteria().length > 0 && q.subCriteria) {
                        answer.selectedSubCriteria().forEach(function(scId) {
                            var sc = q.subCriteria.find(function(s) { return s.id === scId; });
                            if (sc) selectedSubCriteriaNames.push(sc.description);
                        });
                    }

                    answersForSummary.push({
                        groupName: section.name || section.title || '-',
                        questionText: q.text,
                        scoringType: q.scoringType,
                        penaltyType: q.penaltyType,
                        maxPoints: maxPoints,
                        weightPoints: weightPoints,
                        answerNumeric: answerNumeric,
                        earnedPoints: earnedPoints,
                        notes: answer.notes ? answer.notes() : '',
                        selectedSubCriteria: selectedSubCriteriaNames
                    });
                });
            });
        }

        var atamaInfo = self.formData() ? self.formData().atamaInfo : {};

        self.summaryData({
            totalScore: self.totalScoreCalc(),
            maxScore: self.maxScoreCalc(),
            scorePercentage: self.scorePercentageCalc(),
            yellowCardCount: self.yellowCardCountCalc(),
            redCardCount: self.redCardCountCalc(),
            scoredWeight: self.scoredWeightCalc(),
            yellowCardWeight: self.yellowCardWeightCalc(),
            redCardWeight: self.redCardWeightCalc(),
            hedefFirmaAdi: atamaInfo.hedefFirmaAdi || '-',
            arayanAdi: atamaInfo.arayanAdi || '-',
            aramaTarihi: atamaInfo.aramaTarihi ? new Date(atamaInfo.aramaTarihi).toLocaleDateString('tr-TR') : '-',
            comment: self.evaluationComment() || '',
            answers: answersForSummary
        });
        self.isShowingSummary(true);
    };

    self.backToForm = function() {
        self.isShowingSummary(false);
    };

    // ========================
    // CONFIRM SUBMIT
    // ========================

    self.confirmSubmit = function() {
        self.isSavingForm(true);
        var data = self.prepareData();

        fetch('/api/gm/dinleme/submit', {
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
            toastr.success(T('GmDinleme.SubmitSuccess', 'Dinleme değerlendirmesi başarıyla tamamlandı.'));
            self.notifyOpener();
            window.close();
        })
        .catch(function(error) {
            console.error('Submit error:', error);
            toastr.error(T('Evaluation.SubmitError', 'Değerlendirme gönderilirken bir hata oluştu.'));
        })
        .finally(function() {
            self.isSavingForm(false);
        });
    };

    // ========================
    // POPUP SPECIFIC
    // ========================

    self.notifyOpener = function() {
        if (!window.opener || window.opener.closed) return;
        try {
            var aramaEl = window.opener.document.getElementById('aramalarim-app');
            if (aramaEl && ko.dataFor(aramaEl)) {
                var vm = ko.dataFor(aramaEl);
                if (vm.loadDinlemelerim) vm.loadDinlemelerim();
                if (vm.loadAtamalar) vm.loadAtamalar();
            }
            // Takip sayfasi
            var takipEl = window.opener.document.getElementById('takip-app');
            if (takipEl && ko.dataFor(takipEl)) {
                var takipVm = ko.dataFor(takipEl);
                if (takipVm.loadDinlemeler) takipVm.loadDinlemeler();
            }
            // Evaluations sayfasi
            var evalEl = window.opener.document.getElementById('evaluations-app');
            if (evalEl && ko.dataFor(evalEl)) {
                var evalVm = ko.dataFor(evalEl);
                if (evalVm.loadGmDinlemelerim) evalVm.loadGmDinlemelerim();
            }
        } catch (e) {
            console.log('Opener refresh error:', e);
        }
    };

    // ========================
    // INITIALIZE
    // ========================
    self.loadForm();
};

// Translation keys
var TRANSLATION_KEYS = [
    'Evaluation.InvalidParams',
    'Evaluation.FormLoadError',
    'Evaluation.DraftSaveError',
    'Evaluation.DraftSaved',
    'Evaluation.SubmitError',
    'GmDinleme.SubmitSuccess'
];

// Apply bindings when DOM is ready
document.addEventListener('DOMContentLoaded', function() {
    Localization.loadKeys(TRANSLATION_KEYS).then(function() {
        var container = document.getElementById('gm-dinleme-popup');
        if (container) {
            ko.applyBindings(new GmDinlemePopupViewModel(), container);
        }

        var tooltipTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="tooltip"]'));
        tooltipTriggerList.map(function (tooltipTriggerEl) {
            return new bootstrap.Tooltip(tooltipTriggerEl);
        });
    });
});
