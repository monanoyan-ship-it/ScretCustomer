// Maximum Evaluation Popup ViewModel
// Maksimum puanlama modu için değerlendirme formu
// Hesaplama: (GivenPoints / MaxPoints) * WeightPoints

var EvaluationPopupViewModel = function() {
    var self = this;
    var config = window.popupConfig || {};

    // State
    self.isLoading = ko.observable(true);
    self.isSaving = ko.observable(false);
    self.errorMessage = ko.observable('');
    self.formData = ko.observable(null);

    // Form alanları
    self.callId = ko.observable('');
    self.callDate = ko.observable('');
    self.callTime = ko.observable('');
    self.duration = ko.observable('');
    self.generalComment = ko.observable('');

    // Personel bilgileri
    self.availablePersonnel = ko.observableArray([]);
    self.evaluatedPersonnelId = ko.observable(null);
    self.evaluatedUnknownPersonnel = ko.observable('');
    self.personnelSearchText = ko.observable('');
    self.isPersonnelDropdownVisible = ko.observable(false);
    self.selectedPersonnelName = ko.observable('');

    // Filtrelenmiş personel listesi
    self.filteredPersonnel = ko.computed(function() {
        var search = self.personnelSearchText().toLowerCase().trim();
        var personnel = self.availablePersonnel();
        if (search.length < 1) return personnel.slice(0, 20);
        return personnel.filter(function(p) {
            return (p.name || '').toLowerCase().indexOf(search) === 0 ||
                   (p.sicilNo || '').toLowerCase().indexOf(search) === 0;
        }).slice(0, 20);
    });

    self.showPersonnelDropdown = function() {
        self.isPersonnelDropdownVisible(true);
    };

    self.hidePersonnelDropdownDelayed = function() {
        setTimeout(function() {
            self.isPersonnelDropdownVisible(false);
        }, 200);
    };

    self.selectPersonnel = function(personnel) {
        self.evaluatedPersonnelId(personnel.id);
        self.selectedPersonnelName(personnel.name);
        self.personnelSearchText(personnel.name);
        self.isPersonnelDropdownVisible(false);
    };

    self.clearSelectedPersonnel = function() {
        self.evaluatedPersonnelId(null);
        self.selectedPersonnelName('');
        self.personnelSearchText('');
    };

    // Sorular (formData'dan yüklenir)
    self.questions = ko.observableArray([]);

    // Cevaplar - { questionId: { score: ko.observable, earnedPoints: ko.observable, comment: ko.observable } }
    self.answers = {};

    // Puan hesaplama
    self.currentScore = ko.observable(0);
    self.maxScore = ko.observable(0);
    self.scorePercentage = ko.computed(function() {
        if (self.maxScore() === 0) return 0;
        return (self.currentScore() / self.maxScore()) * 100;
    });

    // Soru için answer objesi al/oluştur
    self.getAnswer = function(questionId) {
        if (!self.answers[questionId]) {
            self.answers[questionId] = {
                score: ko.observable(0),
                earnedPoints: ko.observable(0),
                comment: ko.observable('')
            };
        }
        return self.answers[questionId];
    };

    // Puan girişi (slider/input)
    self.getScore = function(questionId) {
        return self.getAnswer(questionId).score;
    };

    // Puan set et ve hesapla
    self.setScore = function(questionId, value, weightPoints, maxPoints) {
        var answer = self.getAnswer(questionId);
        var score = parseFloat(value) || 0;
        answer.score(score);

        // Maximum hesaplama: (GivenPoints / MaxPoints) * WeightPoints
        var earned = maxPoints > 0 ? (score / maxPoints) * weightPoints : 0;
        answer.earnedPoints(earned);

        self.calculateScore();
    };

    // Kazanılan puan (hesaplanmış)
    self.getEarnedPoints = function(questionId) {
        var answer = self.answers[questionId];
        return answer ? answer.earnedPoints() : 0;
    };

    // Yorum al (observable döndür)
    self.getComment = function(questionId) {
        return self.getAnswer(questionId).comment;
    };

    // Puan hesapla
    self.calculateScore = function() {
        var total = 0;
        var max = 0;

        self.questions().forEach(function(q) {
            // Puansız soruları atla
            if (q.scoringType === 'Unscored') return;

            // Maksimum puanı topla (weightPoints)
            max += q.weightPoints || 0;

            // Kazanılan puanı topla
            var answer = self.answers[q.id];
            if (answer && answer.earnedPoints) {
                var earned = answer.earnedPoints();
                // Penalty için negatif olabilir (kazanılandan düş)
                if (q.scoringType === 'Penalty') {
                    total -= earned;
                } else {
                    total += earned;
                }
            }
        });

        self.currentScore(Math.max(0, total)); // Negatif olamaz
        self.maxScore(max);
    };

    // Form yükle
    self.loadForm = function() {
        self.isLoading(true);
        self.errorMessage('');

        var url = '';
        if (config.assignmentId) {
            url = '/api/evaluations/form/' + config.assignmentId;
        } else if (config.evaluationId) {
            url = '/api/evaluations/form/edit/' + config.evaluationId;
        } else {
            self.errorMessage('Geçersiz parametre');
            self.isLoading(false);
            return;
        }

        fetch(url, { credentials: 'include' })
            .then(function(r) {
                if (!r.ok) throw new Error('Form yüklenemedi');
                return r.json();
            })
            .then(function(data) {
                self.formData(data);

                // penaltyGroups'tan soruları düzleştir
                var allQuestions = [];
                if (data.penaltyGroups && data.penaltyGroups.length > 0) {
                    data.penaltyGroups.forEach(function(group) {
                        if (group.questions && group.questions.length > 0) {
                            group.questions.forEach(function(q) {
                                allQuestions.push(q);
                            });
                        }
                    });
                }
                self.questions(allQuestions);

                // Mevcut değerleri yükle
                if (data.callId) self.callId(data.callId);
                if (data.callDate) self.callDate(data.callDate.split('T')[0]);
                if (data.callTime) self.callTime(data.callTime);
                if (data.duration) self.duration(data.duration);
                if (data.evaluationComment) self.generalComment(data.evaluationComment);

                // Personel listesini yükle
                self.availablePersonnel(data.availablePersonnel || []);
                if (data.evaluatedPersonnelId) {
                    self.evaluatedPersonnelId(data.evaluatedPersonnelId);
                    var selectedPersonnel = (data.availablePersonnel || []).find(function(p) { return p.id === data.evaluatedPersonnelId; });
                    if (selectedPersonnel) {
                        self.selectedPersonnelName(selectedPersonnel.name);
                        self.personnelSearchText(selectedPersonnel.name);
                    }
                }
                if (data.evaluatedUnknownPersonnel) self.evaluatedUnknownPersonnel(data.evaluatedUnknownPersonnel);

                // Mevcut cevapları yükle (düzenleme modunda)
                if (data.existingAnswers && data.existingAnswers.length > 0) {
                    data.existingAnswers.forEach(function(a) {
                        var answer = self.getAnswer(a.questionId);
                        answer.score(a.givenPoints || 0);
                        answer.earnedPoints(a.earnedPoints || 0);
                        answer.comment(a.notes || '');
                    });
                }

                self.calculateScore();
            })
            .catch(function(error) {
                console.error('Load form error:', error);
                self.errorMessage('Form yüklenirken bir hata oluştu: ' + error.message);
            })
            .finally(function() {
                self.isLoading(false);
            });
    };

    // Taslak kaydet
    self.saveDraft = function() {
        self.save(false);
    };

    // Değerlendirmeyi tamamla
    self.submitEvaluation = function() {
        self.save(true);
    };

    // Kaydet
    self.save = function(isComplete) {
        self.isSaving(true);
        self.errorMessage('');

        // Cevapları hazırla
        var answersArray = [];
        self.questions().forEach(function(q) {
            var answer = self.answers[q.id];
            if (answer) {
                answersArray.push({
                    questionId: q.id,
                    givenPoints: answer.score() || 0,
                    notes: answer.comment() || ''
                });
            }
        });

        // Validasyon
        if (!self.evaluatedPersonnelId() && !self.evaluatedUnknownPersonnel()) {
            self.errorMessage('Personel seçimi zorunludur');
            self.isSaving(false);
            toastr.error('Personel seçimi zorunludur');
            return;
        }
        if (!self.callDate()) {
            self.errorMessage('Çağrı Tarihi zorunludur');
            self.isSaving(false);
            toastr.error('Çağrı Tarihi zorunludur');
            return;
        }
        if (!self.callTime()) {
            self.errorMessage('Çağrı Saati zorunludur');
            self.isSaving(false);
            toastr.error('Çağrı Saati zorunludur');
            return;
        }
        if (!self.duration()) {
            self.errorMessage('Süre zorunludur');
            self.isSaving(false);
            toastr.error('Süre zorunludur');
            return;
        }

        var data = {
            assignmentId: config.assignmentId || self.formData()?.assignmentId,
            evaluationId: config.evaluationId || self.formData()?.evaluationId,
            callId: self.callId(),
            callDate: self.callDate() || null,
            callTime: self.callTime() || null,
            duration: self.duration() || null,
            evaluationComment: self.generalComment(),
            evaluatedPersonnelId: self.evaluatedPersonnelId() || null,
            evaluatedUnknownPersonnel: self.evaluatedUnknownPersonnel() || null,
            answers: answersArray
        };

        var url = isComplete ? '/api/evaluations/submit' : '/api/evaluations/draft';

        fetch(url, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(data),
            credentials: 'include'
        })
        .then(function(r) {
            if (!r.ok) return r.json().then(function(err) { throw new Error(err.message || 'Kaydetme hatası'); });
            return r.json();
        })
        .then(function(result) {
            toastr.success(isComplete ? 'Değerlendirme tamamlandı' : 'Taslak kaydedildi');

            // Opener'ı bilgilendir
            if (window.opener && !window.opener.closed) {
                try {
                    var vmElement = window.opener.document.getElementById('evaluations-app');
                    if (vmElement && ko.dataFor(vmElement)) {
                        ko.dataFor(vmElement).loadEvaluations();
                    }
                } catch (e) {
                    console.log('Opener refresh error:', e);
                }
            }

            if (isComplete) {
                window.close();
            }
        })
        .catch(function(error) {
            console.error('Save error:', error);
            self.errorMessage('Kaydetme hatası: ' + error.message);
            toastr.error(error.message);
        })
        .finally(function() {
            self.isSaving(false);
        });
    };

    // Başlat
    self.loadForm();
};

// Knockout binding
document.addEventListener('DOMContentLoaded', function() {
    var container = document.getElementById('evaluation-popup');
    if (container) {
        ko.applyBindings(new EvaluationPopupViewModel(), container);
    }
});
