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

    // Dinlemeler/Ziyaretler filtreleri
    self.evaluationsStatusFilter = ko.observable('');
    self.evaluationsDateFrom = ko.observable('');
    self.evaluationsDateTo = ko.observable('');

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

    // Özet görünümü
    self.isShowingSummary = ko.observable(false);
    self.summaryData = ko.observable(null);

    // Form fields
    self.callId = ko.observable('');
    self.callDate = ko.observable('');
    self.callTime = ko.observable('');
    self.duration = ko.observable('');
    self.controlTime = ko.observable('');
    self.descriptions = ko.observableArray([ko.observable('')]); // Her eleman observable
    self.availablePersonnel = ko.observableArray([]);
    self.isLoadingPersonnel = ko.observable(false);
    self.evaluatedPersonnelId = ko.observable(null);
    self.evaluatedUnknownPersonnel = ko.observable('');
    self.evaluationComment = ko.observable('');

    // New Personnel Mode (Listede Yok)
    self.isNewPersonnelMode = ko.observable(false);
    self.newPersonnelFirstName = ko.observable('');
    self.newPersonnelLastName = ko.observable('');

    self.enableNewPersonnelMode = function() {
        self.isNewPersonnelMode(true);
        self.evaluatedPersonnelId(null);
    };

    self.cancelNewPersonnelMode = function() {
        self.isNewPersonnelMode(false);
        self.newPersonnelFirstName('');
        self.newPersonnelLastName('');
    };

    // Açıklama ekle
    self.addDescription = function() {
        self.descriptions.push(ko.observable(''));
    };

    // Açıklama kaldır
    self.removeDescription = function(index) {
        if (self.descriptions().length > 1) {
            self.descriptions.splice(index, 1);
        }
    };

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
        var statusFilter = self.evaluationsStatusFilter();
        var dateFrom = self.evaluationsDateFrom();
        var dateTo = self.evaluationsDateTo();

        return self.allEvaluations().filter(function(e) {
            // Text arama
            if (search) {
                var matchesSearch = (e.projectName || '').toLowerCase().indexOf(search) >= 0 ||
                                   (e.checklistName || '').toLowerCase().indexOf(search) >= 0 ||
                                   (e.evaluatedPersonnelName || '').toLowerCase().indexOf(search) >= 0 ||
                                   (e.evaluatedUnknownPersonnel || '').toLowerCase().indexOf(search) >= 0 ||
                                   (e.callId || '').toLowerCase().indexOf(search) >= 0;
                if (!matchesSearch) return false;
            }
            // Durum filtresi
            if (statusFilter && e.status !== statusFilter) return false;
            // Tarih filtreleri (callDate kullan)
            if (dateFrom && e.callDate) {
                var evalDate = new Date(e.callDate);
                var fromDate = new Date(dateFrom);
                if (evalDate < fromDate) return false;
            }
            if (dateTo && e.callDate) {
                var evalDate = new Date(e.callDate);
                var toDate = new Date(dateTo);
                toDate.setHours(23, 59, 59, 999);
                if (evalDate > toDate) return false;
            }
            return true;
        });
    });

    // Filtreleri temizle
    self.clearEvaluationsFilters = function() {
        self.evaluationsSearch('');
        self.evaluationsStatusFilter('');
        self.evaluationsDateFrom('');
        self.evaluationsDateTo('');
    };

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
            toastr.error(T('Evaluation.LoadError', 'Veriler yüklenirken bir hata oluştu.'));
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
                toastr.error(T('Evaluation.DetailsLoadError', 'Değerlendirme detayları yüklenirken bir hata oluştu.'));
            })
            .finally(function() {
                self.isDetailsLoading(false);
            });
    };

    self.closeDetailsModal = function() {
        self.isDetailsModalOpen(false);
        self.detailsData(null);
    };

    // Taslağa alma talebi modalı
    self.isRevertRequestModalOpen = ko.observable(false);
    self.revertRequestReason = ko.observable('');
    self.isSubmittingRevertRequest = ko.observable(false);
    self.revertRequestEvaluationId = ko.observable(null);

    self.openRevertRequestModal = function() {
        if (self.detailsData()) {
            self.revertRequestEvaluationId(self.detailsData().id);
            self.revertRequestReason('');
            self.isRevertRequestModalOpen(true);
        }
    };

    self.closeRevertRequestModal = function() {
        self.isRevertRequestModalOpen(false);
        self.revertRequestReason('');
        self.revertRequestEvaluationId(null);
    };

    self.submitRevertRequest = function() {
        var evaluationId = self.revertRequestEvaluationId();
        if (!evaluationId) return;

        self.isSubmittingRevertRequest(true);

        fetch('/api/evaluations/' + evaluationId + '/request-revert', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            credentials: 'include',
            body: JSON.stringify({ reason: self.revertRequestReason() || '' })
        })
        .then(function(res) {
            if (!res.ok) {
                return res.json().then(function(d) {
                    throw new Error(d.message || 'Talep gönderilemedi');
                });
            }
            return res.json();
        })
        .then(function(result) {
            toastr.success(T('Evaluation.RevertRequestSent', 'Taslağa alma talebi gönderildi. Admin onayı bekleniyor.'));
            self.closeRevertRequestModal();
            self.closeDetailsModal();
        })
        .catch(function(err) {
            toastr.error(err.message || T('Evaluation.RevertRequestFailed', 'Talep gönderilemedi.'));
        })
        .finally(function() {
            self.isSubmittingRevertRequest(false);
        });
    };

    // ========================
    // PROJECT FILES MODAL
    // ========================

    self.isProjectFilesModalOpen = ko.observable(false);
    self.isLoadingProjectFiles = ko.observable(false);
    self.projectFiles = ko.observableArray([]);
    self.currentProjectId = null;

    self.showProjectFiles = function(assignment) {
        self.currentProjectId = assignment.projectId;
        self.isProjectFilesModalOpen(true);
        self.isLoadingProjectFiles(true);
        self.projectFiles([]);

        fetch('/api/project-files/project/' + assignment.projectId, { credentials: 'include' })
            .then(function(res) {
                if (!res.ok) throw new Error('Files API error');
                return res.json();
            })
            .then(function(files) {
                // Add helper properties for display
                files.forEach(function(f) {
                    f.fileSizeDisplay = formatFileSize(f.fileSize);
                    f.fileIcon = getFileIcon(f.contentType);
                });
                self.projectFiles(files);
            })
            .catch(function(err) {
                console.error('Error loading files:', err);
                toastr.error(T('Project.FilesLoadError', 'Dosyalar yüklenirken hata oluştu.'));
            })
            .finally(function() {
                self.isLoadingProjectFiles(false);
            });
    };

    self.closeProjectFilesModal = function() {
        self.isProjectFilesModalOpen(false);
        self.projectFiles([]);
        self.currentProjectId = null;
    };

    self.downloadProjectFile = function(file) {
        window.location.href = '/api/project-files/' + file.id + '/download';
    };

    function formatFileSize(bytes) {
        if (bytes < 1024) return bytes + ' B';
        if (bytes < 1024 * 1024) return (bytes / 1024).toFixed(1) + ' KB';
        if (bytes < 1024 * 1024 * 1024) return (bytes / (1024 * 1024)).toFixed(1) + ' MB';
        return (bytes / (1024 * 1024 * 1024)).toFixed(1) + ' GB';
    }

    function getFileIcon(contentType) {
        if (!contentType) return 'bi-file-earmark';
        if (contentType.indexOf('pdf') > -1) return 'bi-file-earmark-pdf text-danger';
        if (contentType.indexOf('word') > -1 || contentType.indexOf('document') > -1) return 'bi-file-earmark-word text-primary';
        if (contentType.indexOf('excel') > -1 || contentType.indexOf('spreadsheet') > -1) return 'bi-file-earmark-excel text-success';
        if (contentType.indexOf('image') > -1) return 'bi-file-earmark-image text-info';
        if (contentType.indexOf('video') > -1) return 'bi-file-earmark-play text-warning';
        if (contentType.indexOf('audio') > -1) return 'bi-file-earmark-music text-secondary';
        if (contentType.indexOf('zip') > -1 || contentType.indexOf('rar') > -1) return 'bi-file-earmark-zip text-warning';
        return 'bi-file-earmark';
    }

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
        self.isFormLoading(true);        self.formData(null);
        self.answers = {};
        self.isShowingSummary(false);
        self.summaryData(null);
        self.resetFormFields();
        self.loadForm();

        // Flatpickr 24h time picker başlat (DOM güncellenince)
        setTimeout(function() {
            self.initTimePickers();
        }, 100);
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
        self.callId('');
        self.callDate('');
        self.callTime('');
        self.duration('');
        self.controlTime('');
        self.descriptions([ko.observable('')]); // En az bir boş açıklama observable ile başla
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
        self.scoredWeightCalc(0);
        self.yellowCardWeightCalc(0);
        self.redCardWeightCalc(0);
    };

    self.closeEvaluateModal = function() {
        self.isEvaluateModalOpen(false);
        self.formData(null);
        self.currentAssignmentId = null;
        self.currentEvaluationId = null;
        self.isShowingSummary(false);
        self.summaryData(null);
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
                selectedPenaltyType: ko.observable(''),
                selectedSubCriteria: ko.observableArray([])
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

    // Load form data
    self.loadForm = function() {
        self.isFormLoading(true);        var url = '';
        if (self.currentAssignmentId) {
            url = '/api/evaluations/form/' + self.currentAssignmentId;
        } else if (self.currentEvaluationId) {
            url = '/api/evaluations/form/edit/' + self.currentEvaluationId;
        } else {
            toastr.error(T('Evaluation.InvalidParams', 'Geçersiz parametreler'));
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
                if (data.callTime) self.callTime(data.callTime);
                if (data.duration) self.duration(data.duration);
                if (data.descriptions && data.descriptions.length > 0) {
                    // Her string'i observable'a çevir
                    self.descriptions(data.descriptions.map(function(d) { return ko.observable(d); }));
                } else {
                    self.descriptions([ko.observable('')]); // En az bir boş açıklama
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
                        // Seçili alt kriterleri yükle
                        if (a.selectedSubCriteriaIds && a.selectedSubCriteriaIds.length > 0) {
                            answer.selectedSubCriteria(a.selectedSubCriteriaIds);
                        }
                    });
                }

                // Initialize answers for all questions
                var hasExistingAnswers = data.existingAnswers && data.existingAnswers.length > 0;
                data.sections.forEach(function(section) {
                    section.questions.forEach(function(q) {
                        var answer = self.getAnswer(q.id);
                        // Soru zaten YellowCard/RedCard tanımlıysa otomatik set et
                        if (q.penaltyType === 'YellowCard' || q.penaltyType === 'RedCard') {
                            answer.selectedPenaltyType(q.penaltyType);
                        }
                        // Puanlı sorular için varsayılan olarak maxPoints değerini ata (100 puandan başla)
                        // Sadece yeni değerlendirmede (existingAnswers yoksa) ve cevap henüz girilmemişse
                        if (q.scoringType === 'Scored' && !hasExistingAnswers && answer.answerNumeric() === null) {
                            answer.answerNumeric(q.maxPoints || 5);
                        }
                    });
                });

                self.calculateScores();
            })
            .catch(function(error) {
                console.error('Form loading error:', error);
                toastr.error(T('Evaluation.FormLoadErrorMessage', 'Form yüklenirken bir hata oluştu.'));
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
        // Ağırlık grupları
        var scoredWeight = 0;
        var yellowCardWeight = 0;
        var redCardWeight = 0;

        self.formData().sections.forEach(function(section) {
            section.questions.forEach(function(q) {
                var weight = q.weightPoints || q.points || 0;

                // Ağırlık gruplarını hesapla (tüm sorular için)
                if (q.penaltyType === 'YellowCard') {
                    yellowCardWeight += weight;
                } else if (q.penaltyType === 'RedCard') {
                    redCardWeight += weight;
                } else if (q.scoringType === 'Scored') {
                    scoredWeight += weight;
                }

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
                selectedPenaltyType: shouldApplyPenalty ? penaltyType : null,
                selectedSubCriteriaIds: a.selectedSubCriteria ? a.selectedSubCriteria() : []
            });
        });

        // Boş olmayan açıklamaları filtrele (observable'ları unwrap et)
        var filteredDescriptions = self.descriptions().map(function(d) {
            return ko.unwrap(d); // observable ise değerini al
        }).filter(function(d) {
            return d && d.trim().length > 0;
        });

        return {
            assignmentId: self.formData().assignmentId,
            evaluationId: self.formData().evaluationId || null,
            assignmentPeriodId: self.selectedPeriodId() || null,
            answers: answers,
            notes: '',
            evaluationComment: self.evaluationComment(),
            callId: self.callId() || null,
            callDate: self.callDate() || null,
            callTime: self.callTime() || null,
            duration: self.duration() || null,
            descriptions: filteredDescriptions.length > 0 ? filteredDescriptions : null,
            evaluatedOrganizationId: self.formData().selectedOrganizationId || null,
            evaluatedPersonnelId: self.isNewPersonnelMode() ? null : (self.evaluatedPersonnelId() || null),
            evaluatedUnknownPersonnel: self.evaluatedUnknownPersonnel() || null,
            controlDate: new Date().toISOString().split('T')[0],
            controlTime: self.controlTime() || null,
            formOpenedAt: new Date().toISOString(),
            newPersonnel: self.isNewPersonnelMode() ? {
                firstName: self.newPersonnelFirstName(),
                lastName: self.newPersonnelLastName()
            } : null
        };
    };

    // Zorunlu alan validasyonu
    self.validateRequiredFields = function() {
        var errors = [];

        // Personel seçimi (ya listeden seç, ya yeni personel gir, ya da tanımsız personel gir)
        if (self.isNewPersonnelMode()) {
            // Yeni personel modunda ad ve soyad zorunlu
            if (!self.newPersonnelFirstName() || !self.newPersonnelFirstName().trim()) {
                errors.push(T('Evaluation.NewPersonnelFirstNameRequired', 'Yeni personel için ad zorunludur'));
            }
            if (!self.newPersonnelLastName() || !self.newPersonnelLastName().trim()) {
                errors.push(T('Evaluation.NewPersonnelLastNameRequired', 'Yeni personel için soyad zorunludur'));
            }
        } else if (!self.evaluatedPersonnelId() && !self.evaluatedUnknownPersonnel()) {
            errors.push(T('Evaluation.PersonnelRequired', 'Personel seçimi zorunludur'));
        }

        if (!self.callDate()) {
            errors.push(T('Evaluation.CallDateRequired', 'Çağrı Tarihi zorunludur'));
        }
        if (!self.callTime()) {
            errors.push(T('Evaluation.CallTimeRequired', 'Çağrı Saati zorunludur'));
        }
        if (!self.duration()) {
            errors.push(T('Evaluation.DurationRequired', 'Süre zorunludur'));
        }

        return errors;
    };

    // Save as draft
    self.saveDraft = function() {
        // Zorunlu alan kontrolü
        var validationErrors = self.validateRequiredFields();
        if (validationErrors.length > 0) {
            toastr.error(validationErrors.join('<br>'), T('Evaluation.ValidationError', 'Zorunlu Alanlar'), { enableHtml: true });
            return;
        }

        self.isSavingForm(true);        var data = self.prepareData();

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
            toastr.success(T('Evaluation.DraftSaved', 'Taslak başarıyla kaydedildi.'));
            // Taslak kaydedilince modal kapansın ve liste yenilensin
            self.closeEvaluateModal();
            self.loadEvaluations();
        })
        .catch(function(error) {
            console.error('Draft save error:', error);
            toastr.error(T('Evaluation.DraftSaveErrorMessage', 'Taslak kaydedilirken bir hata oluştu.'));
        })
        .finally(function() {
            self.isSavingForm(false);
        });
    };

    // Show summary before submit (önce özet göster, onay al)
    self.showSummary = function() {
        // Zorunlu alan kontrolü
        var validationErrors = self.validateRequiredFields();
        if (validationErrors.length > 0) {
            toastr.error(validationErrors.join('<br>'), T('Evaluation.ValidationError', 'Zorunlu Alanlar'), { enableHtml: true });
            return;
        }

        // Cevapları hazırla (sorular + verilen cevaplar)
        var answersForSummary = [];
        if (self.formData()) {
            self.formData().sections.forEach(function(section) {
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
            return ko.unwrap(d);
        }).filter(function(d) {
            return d && d.trim().length > 0;
        });

        // Özet verilerini hazırla ve göster (backend'e gitmeden)
        self.summaryData({
            totalScore: self.totalScoreCalc(),
            maxScore: self.maxScoreCalc(),
            scorePercentage: self.scorePercentageCalc(),
            yellowCardCount: self.yellowCardCountCalc(),
            redCardCount: self.redCardCountCalc(),
            scoredWeight: self.scoredWeightCalc(),
            yellowCardWeight: self.yellowCardWeightCalc(),
            redCardWeight: self.redCardWeightCalc(),
            evaluatedPersonnelName: self.availablePersonnel().find(function(p) {
                return p.id === self.evaluatedPersonnelId();
            })?.name || self.evaluatedUnknownPersonnel() || '-',
            callId: self.callId() || '-',
            callDate: self.callDate() || '-',
            callTime: self.callTime() || '-',
            duration: self.duration() || '-',
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

    // Confirm and submit evaluation (onaylandığında backend'e kaydet)
    self.confirmSubmit = function() {
        self.isSavingForm(true);        var data = self.prepareData();
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
        .then(function(result) {
            // API { message, evaluation } döndürüyor
            var newEvaluation = result.evaluation || result;

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

            toastr.success(T('Evaluation.SubmitSuccess', 'Değerlendirme başarıyla kaydedildi.'));
            self.closeEvaluateModal();
            self.loadEvaluations();
        })
        .catch(function(error) {
            console.error('Submit error:', error);
            toastr.error(T('Evaluation.SubmitErrorMessage', 'Değerlendirme gönderilirken bir hata oluştu.'));
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

// Translation keys
var TRANSLATION_KEYS = [
    'Evaluation.LoadError',
    'Evaluation.NotFound',
    'Evaluation.DetailsLoadError',
    'Evaluation.RevertRequestSent',
    'Evaluation.RevertRequestFailed',
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
    // Confirm modal keys
    'Confirm.Title',
    'Confirm.Message',
    'Common.Confirm'
];

// Apply bindings when DOM is ready
document.addEventListener('DOMContentLoaded', function() {
    Localization.loadKeys(TRANSLATION_KEYS).then(function() {
        ko.applyBindings(new EvaluationsViewModel(), document.getElementById('evaluations-app'));

        // Initialize tooltips
        var tooltipTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="tooltip"]'));
        tooltipTriggerList.map(function (tooltipTriggerEl) {
            return new bootstrap.Tooltip(tooltipTriggerEl);
        });
    });
});
