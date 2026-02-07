// Public Evaluation Report ViewModel
function ReportViewModel(token) {
    var self = this;

    // State
    self.isLoading = ko.observable(true);
    self.errorMessage = ko.observable('');
    self.reportType = ko.observable(''); // 'single', 'bulk', 'personnel'

    // Single evaluation
    self.singleEvaluation = ko.observable(null);

    // Bulk/Personnel list
    self.evaluations = ko.observableArray([]);
    self.headerTitle = ko.observable('');
    self.dateRange = ko.observable('');

    // Detail modal (for list items)
    self.isDetailModalOpen = ko.observable(false);
    self.isDetailLoading = ko.observable(false);
    self.detailData = ko.observable(null);

    // Score helpers
    self.getScoreClass = function(score) {
        if (score >= 80) return 'text-success';
        if (score >= 60) return 'text-warning';
        return 'text-danger';
    };

    self.getProgressBarClass = function(score) {
        if (score >= 80) return 'bg-success';
        if (score >= 60) return 'bg-warning';
        return 'bg-danger';
    };

    self.getScoreBadgeClass = function(score) {
        if (score >= 80) return 'bg-success';
        if (score >= 60) return 'bg-warning';
        return 'bg-danger';
    };

    self.formatDate = function(dateStr) {
        if (!dateStr) return '-';
        return new Date(dateStr).toLocaleDateString('tr-TR');
    };

    // Load report data
    self.loadReport = function() {
        self.isLoading(true);
        self.errorMessage('');

        fetch('/api/public/report/' + token)
            .then(function(response) {
                if (!response.ok) {
                    return response.json().then(function(data) {
                        throw new Error(data.message || 'Rapor yüklenemedi.');
                    });
                }
                return response.json();
            })
            .then(function(data) {
                self.reportType(data.type);

                if (data.type === 'single') {
                    self.singleEvaluation(data.evaluation);
                } else if (data.type === 'bulk') {
                    self.headerTitle(data.customerName + ' - Değerlendirmeler');
                    self.dateRange(self.formatDate(data.startDate) + ' - ' + self.formatDate(data.endDate));
                    self.evaluations(data.evaluations || []);
                } else if (data.type === 'personnel') {
                    self.headerTitle(data.personnelName + ' - Değerlendirmeler');
                    self.dateRange(self.formatDate(data.startDate) + ' - ' + self.formatDate(data.endDate));
                    self.evaluations(data.evaluations || []);
                }
            })
            .catch(function(error) {
                console.error('Report load error:', error);
                self.errorMessage(error.message || 'Rapor yüklenirken bir hata oluştu.');
            })
            .finally(function() {
                self.isLoading(false);
            });
    };

    // Show detail for a list item
    self.showDetail = function(evaluation) {
        self.isDetailModalOpen(true);
        self.isDetailLoading(true);
        self.detailData(null);

        fetch('/api/public/report/' + token + '/evaluation/' + evaluation.id)
            .then(function(response) {
                if (!response.ok) {
                    return response.json().then(function(data) {
                        throw new Error(data.message || 'Detay yüklenemedi.');
                    });
                }
                return response.json();
            })
            .then(function(data) {
                self.detailData(data);
            })
            .catch(function(error) {
                console.error('Detail load error:', error);
                toastr.error(error.message || 'Değerlendirme detayı yüklenirken hata oluştu.');
                self.closeDetailModal();
            })
            .finally(function() {
                self.isDetailLoading(false);
            });
    };

    self.closeDetailModal = function() {
        self.isDetailModalOpen(false);
        self.detailData(null);
    };

    // ESC key handler
    document.addEventListener('keydown', function(e) {
        if (e.key === 'Escape' && self.isDetailModalOpen()) {
            self.closeDetailModal();
        }
    });

    // Initialize
    self.loadReport();
}

$(document).ready(function() {
    var vm = new ReportViewModel(REPORT_TOKEN);
    ko.applyBindings(vm, document.getElementById('report-app'));
});
