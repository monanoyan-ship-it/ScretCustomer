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

        // Set only the relevant assignee field
        if (self.assignmentType() === 'user' && self.assignedUserId()) {
            dto.assignedUserId = self.assignedUserId();
        } else if (self.assignmentType() === 'fieldworker' && self.assignedFieldWorkerId()) {
            dto.assignedFieldWorkerId = self.assignedFieldWorkerId();
        } else if (self.assignmentType() === 'external') {
            dto.externalEmail = self.externalEmail() || null;
            dto.externalName = self.externalName() || null;
        }

        return dto;
    };
}

// Bulk Assignment ViewModel
function BulkAssignmentViewModel() {
    var self = this;

    self.projectId = ko.observable('');
    self.dueDate = ko.observable('');
    self.assignmentCount = ko.observable(1);
    self.assignedUserId = ko.observable('');

    self.reset = function() {
        self.projectId('');
        self.dueDate('');
        self.assignmentCount(1);
        self.assignedUserId('');
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
            dto.newAssignedUserId = self.newAssignedUserId();
        } else if (self.newAssigneeType() === 'fieldworker' && self.newAssignedFieldWorkerId()) {
            dto.newAssignedFieldWorkerId = self.newAssignedFieldWorkerId();
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

    // ===== State =====
    self.isLoading = ko.observable(false);
    self.isSaving = ko.observable(false);
    self.errorMessage = ko.observable('');
    self.successMessage = ko.observable('');
    self.modalErrorMessage = ko.observable('');
    self.isEditing = ko.observable(false);

    // ===== Data =====
    self.assignments = ko.observableArray([]);
    self.availableProjects = ko.observableArray([]);
    self.availableChecklists = ko.observableArray([]);
    self.availableEvaluators = ko.observableArray([]);
    self.availableFieldWorkers = ko.observableArray([]);
    self.projectChecklists = ko.observableArray([]);

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

    // ===== Filters =====
    self.filterProjectId = ko.observable('');
    self.filterStatus = ko.observable('');
    self.filterAssignedUserId = ko.observable('');
    self.filterDueDateFrom = ko.observable('');
    self.filterDueDateTo = ko.observable('');
    self.filterSearchTerm = ko.observable('');

    // All assignable users (for filter)
    self.allAssignableUsers = ko.computed(function() {
        var users = [];
        self.availableEvaluators().forEach(function(u) {
            users.push({ id: u.id, displayName: u.fullName + ' (' + T('User.Title', 'Kullanıcı') + ')' });
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

    // Bulk Assignment
    self.bulkAssignment = ko.observable(new BulkAssignmentViewModel());

    // Reassign
    self.reassignData = ko.observable(new ReassignViewModel());

    // QR Code
    self.qrCodeImage = ko.observable('');
    self.qrCodeLink = ko.observable('');

    // ===== Load Data =====
    self.loadAssignments = function() {
        self.isLoading(true);
        self.errorMessage('');

        fetch('/api/assignments', { credentials: 'include' })
            .then(function(res) {
                if (!res.ok) throw new Error(T('Message.LoadError', 'Yükleme başarısız'));
                return res.json();
            })
            .then(function(data) {
                self.assignments(data);
            })
            .catch(function(error) {
                console.error('Error:', error);
                self.errorMessage(T('Assignment.LoadError', 'Atamalar yüklenirken bir hata oluştu.'));
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
                self.availableProjects(data.filter(function(p) { return p.isActive; }));
            })
            .catch(function(error) { console.error('Error loading projects:', error); });
    };

    self.loadChecklists = function() {
        fetch('/api/checklists', { credentials: 'include' })
            .then(function(res) { return res.json(); })
            .then(function(data) {
                self.availableChecklists(data.filter(function(c) { return c.isActive; }));
            })
            .catch(function(error) { console.error('Error loading checklists:', error); });
    };

    self.loadEvaluators = function() {
        fetch('/api/users/role/3', { credentials: 'include' })
            .then(function(res) { return res.json(); })
            .then(function(data) {
                self.availableEvaluators(data);
            })
            .catch(function(error) { console.error('Error loading evaluators:', error); });
    };

    self.loadFieldWorkers = function() {
        fetch('/api/fieldworkers', { credentials: 'include' })
            .then(function(res) { return res.json(); })
            .then(function(data) {
                self.availableFieldWorkers(data);
            })
            .catch(function(error) { console.error('Error loading field workers:', error); });
    };

    // ===== Filter Methods =====
    self.applyFilters = function() {
        self.isLoading(true);
        self.errorMessage('');

        var filter = {
            projectId: self.filterProjectId() || null,
            status: self.filterStatus() || null,
            assignedUserId: self.filterAssignedUserId() || null,
            dueDateFrom: self.filterDueDateFrom() || null,
            dueDateTo: self.filterDueDateTo() || null,
            searchTerm: self.filterSearchTerm() || null
        };

        fetch('/api/assignments/filter', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            credentials: 'include',
            body: JSON.stringify(filter)
        })
            .then(function(res) {
                if (!res.ok) throw new Error(T('Message.FilterError', 'Filtreleme başarısız'));
                return res.json();
            })
            .then(function(data) {
                self.assignments(data);
                self.loadSummary(self.filterProjectId());
            })
            .catch(function(error) {
                console.error('Error:', error);
                self.errorMessage(T('Assignment.FilterError', 'Atamalar filtrelenirken bir hata oluştu.'));
            })
            .finally(function() {
                self.isLoading(false);
            });
    };

    self.clearFilters = function() {
        self.filterProjectId('');
        self.filterStatus('');
        self.filterAssignedUserId('');
        self.filterDueDateFrom('');
        self.filterDueDateTo('');
        self.filterSearchTerm('');
        self.loadAssignments();
        self.loadSummary();
    };

    // ===== CRUD Methods =====
    self.createNew = function() {
        self.isEditing(false);
        self.editingAssignment(new AssignmentEditViewModel());
        self.projectChecklists([]);
        self.modalErrorMessage('');
        self.isModalOpen(true);
    };

    self.onProjectChange = function() {
        var assignment = self.editingAssignment();
        if (!assignment) return;

        var projectId = assignment.projectId();
        if (!projectId) {
            self.projectChecklists([]);
            return;
        }

        // Load checklists for the project
        fetch('/api/checklists?projectId=' + projectId, { credentials: 'include' })
            .then(function(res) { return res.json(); })
            .then(function(data) {
                self.projectChecklists(data.filter(function(c) { return c.isActive; }));
            })
            .catch(function(error) { console.error('Error loading project checklists:', error); });
    };

    self.saveAssignment = function() {
        var assignment = self.editingAssignment();

        // Validation
        if (!assignment.projectId()) {
            self.modalErrorMessage(T('Assignment.SelectProject', 'Proje seçmelisiniz!'));
            return;
        }

        if (!assignment.checklistId()) {
            self.modalErrorMessage(T('Assignment.SelectChecklist', 'Kontrol listesi seçmelisiniz!'));
            return;
        }

        if (!assignment.dueDate()) {
            self.modalErrorMessage(T('Assignment.DueDateRequired', 'Son tarih zorunludur!'));
            return;
        }

        var dto = assignment.toDTO();
        var isEdit = self.isEditing();
        var url = isEdit ? '/api/assignments/' + assignment.id() : '/api/assignments';
        var method = isEdit ? 'PUT' : 'POST';

        self.isSaving(true);
        self.modalErrorMessage('');

        fetch(url, {
            method: method,
            headers: { 'Content-Type': 'application/json' },
            credentials: 'include',
            body: JSON.stringify(dto)
        })
            .then(function(response) {
                if (!response.ok) throw new Error(T('Message.SaveError', 'Kayıt başarısız'));
                return isEdit ? null : response.json();
            })
            .then(function(data) {
                self.successMessage(isEdit ? T('Assignment.UpdateSuccess', 'Atama başarıyla güncellendi.') : T('Assignment.SaveSuccess', 'Atama başarıyla oluşturuldu.'));
                self.closeModal();
                self.loadAssignments();
                self.loadSummary();
            })
            .catch(function(error) {
                console.error('Error:', error);
                self.modalErrorMessage(T('Assignment.SaveError', 'Atama kaydedilirken bir hata oluştu.'));
            })
            .finally(function() {
                self.isSaving(false);
            });
    };

    self.deleteAssignment = function(assignment) {
        showDeleteConfirm(T('Assignment.ThisAssignment', 'Bu atama'), function() {
            fetch('/api/assignments/' + assignment.id, {
                method: 'DELETE',
                credentials: 'include'
            })
                .then(function(response) {
                    if (!response.ok) throw new Error(T('Message.DeleteError', 'Silme başarısız'));
                    toastr.success(T('Assignment.DeleteSuccess', 'Atama başarıyla silindi.'));
                    self.assignments.remove(assignment);
                    self.loadSummary();
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
        self.modalErrorMessage('');
    };

    // ===== Bulk Assignment =====
    self.openBulkAssignmentModal = function() {
        self.bulkAssignment().reset();
        var modal = new bootstrap.Modal(document.getElementById('bulkAssignmentModal'));
        modal.show();
    };

    self.createBulkAssignments = function() {
        var bulk = self.bulkAssignment();

        if (!bulk.projectId()) {
            toastr.warning(T('Assignment.SelectProject', 'Proje seçmelisiniz!'));
            return;
        }

        if (!bulk.dueDate()) {
            toastr.warning(T('Assignment.DueDateRequired', 'Son tarih zorunludur!'));
            return;
        }

        var dto = {
            projectId: bulk.projectId(),
            dueDate: bulk.dueDate(),
            assignmentCount: parseInt(bulk.assignmentCount()) || 1,
            assignedUserId: bulk.assignedUserId() || null
        };

        self.isSaving(true);

        fetch('/api/assignments/project-bulk', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            credentials: 'include',
            body: JSON.stringify(dto)
        })
            .then(function(res) {
                if (!res.ok) throw new Error(T('Assignment.BulkAssignmentError', 'Toplu atama başarısız'));
                return res.json();
            })
            .then(function(data) {
                self.successMessage(data.message);
                bootstrap.Modal.getInstance(document.getElementById('bulkAssignmentModal')).hide();
                self.loadAssignments();
                self.loadSummary();
            })
            .catch(function(error) {
                console.error('Error:', error);
                self.errorMessage(T('Assignment.BulkCreateError', 'Toplu atama oluşturulurken bir hata oluştu.'));
            })
            .finally(function() {
                self.isSaving(false);
            });
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
            self.errorMessage(T('Assignment.NotFound', 'Atama bulunamadı.'));
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
            .then(function(result) {
                self.successMessage(T('Assignment.ReassignSuccess', 'Atama başarıyla yeniden atandı.'));
                bootstrap.Modal.getInstance(document.getElementById('reassignModal')).hide();
                self.loadAssignments();
            })
            .catch(function(error) {
                console.error('Error:', error);
                self.errorMessage(T('Assignment.ReassignProcessError', 'Yeniden atama yapılırken bir hata oluştu.'));
            })
            .finally(function() {
                self.isSaving(false);
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
                    .then(function(result) {
                        toastr.success(T('Assignment.CancelSuccess', 'Atama başarıyla iptal edildi.'));
                        self.loadAssignments();
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
                    .then(function(result) {
                        toastr.success(T('Assignment.ReopenSuccess', 'Atama başarıyla yeniden açıldı.'));
                        self.loadAssignments();
                        self.loadSummary();
                    })
                    .catch(function(error) {
                        console.error('Error:', error);
                        toastr.error(T('Assignment.ReopenProcessError', 'Atama yeniden açılırken bir hata oluştu.'));
                    });
            }
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
                console.log('Detail data:', data);
                self.selectedDetail(data);
                var modal = new bootstrap.Modal(document.getElementById('detailModal'));
                modal.show();
            })
            .catch(function(error) {
                console.error('Error:', error);
                self.errorMessage(T('Assignment.DetailLoadError', 'Detay yüklenirken bir hata oluştu.'));
            });
    };

    // ===== Evaluation Modal =====
    self.openEvaluation = function(assignment) {
        self.selectedEvaluation(assignment);
        var modal = new bootstrap.Modal(document.getElementById('evaluationModal'));
        modal.show();
    };

    // ===== QR Code =====
    self.showQRCode = function(assignment) {
        var baseUrl = window.location.origin;
        var link = baseUrl + '/form/' + assignment.uniqueLink;

        self.qrCodeLink(link);

        fetch('/api/assignments/' + assignment.id + '/qr-code/base64', { credentials: 'include' })
            .then(function(res) { return res.json(); })
            .then(function(data) {
                self.qrCodeImage(data.qrCode);
                var modal = new bootstrap.Modal(document.getElementById('qrCodeModal'));
                modal.show();
            })
            .catch(function(error) {
                console.error('Error:', error);
                self.errorMessage(T('Assignment.QRCodeError', 'QR kod oluşturulurken bir hata oluştu.'));
            });
    };

    self.copyQRLink = function() {
        navigator.clipboard.writeText(self.qrCodeLink()).then(function() {
            toastr.success(T('Message.LinkCopied', 'Link kopyalandı!'));
        }).catch(function(error) {
            console.error('Error copying link:', error);
            toastr.error(T('Message.LinkCopyError', 'Link kopyalanırken hata oluştu.'));
        });
    };

    self.copyLink = function(assignment) {
        var baseUrl = window.location.origin;
        var link = baseUrl + '/form/' + assignment.uniqueLink;

        navigator.clipboard.writeText(link).then(function() {
            self.successMessage(T('Message.LinkCopiedToClipboard', 'Link panoya kopyalandı!'));
        }).catch(function(error) {
            console.error('Error copying link:', error);
            self.errorMessage(T('Message.LinkCopyError', 'Link kopyalanırken bir hata oluştu.'));
        });
    };

    // ===== Status Helpers =====
    self.getStatusBadgeClass = function(status) {
        switch (status) {
            case 'Pending': return 'bg-warning text-dark';
            case 'InProgress': return 'bg-info';
            case 'Completed': return 'bg-success';
            case 'Expired': return 'bg-danger';
            case 'Cancelled': return 'bg-secondary';
            default: return 'bg-light text-dark';
        }
    };

    self.getStatusText = function(status) {
        switch (status) {
            case 'Pending': return T('Status.Pending', 'Bekleyen');
            case 'InProgress': return T('Status.InProgress', 'Devam Eden');
            case 'Completed': return T('Status.Completed', 'Tamamlandı');
            case 'Expired': return T('Status.Expired', 'Süresi Doldu');
            case 'Cancelled': return T('Status.Cancelled', 'İptal Edildi');
            default: return status || T('Status.Unknown', 'Bilinmiyor');
        }
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
        return self.isEditing() ? T('Button.Update', 'Güncelle') : T('Button.Create', 'Oluştur');
    };

    // ===== Initialize =====
    self.loadAssignments();
    self.loadSummary();
    self.loadProjects();
    self.loadChecklists();
    self.loadEvaluators();
    self.loadFieldWorkers();
}

// ===== Apply Bindings =====
$(document).ready(function() {
    ko.applyBindings(new AssignmentsViewModel(), document.getElementById('assignments-app'));
});
