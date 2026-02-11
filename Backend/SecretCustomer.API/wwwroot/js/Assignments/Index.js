// ===== ViewModels =====

// Assignment Edit ViewModel
function AssignmentEditViewModel(data) {
    var self = this;
    data = data || {};

    self.id = ko.observable(data.id || null);
    self.projectId = ko.observable(data.projectId || '');
    self.checklistId = ko.observable(data.checklistId || '');
    self.assignedUserId = ko.observable(data.assignedUserId || '');
    self.assignedFieldWorkerId = ko.observable(data.assignedFieldWorkerId || '');
    self.assignedCustomerPersonnelId = ko.observable(data.assignedCustomerPersonnelId || '');
    self.externalEmail = ko.observable(data.externalEmail || '');
    self.externalName = ko.observable(data.externalName || '');
    self.dueDate = ko.observable(data.dueDate ? data.dueDate.split('T')[0] : '');
    self.notes = ko.observable(data.notes || '');

    // CustomerDealer (Şube) seçimleri
    self.customerDealerIds = ko.observableArray(data.customerDealerIds || []);

    // Assignment type (user, fieldworker, external)
    self.assignmentType = ko.observable(
        data.assignedFieldWorkerId ? 'fieldworker' :
        data.externalEmail ? 'external' : 'user'
    );

    self.toDTO = function() {
        var dto = {
            projectId: self.projectId(),
            checklistId: self.checklistId(),
            dueDate: self.dueDate(),
            notes: self.notes() || null
        };

        // Clear all assignee fields first
        dto.assignedUserId = null;
        dto.assignedFieldWorkerId = null;
        dto.assignedCustomerPersonnelId = null;
        dto.externalEmail = null;
        dto.externalName = null;
        dto.customerDealerIds = null;

        // Set only the relevant assignee field
        if (self.assignmentType() === 'user' && self.assignedUserId()) {
            dto.assignedUserId = parseInt(self.assignedUserId());
        } else if (self.assignmentType() === 'fieldworker' && self.assignedFieldWorkerId()) {
            dto.assignedFieldWorkerId = parseInt(self.assignedFieldWorkerId());
            // Şube seçimleri sadece FieldWorker atamasında gönderilir
            var dealerIds = self.customerDealerIds();
            if (dealerIds && dealerIds.length > 0) {
                dto.customerDealerIds = dealerIds.map(function(id) { return parseInt(id); });
            }
        } else if (self.assignmentType() === 'external') {
            dto.externalEmail = self.externalEmail() || null;
            dto.externalName = self.externalName() || null;
        }

        return dto;
    };
}

// Period Form ViewModel
function PeriodFormViewModel(data) {
    var self = this;
    data = data || {};

    self.assignmentId = ko.observable(data.assignmentId || null);
    self.name = ko.observable(data.name || '');
    self.startDate = ko.observable(data.startDate ? data.startDate.split('T')[0] : '');
    self.endDate = ko.observable(data.endDate ? data.endDate.split('T')[0] : '');
    self.targetCount = ko.observable(data.targetCount || 5);
    self.notes = ko.observable(data.notes || '');

    self.reset = function(assignmentId) {
        self.assignmentId(assignmentId || null);
        self.name('');
        self.startDate('');
        self.endDate('');
        self.targetCount(5);
        self.notes('');
    };

    self.toDTO = function() {
        return {
            assignmentId: self.assignmentId(),
            name: self.name(),
            startDate: self.startDate(),
            endDate: self.endDate(),
            targetCount: parseInt(self.targetCount()) || 5,
            notes: self.notes() || null
        };
    };
}

// Reassign ViewModel
function ReassignViewModel() {
    var self = this;

    self.assignmentId = ko.observable(null);
    self.newAssigneeType = ko.observable('user');
    self.newAssignedUserId = ko.observable('');
    self.newAssignedFieldWorkerId = ko.observable('');
    self.newExternalEmail = ko.observable('');
    self.newExternalName = ko.observable('');
    self.newDueDate = ko.observable('');
    self.reason = ko.observable('');

    self.reset = function() {
        self.assignmentId(null);
        self.newAssigneeType('user');
        self.newAssignedUserId('');
        self.newAssignedFieldWorkerId('');
        self.newExternalEmail('');
        self.newExternalName('');
        self.newDueDate('');
        self.reason('');
    };

    self.toDTO = function() {
        var dto = {
            reason: self.reason() || null,
            newDueDate: self.newDueDate() || null
        };

        if (self.newAssigneeType() === 'user' && self.newAssignedUserId()) {
            dto.newAssignedUserId = parseInt(self.newAssignedUserId());
        } else if (self.newAssigneeType() === 'fieldworker' && self.newAssignedFieldWorkerId()) {
            dto.newAssignedFieldWorkerId = parseInt(self.newAssignedFieldWorkerId());
        } else if (self.newAssigneeType() === 'external') {
            dto.newExternalEmail = self.newExternalEmail() || null;
            dto.newExternalName = self.newExternalName() || null;
        }

        return dto;
    };
}

// ===== Main ViewModel =====
function AssignmentsViewModel() {
    var self = this;

    // ===== User Role (from global) =====
    self.isAdmin = ko.observable(window.userRole === 'Admin');
    self.isQualitySpecialist = ko.observable(window.userRole === 'QualitySpecialist');
    self.isFieldWorker = ko.observable(window.userRole === 'FieldWorker');

    // ===== State =====
    self.isLoading = ko.observable(false);
    self.isSaving = ko.observable(false);
    self.errorMessage = ko.observable('');
    self.successMessage = ko.observable('');
    self.modalErrorMessage = ko.observable('');
    self.isEditing = ko.observable(false);

    // ===== Server-Side Pagination =====
    self.currentPage = ko.observable(1);
    self.pageSize = ko.observable(50);
    self.allAssignments = ko.observableArray([]);

    // Server-side pagination - assignments zaten paginated gelecek
    self.assignments = ko.computed(function() {
        return self.allAssignments();
    });

    // Sayfa boyutu değişince ilk sayfaya dön ve yeniden yükle
    self.pageSize.subscribe(function() {
        self.currentPage(1);
        self.loadAssignments();
    });

    self.goToPage = function(page) {
        if (page >= 1 && page <= self.totalPages()) {
            self.currentPage(page);
            self.loadAssignments();
        }
    };

    self.previousPage = function() {
        if (self.currentPage() > 1) {
            self.currentPage(self.currentPage() - 1);
            self.loadAssignments();
        }
    };

    self.nextPage = function() {
        if (self.currentPage() < self.totalPages()) {
            self.currentPage(self.currentPage() + 1);
            self.loadAssignments();
        }
    };

    self.firstPage = function() {
        if (self.currentPage() !== 1) {
            self.currentPage(1);
            self.loadAssignments();
        }
    };

    self.lastPage = function() {
        if (self.currentPage() !== self.totalPages()) {
            self.currentPage(self.totalPages());
            self.loadAssignments();
        }
    };

    // ===== Data =====
    self.availableProjects = ko.observableArray([]);
    self.availableEvaluators = ko.observableArray([]);
    self.availableFieldWorkers = ko.observableArray([]);
    self.availableDealers = ko.observableArray([]);
    self.isDealersLoading = ko.observable(false);
    self.selectedProjectChecklistName = ko.observable('');
    self.selectedProjectType = ko.observable('');
    self.selectedProjectCustomerId = ko.observable(null);

    // Project Picker
    self.projectPickerSearch = ko.observable('');
    self.isProjectPickerOpen = ko.observable(false);
    self.selectedProjectForDisplay = ko.observable(null);

    // Proje tipine göre atama tipi belirleme (Saha Çalışanı gerektiren projeler)
    self.isPhysicalAuditProject = ko.computed(function() {
        var projectType = self.selectedProjectType();
        return projectType === 'PhysicalAudit' || projectType === 'MysteryShopping';
    });

    // Proje seçiliyse kullanıcı listesi, seçili değilse boş
    self.filteredEvaluators = ko.computed(function() {
        if (!self.selectedProjectForDisplay()) return [];
        return self.availableEvaluators();
    });

    self.filteredFieldWorkers = ko.computed(function() {
        if (!self.selectedProjectForDisplay()) return [];
        return self.availableFieldWorkers();
    });

    self.filteredProjectsForPicker = ko.computed(function() {
        var search = (self.projectPickerSearch() || '').toLowerCase().trim();
        var projects = self.availableProjects();

        if (!search) return projects;

        return projects.filter(function(p) {
            return (p.name && p.name.toLowerCase().indexOf(search) > -1) ||
                   (p.code && p.code.toLowerCase().indexOf(search) > -1) ||
                   (p.customerName && p.customerName.toLowerCase().indexOf(search) > -1) ||
                   (p.organizationName && p.organizationName.toLowerCase().indexOf(search) > -1);
        });
    });

    // Summary
    self.summary = ko.observable({
        totalAssignments: 0,
        pendingCount: 0,
        inProgressCount: 0,
        completedCount: 0,
        expiredCount: 0,
        cancelledCount: 0,
        completionRate: 0,
        averageScore: 0
    });

    // ===== Dynamic Filter System =====
    self.selectedFilterType = ko.observable('');
    self.activeFilters = ko.observableArray([]);

    // Temp filter values
    self.tempFilter = {
        projectId: ko.observable(''),
        status: ko.observable(''),
        assignedUserId: ko.observable(''),
        selectedDueDateType: ko.observable(''),
        searchTerm: ko.observable('')
    };

    // Son Tarih filtre seçenekleri (Projects pattern)
    self.dueDateOptions = [
        { systemName: 'overdue', name: 'Süresi Geçmiş' },
        { systemName: 'today', name: 'Bugün Son Tarih' },
        { systemName: 'tomorrow', name: 'Yarın Son Tarih' },
        { systemName: 'next7Days', name: '7 Gün İçinde' },
        { systemName: 'thisWeek', name: 'Bu Hafta' },
        { systemName: 'next30Days', name: '30 Gün İçinde' },
        { systemName: 'thisMonth', name: 'Bu Ay' },
        { systemName: 'nextMonth', name: 'Gelecek Ay' }
    ];

    // Can add filter
    self.canAddFilter = ko.computed(function() {
        var type = self.selectedFilterType();
        if (!type) return false;

        switch (type) {
            case 'project': return !!self.tempFilter.projectId();
            case 'status': return !!self.tempFilter.status();
            case 'assignedUser': return !!self.tempFilter.assignedUserId();
            case 'dueDate': return !!self.tempFilter.selectedDueDateType();
            case 'search': return !!self.tempFilter.searchTerm();
            default: return false;
        }
    });

    // Status display names
    self.getStatusDisplayName = function(status) {
        var statusNames = {
            'Pending': T('Common.Status.Pending', 'Bekleyen'),
            'InProgress': T('Status.InProgress', 'Devam Eden'),
            'Completed': T('Status.Completed', 'Tamamlanan'),
            'Expired': T('Status.Expired', 'Süresi Dolan'),
            'Cancelled': T('Status.Cancelled', 'İptal Edilen')
        };
        return statusNames[status] || status;
    };

    // Add filter
    self.addFilter = function() {
        var type = self.selectedFilterType();
        if (!type) return;

        var filter = { type: type };

        switch (type) {
            case 'project':
                var projectId = self.tempFilter.projectId();
                var project = self.availableProjects().find(function(p) { return p.id == projectId; });
                if (!project) return;
                filter.label = T('Project.Title', 'Proje');
                filter.value = projectId;
                filter.displayValue = project.code ? project.code + ' - ' + project.name : project.name;
                self.tempFilter.projectId('');
                break;
            case 'status':
                var status = self.tempFilter.status();
                if (!status) return;
                filter.label = T('Common.Status', 'Durum');
                filter.value = status;
                filter.displayValue = self.getStatusDisplayName(status);
                self.tempFilter.status('');
                break;
            case 'assignedUser':
                var userId = self.tempFilter.assignedUserId();
                var user = self.allAssignableUsers().find(function(u) { return u.id == userId; });
                if (!user) return;
                filter.label = T('Assignment.AssignedPerson', 'Atanan Kişi');
                filter.value = userId;
                filter.displayValue = user.displayName;
                self.tempFilter.assignedUserId('');
                break;
            case 'dueDate':
                var dueDateType = self.tempFilter.selectedDueDateType();
                if (!dueDateType) return;

                var optionInfo = self.dueDateOptions.find(function(o) { return o.systemName === dueDateType; });
                filter.label = T('Common.DueDate', 'Son Tarih');
                filter.value = dueDateType;
                filter.displayValue = optionInfo ? optionInfo.name : dueDateType;

                self.tempFilter.selectedDueDateType('');
                break;
            case 'search':
                var term = self.tempFilter.searchTerm();
                if (!term) return;
                filter.label = T('Common.Search', 'Arama');
                filter.value = term;
                filter.displayValue = '"' + term + '"';
                self.tempFilter.searchTerm('');
                break;
            default:
                return;
        }

        // Tüm filtre tipleri çoklu değer destekler (aynı tipten birden fazla eklenebilir)
        self.activeFilters.push(filter);
        self.selectedFilterType('');
        self.applyFilters();
    };

    // Remove filter
    self.removeFilter = function(filter) {
        self.activeFilters.remove(filter);
        self.applyFilters();
    };

    // Sorting
    self.sorting = TableSorting.createSortState('createdAt', 'desc');

    // Subscribe to sorting changes
    self.sorting.sortBy.subscribe(function() {
        self.applyFilters();
    });
    self.sorting.sortDirection.subscribe(function() {
        self.applyFilters();
    });

    // All assignable users (for filter)
    self.allAssignableUsers = ko.computed(function() {
        var users = [];
        self.availableEvaluators().forEach(function(u) {
            users.push({ id: u.id, displayName: u.fullName + ' (' + T('Menu.Users', 'Kullanıcı') + ')' });
        });
        self.availableFieldWorkers().forEach(function(fw) {
            users.push({ id: fw.id, displayName: fw.fullName + ' (' + T('Role.FieldWorker', 'Saha Çalışanı') + ')' });
        });
        return users;
    });

    // ===== Modal State =====
    self.isModalOpen = ko.observable(false);
    self.editingAssignment = ko.observable(null);
    self.selectedEvaluation = ko.observable(null);
    self.selectedDetail = ko.observable(null);

    // Reassign
    self.reassignData = ko.observable(new ReassignViewModel());

    // Update Due Date
    self.updateDueDateData = ko.observable({ assignmentId: ko.observable(null), newDueDate: ko.observable('') });
    self.isSavingDueDate = ko.observable(false);

    // Period Form
    self.periodForm = ko.observable(new PeriodFormViewModel());
    self.periodModalError = ko.observable('');
    self.isSavingPeriod = ko.observable(false);

    // Add Dealer Modal State
    self.addDealerModal = {
        isLoading: ko.observable(false),
        isSaving: ko.observable(false),
        availableDealers: ko.observableArray([]),
        assignmentId: ko.observable(null),
        customerId: ko.observable(null)
    };

    // Total count for server-side pagination
    self.totalCount = ko.observable(0);
    self.totalPages = ko.computed(function() {
        return Math.ceil(self.totalCount() / parseInt(self.pageSize(), 10)) || 1;
    });

    // ===== Load Data =====
    self.loadAssignments = function() {
        self.isLoading(true);
        self.errorMessage('');

        var queryString = self.buildFilterQueryString();
        fetch('/api/assignments?' + queryString, { credentials: 'include' })
            .then(function(res) {
                if (!res.ok) throw new Error(T('Message.LoadError', 'Yükleme başarısız'));
                return res.json();
            })
            .then(function(data) {
                // PagedAssignmentResult: { items, totalCount, page, pageSize }
                self.allAssignments(data.items || []);
                self.totalCount(data.totalCount || 0);
            })
            .catch(function(error) {
                console.error('Error:', error);
                toastr.error(T('Assignment.LoadError', 'Atamalar yüklenirken bir hata oluştu.'));
            })
            .finally(function() {
                self.isLoading(false);
            });
    };

    self.loadSummary = function(projectId) {
        var url = '/api/assignments/summary';
        if (projectId) {
            url += '?projectId=' + projectId;
        }

        fetch(url, { credentials: 'include' })
            .then(function(res) { return res.json(); })
            .then(function(data) {
                self.summary(data);
            })
            .catch(function(error) {
                console.error('Error loading summary:', error);
            });
    };

    self.loadProjects = function() {
        fetch('/api/projects', { credentials: 'include' })
            .then(function(res) { return res.json(); })
            .then(function(data) {
                var today = new Date();
                today.setHours(0, 0, 0, 0);
                self.availableProjects(data.filter(function(p) {
                    // Aktif olmayan projeleri filtrele
                    if (!p.isActive) return false;
                    // Bitiş tarihi geçmiş projeleri filtrele
                    if (p.endDate) {
                        var endDate = new Date(p.endDate);
                        if (endDate < today) return false;
                    }
                    return true;
                }));
            })
            .catch(function(error) { console.error('Error loading projects:', error); });
    };

    self.loadEvaluators = function() {
        // Admin (role 1) ve QualitySpecialist (role 2) kullanıcılarını çek
        // FieldWorker'lar ayrı yükleniyor (loadFieldWorkers)
        Promise.all([
            fetch('/api/users/role/1', { credentials: 'include' }).then(function(r) { return r.json(); }),
            fetch('/api/users/role/2', { credentials: 'include' }).then(function(r) { return r.json(); })
        ])
        .then(function(results) {
            var admins = results[0] || [];
            var qualitySpecialists = results[1] || [];
            // Birleştir ve tekrarları kaldır (id'ye göre)
            var combined = admins.concat(qualitySpecialists);
            var unique = [];
            var ids = {};
            combined.forEach(function(u) {
                if (!ids[u.id]) {
                    ids[u.id] = true;
                    unique.push(u);
                }
            });
            self.availableEvaluators(unique);
        })
        .catch(function(error) { console.error('Error loading evaluators:', error); });
    };

    // FieldWorker rolündeki kullanıcıları yükle
    self.loadFieldWorkers = function() {
        fetch('/api/users/role/3', { credentials: 'include' })
            .then(function(r) { return r.json(); })
            .then(function(data) {
                self.availableFieldWorkers(data || []);
            })
            .catch(function(error) {
                console.error('Error loading field workers:', error);
                self.availableFieldWorkers([]);
            });
    };

    // ===== Filter Methods =====
    // Build query string from active filters
    self.buildFilterQueryString = function() {
        var params = new URLSearchParams();

        // Pagination
        params.append('page', self.currentPage());
        params.append('pageSize', self.pageSize());

        // Sorting
        if (self.sorting.sortBy()) params.append('sortBy', self.sorting.sortBy());
        params.append('sortDirection', self.sorting.sortDirection() || 'desc');

        // Collect filter values
        var projectIds = [];
        var statuses = [];
        var assignedUserIds = [];
        var searchTerms = [];
        var dueDateFilter = '';

        self.activeFilters().forEach(function(f) {
            switch (f.type) {
                case 'project':
                    projectIds.push(f.value);
                    break;
                case 'status':
                    statuses.push(f.value);
                    break;
                case 'assignedUser':
                    assignedUserIds.push(f.value);
                    break;
                case 'dueDate':
                    dueDateFilter = f.value;
                    break;
                case 'search':
                    searchTerms.push(f.value);
                    break;
            }
        });

        // Add arrays to query string (çoğul parametreler)
        projectIds.forEach(function(id) { params.append('projectIds', id); });
        statuses.forEach(function(s) { params.append('statuses', s); });
        assignedUserIds.forEach(function(id) { params.append('assignedUserIds', id); });

        if (dueDateFilter) params.append('dueDateFilter', dueDateFilter);
        if (searchTerms.length > 0) params.append('searchTerm', searchTerms.join(' '));

        return params.toString();
    };

    self.applyFilters = function() {
        self.currentPage(1);
        self.loadAssignments();
    };

    self.clearFilters = function() {
        self.activeFilters([]);
        self.selectedFilterType('');
        self.sorting.reset();
        self.loadAssignments();
        self.loadSummary();
    };

    // ===== CRUD Methods =====
    self.createNew = function() {
        self.isEditing(false);
        self.editingAssignment(new AssignmentEditViewModel());
        self.selectedProjectChecklistName('');
        self.selectedProjectForDisplay(null);
        self.selectedProjectType('');
        self.selectedProjectCustomerId(null);
        self.availableDealers([]);
        self.projectPickerSearch('');
        self.isProjectPickerOpen(false);
        self.isModalOpen(true);
    };

    // Project Picker Methods
    self.toggleProjectPicker = function(data, event) {
        if (event) event.stopPropagation();
        self.isProjectPickerOpen(!self.isProjectPickerOpen());
        if (self.isProjectPickerOpen()) {
            self.projectPickerSearch('');
            // Dışarı tıklayınca kapat
            setTimeout(function() {
                $(document).one('click', function(e) {
                    if (!$(e.target).closest('.project-picker-dropdown').length) {
                        self.isProjectPickerOpen(false);
                    }
                });
            }, 100);
        }
    };

    self.selectProject = function(project) {
        var assignment = self.editingAssignment();
        if (!assignment) return;

        assignment.projectId(project.id);
        assignment.checklistId(project.checklistId);
        self.selectedProjectChecklistName(project.checklistName || '');
        self.selectedProjectForDisplay(project);
        self.selectedProjectType(project.projectType || '');
        self.selectedProjectCustomerId(project.customerId || null);
        self.isProjectPickerOpen(false);
        self.projectPickerSearch('');

        // Proje tipine göre atama türünü otomatik seç
        if (project.projectType === 'PhysicalAudit' || project.projectType === 'MysteryShopping') {
            assignment.assignmentType('fieldworker');
            // Şubeleri yükle
            if (project.customerId) {
                self.loadDealersByCustomer(project.customerId);
            }
        } else {
            assignment.assignmentType('user');
        }

        // Şube seçimlerini temizle
        assignment.customerDealerIds([]);
    };

    // Müşteriye göre şubeleri yükle
    self.loadDealersByCustomer = function(customerId) {
        if (!customerId) {
            self.availableDealers([]);
            return;
        }

        self.isDealersLoading(true);
        fetch('/api/dealers/customer/' + customerId, { credentials: 'include' })
            .then(function(res) { return res.json(); })
            .then(function(data) {
                self.availableDealers(data || []);
            })
            .catch(function(error) {
                console.error('Error loading dealers:', error);
                self.availableDealers([]);
            })
            .finally(function() {
                self.isDealersLoading(false);
            });
    };

    self.clearSelectedProject = function() {
        var assignment = self.editingAssignment();
        if (!assignment) return;

        assignment.projectId('');
        assignment.checklistId('');
        assignment.assignedUserId('');
        assignment.assignedFieldWorkerId('');
        assignment.customerDealerIds([]);
        self.selectedProjectChecklistName('');
        self.selectedProjectForDisplay(null);
        self.selectedProjectType('');
        self.selectedProjectCustomerId(null);
        self.availableDealers([]);
    };

    self.onProjectChange = function() {
        var assignment = self.editingAssignment();
        if (!assignment) return;

        var projectId = assignment.projectId();
        if (!projectId) {
            assignment.checklistId('');
            self.selectedProjectChecklistName('');
            self.selectedProjectForDisplay(null);
            self.selectedProjectType('');
            return;
        }

        // Find selected project and auto-fill checklist
        var selectedProject = self.availableProjects().find(function(p) {
            return p.id == projectId;
        });

        if (selectedProject) {
            assignment.checklistId(selectedProject.checklistId);
            self.selectedProjectChecklistName(selectedProject.checklistName || '');
            self.selectedProjectForDisplay(selectedProject);
            self.selectedProjectType(selectedProject.projectType || '');

            // Proje tipine göre atama türünü otomatik seç
            if (selectedProject.projectType === 'PhysicalAudit' || selectedProject.projectType === 'MysteryShopping') {
                assignment.assignmentType('fieldworker');
            } else {
                assignment.assignmentType('user');
            }
        }
    };

    self.saveAssignment = function(forceActivate) {
        var assignment = self.editingAssignment();

        // Validation
        if (!assignment.projectId()) {
            toastr.error(T('Assignment.SelectProject', 'Proje seçmelisiniz!'));
            return;
        }

        if (!assignment.checklistId()) {
            toastr.error(T('Assignment.SelectChecklist', 'Kontrol listesi seçmelisiniz!'));
            return;
        }

        if (!assignment.dueDate()) {
            toastr.error(T('Assignment.DueDateRequired', 'Son tarih zorunludur!'));
            return;
        }

        var dto = assignment.toDTO();
        var isEdit = self.isEditing();
        var assignmentId = assignment.id();
        var url = isEdit ? '/api/assignments/' + assignmentId : '/api/assignments';
        var method = isEdit ? 'PUT' : 'POST';

        // forceActivate parametresi ile çağrıldıysa DTO'ya ekle (true boolean olmalı, KO click event objesi değil)
        if (forceActivate === true) {
            dto.forceActivateProject = true;
        }

        self.isSaving(true);
        fetch(url, {
            method: method,
            headers: { 'Content-Type': 'application/json' },
            credentials: 'include',
            body: JSON.stringify(dto)
        })
            .then(function(response) {
                if (!response.ok) {
                    return response.json().then(function(errorData) {
                        console.log('API Error Response:', errorData);
                        // PROJECT_NOT_ACTIVE hatası ise özel işlem yap
                        if (errorData.errorCode === 'PROJECT_NOT_ACTIVE') {
                            throw { isProjectNotActive: true, message: errorData.message };
                        }
                        throw new Error(errorData.message || T('Message.SaveError', 'Kayıt başarısız'));
                    });
                }
                return response.json();
            })
            .then(function(savedAssignment) {
                if (isEdit) {
                    // Guncelleme: array'de bul ve guncelle
                    var list = self.allAssignments();
                    for (var i = 0; i < list.length; i++) {
                        if (list[i].id === savedAssignment.id) {
                            self.allAssignments.splice(i, 1, savedAssignment);
                            break;
                        }
                    }
                } else {
                    // Yeni kayit: array'e ekle (son eklenen en üstte)
                    self.allAssignments.unshift(savedAssignment);
                }
                toastr.success(isEdit ? T('Assignment.UpdateSuccess', 'Atama başarıyla güncellendi.') : T('Assignment.SaveSuccess', 'Atama başarıyla oluşturuldu.'));
                self.closeModal();
                self.loadSummary();
            })
            .catch(function(error) {
                console.error('Error:', error);
                // Proje aktif değil hatası - onay modal göster
                if (error.isProjectNotActive) {
                    self.isSaving(false);
                    showConfirmModal({
                        title: T('Assignment.ActivateProjectTitle', 'Proje Aktif Değil'),
                        message: T('Assignment.ActivateProjectMessage', 'Bu proje henüz aktif değil. Projeyi aktif edip atamayı oluşturmak ister misiniz?'),
                        type: 'warning',
                        confirmText: T('Assignment.ActivateAndSave', 'Aktif Et ve Kaydet'),
                        confirmIcon: 'bi-play-fill',
                        onConfirm: function() {
                            // Onay verildi - forceActivate ile tekrar dene
                            self.saveAssignment(true);
                        }
                    });
                    return;
                }
                toastr.error(error.message || T('Assignment.SaveError', 'Atama kaydedilirken bir hata oluştu.'));
            })
            .finally(function() {
                self.isSaving(false);
            });
    };

    self.deleteAssignment = function(assignment) {
        showDeleteConfirm(T('Assignment.ThisAssignment', 'Bu atama'), function() {
            var assignmentId = assignment.id;
            fetch('/api/assignments/' + assignmentId, {
                method: 'DELETE',
                credentials: 'include'
            })
                .then(function(response) {
                    if (!response.ok) throw new Error(T('Message.DeleteError', 'Silme başarısız'));
                    // ID ile eşleştirerek sil (referans sorunu önlenir)
                    self.allAssignments.remove(function(a) { return a.id === assignmentId; });
                    self.loadSummary();
                    toastr.success(T('Assignment.DeleteSuccess', 'Atama başarıyla silindi.'));
                })
                .catch(function(error) {
                    console.error('Error:', error);
                    toastr.error(T('Assignment.DeleteError', 'Atama silinirken bir hata oluştu.'));
                });
        });
    };

    self.closeModal = function() {
        self.isModalOpen(false);
        self.editingAssignment(null);
        self.isEditing(false);
    };

    // ===== Reassign =====
    self.openReassignModal = function(assignment) {
        self.reassignData().reset();
        self.reassignData().assignmentId(assignment.id);
        var modal = new bootstrap.Modal(document.getElementById('reassignModal'));
        modal.show();
    };

    self.saveReassign = function() {
        var data = self.reassignData();
        var assignmentId = data.assignmentId();

        if (!assignmentId) {
            toastr.error(T('Assignment.NotFound', 'Atama bulunamadı.'));
            return;
        }

        var dto = data.toDTO();

        self.isSaving(true);

        fetch('/api/assignments/' + assignmentId + '/reassign', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            credentials: 'include',
            body: JSON.stringify(dto)
        })
            .then(function(res) {
                if (!res.ok) throw new Error(T('Assignment.ReassignError', 'Yeniden atama başarısız'));
                return res.json();
            })
            .then(function(updatedAssignment) {
                // Array'de bul ve guncelle
                var list = self.allAssignments();
                for (var i = 0; i < list.length; i++) {
                    if (list[i].id === updatedAssignment.id) {
                        self.allAssignments.splice(i, 1, updatedAssignment);
                        break;
                    }
                }
                toastr.success(T('Assignment.ReassignSuccess', 'Atama başarıyla yeniden atandı.'));
                bootstrap.Modal.getInstance(document.getElementById('reassignModal')).hide();
            })
            .catch(function(error) {
                console.error('Error:', error);
                toastr.error(T('Assignment.ReassignProcessError', 'Yeniden atama yapılırken bir hata oluştu.'));
            })
            .finally(function() {
                self.isSaving(false);
            });
    };

    // ===== Update Due Date =====
    self.openUpdateDueDateModal = function(assignment) {
        self.updateDueDateData().assignmentId(assignment.id);
        self.updateDueDateData().newDueDate(assignment.dueDate ? assignment.dueDate.split('T')[0] : '');
        var modal = new bootstrap.Modal(document.getElementById('updateDueDateModal'));
        modal.show();
    };

    self.saveUpdateDueDate = function() {
        var data = self.updateDueDateData();
        var assignmentId = data.assignmentId();
        var newDueDate = data.newDueDate();

        if (!assignmentId) {
            toastr.error(T('Assignment.NotFound', 'Atama bulunamadı.'));
            return;
        }

        if (!newDueDate) {
            toastr.error(T('Assignment.DueDateRequired', 'Son tarih zorunludur!'));
            return;
        }

        self.isSavingDueDate(true);

        fetch('/api/assignments/' + assignmentId + '/update-due-date', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            credentials: 'include',
            body: JSON.stringify({ newDueDate: newDueDate })
        })
            .then(function(res) {
                if (!res.ok) throw new Error(T('Assignment.UpdateDueDateError', 'Tarih güncelleme başarısız'));
                return res.json();
            })
            .then(function(updatedAssignment) {
                var list = self.allAssignments();
                for (var i = 0; i < list.length; i++) {
                    if (list[i].id === updatedAssignment.id) {
                        self.allAssignments.splice(i, 1, updatedAssignment);
                        break;
                    }
                }
                toastr.success(T('Assignment.UpdateDueDateSuccess', 'Tarih başarıyla güncellendi.'));
                bootstrap.Modal.getInstance(document.getElementById('updateDueDateModal')).hide();
            })
            .catch(function(error) {
                console.error('Error:', error);
                toastr.error(T('Assignment.UpdateDueDateProcessError', 'Tarih güncellenirken bir hata oluştu.'));
            })
            .finally(function() {
                self.isSavingDueDate(false);
            });
    };

    // ===== Cancel Assignment =====
    self.cancelAssignment = function(assignment) {
        showConfirmModal({
            title: T('Assignment.CancelTitle', 'Atama İptali'),
            message: T('Assignment.CancelConfirm', 'Bu atamayı iptal etmek istediğinizden emin misiniz?'),
            type: 'danger',
            confirmText: T('Button.Cancel', 'İptal Et'),
            confirmIcon: 'bi-x-circle',
            onConfirm: function() {
                fetch('/api/assignments/' + assignment.id + '/cancel', {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    credentials: 'include',
                    body: JSON.stringify({ reason: null })
                })
                    .then(function(res) {
                        if (!res.ok) throw new Error(T('Assignment.CancelError', 'İptal başarısız'));
                        return res.json();
                    })
                    .then(function(updatedAssignment) {
                        // Array'de bul ve guncelle
                        var list = self.allAssignments();
                        for (var i = 0; i < list.length; i++) {
                            if (list[i].id === updatedAssignment.id) {
                                self.allAssignments.splice(i, 1, updatedAssignment);
                                break;
                            }
                        }
                        toastr.success(T('Assignment.CancelSuccess', 'Atama başarıyla iptal edildi.'));
                        self.loadSummary();
                    })
                    .catch(function(error) {
                        console.error('Error:', error);
                        toastr.error(T('Assignment.CancelProcessError', 'Atama iptal edilirken bir hata oluştu.'));
                    });
            }
        });
    };

    // ===== Reopen Assignment =====
    self.reopenAssignment = function(assignment) {
        showConfirmModal({
            title: T('Assignment.ReopenTitle', 'Atamayı Yeniden Aç'),
            message: T('Assignment.ReopenConfirm', 'Bu tamamlanmış atamayı yeniden açmak istediğinizden emin misiniz? Değerlendirme tekrar düzenlenebilir hale gelecektir.'),
            type: 'warning',
            confirmText: T('Button.Reopen', 'Yeniden Aç'),
            confirmIcon: 'bi-arrow-counterclockwise',
            onConfirm: function() {
                fetch('/api/assignments/' + assignment.id + '/reopen', {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    credentials: 'include'
                })
                    .then(function(res) {
                        if (!res.ok) throw new Error(T('Assignment.ReopenError', 'Yeniden açma başarısız'));
                        return res.json();
                    })
                    .then(function(updatedAssignment) {
                        // Array'de bul ve guncelle
                        var list = self.allAssignments();
                        for (var i = 0; i < list.length; i++) {
                            if (list[i].id === updatedAssignment.id) {
                                self.allAssignments.splice(i, 1, updatedAssignment);
                                break;
                            }
                        }
                        toastr.success(T('Assignment.ReopenSuccess', 'Atama başarıyla yeniden açıldı.'));
                        self.loadSummary();
                    })
                    .catch(function(error) {
                        console.error('Error:', error);
                        toastr.error(T('Assignment.ReopenProcessError', 'Atama yeniden açılırken bir hata oluştu.'));
                    });
            }
        });
    };

    // ===== Period Management =====
    self.openAddPeriodModal = function() {
        var detail = self.selectedDetail();
        if (!detail || !detail.id) {
            toastr.warning(T('Assignment.SelectFirst', 'Önce bir atama seçmelisiniz!'));
            return;
        }

        // Reset form with assignment ID
        self.periodForm().reset(detail.id);
        self.periodModalError('');

        // Auto-generate period name (current month)
        var now = new Date();
        var monthNames = ['Ocak', 'Şubat', 'Mart', 'Nisan', 'Mayıs', 'Haziran',
                         'Temmuz', 'Ağustos', 'Eylül', 'Ekim', 'Kasım', 'Aralık'];
        self.periodForm().name(monthNames[now.getMonth()] + ' ' + now.getFullYear());

        // Auto-set start/end dates (current month)
        var startOfMonth = new Date(now.getFullYear(), now.getMonth(), 1);
        var endOfMonth = new Date(now.getFullYear(), now.getMonth() + 1, 0);
        self.periodForm().startDate(startOfMonth.toISOString().split('T')[0]);
        self.periodForm().endDate(endOfMonth.toISOString().split('T')[0]);

        var modal = new bootstrap.Modal(document.getElementById('addPeriodModal'));
        modal.show();
    };

    self.savePeriod = function() {
        var form = self.periodForm();

        // Validation
        if (!form.name()) {
            self.periodModalError(T('Period.NameRequired', 'Dönem adı zorunludur!'));
            return;
        }

        if (!form.startDate()) {
            self.periodModalError(T('Period.StartDateRequired', 'Başlangıç tarihi zorunludur!'));
            return;
        }

        if (!form.endDate()) {
            self.periodModalError(T('Period.EndDateRequired', 'Bitiş tarihi zorunludur!'));
            return;
        }

        if (new Date(form.startDate()) >= new Date(form.endDate())) {
            self.periodModalError(T('Period.InvalidDateRange', 'Bitiş tarihi başlangıç tarihinden sonra olmalıdır!'));
            return;
        }

        if (!form.targetCount() || form.targetCount() < 1) {
            self.periodModalError(T('Period.TargetRequired', 'Hedef değerlendirme sayısı en az 1 olmalıdır!'));
            return;
        }

        var dto = form.toDTO();
        var assignmentId = form.assignmentId();

        self.isSavingPeriod(true);
        self.periodModalError('');

        fetch('/api/assignments/' + assignmentId + '/periods', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            credentials: 'include',
            body: JSON.stringify(dto)
        })
            .then(function(res) {
                if (!res.ok) {
                    return res.json().then(function(err) {
                        throw new Error(err.message || T('Period.CreateError', 'Dönem oluşturulamadı'));
                    });
                }
                return res.json();
            })
            .then(function(data) {
                toastr.success(T('Period.CreateSuccess', 'Dönem başarıyla oluşturuldu.'));
                bootstrap.Modal.getInstance(document.getElementById('addPeriodModal')).hide();

                // Refresh detail modal to show new period
                self.showDetail({ id: assignmentId });
            })
            .catch(function(error) {
                console.error('Error:', error);
                self.periodModalError(error.message || T('Period.CreateError', 'Dönem oluşturulurken bir hata oluştu.'));
            })
            .finally(function() {
                self.isSavingPeriod(false);
            });
    };

    // ===== View Detail =====
    self.showDetail = function(assignment) {
        fetch('/api/assignments/' + assignment.id + '/detail', { credentials: 'include' })
            .then(function(res) {
                if (!res.ok) throw new Error('Detail API error: ' + res.status);
                return res.json();
            })
            .then(function(data) {
                self.selectedDetail(data);
                var modal = new bootstrap.Modal(document.getElementById('detailModal'));
                modal.show();
            })
            .catch(function(error) {
                console.error('Error:', error);
                toastr.error(T('Assignment.DetailLoadError', 'Detay yüklenirken bir hata oluştu.'));
            });
    };

    // ===== Download Project File =====
    self.downloadProjectFile = function(file) {
        window.location.href = '/api/project-files/' + file.id + '/download';
    };

    // ===== Dealer Management =====
    self.openAddDealerModal = function() {
        var detail = self.selectedDetail();
        if (!detail || !detail.customerId) {
            toastr.warning(T('Assignment.NoCustomer', 'Bu atamaya ait müşteri bulunamadı.'));
            return;
        }

        self.addDealerModal.assignmentId(detail.id);
        self.addDealerModal.customerId(detail.customerId);
        self.addDealerModal.isLoading(true);
        self.addDealerModal.availableDealers([]);

        // Get all dealers for this customer
        fetch('/api/dealers/customer/' + detail.customerId, { credentials: 'include' })
            .then(function(res) { return res.json(); })
            .then(function(allDealers) {
                // Filter out already assigned dealers
                var assignedDealerIds = (detail.dealers || []).map(function(d) { return d.customerDealerId; });
                var availableDealers = allDealers.filter(function(d) {
                    return assignedDealerIds.indexOf(d.id) === -1;
                });
                self.addDealerModal.availableDealers(availableDealers);
            })
            .catch(function(error) {
                console.error('Error loading dealers:', error);
                toastr.error(T('Dealer.LoadError', 'Şubeler yüklenirken hata oluştu.'));
            })
            .finally(function() {
                self.addDealerModal.isLoading(false);
            });

        var modal = new bootstrap.Modal(document.getElementById('addDealerModal'));
        modal.show();
    };

    self.addDealerToAssignment = function(customerDealerId) {
        var assignmentId = self.addDealerModal.assignmentId();
        if (!assignmentId || !customerDealerId) return;

        self.addDealerModal.isSaving(true);

        fetch('/api/assignments/' + assignmentId + '/dealers', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            credentials: 'include',
            body: JSON.stringify({ customerDealerId: customerDealerId })
        })
            .then(function(res) {
                if (!res.ok) {
                    return res.json().then(function(err) {
                        throw new Error(err.message || T('Dealer.AddError', 'Şube eklenemedi'));
                    });
                }
                return res.json();
            })
            .then(function(addedDealer) {
                toastr.success(T('Dealer.AddSuccess', 'Şube başarıyla eklendi.'));

                // Close the add dealer modal
                var addModal = bootstrap.Modal.getInstance(document.getElementById('addDealerModal'));
                if (addModal) addModal.hide();

                // Refresh detail modal to show new dealer
                self.showDetail({ id: assignmentId });
            })
            .catch(function(error) {
                console.error('Error:', error);
                toastr.error(error.message || T('Dealer.AddError', 'Şube eklenirken bir hata oluştu.'));
            })
            .finally(function() {
                self.addDealerModal.isSaving(false);
            });
    };

    self.removeDealerFromAssignment = function(assignmentId, customerDealerId) {
        if (!assignmentId || !customerDealerId) return;

        showConfirmModal({
            title: T('Dealer.RemoveTitle', 'Şubeyi Çıkar'),
            message: T('Dealer.RemoveConfirm', 'Bu şubeyi atamadan çıkarmak istediğinizden emin misiniz?'),
            type: 'danger',
            confirmText: T('Common.Remove', 'Çıkar'),
            confirmIcon: 'bi-trash',
            onConfirm: function() {
                fetch('/api/assignments/' + assignmentId + '/dealers/' + customerDealerId, {
                    method: 'DELETE',
                    credentials: 'include'
                })
                    .then(function(res) {
                        if (!res.ok) {
                            return res.json().then(function(err) {
                                throw new Error(err.message || T('Dealer.RemoveError', 'Şube çıkarılamadı'));
                            });
                        }
                        return res.json();
                    })
                    .then(function() {
                        toastr.success(T('Dealer.RemoveSuccess', 'Şube başarıyla çıkarıldı.'));

                        // Refresh detail modal
                        self.showDetail({ id: assignmentId });
                    })
                    .catch(function(error) {
                        console.error('Error:', error);
                        toastr.error(error.message || T('Dealer.RemoveError', 'Şube çıkarılırken bir hata oluştu.'));
                    });
            }
        });
    };

    // ===== Evaluation Modal =====
    self.openEvaluation = function(assignment) {
        self.selectedEvaluation(assignment);
        var modal = new bootstrap.Modal(document.getElementById('evaluationModal'));
        modal.show();
    };

    // ===== Status Helpers - EnumsService kullanir =====
    self.getStatusBadgeClass = function(status) {
        return EnumsService.getAssignmentStatusCss(status);
    };

    self.getStatusText = function(status) {
        return EnumsService.getAssignmentStatusDisplay(status);
    };

    self.getAssigneeTypeText = function(assigneeType) {
        switch (assigneeType) {
            case 'FieldWorker': return T('Role.FieldWorker', 'Saha Çalışanı');
            case 'External': return T('Assignment.External', 'Harici');
            case 'CustomerPersonnel': return T('Role.CustomerPersonnel', 'Müşteri Temsilcisi');
            default: return '';
        }
    };

    self.getDaysRemainingText = function(daysRemaining) {
        if (daysRemaining < 0) {
            return '(' + Math.abs(daysRemaining) + ' ' + T('Common.DaysPassed', 'gün geçti') + ')';
        } else if (daysRemaining === 0) {
            return T('Common.Today', 'Bugün!');
        } else {
            return '(' + daysRemaining + ' ' + T('Common.DaysLeft', 'gün kaldı') + ')';
        }
    };

    self.getModalTitle = function() {
        return self.isEditing() ? T('Assignment.Edit', 'Atamayı Düzenle') : T('Assignment.Create', 'Yeni Atama Oluştur');
    };

    self.getSaveButtonText = function() {
        return self.isEditing() ? T('Common.Update', 'Güncelle') : T('Common.Create', 'Oluştur');
    };

    // ===== Initialize =====
    // Once EnumsService'i yukle, sonra diger verileri cek
    EnumsService.load().then(function() {
        self.loadAssignments();
        self.loadSummary();
        self.loadProjects();
        self.loadEvaluators();
        self.loadFieldWorkers();
    });
}

// Translation keys
var TRANSLATION_KEYS = [
    'Menu.Users',
    'Role.FieldWorker',
    'Message.LoadError',
    'Assignment.LoadError',
    'Message.FilterError',
    'Assignment.FilterError',
    'Assignment.SelectProject',
    'Assignment.SelectChecklist',
    'Assignment.DueDateRequired',
    'Message.SaveError',
    'Assignment.UpdateSuccess',
    'Assignment.SaveSuccess',
    'Assignment.SaveError',
    'Assignment.ThisAssignment',
    'Message.DeleteError',
    'Assignment.DeleteSuccess',
    'Assignment.DeleteError',
    'Assignment.NotFound',
    'Assignment.ReassignError',
    'Assignment.ReassignSuccess',
    'Assignment.ReassignProcessError',
    'Assignment.CancelTitle',
    'Assignment.CancelConfirm',
    'Button.Cancel',
    'Assignment.CancelError',
    'Assignment.CancelSuccess',
    'Assignment.CancelProcessError',
    'Assignment.ReopenTitle',
    'Assignment.ReopenConfirm',
    'Button.Reopen',
    'Assignment.ReopenError',
    'Assignment.ReopenSuccess',
    'Assignment.ReopenProcessError',
    'Assignment.SelectFirst',
    'Period.NameRequired',
    'Period.StartDateRequired',
    'Period.EndDateRequired',
    'Period.InvalidDateRange',
    'Period.TargetRequired',
    'Period.CreateError',
    'Period.CreateSuccess',
    'Assignment.DetailLoadError',
    'Assignment.External',
    'Role.CustomerPersonnel',
    'Common.DaysPassed',
    'Common.Today',
    'Common.DaysLeft',
    'Assignment.Edit',
    'Assignment.Create',
    'Common.Update',
    'Common.Create',
    // Confirm modal keys
    'Common.Confirmation',
    'Confirm.Message',
    'Common.DeleteConfirmation',
    'Common.DeleteConfirmationMessage',
    'Common.YesDelete',
    'Common.Confirm',
    // Update Due Date keys
    'Assignment.UpdateDueDateError',
    'Assignment.UpdateDueDateSuccess',
    'Assignment.UpdateDueDateProcessError'
];

// ===== Apply Bindings =====
$(document).ready(function() {
    Localization.loadKeys(TRANSLATION_KEYS).then(function() {
        ko.applyBindings(new AssignmentsViewModel(), document.getElementById('assignments-app'));
    });
});
