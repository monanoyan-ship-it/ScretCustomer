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

    // Monthly trend filters
    self.monthlyTrendProjectId = ko.observable(null);
    self.monthlyTrendProjects = ko.observableArray([]);

    // Score distribution filters
    self.scoreDistStartDate = ko.observable('');
    self.scoreDistEndDate = ko.observable('');

    // Score distribution modal state
    self.isScoreModalOpen = ko.observable(false);
    self.isScoreModalLoading = ko.observable(false);
    self.selectedCategory = ko.observable('');
    self.scoreModalEvaluations = ko.observableArray([]);
    self.scoreModalTotal = ko.observable(0);
    self.scoreModalPage = ko.observable(1);
    self.scoreModalPageSize = 20;

    // Category labels and colors
    self.categoryLabels = {
        'excellent': 'Mükemmel (90+)',
        'good': 'İyi (80-89)',
        'average': 'Orta (60-79)',
        'poor': 'Düşük (<60)'
    };
    self.categoryColors = {
        'excellent': 'bg-success',
        'good': 'bg-primary',
        'average': 'bg-warning',
        'poor': 'bg-danger'
    };

    self.selectedCategoryLabel = ko.computed(function() {
        return self.categoryLabels[self.selectedCategory()] || '';
    });

    self.selectedCategoryHeaderClass = ko.computed(function() {
        return self.categoryColors[self.selectedCategory()] || 'bg-primary';
    });

    self.scoreModalTotalPages = ko.computed(function() {
        return Math.ceil(self.scoreModalTotal() / self.scoreModalPageSize);
    });

    // Load dashboard data
    self.loadDashboard = function() {
        self.isLoading(true);

        // Load all data in parallel (auth kontrolü server-side yapılıyor)
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

            self.stats({
                totalEvaluations: stats.totalEvaluations || 0,
                averageScore: stats.averageScore || 0,
                organizationCount: stats.organizationCount || 0,
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
        // Prepare monthly trend data
        var monthLabels = [];
        var scoreData = [];
        var countData = [];
        var yellowCardData = [];
        var redCardData = [];

        if (self.monthlyTrendData && self.monthlyTrendData.length > 0) {
            self.monthlyTrendData.forEach(function(item) {
                monthLabels.push(item.month);
                scoreData.push(item.averageScore);
                countData.push(item.count);
                yellowCardData.push(item.yellowCardCount || 0);
                redCardData.push(item.redCardCount || 0);
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
                    }, {
                        label: 'Sarı Kart',
                        data: yellowCardData,
                        borderColor: '#ffc107',
                        backgroundColor: 'transparent',
                        borderWidth: 2,
                        tension: 0.4,
                        yAxisID: 'y1'
                    }, {
                        label: 'Kırmızı Kart',
                        data: redCardData,
                        borderColor: '#dc3545',
                        backgroundColor: 'transparent',
                        borderWidth: 2,
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

        // Score distribution chart
        self.initScoreDistributionChart();
    };

    // Initialize score distribution chart
    self.initScoreDistributionChart = function() {
        var scoreCtx = document.getElementById('scoreDistributionChart');
        var dist = self.scoreDistributionData;
        if (!scoreCtx || !dist) return;

        var chartData = [dist.excellent || 0, dist.good || 0, dist.average || 0, dist.poor || 0];
        var categories = ['excellent', 'good', 'average', 'poor'];
        var total = chartData.reduce(function(a, b) { return a + b; }, 0);

        // Destroy existing chart if any
        if (self.scoreDistributionChart) {
            self.scoreDistributionChart.destroy();
            self.scoreDistributionChart = null;
        }

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
                    },
                    onClick: function(event, elements) {
                        if (elements.length > 0) {
                            var index = elements[0].index;
                            var category = categories[index];
                            self.openScoreModal(category);
                        }
                    }
                }
            });
        }
    };

    // Open score modal
    self.openScoreModal = function(category) {
        self.selectedCategory(category);
        self.scoreModalPage(1);
        self.isScoreModalOpen(true);
        self.loadScoreModalEvaluations(1);
    };

    // Load score modal evaluations
    self.loadScoreModalEvaluations = function(page) {
        self.isScoreModalLoading(true);
        self.scoreModalPage(page);

        var url = '/api/customer/portal/dashboard/score-distribution/evaluations?category=' + self.selectedCategory();
        url += '&page=' + page + '&pageSize=' + self.scoreModalPageSize;

        if (self.scoreDistStartDate()) {
            url += '&startDate=' + self.scoreDistStartDate();
        }
        if (self.scoreDistEndDate()) {
            url += '&endDate=' + self.scoreDistEndDate();
        }

        customerApiFetch(url)
            .then(function(r) {
                if (!r.ok) throw new Error('Evaluations API error: ' + r.status);
                return r.json();
            })
            .then(function(data) {
                self.scoreModalEvaluations(data.items || []);
                self.scoreModalTotal(data.total || 0);
                self.isScoreModalLoading(false);
            })
            .catch(function(error) {
                console.error('Score modal evaluations load error:', error);
                self.isScoreModalLoading(false);
            });
    };

    // Close score modal
    self.closeScoreModal = function() {
        self.isScoreModalOpen(false);
        self.selectedCategory('');
        self.scoreModalEvaluations([]);
        self.scoreModalTotal(0);
    };

    // Export score distribution to Excel
    self.exportScoreDistribution = function() {
        var url = '/api/customer/portal/dashboard/score-distribution/export?category=' + self.selectedCategory();

        if (self.scoreDistStartDate()) {
            url += '&startDate=' + self.scoreDistStartDate();
        }
        if (self.scoreDistEndDate()) {
            url += '&endDate=' + self.scoreDistEndDate();
        }

        customerApiFetch(url)
            .then(function(response) {
                if (!response.ok) throw new Error('Export failed');
                return response.blob();
            })
            .then(function(blob) {
                var link = document.createElement('a');
                link.href = window.URL.createObjectURL(blob);
                link.download = 'PuanDagilimi_' + self.selectedCategory() + '_' + new Date().toISOString().slice(0,10) + '.xlsx';
                document.body.appendChild(link);
                link.click();
                document.body.removeChild(link);
            })
            .catch(function(error) {
                console.error('Export error:', error);
                toastr.error('Export sırasında hata oluştu');
            });
    };

    // Question trend state
    self.questionTrendTab = ko.observable('groups');
    self.questionTrendProjectId = ko.observable(null);
    self.questionTrendProjects = ko.observableArray([]);
    self.questionTrendData = ko.observableArray([]);
    self.questionTrendLabels = ko.observableArray([]);
    self.isQuestionTrendLoading = ko.observable(false);
    self.questionTrendChart = null;

    // Watch for project change
    self.questionTrendProjectId.subscribe(function() {
        if (self.questionTrendTab() === 'groups') {
            self.loadQuestionGroupTrend();
        } else {
            self.loadQuestionTrend();
        }
    });

    // Load question group trend
    self.loadQuestionGroupTrend = function() {
        self.isQuestionTrendLoading(true);

        var url = '/api/customer/portal/dashboard/question-group-trend';
        if (self.questionTrendProjectId()) {
            url += '?projectId=' + self.questionTrendProjectId();
        }

        customerApiFetch(url)
            .then(function(r) {
                if (!r.ok) throw new Error('Question group trend API error: ' + r.status);
                return r.json();
            })
            .then(function(data) {
                self.questionTrendProjects(data.projects || []);
                // Aynı projeleri Aylık Trend için de kullan
                self.monthlyTrendProjects(data.projects || []);
                self.questionTrendLabels(data.monthLabels || []);
                self.questionTrendData(data.groupTrends || []);
                self.isQuestionTrendLoading(false);
                setTimeout(function() {
                    self.initQuestionTrendChart();
                }, 100);
            })
            .catch(function(error) {
                console.error('Question group trend load error:', error);
                self.isQuestionTrendLoading(false);
            });
    };

    // Load question trend
    self.loadQuestionTrend = function() {
        self.isQuestionTrendLoading(true);

        var url = '/api/customer/portal/dashboard/question-trend';
        var params = [];
        if (self.questionTrendProjectId()) {
            params.push('projectId=' + self.questionTrendProjectId());
        }
        if (params.length > 0) {
            url += '?' + params.join('&');
        }

        customerApiFetch(url)
            .then(function(r) {
                if (!r.ok) throw new Error('Question trend API error: ' + r.status);
                return r.json();
            })
            .then(function(data) {
                self.questionTrendLabels(data.monthLabels || []);
                self.questionTrendData(data.questionTrends || []);
                self.isQuestionTrendLoading(false);
                setTimeout(function() {
                    self.initQuestionTrendChart();
                }, 100);
            })
            .catch(function(error) {
                console.error('Question trend load error:', error);
                self.isQuestionTrendLoading(false);
            });
    };

    // Initialize question trend chart
    self.initQuestionTrendChart = function() {
        var ctx = document.getElementById('questionTrendChart');
        if (!ctx) return;

        var data = self.questionTrendData();
        var labels = self.questionTrendLabels();
        if (!data || data.length === 0 || !labels || labels.length === 0) return;

        // Destroy existing chart
        if (self.questionTrendChart) {
            self.questionTrendChart.destroy();
            self.questionTrendChart = null;
        }

        // Generate colors
        var colors = [
            '#198754', '#0d6efd', '#dc3545', '#ffc107', '#6f42c1',
            '#20c997', '#fd7e14', '#0dcaf0', '#d63384', '#6c757d'
        ];

        var datasets = data.map(function(item, index) {
            var color = colors[index % colors.length];
            return {
                label: item.groupName || item.questionText || ('Seri ' + (index + 1)),
                data: item.scores,
                borderColor: color,
                backgroundColor: 'transparent',
                borderWidth: 2,
                tension: 0.4
            };
        });

        self.questionTrendChart = new Chart(ctx, {
            type: 'line',
            data: {
                labels: labels,
                datasets: datasets
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: {
                    legend: {
                        position: 'bottom',
                        labels: {
                            boxWidth: 12
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
                            text: 'Puan (%)'
                        }
                    }
                }
            }
        });
    };

    // Watch for monthly trend project change
    self.monthlyTrendProjectId.subscribe(function() {
        self.loadMonthlyTrend();
    });

    // Load monthly trend with project filter
    self.loadMonthlyTrend = function() {
        var url = '/api/customer/portal/dashboard/monthly-trend';
        if (self.monthlyTrendProjectId()) {
            url += '?projectId=' + self.monthlyTrendProjectId();
        }

        customerApiFetch(url)
            .then(function(r) {
                if (!r.ok) throw new Error('Monthly trend API error: ' + r.status);
                return r.json();
            })
            .then(function(data) {
                self.monthlyTrendData = data;
                self.updateMonthlyChart();
            })
            .catch(function(error) {
                console.error('Monthly trend load error:', error);
            });
    };

    // Update monthly chart with new data
    self.updateMonthlyChart = function() {
        var monthLabels = [];
        var scoreData = [];
        var countData = [];
        var yellowCardData = [];
        var redCardData = [];

        if (self.monthlyTrendData && self.monthlyTrendData.length > 0) {
            self.monthlyTrendData.forEach(function(item) {
                monthLabels.push(item.month);
                scoreData.push(item.averageScore);
                countData.push(item.count);
                yellowCardData.push(item.yellowCardCount || 0);
                redCardData.push(item.redCardCount || 0);
            });
        }

        // Destroy existing chart if any
        if (self.monthlyChart) {
            self.monthlyChart.destroy();
            self.monthlyChart = null;
        }

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
                    }, {
                        label: 'Sarı Kart',
                        data: yellowCardData,
                        borderColor: '#ffc107',
                        backgroundColor: 'transparent',
                        borderWidth: 2,
                        tension: 0.4,
                        yAxisID: 'y1'
                    }, {
                        label: 'Kırmızı Kart',
                        data: redCardData,
                        borderColor: '#dc3545',
                        backgroundColor: 'transparent',
                        borderWidth: 2,
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
    };

    // Load score distribution with filters
    self.loadScoreDistribution = function() {
        var url = '/api/customer/portal/dashboard/score-distribution';
        var params = [];

        if (self.scoreDistStartDate()) {
            params.push('startDate=' + self.scoreDistStartDate());
        }
        if (self.scoreDistEndDate()) {
            params.push('endDate=' + self.scoreDistEndDate());
        }

        if (params.length > 0) {
            url += '?' + params.join('&');
        }

        customerApiFetch(url)
            .then(function(r) {
                if (!r.ok) throw new Error('Score distribution API error: ' + r.status);
                return r.json();
            })
            .then(function(data) {
                self.scoreDistributionData = data;
                self.initScoreDistributionChart();
            })
            .catch(function(error) {
                console.error('Score distribution load error:', error);
            });
    };

    // Export chart to PDF
    self.exportChartToPdf = function(elementId, filename) {
        var element = document.getElementById(elementId);
        if (!element) {
            toastr.error('Grafik bulunamadı');
            return;
        }

        // Chart.js canvas'ını bul
        var canvas = element.querySelector('canvas');
        if (!canvas) {
            toastr.error('Grafik canvas bulunamadı');
            return;
        }

        toastr.info('PDF oluşturuluyor...');

        // Başlığı al
        var headerEl = element.querySelector('.card-header h6');
        var title = headerEl ? headerEl.textContent.trim() : filename;

        // Canvas'ı doğrudan image olarak al
        var imgData = canvas.toDataURL('image/png', 1.0);

        var pdf = new jspdf.jsPDF({
            orientation: canvas.width > canvas.height ? 'landscape' : 'portrait',
            unit: 'mm'
        });

        var pageWidth = pdf.internal.pageSize.getWidth();
        var pageHeight = pdf.internal.pageSize.getHeight();

        // Başlık ekle
        pdf.setFontSize(14);
        pdf.text(title, pageWidth / 2, 15, { align: 'center' });

        // Tarih ekle
        pdf.setFontSize(10);
        pdf.text(new Date().toLocaleDateString('tr-TR'), pageWidth / 2, 22, { align: 'center' });

        var imgWidth = pageWidth - 30;
        var imgHeight = (canvas.height * imgWidth) / canvas.width;

        if (imgHeight > pageHeight - 40) {
            imgHeight = pageHeight - 40;
            imgWidth = (canvas.width * imgHeight) / canvas.height;
        }

        var x = (pageWidth - imgWidth) / 2;
        var y = 28;

        pdf.addImage(imgData, 'PNG', x, y, imgWidth, imgHeight);
        pdf.save(filename + '_' + new Date().toISOString().split('T')[0] + '.pdf');
        toastr.success('PDF indirildi');
    };

    // Initialize
    self.loadDashboard();
    self.loadQuestionGroupTrend();
}

// Apply bindings when DOM is ready
$(document).ready(function() {
    ko.applyBindings(new CustomerDashboardViewModel(), document.getElementById('customer-dashboard-app'));
});
