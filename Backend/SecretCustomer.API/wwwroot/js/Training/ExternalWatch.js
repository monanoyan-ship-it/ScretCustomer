// Dış Katılımcı Video İzleme - Training/External
(function () {
    'use strict';

    function ExternalWatchViewModel() {
        var self = this;

        // State
        self.isLoading = ko.observable(true);
        self.hasError = ko.observable(false);
        self.errorMessage = ko.observable('');
        self.videoInfo = ko.observable(null);

        // Video player reference
        self.videoPlayer = null;
        self.lastSavedTime = 0;
        self.saveInterval = null;
        self.videoSrc = ko.observable('');

        // Quiz State
        self.quizStatus = ko.observable(null);

        // Computed
        self.watchPercentage = ko.computed(function () {
            var info = self.videoInfo();
            if (!info || !info.videoDurationSeconds) return 0;
            return Math.min(100, Math.round((info.watchedSeconds / info.videoDurationSeconds) * 100));
        });

        // ========== INITIALIZATION ==========

        self.init = function () {
            if (!VIDEO_TOKEN) {
                self.hasError(true);
                self.errorMessage('Gecersiz link');
                self.isLoading(false);
                return;
            }

            self.loadVideoInfo();
        };

        self.loadVideoInfo = function () {
            $.ajax({
                url: '/api/training-video-assignments/external/' + VIDEO_TOKEN,
                method: 'GET',
                success: function (data) {
                    self.videoInfo(data);
                    self.videoSrc(data.videoStreamUrl || '');
                    self.isLoading(false);

                    // Quiz durumunu yukle
                    self.loadQuizStatus();

                    // Setup video player after data loads
                    setTimeout(function () {
                        self.setupVideoPlayer();
                    }, 100);
                },
                error: function (xhr) {
                    self.hasError(true);
                    self.errorMessage(xhr.responseJSON?.message || 'Video bilgisi alinamadi');
                    self.isLoading(false);
                }
            });
        };

        // ========== VIDEO PLAYER ==========

        self.setupVideoPlayer = function () {
            self.videoPlayer = document.getElementById('videoPlayer');
            if (!self.videoPlayer) return;

            var info = self.videoInfo();

            // Disable speed change if not allowed
            if (!info.allowSpeedChange) {
                self.videoPlayer.playbackRate = 1.0;
                self.videoPlayer.addEventListener('ratechange', function () {
                    if (self.videoPlayer.playbackRate !== 1.0) {
                        self.videoPlayer.playbackRate = 1.0;
                        toastr.warning('Video hizi degistirilemez');
                    }
                });
            }

            // Disable seeking if not allowed
            if (!info.allowSeeking) {
                var lastTime = 0;
                self.videoPlayer.addEventListener('timeupdate', function () {
                    // Allow small jumps (buffering) but prevent large seeks
                    if (Math.abs(self.videoPlayer.currentTime - lastTime) > 2 && !self.videoPlayer.seeking) {
                        // Normal playback
                        lastTime = self.videoPlayer.currentTime;
                    }
                });

                self.videoPlayer.addEventListener('seeking', function () {
                    // Only allow seeking forward if they've already watched
                    var currentWatched = info.watchedSeconds || 0;
                    if (self.videoPlayer.currentTime > currentWatched + 5) {
                        self.videoPlayer.currentTime = Math.min(lastTime, currentWatched);
                        toastr.warning('Video ileri sarilamaz');
                    }
                });
            }

            // Track progress
            self.videoPlayer.addEventListener('timeupdate', function () {
                var currentTime = Math.floor(self.videoPlayer.currentTime);

                // Save progress every 10 seconds
                if (currentTime - self.lastSavedTime >= 10) {
                    self.saveProgress(currentTime, false);
                    self.lastSavedTime = currentTime;
                }
            });

            // Video ended
            self.videoPlayer.addEventListener('ended', function () {
                var duration = Math.floor(self.videoPlayer.duration);
                self.saveProgress(duration, true);
            });

            // Auto-save on page unload
            window.addEventListener('beforeunload', function () {
                if (self.videoPlayer) {
                    var currentTime = Math.floor(self.videoPlayer.currentTime);
                    self.saveProgressSync(currentTime, false);
                }
            });

            // Resume from last position
            if (info.watchedSeconds > 0 && !info.isCompleted) {
                self.videoPlayer.currentTime = Math.max(0, info.watchedSeconds - 5);
            }
        };

        // ========== PROGRESS SAVING ==========

        self.saveProgress = function (watchedSeconds, isVideoEnded) {
            $.ajax({
                url: '/api/training-video-assignments/external/' + VIDEO_TOKEN + '/progress',
                method: 'POST',
                contentType: 'application/json',
                data: JSON.stringify({
                    watchedSeconds: watchedSeconds,
                    isCompleted: false,
                    isVideoEnded: isVideoEnded
                }),
                success: function () {
                    // Update local info
                    var info = self.videoInfo();
                    if (info) {
                        info.watchedSeconds = Math.max(info.watchedSeconds, watchedSeconds);
                        if (isVideoEnded) {
                            info.currentWatchCount++;
                        }
                        self.videoInfo(info);
                        self.videoInfo.valueHasMutated();

                        // Check if completed
                        if (isVideoEnded) {
                            self.checkCompletion();
                        }
                    }
                }
            });
        };

        self.saveProgressSync = function (watchedSeconds, isVideoEnded) {
            // Synchronous save for beforeunload
            var xhr = new XMLHttpRequest();
            xhr.open('POST', '/api/training-video-assignments/external/' + VIDEO_TOKEN + '/progress', false);
            xhr.setRequestHeader('Content-Type', 'application/json');
            xhr.send(JSON.stringify({
                watchedSeconds: watchedSeconds,
                isCompleted: false,
                isVideoEnded: isVideoEnded
            }));
        };

        self.checkCompletion = function () {
            // Reload video info to get updated completion status
            $.ajax({
                url: '/api/training-video-assignments/external/' + VIDEO_TOKEN,
                method: 'GET',
                success: function (data) {
                    self.videoInfo(data);
                    if (data.isCompleted) {
                        // Quiz kontrolu ve yonlendirme
                        self.checkAndRedirectToQuiz();
                    }
                }
            });
        };

        // ========== QUIZ FUNCTIONS ==========

        self.loadQuizStatus = function () {
            $.ajax({
                url: '/api/training-video-assignments/external/' + VIDEO_TOKEN + '/quiz',
                method: 'GET',
                success: function (data) {
                    var isPassed = data.lastAttemptResult && data.lastAttemptResult.isPassed;
                    self.quizStatus({
                        hasQuiz: true,
                        quizId: data.quizId,
                        isRequired: data.isRequired,
                        isPassed: isPassed,
                        lastScore: data.lastAttemptResult ? data.lastAttemptResult.scorePercentage : null
                    });

                    // Sayfa yuklenduginde: Video tamamlanmis + Quiz bekliyor = Modal goster
                    var info = self.videoInfo();
                    if (info && info.isCompleted && !isPassed) {
                        self.showCompletedChoiceModal();
                    }
                },
                error: function (xhr) {
                    // Quiz yok
                    self.quizStatus({ hasQuiz: false });
                }
            });
        };

        self.showCompletedChoiceModal = function () {
            var modal = new bootstrap.Modal(document.getElementById('completedChoiceModal'));
            modal.show();
        };

        self.checkAndRedirectToQuiz = function () {
            $.ajax({
                url: '/api/training-video-assignments/external/' + VIDEO_TOKEN + '/quiz',
                method: 'GET',
                success: function (data) {
                    // Quiz zaten gecilmis mi?
                    if (data.lastAttemptResult && data.lastAttemptResult.isPassed) {
                        toastr.success('Video ve anket basariyla tamamlandi!');
                        self.loadQuizStatus();
                        return;
                    }

                    // Quiz varsa modal goster
                    self.quizStatus({
                        hasQuiz: true,
                        quizId: data.quizId,
                        isRequired: data.isRequired,
                        isPassed: false,
                        lastScore: null
                    });
                    self.showCompletedChoiceModal();
                },
                error: function (xhr) {
                    // Quiz yok - normal tamamlandi
                    if (xhr.status === 404) {
                        toastr.success('Video basariyla tamamlandi!');
                    }
                }
            });
        };

        // ========== HELPERS ==========

        self.formatDate = function (dateStr) {
            if (!dateStr) return '-';
            var date = new Date(dateStr);
            return date.toLocaleDateString('tr-TR');
        };

        // Initialize
        self.init();
    }

    // Apply bindings
    $(function () {
        ko.applyBindings(new ExternalWatchViewModel(), document.getElementById('training-app'));
    });
})();
