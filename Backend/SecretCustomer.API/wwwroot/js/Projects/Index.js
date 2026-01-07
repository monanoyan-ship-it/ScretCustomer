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
    self.customerId = ko.observable(data.customerId || '');
    self.projectManagerId = ko.observable(data.projectManagerId || '');

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

    // Filters
    self.searchTerm = ko.observable('');
    self.statusFilter = ko.observable('');
    self.projectTypeFilter = ko.observable('');
    self.viewMode = ko.observable('table');

    // Modals
    self.isEditModalOpen = ko.observable(false);
    self.isDetailModalOpen = ko.observable(false);
    self.editingProject = ko.observable(null);
    self.viewingProject = ko.observable(null);

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

    // Filtered projects
    self.filteredProjects = ko.computed(function() {
        var result = self.projects();
        var term = self.searchTerm().toLowerCase();
        if (term) {
            result = result.filter(function(p) {
                return (p.name && p.name.toLowerCase().indexOf(term) >= 0) ||
                       (p.code && p.code.toLowerCase().indexOf(term) >= 0) ||
                       (p.description && p.description.toLowerCase().indexOf(term) >= 0) ||
                       (p.customerName && p.customerName.toLowerCase().indexOf(term) >= 0);
            });
        }
        if (self.statusFilter()) {
            result = result.filter(function(p) { return p.status === self.statusFilter(); });
        }
        if (self.projectTypeFilter()) {
            result = result.filter(function(p) { return p.projectType === self.projectTypeFilter(); });
        }
        return result;
    });

    self.resetFilters = function() {
        self.searchTerm('');
        self.statusFilter('');
        self.projectTypeFilter('');
    };

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
        self.editingProject(new ProjectEditViewModel());
        self.isEditModalOpen(true);
    };

    // Open edit modal
    self.openEditModal = function(project) {
        self.isLoading(true);
        fetch('/api/projects/' + project.id + '/detail', { credentials: 'include' })
            .then(function(res) {
                if (!res.ok) throw new Error('Proje yüklenemedi');
                return res.json();
            })
            .then(function(data) {
                self.editingProject(new ProjectEditViewModel(data));
                self.isDetailModalOpen(false);
                self.isEditModalOpen(true);
            })
            .catch(function(error) {
                console.error('Error:', error);
                toastr.error('Proje yüklenirken bir hata oluştu.');
            })
            .finally(function() {
                self.isLoading(false);
            });
    };

    // Close edit modal
    self.closeEditModal = function() {
        self.isEditModalOpen(false);
        self.editingProject(null);
    };

    // Open detail modal
    self.openDetailModal = function(project) {
        self.isLoading(true);
        fetch('/api/projects/' + project.id + '/detail', { credentials: 'include' })
            .then(function(res) {
                if (!res.ok) throw new Error('Proje yüklenemedi');
                return res.json();
            })
            .then(function(data) {
                self.viewingProject(data);
                self.isDetailModalOpen(true);
            })
            .catch(function(error) {
                console.error('Error:', error);
                toastr.error('Proje detayı yüklenirken bir hata oluştu.');
            })
            .finally(function() {
                self.isLoading(false);
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
                self.projects.push(savedProject);
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

    // Initialize
    self.init = function() {
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
