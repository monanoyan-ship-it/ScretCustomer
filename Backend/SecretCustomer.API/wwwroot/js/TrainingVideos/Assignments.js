// TrainingVideo Assignments ViewModel
var TRANSLATION_KEYS = [
    'Common.Loading',
    'Common.Error',
    'Common.Success',
    'Common.Confirm',
    'Common.All',
    'TrainingVideo.DeleteConfirm',
    'TrainingVideo.CreateSuccess',
    'TrainingVideo.DeleteSuccess',
    'TrainingVideo.RemindersSent'
];

function AssignmentsViewModel() {
    var self = this;

    // State
    self.isLoading = ko.observable(false);
    self.isSaving = ko.observable(false);
    self.isLoadingPreview = ko.observable(false);
    self.isLoadingParticipants = ko.observable(false);
    self.isLoadingPersonnel = ko.observable(false);
    self.assignments = ko.observableArray([]);
    self.videos = ko.observableArray([]);
    self.projects = ko.observableArray([]);
    self.relatedProjects = ko.observableArray([]);
    self.customers = ko.observableArray([]);
    self.organizations = ko.observableArray([]);
    self.customerPersonnel = ko.observableArray([]);
    self.participants = ko.observableArray([]);
    self.emailTemplates = ko.observableArray([]);

    // ===== ASSIGNMENT TAB FILTERS (KURALLAR.md Pattern) =====
    self.selectedFilterType = ko.observable('');
    self.tempFilter = {
        searchTerm: ko.observable(''),
        videoId: ko.observable(''),
        status: ko.observable(''),
        participantType: ko.observable(''),
        startDate: ko.observable(''),
        endDate: ko.observable(''),
        dateRangeType: ko.observable(''),
        dateType: ko.observable('start') // 'start' veya 'due'
    };
    self.activeFilters = ko.observableArray([]);

    // Can add filter?
    self.canAddFilter = ko.computed(function() {
        var type = self.selectedFilterType();
        if (!type) return false;
        if (type === 'searchTerm') return self.tempFilter.searchTerm();
        if (type === 'video') return self.tempFilter.videoId();
        if (type === 'status') return self.tempFilter.status() !== '';
        if (type === 'participantType') return self.tempFilter.participantType() !== '';
        if (type === 'dateRange') return self.tempFilter.startDate() || self.tempFilter.endDate() || self.tempFilter.dateRangeType();
        return false;
    });

    // Add filter
    self.addFilter = function() {
        var type = self.selectedFilterType();
        if (!type) return;

        var filter = { type: type };
        var label = '';
        var displayValue = '';

        if (type === 'searchTerm') {
            filter.value = self.tempFilter.searchTerm();
            label = 'Arama';
            displayValue = filter.value;
            self.tempFilter.searchTerm('');
        } else if (type === 'video') {
            filter.value = self.tempFilter.videoId();
            var video = ko.utils.arrayFirst(self.videos(), function(v) { return v.id == filter.value; });
            label = 'Video';
            displayValue = video ? video.title : filter.value;
            self.tempFilter.videoId('');
        } else if (type === 'status') {
            filter.value = self.tempFilter.status();
            label = 'Durum';
            displayValue = filter.value === 'true' ? 'Aktif' : 'Pasif';
            self.tempFilter.status('');
        } else if (type === 'participantType') {
            filter.value = self.tempFilter.participantType();
            label = 'Tip';
            displayValue = filter.value === 'internal' ? 'Ic' : 'Dis';
            self.tempFilter.participantType('');
        } else if (type === 'dateRange') {
            filter.dateRangeType = self.tempFilter.dateRangeType();
            filter.startDate = self.tempFilter.startDate();
            filter.endDate = self.tempFilter.endDate();
            filter.dateType = self.tempFilter.dateType();
            var dateTypeLabel = filter.dateType === 'due' ? 'Bitis' : 'Baslangic';
            label = dateTypeLabel + ' Tarihi';
            if (filter.dateRangeType && self.dateRangeLabels[filter.dateRangeType]) {
                displayValue = self.dateRangeLabels[filter.dateRangeType];
            } else {
                displayValue = (filter.startDate || '...') + ' - ' + (filter.endDate || '...');
            }
            self.tempFilter.startDate('');
            self.tempFilter.endDate('');
            self.tempFilter.dateRangeType('');
            // Tarih filtreleri için dateType'a göre ayrı tip kullan
            type = 'dateRange_' + filter.dateType; // dateRange_start veya dateRange_due
            filter.type = type;
        }

        // Remove existing filter of same type
        self.activeFilters.remove(function(f) { return f.type === type; });

        filter.label = label;
        filter.displayValue = displayValue;
        self.activeFilters.push(filter);
        self.selectedFilterType('');
        self.search(); // Filtre eklenince otomatik ara
    };

    // Remove filter
    self.removeFilter = function(filter) {
        self.activeFilters.remove(filter);
        self.search(); // Filtre kaldırılınca otomatik ara
    };

    // Clear all filters
    self.clearFilters = function() {
        self.activeFilters([]);
        self.search(); // Filtreler temizlenince otomatik ara
    };

    // Set date range for filter (hızlı seçim)
    self.setFilterDateRange = function(rangeType) {
        var range = self.calculateDateRange(rangeType);
        self.tempFilter.startDate(range.start);
        self.tempFilter.endDate(range.end);
        self.tempFilter.dateRangeType(rangeType);
    };

    // Search (filtreleri uygula)
    self.search = function() {
        self.loadAssignments();
    };

    // Searchable Video Dropdown
    self.videoSearchText = ko.observable('');
    self.isVideoDropdownVisible = ko.observable(false);
    self.videoDropdownTimer = null;

    // Create form - Step 1: Video and Settings
    self.formTitle = ko.observable('');
    self.formVideoId = ko.observable('');
    self.formStartDate = ko.observable('');
    self.formDueDate = ko.observable('');
    self.formMinWatchCount = ko.observable(1);
    self.formMaxWatchCount = ko.observable('');
    self.formAllowSpeedChange = ko.observable(false);
    self.formAllowSeeking = ko.observable(false);
    self.formEmailTemplateId = ko.observable('');
    self.formSendEmail = ko.observable(true);

    // Create form - Step 2: Filters
    self.formCustomerId = ko.observable('');
    self.formOrganizationId = ko.observable('');
    self.formProjectId = ko.observable('');
    self.formScoreThreshold = ko.observable(70);
    self.formSourceStartDate = ko.observable('');
    self.formSourceEndDate = ko.observable('');
    self.formDateRangeType = ko.observable('');
    self.formMinScore = ko.observable('');
    self.formMaxScore = ko.observable(70);
    self.previewResult = ko.observable(null);

    // Personel seçimi için
    self.personnelWithScores = ko.observableArray([]);
    self.selectAll = ko.observable(false);
    self.personnelSearchText = ko.observable('');

    // Video scope info
    self.selectedVideoScopes = ko.observableArray([]);

    // Modals
    self.createModal = null;
    self.participantsModal = null;
    self.emailModal = null;
    self.editModal = null;

    // ===== EDIT MODAL STATE =====
    self.editAssignmentId = ko.observable(null);
    self.editTitle = ko.observable('');
    self.editStartDate = ko.observable('');
    self.editDueDate = ko.observable('');
    self.editIsActive = ko.observable(true);
    self.editEmailTemplateId = ko.observable('');
    self.editMinWatchCount = ko.observable(1);
    self.editMaxWatchCount = ko.observable('');
    self.editAllowSpeedChange = ko.observable(false);
    self.editAllowSeeking = ko.observable(false);
    self.editVideoTitle = ko.observable('');
    self.editVideoId = ko.observable(null);
    self.editIsExternal = ko.observable(false);
    self.isLoadingEdit = ko.observable(false);
    self.isSavingEdit = ko.observable(false);

    // Edit modal - mevcut katılımcılar
    self.editParticipants = ko.observableArray([]);
    self.editExternalParticipants = ko.observableArray([]);

    // Edit modal - yeni katılımcı ekleme
    self.editNewParticipantSearch = ko.observable('');
    self.editNewParticipantId = ko.observable(null);
    self.editNewExternalEmail = ko.observable('');
    self.editNewExternalFirstName = ko.observable('');
    self.editNewExternalLastName = ko.observable('');

    // Edit modal - silinecek/eklenecek listeler
    self.editRemoveParticipantIds = ko.observableArray([]);
    self.editRemoveExternalIds = ko.observableArray([]);
    self.editAddParticipants = ko.observableArray([]);
    self.editAddExternals = ko.observableArray([]);

    // Personel arama için
    self.editPersonnelSearchResults = ko.observableArray([]);
    self.isEditPersonnelDropdownVisible = ko.observable(false);
    self.editPersonnelDropdownTimer = null;

    // Email modal state
    self.emailModalAssignmentId = ko.observable(null);
    self.emailModalAssignmentTitle = ko.observable('');
    self.emailModalParticipants = ko.observableArray([]);
    self.isLoadingEmailParticipants = ko.observable(false);
    self.isSendingEmails = ko.observable(false);
    self.emailModalTypeId = ko.observable('1');
    self.emailSelectAll = ko.observable(false);

    // Email filters
    self.emailFilterEmailSent = ko.observable('');
    self.emailFilterHasStarted = ko.observable('');
    self.emailFilterIsCompleted = ko.observable('');

    // ===== ALL PARTICIPANTS TAB (KURALLAR.md Pattern) =====
    self.allParticipants = ko.observableArray([]);
    self.isLoadingAllParticipants = ko.observable(false);
    self.participantSelectedFilterType = ko.observable('');
    self.participantTempFilter = {
        searchText: ko.observable(''),
        videoId: ko.observable(''),
        status: ko.observable('')
    };
    self.participantActiveFilters = ko.observableArray([]);

    // Status labels for participants
    self.participantStatusLabels = {
        '1': 'Bekliyor',
        '2': 'Izliyor',
        '3': 'Tamamladi'
    };

    // Can add participant filter?
    self.canAddParticipantFilter = ko.computed(function() {
        var type = self.participantSelectedFilterType();
        if (!type) return false;
        if (type === 'searchText') return self.participantTempFilter.searchText();
        if (type === 'video') return self.participantTempFilter.videoId();
        if (type === 'status') return self.participantTempFilter.status();
        return false;
    });

    // Add participant filter
    self.addParticipantFilter = function() {
        var type = self.participantSelectedFilterType();
        if (!type) return;

        var filter = { type: type };
        var label = '';
        var displayValue = '';

        if (type === 'searchText') {
            filter.value = self.participantTempFilter.searchText();
            label = 'Isim';
            displayValue = filter.value;
            self.participantTempFilter.searchText('');
        } else if (type === 'video') {
            filter.value = self.participantTempFilter.videoId();
            var video = ko.utils.arrayFirst(self.videos(), function(v) { return v.id == filter.value; });
            label = 'Video';
            displayValue = video ? video.title : filter.value;
            self.participantTempFilter.videoId('');
        } else if (type === 'status') {
            filter.value = self.participantTempFilter.status();
            label = 'Durum';
            displayValue = self.participantStatusLabels[filter.value] || filter.value;
            self.participantTempFilter.status('');
        }

        // Remove existing filter of same type
        self.participantActiveFilters.remove(function(f) { return f.type === type; });

        filter.label = label;
        filter.displayValue = displayValue;
        self.participantActiveFilters.push(filter);
        self.participantSelectedFilterType('');
    };

    // Remove participant filter
    self.removeParticipantFilter = function(filter) {
        self.participantActiveFilters.remove(filter);
    };

    // Clear all participant filters
    self.clearParticipantFilters = function() {
        self.participantActiveFilters([]);
    };

    // ===== DATE RANGE PATTERN =====
    self.dateRangeLabels = {
        'today': 'Bugun',
        'tomorrow': 'Yarin',
        'yesterday': 'Dun',
        'thisWeek': 'Bu Hafta',
        'nextWeek': 'Gelecek Hafta',
        'lastWeek': 'Gecen Hafta',
        'thisMonth': 'Bu Ay',
        'nextMonth': 'Gelecek Ay',
        'lastMonth': 'Gecen Ay',
        'next3Months': 'Gelecek 3 Ay',
        'last3Months': 'Son 3 Ay',
        'last6Months': 'Son 6 Ay',
        'thisYear': 'Bu Yil',
        'lastYear': 'Gecen Yil'
    };

    self.calculateDateRange = function(rangeType) {
        var today = new Date();
        var start, end;

        if (rangeType === 'today') {
            start = end = today.toISOString().split('T')[0];
        } else if (rangeType === 'tomorrow') {
            var tomorrow = new Date(today);
            tomorrow.setDate(tomorrow.getDate() + 1);
            start = end = tomorrow.toISOString().split('T')[0];
        } else if (rangeType === 'yesterday') {
            var yesterday = new Date(today);
            yesterday.setDate(yesterday.getDate() - 1);
            start = end = yesterday.toISOString().split('T')[0];
        } else if (rangeType === 'thisWeek') {
            var dayOfWeek = today.getDay();
            var diff = today.getDate() - dayOfWeek + (dayOfWeek === 0 ? -6 : 1);
            var weekStart = new Date(today);
            weekStart.setDate(diff);
            var weekEnd = new Date(weekStart);
            weekEnd.setDate(weekStart.getDate() + 6);
            start = weekStart.toISOString().split('T')[0];
            end = weekEnd.toISOString().split('T')[0];
        } else if (rangeType === 'nextWeek') {
            var dayOfWeek = today.getDay();
            var diff = today.getDate() - dayOfWeek + (dayOfWeek === 0 ? -6 : 1);
            var nextWeekStart = new Date(today);
            nextWeekStart.setDate(diff + 7);
            var nextWeekEnd = new Date(nextWeekStart);
            nextWeekEnd.setDate(nextWeekStart.getDate() + 6);
            start = nextWeekStart.toISOString().split('T')[0];
            end = nextWeekEnd.toISOString().split('T')[0];
        } else if (rangeType === 'lastWeek') {
            var dayOfWeek = today.getDay();
            var diff = today.getDate() - dayOfWeek + (dayOfWeek === 0 ? -6 : 1);
            var lastWeekEnd = new Date(today);
            lastWeekEnd.setDate(diff - 1);
            var lastWeekStart = new Date(lastWeekEnd);
            lastWeekStart.setDate(lastWeekEnd.getDate() - 6);
            start = lastWeekStart.toISOString().split('T')[0];
            end = lastWeekEnd.toISOString().split('T')[0];
        } else if (rangeType === 'thisMonth') {
            start = new Date(today.getFullYear(), today.getMonth(), 1).toISOString().split('T')[0];
            end = new Date(today.getFullYear(), today.getMonth() + 1, 0).toISOString().split('T')[0];
        } else if (rangeType === 'nextMonth') {
            start = new Date(today.getFullYear(), today.getMonth() + 1, 1).toISOString().split('T')[0];
            end = new Date(today.getFullYear(), today.getMonth() + 2, 0).toISOString().split('T')[0];
        } else if (rangeType === 'lastMonth') {
            start = new Date(today.getFullYear(), today.getMonth() - 1, 1).toISOString().split('T')[0];
            end = new Date(today.getFullYear(), today.getMonth(), 0).toISOString().split('T')[0];
        } else if (rangeType === 'next3Months') {
            start = new Date(today.getFullYear(), today.getMonth(), 1).toISOString().split('T')[0];
            end = new Date(today.getFullYear(), today.getMonth() + 3, 0).toISOString().split('T')[0];
        } else if (rangeType === 'last3Months') {
            start = new Date(today.getFullYear(), today.getMonth() - 2, 1).toISOString().split('T')[0];
            end = today.toISOString().split('T')[0];
        } else if (rangeType === 'last6Months') {
            start = new Date(today.getFullYear(), today.getMonth() - 5, 1).toISOString().split('T')[0];
            end = today.toISOString().split('T')[0];
        } else if (rangeType === 'thisYear') {
            start = new Date(today.getFullYear(), 0, 1).toISOString().split('T')[0];
            end = new Date(today.getFullYear(), 11, 31).toISOString().split('T')[0];
        } else if (rangeType === 'lastYear') {
            start = new Date(today.getFullYear() - 1, 0, 1).toISOString().split('T')[0];
            end = new Date(today.getFullYear() - 1, 11, 31).toISOString().split('T')[0];
        }

        return { start: start, end: end };
    };

    self.setDateRange = function(rangeType) {
        var range = self.calculateDateRange(rangeType);
        self.formSourceStartDate(range.start);
        self.formSourceEndDate(range.end);
        self.formDateRangeType(rangeType);
    };

    // Format date
    self.formatDate = function(dateStr) {
        if (!dateStr) return '-';
        var d = new Date(dateStr);
        return d.toLocaleDateString('tr-TR');
    };

    // Format duration
    self.formatDuration = function(seconds) {
        if (!seconds) return '0:00';
        var mins = Math.floor(seconds / 60);
        var secs = seconds % 60;
        return mins + ':' + (secs < 10 ? '0' : '') + secs;
    };

    // ===== SEARCHABLE VIDEO DROPDOWN =====

    self.filteredVideos = ko.computed(function() {
        var searchText = (self.videoSearchText() || '').toLowerCase().trim();
        if (!searchText) {
            return self.videos();
        }
        return ko.utils.arrayFilter(self.videos(), function(v) {
            return v.title.toLowerCase().indexOf(searchText) >= 0 ||
                   (v.description && v.description.toLowerCase().indexOf(searchText) >= 0);
        });
    });

    self.showVideoDropdown = function() {
        if (self.videoDropdownTimer) {
            clearTimeout(self.videoDropdownTimer);
        }
        self.isVideoDropdownVisible(true);
    };

    self.hideVideoDropdownDelayed = function() {
        self.videoDropdownTimer = setTimeout(function() {
            self.isVideoDropdownVisible(false);
        }, 200);
    };

    self.selectVideo = function(video) {
        self.formVideoId(video.id.toString());
        self.videoSearchText(video.title);
        self.isVideoDropdownVisible(false);
    };

    self.clearVideoSelection = function() {
        self.formVideoId('');
        self.videoSearchText('');
        self.selectedVideoScopes([]);
        self.relatedProjects([]);
    };

    // ===== COMPUTED OBSERVABLES =====

    // Can go to step 2 (video selected)
    self.canGoToStep2 = ko.computed(function() {
        return !!self.formVideoId();
    });

    // Filtered organizations based on selected customer
    self.filteredOrganizations = ko.computed(function() {
        var customerId = self.formCustomerId();
        if (!customerId) {
            return self.organizations();
        }
        return ko.utils.arrayFilter(self.organizations(), function(org) {
            return org.customerId === parseInt(customerId);
        });
    });

    // Scope filtered customers - müşteriler video kapsamındaki checklistlerde değerlendirmesi olan müşteriler
    self.scopeFilteredCustomers = ko.computed(function() {
        var scopes = self.selectedVideoScopes();
        var customers = self.customers();

        // Eğer scope yoksa tüm müşterileri göster
        if (!scopes || scopes.length === 0) {
            return customers;
        }

        // Şimdilik tüm müşterileri göster - API'den filtrelenmiş müşteri listesi alınacak
        return customers;
    });

    // Filtered projects based on customer/org and video scope
    self.filteredProjects = ko.computed(function() {
        var projects = self.relatedProjects();
        var customerId = self.formCustomerId();
        var organizationId = self.formOrganizationId();

        if (!customerId && !organizationId) {
            return projects;
        }

        return ko.utils.arrayFilter(projects, function(p) {
            if (customerId && p.customerId !== parseInt(customerId)) {
                return false;
            }
            return true;
        });
    });

    // Selected video title
    self.selectedVideoTitle = ko.computed(function() {
        var videoId = self.formVideoId();
        if (!videoId) return '';
        var video = ko.utils.arrayFirst(self.videos(), function(v) {
            return v.id === parseInt(videoId);
        });
        return video ? video.title : '';
    });

    // Selected email template name
    self.selectedEmailTemplateName = ko.computed(function() {
        var templateId = self.formEmailTemplateId();
        if (!templateId) return '';
        var template = ko.utils.arrayFirst(self.emailTemplates(), function(t) {
            return t.id === parseInt(templateId);
        });
        return template ? template.name : '';
    });

    // Can show personnel (date range required)
    self.canShowPersonnel = ko.computed(function() {
        return self.formSourceStartDate() && self.formSourceEndDate();
    });

    // Scope column headers (dynamic based on video scope)
    self.scopeColumnHeaders = ko.computed(function() {
        var scopes = self.selectedVideoScopes();
        if (!scopes || scopes.length === 0) {
            return [];
        }
        return scopes.map(function(s) {
            if (s.scopeTypeId === 1) {
                return s.checklistName || 'Checklist';
            } else if (s.scopeTypeId === 2) {
                return s.questionGroupName || 'Grup';
            } else {
                return (s.questionText || 'Soru').substring(0, 20) + '...';
            }
        });
    });

    // Visible personnel (filtered by score and search)
    self.visiblePersonnel = ko.computed(function() {
        var searchText = (self.personnelSearchText() || '').toLowerCase().trim();
        return ko.utils.arrayFilter(self.personnelWithScores(), function(p) {
            if (!p.visible()) return false;
            if (searchText && p.userName.toLowerCase().indexOf(searchText) < 0) {
                return false;
            }
            return true;
        });
    });

    // Selected personnel count
    self.selectedCount = ko.computed(function() {
        return ko.utils.arrayFilter(self.personnelWithScores(), function(p) {
            return p.selected();
        }).length;
    });

    // Selected personnel preview (first 5)
    self.selectedPersonnelPreview = ko.computed(function() {
        var selected = ko.utils.arrayFilter(self.personnelWithScores(), function(p) {
            return p.selected();
        });
        return selected.slice(0, 5);
    });

    // Filtered all participants (for participants tab - uses activeFilters)
    self.filteredAllParticipants = ko.computed(function() {
        var filters = self.participantActiveFilters();

        // Get filter values from activeFilters
        var searchText = '';
        var videoId = null;
        var status = null;

        filters.forEach(function(f) {
            if (f.type === 'searchText') searchText = (f.value || '').toLowerCase().trim();
            if (f.type === 'video') videoId = parseInt(f.value);
            if (f.type === 'status') status = parseInt(f.value);
        });

        return ko.utils.arrayFilter(self.allParticipants(), function(p) {
            // Name search
            if (searchText && p.userName.toLowerCase().indexOf(searchText) < 0) {
                return false;
            }
            // Video filter
            if (videoId && p.videoId !== videoId) {
                return false;
            }
            // Status filter
            if (status && p.statusId !== status) {
                return false;
            }
            return true;
        });
    });

    // ===== SUBSCRIPTIONS =====

    // Video değiştiğinde scope bilgisini ve ilişkili projeleri yükle
    self.formVideoId.subscribe(function(videoId) {
        if (videoId) {
            self.loadVideoDetails(videoId);
            self.loadRelatedProjects(videoId);
            self.loadScopeFilteredCustomers(videoId);
        } else {
            self.selectedVideoScopes([]);
            self.relatedProjects([]);
            self.formProjectId('');
        }
    });

    // Customer değiştiğinde organization'ı sıfırla
    self.formCustomerId.subscribe(function() {
        self.formOrganizationId('');
    });

    // Min/Max score filter
    self.formMinScore.subscribe(function() {
        self.filterPersonnelByScore();
    });
    self.formMaxScore.subscribe(function() {
        self.filterPersonnelByScore();
    });

    // Select all checkbox
    self.selectAll.subscribe(function(checked) {
        ko.utils.arrayForEach(self.visiblePersonnel(), function(p) {
            p.selected(checked);
        });
    });

    // ===== TAB NAVIGATION =====

    self.goToStep1 = function() {
        var tab = new bootstrap.Tab(document.getElementById('step1-tab'));
        tab.show();
    };

    self.goToStep2 = function() {
        if (!self.formVideoId()) return;
        var tab = new bootstrap.Tab(document.getElementById('step2-tab'));
        tab.show();
    };

    self.goToStep3 = function() {
        if (!self.canShowPersonnel()) return;
        self.loadPersonnelWithScores();
        var tab = new bootstrap.Tab(document.getElementById('step3-tab'));
        tab.show();
    };

    self.goToStep4 = function() {
        if (self.selectedCount() === 0) return;
        var tab = new bootstrap.Tab(document.getElementById('step4-tab'));
        tab.show();
    };

    // ===== DATA LOADING =====

    // Build filter params from activeFilters (KURALLAR.md Section 20 Pattern)
    self.buildFilterParams = function() {
        var params = new URLSearchParams();
        self.activeFilters().forEach(function(filter) {
            switch (filter.type) {
                case 'video':
                    params.append('videoIds', filter.value);
                    break;
                case 'status':
                    params.append('isActive', filter.value);
                    break;
                case 'searchTerm':
                    params.append('searchTerm', filter.value);
                    break;
                case 'dateRange_start':
                case 'dateRange_due':
                    // DateRanges pattern - KURALLAR.md Section 20
                    // Her tarih tipi için ayrı dateRanges index'i
                    var dateIndex = filter.type === 'dateRange_start' ? 0 : 1;
                    if (filter.startDate) {
                        params.append('dateRanges[' + dateIndex + '].startDate', filter.startDate);
                    }
                    if (filter.endDate) {
                        params.append('dateRanges[' + dateIndex + '].endDate', filter.endDate);
                    }
                    if (filter.dateType) {
                        params.append('dateRanges[' + dateIndex + '].filterType', filter.dateType);
                    }
                    break;
            }
        });
        return params;
    };

    // Load assignments
    self.loadAssignments = function() {
        self.isLoading(true);

        var params = self.buildFilterParams();

        fetch('/api/training-video-assignments?' + params.toString(), { credentials: 'include' })
            .then(function(r) { return r.json(); })
            .then(function(data) {
                // Client-side participantType filter
                var participantTypeFilter = ko.utils.arrayFirst(self.activeFilters(), function(f) { return f.type === 'participantType'; });
                if (participantTypeFilter) {
                    data = data.filter(function(a) {
                        if (participantTypeFilter.value === 'internal') {
                            return !a.isExternal;
                        } else if (participantTypeFilter.value === 'external') {
                            return a.isExternal;
                        }
                        return true;
                    });
                }
                self.assignments(data);
            })
            .catch(function(err) {
                toastr.error(T('Common.Error', 'Hata olustu'));
                console.error(err);
            })
            .finally(function() {
                self.isLoading(false);
            });
    };

    // Load videos
    self.loadVideos = function() {
        fetch('/api/training-videos', { credentials: 'include' })
            .then(function(r) { return r.json(); })
            .then(function(data) {
                self.videos(data);
            });
    };

    // Load video details (for scope info)
    self.loadVideoDetails = function(videoId) {
        fetch('/api/training-videos/' + videoId, { credentials: 'include' })
            .then(function(r) { return r.json(); })
            .then(function(data) {
                self.selectedVideoScopes(data.scopes || []);
            })
            .catch(function(err) {
                console.error('Error loading video details:', err);
                self.selectedVideoScopes([]);
            });
    };

    // Load projects
    self.loadProjects = function() {
        fetch('/api/projects', { credentials: 'include' })
            .then(function(r) { return r.json(); })
            .then(function(data) {
                self.projects(data);
            });
    };

    // Load customers
    self.loadCustomers = function() {
        fetch('/api/customers/active', { credentials: 'include' })
            .then(function(r) { return r.json(); })
            .then(function(data) {
                self.customers(data);
            })
            .catch(function(err) {
                console.error('Error loading customers:', err);
            });
    };

    // Load scope-filtered customers
    self.loadScopeFilteredCustomers = function(videoId) {
        fetch('/api/training-videos/' + videoId + '/scope-customers', { credentials: 'include' })
            .then(function(r) { return r.json(); })
            .then(function(data) {
                // Scope'a göre filtrelenmiş müşteriler
                self.customers(data);
            })
            .catch(function(err) {
                console.error('Error loading scope customers:', err);
                // Fallback: tüm müşterileri yükle
                self.loadCustomers();
            });
    };

    // Load organizations
    self.loadOrganizations = function() {
        fetch('/api/customer-organizations', { credentials: 'include' })
            .then(function(r) { return r.json(); })
            .then(function(data) {
                // Handle both paginated and non-paginated response
                var orgs = data.items || data;
                self.organizations(orgs);
            })
            .catch(function(err) {
                console.error('Error loading organizations:', err);
            });
    };

    // Load related projects for selected video
    self.loadRelatedProjects = function(videoId) {
        fetch('/api/training-videos/' + videoId + '/related-projects', { credentials: 'include' })
            .then(function(r) { return r.json(); })
            .then(function(data) {
                self.relatedProjects(data);
            })
            .catch(function(err) {
                console.error('Error loading related projects:', err);
                self.relatedProjects([]);
            });
    };

    // Load email templates
    self.loadEmailTemplates = function() {
        fetch('/api/training-video-assignments/email-templates', { credentials: 'include' })
            .then(function(r) { return r.json(); })
            .then(function(data) {
                self.emailTemplates(data);
                // Varsayılan şablonu seç
                var defaultTemplate = data.find(function(t) { return t.isDefault; });
                if (defaultTemplate) {
                    self.formEmailTemplateId(defaultTemplate.id.toString());
                }
            })
            .catch(function(err) {
                console.error('Error loading email templates:', err);
            });
    };

    // Load customer personnel (for manual assignment)
    self.loadCustomerPersonnel = function() {
        fetch('/api/customer-personnel', { credentials: 'include' })
            .then(function(r) { return r.json(); })
            .then(function(data) {
                self.customerPersonnel(data);
            });
    };

    // Load all participants (for participants tab)
    self.loadAllParticipants = function() {
        if (self.allParticipants().length > 0) return; // Already loaded

        self.isLoadingAllParticipants(true);

        fetch('/api/training-video-assignments/all-participants', { credentials: 'include' })
            .then(function(r) { return r.json(); })
            .then(function(data) {
                self.allParticipants(data);
            })
            .catch(function(err) {
                toastr.error(T('Common.Error', 'Katilimcilar yuklenemedi'));
                console.error(err);
            })
            .finally(function() {
                self.isLoadingAllParticipants(false);
            });
    };

    // Load personnel with scores based on filters
    self.loadPersonnelWithScores = function() {
        if (!self.formVideoId() || !self.formSourceStartDate() || !self.formSourceEndDate()) {
            self.personnelWithScores([]);
            return;
        }

        self.isLoadingPersonnel(true);

        var dto = {
            trainingVideoId: parseInt(self.formVideoId()),
            scoreThreshold: 100, // Tüm personeli getir
            sourceStartDate: self.formSourceStartDate(),
            sourceEndDate: self.formSourceEndDate()
        };

        // Add optional filters
        if (self.formCustomerId()) {
            dto.customerId = parseInt(self.formCustomerId());
        }
        if (self.formOrganizationId()) {
            dto.organizationId = parseInt(self.formOrganizationId());
        }
        if (self.formProjectId()) {
            dto.projectId = parseInt(self.formProjectId());
        }

        fetch('/api/training-video-assignments/preview', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(dto),
            credentials: 'include'
        })
        .then(function(r) { return r.json(); })
        .then(function(data) {
            var scopes = self.selectedVideoScopes();
            var personnel = (data.users || []).map(function(u) {
                // Scope scores (birden fazla scope varsa her biri için ayrı puan)
                var scopeScores = [];
                if (u.scopeScores && u.scopeScores.length > 0) {
                    scopeScores = u.scopeScores.map(function(ss) {
                        return { score: ss };
                    });
                } else if (scopes.length > 0) {
                    // Fallback: tek puan varsa tüm scope'lara aynı puanı ver
                    for (var i = 0; i < scopes.length; i++) {
                        scopeScores.push({ score: u.scopeScore || 0 });
                    }
                }

                // Ortalama puan hesapla
                var avgScore = u.scopeScore || 0;
                if (scopeScores.length > 0) {
                    var sum = 0;
                    var count = 0;
                    scopeScores.forEach(function(ss) {
                        if (ss.score !== null) {
                            sum += ss.score;
                            count++;
                        }
                    });
                    avgScore = count > 0 ? sum / count : 0;
                }

                return {
                    userId: u.userId,
                    userName: u.userName,
                    email: u.email,
                    customerName: u.customerName || '',
                    scopeScore: u.scopeScore || 0,
                    scopeScores: scopeScores,
                    avgScore: avgScore,
                    selected: ko.observable(false),
                    visible: ko.observable(true)
                };
            });
            // Puana göre sırala (düşükten yükseğe)
            personnel.sort(function(a, b) { return a.avgScore - b.avgScore; });
            self.personnelWithScores(personnel);
            self.filterPersonnelByScore();
        })
        .catch(function(err) {
            toastr.error(T('Common.Error', 'Personel listesi yuklenemedi'));
            console.error(err);
        })
        .finally(function() {
            self.isLoadingPersonnel(false);
        });
    };

    // Filter personnel by score range
    self.filterPersonnelByScore = function() {
        var minScore = parseFloat(self.formMinScore()) || 0;
        var maxScore = parseFloat(self.formMaxScore()) || 100;

        ko.utils.arrayForEach(self.personnelWithScores(), function(p) {
            var visible = p.avgScore >= minScore && p.avgScore <= maxScore;
            p.visible(visible);
            if (!visible) {
                p.selected(false);
            }
        });
    };

    // Clear personnel selection
    self.clearSelection = function() {
        ko.utils.arrayForEach(self.personnelWithScores(), function(p) {
            p.selected(false);
        });
        self.selectAll(false);
    };

    // Open create modal
    self.openCreateModal = function() {
        self.formTitle('');
        self.formVideoId('');
        self.videoSearchText('');
        self.formStartDate('');
        self.formDueDate('');
        self.formMinWatchCount(1);
        self.formMaxWatchCount('');
        self.formAllowSpeedChange(false);
        self.formAllowSeeking(false);
        self.formCustomerId('');
        self.formOrganizationId('');
        self.formProjectId('');
        self.formScoreThreshold(70);
        self.formSourceStartDate('');
        self.formSourceEndDate('');
        self.formDateRangeType('');
        self.formMinScore('');
        self.formMaxScore(70);
        self.formEmailTemplateId('');
        self.formSendEmail(true);
        self.previewResult(null);
        self.personnelWithScores([]);
        self.personnelSearchText('');
        self.selectAll(false);
        self.relatedProjects([]);
        self.selectedVideoScopes([]);

        // Müşterileri yeniden yükle
        self.loadCustomers();

        // Reset to step 1
        var step1Tab = document.getElementById('step1-tab');
        if (step1Tab) {
            var tab = new bootstrap.Tab(step1Tab);
            tab.show();
        }

        if (!self.createModal) {
            self.createModal = new bootstrap.Modal(document.getElementById('createModal'));
        }
        self.createModal.show();
    };

    // Select personnel below threshold
    self.selectBelowThreshold = function() {
        var threshold = parseFloat(self.formMaxScore()) || 70;
        ko.utils.arrayForEach(self.personnelWithScores(), function(p) {
            if (p.visible() && p.avgScore < threshold) {
                p.selected(true);
            }
        });
    };

    // Create assignment
    self.createAssignment = function() {
        if (!self.formTitle() || !self.formVideoId() || !self.formStartDate() || !self.formDueDate()) {
            toastr.warning('Lutfen zorunlu alanlari doldurun');
            return;
        }

        // Seçili personelleri al
        var selectedUserIds = [];
        ko.utils.arrayForEach(self.personnelWithScores(), function(p) {
            if (p.selected()) {
                selectedUserIds.push(p.userId);
            }
        });

        if (selectedUserIds.length === 0) {
            toastr.warning('Lutfen en az bir personel secin');
            return;
        }

        self.isSaving(true);

        var dto = {
            title: self.formTitle(),
            trainingVideoId: parseInt(self.formVideoId()),
            startDate: self.formStartDate(),
            dueDate: self.formDueDate(),
            manualUserIds: selectedUserIds,
            sendEmail: self.formSendEmail(),
            minWatchCount: parseInt(self.formMinWatchCount()) || 1,
            maxWatchCount: self.formMaxWatchCount() ? parseInt(self.formMaxWatchCount()) : null,
            allowSpeedChange: self.formAllowSpeedChange(),
            allowSeeking: self.formAllowSeeking()
        };

        if (self.formEmailTemplateId()) {
            dto.emailTemplateId = parseInt(self.formEmailTemplateId());
        }

        // Kaynak bilgilerini de kaydet (referans için)
        if (self.formCustomerId()) {
            dto.customerId = parseInt(self.formCustomerId());
        }
        if (self.formOrganizationId()) {
            dto.organizationId = parseInt(self.formOrganizationId());
        }
        if (self.formProjectId()) {
            dto.projectId = parseInt(self.formProjectId());
            dto.scoreThreshold = parseFloat(self.formMaxScore());
            dto.sourceStartDate = self.formSourceStartDate();
            dto.sourceEndDate = self.formSourceEndDate();
        }

        fetch('/api/training-video-assignments', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(dto),
            credentials: 'include'
        })
        .then(function(r) { return r.json(); })
        .then(function(result) {
            if (result.id) {
                toastr.success(T('TrainingVideo.CreateSuccess', 'Atama basariyla olusturuldu'));
                self.createModal.hide();
                self.loadAssignments();
            } else {
                toastr.error(result.message || T('Common.Error', 'Hata olustu'));
            }
        })
        .catch(function(err) {
            toastr.error(T('Common.Error', 'Hata olustu'));
            console.error(err);
        })
        .finally(function() {
            self.isSaving(false);
        });
    };

    // Delete assignment
    self.deleteAssignment = function(assignment) {
        showConfirmModal({
            title: T('Common.Delete', 'Sil'),
            message: T('TrainingVideo.DeleteConfirm', 'Bu atamayı silmek istediğinize emin misiniz?'),
            confirmText: T('Common.Delete', 'Sil'),
            confirmClass: 'btn-danger',
            onConfirm: function() {
                fetch('/api/training-video-assignments/' + assignment.id, {
                    method: 'DELETE',
                    credentials: 'include'
                })
                .then(function(r) { return r.json(); })
                .then(function(result) {
                    if (result.message) {
                        toastr.success(T('TrainingVideo.DeleteSuccess', 'Atama silindi'));
                        self.loadAssignments();
                    } else {
                        toastr.error(result.message || T('Common.Error', 'Hata oluştu'));
                    }
                })
                .catch(function(err) {
                    toastr.error(T('Common.Error', 'Hata oluştu'));
                    console.error(err);
                });
            }
        });
    };

    // ===== EDIT MODAL FUNCTIONS =====

    // Edit modal aç
    self.openEditModal = function(assignment) {
        self.editAssignmentId(assignment.id);
        self.editTitle(assignment.title);
        self.editStartDate(assignment.startDate ? assignment.startDate.split('T')[0] : '');
        self.editDueDate(assignment.dueDate ? assignment.dueDate.split('T')[0] : '');
        self.editIsActive(assignment.isActive);
        self.editVideoTitle(assignment.trainingVideoTitle);
        self.editVideoId(assignment.trainingVideoId);
        self.editIsExternal(assignment.isExternal);
        self.editEmailTemplateId('');
        self.editMinWatchCount(1);
        self.editMaxWatchCount('');
        self.editAllowSpeedChange(false);
        self.editAllowSeeking(false);

        // Reset lists
        self.editParticipants([]);
        self.editExternalParticipants([]);
        self.editRemoveParticipantIds([]);
        self.editRemoveExternalIds([]);
        self.editAddParticipants([]);
        self.editAddExternals([]);
        self.editNewParticipantSearch('');
        self.editNewParticipantId(null);
        self.editNewExternalEmail('');
        self.editNewExternalFirstName('');
        self.editNewExternalLastName('');

        if (!self.editModal) {
            self.editModal = new bootstrap.Modal(document.getElementById('editModal'));
        }
        self.editModal.show();

        // Detay bilgileri yükle
        self.loadAssignmentForEdit(assignment.id);
    };

    // Assignment detaylarını yükle
    self.loadAssignmentForEdit = function(assignmentId) {
        self.isLoadingEdit(true);

        Promise.all([
            fetch('/api/training-video-assignments/' + assignmentId, { credentials: 'include' }).then(function(r) { return r.json(); }),
            fetch('/api/training-video-assignments/' + assignmentId + '/participants', { credentials: 'include' }).then(function(r) { return r.json(); }),
            fetch('/api/training-video-assignments/' + assignmentId + '/external-participants', { credentials: 'include' }).then(function(r) { return r.json(); })
        ])
        .then(function(results) {
            var assignment = results[0];
            var participants = Array.isArray(results[1]) ? results[1] : [];
            var externals = Array.isArray(results[2]) ? results[2] : [];

            // Detay alanlarını doldur
            if (assignment.emailTemplateId) {
                self.editEmailTemplateId(assignment.emailTemplateId.toString());
            }
            if (assignment.minWatchCount !== undefined) {
                self.editMinWatchCount(assignment.minWatchCount);
            }
            if (assignment.maxWatchCount) {
                self.editMaxWatchCount(assignment.maxWatchCount);
            }
            self.editAllowSpeedChange(assignment.allowSpeedChange || false);
            self.editAllowSeeking(assignment.allowSeeking || false);

            // Katılımcıları doldur
            self.editParticipants(participants.map(function(p) {
                return {
                    id: p.id,
                    userId: p.userId,
                    userName: p.userName,
                    email: p.email,
                    statusId: p.statusId,
                    isCompleted: p.isCompleted,
                    markedForRemoval: ko.observable(false)
                };
            }));

            self.editExternalParticipants(externals.map(function(p) {
                return {
                    id: p.id,
                    email: p.email,
                    firstName: p.firstName,
                    lastName: p.lastName,
                    fullName: p.fullName || p.email,
                    statusId: p.statusId,
                    isCompleted: p.isCompleted,
                    markedForRemoval: ko.observable(false)
                };
            }));
        })
        .catch(function(err) {
            toastr.error('Atama detayları yüklenemedi');
            console.error(err);
        })
        .finally(function() {
            self.isLoadingEdit(false);
        });
    };

    // Katılımcıyı silme için işaretle
    self.toggleRemoveParticipant = function(participant) {
        participant.markedForRemoval(!participant.markedForRemoval());
    };

    // Dış katılımcıyı silme için işaretle
    self.toggleRemoveExternal = function(external) {
        external.markedForRemoval(!external.markedForRemoval());
    };

    // Yeni iç katılımcı ara
    self.searchEditPersonnel = function() {
        var searchText = self.editNewParticipantSearch();
        var videoId = self.editVideoId();

        if (!searchText || searchText.length < 2) {
            self.editPersonnelSearchResults([]);
            return;
        }

        if (!videoId) {
            // VideoId yoksa eski yöntemi kullan
            fetch('/api/customer-personnel?search=' + encodeURIComponent(searchText) + '&pageSize=20', { credentials: 'include' })
                .then(function(r) { return r.json(); })
                .then(function(data) {
                    var items = data.items || data || [];
                    var existingIds = self.editParticipants().map(function(p) { return p.userId; });
                    var addedIds = self.editAddParticipants().map(function(p) { return p.userId; });
                    items = items.filter(function(p) {
                        return existingIds.indexOf(p.id) === -1 && addedIds.indexOf(p.id) === -1;
                    });
                    self.editPersonnelSearchResults(items.map(function(p) {
                        return { id: p.id, fullName: p.fullName || (p.firstName + ' ' + p.lastName), email: p.email, customerName: p.customerName };
                    }));
                });
            return;
        }

        // Video scope'una göre personel ara
        fetch('/api/training-videos/' + videoId + '/scope-personnel?search=' + encodeURIComponent(searchText) + '&maxResults=20', { credentials: 'include' })
            .then(function(r) { return r.json(); })
            .then(function(data) {
                var items = data || [];
                // Zaten ekli olanları filtrele
                var existingIds = self.editParticipants().map(function(p) { return p.userId; });
                var addedIds = self.editAddParticipants().map(function(p) { return p.userId; });
                items = items.filter(function(p) {
                    return existingIds.indexOf(p.id) === -1 && addedIds.indexOf(p.id) === -1;
                });
                self.editPersonnelSearchResults(items);
            });
    };

    self.showEditPersonnelDropdown = function() {
        if (self.editPersonnelDropdownTimer) clearTimeout(self.editPersonnelDropdownTimer);
        self.isEditPersonnelDropdownVisible(true);
    };

    self.hideEditPersonnelDropdownDelayed = function() {
        self.editPersonnelDropdownTimer = setTimeout(function() {
            self.isEditPersonnelDropdownVisible(false);
        }, 200);
    };

    // Yeni iç katılımcı seç
    self.selectEditPersonnel = function(personnel) {
        self.editAddParticipants.push({
            userId: personnel.id,
            userName: personnel.fullName || (personnel.firstName + ' ' + personnel.lastName),
            email: personnel.email
        });
        self.editNewParticipantSearch('');
        self.editPersonnelSearchResults([]);
        self.isEditPersonnelDropdownVisible(false);
    };

    // Eklenmek üzere olan iç katılımcıyı kaldır
    self.removeAddedParticipant = function(participant) {
        self.editAddParticipants.remove(participant);
    };

    // Yeni dış katılımcı ekle
    self.addEditExternal = function() {
        var email = self.editNewExternalEmail();
        if (!email || !email.trim()) {
            toastr.warning('Email adresi gerekli');
            return;
        }

        // Email formatı kontrol
        var emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
        if (!emailRegex.test(email)) {
            toastr.warning('Geçerli bir email adresi girin');
            return;
        }

        // Zaten var mı kontrol
        var existingEmails = self.editExternalParticipants().map(function(p) { return p.email.toLowerCase(); });
        var addedEmails = self.editAddExternals().map(function(p) { return p.email.toLowerCase(); });
        if (existingEmails.indexOf(email.toLowerCase()) >= 0 || addedEmails.indexOf(email.toLowerCase()) >= 0) {
            toastr.warning('Bu email zaten listede');
            return;
        }

        self.editAddExternals.push({
            email: email.trim(),
            firstName: self.editNewExternalFirstName().trim() || null,
            lastName: self.editNewExternalLastName().trim() || null
        });

        self.editNewExternalEmail('');
        self.editNewExternalFirstName('');
        self.editNewExternalLastName('');
    };

    // Eklenmek üzere olan dış katılımcıyı kaldır
    self.removeAddedExternal = function(external) {
        self.editAddExternals.remove(external);
    };

    // Güncellemeyi kaydet
    self.saveEdit = function() {
        if (!self.editTitle()) {
            toastr.warning('Başlık gerekli');
            return;
        }
        if (!self.editStartDate() || !self.editDueDate()) {
            toastr.warning('Tarihler gerekli');
            return;
        }

        self.isSavingEdit(true);

        // Silinecek katılımcıları topla
        var removeParticipantIds = [];
        self.editParticipants().forEach(function(p) {
            if (p.markedForRemoval()) {
                removeParticipantIds.push(p.id);
            }
        });

        var removeExternalIds = [];
        self.editExternalParticipants().forEach(function(p) {
            if (p.markedForRemoval()) {
                removeExternalIds.push(p.id);
            }
        });

        var dto = {
            title: self.editTitle(),
            startDate: self.editStartDate(),
            dueDate: self.editDueDate(),
            isActive: self.editIsActive(),
            emailTemplateId: self.editEmailTemplateId() ? parseInt(self.editEmailTemplateId()) : null,
            minWatchCount: parseInt(self.editMinWatchCount()) || 1,
            maxWatchCount: self.editMaxWatchCount() ? parseInt(self.editMaxWatchCount()) : null,
            allowSpeedChange: self.editAllowSpeedChange(),
            allowSeeking: self.editAllowSeeking(),
            addParticipantIds: self.editAddParticipants().map(function(p) { return p.userId; }),
            removeParticipantIds: removeParticipantIds,
            addExternalParticipants: self.editAddExternals().map(function(p) {
                return { email: p.email, firstName: p.firstName, lastName: p.lastName };
            }),
            removeExternalParticipantIds: removeExternalIds
        };

        fetch('/api/training-video-assignments/' + self.editAssignmentId(), {
            method: 'PUT',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(dto),
            credentials: 'include'
        })
        .then(function(r) {
            if (!r.ok) throw new Error('Update failed');
            return r.json();
        })
        .then(function(result) {
            toastr.success('Atama güncellendi');
            self.editModal.hide();
            self.loadAssignments();
        })
        .catch(function(err) {
            toastr.error('Güncelleme sırasında hata oluştu');
            console.error(err);
        })
        .finally(function() {
            self.isSavingEdit(false);
        });
    };

    // View participants
    self.viewParticipants = function(assignment) {
        self.participants([]);
        self.isLoadingParticipants(true);

        if (!self.participantsModal) {
            self.participantsModal = new bootstrap.Modal(document.getElementById('participantsModal'));
        }
        self.participantsModal.show();

        // Hem iç hem dış katılımcıları yükle
        Promise.all([
            fetch('/api/training-video-assignments/' + assignment.id + '/participants', { credentials: 'include' }).then(function(r) { return r.json(); }),
            fetch('/api/training-video-assignments/' + assignment.id + '/external-participants', { credentials: 'include' }).then(function(r) { return r.json(); })
        ])
        .then(function(results) {
            var internalData = Array.isArray(results[0]) ? results[0] : [];
            var externalData = Array.isArray(results[1]) ? results[1] : [];

            var internalParticipants = internalData.map(function(p) {
                p.isExternal = false;
                return p;
            });
            var externalParticipants = externalData.map(function(p) {
                return {
                    userName: p.fullName || p.email,
                    email: p.email,
                    statusId: p.statusId,
                    watchedSeconds: p.watchedSeconds,
                    completedAt: p.completedAt,
                    isExternal: true
                };
            });
            self.participants(internalParticipants.concat(externalParticipants));
        })
        .catch(function(err) {
            toastr.error(T('Common.Error', 'Hata olustu'));
            console.error(err);
        })
        .finally(function() {
            self.isLoadingParticipants(false);
        });
    };

    // ===== EMAIL MODAL =====

    // Filtrelenen katılımcılar
    self.emailFilteredParticipants = ko.computed(function() {
        var participants = self.emailModalParticipants();
        var filterEmailSent = self.emailFilterEmailSent();
        var filterHasStarted = self.emailFilterHasStarted();
        var filterIsCompleted = self.emailFilterIsCompleted();

        return ko.utils.arrayFilter(participants, function(p) {
            // Email durumu filtresi
            if (filterEmailSent === 'true' && p.emailSentCount === 0) return false;
            if (filterEmailSent === 'false' && p.emailSentCount > 0) return false;

            // İzleme durumu filtresi
            if (filterHasStarted === 'true' && p.statusId === 1) return false;
            if (filterHasStarted === 'false' && p.statusId !== 1) return false;

            // Tamamlama durumu filtresi
            if (filterIsCompleted === 'true' && !p.isCompleted) return false;
            if (filterIsCompleted === 'false' && p.isCompleted) return false;

            return true;
        });
    });

    // Seçili sayısı
    self.emailSelectedCount = ko.computed(function() {
        return ko.utils.arrayFilter(self.emailModalParticipants(), function(p) {
            return p.selected();
        }).length;
    });

    // Select all checkbox subscription
    self.emailSelectAll.subscribe(function(checked) {
        ko.utils.arrayForEach(self.emailFilteredParticipants(), function(p) {
            p.selected(checked);
        });
    });

    // Yeni dış katılımcı atama popup'ı (header butonu)
    self.openExternalAssignmentModal = function() {
        var width = 1200;
        var height = 700;
        var left = (screen.width - width) / 2;
        var top = (screen.height - height) / 2;
        window.open(
            '/TrainingVideos/ExternalParticipants/0',
            'ExternalParticipants_New',
            'width=' + width + ',height=' + height + ',left=' + left + ',top=' + top + ',resizable=yes,scrollbars=yes'
        );
    };

    // Mevcut atamaya dış katılımcı ekleme popup'ı
    self.openExternalParticipantsModal = function(assignment) {
        var width = 1100;
        var height = 650;
        var left = (screen.width - width) / 2;
        var top = (screen.height - height) / 2;
        window.open(
            '/TrainingVideos/ExternalParticipants/' + assignment.id,
            'ExternalParticipants_' + assignment.id,
            'width=' + width + ',height=' + height + ',left=' + left + ',top=' + top + ',resizable=yes,scrollbars=yes'
        );
    };

    // Email modalını aç
    self.openEmailModal = function(assignment) {
        self.emailModalAssignmentId(assignment.id);
        self.emailModalAssignmentTitle(assignment.title);
        self.emailModalParticipants([]);
        self.emailFilterEmailSent('');
        self.emailFilterHasStarted('');
        self.emailFilterIsCompleted('');
        self.emailModalTypeId('1');
        self.emailSelectAll(false);

        if (!self.emailModal) {
            self.emailModal = new bootstrap.Modal(document.getElementById('emailModal'));
        }
        self.emailModal.show();

        self.loadEmailParticipants(assignment.id);
    };

    // Email katılımcılarını yükle (iç + dış)
    self.loadEmailParticipants = function(assignmentId) {
        self.isLoadingEmailParticipants(true);

        // Hem iç hem dış katılımcıları yükle
        Promise.all([
            fetch('/api/training-video-assignments/' + assignmentId + '/participants', { credentials: 'include' }).then(function(r) { return r.json(); }),
            fetch('/api/training-video-assignments/' + assignmentId + '/external-participants', { credentials: 'include' }).then(function(r) { return r.json(); })
        ])
        .then(function(results) {
            var internalData = Array.isArray(results[0]) ? results[0] : [];
            var externalData = Array.isArray(results[1]) ? results[1] : [];

            var internalParticipants = internalData.map(function(p) {
                p.selected = ko.observable(false);
                p.isExternal = false;
                return p;
            });
            var externalParticipants = externalData.map(function(p) {
                return {
                    id: p.id,
                    userName: p.fullName || p.email,
                    email: p.email,
                    statusId: p.statusId,
                    emailSentCount: p.emailSentCount || 0,
                    lastEmailSentAt: p.lastEmailSentAt,
                    isCompleted: p.isCompleted,
                    watchedSeconds: p.watchedSeconds,
                    selected: ko.observable(false),
                    isExternal: true
                };
            });
            self.emailModalParticipants(internalParticipants.concat(externalParticipants));
        })
        .catch(function(err) {
            toastr.error(T('Common.Error', 'Katilimcilar yuklenemedi'));
            console.error(err);
        })
        .finally(function() {
            self.isLoadingEmailParticipants(false);
        });
    };

    // Tümünü seç
    self.selectAllEmailParticipants = function() {
        ko.utils.arrayForEach(self.emailFilteredParticipants(), function(p) {
            p.selected(true);
        });
    };

    // Seçimi temizle
    self.clearEmailSelection = function() {
        ko.utils.arrayForEach(self.emailModalParticipants(), function(p) {
            p.selected(false);
        });
    };

    // Email gönder (iç + dış katılımcılar için ayrı API)
    self.sendEmails = function() {
        var internalIds = [];
        var externalIds = [];
        ko.utils.arrayForEach(self.emailModalParticipants(), function(p) {
            if (p.selected()) {
                if (p.isExternal) {
                    externalIds.push(p.id);
                } else {
                    internalIds.push(p.id);
                }
            }
        });

        var totalSelected = internalIds.length + externalIds.length;
        if (totalSelected === 0) {
            toastr.warning(T('TrainingVideo.SelectParticipant', 'Lütfen en az bir katılımcı seçin'));
            return;
        }

        showConfirmModal({
            title: T('TrainingVideo.SendEmailTitle', 'Email Gönder'),
            message: totalSelected + T('TrainingVideo.SendEmailConfirm', ' kişiye email göndermek istediğinize emin misiniz?'),
            confirmText: T('Common.Send', 'Gönder'),
            confirmClass: 'btn-primary',
            onConfirm: function() {
                self.isSendingEmails(true);

                var promises = [];
                var assignmentId = self.emailModalAssignmentId();

                // İç katılımcılar için
                if (internalIds.length > 0) {
                    var internalDto = {
                        assignmentId: assignmentId,
                        participantIds: internalIds,
                        emailTypeId: parseInt(self.emailModalTypeId())
                    };
                    promises.push(
                        fetch('/api/training-video-assignments/send-emails', {
                            method: 'POST',
                            headers: { 'Content-Type': 'application/json' },
                            body: JSON.stringify(internalDto),
                            credentials: 'include'
                        }).then(function(r) { return r.json(); })
                    );
                }

                // Dış katılımcılar için
                if (externalIds.length > 0) {
                    var externalDto = {
                        participantIds: externalIds,
                        emailTypeId: parseInt(self.emailModalTypeId())
                    };
                    promises.push(
                        fetch('/api/training-video-assignments/' + assignmentId + '/external-participants/send-emails', {
                            method: 'POST',
                            headers: { 'Content-Type': 'application/json' },
                            body: JSON.stringify(externalDto),
                            credentials: 'include'
                        }).then(function(r) { return r.json(); })
                    );
                }

                Promise.all(promises)
                .then(function(results) {
                    var totalSent = 0;
                    results.forEach(function(result) {
                        if (result.sentCount !== undefined) {
                            totalSent += result.sentCount;
                        }
                    });
                    if (totalSent > 0) {
                        toastr.success(totalSent + T('TrainingVideo.EmailsSent', ' email gönderildi'));
                        // Listeyi yenile
                        self.loadEmailParticipants(assignmentId);
                        self.loadAssignments();
                    } else {
                        toastr.error(T('Common.Error', 'Hata oluştu'));
                    }
                })
                .catch(function(err) {
                    toastr.error(T('TrainingVideo.EmailSendError', 'Email gönderilemedi'));
                    console.error(err);
                })
                .finally(function() {
                    self.isSendingEmails(false);
                });
            }
        });
    };

    // Initialize
    self.loadAssignments();
    self.loadVideos();
    self.loadProjects();
    self.loadCustomers();
    self.loadOrganizations();
    self.loadCustomerPersonnel();
    self.loadEmailTemplates();
}

// Apply bindings after translations loaded
$(document).ready(function() {
    Localization.loadKeys(TRANSLATION_KEYS).then(function() {
        ko.applyBindings(new AssignmentsViewModel(), document.getElementById('assignments-app'));
    });
});
