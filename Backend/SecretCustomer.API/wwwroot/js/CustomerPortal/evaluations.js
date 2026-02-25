// CustomerPortal Evaluations ViewModel - Müşteri Portalı Değerlendirme Sayfası
// Evaluations/Index.js ile aynı işlevsellik
function EvaluationsViewModel() {
    var self = this;

    // Score helpers (parametric thresholds)
    self.getScoreClass = function(score, projectTypeId) {
        return ScoreThresholds.getScoreClass(score, projectTypeId);
    };
    self.getProgressBarClass = function(score, projectTypeId) {
        return ScoreThresholds.getProgressBarClass(score, projectTypeId);
    };

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
    self.expiredSearch = ko.observable('');

    // Tab değiştirme fonksiyonu - Dinlemeler/Ziyaretler tabına geçince listeyi yenile
    self.setActiveTab = function(tab) {
        self.activeTab(tab);
        if (tab === 'evaluations') {
            self.loadEvaluations();
        }
    };

    // ==================== EVALUATIONS FILTER SYSTEM ====================
    self.evalSelectedFilterType = ko.observable('');
    self.evalActiveFilters = ko.observableArray([]);

    // Temp filter values
    self.evalTempFilter = {
        status: ko.observable(''),
        searchTerm: ko.observable(''),
        personnelName: ko.observable(''),
        projectId: ko.observable(''),
        startDate: ko.observable(''),
        endDate: ko.observable(''),
        selectedDateRangeType: ko.observable(null),
        // Değerlendirme Tarihi (CreatedAt) filter
        controlStartDate: ko.observable(''),
        controlEndDate: ko.observable(''),
        selectedControlDateRangeType: ko.observable(null)
    };

    // Status labels for display
    self.evalStatusLabels = {
        'Completed': 'Tamamlandı',
        'Draft': 'Taslak',
        'InProgress': 'Devam Ediyor'
    };

    // Date ranges for quick select
    self.evalDateRanges = [
        { name: 'Bugün', systemName: 'today' },
        { name: 'Dün', systemName: 'yesterday' },
        { name: 'Bu Hafta', systemName: 'thisWeek' },
        { name: 'Geçen Hafta', systemName: 'lastWeek' },
        { name: 'Bu Ay', systemName: 'thisMonth' },
        { name: 'Geçen Ay', systemName: 'lastMonth' }
    ];

    // Manual date change tracking
    self._evalManualDateChange = true;
    self.evalTempFilter.startDate.subscribe(function() {
        if (self._evalManualDateChange) self.evalTempFilter.selectedDateRangeType(null);
    });
    self.evalTempFilter.endDate.subscribe(function() {
        if (self._evalManualDateChange) self.evalTempFilter.selectedDateRangeType(null);
    });
    self.evalTempFilter.controlStartDate.subscribe(function() {
        if (self._evalManualDateChange) self.evalTempFilter.selectedControlDateRangeType(null);
    });
    self.evalTempFilter.controlEndDate.subscribe(function() {
        if (self._evalManualDateChange) self.evalTempFilter.selectedControlDateRangeType(null);
    });

    // Can add filter check
    self.evalCanAddFilter = ko.computed(function() {
        var type = self.evalSelectedFilterType();
        if (!type) return false;
        switch (type) {
            case 'status': return self.evalTempFilter.status();
            case 'search': return self.evalTempFilter.searchTerm().trim() !== '';
            case 'personnel': return self.evalTempFilter.personnelName().trim() !== '';
            case 'project': return self.evalTempFilter.projectId();
            case 'dateRange': return self.evalTempFilter.startDate() || self.evalTempFilter.endDate();
            case 'controlDate': return self.evalTempFilter.controlStartDate() || self.evalTempFilter.controlEndDate();
            default: return false;
        }
    });

    // Format date helper
    self._evalFormatDate = function(date) {
        var d = new Date(date);
        var month = '' + (d.getMonth() + 1);
        var day = '' + d.getDate();
        var year = d.getFullYear();
        if (month.length < 2) month = '0' + month;
        if (day.length < 2) day = '0' + day;
        return [year, month, day].join('-');
    };

    // Helper: aktif filtrelerden URL query parametreleri oluştur
    self._evalBuildQueryParams = function() {
        var filters = self.evalActiveFilters();
        var params = [];

        filters.forEach(function(f) {
            switch (f.type) {
                case 'status':
                    params.push('statuses=' + encodeURIComponent(f.value));
                    break;
                case 'search':
                    params.push('search=' + encodeURIComponent(f.value));
                    break;
                case 'personnel':
                    params.push('personnel=' + encodeURIComponent(f.value));
                    break;
                case 'project':
                    params.push('projectIds=' + encodeURIComponent(f.value));
                    break;
                case 'dateRange':
                    if (f.value.start) params.push('startDate=' + encodeURIComponent(f.value.start));
                    if (f.value.end) params.push('endDate=' + encodeURIComponent(f.value.end));
                    break;
                case 'controlDate':
                    if (f.value.start) params.push('controlStartDate=' + encodeURIComponent(f.value.start));
                    if (f.value.end) params.push('controlEndDate=' + encodeURIComponent(f.value.end));
                    break;
            }
        });

        return params.length > 0 ? '?' + params.join('&') : '';
    };

    // Set temp date range (quick select)
    self.evalSetTempDateRange = function(range) {
        self._evalManualDateChange = false;
        var today = new Date();
        var startDate, endDate;

        switch (range) {
            case 'today':
                startDate = endDate = today;
                break;
            case 'yesterday':
                startDate = endDate = new Date(today.getFullYear(), today.getMonth(), today.getDate() - 1);
                break;
            case 'thisWeek':
                var dayOfWeek = today.getDay();
                var diff = today.getDate() - dayOfWeek + (dayOfWeek === 0 ? -6 : 1);
                startDate = new Date(today.getFullYear(), today.getMonth(), diff);
                endDate = today;
                break;
            case 'lastWeek':
                var dayOfWeek = today.getDay();
                var diff = today.getDate() - dayOfWeek + (dayOfWeek === 0 ? -6 : 1);
                startDate = new Date(today.getFullYear(), today.getMonth(), diff - 7);
                endDate = new Date(today.getFullYear(), today.getMonth(), diff - 1);
                break;
            case 'thisMonth':
                startDate = new Date(today.getFullYear(), today.getMonth(), 1);
                endDate = today;
                break;
            case 'lastMonth':
                startDate = new Date(today.getFullYear(), today.getMonth() - 1, 1);
                endDate = new Date(today.getFullYear(), today.getMonth(), 0);
                break;
        }

        if (startDate) self.evalTempFilter.startDate(self._evalFormatDate(startDate));
        if (endDate) self.evalTempFilter.endDate(self._evalFormatDate(endDate));
        self.evalTempFilter.selectedDateRangeType(range);
        self._evalManualDateChange = true;
    };

    // Set temp control date range (quick select for Değerlendirme Tarihi)
    self.evalSetTempControlDateRange = function(range) {
        self._evalManualDateChange = false;
        var today = new Date();
        var startDate, endDate;

        switch (range) {
            case 'today':
                startDate = endDate = today;
                break;
            case 'yesterday':
                startDate = endDate = new Date(today.getFullYear(), today.getMonth(), today.getDate() - 1);
                break;
            case 'thisWeek':
                var dayOfWeek = today.getDay();
                var diff = today.getDate() - dayOfWeek + (dayOfWeek === 0 ? -6 : 1);
                startDate = new Date(today.getFullYear(), today.getMonth(), diff);
                endDate = today;
                break;
            case 'lastWeek':
                var dayOfWeek = today.getDay();
                var diff = today.getDate() - dayOfWeek + (dayOfWeek === 0 ? -6 : 1);
                startDate = new Date(today.getFullYear(), today.getMonth(), diff - 7);
                endDate = new Date(today.getFullYear(), today.getMonth(), diff - 1);
                break;
            case 'thisMonth':
                startDate = new Date(today.getFullYear(), today.getMonth(), 1);
                endDate = today;
                break;
            case 'lastMonth':
                startDate = new Date(today.getFullYear(), today.getMonth() - 1, 1);
                endDate = new Date(today.getFullYear(), today.getMonth(), 0);
                break;
        }

        if (startDate) self.evalTempFilter.controlStartDate(self._evalFormatDate(startDate));
        if (endDate) self.evalTempFilter.controlEndDate(self._evalFormatDate(endDate));
        self.evalTempFilter.selectedControlDateRangeType(range);
        self._evalManualDateChange = true;
    };

    // Add filter - tüm filtre tipleri çoğul değer destekler (internalEvaluations pattern'i)
    self.evalAddFilter = function() {
        var type = self.evalSelectedFilterType();
        if (!type) return;

        var filter = {
            type: type,
            label: '',
            value: null,
            displayValue: ''
        };

        switch (type) {
            case 'status':
                var status = self.evalTempFilter.status();
                if (!status) return;
                filter.label = 'Durum';
                filter.value = status;
                filter.displayValue = self.evalStatusLabels[status] || status;
                break;

            case 'search':
                var searchTerm = self.evalTempFilter.searchTerm().trim();
                if (!searchTerm) return;
                filter.label = 'Arama';
                filter.value = searchTerm;
                filter.displayValue = '"' + searchTerm + '"';
                break;

            case 'personnel':
                var personnelName = self.evalTempFilter.personnelName().trim();
                if (!personnelName) return;
                filter.label = 'Temsilci';
                filter.value = personnelName;
                filter.displayValue = '"' + personnelName + '"';
                break;

            case 'project':
                var selectedProjectId = self.evalTempFilter.projectId();
                if (!selectedProjectId) return;
                var proj = self.availableProjects().find(function(p) { return String(p.id) === String(selectedProjectId); });
                filter.label = 'Proje';
                filter.value = parseInt(selectedProjectId);
                filter.displayValue = proj ? proj.name : selectedProjectId;
                break;

            case 'dateRange':
                var startDate = self.evalTempFilter.startDate();
                var endDate = self.evalTempFilter.endDate();
                if (!startDate && !endDate) return;
                filter.label = 'Çağrı Tarihi';
                filter.value = { start: startDate, end: endDate };
                if (startDate && endDate) {
                    filter.displayValue = startDate + ' - ' + endDate;
                } else if (startDate) {
                    filter.displayValue = startDate + ' sonrası';
                } else {
                    filter.displayValue = endDate + ' öncesi';
                }
                break;

            case 'controlDate':
                var controlStartDate = self.evalTempFilter.controlStartDate();
                var controlEndDate = self.evalTempFilter.controlEndDate();
                if (!controlStartDate && !controlEndDate) return;
                filter.label = 'Değerlendirme Tarihi';
                filter.value = { start: controlStartDate, end: controlEndDate };
                if (controlStartDate && controlEndDate) {
                    filter.displayValue = controlStartDate + ' - ' + controlEndDate;
                } else if (controlStartDate) {
                    filter.displayValue = controlStartDate + ' sonrası';
                } else {
                    filter.displayValue = controlEndDate + ' öncesi';
                }
                break;
        }

        self.evalActiveFilters.push(filter);
        self.evalResetTempFilter();
        self.loadEvaluations();
    };

    // Temp filter değerlerini sıfırla
    self.evalResetTempFilter = function() {
        self.evalSelectedFilterType('');
        self.evalTempFilter.status('');
        self.evalTempFilter.searchTerm('');
        self.evalTempFilter.personnelName('');
        self.evalTempFilter.projectId('');
        self.evalTempFilter.startDate('');
        self.evalTempFilter.endDate('');
        self.evalTempFilter.selectedDateRangeType(null);
        self.evalTempFilter.controlStartDate('');
        self.evalTempFilter.controlEndDate('');
        self.evalTempFilter.selectedControlDateRangeType(null);
    };

    // Remove filter
    self.evalRemoveFilter = function(filter) {
        self.evalActiveFilters.remove(filter);
        self.loadEvaluations();
    };

    // Clear all filters
    self.evalClearFilters = function() {
        self.evalActiveFilters.removeAll();
        self.evalResetTempFilter();
        self.loadEvaluations();
    };

    // List Data
    self.allAssignments = ko.observableArray([]);
    self.allEvaluations = ko.observableArray([]);

    // Available projects for filter dropdown (allEvaluations tanımlandıktan sonra)
    // Mevcut projeler listesi (evaluations'dan çıkarılır) - {id, name} objeleri
    self.availableProjects = ko.computed(function() {
        var projects = [];
        var seen = {};
        self.allEvaluations().forEach(function(e) {
            if (e.projectId && !seen[e.projectId]) {
                seen[e.projectId] = true;
                projects.push({ id: e.projectId, name: e.projectName || '' });
            }
        });
        return projects.sort(function(a, b) { return a.name.localeCompare(b.name); });
    });

    // Sorting states for each table
    self.assignmentsSorting = TableSorting.createSortState('dueDate', 'asc');
    self.evaluationsSorting = TableSorting.createSortState('callDate', 'desc');
    self.expiredSorting = TableSorting.createSortState('dueDate', 'desc');

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
    self.isUploadingFile = ko.observable(false);
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
    self.descriptions = ko.observableArray([{text: ko.observable('')}]); // Wrapper object pattern
    self.pastDescriptions = ko.observableArray([]);
    self.availablePersonnel = ko.observableArray([]);
    self.isLoadingPersonnel = ko.observable(false);
    self.evaluatedPersonnelId = ko.observable(null);
    self.evaluatedUnknownPersonnel = ko.observable('');
    self.evaluationComment = ko.observable('');

    // New Personnel Mode (Listede Yok)
    self.isNewPersonnelMode = ko.observable(false);
    self.newPersonnelFirstName = ko.observable('');
    self.newPersonnelLastName = ko.observable('');

    // Personnel Autocomplete
    self.personnelSearchText = ko.observable('');
    self.isPersonnelDropdownVisible = ko.observable(false);
    self.selectedPersonnelName = ko.observable('');
    self._personnelDropdownTimeout = null;

    // Filtered personnel based on search text (startsWith pattern)
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
    // LIST COMPUTED
    // ========================

    // Sekme 1: Aktif Atamalar (tarihi geçmemiş, tamamlanmamış)
    self.activeAssignments = ko.computed(function() {
        var assignments = self.allAssignments();
        var search = self.assignmentsSearch().toLowerCase();
        var today = new Date();
        today.setHours(0, 0, 0, 0);
        var sortBy = self.assignmentsSorting.sortBy();
        var sortDir = self.assignmentsSorting.sortDirection();

        var filtered = assignments.filter(function(a) {
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

        return TableSorting.clientSort(filtered, sortBy, sortDir);
    });

    // Sekme 2: Tüm Dinlemeler - Server-side filtreleme, client-side sadece sıralama
    self.allEvaluationsList = ko.computed(function() {
        var sortBy = self.evaluationsSorting.sortBy();
        var sortDir = self.evaluationsSorting.sortDirection();
        return TableSorting.clientSort(self.allEvaluations(), sortBy, sortDir);
    });

    // Sekme 3: Tarihi Geçmiş Atamalar (hala dinleme eklenebilir)
    self.expiredAssignments = ko.computed(function() {
        var assignments = self.allAssignments();
        var search = self.expiredSearch().toLowerCase();
        var today = new Date();
        today.setHours(0, 0, 0, 0);
        var sortBy = self.expiredSorting.sortBy();
        var sortDir = self.expiredSorting.sortDirection();

        var filtered = assignments.filter(function(a) {
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

        return TableSorting.clientSort(filtered, sortBy, sortDir);
    });

    // Admin mi kontrolü
    self.isAdmin = ko.computed(function() {
        return self.currentUserRole() === 'Admin';
    });

    // Sadece taslak (Draft) durumundaki evaluation düzenlenebilir
    self.canEditEvaluation = function(evaluation) {
        return evaluation.status === 'Draft';
    };

    // Mail bildirim gönder (tamamlanmış + mail gönderilmemiş)
    self.sendNotification = function(evaluation) {
        showConfirmModal(T('Evaluation.MailSendConfirm', 'Bu değerlendirme için bildirim maili gönderilsin mi?'), function() {
            fetch('/api/evaluations/' + evaluation.id + '/send-notification', {
                method: 'POST',
                credentials: 'include'
            })
            .then(function(response) {
                if (!response.ok) return response.json().then(function(err) { throw new Error(err.message || 'Hata'); });
                return response.json();
            })
            .then(function(result) {
                toastr.success(T('Evaluation.MailSentSuccess', 'Mail başarıyla gönderildi'));
                // Listedeki öğeyi güncelle (liste yenileme yerine)
                var items = self.allEvaluations();
                for (var i = 0; i < items.length; i++) {
                    if (items[i].id === evaluation.id) {
                        items[i].isNotificationSent = true;
                        break;
                    }
                }
                self.allEvaluations(items);
            })
            .catch(function(error) {
                toastr.error(error.message || 'Mail gönderilirken bir hata oluştu.');
            });
        });
    };

    // ========================
    // LIST FUNCTIONS
    // ========================

    self.loadEvaluations = function() {
        self.isLoading(true);
        self.errorMessage('');

        // Aktif filtrelerden query parametreleri oluştur
        var queryParams = self._evalBuildQueryParams();

        Promise.all([
            fetch('/api/assignments/my-assignments', { credentials: 'include' }).then(function(r) { return r.json(); }),
            fetch('/api/evaluations/evaluator' + queryParams, { credentials: 'include' }).then(function(r) { return r.json(); }),
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
    // EVALUATE MODAL FUNCTIONS
    // ========================

    self.startEvaluation = function(assignment) {
        // scoringMethod'a göre doğru popup'ı aç (isInternal=true ile)
        self.openEvaluationPopup(assignment.id, null, assignment.scoringMethod);
    };

    self.continueEvaluation = function(evaluation) {
        // scoringMethod'a göre doğru popup'ı aç (isInternal=true ile)
        self.openEvaluationPopup(null, evaluation.id, evaluation.scoringMethod);
    };

    // Değerlendirme popup'ı aç (scoringMethod'a göre farklı URL, isInternal=true)
    self.openEvaluationPopup = function(assignmentId, evaluationId, scoringMethod) {
        var width = 1200;
        var height = 800;
        var left = (screen.width - width) / 2;
        var top = (screen.height - height) / 2;

        var baseUrl = '/Evaluations/';
        if (scoringMethod === 'CriteriaTotal') {
            baseUrl += 'PopupCriteriaTotal?';
        } else {
            baseUrl += 'PopupMaximum?';
        }

        var url = baseUrl;
        if (assignmentId) url += 'assignmentId=' + assignmentId + '&';
        if (evaluationId) url += 'evaluationId=' + evaluationId + '&';
        url += 'isInternal=true';

        window.open(url, 'EvaluationPopup',
            'width=' + width + ',height=' + height + ',left=' + left + ',top=' + top + ',resizable=yes,scrollbars=yes');
    };

    self.openEvaluateModal = function() {
        self.isEvaluateModalOpen(true);
        self.isFormLoading(true);
        self.formData(null);
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
        if (typeof Inputmask !== 'undefined') {
            Inputmask('99:99', { insertMode: false }).mask('.time-mask');
            Inputmask('99:99:99', { insertMode: false }).mask('.duration-mask');
        }

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
        self.descriptions([{text: ko.observable('')}]); // Wrapper object pattern
        self.availablePersonnel([]);
        self.evaluatedPersonnelId(null);
        self.evaluatedUnknownPersonnel('');
        self.evaluationComment('');
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

    self.closeEvaluateModal = function() {
        self.isEvaluateModalOpen(false);
        self.formData(null);
        self.currentAssignmentId = null;
        self.currentEvaluationId = null;
        self.isShowingSummary(false);
        self.summaryData(null);
    };

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
    // EVALUATION ATTACHMENTS (Değerlendirme Geneli Dosyalar)
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

    // Load form data
    self.loadForm = function() {
        self.isFormLoading(true);
        var url = '';
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

        self.formData().penaltyGroups.forEach(function(section) {
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

                // Skip unscored questions
                if (q.scoringType === 'Unscored') return;

                // Zorunlu olmayan Scored sorular için isIncluded kontrolü
                // Penalty sorular bu kontrolden muaf (her zaman opsiyonel)
                if (q.scoringType === 'Scored' && !q.isRequired) {
                    var isIncluded = answer.isIncluded ? answer.isIncluded() : true;
                    if (!isIncluded) return;
                }

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
                questionId: questionId,
                answerText: a.answerText() || null,
                answerNumeric: answerNumericVal,
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

        return {
            projectId: self.formData().projectId,
            assignmentId: self.formData().assignmentId || null,
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
            controlDate: null,
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

        if (!self.callId() || !self.callId().trim()) {
            errors.push(T('Evaluation.CallIdRequired', 'Çağrı ID zorunludur'));
        }
        if (!self.callDate()) {
            errors.push(T('Evaluation.CallDateRequired', 'Çağrı Tarihi zorunludur'));
        }
        if (!self.callTime() || self.callTime().indexOf('_') >= 0 || !/^\d{2}:\d{2}$/.test(self.callTime())) {
            errors.push(T('Evaluation.CallTimeRequired', 'Çağrı Saati zorunludur (SS:DD formatında giriniz)'));
        }
        if (!self.duration() || self.duration().indexOf('_') >= 0 || !/^\d{2}:\d{2}:\d{2}$/.test(self.duration()) || parseInt(self.duration().replace(/\D/g, ''), 10) === 0) {
            errors.push(T('Evaluation.DurationRequired', 'Süre zorunludur (SS:DD:SN formatında giriniz)'));
        }

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
            var projectId = self.formData() ? self.formData().projectId : null;
            var evaluationId = self.formData() ? self.formData().evaluationId : null;
            if (!projectId) {
                resolve(false);
                return;
            }
            var url = '/api/evaluations/check-call-id?callId=' + encodeURIComponent(callId) +
                      '&projectId=' + projectId;
            if (evaluationId) {
                url += '&evaluationId=' + evaluationId;
            }
            fetch(url, { credentials: 'include' })
                .then(function(response) { return response.json(); })
                .then(function(data) { resolve(data.exists === true); })
                .catch(function() { resolve(false); });
        });
    };

    // Save as draft
    self.saveDraft = function(callback) {
        // Zorunlu alan kontrolü
        var validationErrors = self.validateRequiredFields();
        if (validationErrors.length > 0) {
            toastr.error(validationErrors.join('<br>'), T('Evaluation.ValidationError', 'Zorunlu Alanlar'), { enableHtml: true });
            return;
        }

        self.isSavingForm(true);

        // CallId tekrar kontrolü
        self.checkCallIdExists().then(function(exists) {
            if (exists) {
                self.isSavingForm(false);
                toastr.error(T('Evaluation.CallIdExists', 'Bu Çağrı ID daha önce kaydedilmiş. Aynı Çağrı ID ile yeni dinleme eklenemez.'));
                return;
            }

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
                // API { message, evaluation, answers } döndürüyor
                var savedEvaluation = result.evaluation || result;

                // Update answer IDs from result
                if (result.answers) {
                    result.answers.forEach(function(a) {
                        // questionId string'e çevir (object keys string olduğu için)
                        var qId = String(a.questionId);
                        if (self.answers[qId]) {
                            self.answers[qId].answerId(a.id);
                        }
                    });
                }

                // Pending dosyaları yükle (değerlendirme ID ile)
                return self.uploadPendingAttachments(savedEvaluation.id).then(function() {
                    if (typeof callback === 'function') {
                        // Called from file upload - just call callback
                        callback();
                    } else {
                        toastr.success(T('Evaluation.DraftSaved', 'Taslak başarıyla kaydedildi.'));
                        // Taslak kaydedilince modal kapansın ve liste yenilensin
                        self.closeEvaluateModal();
                        self.loadEvaluations();
                    }
                });
            })
            .catch(function(error) {
                console.error('Draft save error:', error);
                toastr.error(T('Evaluation.DraftSaveError', 'Taslak kaydedilirken bir hata oluştu.'));
            })
            .finally(function() {
                self.isSavingForm(false);
            });
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

        // CallId tekrar kontrolü
        self.checkCallIdExists().then(function(exists) {
            if (exists) {
                toastr.error(T('Evaluation.CallIdExists', 'Bu Çağrı ID daha önce kaydedilmiş. Aynı Çağrı ID ile yeni dinleme eklenemez.'));
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
        });
    };

    // Go back to form from summary (özetten forma geri dön)
    self.backToForm = function() {
        self.isShowingSummary(false);
    };

    // Confirm and submit evaluation (onaylandığında backend'e kaydet)
    self.confirmSubmit = function() {
        self.isSavingForm(true);
        var data = self.prepareData();
        var projectId = data.projectId;

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
            // API { message, evaluation, answers } döndürüyor
            var newEvaluation = result.evaluation || result;

            // Update answer IDs from result
            if (result.answers) {
                result.answers.forEach(function(a) {
                    // questionId string'e çevir (object keys string olduğu için)
                    var qId = String(a.questionId);
                    if (self.answers[qId]) {
                        self.answers[qId].answerId(a.id);
                    }
                });
            }

            // Pending dosyaları yükle (değerlendirme ID ile)
            return self.uploadPendingAttachments(newEvaluation.id).then(function() {
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

                // Assignment'i tamamlandi olarak isaretle (projectId ile eşleştir)
                var assignments = self.allAssignments();
                for (var j = 0; j < assignments.length; j++) {
                    if (assignments[j].projectId === projectId) {
                        assignments[j].isCompleted = true;
                        self.allAssignments.splice(j, 1, assignments[j]);
                        break;
                    }
                }

                toastr.success(T('Evaluation.SubmitSuccess', 'Değerlendirme başarıyla kaydedildi.'));
                self.closeEvaluateModal();
                self.loadEvaluations();
            });
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
    // PROJECT FILES MODAL
    // ========================
    self.isProjectFilesModalOpen = ko.observable(false);
    self.isLoadingProjectFiles = ko.observable(false);
    self.projectFiles = ko.observableArray([]);

    self.showProjectFiles = function(assignment) {
        self.isProjectFilesModalOpen(true);
        self.isLoadingProjectFiles(true);
        self.projectFiles([]);

        fetch('/api/project-files/project/' + assignment.projectId, { credentials: 'include' })
            .then(function(response) {
                if (!response.ok) throw new Error('Dosyalar yüklenemedi');
                return response.json();
            })
            .then(function(files) {
                self.projectFiles(files || []);
            })
            .catch(function(error) {
                console.error('Project files load error:', error);
                toastr.error(T('Project.FilesLoadError', 'Dosyalar yüklenirken bir hata oluştu.'));
            })
            .finally(function() {
                self.isLoadingProjectFiles(false);
            });
    };

    self.closeProjectFilesModal = function() {
        self.isProjectFilesModalOpen(false);
        self.projectFiles([]);
    };

    self.downloadProjectFile = function(file) {
        window.location.href = '/api/project-files/' + file.id + '/download';
    };

    // Helper: Format file size
    self.formatFileSize = function(bytes) {
        if (!bytes || bytes === 0) return '0 B';
        var k = 1024;
        var sizes = ['B', 'KB', 'MB', 'GB'];
        var i = Math.floor(Math.log(bytes) / Math.log(k));
        return parseFloat((bytes / Math.pow(k, i)).toFixed(2)) + ' ' + sizes[i];
    };

    // Helper: Get file icon based on content type
    self.getFileIcon = function(contentType) {
        if (!contentType) return 'bi-file-earmark';
        if (contentType.indexOf('pdf') >= 0) return 'bi-file-earmark-pdf text-danger';
        if (contentType.indexOf('word') >= 0 || contentType.indexOf('document') >= 0) return 'bi-file-earmark-word text-primary';
        if (contentType.indexOf('excel') >= 0 || contentType.indexOf('spreadsheet') >= 0) return 'bi-file-earmark-excel text-success';
        if (contentType.indexOf('image') >= 0) return 'bi-file-earmark-image text-info';
        if (contentType.indexOf('zip') >= 0 || contentType.indexOf('rar') >= 0) return 'bi-file-earmark-zip text-warning';
        if (contentType.indexOf('text') >= 0) return 'bi-file-earmark-text';
        return 'bi-file-earmark';
    };

    // ========================
    // DELETE DRAFT
    // ========================
    self.deleteDraft = function(evaluation) {
        if (evaluation.status !== 'Draft') {
            toastr.error(T('Evaluation.OnlyDraftCanBeDeleted', 'Sadece taslak durumundaki değerlendirmeler silinebilir.'));
            return;
        }

        showDeleteConfirm(T('Evaluation.DraftEvaluation', 'Taslak Değerlendirme'), function() {
            fetch('/api/customer/portal/evaluations/' + evaluation.id, {
                method: 'DELETE',
                credentials: 'include'
            })
            .then(function(response) {
                if (!response.ok) {
                    return response.json().then(function(data) {
                        throw new Error(data.message || T('Common.DeleteError', 'Silme işlemi başarısız'));
                    });
                }
                return response.json();
            })
            .then(function() {
                self.allEvaluations.remove(evaluation);
                toastr.success(T('Evaluation.DraftDeleted', 'Taslak başarıyla silindi.'));
            })
            .catch(function(error) {
                console.error('Delete error:', error);
                toastr.error(error.message || T('Evaluation.DeleteErrorMessage', 'Taslak silinirken bir hata oluştu.'));
            });
        });
    };

    // ========================
    // INITIALIZE
    // ========================
    // Varsayılan bugün filtresi ekle
    var today = self._evalFormatDate(new Date());
    self.evalActiveFilters.push({
        type: 'controlDate',
        label: 'Değerlendirme Tarihi',
        value: { start: today, end: today },
        displayValue: today + ' - ' + today
    });

    // Once EnumsService'i yukle, sonra diger verileri cek
    if (typeof EnumsService !== 'undefined') {
        EnumsService.load().then(function() {
            self.loadEvaluations();
        });
    } else {
        self.loadEvaluations();
    }
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
    'Evaluation.FormLoadError',
    'Evaluation.PersonnelRequired',
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
    // Confirm modal keys
    'Common.Confirmation',
    'Confirm.Message',
    'Common.Confirm',
    // Project files keys
    'Project.FilesLoadError'
];

// Apply bindings when DOM is ready
document.addEventListener('DOMContentLoaded', function() {
    if (typeof Localization !== 'undefined') {
        Localization.loadKeys(TRANSLATION_KEYS).then(function() {
            ko.applyBindings(new EvaluationsViewModel(), document.getElementById('evaluations-app'));

            // Initialize tooltips
            var tooltipTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="tooltip"]'));
            tooltipTriggerList.map(function (tooltipTriggerEl) {
                return new bootstrap.Tooltip(tooltipTriggerEl);
            });
        });
    } else {
        ko.applyBindings(new EvaluationsViewModel(), document.getElementById('evaluations-app'));
    }
});
