// TrainingVideo Assignments ViewModel
var TRANSLATION_KEYS = [
    'Common.Loading',
    'Common.Error',
    'Common.Success',
    'Common.Confirm',
    'Common.All',
    'TrainingVideo.DeleteConfirm',
    'TrainingVideo.CreateSuccess',
    'TrainingVideo.DeleteSuccess',
    'TrainingVideo.RemindersSent'
];

function AssignmentsViewModel() {
    var self = this;

    // State
    self.isLoading = ko.observable(false);
    self.isSaving = ko.observable(false);
    self.isLoadingPreview = ko.observable(false);
    self.isLoadingParticipants = ko.observable(false);
    self.assignments = ko.observableArray([]);
    self.videos = ko.observableArray([]);
    self.projects = ko.observableArray([]);
    self.customerPersonnel = ko.observableArray([]);
    self.participants = ko.observableArray([]);

    // Filters
    self.searchTerm = ko.observable('');
    self.filterVideoId = ko.observable('');
    self.filterStatus = ko.observable('');

    // Create form
    self.formTitle = ko.observable('');
    self.formVideoId = ko.observable('');
    self.formStartDate = ko.observable('');
    self.formDueDate = ko.observable('');
    self.formProjectId = ko.observable('');
    self.formScoreThreshold = ko.observable(70);
    self.formSourceStartDate = ko.observable('');
    self.formSourceEndDate = ko.observable('');
    self.formManualUserIds = ko.observableArray([]);
    self.previewResult = ko.observable(null);

    // Modals
    self.createModal = null;
    self.participantsModal = null;

    // Format date
    self.formatDate = function(dateStr) {
        if (!dateStr) return '-';
        var d = new Date(dateStr);
        return d.toLocaleDateString('tr-TR');
    };

    // Format duration
    self.formatDuration = function(seconds) {
        if (!seconds) return '0:00';
        var mins = Math.floor(seconds / 60);
        var secs = seconds % 60;
        return mins + ':' + (secs < 10 ? '0' : '') + secs;
    };

    // Load assignments
    self.loadAssignments = function() {
        self.isLoading(true);

        var params = new URLSearchParams();
        if (self.searchTerm()) params.append('searchTerm', self.searchTerm());
        if (self.filterVideoId()) params.append('videoIds', self.filterVideoId());
        if (self.filterStatus() !== '') params.append('isActive', self.filterStatus());

        fetch('/api/training-video-assignments?' + params.toString(), { credentials: 'include' })
            .then(function(r) { return r.json(); })
            .then(function(data) {
                self.assignments(data);
            })
            .catch(function(err) {
                toastr.error(T('Common.Error', 'Hata olustu'));
                console.error(err);
            })
            .finally(function() {
                self.isLoading(false);
            });
    };

    // Load videos
    self.loadVideos = function() {
        fetch('/api/training-videos', { credentials: 'include' })
            .then(function(r) { return r.json(); })
            .then(function(data) {
                self.videos(data);
            });
    };

    // Load projects
    self.loadProjects = function() {
        fetch('/api/projects', { credentials: 'include' })
            .then(function(r) { return r.json(); })
            .then(function(data) {
                self.projects(data);
            });
    };

    // Load customer personnel (for manual assignment)
    self.loadCustomerPersonnel = function() {
        fetch('/api/customer-personnel', { credentials: 'include' })
            .then(function(r) { return r.json(); })
            .then(function(data) {
                self.customerPersonnel(data);
            });
    };

    // Clear filters
    self.clearFilters = function() {
        self.searchTerm('');
        self.filterVideoId('');
        self.filterStatus('');
        self.loadAssignments();
    };

    // Open create modal
    self.openCreateModal = function() {
        self.formTitle('');
        self.formVideoId('');
        self.formStartDate('');
        self.formDueDate('');
        self.formProjectId('');
        self.formScoreThreshold(70);
        self.formSourceStartDate('');
        self.formSourceEndDate('');
        self.formManualUserIds([]);
        self.previewResult(null);

        if (!self.createModal) {
            self.createModal = new bootstrap.Modal(document.getElementById('createModal'));
        }
        self.createModal.show();
    };

    // Preview auto assignment
    self.previewAutoAssignment = function() {
        if (!self.formVideoId() || !self.formProjectId() || !self.formScoreThreshold() ||
            !self.formSourceStartDate() || !self.formSourceEndDate()) {
            toastr.warning('Lutfen tum alanlari doldurun');
            return;
        }

        self.isLoadingPreview(true);

        var dto = {
            trainingVideoId: parseInt(self.formVideoId()),
            projectId: parseInt(self.formProjectId()),
            scoreThreshold: parseFloat(self.formScoreThreshold()),
            sourceStartDate: self.formSourceStartDate(),
            sourceEndDate: self.formSourceEndDate()
        };

        fetch('/api/training-video-assignments/preview', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(dto),
            credentials: 'include'
        })
        .then(function(r) { return r.json(); })
        .then(function(data) {
            self.previewResult(data);
        })
        .catch(function(err) {
            toastr.error(T('Common.Error', 'Hata olustu'));
            console.error(err);
        })
        .finally(function() {
            self.isLoadingPreview(false);
        });
    };

    // Create assignment
    self.createAssignment = function() {
        if (!self.formTitle() || !self.formVideoId() || !self.formStartDate() || !self.formDueDate()) {
            toastr.warning('Lutfen zorunlu alanlari doldurun');
            return;
        }

        // En az bir atama yontemi secilmeli
        var hasAutoAssignment = self.formProjectId() && self.formScoreThreshold() && self.formSourceStartDate() && self.formSourceEndDate();
        var hasManualAssignment = self.formManualUserIds().length > 0;

        if (!hasAutoAssignment && !hasManualAssignment) {
            toastr.warning('Lutfen otomatik atama kriterleri girin veya manuel olarak kullanici secin');
            return;
        }

        self.isSaving(true);

        var dto = {
            title: self.formTitle(),
            trainingVideoId: parseInt(self.formVideoId()),
            startDate: self.formStartDate(),
            dueDate: self.formDueDate()
        };

        if (hasAutoAssignment) {
            dto.projectId = parseInt(self.formProjectId());
            dto.scoreThreshold = parseFloat(self.formScoreThreshold());
            dto.sourceStartDate = self.formSourceStartDate();
            dto.sourceEndDate = self.formSourceEndDate();
        }

        if (hasManualAssignment) {
            dto.manualUserIds = self.formManualUserIds().map(function(id) { return parseInt(id); });
        }

        fetch('/api/training-video-assignments', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(dto),
            credentials: 'include'
        })
        .then(function(r) { return r.json(); })
        .then(function(result) {
            if (result.id) {
                toastr.success(T('TrainingVideo.CreateSuccess', 'Atama basariyla olusturuldu'));
                self.createModal.hide();
                self.loadAssignments();
            } else {
                toastr.error(result.message || T('Common.Error', 'Hata olustu'));
            }
        })
        .catch(function(err) {
            toastr.error(T('Common.Error', 'Hata olustu'));
            console.error(err);
        })
        .finally(function() {
            self.isSaving(false);
        });
    };

    // Delete assignment
    self.deleteAssignment = function(assignment) {
        if (!confirm(T('TrainingVideo.DeleteConfirm', 'Bu atamayi silmek istediginize emin misiniz?'))) return;

        fetch('/api/training-video-assignments/' + assignment.id, {
            method: 'DELETE',
            credentials: 'include'
        })
        .then(function(r) { return r.json(); })
        .then(function(result) {
            if (result.message) {
                toastr.success(T('TrainingVideo.DeleteSuccess', 'Atama silindi'));
                self.loadAssignments();
            } else {
                toastr.error(result.message || T('Common.Error', 'Hata olustu'));
            }
        })
        .catch(function(err) {
            toastr.error(T('Common.Error', 'Hata olustu'));
            console.error(err);
        });
    };

    // View participants
    self.viewParticipants = function(assignment) {
        self.participants([]);
        self.isLoadingParticipants(true);

        if (!self.participantsModal) {
            self.participantsModal = new bootstrap.Modal(document.getElementById('participantsModal'));
        }
        self.participantsModal.show();

        fetch('/api/training-video-assignments/' + assignment.id + '/participants', { credentials: 'include' })
            .then(function(r) { return r.json(); })
            .then(function(data) {
                self.participants(data);
            })
            .catch(function(err) {
                toastr.error(T('Common.Error', 'Hata olustu'));
                console.error(err);
            })
            .finally(function() {
                self.isLoadingParticipants(false);
            });
    };

    // Send reminders
    self.sendReminders = function(assignment) {
        if (!confirm('Tamamlamamis katilimcilara hatirlatma gondermek istediginize emin misiniz?')) return;

        fetch('/api/training-video-assignments/' + assignment.id + '/send-reminders', {
            method: 'POST',
            credentials: 'include'
        })
        .then(function(r) { return r.json(); })
        .then(function(result) {
            toastr.success(T('TrainingVideo.RemindersSent', 'Hatirlatmalar gonderildi'));
        })
        .catch(function(err) {
            toastr.error(T('Common.Error', 'Hata olustu'));
            console.error(err);
        });
    };

    // Initialize
    self.loadAssignments();
    self.loadVideos();
    self.loadProjects();
    self.loadCustomerPersonnel();
}

// Apply bindings after translations loaded
$(document).ready(function() {
    Localization.loadKeys(TRANSLATION_KEYS).then(function() {
        ko.applyBindings(new AssignmentsViewModel(), document.getElementById('assignments-app'));
    });
});
