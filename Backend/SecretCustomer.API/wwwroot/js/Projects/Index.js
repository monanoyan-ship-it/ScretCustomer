// Projects Index ViewModel - Consolidated
// Pattern: Single Index.cshtml + Index.js with modals

// Team Member ViewModel
function TeamMemberViewModel(data) {
    var self = this;
    data = data || {};
    self.userId = ko.observable(data.userId || '');
    self.role = ko.observable(data.role || 'Evaluator');
}

// Project Edit ViewModel
function ProjectEditViewModel(data) {
    var self = this;
    data = data || {};

    self.id = data.id || null;
    self.code = ko.observable(data.code || '');
    self.name = ko.observable(data.name || '');
    self.description = ko.observable(data.description || '');
    self.checklistId = ko.observable(data.checklistId || '');
    self.projectType = ko.observable(data.projectType || 'MysteryShopping');
    self.assignmentType = ko.observable(data.assignmentType || 'InternalBranch');
    self.startDate = ko.observable(data.startDate ? data.startDate.split('T')[0] : '');
    self.endDate = ko.observable(data.endDate ? data.endDate.split('T')[0] : '');
    self.customerId = ko.observable(data.customerId || null);
    self.organizationId = ko.observable(data.organizationId || null);
    self.projectManagerId = ko.observable(data.projectManagerId || null);

    // Budget
    self.estimatedBudget = ko.observable(data.estimatedBudget || null);
    self.costPerEvaluation = ko.observable(data.costPerEvaluation || null);

    // Reporting
    self.reportingFrequencyDays = ko.observable(data.reportingFrequencyDays || null);
    self.autoGenerateReports = ko.observable(data.autoGenerateReports || false);
    self.minimumScoreThreshold = ko.observable(data.minimumScoreThreshold || null);

    // Other
    self.priority = ko.observable(data.priority || '');
    self.tags = ko.observable(data.tags || '');
    self.notes = ko.observable(data.notes || '');

    // Survey Settings (OnlineSurvey proje tipi için)
    self.surveyIdentityType = ko.observable(data.surveyIdentityType || 'Anonymous');
    self.emailTemplateId = ko.observable(data.emailTemplateId || null);
    self.reminderEmailTemplateId = ko.observable(data.reminderEmailTemplateId || null);
    self.sendReminderEmails = ko.observable(data.sendReminderEmails || false);
    self.reminderDaysBeforeEnd = ko.observable(data.reminderDaysBeforeEnd || null);

    // Assessment Settings (PersonnelAssessment proje tipi için)
    self.assessmentModeId = ko.observable(data.assessmentModeId || 1);
    self.isAnonymous = ko.observable(data.isAnonymous || false);
    self.minRespondentsForAnonymity = ko.observable(data.minRespondentsForAnonymity || 2);

    // Team Members
    self.teamMembers = ko.observableArray([]);

    // Load team members
    if (data.teamMembers && data.teamMembers.length > 0) {
        data.teamMembers.forEach(function(tm) {
            self.teamMembers.push(new TeamMemberViewModel({ userId: tm.userId, role: tm.role }));
        });
    }

    self.toDTO = function() {
        return {
            code: self.code() || null,
            name: self.name(),
            description: self.description() || null,
            checklistId: self.checklistId(),
            projectType: self.projectType(),
            assignmentType: self.assignmentType(),
            startDate: self.startDate(),
            endDate: self.endDate(),
            customerId: self.customerId() || null,
            organizationId: self.organizationId() || null,
            projectManagerId: self.projectManagerId() || null,
            estimatedBudget: self.estimatedBudget() || null,
            costPerEvaluation: self.costPerEvaluation() || null,
            reportingFrequencyDays: self.reportingFrequencyDays() || null,
            autoGenerateReports: self.autoGenerateReports(),
            minimumScoreThreshold: self.minimumScoreThreshold() || null,
            priority: self.priority() || null,
            tags: self.tags() || null,
            notes: self.notes() || null,
            // Survey Settings
            surveyIdentityType: self.projectType() === 'OnlineSurvey' ? self.surveyIdentityType() : null,
            emailTemplateId: self.projectType() === 'OnlineSurvey' ? self.emailTemplateId() : null,
            reminderEmailTemplateId: self.projectType() === 'OnlineSurvey' ? self.reminderEmailTemplateId() : null,
            sendReminderEmails: self.projectType() === 'OnlineSurvey' ? self.sendReminderEmails() : false,
            reminderDaysBeforeEnd: self.projectType() === 'OnlineSurvey' ? self.reminderDaysBeforeEnd() : null,
            // Assessment Settings
            assessmentModeId: self.projectType() === 'PersonnelAssessment' ? parseInt(self.assessmentModeId()) : null,
            isAnonymous: self.projectType() === 'PersonnelAssessment' ? self.isAnonymous() : false,
            minRespondentsForAnonymity: self.projectType() === 'PersonnelAssessment' ? parseInt(self.minRespondentsForAnonymity()) : 2,
            teamMembers: self.teamMembers().map(function(tm) {
                return { userId: tm.userId(), role: tm.role() };
            }).filter(function(tm) { return tm.userId; })
        };
    };
}

// Main ViewModel
function ProjectsViewModel() {
    var self = this;

    // State
    self.isLoading = ko.observable(false);
    self.isLoadingModal = ko.observable(false);
    self.isSaving = ko.observable(false);
    self.errorMessage = ko.observable('');
    self.successMessage = ko.observable('');
    self.modalErrorMessage = ko.observable('');

    // Data
    self.projects = ko.observableArray([]);
    self.stats = ko.observable({ total: 0, active: 0, upcoming: 0, completed: 0 });

    // Dropdown data (loaded via API)
    self.checklists = ko.observableArray([]);
    self.customers = ko.observableArray([]);
    self.users = ko.observableArray([]);
    self.availableOrganizations = ko.observableArray([]);
    self.emailTemplates = ko.observableArray([]);

    // Proje tipi - Checklist tipi eslesmesi
    var projectChecklistRelations = {
        'CallAuditing': ['CallPerformance'],
        'PhysicalAudit': ['PhysicalAudit'],
        'MysteryShopping': ['MysteryShopping', 'CallPerformance'],
        'OnlineSurvey': ['Survey', 'Enneagram', 'OnlineEvaluation'],
        'CustomerSatisfaction': ['Survey', 'CallPerformance'],
        'TrainingEvaluation': ['OnlineEvaluation'],
        'QualityControl': ['CallPerformance', 'PhysicalAudit'],
        'PersonnelAssessment': ['PersonnelAssessment']
    };

    // View mode
    self.viewMode = ko.observable('table');

    // Sorting
    self.sorting = TableSorting.createSortState('createdAt', 'desc');

    // Pagination
    self.currentPage = ko.observable(1);
    self.pageSize = ko.observable(20);

    // ==================== FILTER SYSTEM ====================
    self.selectedFilterType = ko.observable('');
    self.activeFilters = ko.observableArray([]);

    // Temp filter values (for adding new filter)
    self.tempFilter = {
        customerId: ko.observable(null),
        projectType: ko.observable(''),
        status: ko.observable(''),
        projectManagerId: ko.observable(null),
        searchTerm: ko.observable(''),
        startDate: ko.observable(''),
        endDate: ko.observable(''),
        selectedDateRangeType: ko.observable(null)
    };

    // Date range quick select tracking
    self._manualDateChange = true;
    self.tempFilter.startDate.subscribe(function() {
        if (self._manualDateChange) self.tempFilter.selectedDateRangeType(null);
    });
    self.tempFilter.endDate.subscribe(function() {
        if (self._manualDateChange) self.tempFilter.selectedDateRangeType(null);
    });

    // Filter labels
    self.filterLabels = {
        customer: 'Müşteri',
        projectType: 'Proje Tipi',
        status: 'Durum',
        projectManager: 'Proje Yöneticisi',
        search: 'Arama',
        endingDate: 'Bitiş Tarihi'
    };

    self.statusLabels = {
        'Draft': 'Taslak',
        'Planned': 'Planlandı',
        'Active': 'Aktif',
        'Paused': 'Duraklatıldı',
        'Completed': 'Tamamlandı',
        'Cancelled': 'İptal Edildi'
    };

    // Proje bitiş tarihi seçenekleri
    self.endingDateOptions = [
        { systemName: 'overdue', name: 'Süresi Geçmiş' },
        { systemName: 'today', name: 'Bugün Bitecek' },
        { systemName: 'next7Days', name: '7 Gün İçinde Bitecek' },
        { systemName: 'thisWeek', name: 'Bu Hafta Bitecek' },
        { systemName: 'next30Days', name: '30 Gün İçinde Bitecek' },
        { systemName: 'thisMonth', name: 'Bu Ay Bitecek' },
        { systemName: 'nextMonth', name: 'Gelecek Ay Bitecek' },
        { systemName: 'thisQuarter', name: 'Bu Çeyrek Bitecek' }
    ];

    // Can add filter check
    self.canAddFilter = ko.computed(function() {
        var type = self.selectedFilterType();
        if (!type) return false;

        switch (type) {
            case 'customer': return self.tempFilter.customerId();
            case 'projectType': return self.tempFilter.projectType();
            case 'status': return self.tempFilter.status();
            case 'projectManager': return self.tempFilter.projectManagerId();
            case 'search': return self.tempFilter.searchTerm().trim() !== '';
            case 'endingDate': return self.tempFilter.selectedDateRangeType();
            default: return false;
        }
    });

    // Set temp date range (quick select)
    self.setTempDateRange = function(range) {
        var today = new Date();
        today.setHours(0, 0, 0, 0);
        var startDate = null;
        var endDate = null;

        var formatDate = function(date) {
            var year = date.getFullYear();
            var month = String(date.getMonth() + 1).padStart(2, '0');
            var day = String(date.getDate()).padStart(2, '0');
            return year + '-' + month + '-' + day;
        };

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
            case 'thisQuarter':
                var quarter = Math.floor(today.getMonth() / 3);
                startDate = new Date(today.getFullYear(), quarter * 3, 1);
                endDate = new Date(today.getTime());
                break;
            case 'thisYear':
                startDate = new Date(today.getFullYear(), 0, 1);
                endDate = new Date(today.getTime());
                break;
        }

        self._manualDateChange = false;
        if (startDate) self.tempFilter.startDate(formatDate(startDate));
        if (endDate) self.tempFilter.endDate(formatDate(endDate));
        self.tempFilter.selectedDateRangeType(range);
        self._manualDateChange = true;
    };

    // Add filter
    self.addFilter = function() {
        var type = self.selectedFilterType();
        if (!type) return;

        var filter = {
            id: Date.now(),
            type: type,
            displayLabel: self.filterLabels[type],
            value: null,
            displayValue: '',
            isFixed: false
        };

        switch (type) {
            case 'customer':
                var customerId = self.tempFilter.customerId();
                var customer = self.customers().find(function(c) { return c.id === customerId; });
                if (!customer) return;
                filter.value = customerId;
                filter.displayValue = customer.companyName + (customer.code ? ' (' + customer.code + ')' : '');
                self.tempFilter.customerId(null);
                break;

            case 'projectType':
                var projectType = self.tempFilter.projectType();
                if (!projectType) return;
                filter.value = projectType;
                filter.displayValue = self.projectTypeTexts[projectType] || projectType;
                self.tempFilter.projectType('');
                break;

            case 'status':
                var status = self.tempFilter.status();
                if (!status) return;
                filter.value = status;
                filter.displayValue = self.statusLabels[status] || status;
                self.tempFilter.status('');
                break;

            case 'projectManager':
                var managerId = self.tempFilter.projectManagerId();
                var manager = self.users().find(function(u) { return u.id === managerId; });
                if (!manager) return;
                filter.value = managerId;
                filter.displayValue = manager.fullName;
                self.tempFilter.projectManagerId(null);
                break;

            case 'search':
                var searchTerm = self.tempFilter.searchTerm().trim();
                if (!searchTerm) return;
                filter.value = searchTerm;
                filter.displayValue = '"' + searchTerm + '"';
                self.tempFilter.searchTerm('');
                break;

            case 'endingDate':
                var endingType = self.tempFilter.selectedDateRangeType();
                if (!endingType) return;

                var optionInfo = self.endingDateOptions.find(function(o) { return o.systemName === endingType; });
                filter.value = endingType;
                filter.displayValue = optionInfo ? optionInfo.name : endingType;

                self.tempFilter.selectedDateRangeType(null);
                break;

            default:
                return;
        }

        // Tüm filtre tipleri çoklu değer destekler (aynı tipten birden fazla eklenebilir)
        self.activeFilters.push(filter);
        self.selectedFilterType('');
        self.currentPage(1);
        self.loadProjects(); // Backend'e filtre gönder
    };

    // Remove filter
    self.removeFilter = function(filter) {
        // Sabit filtreler kaldırılamaz
        if (filter.isFixed) return;

        self.activeFilters.remove(filter);
        self.currentPage(1);
        self.loadProjects(); // Backend'e filtre gönder
    };

    // Clear all filters
    self.clearFilters = function() {
        // Sabit filtreleri koru, diğerlerini kaldır
        var fixedFilters = self.activeFilters().filter(function(f) { return f.isFixed; });
        self.activeFilters(fixedFilters);
        self.sorting.reset();
        self.currentPage(1);
        self.loadProjects(); // Backend'e filtre gönder
    };

    // Modals
    self.isEditModalOpen = ko.observable(false);
    self.isDetailModalOpen = ko.observable(false);
    self.editingProject = ko.observable(null);
    self.viewingProject = ko.observable(null);

    // Proje tipine gore filtrelenmis checklist listesi
    self.filteredChecklists = ko.computed(function() {
        var project = self.editingProject();
        if (!project) return self.checklists();
        var pt = project.projectType();
        var allowed = projectChecklistRelations[pt];
        if (!allowed) return self.checklists();
        return self.checklists().filter(function(c) {
            return allowed.indexOf(c.checklistType) !== -1;
        });
    });

    // File Management
    self.projectFiles = ko.observableArray([]);
    self.isUploadingFile = ko.observable(false);
    self.currentUserRole = ko.observable('');

    // Survey Modal
    self.isSurveyModalOpen = ko.observable(false);
    self.isSurveyLoading = ko.observable(false);
    self.isSurveySending = ko.observable(false);
    self.isRetrying = ko.observable(false);
    self.surveyProject = ko.observable(null);
    self.surveyPersonnelStats = ko.observable(null);
    self.surveyResult = ko.observable(null);
    self.invitationStats = ko.observable(null);
    self.invitationList = ko.observableArray([]);
    self.surveyTabMode = ko.observable('personnel'); // 'personnel' or 'external'

    // External Survey
    self.externalEmails = ko.observable('');
    self.externalEmailTemplateId = ko.observable(null);
    self.externalInvitationStats = ko.observable(null);
    self.externalInvitations = ko.observableArray([]);
    self.externalSendResult = ko.observable(null);
    self.isExternalSending = ko.observable(false);
    self.isExternalRetrying = ko.observable(false);
    self.isExternalUploading = ko.observable(false);
    self.externalUploadResult = ko.observable(null);
    self.externalReminderFilter = ko.observable('notCompleted');
    self.isExternalSendingReminder = ko.observable(false);

    // Check if user can manage files (Admin or TeamLeader)
    self.canManageFiles = ko.computed(function() {
        var role = self.currentUserRole();
        return role === 'Admin' || role === 'TeamLeader';
    });

    // Page config - proje tipi sabitlenmiş mi?
    self.pageProjectTypeId = (window.pageConfig && window.pageConfig.projectTypeId) ? window.pageConfig.projectTypeId : null;

    // Project Type mappings (EnumsService'de yok, burada kalabilir)
    self.projectTypeById = {
        1: 'MysteryShopping', 2: 'CallAuditing', 3: 'PhysicalAudit', 4: 'OnlineSurvey',
        5: 'CustomerSatisfaction', 6: 'TrainingEvaluation', 7: 'QualityControl', 8: 'PersonnelAssessment'
    };
    self.projectTypeTexts = {
        'MysteryShopping': 'Gizli Müşteri', 'CallAuditing': 'Çağrı Denetimi',
        'PhysicalAudit': 'Fiziksel Denetim', 'OnlineSurvey': 'Online Anket',
        'CustomerSatisfaction': 'Müşteri Memnuniyeti', 'TrainingEvaluation': 'Eğitim Değerlendirme',
        'QualityControl': 'Kalite Kontrol', 'PersonnelAssessment': 'Personel Değerlendirme'
    };
    // Sabitlenmiş proje tipi varsa string değerini hesapla
    self.pageProjectType = self.pageProjectTypeId ? self.projectTypeById[self.pageProjectTypeId] : null;
    self.projectTypeBadges = {
        'MysteryShopping': 'bg-primary', 'CallAuditing': 'bg-info',
        'PhysicalAudit': 'bg-secondary', 'OnlineSurvey': 'bg-success',
        'CustomerSatisfaction': 'bg-warning text-dark', 'TrainingEvaluation': 'bg-dark',
        'QualityControl': 'bg-danger', 'PersonnelAssessment': 'bg-info'
    };
    self.roleTexts = { 'Evaluator': 'Değerlendirici', 'Manager': 'Yönetici', 'Observer': 'Gözlemci' };

    // Helpers - EnumsService kullanir
    self.getStatusText = function(status) { return EnumsService.getProjectStatusDisplay(status); };
    self.getStatusBadge = function(status) { return EnumsService.getProjectStatusCss(status); };
    self.getProjectTypeText = function(type) { return self.projectTypeTexts[type] || type; };
    self.getProjectTypeBadge = function(type) { return self.projectTypeBadges[type] || 'bg-secondary'; };
    self.getRoleText = function(role) { return self.roleTexts[role] || role; };
    self.formatDate = function(dateStr) {
        if (!dateStr) return '-';
        return new Date(dateStr).toLocaleDateString('tr-TR');
    };
    self.formatCurrency = function(value) {
        if (!value) return '-';
        return new Intl.NumberFormat('tr-TR', { style: 'currency', currency: 'TRY' }).format(value);
    };

    // Sorted projects - Filtreleme backend'de yapılıyor, sadece client-side sıralama
    self.allFilteredProjects = ko.computed(function() {
        var result = self.projects();

        // Apply sorting (filtreleme backend'de yapılıyor)
        var sortBy = self.sorting.sortBy();
        var sortDir = self.sorting.sortDirection();
        if (sortBy) {
            result = TableSorting.clientSort(result, sortBy, sortDir);
        }
        return result;
    });

    // Pagination computed'ları
    self.totalCount = ko.computed(function() { return self.allFilteredProjects().length; });
    self.totalPages = ko.computed(function() {
        return Math.ceil(self.totalCount() / parseInt(self.pageSize(), 10)) || 1;
    });

    // Sayfalanmış liste (view'da kullanılacak)
    self.filteredProjects = ko.computed(function() {
        var list = self.allFilteredProjects();
        var page = parseInt(self.currentPage(), 10);
        var pageSize = parseInt(self.pageSize(), 10);
        var start = (page - 1) * pageSize;
        return list.slice(start, start + pageSize);
    });

    // Sayfa boyutu değişince sayfa 1'e dön
    self.pageSize.subscribe(function() { self.currentPage(1); });

    // Pagination fonksiyonları
    self.goToPage = function(page) {
        if (page >= 1 && page <= self.totalPages()) self.currentPage(page);
    };
    self.prevPage = function() { if (self.currentPage() > 1) self.currentPage(self.currentPage() - 1); };
    self.nextPage = function() { if (self.currentPage() < self.totalPages()) self.currentPage(self.currentPage() + 1); };
    self.firstPage = function() { self.currentPage(1); };
    self.lastPage = function() { self.currentPage(self.totalPages()); };

    // Sayfa numaraları dizisi
    self.pageNumbers = ko.computed(function() {
        var current = self.currentPage();
        var total = self.totalPages();
        var pages = [];
        var start = Math.max(1, current - 2);
        var end = Math.min(total, start + 4);
        if (end - start < 4) start = Math.max(1, end - 4);
        for (var i = start; i <= end; i++) pages.push(i);
        return pages;
    });

    // Load dropdown data
    self.loadDropdownData = function() {
        // Load checklists
        fetch('/api/checklists', { credentials: 'include' })
            .then(function(res) { return res.json(); })
            .then(function(data) { self.checklists(data || []); })
            .catch(function() { console.error('Checklists could not be loaded'); });

        // Load customers (PagedCustomerResult döner, items içindeki listeyi al)
        fetch('/api/customers?pageSize=1000', { credentials: 'include' })
            .then(function(res) { return res.json(); })
            .then(function(data) { self.customers(data.items || []); })
            .catch(function() { console.error('Customers could not be loaded'); });

        // Load users
        fetch('/api/users', { credentials: 'include' })
            .then(function(res) { return res.json(); })
            .then(function(data) { self.users(data || []); })
            .catch(function() { console.error('Users could not be loaded'); });

        // Load email templates (for survey projects)
        fetch('/api/email-templates?templateTypeId=1', { credentials: 'include' })
            .then(function(res) { return res.json(); })
            .then(function(data) { self.emailTemplates(data || []); })
            .catch(function() { console.error('Email templates could not be loaded'); });
    };

    // Load organizations by customer
    self.loadOrganizations = function(customerId) {
        if (!customerId) {
            self.availableOrganizations([]);
            return;
        }

        fetch('/api/customer-organizations/by-customer/' + customerId, { credentials: 'include' })
            .then(function(res) {
                if (!res.ok) throw new Error('Organizasyonlar yüklenemedi');
                return res.json();
            })
            .then(function(data) {
                self.availableOrganizations(data || []);
            })
            .catch(function(error) {
                console.error('Error loading organizations:', error);
                self.availableOrganizations([]);
            });
    };

    // Build query string from active filters
    self.buildFilterQueryString = function() {
        var params = new URLSearchParams();
        params.append('includeInactive', 'true');

        // Collect filter values by type (çoklu değer desteği)
        var customerIds = [];
        var projectTypes = [];
        var statuses = [];
        var projectManagerIds = [];
        var searchText = '';
        var endingDateFilter = '';

        self.activeFilters().forEach(function(filter) {
            switch (filter.type) {
                case 'customer':
                    customerIds.push(filter.value);
                    break;
                case 'projectType':
                    projectTypes.push(filter.value);
                    break;
                case 'status':
                    statuses.push(filter.value);
                    break;
                case 'projectManager':
                    projectManagerIds.push(filter.value);
                    break;
                case 'search':
                    searchText = filter.value;
                    break;
                case 'endingDate':
                    endingDateFilter = filter.value;
                    break;
            }
        });

        // Add to query string (çoğul parametreler)
        customerIds.forEach(function(id) { params.append('customerIds', id); });
        projectTypes.forEach(function(t) { params.append('projectTypes', t); });
        statuses.forEach(function(s) { params.append('statuses', s); });
        projectManagerIds.forEach(function(id) { params.append('projectManagerIds', id); });
        if (searchText) params.append('searchText', searchText);
        if (endingDateFilter) params.append('endingDateFilter', endingDateFilter);

        return params.toString();
    };

    // Load projects with filters
    self.loadProjects = function() {
        self.isLoading(true);
        self.errorMessage('');

        var queryString = self.buildFilterQueryString();
        fetch('/api/projects?' + queryString, { credentials: 'include' })
            .then(function(res) {
                if (!res.ok) throw new Error('Yükleme başarısız');
                return res.json();
            })
            .then(function(data) {
                self.projects(data);
                self.calculateStats(data);
            })
            .catch(function(error) {
                console.error('Error:', error);
                toastr.error('Projeler yüklenirken bir hata oluştu.');
            })
            .finally(function() {
                self.isLoading(false);
            });
    };

    self.calculateStats = function(projects) {
        var total = projects.length;
        var active = projects.filter(function(p) { return p.status === 'Active'; }).length;
        var completed = projects.filter(function(p) { return p.status === 'Completed'; }).length;
        var now = new Date();
        var weekLater = new Date();
        weekLater.setDate(weekLater.getDate() + 7);
        var upcoming = projects.filter(function(p) {
            if (p.status !== 'Active') return false;
            var endDate = new Date(p.endDate);
            return endDate >= now && endDate <= weekLater;
        }).length;
        self.stats({ total: total, active: active, upcoming: upcoming, completed: completed });
    };

    // Open create modal
    self.openCreateModal = function() {
        // Sabitlenmiş proje tipi varsa otomatik seç
        var initialData = {};
        if (self.pageProjectType) {
            initialData.projectType = self.pageProjectType;
        }

        var editVm = new ProjectEditViewModel(initialData);
        self.editingProject(editVm);
        self.availableOrganizations([]);

        // Subscribe to customerId changes to load organizations
        editVm.customerId.subscribe(function(customerId) {
            self.loadOrganizations(customerId);
            editVm.organizationId(null); // Reset organization when customer changes
        });

        // Proje tipi değişince uyumsuz checklist seçimini sıfırla
        editVm.projectType.subscribe(function(newType) {
            var allowed = projectChecklistRelations[newType];
            if (allowed && editVm.checklistId()) {
                var currentChecklist = self.checklists().find(function(c) { return c.id == editVm.checklistId(); });
                if (currentChecklist && allowed.indexOf(currentChecklist.checklistType) === -1) {
                    editVm.checklistId('');
                }
            }
        });

        self.isEditModalOpen(true);
    };

    // Open edit modal
    self.openEditModal = function(project) {
        self.isLoadingModal(true);
        fetch('/api/projects/' + project.id + '/detail', { credentials: 'include' })
            .then(function(res) {
                if (!res.ok) throw new Error('Proje yüklenemedi');
                return res.json();
            })
            .then(function(data) {
                // Önce organizasyonları yükle, sonra editingProject'i set et
                var loadOrgsPromise = data.customerId
                    ? fetch('/api/customer-organizations/by-customer/' + data.customerId, { credentials: 'include' })
                        .then(function(res) { return res.ok ? res.json() : []; })
                        .catch(function() { return []; })
                    : Promise.resolve([]);

                loadOrgsPromise.then(function(orgs) {
                    self.availableOrganizations(orgs);

                    var editVm = new ProjectEditViewModel(data);
                    self.editingProject(editVm);

                    // Subscribe to customerId changes to load organizations
                    editVm.customerId.subscribe(function(customerId) {
                        self.loadOrganizations(customerId);
                        editVm.organizationId(null); // Reset organization when customer changes
                    });

                    // Proje tipi değişince uyumsuz checklist seçimini sıfırla
                    editVm.projectType.subscribe(function(newType) {
                        var allowed = projectChecklistRelations[newType];
                        if (allowed && editVm.checklistId()) {
                            var currentChecklist = self.checklists().find(function(c) { return c.id == editVm.checklistId(); });
                            if (currentChecklist && allowed.indexOf(currentChecklist.checklistType) === -1) {
                                editVm.checklistId('');
                            }
                        }
                    });

                    self.isDetailModalOpen(false);
                    self.isEditModalOpen(true);
                    self.isLoadingModal(false);
                });
            })
            .catch(function(error) {
                console.error('Error:', error);
                toastr.error('Proje yüklenirken bir hata oluştu.');
                self.isLoadingModal(false);
            });
    };

    // Close edit modal
    self.closeEditModal = function() {
        self.isEditModalOpen(false);
        self.editingProject(null);
    };

    // Open detail modal
    self.openDetailModal = function(project) {
        // PersonnelAssessment projelerinde assessment panelini aç
        if (project.projectType === 'PersonnelAssessment') {
            self.openAssessmentPanel(project);
            return;
        }

        self.isLoadingModal(true);
        self.projectFiles([]); // Clear previous files
        fetch('/api/projects/' + project.id + '/detail', { credentials: 'include' })
            .then(function(res) {
                if (!res.ok) throw new Error('Proje yüklenemedi');
                return res.json();
            })
            .then(function(data) {
                self.viewingProject(data);
                self.isDetailModalOpen(true);
                // Load project files
                self.loadProjectFiles(data.id);
            })
            .catch(function(error) {
                console.error('Error:', error);
                toastr.error('Proje detayı yüklenirken bir hata oluştu.');
            })
            .finally(function() {
                self.isLoadingModal(false);
            });
    };

    // Close detail modal
    self.closeDetailModal = function() {
        self.isDetailModalOpen(false);
        self.viewingProject(null);
    };

    // ===== PersonnelAssessment - Dönem Yönetim Paneli =====

    self.isAssessmentPanelOpen = ko.observable(false);
    self.assessmentProject = ko.observable(null);
    self.assessmentPeriods = ko.observableArray([]);
    self.isAssessmentLoading = ko.observable(false);

    // Dönem form
    self.isPeriodFormOpen = ko.observable(false);
    self.isEditingPeriod = ko.observable(false);
    self.periodForm = {
        id: ko.observable(null),
        name: ko.observable(''),
        startDate: ko.observable(''),
        endDate: ko.observable(''),
        targetCount: ko.observable(0),
        notes: ko.observable('')
    };

    self.resetPeriodForm = function() {
        self.periodForm.id(null);
        self.periodForm.name('');
        self.periodForm.startDate('');
        self.periodForm.endDate('');
        self.periodForm.targetCount(0);
        self.periodForm.notes('');
        self.isEditingPeriod(false);
    };

    // Katılımcı yönetimi
    self.isParticipantsModalOpen = ko.observable(false);
    self.participantsPeriod = ko.observable(null);
    self.participants = ko.observableArray([]);
    self.isParticipantsLoading = ko.observable(false);
    self.availablePersonnel = ko.observableArray([]);
    self.newParticipantPersonnelId = ko.observable('');
    self.newParticipantParentId = ko.observable('');

    // Flat participant list (for parent dropdown)
    self.flatParticipants = ko.computed(function() {
        var result = [];
        function flatten(list) {
            for (var i = 0; i < list.length; i++) {
                var p = list[i];
                result.push(p);
                if (p.children && p.children.length > 0) flatten(p.children);
            }
        }
        flatten(self.participants());
        return result;
    });

    // Tree data: root nodes only (parentId == null)
    self.participantTreeData = ko.computed(function() {
        var all = self.participants();
        var roots = [];
        for (var i = 0; i < all.length; i++) {
            if (!all[i].parentId) roots.push(all[i]);
        }
        return { roots: roots };
    });

    // Participant role helpers (RoleId: 1=Manager, 2=Supervisor, 3=Operator)
    var roleNames = { 1: 'Yönetici', 2: 'Süpervizör', 3: 'Operatör' };
    var roleBadges = { 1: 'bg-primary', 2: 'bg-warning text-dark', 3: 'bg-secondary' };
    var roleIcons = { 1: 'bi-person-fill-gear text-primary', 2: 'bi-person-badge text-warning', 3: 'bi-person text-secondary' };

    self.getParticipantRoleName = function(cp) {
        if (!cp) return '';
        return roleNames[cp.roleId] || cp.title || '';
    };

    self.getParticipantRoleBadge = function(cp) {
        if (!cp) return 'bg-secondary';
        return roleBadges[cp.roleId] || 'bg-secondary';
    };

    self.getParticipantIcon = function(cp) {
        if (!cp) return 'bi-person text-secondary';
        return roleIcons[cp.roleId] || 'bi-person text-secondary';
    };

    // Move participant modal
    self.isMoveParticipantModalOpen = ko.observable(false);
    self.movingParticipant = ko.observable(null);
    self.moveTargetParentId = ko.observable('');

    self.moveParentOptions = ko.computed(function() {
        var moving = self.movingParticipant();
        if (!moving) return [];
        var flat = self.flatParticipants();
        return flat.filter(function(p) {
            // Exclude self and own children
            if (p.id === moving.id) return false;
            if (p.parentId === moving.id) return false;
            return true;
        });
    });

    self.openMoveParticipantModal = function(participant) {
        self.movingParticipant(participant);
        self.moveTargetParentId(participant.parentId ? String(participant.parentId) : '');
        self.isMoveParticipantModalOpen(true);
    };

    self.closeMoveParticipantModal = function() {
        self.isMoveParticipantModalOpen(false);
        self.movingParticipant(null);
        self.moveTargetParentId('');
    };

    self.confirmMoveParticipant = function() {
        var participant = self.movingParticipant();
        var period = self.participantsPeriod();
        if (!participant || !period) return;

        var newParentId = self.moveTargetParentId() ? parseInt(self.moveTargetParentId()) : null;

        fetch('/api/assessment/participants/' + participant.id + '/move', {
            method: 'PUT',
            headers: { 'Content-Type': 'application/json' },
            credentials: 'include',
            body: JSON.stringify({ newParentId: newParentId })
        })
        .then(function(res) {
            if (!res.ok) return res.json().then(function(e) { throw new Error(e.message || 'Hata'); });
            return res.json();
        })
        .then(function() {
            toastr.success('Katılımcı taşındı.');
            self.closeMoveParticipantModal();
            self.loadParticipants(period.id);
        })
        .catch(function(err) { toastr.error(err.message || 'Katılımcı taşınamadı.'); });
    };

    self.openAssessmentPanel = function(project) {
        self.isLoadingModal(true);
        fetch('/api/projects/' + project.id + '/detail', { credentials: 'include' })
            .then(function(res) {
                if (!res.ok) throw new Error('Proje yüklenemedi');
                return res.json();
            })
            .then(function(data) {
                self.assessmentProject(data);
                self.isAssessmentPanelOpen(true);
                self.loadAssessmentPeriods(data.id);
                // Organizasyon personelini yükle (katılımcı ekleme için)
                if (data.customerId) {
                    self.loadAvailablePersonnel(data.customerId, data.organizationId);
                }
            })
            .catch(function(err) {
                console.error('Assessment panel error:', err);
                toastr.error('Proje detayı yüklenemedi: ' + (err.message || ''));
            })
            .finally(function() { self.isLoadingModal(false); });
    };

    self.closeAssessmentPanel = function() {
        self.isAssessmentPanelOpen(false);
        self.assessmentProject(null);
        self.assessmentPeriods([]);
        self.closeParticipantsModal();
    };

    self.loadAssessmentPeriods = function(projectId) {
        self.isAssessmentLoading(true);
        fetch('/api/assessment/periods?projectId=' + projectId, { credentials: 'include' })
            .then(function(res) { return res.json(); })
            .then(function(data) { self.assessmentPeriods(data || []); })
            .catch(function() { toastr.error('Dönemler yüklenemedi.'); })
            .finally(function() { self.isAssessmentLoading(false); });
    };

    self.openAddPeriodForm = function() {
        self.resetPeriodForm();
        self.isPeriodFormOpen(true);
    };

    self.openEditPeriodForm = function(period) {
        self.periodForm.id(period.id);
        self.periodForm.name(period.name);
        self.periodForm.startDate(period.startDate ? period.startDate.split('T')[0] : '');
        self.periodForm.endDate(period.endDate ? period.endDate.split('T')[0] : '');
        self.periodForm.targetCount(period.targetCount || 0);
        self.periodForm.notes(period.notes || '');
        self.isEditingPeriod(true);
        self.isPeriodFormOpen(true);
    };

    self.closePeriodForm = function() {
        self.isPeriodFormOpen(false);
        self.resetPeriodForm();
    };

    self.savePeriod = function() {
        var project = self.assessmentProject();
        if (!project) return;

        if (!self.periodForm.name()) {
            toastr.error('Dönem adı zorunludur.');
            return;
        }
        if (!self.periodForm.startDate() || !self.periodForm.endDate()) {
            toastr.error('Başlangıç ve bitiş tarihi zorunludur.');
            return;
        }

        var isEdit = self.isEditingPeriod();
        var url = isEdit ? '/api/assessment/periods/' + self.periodForm.id() : '/api/assessment/periods';
        var method = isEdit ? 'PUT' : 'POST';

        var body = {
            projectId: project.id,
            name: self.periodForm.name(),
            startDate: self.periodForm.startDate(),
            endDate: self.periodForm.endDate(),
            targetCount: parseInt(self.periodForm.targetCount()) || 0,
            notes: self.periodForm.notes() || null
        };

        fetch(url, {
            method: method,
            headers: { 'Content-Type': 'application/json' },
            credentials: 'include',
            body: JSON.stringify(body)
        })
        .then(function(res) {
            if (!res.ok) return res.json().then(function(e) { throw new Error(e.message || 'Hata'); });
            return res.json();
        })
        .then(function() {
            toastr.success(isEdit ? 'Dönem güncellendi.' : 'Dönem oluşturuldu.');
            self.closePeriodForm();
            self.loadAssessmentPeriods(project.id);
        })
        .catch(function(err) { toastr.error(err.message || 'Dönem kaydedilemedi.'); });
    };

    self.deletePeriod = function(period) {
        showDeleteConfirm({
            title: 'Dönem Sil',
            message: '"' + period.name + '" dönemi silinecek. Emin misiniz?',
            onConfirm: function() {
                fetch('/api/assessment/periods/' + period.id, {
                    method: 'DELETE',
                    credentials: 'include'
                })
                .then(function(res) {
                    if (!res.ok) throw new Error('Silinemedi');
                    toastr.success('Dönem silindi.');
                    var project = self.assessmentProject();
                    if (project) self.loadAssessmentPeriods(project.id);
                })
                .catch(function() { toastr.error('Dönem silinirken hata oluştu.'); });
            }
        });
    };

    // ===== Katılımcı Yönetimi =====

    self.loadAvailablePersonnel = function(customerId, organizationId) {
        if (organizationId) {
            // Organizasyon endpoint: supervisor/team yapısı
            fetch('/api/customer-organizations/' + organizationId + '/personnel', { credentials: 'include' })
                .then(function(res) { return res.json(); })
                .then(function(data) {
                    var flat = [];
                    (data.supervisors || []).forEach(function(s) {
                        flat.push({ id: s.id, displayName: s.fullName, roleName: s.roleName, isSupervisor: true });
                        (s.teamMembers || []).forEach(function(tm) {
                            flat.push({ id: tm.id, displayName: tm.fullName, roleName: tm.roleName, isSupervisor: false });
                        });
                    });
                    (data.operators || []).forEach(function(o) {
                        flat.push({ id: o.id, displayName: o.fullName, roleName: o.roleName, isSupervisor: false });
                    });
                    self.availablePersonnel(flat);
                })
                .catch(function() { console.error('Organizasyon personel listesi yüklenemedi'); });
        } else {
            // Fallback: tüm müşteri personeli
            fetch('/api/customer-personnel/by-customer/' + customerId, { credentials: 'include' })
                .then(function(res) { return res.json(); })
                .then(function(data) {
                    var mapped = (data || []).map(function(p) {
                        return { id: p.id, displayName: (p.firstName + ' ' + p.lastName).trim(), roleName: p.title || '', isSupervisor: false };
                    });
                    self.availablePersonnel(mapped);
                })
                .catch(function() { console.error('Personel listesi yüklenemedi'); });
        }
    };

    self.openParticipantsModal = function(period) {
        self.participantsPeriod(period);
        self.isParticipantsModalOpen(true);
        self.loadParticipants(period.id);
    };

    self.closeParticipantsModal = function() {
        self.isParticipantsModalOpen(false);
        self.participantsPeriod(null);
        self.participants([]);
        self.newParticipantPersonnelId('');
        self.newParticipantParentId('');
    };

    self.loadParticipants = function(periodId) {
        self.isParticipantsLoading(true);
        fetch('/api/assessment/participants/' + periodId, { credentials: 'include' })
            .then(function(res) { return res.json(); })
            .then(function(data) { self.participants(data || []); })
            .catch(function() { toastr.error('Katılımcılar yüklenemedi.'); })
            .finally(function() { self.isParticipantsLoading(false); });
    };

    self.addParticipant = function() {
        var period = self.participantsPeriod();
        if (!period) return;
        var personnelId = self.newParticipantPersonnelId();
        if (!personnelId) {
            toastr.error('Personel seçmelisiniz.');
            return;
        }

        fetch('/api/assessment/participants', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            credentials: 'include',
            body: JSON.stringify({
                assignmentPeriodId: period.id,
                customerPersonnelId: parseInt(personnelId),
                parentId: self.newParticipantParentId() ? parseInt(self.newParticipantParentId()) : null
            })
        })
        .then(function(res) {
            if (!res.ok) return res.json().then(function(e) { throw new Error(e.message || 'Hata'); });
            return res.json();
        })
        .then(function() {
            toastr.success('Katılımcı eklendi.');
            self.newParticipantPersonnelId('');
            self.newParticipantParentId('');
            self.loadParticipants(period.id);
        })
        .catch(function(err) { toastr.error(err.message || 'Katılımcı eklenemedi.'); });
    };

    self.removeParticipant = function(participant) {
        var period = self.participantsPeriod();
        var name = participant.customerPersonnel
            ? (participant.customerPersonnel.firstName + ' ' + participant.customerPersonnel.lastName).trim()
            : 'Katılımcı';

        showDeleteConfirm({
            title: 'Katılımcı Çıkar',
            message: '"' + name + '" katılımcı listesinden çıkarılacak. Emin misiniz?',
            onConfirm: function() {
                fetch('/api/assessment/participants/' + participant.id, {
                    method: 'DELETE',
                    credentials: 'include'
                })
                .then(function(res) {
                    if (!res.ok) throw new Error('Silinemedi');
                    toastr.success('Katılımcı çıkarıldı.');
                    if (period) self.loadParticipants(period.id);
                })
                .catch(function() { toastr.error('Katılımcı çıkarılırken hata oluştu.'); });
            }
        });
    };

    self.importFromOrganization = function() {
        var period = self.participantsPeriod();
        var project = self.assessmentProject();
        if (!period || !project) return;

        self.isParticipantsLoading(true);
        fetch('/api/assessment/participants/import/' + period.id, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            credentials: 'include',
            body: JSON.stringify({ projectId: project.id })
        })
        .then(function(res) {
            if (!res.ok) return res.json().then(function(e) { throw new Error(e.message || 'Hata'); });
            return res.json();
        })
        .then(function(data) {
            toastr.success(data.message || 'Organizasyon aktarıldı.');
            self.loadParticipants(period.id);
        })
        .catch(function(err) { toastr.error(err.message || 'Organizasyon aktarılamadı.'); })
        .finally(function() { self.isParticipantsLoading(false); });
    };

    // ===== Email Gönderimi =====

    self.isEmailPanelOpen = ko.observable(false);
    self.assessmentEmailTemplates = ko.observableArray([]);
    self.selectedEmailTemplateId = ko.observable('');
    self.isEmailSending = ko.observable(false);
    self.emailSendResult = ko.observable(null);

    self.toggleEmailPanel = function() {
        var open = !self.isEmailPanelOpen();
        self.isEmailPanelOpen(open);
        if (open && self.assessmentEmailTemplates().length === 0) {
            self.loadAssessmentEmailTemplates();
        }
    };

    self.loadAssessmentEmailTemplates = function() {
        // AssessmentInvitation = templateTypeId 10
        fetch('/api/email-templates?templateTypeId=10', { credentials: 'include' })
            .then(function(res) { return res.json(); })
            .then(function(data) { self.assessmentEmailTemplates(data || []); })
            .catch(function() { toastr.error('Email şablonları yüklenemedi.'); });
    };

    self.sendAssessmentEmails = function() {
        var project = self.assessmentProject();
        var period = self.participantsPeriod();
        var templateId = self.selectedEmailTemplateId();

        if (!project || !period) return;
        if (!templateId) {
            toastr.error('Lütfen bir email şablonu seçin.');
            return;
        }

        showConfirmModal({
            title: 'Davetiye Gönder',
            message: 'Tüm katılımcılara değerlendirme davetiyesi gönderilecek. Devam etmek istiyor musunuz?',
            confirmText: 'Gönder',
            confirmClass: 'btn-info',
            onConfirm: function() {
                self.isEmailSending(true);
                self.emailSendResult(null);

                var baseUrl = window.location.origin + '/Assessment/Fill';

                fetch('/api/assessment/send-invitations', {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    credentials: 'include',
                    body: JSON.stringify({
                        projectId: project.id,
                        emailTemplateId: parseInt(templateId),
                        baseUrl: baseUrl,
                        assignmentPeriodId: period.id
                    })
                })
                .then(function(res) {
                    if (!res.ok) return res.json().then(function(e) { throw new Error(e.message || 'Hata'); });
                    return res.json();
                })
                .then(function(data) {
                    self.emailSendResult(data);
                    if (data.successCount > 0) {
                        toastr.success(data.message);
                    }
                    if (data.failCount > 0) {
                        toastr.warning(data.failCount + ' davetiye gönderilemedi.');
                    }
                })
                .catch(function(err) { toastr.error(err.message || 'Davetiyeler gönderilemedi.'); })
                .finally(function() { self.isEmailSending(false); });
            }
        });
    };

    self.generateAssessmentTasks = function() {
        var period = self.participantsPeriod();
        var project = self.assessmentProject();
        if (!period || !project) return;

        showConfirmModal({
            title: 'Değerlendirme Görevleri Oluştur',
            message: 'Tüm katılımcılar için değerlendirme görevleri ve davetiyeler oluşturulacak. Devam etmek istiyor musunuz?',
            confirmText: 'Oluştur',
            confirmClass: 'btn-info',
            onConfirm: function() {
                self.isParticipantsLoading(true);
                fetch('/api/assessment/generate-tasks', {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    credentials: 'include',
                    body: JSON.stringify({
                        assignmentPeriodId: period.id,
                        projectId: project.id
                    })
                })
                .then(function(res) {
                    if (!res.ok) return res.json().then(function(e) { throw new Error(e.message || 'Hata'); });
                    return res.json();
                })
                .then(function(data) {
                    toastr.success(data.message || 'Görevler oluşturuldu.');
                })
                .catch(function(err) { toastr.error(err.message || 'Görevler oluşturulamadı.'); })
                .finally(function() { self.isParticipantsLoading(false); });
            }
        });
    };

    // Add team member
    self.addTeamMember = function() {
        if (self.editingProject()) {
            self.editingProject().teamMembers.push(new TeamMemberViewModel());
        }
    };

    // Remove team member
    self.removeTeamMember = function(member) {
        if (self.editingProject()) {
            self.editingProject().teamMembers.remove(member);
        }
    };

    // Generate code
    self.generateCode = function() {
        fetch('/api/projects/generate-code', { credentials: 'include' })
            .then(function(res) {
                if (!res.ok) throw new Error('Kod oluşturulamadı');
                return res.json();
            })
            .then(function(data) {
                if (self.editingProject()) {
                    self.editingProject().code(data.code);
                }
            })
            .catch(function(error) {
                console.error('Error:', error);
                toastr.error('Proje kodu oluşturulurken bir hata oluştu.');
            });
    };

    // Save project
    self.saveProject = function() {
        var project = self.editingProject();
        if (!project) return;

        // Validation
        if (!project.name() || project.name().trim() === '') {
            toastr.error('Proje adı zorunludur!');
            return;
        }
        if (!project.checklistId()) {
            toastr.error('Kontrol listesi seçmelisiniz!');
            return;
        }
        if (!project.startDate() || !project.endDate()) {
            toastr.error('Başlangıç ve bitiş tarihleri zorunludur!');
            return;
        }

        var dto = project.toDTO();
        var isNew = !project.id;
        self.isSaving(true);
        self.errorMessage('');

        var endpoint = isNew ? '/api/projects' : '/api/projects/' + project.id;
        var method = isNew ? 'POST' : 'PUT';

        fetch(endpoint, {
            method: method,
            headers: { 'Content-Type': 'application/json' },
            credentials: 'include',
            body: JSON.stringify(dto)
        })
        .then(function(res) {
            if (!res.ok) {
                return res.json().then(function(data) {
                    throw new Error(data.message || data.error || 'Kayıt başarısız');
                });
            }
            return res.json();
        })
        .then(function(savedProject) {
            if (isNew) {
                // Yeni kayit: array'e ekle
                self.projects.unshift(savedProject); // Son eklenen en üstte
            } else {
                // Guncelleme: array'de bul ve guncelle
                var list = self.projects();
                for (var i = 0; i < list.length; i++) {
                    if (list[i].id === savedProject.id) {
                        self.projects.splice(i, 1, savedProject);
                        break;
                    }
                }
            }
            self.calculateStats(self.projects());
            toastr.success(isNew ? 'Proje oluşturuldu.' : 'Proje güncellendi.');
            self.closeEditModal();
        })
        .catch(function(error) {
            console.error('Error:', error);
            toastr.error('Proje kaydedilirken bir hata oluştu: ' + error.message);
        })
        .finally(function() {
            self.isSaving(false);
        });
    };

    // Helper: Array'de projeyi guncelle
    self.updateProjectInArray = function(updatedProject) {
        var list = self.projects();
        for (var i = 0; i < list.length; i++) {
            if (list[i].id === updatedProject.id) {
                self.projects.splice(i, 1, updatedProject);
                break;
            }
        }
        self.calculateStats(self.projects());
        // Detail modal aciksa guncelle
        if (self.isDetailModalOpen() && self.viewingProject() && self.viewingProject().id === updatedProject.id) {
            self.viewingProject(updatedProject);
        }
    };

    // Start project
    self.startProject = function(project) {
        showConfirmModal({
            title: 'Proje Başlat',
            message: 'Projeyi başlatmak istediğinizden emin misiniz?',
            type: 'success',
            confirmText: 'Başlat',
            confirmIcon: 'bi-play-fill',
            onConfirm: function() {
                fetch('/api/projects/' + project.id + '/start', {
                    method: 'POST',
                    credentials: 'include'
                })
                .then(function(res) {
                    if (!res.ok) return res.json().then(function(data) { throw new Error(data.message); });
                    return res.json();
                })
                .then(function(updatedProject) {
                    toastr.success('Proje başlatıldı.');
                    self.updateProjectInArray(updatedProject);
                })
                .catch(function(error) {
                    console.error('Error:', error);
                    toastr.error(error.message || 'Proje başlatılırken bir hata oluştu.');
                });
            }
        });
    };

    // Pause project
    self.pauseProject = function(project) {
        showConfirmModal({
            title: 'Proje Duraklat',
            message: 'Projeyi duraklatmak istediğinizden emin misiniz?',
            type: 'warning',
            confirmText: 'Duraklat',
            confirmIcon: 'bi-pause-fill',
            onConfirm: function() {
                fetch('/api/projects/' + project.id + '/pause', {
                    method: 'POST',
                    credentials: 'include'
                })
                .then(function(res) {
                    if (!res.ok) return res.json().then(function(data) { throw new Error(data.message); });
                    return res.json();
                })
                .then(function(updatedProject) {
                    toastr.success('Proje duraklatıldı.');
                    self.updateProjectInArray(updatedProject);
                })
                .catch(function(error) {
                    console.error('Error:', error);
                    toastr.error(error.message || 'Proje duraklatılırken bir hata oluştu.');
                });
            }
        });
    };

    // Complete project
    self.completeProject = function(project) {
        showConfirmModal({
            title: 'Proje Tamamla',
            message: 'Projeyi tamamlamak istediğinizden emin misiniz?',
            type: 'success',
            confirmText: 'Tamamla',
            confirmIcon: 'bi-check-circle',
            onConfirm: function() {
                fetch('/api/projects/' + project.id + '/complete', {
                    method: 'POST',
                    credentials: 'include'
                })
                .then(function(res) {
                    if (!res.ok) return res.json().then(function(data) { throw new Error(data.message); });
                    return res.json();
                })
                .then(function(updatedProject) {
                    toastr.success('Proje tamamlandı.');
                    self.updateProjectInArray(updatedProject);
                })
                .catch(function(error) {
                    console.error('Error:', error);
                    toastr.error(error.message || 'Proje tamamlanırken bir hata oluştu.');
                });
            }
        });
    };

    // Cancel project
    self.cancelProject = function(project) {
        showConfirmModal({
            title: 'Proje İptal',
            message: 'Projeyi iptal etmek istediğinizden emin misiniz?',
            type: 'danger',
            confirmText: 'İptal Et',
            confirmIcon: 'bi-x-circle',
            onConfirm: function() {
                fetch('/api/projects/' + project.id + '/cancel', {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    credentials: 'include',
                    body: JSON.stringify({ reason: null })
                })
                .then(function(res) {
                    if (!res.ok) return res.json().then(function(data) { throw new Error(data.message); });
                    return res.json();
                })
                .then(function(updatedProject) {
                    toastr.success('Proje iptal edildi.');
                    self.updateProjectInArray(updatedProject);
                    if (self.isDetailModalOpen()) {
                        self.closeDetailModal();
                    }
                })
                .catch(function(error) {
                    console.error('Error:', error);
                    toastr.error(error.message || 'Proje iptal edilirken bir hata oluştu.');
                });
            }
        });
    };

    // Reactivate project
    self.reactivateProject = function(project) {
        showConfirmModal({
            title: 'Projeyi Yeniden Aktif Et',
            message: 'Tamamlanan projeyi yeniden aktif etmek istediğinizden emin misiniz?',
            type: 'success',
            confirmText: 'Aktif Et',
            confirmIcon: 'bi-arrow-counterclockwise',
            onConfirm: function() {
                fetch('/api/projects/' + project.id + '/reactivate', {
                    method: 'POST',
                    credentials: 'include'
                })
                .then(function(res) {
                    if (!res.ok) return res.json().then(function(data) { throw new Error(data.message); });
                    return res.json();
                })
                .then(function(updatedProject) {
                    toastr.success('Proje yeniden aktif edildi.');
                    self.updateProjectInArray(updatedProject);
                })
                .catch(function(error) {
                    console.error('Error:', error);
                    toastr.error(error.message || 'Proje aktif edilirken bir hata oluştu.');
                });
            }
        });
    };

    // Delete project
    self.deleteProject = function(project) {
        showDeleteConfirm(project.name + ' projesi', function() {
            fetch('/api/projects/' + project.id, {
                method: 'DELETE',
                credentials: 'include'
            })
            .then(function(res) {
                if (!res.ok) throw new Error('Silme başarısız');
                toastr.success('Proje başarıyla silindi.');
                self.projects.remove(project);
                self.calculateStats(self.projects());
            })
            .catch(function(error) {
                console.error('Error:', error);
                toastr.error('Proje silinirken bir hata oluştu.');
            });
        });
    };

    // ========== FILE MANAGEMENT ==========

    // Load project files
    self.loadProjectFiles = function(projectId) {
        fetch('/api/project-files/project/' + projectId, { credentials: 'include' })
            .then(function(res) {
                if (!res.ok) throw new Error('Dosyalar yüklenemedi');
                return res.json();
            })
            .then(function(files) {
                self.projectFiles(files || []);
            })
            .catch(function(error) {
                console.error('Error loading files:', error);
                self.projectFiles([]);
            });
    };

    // Upload file
    self.uploadFile = function(event) {
        var files = event.target.files;
        if (!files || files.length === 0) return;

        var project = self.viewingProject();
        if (!project) return;

        self.isUploadingFile(true);

        // Upload files one by one
        var uploadPromises = [];
        for (var i = 0; i < files.length; i++) {
            var file = files[i];
            var formData = new FormData();
            formData.append('file', file);

            uploadPromises.push(
                fetch('/api/project-files/project/' + project.id, {
                    method: 'POST',
                    credentials: 'include',
                    body: formData
                })
                .then(function(res) {
                    if (!res.ok) {
                        return res.json().then(function(data) {
                            throw new Error(data.message || 'Yükleme başarısız');
                        });
                    }
                    return res.json();
                })
            );
        }

        Promise.all(uploadPromises)
            .then(function(uploadedFiles) {
                uploadedFiles.forEach(function(f) {
                    self.projectFiles.unshift(f);
                });
                toastr.success(uploadedFiles.length + ' dosya yüklendi.');
            })
            .catch(function(error) {
                console.error('Upload error:', error);
                toastr.error('Dosya yüklenirken bir hata oluştu: ' + error.message);
            })
            .finally(function() {
                self.isUploadingFile(false);
                // Reset file input
                event.target.value = '';
            });
    };

    // Download file
    self.downloadFile = function(file) {
        window.open('/api/project-files/' + file.id + '/download', '_blank');
    };

    // Delete file
    self.deleteFile = function(file) {
        showDeleteConfirm(file.originalFileName, function() {
            fetch('/api/project-files/' + file.id, {
                method: 'DELETE',
                credentials: 'include'
            })
            .then(function(res) {
                if (!res.ok) throw new Error('Silme başarısız');
                toastr.success('Dosya silindi.');
                self.projectFiles.remove(file);
            })
            .catch(function(error) {
                console.error('Error:', error);
                toastr.error('Dosya silinirken bir hata oluştu.');
            });
        });
    };

    // Get file icon based on content type
    self.getFileIcon = function(contentType) {
        if (!contentType) return 'bi-file-earmark';

        if (contentType.indexOf('image') >= 0) return 'bi-file-earmark-image text-success';
        if (contentType.indexOf('pdf') >= 0) return 'bi-file-earmark-pdf text-danger';
        if (contentType.indexOf('word') >= 0 || contentType.indexOf('document') >= 0) return 'bi-file-earmark-word text-primary';
        if (contentType.indexOf('excel') >= 0 || contentType.indexOf('spreadsheet') >= 0) return 'bi-file-earmark-excel text-success';
        if (contentType.indexOf('powerpoint') >= 0 || contentType.indexOf('presentation') >= 0) return 'bi-file-earmark-ppt text-warning';
        if (contentType.indexOf('zip') >= 0 || contentType.indexOf('rar') >= 0 || contentType.indexOf('compressed') >= 0) return 'bi-file-earmark-zip text-secondary';
        if (contentType.indexOf('text') >= 0) return 'bi-file-earmark-text';

        return 'bi-file-earmark';
    };

    // Load current user role
    self.loadCurrentUserRole = function() {
        fetch('/api/auth/me', { credentials: 'include' })
            .then(function(res) {
                if (!res.ok) return;
                return res.json();
            })
            .then(function(user) {
                if (user && user.role) {
                    self.currentUserRole(user.role);
                }
            })
            .catch(function() {
                // Ignore errors
            });
    };

    // ==================== SURVEY MODAL FUNCTIONS ====================

    // Open survey invitation modal
    self.exportSurveyLinks = function(project) {
        window.location.href = '/api/surveys/' + project.id + '/export-links';
    };

    self.exportSurveyQRZip = function(project) {
        window.location.href = '/api/surveys/' + project.id + '/export-qr-zip';
    };

    self.exportSurveyQRPdf = function(project) {
        window.location.href = '/api/surveys/' + project.id + '/export-qr-pdf';
    };

    self.openSurveyModal = function(project) {
        self.surveyProject(project);
        self.surveyPersonnelStats(null);
        self.surveyResult(null);
        self.invitationStats(null);
        self.invitationList([]);
        self.surveyTabMode('personnel');
        // External reset
        self.externalEmails('');
        self.externalEmailTemplateId(null);
        self.externalInvitationStats(null);
        self.externalInvitations([]);
        self.externalSendResult(null);
        self.externalReminderFilter('notCompleted');

        self.isSurveyModalOpen(true);
        document.body.classList.add('modal-open');

        // Load personnel preview and invitation stats
        self.loadSurveyPersonnelPreview(project.id);
        self.loadInvitationStats(project.id);
        // Load external invitation stats
        self.loadExternalInvitationStats(project.id);
    };

    // Close survey modal
    self.closeSurveyModal = function() {
        self.isSurveyModalOpen(false);
        document.body.classList.remove('modal-open');
        self.surveyProject(null);
        self.surveyPersonnelStats(null);
        self.surveyResult(null);
        self.invitationStats(null);
        self.invitationList([]);
        // External reset
        self.externalEmails('');
        self.externalEmailTemplateId(null);
        self.externalInvitationStats(null);
        self.externalInvitations([]);
        self.externalSendResult(null);
        self.externalReminderFilter('notCompleted');
    };

    // Load invitation stats
    self.loadInvitationStats = function(projectId) {
        fetch('/api/surveys/' + projectId + '/invitation-stats', { credentials: 'include' })
            .then(function(res) { return res.json(); })
            .then(function(data) {
                self.invitationStats(data);
            })
            .catch(function(error) {
                console.error('Error loading invitation stats:', error);
            });
    };

    // Load invitation list (optional - for detailed view)
    self.loadInvitationList = function(projectId, status) {
        var url = '/api/surveys/' + projectId + '/invitations';
        if (status) url += '?status=' + status;

        fetch(url, { credentials: 'include' })
            .then(function(res) { return res.json(); })
            .then(function(data) {
                self.invitationList(data);
            })
            .catch(function(error) {
                console.error('Error loading invitations:', error);
            });
    };

    // Retry failed invitations
    self.retryFailedInvitations = function() {
        var project = self.surveyProject();
        if (!project) return;

        var stats = self.invitationStats();
        if (!stats || stats.failed === 0) {
            toastr.info(T('Survey.NoFailedInvitations', 'Yeniden gönderilecek başarısız davetiye yok.'));
            return;
        }

        showConfirmModal({
            title: T('Project.RetryFailed', 'Başarısız Davetiyeleri Yeniden Gönder'),
            message: T('Survey.RetryFailedConfirm', 'Toplam ') + stats.failed + T('Survey.RetryFailedConfirm2', ' başarısız davetiye yeniden gönderilecek. Devam etmek istiyor musunuz?'),
            confirmText: T('Common.Send', 'Gönder'),
            confirmClass: 'btn-primary',
            onConfirm: function() {
                self.isRetrying(true);

                var baseUrl = window.location.origin + '/Survey/Form';
                var dto = { baseUrl: baseUrl };

                fetch('/api/surveys/' + project.id + '/retry-failed', {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    credentials: 'include',
                    body: JSON.stringify(dto)
                })
                .then(function(res) { return res.json(); })
                .then(function(result) {
                    if (result.success) {
                        toastr.success(result.message);
                        // Refresh stats
                        self.loadInvitationStats(project.id);
                    } else {
                        toastr.error(result.message || T('Survey.RetryError', 'Yeniden gönderim sırasında hata oluştu.'));
                    }
                })
                .catch(function(error) {
                    console.error('Error retrying invitations:', error);
                    toastr.error(T('Survey.RetryError', 'Yeniden gönderim sırasında bir hata oluştu.'));
                })
                .finally(function() {
                    self.isRetrying(false);
                });
            }
        });
    };

    // Load personnel preview for survey
    self.loadSurveyPersonnelPreview = function(projectId) {
        self.isSurveyLoading(true);

        fetch('/api/surveys/' + projectId + '/personnel-preview', { credentials: 'include' })
            .then(function(res) { return res.json(); })
            .then(function(data) {
                self.surveyPersonnelStats(data);
            })
            .catch(function(error) {
                console.error('Error loading personnel preview:', error);
                toastr.error('Personel listesi yüklenirken hata oluştu.');
            })
            .finally(function() {
                self.isSurveyLoading(false);
            });
    };

    // ==================== EXTERNAL SURVEY FUNCTIONS ====================

    // Load external invitation stats
    self.loadExternalInvitationStats = function(projectId) {
        fetch('/api/surveys/' + projectId + '/external-invitation-stats', { credentials: 'include' })
            .then(function(res) { return res.json(); })
            .then(function(data) {
                self.externalInvitationStats(data);
            })
            .catch(function(error) {
                console.error('Error loading external invitation stats:', error);
            });

        // Also load the list
        self.loadExternalInvitations(projectId);
    };

    // Load external invitations list
    self.loadExternalInvitations = function(projectId) {
        fetch('/api/surveys/' + projectId + '/external-invitations', { credentials: 'include' })
            .then(function(res) { return res.json(); })
            .then(function(data) {
                self.externalInvitations(data || []);
            })
            .catch(function(error) {
                console.error('Error loading external invitations:', error);
            });
    };

    // Send external invitations
    self.sendExternalInvitations = function() {
        var project = self.surveyProject();
        if (!project) return;

        var emails = self.externalEmails();
        if (!emails || emails.trim() === '') {
            toastr.warning('Lütfen email adresi girin.');
            return;
        }

        // Check if project has email template
        if (!project.emailTemplateId) {
            toastr.error('Email şablonu seçilmemiş. Proje ayarlarından şablon seçin.');
            return;
        }

        self.isExternalSending(true);
        self.externalSendResult(null);

        var baseUrl = window.location.origin + '/Survey/Form';
        var dto = {
            emails: emails,
            emailTemplateId: null, // Proje şablonu kullanılacak
            baseUrl: baseUrl
        };

        fetch('/api/surveys/' + project.id + '/send-external-invitations', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            credentials: 'include',
            body: JSON.stringify(dto)
        })
        .then(function(res) { return res.json(); })
        .then(function(result) {
            self.externalSendResult(result);
            if (result.success) {
                toastr.success(result.message);
                self.externalEmails(''); // Clear input
                // Refresh stats and list
                self.loadExternalInvitationStats(project.id);
            } else {
                toastr.error(result.message || 'Gönderim sırasında hata oluştu.');
            }
        })
        .catch(function(error) {
            console.error('Error sending external invitations:', error);
            toastr.error('Davetiye gönderilirken bir hata oluştu.');
            self.externalSendResult({ success: false, message: 'Beklenmeyen bir hata oluştu.' });
        })
        .finally(function() {
            self.isExternalSending(false);
        });
    };

    // Retry failed external invitations
    self.retryExternalFailed = function() {
        var project = self.surveyProject();
        if (!project) return;

        var stats = self.externalInvitationStats();
        if (!stats || stats.failed === 0) {
            toastr.info(T('Survey.NoFailedInvitations', 'Yeniden gönderilecek başarısız davetiye yok.'));
            return;
        }

        showConfirmModal({
            title: T('Project.RetryFailed', 'Başarısız Davetiyeleri Yeniden Gönder'),
            message: T('Survey.RetryFailedConfirm', 'Toplam ') + stats.failed + T('Survey.RetryFailedConfirm2', ' başarısız davetiye yeniden gönderilecek. Devam etmek istiyor musunuz?'),
            confirmText: T('Common.Send', 'Gönder'),
            confirmClass: 'btn-primary',
            onConfirm: function() {
                self.isExternalRetrying(true);

                var baseUrl = window.location.origin + '/Survey/Form';
                var dto = { baseUrl: baseUrl };

                fetch('/api/surveys/' + project.id + '/retry-external-failed', {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    credentials: 'include',
                    body: JSON.stringify(dto)
                })
                .then(function(res) { return res.json(); })
                .then(function(result) {
                    if (result.success) {
                        toastr.success(result.message);
                        // Refresh stats and list
                        self.loadExternalInvitationStats(project.id);
                    } else {
                        toastr.error(result.message || T('Survey.RetryError', 'Yeniden gönderim sırasında hata oluştu.'));
                    }
                })
                .catch(function(error) {
                    console.error('Error retrying external invitations:', error);
                    toastr.error(T('Survey.RetryError', 'Yeniden gönderim sırasında bir hata oluştu.'));
                })
                .finally(function() {
                    self.isExternalRetrying(false);
                });
            }
        });
    };

    // Send external reminders
    self.sendExternalReminders = function() {
        var project = self.surveyProject();
        if (!project) return;

        var stats = self.externalInvitationStats();
        if (!stats || stats.sent === 0) {
            toastr.info('Hatırlatma gönderilecek davetiye bulunamadı.');
            return;
        }

        // Check if project has email template
        if (!project.emailTemplateId) {
            toastr.error('Email şablonu seçilmemiş. Proje ayarlarından şablon seçin.');
            return;
        }

        var filter = self.externalReminderFilter();
        var filterText = filter === 'all' ? T('Common.All', 'tümüne') :
                         filter === 'completed' ? T('Survey.FilterCompleted', 'tamamlamış olanlara') :
                         T('Survey.FilterNotCompleted', 'tamamlamamış olanlara');

        showConfirmModal({
            title: T('Project.SendReminder', 'Hatırlatma Gönder'),
            message: T('Survey.SendReminderConfirm', 'Seçilen filtreye göre (') + filterText + T('Survey.SendReminderConfirm2', ') hatırlatma göndermek istediğinize emin misiniz?'),
            confirmText: T('Common.Send', 'Gönder'),
            confirmClass: 'btn-primary',
            onConfirm: function() {
                self.isExternalSendingReminder(true);

                var baseUrl = window.location.origin + '/Survey/Form';

                fetch('/api/surveys/' + project.id + '/send-external-reminders', {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    credentials: 'include',
                    body: JSON.stringify({
                        baseUrl: baseUrl,
                        emailTemplateId: null, // Proje şablonunu kullan
                        filter: filter
                    })
                })
                .then(function(res) { return res.json(); })
                .then(function(result) {
                    if (result.success) {
                        toastr.success(result.message);
                        // Refresh stats and list
                        self.loadExternalInvitationStats(project.id);
                    } else {
                        toastr.error(result.message || T('Survey.ReminderError', 'Hatırlatma gönderilirken hata oluştu.'));
                    }
                })
                .catch(function(error) {
                    console.error('Error sending external reminders:', error);
                    toastr.error(T('Survey.ReminderError', 'Hatırlatma gönderilirken bir hata oluştu.'));
                })
                .finally(function() {
                    self.isExternalSendingReminder(false);
                });
            }
        });
    };

    // Upload external emails from CSV/Excel file
    self.uploadExternalEmails = function() {
        var project = self.surveyProject();
        if (!project) return;

        var fileInput = document.getElementById('externalEmailFile');
        if (!fileInput || !fileInput.files || fileInput.files.length === 0) {
            toastr.warning('Lütfen bir dosya seçin (CSV veya Excel).');
            return;
        }

        var file = fileInput.files[0];
        var extension = file.name.split('.').pop().toLowerCase();
        if (extension !== 'csv' && extension !== 'xlsx' && extension !== 'xls') {
            toastr.error('Sadece CSV ve Excel (.xlsx, .xls) dosyaları desteklenir.');
            return;
        }

        // Check if project has email template
        if (!project.emailTemplateId) {
            toastr.error('Email şablonu seçilmemiş. Proje ayarlarından şablon seçin.');
            return;
        }

        self.isExternalUploading(true);
        self.externalUploadResult(null);

        var formData = new FormData();
        formData.append('file', file);
        // emailTemplateId gönderilmiyor - proje şablonu kullanılacak
        formData.append('sendImmediately', 'true');

        fetch('/api/surveys/' + project.id + '/upload-external-emails', {
            method: 'POST',
            credentials: 'include',
            body: formData
        })
        .then(function(res) { return res.json(); })
        .then(function(result) {
            self.externalUploadResult(result);
            if (result.success) {
                toastr.success(result.message);
                fileInput.value = ''; // Clear file input
                // Refresh stats and list
                self.loadExternalInvitationStats(project.id);
            } else {
                toastr.error(result.message || 'Dosya yükleme sırasında hata oluştu.');
            }
        })
        .catch(function(error) {
            console.error('Error uploading external emails:', error);
            toastr.error('Dosya yüklenirken bir hata oluştu.');
            self.externalUploadResult({ success: false, message: 'Beklenmeyen bir hata oluştu.' });
        })
        .finally(function() {
            self.isExternalUploading(false);
        });
    };

    // Download external email template
    self.downloadExternalEmailTemplate = function(format) {
        format = format || 'csv';
        window.location.href = '/api/surveys/external-email-template?format=' + format;
    };

    // Get external invitation status text
    self.getExternalStatusText = function(status) {
        var map = {
            'Pending': 'Bekliyor',
            'Sent': 'Gönderildi',
            'Failed': 'Başarısız'
        };
        return map[status] || status;
    };

    // Get external invitation status badge
    self.getExternalStatusBadge = function(status) {
        var map = {
            'Pending': 'bg-warning text-dark',
            'Sent': 'bg-success',
            'Failed': 'bg-danger'
        };
        return map[status] || 'bg-secondary';
    };

    // Send survey invitations
    self.sendSurveyInvitations = function() {
        var project = self.surveyProject();
        if (!project) return;

        // Email şablonu kontrolü
        if (!project.emailTemplateId) {
            toastr.error('Proje için email şablonu seçilmemiş. Proje ayarlarından şablon seçin.');
            return;
        }

        var stats = self.surveyPersonnelStats();
        if (!stats || stats.withEmail === 0) {
            toastr.warning(T('Survey.NoPersonnelWithEmail', 'Email adresi olan personel bulunamadı.'));
            return;
        }

        // Confirmation
        showConfirmModal({
            title: T('Project.SendInvitations', 'Anket Davetiyesi Gönder'),
            message: T('Survey.SendInvitationsConfirm', 'Toplam ') + stats.withEmail + T('Survey.SendInvitationsConfirm2', ' kişiye anket davetiyesi gönderilecek. Devam etmek istiyor musunuz?'),
            confirmText: T('Common.Send', 'Gönder'),
            confirmClass: 'btn-primary',
            onConfirm: function() {
                self.isSurveySending(true);
                self.surveyResult(null);

                // BaseUrl otomatik oluştur
                var baseUrl = window.location.origin + '/Survey/Form';

                var dto = {
                    baseUrl: baseUrl
                    // emailTemplateId gönderilmiyor - projede tanımlı şablon kullanılacak
                };

                fetch('/api/surveys/' + project.id + '/send-invitations', {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    credentials: 'include',
                    body: JSON.stringify(dto)
                })
                .then(function(res) { return res.json(); })
                .then(function(result) {
                    self.surveyResult(result);
                    if (result.success) {
                        toastr.success(result.message);
                        // Refresh invitation stats
                        self.loadInvitationStats(project.id);
                    } else {
                        toastr.error(result.message || T('Survey.SendError', 'Gönderim sırasında hata oluştu.'));
                    }
                })
                .catch(function(error) {
                    console.error('Error sending invitations:', error);
                    toastr.error(T('Survey.SendError', 'Davetiye gönderilirken bir hata oluştu.'));
                    self.surveyResult({ success: false, message: T('Common.UnexpectedError', 'Beklenmeyen bir hata oluştu.') });
                })
                .finally(function() {
                    self.isSurveySending(false);
                });
            }
        });
    };

    // Initialize
    self.init = function() {
        // Load current user role
        self.loadCurrentUserRole();

        // Once EnumsService'i yukle, sonra diger verileri cek
        EnumsService.load().then(function() {
            self.loadDropdownData();

            // Sabitlenmiş proje tipi varsa filtreyi baştan ekle
            if (self.pageProjectType) {
                self.activeFilters.push({
                    id: Date.now(),
                    type: 'projectType',
                    value: self.pageProjectType,
                    displayLabel: 'Proje Tipi',
                    displayValue: self.projectTypeTexts[self.pageProjectType] || self.pageProjectType,
                    isFixed: true // Sabit filtre - kaldırılamaz
                });
            }

            self.loadProjects();
        });
    };

    self.init();
}

// Apply bindings
$(document).ready(function() {
    ko.applyBindings(new ProjectsViewModel(), document.getElementById('projects-app'));
});
