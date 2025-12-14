function CustomerBranchesViewModel() {
    var self = this;

    // State
    self.isLoading = ko.observable(true);
    self.branches = ko.observableArray([]);

    // Helper
    self.getScoreClass = function(score) {
        if (score >= 80) return 'text-success';
        if (score >= 60) return 'text-warning';
        if (score > 0) return 'text-danger';
        return 'text-muted';
    };

    // Load branches
    self.loadBranches = function() {
        self.isLoading(true);

        customerApiFetch('/api/customer/portal/branches')
            .then(function(response) {
                if (!response.ok) {
                    throw new Error('Şubeler yüklenemedi');
                }
                return response.json();
            })
            .then(function(data) {
                self.branches(data || []);
                self.isLoading(false);
            })
            .catch(function(error) {
                console.error('Branches load error:', error);
                self.isLoading(false);
            });
    };

    // Initialize
    self.loadBranches();
}

// Apply bindings when DOM is ready
$(document).ready(function() {
    ko.applyBindings(new CustomerBranchesViewModel(), document.getElementById('customer-branches-app'));
});
