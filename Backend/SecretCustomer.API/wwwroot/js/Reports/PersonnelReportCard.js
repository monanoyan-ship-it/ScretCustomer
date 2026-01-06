// Personnel Report Card (Temsilci Karnesi) JavaScript
function PersonnelReportCardViewModel() {
    var self = this;

    // Observables
    self.isLoading = ko.observable(false);
    self.isExporting = ko.observable(false);
    self.errorMessage = ko.observable('');
    self.personnelList = ko.observableArray([]);
    self.selectedPersonnelId = ko.observable('');
    self.report = ko.observable(null);

    // Filter
    self.filter = {
        startDate: ko.observable(''),
        endDate: ko.observable('')
    };

    // Load personnel list
    self.loadPersonnelList = function() {
        fetch('/api/reports/personnel-list', { credentials: 'include' })
            .then(function(response) {
                if (!response.ok) throw new Error('Personel listesi yuklenemedi');
                return response.json();
            })
            .then(function(data) {
                self.personnelList(data);

                // Pre-select if initial personnel ID is provided
                if (typeof initialPersonnelId !== 'undefined' && initialPersonnelId) {
                    self.selectedPersonnelId(initialPersonnelId);
                    self.loadReport();
                }
            })
            .catch(function(error) {
                console.error('Error loading personnel list:', error);
                toastr.error('Personel listesi yuklenirken bir hata olustu.');
            });
    };

    // Load report
    self.loadReport = function() {
        if (!self.selectedPersonnelId()) {
            toastr.error('Lutfen bir temsilci secin.');
            return;
        }

        self.isLoading(true);
        self.errorMessage('');
        self.report(null);

        var url = '/api/reports/personnel-report-card/' + self.selectedPersonnelId();
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

        fetch(url, { credentials: 'include' })
            .then(function(response) {
                if (!response.ok) {
                    return response.json().then(function(err) {
                        throw new Error(err.message || 'Karne yuklenemedi');
                    });
                }
                return response.json();
            })
            .then(function(data) {
                self.report(data);
            })
            .catch(function(error) {
                console.error('Error loading report:', error);
                toastr.error(error.message || 'Karne yuklenirken bir hata olustu.');
            })
            .finally(function() {
                self.isLoading(false);
            });
    };

    // Clear filters
    self.clearFilters = function() {
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

        var url = '/api/reports/personnel-report-card/' + self.selectedPersonnelId() + '/export';
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

        fetch(url, { credentials: 'include' })
            .then(function(response) {
                if (!response.ok) throw new Error('Excel dosyasi olusturulamadi');
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
    self.loadPersonnelList();
}

// Apply bindings when DOM is ready
document.addEventListener('DOMContentLoaded', function() {
    var element = document.getElementById('personnel-report-card-app');
    if (element) {
        ko.applyBindings(new PersonnelReportCardViewModel(), element);
    }
});
