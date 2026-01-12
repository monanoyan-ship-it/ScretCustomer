function CustomerSupervisorsViewModel() {
    var self = this;

    self.isLoading = ko.observable(true);
    self.groups = ko.observableArray([]);

    self.getScoreClass = function(score) {
        if (score >= 80) return 'text-success';
        if (score >= 60) return 'text-warning';
        if (score > 0) return 'text-danger';
        return 'text-muted';
    };

    self.loadSupervisors = function() {
        self.isLoading(true);

        customerApiFetch('/api/customer/portal/supervisors')
            .then(function(response) {
                if (!response.ok) throw new Error('Supervizorler yuklenemedi');
                return response.json();
            })
            .then(function(data) {
                self.groups(data || []);
                self.isLoading(false);
            })
            .catch(function(error) {
                console.error('Supervisors load error:', error);
                self.isLoading(false);
            });
    };

    self.loadSupervisors();
}

$(document).ready(function() {
    ko.applyBindings(new CustomerSupervisorsViewModel(), document.getElementById('customer-supervisors-app'));
});
