function CustomerReportsViewModel() {
    var self = this;

    // State
    self.isLoading = ko.observable(true);
    self.summary = ko.observable({});
    self.projectPerformance = ko.observableArray([]);
    self.monthlyTrend = ko.observableArray([]);
    self.trendChart = null;

    // Filters - default last 3 months
    var today = new Date();
    var threeMonthsAgo = new Date(today.getFullYear(), today.getMonth() - 3, 1);

    self.startDate = ko.observable(threeMonthsAgo.toISOString().split('T')[0]);
    self.endDate = ko.observable(today.toISOString().split('T')[0]);

    // Score Range Modal State
    self.isScoreRangeModalOpen = ko.observable(false);
    self.isScoreRangeLoading = ko.observable(false);
    self.scoreRangeModalTitle = ko.observable('');
    self.scoreRangeModalColor = ko.observable('primary');
    self.scoreRangeEvaluations = ko.observableArray([]);

    // Helpers
    self.getScoreBadgeClass = function(score) {
        if (score >= 90) return 'bg-success';
        if (score >= 80) return 'bg-primary';
        if (score >= 60) return 'bg-warning text-dark';
        return 'bg-danger';
    };

    // Build query string
    self.buildQueryString = function() {
        var params = [];
        if (self.startDate()) params.push('startDate=' + self.startDate());
        if (self.endDate()) params.push('endDate=' + self.endDate());
        return params.length > 0 ? '?' + params.join('&') : '';
    };

    // Load reports
    self.loadReports = function() {
        self.isLoading(true);
        var queryString = self.buildQueryString();

        Promise.all([
            customerApiFetch('/api/customer/portal/reports/summary' + queryString).then(function(r) {
                if (!r.ok) throw new Error('Summary API error: ' + r.status);
                return r.json();
            }),
            customerApiFetch('/api/customer/portal/reports/project-performance' + queryString).then(function(r) {
                if (!r.ok) throw new Error('Project performance API error: ' + r.status);
                return r.json();
            }),
            customerApiFetch('/api/customer/portal/reports/monthly-trend' + queryString).then(function(r) {
                if (!r.ok) throw new Error('Monthly trend API error: ' + r.status);
                return r.json();
            })
        ])
        .then(function(results) {
            self.summary(results[0] || {});
            self.projectPerformance(results[1] || []);
            self.monthlyTrend(results[2] || []);

            self.isLoading(false);

            // Initialize chart
            setTimeout(function() {
                self.initChart();
            }, 100);
        })
        .catch(function(error) {
            console.error('Reports load error:', error);
            self.isLoading(false);
        });
    };

    // Initialize trend chart
    self.initChart = function() {
        var ctx = document.getElementById('trendChart');
        if (!ctx) return;

        // Destroy existing chart
        if (self.trendChart) {
            self.trendChart.destroy();
        }

        var data = self.monthlyTrend();
        if (!data || data.length === 0) {
            return;
        }

        var labels = data.map(function(d) { return d.monthName; });
        var scores = data.map(function(d) { return d.averageScore; });
        var counts = data.map(function(d) { return d.count; });

        self.trendChart = new Chart(ctx, {
            type: 'bar',
            data: {
                labels: labels,
                datasets: [{
                    label: 'Ortalama Puan',
                    data: scores,
                    backgroundColor: 'rgba(25, 135, 84, 0.8)',
                    yAxisID: 'y'
                }, {
                    label: 'Değerlendirme Sayısı',
                    data: counts,
                    type: 'line',
                    borderColor: '#0d6efd',
                    backgroundColor: 'transparent',
                    yAxisID: 'y1'
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: {
                    legend: {
                        position: 'bottom',
                        labels: { boxWidth: 12 }
                    }
                },
                scales: {
                    y: {
                        beginAtZero: false,
                        min: 0,
                        max: 100,
                        title: { display: false }
                    },
                    y1: {
                        position: 'right',
                        beginAtZero: true,
                        grid: { drawOnChartArea: false },
                        title: { display: false }
                    }
                }
            }
        });
    };

    // Show Score Range Detail Modal
    self.showScoreRangeDetail = function(minScore, maxScore, title, color) {
        self.scoreRangeModalTitle(title);
        self.scoreRangeModalColor(color);
        self.isScoreRangeModalOpen(true);
        self.isScoreRangeLoading(true);
        self.scoreRangeEvaluations([]);

        var params = [];
        if (self.startDate()) params.push('startDate=' + self.startDate());
        if (self.endDate()) params.push('endDate=' + self.endDate());
        params.push('minScore=' + minScore);
        params.push('maxScore=' + maxScore);

        var url = '/api/customer/portal/reports/score-range?' + params.join('&');

        customerApiFetch(url)
            .then(function(r) {
                if (!r.ok) throw new Error('API error: ' + r.status);
                return r.json();
            })
            .then(function(data) {
                self.scoreRangeEvaluations(data || []);
            })
            .catch(function(error) {
                console.error('Score range load error:', error);
                toastr.error('Veriler yüklenirken hata oluştu.');
            })
            .finally(function() {
                self.isScoreRangeLoading(false);
            });
    };

    // Close Score Range Modal
    self.closeScoreRangeModal = function() {
        self.isScoreRangeModalOpen(false);
        self.scoreRangeEvaluations([]);
    };

    // Initialize
    self.loadReports();
}

// Apply bindings when DOM is ready
$(document).ready(function() {
    ko.applyBindings(new CustomerReportsViewModel(), document.getElementById('customer-reports-app'));
});
