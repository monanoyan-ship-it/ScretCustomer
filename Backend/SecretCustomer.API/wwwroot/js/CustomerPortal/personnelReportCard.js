// CustomerPortal Personnel Report Card (Temsilci Karnesi) ViewModel
function CustomerPersonnelReportCardViewModel() {
    var self = this;

    // Observables
    self.isLoading = ko.observable(false);
    self.isExporting = ko.observable(false);
    self.errorMessage = ko.observable('');
    self.report = ko.observable(null);

    // Selection data (Müşteri dropdown'u YOK - otomatik)
    self.organizations = ko.observableArray([]);
    self.personnelList = ko.observableArray([]);

    // Selection state
    self.selectedOrganizationId = ko.observable('');
    self.selectedPersonnelId = ko.observable('');

    // Filter
    self.filter = {
        startDate: ko.observable(''),
        endDate: ko.observable('')
    };

    // Computed: Filtered personnel based on organization selection
    self.filteredPersonnelList = ko.computed(function() {
        var organizationId = self.selectedOrganizationId();
        var list = self.personnelList();

        if (organizationId) {
            list = list.filter(function(p) {
                return p.organizationId == organizationId;
            });
        }

        return list;
    });

    // Load organizations (müşteriye ait)
    self.loadOrganizations = function() {
        customerApiFetch('/api/customer/portal/organizations')
            .then(function(response) {
                if (!response.ok) throw new Error('Organizasyon listesi yüklenemedi');
                return response.json();
            })
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

    // Load personnel list (müşteriye ait)
    self.loadPersonnelList = function(organizationId) {
        var url = '/api/customer/portal/reports/personnel-list';

        if (organizationId) {
            url += '?organizationId=' + organizationId;
        }

        customerApiFetch(url)
            .then(function(response) {
                if (!response.ok) throw new Error('Personel listesi yüklenemedi');
                return response.json();
            })
            .then(function(data) {
                self.personnelList(data || []);
            })
            .catch(function(error) {
                console.error('Error loading personnel list:', error);
                toastr.error('Personel listesi yüklenirken bir hata oluştu.');
            });
    };

    // Organization change handler
    self.onOrganizationChange = function() {
        // Reset personnel selection
        self.selectedPersonnelId('');
        self.report(null);
    };

    // Load report
    self.loadReport = function() {
        if (!self.selectedPersonnelId()) {
            toastr.error('Lütfen bir temsilci seçin.');
            return;
        }

        self.isLoading(true);
        self.errorMessage('');
        self.report(null);

        var url = '/api/customer/portal/reports/personnel-report-card/' + self.selectedPersonnelId();
        var params = [];

        if (self.filter.startDate()) {
            params.push('startDate=' + self.filter.startDate());
        }
        if (self.filter.endDate()) {
            params.push('endDate=' + self.filter.endDate());
        }

        if (params.length > 0) {
            url += '?' + params.join('&');
        }

        customerApiFetch(url)
            .then(function(response) {
                if (!response.ok) {
                    return response.json().then(function(err) {
                        throw new Error(err.message || 'Karne yüklenemedi');
                    });
                }
                return response.json();
            })
            .then(function(data) {
                self.report(data);
            })
            .catch(function(error) {
                console.error('Error loading report:', error);
                toastr.error(error.message || 'Karne yüklenirken bir hata oluştu.');
            })
            .finally(function() {
                self.isLoading(false);
            });
    };

    // Clear filters
    self.clearFilters = function() {
        self.selectedOrganizationId('');
        self.selectedPersonnelId('');
        self.filter.startDate('');
        self.filter.endDate('');
        self.report(null);
        self.errorMessage('');
    };

    // Export to Excel
    self.exportToExcel = function() {
        if (!self.selectedPersonnelId()) return;

        self.isExporting(true);

        var url = '/api/customer/portal/reports/personnel-report-card/' + self.selectedPersonnelId() + '/export';
        var params = [];

        if (self.filter.startDate()) {
            params.push('startDate=' + self.filter.startDate());
        }
        if (self.filter.endDate()) {
            params.push('endDate=' + self.filter.endDate());
        }

        if (params.length > 0) {
            url += '?' + params.join('&');
        }

        customerApiFetch(url)
            .then(function(response) {
                if (!response.ok) throw new Error('Excel dosyası oluşturulamadı');
                return response.blob();
            })
            .then(function(blob) {
                var a = document.createElement('a');
                var objectUrl = URL.createObjectURL(blob);
                a.href = objectUrl;
                a.download = 'TemsilciKarnesi_' + (self.report() ? self.report().personnelName.replace(/ /g, '_') : 'rapor') + '.xlsx';
                document.body.appendChild(a);
                a.click();
                document.body.removeChild(a);
                URL.revokeObjectURL(objectUrl);
            })
            .catch(function(error) {
                console.error('Export error:', error);
                toastr.error('Excel dosyası oluşturulurken bir hata oluştu.');
            })
            .finally(function() {
                self.isExporting(false);
            });
    };

    // Print report as PDF
    self.printReport = function() {
        // Hide non-printable elements
        var header = document.querySelector('.d-flex.justify-content-between.align-items-center.mb-4');
        var selection = document.querySelector('.card.shadow-sm.mb-4');

        if (header) header.style.display = 'none';
        if (selection) selection.style.display = 'none';

        // Print
        window.print();

        // Restore elements
        if (header) header.style.display = '';
        if (selection) selection.style.display = '';
    };

    // Initialize
    self.loadOrganizations();
    self.loadPersonnelList();
}

// Apply bindings when DOM is ready
$(document).ready(function() {
    ko.applyBindings(new CustomerPersonnelReportCardViewModel(), document.getElementById('personnel-report-card-app'));
});
