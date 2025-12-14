function CustomerDashboardViewModel() {
    var self = this;

    // State
    self.isLoading = ko.observable(true);
    self.stats = ko.observable({});
    self.recentEvaluations = ko.observableArray([]);

    // Charts
    self.monthlyChart = null;
    self.scoreDistributionChart = null;

    // Helper functions
    self.getScoreBadgeClass = function(score) {
        if (score >= 80) return 'bg-success';
        if (score >= 60) return 'bg-warning';
        return 'bg-danger';
    };

    self.getStatusBadgeClass = function(status) {
        switch (status) {
            case 'Completed': return 'bg-success';
            case 'Draft': return 'bg-secondary';
            case 'Cancelled': return 'bg-danger';
            default: return 'bg-info';
        }
    };

    // Chart data
    self.monthlyTrendData = null;
    self.scoreDistributionData = null;

    // Load dashboard data
    self.loadDashboard = function() {
        self.isLoading(true);

        // Get user info
        var userInfo = localStorage.getItem('customerUser');
        if (!userInfo) {
            window.location.href = '/CustomerPortal/Login';
            return;
        }

        // Load all data in parallel
        Promise.all([
            customerApiFetch('/api/customer/portal/dashboard/stats').then(function(r) {
                if (!r.ok) throw new Error('Stats API error: ' + r.status);
                return r.json();
            }),
            customerApiFetch('/api/customer/portal/evaluations/recent?count=5').then(function(r) {
                if (!r.ok) throw new Error('Recent evaluations API error: ' + r.status);
                return r.json();
            }),
            customerApiFetch('/api/customer/portal/dashboard/monthly-trend').then(function(r) {
                if (!r.ok) throw new Error('Monthly trend API error: ' + r.status);
                return r.json();
            }),
            customerApiFetch('/api/customer/portal/dashboard/score-distribution').then(function(r) {
                if (!r.ok) throw new Error('Score distribution API error: ' + r.status);
                return r.json();
            })
        ])
        .then(function(results) {
            var stats = results[0];
            var evaluations = results[1];
            self.monthlyTrendData = results[2];
            self.scoreDistributionData = results[3];

            console.log('Dashboard data loaded:', {
                stats: stats,
                evaluations: evaluations,
                monthlyTrend: self.monthlyTrendData,
                scoreDistribution: self.scoreDistributionData
            });

            self.stats({
                totalEvaluations: stats.totalEvaluations || 0,
                averageScore: stats.averageScore || 0,
                branchCount: stats.branchCount || 0,
                thisMonthEvaluations: stats.thisMonthEvaluations || 0
            });

            self.recentEvaluations(evaluations || []);

            self.isLoading(false);

            // Initialize charts after data is loaded
            setTimeout(function() {
                self.initCharts();
            }, 100);
        })
        .catch(function(error) {
            console.error('Dashboard load error:', error);
            self.isLoading(false);
        });
    };

    // Initialize charts
    self.initCharts = function() {
        console.log('Initializing charts with data:', {
            monthlyTrend: self.monthlyTrendData,
            scoreDistribution: self.scoreDistributionData
        });

        // Prepare monthly trend data
        var monthLabels = [];
        var scoreData = [];
        var countData = [];

        if (self.monthlyTrendData && self.monthlyTrendData.length > 0) {
            self.monthlyTrendData.forEach(function(item) {
                monthLabels.push(item.month);
                scoreData.push(item.averageScore);
                countData.push(item.count);
            });
        }

        // Monthly trend chart - only if we have data
        var monthlyCtx = document.getElementById('monthlyChart');
        if (monthlyCtx && monthLabels.length > 0) {
            self.monthlyChart = new Chart(monthlyCtx, {
                type: 'line',
                data: {
                    labels: monthLabels,
                    datasets: [{
                        label: 'Ortalama Puan',
                        data: scoreData,
                        borderColor: '#198754',
                        backgroundColor: 'rgba(25, 135, 84, 0.1)',
                        fill: true,
                        tension: 0.4
                    }, {
                        label: 'Değerlendirme Sayısı',
                        data: countData,
                        borderColor: '#0d6efd',
                        backgroundColor: 'transparent',
                        borderDash: [5, 5],
                        tension: 0.4,
                        yAxisID: 'y1'
                    }]
                },
                options: {
                    responsive: true,
                    maintainAspectRatio: false,
                    plugins: {
                        legend: {
                            position: 'bottom'
                        }
                    },
                    scales: {
                        y: {
                            beginAtZero: false,
                            min: 0,
                            max: 100,
                            title: {
                                display: true,
                                text: 'Puan'
                            }
                        },
                        y1: {
                            position: 'right',
                            beginAtZero: true,
                            grid: {
                                drawOnChartArea: false
                            },
                            title: {
                                display: true,
                                text: 'Adet'
                            }
                        }
                    }
                }
            });
        }

        // Score distribution chart - only if we have data
        var scoreCtx = document.getElementById('scoreDistributionChart');
        var dist = self.scoreDistributionData;
        if (scoreCtx && dist) {
            var chartData = [dist.excellent || 0, dist.good || 0, dist.average || 0, dist.poor || 0];
            var total = chartData.reduce(function(a, b) { return a + b; }, 0);

            if (total > 0) {
                self.scoreDistributionChart = new Chart(scoreCtx, {
                    type: 'doughnut',
                    data: {
                        labels: ['Mükemmel (90+)', 'İyi (80-89)', 'Orta (60-79)', 'Düşük (<60)'],
                        datasets: [{
                            data: chartData,
                            backgroundColor: [
                                '#198754',
                                '#0d6efd',
                                '#ffc107',
                                '#dc3545'
                            ]
                        }]
                    },
                    options: {
                        responsive: true,
                        maintainAspectRatio: false,
                        plugins: {
                            legend: {
                                position: 'bottom'
                            }
                        }
                    }
                });
            } else {
                console.log('No score distribution data to display');
            }
        }
        console.log('Charts initialized');
    };

    // Initialize
    self.loadDashboard();
}

// Apply bindings when DOM is ready
$(document).ready(function() {
    ko.applyBindings(new CustomerDashboardViewModel(), document.getElementById('customer-dashboard-app'));
});
