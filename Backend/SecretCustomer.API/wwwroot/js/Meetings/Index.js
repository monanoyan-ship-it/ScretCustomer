(function() {
    function MeetingsViewModel() {
        var self = this;

        // State
        self.isLoading = ko.observable(false);
        self.isSaving = ko.observable(false);
        self.isUploading = ko.observable(false);
        self.errorMessage = ko.observable('');

        // Data
        self.meetings = ko.observableArray([]);
        self.projects = ko.observableArray([]);
        self.users = ko.observableArray([]);
        self.summary = ko.observable({
            totalMeetings: 0,
            upcomingMeetings: 0,
            todayMeetings: 0,
            completedMeetings: 0
        });

        // Pagination
        self.currentPage = ko.observable(1);
        self.pageSize = ko.observable(20);
        self.totalCount = ko.observable(0);
        self.totalPages = ko.computed(function() {
            return Math.ceil(self.totalCount() / self.pageSize());
        });

        // ========== CHIP-BASED FILTER SYSTEM ==========
        self.selectedFilterType = ko.observable('');
        self.activeFilters = ko.observableArray([]);

        // Temp filter values
        self.tempFilter = {
            meetingType: ko.observable(null),
            status: ko.observable(null),
            dateRange: { startDate: ko.observable(''), endDate: ko.observable('') },
            isOnline: ko.observable(null)
        };

        // Filter labels
        self.filterLabels = {
            meetingType: 'Toplantı Tipi',
            status: 'Durum',
            dateRange: 'Tarih Aralığı',
            isOnline: 'Online'
        };

        self.meetingTypeOptions = [
            { id: 'General', name: 'Genel' },
            { id: 'Project', name: 'Proje' },
            { id: 'Evaluation', name: 'Değerlendirme' },
            { id: 'Training', name: 'Eğitim' },
            { id: 'Customer', name: 'Müşteri' },
            { id: 'KickOff', name: 'Kick-off' },
            { id: 'Closing', name: 'Kapanış' }
        ];

        self.statusOptions = [
            { id: 'Planned', name: 'Planlandı' },
            { id: 'PendingApproval', name: 'Onay Bekliyor' },
            { id: 'Approved', name: 'Onaylandı' },
            { id: 'InProgress', name: 'Devam Ediyor' },
            { id: 'Completed', name: 'Tamamlandı' },
            { id: 'Cancelled', name: 'İptal' },
            { id: 'Postponed', name: 'Ertelendi' }
        ];

        self.onlineOptions = [
            { id: true, name: 'Evet' },
            { id: false, name: 'Hayır' }
        ];

        // Can add filter
        self.canAddFilter = ko.computed(function() {
            var type = self.selectedFilterType();
            if (!type) return false;

            switch (type) {
                case 'meetingType': return self.tempFilter.meetingType() !== null;
                case 'status': return self.tempFilter.status() !== null;
                case 'dateRange': return self.tempFilter.dateRange.startDate() || self.tempFilter.dateRange.endDate();
                case 'isOnline': return self.tempFilter.isOnline() !== null;
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
                case 'meetingType':
                    var meetingTypeId = self.tempFilter.meetingType();
                    if (!meetingTypeId) return;
                    var meetingType = self.meetingTypeOptions.find(function(t) { return t.id === meetingTypeId; });
                    filter.value = meetingTypeId;
                    filter.displayValue = meetingType ? meetingType.name : meetingTypeId;
                    self.tempFilter.meetingType(null);
                    break;

                case 'status':
                    var statusId = self.tempFilter.status();
                    if (!statusId) return;
                    var status = self.statusOptions.find(function(s) { return s.id === statusId; });
                    filter.value = statusId;
                    filter.displayValue = status ? status.name : statusId;
                    self.tempFilter.status(null);
                    break;

                case 'dateRange':
                    var startDate = self.tempFilter.dateRange.startDate();
                    var endDate = self.tempFilter.dateRange.endDate();
                    if (!startDate && !endDate) return;
                    filter.value = { startDate: startDate, endDate: endDate };
                    filter.displayValue = (startDate || '...') + ' - ' + (endDate || '...');
                    self.tempFilter.dateRange.startDate('');
                    self.tempFilter.dateRange.endDate('');
                    break;

                case 'isOnline':
                    var isOnline = self.tempFilter.isOnline();
                    if (isOnline === null) return;
                    filter.value = isOnline;
                    filter.displayValue = isOnline ? 'Evet' : 'Hayır';
                    self.tempFilter.isOnline(null);
                    break;

                default:
                    return;
            }

            self.activeFilters.push(filter);
            self.selectedFilterType('');
            self.currentPage(1);
            self.loadMeetings();
        };

        // Remove filter
        self.removeFilter = function(filter) {
            self.activeFilters.remove(filter);
            self.currentPage(1);
            self.loadMeetings();
        };

        // Clear all filters
        self.clearFilters = function() {
            self.activeFilters([]);
            self.sorting.reset();
            self.currentPage(1);
            self.loadMeetings();
        };

        // Build filter params
        self.buildFilterParams = function(params) {
            self.activeFilters().forEach(function(filter) {
                switch (filter.type) {
                    case 'meetingType':
                        params.append('meetingType', filter.value);
                        break;
                    case 'status':
                        params.append('status', filter.value);
                        break;
                    case 'dateRange':
                        if (filter.value.startDate) params.append('startDate', filter.value.startDate);
                        if (filter.value.endDate) params.append('endDate', filter.value.endDate);
                        break;
                    case 'isOnline':
                        params.append('isOnline', filter.value);
                        break;
                }
            });
        };

        // Sorting
        self.sorting = TableSorting.createSortState('plannedDate', 'desc');

        // Subscribe to sorting changes
        self.sorting.sortBy.subscribe(function() {
            self.currentPage(1);
            self.loadMeetings();
        });
        self.sorting.sortDirection.subscribe(function() {
            self.loadMeetings();
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
        self.isCompleteModalOpen = ko.observable(false);
        self.isUploadModalOpen = ko.observable(false);

        // Modal data
        self.editingMeeting = ko.observable(null);
        self.viewingMeeting = ko.observable(null);
        self.currentMeetingId = ko.observable(null);
        self.uploadDescription = ko.observable('');

        // Complete meeting data
        self.completeData = {
            minutes: ko.observable(''),
            decisions: ko.observable(''),
            notes: ko.observable('')
        };

        // ========== TEXT HELPERS ==========

        self.getMeetingTypeText = function(type) {
            var map = {
                'General': 'Genel', 'Project': 'Proje', 'Evaluation': 'Değerlendirme',
                'Training': 'Eğitim', 'Customer': 'Müşteri', 'KickOff': 'Kick-off', 'Closing': 'Kapanış'
            };
            return map[type] || type;
        };

        self.getStatusText = function(status) {
            var map = {
                'Planned': 'Planlandı', 'PendingApproval': 'Onay Bekliyor', 'Approved': 'Onaylandı',
                'InProgress': 'Devam Ediyor', 'Completed': 'Tamamlandı', 'Cancelled': 'İptal', 'Postponed': 'Ertelendi'
            };
            return map[status] || status;
        };

        self.getStatusClass = function(status) {
            var map = {
                'Planned': 'bg-secondary', 'PendingApproval': 'bg-warning text-dark', 'Approved': 'bg-info',
                'InProgress': 'bg-primary', 'Completed': 'bg-success', 'Cancelled': 'bg-danger', 'Postponed': 'bg-dark'
            };
            return map[status] || 'bg-secondary';
        };

        self.getParticipantStatusText = function(status) {
            var map = {
                'Invited': 'Davet Edildi', 'Accepted': 'Kabul Etti', 'Declined': 'Reddetti',
                'Tentative': 'Belirsiz', 'Attended': 'Katıldı', 'NotAttended': 'Katılmadı'
            };
            return map[status] || status;
        };

        self.getParticipantStatusClass = function(status) {
            var map = {
                'Invited': 'bg-secondary', 'Accepted': 'bg-success', 'Declined': 'bg-danger',
                'Tentative': 'bg-warning text-dark', 'Attended': 'bg-success', 'NotAttended': 'bg-danger'
            };
            return map[status] || 'bg-secondary';
        };

        self.formatDate = function(dateStr) {
            if (!dateStr) return '-';
            var d = new Date(dateStr);
            return d.toLocaleDateString('tr-TR') + ' ' + d.toLocaleTimeString('tr-TR', { hour: '2-digit', minute: '2-digit' });
        };

        self.formatFileSize = function(bytes) {
            if (!bytes) return '0 B';
            if (bytes < 1024) return bytes + ' B';
            if (bytes < 1024 * 1024) return (bytes / 1024).toFixed(1) + ' KB';
            return (bytes / (1024 * 1024)).toFixed(1) + ' MB';
        };

        // ========== LOAD DATA ==========

        self.loadMeetings = function() {
            self.isLoading(true);
            var params = new URLSearchParams({
                page: self.currentPage(),
                pageSize: self.pageSize()
            });

            // Add chip-based filters
            self.buildFilterParams(params);

            // Sorting params
            if (self.sorting.sortBy()) {
                params.append('sortBy', self.sorting.sortBy());
                params.append('sortDirection', self.sorting.sortDirection());
            }

            fetch('/api/meetings?' + params.toString())
                .then(function(response) { return response.json(); })
                .then(function(data) {
                    self.meetings(data.items || []);
                    self.totalCount(data.totalCount || 0);
                })
                .finally(function() {
                    self.isLoading(false);
                });
        };

        self.loadSummary = function() {
            fetch('/api/meetings/summary')
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

        self.loadUsers = function() {
            fetch('/api/users')
                .then(function(response) { return response.json(); })
                .then(function(data) {
                    var users = (data.items || data || []).map(function(u) {
                        return {
                            id: u.id,
                            fullName: (u.firstName || '') + ' ' + (u.lastName || '')
                        };
                    });
                    self.users(users);
                });
        };

        // ========== CREATE/EDIT MODAL ==========

        self.createNew = function() {
            var now = new Date();
            now.setMinutes(now.getMinutes() - now.getTimezoneOffset());

            self.editingMeeting({
                id: ko.observable(null),
                title: ko.observable(''),
                description: ko.observable(''),
                meetingType: ko.observable('General'),
                status: ko.observable('Planned'),
                plannedDate: ko.observable(now.toISOString().slice(0, 16)),
                plannedEndDate: ko.observable(''),
                durationMinutes: ko.observable(60),
                location: ko.observable(''),
                isOnline: ko.observable(false),
                onlineLink: ko.observable(''),
                onlinePlatform: ko.observable(''),
                agenda: ko.observable(''),
                notes: ko.observable(''),
                minutes: ko.observable(''),
                decisions: ko.observable(''),
                projectId: ko.observable(''),
                reminderHoursBefore: ko.observable(24),
                participants: ko.observableArray([])
            });
            self.isFormModalOpen(true);
        };

        self.editMeeting = function(meeting) {
            fetch('/api/meetings/' + meeting.id)
                .then(function(response) { return response.json(); })
                .then(function(data) {
                    var participants = (data.participants || []).map(function(p) {
                        return {
                            id: p.id,
                            userId: ko.observable(p.userId || ''),
                            externalName: ko.observable(p.externalName || ''),
                            externalEmail: ko.observable(p.externalEmail || ''),
                            isRequired: ko.observable(p.isRequired)
                        };
                    });

                    self.editingMeeting({
                        id: ko.observable(data.id),
                        title: ko.observable(data.title),
                        description: ko.observable(data.description || ''),
                        meetingType: ko.observable(data.meetingType),
                        status: ko.observable(data.status),
                        plannedDate: ko.observable(self.formatDateForInput(data.plannedDate)),
                        plannedEndDate: ko.observable(data.plannedEndDate ? self.formatDateForInput(data.plannedEndDate) : ''),
                        durationMinutes: ko.observable(data.durationMinutes || 60),
                        location: ko.observable(data.location || ''),
                        isOnline: ko.observable(data.isOnline),
                        onlineLink: ko.observable(data.onlineLink || ''),
                        onlinePlatform: ko.observable(data.onlinePlatform || ''),
                        agenda: ko.observable(data.agenda || ''),
                        notes: ko.observable(data.notes || ''),
                        minutes: ko.observable(data.minutes || ''),
                        decisions: ko.observable(data.decisions || ''),
                        projectId: ko.observable(data.projectId || ''),
                        reminderHoursBefore: ko.observable(data.reminderHoursBefore || 24),
                        participants: ko.observableArray(participants)
                    });
                    self.isFormModalOpen(true);
                });
        };

        self.formatDateForInput = function(dateStr) {
            if (!dateStr) return '';
            var d = new Date(dateStr);
            d.setMinutes(d.getMinutes() - d.getTimezoneOffset());
            return d.toISOString().slice(0, 16);
        };

        self.closeFormModal = function() {
            self.isFormModalOpen(false);
            self.editingMeeting(null);
        };

        self.addParticipantToForm = function() {
            if (self.editingMeeting()) {
                self.editingMeeting().participants.push({
                    userId: ko.observable(''),
                    externalName: ko.observable(''),
                    externalEmail: ko.observable(''),
                    isRequired: ko.observable(true)
                });
            }
        };

        self.removeParticipantFromForm = function(participant) {
            if (self.editingMeeting()) {
                self.editingMeeting().participants.remove(participant);
            }
        };

        self.saveMeeting = function() {
            var item = self.editingMeeting();
            if (!item.title()) {
                toastr.warning('Toplantı başlığı gereklidir');
                return;
            }
            if (!item.plannedDate()) {
                toastr.warning('Başlangıç tarihi gereklidir');
                return;
            }

            var isNew = !item.id();
            var url = isNew ? '/api/meetings' : '/api/meetings/' + item.id();
            var method = isNew ? 'POST' : 'PUT';

            self.isSaving(true);

            var participants = item.participants().map(function(p) {
                return {
                    userId: p.userId() || null,
                    externalName: p.externalName() || null,
                    externalEmail: p.externalEmail() || null,
                    isRequired: p.isRequired()
                };
            }).filter(function(p) {
                return p.userId || p.externalName;
            });

            var data = {
                title: item.title(),
                description: item.description(),
                meetingType: item.meetingType(),
                status: item.status(),
                plannedDate: item.plannedDate(),
                plannedEndDate: item.plannedEndDate() || null,
                durationMinutes: item.durationMinutes() ? parseInt(item.durationMinutes()) : null,
                location: item.location(),
                isOnline: item.isOnline(),
                onlineLink: item.onlineLink(),
                onlinePlatform: item.onlinePlatform(),
                agenda: item.agenda(),
                notes: item.notes(),
                minutes: item.minutes(),
                decisions: item.decisions(),
                projectId: item.projectId() || null,
                reminderHoursBefore: item.reminderHoursBefore() || 24,
                participants: isNew ? participants : undefined
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
            .then(function(savedMeeting) {
                if (isNew) {
                    // Yeni kayit: array'e ekle
                    self.meetings.push(savedMeeting);
                    self.totalCount(self.totalCount() + 1);
                } else {
                    // Guncelleme: array'de bul ve guncelle
                    var list = self.meetings();
                    for (var i = 0; i < list.length; i++) {
                        if (list[i].id === savedMeeting.id) {
                            self.meetings.splice(i, 1, savedMeeting);
                            break;
                        }
                    }
                }
                self.closeFormModal();
                self.loadSummary();
                toastr.success(isNew ? 'Toplantı oluşturuldu' : 'Toplantı güncellendi');
            })
            .catch(function(error) {
                toastr.error(error.message || 'Bir hata oluştu');
            })
            .finally(function() {
                self.isSaving(false);
            });
        };

        // ========== DETAIL MODAL ==========

        self.showDetail = function(meeting) {
            self.currentMeetingId(meeting.id);
            self.loadMeetingDetail(meeting.id);
            self.isDetailModalOpen(true);
        };

        self.loadMeetingDetail = function(id) {
            fetch('/api/meetings/' + id)
                .then(function(response) { return response.json(); })
                .then(function(data) {
                    self.viewingMeeting(data);
                });
        };

        self.closeDetailModal = function() {
            self.isDetailModalOpen(false);
            self.viewingMeeting(null);
            self.currentMeetingId(null);
        };

        // ========== MEETING ACTIONS ==========

        self.startMeeting = function(meeting) {
            showConfirmModal({
                title: 'Toplantı Başlat',
                message: 'Toplantıyı başlatmak istediğinizden emin misiniz?',
                type: 'success',
                confirmText: 'Başlat',
                confirmIcon: 'bi-play-fill',
                onConfirm: function() {
                    fetch('/api/meetings/' + meeting.id + '/start', { method: 'POST' })
                        .then(function(response) {
                            if (response.ok) return response.json();
                            throw new Error('İşlem başarısız');
                        })
                        .then(function(updatedMeeting) {
                            // Array'de bul ve güncelle
                            var list = self.meetings();
                            for (var i = 0; i < list.length; i++) {
                                if (list[i].id === updatedMeeting.id) {
                                    self.meetings.splice(i, 1, updatedMeeting);
                                    break;
                                }
                            }
                            self.loadSummary();
                            if (self.currentMeetingId()) {
                                self.viewingMeeting(updatedMeeting);
                            }
                            toastr.success('Toplantı başlatıldı');
                        });
                }
            });
        };

        self.showCompleteMeetingModal = function(meeting) {
            self.currentMeetingId(meeting.id);
            self.completeData.minutes('');
            self.completeData.decisions('');
            self.completeData.notes('');
            self.isCompleteModalOpen(true);
        };

        self.closeCompleteModal = function() {
            self.isCompleteModalOpen(false);
        };

        self.submitComplete = function() {
            var data = {
                minutes: self.completeData.minutes() || null,
                decisions: self.completeData.decisions() || null,
                notes: self.completeData.notes() || null,
                attendances: []
            };

            fetch('/api/meetings/' + self.currentMeetingId() + '/complete', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(data)
            })
            .then(function(response) {
                if (response.ok) return response.json();
                throw new Error('İşlem başarısız');
            })
            .then(function(updatedMeeting) {
                // Array'de bul ve güncelle
                var list = self.meetings();
                for (var i = 0; i < list.length; i++) {
                    if (list[i].id === updatedMeeting.id) {
                        self.meetings.splice(i, 1, updatedMeeting);
                        break;
                    }
                }
                self.closeCompleteModal();
                self.closeDetailModal();
                self.loadSummary();
                toastr.success('Toplantı tamamlandı');
            });
        };

        self.cancelMeeting = function(meeting) {
            showConfirmModal({
                title: 'Toplantı İptali',
                message: 'Bu toplantıyı iptal etmek istediğinizden emin misiniz?',
                type: 'warning',
                confirmText: 'İptal Et',
                confirmIcon: 'bi-x-circle',
                onConfirm: function() {
                    fetch('/api/meetings/' + meeting.id + '/cancel', {
                        method: 'POST',
                        headers: { 'Content-Type': 'application/json' },
                        body: JSON.stringify({ reason: null })
                    })
                    .then(function(response) {
                        if (response.ok) return response.json();
                        throw new Error('İşlem başarısız');
                    })
                    .then(function(updatedMeeting) {
                        // Array'de bul ve güncelle
                        var list = self.meetings();
                        for (var i = 0; i < list.length; i++) {
                            if (list[i].id === updatedMeeting.id) {
                                self.meetings.splice(i, 1, updatedMeeting);
                                break;
                            }
                        }
                        self.loadSummary();
                        if (self.currentMeetingId()) {
                            self.viewingMeeting(updatedMeeting);
                        }
                        toastr.success('Toplantı iptal edildi');
                    });
                }
            });
        };

        self.deleteMeeting = function(meeting) {
            showDeleteConfirm('Bu toplantı', function() {
                fetch('/api/meetings/' + meeting.id, { method: 'DELETE' })
                    .then(function(response) {
                        if (response.ok) {
                            // Array'den sil
                            self.meetings.remove(function(m) { return m.id === meeting.id; });
                            self.totalCount(self.totalCount() - 1);
                            self.loadSummary();
                            toastr.success('Toplantı silindi');
                        }
                    });
            });
        };

        // ========== FILE UPLOAD ==========

        self.showUploadModal = function(meeting) {
            self.currentMeetingId(meeting.id);
            self.uploadDescription('');
            var fileInput = document.getElementById('uploadFileInput');
            if (fileInput) fileInput.value = '';
            self.isUploadModalOpen(true);
        };

        self.closeUploadModal = function() {
            self.isUploadModalOpen(false);
        };

        self.uploadFile = function() {
            var fileInput = document.getElementById('uploadFileInput');
            if (!fileInput.files || fileInput.files.length === 0) {
                toastr.warning('Lütfen bir dosya seçin');
                return;
            }

            self.isUploading(true);

            var formData = new FormData();
            formData.append('file', fileInput.files[0]);
            formData.append('description', self.uploadDescription() || '');

            fetch('/api/meetings/' + self.currentMeetingId() + '/attachments', {
                method: 'POST',
                body: formData
            })
            .then(function(response) {
                if (response.ok) {
                    self.closeUploadModal();
                    self.loadMeetingDetail(self.currentMeetingId());
                    toastr.success('Dosya yüklendi');
                }
            })
            .finally(function() {
                self.isUploading(false);
            });
        };

        self.deleteAttachment = function(meeting, attachment) {
            showDeleteConfirm('Bu dosya', function() {
                fetch('/api/meetings/' + meeting.id + '/attachments/' + attachment.id, {
                    method: 'DELETE'
                })
                .then(function(response) {
                    if (response.ok) {
                        self.loadMeetingDetail(meeting.id);
                        toastr.success('Dosya silindi');
                    }
                });
            });
        };

        // ========== PAGINATION ==========

        self.previousPage = function() {
            if (self.currentPage() > 1) {
                self.currentPage(self.currentPage() - 1);
                self.loadMeetings();
            }
        };

        self.nextPage = function() {
            if (self.currentPage() < self.totalPages()) {
                self.currentPage(self.currentPage() + 1);
                self.loadMeetings();
            }
        };

        self.goToPage = function(page) {
            self.currentPage(page);
            self.loadMeetings();
        };

        // ========== INITIALIZE ==========

        self.init = function() {
            // Önce EnumsService'i yükle, sonra diğer verileri çek
            EnumsService.load().then(function() {
                self.loadMeetings();
                self.loadSummary();
                self.loadProjects();
                self.loadUsers();
            });
        };

        self.init();
    }

    // CORRECT BINDING - Specific element
    $(document).ready(function() {
        ko.applyBindings(new MeetingsViewModel(), document.getElementById('meetings-app'));
    });
})();
