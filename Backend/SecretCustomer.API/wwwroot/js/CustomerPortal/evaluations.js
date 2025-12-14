function CustomerEvaluationsViewModel() {
    var self = this;

    // State
    self.isLoading = ko.observable(true);
    self.evaluations = ko.observableArray([]);
    self.currentPage = ko.observable(1);
    self.pageSize = ko.observable(20);
    self.totalCount = ko.observable(0);
    self.totalPages = ko.observable(0);

    // Computed: page numbers for pagination
    self.pageNumbers = ko.computed(function() {
        var pages = [];
        var total = self.totalPages();
        var current = self.currentPage();

        var start = Math.max(1, current - 2);
        var end = Math.min(total, current + 2);

        for (var i = start; i <= end; i++) {
            pages.push(i);
        }
        return pages;
    });

    // Helpers
    self.formatDate = function(dateString) {
        if (!dateString) return '-';
        var date = new Date(dateString);
        return date.toLocaleDateString('tr-TR', {
            day: '2-digit',
            month: '2-digit',
            year: 'numeric',
            hour: '2-digit',
            minute: '2-digit'
        });
    };

    self.getScoreBadgeClass = function(score) {
        if (score >= 80) return 'bg-success';
        if (score >= 60) return 'bg-warning text-dark';
        if (score > 0) return 'bg-danger';
        return 'bg-secondary';
    };

    self.getStatusBadgeClass = function(status) {
        switch (status) {
            case 'Completed': return 'bg-success';
            case 'InProgress': return 'bg-primary';
            case 'Pending': return 'bg-warning text-dark';
            case 'Draft': return 'bg-secondary';
            case 'Cancelled': return 'bg-danger';
            default: return 'bg-secondary';
        }
    };

    // Load evaluations
    self.loadEvaluations = function() {
        self.isLoading(true);

        var url = '/api/customer/portal/evaluations?page=' + self.currentPage() + '&pageSize=' + self.pageSize();

        customerApiFetch(url)
            .then(function(response) {
                if (!response.ok) {
                    throw new Error('Değerlendirmeler yüklenemedi');
                }
                return response.json();
            })
            .then(function(data) {
                self.evaluations(data.items || []);
                self.totalCount(data.totalCount || 0);
                self.totalPages(data.totalPages || 0);
                self.currentPage(data.page || 1);
                self.isLoading(false);
            })
            .catch(function(error) {
                console.error('Evaluations load error:', error);
                self.isLoading(false);
            });
    };

    // Pagination
    self.goToPage = function(page) {
        if (page >= 1 && page <= self.totalPages()) {
            self.currentPage(page);
            self.loadEvaluations();
        }
    };

    // Initialize
    self.loadEvaluations();
}

// Apply bindings when DOM is ready
$(document).ready(function() {
    ko.applyBindings(new CustomerEvaluationsViewModel(), document.getElementById('customer-evaluations-app'));
});
