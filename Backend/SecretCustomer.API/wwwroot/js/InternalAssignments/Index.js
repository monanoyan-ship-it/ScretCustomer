// ===== Internal Assignments ViewModel =====
function InternalAssignmentsViewModel() {
    var self = this;

    // ===== State =====
    self.isLoading = ko.observable(false);
    self.isCreating = ko.observable(false);
    self.isLoadingPersonnel = ko.observable(false);

    // ===== Data =====
    self.assignments = ko.observableArray([]);
    self.customers = ko.observableArray([]);
    self.projects = ko.observableArray([]);
    self.availableProjects = ko.observableArray([]);
    self.availablePersonnel = ko.observableArray([]);

    // Project Picker
    self.projectPickerSearch = ko.observable('');
    self.isProjectPickerOpen = ko.observable(false);
    self.selectedProjectForDisplay = ko.observable(null);

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

    // ===== Sorting =====
    self.sorting = TableSorting.createSortState('createdAt', 'desc');

    // Sorted Assignments
    self.sortedAssignments = ko.computed(function() {
        var items = self.assignments();
        var sortBy = self.sorting.sortBy();
        var sortDir = self.sorting.sortDirection();
        if (!sortBy || items.length === 0) return items;
        return TableSorting.clientSort(items, sortBy, sortDir);
    });

    // Summary
    self.summary = ko.observable({
        totalAssignments: 0,
        completedAssignments: 0,
        pendingAssignments: 0,
        overdueAssignments: 0
    });

    // ===== CHIP-BASED FILTER SYSTEM =====
    self.selectedFilterType = ko.observable('');
    self.activeFilters = ko.observableArray([]);

    // Temp filter values
    self.tempFilter = {
        customerId: ko.observable(null),
        projectId: ko.observable(null),
        status: ko.observable(null),
        selectedDueDateType: ko.observable('')
    };

    // Filter labels
    self.filterLabels = {
        customer: 'Müşteri',
        project: 'Proje',
        status: 'Durum',
        dueDate: 'Son Tarih'
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

    self.statusOptions = [
        { id: 'NotStarted', name: 'Başlamadı' },
        { id: 'InProgress', name: 'Devam Ediyor' },
        { id: 'Completed', name: 'Tamamlandı' },
        { id: 'Expired', name: 'Süresi Doldu' },
        { id: 'Cancelled', name: 'İptal Edildi' }
    ];

    // Can add filter
    self.canAddFilter = ko.computed(function() {
        var type = self.selectedFilterType();
        if (!type) return false;

        switch (type) {
            case 'customer': return self.tempFilter.customerId() !== null;
            case 'project': return self.tempFilter.projectId() !== null;
            case 'status': return self.tempFilter.status() !== null;
            case 'dueDate': return !!self.tempFilter.selectedDueDateType();
            default: return false;
        }
    });

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
                if (!customerId) return;
                var customer = self.customers().find(function(c) { return String(c.id) === String(customerId); });
                filter.value = customerId;
                filter.displayValue = customer ? customer.name : customerId;
                self.tempFilter.customerId(null);
                break;

            case 'project':
                var projectId = self.tempFilter.projectId();
                if (!projectId) return;
                var project = self.projects().find(function(p) { return String(p.id) === String(projectId); });
                filter.value = projectId;
                filter.displayValue = project ? project.name : projectId;
                self.tempFilter.projectId(null);
                break;

            case 'status':
                var statusId = self.tempFilter.status();
                if (!statusId) return;
                var status = self.statusOptions.find(function(s) { return s.id === statusId; });
                filter.value = statusId;
                filter.displayValue = status ? status.name : statusId;
                self.tempFilter.status(null);
                break;

            case 'dueDate':
                var dueDateType = self.tempFilter.selectedDueDateType();
                if (!dueDateType) return;

                var optionInfo = self.dueDateOptions.find(function(o) { return o.systemName === dueDateType; });
                filter.value = dueDateType;
                filter.displayValue = optionInfo ? optionInfo.name : dueDateType;

                self.tempFilter.selectedDueDateType('');
                break;

            default:
                return;
        }

        self.activeFilters.push(filter);
        self.selectedFilterType('');
        self.loadAssignments();
    };

    // Remove filter
    self.removeFilter = function(filter) {
        self.activeFilters.remove(filter);
        self.loadAssignments();
    };

    // Clear all filters
    self.clearFilters = function() {
        self.activeFilters([]);
        self.loadAssignments();
        self.loadSummary();
    };

    // Build filter params (URLSearchParams pattern - KURALLAR.md #20)
    self.buildFilterParams = function() {
        var params = new URLSearchParams();

        self.activeFilters().forEach(function(filter) {
            switch (filter.type) {
                case 'customer':
                    params.append('customerIds', filter.value);
                    break;
                case 'project':
                    params.append('projectIds', filter.value);
                    break;
                case 'status':
                    params.append('status', filter.value);
                    break;
                case 'dueDate':
                    params.append('dueDateFilter', filter.value);
                    break;
            }
        });

        return params;
    };

    // ===== Create Modal State =====
    self.selectionType = ko.observable('role');
    self.createError = ko.observable('');
    self.createResult = ko.observable({});

    self.newAssignment = {
        customerId: ko.observable(null),
        projectId: ko.observable(null),
        dueDate: ko.observable(getDefaultDueDate()),
        roleFilter: ko.observable(''),
        personnelIds: ko.observableArray([])
    };

    // ===== Detail Modal =====
    self.selectedDetail = ko.observable(null);

    // ===== Computed =====
    self.selectedProjectChecklist = ko.computed(function() {
        var projectId = self.newAssignment.projectId();
        if (!projectId) return '';
        var project = self.availableProjects().find(function(p) { return p.id === projectId; });
        return project ? project.checklistName : '';
    });

    self.selectedPersonnelCount = ko.computed(function() {
        if (self.selectionType() === 'individual') {
            return self.newAssignment.personnelIds().length;
        }
        return self.availablePersonnel().length;
    });

    self.canCreate = ko.computed(function() {
        if (!self.newAssignment.customerId() ||
            !self.newAssignment.projectId() ||
            !self.newAssignment.dueDate()) {
            return false;
        }
        if (self.selectionType() === 'individual') {
            return self.newAssignment.personnelIds().length > 0;
        }
        return self.availablePersonnel().length > 0;
    });

    // ===== Load Data =====
    self.loadAssignments = function() {
        self.isLoading(true);

        var params = self.buildFilterParams();
        var paramStr = params.toString();
        var url = '/api/internal-assignments' + (paramStr ? '?' + paramStr : '');

        fetch(url, { credentials: 'include' })
            .then(function(r) { return r.json(); })
            .then(function(data) {
                self.assignments(data);
            })
            .catch(function(error) {
                console.error('Error loading assignments:', error);
                toastr.error(T('InternalAssignments.LoadError', 'Atamalar yüklenirken hata oluştu'));
            })
            .finally(function() {
                self.isLoading(false);
            });
    };

    self.loadSummary = function() {
        fetch('/api/internal-assignments/summary', { credentials: 'include' })
            .then(function(r) { return r.json(); })
            .then(function(data) {
                if (data) {
                    self.summary({
                        totalAssignments: data.totalAssignments || 0,
                        completedAssignments: data.completedAssignments || 0,
                        pendingAssignments: data.pendingAssignments || 0,
                        overdueAssignments: data.overdueAssignments || 0
                    });
                }
            })
            .catch(function(error) {
                console.error('Error loading summary:', error);
            });
    };

    self.loadCustomers = function() {
        fetch('/api/internal-assignments/customers', { credentials: 'include' })
            .then(function(r) { return r.json(); })
            .then(function(data) {
                self.customers(data);
            })
            .catch(function(error) {
                console.error('Error loading customers:', error);
            });
    };

    self.loadProjects = function() {
        fetch('/api/internal-assignments/projects', { credentials: 'include' })
            .then(function(r) { return r.json(); })
            .then(function(data) {
                self.projects(data);
                self.availableProjects(data);
            })
            .catch(function(error) {
                console.error('Error loading projects:', error);
            });
    };

    self.loadPersonnel = function() {
        var customerId = self.newAssignment.customerId();
        if (!customerId) {
            self.availablePersonnel([]);
            return;
        }

        self.isLoadingPersonnel(true);
        var roleFilter = self.newAssignment.roleFilter();
        var url = '/api/internal-assignments/customers/' + customerId + '/personnel';
        if (roleFilter) {
            url += '?roleFilter=' + roleFilter;
        }

        fetch(url, { credentials: 'include' })
            .then(function(r) { return r.json(); })
            .then(function(data) {
                self.availablePersonnel(data);
            })
            .catch(function(error) {
                console.error('Error loading personnel:', error);
            })
            .finally(function() {
                self.isLoadingPersonnel(false);
            });
    };

    // ===== Create Modal =====
    self.openCreateModal = function() {
        self.selectionType('role');
        self.createError('');
        self.newAssignment.customerId(null);
        self.newAssignment.projectId(null);
        self.newAssignment.dueDate(getDefaultDueDate());
        self.newAssignment.roleFilter('');
        self.newAssignment.personnelIds([]);
        self.availablePersonnel([]);
        self.selectedProjectForDisplay(null);
        self.projectPickerSearch('');
        self.isProjectPickerOpen(false);

        var modal = new bootstrap.Modal(document.getElementById('createModal'));
        modal.show();
    };

    // Project Picker Methods
    self.toggleProjectPicker = function() {
        if (!self.newAssignment.customerId()) {
            toastr.warning(T('InternalAssignments.SelectCustomerFirst', 'Önce müşteri seçiniz'));
            return;
        }
        self.isProjectPickerOpen(!self.isProjectPickerOpen());
        if (self.isProjectPickerOpen()) {
            self.projectPickerSearch('');
        }
    };

    self.selectProject = function(project) {
        self.newAssignment.projectId(project.id);
        self.selectedProjectForDisplay(project);
        self.isProjectPickerOpen(false);
        self.projectPickerSearch('');
    };

    self.clearSelectedProject = function() {
        self.newAssignment.projectId(null);
        self.selectedProjectForDisplay(null);
    };

    self.createAssignments = function() {
        if (!self.newAssignment.customerId() || !self.newAssignment.projectId() || !self.newAssignment.dueDate()) {
            self.createError(T('InternalAssignments.FillAllFields', 'Lütfen tüm zorunlu alanları doldurun'));
            return;
        }

        if (!self.canCreate()) {
            self.createError(T('Evaluation.SelectPersonnel', 'Lütfen en az bir personel seçin'));
            return;
        }

        self.isCreating(true);
        self.createError('');

        var dto = {
            customerId: self.newAssignment.customerId(),
            projectId: self.newAssignment.projectId(),
            dueDate: self.newAssignment.dueDate()
        };

        if (self.selectionType() === 'individual') {
            dto.personnelIds = self.newAssignment.personnelIds();
        } else {
            var roleFilter = self.newAssignment.roleFilter();
            if (roleFilter) {
                dto.roleFilter = parseInt(roleFilter);
            }
        }

        fetch('/api/internal-assignments', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            credentials: 'include',
            body: JSON.stringify(dto)
        })
            .then(function(r) {
                if (!r.ok) {
                    return r.text().then(function(text) {
                        var errorMessage = 'API Error (Status: ' + r.status + ')';
                        if (text) {
                            try {
                                var err = JSON.parse(text);
                                errorMessage = err.message || err.title || errorMessage;
                            } catch (e) {
                                errorMessage = text || errorMessage;
                            }
                        }
                        throw new Error(errorMessage);
                    });
                }
                return r.json();
            })
            .then(function(result) {
                self.createResult(result);
                bootstrap.Modal.getInstance(document.getElementById('createModal')).hide();

                var resultModal = new bootstrap.Modal(document.getElementById('resultModal'));
                resultModal.show();

                self.loadAssignments();
                self.loadSummary();

                if (result.successCount > 0) {
                    toastr.success(result.successCount + ' ' + T('InternalAssignments.AssignmentsCreated', 'atama başarıyla oluşturuldu'));
                }
            })
            .catch(function(error) {
                console.error('Error creating assignments:', error);
                self.createError(error.message || T('InternalAssignments.CreateError', 'Atamalar oluşturulurken hata oluştu'));
            })
            .finally(function() {
                self.isCreating(false);
            });
    };

    // ===== Detail Modal =====
    self.showDetail = function(assignment) {
        fetch('/api/assignments/' + assignment.id + '/detail', { credentials: 'include' })
            .then(function(r) { return r.json(); })
            .then(function(data) {
                self.selectedDetail(data);
                var modal = new bootstrap.Modal(document.getElementById('detailModal'));
                modal.show();
            })
            .catch(function(error) {
                console.error('Error loading detail:', error);
                toastr.error(T('InternalAssignments.DetailLoadError', 'Detay yüklenirken hata oluştu'));
            });
    };

    // ===== Actions =====
    self.copyLink = function(assignment) {
        var link = window.location.origin + '/e/' + assignment.uniqueLink;
        navigator.clipboard.writeText(link).then(function() {
            toastr.success(T('Common.LinkCopied', 'Link kopyalandı'));
        }).catch(function() {
            toastr.error(T('Common.LinkCopyError', 'Link kopyalanamadı'));
        });
    };

    self.deleteAssignment = function(assignment) {
        showDeleteConfirm(T('InternalAssignments.ThisAssignment', 'Bu atama'), function() {
            fetch('/api/assignments/' + assignment.id, {
                method: 'DELETE',
                credentials: 'include'
            })
                .then(function(r) {
                    if (!r.ok) throw new Error('Delete failed');
                    self.assignments.remove(assignment);
                    self.loadSummary();
                    toastr.success(T('InternalAssignments.DeleteSuccess', 'Atama silindi'));
                })
                .catch(function(error) {
                    console.error('Error deleting assignment:', error);
                    toastr.error(T('InternalAssignments.DeleteError', 'Atama silinirken hata oluştu'));
                });
        });
    };

    // ===== Status Helpers - EnumsService kullanır =====
    self.getStatusBadgeClass = function(status) {
        return EnumsService.getAssignmentStatusCss(status);
    };

    self.getStatusText = function(status) {
        return EnumsService.getAssignmentStatusDisplay(status);
    };

    self.getDaysRemainingText = function(daysRemaining) {
        if (daysRemaining === undefined || daysRemaining === null) return '';
        if (daysRemaining < 0) {
            return '(' + Math.abs(daysRemaining) + ' ' + T('Common.DaysPassed', 'gün geçti') + ')';
        } else if (daysRemaining === 0) {
            return T('Common.Today', 'Bugün!');
        } else {
            return '(' + daysRemaining + ' ' + T('Common.DaysLeft', 'gün kaldı') + ')';
        }
    };

    self.getRowClass = function(assignment) {
        if (assignment.status === 'Expired') return 'table-danger';
        if (assignment.status === 'Cancelled') return 'table-warning';
        return '';
    };

    // ===== Helpers =====
    self.formatDate = function(dateStr) {
        if (!dateStr) return '-';
        var date = new Date(dateStr);
        return date.toLocaleDateString('tr-TR');
    };

    function getDefaultDueDate() {
        var date = new Date();
        date.setDate(date.getDate() + 7);
        return date.toISOString().split('T')[0];
    }

    // ===== Subscriptions =====
    self.newAssignment.customerId.subscribe(function(customerId) {
        if (customerId) {
            // Sadece seçilen firmaya ait projeleri getir
            var filtered = self.projects().filter(function(p) {
                return p.customerId === parseInt(customerId);
            });
            self.availableProjects(filtered);
            self.loadPersonnel();
        } else {
            self.availableProjects([]);
            self.availablePersonnel([]);
        }
        self.newAssignment.projectId(null);
        self.newAssignment.personnelIds([]);
        self.selectedProjectForDisplay(null);
        self.isProjectPickerOpen(false);
    });

    self.newAssignment.roleFilter.subscribe(function() {
        if (self.newAssignment.customerId()) {
            self.loadPersonnel();
        }
    });

    self.selectionType.subscribe(function() {
        self.newAssignment.personnelIds([]);
        if (self.newAssignment.customerId()) {
            self.loadPersonnel();
        }
    });

    // ===== Initialize =====
    self.loadAssignments();
    self.loadSummary();
    self.loadCustomers();
    self.loadProjects();
}

// ===== Translation Keys =====
var TRANSLATION_KEYS = [
    'InternalAssignments.LoadError',
    'InternalAssignments.FillAllFields',
    'Evaluation.SelectPersonnel',
    'InternalAssignments.AssignmentsCreated',
    'InternalAssignments.CreateError',
    'InternalAssignments.DetailLoadError',
    'InternalAssignments.ThisAssignment',
    'InternalAssignments.DeleteSuccess',
    'InternalAssignments.DeleteError',
    'Common.LinkCopied',
    'Common.LinkCopyError',
    'Common.DaysPassed',
    'Common.Today',
    'Common.DaysLeft'
];

// ===== Apply Bindings =====
$(document).ready(function() {
    EnumsService.load().then(function() {
        return Localization.loadKeys(TRANSLATION_KEYS);
    }).then(function() {
        ko.applyBindings(new InternalAssignmentsViewModel(), document.getElementById('internal-assignments-app'));
    });
});
