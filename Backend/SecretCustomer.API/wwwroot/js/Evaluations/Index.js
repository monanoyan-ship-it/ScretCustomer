// Evaluations List Component ViewModel
function EvaluationsListViewModel() {
    var self = this;

    self.isLoading = ko.observable(true);
    self.errorMessage = ko.observable('');
    self.evaluations = ko.observableArray([]);
    self.selectedEvaluation = ko.observable(null);

    self.loadEvaluations = function() {
        self.isLoading(true);
        self.errorMessage('');

        fetch('/api/evaluations/evaluator', {
            credentials: 'include'
        })
        .then(res => {
            if (!res.ok) throw new Error('Yükleme başarısız');
            return res.json();
        })
        .then(data => {
            self.evaluations(data);
        })
        .catch(error => {
            console.error('Error:', error);
            self.errorMessage('Değerlendirmeler yüklenirken bir hata oluştu.');
        })
        .finally(() => {
            self.isLoading(false);
        });
    };

    self.openEvaluation = function(evaluation) {
        self.selectedEvaluation(evaluation);
        var modal = new bootstrap.Modal(document.getElementById('evaluationModal'));
        modal.show();
    };

    self.loadEvaluations();
}

// Apply binding to component
$(document).ready(function() {
    ko.applyBindings(
        new EvaluationsListViewModel(),
        document.getElementById('evaluations-list-component')
    );
});
