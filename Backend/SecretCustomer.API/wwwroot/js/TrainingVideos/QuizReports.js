function QuizReportsViewModel() {
    var self = this;

    // State
    self.isLoading = ko.observable(true);
    self.isExporting = ko.observable(false);
    self.responses = ko.observableArray([]);
    self._skipSubscription = false; // Subscription tetiklemeyi önlemek için

    // Filter options
    self.videos = ko.observableArray([]);
    self.quizzes = ko.observableArray([]);

    // Filters
    self.filterVideoId = ko.observable('');
    self.filterQuizId = ko.observable('');
    self.filterPassed = ko.observable('');
    self.filterIsExternal = ko.observable('');

    // Details modal
    self.isDetailsModalOpen = ko.observable(false);
    self.selectedResponse = ko.observable(null);

    // Computed stats
    self.passedCount = ko.computed(function() {
        return self.responses().filter(function(r) { return r.isPassed; }).length;
    });

    self.failedCount = ko.computed(function() {
        return self.responses().filter(function(r) { return !r.isPassed; }).length;
    });

    self.averageScore = ko.computed(function() {
        var items = self.responses();
        if (items.length === 0) return 0;
        var sum = items.reduce(function(acc, r) { return acc + r.scorePercentage; }, 0);
        return sum / items.length;
    });

    // Load filter options
    self.loadFilterOptions = function() {
        // Load videos
        fetch('/api/training-videos')
            .then(function(r) { return r.json(); })
            .then(function(data) {
                self.videos(data || []);
            });

        // Load quizzes
        fetch('/api/training-quiz')
            .then(function(r) { return r.json(); })
            .then(function(data) {
                self.quizzes(data || []);
            });
    };

    // Load responses
    self.loadResponses = function() {
        self.isLoading(true);

        var url = '/api/training-quiz/responses?';
        var params = [];

        if (self.filterQuizId()) {
            params.push('quizId=' + self.filterQuizId());
        }
        if (self.filterVideoId()) {
            params.push('videoId=' + self.filterVideoId());
        }
        if (self.filterPassed() !== '') {
            params.push('isPassed=' + self.filterPassed());
        }
        if (self.filterIsExternal() !== '') {
            params.push('isExternal=' + self.filterIsExternal());
        }

        url += params.join('&');

        fetch(url)
            .then(function(r) { return r.json(); })
            .then(function(data) {
                self.responses(data || []);
            })
            .catch(function(error) {
                console.error('Error loading responses:', error);
                toastr.error('Veriler yuklenirken hata olustu');
            })
            .finally(function() {
                self.isLoading(false);
            });
    };

    // Clear filters
    self.clearFilters = function() {
        self._skipSubscription = true;
        self.filterVideoId('');
        self.filterQuizId('');
        self.filterPassed('');
        self.filterIsExternal('');
        self._skipSubscription = false;
        self.loadResponses();
    };

    // Show details modal
    self.showDetails = function(response) {
        // Load full response details with answers
        fetch('/api/training-quiz/responses/' + response.id)
            .then(function(r) { return r.json(); })
            .then(function(data) {
                self.selectedResponse(data);
                self.isDetailsModalOpen(true);
            })
            .catch(function(error) {
                console.error('Error loading response details:', error);
                toastr.error('Detay yuklenirken hata olustu');
            });
    };

    self.closeDetailsModal = function() {
        self.isDetailsModalOpen(false);
        self.selectedResponse(null);
    };

    // Export to Excel
    self.exportToExcel = function() {
        self.isExporting(true);

        var url = '/api/training-quiz/responses/export?';
        var params = [];

        if (self.filterQuizId()) {
            params.push('quizId=' + self.filterQuizId());
        }
        if (self.filterVideoId()) {
            params.push('videoId=' + self.filterVideoId());
        }
        if (self.filterPassed() !== '') {
            params.push('isPassed=' + self.filterPassed());
        }
        if (self.filterIsExternal() !== '') {
            params.push('isExternal=' + self.filterIsExternal());
        }

        url += params.join('&');

        fetch(url)
            .then(function(response) {
                if (!response.ok) throw new Error('Export failed');
                return response.blob();
            })
            .then(function(blob) {
                var filename = 'Anket_Raporlari_' + new Date().toISOString().slice(0, 10).replace(/-/g, '') + '.xlsx';
                var link = document.createElement('a');
                link.href = window.URL.createObjectURL(blob);
                link.download = filename;
                link.click();
                toastr.success('Rapor indirildi');
            })
            .catch(function(error) {
                console.error('Export error:', error);
                toastr.error('Export sirasinda hata olustu');
            })
            .finally(function() {
                self.isExporting(false);
            });
    };

    // Filter subscriptions (değişiklikte otomatik yükle)
    var onFilterChange = function() {
        if (!self._skipSubscription) {
            self.loadResponses();
        }
    };
    self.filterVideoId.subscribe(onFilterChange);
    self.filterQuizId.subscribe(onFilterChange);
    self.filterPassed.subscribe(onFilterChange);
    self.filterIsExternal.subscribe(onFilterChange);

    // Initialize
    self.loadFilterOptions();
    self.loadResponses();
}

$(document).ready(function() {
    ko.applyBindings(new QuizReportsViewModel(), document.getElementById('quiz-reports-app'));
});
