function CustomerProjectsViewModel() {
    var self = this;

    // State
    self.isLoading = ko.observable(true);
    self.projects = ko.observableArray([]);

    // Helper
    self.getScoreClass = function(score, projectTypeId) {
        return ScoreThresholds.getScoreClass(score, projectTypeId);
    };

    // Load projects
    self.loadProjects = function() {
        self.isLoading(true);

        customerApiFetch('/api/customer/portal/projects')
            .then(function(response) {
                if (!response.ok) {
                    throw new Error('Projeler yüklenemedi');
                }
                return response.json();
            })
            .then(function(data) {
                self.projects(data || []);
                self.isLoading(false);
            })
            .catch(function(error) {
                console.error('Projects load error:', error);
                self.isLoading(false);
            });
    };

    // Initialize
    self.loadProjects();
}

// Apply bindings when DOM is ready
$(document).ready(function() {
    ko.applyBindings(new CustomerProjectsViewModel(), document.getElementById('customer-projects-app'));
});
