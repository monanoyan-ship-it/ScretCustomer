// CustomerPortal MyTrainings ViewModel
var TRANSLATION_KEYS = [
    'Common.Loading',
    'Common.Error',
    'MyTrainings.ProgressSaved',
    'MyTrainings.StartWatching',
    'MyTrainings.ConfirmWatchVideo',
    'MyTrainings.WatchCountWarning',
    'MyTrainings.CurrentWatchCount',
    'MyTrainings.TimesWatched',
    'MyTrainings.RemainingAfter',
    'MyTrainings.CloseVideo',
    'MyTrainings.CloseVideoWarning',
    'MyTrainings.CloseAndSave',
    'MyTrainings.Quiz.Title',
    'MyTrainings.Quiz.Required',
    'MyTrainings.Quiz.Optional',
    'MyTrainings.Quiz.Start',
    'MyTrainings.Quiz.Skip',
    'MyTrainings.Quiz.Submit',
    'MyTrainings.Quiz.Result',
    'MyTrainings.Quiz.Passed',
    'MyTrainings.Quiz.Failed',
    'MyTrainings.Quiz.TryAgain'
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
    self.activeTab = ko.observable('active');

    // Modal
    self.watchModal = null;
    self.videoPlayer = null;
    self.progressInterval = null;


    // Tab switch
    self.switchTab = function(tab) {
        self.activeTab(tab);
    };

    // Helper: Get date without time for comparison
    self.getDateOnly = function(dateStr) {
        var d = new Date(dateStr);
        return new Date(d.getFullYear(), d.getMonth(), d.getDate());
    };

    self.getTodayOnly = function() {
        var now = new Date();
        return new Date(now.getFullYear(), now.getMonth(), now.getDate());
    };

    // Filtered trainings by tab
    self.activeTrainings = ko.computed(function() {
        var today = self.getTodayOnly();
        return self.trainings().filter(function(t) {
            var startDate = self.getDateOnly(t.startDate);
            var dueDate = self.getDateOnly(t.dueDate);
            return startDate <= today && dueDate >= today;
        });
    });

    self.upcomingTrainings = ko.computed(function() {
        var today = self.getTodayOnly();
        return self.trainings().filter(function(t) {
            var startDate = self.getDateOnly(t.startDate);
            return startDate > today;
        });
    });

    self.pastTrainings = ko.computed(function() {
        var today = self.getTodayOnly();
        return self.trainings().filter(function(t) {
            var dueDate = self.getDateOnly(t.dueDate);
            return dueDate < today;
        });
    });

    self.filteredTrainings = ko.computed(function() {
        var tab = self.activeTab();
        switch (tab) {
            case 'active': return self.activeTrainings();
            case 'upcoming': return self.upcomingTrainings();
            case 'past': return self.pastTrainings();
            default: return self.trainings();
        }
    });

    // Stats computed (only for active tab)
    self.stats = ko.computed(function() {
        var list = self.activeTrainings();
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

    // Load trainings - CustomerPortal API kullanir
    self.loadTrainings = function() {
        self.isLoading(true);

        customerApiFetch('/api/customer/portal/my-trainings')
            .then(function(r) {
                console.log('MyTrainings API response status:', r.status);
                return r.json();
            })
            .then(function(data) {
                console.log('MyTrainings API data:', data);
                // Her training icin quiz status ekle
                data.forEach(function(t) {
                    t.quizStatus = null; // Baslangicta null
                });
                self.trainings(data);
                // Quiz durumlarini yukle
                self.loadQuizStatuses();
            })
            .catch(function(err) {
                toastr.error(T('Common.Error', 'Hata olustu'));
                console.error('MyTrainings API error:', err);
            })
            .finally(function() {
                self.isLoading(false);
            });
    };

    // Her training icin quiz durumunu yukle
    self.loadQuizStatuses = function() {
        self.trainings().forEach(function(training) {
            customerApiFetch('/api/training-videos/participant/' + training.participantId + '/quiz')
                .then(function(r) {
                    if (r.status === 404) return null;
                    return r.json();
                })
                .then(function(data) {
                    if (data) {
                        training.quizStatus = {
                            hasQuiz: true,
                            quizId: data.quizId,
                            isRequired: data.isRequired,
                            isPassed: data.lastAttemptResult && data.lastAttemptResult.isPassed,
                            lastScore: data.lastAttemptResult ? data.lastAttemptResult.scorePercentage : null
                        };
                    } else {
                        training.quizStatus = { hasQuiz: false };
                    }
                    self.trainings.valueHasMutated();
                })
                .catch(function() {
                    training.quizStatus = { hasQuiz: false };
                });
        });
    };

    // Start watch session - video açıldığında izleme hakkını kullan
    self.startWatchSession = function(training) {
        customerApiFetch('/api/customer/portal/my-trainings/' + training.participantId + '/start-session', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' }
        })
        .then(function(r) { return r.json(); })
        .then(function(result) {
            if (result.success) {
                // Local veriyi güncelle
                training.watchCount = result.watchCount;
                self.trainings.valueHasMutated();

                if (result.remainingWatches !== null && result.remainingWatches <= 2) {
                    toastr.info('Kalan izleme hakkiniz: ' + result.remainingWatches);
                }
            }
        })
        .catch(function(err) {
            console.error('Start session error:', err);
        });
    };

    // Watch video
    self.lastAllowedTime = ko.observable(0);

    // Actual video opening logic (called after confirmation)
    self.openVideoPlayer = function(training) {
        self.currentTraining(training);
        self.videoUrl('/api/training-videos/' + training.videoId + '/stream');
        self.watchedSeconds(0);
        self.lastAllowedTime(0);
        self.isVideoCompleted(training.isCompleted);

        // Video açıldığında izleme hakkını kullan
        self.startWatchSession(training);

        if (!self.watchModal) {
            self.watchModal = new bootstrap.Modal(document.getElementById('watchModal'), {
                backdrop: 'static',
                keyboard: false
            });
        }
        self.watchModal.show();

        // Initialize video player after modal is shown
        setTimeout(function() {
            self.videoPlayer = document.getElementById('videoPlayer');
            if (self.videoPlayer) {
                // Her zaman baştan başla
                self.videoPlayer.currentTime = 0;

                // Apply watching rules
                self.applyWatchingRules(training);

                // Track progress
                self.videoPlayer.addEventListener('timeupdate', self.onTimeUpdate);
                self.videoPlayer.addEventListener('ended', self.onVideoEnded);
                self.videoPlayer.addEventListener('seeking', self.onSeeking);

                // Start progress save interval
                self.progressInterval = setInterval(self.saveProgress, 10000); // Save every 10 seconds
            }
        }, 500);
    };

    self.watchVideo = function(training) {
        // Max watch count kontrolü
        if (training.maxWatchCount && training.watchCount >= training.maxWatchCount) {
            toastr.warning('Bu video icin izleme hakkiniz dolmustur. (' + training.watchCount + '/' + training.maxWatchCount + ')');
            return;
        }

        // İzleme hakkı uyarısı
        var remainingAfter = training.maxWatchCount ? (training.maxWatchCount - training.watchCount - 1) : null;
        var confirmTitle = T('MyTrainings.StartWatching', 'Videoyu Izle');
        var confirmMsg = T('MyTrainings.ConfirmWatchVideo', 'Videoyu izlemeye baslamak istiyor musunuz?');

        if (remainingAfter !== null) {
            confirmMsg = T('MyTrainings.WatchCountWarning', 'Bu videoyu izlemeye basladiginizda 1 izleme hakkiniz kullanilacak.');
            confirmMsg += '<br><br>';
            confirmMsg += '<strong>' + T('MyTrainings.CurrentWatchCount', 'Mevcut') + ':</strong> ' + training.watchCount + '/' + training.maxWatchCount + ' ' + T('MyTrainings.TimesWatched', 'izleme');
            confirmMsg += '<br>';
            confirmMsg += '<strong>' + T('MyTrainings.RemainingAfter', 'Kalan hak') + ':</strong> ' + remainingAfter;
        }

        showConfirmModal({
            title: confirmTitle,
            message: confirmMsg,
            type: remainingAfter !== null && remainingAfter <= 1 ? 'warning' : 'info',
            confirmText: T('MyTrainings.StartWatching', 'Izlemeye Basla'),
            confirmIcon: 'bi-play-fill',
            onConfirm: function() {
                self.openVideoPlayer(training);
            }
        });
    };

    // Apply watching rules (skip, speed, etc.)
    self.applyWatchingRules = function(training) {
        if (!self.videoPlayer) return;

        // Hız değiştirme kontrolü (Assignment'tan allowSpeedChange)
        if (!training.allowSpeedChange) {
            // Hızı 1x'e sabitle
            self.videoPlayer.playbackRate = 1.0;

            // Hız değişikliğini tamamen engelle
            self.videoPlayer.addEventListener('ratechange', function() {
                if (self.videoPlayer.playbackRate !== 1.0) {
                    self.videoPlayer.playbackRate = 1.0;
                    toastr.warning('Hiz degistirme devre disi.');
                }
            });
        } else {
            // Max playback speed kontrolü (Video'dan maxPlaybackSpeed)
            var maxSpeed = training.maxPlaybackSpeed || 2.0;
            self.videoPlayer.playbackRate = Math.min(self.videoPlayer.playbackRate, maxSpeed);

            self.videoPlayer.addEventListener('ratechange', function() {
                if (self.videoPlayer.playbackRate > maxSpeed) {
                    self.videoPlayer.playbackRate = maxSpeed;
                    toastr.warning('Maksimum hiz: ' + maxSpeed + 'x');
                }
            });
        }

        // Controls'u gizle veya göster
        // allowSeeking false ise ve allowSkipping false ise progress bar'ı gizle
        if (!training.allowSeeking && !training.allowSkipping) {
            // CSS ile progress bar'ı gizle
            self.videoPlayer.style.setProperty('--seek-before-width', '0%');
        }
    };

    // Seek (ileri/geri sarma) kontrolü
    self.onSeeking = function() {
        var training = self.currentTraining();
        if (!training || !self.videoPlayer) return;

        // Tamamlanmış videolarda serbest hareket
        if (training.isCompleted) return;

        // AllowSeeking kontrolü (Assignment'tan) - false ise ileri/geri sarma yasak
        // AllowSkipping kontrolü (Video'dan) - false ise atlama yasak
        var canSeek = training.allowSeeking !== false && training.allowSkipping !== false;

        if (!canSeek) {
            var currentTime = self.videoPlayer.currentTime;
            var allowedTime = self.lastAllowedTime();

            // İleri sarma engeli - sadece izlenen yere kadar gidilebilir
            if (currentTime > allowedTime + 2) {
                self.videoPlayer.currentTime = allowedTime;
                toastr.warning('Ileri sarma devre disi. Videoyu sirasiyla izlemelisiniz.');
            }

            // Geri sarma da engellenebilir (allowSeeking false ise)
            if (!training.allowSeeking && currentTime < allowedTime - 5) {
                // Geri sarmaya izin verme, mevcut konuma dön
                // Aslında geri sarma genelde izin verilir, sadece ileri sarma engellenir
            }
        }
    };

    // On time update
    self.onTimeUpdate = function() {
        if (self.videoPlayer) {
            var currentTime = Math.floor(self.videoPlayer.currentTime);
            self.watchedSeconds(currentTime);

            // İzlenen en ileri noktayı güncelle (ileri sarma engeli için)
            if (currentTime > self.lastAllowedTime()) {
                self.lastAllowedTime(currentTime);
            }
        }
    };

    // On video ended
    self.onVideoEnded = function() {
        self.isVideoCompleted(true);
        self.saveProgress(true);

        // Quiz kontrolü - video tamamlandıktan sonra
        self.checkAndShowQuiz();
    };

    // ===== Quiz Functions =====

    // Video bittiginde quiz kontrolu ve yonlendirme
    self.checkAndShowQuiz = function() {
        var training = self.currentTraining();
        if (!training) return;

        customerApiFetch('/api/training-videos/participant/' + training.participantId + '/quiz')
            .then(function(r) {
                if (r.status === 404) {
                    // Quiz yok - video tamamlandi mesaji goster
                    toastr.success('Video tamamlandi!');
                    return null;
                }
                return r.json();
            })
            .then(function(data) {
                if (!data) return;

                // Quiz zaten gecilmis mi?
                if (data.lastAttemptResult && data.lastAttemptResult.isPassed) {
                    toastr.success('Video ve anket basariyla tamamlandi!');
                    return;
                }

                // Quiz zorunlu mu?
                if (data.isRequired) {
                    // Zorunlu - direkt yonlendir
                    toastr.info('Ankete yonlendiriliyorsunuz...');
                    setTimeout(function() {
                        window.location.href = '/CustomerPortal/TrainingQuiz/' + training.participantId;
                    }, 1500);
                } else {
                    // Opsiyonel - kullaniciya sor
                    showConfirmModal({
                        title: T('MyTrainings.Quiz.Title', 'Anket'),
                        message: T('MyTrainings.Quiz.Optional', 'Bu video icin opsiyonel bir anket mevcut. Anketi simdi doldurmak ister misiniz?'),
                        type: 'info',
                        confirmText: T('MyTrainings.Quiz.Start', 'Ankete Git'),
                        cancelText: T('MyTrainings.Quiz.Skip', 'Sonra'),
                        onConfirm: function() {
                            window.location.href = '/CustomerPortal/TrainingQuiz/' + training.participantId;
                        }
                    });
                }
            })
            .catch(function(err) {
                console.error('Quiz check error:', err);
            });
    };

    // Save progress - CustomerPortal API kullanir
    self.saveProgress = function(isCompleted) {
        var training = self.currentTraining();
        if (!training) return;

        var dto = {
            watchedSeconds: self.watchedSeconds(),
            isCompleted: isCompleted === true || self.isVideoCompleted()
        };

        customerApiFetch('/api/customer/portal/my-trainings/' + training.participantId + '/progress', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(dto)
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

    // Actual close logic
    self.doCloseModal = function() {
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
            self.videoPlayer.removeEventListener('seeking', self.onSeeking);
            self.videoPlayer.src = '';
        }

        self.currentTraining(null);
        self.videoUrl('');
        self.lastAllowedTime(0);

        if (self.watchModal) {
            self.watchModal.hide();
        }

        // Reload to get updated data
        self.loadTrainings();
    };

    // Close watch modal with confirmation
    self.closeWatchModal = function() {
        var training = self.currentTraining();

        // Video tamamlandıysa direkt kapat
        if (self.isVideoCompleted()) {
            self.doCloseModal();
            return;
        }

        var confirmTitle = T('MyTrainings.CloseVideo', 'Videoyu Kapat');
        var confirmMsg = T('MyTrainings.CloseVideoWarning', 'Videoyu kapatmak istediginize emin misiniz? Ilerlemeniz kaydedilecektir.');

        showConfirmModal({
            title: confirmTitle,
            message: confirmMsg,
            type: 'warning',
            confirmText: T('MyTrainings.CloseAndSave', 'Kapat ve Kaydet'),
            confirmIcon: 'bi-x-circle',
            onConfirm: function() {
                self.doCloseModal();
            }
        });
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
