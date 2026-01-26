// Maximum Evaluation Popup ViewModel
// Index.js modal formatına uyumlu - Likert butonları, gruplu sorular, alt kriterler

var EvaluationPopupViewModel = function() {
    var self = this;
    var config = window.popupConfig || {};

    // State
    self.isLoading = ko.observable(true);
    self.isSaving = ko.observable(false);
    self.isSavingForm = ko.observable(false);
    self.errorMessage = ko.observable('');
    self.formData = ko.observable(null);

    // Summary View
    self.isShowingSummary = ko.observable(false);
    self.summaryData = ko.observable(null);

    // Score observables (for summary)
    self.totalScoreCalc = ko.observable(0);
    self.maxScoreCalc = ko.observable(0);
    self.scorePercentageCalc = ko.observable(0);
    self.yellowCardCountCalc = ko.observable(0);
    self.redCardCountCalc = ko.observable(0);
    // Ağırlık grupları
    self.scoredWeightCalc = ko.observable(0);
    self.yellowCardWeightCalc = ko.observable(0);
    self.redCardWeightCalc = ko.observable(0);

    // Form alanları
    self.callId = ko.observable('');
    self.callIdExists = ko.observable(false);
    self.isCheckingCallId = ko.observable(false);
    self.callDate = ko.observable('');
    self.callTime = ko.observable('');
    self.duration = ko.observable('');
    self.controlTime = ko.observable('');
    self.evaluationComment = ko.observable('');

    // Açıklamalar (descriptions)
    self.descriptions = ko.observableArray([ko.observable('')]);

    self.addDescription = function() {
        self.descriptions.push(ko.observable(''));
    };

    self.removeDescription = function(index) {
        if (self.descriptions().length > 1) {
            self.descriptions.splice(index, 1);
        }
    };

    // Dönem bilgileri
    self.availablePeriods = ko.observableArray([]);
    self.selectedPeriodId = ko.observable(null);

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
            return (p.name || '').toLowerCase().indexOf(search) >= 0 ||
                   (p.sicilNo || '').toLowerCase().indexOf(search) >= 0;
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

    // ========================
    // ATTACHMENTS
    // ========================
    self.uploadedAttachments = ko.observableArray([]);
    self.pendingAttachments = ko.observableArray([]);
    self.isUploadingFile = ko.observable(false);

    // Helper: Format file size
    function formatFileSize(bytes) {
        if (bytes === 0) return '0 B';
        var k = 1024;
        var sizes = ['B', 'KB', 'MB', 'GB'];
        var i = Math.floor(Math.log(bytes) / Math.log(k));
        return parseFloat((bytes / Math.pow(k, i)).toFixed(2)) + ' ' + sizes[i];
    }

    // Dosya seçildiğinde
    self.selectAttachment = function(data, event) {
        var files = event.target.files;
        if (!files || files.length === 0) return;

        for (var i = 0; i < files.length; i++) {
            var file = files[i];
            self.pendingAttachments.push({
                file: file,
                name: file.name,
                size: file.size,
                sizeDisplay: formatFileSize(file.size)
            });
        }

        event.target.value = '';
        toastr.info('Dosyalar seçildi. Form kaydedildiğinde yüklenecek.');
    };

    // Bekleyen dosyayı kaldır
    self.removePendingAttachment = function(attachment) {
        self.pendingAttachments.remove(attachment);
    };

    // Yüklenmiş dosyayı sil
    self.deleteAttachment = function(attachment) {
        if (!confirm('Dosyayı silmek istediğinize emin misiniz?')) return;

        fetch('/api/evaluations/attachments/' + attachment.id, {
            method: 'DELETE',
            credentials: 'include'
        })
        .then(function(response) {
            if (!response.ok) throw new Error('Delete failed');
            return response.json();
        })
        .then(function() {
            self.uploadedAttachments.remove(attachment);
            toastr.success('Dosya silindi');
        })
        .catch(function(error) {
            console.error('Delete error:', error);
            toastr.error('Dosya silinirken hata oluştu');
        });
    };

    // Dosya indir
    self.downloadAttachment = function(attachment) {
        window.open('/api/evaluations/attachments/' + attachment.id + '/download', '_blank');
    };

    // Bekleyen dosyaları yükle
    self.uploadPendingAttachments = function(evaluationId) {
        var pending = self.pendingAttachments();
        if (pending.length === 0) return Promise.resolve();

        self.isUploadingFile(true);

        var uploadPromises = pending.map(function(attachment) {
            var formData = new FormData();
            formData.append('file', attachment.file);

            return fetch('/api/evaluations/' + evaluationId + '/attachments', {
                method: 'POST',
                credentials: 'include',
                body: formData
            })
            .then(function(response) {
                if (!response.ok) throw new Error('Upload failed');
                return response.json();
            })
            .then(function(result) {
                self.uploadedAttachments.push({
                    id: result.attachmentId,
                    fileName: result.fileName,
                    fileSize: result.fileSize,
                    sizeDisplay: formatFileSize(result.fileSize)
                });
            })
            .catch(function(error) {
                console.error('Upload error:', error);
                toastr.error('Dosya yüklenemedi: ' + attachment.name);
            });
        });

        return Promise.all(uploadPromises).then(function() {
            self.pendingAttachments([]);
        }).finally(function() {
            self.isUploadingFile(false);
        });
    };

    // Mevcut dosyaları yükle
    self.loadExistingAttachments = function(evaluationId) {
        if (!evaluationId) return;

        fetch('/api/evaluations/' + evaluationId + '/attachments', { credentials: 'include' })
            .then(function(response) {
                if (!response.ok) throw new Error('Load failed');
                return response.json();
            })
            .then(function(attachments) {
                self.uploadedAttachments(attachments.map(function(a) {
                    return {
                        id: a.id,
                        fileName: a.fileName,
                        fileSize: a.fileSize,
                        sizeDisplay: formatFileSize(a.fileSize)
                    };
                }));
            })
            .catch(function(error) {
                console.error('Load attachments error:', error);
            });
    };

    // ========================
    // ANSWER SYSTEM (Index.js ile aynı)
    // ========================
    self.answers = {};

    // Get or create answer for a question
    self.getAnswer = function(questionId, isRequired) {
        if (!self.answers[questionId]) {
            self.answers[questionId] = {
                questionId: questionId,
                answerId: ko.observable(null),
                answerText: ko.observable(''),
                answerNumeric: ko.observable(null),
                givenPoints: ko.observable(null),
                notes: ko.observable(''),
                recommendationNotes: ko.observable(''),
                applyPenalty: ko.observable(false),
                selectedPenaltyType: ko.observable(''),
                selectedSubCriteria: ko.observableArray([]),
                isIncluded: ko.observable(isRequired !== false) // Zorunlu sorular varsayılan dahil
            };

            // Subscribe to changes - puan seçildiğinde otomatik "Dahil" yap ve hesapla
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
                // "Hariç"e geçirildiğinde puanları temizle
                if (!newValue) {
                    self.answers[questionId].answerNumeric(null);
                    self.answers[questionId].givenPoints(null);
                }
                self.calculateScores();
            });
        }
        return self.answers[questionId];
    };

    // Helper: Generate score options array [0, 1, 2, ..., maxPoints]
    self.getScoreOptions = function(maxPoints) {
        var max = parseInt(maxPoints) || 5;
        if (max > 10) max = 10; // Max 10 seçenek göster (UI için)
        var options = [];
        for (var i = 0; i <= max; i++) {
            options.push(i);
        }
        return options;
    };

    // Toggle sub-criteria selection
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

    // Check if sub-criteria is selected
    self.isSubCriteriaSelected = function(questionId, subCriteriaId) {
        var answer = self.getAnswer(questionId);
        return answer.selectedSubCriteria().indexOf(subCriteriaId) >= 0;
    };

    // ========================
    // SCORE CALCULATION (Index.js ile aynı mantık)
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

        var groups = self.formData().penaltyGroups || [];
        groups.forEach(function(group) {
            (group.questions || []).forEach(function(q) {
                var weight = q.weightPoints || q.points || 0;
                var answer = self.answers[q.id];

                // Skip unscored questions
                if (q.scoringType === 'Unscored') return;

                // Penalty sorular: her zaman opsiyonel, ağırlık grubuna ekle ama max'a ekleme
                if (q.scoringType === 'Penalty') {
                    if (q.penaltyType === 'YellowCard') {
                        yellowCardWeight += weight;
                    } else if (q.penaltyType === 'RedCard') {
                        redCardWeight += weight;
                    }
                } else {
                    // Scored sorular için ağırlık grubu
                    if (!q.isRequired && (!answer || !answer.isIncluded())) {
                        return;
                    }
                    scoredWeight += weight;
                }

                if (!answer) return;
                if (!q.isRequired && !answer.isIncluded()) return;

                // Penalty sorular - ceza olarak düşülür
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

                // Normal scored questions - Maximum hesaplama
                var maxScore = q.maxPoints || 5;
                max += weight;

                // Use given points if available (manual override)
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
        // Ağırlık grupları
        self.scoredWeightCalc(scoredWeight);
        self.yellowCardWeightCalc(yellowCardWeight);
        self.redCardWeightCalc(redCardWeight);
    };

    // ========================
    // VALIDATION
    // ========================
    self.validateRequiredFields = function() {
        var errors = [];

        // Personel seçimi (ya listeden seç, ya yeni personel gir, ya da tanımsız personel gir)
        if (self.isNewPersonnelMode()) {
            // Yeni personel modunda ad ve soyad zorunlu
            if (!self.newPersonnelFirstName() || !self.newPersonnelFirstName().trim()) {
                errors.push('Yeni personel için ad zorunludur');
            }
            if (!self.newPersonnelLastName() || !self.newPersonnelLastName().trim()) {
                errors.push('Yeni personel için soyad zorunludur');
            }
        } else if (!self.evaluatedPersonnelId() && !self.evaluatedUnknownPersonnel()) {
            errors.push('Personel seçimi zorunludur');
        }

        // Çağrı bilgileri
        if (!self.callDate()) errors.push('Çağrı Tarihi zorunludur');
        if (!self.callTime()) errors.push('Çağrı Saati zorunludur');
        if (!self.duration()) errors.push('Süre zorunludur');

        return errors;
    };

    // CallId tekrar kontrolü - aynı müşteride aynı CallId varsa hata ver
    self.checkCallIdExists = function() {
        return new Promise(function(resolve) {
            var callId = self.callId();
            if (!callId || !callId.trim()) {
                resolve(false);
                return;
            }
            var assignmentId = config.assignmentId || (self.formData() ? self.formData().assignmentId : null);
            var evaluationId = config.evaluationId || (self.formData() ? self.formData().evaluationId : null);
            if (!assignmentId) {
                resolve(false);
                return;
            }
            var url = '/api/evaluations/check-call-id?callId=' + encodeURIComponent(callId) +
                      '&assignmentId=' + assignmentId;
            if (evaluationId) {
                url += '&evaluationId=' + evaluationId;
            }
            fetch(url, { credentials: 'include' })
                .then(function(response) { return response.json(); })
                .then(function(data) { resolve(data.exists === true); })
                .catch(function() { resolve(false); });
        });
    };

    // CallId değiştiğinde otomatik kontrol (debounced)
    var callIdCheckTimeout = null;
    self.callId.subscribe(function(newValue) {
        // Önceki timeout'u temizle
        if (callIdCheckTimeout) {
            clearTimeout(callIdCheckTimeout);
            callIdCheckTimeout = null;
        }

        // Boşsa veya form açık değilse kontrol etme
        if (!newValue || !newValue.trim() || !self.formData()) {
            self.callIdExists(false);
            self.isCheckingCallId(false);
            return;
        }

        // 500ms sonra kontrol et (debounce)
        self.isCheckingCallId(true);
        callIdCheckTimeout = setTimeout(function() {
            self.checkCallIdExists().then(function(exists) {
                self.callIdExists(exists);
                self.isCheckingCallId(false);
            });
        }, 500);
    });

    // ========================
    // SUMMARY VIEW
    // ========================
    self.showSummary = function() {
        var validationErrors = self.validateRequiredFields();
        if (validationErrors.length > 0) {
            toastr.error(validationErrors.join('<br>'), 'Zorunlu Alanlar', { enableHtml: true });
            return;
        }

        self.checkCallIdExists().then(function(exists) {
            if (exists) {
                toastr.error('Bu Çağrı ID daha önce kaydedilmiş. Aynı Çağrı ID ile yeni dinleme eklenemez.');
                return;
            }

            // Cevapları hazırla
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

            var filteredDescriptions = self.descriptions().map(function(d) {
                return ko.unwrap(d);
            }).filter(function(d) {
                return d && d.trim().length > 0;
            });

            var personnelName = '';
            if (self.isNewPersonnelMode()) {
                personnelName = self.newPersonnelFirstName().trim() + ' ' + self.newPersonnelLastName().trim();
            } else {
                var p = self.availablePersonnel().find(function(p) { return p.id === self.evaluatedPersonnelId(); });
                personnelName = p ? p.name : (self.evaluatedUnknownPersonnel() || '-');
            }

            self.summaryData({
                totalScore: self.totalScoreCalc(),
                maxScore: self.maxScoreCalc(),
                scorePercentage: self.scorePercentageCalc(),
                yellowCardCount: self.yellowCardCountCalc(),
                redCardCount: self.redCardCountCalc(),
                scoredWeight: self.scoredWeightCalc(),
                yellowCardWeight: self.yellowCardWeightCalc(),
                redCardWeight: self.redCardWeightCalc(),
                evaluatedPersonnelName: personnelName,
                callId: self.callId() || '-',
                callDate: self.callDate() || '-',
                callTime: self.callTime() || '-',
                duration: self.duration() || '-',
                descriptions: filteredDescriptions,
                evaluationComment: self.evaluationComment() || '',
                answers: answersForSummary
            });
            self.isShowingSummary(true);
        });
    };

    self.backToForm = function() {
        self.isShowingSummary(false);
    };

    self.confirmSubmit = function() {
        self.isSavingForm(true);
        self.doSave(true);
    };

    // ========================
    // FORM LOADING
    // ========================
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

                // Mevcut değerleri yükle
                if (data.callId) self.callId(data.callId);
                if (data.callDate) self.callDate(data.callDate.split('T')[0]);
                if (data.callTime) self.callTime(data.callTime);
                if (data.duration) self.duration(data.duration);
                if (data.evaluationComment) self.evaluationComment(data.evaluationComment);

                // Descriptions
                if (data.descriptions && data.descriptions.length > 0) {
                    self.descriptions(data.descriptions.map(function(d) { return ko.observable(d); }));
                } else {
                    self.descriptions([ko.observable('')]); // En az bir boş açıklama
                }

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

                // Initialize answers for all questions
                var groups = data.penaltyGroups || [];
                groups.forEach(function(group) {
                    (group.questions || []).forEach(function(q) {
                        // isRequired bilgisini geç - zorunlu sorular varsayılan dahil, opsiyonel sorular varsayılan hariç
                        var answer = self.getAnswer(q.id, q.isRequired);

                        // Soru zaten YellowCard/RedCard tanımlıysa otomatik set et
                        if (q.penaltyType === 'YellowCard' || q.penaltyType === 'RedCard') {
                            answer.selectedPenaltyType(q.penaltyType);
                        }

                        // Mevcut cevap var mı kontrol et
                        var existingAnswer = (data.existingAnswers || []).find(function(ea) { return ea.questionId === q.id; });

                        if (existingAnswer) {
                            // Mevcut cevapları yükle (edit mode)
                            if (existingAnswer.answerNumeric !== null && existingAnswer.answerNumeric !== undefined) {
                                answer.answerNumeric(existingAnswer.answerNumeric);
                            }
                            if (existingAnswer.givenPoints !== null && existingAnswer.givenPoints !== undefined) {
                                answer.givenPoints(existingAnswer.givenPoints);
                            }
                            if (existingAnswer.answerText) answer.answerText(existingAnswer.answerText);
                            if (existingAnswer.notes) answer.notes(existingAnswer.notes);
                            if (existingAnswer.selectedSubCriteriaIds && existingAnswer.selectedSubCriteriaIds.length > 0) {
                                answer.selectedSubCriteria(existingAnswer.selectedSubCriteriaIds);
                            }
                            // Mevcut cevabı olan sorular dahil edilmiş demektir
                            answer.isIncluded(true);
                        } else {
                            // Yeni değerlendirmede puanlı ve dahil edilen sorular için varsayılan max puan
                            if (q.scoringType === 'Scored' && answer.answerNumeric() === null && answer.isIncluded()) {
                                answer.answerNumeric(q.maxPoints || 5);
                            }
                        }
                    });
                });

                // Mevcut değerlendirme ise dosyaları yükle
                if (data.evaluationId) {
                    self.loadExistingAttachments(data.evaluationId);
                }

                // Puan hesapla
                self.calculateScores();
            })
            .catch(function(error) {
                console.error('Load form error:', error);
                self.errorMessage('Form yüklenirken bir hata oluştu: ' + error.message);
            })
            .finally(function() {
                self.isLoading(false);
                self.initTimePickers();
            });
    };

    // Input mask başlatma
    self.initTimePickers = function() {
        Inputmask('99:99', { insertMode: false }).mask('.time-mask');
        Inputmask('99:99:99', { insertMode: false }).mask('.duration-mask');

        // Süre varsayılan olarak 00: ile başlasın
        if (!self.duration()) {
            self.duration('00:');
        }
    };

    // ========================
    // SAVE
    // ========================
    self.saveDraft = function() {
        // Zorunlu alan kontrolü
        var validationErrors = self.validateRequiredFields();
        if (validationErrors.length > 0) {
            toastr.error(validationErrors.join('<br>'), 'Zorunlu Alanlar', { enableHtml: true });
            return;
        }

        self.isSavingForm(true);

        // CallId tekrar kontrolü
        self.checkCallIdExists().then(function(exists) {
            if (exists) {
                self.isSavingForm(false);
                toastr.error('Bu Çağrı ID daha önce kaydedilmiş. Aynı Çağrı ID ile yeni dinleme eklenemez.');
                return;
            }

            self.doSave(false);
        });
    };

    self.submitEvaluation = function() {
        // Önce özet göster, onay sonrası kaydet
        self.showSummary();
    };

    self.doSave = function(isComplete) {
        self.isSaving(true);
        self.isSavingForm(true);
        self.errorMessage('');

        // Validasyon - Personel seçimi (ya listeden seç, ya yeni personel gir, ya da tanımsız personel gir)
        var personnelName = '';
        if (self.isNewPersonnelMode()) {
            var firstName = self.newPersonnelFirstName().trim();
            var lastName = self.newPersonnelLastName().trim();
            if (!firstName || !lastName) {
                self.errorMessage('Ad ve Soyad zorunludur');
                self.isSaving(false);
                self.isSavingForm(false);
                toastr.error('Ad ve Soyad zorunludur');
                return;
            }
            personnelName = firstName + ' ' + lastName;
        } else if (!self.evaluatedPersonnelId() && !self.evaluatedUnknownPersonnel()) {
            self.errorMessage('Personel seçimi zorunludur');
            self.isSaving(false);
            self.isSavingForm(false);
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

        // Cevapları hazırla (Index.js prepareData ile aynı mantık)
        var answersArray = [];

        // Soruları map'e al (penaltyType için)
        var questionMap = {};
        var groups = self.formData().penaltyGroups || [];
        groups.forEach(function(group) {
            (group.questions || []).forEach(function(q) {
                questionMap[q.id] = q;
            });
        });

        Object.keys(self.answers).forEach(function(questionId) {
            var a = self.answers[questionId];
            var q = questionMap[questionId];

            // penaltyType sorudan geliyor (checklist'te belirlendi)
            var penaltyType = q && q.penaltyType && q.penaltyType !== 'None' ? q.penaltyType : null;
            // Cezalı sorularda: değer > 0 ise ceza uygula
            var shouldApplyPenalty = q && q.scoringType === 'Penalty' &&
                a.answerNumeric() !== null && a.answerNumeric() !== '' &&
                parseFloat(a.answerNumeric()) > 0;

            var answerNumericVal = a.answerNumeric() !== null && a.answerNumeric() !== '' ? parseFloat(a.answerNumeric()) : null;
            var givenPointsVal = a.givenPoints() !== null && a.givenPoints() !== '' ? parseFloat(a.givenPoints()) : null;

            // isIncluded: Eğer cevap verilmişse (puan veya givenPoints varsa) true olmalı
            var isIncludedVal = a.isIncluded ? a.isIncluded() : true;
            if (answerNumericVal !== null || givenPointsVal !== null) {
                isIncludedVal = true;
            }

            answersArray.push({
                questionId: questionId,
                answerText: a.answerText() || null,
                answerNumeric: answerNumericVal,
                givenPoints: givenPointsVal,
                notes: a.notes() || null,
                recommendationNotes: a.recommendationNotes ? a.recommendationNotes() : null,
                applyPenalty: shouldApplyPenalty,
                selectedPenaltyType: shouldApplyPenalty ? penaltyType : null,
                selectedSubCriteriaIds: a.selectedSubCriteria ? a.selectedSubCriteria() : [],
                isIncluded: isIncludedVal
            });
        });

        // Descriptions
        var descriptionsArray = self.descriptions().map(function(d) {
            return typeof d === 'function' ? d() : d;
        }).filter(function(d) { return d && d.trim(); });

        var data = {
            assignmentId: config.assignmentId || self.formData()?.assignmentId,
            evaluationId: config.evaluationId || self.formData()?.evaluationId,
            assignmentPeriodId: self.selectedPeriodId ? self.selectedPeriodId() : null,
            answers: answersArray,
            notes: '',
            evaluationComment: self.evaluationComment(),
            callId: self.callId() || null,
            callDate: self.callDate() || null,
            callTime: self.callTime() || null,
            duration: self.duration() || null,
            descriptions: descriptionsArray.length > 0 ? descriptionsArray : null,
            evaluatedOrganizationId: self.formData()?.selectedOrganizationId || null,
            evaluatedPersonnelId: self.isNewPersonnelMode() ? null : (self.evaluatedPersonnelId() || null),
            evaluatedUnknownPersonnel: self.evaluatedUnknownPersonnel() || null,
            controlDate: new Date().toISOString().split('T')[0],
            controlTime: self.controlTime ? self.controlTime() : null,
            formOpenedAt: new Date().toISOString(),
            newPersonnel: self.isNewPersonnelMode() ? {
                firstName: self.newPersonnelFirstName(),
                lastName: self.newPersonnelLastName()
            } : null
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
            // API { message, evaluation, answers } döndürüyor
            var savedEvaluation = result.evaluation || result;
            var evaluationId = savedEvaluation.id || config.evaluationId;

            // Update answer IDs from result
            if (result.answers) {
                result.answers.forEach(function(a) {
                    var qId = String(a.questionId);
                    if (self.answers[qId]) {
                        self.answers[qId].answerId(a.id);
                    }
                });
            }

            // Pending dosyaları yükle
            return self.uploadPendingAttachments(evaluationId).then(function() {
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
            });
        })
        .catch(function(error) {
            console.error('Save error:', error);
            self.errorMessage('Kaydetme hatası: ' + error.message);
            toastr.error(error.message);
        })
        .finally(function() {
            self.isSaving(false);
            self.isSavingForm(false);
        });
    };

    // Başlat
    self.loadForm();
};

// Translation keys
var TRANSLATION_KEYS = [
    'Evaluation.LoadError',
    'Evaluation.NotFound',
    'Evaluation.DetailsLoadError',
    'Evaluation.InvalidParams',
    'Evaluation.FormLoadError',
    'Evaluation.FormLoadErrorMessage',
    'Evaluation.PersonnelRequired',
    'Evaluation.CallDateRequired',
    'Evaluation.CallTimeRequired',
    'Evaluation.DurationRequired',
    'Evaluation.ValidationError',
    'Evaluation.DraftSaveError',
    'Evaluation.DraftSaved',
    'Evaluation.DraftSaveErrorMessage',
    'Evaluation.SubmitError',
    'Evaluation.SubmitSuccess',
    'Evaluation.SubmitErrorMessage',
    'Confirm.Title',
    'Confirm.Message',
    'Common.Confirm'
];

// Apply bindings when DOM is ready
document.addEventListener('DOMContentLoaded', function() {
    Localization.loadKeys(TRANSLATION_KEYS).then(function() {
        var container = document.getElementById('evaluation-popup');
        if (container) {
            ko.applyBindings(new EvaluationPopupViewModel(), container);
        }

        // Initialize tooltips
        var tooltipTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="tooltip"]'));
        tooltipTriggerList.map(function (tooltipTriggerEl) {
            return new bootstrap.Tooltip(tooltipTriggerEl);
        });
    });
});
