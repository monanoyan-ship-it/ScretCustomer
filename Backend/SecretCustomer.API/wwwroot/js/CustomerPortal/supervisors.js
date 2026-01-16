function CustomerSupervisorsViewModel() {
    var self = this;

    // State
    self.isLoading = ko.observable(true);
    self.groups = ko.observableArray([]);

    // Modal state
    self.isModalOpen = ko.observable(false);
    self.isChartLoading = ko.observable(false);
    self.selectedSupervisorId = ko.observable(null);
    self.selectedSupervisorName = ko.observable('');
    self.chartData = ko.observable({});

    // Chart instance
    self.monthlyChart = null;

    // Computed values for modal
    self.totalEvaluationCount = ko.computed(function() {
        var data = self.chartData();
        if (!data.monthlyTrend) return 0;
        return data.monthlyTrend.reduce(function(sum, item) {
            return sum + (item.count || 0);
        }, 0);
    });

    self.overallAverageScore = ko.computed(function() {
        var data = self.chartData();
        if (!data.monthlyTrend) return '0.0';
        var monthsWithData = data.monthlyTrend.filter(function(item) {
            return item.count > 0;
        });
        if (monthsWithData.length === 0) return '0.0';
        var sum = monthsWithData.reduce(function(total, item) {
            return total + (item.averageScore || 0);
        }, 0);
        return (sum / monthsWithData.length).toFixed(1);
    });

    self.getScoreClass = function(score) {
        if (score >= 80) return 'text-success';
        if (score >= 60) return 'text-warning';
        if (score > 0) return 'text-danger';
        return 'text-muted';
    };

    self.loadSupervisors = function() {
        self.isLoading(true);

        customerApiFetch('/api/customer/portal/supervisors')
            .then(function(response) {
                if (!response.ok) throw new Error('Supervizorler yuklenemedi');
                return response.json();
            })
            .then(function(data) {
                self.groups(data || []);
                self.isLoading(false);
            })
            .catch(function(error) {
                console.error('Supervisors load error:', error);
                self.isLoading(false);
            });
    };

    // Show monthly trend modal
    self.showMonthlyTrend = function(supervisor) {
        self.selectedSupervisorId(supervisor.id);
        self.selectedSupervisorName(supervisor.fullName);
        self.isModalOpen(true);
        self.loadMonthlyTrend(supervisor.id);
    };

    // Load monthly trend data
    self.loadMonthlyTrend = function(supervisorId) {
        self.isChartLoading(true);

        customerApiFetch('/api/customer/portal/supervisors/' + supervisorId + '/monthly-trend')
            .then(function(response) {
                if (!response.ok) throw new Error('Aylik trend yuklenemedi');
                return response.json();
            })
            .then(function(data) {
                self.chartData(data);
                self.isChartLoading(false);
                setTimeout(function() {
                    self.initChart();
                }, 100);
            })
            .catch(function(error) {
                console.error('Monthly trend load error:', error);
                self.isChartLoading(false);
            });
    };

    // Initialize chart
    self.initChart = function() {
        var data = self.chartData();
        if (!data.monthlyTrend || data.monthlyTrend.length === 0) return;

        var monthLabels = [];
        var scoreData = [];
        var countData = [];

        data.monthlyTrend.forEach(function(item) {
            monthLabels.push(item.month);
            scoreData.push(item.averageScore);
            countData.push(item.count);
        });

        // Destroy existing chart if any
        if (self.monthlyChart) {
            self.monthlyChart.destroy();
            self.monthlyChart = null;
        }

        var ctx = document.getElementById('supervisorMonthlyChart');
        if (!ctx) return;

        self.monthlyChart = new Chart(ctx, {
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
    };

    // Close modal
    self.closeModal = function() {
        self.isModalOpen(false);
        self.selectedSupervisorId(null);
        self.selectedSupervisorName('');
        self.chartData({});

        // Destroy chart
        if (self.monthlyChart) {
            self.monthlyChart.destroy();
            self.monthlyChart = null;
        }
    };

    // Initialize
    self.loadSupervisors();
}

$(document).ready(function() {
    ko.applyBindings(new CustomerSupervisorsViewModel(), document.getElementById('customer-supervisors-app'));
});
