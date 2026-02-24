function CustomerDashboardViewModel() {
    var self = this;

    // Non-scoring ChecklistType IDs (for score distribution filter)
    var nonScoringTypeIds = [5, 7]; // Survey, Enneagram

    // State
    self.isLoading = ko.observable(true);
    self.stats = ko.observable({});
    self.recentEvaluations = ko.observableArray([]);

    // Charts
    self.monthlyTrendCharts = {};
    self.scoreDistributionCharts = {};

    // Helper functions
    self.getScoreBadgeClass = function(score) {
        if (score >= 90) return 'bg-success';
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

    // Monthly trend by type
    self.monthlyTrendByType = ko.observableArray([]);
    self.isMonthlyTrendLoading = ko.observable(false);

    // Chart data
    self.scoreDistributionByType = ko.observableArray([]);
    self.isScoreDistLoading = ko.observable(false);

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
            })
        ])
        .then(function(results) {
            var stats = results[0];
            var evaluations = results[1];

            self.stats({
                totalEvaluations: stats.totalEvaluations || 0,
                averageScore: stats.averageScore || 0,
                organizationCount: stats.organizationCount || 0,
                thisMonthEvaluations: stats.thisMonthEvaluations || 0
            });

            self.recentEvaluations(evaluations || []);

            self.isLoading(false);

            // Load monthly trend by type and score distribution separately
            self.loadMonthlyTrendByType();
            self.loadScoreDistribution();
        })
        .catch(function(error) {
            console.error('Dashboard load error:', error);
            self.isLoading(false);
        });
    };

    // Load monthly trend by project type
    self.loadMonthlyTrendByType = function() {
        self.isMonthlyTrendLoading(true);

        customerApiFetch('/api/customer/portal/dashboard/monthly-trend-by-type')
            .then(function(r) {
                if (!r.ok) throw new Error('Monthly trend by type API error: ' + r.status);
                return r.json();
            })
            .then(function(data) {
                // Her item'a KO observable'lar ekle
                (data || []).forEach(function(item) {
                    item.selectedProjectId = ko.observable(null);
                    item.dateRange = ko.observable('');
                    item.startDate = ko.observable('');
                    item.endDate = ko.observable('');
                    item.trendData = item.trend;

                    // dateRange değişince tarih alanlarını güncelle
                    item.dateRange.subscribe(function(val) {
                        applyDateRange(val, item.startDate, item.endDate);
                    });
                });

                self.monthlyTrendByType(data || []);
                self.isMonthlyTrendLoading(false);

                // Knockout foreach render ettikten sonra chart'ları oluştur
                setTimeout(function() {
                    self.initMonthlyTrendCharts();
                }, 150);
            })
            .catch(function(error) {
                console.error('Monthly trend by type load error:', error);
                self.isMonthlyTrendLoading(false);
            });
    };

    // Initialize all monthly trend charts
    self.initMonthlyTrendCharts = function() {
        // Mevcut chart'ları temizle
        Object.keys(self.monthlyTrendCharts).forEach(function(key) {
            if (self.monthlyTrendCharts[key]) {
                self.monthlyTrendCharts[key].destroy();
            }
        });
        self.monthlyTrendCharts = {};

        var types = self.monthlyTrendByType();
        if (!types || types.length === 0) return;

        types.forEach(function(item) {
            self.renderMonthlyTrendChart(item);
        });
    };

    // Render a single monthly trend chart (dispatcher)
    self.renderMonthlyTrendChart = function(panelItem) {
        var projectTypeId = panelItem.projectTypeId;
        var panelType = panelItem.panelType || 'scoreTrend';

        // Destroy existing charts for this panel
        if (self.monthlyTrendCharts[projectTypeId]) {
            self.monthlyTrendCharts[projectTypeId].destroy();
            self.monthlyTrendCharts[projectTypeId] = null;
        }
        if (self.monthlyTrendCharts[projectTypeId + '_dist']) {
            self.monthlyTrendCharts[projectTypeId + '_dist'].destroy();
            self.monthlyTrendCharts[projectTypeId + '_dist'] = null;
        }

        switch (panelType) {
            case 'scoreTrend':
                self.renderScoreTrendChart(projectTypeId, panelItem.trendData);
                break;
            case 'scoreTrendNoCards':
                self.renderScoreTrendNoCardsChart(projectTypeId, panelItem.trendData);
                break;
            case 'survey':
                self.renderSurveyChart(projectTypeId, panelItem.trendData, panelItem.summary);
                break;
            case 'enneagram':
                self.renderEnneagramChart(projectTypeId, panelItem.typeTrend || panelItem.trendData, panelItem.distribution, panelItem.summary);
                break;
            default:
                self.renderScoreTrendChart(projectTypeId, panelItem.trendData);
                break;
        }
    };

    // Score Trend Chart (Çağrı, Fiziksel, Gizli Müşteri) - Line, 4 dataset, dual y-axis
    self.renderScoreTrendChart = function(projectTypeId, trendData) {
        var ctx = document.getElementById('monthlyChart_' + projectTypeId);
        if (!ctx) return;

        var labels = [], scoreData = [], countData = [], yellowData = [], redData = [];
        (trendData || []).forEach(function(item) {
            labels.push(item.month);
            scoreData.push(item.averageScore);
            countData.push(item.count);
            yellowData.push(item.yellowCardCount || 0);
            redData.push(item.redCardCount || 0);
        });
        if (labels.length === 0) return;

        self.monthlyTrendCharts[projectTypeId] = new Chart(ctx, {
            type: 'line',
            data: {
                labels: labels,
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
                    data: yellowData,
                    borderColor: '#ffc107',
                    backgroundColor: 'transparent',
                    borderWidth: 2,
                    tension: 0.4,
                    yAxisID: 'y1'
                }, {
                    label: 'Kırmızı Kart',
                    data: redData,
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
                plugins: { legend: { position: 'bottom' } },
                scales: {
                    y: { beginAtZero: false, min: 0, max: 100, title: { display: true, text: 'Puan' } },
                    y1: { position: 'right', beginAtZero: true, grid: { drawOnChartArea: false }, title: { display: true, text: 'Adet' } }
                }
            }
        });
    };

    // Score Trend No Cards (Online Değerlendirme) - Line, 2 dataset, dual y-axis
    self.renderScoreTrendNoCardsChart = function(projectTypeId, trendData) {
        var ctx = document.getElementById('monthlyChart_' + projectTypeId);
        if (!ctx) return;

        var labels = [], scoreData = [], countData = [];
        (trendData || []).forEach(function(item) {
            labels.push(item.month);
            scoreData.push(item.averageScore);
            countData.push(item.count);
        });
        if (labels.length === 0) return;

        self.monthlyTrendCharts[projectTypeId] = new Chart(ctx, {
            type: 'line',
            data: {
                labels: labels,
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
                plugins: { legend: { position: 'bottom' } },
                scales: {
                    y: { beginAtZero: false, min: 0, max: 100, title: { display: true, text: 'Puan' } },
                    y1: { position: 'right', beginAtZero: true, grid: { drawOnChartArea: false }, title: { display: true, text: 'Adet' } }
                }
            }
        });
    };

    // Survey Chart - Mixed: Bar (yanıt sayısı) + Line (ortalama puan), çift Y ekseni
    self.renderSurveyChart = function(projectTypeId, trendData, summary) {
        var ctx = document.getElementById('monthlyChart_' + projectTypeId);
        if (!ctx) return;

        var labels = [], responseData = [], scoreData = [];
        (trendData || []).forEach(function(item) {
            labels.push(item.month);
            responseData.push(item.responseCount);
            scoreData.push(item.averageScore);
        });
        if (labels.length === 0) return;

        self.monthlyTrendCharts[projectTypeId] = new Chart(ctx, {
            type: 'bar',
            data: {
                labels: labels,
                datasets: [{
                    label: 'Yanıt Sayısı',
                    data: responseData,
                    backgroundColor: 'rgba(108, 117, 125, 0.5)',
                    borderColor: '#6c757d',
                    borderWidth: 1,
                    yAxisID: 'y',
                    order: 2
                }, {
                    label: 'Ortalama Puan (%)',
                    data: scoreData,
                    type: 'line',
                    borderColor: '#198754',
                    backgroundColor: 'rgba(25, 135, 84, 0.1)',
                    fill: true,
                    tension: 0.4,
                    pointRadius: 4,
                    pointBackgroundColor: '#198754',
                    yAxisID: 'y1',
                    order: 1
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: { legend: { position: 'bottom' } },
                scales: {
                    y: { beginAtZero: true, position: 'left', title: { display: true, text: 'Yanıt' } },
                    y1: { beginAtZero: false, min: 0, max: 100, position: 'right', grid: { drawOnChartArea: false }, title: { display: true, text: 'Puan (%)' } }
                }
            }
        });
    };

    // Enneagram Chart - Multi-Line (aylık tip puanları) + Doughnut (kişilik dağılımı)
    self.renderEnneagramChart = function(projectTypeId, typeTrendData, distribution, summary) {
        var distColors = [
            '#6f42c1', '#0d6efd', '#198754', '#dc3545', '#ffc107',
            '#20c997', '#fd7e14', '#0dcaf0', '#d63384', '#6c757d'
        ];

        // Multi-line trend chart (kişilik tipi bazlı aylık puanlar)
        var trendCtx = document.getElementById('monthlyChart_' + projectTypeId);
        if (trendCtx && typeTrendData && typeTrendData.length > 0) {
            var labels = [];
            // Tip isimlerini ilk veri noktasından al
            var typeNames = [];
            if (typeTrendData[0] && typeTrendData[0].types) {
                typeNames = Object.keys(typeTrendData[0].types);
            }

            typeTrendData.forEach(function(item) { labels.push(item.month); });

            var datasets = [];
            typeNames.forEach(function(typeName, idx) {
                var data = typeTrendData.map(function(item) {
                    return item.types && item.types[typeName] != null ? item.types[typeName] : null;
                });
                datasets.push({
                    label: typeName,
                    data: data,
                    borderColor: distColors[idx % distColors.length],
                    backgroundColor: 'transparent',
                    tension: 0.4,
                    pointRadius: 3,
                    borderWidth: 2,
                    spanGaps: true
                });
            });

            if (datasets.length > 0) {
                self.monthlyTrendCharts[projectTypeId] = new Chart(trendCtx, {
                    type: 'line',
                    data: { labels: labels, datasets: datasets },
                    options: {
                        responsive: true,
                        maintainAspectRatio: false,
                        plugins: { legend: { position: 'bottom', labels: { boxWidth: 12 } } },
                        scales: {
                            y: { beginAtZero: false, min: 0, max: 100, title: { display: true, text: 'Puan (%)' } }
                        }
                    }
                });
            }
        }

        // Distribution doughnut chart (aynı)
        var distCtx = document.getElementById('enneagramDistChart_' + projectTypeId);
        if (distCtx && distribution && distribution.length > 0) {
            var distLabels = [], distData = [];
            distribution.forEach(function(d) {
                distLabels.push(d.personalityType);
                distData.push(d.averagePercentage);
            });

            self.monthlyTrendCharts[projectTypeId + '_dist'] = new Chart(distCtx, {
                type: 'doughnut',
                data: {
                    labels: distLabels,
                    datasets: [{
                        data: distData,
                        backgroundColor: distColors.slice(0, distLabels.length)
                    }]
                },
                options: {
                    responsive: true,
                    maintainAspectRatio: false,
                    plugins: {
                        legend: { position: 'bottom', labels: { boxWidth: 12 } }
                    }
                }
            });
        }
    };

    // Reload a single monthly trend panel (filter button click)
    self.reloadMonthlyTrendPanel = function(projectTypeId) {
        // Find the panel item
        var panels = self.monthlyTrendByType();
        var panel = null;
        for (var i = 0; i < panels.length; i++) {
            if (panels[i].projectTypeId === projectTypeId) {
                panel = panels[i];
                break;
            }
        }
        if (!panel) return;

        var panelType = panel.panelType || 'scoreTrend';

        // Enneagram ve survey panelleri özel veri yapısı gerektirir (typeTrend, averageScore)
        // Bu yüzden by-type endpoint'ini kullanıyoruz
        if (panelType === 'enneagram' || panelType === 'survey') {
            var byTypeUrl = '/api/customer/portal/dashboard/monthly-trend-by-type';
            var byTypeParams = [];
            byTypeParams.push('projectTypeId=' + projectTypeId);
            if (panel.startDate()) byTypeParams.push('startDate=' + panel.startDate());
            if (panel.endDate()) byTypeParams.push('endDate=' + panel.endDate());
            if (byTypeParams.length > 0) byTypeUrl += '?' + byTypeParams.join('&');

            customerApiFetch(byTypeUrl)
                .then(function(r) {
                    if (!r.ok) throw new Error('Monthly trend by-type API error: ' + r.status);
                    return r.json();
                })
                .then(function(allPanels) {
                    // İlgili panel verisini bul
                    var found = null;
                    (allPanels || []).forEach(function(p) {
                        if (p.projectTypeId === projectTypeId) found = p;
                    });
                    if (found) {
                        panel.trendData = found.trend;
                        if (found.typeTrend) panel.typeTrend = found.typeTrend;
                        if (found.distribution) panel.distribution = found.distribution;
                        if (found.summary) panel.summary = found.summary;
                    }
                    self.renderMonthlyTrendChart(panel);
                })
                .catch(function(error) {
                    console.error('Monthly trend panel reload error:', error);
                });
        } else {
            var url = '/api/customer/portal/dashboard/monthly-trend';
            var params = ['projectTypeId=' + projectTypeId];
            if (panel.selectedProjectId()) params.push('projectId=' + panel.selectedProjectId());
            if (panel.startDate()) params.push('startDate=' + panel.startDate());
            if (panel.endDate()) params.push('endDate=' + panel.endDate());
            url += '?' + params.join('&');

            customerApiFetch(url)
                .then(function(r) {
                    if (!r.ok) throw new Error('Monthly trend API error: ' + r.status);
                    return r.json();
                })
                .then(function(data) {
                    panel.trendData = data;
                    self.renderMonthlyTrendChart(panel);
                })
                .catch(function(error) {
                    console.error('Monthly trend panel reload error:', error);
                });
        }
    };

    // Export monthly trend panel to Excel
    self.exportMonthlyTrendPanelToExcel = function(projectTypeId) {
        var elementId = 'monthlyChartCard_' + projectTypeId;
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

        // Find the panel data
        var panels = self.monthlyTrendByType();
        var panel = null;
        for (var i = 0; i < panels.length; i++) {
            if (panels[i].projectTypeId === projectTypeId) {
                panel = panels[i];
                break;
            }
        }

        var chartImage = canvas.toDataURL('image/png', 1.0);
        var headerEl = element.querySelector('.card-header h6');
        var chartTitle = headerEl ? headerEl.textContent.trim() : 'Aylik_Trend';
        var dataObj = panel ? panel.trendData : [];

        var requestBody = {
            chartType: 'monthly-trend',
            chartImage: chartImage,
            chartTitle: chartTitle,
            dataJson: JSON.stringify(dataObj)
        };

        var filename = (panel ? panel.projectTypeName : 'Trend') + '_Aylik_Trend';
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
                // Aynı projeleri Puan Dağılımı için de kullan
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
                // Puansız tipleri filtrele (Anket, Enneagram)
                var filteredData = (data || []).filter(function(item) {
                    return nonScoringTypeIds.indexOf(item.projectTypeId) === -1;
                });
                self.scoreDistributionByType(filteredData);
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
                dataObj = [];
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

    // ===== ALL EVALUATIONS MODAL (tamamen izole, başka hiçbir şeyi etkilemez) =====
    self.isAllEvalsModalOpen = ko.observable(false);
    self.isAllEvalsLoading = ko.observable(false);
    self.allEvalsItems = ko.observableArray([]);
    self.allEvalsTotal = ko.observable(0);
    self.allEvalsPage = ko.observable(1);
    self.allEvalsPageSize = 20;
    self.allEvalsProjects = ko.observableArray([]);
    self.allEvalsProjectId = ko.observable('');
    self.allEvalsStartDate = ko.observable('');
    self.allEvalsEndDate = ko.observable('');
    self.allEvalsDateRange = ko.observable('');

    self.allEvalsDateRange.subscribe(function(val) {
        applyDateRange(val, self.allEvalsStartDate, self.allEvalsEndDate);
    });

    self.allEvalsTotalPages = ko.computed(function() {
        return Math.ceil(self.allEvalsTotal() / self.allEvalsPageSize);
    });

    self.getEvalStatusBadgeClass = function(status) {
        switch (status) {
            case 'Completed': return 'bg-success';
            case 'Draft': return 'bg-secondary';
            case 'InProgress': return 'bg-info';
            case 'Pending': return 'bg-warning text-dark';
            case 'Cancelled': return 'bg-danger';
            default: return 'bg-secondary';
        }
    };

    self.openAllEvalsModal = function() {
        self.allEvalsPage(1);
        self.isAllEvalsModalOpen(true);
        // Proje listesini bağımsız olarak yükle (questionTrendProjects'e DOKUNMA)
        if (self.allEvalsProjects().length === 0) {
            customerApiFetch('/api/customer/portal/projects')
                .then(function(r) { return r.ok ? r.json() : []; })
                .then(function(data) { self.allEvalsProjects(data || []); })
                .catch(function() {});
        }
        self.loadAllEvals(1);
    };

    self.closeAllEvalsModal = function() {
        self.isAllEvalsModalOpen(false);
        self.allEvalsItems([]);
        self.allEvalsTotal(0);
    };

    self.filterAllEvals = function() {
        self.allEvalsPage(1);
        self.loadAllEvals(1);
    };

    self.resetAllEvalsFilters = function() {
        self.allEvalsProjectId('');
        self.allEvalsDateRange('');
        self.allEvalsStartDate('');
        self.allEvalsEndDate('');
        self.allEvalsPage(1);
        self.loadAllEvals(1);
    };

    self.loadAllEvals = function(page) {
        self.isAllEvalsLoading(true);
        self.allEvalsPage(page);

        var url = '/api/customer/portal/evaluations?page=' + page + '&pageSize=' + self.allEvalsPageSize;
        if (self.allEvalsProjectId()) url += '&projectId=' + self.allEvalsProjectId();
        if (self.allEvalsStartDate()) url += '&startDate=' + self.allEvalsStartDate();
        if (self.allEvalsEndDate()) url += '&endDate=' + self.allEvalsEndDate();

        customerApiFetch(url)
            .then(function(r) {
                if (!r.ok) throw new Error('Evaluations API error: ' + r.status);
                return r.json();
            })
            .then(function(data) {
                self.allEvalsItems(data.items || []);
                self.allEvalsTotal(data.totalCount || 0);
                self.isAllEvalsLoading(false);
            })
            .catch(function(error) {
                console.error('All evaluations load error:', error);
                self.isAllEvalsLoading(false);
            });
    };

    self.exportAllEvals = function() {
        var url = '/api/customer/portal/evaluations/export?_=1';
        if (self.allEvalsProjectId()) url += '&projectId=' + self.allEvalsProjectId();
        if (self.allEvalsStartDate()) url += '&startDate=' + self.allEvalsStartDate();
        if (self.allEvalsEndDate()) url += '&endDate=' + self.allEvalsEndDate();

        customerApiFetch(url)
            .then(function(r) {
                if (!r.ok) throw new Error('Export error: ' + r.status);
                return r.blob();
            })
            .then(function(blob) {
                var a = document.createElement('a');
                a.href = URL.createObjectURL(blob);
                a.download = 'Degerlendirmeler.xlsx';
                document.body.appendChild(a);
                a.click();
                document.body.removeChild(a);
                URL.revokeObjectURL(a.href);
            })
            .catch(function(error) {
                console.error('Excel export error:', error);
                toastr.error('Excel indirme hatası');
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
