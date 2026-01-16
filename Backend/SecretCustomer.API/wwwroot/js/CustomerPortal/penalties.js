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

    // Filters (Müşteri ve Değerlendirici filtresi YOK - otomatik token'dan)
    self.filter = {
        projectId: ko.observable(''),
        organizationId: ko.observable(''),
        penaltyType: ko.observable(''),
        startDate: ko.observable(''),
        endDate: ko.observable(''),
        page: ko.observable(1),
        pageSize: ko.observable(50)
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

    // Build query params
    self.buildQueryParams = function() {
        var params = [];
        if (self.filter.projectId()) params.push('projectId=' + self.filter.projectId());
        if (self.filter.organizationId()) params.push('organizationId=' + self.filter.organizationId());
        if (self.filter.penaltyType()) params.push('penaltyType=' + self.filter.penaltyType());
        if (self.filter.startDate()) params.push('startDate=' + self.filter.startDate());
        if (self.filter.endDate()) params.push('endDate=' + self.filter.endDate());
        params.push('page=' + self.filter.page());
        params.push('pageSize=' + self.filter.pageSize());
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

    // Apply filters
    self.applyFilters = function() {
        self.filter.page(1); // Reset to first page
        self.loadReport();
    };

    // Clear filters
    self.clearFilters = function() {
        self.filter.projectId('');
        self.filter.organizationId('');
        self.filter.penaltyType('');
        self.filter.startDate('');
        self.filter.endDate('');
        self.filter.page(1);
        self.loadReport();
    };

    // Pagination
    self.previousPage = function() {
        if (self.filter.page() > 1) {
            self.filter.page(self.filter.page() - 1);
            self.loadReport();
        }
    };

    self.nextPage = function() {
        if (self.filter.page() < self.totalPages()) {
            self.filter.page(self.filter.page() + 1);
            self.loadReport();
        }
    };

    // Export to Excel
    self.exportToExcel = function() {
        self.isExporting(true);

        var params = [];
        if (self.filter.projectId()) params.push('projectId=' + self.filter.projectId());
        if (self.filter.organizationId()) params.push('organizationId=' + self.filter.organizationId());
        if (self.filter.penaltyType()) params.push('penaltyType=' + self.filter.penaltyType());
        if (self.filter.startDate()) params.push('startDate=' + self.filter.startDate());
        if (self.filter.endDate()) params.push('endDate=' + self.filter.endDate());

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
                a.download = 'CezaliKLRaporu_' + new Date().toISOString().split('T')[0] + '.xlsx';
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
                a.download = 'Degerlendirme_' + data.id + '_' + new Date().toISOString().split('T')[0] + '.xlsx';
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
    self.getScoreClass = function(score) {
        if (score >= 80) return 'text-success';
        if (score >= 60) return 'text-warning';
        return 'text-danger';
    };

    // Initialize
    self.loadFilterOptions();
    self.loadReport();
}

// Apply bindings when DOM is ready
$(document).ready(function() {
    ko.applyBindings(new CustomerPenaltiesViewModel(), document.getElementById('penalties-app'));
});
