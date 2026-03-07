// FieldWorker Visits ViewModel - Tab yapısı ile Atamalar + Ziyaretler
function FieldWorkerVisitsViewModel() {
    var self = this;

    // ========================
    // GLOBAL STATE
    // ========================
    self.isLoading = ko.observable(true);
    self.isSaving = ko.observable(false);
    self.activeTab = ko.observable('assignments');

    // Tab değiştirme - Ziyaretler tabına geçince listeyi yükle
    self.setActiveTab = function(tab) {
        self.activeTab(tab);
        if (tab === 'visits' && self.visits().length === 0) {
            self.loadVisits();
        }
    };

    // ========================
    // ASSIGNMENTS TAB
    // ========================
    self.isAssignmentsLoading = ko.observable(true);
    self.assignments = ko.observableArray([]);
    self.assignmentsSearch = ko.observable('');

    // Filtered assignments based on search
    self.filteredAssignments = ko.computed(function() {
        var search = self.assignmentsSearch().toLowerCase();
        if (!search) return self.assignments();

        return self.assignments().filter(function(a) {
            return (a.projectName && a.projectName.toLowerCase().indexOf(search) > -1) ||
                   (a.customerName && a.customerName.toLowerCase().indexOf(search) > -1);
        });
    });

    // Load assignments (projects)
    self.loadAssignments = function() {
        self.isAssignmentsLoading(true);
        return ApiService.get('/fieldworker/projects')
            .then(function(data) {
                self.assignments(data || []);
                // projects dizisini de güncelle (modal için)
                self.projects(data || []);
            })
            .catch(function(error) {
                console.error('Error loading assignments:', error);
                toastr.error(T('FieldWorker.LoadAssignmentsError', 'Atamalar yüklenirken hata oluştu.'));
            })
            .finally(function() {
                self.isAssignmentsLoading(false);
            });
    };

    // ========================
    // VISITS TAB
    // ========================
    self.isVisitsLoading = ko.observable(false);
    self.visits = ko.observableArray([]);
    self.projects = ko.observableArray([]);
    self.dealers = ko.observableArray([]);

    // Pagination
    self.currentPage = ko.observable(1);
    self.pageSize = 20;
    self.totalCount = ko.observable(0);
    self.totalPages = ko.computed(function() {
        return Math.ceil(self.totalCount() / self.pageSize) || 1;
    });

    // ==================== FILTER SYSTEM ====================
    self.selectedFilterType = ko.observable('');
    self.activeFilters = ko.observableArray([]);

    // Temp filter values
    self.tempFilter = {
        searchTerm: ko.observable(''),
        projectId: ko.observable(''),
        status: ko.observable(''),
        startDate: ko.observable(''),
        endDate: ko.observable('')
    };

    // Filter labels
    self.filterLabels = {
        search: 'Arama',
        project: 'Proje',
        status: 'Durum',
        dateRange: 'Tarih'
    };

    self.statusLabels = {
        '1': 'Beklemede',
        '2': 'Devam Ediyor',
        '3': 'Tamamlandı',
        '4': 'Taslak',
        '5': 'İptal'
    };

    // Can add filter check
    self.canAddFilter = ko.computed(function() {
        var type = self.selectedFilterType();
        if (!type) return false;
        switch (type) {
            case 'search': return self.tempFilter.searchTerm().trim() !== '';
            case 'project': return self.tempFilter.projectId();
            case 'status': return self.tempFilter.status();
            case 'dateRange': return self.tempFilter.startDate() || self.tempFilter.endDate();
            default: return false;
        }
    });

    // Add filter
    self.addFilter = function() {
        var type = self.selectedFilterType();
        if (!type) return;

        var filter = { type: type, label: self.filterLabels[type] };

        switch (type) {
            case 'search':
                filter.value = self.tempFilter.searchTerm().trim();
                filter.displayValue = filter.value;
                self.tempFilter.searchTerm('');
                break;
            case 'project':
                filter.value = self.tempFilter.projectId();
                var project = self.projects().find(function(p) { return p.projectId == filter.value; });
                filter.displayValue = project ? project.projectName : filter.value;
                self.tempFilter.projectId('');
                break;
            case 'status':
                filter.value = self.tempFilter.status();
                filter.displayValue = self.statusLabels[filter.value] || filter.value;
                self.tempFilter.status('');
                break;
            case 'dateRange':
                filter.startDate = self.tempFilter.startDate();
                filter.endDate = self.tempFilter.endDate();
                filter.displayValue = (filter.startDate || '...') + ' - ' + (filter.endDate || '...');
                self.tempFilter.startDate('');
                self.tempFilter.endDate('');
                break;
        }

        self.activeFilters.push(filter);
        self.selectedFilterType('');
        self.currentPage(1);
        self.loadVisits();
    };

    // Remove filter
    self.removeFilter = function(filter) {
        self.activeFilters.remove(filter);
        self.currentPage(1);
        self.loadVisits();
    };

    // Clear all filters
    self.clearAllFilters = function() {
        self.activeFilters.removeAll();
        self.currentPage(1);
        self.loadVisits();
    };

    // Date range quick select
    self.setTempDateRange = function(range) {
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

        var formatDate = function(d) {
            return d.toISOString().split('T')[0];
        };

        switch (range) {
            case 'today':
                startDate = endDate = formatDate(today);
                break;
            case 'yesterday':
                var yesterday = new Date(today);
                yesterday.setDate(yesterday.getDate() - 1);
                startDate = endDate = formatDate(yesterday);
                break;
            case 'thisWeek':
                startDate = formatDate(getMonday(today));
                endDate = formatDate(today);
                break;
            case 'lastWeek':
                var lastWeekStart = getMonday(today);
                lastWeekStart.setDate(lastWeekStart.getDate() - 7);
                var lastWeekEnd = new Date(lastWeekStart);
                lastWeekEnd.setDate(lastWeekEnd.getDate() + 6);
                startDate = formatDate(lastWeekStart);
                endDate = formatDate(lastWeekEnd);
                break;
            case 'thisMonth':
                startDate = formatDate(new Date(today.getFullYear(), today.getMonth(), 1));
                endDate = formatDate(today);
                break;
            case 'lastMonth':
                startDate = formatDate(new Date(today.getFullYear(), today.getMonth() - 1, 1));
                endDate = formatDate(new Date(today.getFullYear(), today.getMonth(), 0));
                break;
        }

        self.tempFilter.startDate(startDate);
        self.tempFilter.endDate(endDate);
    };

    // Build filter params for API
    self.buildFilterParams = function() {
        var params = new URLSearchParams();
        params.append('page', self.currentPage());
        params.append('pageSize', self.pageSize);

        self.activeFilters().forEach(function(filter) {
            switch (filter.type) {
                case 'search':
                    params.append('searchTerm', filter.value);
                    break;
                case 'project':
                    params.append('projectId', filter.value);
                    break;
                case 'status':
                    params.append('statusId', filter.value);
                    break;
                case 'dateRange':
                    if (filter.startDate) params.append('startDate', filter.startDate);
                    if (filter.endDate) params.append('endDate', filter.endDate);
                    break;
            }
        });

        return params.toString();
    };

    // Load visits
    self.loadVisits = function() {
        self.isVisitsLoading(true);

        var queryString = self.buildFilterParams();
        ApiService.get('/fieldworker/visits?' + queryString)
            .then(function(data) {
                self.visits(data.items || []);
                self.totalCount(data.totalCount || 0);
            })
            .catch(function(error) {
                console.error('Error loading visits:', error);
                toastr.error(T('FieldWorker.LoadVisitsError', 'Ziyaretler yüklenirken hata oluştu.'));
            })
            .finally(function() {
                self.isVisitsLoading(false);
            });
    };

    // Load dealers
    self.loadDealers = function() {
        return ApiService.get('/fieldworker/dealers')
            .then(function(data) {
                self.dealers(data || []);
            });
    };

    // Pagination
    self.prevPage = function() {
        if (self.currentPage() > 1) {
            self.currentPage(self.currentPage() - 1);
            self.loadVisits();
        }
    };

    self.nextPage = function() {
        if (self.currentPage() < self.totalPages()) {
            self.currentPage(self.currentPage() + 1);
            self.loadVisits();
        }
    };

    // ========================
    // HELPERS
    // ========================
    self.getStatusBadgeClass = function(statusId) {
        var classes = {
            1: 'bg-secondary',      // Pending
            2: 'bg-primary',        // InProgress
            3: 'bg-success',        // Completed
            4: 'bg-warning text-dark', // Draft
            5: 'bg-danger'          // Cancelled
        };
        return classes[statusId] || 'bg-secondary';
    };

    self.formatDate = function(dateStr) {
        if (!dateStr) return '-';
        var date = new Date(dateStr);
        return date.toLocaleDateString('tr-TR');
    };

    self.formatDateTime = function(dateStr) {
        if (!dateStr) return '-';
        var date = new Date(dateStr);
        return date.toLocaleDateString('tr-TR') + ' ' + date.toLocaleTimeString('tr-TR', { hour: '2-digit', minute: '2-digit' });
    };

    // ========================
    // NEW VISIT MODAL
    // ========================
    self.isNewVisitModalOpen = ko.observable(false);
    self.newVisit = {
        projectId: ko.observable(''),
        customerDealerId: ko.observable(''),
        controlDate: ko.observable(''),
        controlTime: ko.observable(''),
        auditorComment: ko.observable('')
    };

    // Checklist soruları için
    self.formData = ko.observable(null);
    self.questions = ko.observableArray([]);
    self.isLoadingQuestions = ko.observable(false);

    // Proje seçildiğinde soruları yükle
    self.newVisit.projectId.subscribe(function(projectId) {
        if (!projectId) {
            self.formData(null);
            self.questions([]);
            return;
        }

        var project = self.projects().find(function(p) { return p.projectId === parseInt(projectId); });
        if (!project || !project.projectId) {
            self.formData(null);
            self.questions([]);
            return;
        }

        self.isLoadingQuestions(true);
        ApiService.get('/evaluations/form/' + project.projectId)
            .then(function(data) {
                self.formData(data);
                // penaltyGroups'tan soruları düzleştir
                var allQuestions = [];
                if (data.penaltyGroups && data.penaltyGroups.length > 0) {
                    data.penaltyGroups.forEach(function(group) {
                        if (group.questions && group.questions.length > 0) {
                            group.questions.forEach(function(q) {
                                // Her soru için observable'lar ekle
                                q.selectedValue = ko.observable(q.answer || '');
                                q.notes = ko.observable(q.notes || '');
                                allQuestions.push(q);
                            });
                        }
                    });
                }
                self.questions(allQuestions);
            })
            .catch(function(error) {
                console.error('Error loading questions:', error);
                toastr.error(T('FieldWorker.LoadQuestionsError', 'Sorular yüklenirken hata oluştu.'));
                self.formData(null);
                self.questions([]);
            })
            .finally(function() {
                self.isLoadingQuestions(false);
            });
    });

    // Filtered dealers based on selected project
    self.filteredDealers = ko.computed(function() {
        var projectId = self.newVisit.projectId();
        if (!projectId) return [];

        var project = self.projects().find(function(p) { return p.projectId === parseInt(projectId); });
        if (!project) return [];

        return self.dealers().filter(function(d) {
            return d.customerId === project.customerId;
        });
    });

    self.showNewVisitModal = function() {
        self.newVisit.projectId('');
        self.newVisit.customerDealerId('');
        self.newVisit.controlDate(new Date().toISOString().split('T')[0]);
        self.newVisit.controlTime('');
        self.newVisit.auditorComment('');
        self.formData(null);
        self.questions([]);
        self.isNewVisitModalOpen(true);
    };

    self.closeNewVisitModal = function() {
        self.isNewVisitModalOpen(false);
        self.formData(null);
        self.questions([]);
    };

    self.saveVisit = function() {
        self.doSaveVisit(false);
    };

    self.saveAsDraft = function() {
        self.doSaveVisit(true);
    };

    self.doSaveVisit = function(isDraft) {
        if (!self.newVisit.projectId()) {
            toastr.warning(T('FieldWorker.ProjectRequired', 'Proje seçimi zorunludur.'));
            return;
        }
        if (!self.newVisit.customerDealerId()) {
            toastr.warning(T('FieldWorker.DealerRequired', 'Bayi seçimi zorunludur.'));
            return;
        }

        self.isSaving(true);

        // Soruların cevaplarını topla
        var answers = [];
        self.questions().forEach(function(q) {
            var value = q.selectedValue ? q.selectedValue() : '';
            if (value !== '' && value !== null && value !== undefined) {
                answers.push({
                    questionId: q.id,
                    numericAnswer: !isNaN(parseFloat(value)) ? parseFloat(value) : null,
                    textAnswer: isNaN(parseFloat(value)) ? value : null,
                    notes: q.notes ? q.notes() : null
                });
            }
        });

        // Seçili projenin checklistId'sini al
        var project = self.projects().find(function(p) { return p.projectId === parseInt(self.newVisit.projectId()); });
        var checklistId = project ? project.checklistId : 0;

        var data = {
            projectId: parseInt(self.newVisit.projectId()),
            customerDealerId: parseInt(self.newVisit.customerDealerId()),
            checklistId: checklistId,
            controlDate: self.newVisit.controlDate() ? new Date(self.newVisit.controlDate()).toISOString() : null,
            controlTime: self.newVisit.controlTime() || null,
            auditorComment: self.newVisit.auditorComment() || null,
            isDraft: isDraft,
            answers: answers
        };

        ApiService.post('/fieldworker/visits', data)
            .then(function(response) {
                var message = isDraft
                    ? T('FieldWorker.VisitSavedAsDraft', 'Ziyaret taslak olarak kaydedildi.')
                    : T('FieldWorker.VisitSaved', 'Ziyaret başarıyla kaydedildi.');
                toastr.success(message);
                self.closeNewVisitModal();

                // Assignments'ı yenile (tamamlanan sayısı güncellensin)
                self.loadAssignments();

                // Visits tabındaysak listeyi yenile
                if (self.activeTab() === 'visits') {
                    self.loadVisits();
                }

                // Yeni ziyaretin detayını göster
                if (response.evaluationId) {
                    self.showVisitDetail({ evaluationId: response.evaluationId });
                }
            })
            .catch(function(error) {
                console.error('Error saving visit:', error);
                toastr.error(error.message || T('FieldWorker.SaveVisitError', 'Ziyaret kaydedilirken hata oluştu.'));
            })
            .finally(function() {
                self.isSaving(false);
            });
    };

    // ========================
    // ASSIGNMENT VISITS MODAL
    // ========================
    self.isAssignmentVisitsModalOpen = ko.observable(false);
    self.isAssignmentVisitsLoading = ko.observable(false);
    self.assignmentVisits = ko.observableArray([]);
    self.selectedAssignment = ko.observable(null);

    self.showAssignmentVisits = function(assignment) {
        self.selectedAssignment(assignment);
        self.assignmentVisits([]);
        self.isAssignmentVisitsLoading(true);
        self.isAssignmentVisitsModalOpen(true);

        ApiService.get('/fieldworker/visits?projectId=' + assignment.projectId + '&pageSize=100')
            .then(function(data) {
                var items = (data.items || []).filter(function(v) {
                    return v.statusId !== 3 && v.statusId !== 5;
                });
                self.assignmentVisits(items);
            })
            .catch(function(error) {
                console.error('Error loading assignment visits:', error);
                toastr.error(T('FieldWorker.LoadVisitsError', 'Ziyaretler yüklenirken hata oluştu.'));
            })
            .finally(function() {
                self.isAssignmentVisitsLoading(false);
            });
    };

    self.closeAssignmentVisitsModal = function() {
        self.isAssignmentVisitsModalOpen(false);
        self.selectedAssignment(null);
        self.assignmentVisits([]);
    };

    self.openVisitInPopup = function(visit) {
        var url = '/FieldWorker/VisitPopup?evaluationId=' + visit.evaluationId;
        var width = 1200;
        var height = 800;
        var left = (screen.width - width) / 2;
        var top = (screen.height - height) / 2;
        window.open(url, 'FieldWorkerVisitPopup',
            'width=' + width + ',height=' + height + ',left=' + left + ',top=' + top + ',scrollbars=yes,resizable=yes');
    };

    // ========================
    // DETAIL MODAL
    // ========================
    self.isDetailModalOpen = ko.observable(false);
    self.isDetailsLoading = ko.observable(false);
    self.selectedVisit = ko.observable(null);
    self.detailsData = ko.observable(null);

    self.showVisitDetail = function(visit) {
        self.selectedVisit(visit);
        self.detailsData(null);
        self.isDetailsLoading(true);
        self.isDetailModalOpen(true);

        if (visit.evaluationId) {
            ApiService.get('/evaluations/' + visit.evaluationId)
                .then(function(data) {
                    self.detailsData(data);
                })
                .catch(function(error) {
                    console.error('Error loading evaluation:', error);
                    toastr.error(T('Evaluation.DetailsLoadError', 'Değerlendirme detayları yüklenirken bir hata oluştu.'));
                })
                .finally(function() {
                    self.isDetailsLoading(false);
                });
        } else {
            self.isDetailsLoading(false);
        }
    };

    self.closeDetailModal = function() {
        self.isDetailModalOpen(false);
        self.selectedVisit(null);
        self.detailsData(null);
    };

    self.editVisit = function(visit) {
        if (visit.evaluationId) {
            var url = '/FieldWorker/VisitPopup?evaluationId=' + visit.evaluationId;
            var width = 1200;
            var height = 800;
            var left = (screen.width - width) / 2;
            var top = (screen.height - height) / 2;
            window.open(url, 'FieldWorkerVisitPopup',
                'width=' + width + ',height=' + height + ',left=' + left + ',top=' + top + ',scrollbars=yes,resizable=yes');
        }
    };

    // Taslak ziyareti sil
    self.deleteVisit = function(visit) {
        if (!visit.evaluationId) return;

        if (!confirm(T('FieldWorker.ConfirmDeleteVisit', 'Bu taslak ziyareti silmek istediğinizden emin misiniz?'))) {
            return;
        }

        ApiService.delete('/evaluations/' + visit.evaluationId)
            .then(function(response) {
                toastr.success(T('FieldWorker.VisitDeleted', 'Taslak ziyaret başarıyla silindi.'));
                self.loadVisits();
                self.loadAssignments(); // Tamamlanan sayısı güncellensin
            })
            .catch(function(error) {
                console.error('Error deleting visit:', error);
                toastr.error(error.message || T('FieldWorker.DeleteVisitError', 'Ziyaret silinirken hata oluştu.'));
            });
    };

    self.continueEvaluation = function() {
        var visit = self.selectedVisit();
        if (visit && visit.evaluationId) {
            var url = '/FieldWorker/VisitPopup?evaluationId=' + visit.evaluationId;
            var width = 1200;
            var height = 800;
            var left = (screen.width - width) / 2;
            var top = (screen.height - height) / 2;
            window.open(url, 'FieldWorkerVisitPopup',
                'width=' + width + ',height=' + height + ',left=' + left + ',top=' + top + ',scrollbars=yes,resizable=yes');
            self.closeDetailModal();
        }
    };

    // ========================
    // REVERT REQUEST (Taslağa Al Talebi)
    // ========================
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
            self.closeDetailModal();
        })
        .catch(function(err) {
            toastr.error(err.message || T('Evaluation.RevertRequestFailed', 'Talep gönderilemedi.'));
        })
        .finally(function() {
            self.isSubmittingRevertRequest(false);
        });
    };

    // ========================
    // INITIALIZE
    // ========================

    // Popup kapandığında yenile
    window.addEventListener('message', function(event) {
        if (event.data === 'visitSaved' || event.data === 'evaluationSaved') {
            self.loadAssignments();
            if (self.activeTab() === 'visits') {
                self.loadVisits();
            }
            if (self.isAssignmentVisitsModalOpen() && self.selectedAssignment()) {
                self.showAssignmentVisits(self.selectedAssignment());
            }
        }
    });

    self.init = function() {
        // URL parametrelerini kontrol et
        var urlParams = new URLSearchParams(window.location.search);
        var tab = urlParams.get('tab');
        if (tab === 'visits') {
            self.activeTab('visits');
        }

        // Verileri yükle
        Promise.all([
            self.loadAssignments(),
            self.loadDealers()
        ]).then(function() {
            self.isLoading(false);

            // Eğer visits tabındaysak ziyaretleri yükle
            if (self.activeTab() === 'visits') {
                self.loadVisits();
            }
        }).catch(function() {
            self.isLoading(false);
        });
    };

    self.init();
}

// Translation keys
var TRANSLATION_KEYS = [
    'FieldWorker.LoadAssignmentsError',
    'FieldWorker.LoadVisitsError',
    'FieldWorker.ProjectRequired',
    'FieldWorker.DealerRequired',
    'FieldWorker.VisitSavedAsDraft',
    'FieldWorker.VisitSaved',
    'FieldWorker.SaveVisitError',
    'Evaluation.DetailsLoadError',
    'Evaluation.RevertRequestSent',
    'Evaluation.RevertRequestFailed',
    'Common.Confirm',
    'Common.Cancel'
];

// Initialize
$(document).ready(function() {
    var app = document.getElementById('fieldworker-visits-app');
    if (app) {
        Localization.loadKeys(TRANSLATION_KEYS).then(function() {
            ko.applyBindings(new FieldWorkerVisitsViewModel(), app);
        });
    }
});
