// Training Quiz Page - Hem Internal hem External katilimcilar icin
(function () {
    'use strict';

    function QuizViewModel() {
        var self = this;

        // State
        self.isLoading = ko.observable(true);
        self.hasError = ko.observable(false);
        self.errorMessage = ko.observable('');
        self.isSubmitting = ko.observable(false);

        // Quiz data
        self.quizData = ko.observable(null);
        self.questions = ko.observableArray([]);
        self.currentIndex = ko.observable(0);
        self.answers = ko.observableArray([]);

        // Result
        self.showResult = ko.observable(false);
        self.quizResult = ko.observable(null);

        // Current question
        self.currentQuestion = ko.computed(function () {
            return self.questions()[self.currentIndex()] || null;
        });

        // Current answer
        self.currentAnswer = ko.computed(function () {
            return self.answers()[self.currentIndex()] || null;
        });

        // ========== INITIALIZATION ==========

        self.init = function () {
            if (IS_EXTERNAL) {
                if (!PARTICIPANT_TOKEN) {
                    self.hasError(true);
                    self.errorMessage('Gecersiz link');
                    self.isLoading(false);
                    return;
                }
                self.loadExternalQuiz();
            } else {
                if (!PARTICIPANT_ID) {
                    self.hasError(true);
                    self.errorMessage('Gecersiz katilimci');
                    self.isLoading(false);
                    return;
                }
                self.loadInternalQuiz();
            }
        };

        // ========== LOAD QUIZ ==========

        self.loadExternalQuiz = function () {
            $.ajax({
                url: '/api/training-video-assignments/external/' + PARTICIPANT_TOKEN + '/quiz',
                method: 'GET',
                success: function (data) {
                    self.quizData(data);

                    // Quiz zaten gecilmis mi?
                    if (data.lastAttemptResult && data.lastAttemptResult.isPassed) {
                        self.isLoading(false);
                        return;
                    }

                    // Sorulari yukle
                    self.startQuiz();
                },
                error: function (xhr) {
                    if (xhr.status === 404) {
                        // Quiz yok
                        self.quizData(null);
                    } else {
                        self.hasError(true);
                        self.errorMessage(xhr.responseJSON?.message || 'Anket yuklenemedi');
                    }
                    self.isLoading(false);
                }
            });
        };

        self.loadInternalQuiz = function () {
            $.ajax({
                url: '/api/training-videos/participant/' + PARTICIPANT_ID + '/quiz',
                method: 'GET',
                success: function (data) {
                    self.quizData(data);

                    // Quiz zaten gecilmis mi?
                    if (data.lastAttemptResult && data.lastAttemptResult.isPassed) {
                        self.isLoading(false);
                        return;
                    }

                    // Sorulari yukle
                    self.startQuiz();
                },
                error: function (xhr) {
                    if (xhr.status === 404) {
                        // Quiz yok
                        self.quizData(null);
                    } else {
                        self.hasError(true);
                        self.errorMessage(xhr.responseJSON?.message || 'Anket yuklenemedi');
                    }
                    self.isLoading(false);
                }
            });
        };

        self.startQuiz = function () {
            var url = IS_EXTERNAL
                ? '/api/training-video-assignments/external/' + PARTICIPANT_TOKEN + '/quiz/start'
                : '/api/training-videos/participant/' + PARTICIPANT_ID + '/quiz/start';

            $.ajax({
                url: url,
                method: 'POST',
                success: function (data) {
                    self.questions(data.questions || []);

                    // Her soru icin bos cevap hazirla
                    var answers = data.questions.map(function (q) {
                        return {
                            questionId: q.id,
                            selectedOptionId: ko.observable(null),
                            selectedOptionIds: ko.observableArray([])
                        };
                    });
                    self.answers(answers);
                    self.currentIndex(0);
                },
                error: function (xhr) {
                    self.hasError(true);
                    self.errorMessage('Anket baslatilamadi');
                },
                complete: function () {
                    self.isLoading(false);
                }
            });
        };

        // ========== QUIZ INTERACTION ==========

        self.isQuestionAnswered = function (index) {
            var answer = self.answers()[index];
            if (!answer) return false;

            var question = self.questions()[index];
            if (question.questionTypeId === 1) {
                return answer.selectedOptionId() !== null;
            } else {
                return answer.selectedOptionIds().length > 0;
            }
        };

        self.selectOption = function (option) {
            var answer = self.currentAnswer();
            if (!answer) return;

            var question = self.currentQuestion();
            if (question.questionTypeId === 1) {
                // Tekli secim
                answer.selectedOptionId(option.id);
            } else {
                // Coklu secim
                var ids = answer.selectedOptionIds();
                var index = ids.indexOf(option.id);
                if (index >= 0) {
                    answer.selectedOptionIds.splice(index, 1);
                } else {
                    answer.selectedOptionIds.push(option.id);
                }
            }
        };

        self.isOptionSelected = function (option) {
            var answer = self.currentAnswer();
            if (!answer) return false;

            var question = self.currentQuestion();
            if (question.questionTypeId === 1) {
                return answer.selectedOptionId() === option.id;
            } else {
                return answer.selectedOptionIds().indexOf(option.id) >= 0;
            }
        };

        self.nextQuestion = function () {
            if (self.currentIndex() < self.questions().length - 1) {
                self.currentIndex(self.currentIndex() + 1);
            }
        };

        self.prevQuestion = function () {
            if (self.currentIndex() > 0) {
                self.currentIndex(self.currentIndex() - 1);
            }
        };

        // ========== SUBMIT ==========

        self.submitQuiz = function () {
            var quizData = self.quizData();
            if (!quizData) return;

            self.isSubmitting(true);

            var answersData = self.answers().map(function (a) {
                var question = self.questions().find(function (q) { return q.id === a.questionId; });
                if (question.questionTypeId === 1) {
                    return {
                        questionId: a.questionId,
                        selectedOptionId: a.selectedOptionId(),
                        selectedOptionIds: null
                    };
                } else {
                    return {
                        questionId: a.questionId,
                        selectedOptionId: null,
                        selectedOptionIds: a.selectedOptionIds()
                    };
                }
            });

            var dto = {
                quizId: quizData.quizId,
                participantId: IS_EXTERNAL ? null : PARTICIPANT_ID,
                externalParticipantId: null,
                answers: answersData
            };

            var url = IS_EXTERNAL
                ? '/api/training-video-assignments/external/' + PARTICIPANT_TOKEN + '/quiz/submit'
                : '/api/training-videos/participant/' + PARTICIPANT_ID + '/quiz/submit';

            $.ajax({
                url: url,
                method: 'POST',
                contentType: 'application/json',
                data: JSON.stringify(dto),
                success: function (result) {
                    self.quizResult(result);
                    self.showResult(true);

                    toastr.success('Anketiniz kaydedildi. Katiliminiz icin tesekkurler.');
                },
                error: function (xhr) {
                    toastr.error('Anket gonderilemedi');
                    console.error('Quiz submit error:', xhr);
                },
                complete: function () {
                    self.isSubmitting(false);
                }
            });
        };

        // ========== RETRY ==========

        self.retryQuiz = function () {
            self.showResult(false);
            self.quizResult(null);
            self.currentIndex(0);
            self.isLoading(true);
            self.startQuiz();
        };

        // Initialize
        self.init();
    }

    // Apply bindings
    $(function () {
        ko.applyBindings(new QuizViewModel(), document.getElementById('quiz-app'));
    });
})();
