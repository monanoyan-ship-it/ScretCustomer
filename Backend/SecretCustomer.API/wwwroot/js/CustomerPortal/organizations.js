function CustomerOrganizationsViewModel() {
    var self = this;

    self.isLoading = ko.observable(true);
    self.groups = ko.observableArray([]);

    self.getScoreClass = function(score) {
        if (score >= 80) return 'text-success';
        if (score >= 60) return 'text-warning';
        if (score > 0) return 'text-danger';
        return 'text-muted';
    };

    self.loadOrganizations = function() {
        self.isLoading(true);

        customerApiFetch('/api/customer/portal/organizations')
            .then(function(response) {
                if (!response.ok) throw new Error('Organizasyonlar yuklenemedi');
                return response.json();
            })
            .then(function(data) {
                self.groups(data || []);
                self.isLoading(false);
            })
            .catch(function(error) {
                console.error('Organizations load error:', error);
                self.isLoading(false);
            });
    };

    self.loadOrganizations();
}

$(document).ready(function() {
    ko.applyBindings(new CustomerOrganizationsViewModel(), document.getElementById('customer-organizations-app'));
});
