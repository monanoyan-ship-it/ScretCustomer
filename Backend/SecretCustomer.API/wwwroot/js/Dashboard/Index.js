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
        topBranches: [],
        bottomBranches: [],
        monthlyTrends: [],
        branchComparisons: []
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

    // Announcements
    self.announcements = ko.observableArray([]);

    // Branch search
    self.branchSearchTerm = ko.observable('');

    // Filtered branches for table
    self.filteredBranches = ko.computed(function() {
        var searchTerm = self.branchSearchTerm().toLowerCase();
        var branches = self.stats().branchComparisons || [];

        if (!searchTerm) {
            return branches;
        }

        return branches.filter(function(branch) {
            return branch.branchName.toLowerCase().indexOf(searchTerm) !== -1 ||
                   branch.region.toLowerCase().indexOf(searchTerm) !== -1;
        });
    });

    // Chart instances
    var monthlyTrendChart = null;
    var branchComparisonChart = null;

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
                    throw new Error('Dashboard verileri yüklenemedi');
                }
                return response.json();
            })
            .then(function(data) {
                self.stats(data);
                self.updateCharts(data);
            })
            .catch(function(error) {
                console.error('Dashboard error:', error);
                self.errorMessage(error.message || 'Dashboard verileri yüklenirken bir hata oluştu.');
            })
            .finally(function() {
                self.isLoading(false);
            });
    };

    // Update charts with new data
    self.updateCharts = function(data) {
        self.updateMonthlyTrendChart(data.monthlyTrends || []);
        self.updateBranchComparisonChart(data.topBranches || []);
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
                        label: 'Ortalama Puan (%)',
                        data: scores,
                        borderColor: 'rgb(75, 192, 192)',
                        backgroundColor: 'rgba(75, 192, 192, 0.1)',
                        tension: 0.3,
                        fill: true,
                        yAxisID: 'y'
                    },
                    {
                        label: 'Değerlendirme Sayısı',
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
                            text: 'Puan (%)'
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
                            text: 'Değerlendirme'
                        },
                        grid: {
                            drawOnChartArea: false
                        }
                    }
                }
            }
        });
    };

    // Branch Comparison Chart (Horizontal Bar Chart)
    self.updateBranchComparisonChart = function(topBranches) {
        var ctx = document.getElementById('branchComparisonChart');
        if (!ctx) return;

        // Destroy existing chart
        if (branchComparisonChart) {
            branchComparisonChart.destroy();
        }

        var labels = topBranches.map(function(b) { return b.branchName; });
        var scores = topBranches.map(function(b) { return b.averageScore; });

        // Generate colors based on score
        var backgroundColors = scores.map(function(score) {
            if (score >= 70) return 'rgba(40, 167, 69, 0.7)';
            if (score >= 50) return 'rgba(255, 193, 7, 0.7)';
            return 'rgba(220, 53, 69, 0.7)';
        });

        var borderColors = scores.map(function(score) {
            if (score >= 70) return 'rgb(40, 167, 69)';
            if (score >= 50) return 'rgb(255, 193, 7)';
            return 'rgb(220, 53, 69)';
        });

        branchComparisonChart = new Chart(ctx, {
            type: 'bar',
            data: {
                labels: labels,
                datasets: [{
                    label: 'Ortalama Puan (%)',
                    data: scores,
                    backgroundColor: backgroundColors,
                    borderColor: borderColors,
                    borderWidth: 1
                }]
            },
            options: {
                indexAxis: 'y',
                responsive: true,
                maintainAspectRatio: true,
                plugins: {
                    legend: {
                        display: false
                    },
                    tooltip: {
                        callbacks: {
                            label: function(context) {
                                return 'Puan: ' + context.raw.toFixed(1) + '%';
                            }
                        }
                    }
                },
                scales: {
                    x: {
                        min: 0,
                        max: 100,
                        title: {
                            display: true,
                            text: 'Puan (%)'
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
                if (!response.ok) throw new Error('Scorecard yüklenemedi');
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
                if (!response.ok) throw new Error('Duyurular yüklenemedi');
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
            case 1: return 'Uyarı';
            case 2: return 'Başarı';
            case 3: return 'Önemli';
            case 4: return 'Haber';
            case 5: return 'Sistem';
            default: return 'Bilgi';
        }
    };

    // Format date
    self.formatDate = function(dateStr) {
        if (!dateStr) return '';
        var date = new Date(dateStr);
        return date.toLocaleDateString('tr-TR');
    };

    // Initialize
    self.loadDashboard();
    self.loadScorecard();
    self.loadAnnouncements();
}

// Apply bindings when document is ready
$(document).ready(function() {
    ko.applyBindings(new DashboardViewModel(), document.getElementById('dashboard-app'));
});
