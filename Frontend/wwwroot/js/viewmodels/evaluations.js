// Evaluations ViewModel
function EvaluationsViewModel() {
    const self = this;

    // Observable arrays and properties
    self.pendingAssignments = ko.observableArray([]);
    self.isLoading = ko.observable(false);
    self.isEvaluating = ko.observable(false);
    self.isSubmitting = ko.observable(false);

    // Current evaluation state
    self.currentAssignment = ko.observable(null);
    self.evaluationSections = ko.observableArray([]);
    self.notes = ko.observable('');

    // Progress tracking
    self.totalQuestions = ko.computed(() => {
        let total = 0;
        self.evaluationSections().forEach(section => {
            total += section.questions.length;
        });
        return total;
    });

    self.answeredCount = ko.computed(() => {
        let answered = 0;
        self.evaluationSections().forEach(section => {
            section.questions.forEach(question => {
                if (question.answerIsNA()) {
                    answered++;
                } else if (question.questionType === 'YesNo' || question.questionType === 'Rating') {
                    if (question.answerNumeric() !== null) answered++;
                } else if (question.questionType === 'Text' || question.questionType === 'MultipleChoice') {
                    if (question.answerText() && question.answerText().trim() !== '') answered++;
                }
            });
        });
        return answered;
    });

    self.progressPercentage = ko.computed(() => {
        const total = self.totalQuestions();
        if (total === 0) return 0;
        return Math.round((self.answeredCount() / total) * 100);
    });

    // Load pending assignments for the current user
    self.loadPendingAssignments = async function() {
        self.isLoading(true);
        try {
            const currentUser = authService.getCurrentUser();
            if (!currentUser) {
                throw new Error('Kullanıcı bilgisi bulunamadı');
            }

            // Load assignments for current user
            const data = await apiService.get(`/assignment/evaluator/${currentUser.userId}`);

            // Filter only pending assignments
            const pending = data.filter(assignment =>
                assignment.status === 'Pending' || assignment.status === 'InProgress'
            );

            self.pendingAssignments(pending);
        } catch (error) {
            console.error('Error loading assignments:', error);
            alert('Atamalar yüklenirken hata oluştu: ' + error.message);
        } finally {
            self.isLoading(false);
        }
    };

    // Show new evaluation form (same as load pending assignments)
    self.showNewEvaluationForm = function() {
        self.loadPendingAssignments();
    };

    // Start an evaluation
    self.startEvaluation = async function(assignment) {
        try {
            // Load the checklist for this assignment
            const checklist = await apiService.get(`/checklist/${assignment.checklistId}`);

            // Transform checklist sections into evaluation format
            const sections = checklist.sections.map(section => ({
                id: section.id,
                name: section.name,
                order: section.order,
                questions: section.questions.map(question => ({
                    id: question.id,
                    text: question.text,
                    questionType: question.questionType,
                    points: question.points,
                    allowNA: question.allowNA,
                    options: question.options,
                    order: question.order,
                    // Answer observables
                    answerText: ko.observable(''),
                    answerNumeric: ko.observable(null),
                    answerIsNA: ko.observable(false)
                }))
            }));

            self.currentAssignment(assignment);
            self.evaluationSections(sections);
            self.notes('');
            self.isEvaluating(true);

        } catch (error) {
            console.error('Error starting evaluation:', error);
            alert('Değerlendirme başlatılırken hata oluştu: ' + error.message);
        }
    };

    // Cancel current evaluation
    self.cancelEvaluation = function() {
        if (confirm('Değerlendirmeden çıkmak istediğinize emin misiniz? Girilen veriler kaydedilmeyecektir.')) {
            self.isEvaluating(false);
            self.currentAssignment(null);
            self.evaluationSections([]);
            self.notes('');
        }
    };

    // Submit evaluation
    self.submitEvaluation = async function() {
        // Validation: Check if all non-NA questions are answered
        let unansweredCount = 0;
        self.evaluationSections().forEach(section => {
            section.questions.forEach(question => {
                const isNA = question.answerIsNA();
                const hasNumericAnswer = question.answerNumeric() !== null;
                const hasTextAnswer = question.answerText() && question.answerText().trim() !== '';

                if (!isNA) {
                    if ((question.questionType === 'YesNo' || question.questionType === 'Rating') && !hasNumericAnswer) {
                        unansweredCount++;
                    } else if ((question.questionType === 'Text' || question.questionType === 'MultipleChoice') && !hasTextAnswer) {
                        unansweredCount++;
                    }
                }
            });
        });

        if (unansweredCount > 0) {
            alert(`Lütfen tüm soruları cevaplayın veya N/A işaretleyin. ${unansweredCount} soru cevaplanmamış.`);
            return;
        }

        if (!confirm('Değerlendirmeyi göndermek istediğinize emin misiniz? Bu işlem geri alınamaz.')) {
            return;
        }

        self.isSubmitting(true);

        try {
            const currentUser = authService.getCurrentUser();

            // Prepare answers array
            const answers = [];
            self.evaluationSections().forEach(section => {
                section.questions.forEach(question => {
                    answers.push({
                        questionId: question.id,
                        answerText: question.answerText() || null,
                        answerNumeric: question.answerNumeric(),
                        isNA: question.answerIsNA()
                    });
                });
            });

            // Prepare DTO
            const dto = {
                assignmentId: self.currentAssignment().id,
                evaluatorId: currentUser.userId,
                answers: answers,
                notes: self.notes() || null
            };

            // Submit to API
            await apiService.post('/evaluation/submit', dto);

            alert('Değerlendirme başarıyla gönderildi!');

            // Reset state and reload assignments
            self.isEvaluating(false);
            self.currentAssignment(null);
            self.evaluationSections([]);
            self.notes('');
            await self.loadPendingAssignments();

        } catch (error) {
            console.error('Error submitting evaluation:', error);
            alert('Değerlendirme gönderilirken hata oluştu: ' + error.message);
        } finally {
            self.isSubmitting(false);
        }
    };

    // Initialize
    self.loadPendingAssignments();
}
