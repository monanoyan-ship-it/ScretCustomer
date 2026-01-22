// Evaluations ViewModel - Çağrı Denetleme (Birleştirilmiş: Liste + Detay + Değerlendirme Formu)
function EvaluationsViewModel() {
    var self = this;

    // ========================
    // LIST STATE
    // ========================
    self.isLoading = ko.observable(true);
    self.isAssignmentsLoading = ko.observable(true); // Assignments için ayrı loading state
    self.errorMessage = ko.observable('');
    self.activeTab = ko.observable('assignments');
    self.currentUserRole = ko.observable(''); // Kullanıcı rolü (Admin kontrolü için)
    self.filterStatus = ko.observable('');
    // Her tab için ayrı search
    self.assignmentsSearch = ko.observable('');
    self.expiredSearch = ko.observable('');

    // Dinlemeler/Ziyaretler pagination
    self.evaluationsPage = ko.observable(1);
    self.evaluationsPageSize = ko.observable(20);

    // ==================== EVALUATIONS FILTER SYSTEM ====================
    self.evalSelectedFilterType = ko.observable('');
    self.evalActiveFilters = ko.observableArray([]);

    // Temp filter values
    self.evalTempFilter = {
        status: ko.observable(''),
        searchTerm: ko.observable(''),
        personnelName: ko.observable(''),
        startDate: ko.observable(''),
        endDate: ko.observable(''),
        selectedDateRangeType: ko.observable(null),
        // Kontrol Tarihi (CreatedAt) filter
        controlStartDate: ko.observable(''),
        controlEndDate: ko.observable(''),
        selectedControlDateRangeType: ko.observable(null)
    };

    // Filter labels
    self.evalFilterLabels = {
        status: 'Durum',
        search: 'Arama',
        personnel: 'Temsilci',
        dateRange: 'Tarih',
        controlDate: 'Kontrol Tarihi'
    };

    self.evalStatusLabels = {
        'Completed': 'Tamamlandı',
        'Draft': 'Taslak',
        'InProgress': 'Devam Ediyor'
    };

    // Date range options
    self.evalDateRanges = [
        { systemName: 'today', name: 'Bugün' },
        { systemName: 'yesterday', name: 'Dün' },
        { systemName: 'thisWeek', name: 'Bu Hafta' },
        { systemName: 'lastWeek', name: 'Geçen Hafta' },
        { systemName: 'thisMonth', name: 'Bu Ay' },
        { systemName: 'lastMonth', name: 'Geçen Ay' },
        { systemName: 'last7Days', name: 'Son 7 Gün' },
        { systemName: 'last30Days', name: 'Son 30 Gün' }
    ];

    // Date range quick select tracking
    self._evalManualDateChange = true;
    self.evalTempFilter.startDate.subscribe(function() {
        if (self._evalManualDateChange) self.evalTempFilter.selectedDateRangeType(null);
    });
    self.evalTempFilter.endDate.subscribe(function() {
        if (self._evalManualDateChange) self.evalTempFilter.selectedDateRangeType(null);
    });

    // Can add filter check
    self.evalCanAddFilter = ko.computed(function() {
        var type = self.evalSelectedFilterType();
        if (!type) return false;
        switch (type) {
            case 'status': return self.evalTempFilter.status();
            case 'search': return self.evalTempFilter.searchTerm().trim() !== '';
            case 'personnel': return self.evalTempFilter.personnelName().trim() !== '';
            case 'dateRange': return self.evalTempFilter.startDate() || self.evalTempFilter.endDate();
            case 'controlDate': return self.evalTempFilter.controlStartDate() || self.evalTempFilter.controlEndDate();
            default: return false;
        }
    });

    // Helper: format date
    self._evalFormatDate = function(date) {
        var year = date.getFullYear();
        var month = String(date.getMonth() + 1).padStart(2, '0');
        var day = String(date.getDate()).padStart(2, '0');
        return year + '-' + month + '-' + day;
    };

    // Set temp date range (quick select)
    self.evalSetTempDateRange = function(range) {
        var today = new Date();
        today.setHours(0, 0, 0, 0);
        var startDate = null;
        var endDate = null;

        var getMonday = function(d) {
            var date = new Date(d.getTime());
            var day = date.getDay();
            var diff = date.getDate() - day + (day === 0 ? -6 : 1);
            date.setDate(diff);
            return date;
        };

        switch (range) {
            case 'today':
                startDate = new Date(today.getTime());
                endDate = new Date(today.getTime());
                break;
            case 'yesterday':
                var yesterday = new Date(today.getTime());
                yesterday.setDate(yesterday.getDate() - 1);
                startDate = yesterday;
                endDate = new Date(yesterday.getTime());
                break;
            case 'thisWeek':
                startDate = getMonday(today);
                endDate = new Date(today.getTime());
                break;
            case 'lastWeek':
                var lastWeekStart = getMonday(today);
                lastWeekStart.setDate(lastWeekStart.getDate() - 7);
                var lastWeekEnd = new Date(lastWeekStart.getTime());
                lastWeekEnd.setDate(lastWeekEnd.getDate() + 6);
                startDate = lastWeekStart;
                endDate = lastWeekEnd;
                break;
            case 'thisMonth':
                startDate = new Date(today.getFullYear(), today.getMonth(), 1);
                endDate = new Date(today.getTime());
                break;
            case 'lastMonth':
                startDate = new Date(today.getFullYear(), today.getMonth() - 1, 1);
                endDate = new Date(today.getFullYear(), today.getMonth(), 0);
                break;
            case 'last7Days':
                startDate = new Date(today.getTime());
                startDate.setDate(startDate.getDate() - 6);
                endDate = new Date(today.getTime());
                break;
            case 'last30Days':
                startDate = new Date(today.getTime());
                startDate.setDate(startDate.getDate() - 29);
                endDate = new Date(today.getTime());
                break;
        }

        self._evalManualDateChange = false;
        if (startDate) self.evalTempFilter.startDate(self._evalFormatDate(startDate));
        if (endDate) self.evalTempFilter.endDate(self._evalFormatDate(endDate));
        self.evalTempFilter.selectedDateRangeType(range);
        self._evalManualDateChange = true;
    };

    // Control Date quick select tracking
    self._evalManualControlDateChange = true;
    self.evalTempFilter.controlStartDate.subscribe(function() {
        if (self._evalManualControlDateChange) self.evalTempFilter.selectedControlDateRangeType(null);
    });
    self.evalTempFilter.controlEndDate.subscribe(function() {
        if (self._evalManualControlDateChange) self.evalTempFilter.selectedControlDateRangeType(null);
    });

    // Set temp control date range (quick select)
    self.evalSetTempControlDateRange = function(range) {
        var today = new Date();
        today.setHours(0, 0, 0, 0);
        var startDate = null;
        var endDate = null;

        var getMonday = function(d) {
            var date = new Date(d.getTime());
            var day = date.getDay();
            var diff = date.getDate() - day + (day === 0 ? -6 : 1);
            date.setDate(diff);
            return date;
        };

        switch (range) {
            case 'today':
                startDate = new Date(today.getTime());
                endDate = new Date(today.getTime());
                break;
            case 'yesterday':
                var yesterday = new Date(today.getTime());
                yesterday.setDate(yesterday.getDate() - 1);
                startDate = yesterday;
                endDate = new Date(yesterday.getTime());
                break;
            case 'thisWeek':
                startDate = getMonday(today);
                endDate = new Date(today.getTime());
                break;
            case 'lastWeek':
                var lastWeekStart = getMonday(today);
                lastWeekStart.setDate(lastWeekStart.getDate() - 7);
                var lastWeekEnd = new Date(lastWeekStart.getTime());
                lastWeekEnd.setDate(lastWeekEnd.getDate() + 6);
                startDate = lastWeekStart;
                endDate = lastWeekEnd;
                break;
            case 'thisMonth':
                startDate = new Date(today.getFullYear(), today.getMonth(), 1);
                endDate = new Date(today.getTime());
                break;
            case 'lastMonth':
                startDate = new Date(today.getFullYear(), today.getMonth() - 1, 1);
                endDate = new Date(today.getFullYear(), today.getMonth(), 0);
                break;
            case 'last7Days':
                startDate = new Date(today.getTime());
                startDate.setDate(startDate.getDate() - 6);
                endDate = new Date(today.getTime());
                break;
            case 'last30Days':
                startDate = new Date(today.getTime());
                startDate.setDate(startDate.getDate() - 29);
                endDate = new Date(today.getTime());
                break;
        }

        self._evalManualControlDateChange = false;
        if (startDate) self.evalTempFilter.controlStartDate(self._evalFormatDate(startDate));
        if (endDate) self.evalTempFilter.controlEndDate(self._evalFormatDate(endDate));
        self.evalTempFilter.selectedControlDateRangeType(range);
        self._evalManualControlDateChange = true;
    };

    // Add filter
    self.evalAddFilter = function() {
        var type = self.evalSelectedFilterType();
        if (!type) return;

        // Aynı tipte filtre varsa önce kaldır
        self.evalActiveFilters.remove(function(f) { return f.type === type; });

        var filter = {
            type: type,
            label: self.evalFilterLabels[type],
            value: null,
            displayValue: ''
        };

        switch (type) {
            case 'status':
                var status = self.evalTempFilter.status();
                if (!status) return;
                filter.value = status;
                filter.displayValue = self.evalStatusLabels[status] || status;
                self.evalTempFilter.status('');
                break;

            case 'search':
                var searchTerm = self.evalTempFilter.searchTerm().trim();
                if (!searchTerm) return;
                filter.value = searchTerm;
                filter.displayValue = '"' + searchTerm + '"';
                self.evalTempFilter.searchTerm('');
                break;

            case 'personnel':
                var personnelName = self.evalTempFilter.personnelName().trim();
                if (!personnelName) return;
                filter.value = personnelName;
                filter.displayValue = '"' + personnelName + '"';
                self.evalTempFilter.personnelName('');
                break;

            case 'dateRange':
                var startDate = self.evalTempFilter.startDate();
                var endDate = self.evalTempFilter.endDate();
                if (!startDate && !endDate) return;
                filter.value = { start: startDate, end: endDate };
                if (startDate && endDate) {
                    filter.displayValue = startDate + ' - ' + endDate;
                } else if (startDate) {
                    filter.displayValue = startDate + ' →';
                } else {
                    filter.displayValue = '→ ' + endDate;
                }
                self.evalTempFilter.startDate('');
                self.evalTempFilter.endDate('');
                self.evalTempFilter.selectedDateRangeType(null);
                break;

            case 'controlDate':
                var controlStartDate = self.evalTempFilter.controlStartDate();
                var controlEndDate = self.evalTempFilter.controlEndDate();
                if (!controlStartDate && !controlEndDate) return;
                filter.value = { start: controlStartDate, end: controlEndDate };
                if (controlStartDate && controlEndDate) {
                    filter.displayValue = controlStartDate + ' - ' + controlEndDate;
                } else if (controlStartDate) {
                    filter.displayValue = controlStartDate + ' →';
                } else {
                    filter.displayValue = '→ ' + controlEndDate;
                }
                self.evalTempFilter.controlStartDate('');
                self.evalTempFilter.controlEndDate('');
                self.evalTempFilter.selectedControlDateRangeType(null);
                break;
        }

        self.evalActiveFilters.push(filter);
        self.evalSelectedFilterType('');
        self.evaluationsPage(1);
    };

    // Remove filter
    self.evalRemoveFilter = function(filter) {
        self.evalActiveFilters.remove(filter);
        self.evaluationsPage(1);
    };

    // Clear all filters
    self.evalClearFilters = function() {
        self.evalActiveFilters.removeAll();
        self.evalSelectedFilterType('');
        self.evalTempFilter.status('');
        self.evalTempFilter.searchTerm('');
        self.evalTempFilter.personnelName('');
        self.evalTempFilter.startDate('');
        self.evalTempFilter.endDate('');
        self.evalTempFilter.selectedDateRangeType(null);
        self.evalTempFilter.controlStartDate('');
        self.evalTempFilter.controlEndDate('');
        self.evalTempFilter.selectedControlDateRangeType(null);
        self.evaluationsPage(1);
    };

    // List Data
    self.allAssignments = ko.observableArray([]);
    self.allEvaluations = ko.observableArray([]);

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
    self.callIdExists = ko.observable(false);
    self.isCheckingCallId = ko.observable(false);
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

    // Sekme 2: Tüm Dinlemeler (yapılmış evaluation'lar) - Filtrelenmiş liste
    self.filteredEvaluationsList = ko.computed(function() {
        var filters = self.evalActiveFilters();
        var sortBy = self.evaluationsSorting.sortBy();
        var sortDir = self.evaluationsSorting.sortDirection();

        // Aktif filtreleri çıkar
        var searchFilter = filters.find(function(f) { return f.type === 'search'; });
        var statusFilter = filters.find(function(f) { return f.type === 'status'; });
        var personnelFilter = filters.find(function(f) { return f.type === 'personnel'; });
        var dateFilter = filters.find(function(f) { return f.type === 'dateRange'; });
        var controlDateFilter = filters.find(function(f) { return f.type === 'controlDate'; });

        var search = searchFilter ? searchFilter.value.toLowerCase() : '';
        var status = statusFilter ? statusFilter.value : '';
        var personnelName = personnelFilter ? personnelFilter.value.toLowerCase() : '';
        var dateFrom = dateFilter && dateFilter.value.start ? dateFilter.value.start : '';
        var dateTo = dateFilter && dateFilter.value.end ? dateFilter.value.end : '';
        var controlFrom = controlDateFilter && controlDateFilter.value.start ? controlDateFilter.value.start : '';
        var controlTo = controlDateFilter && controlDateFilter.value.end ? controlDateFilter.value.end : '';

        var filtered = self.allEvaluations().filter(function(e) {
            // Text arama
            if (search) {
                var matchesSearch = (e.projectName || '').toLowerCase().indexOf(search) >= 0 ||
                                   (e.checklistName || '').toLowerCase().indexOf(search) >= 0 ||
                                   (e.evaluatedPersonnelName || '').toLowerCase().indexOf(search) >= 0 ||
                                   (e.evaluatedUnknownPersonnel || '').toLowerCase().indexOf(search) >= 0 ||
                                   (e.callId || '').toLowerCase().indexOf(search) >= 0;
                if (!matchesSearch) return false;
            }
            // Temsilci adı filtresi
            if (personnelName) {
                var matchesPersonnel = (e.evaluatedPersonnelName || '').toLowerCase().indexOf(personnelName) >= 0 ||
                                       (e.evaluatedUnknownPersonnel || '').toLowerCase().indexOf(personnelName) >= 0;
                if (!matchesPersonnel) return false;
            }
            // Durum filtresi
            if (status && e.status !== status) return false;

            // Tarih filtreleri (callDate, completedAt veya createdAt kullan)
            if (dateFrom || dateTo) {
                var dateStr = e.callDate || e.completedAt || e.createdAt;
                if (!dateStr) return false; // Tarihi olmayan kayıtları filtrele

                var evalDate = new Date(dateStr);
                evalDate.setHours(0, 0, 0, 0);

                if (dateFrom) {
                    var fromDate = new Date(dateFrom);
                    fromDate.setHours(0, 0, 0, 0);
                    if (evalDate < fromDate) return false;
                }
                if (dateTo) {
                    var toDate = new Date(dateTo);
                    toDate.setHours(23, 59, 59, 999);
                    if (evalDate > toDate) return false;
                }
            }

            // Kontrol Tarihi filtresi (sadece createdAt kullan)
            if (controlFrom || controlTo) {
                var controlDateStr = e.createdAt;
                if (!controlDateStr) return false;

                var controlDate = new Date(controlDateStr);
                controlDate.setHours(0, 0, 0, 0);

                if (controlFrom) {
                    var cFromDate = new Date(controlFrom);
                    cFromDate.setHours(0, 0, 0, 0);
                    if (controlDate < cFromDate) return false;
                }
                if (controlTo) {
                    var cToDate = new Date(controlTo);
                    cToDate.setHours(23, 59, 59, 999);
                    if (controlDate > cToDate) return false;
                }
            }
            return true;
        });

        return TableSorting.clientSort(filtered, sortBy, sortDir);
    });

    // Sayfa boyutu değişince sayfa 1'e dön
    self.evaluationsPageSize.subscribe(function() { self.evaluationsPage(1); });

    // Pagination computed'ları
    self.evaluationsTotalCount = ko.computed(function() {
        return self.filteredEvaluationsList().length;
    });

    self.evaluationsTotalPages = ko.computed(function() {
        return Math.ceil(self.evaluationsTotalCount() / parseInt(self.evaluationsPageSize(), 10)) || 1;
    });

    // Sayfalanmış liste (view'da kullanılacak)
    self.allEvaluationsList = ko.computed(function() {
        var list = self.filteredEvaluationsList();
        var page = parseInt(self.evaluationsPage(), 10);
        var pageSize = parseInt(self.evaluationsPageSize(), 10);
        var start = (page - 1) * pageSize;
        return list.slice(start, start + pageSize);
    });

    // Pagination fonksiyonları
    self.evaluationsGoToPage = function(page) {
        if (page >= 1 && page <= self.evaluationsTotalPages()) {
            self.evaluationsPage(page);
        }
    };
    self.evaluationsPrevPage = function() {
        if (self.evaluationsPage() > 1) self.evaluationsPage(self.evaluationsPage() - 1);
    };
    self.evaluationsNextPage = function() {
        if (self.evaluationsPage() < self.evaluationsTotalPages()) self.evaluationsPage(self.evaluationsPage() + 1);
    };
    self.evaluationsFirstPage = function() { self.evaluationsPage(1); };
    self.evaluationsLastPage = function() { self.evaluationsPage(self.evaluationsTotalPages()); };

    // Sayfa numaraları dizisi (max 5 sayfa göster)
    self.evaluationsPageNumbers = ko.computed(function() {
        var current = self.evaluationsPage();
        var total = self.evaluationsTotalPages();
        var pages = [];
        var start = Math.max(1, current - 2);
        var end = Math.min(total, start + 4);
        if (end - start < 4) start = Math.max(1, end - 4);
        for (var i = start; i <= end; i++) pages.push(i);
        return pages;
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

    // ========================
    // LIST FUNCTIONS
    // ========================

    self.loadEvaluations = function() {
        self.isLoading(true);
        self.errorMessage('');

        // Önce evaluations ve user bilgisini yükle (hızlı)
        Promise.all([
            fetch('/api/evaluations/evaluator', { credentials: 'include' }).then(function(r) { return r.json(); }),
            fetch('/api/auth/me', { credentials: 'include' }).then(function(r) { return r.json(); })
        ])
        .then(function(results) {
            console.log('[Evaluations] evaluator:', results[0]);
            console.log('[Evaluations] me:', results[1]);
            self.allEvaluations(results[0] || []);
            if (results[1] && results[1].role) {
                self.currentUserRole(results[1].role);
            }
        })
        .catch(function(error) {
            console.error('Load error:', error);
            toastr.error(T('Evaluation.LoadError', 'Veriler yüklenirken bir hata oluştu.'));
        })
        .finally(function() {
            self.isLoading(false);
        });

        // Assignments'ı ayrı yükle (arka planda)
        self.isAssignmentsLoading(true);
        fetch('/api/assignments/my-assignments', { credentials: 'include' })
            .then(function(r) { return r.json(); })
            .then(function(data) {
                console.log('[Evaluations] my-assignments:', data);
                self.allAssignments(data || []);
            })
            .catch(function(error) {
                console.error('Assignments load error:', error);
            })
            .finally(function() {
                self.isAssignmentsLoading(false);
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

    // CallId tekrar kontrolü - aynı müşteride aynı CallId varsa hata ver
    self.checkCallIdExists = function() {
        return new Promise(function(resolve) {
            var callId = self.callId();
            if (!callId || !callId.trim()) {
                resolve(false);
                return;
            }
            var assignmentId = self.formData() ? self.formData().assignmentId : null;
            var evaluationId = self.formData() ? self.formData().evaluationId : null;
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
                        // Warning varsa göster
                        if (result.warnings && result.warnings.length > 0) {
                            result.warnings.forEach(function(warning) {
                                toastr.warning(warning);
                            });
                        }
                        // Taslak kaydedilince modal kapansın ve liste yenilensin
                        self.closeEvaluateModal();
                        self.loadEvaluations();
                    }
                });
            })
            .catch(function(error) {
                console.error('Draft save error:', error);
                toastr.error(T('Evaluation.DraftSaveErrorMessage', 'Taslak kaydedilirken bir hata oluştu.'));
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
        });
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
                // Warning varsa göster
                if (newEvaluation.warnings && newEvaluation.warnings.length > 0) {
                    newEvaluation.warnings.forEach(function(warning) {
                        toastr.warning(warning);
                    });
                }
                self.closeEvaluateModal();
                self.loadEvaluations();
            });
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
    // DELETE DRAFT
    // ========================
    self.deleteDraft = function(evaluation) {
        if (evaluation.status !== 'Draft') {
            toastr.error(T('Evaluation.OnlyDraftCanBeDeleted', 'Sadece taslak durumundaki değerlendirmeler silinebilir.'));
            return;
        }

        showDeleteConfirm(T('Evaluation.DraftEvaluation', 'Taslak Değerlendirme'), function() {
            fetch('/api/evaluations/' + evaluation.id, {
                method: 'DELETE',
                credentials: 'include'
            })
            .then(function(response) {
                if (!response.ok) {
                    return response.json().then(function(data) {
                        throw new Error(data.message || T('Evaluation.DeleteError', 'Silme işlemi başarısız'));
                    });
                }
                return response.json();
            })
            .then(function() {
                // Listeden kaldır
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
    // EXCEL EXPORT
    // ========================
    self.isExportingEvaluations = ko.observable(false);

    self.exportEvaluationsToExcel = function() {
        self.isExportingEvaluations(true);

        // Aktif filtreleri URL parametrelerine dönüştür
        var filters = self.evalActiveFilters();
        var params = [];

        filters.forEach(function(f) {
            switch (f.type) {
                case 'status':
                    params.push('status=' + encodeURIComponent(f.value));
                    break;
                case 'search':
                    params.push('search=' + encodeURIComponent(f.value));
                    break;
                case 'personnel':
                    params.push('personnel=' + encodeURIComponent(f.value));
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

        var url = '/api/evaluations/export';
        if (params.length > 0) {
            url += '?' + params.join('&');
        }

        // Dosyayı indir
        window.location.href = url;

        // Export durumunu birkaç saniye sonra sıfırla
        setTimeout(function() {
            self.isExportingEvaluations(false);
        }, 2000);
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
