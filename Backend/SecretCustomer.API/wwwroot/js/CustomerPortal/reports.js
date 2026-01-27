function CustomerReportsViewModel() {
    var self = this;

    // State
    self.isLoading = ko.observable(true);
    self.summary = ko.observable({});
    self.projectPerformance = ko.observableArray([]);
    self.monthlyTrend = ko.observableArray([]);
    self.trendChart = null;

    // Lookup data
    self.projects = ko.observableArray([]);
    self.organizations = ko.observableArray([]);

    // Filter system
    self.selectedFilterType = ko.observable('');
    self.activeFilters = ko.observableArray([]);

    // Temp filter values
    self.tempFilter = {
        projectId: ko.observable(null),
        organizationId: ko.observable(null),
        startDate: ko.observable(''),
        endDate: ko.observable(''),
        selectedDateRangeType: ko.observable(null)
    };

    // Date range type reset on manual date change
    self.tempFilter.startDate.subscribe(function() {
        if (self._manualDateChange) {
            self.tempFilter.selectedDateRangeType(null);
        }
    });
    self.tempFilter.endDate.subscribe(function() {
        if (self._manualDateChange) {
            self.tempFilter.selectedDateRangeType(null);
        }
    });
    self._manualDateChange = true;

    // Filter labels
    self.filterLabels = {
        project: 'Proje',
        organization: 'Organizasyon',
        dateRange: 'Tarih'
    };

    // Date range presets
    self.dateRanges = [
        { systemName: 'today', name: 'Bugün' },
        { systemName: 'yesterday', name: 'Dün' },
        { systemName: 'thisWeek', name: 'Bu Hafta' },
        { systemName: 'lastWeek', name: 'Geçen Hafta' },
        { systemName: 'thisMonth', name: 'Bu Ay' },
        { systemName: 'lastMonth', name: 'Geçen Ay' },
        { systemName: 'last7Days', name: 'Son 7 Gün' },
        { systemName: 'last30Days', name: 'Son 30 Gün' }
    ];

    // Score Range Modal State
    self.isScoreRangeModalOpen = ko.observable(false);
    self.isScoreRangeLoading = ko.observable(false);
    self.scoreRangeModalTitle = ko.observable('');
    self.scoreRangeModalColor = ko.observable('primary');
    self.scoreRangeEvaluations = ko.observableArray([]);

    // Evaluation Details Modal State
    self.isDetailsModalOpen = ko.observable(false);
    self.isDetailsLoading = ko.observable(false);
    self.detailsData = ko.observable(null);

    // Helpers
    self.getScoreBadgeClass = function(score) {
        if (score >= 90) return 'bg-success';
        if (score >= 80) return 'bg-primary';
        if (score >= 60) return 'bg-warning text-dark';
        return 'bg-danger';
    };

    // Can add filter computed
    self.canAddFilter = ko.computed(function() {
        var type = self.selectedFilterType();
        if (!type) return false;

        switch (type) {
            case 'project': return self.tempFilter.projectId();
            case 'organization': return self.tempFilter.organizationId();
            case 'dateRange': return self.tempFilter.startDate() || self.tempFilter.endDate();
            default: return false;
        }
    });

    // Add filter
    self.addFilter = function() {
        var type = self.selectedFilterType();
        if (!type) return;

        var filter = {
            type: type,
            label: self.filterLabels[type],
            value: null,
            displayValue: ''
        };

        switch (type) {
            case 'project':
                var projectId = self.tempFilter.projectId();
                var project = self.projects().find(function(p) { return p.id === projectId; });
                if (!project) return;
                filter.value = projectId;
                filter.displayValue = project.name;
                self.tempFilter.projectId(null);
                break;

            case 'organization':
                var orgId = self.tempFilter.organizationId();
                var org = self.organizations().find(function(o) { return o.id === orgId; });
                if (!org) return;
                filter.value = orgId;
                filter.displayValue = org.name;
                self.tempFilter.organizationId(null);
                break;

            case 'dateRange':
                var startDate = self.tempFilter.startDate();
                var endDate = self.tempFilter.endDate();
                var dateRangeType = self.tempFilter.selectedDateRangeType();
                if (!startDate && !endDate) return;

                filter.value = {
                    startDate: startDate,
                    endDate: endDate,
                    dateRangeType: dateRangeType
                };

                if (dateRangeType) {
                    var rangeInfo = self.dateRanges.find(function(r) { return r.systemName === dateRangeType; });
                    filter.displayValue = rangeInfo ? rangeInfo.name : dateRangeType;
                } else {
                    filter.displayValue = (startDate || '...') + ' - ' + (endDate || '...');
                }

                self.tempFilter.startDate('');
                self.tempFilter.endDate('');
                self.tempFilter.selectedDateRangeType(null);
                break;

            default:
                return;
        }

        self.activeFilters.push(filter);
        self.selectedFilterType('');
        self.loadReports();
    };

    // Remove filter
    self.removeFilter = function(filter) {
        self.activeFilters.remove(filter);
        self.loadReports();
    };

    // Clear all filters
    self.clearFilters = function() {
        self.activeFilters([]);
        self.loadReports();
    };

    // Set temp date range (quick select buttons)
    self.setTempDateRange = function(range) {
        var today = new Date();
        today.setHours(0, 0, 0, 0);
        var startDate = null;
        var endDate = null;

        var formatDate = function(date) {
            var year = date.getFullYear();
            var month = String(date.getMonth() + 1).padStart(2, '0');
            var day = String(date.getDate()).padStart(2, '0');
            return year + '-' + month + '-' + day;
        };

        var getMonday = function(d) {
            var date = new Date(d.getTime());
            var day = date.getDay();
            var diff = date.getDate() - day + (day === 0 ? -6 : 1);
            date.setDate(diff);
            return date;
        };

        switch (range) {
            case 'today':
                startDate = new Date(today.getTime());
                endDate = new Date(today.getTime());
                break;
            case 'yesterday':
                var yesterday = new Date(today.getTime());
                yesterday.setDate(yesterday.getDate() - 1);
                startDate = yesterday;
                endDate = new Date(yesterday.getTime());
                break;
            case 'thisWeek':
                startDate = getMonday(today);
                endDate = new Date(today.getTime());
                break;
            case 'lastWeek':
                var lastWeekStart = getMonday(today);
                lastWeekStart.setDate(lastWeekStart.getDate() - 7);
                var lastWeekEnd = new Date(lastWeekStart.getTime());
                lastWeekEnd.setDate(lastWeekEnd.getDate() + 6);
                startDate = lastWeekStart;
                endDate = lastWeekEnd;
                break;
            case 'thisMonth':
                startDate = new Date(today.getFullYear(), today.getMonth(), 1);
                endDate = new Date(today.getTime());
                break;
            case 'lastMonth':
                startDate = new Date(today.getFullYear(), today.getMonth() - 1, 1);
                endDate = new Date(today.getFullYear(), today.getMonth(), 0);
                break;
            case 'last7Days':
                startDate = new Date(today.getTime());
                startDate.setDate(startDate.getDate() - 6);
                endDate = new Date(today.getTime());
                break;
            case 'last30Days':
                startDate = new Date(today.getTime());
                startDate.setDate(startDate.getDate() - 29);
                endDate = new Date(today.getTime());
                break;
        }

        self._manualDateChange = false;
        if (startDate) self.tempFilter.startDate(formatDate(startDate));
        if (endDate) self.tempFilter.endDate(formatDate(endDate));
        self.tempFilter.selectedDateRangeType(range);
        self._manualDateChange = true;
    };

    // Build query params using URLSearchParams
    self.buildQueryParams = function() {
        var params = new URLSearchParams();

        self.activeFilters().forEach(function(filter) {
            switch (filter.type) {
                case 'project':
                    params.append('projectIds', filter.value);
                    break;
                case 'organization':
                    params.append('organizationIds', filter.value);
                    break;
                case 'dateRange':
                    if (filter.value.startDate) params.append('startDate', filter.value.startDate);
                    if (filter.value.endDate) params.append('endDate', filter.value.endDate);
                    break;
            }
        });

        return params.toString();
    };

    // Build query string (for backward compatibility)
    self.buildQueryString = function() {
        var params = self.buildQueryParams();
        return params ? '?' + params : '';
    };

    // Load filter options
    self.loadFilterOptions = function() {
        // Load projects
        customerApiFetch('/api/customer/portal/projects')
            .then(function(r) { return r.json(); })
            .then(function(data) {
                self.projects(data || []);
            })
            .catch(function(error) {
                console.error('Error loading projects:', error);
            });

        // Load organizations
        customerApiFetch('/api/customer/portal/organizations')
            .then(function(r) { return r.json(); })
            .then(function(data) {
                // Flatten if hierarchical
                var orgs = data || [];
                self.organizations(orgs);
            })
            .catch(function(error) {
                console.error('Error loading organizations:', error);
            });
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

        // Build params from active filters + score range
        var params = new URLSearchParams();

        self.activeFilters().forEach(function(filter) {
            switch (filter.type) {
                case 'project':
                    params.append('projectIds', filter.value);
                    break;
                case 'organization':
                    params.append('organizationIds', filter.value);
                    break;
                case 'dateRange':
                    if (filter.value.startDate) params.append('startDate', filter.value.startDate);
                    if (filter.value.endDate) params.append('endDate', filter.value.endDate);
                    break;
            }
        });

        params.append('minScore', minScore);
        params.append('maxScore', maxScore);

        var url = '/api/customer/portal/reports/score-range?' + params.toString();

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

    // Show Evaluation Details Modal
    self.showDetails = function(evaluation) {
        self.isDetailsModalOpen(true);
        self.isDetailsLoading(true);
        self.detailsData(null);

        customerApiFetch('/api/customer/portal/evaluations/' + evaluation.evaluationId)
            .then(function(response) {
                if (!response.ok) throw new Error('Detay yüklenemedi');
                return response.json();
            })
            .then(function(data) {
                self.detailsData(data);
            })
            .catch(function(error) {
                console.error('Details load error:', error);
                toastr.error('Detay yüklenirken hata oluştu.');
                self.closeDetailsModal();
            })
            .finally(function() {
                self.isDetailsLoading(false);
            });
    };

    // Close Details Modal
    self.closeDetailsModal = function() {
        self.isDetailsModalOpen(false);
        self.detailsData(null);
    };

    // Score class helpers for details modal
    self.getScoreClass = function(score) {
        if (score >= 90) return 'text-success';
        if (score >= 80) return 'text-primary';
        if (score >= 60) return 'text-warning';
        return 'text-danger';
    };

    self.getProgressBarClass = function(score) {
        if (score >= 90) return 'bg-success';
        if (score >= 80) return 'bg-primary';
        if (score >= 60) return 'bg-warning';
        return 'bg-danger';
    };

    // Initialize with default date filter (last 3 months)
    self.initDefaultFilters = function() {
        var today = new Date();
        var threeMonthsAgo = new Date(today.getFullYear(), today.getMonth() - 3, 1);

        var formatDate = function(date) {
            var year = date.getFullYear();
            var month = String(date.getMonth() + 1).padStart(2, '0');
            var day = String(date.getDate()).padStart(2, '0');
            return year + '-' + month + '-' + day;
        };

        // Add default date filter
        self.activeFilters.push({
            type: 'dateRange',
            label: self.filterLabels.dateRange,
            value: {
                startDate: formatDate(threeMonthsAgo),
                endDate: formatDate(today),
                dateRangeType: null
            },
            displayValue: formatDate(threeMonthsAgo) + ' - ' + formatDate(today)
        });
    };

    // Initialize
    self.loadFilterOptions();
    self.initDefaultFilters();
    self.loadReports();
}

// Apply bindings when DOM is ready
$(document).ready(function() {
    ko.applyBindings(new CustomerReportsViewModel(), document.getElementById('customer-reports-app'));
});
