function CustomerSupervisorsViewModel() {
    var self = this;

    // State
    self.isLoading = ko.observable(true);
    self.groups = ko.observableArray([]);

    // Filter options
    self.organizations = ko.observableArray([]);

    // Filter UI - Chip-based filtre sistemi
    self.selectedFilterType = ko.observable('');
    self.tempFilter = {
        organizationId: ko.observable(''),
        searchText: ko.observable('')
    };

    // Active filters (chip-based)
    self.activeFilters = ko.observableArray([]);

    // Can add filter check
    self.canAddFilter = ko.computed(function() {
        var type = self.selectedFilterType();
        if (!type) return false;
        if (type === 'organization') return self.tempFilter.organizationId();
        if (type === 'searchText') return self.tempFilter.searchText();
        return false;
    });

    // Add filter
    self.addFilter = function() {
        var type = self.selectedFilterType();
        if (!type) return;

        var filter = { type: type };
        var label = '';
        var displayValue = '';

        if (type === 'organization') {
            filter.value = self.tempFilter.organizationId();
            var org = self.organizations().find(function(o) { return o.id == filter.value; });
            label = 'Organizasyon';
            displayValue = org ? org.name : filter.value;
        } else if (type === 'searchText') {
            filter.value = self.tempFilter.searchText();
            label = 'Arama';
            displayValue = filter.value;
        }

        // Tüm filtre tipleri çoklu değer destekler
        self.activeFilters.push({
            type: type,
            value: filter.value,
            label: label,
            displayValue: displayValue
        });

        // Reset temp
        self.resetTempFilter();
        self.selectedFilterType('');
        self.search(); // Filtre eklenince otomatik ara
    };

    self.resetTempFilter = function() {
        self.tempFilter.organizationId('');
        self.tempFilter.searchText('');
    };

    self.removeFilter = function(filter) {
        self.activeFilters.remove(filter);
        self.search(); // Filtre kaldırılınca otomatik ara
    };

    self.clearFilters = function() {
        self.activeFilters.removeAll();
        self.search(); // Tüm filtreler temizlenince otomatik ara
    };

    // Search
    self.search = function() {
        self.loadSupervisors();
    };

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

    // Load filter options
    self.loadFilterOptions = function() {
        customerApiFetch('/api/customer/portal/organizations')
            .then(function(response) { return response.json(); })
            .then(function(data) {
                var orgs = [];
                (data || []).forEach(function(group) {
                    (group.organizations || []).forEach(function(org) {
                        orgs.push(org);
                    });
                });
                self.organizations(orgs);
            })
            .catch(function(error) {
                console.error('Error loading organizations:', error);
            });
    };

    // Build query params from active filters
    self.buildQueryParams = function() {
        var params = [];

        // Çoklu değer desteği için array'ler
        var organizationIds = [];
        var searchTexts = [];

        self.activeFilters().forEach(function(f) {
            if (f.type === 'organization') {
                organizationIds.push(f.value);
            } else if (f.type === 'searchText') {
                searchTexts.push(f.value);
            }
        });

        // Çoklu değerleri query string'e ekle
        organizationIds.forEach(function(id) { params.push('organizationId=' + id); });
        if (searchTexts.length > 0) {
            params.push('searchText=' + encodeURIComponent(searchTexts.join(' ')));
        }

        return params.length > 0 ? '?' + params.join('&') : '';
    };

    self.loadSupervisors = function() {
        self.isLoading(true);

        var url = '/api/customer/portal/supervisors' + self.buildQueryParams();

        customerApiFetch(url)
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
    self.loadFilterOptions();
    self.loadSupervisors();
}

$(document).ready(function() {
    ko.applyBindings(new CustomerSupervisorsViewModel(), document.getElementById('customer-supervisors-app'));
});
