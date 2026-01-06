// Penalties Report ViewModel
function PenaltiesViewModel() {
    var self = this;

    // State
    self.isLoading = ko.observable(false);
    self.isExporting = ko.observable(false);
    self.errorMessage = ko.observable('');

    // Filter options
    self.projects = ko.observableArray([]);
    self.branches = ko.observableArray([]);

    // Filters
    self.filter = {
        projectId: ko.observable(''),
        branchId: ko.observable(''),
        penaltyType: ko.observable(''),
        startDate: ko.observable(''),
        endDate: ko.observable('')
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
    self.topPenaltyBranches = ko.observableArray([]);
    self.monthlyTrend = ko.observableArray([]);

    // Chart instance
    var penaltyTrendChart = null;

    // Load filter options
    self.loadFilterOptions = function() {
        // Load projects
        fetch('/api/projects', { credentials: 'include' })
            .then(function(response) { return response.json(); })
            .then(function(data) {
                self.projects(data);
            })
            .catch(function(error) {
                console.error('Error loading projects:', error);
            });

        // Branches modülü kaldırıldı - CustomerOrganizations kullanılıyor
        fetch('/api/customer-organizations', { credentials: 'include' })
            .then(function(response) { return response.json(); })
            .then(function(data) {
                self.branches(data || []);
            })
            .catch(function(error) {
                console.error('Error loading organizations:', error);
                self.branches([]);
            });
    };

    // Load penalty report
    self.loadReport = function() {
        self.isLoading(true);
        self.errorMessage('');

        var params = [];
        if (self.filter.projectId()) params.push('projectId=' + self.filter.projectId());
        if (self.filter.branchId()) params.push('branchId=' + self.filter.branchId());
        if (self.filter.penaltyType()) params.push('penaltyType=' + self.filter.penaltyType());
        if (self.filter.startDate()) params.push('startDate=' + self.filter.startDate());
        if (self.filter.endDate()) params.push('endDate=' + self.filter.endDate());

        var url = '/api/reports/penalties' + (params.length > 0 ? '?' + params.join('&') : '');

        fetch(url, { credentials: 'include' })
            .then(function(response) {
                if (!response.ok) throw new Error(T('Report.LoadError', 'Rapor yüklenemedi'));
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
                self.topPenaltyBranches(data.topPenaltyBranches || []);
                self.monthlyTrend(data.monthlyTrend || []);
                self.updateChart(data.monthlyTrend || []);
            })
            .catch(function(error) {
                console.error('Penalties report error:', error);
                toastr.error(error.message || T('Report.LoadErrorMessage', 'Rapor yüklenirken bir hata oluştu.'));
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
                        label: T('Penalty.YellowCard', 'Sarı Kart'),
                        data: yellowData,
                        backgroundColor: 'rgba(255, 193, 7, 0.7)',
                        borderColor: 'rgb(255, 193, 7)',
                        borderWidth: 1
                    },
                    {
                        label: T('Penalty.RedCard', 'Kırmızı Kart'),
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
        self.loadReport();
    };

    // Clear filters
    self.clearFilters = function() {
        self.filter.projectId('');
        self.filter.branchId('');
        self.filter.penaltyType('');
        self.filter.startDate('');
        self.filter.endDate('');
        self.loadReport();
    };

    // Export to Excel
    self.exportToExcel = function() {
        self.isExporting(true);

        var params = [];
        if (self.filter.projectId()) params.push('projectId=' + self.filter.projectId());
        if (self.filter.branchId()) params.push('branchId=' + self.filter.branchId());
        if (self.filter.penaltyType()) params.push('penaltyType=' + self.filter.penaltyType());
        if (self.filter.startDate()) params.push('startDate=' + self.filter.startDate());
        if (self.filter.endDate()) params.push('endDate=' + self.filter.endDate());

        var url = '/api/reports/penalties/export' + (params.length > 0 ? '?' + params.join('&') : '');

        fetch(url, { credentials: 'include' })
            .then(function(response) {
                if (!response.ok) throw new Error(T('Report.ExportError', 'Export başarısız'));
                return response.blob();
            })
            .then(function(blob) {
                var url = window.URL.createObjectURL(blob);
                var a = document.createElement('a');
                a.href = url;
                a.download = T('File.PenaltyReport', 'CezaliKLRaporu') + '_' + new Date().toISOString().split('T')[0] + '.xlsx';
                document.body.appendChild(a);
                a.click();
                window.URL.revokeObjectURL(url);
                a.remove();
            })
            .catch(function(error) {
                console.error('Export error:', error);
                toastr.error(T('Report.ExcelExportError', 'Excel export başarısız') + ': ' + error.message);
            })
            .finally(function() {
                self.isExporting(false);
            });
    };

    // Initialize
    self.loadFilterOptions();
    self.loadReport();
}

// Apply bindings
$(document).ready(function() {
    ko.applyBindings(new PenaltiesViewModel(), document.getElementById('penalties-app'));
});
