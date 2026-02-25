// CriteriaTotal Evaluation Popup ViewModel
// Kriter Toplam puanlama modu için değerlendirme formu

var EvaluationPopupViewModel = function() {
    var self = this;
    var config = window.popupConfig || {};

    // State
    self.isLoading = ko.observable(true);
    self.isSaving = ko.observable(false);
    self.errorMessage = ko.observable('');
    self.formData = ko.observable(null);
    self.currentUserId = ko.observable(null); // Mevcut kullanıcı ID (User veya CustomerPersonnel)

    // Form alanları
    self.callId = ko.observable('');
    self.callDate = ko.observable('');
    self.callTime = ko.observable('');
    self.duration = ko.observable('');
    self.generalComment = ko.observable('');

    // Dönem seçimi
    self.availablePeriods = ko.observableArray([]);
    self.selectedPeriodId = ko.observable(null);

    // Açıklamalar (wrapper object pattern - KnockoutJS foreach write-back için)
    self.descriptions = ko.observableArray([{text: ko.observable('')}]);
    self.pastDescriptions = ko.observableArray([]);

    self.addDescription = function() {
        self.descriptions.push({text: ko.observable('')});
    };

    self.removeDescription = function(index) {
        if (self.descriptions().length > 1) {
            self.descriptions.splice(index, 1);
        }
    };

    // Geçmiş açıklamalar (autocomplete)
    self.loadPastDescriptions = function() {
        fetch('/api/evaluations/past-descriptions', { credentials: 'include' })
            .then(function(r) { return r.ok ? r.json() : []; })
            .then(function(data) { self.pastDescriptions(data); })
            .catch(function() { /* ignore */ });
    };
    self.loadPastDescriptions();

    // CallId kontrolü
    self.callIdExists = ko.observable(false);
    self.isCheckingCallId = ko.observable(false);
    var callIdCheckTimeout = null;

    self.callId.subscribe(function(newValue) {
        if (callIdCheckTimeout) clearTimeout(callIdCheckTimeout);
        if (!newValue || newValue.length < 3) {
            self.callIdExists(false);
            return;
        }
        callIdCheckTimeout = setTimeout(function() {
            self.checkCallIdExists(newValue);
        }, 500);
    });

    self.checkCallIdExists = function(callId) {
        var formData = self.formData();
        if (!formData) return;

        self.isCheckingCallId(true);
        var evaluationId = config.evaluationId || formData.evaluationId;
        var projectId = formData.projectId;

        var url = '/api/evaluations/check-call-id?callId=' + encodeURIComponent(callId) +
            '&projectId=' + (projectId || 0);
        if (evaluationId) url += '&evaluationId=' + evaluationId;

        fetch(url, { credentials: 'include' })
            .then(function(r) { return r.json(); })
            .then(function(data) {
                self.callIdExists(data.exists === true);
            })
            .catch(function() {
                self.callIdExists(false);
            })
            .finally(function() {
                self.isCheckingCallId(false);
            });
    };

    // Personel bilgileri
    self.availablePersonnel = ko.observableArray([]);
    self.evaluatedPersonnelId = ko.observable(null);
    self.evaluatedUnknownPersonnel = ko.observable('');
    self.personnelSearchText = ko.observable('');
    self.isPersonnelDropdownVisible = ko.observable(false);
    self.selectedPersonnelName = ko.observable('');
    self.isLoadingPersonnel = ko.observable(false);

    // Yeni personel modu
    self.isNewPersonnelMode = ko.observable(false);
    self.newPersonnelFirstName = ko.observable('');
    self.newPersonnelLastName = ko.observable('');

    self.enableNewPersonnelMode = function() {
        self.isNewPersonnelMode(true);
        self.evaluatedPersonnelId(null);
        self.selectedPersonnelName('');
        self.personnelSearchText('');
    };

    self.cancelNewPersonnelMode = function() {
        self.isNewPersonnelMode(false);
        self.newPersonnelFirstName('');
        self.newPersonnelLastName('');
    };

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
    // Gruplar (görüntü için)
    self.penaltyGroups = ko.observableArray([]);

    // Cevaplar - { questionId: { selectedOptionId: ko.observable, points: ko.observable, comment: ko.observable } }
    self.answers = {};

    // Puan hesaplama
    self.currentScore = ko.observable(0);
    self.maxScore = ko.observable(0);
    self.scorePercentage = ko.computed(function() {
        if (self.maxScore() === 0) return 0;
        return (self.currentScore() / self.maxScore()) * 100;
    });

    // Summary View
    self.isShowingSummary = ko.observable(false);
    self.summaryData = ko.observable(null);

    // Validasyon hatası olan soru ID'leri (formda kırmızı border göstermek için)
    self.invalidQuestionIds = ko.observableArray([]);

    // Soru için answer objesi al/oluştur
    self.getAnswer = function(questionId) {
        if (!self.answers[questionId]) {
            self.answers[questionId] = {
                selectedOptionId: ko.observable(null),
                points: ko.observable(0),
                comment: ko.observable(''),
                isNotApplicable: ko.observable(false)
            };
            // Seçim değiştiğinde puanları yeniden hesapla
            self.answers[questionId].selectedOptionId.subscribe(function() {
                self.calculateScore();
            });
            // Gerekmedi işaretlendiğinde subcriteria seçimini temizle ve validasyon hatasını kaldır
            (function(qId) {
                self.answers[qId].isNotApplicable.subscribe(function(newValue) {
                    if (newValue) {
                        self.answers[qId].points(0);
                        self.answers[qId].selectedOptionId(null);
                    }
                    self.invalidQuestionIds.remove(qId);
                });
            })(questionId);
        }
        return self.answers[questionId];
    };

    // Seçenek seçildiğinde (zorunlu olmayan sorularda toggle davranışı)
    self.selectOption = function(questionId, optionId, points) {
        var answer = self.getAnswer(questionId);

        // Aynı seçenek tekrar tıklanırsa ve soru zorunlu değilse → seçimi temizle
        var question = self.questions().find(function(q) { return q.id === questionId; });
        if (answer.selectedOptionId() === optionId && question && !question.isRequired) {
            answer.points(0);
            answer.selectedOptionId(null);
            return;
        }

        // Subcriteria seçildiğinde "Gerekmedi" temizle ve validasyon hatasını kaldır
        answer.isNotApplicable(false);
        self.invalidQuestionIds.remove(questionId);

        // ÖNCE points'i set et, SONRA selectedOptionId'yi (subscription calculateScore'u tetikler)
        answer.points(points);
        answer.selectedOptionId(optionId);
    };

    // Seçilen seçeneği al (observable döndür)
    self.getSelectedOption = function(questionId) {
        return self.getAnswer(questionId).selectedOptionId;
    };

    // Yorum al (observable döndür)
    self.getComment = function(questionId) {
        return self.getAnswer(questionId).comment;
    };

    // Gerekmedi durumunu al (observable döndür)
    self.getIsNotApplicable = function(questionId) {
        return self.getAnswer(questionId).isNotApplicable;
    };

    // toggleNotApplicable artık subscribe ile yönetiliyor (getAnswer içinde)

    // Puan hesapla (orantılı)
    // Normal sorular: orantılı hesaplama (earned / rawMax × maxTotal)
    // Sarı/Kırmızı kart: rawMax'a eklenmez, seçilen puan earned'dan düşülür, skor minimum 0
    self.calculateScore = function() {
        var earnedPoints = 0;  // Normal soruların kazanılan puanları
        var rawMax = 0;        // Normal soruların max puanları toplamı
        var penaltyDeduction = 0; // Ceza puanları toplamı

        self.questions().forEach(function(q) {
            if (!q.subCriteria || q.subCriteria.length === 0) return;

            var answer = self.answers[q.id];
            var selectedOptionId = answer ? answer.selectedOptionId() : null;

            if (!selectedOptionId) return;

            var pts = answer.points();
            pts = (pts !== undefined && pts !== null) ? pts : 0;

            if (q.penaltyType && q.penaltyType !== 'None') {
                // Sarı/Kırmızı kart: rawMax'a eklenmez, seçilen puan ceza olarak düşülür
                penaltyDeduction += Math.abs(pts);
            } else {
                // Normal soru: orantılı hesaplamaya dahil
                var maxOptionPoints = Math.max.apply(null, q.subCriteria.map(function(sc) {
                    return sc.weightPoints !== undefined ? sc.weightPoints : 0;
                }));
                rawMax += maxOptionPoints;
                earnedPoints += pts;
            }
        });

        // maxTotalPoints checklist'ten gelir (varsayılan 100)
        var maxTotalPoints = self.formData() ? (self.formData().maxTotalPoints || 100) : 100;
        self.maxScore(maxTotalPoints);

        // Orantılı hesaplama: ((kazanılan - ceza) / ham max) × maxTotalPoints, minimum 0
        if (rawMax > 0) {
            var score = ((earnedPoints - penaltyDeduction) / rawMax) * maxTotalPoints;
            self.currentScore(Math.max(0, score));
        } else {
            self.currentScore(0);
        }
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

        // Önce kullanıcı bilgisini al
        fetch('/api/auth/me', { credentials: 'include' })
            .then(function(response) { return response.json(); })
            .then(function(userData) {
                self.currentUserId(parseInt(userData.id) || null);
            })
            .catch(function() {
                console.warn('Could not fetch current user');
            });

        fetch(url, { credentials: 'include' })
            .then(function(r) {
                if (!r.ok) throw new Error('Form yüklenemedi');
                return r.json();
            })
            .then(function(data) {
                self.formData(data);

                // penaltyGroups'u sakla (görüntü için) ve soruları düzleştir (hesaplama için)
                var allQuestions = [];
                if (data.penaltyGroups && data.penaltyGroups.length > 0) {
                    self.penaltyGroups(data.penaltyGroups);
                    data.penaltyGroups.forEach(function(group) {
                        if (group.questions && group.questions.length > 0) {
                            group.questions.forEach(function(q) {
                                allQuestions.push(q);
                            });
                        }
                    });
                }
                self.questions(allQuestions);

                // Dönem listesini yükle
                self.availablePeriods(data.availablePeriods || []);
                if (data.selectedPeriodId) self.selectedPeriodId(data.selectedPeriodId);

                // Mevcut değerleri yükle
                if (data.callId) self.callId(data.callId);
                if (data.callDate) self.callDate(data.callDate.split('T')[0]);
                if (data.callTime) self.callTime(data.callTime);
                if (data.duration) self.duration(data.duration);
                if (data.evaluationComment) self.generalComment(data.evaluationComment);

                // Açıklamaları yükle
                if (data.descriptions && data.descriptions.length > 0) {
                    self.descriptions(data.descriptions.map(function(d) { return {text: ko.observable(d)}; }));
                }

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
                if (data.evaluatedUnknownPersonnel) {
                    self.evaluatedUnknownPersonnel(data.evaluatedUnknownPersonnel);
                    self.isNewPersonnelMode(true);
                    // Ad soyad ayır
                    var parts = data.evaluatedUnknownPersonnel.split(' ');
                    if (parts.length >= 2) {
                        self.newPersonnelFirstName(parts[0]);
                        self.newPersonnelLastName(parts.slice(1).join(' '));
                    } else {
                        self.newPersonnelFirstName(data.evaluatedUnknownPersonnel);
                    }
                }

                // Mevcut cevapları yükle (düzenleme modunda)
                var hasExistingAnswers = data.existingAnswers && data.existingAnswers.length > 0;
                if (hasExistingAnswers) {
                    data.existingAnswers.forEach(function(a) {
                        var answer = self.getAnswer(a.questionId);
                        if (a.selectedSubCriteriaIds && a.selectedSubCriteriaIds.length > 0) {
                            answer.selectedOptionId(a.selectedSubCriteriaIds[0]);
                        }
                        answer.points(a.earnedPoints || 0);
                        answer.comment(a.notes || '');
                        answer.isNotApplicable(a.isNotApplicable || false);
                    });
                }

                // Zorunlu sorular için en yüksek puanlı seçeneği otomatik seç (yeni değerlendirmede)
                if (!hasExistingAnswers) {
                    allQuestions.forEach(function(q) {
                        if (q.isRequired && q.subCriteria && q.subCriteria.length > 0) {
                            // En yüksek puanlı seçeneği bul
                            var maxOption = q.subCriteria.reduce(function(best, current) {
                                var currentPoints = current.weightPoints !== undefined ? current.weightPoints : 0;
                                var bestPoints = best.weightPoints !== undefined ? best.weightPoints : 0;
                                return currentPoints > bestPoints ? current : best;
                            }, q.subCriteria[0]);

                            if (maxOption) {
                                self.selectOption(q.id, maxOption.id, maxOption.weightPoints || 0);
                            }
                        }
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
                // Input mask'ları başlat (DOM güncellenince)
                setTimeout(function() {
                    self.initTimePickers();
                }, 100);
            });
    };

    // Input mask başlatma
    self.initTimePickers = function() {
        if (typeof Inputmask !== 'undefined') {
            Inputmask('99:99', { insertMode: false }).mask('.time-mask');
            Inputmask('99:99:99', { insertMode: false }).mask('.duration-mask');
        }

        // Süre varsayılan olarak 00: ile başlasın
        if (!self.duration()) {
            self.duration('00:');
        }
    };

    // Taslak kaydet
    self.saveDraft = function() {
        self.save(false);
    };

    // Değerlendirmeyi tamamla
    self.submitEvaluation = function() {
        self.save(true);
    };

    // Özet göster
    self.showSummary = function() {
        self.errorMessage('');

        // Yeni personel modunda ad soyad'ı birleştir
        var personnelName = '';
        if (self.isNewPersonnelMode()) {
            var firstName = self.newPersonnelFirstName().trim();
            var lastName = self.newPersonnelLastName().trim();
            if (firstName && lastName) {
                personnelName = firstName + ' ' + lastName;
            }
        } else if (self.selectedPersonnelName()) {
            personnelName = self.selectedPersonnelName();
        }

        // Validasyon
        if (!self.evaluatedPersonnelId() && !personnelName) {
            toastr.error('Personel seçimi zorunludur');
            return;
        }
        if (!self.callDate()) {
            toastr.error('Çağrı Tarihi zorunludur');
            return;
        }
        if (!self.callTime() || self.callTime().indexOf('_') >= 0 || !/^\d{2}:\d{2}$/.test(self.callTime())) {
            toastr.error('Çağrı Saati zorunludur (SS:DD formatında giriniz)');
            return;
        }
        if (!self.duration() || self.duration().indexOf('_') >= 0 || !/^\d{2}:\d{2}:\d{2}$/.test(self.duration()) || parseInt(self.duration().replace(/\D/g, ''), 10) === 0) {
            toastr.error('Süre zorunludur (SS:DD:SN formatında giriniz)');
            return;
        }

        // Zorunlu olmayan + penalty=None sorularda: subcriteria ya da "Gerekmedi" seçilmiş olmalı
        var incompleteOptional = [];
        var invalidIds = [];
        self.questions().forEach(function(q) {
            if (!q.isRequired && q.penaltyType === 'None') {
                var answer = self.answers[q.id];
                var selectedId = answer ? answer.selectedOptionId() : null;
                var isNA = answer ? answer.isNotApplicable() : false;
                if (!selectedId && !isNA) {
                    incompleteOptional.push(q.order + '. ' + q.text);
                    invalidIds.push(q.id);
                }
            }
        });

        if (incompleteOptional.length > 0) {
            self.invalidQuestionIds(invalidIds);
            var msg = incompleteOptional.length + ' opsiyonel soruda seçim yapılmamış veya "Gerekmedi" işaretlenmemiş:\n';
            msg += incompleteOptional.join('\n');
            toastr.error(msg, '', { timeOut: 10000 });

            // İlk hatalı soruya scroll
            var firstInvalidEl = document.querySelector('[data-question-id="' + invalidIds[0] + '"]');
            if (firstInvalidEl) {
                firstInvalidEl.scrollIntoView({ behavior: 'smooth', block: 'center' });
            }
            return;
        }
        self.invalidQuestionIds([]);

        // CallId duplicate kontrolü (async)
        var callIdVal = self.callId();
        if (callIdVal && callIdVal.length >= 3) {
            var projectId = self.formData() ? self.formData().projectId : null;
            var evaluationId = config.evaluationId || (self.formData() ? self.formData().evaluationId : null);
            var checkUrl = '/api/evaluations/check-call-id?callId=' + encodeURIComponent(callIdVal) +
                '&projectId=' + (projectId || 0);
            if (evaluationId) checkUrl += '&evaluationId=' + evaluationId;

            fetch(checkUrl, { credentials: 'include' })
                .then(function(r) { return r.json(); })
                .then(function(data) {
                    if (data.exists === true) {
                        toastr.error('Bu Çağrı ID (' + callIdVal + ') daha önce kaydedilmiş.');
                        self.callIdExists(true);
                    } else {
                        self.callIdExists(false);
                        self._buildAndShowSummary(personnelName);
                    }
                })
                .catch(function() {
                    // API hatası durumunda devam et
                    self._buildAndShowSummary(personnelName);
                });
            return;
        }

        self._buildAndShowSummary(personnelName);
    };

    // Summary oluşturma (showSummary'den ayrıldı - callId kontrolü sonrası çağrılır)
    self._buildAndShowSummary = function(personnelName) {
        // Cevapları hazırla
        var answersArray = [];
        var index = 1;
        self.questions().forEach(function(q) {
            var answer = self.answers[q.id];
            var selectedId = answer ? answer.selectedOptionId() : null;
            var isAnswered = selectedId !== null;
            var answerText = '';
            var points = 0;

            if (isAnswered && q.subCriteria) {
                var selectedOption = q.subCriteria.find(function(sc) { return sc.id === selectedId; });
                if (selectedOption) {
                    answerText = selectedOption.description;
                    points = selectedOption.weightPoints || 0;
                }
            }

            var isNA = answer ? answer.isNotApplicable() : false;

            answersArray.push({
                index: index++,
                questionText: q.text,
                isRequired: q.isRequired,
                isAnswered: isAnswered,
                isNotApplicable: isNA,
                answerText: answerText,
                points: points,
                comment: answer ? answer.comment() : ''
            });
        });

        // Açıklamaları string array'e çevir
        var descriptionsArray = self.descriptions().map(function(d) {
            return d.text();
        }).filter(function(d) { return d && d.trim(); });

        // Summary data oluştur
        self.summaryData({
            evaluatedPersonnelName: personnelName,
            callId: self.callId(),
            callDate: self.callDate(),
            callTime: self.callTime(),
            duration: self.duration(),
            descriptions: descriptionsArray,
            evaluationComment: self.generalComment(),
            currentScore: self.currentScore(),
            maxScore: self.maxScore(),
            scorePercentage: self.scorePercentage(),
            answers: answersArray
        });

        self.isShowingSummary(true);
    };

    // Forma geri dön
    self.backToForm = function() {
        self.isShowingSummary(false);
        self.summaryData(null);
    };

    // Özetten onayla ve kaydet
    self.confirmSubmit = function() {
        // Zorunlu soruların cevaplanıp cevaplanmadığını kontrol et
        var unansweredRequired = [];
        self.questions().forEach(function(q, idx) {
            if (q.isRequired) {
                var answer = self.answers[q.id];
                var selectedId = answer ? answer.selectedOptionId() : null;
                if (!selectedId) {
                    unansweredRequired.push((idx + 1) + '. ' + q.text);
                }
            }
        });

        if (unansweredRequired.length > 0) {
            toastr.error('Zorunlu sorular cevaplanmalıdır: ' + unansweredRequired[0]);
            self.isShowingSummary(false);
            return;
        }

        self.save(true);
    };

    // Kaydet
    self.save = function(isComplete) {
        self.isSaving(true);
        self.errorMessage('');

        // Cevapları hazırla - TÜM soruları gönder (raporlama için cevaplanmayan sorular da kaydedilmeli)
        var answersArray = [];
        self.questions().forEach(function(q) {
            var answer = self.answers[q.id];
            var selectedId = answer ? answer.selectedOptionId() : null;
            var isNA = answer ? answer.isNotApplicable() : false;

            answersArray.push({
                questionId: q.id,
                selectedSubCriteriaIds: selectedId ? [selectedId] : [],
                notes: answer ? (answer.comment() || '') : '',
                isNotApplicable: isNA
            });
        });

        // Yeni personel modunda ad soyad'ı birleştir
        var unknownPersonnel = '';
        if (self.isNewPersonnelMode()) {
            var firstName = self.newPersonnelFirstName().trim();
            var lastName = self.newPersonnelLastName().trim();
            if (firstName && lastName) {
                unknownPersonnel = firstName + ' ' + lastName;
            }
        }

        // Validasyon
        if (!self.evaluatedPersonnelId() && !unknownPersonnel) {
            self.errorMessage('Personel seçimi zorunludur');
            self.isSaving(false);
            toastr.error('Personel seçimi zorunludur');
            return;
        }
        if (!self.callId() || !self.callId().trim()) {
            self.errorMessage('Çağrı ID zorunludur');
            self.isSaving(false);
            toastr.error('Çağrı ID zorunludur');
            return;
        }
        if (!self.callDate()) {
            self.errorMessage('Çağrı Tarihi zorunludur');
            self.isSaving(false);
            toastr.error('Çağrı Tarihi zorunludur');
            return;
        }
        if (!self.callTime() || self.callTime().indexOf('_') >= 0 || !/^\d{2}:\d{2}$/.test(self.callTime())) {
            self.errorMessage('Çağrı Saati zorunludur (SS:DD formatında giriniz)');
            self.isSaving(false);
            toastr.error('Çağrı Saati zorunludur (SS:DD formatında giriniz)');
            return;
        }
        if (!self.duration() || self.duration().indexOf('_') >= 0 || !/^\d{2}:\d{2}:\d{2}$/.test(self.duration()) || parseInt(self.duration().replace(/\D/g, ''), 10) === 0) {
            self.errorMessage('Süre zorunludur (SS:DD:SN formatında giriniz)');
            self.isSaving(false);
            toastr.error('Süre zorunludur (SS:DD:SN formatında giriniz)');
            return;
        }

        // Açıklamaları string array'e çevir
        var descriptionsArray = self.descriptions().map(function(d) {
            return d.text();
        }).filter(function(d) { return d && d.trim(); });

        // isInternal parametresine göre evaluator ID'yi belirle
        var evaluatorId = null;
        var evaluatorCustomerPersonnelId = null;
        if (config.isInternal) {
            evaluatorCustomerPersonnelId = self.currentUserId();
        } else {
            evaluatorId = self.currentUserId();
        }

        var data = {
            projectId: self.formData()?.projectId,
            assignmentId: config.assignmentId || self.formData()?.assignmentId,
            evaluationId: config.evaluationId || self.formData()?.evaluationId,
            evaluatorId: evaluatorId,
            evaluatorCustomerPersonnelId: evaluatorCustomerPersonnelId,
            periodId: self.selectedPeriodId() || null,
            callId: self.callId(),
            callDate: self.callDate() || null,
            callTime: self.callTime() || null,
            duration: self.duration() || null,
            descriptions: descriptionsArray,
            evaluationComment: self.generalComment(),
            evaluatedPersonnelId: self.evaluatedPersonnelId() || null,
            evaluatedUnknownPersonnel: unknownPersonnel || null,
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

            // Opener'ı bilgilendir (filtreleri bozmadan)
            self.notifyOpener();
            window.close();
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

    // Opener pencereyi bilgilendir (filtreleri bozmadan)
    self.notifyOpener = function() {
        if (!window.opener || window.opener.closed) return;
        try {
            // /Evaluations sayfası
            var evalEl = window.opener.document.getElementById('evaluations-app');
            if (evalEl && ko.dataFor(evalEl) && ko.dataFor(evalEl).loadEvaluations) {
                ko.dataFor(evalEl).loadEvaluations();
            }
            // /GolgeMusteri/Aramalarim sayfası
            var aramaEl = window.opener.document.getElementById('aramalarim-app');
            if (aramaEl && ko.dataFor(aramaEl)) {
                var vm = ko.dataFor(aramaEl);
                if (vm.loadAtamalar) vm.loadAtamalar();
                if (vm.loadTamamlananAramalar) vm.loadTamamlananAramalar();
            }
        } catch (e) {
            console.log('Opener refresh error:', e);
        }
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
