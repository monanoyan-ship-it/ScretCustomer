// TrainingQuiz/Index.js - Egitim Anketi Yonetimi (Wizard)
(function () {
    'use strict';

    function TrainingQuizViewModel() {
        var self = this;

        // Localization helper
        self.T = function (key, defaultValue) {
            return window.T ? window.T(key, defaultValue) : defaultValue;
        };

        // State
        self.isLoading = ko.observable(false);
        self.isSaving = ko.observable(false);

        // Lists
        self.quizzes = ko.observableArray([]);
        self.videos = ko.observableArray([]);
        self.responses = ko.observableArray([]);

        // Filters
        self.searchTerm = ko.observable('');
        self.filterVideoId = ko.observable('');
        self.filterStatus = ko.observable('');
        self.filterRequired = ko.observable('');

        // Wizard State
        self.currentStep = ko.observable(1);
        self.isEditing = ko.observable(false);
        self.editingQuizId = ko.observable(null);

        // Quiz Form - Step 1
        self.formVideoId = ko.observable('');
        self.formTitle = ko.observable('');
        self.formDescription = ko.observable('');
        self.formPassingScore = ko.observable(null);
        self.formIsRequired = ko.observable(true);
        self.formIsActive = ko.observable(true);
        self.formShuffleQuestions = ko.observable(false);
        self.formShuffleOptions = ko.observable(false);
        self.formShowResults = ko.observable(true);

        // Quiz Form - Step 2 (Questions)
        self.formQuestions = ko.observableArray([]);

        // Responses Modal
        self.selectedQuizId = ko.observable(null);
        self.selectedQuizTitle = ko.observable('');

        // Computed: Can go to step 2
        self.canGoToStep2 = ko.computed(function () {
            return self.formTitle();
        });

        // ===== Initialization =====

        self.init = function () {
            self.loadVideos();
            self.loadQuizzes();
        };

        // ===== Data Loading =====

        self.loadQuizzes = function () {
            self.isLoading(true);

            var params = new URLSearchParams();
            if (self.searchTerm()) params.append('searchTerm', self.searchTerm());
            if (self.filterVideoId()) params.append('videoIds', self.filterVideoId());
            if (self.filterStatus() !== '') params.append('isActive', self.filterStatus());
            if (self.filterRequired() !== '') params.append('isRequired', self.filterRequired());

            $.ajax({
                url: '/api/training-quiz?' + params.toString(),
                method: 'GET',
                success: function (data) {
                    self.quizzes(data);
                },
                error: function (xhr) {
                    showToast('error', self.T('TrainingQuiz.LoadError', 'Anketler yuklenirken hata olustu'));
                },
                complete: function () {
                    self.isLoading(false);
                }
            });
        };

        self.loadVideos = function () {
            $.ajax({
                url: '/api/training-videos',
                method: 'GET',
                success: function (data) {
                    self.videos(data);
                }
            });
        };

        self.clearFilters = function () {
            self.searchTerm('');
            self.filterVideoId('');
            self.filterStatus('');
            self.filterRequired('');
            self.loadQuizzes();
        };

        // ===== Wizard Navigation =====

        self.goToStep1 = function () {
            self.currentStep(1);
        };

        self.goToStep2 = function () {
            if (!self.canGoToStep2()) {
                showToast('warning', self.T('Common.RequiredFields', 'Anket basligi zorunludur'));
                return;
            }
            self.currentStep(2);
        };

        // ===== Quiz CRUD =====

        self.openCreateModal = function () {
            self.isEditing(false);
            self.editingQuizId(null);
            self.currentStep(1);

            // Reset form
            self.formVideoId('');
            self.formTitle('');
            self.formDescription('');
            self.formPassingScore(null);
            self.formIsRequired(true);
            self.formIsActive(true);
            self.formShuffleQuestions(false);
            self.formShuffleOptions(false);
            self.formShowResults(true);
            self.formQuestions([]);

            var modal = bootstrap.Modal.getOrCreateInstance(document.getElementById('quizModal'));
            modal.show();
        };

        self.openEditModal = function (quiz) {
            self.isEditing(true);
            self.editingQuizId(quiz.id);
            self.currentStep(1);

            // Load quiz details
            $.ajax({
                url: '/api/training-quiz/' + quiz.id,
                method: 'GET',
                success: function (data) {
                    self.formVideoId(data.trainingVideoId);
                    self.formTitle(data.title);
                    self.formDescription(data.description || '');
                    self.formPassingScore(data.passingScore);
                    self.formIsRequired(data.isRequired);
                    self.formIsActive(data.isActive);
                    self.formShuffleQuestions(data.shuffleQuestions);
                    self.formShuffleOptions(data.shuffleOptions);
                    self.formShowResults(data.showResults);

                    // Load questions
                    var questions = (data.questions || []).map(function (q) {
                        return {
                            id: q.id,
                            text: ko.observable(q.text),
                            helpText: ko.observable(q.helpText || ''),
                            questionTypeId: ko.observable(q.questionTypeId),
                            options: ko.observableArray((q.options || []).map(function (o) {
                                return {
                                    id: o.id,
                                    text: ko.observable(o.text),
                                    weightPoints: ko.observable(o.weightPoints),
                                    isCorrect: ko.observable(o.isCorrect)
                                };
                            }))
                        };
                    });
                    self.formQuestions(questions);

                    var modal = bootstrap.Modal.getOrCreateInstance(document.getElementById('quizModal'));
                    modal.show();
                },
                error: function () {
                    showToast('error', 'Anket detaylari yuklenemedi');
                }
            });
        };

        self.saveQuiz = function () {
            // Validate
            if (!self.formTitle()) {
                showToast('warning', self.T('Common.RequiredFields', 'Anket basligi zorunludur'));
                return;
            }

            if (self.formQuestions().length === 0) {
                showToast('warning', 'En az bir soru eklemelisiniz');
                return;
            }

            // Validate questions
            for (var i = 0; i < self.formQuestions().length; i++) {
                var q = self.formQuestions()[i];
                if (!ko.unwrap(q.text)) {
                    showToast('warning', 'Soru ' + (i + 1) + ': Soru metni bos olamaz');
                    return;
                }
                var options = ko.unwrap(q.options);
                var filledOptions = options.filter(function (o) { return ko.unwrap(o.text); });
                if (filledOptions.length < 2) {
                    showToast('warning', 'Soru ' + (i + 1) + ': En az 2 secenek gerekli');
                    return;
                }
                var hasCorrect = options.some(function (o) { return ko.unwrap(o.isCorrect); });
                if (!hasCorrect) {
                    showToast('warning', 'Soru ' + (i + 1) + ': En az bir dogru cevap secmelisiniz');
                    return;
                }
            }

            self.isSaving(true);

            // Build questions data
            var questionsData = self.formQuestions().map(function (q, idx) {
                var options = ko.unwrap(q.options).filter(function (o) { return ko.unwrap(o.text); });
                return {
                    text: ko.unwrap(q.text),
                    helpText: ko.unwrap(q.helpText) || null,
                    order: idx + 1,
                    questionTypeId: parseInt(ko.unwrap(q.questionTypeId)),
                    options: options.map(function (o, oidx) {
                        return {
                            text: ko.unwrap(o.text),
                            order: oidx + 1,
                            weightPoints: parseFloat(ko.unwrap(o.weightPoints)) || 0,
                            isCorrect: ko.unwrap(o.isCorrect)
                        };
                    })
                };
            });

            var data = {
                trainingVideoId: null, // Video tarafından seçilecek
                title: self.formTitle(),
                description: self.formDescription() || null,
                passingScore: self.formPassingScore() ? parseInt(self.formPassingScore()) : null,
                isRequired: self.formIsRequired(),
                isActive: self.formIsActive(),
                shuffleQuestions: self.formShuffleQuestions(),
                shuffleOptions: self.formShuffleOptions(),
                showResults: self.formShowResults(),
                questions: questionsData
            };

            if (self.isEditing()) {
                // Update existing quiz - first update quiz info, then sync questions
                self.updateExistingQuiz(data);
            } else {
                // Create new quiz with questions
                $.ajax({
                    url: '/api/training-quiz',
                    method: 'POST',
                    contentType: 'application/json',
                    data: JSON.stringify(data),
                    success: function () {
                        bootstrap.Modal.getInstance(document.getElementById('quizModal')).hide();
                        self.loadQuizzes();
                        showToast('success', self.T('Common.Saved', 'Kaydedildi'));
                    },
                    error: function (xhr) {
                        var msg = xhr.responseJSON?.message || self.T('Common.SaveError', 'Kaydetme hatasi');
                        showToast('error', msg);
                    },
                    complete: function () {
                        self.isSaving(false);
                    }
                });
            }
        };

        self.updateExistingQuiz = function (data) {
            // Update quiz info
            $.ajax({
                url: '/api/training-quiz/' + self.editingQuizId(),
                method: 'PUT',
                contentType: 'application/json',
                data: JSON.stringify({
                    title: data.title,
                    description: data.description,
                    passingScore: data.passingScore,
                    isRequired: data.isRequired,
                    shuffleQuestions: data.shuffleQuestions,
                    shuffleOptions: data.shuffleOptions,
                    showResults: data.showResults,
                    isActive: data.isActive
                }),
                success: function () {
                    // Now sync questions - for simplicity, delete all and re-add
                    self.syncQuestions(data.questions);
                },
                error: function (xhr) {
                    var msg = xhr.responseJSON?.message || 'Guncelleme hatasi';
                    showToast('error', msg);
                    self.isSaving(false);
                }
            });
        };

        self.syncQuestions = function (questionsData) {
            // Get existing questions and delete them
            var quizId = self.editingQuizId();

            $.ajax({
                url: '/api/training-quiz/' + quizId,
                method: 'GET',
                success: function (quiz) {
                    var existingQuestions = quiz.questions || [];
                    var deletePromises = existingQuestions.map(function (q) {
                        return $.ajax({
                            url: '/api/training-quiz/questions/' + q.id,
                            method: 'DELETE'
                        });
                    });

                    // After deleting, add new questions
                    $.when.apply($, deletePromises).always(function () {
                        var addPromises = questionsData.map(function (q) {
                            return $.ajax({
                                url: '/api/training-quiz/' + quizId + '/questions',
                                method: 'POST',
                                contentType: 'application/json',
                                data: JSON.stringify(q)
                            });
                        });

                        $.when.apply($, addPromises).always(function () {
                            bootstrap.Modal.getInstance(document.getElementById('quizModal')).hide();
                            self.loadQuizzes();
                            showToast('success', 'Guncellendi');
                            self.isSaving(false);
                        });
                    });
                },
                error: function () {
                    showToast('error', 'Sorular guncellenirken hata');
                    self.isSaving(false);
                }
            });
        };

        self.deleteQuiz = function (quiz) {
            if (!confirm(self.T('TrainingQuiz.DeleteConfirm', 'Bu anketi silmek istediginize emin misiniz?'))) {
                return;
            }

            $.ajax({
                url: '/api/training-quiz/' + quiz.id,
                method: 'DELETE',
                success: function () {
                    self.loadQuizzes();
                    showToast('success', self.T('Common.Deleted', 'Silindi'));
                },
                error: function (xhr) {
                    var msg = xhr.responseJSON?.message || self.T('Common.DeleteError', 'Silme hatasi');
                    showToast('error', msg);
                }
            });
        };

        // ===== Question Management =====

        self.addQuestion = function () {
            self.formQuestions.push({
                id: null,
                text: ko.observable(''),
                helpText: ko.observable(''),
                questionTypeId: ko.observable(1),
                options: ko.observableArray([
                    { id: null, text: ko.observable(''), weightPoints: ko.observable(1), isCorrect: ko.observable(true) },
                    { id: null, text: ko.observable(''), weightPoints: ko.observable(0), isCorrect: ko.observable(false) }
                ])
            });
        };

        self.removeQuestion = function (question) {
            if (confirm('Bu soruyu silmek istediginize emin misiniz?')) {
                self.formQuestions.remove(question);
            }
        };

        self.moveQuestionUp = function (question) {
            var idx = self.formQuestions.indexOf(question);
            if (idx > 0) {
                var questions = self.formQuestions();
                self.formQuestions.splice(idx - 1, 2, questions[idx], questions[idx - 1]);
            }
        };

        self.moveQuestionDown = function (question) {
            var idx = self.formQuestions.indexOf(question);
            if (idx < self.formQuestions().length - 1) {
                var questions = self.formQuestions();
                self.formQuestions.splice(idx, 2, questions[idx + 1], questions[idx]);
            }
        };

        self.addOption = function (question) {
            question.options.push({
                id: null,
                text: ko.observable(''),
                weightPoints: ko.observable(0),
                isCorrect: ko.observable(false)
            });
        };

        self.removeOption = function (question, option) {
            if (question.options().length > 2) {
                question.options.remove(option);
            }
        };

        // Tek dogru cevap secimi - radio button mantigi
        self.setCorrectOption = function (question, selectedOption) {
            // Tum seceneklerin isCorrect'ini false yap
            question.options().forEach(function (opt) {
                opt.isCorrect(false);
            });
            // Sadece secilen secenegin isCorrect'ini true yap
            selectedOption.isCorrect(true);
        };

        // ===== Responses Modal =====

        self.openResponsesModal = function (quiz) {
            self.selectedQuizId(quiz.id);
            self.selectedQuizTitle(quiz.title);
            self.loadResponses(quiz.id);

            var modal = bootstrap.Modal.getOrCreateInstance(document.getElementById('responsesModal'));
            modal.show();
        };

        self.loadResponses = function (quizId) {
            $.ajax({
                url: '/api/training-quiz/responses?quizId=' + quizId,
                method: 'GET',
                success: function (data) {
                    self.responses(data);
                }
            });
        };

        // Initialize
        self.init();
    }

    // Helper function for toast notifications
    function showToast(type, message) {
        if (window.toastr) {
            toastr[type](message);
        } else {
            alert(message);
        }
    }

    // Apply bindings when DOM is ready
    $(function () {
        ko.applyBindings(new TrainingQuizViewModel(), document.getElementById('training-quiz-app'));
    });
})();
