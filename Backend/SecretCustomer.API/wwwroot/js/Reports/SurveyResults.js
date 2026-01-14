// Survey Results Report ViewModel
function SurveyResultsViewModel() {
    var self = this;

    // State
    self.isLoading = ko.observable(false);
    self.isExporting = ko.observable(false);

    // Filters
    self.surveyProjects = ko.observableArray([]);
    self.selectedProjectId = ko.observable(null);
    self.startDate = ko.observable('');
    self.endDate = ko.observable('');

    // Results
    self.results = ko.observable(null);

    // Load survey projects (OnlineSurvey type only)
    self.loadProjects = function() {
        apiService.get('/projects?projectTypeId=3') // OnlineSurvey = 3
            .then(function(projects) {
                self.surveyProjects(projects || []);
            })
            .catch(function(error) {
                console.error('Error loading projects:', error);
                toastr.error('Projeler yüklenirken hata oluştu.');
            });
    };

    // Load results for selected project
    self.loadResults = function() {
        var projectId = self.selectedProjectId();
        if (!projectId) {
            toastr.warning('Lütfen bir proje seçin.');
            return;
        }

        self.isLoading(true);
        self.results(null);

        var params = [];
        if (self.startDate()) params.push('startDate=' + self.startDate());
        if (self.endDate()) params.push('endDate=' + self.endDate());
        var queryString = params.length > 0 ? '?' + params.join('&') : '';

        apiService.get('/reports/survey-results/' + projectId + queryString)
            .then(function(data) {
                self.results(data);
            })
            .catch(function(error) {
                console.error('Error loading results:', error);
                toastr.error('Sonuçlar yüklenirken hata oluştu.');
            })
            .finally(function() {
                self.isLoading(false);
            });
    };

    // Clear filters
    self.clearFilters = function() {
        self.selectedProjectId(null);
        self.startDate('');
        self.endDate('');
        self.results(null);
    };

    // Export to Excel
    self.exportToExcel = function() {
        var projectId = self.selectedProjectId();
        if (!projectId) {
            toastr.warning('Lütfen bir proje seçin.');
            return;
        }

        self.isExporting(true);

        var params = [];
        if (self.startDate()) params.push('startDate=' + self.startDate());
        if (self.endDate()) params.push('endDate=' + self.endDate());
        var queryString = params.length > 0 ? '?' + params.join('&') : '';

        // Download file
        window.location.href = '/api/reports/survey-results/' + projectId + '/export' + queryString;

        setTimeout(function() {
            self.isExporting(false);
        }, 2000);
    };

    // Initialize
    self.init = function() {
        self.loadProjects();
    };

    self.init();
}

// Apply bindings
$(document).ready(function() {
    var app = document.getElementById('survey-results-app');
    if (app) {
        ko.applyBindings(new SurveyResultsViewModel(), app);
    }
});
