// TrainingVideos Index ViewModel
var TRANSLATION_KEYS = [
    'Common.Loading',
    'Common.Error',
    'Common.Success',
    'Common.Confirm',
    'Common.Delete',
    'TrainingVideo.DeleteConfirm',
    'TrainingVideo.UploadSuccess',
    'TrainingVideo.UpdateSuccess',
    'TrainingVideo.DeleteSuccess'
];

function TrainingVideosViewModel() {
    var self = this;

    // State
    self.isLoading = ko.observable(false);
    self.isSaving = ko.observable(false);
    self.isUploading = ko.observable(false);
    self.videos = ko.observableArray([]);
    self.checklists = ko.observableArray([]);
    self.scopeOptions = ko.observable({ checklists: [], questionGroups: [], questions: [] });

    // Filters
    self.searchTerm = ko.observable('');
    self.filterStatus = ko.observable('');
    self.filterChecklistId = ko.observable('');

    // Upload form
    self.formTitle = ko.observable('');
    self.formDescription = ko.observable('');
    self.formScopes = ko.observableArray([]);
    self.formDurationSeconds = ko.observable(0);
    self.formThumbnailBlob = ko.observable(null);
    self.formThumbnailPreview = ko.observable('');

    // Edit form
    self.editId = ko.observable(null);
    self.editTitle = ko.observable('');
    self.editDescription = ko.observable('');
    self.editIsActive = ko.observable(true);
    self.editScopes = ko.observableArray([]);

    // Scope selection (hierarchical)
    self.newScopeChecklistId = ko.observable('');
    self.newScopeGroupName = ko.observable('');
    self.newScopeQuestionId = ko.observable('');

    // Preview
    self.previewTitle = ko.observable('');
    self.previewUrl = ko.observable('');

    // Modals
    self.uploadModal = null;
    self.editModal = null;
    self.previewModal = null;

    // Filtered question groups based on selected checklist
    self.filteredQuestionGroups = ko.computed(function() {
        var checklistId = self.newScopeChecklistId();
        if (!checklistId) return [];
        var allGroups = self.scopeOptions().questionGroups || [];
        return allGroups.filter(function(g) { return g.checklistId == checklistId; });
    });

    // Filtered questions based on selected checklist and/or group
    self.filteredQuestions = ko.computed(function() {
        var checklistId = self.newScopeChecklistId();
        var groupName = self.newScopeGroupName();
        if (!checklistId) return [];
        var allQuestions = self.scopeOptions().questions || [];
        return allQuestions.filter(function(q) {
            if (q.checklistId != checklistId) return false;
            if (groupName && q.groupName !== groupName) return false;
            return true;
        });
    });

    // Reset dependent dropdowns when checklist changes
    self.newScopeChecklistId.subscribe(function() {
        self.newScopeGroupName('');
        self.newScopeQuestionId('');
    });

    // Reset question when group changes
    self.newScopeGroupName.subscribe(function() {
        self.newScopeQuestionId('');
    });

    // Can add scope - just need a checklist selected
    self.canAddScope = ko.computed(function() {
        return !!self.newScopeChecklistId();
    });

    // Format duration
    self.formatDuration = function(seconds) {
        if (!seconds) return '0:00';
        var mins = Math.floor(seconds / 60);
        var secs = seconds % 60;
        return mins + ':' + (secs < 10 ? '0' : '') + secs;
    };

    // Get scope display text (hierarchical)
    self.getScopeDisplayText = function(scope) {
        var parts = [];

        // Checklist name
        var checklist = self.scopeOptions().checklists.find(function(c) { return c.id == scope.checklistId; });
        if (checklist) {
            parts.push(checklist.name);
        }

        // Question group name
        if (scope.questionGroupName) {
            parts.push(scope.questionGroupName);
        }

        // Question text
        if (scope.questionId) {
            var question = self.scopeOptions().questions.find(function(q) { return q.id == scope.questionId; });
            if (question) {
                parts.push(question.text.substring(0, 20) + '...');
            }
        }

        return parts.join(' > ') || 'Bilinmiyor';
    };

    // Load videos
    self.loadVideos = function() {
        self.isLoading(true);

        var params = new URLSearchParams();
        if (self.searchTerm()) params.append('searchTerm', self.searchTerm());
        if (self.filterStatus() !== '') params.append('isActive', self.filterStatus());
        if (self.filterChecklistId()) params.append('checklistIds', self.filterChecklistId());

        fetch('/api/training-videos?' + params.toString(), { credentials: 'include' })
            .then(function(r) { return r.json(); })
            .then(function(data) {
                self.videos(data);
            })
            .catch(function(err) {
                toastr.error(T('Common.Error', 'Hata olustu'));
                console.error(err);
            })
            .finally(function() {
                self.isLoading(false);
            });
    };

    // Load scope options
    self.loadScopeOptions = function() {
        fetch('/api/training-videos/scopes/options', { credentials: 'include' })
            .then(function(r) { return r.json(); })
            .then(function(data) {
                self.scopeOptions(data);
                self.checklists(data.checklists || []);
            });
    };

    // Clear filters
    self.clearFilters = function() {
        self.searchTerm('');
        self.filterStatus('');
        self.filterChecklistId('');
        self.loadVideos();
    };

    // Open upload modal
    self.openUploadModal = function() {
        self.formTitle('');
        self.formDescription('');
        self.formScopes([]);
        self.formDurationSeconds(0);
        self.formThumbnailBlob(null);
        self.formThumbnailPreview('');
        self.resetScopeSelection();
        document.getElementById('videoFile').value = '';

        if (!self.uploadModal) {
            self.uploadModal = new bootstrap.Modal(document.getElementById('uploadModal'));
        }
        self.uploadModal.show();
    };

    // Get video duration and thumbnail when file selected
    self.onVideoFileChange = function() {
        var fileInput = document.getElementById('videoFile');
        if (!fileInput.files || !fileInput.files[0]) {
            self.formDurationSeconds(0);
            self.formThumbnailBlob(null);
            self.formThumbnailPreview('');
            return;
        }

        var file = fileInput.files[0];
        var video = document.createElement('video');
        video.preload = 'metadata';
        video.muted = true;

        video.onloadedmetadata = function() {
            var duration = Math.floor(video.duration);
            self.formDurationSeconds(duration);
            console.log('Video suresi:', duration, 'saniye');

            // Seek to 1 second (or 10% of video) for thumbnail
            var seekTime = Math.min(1, duration * 0.1);
            video.currentTime = seekTime;
        };

        video.onseeked = function() {
            // Create thumbnail from video frame
            var canvas = document.createElement('canvas');
            canvas.width = 320;
            canvas.height = 180;
            var ctx = canvas.getContext('2d');
            ctx.drawImage(video, 0, 0, canvas.width, canvas.height);

            // Get preview URL for display
            self.formThumbnailPreview(canvas.toDataURL('image/jpeg', 0.7));

            // Convert to blob for upload
            canvas.toBlob(function(blob) {
                self.formThumbnailBlob(blob);
                console.log('Thumbnail olusturuldu');
            }, 'image/jpeg', 0.7);

            window.URL.revokeObjectURL(video.src);
        };

        video.onerror = function() {
            console.warn('Video metadata okunamadi');
            self.formDurationSeconds(0);
            self.formThumbnailBlob(null);
            self.formThumbnailPreview('');
        };

        video.src = URL.createObjectURL(file);
    };

    // Reset scope selection
    self.resetScopeSelection = function() {
        self.newScopeChecklistId('');
        self.newScopeGroupName('');
        self.newScopeQuestionId('');
    };

    // Determine scope type based on selections
    self.determineScopeType = function() {
        if (self.newScopeQuestionId()) return 3; // Question
        if (self.newScopeGroupName()) return 2;  // QuestionGroup
        return 1; // Checklist
    };

    // Add scope to upload form
    self.addScope = function() {
        var checklistId = parseInt(self.newScopeChecklistId());
        if (!checklistId) return;

        var scope = {
            scopeTypeId: self.determineScopeType(),
            checklistId: checklistId
        };

        if (self.newScopeGroupName()) {
            scope.questionGroupName = self.newScopeGroupName();
        }
        if (self.newScopeQuestionId()) {
            scope.questionId = parseInt(self.newScopeQuestionId());
        }

        self.formScopes.push(scope);
        self.resetScopeSelection();
    };

    // Remove scope from upload form
    self.removeScope = function(scope) {
        self.formScopes.remove(scope);
    };

    // Add scope to edit form
    self.addEditScope = function() {
        var checklistId = parseInt(self.newScopeChecklistId());
        if (!checklistId) return;

        var scope = {
            scopeTypeId: self.determineScopeType(),
            checklistId: checklistId
        };

        if (self.newScopeGroupName()) {
            scope.questionGroupName = self.newScopeGroupName();
        }
        if (self.newScopeQuestionId()) {
            scope.questionId = parseInt(self.newScopeQuestionId());
        }

        self.editScopes.push(scope);
        self.resetScopeSelection();
    };

    // Remove scope from edit form
    self.removeEditScope = function(scope) {
        self.editScopes.remove(scope);
    };

    // Upload video
    self.uploadVideo = function() {
        var fileInput = document.getElementById('videoFile');
        if (!fileInput.files || !fileInput.files[0]) {
            toastr.warning('Lutfen bir video dosyasi secin');
            return;
        }
        if (!self.formTitle()) {
            toastr.warning('Lutfen video basligi girin');
            return;
        }

        self.isUploading(true);

        var formData = new FormData();
        formData.append('videoFile', fileInput.files[0]);
        formData.append('title', self.formTitle());
        formData.append('description', self.formDescription() || '');
        formData.append('durationSeconds', self.formDurationSeconds() || 0);

        // Add thumbnail if available
        if (self.formThumbnailBlob()) {
            formData.append('thumbnailFile', self.formThumbnailBlob(), 'thumbnail.jpg');
        }

        // Add scopes
        self.formScopes().forEach(function(scope, index) {
            formData.append('scopes[' + index + '].scopeTypeId', scope.scopeTypeId);
            if (scope.checklistId) formData.append('scopes[' + index + '].checklistId', scope.checklistId);
            if (scope.questionGroupName) formData.append('scopes[' + index + '].questionGroupName', scope.questionGroupName);
            if (scope.questionId) formData.append('scopes[' + index + '].questionId', scope.questionId);
        });

        fetch('/api/training-videos', {
            method: 'POST',
            body: formData,
            credentials: 'include'
        })
        .then(function(r) { return r.json(); })
        .then(function(result) {
            if (result.id) {
                toastr.success(T('TrainingVideo.UploadSuccess', 'Video basariyla yuklendi'));
                self.uploadModal.hide();
                self.loadVideos();
            } else {
                toastr.error(result.message || T('Common.Error', 'Hata olustu'));
            }
        })
        .catch(function(err) {
            toastr.error(T('Common.Error', 'Hata olustu'));
            console.error(err);
        })
        .finally(function() {
            self.isUploading(false);
        });
    };

    // Open edit modal
    self.openEditModal = function(video) {
        self.editId(video.id);
        self.editTitle(video.title);
        self.editDescription(video.description || '');
        self.editIsActive(video.isActive);
        self.resetScopeSelection();

        // Load full video details to get scopes
        fetch('/api/training-videos/' + video.id, { credentials: 'include' })
            .then(function(r) { return r.json(); })
            .then(function(data) {
                self.editScopes(data.scopes || []);
            });

        if (!self.editModal) {
            self.editModal = new bootstrap.Modal(document.getElementById('editModal'));
        }
        self.editModal.show();
    };

    // Update video
    self.updateVideo = function() {
        if (!self.editTitle()) {
            toastr.warning('Lutfen video basligi girin');
            return;
        }

        self.isSaving(true);

        var dto = {
            title: self.editTitle(),
            description: self.editDescription(),
            isActive: self.editIsActive(),
            scopes: self.editScopes().map(function(s) {
                return {
                    scopeTypeId: s.scopeTypeId,
                    checklistId: s.checklistId || s.newChecklistId || null,
                    questionGroupName: s.questionGroupName || s.newGroupName || null,
                    questionId: s.questionId || s.newQuestionId || null
                };
            })
        };

        fetch('/api/training-videos/' + self.editId(), {
            method: 'PUT',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(dto),
            credentials: 'include'
        })
        .then(function(r) { return r.json(); })
        .then(function(result) {
            if (result.id) {
                toastr.success(T('TrainingVideo.UpdateSuccess', 'Video basariyla guncellendi'));
                self.editModal.hide();
                self.loadVideos();
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

    // Delete video
    self.deleteVideo = function(video) {
        if (!confirm(T('TrainingVideo.DeleteConfirm', 'Bu videoyu silmek istediginize emin misiniz?'))) return;

        fetch('/api/training-videos/' + video.id, {
            method: 'DELETE',
            credentials: 'include'
        })
        .then(function(r) { return r.json(); })
        .then(function(result) {
            if (result.message) {
                toastr.success(T('TrainingVideo.DeleteSuccess', 'Video silindi'));
                self.loadVideos();
            } else {
                toastr.error(result.message || T('Common.Error', 'Hata olustu'));
            }
        })
        .catch(function(err) {
            toastr.error(T('Common.Error', 'Hata olustu'));
            console.error(err);
        });
    };

    // Preview video
    self.previewVideo = function(video) {
        self.previewTitle(video.title);
        self.previewUrl('/api/training-videos/' + video.id + '/stream');

        if (!self.previewModal) {
            self.previewModal = new bootstrap.Modal(document.getElementById('previewModal'));
            // Stop video when modal closes
            document.getElementById('previewModal').addEventListener('hidden.bs.modal', function() {
                var player = document.getElementById('previewPlayer');
                if (player) {
                    player.pause();
                    player.src = '';
                }
            });
        }
        self.previewModal.show();
    };

    // Initialize
    self.loadVideos();
    self.loadScopeOptions();
}

// Apply bindings after translations loaded
$(document).ready(function() {
    Localization.loadKeys(TRANSLATION_KEYS).then(function() {
        ko.applyBindings(new TrainingVideosViewModel(), document.getElementById('training-videos-app'));
    });
});
