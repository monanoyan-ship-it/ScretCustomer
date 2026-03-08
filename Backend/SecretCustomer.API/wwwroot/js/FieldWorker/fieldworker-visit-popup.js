// FieldWorker Visit Popup ViewModel
// popup-maximum.js'den kopyalandı - şube seçimi eklendi

var FieldWorkerVisitPopupViewModel = function() {
    var self = this;
    var config = window.popupConfig || {};

    // ========================
    // STATE (Index.js EVALUATE MODAL STATE ile aynı)
    // ========================
    self.isLoading = ko.observable(true);
    self.currentUserId = ko.observable(null); // Mevcut kullanıcı ID (User veya CustomerPersonnel)
    self.isFormLoading = ko.observable(false);
    self.isSavingForm = ko.observable(false);
    self.isSaving = ko.observable(false);
    self.isUploadingFile = ko.observable(false);
    self.errorMessage = ko.observable('');
    self.formSuccessMessage = ko.observable('');
    self.formData = ko.observable(null);

    // Özet görünümü
    self.isShowingSummary = ko.observable(false);
    self.summaryData = ko.observable(null);

    // Form fields
    self.callDate = ko.observable('');
    self.callTime = ko.observable('');
    self.duration = ko.observable('');
    self.controlTime = ko.observable('');
    self.descriptions = ko.observableArray([{text: ko.observable('')}]); // Wrapper object pattern
    self.pastDescriptions = ko.observableArray([]);
    self.availablePersonnel = ko.observableArray([]);
    self.isLoadingPersonnel = ko.observable(false);
    self.evaluatedPersonnelId = ko.observable(null);
    self.evaluatedUnknownPersonnel = ko.observable('');
    self.evaluationComment = ko.observable('');

    // FieldWorker için Şube (CustomerDealer) seçimi
    self.availableDealers = ko.observableArray([]);
    self.selectedDealerId = ko.observable(null);
    self.isLoadingDealers = ko.observable(false);
    self.isDealerLocked = ko.observable(false); // Dashboard'dan gelindiyse kilitli

    // New Personnel Mode (Listede Yok)
    self.isNewPersonnelMode = ko.observable(false);
    self.newPersonnelFirstName = ko.observable('');
    self.newPersonnelLastName = ko.observable('');

    // Personnel Autocomplete
    self.personnelSearchText = ko.observable('');
    self.isPersonnelDropdownVisible = ko.observable(false);
    self.selectedPersonnelName = ko.observable('');
    self._personnelDropdownTimeout = null;

    // Filtered personnel based on search text (startsWith pattern - Index.js ile aynı)
    self.filteredPersonnel = ko.computed(function() {
        var search = self.personnelSearchText().toLowerCase().trim();
        var personnel = self.availablePersonnel();
        if (search.length < 1) return personnel.slice(0, 20); // Show first 20 if no search
        return personnel.filter(function(p) {
            // startsWith - isim veya sicil no ile başlayanlar
            return (p.name || '').toLowerCase().indexOf(search) === 0 ||
                   (p.sicilNo || '').toLowerCase().indexOf(search) === 0;
        }).slice(0, 20); // Limit to 20 results
    });

    self.showPersonnelDropdown = function() {
        if (self._personnelDropdownTimeout) {
            clearTimeout(self._personnelDropdownTimeout);
            self._personnelDropdownTimeout = null;
        }
        self.isPersonnelDropdownVisible(true);
    };

    self.hidePersonnelDropdownDelayed = function() {
        self._personnelDropdownTimeout = setTimeout(function() {
            self.isPersonnelDropdownVisible(false);
        }, 200);
    };

    self.selectPersonnel = function(personnel) {
        self.evaluatedPersonnelId(personnel.id);
        self.selectedPersonnelName(personnel.name);
        self.personnelSearchText(personnel.name); // Input'a seçilen adı yaz
        self.isPersonnelDropdownVisible(false);
    };

    self.clearSelectedPersonnel = function() {
        self.evaluatedPersonnelId(null);
        self.selectedPersonnelName('');
        self.personnelSearchText('');
    };

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

    // Açıklama ekle
    self.addDescription = function() {
        self.descriptions.push({text: ko.observable('')});
    };

    // Açıklama kaldır
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
    // Ağırlık grupları
    self.scoredWeightCalc = ko.observable(0);      // Normal soru ağırlığı
    self.yellowCardWeightCalc = ko.observable(0);  // Sarı kart ağırlığı
    self.redCardWeightCalc = ko.observable(0);     // Kırmızı kart ağırlığı

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
    // ANSWER SYSTEM (Index.js ile birebir aynı)
    // ========================

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
                // Zorunlu olmayan sorular varsayılan kapalı gelir
                isIncluded: ko.observable(isRequired !== false)
            };

            // Subscribe to changes to recalculate scores
            self.answers[questionId].answerNumeric.subscribe(function(newValue) {
                // Puan seçildiğinde otomatik "Dahil" yap
                if (newValue !== null && newValue !== '') {
                    self.answers[questionId].isIncluded(true);
                }
                self.calculateScores();
            });
            self.answers[questionId].answerText.subscribe(function() { self.calculateScores(); });
            self.answers[questionId].givenPoints.subscribe(function(newValue) {
                // Puan girildiğinde otomatik "Dahil" yap
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
    // EVALUATION ATTACHMENTS (Index.js ile birebir aynı)
    // ========================

    // Yüklenmiş dosyalar (sunucudaki)
    self.uploadedAttachments = ko.observableArray([]);
    // Bekleyen dosyalar (henüz yüklenmemiş)
    self.pendingAttachments = ko.observableArray([]);

    function formatFileSize(bytes) {
        if (bytes < 1024) return bytes + ' B';
        if (bytes < 1024 * 1024) return (bytes / 1024).toFixed(1) + ' KB';
        if (bytes < 1024 * 1024 * 1024) return (bytes / (1024 * 1024)).toFixed(1) + ' MB';
        return (bytes / (1024 * 1024 * 1024)).toFixed(1) + ' GB';
    }

    // Dosya seçildiğinde bekleyenler listesine ekle
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

        // Clear input for re-selection
        event.target.value = '';
        toastr.info(T('Evaluation.FilesSelected', 'Dosyalar seçildi. Form kaydedildiğinde yüklenecek.'));
    };

    // Bekleyen dosyayı kaldır
    self.removePendingAttachment = function(attachment) {
        self.pendingAttachments.remove(attachment);
    };

    // Yüklenmiş dosyayı sil
    self.deleteAttachment = function(attachment) {
        showConfirmModal({
            title: T('Common.Delete', 'Sil'),
            message: T('Evaluation.ConfirmDeleteAttachment', 'Dosyayı silmek istediğinize emin misiniz?'),
            confirmText: T('Common.Delete', 'Sil'),
            confirmClass: 'btn-danger',
            onConfirm: function() {
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
                    toastr.success(T('Evaluation.FileDeleted', 'Dosya silindi'));
                })
                .catch(function(error) {
                    console.error('Delete error:', error);
                    toastr.error(T('Evaluation.FileDeleteError', 'Dosya silinirken hata oluştu'));
                });
            }
        });
    };

    // Dosya indir
    self.downloadAttachment = function(attachment) {
        window.open('/api/evaluations/attachments/' + attachment.id + '/download', '_blank');
    };

    // Tüm bekleyen dosyaları yükle (form kaydedildikten sonra çağrılır)
    self.uploadPendingAttachments = function(evaluationId) {
        var pending = self.pendingAttachments();
        if (pending.length === 0) {
            return Promise.resolve();
        }

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
                // Yüklenen dosyayı listeye ekle
                self.uploadedAttachments.push({
                    id: result.attachmentId,
                    fileName: result.fileName,
                    fileSize: result.fileSize,
                    sizeDisplay: formatFileSize(result.fileSize)
                });
            })
            .catch(function(error) {
                console.error('Upload error for ' + attachment.name + ':', error);
                toastr.error(T('Evaluation.FileUploadError', 'Dosya yüklenemedi: ') + attachment.name);
            });
        });

        return Promise.all(uploadPromises).then(function() {
            // Başarılı yüklenen dosyaları bekleyenlerden temizle
            self.pendingAttachments([]);
        }).finally(function() {
            self.isUploadingFile(false);
        });
    };

    // Mevcut değerlendirmenin dosyalarını yükle
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

    // Input mask başlatma
    self.initTimePickers = function() {
        Inputmask('99:99', { insertMode: false }).mask('.time-mask');
        Inputmask('99:99:99', { insertMode: false }).mask('.duration-mask');

        // Süre varsayılan olarak 00: ile başlasın
        if (!self.duration()) {
            self.duration('00:');
        }
    };

    self.resetFormFields = function() {
        self.callDate('');
        self.callTime('');
        self.duration('');
        self.controlTime('');
        self.descriptions([{text: ko.observable('')}]); // Wrapper object pattern
        self.availablePersonnel([]);
        self.evaluatedPersonnelId(null);
        self.evaluatedUnknownPersonnel('');
        self.evaluationComment('');
        // FieldWorker dealer reset
        self.availableDealers([]);
        self.selectedDealerId(null);
        // Autocomplete state reset
        self.personnelSearchText('');
        self.selectedPersonnelName('');
        self.isPersonnelDropdownVisible(false);
        self.isNewPersonnelMode(false);
        self.newPersonnelFirstName('');
        self.newPersonnelLastName('');
        self.selectedPeriodId(null);
        self.availablePeriods([]);
        self.totalScoreCalc(0);
        self.maxScoreCalc(0);
        self.scorePercentageCalc(0);
        self.yellowCardCountCalc(0);
        self.redCardCountCalc(0);
        self.scoredWeightCalc(0);
        self.yellowCardWeightCalc(0);
        self.redCardWeightCalc(0);
        // Attachments reset
        self.uploadedAttachments([]);
        self.pendingAttachments([]);
    };

    // ========================
    // LOAD DEALERS (FieldWorker için şube listesi)
    // ========================

    self.loadDealers = function() {
        if (!config.assignmentId) return;

        self.isLoadingDealers(true);
        // projectId ile çağır - ziyaret edilmiş bayileri hariç tutar
        fetch('/api/fieldworker/dealers-for-assignment?assignmentId=' + config.assignmentId, { credentials: 'include' })
            .then(function(response) { return response.json(); })
            .then(function(data) {
                self.availableDealers(data || []);
                // Config'de dealerId varsa seç ve kilitle
                if (config.dealerId) {
                    self.selectedDealerId(config.dealerId);
                    self.isDealerLocked(true);
                }
            })
            .catch(function(error) {
                console.error('Error loading dealers:', error);
            })
            .finally(function() {
                self.isLoadingDealers(false);
            });
    };

    // ========================
    // LOAD FORM (Index.js loadForm ile birebir aynı)
    // ========================

    self.loadForm = function() {
        self.isLoading(true);
        self.formData(null);
        self.answers = {};
        self.isShowingSummary(false);
        self.summaryData(null);
        self.resetFormFields();

        var url = '';
        if (config.assignmentId) {
            url = '/api/evaluations/form/' + config.assignmentId;
        } else if (config.evaluationId) {
            url = '/api/evaluations/form/edit/' + config.evaluationId;
        } else {
            toastr.error(T('Evaluation.InvalidParams', 'Geçersiz parametreler'));
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

        // FieldWorker için şubeleri yükle
        self.loadDealers();

        fetch(url, { credentials: 'include' })
            .then(function(response) {
                if (!response.ok) throw new Error(T('Evaluation.FormLoadError', 'Form yüklenemedi'));
                return response.json();
            })
            .then(function(data) {
                self.formData(data);

                // Load existing values if any
                if (data.callDate) self.callDate(data.callDate.split('T')[0]);
                if (data.callTime) self.callTime(data.callTime);
                if (data.duration) self.duration(data.duration);
                if (data.descriptions && data.descriptions.length > 0) {
                    // Her string'i observable'a çevir
                    self.descriptions(data.descriptions.map(function(d) { return {text: ko.observable(d)}; }));
                } else {
                    self.descriptions([{text: ko.observable('')}]); // Wrapper object pattern
                }
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
                    // Seçili personelin adını göster
                    var selectedPerson = (data.availablePersonnel || []).find(function(p) { return p.id === data.evaluatedPersonnelId; });
                    if (selectedPerson) {
                        self.personnelSearchText(selectedPerson.name);
                        self.selectedPersonnelName(selectedPerson.name);
                    }
                }

                // ÖNCE tüm soruları initialize et (isRequired bilgisiyle)
                var hasExistingAnswers = data.existingAnswers && data.existingAnswers.length > 0;
                var existingAnswerMap = {};
                if (hasExistingAnswers) {
                    data.existingAnswers.forEach(function(a) {
                        existingAnswerMap[a.questionId] = a;
                    });
                }

                data.penaltyGroups.forEach(function(section) {
                    section.questions.forEach(function(q) {
                        // isRequired bilgisini geç - zorunlu sorular varsayılan dahil, opsiyonel sorular varsayılan hariç
                        var answer = self.getAnswer(q.id, q.isRequired);

                        // Soru zaten YellowCard/RedCard tanımlıysa otomatik set et
                        if (q.penaltyType === 'YellowCard' || q.penaltyType === 'RedCard') {
                            answer.selectedPenaltyType(q.penaltyType);
                        }

                        // Mevcut cevap varsa yükle
                        var existingAnswer = existingAnswerMap[q.id];
                        if (existingAnswer) {
                            // Answer ID
                            if (existingAnswer.id) answer.answerId(existingAnswer.id);
                            if (existingAnswer.answerText) answer.answerText(existingAnswer.answerText);
                            if (existingAnswer.answerNumeric !== null && existingAnswer.answerNumeric !== undefined) {
                                answer.answerNumeric(existingAnswer.answerNumeric);
                            }
                            if (existingAnswer.givenPoints) answer.givenPoints(existingAnswer.givenPoints);
                            if (existingAnswer.notes) answer.notes(existingAnswer.notes);
                            if (existingAnswer.recommendationNotes) answer.recommendationNotes(existingAnswer.recommendationNotes);
                            answer.applyPenalty(existingAnswer.isPenaltyApplied || false);
                            if (existingAnswer.appliedPenaltyType && existingAnswer.appliedPenaltyType !== 'None') {
                                answer.selectedPenaltyType(existingAnswer.appliedPenaltyType);
                            }
                            // Seçili alt kriterleri yükle
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

                self.calculateScores();

                // Mevcut değerlendirme ise dosyaları yükle
                if (data.evaluationId) {
                    self.loadExistingAttachments(data.evaluationId);
                }
            })
            .catch(function(error) {
                console.error('Form loading error:', error);
                toastr.error(T('Evaluation.FormLoadError', 'Form yüklenirken bir hata oluştu.'));
            })
            .finally(function() {
                self.isLoading(false);
                // Flatpickr 24h time picker başlat (DOM güncellenince)
                setTimeout(function() {
                    self.initTimePickers();
                }, 100);
            });
    };

    // ========================
    // CALCULATE SCORES (Index.js ile birebir aynı)
    // ========================

    self.calculateScores = function() {
        if (!self.formData()) return;

        var total = 0;
        var max = 0;
        var yellowCards = 0;
        var redCards = 0;
        // Ağırlık grupları
        var scoredWeight = 0;
        var yellowCardWeight = 0;
        var redCardWeight = 0;

        self.formData().penaltyGroups.forEach(function(section) {
            section.questions.forEach(function(q) {
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
                    // Zorunlu olmayan ve dahil edilmemiş → ağırlık hesaba katılmaz
                    if (!q.isRequired && (!answer || !answer.isIncluded())) {
                        return; // Bu soruyu tamamen atla
                    }
                    scoredWeight += weight;
                }

                if (!answer) return;

                // Zorunlu olmayan soru ve dahil edilmemiş → atla
                if (!q.isRequired && !answer.isIncluded()) return;

                // Handle penalty questions - penaltyType sorudan geliyor (checklist'te belirlendi)
                if (q.scoringType === 'Penalty') {
                    // Cevaplanmadıysa etkisi yok
                    if (answer.answerNumeric() === null || answer.answerNumeric() === '') return;

                    var numericValue = parseFloat(answer.answerNumeric()) || 0;
                    var maxScore = q.maxPoints || 2;

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
        // Ağırlık grupları
        self.scoredWeightCalc(scoredWeight);
        self.yellowCardWeightCalc(yellowCardWeight);
        self.redCardWeightCalc(redCardWeight);
    };

    // ========================
    // PREPARE DATA (Index.js ile birebir aynı)
    // ========================

    self.prepareData = function(isDraft) {
        var answers = [];

        // Soruları map'e al (penaltyType için)
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

            answers.push({
                questionId: parseInt(questionId),
                textAnswer: a.answerText() || null,
                numericAnswer: answerNumericVal,
                givenPoints: givenPointsVal,
                notes: a.notes() || null,
                recommendationNotes: a.recommendationNotes() || null,
                applyPenalty: shouldApplyPenalty,
                selectedPenaltyType: shouldApplyPenalty ? penaltyType : null,
                selectedSubCriteriaIds: a.selectedSubCriteria ? a.selectedSubCriteria() : [],
                isIncluded: isIncludedVal
            });
        });

        // Boş olmayan açıklamaları filtrele (observable'ları unwrap et)
        var filteredDescriptions = self.descriptions().map(function(d) {
            return d.text();
        }).filter(function(d) {
            return d && d.trim().length > 0;
        });

        // CreateVisitDto formatında döndür
        return {
            assignmentId: config.assignmentId,
            evaluationId: self.formData().evaluationId || null,
            customerDealerId: self.selectedDealerId() ? parseInt(self.selectedDealerId()) : 0,
            controlDate: self.callDate() || null,
            controlTime: self.callTime() || null,
            evaluationComment: self.evaluationComment() || null,
            descriptions: filteredDescriptions.length > 0 ? filteredDescriptions : null,
            isDraft: isDraft === true,
            answers: answers
        };
    };

    // ========================
    // VALIDATION (Index.js ile birebir aynı)
    // ========================

    self.validateRequiredFields = function() {
        var errors = [];

        // FieldWorker için bayi seçimi zorunlu
        if (!self.selectedDealerId()) {
            errors.push(T('FieldWorker.DealerRequired', 'Bayi seçimi zorunludur'));
        }

        // Ziyaret tarihi (callDate yerine controlDate kullanıyoruz ama zaten formda var)
        if (!self.callDate()) {
            errors.push(T('FieldWorker.VisitDateRequired', 'Ziyaret Tarihi zorunludur'));
        }
        if (!self.callTime() || self.callTime().indexOf('_') >= 0 || !/^\d{2}:\d{2}$/.test(self.callTime())) {
            errors.push(T('FieldWorker.VisitTimeRequired', 'Ziyaret Saati zorunludur (SS:DD formatında giriniz)'));
        }

        return errors;
    };

    // ========================
    // SAVE DRAFT (FieldWorker için özelleştirildi)
    // ========================

    self.saveDraft = function(callback) {
        // Zorunlu alan kontrolü
        var validationErrors = self.validateRequiredFields();
        if (validationErrors.length > 0) {
            toastr.error(validationErrors.join('<br>'), T('FieldWorker.ValidationError', 'Zorunlu Alanlar'), { enableHtml: true });
            return;
        }

        self.isSavingForm(true);
        var data = self.prepareData(true); // isDraft = true

            fetch('/api/fieldworker/visits', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                credentials: 'include',
                body: JSON.stringify(data)
            })
            .then(function(response) {
                if (!response.ok) {
                    return response.json().then(function(err) {
                        throw new Error(err.message || T('FieldWorker.DraftSaveError', 'Taslak kaydedilemedi'));
                    });
                }
                return response.json();
            })
            .then(function(result) {
                // API { evaluationId, message } döndürüyor
                var evaluationId = result.evaluationId;

                // formData'ya evaluationId'yi kaydet (sonraki kayıtlar için)
                if (self.formData()) {
                    self.formData().evaluationId = evaluationId;
                }

                // Pending dosyaları yükle
                return self.uploadPendingAttachments(evaluationId).then(function() {
                    if (typeof callback === 'function') {
                        callback();
                    } else {
                        toastr.success(T('FieldWorker.DraftSaved', 'Ziyaret taslak olarak kaydedildi.'));
                        self.notifyOpener();
                        window.close();
                    }
                });
            })
            .catch(function(error) {
                console.error('Draft save error:', error);
                toastr.error(error.message || T('FieldWorker.DraftSaveErrorMessage', 'Taslak kaydedilirken bir hata oluştu.'));
            })
            .finally(function() {
                self.isSavingForm(false);
            });
    };

    // ========================
    // SHOW SUMMARY (FieldWorker için özelleştirildi)
    // ========================

    self.showSummary = function() {
        // Zorunlu alan kontrolü
        var validationErrors = self.validateRequiredFields();
        if (validationErrors.length > 0) {
            toastr.error(validationErrors.join('<br>'), T('FieldWorker.ValidationError', 'Zorunlu Alanlar'), { enableHtml: true });
            return;
        }

        // Cevapları hazırla (sorular + verilen cevaplar)
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

                    // Puan hesapla
                    if (q.scoringType === 'Scored' && answerNumeric !== null && answerNumeric !== '') {
                        earnedPoints = (parseFloat(answerNumeric) / maxPoints) * weightPoints;
                    } else if (q.scoringType === 'Penalty' && answerNumeric !== null && answerNumeric !== '' && parseFloat(answerNumeric) > 0) {
                        earnedPoints = -((parseFloat(answerNumeric) / maxPoints) * weightPoints);
                    }

                    // Seçili alt kriterleri al
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

        // Açıklamaları al (boş olmayanlar)
        var filteredDescriptions = self.descriptions().map(function(d) {
            return d.text();
        }).filter(function(d) {
            return d && d.trim().length > 0;
        });

        // Seçili bayi bilgisi
        var selectedDealer = self.availableDealers().find(function(d) {
            return String(d.id) === String(self.selectedDealerId());
        });

        // Özet verilerini hazırla ve göster
        self.summaryData({
            totalScore: self.totalScoreCalc(),
            maxScore: self.maxScoreCalc(),
            scorePercentage: self.scorePercentageCalc(),
            yellowCardCount: self.yellowCardCountCalc(),
            redCardCount: self.redCardCountCalc(),
            scoredWeight: self.scoredWeightCalc(),
            yellowCardWeight: self.yellowCardWeightCalc(),
            redCardWeight: self.redCardWeightCalc(),
            // FieldWorker için bayi bilgisi (personel yerine)
            evaluatedPersonnelName: selectedDealer ? selectedDealer.name : '-',
            callDate: self.callDate() || '-',
            callTime: self.callTime() || '-',
            duration: '-',
            descriptions: filteredDescriptions,
            evaluationComment: self.evaluationComment() || '',
            answers: answersForSummary
        });
        self.isShowingSummary(true);
    };

    // Go back to form from summary (özetten forma geri dön)
    self.backToForm = function() {
        self.isShowingSummary(false);
    };

    // ========================
    // CONFIRM SUBMIT (Index.js ile birebir aynı)
    // ========================

    self.confirmSubmit = function() {
        self.isSavingForm(true);
        var data = self.prepareData(false); // isDraft = false

        fetch('/api/fieldworker/visits', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            credentials: 'include',
            body: JSON.stringify(data)
        })
        .then(function(response) {
            if (!response.ok) {
                return response.json().then(function(err) {
                    throw new Error(err.message || T('FieldWorker.SubmitError', 'Ziyaret kaydedilemedi'));
                });
            }
            return response.json();
        })
        .then(function(result) {
            // API { evaluationId, message } döndürüyor
            var evaluationId = result.evaluationId;

            // Pending dosyaları yükle
            return self.uploadPendingAttachments(evaluationId).then(function() {
                toastr.success(T('FieldWorker.SubmitSuccess', 'Ziyaret başarıyla kaydedildi.'));
                // Opener'ı bilgilendir ve pencereyi kapat
                self.notifyOpener();
                window.close();
            });
        })
        .catch(function(error) {
            console.error('Submit error:', error);
            toastr.error(error.message || T('FieldWorker.SubmitErrorMessage', 'Ziyaret kaydedilirken bir hata oluştu.'));
        })
        .finally(function() {
            self.isSavingForm(false);
        });
    };

    // ========================
    // POPUP SPECIFIC
    // ========================

    // Tamamla butonuna tıklandığında önce özet göster
    self.submitEvaluation = function() {
        self.showSummary();
    };

    // Opener pencereyi bilgilendir (filtreleri bozmadan)
    self.notifyOpener = function() {
        if (!window.opener || window.opener.closed) return;
        try {
            // Dashboard için postMessage kullan
            window.opener.postMessage('visitSaved', '*');

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

    // ========================
    // INITIALIZE
    // ========================
    self.loadForm();
};

// Translation keys
var TRANSLATION_KEYS = [
    'Evaluation.LoadError',
    'Evaluation.NotFound',
    'Evaluation.DetailsLoadError',
    'Evaluation.InvalidParams',
    'Evaluation.FormLoadError',
    'Evaluation.FormLoadError',
    'Evaluation.PersonnelRequired',
    'Evaluation.NewPersonnelFirstNameRequired',
    'Evaluation.NewPersonnelLastNameRequired',
    'Evaluation.CallDateRequired',
    'Evaluation.CallTimeRequired',
    'Evaluation.DurationRequired',
    'Evaluation.ValidationError',
    'Evaluation.DraftSaveError',
    'Evaluation.DraftSaved',
    'Evaluation.DraftSaveError',
    'Evaluation.SubmitError',
    'Evaluation.SubmitSuccess',
    'Evaluation.SubmitError',
    'Evaluation.CallIdExists',
    'Evaluation.FilesSelected',
    'Evaluation.FileDeleted',
    'Evaluation.FileDeleteError',
    'Evaluation.FileUploadError',
    'Evaluation.ConfirmDeleteAttachment',
    'Common.Delete',
    'Common.Confirmation',
    'Confirm.Message',
    'Common.Confirm'
];

// Apply bindings when DOM is ready
document.addEventListener('DOMContentLoaded', function() {
    Localization.loadKeys(TRANSLATION_KEYS).then(function() {
        var container = document.getElementById('evaluation-popup');
        if (container) {
            ko.applyBindings(new FieldWorkerVisitPopupViewModel(), container);
        }

        // Initialize tooltips
        var tooltipTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="tooltip"]'));
        tooltipTriggerList.map(function (tooltipTriggerEl) {
            return new bootstrap.Tooltip(tooltipTriggerEl);
        });
    });
});
