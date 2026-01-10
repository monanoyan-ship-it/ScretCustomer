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

    // ===== Data =====
    self.assignments = ko.observableArray([]);
    self.availableProjects = ko.observableArray([]);
    self.availableEvaluators = ko.observableArray([]);
    self.availableFieldWorkers = ko.observableArray([]);
    self.selectedProjectChecklistName = ko.observable('');

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

    // Sorting
    self.sorting = TableSorting.createSortState('dueDate', 'desc');

    // Subscribe to sorting changes
    self.sorting.sortBy.subscribe(function() {
        self.applyFilters();
    });
    self.sorting.sortDirection.subscribe(function() {
        self.applyFilters();
    });

    // Debounce helper
    var searchTimeout = null;
    self.filterSearchTerm.subscribe(function(newValue) {
        clearTimeout(searchTimeout);
        searchTimeout = setTimeout(function() {
            self.applyFilters();
        }, 400); // 400ms bekle
    });

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

    // Reassign
    self.reassignData = ko.observable(new ReassignViewModel());

    // Period Form
    self.periodForm = ko.observable(new PeriodFormViewModel());
    self.periodModalError = ko.observable('');
    self.isSavingPeriod = ko.observable(false);

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
                self.availableProjects(data.filter(function(p) { return p.isActive; }));
            })
            .catch(function(error) { console.error('Error loading projects:', error); });
    };

    self.loadEvaluators = function() {
        // Admin (role 1), QualitySpecialist (role 2), FieldWorker (role 3) kullanıcılarını çek
        Promise.all([
            fetch('/api/users/role/1', { credentials: 'include' }).then(function(r) { return r.json(); }),
            fetch('/api/users/role/2', { credentials: 'include' }).then(function(r) { return r.json(); }),
            fetch('/api/users/role/3', { credentials: 'include' }).then(function(r) { return r.json(); })
        ])
        .then(function(results) {
            var admins = results[0] || [];
            var qualitySpecialists = results[1] || [];
            var fieldWorkers = results[2] || [];
            // Birleştir ve tekrarları kaldır (id'ye göre)
            var combined = admins.concat(qualitySpecialists).concat(fieldWorkers);
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

    // FieldWorker modülü kaldırıldı - artık sadece User'lar kullanılıyor
    self.loadFieldWorkers = function() {
        // FieldWorker API artık mevcut değil, boş array set et
        self.availableFieldWorkers([]);
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
            searchTerm: self.filterSearchTerm() || null,
            sortBy: self.sorting.sortBy() || null,
            sortDirection: self.sorting.sortDirection() || 'desc'
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
                toastr.error(T('Assignment.FilterError', 'Atamalar filtrelenirken bir hata oluştu.'));
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
        self.sorting.reset();
        self.loadAssignments();
        self.loadSummary();
    };

    // ===== CRUD Methods =====
    self.createNew = function() {
        self.isEditing(false);
        self.editingAssignment(new AssignmentEditViewModel());
        self.selectedProjectChecklistName('');        self.isModalOpen(true);
    };

    self.onProjectChange = function() {
        var assignment = self.editingAssignment();
        if (!assignment) return;

        var projectId = assignment.projectId();
        if (!projectId) {
            assignment.checklistId('');
            self.selectedProjectChecklistName('');
            return;
        }

        // Find selected project and auto-fill checklist
        var selectedProject = self.availableProjects().find(function(p) {
            return p.id == projectId;
        });

        if (selectedProject) {
            assignment.checklistId(selectedProject.checklistId);
            self.selectedProjectChecklistName(selectedProject.checklistName || '');
        }
    };

    self.saveAssignment = function() {
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

        self.isSaving(true);        fetch(url, {
            method: method,
            headers: { 'Content-Type': 'application/json' },
            credentials: 'include',
            body: JSON.stringify(dto)
        })
            .then(function(response) {
                if (!response.ok) throw new Error(T('Message.SaveError', 'Kayıt başarısız'));
                return response.json();
            })
            .then(function(savedAssignment) {
                if (isEdit) {
                    // Guncelleme: array'de bul ve guncelle
                    var list = self.assignments();
                    for (var i = 0; i < list.length; i++) {
                        if (list[i].id === savedAssignment.id) {
                            self.assignments.splice(i, 1, savedAssignment);
                            break;
                        }
                    }
                } else {
                    // Yeni kayit: array'e ekle (son eklenen en üstte)
                    self.assignments.unshift(savedAssignment);
                }
                toastr.success(isEdit ? T('Assignment.UpdateSuccess', 'Atama başarıyla güncellendi.') : T('Assignment.SaveSuccess', 'Atama başarıyla oluşturuldu.'));
                self.closeModal();
                self.loadSummary();
            })
            .catch(function(error) {
                console.error('Error:', error);
                toastr.error(T('Assignment.SaveError', 'Atama kaydedilirken bir hata oluştu.'));
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
        self.isEditing(false);    };

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
                var list = self.assignments();
                for (var i = 0; i < list.length; i++) {
                    if (list[i].id === updatedAssignment.id) {
                        self.assignments.splice(i, 1, updatedAssignment);
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
                        var list = self.assignments();
                        for (var i = 0; i < list.length; i++) {
                            if (list[i].id === updatedAssignment.id) {
                                self.assignments.splice(i, 1, updatedAssignment);
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
                        var list = self.assignments();
                        for (var i = 0; i < list.length; i++) {
                            if (list[i].id === updatedAssignment.id) {
                                self.assignments.splice(i, 1, updatedAssignment);
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
                toastr.error(T('Assignment.QRCodeError', 'QR kod oluşturulurken bir hata oluştu.'));
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
            toastr.success(T('Message.LinkCopiedToClipboard', 'Link panoya kopyalandı!'));
        }).catch(function(error) {
            console.error('Error copying link:', error);
            toastr.error(T('Message.LinkCopyError', 'Link kopyalanırken bir hata oluştu.'));
        });
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
        return self.isEditing() ? T('Button.Update', 'Güncelle') : T('Button.Create', 'Oluştur');
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
    'User.Title',
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
    'Assignment.QRCodeError',
    'Message.LinkCopied',
    'Message.LinkCopyError',
    'Message.LinkCopiedToClipboard',
    'Assignment.External',
    'Role.CustomerPersonnel',
    'Common.DaysPassed',
    'Common.Today',
    'Common.DaysLeft',
    'Assignment.Edit',
    'Assignment.Create',
    'Button.Update',
    'Button.Create',
    // Confirm modal keys
    'Confirm.Title',
    'Confirm.Message',
    'Confirm.DeleteTitle',
    'Confirm.DeleteMessage',
    'Confirm.YesDelete',
    'Common.Confirm'
];

// ===== Apply Bindings =====
$(document).ready(function() {
    Localization.loadKeys(TRANSLATION_KEYS).then(function() {
        ko.applyBindings(new AssignmentsViewModel(), document.getElementById('assignments-app'));
    });
});
