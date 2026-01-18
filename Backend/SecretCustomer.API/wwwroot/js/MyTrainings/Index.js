// MyTrainings Index ViewModel
var TRANSLATION_KEYS = [
    'Common.Loading',
    'Common.Error',
    'MyTrainings.ProgressSaved'
];

function MyTrainingsViewModel() {
    var self = this;

    // State
    self.isLoading = ko.observable(false);
    self.trainings = ko.observableArray([]);
    self.currentTraining = ko.observable(null);
    self.videoUrl = ko.observable('');
    self.watchedSeconds = ko.observable(0);
    self.isVideoCompleted = ko.observable(false);

    // Modal
    self.watchModal = null;
    self.videoPlayer = null;
    self.progressInterval = null;

    // Stats computed
    self.stats = ko.computed(function() {
        var list = self.trainings();
        return {
            pending: list.filter(function(t) { return t.statusId === 1; }).length,
            inProgress: list.filter(function(t) { return t.statusId === 2; }).length,
            completed: list.filter(function(t) { return t.statusId === 3 || t.isCompleted; }).length,
            overdue: list.filter(function(t) { return t.isOverdue && !t.isCompleted; }).length
        };
    });

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
        var secs = Math.floor(seconds % 60);
        return mins + ':' + (secs < 10 ? '0' : '') + secs;
    };

    // Get progress percentage
    self.getProgressPercentage = function(training) {
        if (!training.videoDurationSeconds) return 0;
        return Math.min(100, Math.round((training.watchedSeconds / training.videoDurationSeconds) * 100));
    };

    // Load trainings
    self.loadTrainings = function() {
        self.isLoading(true);

        fetch('/api/my-trainings', { credentials: 'include' })
            .then(function(r) { return r.json(); })
            .then(function(data) {
                self.trainings(data);
            })
            .catch(function(err) {
                toastr.error(T('Common.Error', 'Hata olustu'));
                console.error(err);
            })
            .finally(function() {
                self.isLoading(false);
            });
    };

    // Watch video
    self.watchVideo = function(training) {
        self.currentTraining(training);
        self.videoUrl('/api/training-videos/' + training.videoId + '/stream');
        self.watchedSeconds(training.watchedSeconds || 0);
        self.isVideoCompleted(training.isCompleted);

        if (!self.watchModal) {
            self.watchModal = new bootstrap.Modal(document.getElementById('watchModal'));
        }
        self.watchModal.show();

        // Initialize video player after modal is shown
        setTimeout(function() {
            self.videoPlayer = document.getElementById('videoPlayer');
            if (self.videoPlayer) {
                // Jump to last position
                self.videoPlayer.currentTime = training.watchedSeconds || 0;

                // Track progress
                self.videoPlayer.addEventListener('timeupdate', self.onTimeUpdate);
                self.videoPlayer.addEventListener('ended', self.onVideoEnded);

                // Start progress save interval
                self.progressInterval = setInterval(self.saveProgress, 10000); // Save every 10 seconds
            }
        }, 500);
    };

    // On time update
    self.onTimeUpdate = function() {
        if (self.videoPlayer) {
            self.watchedSeconds(Math.floor(self.videoPlayer.currentTime));
        }
    };

    // On video ended
    self.onVideoEnded = function() {
        self.isVideoCompleted(true);
        self.saveProgress(true);
    };

    // Save progress
    self.saveProgress = function(isCompleted) {
        var training = self.currentTraining();
        if (!training) return;

        var dto = {
            watchedSeconds: self.watchedSeconds(),
            isCompleted: isCompleted === true || self.isVideoCompleted()
        };

        fetch('/api/my-trainings/' + training.participantId + '/progress', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(dto),
            credentials: 'include'
        })
        .then(function(r) { return r.json(); })
        .then(function(result) {
            if (result.success) {
                // Update local data
                var t = self.trainings().find(function(x) { return x.participantId === training.participantId; });
                if (t) {
                    t.watchedSeconds = self.watchedSeconds();
                    if (dto.isCompleted) {
                        t.isCompleted = true;
                        t.statusId = 3;
                    } else if (t.statusId === 1) {
                        t.statusId = 2;
                    }
                    self.trainings.valueHasMutated();
                }
            }
        })
        .catch(function(err) {
            console.error('Progress save error:', err);
        });
    };

    // Close watch modal
    self.closeWatchModal = function() {
        // Save final progress
        self.saveProgress();

        // Clean up
        if (self.progressInterval) {
            clearInterval(self.progressInterval);
            self.progressInterval = null;
        }

        if (self.videoPlayer) {
            self.videoPlayer.pause();
            self.videoPlayer.removeEventListener('timeupdate', self.onTimeUpdate);
            self.videoPlayer.removeEventListener('ended', self.onVideoEnded);
            self.videoPlayer.src = '';
        }

        self.currentTraining(null);
        self.videoUrl('');

        if (self.watchModal) {
            self.watchModal.hide();
        }

        // Reload to get updated data
        self.loadTrainings();
    };

    // Initialize
    self.loadTrainings();
}

// Apply bindings after translations loaded
$(document).ready(function() {
    Localization.loadKeys(TRANSLATION_KEYS).then(function() {
        ko.applyBindings(new MyTrainingsViewModel(), document.getElementById('my-trainings-app'));
    });
});
