// CustomerPortal Enneagram Results ViewModel
function CustomerEnneagramResultsViewModel() {
    var self = this;

    // State
    self.isLoading = ko.observable(true);
    self.isLoadingResults = ko.observable(false);
    self.isLoadingDetail = ko.observable(false);
    self.isLoadingDistribution = ko.observable(false);

    // Data
    self.projects = ko.observableArray([]);
    self.results = ko.observableArray([]);
    self.summary = ko.observable({});
    self.detail = ko.observable(null);
    self.distribution = ko.observableArray([]);
    self.distributionInfo = ko.observable({ totalResponses: 0, projectName: '' });

    // Computed: Distribution total points
    self.distributionTotalPoints = ko.computed(function() {
        var total = 0;
        self.distribution().forEach(function(d) {
            total += d.totalPoints || 0;
        });
        return total;
    });

    // Pagination
    self.currentPage = ko.observable(1);
    self.pageSize = ko.observable(50);
    self.totalCount = ko.observable(0);
    self.totalPages = ko.observable(0);

    // Filters
    self.filter = {
        projectId: ko.observable(null),
        searchTerm: ko.observable('')
    };

    // Modals
    self.detailModal = null;

    // Initialize modal
    self.initModal = function() {
        var modalEl = document.getElementById('enneagramDetailModal');
        if (modalEl) {
            self.detailModal = new bootstrap.Modal(modalEl);
        }
    };

    // Load initial data
    self.loadData = function() {
        self.isLoading(true);

        // Load projects
        customerApiFetch('/api/customer/portal/reports/enneagram-projects')
            .then(function(r) { return r.json(); })
            .then(function(data) {
                self.projects(data || []);
            })
            .catch(function(error) {
                console.error('Error loading Enneagram projects:', error);
                toastr.error('Projeler yuklenirken hata olustu.');
            })
            .finally(function() {
                self.isLoading(false);
                // Load results after projects
                self.loadResults();
            });
    };

    // Build filter params using URLSearchParams
    self.buildFilterParams = function() {
        var params = new URLSearchParams();

        if (self.filter.projectId()) {
            params.append('projectId', self.filter.projectId());
        }
        if (self.filter.searchTerm()) {
            params.append('searchTerm', self.filter.searchTerm());
        }

        params.append('page', self.currentPage());
        params.append('pageSize', self.pageSize());

        return params.toString();
    };

    // Load results with filters
    self.loadResults = function() {
        self.isLoadingResults(true);
        self.currentPage(1);

        var queryString = self.buildFilterParams();
        var url = '/api/customer/portal/reports/enneagram-results' + (queryString ? '?' + queryString : '');

        customerApiFetch(url)
            .then(function(r) { return r.json(); })
            .then(function(data) {
                self.results(data.results || []);
                self.summary(data.summary || {});
                self.totalCount(data.totalCount || 0);
                self.totalPages(data.totalPages || 0);
            })
            .catch(function(error) {
                console.error('Error loading Enneagram results:', error);
                toastr.error('Sonuclar yuklenirken hata olustu.');
            })
            .finally(function() {
                self.isLoadingResults(false);
            });

        // Also load distribution if project selected
        self.loadDistribution();
    };

    // Load personality distribution for selected project
    self.loadDistribution = function() {
        var projectId = self.filter.projectId();

        if (!projectId) {
            self.distribution([]);
            self.distributionInfo({ totalResponses: 0, projectName: '' });
            return;
        }

        self.isLoadingDistribution(true);

        customerApiFetch('/api/customer/portal/reports/enneagram-distribution/' + projectId)
            .then(function(r) { return r.json(); })
            .then(function(data) {
                self.distribution(data.distribution || []);
                self.distributionInfo({
                    totalResponses: data.totalResponses || 0,
                    projectName: data.projectName || ''
                });
            })
            .catch(function(error) {
                console.error('Error loading Enneagram distribution:', error);
                self.distribution([]);
                self.distributionInfo({ totalResponses: 0, projectName: '' });
            })
            .finally(function() {
                self.isLoadingDistribution(false);
            });
    };

    // Show result detail
    self.showDetail = function(result) {
        self.detail(null);
        self.isLoadingDetail(true);
        self.detailModal.show();

        customerApiFetch('/api/customer/portal/reports/enneagram-results/' + result.evaluationId)
            .then(function(r) { return r.json(); })
            .then(function(data) {
                self.detail(data);
            })
            .catch(function(error) {
                console.error('Error loading Enneagram result detail:', error);
                toastr.error('Sonuc detayi yuklenirken hata olustu.');
            })
            .finally(function() {
                self.isLoadingDetail(false);
            });
    };

    // Export results to Excel (uses admin API with customer filter)
    self.exportResults = function() {
        var params = new URLSearchParams();

        if (self.filter.projectId()) {
            params.append('projectIds', self.filter.projectId());
        }
        if (self.filter.searchTerm()) {
            params.append('searchTerm', self.filter.searchTerm());
        }

        var queryString = params.toString();
        var url = '/api/reports/enneagram-results/export' + (queryString ? '?' + queryString : '');
        window.location.href = url;
    };

    // Initialize
    self.init = function() {
        self.initModal();
        self.loadData();
    };

    self.init();
}

// Apply bindings
$(document).ready(function() {
    var app = document.getElementById('enneagram-results-app');
    if (app) {
        ko.applyBindings(new CustomerEnneagramResultsViewModel(), app);
    }
});
