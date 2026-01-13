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

    // Targets
    self.targetCount = ko.observable(data.targetCount || null);
    self.dailyQuota = ko.observable(data.dailyQuota || null);
    self.weeklyQuota = ko.observable(data.weeklyQuota || null);
    self.monthlyQuota = ko.observable(data.monthlyQuota || null);

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
            targetCount: self.targetCount() || null,
            dailyQuota: self.dailyQuota() || null,
            weeklyQuota: self.weeklyQuota() || null,
            monthlyQuota: self.monthlyQuota() || null,
            estimatedBudget: self.estimatedBudget() || null,
            costPerEvaluation: self.costPerEvaluation() || null,
            reportingFrequencyDays: self.reportingFrequencyDays() || null,
            autoGenerateReports: self.autoGenerateReports(),
            minimumScoreThreshold: self.minimumScoreThreshold() || null,
            priority: self.priority() || null,
            tags: self.tags() || null,
            notes: self.notes() || null,
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

    // View mode
    self.viewMode = ko.observable('table');

    // Sorting
    self.sorting = TableSorting.createSortState('createdAt', 'desc');

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
        dateRange: 'Tarih'
    };

    self.statusLabels = {
        'Draft': 'Taslak',
        'Planned': 'Planlandı',
        'Active': 'Aktif',
        'Paused': 'Duraklatıldı',
        'Completed': 'Tamamlandı',
        'Cancelled': 'İptal Edildi'
    };

    // Date range options
    self.dateRanges = [
        { systemName: 'today', name: 'Bugün' },
        { systemName: 'yesterday', name: 'Dün' },
        { systemName: 'thisWeek', name: 'Bu Hafta' },
        { systemName: 'lastWeek', name: 'Geçen Hafta' },
        { systemName: 'thisMonth', name: 'Bu Ay' },
        { systemName: 'lastMonth', name: 'Geçen Ay' },
        { systemName: 'last7Days', name: 'Son 7 Gün' },
        { systemName: 'last30Days', name: 'Son 30 Gün' },
        { systemName: 'thisQuarter', name: 'Bu Çeyrek' },
        { systemName: 'thisYear', name: 'Bu Yıl' }
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
            case 'dateRange': return self.tempFilter.startDate() || self.tempFilter.endDate();
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
            type: type,
            label: self.filterLabels[type],
            value: null,
            displayValue: ''
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

            case 'dateRange':
                var startDate = self.tempFilter.startDate();
                var endDate = self.tempFilter.endDate();
                var dateRangeType = self.tempFilter.selectedDateRangeType();
                if (!startDate && !endDate) return;

                filter.value = { startDate: startDate, endDate: endDate, dateRangeType: dateRangeType };

                if (dateRangeType) {
                    var rangeInfo = self.dateRanges.find(function(r) { return r.systemName === dateRangeType; });
                    filter.displayValue = rangeInfo ? rangeInfo.name : dateRangeType;
                } else {
                    filter.displayValue = (startDate || '...') + ' - ' + (endDate || '...');
                }

                self.tempFilter.startDate('');
                self.tempFilter.endDate('');
                self.tempFilter.selectedDateRangeType(null);
                break;

            default:
                return;
        }

        // Remove existing filter of same type (except search which can have multiple)
        if (type !== 'search') {
            self.activeFilters.remove(function(f) { return f.type === type; });
        }

        self.activeFilters.push(filter);
        self.selectedFilterType('');
    };

    // Remove filter
    self.removeFilter = function(filter) {
        self.activeFilters.remove(filter);
    };

    // Clear all filters
    self.clearFilters = function() {
        self.activeFilters([]);
        self.sorting.reset();
    };

    // Modals
    self.isEditModalOpen = ko.observable(false);
    self.isDetailModalOpen = ko.observable(false);
    self.editingProject = ko.observable(null);
    self.viewingProject = ko.observable(null);

    // File Management
    self.projectFiles = ko.observableArray([]);
    self.isUploadingFile = ko.observable(false);
    self.currentUserRole = ko.observable('');

    // Check if user can manage files (Admin or TeamLeader)
    self.canManageFiles = ko.computed(function() {
        var role = self.currentUserRole();
        return role === 'Admin' || role === 'TeamLeader';
    });

    // Project Type mappings (EnumsService'de yok, burada kalabilir)
    self.projectTypeTexts = {
        'MysteryShopping': 'Gizli Müşteri', 'CallAuditing': 'Çağrı Denetimi',
        'PhysicalAudit': 'Fiziksel Denetim', 'OnlineSurvey': 'Online Anket',
        'CustomerSatisfaction': 'Müşteri Memnuniyeti', 'TrainingEvaluation': 'Eğitim Değerlendirme',
        'QualityControl': 'Kalite Kontrol'
    };
    self.projectTypeBadges = {
        'MysteryShopping': 'bg-primary', 'CallAuditing': 'bg-info',
        'PhysicalAudit': 'bg-secondary', 'OnlineSurvey': 'bg-success',
        'CustomerSatisfaction': 'bg-warning text-dark', 'TrainingEvaluation': 'bg-dark',
        'QualityControl': 'bg-danger'
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

    // Filtered projects (based on active filters)
    self.filteredProjects = ko.computed(function() {
        var result = self.projects();

        // Apply active filters
        self.activeFilters().forEach(function(filter) {
            switch (filter.type) {
                case 'customer':
                    result = result.filter(function(p) { return p.customerId === filter.value; });
                    break;
                case 'projectType':
                    result = result.filter(function(p) { return p.projectType === filter.value; });
                    break;
                case 'status':
                    result = result.filter(function(p) { return p.status === filter.value; });
                    break;
                case 'projectManager':
                    result = result.filter(function(p) { return p.projectManagerId === filter.value; });
                    break;
                case 'search':
                    var term = filter.value.toLowerCase();
                    result = result.filter(function(p) {
                        return (p.name && p.name.toLowerCase().indexOf(term) >= 0) ||
                               (p.code && p.code.toLowerCase().indexOf(term) >= 0) ||
                               (p.description && p.description.toLowerCase().indexOf(term) >= 0) ||
                               (p.customerName && p.customerName.toLowerCase().indexOf(term) >= 0) ||
                               (p.customerCode && p.customerCode.toLowerCase().indexOf(term) >= 0);
                    });
                    break;
                case 'dateRange':
                    var startDate = filter.value.startDate ? new Date(filter.value.startDate) : null;
                    var endDate = filter.value.endDate ? new Date(filter.value.endDate) : null;
                    if (endDate) endDate.setHours(23, 59, 59, 999);
                    result = result.filter(function(p) {
                        var pStart = new Date(p.startDate);
                        var pEnd = new Date(p.endDate);
                        // Proje tarihleri filtre tarih aralığıyla kesişiyorsa göster
                        if (startDate && pEnd < startDate) return false;
                        if (endDate && pStart > endDate) return false;
                        return true;
                    });
                    break;
            }
        });

        // Apply sorting
        var sortBy = self.sorting.sortBy();
        var sortDir = self.sorting.sortDirection();
        if (sortBy) {
            result = TableSorting.clientSort(result, sortBy, sortDir);
        }
        return result;
    });

    // Load dropdown data
    self.loadDropdownData = function() {
        // Load checklists
        fetch('/api/checklists', { credentials: 'include' })
            .then(function(res) { return res.json(); })
            .then(function(data) { self.checklists(data || []); })
            .catch(function() { console.error('Checklists could not be loaded'); });

        // Load customers
        fetch('/api/customers', { credentials: 'include' })
            .then(function(res) { return res.json(); })
            .then(function(data) { self.customers(data || []); })
            .catch(function() { console.error('Customers could not be loaded'); });

        // Load users
        fetch('/api/users', { credentials: 'include' })
            .then(function(res) { return res.json(); })
            .then(function(data) { self.users(data || []); })
            .catch(function() { console.error('Users could not be loaded'); });
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

    // Load projects
    self.loadProjects = function() {
        self.isLoading(true);
        self.errorMessage('');

        fetch('/api/projects?includeInactive=true', { credentials: 'include' })
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
        var editVm = new ProjectEditViewModel();
        self.editingProject(editVm);
        self.availableOrganizations([]);

        // Subscribe to customerId changes to load organizations
        editVm.customerId.subscribe(function(customerId) {
            self.loadOrganizations(customerId);
            editVm.organizationId(null); // Reset organization when customer changes
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

    // Initialize
    self.init = function() {
        // Load current user role
        self.loadCurrentUserRole();

        // Once EnumsService'i yukle, sonra diger verileri cek
        EnumsService.load().then(function() {
            self.loadDropdownData();
            self.loadProjects();
        });
    };

    self.init();
}

// Apply bindings
$(document).ready(function() {
    ko.applyBindings(new ProjectsViewModel(), document.getElementById('projects-app'));
});
