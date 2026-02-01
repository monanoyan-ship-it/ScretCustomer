/**
 * MyPerformance - Temsilcinin Kendi Performans Sayfası
 * CustomerOperator rolü için dinlenen çağrıları, puanları, karnesi ve yorumları gösterir
 */

function MyPerformanceViewModel() {
    var self = this;

    // State
    self.isLoading = ko.observable(true);
    self.evaluations = ko.observableArray([]);
    self.reportCard = ko.observable(null);
    self.selectedEvaluation = ko.observable(null);
    self.searchText = ko.observable('');

    // Pagination
    self.currentPage = ko.observable(1);
    self.pageSize = ko.observable(10);

    // Summary computed values (from evaluations)
    self.totalEvaluations = ko.computed(function() {
        return self.evaluations().length;
    });

    self.averageScore = ko.computed(function() {
        var evals = self.evaluations();
        if (evals.length === 0) return 0;
        var sum = evals.reduce(function(acc, e) {
            return acc + (e.scorePercentage || 0);
        }, 0);
        return sum / evals.length;
    });

    self.thisMonthScore = ko.computed(function() {
        var now = new Date();
        var thisMonth = now.getMonth();
        var thisYear = now.getFullYear();

        var monthEvals = self.evaluations().filter(function(e) {
            if (!e.completedAt) return false;
            var date = new Date(e.completedAt);
            return date.getMonth() === thisMonth && date.getFullYear() === thisYear;
        });

        if (monthEvals.length === 0) return 0;
        var sum = monthEvals.reduce(function(acc, e) {
            return acc + (e.scorePercentage || 0);
        }, 0);
        return sum / monthEvals.length;
    });

    self.totalYellowCards = ko.computed(function() {
        return self.evaluations().reduce(function(acc, e) {
            return acc + (e.yellowCardCount || 0);
        }, 0);
    });

    self.totalRedCards = ko.computed(function() {
        return self.evaluations().reduce(function(acc, e) {
            return acc + (e.redCardCount || 0);
        }, 0);
    });

    // Filtered evaluations (search)
    self.filteredEvaluations = ko.computed(function() {
        var search = (self.searchText() || '').toLowerCase();
        var evals = self.evaluations();

        if (search) {
            evals = evals.filter(function(e) {
                return (e.callId && e.callId.toLowerCase().indexOf(search) >= 0) ||
                       (e.projectName && e.projectName.toLowerCase().indexOf(search) >= 0) ||
                       (e.checklistName && e.checklistName.toLowerCase().indexOf(search) >= 0);
            });
        }

        // Apply pagination
        var start = (self.currentPage() - 1) * self.pageSize();
        return evals.slice(start, start + self.pageSize());
    });

    // Pagination computed
    self.totalPages = ko.computed(function() {
        var search = (self.searchText() || '').toLowerCase();
        var evals = self.evaluations();

        if (search) {
            evals = evals.filter(function(e) {
                return (e.callId && e.callId.toLowerCase().indexOf(search) >= 0) ||
                       (e.projectName && e.projectName.toLowerCase().indexOf(search) >= 0) ||
                       (e.checklistName && e.checklistName.toLowerCase().indexOf(search) >= 0);
            });
        }

        return Math.ceil(evals.length / self.pageSize()) || 1;
    });

    self.visiblePages = ko.computed(function() {
        var total = self.totalPages();
        var current = self.currentPage();
        var pages = [];

        var start = Math.max(1, current - 2);
        var end = Math.min(total, current + 2);

        for (var i = start; i <= end; i++) {
            pages.push(i);
        }

        return pages;
    });

    // Load evaluations
    self.loadEvaluations = function() {
        self.currentPage(1);

        fetch('/api/evaluations/my-evaluations', {
            method: 'GET',
            credentials: 'include'
        })
        .then(function(response) {
            if (!response.ok) {
                throw new Error('Network response was not ok');
            }
            return response.json();
        })
        .then(function(data) {
            self.evaluations(data || []);
        })
        .catch(function(error) {
            console.error('Error loading evaluations:', error);
            self.evaluations([]);
        });
    };

    // Load report card
    self.loadReportCard = function() {
        fetch('/api/customer/portal/reports/my-report-card', {
            method: 'GET',
            credentials: 'include'
        })
        .then(function(response) {
            if (!response.ok) {
                if (response.status === 404) {
                    // No data yet, that's ok
                    return null;
                }
                throw new Error('Network response was not ok');
            }
            return response.json();
        })
        .then(function(data) {
            self.reportCard(data);
        })
        .catch(function(error) {
            console.error('Error loading report card:', error);
            self.reportCard(null);
        });
    };

    // Load all data
    self.loadAll = function() {
        self.isLoading(true);

        Promise.all([
            fetch('/api/evaluations/my-evaluations', { credentials: 'include' }).then(function(r) { return r.ok ? r.json() : []; }),
            fetch('/api/customer/portal/reports/my-report-card', { credentials: 'include' }).then(function(r) { return r.ok ? r.json() : null; })
        ])
        .then(function(results) {
            self.evaluations(results[0] || []);
            self.reportCard(results[1]);
        })
        .catch(function(error) {
            console.error('Error loading data:', error);
            toastr.error('Veriler yuklenirken bir hata olustu.');
        })
        .finally(function() {
            self.isLoading(false);
        });
    };

    // Show detail modal
    self.showDetail = function(evaluation) {
        self.selectedEvaluation(evaluation);
        var modal = new bootstrap.Modal(document.getElementById('detailModal'));
        modal.show();
    };

    // Helper: Get score badge class
    self.getScoreBadgeClass = function(score) {
        if (score === null || score === undefined) return 'bg-secondary';
        if (score >= 80) return 'bg-success';
        if (score >= 60) return 'bg-warning text-dark';
        return 'bg-danger';
    };

    // Helper: Get score text class
    self.getScoreTextClass = function(score) {
        if (score === null || score === undefined) return 'text-secondary';
        if (score >= 80) return 'text-success';
        if (score >= 60) return 'text-warning';
        return 'text-danger';
    };

    // Helper: Copy to clipboard
    self.copyToClipboard = function(text) {
        if (!text) return;

        if (navigator.clipboard && window.isSecureContext) {
            navigator.clipboard.writeText(text).then(function() {
                toastr.success('Panoya kopyalandi');
            }).catch(function() {
                self.fallbackCopy(text);
            });
        } else {
            self.fallbackCopy(text);
        }
    };

    self.fallbackCopy = function(text) {
        var textarea = document.createElement('textarea');
        textarea.value = text;
        document.body.appendChild(textarea);
        textarea.select();
        document.execCommand('copy');
        document.body.removeChild(textarea);
        toastr.success('Panoya kopyalandi');
    };

    // Initialize
    self.loadAll();
}

// Apply bindings when document is ready
$(document).ready(function() {
    ko.applyBindings(new MyPerformanceViewModel(), document.getElementById('my-performance-app'));
});
