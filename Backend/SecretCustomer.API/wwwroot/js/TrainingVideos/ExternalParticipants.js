// Dış Katılımcılar Popup - TrainingVideos/ExternalParticipants
(function () {
    'use strict';

    function ExternalParticipantsViewModel() {
        var self = this;

        // Mode
        self.isNewMode = ko.observable(IS_NEW_MODE);
        self.assignmentId = ko.observable(ASSIGNMENT_ID);
        self.assignmentTitle = ko.observable('');

        // Loading States
        self.isLoading = ko.observable(true);
        self.isLoadingParticipants = ko.observable(false);
        self.isAdding = ko.observable(false);
        self.isSendingEmails = ko.observable(false);
        self.isCreating = ko.observable(false);

        // Videos (for new mode)
        self.videos = ko.observableArray([]);

        // Email Templates
        self.emailTemplates = ko.observableArray([]);
        self.formEmailTemplateId = ko.observable(null);

        // Form Fields (for new mode)
        self.formVideoId = ko.observable(null);
        self.formTitle = ko.observable('');
        self.formStartDate = ko.observable('');
        self.formDueDate = ko.observable('');
        self.formMinWatchCount = ko.observable(1);
        self.formMaxWatchCount = ko.observable(null);
        self.formAllowSpeedChange = ko.observable(false);
        self.formAllowSeeking = ko.observable(false);

        // Add Mode
        self.addMode = ko.observable('text');
        self.textInput = ko.observable('');
        self.parsedFromFile = ko.observableArray([]);
        self.sendEmailOnAdd = ko.observable(true);

        // Participants List (for existing mode)
        self.participants = ko.observableArray([]);

        // Selection
        self.selectAll = ko.observable(false);

        self.selectedCount = ko.computed(function () {
            return self.participants().filter(function (p) { return p.selected(); }).length;
        });

        // Select All toggle
        self.selectAll.subscribe(function (newValue) {
            self.participants().forEach(function (p) {
                p.selected(newValue);
            });
        });

        // Parse text input helper (defined early for computed)
        // Esnek format: satırda @ içeren kelimeyi email olarak al, geri kalanı ad/soyad
        self.parseTextInputForPreview = function () {
            var text = self.textInput();
            if (!text) return [];

            var lines = text.split('\n');
            var participants = [];
            var emailRegex = /[^\s;,]+@[^\s;,]+\.[^\s;,]+/;

            lines.forEach(function (line) {
                line = line.trim();
                if (!line) return;

                // Satırda email bul
                var emailMatch = line.match(emailRegex);
                if (!emailMatch) return;

                var email = emailMatch[0].trim();

                // Email'i satırdan çıkar, kalan kısımları ad/soyad olarak al
                var remaining = line.replace(email, '').trim();
                var parts = remaining.split(/[;,\s]+/).filter(function(p) { return p.length > 0; });

                participants.push({
                    email: email,
                    firstName: parts[0] || null,
                    lastName: parts[1] || null
                });
            });

            return participants;
        };

        // Parsed participants (for preview in new mode)
        self.parsedParticipants = ko.computed(function () {
            if (self.addMode() === 'excel') {
                return self.parsedFromFile();
            }
            return self.parseTextInputForPreview();
        });

        // Can create assignment
        self.canCreate = ko.computed(function () {
            var reasons = [];
            if (!self.isNewMode()) reasons.push('not new mode');
            if (!self.formVideoId()) reasons.push('no video');
            if (!self.formTitle()) reasons.push('no title');
            if (!self.formStartDate()) reasons.push('no start date');
            if (!self.formDueDate()) reasons.push('no due date');
            if (self.parsedParticipants().length === 0) reasons.push('no participants');

            if (reasons.length > 0) {
                console.log('canCreate false:', reasons.join(', '));
                return false;
            }
            return true;
        });

        // ========== INITIALIZATION ==========

        self.init = function () {
            if (self.isNewMode()) {
                // New mode: load videos
                self.loadVideos();
                self.setDefaultDates();
            } else {
                // Existing mode: load assignment info and participants
                if (!self.assignmentId()) {
                    toastr.error('Atama ID bulunamadi');
                    self.isLoading(false);
                    return;
                }
                self.loadAssignmentInfo();
                self.loadParticipants();
            }
        };

        self.setDefaultDates = function () {
            var today = new Date();
            var nextMonth = new Date(today.getFullYear(), today.getMonth() + 1, today.getDate());
            self.formStartDate(today.toISOString().split('T')[0]);
            self.formDueDate(nextMonth.toISOString().split('T')[0]);
        };

        self.loadVideos = function () {
            $.ajax({
                url: '/api/training-videos',
                method: 'GET',
                success: function (data) {
                    self.videos(data.filter(function (v) { return v.isActive; }));
                    self.loadEmailTemplates();
                },
                error: function () {
                    toastr.error('Videolar yuklenemedi');
                    self.isLoading(false);
                }
            });
        };

        self.loadEmailTemplates = function () {
            $.ajax({
                url: '/api/training-video-assignments/email-templates',
                method: 'GET',
                success: function (data) {
                    console.log('Email templates loaded:', data);
                    self.emailTemplates(data || []);
                    // Varsayılan şablonu seç
                    var defaultTemplate = (data || []).find(function (t) { return t.isDefault; });
                    if (defaultTemplate) {
                        self.formEmailTemplateId(defaultTemplate.id);
                    }
                    self.isLoading(false);
                },
                error: function (xhr) {
                    console.error('Email templates error:', xhr);
                    // Email şablonu opsiyonel, hata verse de devam et
                    self.isLoading(false);
                }
            });
        };

        self.loadAssignmentInfo = function () {
            $.ajax({
                url: '/api/training-video-assignments/' + self.assignmentId(),
                method: 'GET',
                success: function (data) {
                    self.assignmentTitle(data.title || '');
                    self.isLoading(false);
                },
                error: function () {
                    toastr.error('Atama bilgisi yuklenemedi');
                    self.isLoading(false);
                }
            });
        };

        self.loadParticipants = function () {
            self.isLoadingParticipants(true);
            $.ajax({
                url: '/api/training-video-assignments/' + self.assignmentId() + '/external-participants',
                method: 'GET',
                success: function (data) {
                    var items = data.map(function (p) {
                        p.selected = ko.observable(false);
                        return p;
                    });
                    self.participants(items);
                    self.isLoadingParticipants(false);
                },
                error: function () {
                    toastr.error('Katilimcilar yuklenemedi');
                    self.isLoadingParticipants(false);
                }
            });
        };

        // ========== CREATE ASSIGNMENT (NEW MODE) ==========

        self.createAssignment = function () {
            if (!self.canCreate()) return;

            self.isCreating(true);

            // Step 1: Create assignment
            var assignmentData = {
                trainingVideoId: parseInt(self.formVideoId(), 10),
                title: self.formTitle(),
                startDate: self.formStartDate(),
                dueDate: self.formDueDate(),
                minWatchCount: parseInt(self.formMinWatchCount(), 10) || 1,
                maxWatchCount: self.formMaxWatchCount() ? parseInt(self.formMaxWatchCount(), 10) : null,
                allowSpeedChange: self.formAllowSpeedChange(),
                allowSeeking: self.formAllowSeeking(),
                emailTemplateId: self.formEmailTemplateId() ? parseInt(self.formEmailTemplateId(), 10) : null,
                sendEmail: false, // Dış katılımcılar için ayrı email gönderilecek
                manualUserIds: [] // Dış katılımcı ataması - iç kullanıcı yok
            };

            $.ajax({
                url: '/api/training-video-assignments',
                method: 'POST',
                contentType: 'application/json',
                data: JSON.stringify(assignmentData),
                success: function (result) {
                    var newAssignmentId = result.id;

                    // Step 2: Add external participants
                    $.ajax({
                        url: '/api/training-video-assignments/' + newAssignmentId + '/external-participants',
                        method: 'POST',
                        contentType: 'application/json',
                        data: JSON.stringify({
                            participants: self.parsedParticipants(),
                            sendEmail: self.sendEmailOnAdd(),
                            emailTemplateId: self.formEmailTemplateId() || null
                        }),
                        success: function (participantResult) {
                            toastr.success('Atama olusturuldu ve ' + self.parsedParticipants().length + ' katilimci eklendi');

                            // Refresh opener if exists
                            if (window.opener && !window.opener.closed) {
                                if (typeof window.opener.refreshAssignments === 'function') {
                                    window.opener.refreshAssignments();
                                } else {
                                    window.opener.location.reload();
                                }
                            }

                            // Close popup after short delay
                            setTimeout(function () {
                                window.close();
                            }, 1500);
                        },
                        error: function (xhr) {
                            var msg = xhr.responseJSON?.message || 'Katilimci ekleme sirasinda hata';
                            toastr.warning('Atama olusturuldu fakat: ' + msg);
                            self.isCreating(false);
                        }
                    });
                },
                error: function (xhr) {
                    var msg = xhr.responseJSON?.message || 'Atama olusturulamadi';
                    toastr.error(msg);
                    self.isCreating(false);
                }
            });
        };

        // ========== ADD PARTICIPANTS (EXISTING MODE) ==========

        self.addParticipants = function () {
            var participantList = [];

            if (self.addMode() === 'text') {
                participantList = self.parseTextInput();
            } else {
                participantList = self.parsedFromFile();
            }

            if (participantList.length === 0) {
                toastr.warning('Eklenecek email bulunamadi');
                return;
            }

            self.isAdding(true);

            $.ajax({
                url: '/api/training-video-assignments/' + self.assignmentId() + '/external-participants',
                method: 'POST',
                contentType: 'application/json',
                data: JSON.stringify({
                    participants: participantList,
                    sendEmail: self.sendEmailOnAdd(),
                    emailTemplateId: self.formEmailTemplateId() || null
                }),
                success: function (result) {
                    toastr.success(result.message || 'Katilimcilar eklendi');

                    // Clear inputs
                    self.textInput('');
                    self.parsedFromFile([]);
                    $('#fileInput').val('');

                    // Reload list
                    self.loadParticipants();
                    self.isAdding(false);
                },
                error: function (xhr) {
                    var msg = xhr.responseJSON?.message || 'Ekleme sirasinda hata olustu';
                    toastr.error(msg);
                    self.isAdding(false);
                }
            });
        };

        self.parseTextInput = function () {
            var text = self.textInput();
            if (!text) return [];

            var lines = text.split('\n');
            var participants = [];
            var emailRegex = /[^\s;,]+@[^\s;,]+\.[^\s;,]+/;

            lines.forEach(function (line) {
                line = line.trim();
                if (!line) return;

                // Satırda email bul
                var emailMatch = line.match(emailRegex);
                if (!emailMatch) return;

                var email = emailMatch[0].trim();

                // Email'i satırdan çıkar, kalan kısımları ad/soyad olarak al
                var remaining = line.replace(email, '').trim();
                var parts = remaining.split(/[;,\s]+/).filter(function(p) { return p.length > 0; });

                participants.push({
                    email: email,
                    firstName: parts[0] || null,
                    lastName: parts[1] || null
                });
            });

            return participants;
        };

        self.isValidEmail = function (email) {
            var re = /[^\s;,]+@[^\s;,]+\.[^\s;,]+/;
            return re.test(email);
        };

        // ========== FILE HANDLING ==========

        self.handleFileSelect = function (vm, event) {
            var file = event.target.files[0];
            if (!file) {
                self.parsedFromFile([]);
                return;
            }

            var reader = new FileReader();
            reader.onload = function (e) {
                try {
                    var data = new Uint8Array(e.target.result);
                    var workbook = XLSX.read(data, { type: 'array' });
                    var firstSheet = workbook.Sheets[workbook.SheetNames[0]];
                    var rows = XLSX.utils.sheet_to_json(firstSheet, { header: 1 });

                    // Skip header row
                    var participants = [];
                    for (var i = 1; i < rows.length; i++) {
                        var row = rows[i];
                        if (!row || !row[0]) continue;

                        var email = String(row[0]).trim();
                        if (!self.isValidEmail(email)) continue;

                        participants.push({
                            email: email,
                            firstName: row[1] ? String(row[1]).trim() : null,
                            lastName: row[2] ? String(row[2]).trim() : null
                        });
                    }

                    self.parsedFromFile(participants);

                    if (participants.length === 0) {
                        toastr.warning('Dosyada gecerli email bulunamadi');
                    } else {
                        toastr.info(participants.length + ' kisi bulundu');
                    }
                } catch (err) {
                    toastr.error('Dosya okunamadi: ' + err.message);
                    self.parsedFromFile([]);
                }
            };
            reader.readAsArrayBuffer(file);
        };

        // ========== DELETE ==========

        self.deleteParticipant = function (participant) {
            if (typeof showConfirmModal === 'function') {
                showConfirmModal(
                    'Katilimciyi Sil',
                    participant.email + ' adresini silmek istediginize emin misiniz?',
                    function () {
                        self.doDeleteParticipant(participant);
                    }
                );
            } else {
                if (confirm(participant.email + ' adresini silmek istediginize emin misiniz?')) {
                    self.doDeleteParticipant(participant);
                }
            }
        };

        self.doDeleteParticipant = function (participant) {
            $.ajax({
                url: '/api/training-video-assignments/external-participants/' + participant.id,
                method: 'DELETE',
                success: function () {
                    toastr.success('Katilimci silindi');
                    self.participants.remove(participant);
                },
                error: function (xhr) {
                    var msg = xhr.responseJSON?.message || 'Silme sirasinda hata olustu';
                    toastr.error(msg);
                }
            });
        };

        // ========== EMAIL ==========

        self.sendEmailsToSelected = function () {
            var selectedIds = self.participants()
                .filter(function (p) { return p.selected(); })
                .map(function (p) { return p.id; });

            if (selectedIds.length === 0) {
                toastr.warning('Lutfen email gondermek icin katilimci secin');
                return;
            }

            self.isSendingEmails(true);

            $.ajax({
                url: '/api/training-video-assignments/' + self.assignmentId() + '/external-participants/send-emails',
                method: 'POST',
                contentType: 'application/json',
                data: JSON.stringify({
                    participantIds: selectedIds,
                    emailTypeId: 1
                }),
                success: function (result) {
                    toastr.success(result.message || 'Emailler gonderildi');
                    self.clearSelection();
                    self.loadParticipants();
                    self.isSendingEmails(false);
                },
                error: function (xhr) {
                    var msg = xhr.responseJSON?.message || 'Email gonderimi sirasinda hata olustu';
                    toastr.error(msg);
                    self.isSendingEmails(false);
                }
            });
        };

        // ========== SELECTION ==========

        self.clearSelection = function () {
            self.selectAll(false);
            self.participants().forEach(function (p) {
                p.selected(false);
            });
        };

        // ========== COPY LINK ==========

        self.copyLink = function (participant) {
            var url = participant.watchUrl;
            if (!url) {
                toastr.warning('Link bulunamadi');
                return;
            }

            navigator.clipboard.writeText(url).then(function () {
                toastr.success('Link kopyalandi');
            }).catch(function () {
                // Fallback
                var textarea = document.createElement('textarea');
                textarea.value = url;
                document.body.appendChild(textarea);
                textarea.select();
                document.execCommand('copy');
                document.body.removeChild(textarea);
                toastr.success('Link kopyalandi');
            });
        };

        // Initialize
        self.init();
    }

    // Apply bindings
    $(function () {
        ko.applyBindings(new ExternalParticipantsViewModel(), document.getElementById('external-participants-app'));
    });
})();
