function CustomerDashboardViewModel() {
    var self = this;

    // State
    self.isLoading = ko.observable(true);
    self.stats = ko.observable({});
    self.recentEvaluations = ko.observableArray([]);

    // Charts
    self.monthlyChart = null;
    self.scoreDistributionCharts = {};

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
    self.scoreDistributionByType = ko.observableArray([]);
    self.isScoreDistLoading = ko.observable(false);

    // Monthly trend filters
    self.monthlyTrendProjectId = ko.observable(null);
    self.monthlyTrendProjects = ko.observableArray([]);
    self.monthlyTrendStartDate = ko.observable('');
    self.monthlyTrendEndDate = ko.observable('');
    self.monthlyTrendDateRange = ko.observable('');

    // Score distribution filters
    self.scoreDistProjectId = ko.observable(null);
    self.scoreDistProjects = ko.observableArray([]);
    self.scoreDistStartDate = ko.observable('');
    self.scoreDistEndDate = ko.observable('');
    self.scoreDistDateRange = ko.observable('');

    // Tarih aralığı hızlı seçim helper
    function applyDateRange(val, startObs, endObs) {
        if (!val) { startObs(''); endObs(''); return; }
        var now = new Date();
        var end = now.toISOString().split('T')[0];
        var start;
        switch(val) {
            case 'thisWeek':
                var day = now.getDay() || 7;
                var monday = new Date(now);
                monday.setDate(now.getDate() - day + 1);
                start = monday.toISOString().split('T')[0];
                break;
            case 'lastWeek':
                var day2 = now.getDay() || 7;
                var lastMonday = new Date(now);
                lastMonday.setDate(now.getDate() - day2 - 6);
                var lastSunday = new Date(lastMonday);
                lastSunday.setDate(lastMonday.getDate() + 6);
                start = lastMonday.toISOString().split('T')[0];
                end = lastSunday.toISOString().split('T')[0];
                break;
            case 'thisMonth':
                start = new Date(now.getFullYear(), now.getMonth(), 1).toISOString().split('T')[0];
                break;
            case 'lastMonth':
                start = new Date(now.getFullYear(), now.getMonth() - 1, 1).toISOString().split('T')[0];
                end = new Date(now.getFullYear(), now.getMonth(), 0).toISOString().split('T')[0];
                break;
            case 'last3Months':
                var d3 = new Date(now); d3.setMonth(d3.getMonth() - 3);
                start = d3.toISOString().split('T')[0];
                break;
            case 'last6Months':
                var d6 = new Date(now); d6.setMonth(d6.getMonth() - 6);
                start = d6.toISOString().split('T')[0];
                break;
            case 'thisYear':
                start = new Date(now.getFullYear(), 0, 1).toISOString().split('T')[0];
                break;
            case 'lastYear':
                start = new Date(now.getFullYear() - 1, 0, 1).toISOString().split('T')[0];
                end = new Date(now.getFullYear() - 1, 11, 31).toISOString().split('T')[0];
                break;
        }
        startObs(start);
        endObs(end);
    }

    self.monthlyTrendDateRange.subscribe(function(val) { applyDateRange(val, self.monthlyTrendStartDate, self.monthlyTrendEndDate); });
    self.scoreDistDateRange.subscribe(function(val) { applyDateRange(val, self.scoreDistStartDate, self.scoreDistEndDate); });

    // Score distribution modal state
    self.isScoreModalOpen = ko.observable(false);
    self.isScoreModalLoading = ko.observable(false);
    self.selectedCategory = ko.observable('');
    self.scoreModalEvaluations = ko.observableArray([]);
    self.scoreModalTotal = ko.observable(0);
    self.scoreModalPage = ko.observable(1);
    self.scoreModalPageSize = 20;

    // Category labels and colors (eşik tabanlı)
    self.selectedProjectTypeId = ko.observable(null);
    self.selectedProjectTypeName = ko.observable('');

    self.categoryLabels = {
        'success': 'Başarılı',
        'warning': 'Uyarı',
        'danger': 'Başarısız'
    };
    self.categoryColors = {
        'success': 'bg-success',
        'warning': 'bg-warning',
        'danger': 'bg-danger'
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
            })
        ])
        .then(function(results) {
            var stats = results[0];
            var evaluations = results[1];
            self.monthlyTrendData = results[2];

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

            // Score distribution ayrı yüklenir (proje tipine göre)
            self.loadScoreDistribution();
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
            // Destroy existing chart if any
            if (self.monthlyChart) {
                self.monthlyChart.destroy();
                self.monthlyChart = null;
            }
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

    // Initialize score distribution charts (proje tipine göre)
    self.initScoreDistributionCharts = function() {
        // Mevcut chart'ları temizle
        Object.keys(self.scoreDistributionCharts).forEach(function(key) {
            if (self.scoreDistributionCharts[key]) {
                self.scoreDistributionCharts[key].destroy();
            }
        });
        self.scoreDistributionCharts = {};

        var types = self.scoreDistributionByType();
        if (!types || types.length === 0) return;

        types.forEach(function(item) {
            var canvasId = 'scoreDistChart_' + item.projectTypeId;
            var ctx = document.getElementById(canvasId);
            if (!ctx) return;

            var chartData = [item.success || 0, item.warning || 0, item.danger || 0];
            var categories = ['success', 'warning', 'danger'];
            var total = chartData.reduce(function(a, b) { return a + b; }, 0);

            if (total > 0) {
                var ptId = item.projectTypeId;
                var ptName = item.projectTypeName;
                self.scoreDistributionCharts[ptId] = new Chart(ctx, {
                    type: 'doughnut',
                    data: {
                        labels: [
                            'Başarılı (≥' + item.successThreshold + '%)',
                            'Uyarı (' + item.warningThreshold + '-' + item.successThreshold + '%)',
                            'Başarısız (<' + item.warningThreshold + '%)'
                        ],
                        datasets: [{
                            data: chartData,
                            backgroundColor: ['#198754', '#ffc107', '#dc3545']
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
                        onClick: function(event, elements) {
                            if (elements.length > 0) {
                                var index = elements[0].index;
                                var category = categories[index];
                                self.openScoreModal(category, ptId, ptName);
                            }
                        }
                    }
                });
            }
        });
    };

    // Open score modal
    self.openScoreModal = function(category, projectTypeId, projectTypeName) {
        self.selectedCategory(category);
        self.selectedProjectTypeId(projectTypeId);
        self.selectedProjectTypeName(projectTypeName || '');
        self.scoreModalPage(1);
        self.isScoreModalOpen(true);
        self.loadScoreModalEvaluations(1);
    };

    // Load score modal evaluations
    self.loadScoreModalEvaluations = function(page) {
        self.isScoreModalLoading(true);
        self.scoreModalPage(page);

        var url = '/api/customer/portal/dashboard/score-distribution/evaluations?category=' + self.selectedCategory();
        url += '&projectTypeId=' + self.selectedProjectTypeId();
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
        self.selectedProjectTypeId(null);
        self.selectedProjectTypeName('');
        self.scoreModalEvaluations([]);
        self.scoreModalTotal(0);
    };

    // Export score distribution to Excel
    self.exportScoreDistribution = function() {
        var url = '/api/customer/portal/dashboard/score-distribution/export?category=' + self.selectedCategory();
        url += '&projectTypeId=' + self.selectedProjectTypeId();

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
    self.questionTrendStartDate = ko.observable('');
    self.questionTrendEndDate = ko.observable('');
    self.questionTrendDateRange = ko.observable('');
    self.questionTrendData = ko.observableArray([]);
    self.questionTrendLabels = ko.observableArray([]);
    self.isQuestionTrendLoading = ko.observable(false);
    self.questionTrendChart = null;

    self.questionTrendDateRange.subscribe(function(val) { applyDateRange(val, self.questionTrendStartDate, self.questionTrendEndDate); });

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
        var params = [];
        if (self.questionTrendProjectId()) params.push('projectId=' + self.questionTrendProjectId());
        if (self.questionTrendStartDate()) params.push('startDate=' + self.questionTrendStartDate());
        if (self.questionTrendEndDate()) params.push('endDate=' + self.questionTrendEndDate());
        if (params.length > 0) url += '?' + params.join('&');

        customerApiFetch(url)
            .then(function(r) {
                if (!r.ok) throw new Error('Question group trend API error: ' + r.status);
                return r.json();
            })
            .then(function(data) {
                self.questionTrendProjects(data.projects || []);
                // Aynı projeleri Aylık Trend ve Puan Dağılımı için de kullan
                self.monthlyTrendProjects(data.projects || []);
                self.scoreDistProjects(data.projects || []);
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
        if (self.questionTrendProjectId()) params.push('projectId=' + self.questionTrendProjectId());
        if (self.questionTrendStartDate()) params.push('startDate=' + self.questionTrendStartDate());
        if (self.questionTrendEndDate()) params.push('endDate=' + self.questionTrendEndDate());
        if (params.length > 0) url += '?' + params.join('&');

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

    // Load monthly trend with project + date filter
    self.loadMonthlyTrend = function() {
        var url = '/api/customer/portal/dashboard/monthly-trend';
        var params = [];
        if (self.monthlyTrendProjectId()) params.push('projectId=' + self.monthlyTrendProjectId());
        if (self.monthlyTrendStartDate()) params.push('startDate=' + self.monthlyTrendStartDate());
        if (self.monthlyTrendEndDate()) params.push('endDate=' + self.monthlyTrendEndDate());
        if (params.length > 0) url += '?' + params.join('&');

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

    // Load score distribution with filters (proje tipine göre)
    self.loadScoreDistribution = function() {
        self.isScoreDistLoading(true);
        var url = '/api/customer/portal/dashboard/score-distribution';
        var params = [];

        if (self.scoreDistProjectId()) {
            params.push('projectId=' + self.scoreDistProjectId());
        }
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
                self.scoreDistributionByType(data || []);
                self.isScoreDistLoading(false);
                // Knockout foreach render ettikten sonra chart'ları oluştur
                setTimeout(function() {
                    self.initScoreDistributionCharts();
                }, 150);
            })
            .catch(function(error) {
                console.error('Score distribution load error:', error);
                self.isScoreDistLoading(false);
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

    // Export chart to Excel (image + data table)
    self.exportChartToExcel = function(chartType, elementId, filename) {
        var element = document.getElementById(elementId);
        if (!element) {
            toastr.error('Grafik bulunamadi');
            return;
        }

        var canvas = element.querySelector('canvas');
        if (!canvas) {
            toastr.error('Grafik canvas bulunamadi');
            return;
        }

        toastr.info('Excel olusturuluyor...');

        var chartImage = canvas.toDataURL('image/png', 1.0);

        // Get chart title
        var headerEl = element.querySelector('.card-header h6');
        var chartTitle = headerEl ? headerEl.textContent.trim() : filename;

        // Get data based on chart type
        var dataObj;
        switch (chartType) {
            case 'monthly-trend':
                dataObj = self.monthlyTrendData || [];
                break;
            case 'score-distribution':
                dataObj = self.scoreDistributionByType() || [];
                break;
            case 'question-trend':
                dataObj = {
                    monthLabels: self.questionTrendLabels(),
                    groupTrends: self.questionTrendTab() === 'groups' ? self.questionTrendData() : undefined,
                    questionTrends: self.questionTrendTab() === 'questions' ? self.questionTrendData() : undefined
                };
                break;
            default:
                dataObj = {};
        }

        var requestBody = {
            chartType: chartType,
            chartImage: chartImage,
            chartTitle: chartTitle,
            dataJson: JSON.stringify(dataObj)
        };

        customerApiDownloadPost(
            '/api/customer/portal/dashboard/charts/export',
            requestBody,
            filename + '_' + new Date().toISOString().split('T')[0] + '.xlsx'
        ).then(function() {
            toastr.success('Excel indirildi');
        }).catch(function(error) {
            console.error('Excel export error:', error);
            toastr.error('Excel olusturulurken hata olustu');
        });
    };

    // Initialize
    self.loadDashboard();
    self.loadQuestionGroupTrend();
}

// Apply bindings when DOM is ready
$(document).ready(function() {
    ko.applyBindings(new CustomerDashboardViewModel(), document.getElementById('customer-dashboard-app'));
});
