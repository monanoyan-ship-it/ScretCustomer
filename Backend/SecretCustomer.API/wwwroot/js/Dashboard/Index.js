// Dashboard ViewModel with Chart.js Integration
function DashboardViewModel() {
    var self = this;

    // State
    self.isLoading = ko.observable(false);
    self.errorMessage = ko.observable('');

    // Filters
    self.startDate = ko.observable('');
    self.endDate = ko.observable('');

    // Data
    self.stats = ko.observable({
        totalEvaluations: 0,
        averageScore: 0,
        percentageChange: 0,
        monthlyTrends: []
    });

    // Scorecard data
    self.scorecard = ko.observable({
        userName: '',
        role: '',
        currentMonthEvaluations: 0,
        currentMonthAverage: 0,
        lastMonthEvaluations: 0,
        lastMonthAverage: 0,
        totalEvaluations: 0,
        totalAverage: 0,
        monthlyChange: 0,
        teamAverage: 0,
        companyAverage: 0,
        userRank: 0,
        totalUsers: 0,
        recentEvaluations: []
    });

    // Daily metrics data (Günlük dinleme metrikleri)
    self.dailyMetrics = ko.observable({
        todayEvaluations: 0,
        thisWeekEvaluations: 0,
        thisMonthEvaluations: 0,
        dailyTarget: 55,
        dailyTargetPercentage: 0,
        todayAverageScore: 0,
        thisWeekAverageScore: 0,
        dailyTrends: []
    });

    // User performance data (Kullanıcı performansı)
    self.userPerformance = ko.observable({
        topEvaluatorsToday: [],
        topEvaluatorsMonth: [],
        userRankings: []
    });

    // Target progress data (Hedef takibi)
    self.targetProgress = ko.observable({
        currentPeriodName: null,
        periodStartDate: null,
        periodEndDate: null,
        periodTarget: 0,
        periodCompleted: 0,
        periodPercentage: 0,
        remaining: 0,
        dailyTarget: 55,
        todayCompleted: 0,
        projectTargets: []
    });

    // Announcements
    self.announcements = ko.observableArray([]);

    // Chart instances
    var monthlyTrendChart = null;

    // Flag to track if user has admin access
    self.hasAdminAccess = ko.observable(true);

    // Load dashboard data
    self.loadDashboard = function() {
        self.isLoading(true);
        self.errorMessage('');

        var url = '/api/dashboard/admin';
        var params = [];

        if (self.startDate()) {
            params.push('startDate=' + self.startDate());
        }
        if (self.endDate()) {
            params.push('endDate=' + self.endDate());
        }

        if (params.length > 0) {
            url += '?' + params.join('&');
        }

        fetch(url, { credentials: 'include' })
            .then(function(response) {
                if (!response.ok) {
                    // Any non-OK response from admin endpoint means no admin access
                    console.log('Dashboard admin API status:', response.status);
                    self.hasAdminAccess(false);
                    return null;
                }
                return response.json();
            })
            .then(function(data) {
                if (data) {
                    self.stats(data);
                    self.updateCharts(data);
                }
            })
            .catch(function(error) {
                console.error('Dashboard error:', error);
                // Don't show error - just hide admin sections
                self.hasAdminAccess(false);
            })
            .finally(function() {
                self.isLoading(false);
            });
    };

    // Update charts with new data
    self.updateCharts = function(data) {
        self.updateMonthlyTrendChart(data.monthlyTrends || []);
    };

    // Monthly Trend Chart (Line Chart)
    self.updateMonthlyTrendChart = function(monthlyTrends) {
        var ctx = document.getElementById('monthlyTrendChart');
        if (!ctx) return;

        // Destroy existing chart
        if (monthlyTrendChart) {
            monthlyTrendChart.destroy();
        }

        var labels = monthlyTrends.map(function(m) { return m.monthName + ' ' + m.year; });
        var scores = monthlyTrends.map(function(m) { return m.averageScore; });
        var counts = monthlyTrends.map(function(m) { return m.evaluationCount; });

        monthlyTrendChart = new Chart(ctx, {
            type: 'line',
            data: {
                labels: labels,
                datasets: [
                    {
                        label: T('Dashboard.AverageScore', 'Ortalama Puan (%)'),
                        data: scores,
                        borderColor: 'rgb(75, 192, 192)',
                        backgroundColor: 'rgba(75, 192, 192, 0.1)',
                        tension: 0.3,
                        fill: true,
                        yAxisID: 'y'
                    },
                    {
                        label: T('Dashboard.EvaluationCount', 'Değerlendirme Sayısı'),
                        data: counts,
                        borderColor: 'rgb(54, 162, 235)',
                        backgroundColor: 'rgba(54, 162, 235, 0.1)',
                        tension: 0.3,
                        fill: false,
                        yAxisID: 'y1'
                    }
                ]
            },
            options: {
                responsive: true,
                maintainAspectRatio: true,
                interaction: {
                    mode: 'index',
                    intersect: false
                },
                plugins: {
                    legend: {
                        position: 'top'
                    },
                    tooltip: {
                        callbacks: {
                            label: function(context) {
                                var label = context.dataset.label || '';
                                var value = context.raw;
                                if (context.datasetIndex === 0) {
                                    return label + ': ' + value.toFixed(1) + '%';
                                }
                                return label + ': ' + value;
                            }
                        }
                    }
                },
                scales: {
                    y: {
                        type: 'linear',
                        display: true,
                        position: 'left',
                        title: {
                            display: true,
                            text: T('Dashboard.Score', 'Puan (%)')
                        },
                        min: 0,
                        max: 100
                    },
                    y1: {
                        type: 'linear',
                        display: true,
                        position: 'right',
                        title: {
                            display: true,
                            text: T('Dashboard.Evaluation', 'Değerlendirme')
                        },
                        grid: {
                            drawOnChartArea: false
                        }
                    }
                }
            }
        });
    };

    // Load scorecard
    self.loadScorecard = function() {
        fetch('/api/dashboard/scorecard', { credentials: 'include' })
            .then(function(response) {
                if (!response.ok) {
                    // Return empty scorecard on error
                    return {
                        userName: T('Dashboard.User', 'Kullanıcı'),
                        role: '',
                        currentMonthEvaluations: 0,
                        currentMonthAverage: 0,
                        lastMonthEvaluations: 0,
                        lastMonthAverage: 0,
                        totalEvaluations: 0,
                        totalAverage: 0,
                        monthlyChange: 0,
                        teamAverage: 0,
                        companyAverage: 0,
                        userRank: 0,
                        totalUsers: 0,
                        recentEvaluations: []
                    };
                }
                return response.json();
            })
            .then(function(data) {
                self.scorecard(data);
            })
            .catch(function(error) {
                console.error('Scorecard error:', error);
            });
    };

    // Load announcements
    self.loadAnnouncements = function() {
        fetch('/api/announcements/dashboard?count=5', { credentials: 'include' })
            .then(function(response) {
                if (!response.ok) throw new Error(T('Dashboard.AnnouncementsLoadError', 'Duyurular yüklenemedi'));
                return response.json();
            })
            .then(function(data) {
                self.announcements(data);
            })
            .catch(function(error) {
                console.error('Announcements error:', error);
            });
    };

    // Get announcement type class for badge
    self.getAnnouncementTypeClass = function(type) {
        switch (type) {
            case 1: return 'bg-warning text-dark'; // Warning
            case 2: return 'bg-success'; // Success
            case 3: return 'bg-danger'; // Important
            case 4: return 'bg-primary'; // News
            case 5: return 'bg-secondary'; // System
            default: return 'bg-info'; // Info
        }
    };

    // Get announcement type name
    self.getAnnouncementTypeName = function(type) {
        switch (type) {
            case 1: return T('Common.Warning', 'Uyarı');
            case 2: return T('Common.Success', 'Başarı');
            case 3: return T('Common.Important', 'Önemli');
            case 4: return T('Common.News', 'Haber');
            case 5: return T('Common.System', 'Sistem');
            default: return T('Common.Info', 'Bilgi');
        }
    };

    // Format date
    self.formatDate = function(dateStr) {
        if (!dateStr) return '';
        var date = new Date(dateStr);
        return date.toLocaleDateString('tr-TR');
    };

    // Load daily metrics
    self.loadDailyMetrics = function() {
        fetch('/api/dashboard/daily-metrics', { credentials: 'include' })
            .then(function(response) {
                if (!response.ok) throw new Error('Daily metrics load error');
                return response.json();
            })
            .then(function(data) {
                self.dailyMetrics(data);
                self.updateDailyTrendChart(data.dailyTrends || []);
            })
            .catch(function(error) {
                console.error('Daily metrics error:', error);
            });
    };

    // Load user performance
    self.loadUserPerformance = function() {
        fetch('/api/dashboard/user-performance', { credentials: 'include' })
            .then(function(response) {
                if (!response.ok) throw new Error('User performance load error');
                return response.json();
            })
            .then(function(data) {
                self.userPerformance(data);
            })
            .catch(function(error) {
                console.error('User performance error:', error);
            });
    };

    // Load target progress
    self.loadTargetProgress = function() {
        fetch('/api/dashboard/target-progress', { credentials: 'include' })
            .then(function(response) {
                if (!response.ok) throw new Error('Target progress load error');
                return response.json();
            })
            .then(function(data) {
                self.targetProgress(data);
            })
            .catch(function(error) {
                console.error('Target progress error:', error);
            });
    };

    // Daily Trend Chart (last 7 days)
    var dailyTrendChart = null;
    self.updateDailyTrendChart = function(dailyTrends) {
        var ctx = document.getElementById('dailyTrendChart');
        if (!ctx) return;

        if (dailyTrendChart) {
            dailyTrendChart.destroy();
        }

        var labels = dailyTrends.map(function(d) { return d.dayName; });
        var counts = dailyTrends.map(function(d) { return d.evaluationCount; });
        var target = self.dailyMetrics().dailyTarget || 55;

        dailyTrendChart = new Chart(ctx, {
            type: 'bar',
            data: {
                labels: labels,
                datasets: [
                    {
                        label: T('Dashboard.EvaluationCount', 'Değerlendirme'),
                        data: counts,
                        backgroundColor: counts.map(function(c) {
                            return c >= target ? 'rgba(40, 167, 69, 0.7)' : 'rgba(255, 193, 7, 0.7)';
                        }),
                        borderColor: counts.map(function(c) {
                            return c >= target ? 'rgb(40, 167, 69)' : 'rgb(255, 193, 7)';
                        }),
                        borderWidth: 1
                    }
                ]
            },
            options: {
                responsive: true,
                maintainAspectRatio: true,
                plugins: {
                    legend: { display: false },
                    annotation: {
                        annotations: {
                            targetLine: {
                                type: 'line',
                                yMin: target,
                                yMax: target,
                                borderColor: 'rgb(220, 53, 69)',
                                borderWidth: 2,
                                borderDash: [5, 5],
                                label: {
                                    enabled: true,
                                    content: T('Dashboard.Target', 'Hedef') + ': ' + target
                                }
                            }
                        }
                    }
                },
                scales: {
                    y: {
                        beginAtZero: true,
                        title: {
                            display: true,
                            text: T('Dashboard.EvaluationCount', 'Değerlendirme')
                        }
                    }
                }
            }
        });
    };

    // Calculate target bar color
    self.getTargetBarColor = function(percentage) {
        if (percentage >= 100) return 'bg-success';
        if (percentage >= 75) return 'bg-info';
        if (percentage >= 50) return 'bg-warning';
        return 'bg-danger';
    };

    // Initialize - Dashboard verilerini yükle
    self.loadDashboard();
    self.loadScorecard();
    self.loadAnnouncements();
    self.loadDailyMetrics();
    self.loadUserPerformance();
    self.loadTargetProgress();
}

// Apply bindings when document is ready
$(document).ready(function() {
    ko.applyBindings(new DashboardViewModel(), document.getElementById('dashboard-app'));
});
