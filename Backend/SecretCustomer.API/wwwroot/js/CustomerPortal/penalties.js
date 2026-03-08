// CustomerPortal Penalties Report ViewModel
function CustomerPenaltiesViewModel() {
    var self = this;

    // State
    self.isLoading = ko.observable(false);
    self.isExporting = ko.observable(false);
    self.errorMessage = ko.observable('');

    // Details Modal State
    self.isDetailsModalOpen = ko.observable(false);
    self.isDetailsLoading = ko.observable(false);
    self.detailsData = ko.observable(null);
    self.isExportingDetail = ko.observable(false);

    // Filter options (müşteriye özel)
    self.projects = ko.observableArray([]);
    self.organizations = ko.observableArray([]);

    // Filter UI - Chip-based filtre sistemi
    self.selectedFilterType = ko.observable('');
    self.tempFilter = {
        projectId: ko.observable(''),
        organizationId: ko.observable(''),
        penaltyType: ko.observable(''),
        startDate: ko.observable(''),
        endDate: ko.observable(''),
        dateRangeType: ko.observable('')
    };

    // Active filters (chip-based)
    self.activeFilters = ko.observableArray([]);

    // Date range labels
    self.dateRangeLabels = {
        'today': 'Bugün',
        'yesterday': 'Dün',
        'thisWeek': 'Bu Hafta',
        'lastWeek': 'Geçen Hafta',
        'thisMonth': 'Bu Ay',
        'lastMonth': 'Geçen Ay',
        'last3Months': 'Son 3 Ay',
        'last6Months': 'Son 6 Ay',
        'thisYear': 'Bu Yıl',
        'lastYear': 'Geçen Yıl'
    };

    // Penalty type labels
    self.penaltyTypeLabels = {
        'YellowCard': 'Sarı Kart',
        'RedCard': 'Kırmızı Kart'
    };

    // Can add filter check
    self.canAddFilter = ko.computed(function() {
        var type = self.selectedFilterType();
        if (!type) return false;
        if (type === 'project') return self.tempFilter.projectId();
        if (type === 'organization') return self.tempFilter.organizationId();
        if (type === 'penaltyType') return self.tempFilter.penaltyType();
        if (type === 'dateRange') return self.tempFilter.startDate() || self.tempFilter.endDate() || self.tempFilter.dateRangeType();
        return false;
    });

    // Date range helper
    self.calculateDateRange = function(rangeType) {
        var today = new Date();
        var start, end;

        if (rangeType === 'today') {
            start = end = formatLocalDate(today);
        } else if (rangeType === 'yesterday') {
            var yesterday = new Date(today);
            yesterday.setDate(yesterday.getDate() - 1);
            start = end = formatLocalDate(yesterday);
        } else if (rangeType === 'thisWeek') {
            var dayOfWeek = today.getDay();
            var diff = today.getDate() - dayOfWeek + (dayOfWeek === 0 ? -6 : 1);
            var weekStart = new Date(today);
            weekStart.setDate(diff);
            start = formatLocalDate(weekStart);
            end = formatLocalDate(today);
        } else if (rangeType === 'lastWeek') {
            var dayOfWeek = today.getDay();
            var diff = today.getDate() - dayOfWeek + (dayOfWeek === 0 ? -6 : 1);
            var thisWeekStart = new Date(today);
            thisWeekStart.setDate(diff);
            var lastWeekStart = new Date(thisWeekStart);
            lastWeekStart.setDate(lastWeekStart.getDate() - 7);
            var lastWeekEnd = new Date(lastWeekStart);
            lastWeekEnd.setDate(lastWeekEnd.getDate() + 6);
            start = formatLocalDate(lastWeekStart);
            end = formatLocalDate(lastWeekEnd);
        } else if (rangeType === 'thisMonth') {
            start = formatLocalDate(new Date(today.getFullYear(), today.getMonth(), 1));
            end = formatLocalDate(today);
        } else if (rangeType === 'lastMonth') {
            start = formatLocalDate(new Date(today.getFullYear(), today.getMonth() - 1, 1));
            end = formatLocalDate(new Date(today.getFullYear(), today.getMonth(), 0));
        } else if (rangeType === 'last3Months') {
            start = formatLocalDate(new Date(today.getFullYear(), today.getMonth() - 2, 1));
            end = formatLocalDate(today);
        } else if (rangeType === 'last6Months') {
            start = formatLocalDate(new Date(today.getFullYear(), today.getMonth() - 5, 1));
            end = formatLocalDate(today);
        } else if (rangeType === 'thisYear') {
            start = formatLocalDate(new Date(today.getFullYear(), 0, 1));
            end = formatLocalDate(today);
        } else if (rangeType === 'lastYear') {
            start = formatLocalDate(new Date(today.getFullYear() - 1, 0, 1));
            end = formatLocalDate(new Date(today.getFullYear() - 1, 11, 31));
        }

        return { start: start, end: end };
    };

    // Date range helper for UI (dropdown içi)
    self.setDateRange = function(rangeType) {
        var range = self.calculateDateRange(rangeType);
        self.tempFilter.startDate(range.start);
        self.tempFilter.endDate(range.end);
        self.tempFilter.dateRangeType(rangeType);
    };

    // Quick date range filter - direkt uygula ve ara
    self.setQuickDateRange = function(rangeType) {
        var range = self.calculateDateRange(rangeType);
        var displayValue = self.dateRangeLabels[rangeType] || (range.start + ' - ' + range.end);

        self.activeFilters.push({
            type: 'dateRange',
            value: null,
            startDate: range.start,
            endDate: range.end,
            dateRangeType: rangeType,
            label: 'Tarih',
            displayValue: displayValue
        });

        self.search();
    };

    // Add filter
    self.addFilter = function() {
        var type = self.selectedFilterType();
        if (!type) return;

        var filter = { type: type };
        var label = '';
        var displayValue = '';

        if (type === 'project') {
            filter.value = self.tempFilter.projectId();
            var project = self.projects().find(function(p) { return p.id == filter.value; });
            label = 'Proje';
            displayValue = project ? project.name : filter.value;
        } else if (type === 'organization') {
            filter.value = self.tempFilter.organizationId();
            var org = self.organizations().find(function(o) { return o.id == filter.value; });
            label = 'Organizasyon';
            displayValue = org ? org.name : filter.value;
        } else if (type === 'penaltyType') {
            filter.value = self.tempFilter.penaltyType();
            label = 'Ceza Tipi';
            displayValue = self.penaltyTypeLabels[filter.value] || filter.value;
        } else if (type === 'dateRange') {
            filter.dateRangeType = self.tempFilter.dateRangeType();
            filter.startDate = self.tempFilter.startDate();
            filter.endDate = self.tempFilter.endDate();
            label = 'Tarih';
            if (filter.dateRangeType && self.dateRangeLabels[filter.dateRangeType]) {
                displayValue = self.dateRangeLabels[filter.dateRangeType];
            } else {
                displayValue = (filter.startDate || '...') + ' - ' + (filter.endDate || '...');
            }
        }

        // Tüm filtre tipleri çoklu değer destekler
        self.activeFilters.push({
            type: type,
            value: filter.value,
            startDate: filter.startDate,
            endDate: filter.endDate,
            dateRangeType: filter.dateRangeType,
            label: label,
            displayValue: displayValue
        });

        // Reset temp
        self.resetTempFilter();
        self.selectedFilterType('');
        self.search(); // Filtre eklenince otomatik ara
    };

    self.resetTempFilter = function() {
        self.tempFilter.projectId('');
        self.tempFilter.organizationId('');
        self.tempFilter.penaltyType('');
        self.tempFilter.startDate('');
        self.tempFilter.endDate('');
        self.tempFilter.dateRangeType('');
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
        self.page(1);
        self.loadReport();
    };

    // Summary
    self.summary = ko.observable({
        totalPenalties: 0,
        totalYellowCards: 0,
        totalRedCards: 0,
        affectedEvaluations: 0
    });

    // Data
    self.penalties = ko.observableArray([]);
    self.topPenaltyQuestions = ko.observableArray([]);
    self.topPenaltyOrganizations = ko.observableArray([]);
    self.topPenaltyPersonnel = ko.observableArray([]);
    self.monthlyTrend = ko.observableArray([]);

    // Pagination
    self.page = ko.observable(1);
    self.pageSize = ko.observable(50);
    self.totalCount = ko.observable(0);
    self.totalPages = ko.observable(0);

    // Chart instance
    var penaltyTrendChart = null;

    // Load filter options
    self.loadFilterOptions = function() {
        // Load projects (müşteriye ait)
        customerApiFetch('/api/customer/portal/projects')
            .then(function(response) { return response.json(); })
            .then(function(data) {
                self.projects(data || []);
            })
            .catch(function(error) {
                console.error('Error loading projects:', error);
            });

        // Load organizations (müşteriye ait)
        customerApiFetch('/api/customer/portal/organizations')
            .then(function(response) { return response.json(); })
            .then(function(data) {
                // Flatten grouped organizations
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
        var projectIds = [];
        var organizationIds = [];
        var penaltyTypes = [];

        self.activeFilters().forEach(function(f) {
            if (f.type === 'project') {
                projectIds.push(f.value);
            } else if (f.type === 'organization') {
                organizationIds.push(f.value);
            } else if (f.type === 'penaltyType') {
                penaltyTypes.push(f.value);
            } else if (f.type === 'dateRange') {
                if (f.dateRangeType && self.dateRangeLabels[f.dateRangeType]) {
                    var range = self.calculateDateRange(f.dateRangeType);
                    params.push('startDate=' + range.start);
                    params.push('endDate=' + range.end);
                } else {
                    if (f.startDate) params.push('startDate=' + f.startDate);
                    if (f.endDate) params.push('endDate=' + f.endDate);
                }
            }
        });

        // Çoklu değerleri query string'e ekle
        projectIds.forEach(function(id) { params.push('projectId=' + id); });
        organizationIds.forEach(function(id) { params.push('organizationId=' + id); });
        penaltyTypes.forEach(function(type) { params.push('penaltyType=' + type); });

        params.push('page=' + self.page());
        params.push('pageSize=' + self.pageSize());
        return params;
    };

    // Load penalty report
    self.loadReport = function() {
        self.isLoading(true);
        self.errorMessage('');

        var params = self.buildQueryParams();
        var url = '/api/customer/portal/reports/penalties' + (params.length > 0 ? '?' + params.join('&') : '');

        customerApiFetch(url)
            .then(function(response) {
                if (!response.ok) throw new Error('Rapor yüklenemedi');
                return response.json();
            })
            .then(function(data) {
                self.summary(data.summary || {
                    totalPenalties: 0,
                    totalYellowCards: 0,
                    totalRedCards: 0,
                    affectedEvaluations: 0
                });
                self.penalties(data.penalties || []);
                self.topPenaltyQuestions(data.topPenaltyQuestions || []);
                self.topPenaltyOrganizations(data.topPenaltyOrganizations || []);
                self.topPenaltyPersonnel(data.topPenaltyPersonnel || []);
                self.monthlyTrend(data.monthlyTrend || []);
                self.totalCount(data.totalCount || 0);
                self.totalPages(data.totalPages || 0);
                self.updateChart(data.monthlyTrend || []);
            })
            .catch(function(error) {
                console.error('Penalties report error:', error);
                toastr.error(error.message || 'Rapor yüklenirken bir hata oluştu.');
            })
            .finally(function() {
                self.isLoading(false);
            });
    };

    // Update chart
    self.updateChart = function(monthlyTrend) {
        var ctx = document.getElementById('penaltyTrendChart');
        if (!ctx) return;

        if (penaltyTrendChart) {
            penaltyTrendChart.destroy();
        }

        var labels = monthlyTrend.map(function(m) { return m.monthName; });
        var yellowData = monthlyTrend.map(function(m) { return m.yellowCardCount; });
        var redData = monthlyTrend.map(function(m) { return m.redCardCount; });

        penaltyTrendChart = new Chart(ctx, {
            type: 'bar',
            data: {
                labels: labels,
                datasets: [
                    {
                        label: 'Sarı Kart',
                        data: yellowData,
                        backgroundColor: 'rgba(255, 193, 7, 0.7)',
                        borderColor: 'rgb(255, 193, 7)',
                        borderWidth: 1
                    },
                    {
                        label: 'Kırmızı Kart',
                        data: redData,
                        backgroundColor: 'rgba(220, 53, 69, 0.7)',
                        borderColor: 'rgb(220, 53, 69)',
                        borderWidth: 1
                    }
                ]
            },
            options: {
                responsive: true,
                maintainAspectRatio: true,
                plugins: {
                    legend: {
                        position: 'top'
                    }
                },
                scales: {
                    y: {
                        beginAtZero: true,
                        ticks: {
                            stepSize: 1
                        }
                    }
                }
            }
        });
    };

    // Pagination
    self.previousPage = function() {
        if (self.page() > 1) {
            self.page(self.page() - 1);
            self.loadReport();
        }
    };

    self.nextPage = function() {
        if (self.page() < self.totalPages()) {
            self.page(self.page() + 1);
            self.loadReport();
        }
    };

    // Export to Excel
    self.exportToExcel = function() {
        self.isExporting(true);

        var params = [];

        // Çoklu değer desteği için array'ler
        var projectIds = [];
        var organizationIds = [];
        var penaltyTypes = [];

        self.activeFilters().forEach(function(f) {
            if (f.type === 'project') {
                projectIds.push(f.value);
            } else if (f.type === 'organization') {
                organizationIds.push(f.value);
            } else if (f.type === 'penaltyType') {
                penaltyTypes.push(f.value);
            } else if (f.type === 'dateRange') {
                if (f.dateRangeType && self.dateRangeLabels[f.dateRangeType]) {
                    var range = self.calculateDateRange(f.dateRangeType);
                    params.push('startDate=' + range.start);
                    params.push('endDate=' + range.end);
                } else {
                    if (f.startDate) params.push('startDate=' + f.startDate);
                    if (f.endDate) params.push('endDate=' + f.endDate);
                }
            }
        });

        projectIds.forEach(function(id) { params.push('projectId=' + id); });
        organizationIds.forEach(function(id) { params.push('organizationId=' + id); });
        penaltyTypes.forEach(function(type) { params.push('penaltyType=' + type); });

        var url = '/api/customer/portal/reports/penalties/export' + (params.length > 0 ? '?' + params.join('&') : '');

        customerApiFetch(url)
            .then(function(response) {
                if (!response.ok) throw new Error('Export başarısız');
                return response.blob();
            })
            .then(function(blob) {
                var url = window.URL.createObjectURL(blob);
                var a = document.createElement('a');
                a.href = url;
                a.download = 'CezaliKLRaporu_' + formatLocalDate(new Date()) + '.xlsx';
                document.body.appendChild(a);
                a.click();
                window.URL.revokeObjectURL(url);
                a.remove();
            })
            .catch(function(error) {
                console.error('Export error:', error);
                toastr.error('Excel export başarısız: ' + error.message);
            })
            .finally(function() {
                self.isExporting(false);
            });
    };

    // Details Modal Functions
    self.showDetails = function(penalty) {
        self.isDetailsModalOpen(true);
        self.isDetailsLoading(true);
        self.detailsData(null);

        customerApiFetch('/api/customer/portal/evaluations/' + penalty.evaluationId)
            .then(function(response) {
                if (!response.ok) throw new Error('Detay yuklenemedi');
                return response.json();
            })
            .then(function(data) {
                self.detailsData(data);
            })
            .catch(function(error) {
                console.error('Details load error:', error);
                toastr.error('Değerlendirme detayı yüklenemedi.');
                self.closeDetailsModal();
            })
            .finally(function() {
                self.isDetailsLoading(false);
            });
    };

    self.closeDetailsModal = function() {
        self.isDetailsModalOpen(false);
        self.detailsData(null);
    };

    self.exportDetailToExcel = function() {
        var data = self.detailsData();
        if (!data) return;

        self.isExportingDetail(true);

        customerApiFetch('/api/customer/portal/evaluations/' + data.id + '/export')
            .then(function(response) {
                if (!response.ok) throw new Error('Export başarısız');
                return response.blob();
            })
            .then(function(blob) {
                var url = window.URL.createObjectURL(blob);
                var a = document.createElement('a');
                a.href = url;
                a.download = 'Degerlendirme_' + data.id + '_' + formatLocalDate(new Date()) + '.xlsx';
                document.body.appendChild(a);
                a.click();
                window.URL.revokeObjectURL(url);
                a.remove();
            })
            .catch(function(error) {
                console.error('Export error:', error);
                toastr.error('Excel export başarısız.');
            })
            .finally(function() {
                self.isExportingDetail(false);
            });
    };

    // Score class helper
    self.getScoreClass = function(score, projectTypeId) {
        return ScoreThresholds.getScoreClass(score, projectTypeId);
    };

    // Initialize
    self.loadFilterOptions();
    self.loadReport();
}

// Apply bindings when DOM is ready
$(document).ready(function() {
    ko.applyBindings(new CustomerPenaltiesViewModel(), document.getElementById('penalties-app'));
});
