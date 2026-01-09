// ===== Customer Portal - Internal Assignments ViewModel =====
function CustomerInternalAssignmentsViewModel() {
    var self = this;

    // Get customer info from localStorage
    var userInfo = JSON.parse(localStorage.getItem('customerUser') || '{}');
    self.customerId = userInfo.customerId;

    // ===== State =====
    self.isLoading = ko.observable(false);
    self.isCreating = ko.observable(false);
    self.isLoadingPersonnel = ko.observable(false);

    // ===== Data =====
    self.assignments = ko.observableArray([]);
    self.projects = ko.observableArray([]);
    self.availableProjects = ko.observableArray([]);
    self.availablePersonnel = ko.observableArray([]);

    // Summary
    self.summary = ko.observable({
        totalAssignments: 0,
        completedAssignments: 0,
        pendingAssignments: 0,
        overdueAssignments: 0
    });

    // ===== Filters =====
    self.filter = {
        projectId: ko.observable(null),
        isCompleted: ko.observable(''),
        dueDateTo: ko.observable(null)
    };

    // ===== Create Modal State =====
    self.selectionType = ko.observable('role');
    self.createError = ko.observable('');
    self.createResult = ko.observable({});

    self.newAssignment = {
        projectId: ko.observable(null),
        dueDate: ko.observable(getDefaultDueDate()),
        roleFilter: ko.observable(''),
        personnelIds: ko.observableArray([])
    };

    // ===== Detail Modal =====
    self.selectedDetail = ko.observable(null);

    // ===== API Helper =====
    function apiFetch(url, options) {
        options = options || {};
        options.headers = options.headers || {};

        var token = getCookie('CustomerToken');
        if (token) {
            options.headers['Authorization'] = 'Bearer ' + token;
        }
        options.headers['Content-Type'] = 'application/json';

        return fetch(url, options).then(function(response) {
            if (response.status === 401) {
                localStorage.removeItem('customerUser');
                document.cookie = 'CustomerToken=; path=/; max-age=0';
                window.location.href = '/Account/Login';
                throw new Error('Unauthorized');
            }
            if (!response.ok) {
                return response.json().then(function(err) {
                    throw new Error(err.message || 'API Error');
                });
            }
            return response.json();
        });
    }

    function getCookie(name) {
        var value = '; ' + document.cookie;
        var parts = value.split('; ' + name + '=');
        if (parts.length === 2) return parts.pop().split(';').shift();
        return null;
    }

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
        if (!self.newAssignment.projectId() || !self.newAssignment.dueDate()) {
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
        var params = ['customerId=' + self.customerId];

        if (self.filter.projectId()) params.push('projectId=' + self.filter.projectId());
        if (self.filter.isCompleted() !== '') params.push('isCompleted=' + self.filter.isCompleted());
        if (self.filter.dueDateTo()) params.push('dueDateTo=' + self.filter.dueDateTo());

        apiFetch('/api/internal-assignments?' + params.join('&'))
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
        apiFetch('/api/internal-assignments/summary?customerId=' + self.customerId)
            .then(function(data) {
                if (data && data.length > 0) {
                    var total = {
                        totalAssignments: 0,
                        completedAssignments: 0,
                        pendingAssignments: 0,
                        overdueAssignments: 0
                    };
                    data.forEach(function(s) {
                        total.totalAssignments += s.totalAssignments;
                        total.completedAssignments += s.completedAssignments;
                        total.pendingAssignments += s.pendingAssignments;
                        total.overdueAssignments += s.overdueAssignments;
                    });
                    self.summary(total);
                }
            })
            .catch(function(error) {
                console.error('Error loading summary:', error);
            });
    };

    self.loadProjects = function() {
        apiFetch('/api/internal-assignments/projects?customerId=' + self.customerId)
            .then(function(data) {
                self.projects(data);
                self.availableProjects(data);
            })
            .catch(function(error) {
                console.error('Error loading projects:', error);
            });
    };

    self.loadPersonnel = function() {
        self.isLoadingPersonnel(true);
        var roleFilter = self.newAssignment.roleFilter();
        var url = '/api/internal-assignments/customers/' + self.customerId + '/personnel';
        if (roleFilter) {
            url += '?roleFilter=' + roleFilter;
        }

        apiFetch(url)
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

    // ===== Filter Methods =====
    self.applyFilter = function() {
        self.loadAssignments();
    };

    self.clearFilters = function() {
        self.filter.projectId(null);
        self.filter.isCompleted('');
        self.filter.dueDateTo(null);
        self.loadAssignments();
        self.loadSummary();
    };

    // ===== Create Modal =====
    self.openCreateModal = function() {
        self.selectionType('role');
        self.createError('');
        self.newAssignment.projectId(null);
        self.newAssignment.dueDate(getDefaultDueDate());
        self.newAssignment.roleFilter('');
        self.newAssignment.personnelIds([]);

        if (self.availablePersonnel().length === 0) {
            self.loadPersonnel();
        }

        var modal = new bootstrap.Modal(document.getElementById('createModal'));
        modal.show();
    };

    self.createAssignments = function() {
        if (!self.newAssignment.projectId() || !self.newAssignment.dueDate()) {
            self.createError(T('InternalAssignments.FillAllFields', 'Lütfen tüm zorunlu alanları doldurun'));
            return;
        }

        if (!self.canCreate()) {
            self.createError(T('InternalAssignments.SelectPersonnel', 'Lütfen en az bir personel seçin'));
            return;
        }

        self.isCreating(true);
        self.createError('');

        var dto = {
            customerId: self.customerId,
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

        apiFetch('/api/internal-assignments', {
            method: 'POST',
            body: JSON.stringify(dto)
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
        apiFetch('/api/assignments/' + assignment.id + '/detail')
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
            apiFetch('/api/assignments/' + assignment.id, { method: 'DELETE' })
                .then(function() {
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
    self.newAssignment.roleFilter.subscribe(function() {
        self.loadPersonnel();
    });

    self.selectionType.subscribe(function() {
        self.newAssignment.personnelIds([]);
        self.loadPersonnel();
    });

    // ===== Initialize =====
    self.init = function() {
        if (!self.customerId) {
            console.error('Customer ID not found');
            return;
        }
        self.loadProjects();
        self.loadAssignments();
        self.loadSummary();
        self.loadPersonnel();
    };

    self.init();
}

// ===== Translation Keys =====
var TRANSLATION_KEYS = [
    'InternalAssignments.LoadError',
    'InternalAssignments.FillAllFields',
    'InternalAssignments.SelectPersonnel',
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
        ko.applyBindings(new CustomerInternalAssignmentsViewModel(), document.getElementById('internal-assignments-app'));
    });
});
