// Translation Keys
var TRANSLATION_KEYS = [
    'Survey.AnswerAtLeastOne',
    'Survey.RequiredUnanswered',
    'Survey.SubmitFailed',
    'Survey.SubmitError',
    'Survey.GeneralGroup',
    'Common.Confirm',
    'Assessment.Fill.ConfirmSubmit',
    'Assessment.Fill.ConfirmSubmitMessage',
    'Assessment.Fill.SubmitSuccess',
    'Assessment.Fill.NextTaskLoading',
    'FeedbackRole.Self',
    'FeedbackRole.Manager',
    'FeedbackRole.Peer',
    'FeedbackRole.Subordinate'
];

// Feedback role mapping
var FEEDBACK_ROLES = {
    1: { nameKey: 'FeedbackRole.Self', defaultName: 'Öz Değerlendirme', badgeClass: 'bg-primary', icon: 'bi-person' },
    2: { nameKey: 'FeedbackRole.Manager', defaultName: 'Yönetici Değerlendirmesi', badgeClass: 'bg-info', icon: 'bi-person-up' },
    3: { nameKey: 'FeedbackRole.Peer', defaultName: 'Eş Düzey Değerlendirmesi', badgeClass: 'bg-success', icon: 'bi-people' },
    4: { nameKey: 'FeedbackRole.Subordinate', defaultName: 'Ast Değerlendirmesi', badgeClass: 'bg-warning text-dark', icon: 'bi-person-down' }
};

function AssessmentFillViewModel() {
    var self = this;

    // Config
    self.token = window.assessmentConfig ? window.assessmentConfig.token : '';

    // State
    self.isLoading = ko.observable(true);
    self.isSubmitting = ko.observable(false);
    self.errorMessage = ko.observable('');
    self.isAllCompleted = ko.observable(false);

    // Context data
    self.currentContext = ko.observable(null);
    self.totalTaskCount = ko.observable(0);
    self.completedTaskCount = ko.observable(0);

    // Questions
    self.questions = ko.observableArray([]);
    self.hideGroupNames = ko.observable(false);

    // Progress
    self.progressPercent = ko.computed(function () {
        var total = self.totalTaskCount();
        if (total === 0) return 0;
        return Math.round((self.completedTaskCount() / total) * 100);
    });

    // Whether current task has a next task (hint for button text)
    self.hasNextTaskHint = ko.computed(function () {
        var completed = self.completedTaskCount();
        var total = self.totalTaskCount();
        return completed + 1 < total;
    });

    // Feedback role name
    self.feedbackRoleName = ko.computed(function () {
        var ctx = self.currentContext();
        if (!ctx || !ctx.currentTask) return '';
        var role = FEEDBACK_ROLES[ctx.currentTask.feedbackRoleId];
        if (!role) return '';
        return T(role.nameKey, role.defaultName);
    });

    // Feedback role badge class
    self.feedbackRoleBadgeClass = ko.computed(function () {
        var ctx = self.currentContext();
        if (!ctx || !ctx.currentTask) return 'bg-secondary';
        var role = FEEDBACK_ROLES[ctx.currentTask.feedbackRoleId];
        return role ? role.badgeClass : 'bg-secondary';
    });

    // Computed: Has any group name
    self.hasAnyGroupName = ko.computed(function () {
        if (self.hideGroupNames()) return false;
        return self.questions().some(function (q) {
            return q.groupName && q.groupName.trim() !== '';
        });
    });

    // Computed: Question groups
    self.questionGroups = ko.computed(function () {
        var questions = self.questions();
        var groups = {};
        var groupOrder = [];

        var defaultGroupName = T('Survey.GeneralGroup', 'Genel');
        questions.forEach(function (q) {
            var isDefault = !q.groupName || q.groupName.trim() === '';
            var groupName = isDefault ? defaultGroupName : q.groupName;
            if (!groups[groupName]) {
                groups[groupName] = {
                    name: groupName,
                    isDefaultGroup: isDefault,
                    order: q.groupOrder || 0,
                    questions: []
                };
                groupOrder.push(groupName);
            }
            groups[groupName].questions.push(q);
        });

        return groupOrder
            .map(function (name) { return groups[name]; })
            .sort(function (a, b) { return a.order - b.order; });
    });

    // Is question answered
    self.isQuestionAnswered = function (q) {
        var answer = q.answer();
        var hasSubCriteriaSelection = answer.selectedSubCriteriaIds && answer.selectedSubCriteriaIds().length > 0;
        if (q.scoringType === 'Descriptive') {
            return answer.comment() && answer.comment().trim() !== '';
        }
        return answer.score() !== null || hasSubCriteriaSelection;
    };

    // Computed: Answered count
    self.answeredCount = ko.computed(function () {
        var count = 0;
        self.questions().forEach(function (q) {
            if (self.isQuestionAnswered(q)) count++;
        });
        return count;
    });

    // Helper: Get score options
    self.getScoreOptions = function (maxPoints) {
        var options = [];
        for (var i = 0; i <= maxPoints; i++) options.push(i);
        return options;
    };

    // SubCriteria selection toggle
    self.selectSubCriteria = function (question, subCriteria) {
        if (question.selectionTypeId === 1) {
            question.subCriteria.forEach(function (sc) {
                sc.isSelected(sc.id === subCriteria.id);
            });
            var answer = question.answer();
            answer.selectedSubCriteriaIds([subCriteria.id]);
        } else {
            subCriteria.isSelected(!subCriteria.isSelected());
            var answer = question.answer();
            var selectedIds = question.subCriteria
                .filter(function (sc) { return sc.isSelected(); })
                .map(function (sc) { return sc.id; });
            answer.selectedSubCriteriaIds(selectedIds);
        }
    };

    // Load fill context from API
    self.loadContext = function () {
        if (!self.token) {
            self.errorMessage('Geçersiz değerlendirme linki. Token bulunamadı.');
            self.isLoading(false);
            return;
        }

        fetch('/api/assessment/fill/' + encodeURIComponent(self.token))
            .then(function (res) {
                if (res.status === 404) {
                    return res.json().then(function (data) {
                        throw new Error(data.message || 'Değerlendirme bulunamadı.');
                    });
                }
                if (!res.ok) {
                    return res.json().then(function (data) {
                        throw new Error(data.message || 'Değerlendirme formu yüklenemedi.');
                    });
                }
                return res.json();
            })
            .then(function (data) {
                self.totalTaskCount(data.totalTaskCount || 0);
                self.completedTaskCount(data.completedTaskCount || 0);

                if (data.isAllCompleted) {
                    self.isAllCompleted(true);
                    return;
                }

                self.currentContext(data);
                self.loadQuestions(data.questions || []);
            })
            .catch(function (error) {
                console.error('Error loading assessment context:', error);
                self.errorMessage(error.message || 'Değerlendirme formu yüklenirken bir hata oluştu.');
            })
            .finally(function () {
                self.isLoading(false);
            });
    };

    // Load questions from API data
    self.loadQuestions = function (questionsData) {
        var questions = (questionsData || []).map(function (q, index) {
            var subCriteriaList = (q.subCriteria || []).map(function (sc) {
                return {
                    id: sc.id,
                    description: sc.description,
                    order: sc.order || 0,
                    isSelected: ko.observable(false)
                };
            });

            return {
                id: q.id,
                text: q.text,
                description: q.description,
                groupName: q.groupName,
                groupOrder: q.groupOrder || 0,
                order: q.order || (index + 1),
                scoringType: q.scoringType || 'Scored',
                maxPoints: q.maxPoints || 10,
                weightPoints: q.weightPoints || 1,
                selectionTypeId: parseInt(q.selectionTypeId) || 2,
                showScoreInput: q.showScoreInput !== false,
                allowComment: q.allowComment !== false,
                isRequired: q.isRequired === true,
                subCriteria: subCriteriaList,
                hasSubCriteria: subCriteriaList.length > 0,
                answer: ko.observable({
                    score: ko.observable(null),
                    comment: ko.observable(''),
                    selectedSubCriteriaIds: ko.observableArray([])
                })
            };
        });
        self.questions(questions);
    };

    // Submit current task
    self.submitTask = function () {
        if (self.answeredCount() === 0) {
            toastr.warning(T('Survey.AnswerAtLeastOne', 'Lütfen en az bir soruyu cevaplayın.'));
            return;
        }

        // Clear highlights
        $('[data-question-id]').removeClass('border-danger border-2');

        // Required question check
        var unansweredRequired = self.questions().filter(function (q) {
            return q.isRequired && !self.isQuestionAnswered(q);
        });
        if (unansweredRequired.length > 0) {
            unansweredRequired.forEach(function (q) {
                $('[data-question-id="' + q.id + '"]').addClass('border-danger border-2');
            });
            var firstUnanswered = $('[data-question-id="' + unansweredRequired[0].id + '"]');
            if (firstUnanswered.length) {
                $('html, body').animate({ scrollTop: firstUnanswered.offset().top - 100 }, 300);
            }
            toastr.error(T('Survey.RequiredUnanswered', '{0} zorunlu soru cevaplanmadı.').replace('{0}', unansweredRequired.length));
            return;
        }

        // Prepare answers
        var answers = [];
        self.questions().forEach(function (q) {
            var answer = q.answer();
            var score = answer.score();
            var comment = answer.comment();
            var selectedSubCriteriaIds = answer.selectedSubCriteriaIds ? answer.selectedSubCriteriaIds() : [];

            if (q.scoringType === 'Descriptive') {
                if (comment && comment.trim()) {
                    answers.push({ questionId: q.id, score: null, comment: comment, selectedSubCriteriaIds: selectedSubCriteriaIds });
                }
            } else if (score !== null || selectedSubCriteriaIds.length > 0) {
                answers.push({ questionId: q.id, score: score, comment: comment || null, selectedSubCriteriaIds: selectedSubCriteriaIds });
            }
        });

        if (answers.length === 0) {
            toastr.warning(T('Survey.AnswerAtLeastOne', 'Lütfen en az bir soruyu cevaplayın.'));
            return;
        }

        var ctx = self.currentContext();
        showConfirmModal({
            title: T('Assessment.Fill.ConfirmSubmit', 'Değerlendirme Gönderimi'),
            message: T('Assessment.Fill.ConfirmSubmitMessage', 'Bu adımı göndermek istediğinize emin misiniz?'),
            type: 'primary',
            confirmText: T('Common.Confirm', 'Gönder'),
            confirmIcon: 'bi-send',
            onConfirm: function () {
                self.doSubmit(ctx.currentTask.id, answers);
            }
        });
    };

    // Actual submit
    self.doSubmit = function (assessmentTaskId, answers) {
        self.isSubmitting(true);

        fetch('/api/assessment/fill/submit', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({
                token: self.token,
                assessmentTaskId: assessmentTaskId,
                answers: answers
            })
        })
            .then(function (res) {
                if (!res.ok) {
                    return res.json().then(function (data) {
                        throw new Error(data.message || 'Gönderim başarısız');
                    });
                }
                return res.json();
            })
            .then(function (result) {
                if (!result.success) {
                    toastr.error(result.message || T('Survey.SubmitFailed', 'Değerlendirme gönderilemedi.'));
                    return;
                }

                toastr.success(T('Assessment.Fill.SubmitSuccess', 'Değerlendirme başarıyla gönderildi!'));

                var newCompleted = self.completedTaskCount() + 1;
                self.completedTaskCount(newCompleted);

                if (result.isAllCompleted) {
                    // All tasks done
                    self.isAllCompleted(true);
                    self.currentContext(null);
                    self.questions([]);
                } else if (result.hasNextTask && result.nextTask) {
                    // Load next task - re-fetch context for fresh questions
                    setTimeout(function () {
                        self.loadNextTask();
                    }, 800);
                }
            })
            .catch(function (error) {
                console.error('Error submitting assessment:', error);
                toastr.error(error.message || T('Survey.SubmitError', 'Değerlendirme gönderilirken bir hata oluştu.'));
            })
            .finally(function () {
                self.isSubmitting(false);
            });
    };

    // Load next task (re-fetch context)
    self.loadNextTask = function () {
        self.isLoading(true);
        self.questions([]);
        self.currentContext(null);

        // Scroll to top
        $('html, body').animate({ scrollTop: 0 }, 300);

        fetch('/api/assessment/fill/' + encodeURIComponent(self.token))
            .then(function (res) {
                if (!res.ok) {
                    return res.json().then(function (data) {
                        throw new Error(data.message || 'Sonraki adım yüklenemedi.');
                    });
                }
                return res.json();
            })
            .then(function (data) {
                self.totalTaskCount(data.totalTaskCount || 0);
                self.completedTaskCount(data.completedTaskCount || 0);

                if (data.isAllCompleted) {
                    self.isAllCompleted(true);
                    return;
                }

                self.currentContext(data);
                self.loadQuestions(data.questions || []);
            })
            .catch(function (error) {
                console.error('Error loading next task:', error);
                self.errorMessage(error.message || 'Sonraki değerlendirme yüklenirken hata oluştu.');
            })
            .finally(function () {
                self.isLoading(false);
            });
    };

    // Initialize
    self.init = function () {
        self.loadContext();
    };

    self.init();
}

// Apply bindings with localization
$(document).ready(function () {
    Localization.loadKeys(TRANSLATION_KEYS).then(function () {
        var app = document.getElementById('assessment-app');
        if (app) {
            ko.applyBindings(new AssessmentFillViewModel(), app);
        }
    });
});
