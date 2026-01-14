// Penalties Report ViewModel
function PenaltiesViewModel() {
    var self = this;

    // State
    self.isLoading = ko.observable(false);
    self.isExporting = ko.observable(false);
    self.errorMessage = ko.observable('');

    // Filter options
    self.customers = ko.observableArray([]);
    self.projects = ko.observableArray([]);
    self.organizations = ko.observableArray([]);
    self.checklists = ko.observableArray([]);
    self.evaluators = ko.observableArray([]);

    // Filters
    self.filter = {
        customerId: ko.observable(''),
        projectId: ko.observable(''),
        organizationId: ko.observable(''),
        checklistId: ko.observable(''),
        evaluatorId: ko.observable(''),
        penaltyType: ko.observable(''),
        startDate: ko.observable(''),
        endDate: ko.observable(''),
        page: ko.observable(1),
        pageSize: ko.observable(50)
    };

    // Filtered options based on customer selection
    self.filteredProjects = ko.computed(function() {
        var customerId = self.filter.customerId();
        if (!customerId) return self.projects();
        return self.projects().filter(function(p) {
            return p.customerId == customerId;
        });
    });

    self.filteredOrganizations = ko.computed(function() {
        var customerId = self.filter.customerId();
        if (!customerId) return self.organizations();
        return self.organizations().filter(function(o) {
            return o.customerId == customerId;
        });
    });

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

    // Customer change handler
    self.onCustomerChange = function() {
        self.filter.projectId('');
        self.filter.organizationId('');
    };

    // Load filter options
    self.loadFilterOptions = function() {
        // Load customers
        fetch('/api/customers', { credentials: 'include' })
            .then(function(response) { return response.json(); })
            .then(function(data) {
                self.customers(data || []);
            })
            .catch(function(error) {
                console.error('Error loading customers:', error);
            });

        // Load projects
        fetch('/api/projects', { credentials: 'include' })
            .then(function(response) { return response.json(); })
            .then(function(data) {
                self.projects(data || []);
            })
            .catch(function(error) {
                console.error('Error loading projects:', error);
            });

        // Load organizations
        fetch('/api/customer-organizations', { credentials: 'include' })
            .then(function(response) { return response.json(); })
            .then(function(data) {
                self.organizations(data || []);
            })
            .catch(function(error) {
                console.error('Error loading organizations:', error);
            });

        // Load checklists
        fetch('/api/checklists', { credentials: 'include' })
            .then(function(response) { return response.json(); })
            .then(function(data) {
                self.checklists(data || []);
            })
            .catch(function(error) {
                console.error('Error loading checklists:', error);
            });

        // Load evaluators (users)
        fetch('/api/users', { credentials: 'include' })
            .then(function(response) { return response.json(); })
            .then(function(data) {
                var users = (data || []).map(function(u) {
                    return {
                        id: u.id,
                        fullName: u.firstName + ' ' + u.lastName
                    };
                });
                self.evaluators(users);
            })
            .catch(function(error) {
                console.error('Error loading evaluators:', error);
            });
    };

    // Build query params
    self.buildQueryParams = function() {
        var params = [];
        if (self.filter.projectId()) params.push('projectId=' + self.filter.projectId());
        if (self.filter.customerId()) params.push('customerId=' + self.filter.customerId());
        if (self.filter.organizationId()) params.push('organizationId=' + self.filter.organizationId());
        if (self.filter.checklistId()) params.push('checklistId=' + self.filter.checklistId());
        if (self.filter.evaluatorId()) params.push('evaluatorId=' + self.filter.evaluatorId());
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
        var url = '/api/reports/penalties' + (params.length > 0 ? '?' + params.join('&') : '');

        fetch(url, { credentials: 'include' })
            .then(function(response) {
                if (!response.ok) throw new Error(T('Report.LoadError', 'Rapor yuklenemedi'));
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
                toastr.error(error.message || T('Report.LoadErrorMessage', 'Rapor yuklenirken bir hata olustu.'));
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
                        label: T('Penalty.YellowCard', 'Sari Kart'),
                        data: yellowData,
                        backgroundColor: 'rgba(255, 193, 7, 0.7)',
                        borderColor: 'rgb(255, 193, 7)',
                        borderWidth: 1
                    },
                    {
                        label: T('Penalty.RedCard', 'Kirmizi Kart'),
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
        self.filter.customerId('');
        self.filter.projectId('');
        self.filter.organizationId('');
        self.filter.checklistId('');
        self.filter.evaluatorId('');
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
        if (self.filter.customerId()) params.push('customerId=' + self.filter.customerId());
        if (self.filter.organizationId()) params.push('organizationId=' + self.filter.organizationId());
        if (self.filter.checklistId()) params.push('checklistId=' + self.filter.checklistId());
        if (self.filter.evaluatorId()) params.push('evaluatorId=' + self.filter.evaluatorId());
        if (self.filter.penaltyType()) params.push('penaltyType=' + self.filter.penaltyType());
        if (self.filter.startDate()) params.push('startDate=' + self.filter.startDate());
        if (self.filter.endDate()) params.push('endDate=' + self.filter.endDate());

        var url = '/api/reports/penalties/export' + (params.length > 0 ? '?' + params.join('&') : '');

        fetch(url, { credentials: 'include' })
            .then(function(response) {
                if (!response.ok) throw new Error(T('Report.ExportError', 'Export basarisiz'));
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
                toastr.error(T('Report.ExcelExportError', 'Excel export basarisiz') + ': ' + error.message);
            })
            .finally(function() {
                self.isExporting(false);
            });
    };

    // Initialize
    self.loadFilterOptions();
    self.loadReport();
}

// Translation keys
var TRANSLATION_KEYS = [
    'Report.LoadError',
    'Report.LoadErrorMessage',
    'Report.ExportError',
    'Report.ExcelExportError',
    'Penalty.YellowCard',
    'Penalty.RedCard',
    'File.PenaltyReport'
];

// Apply bindings
$(document).ready(function() {
    Localization.loadKeys(TRANSLATION_KEYS).then(function() {
        ko.applyBindings(new PenaltiesViewModel(), document.getElementById('penalties-app'));
    });
});
