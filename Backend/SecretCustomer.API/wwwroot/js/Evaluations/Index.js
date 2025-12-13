// Evaluations Index ViewModel - Cagri Denetleme Listesi
function EvaluationsIndexViewModel() {
    var self = this;

    // State
    self.isLoading = ko.observable(true);
    self.errorMessage = ko.observable('');
    self.activeTab = ko.observable('pending');
    self.filterStatus = ko.observable('');
    self.searchTerm = ko.observable('');

    // Data
    self.allAssignments = ko.observableArray([]);
    self.allEvaluations = ko.observableArray([]);

    // Computed - Pending Assignments (no evaluation yet)
    self.pendingAssignments = ko.computed(function() {
        var assignments = self.allAssignments();
        var evaluationAssignmentIds = self.allEvaluations().map(function(e) { return e.assignmentId; });
        var search = self.searchTerm().toLowerCase();

        return assignments.filter(function(a) {
            // Filter out completed assignments
            if (a.isCompleted) return false;
            // Filter out assignments that already have evaluations
            if (evaluationAssignmentIds.indexOf(a.id) >= 0) return false;
            // Search filter
            if (search) {
                var matchesSearch = (a.projectName || '').toLowerCase().indexOf(search) >= 0 ||
                                   (a.branchName || '').toLowerCase().indexOf(search) >= 0 ||
                                   (a.checklistName || '').toLowerCase().indexOf(search) >= 0;
                if (!matchesSearch) return false;
            }
            return true;
        });
    });

    // Computed - Completed Evaluations
    self.completedEvaluations = ko.computed(function() {
        var search = self.searchTerm().toLowerCase();
        return self.allEvaluations().filter(function(e) {
            if (e.status !== 'Completed') return false;
            if (search) {
                var matchesSearch = (e.projectName || '').toLowerCase().indexOf(search) >= 0 ||
                                   (e.branchName || '').toLowerCase().indexOf(search) >= 0;
                if (!matchesSearch) return false;
            }
            return true;
        });
    });

    // Computed - Draft Evaluations
    self.draftEvaluations = ko.computed(function() {
        var search = self.searchTerm().toLowerCase();
        return self.allEvaluations().filter(function(e) {
            if (e.status !== 'Draft' && e.status !== 'InProgress') return false;
            if (search) {
                var matchesSearch = (e.projectName || '').toLowerCase().indexOf(search) >= 0 ||
                                   (e.branchName || '').toLowerCase().indexOf(search) >= 0;
                if (!matchesSearch) return false;
            }
            return true;
        });
    });

    // Load data
    self.loadEvaluations = function() {
        self.isLoading(true);
        self.errorMessage('');

        // Load both assignments and evaluations in parallel
        Promise.all([
            fetch('/api/assignments', { credentials: 'include' }).then(function(r) { return r.json(); }),
            fetch('/api/evaluations/evaluator', { credentials: 'include' }).then(function(r) { return r.json(); })
        ])
        .then(function(results) {
            self.allAssignments(results[0] || []);
            self.allEvaluations(results[1] || []);
        })
        .catch(function(error) {
            console.error('Load error:', error);
            self.errorMessage('Veriler yuklenirken bir hata olustu.');
        })
        .finally(function() {
            self.isLoading(false);
        });
    };

    // Initialize
    self.loadEvaluations();
}

// Apply bindings when DOM is ready
document.addEventListener('DOMContentLoaded', function() {
    ko.applyBindings(new EvaluationsIndexViewModel(), document.getElementById('evaluations-app'));
});
