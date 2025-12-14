// MyAssignments/Index.js
(function () {
    'use strict';

    function AssignmentViewModel(data) {
        const self = this;

        // Assignment properties
        self.Id = data.id;
        self.Project = data.project;
        self.Checklist = data.checklist;
        self.Branch = data.branch;
        self.DueDate = data.dueDate;
        self.IsCompleted = data.isCompleted;
        self.CompletedAt = data.completedAt;
        self.UniqueLink = data.uniqueLink;

        // Computed: Check if overdue
        self.isOverdue = function () {
            if (self.IsCompleted) return false;
            const now = new Date();
            const dueDate = new Date(self.DueDate);
            return dueDate < now;
        };
    }

    function MyAssignmentsViewModel() {
        const self = this;

        // Observable arrays
        self.assignments = ko.observableArray([]);
        self.selectedAssignment = ko.observable(null);

        // Format date helper
        self.formatDate = function (dateString) {
            if (!dateString) return '-';
            const date = new Date(dateString);
            return date.toLocaleDateString('tr-TR', {
                year: 'numeric',
                month: 'long',
                day: 'numeric',
                hour: '2-digit',
                minute: '2-digit'
            });
        };

        // Start evaluation
        self.startEvaluation = function (assignment) {
            self.selectedAssignment(assignment);
            const modal = new bootstrap.Modal(document.getElementById('evaluationModal'));
            modal.show();
        };

        // View completed evaluation
        self.viewEvaluation = function (assignment) {
            self.selectedAssignment(assignment);
            const modal = new bootstrap.Modal(document.getElementById('evaluationModal'));
            modal.show();
        };

        // Save evaluation
        self.saveEvaluation = function () {
            const assignment = self.selectedAssignment();
            if (!assignment) return;

            // TODO: Implement save logic
            toastr.info('Değerlendirme kaydetme fonksiyonu geliştirilecek.');
        };

        // Load assignments
        self.loadAssignments = function () {
            fetch('/api/assignments/my-assignments', { credentials: 'include' })
                .then(response => {
                    if (!response.ok) throw new Error('Yükleme başarısız');
                    return response.json();
                })
                .then(data => {
                    const mappedAssignments = data.map(function (item) {
                        return new AssignmentViewModel({
                            id: item.id,
                            project: item.project ? { name: item.project.name } : null,
                            checklist: item.checklist ? { name: item.checklist.name } : null,
                            branch: item.branch ? { name: item.branch.name } : null,
                            dueDate: item.dueDate,
                            isCompleted: item.isCompleted,
                            completedAt: item.completedAt,
                            uniqueLink: item.uniqueLink
                        });
                    });
                    self.assignments(mappedAssignments);
                })
                .catch(error => {
                    console.error('Error loading assignments:', error);
                });
        };
    }

    // Initialize on document ready
    $(document).ready(function () {
        const viewModel = new MyAssignmentsViewModel();
        viewModel.loadAssignments();
        ko.applyBindings(viewModel);
    });
})();
