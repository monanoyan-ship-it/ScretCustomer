(function() {
    function TrainingsViewModel() {
        var self = this;

        // State
        self.isLoading = ko.observable(false);
        self.isSaving = ko.observable(false);
        self.errorMessage = ko.observable('');
        self.successMessage = ko.observable('');

        // Data
        self.trainings = ko.observableArray([]);
        self.projects = ko.observableArray([]);
        self.customers = ko.observableArray([]);
        self.users = ko.observableArray([]);
        self.summary = ko.observable({
            totalTrainings: 0,
            plannedTrainings: 0,
            inProgressTrainings: 0,
            completedTrainings: 0,
            totalParticipants: 0,
            certificatesIssued: 0
        });

        // Pagination
        self.currentPage = ko.observable(1);
        self.pageSize = ko.observable(20);
        self.totalCount = ko.observable(0);
        self.totalPages = ko.computed(function() {
            return Math.ceil(self.totalCount() / self.pageSize());
        });

        // Filters
        self.filter = {
            projectId: ko.observable(''),
            trainingType: ko.observable(''),
            status: ko.observable(''),
            category: ko.observable(''),
            startDate: ko.observable(''),
            endDate: ko.observable(''),
            isMandatory: ko.observable(false),
            searchTerm: ko.observable('')
        };

        // Subscribe to filter changes
        Object.keys(self.filter).forEach(function(key) {
            self.filter[key].subscribe(function() {
                self.currentPage(1);
                self.loadTrainings();
            });
        });

        // Visible pages
        self.visiblePages = ko.computed(function() {
            var pages = [];
            var total = self.totalPages();
            var current = self.currentPage();
            var start = Math.max(1, current - 2);
            var end = Math.min(total, current + 2);
            for (var i = start; i <= end; i++) {
                pages.push(i);
            }
            return pages;
        });

        // Modal states
        self.isFormModalOpen = ko.observable(false);
        self.isDetailModalOpen = ko.observable(false);
        self.isParticipantModalOpen = ko.observable(false);
        self.isUpdateParticipantModalOpen = ko.observable(false);
        self.isMaterialModalOpen = ko.observable(false);

        // Modal data
        self.editingTraining = ko.observable(null);
        self.viewingTraining = ko.observable(null);
        self.currentTrainingId = ko.observable(null);

        // New participant
        self.newParticipant = {
            userId: ko.observable(''),
            externalName: ko.observable(''),
            externalEmail: ko.observable(''),
            externalPhone: ko.observable('')
        };

        // Update participant
        self.updateParticipant = {
            id: ko.observable(''),
            status: ko.observable('Attended'),
            score: ko.observable(''),
            feedback: ko.observable(''),
            rating: ko.observable(''),
            notes: ko.observable('')
        };

        // New material
        self.newMaterial = {
            name: ko.observable(''),
            description: ko.observable(''),
            externalUrl: ko.observable(''),
            isRequired: ko.observable(false)
        };

        // ========== TEXT HELPERS ==========

        self.getTrainingTypeText = function(type) {
            var map = {
                'InPerson': 'Yüz Yüze', 'Online': 'Online', 'Hybrid': 'Hibrit',
                'SelfPaced': 'Kendi Kendine', 'OnTheJob': 'İş Başı', 'Workshop': 'Workshop',
                'Seminar': 'Seminer', 'Certification': 'Sertifika'
            };
            return map[type] || type;
        };

        self.getTrainingTypeBadgeClass = function(type) {
            var map = {
                'InPerson': 'bg-primary', 'Online': 'bg-info', 'Hybrid': 'bg-warning text-dark',
                'SelfPaced': 'bg-secondary', 'OnTheJob': 'bg-success', 'Workshop': 'bg-dark',
                'Seminar': 'bg-primary', 'Certification': 'bg-danger'
            };
            return map[type] || 'bg-secondary';
        };

        self.getStatusText = function(status) {
            var map = {
                'Draft': 'Taslak', 'Planned': 'Planlandı', 'PendingApproval': 'Onay Bekliyor',
                'Approved': 'Onaylandı', 'InProgress': 'Devam Ediyor', 'Completed': 'Tamamlandı',
                'Cancelled': 'İptal', 'Postponed': 'Ertelendi'
            };
            return map[status] || status;
        };

        self.getStatusBadgeClass = function(status) {
            var map = {
                'Draft': 'bg-secondary', 'Planned': 'bg-info', 'PendingApproval': 'bg-warning text-dark',
                'Approved': 'bg-primary', 'InProgress': 'bg-warning text-dark', 'Completed': 'bg-success',
                'Cancelled': 'bg-danger', 'Postponed': 'bg-secondary'
            };
            return map[status] || 'bg-secondary';
        };

        self.getCategoryText = function(category) {
            var map = {
                'General': 'Genel', 'Technical': 'Teknik', 'Sales': 'Satış',
                'CustomerService': 'Müşteri Hizmetleri', 'Management': 'Yönetim',
                'Communication': 'İletişim', 'Product': 'Ürün', 'Process': 'Süreç',
                'Safety': 'Güvenlik', 'Compliance': 'Uyum'
            };
            return map[category] || category;
        };

        self.getParticipantStatusText = function(status) {
            var map = {
                'Invited': 'Davetli', 'Accepted': 'Kabul Etti', 'Declined': 'Reddetti',
                'Attended': 'Katıldı', 'NotAttended': 'Katılmadı', 'Completed': 'Tamamladı', 'Failed': 'Başarısız'
            };
            return map[status] || status;
        };

        self.getParticipantStatusBadgeClass = function(status) {
            var map = {
                'Invited': 'bg-info', 'Accepted': 'bg-primary', 'Declined': 'bg-danger',
                'Attended': 'bg-success', 'NotAttended': 'bg-warning text-dark',
                'Completed': 'bg-success', 'Failed': 'bg-danger'
            };
            return map[status] || 'bg-secondary';
        };

        // ========== LOAD DATA ==========

        self.loadTrainings = function() {
            self.isLoading(true);
            var params = new URLSearchParams({
                page: self.currentPage(),
                pageSize: self.pageSize()
            });

            if (self.filter.projectId()) params.append('projectId', self.filter.projectId());
            if (self.filter.trainingType()) params.append('trainingType', self.filter.trainingType());
            if (self.filter.status()) params.append('status', self.filter.status());
            if (self.filter.category()) params.append('category', self.filter.category());
            if (self.filter.startDate()) params.append('startDate', self.filter.startDate());
            if (self.filter.endDate()) params.append('endDate', self.filter.endDate());
            if (self.filter.isMandatory()) params.append('isMandatory', 'true');
            if (self.filter.searchTerm()) params.append('searchTerm', self.filter.searchTerm());

            fetch('/api/trainings?' + params.toString())
                .then(function(response) { return response.json(); })
                .then(function(data) {
                    self.trainings(data.items || []);
                    self.totalCount(data.totalCount || 0);
                })
                .finally(function() {
                    self.isLoading(false);
                });
        };

        self.loadSummary = function() {
            fetch('/api/trainings/summary')
                .then(function(response) { return response.json(); })
                .then(function(data) {
                    self.summary(data);
                });
        };

        self.loadProjects = function() {
            fetch('/api/projects')
                .then(function(response) { return response.json(); })
                .then(function(data) {
                    self.projects(data.items || data || []);
                });
        };

        self.loadCustomers = function() {
            fetch('/api/customers')
                .then(function(response) { return response.json(); })
                .then(function(data) {
                    self.customers(data.items || data || []);
                });
        };

        self.loadUsers = function() {
            fetch('/api/users')
                .then(function(response) { return response.json(); })
                .then(function(data) {
                    self.users(data.items || data || []);
                });
        };

        // ========== CREATE/EDIT MODAL ==========

        self.createNew = function() {
            self.editingTraining({
                id: ko.observable(null),
                title: ko.observable(''),
                description: ko.observable(''),
                trainingType: ko.observable('InPerson'),
                status: ko.observable('Draft'),
                category: ko.observable('General'),
                startDate: ko.observable(''),
                endDate: ko.observable(''),
                durationMinutes: ko.observable(''),
                location: ko.observable(''),
                onlineLink: ko.observable(''),
                maxParticipants: ko.observable(''),
                isMandatory: ko.observable(false),
                hasCertificate: ko.observable(false),
                passingScore: ko.observable(''),
                curriculum: ko.observable(''),
                prerequisites: ko.observable(''),
                objectives: ko.observable(''),
                trainerId: ko.observable(''),
                externalTrainerName: ko.observable(''),
                externalTrainerCompany: ko.observable(''),
                projectId: ko.observable(''),
                customerId: ko.observable(''),
                cost: ko.observable(''),
                currency: ko.observable('TRY'),
                tags: ko.observable('')
            });
            self.isFormModalOpen(true);
        };

        self.editTraining = function(training) {
            fetch('/api/trainings/' + training.id)
                .then(function(response) { return response.json(); })
                .then(function(data) {
                    self.editingTraining({
                        id: ko.observable(data.id),
                        title: ko.observable(data.title),
                        description: ko.observable(data.description || ''),
                        trainingType: ko.observable(data.trainingType),
                        status: ko.observable(data.status),
                        category: ko.observable(data.category),
                        startDate: ko.observable(data.startDate ? data.startDate.substring(0, 16) : ''),
                        endDate: ko.observable(data.endDate ? data.endDate.substring(0, 16) : ''),
                        durationMinutes: ko.observable(data.durationMinutes || ''),
                        location: ko.observable(data.location || ''),
                        onlineLink: ko.observable(data.onlineLink || ''),
                        maxParticipants: ko.observable(data.maxParticipants || ''),
                        isMandatory: ko.observable(data.isMandatory),
                        hasCertificate: ko.observable(data.hasCertificate),
                        passingScore: ko.observable(data.passingScore || ''),
                        curriculum: ko.observable(data.curriculum || ''),
                        prerequisites: ko.observable(data.prerequisites || ''),
                        objectives: ko.observable(data.objectives || ''),
                        trainerId: ko.observable(data.trainerId || ''),
                        externalTrainerName: ko.observable(data.externalTrainerName || ''),
                        externalTrainerCompany: ko.observable(data.externalTrainerCompany || ''),
                        projectId: ko.observable(data.projectId || ''),
                        customerId: ko.observable(data.customerId || ''),
                        cost: ko.observable(data.cost || ''),
                        currency: ko.observable(data.currency || 'TRY'),
                        tags: ko.observable(data.tags || '')
                    });
                    self.isFormModalOpen(true);
                });
        };

        self.closeFormModal = function() {
            self.isFormModalOpen(false);
            self.editingTraining(null);
        };

        self.saveTraining = function() {
            var item = self.editingTraining();
            if (!item.title()) {
                toastr.warning('Başlık gereklidir');
                return;
            }

            var isNew = !item.id();
            var url = isNew ? '/api/trainings' : '/api/trainings/' + item.id();
            var method = isNew ? 'POST' : 'PUT';

            self.isSaving(true);

            var data = {
                title: item.title(),
                description: item.description(),
                trainingType: item.trainingType(),
                status: item.status(),
                category: item.category(),
                startDate: item.startDate() || null,
                endDate: item.endDate() || null,
                durationMinutes: item.durationMinutes() ? parseInt(item.durationMinutes()) : null,
                location: item.location(),
                onlineLink: item.onlineLink(),
                maxParticipants: item.maxParticipants() ? parseInt(item.maxParticipants()) : null,
                isMandatory: item.isMandatory(),
                hasCertificate: item.hasCertificate(),
                passingScore: item.passingScore() ? parseInt(item.passingScore()) : null,
                curriculum: item.curriculum(),
                prerequisites: item.prerequisites(),
                objectives: item.objectives(),
                trainerId: item.trainerId() || null,
                externalTrainerName: item.externalTrainerName(),
                externalTrainerCompany: item.externalTrainerCompany(),
                projectId: item.projectId() || null,
                customerId: item.customerId() || null,
                cost: item.cost() ? parseFloat(item.cost()) : null,
                currency: item.currency(),
                tags: item.tags()
            };

            fetch(url, {
                method: method,
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(data)
            })
            .then(function(response) {
                if (!response.ok) {
                    return response.json().then(function(err) {
                        throw new Error(err.message || 'Bir hata oluştu');
                    });
                }
                return response.json();
            })
            .then(function(savedTraining) {
                if (isNew) {
                    // Yeni kayıt: array'e ekle
                    self.trainings.push(savedTraining);
                    self.totalCount(self.totalCount() + 1);
                } else {
                    // Güncelleme: array'de bul ve güncelle
                    var list = self.trainings();
                    for (var i = 0; i < list.length; i++) {
                        if (list[i].id === savedTraining.id) {
                            self.trainings.splice(i, 1, savedTraining);
                            break;
                        }
                    }
                }
                self.closeFormModal();
                self.loadSummary();
                toastr.success(isNew ? 'Eğitim oluşturuldu' : 'Eğitim güncellendi');
            })
            .catch(function(error) {
                toastr.error(error.message || 'Bir hata oluştu');
            })
            .finally(function() {
                self.isSaving(false);
            });
        };

        // ========== DETAIL MODAL ==========

        self.showDetail = function(training) {
            self.currentTrainingId(training.id);
            self.loadTrainingDetail(training.id);
            self.isDetailModalOpen(true);
        };

        self.loadTrainingDetail = function(id) {
            fetch('/api/trainings/' + id)
                .then(function(response) { return response.json(); })
                .then(function(data) {
                    self.viewingTraining(data);
                });
        };

        self.closeDetailModal = function() {
            self.isDetailModalOpen(false);
            self.viewingTraining(null);
            self.currentTrainingId(null);
        };

        // ========== TRAINING ACTIONS ==========

        self.startTraining = function(training) {
            var id = training.id || self.currentTrainingId();
            fetch('/api/trainings/' + id + '/start', { method: 'POST' })
                .then(function(response) {
                    if (response.ok) return response.json();
                    throw new Error('İşlem başarısız');
                })
                .then(function(updatedTraining) {
                    // Array'de bul ve güncelle
                    var list = self.trainings();
                    for (var i = 0; i < list.length; i++) {
                        if (list[i].id === updatedTraining.id) {
                            self.trainings.splice(i, 1, updatedTraining);
                            break;
                        }
                    }
                    self.loadSummary();
                    if (self.currentTrainingId()) {
                        self.viewingTraining(updatedTraining);
                    }
                    toastr.success('Eğitim başlatıldı');
                });
        };

        self.completeTraining = function(training) {
            var id = training.id || self.currentTrainingId();
            fetch('/api/trainings/' + id + '/complete', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({})
            })
            .then(function(response) {
                if (response.ok) return response.json();
                throw new Error('İşlem başarısız');
            })
            .then(function(updatedTraining) {
                // Array'de bul ve güncelle
                var list = self.trainings();
                for (var i = 0; i < list.length; i++) {
                    if (list[i].id === updatedTraining.id) {
                        self.trainings.splice(i, 1, updatedTraining);
                        break;
                    }
                }
                self.loadSummary();
                if (self.currentTrainingId()) {
                    self.viewingTraining(updatedTraining);
                }
                toastr.success('Eğitim tamamlandı');
            });
        };

        self.deleteTraining = function(training) {
            showDeleteConfirm(training.title, function() {
                fetch('/api/trainings/' + training.id, { method: 'DELETE' })
                    .then(function(response) {
                        if (response.ok) {
                            // Array'den sil
                            self.trainings.remove(function(t) { return t.id === training.id; });
                            self.totalCount(self.totalCount() - 1);
                            self.loadSummary();
                            toastr.success('Eğitim silindi');
                        }
                    });
            });
        };

        // ========== PARTICIPANTS ==========

        self.showAddParticipantModal = function(training) {
            self.currentTrainingId(training.id);
            self.newParticipant.userId('');
            self.newParticipant.externalName('');
            self.newParticipant.externalEmail('');
            self.newParticipant.externalPhone('');
            self.isParticipantModalOpen(true);
        };

        self.closeParticipantModal = function() {
            self.isParticipantModalOpen(false);
        };

        self.addParticipant = function() {
            var data = {
                userId: self.newParticipant.userId() || null,
                externalName: self.newParticipant.externalName(),
                externalEmail: self.newParticipant.externalEmail(),
                externalPhone: self.newParticipant.externalPhone()
            };

            fetch('/api/trainings/' + self.currentTrainingId() + '/participants', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(data)
            })
            .then(function(response) {
                if (response.ok) {
                    self.closeParticipantModal();
                    self.loadTrainingDetail(self.currentTrainingId());
                    toastr.success('Katılımcı eklendi');
                } else {
                    return response.json().then(function(err) { throw new Error(err.message); });
                }
            })
            .catch(function(error) {
                toastr.error(error.message || 'Hata oluştu');
            });
        };

        self.showUpdateParticipantModal = function(training, participant) {
            self.currentTrainingId(training.id);
            self.updateParticipant.id(participant.id);
            self.updateParticipant.status(participant.status);
            self.updateParticipant.score(participant.score || '');
            self.updateParticipant.feedback(participant.feedback || '');
            self.updateParticipant.rating(participant.rating || '');
            self.updateParticipant.notes(participant.notes || '');
            self.isUpdateParticipantModalOpen(true);
        };

        self.closeUpdateParticipantModal = function() {
            self.isUpdateParticipantModalOpen(false);
        };

        self.updateParticipantStatus = function() {
            var data = {
                status: self.updateParticipant.status(),
                score: self.updateParticipant.score() ? parseInt(self.updateParticipant.score()) : null,
                feedback: self.updateParticipant.feedback(),
                rating: self.updateParticipant.rating() ? parseInt(self.updateParticipant.rating()) : null,
                notes: self.updateParticipant.notes()
            };

            fetch('/api/trainings/' + self.currentTrainingId() + '/participants/' + self.updateParticipant.id(), {
                method: 'PUT',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(data)
            })
            .then(function(response) {
                if (response.ok) {
                    self.closeUpdateParticipantModal();
                    self.loadTrainingDetail(self.currentTrainingId());
                    toastr.success('Katılımcı güncellendi');
                }
            });
        };

        self.issueCertificate = function(training, participant) {
            fetch('/api/trainings/' + training.id + '/participants/' + participant.id + '/certificate', {
                method: 'POST'
            })
            .then(function(response) {
                if (response.ok) {
                    self.loadTrainingDetail(training.id);
                    toastr.success('Sertifika verildi');
                }
            });
        };

        self.removeParticipant = function(training, participant) {
            showDeleteConfirm('Bu katılımcı', function() {
                fetch('/api/trainings/' + training.id + '/participants/' + participant.id, {
                    method: 'DELETE'
                })
                .then(function(response) {
                    if (response.ok) {
                        self.loadTrainingDetail(training.id);
                        toastr.success('Katılımcı kaldırıldı');
                    }
                });
            });
        };

        // ========== MATERIALS ==========

        self.showAddMaterialModal = function(training) {
            self.currentTrainingId(training.id);
            self.newMaterial.name('');
            self.newMaterial.description('');
            self.newMaterial.externalUrl('');
            self.newMaterial.isRequired(false);
            var fileInput = document.getElementById('materialFileInput');
            if (fileInput) fileInput.value = '';
            self.isMaterialModalOpen(true);
        };

        self.closeMaterialModal = function() {
            self.isMaterialModalOpen(false);
        };

        self.addMaterial = function() {
            if (!self.newMaterial.name()) {
                toastr.warning('Materyal adı gereklidir');
                return;
            }

            var formData = new FormData();
            formData.append('name', self.newMaterial.name());
            formData.append('description', self.newMaterial.description() || '');
            formData.append('externalUrl', self.newMaterial.externalUrl() || '');
            formData.append('isRequired', self.newMaterial.isRequired());

            var fileInput = document.getElementById('materialFileInput');
            if (fileInput && fileInput.files && fileInput.files[0]) {
                formData.append('file', fileInput.files[0]);
            }

            fetch('/api/trainings/' + self.currentTrainingId() + '/materials', {
                method: 'POST',
                body: formData
            })
            .then(function(response) {
                if (response.ok) {
                    self.closeMaterialModal();
                    self.loadTrainingDetail(self.currentTrainingId());
                    toastr.success('Materyal eklendi');
                }
            });
        };

        self.deleteMaterial = function(training, material) {
            showDeleteConfirm(material.name + ' materyali', function() {
                fetch('/api/trainings/' + training.id + '/materials/' + material.id, {
                    method: 'DELETE'
                })
                .then(function(response) {
                    if (response.ok) {
                        self.loadTrainingDetail(training.id);
                        toastr.success('Materyal silindi');
                    }
                });
            });
        };

        // ========== PAGINATION ==========

        self.previousPage = function() {
            if (self.currentPage() > 1) {
                self.currentPage(self.currentPage() - 1);
                self.loadTrainings();
            }
        };

        self.nextPage = function() {
            if (self.currentPage() < self.totalPages()) {
                self.currentPage(self.currentPage() + 1);
                self.loadTrainings();
            }
        };

        self.goToPage = function(page) {
            self.currentPage(page);
            self.loadTrainings();
        };

        self.clearFilters = function() {
            self.filter.projectId('');
            self.filter.trainingType('');
            self.filter.status('');
            self.filter.category('');
            self.filter.startDate('');
            self.filter.endDate('');
            self.filter.isMandatory(false);
            self.filter.searchTerm('');
        };

        // ========== INITIALIZE ==========

        self.init = function() {
            // Önce EnumsService'i yükle, sonra diğer verileri çek
            EnumsService.load().then(function() {
                self.loadTrainings();
                self.loadSummary();
                self.loadProjects();
                self.loadCustomers();
                self.loadUsers();
            });
        };

        self.init();
    }

    // CORRECT BINDING - Specific element
    $(document).ready(function() {
        ko.applyBindings(new TrainingsViewModel(), document.getElementById('trainings-app'));
    });
})();
