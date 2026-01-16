function CustomerOrganizationsViewModel() {
    var self = this;

    self.isLoading = ko.observable(true);
    self.isChartLoading = ko.observable(true);
    self.groups = ko.observableArray([]);
    self.hasChartData = ko.observable(false);

    // Chart instance
    self.monthlyChart = null;
    self.chartData = null;

    // Tarih filtreleri
    self.chartStartDate = ko.observable('');
    self.chartEndDate = ko.observable('');
    self.quickRange = ko.observable('week');
    self.periodTypeText = ko.observable('Günlük');

    // Renk paleti (organizasyonlar için)
    self.chartColors = [
        { border: '#198754', background: 'rgba(25, 135, 84, 0.8)' },   // Yeşil
        { border: '#0d6efd', background: 'rgba(13, 110, 253, 0.8)' },  // Mavi
        { border: '#ffc107', background: 'rgba(255, 193, 7, 0.8)' },   // Sarı
        { border: '#dc3545', background: 'rgba(220, 53, 69, 0.8)' },   // Kırmızı
        { border: '#6f42c1', background: 'rgba(111, 66, 193, 0.8)' }   // Mor
    ];

    // Tarih formatlama yardımcısı (YYYY-MM-DD)
    self.formatDate = function(date) {
        var d = new Date(date);
        var month = '' + (d.getMonth() + 1);
        var day = '' + d.getDate();
        var year = d.getFullYear();

        if (month.length < 2) month = '0' + month;
        if (day.length < 2) day = '0' + day;

        return [year, month, day].join('-');
    };

    // Bu haftanın pazartesisini hesapla
    self.getMonday = function(date) {
        var d = new Date(date);
        var day = d.getDay();
        var diff = d.getDate() - day + (day === 0 ? -6 : 1);
        return new Date(d.setDate(diff));
    };

    // Hızlı tarih aralığı seçimi
    self.setQuickDateRange = function(range) {
        var today = new Date();
        var start, end = today;

        switch (range) {
            case 'week':
                start = self.getMonday(today);
                break;
            case 'month':
                start = new Date(today.getFullYear(), today.getMonth(), 1);
                break;
            case 'quarter':
                start = new Date(today.getFullYear(), today.getMonth() - 2, 1);
                break;
            case 'year':
                start = new Date(today.getFullYear(), 0, 1);
                break;
            default:
                start = self.getMonday(today);
        }

        self.chartStartDate(self.formatDate(start));
        self.chartEndDate(self.formatDate(end));
        self.quickRange(range);
        self.loadMonthlyTrend();
    };

    // Filtre uygula
    self.applyChartFilter = function() {
        self.quickRange('');
        self.loadMonthlyTrend();
    };

    // Varsayılan tarih aralığını ayarla (bu hafta)
    self.initDefaultDates = function() {
        var today = new Date();
        var monday = self.getMonday(today);
        self.chartStartDate(self.formatDate(monday));
        self.chartEndDate(self.formatDate(today));
        self.quickRange('week');
    };

    self.getScoreClass = function(score) {
        if (score >= 80) return 'text-success';
        if (score >= 60) return 'text-warning';
        if (score > 0) return 'text-danger';
        return 'text-muted';
    };

    self.loadOrganizations = function() {
        self.isLoading(true);

        customerApiFetch('/api/customer/portal/organizations')
            .then(function(response) {
                if (!response.ok) throw new Error('Organizasyonlar yuklenemedi');
                return response.json();
            })
            .then(function(data) {
                self.groups(data || []);
                self.isLoading(false);
            })
            .catch(function(error) {
                console.error('Organizations load error:', error);
                self.isLoading(false);
            });
    };

    self.loadMonthlyTrend = function() {
        self.isChartLoading(true);

        var params = new URLSearchParams();
        if (self.chartStartDate()) {
            params.append('startDate', self.chartStartDate());
        }
        if (self.chartEndDate()) {
            params.append('endDate', self.chartEndDate());
        }

        var url = '/api/customer/portal/organizations/monthly-trend';
        if (params.toString()) {
            url += '?' + params.toString();
        }

        customerApiFetch(url)
            .then(function(response) {
                if (!response.ok) throw new Error('Trend verisi yuklenemedi');
                return response.json();
            })
            .then(function(data) {
                self.chartData = data;
                var hasData = data.overallTrend && data.overallTrend.some(function(item) { return item.count > 0; });
                self.hasChartData(hasData);
                self.isChartLoading(false);

                // Periyod tipini güncelle
                if (data.periodType === 'daily') {
                    self.periodTypeText('Günlük');
                } else if (data.periodType === 'weekly') {
                    self.periodTypeText('Haftalık');
                } else {
                    self.periodTypeText('Aylık');
                }

                if (hasData) {
                    setTimeout(function() {
                        self.initChart();
                    }, 100);
                }
            })
            .catch(function(error) {
                console.error('Monthly trend load error:', error);
                self.isChartLoading(false);
                self.hasChartData(false);
            });
    };

    self.initChart = function() {
        var ctx = document.getElementById('organizationsMonthlyChart');
        if (!ctx || !self.chartData) return;

        // Önceki chart'ı temizle
        if (self.monthlyChart) {
            self.monthlyChart.destroy();
        }

        var datasets = [];

        // Genel ortalama (çizgi grafik)
        var overallScores = self.chartData.overallTrend.map(function(item) { return item.averageScore; });
        datasets.push({
            label: 'Genel Ortalama',
            data: overallScores,
            borderColor: '#343a40',
            backgroundColor: 'rgba(52, 58, 64, 0.1)',
            borderWidth: 3,
            fill: false,
            tension: 0.4,
            type: 'line',
            order: 0
        });

        // Organizasyon bazlı (bar grafik)
        if (self.chartData.organizationTrends && self.chartData.organizationTrends.length > 0) {
            self.chartData.organizationTrends.forEach(function(org, index) {
                var colorIndex = index % self.chartColors.length;
                datasets.push({
                    label: org.organizationName,
                    data: org.data,
                    backgroundColor: self.chartColors[colorIndex].background,
                    borderColor: self.chartColors[colorIndex].border,
                    borderWidth: 1,
                    type: 'bar',
                    order: 1
                });
            });
        }

        self.monthlyChart = new Chart(ctx, {
            type: 'bar',
            data: {
                labels: self.chartData.labels,
                datasets: datasets
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: {
                    legend: {
                        position: 'bottom',
                        labels: {
                            usePointStyle: true,
                            padding: 15
                        }
                    },
                    tooltip: {
                        callbacks: {
                            label: function(context) {
                                return context.dataset.label + ': ' + context.parsed.y.toFixed(1) + ' puan';
                            }
                        }
                    }
                },
                scales: {
                    y: {
                        beginAtZero: false,
                        min: 0,
                        max: 100,
                        title: {
                            display: true,
                            text: 'Ortalama Puan'
                        },
                        ticks: {
                            callback: function(value) {
                                return value + '%';
                            }
                        }
                    },
                    x: {
                        title: {
                            display: true,
                            text: 'Ay'
                        }
                    }
                }
            }
        });
    };

    // Initialize
    self.initDefaultDates();
    self.loadOrganizations();
    self.loadMonthlyTrend();
}

$(document).ready(function() {
    ko.applyBindings(new CustomerOrganizationsViewModel(), document.getElementById('customer-organizations-app'));
});
