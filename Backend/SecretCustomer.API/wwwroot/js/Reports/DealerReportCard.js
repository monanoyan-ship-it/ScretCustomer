// Dealer Report Card (Şube Karnesi) JavaScript
function DealerReportCardViewModel() {
    var self = this;

    // State
    self.isLoading = ko.observable(false);
    self.isExporting = ko.observable(false);
    self.errorMessage = ko.observable('');
    self.report = ko.observable(null);
    self.selectedDetail = ko.observable(null);

    // Hiyerarşik seçim
    self.customers = ko.observableArray([]);
    self.dealerList = ko.observableArray([]);

    self.selectedCustomerId = ko.observable('');
    self.selectedDealerId = ko.observable('');

    // Filtre sistemi
    self.projects = ko.observableArray([]);
    self.activeFilters = ko.observableArray([]);
    self.selectedFilterType = ko.observable('');

    self.tempFilter = {
        projectId: ko.observable(null),
        startDate: ko.observable(''),
        endDate: ko.observable(''),
        dateRangeType: ko.observable('')
    };

    self.filterLabels = {
        project: 'Proje',
        dateRange: 'Tarih'
    };

    self.dateRanges = ko.observableArray([
        { systemName: 'today', name: 'Bugün' },
        { systemName: 'yesterday', name: 'Dün' },
        { systemName: 'thisWeek', name: 'Bu Hafta' },
        { systemName: 'lastWeek', name: 'Geçen Hafta' },
        { systemName: 'thisMonth', name: 'Bu Ay' },
        { systemName: 'lastMonth', name: 'Geçen Ay' },
        { systemName: 'last3Months', name: 'Son 3 Ay' },
        { systemName: 'last6Months', name: 'Son 6 Ay' },
        { systemName: 'thisYear', name: 'Bu Yıl' },
        { systemName: 'lastYear', name: 'Geçen Yıl' }
    ]);

    self.canAddFilter = ko.computed(function() {
        var type = self.selectedFilterType();
        if (!type) return false;
        switch (type) {
            case 'project': return self.tempFilter.projectId();
            case 'dateRange': return self.tempFilter.startDate() || self.tempFilter.endDate();
            default: return false;
        }
    });

    self.calculateDateRange = function(rangeType) {
        var today = new Date();
        var start, end;
        var formatDate = function(date) { return date.toISOString().split('T')[0]; };

        switch (rangeType) {
            case 'today': start = end = today; break;
            case 'yesterday': start = end = new Date(today.getFullYear(), today.getMonth(), today.getDate() - 1); break;
            case 'thisWeek':
                var dayOfWeek = today.getDay();
                var diff = today.getDate() - dayOfWeek + (dayOfWeek === 0 ? -6 : 1);
                start = new Date(today.getFullYear(), today.getMonth(), diff);
                end = today;
                break;
            case 'lastWeek':
                var dayOfWeek = today.getDay();
                var diff = today.getDate() - dayOfWeek + (dayOfWeek === 0 ? -6 : 1);
                start = new Date(today.getFullYear(), today.getMonth(), diff - 7);
                end = new Date(today.getFullYear(), today.getMonth(), diff - 1);
                break;
            case 'thisMonth': start = new Date(today.getFullYear(), today.getMonth(), 1); end = today; break;
            case 'lastMonth': start = new Date(today.getFullYear(), today.getMonth() - 1, 1); end = new Date(today.getFullYear(), today.getMonth(), 0); break;
            case 'last3Months': start = new Date(today.getFullYear(), today.getMonth() - 2, 1); end = today; break;
            case 'last6Months': start = new Date(today.getFullYear(), today.getMonth() - 5, 1); end = today; break;
            case 'thisYear': start = new Date(today.getFullYear(), 0, 1); end = today; break;
            case 'lastYear': start = new Date(today.getFullYear() - 1, 0, 1); end = new Date(today.getFullYear() - 1, 11, 31); break;
            default: return null;
        }
        return { start: formatDate(start), end: formatDate(end) };
    };

    self.setTempDateRange = function(rangeType) {
        var range = self.calculateDateRange(rangeType);
        if (range) {
            self.tempFilter.startDate(range.start);
            self.tempFilter.endDate(range.end);
            self.tempFilter.dateRangeType(rangeType);
        }
    };

    self.addFilter = function() {
        var type = self.selectedFilterType();
        if (!type) return;

        var filter = { type: type, label: self.filterLabels[type], value: null, displayValue: '' };

        switch (type) {
            case 'project':
                var projectId = self.tempFilter.projectId();
                var project = self.projects().find(function(p) { return p.id === projectId; });
                if (!project) return;
                if (self.activeFilters().some(function(f) { return f.type === 'project' && f.value === projectId; })) {
                    toastr.warning('Bu proje zaten filtrelere eklenmiş.');
                    self.tempFilter.projectId(null);
                    return;
                }
                filter.value = projectId;
                filter.displayValue = project.code ? project.code + ' - ' + project.name : project.name;
                self.tempFilter.projectId(null);
                break;

            case 'dateRange':
                var startDate = self.tempFilter.startDate();
                var endDate = self.tempFilter.endDate();
                var dateRangeType = self.tempFilter.dateRangeType();
                if (!startDate && !endDate) return;
                filter.value = { startDate: startDate, endDate: endDate, dateRangeType: dateRangeType };
                if (dateRangeType) {
                    var rangeInfo = self.dateRanges().find(function(r) { return r.systemName === dateRangeType; });
                    filter.displayValue = rangeInfo ? rangeInfo.name : dateRangeType;
                } else {
                    filter.displayValue = (startDate || '...') + ' - ' + (endDate || '...');
                }
                self.tempFilter.startDate('');
                self.tempFilter.endDate('');
                self.tempFilter.dateRangeType('');
                break;

            default: return;
        }

        self.activeFilters.push(filter);
        self.selectedFilterType('');
    };

    self.removeFilter = function(filter) {
        self.activeFilters.remove(filter);
    };

    self.clearAllFilters = function() {
        self.activeFilters([]);
    };

    // Filtered dealers based on customer selection
    self.filteredDealerList = ko.computed(function() {
        var customerId = self.selectedCustomerId();
        if (!customerId) return self.dealerList();
        return self.dealerList().filter(function(d) {
            return d.customerId == customerId;
        });
    });

    // Load customers (bayisi olan müşteriler)
    self.loadCustomers = function() {
        fetch('/api/reports/dealer-report-card/customers', { credentials: 'include' })
            .then(function(response) {
                if (!response.ok) throw new Error('Müşteri listesi yüklenemedi');
                return response.json();
            })
            .then(function(data) {
                self.customers(data || []);
            })
            .catch(function(error) {
                console.error('Error loading customers:', error);
            });
    };

    // Load dealer list
    self.loadDealerList = function(customerId) {
        var url = '/api/reports/dealer-list';
        if (customerId) {
            url += '?customerIds=' + customerId;
        }

        fetch(url, { credentials: 'include' })
            .then(function(response) {
                if (!response.ok) throw new Error('Şube listesi yüklenemedi');
                return response.json();
            })
            .then(function(data) {
                self.dealerList(data || []);
            })
            .catch(function(error) {
                console.error('Error loading dealer list:', error);
                toastr.error('Şube listesi yüklenirken bir hata oluştu.');
            });
    };

    // Load projects for selected dealer
    self.loadProjectsForDealer = function(dealerId) {
        if (!dealerId) {
            self.projects([]);
            return;
        }

        fetch('/api/reports/dealer-projects/' + dealerId, { credentials: 'include' })
            .then(function(response) {
                if (!response.ok) throw new Error('Proje listesi yüklenemedi');
                return response.json();
            })
            .then(function(data) {
                self.projects(data || []);
            })
            .catch(function(error) {
                console.error('Error loading projects for dealer:', error);
                self.projects([]);
            });
    };

    // Customer change handler
    self.onCustomerChange = function() {
        self.selectedDealerId('');
        self.projects([]);
        self.activeFilters([]);
        self.report(null);
        var customerId = self.selectedCustomerId();
        self.loadDealerList(customerId || null);
    };

    // Dealer change handler
    self.onDealerChange = function() {
        var dealerId = self.selectedDealerId();
        self.activeFilters([]);
        self.report(null);
        self.loadProjectsForDealer(dealerId);
    };

    // Build filter params
    self.buildFilterParams = function() {
        var params = new URLSearchParams();

        self.activeFilters().forEach(function(filter) {
            switch (filter.type) {
                case 'project':
                    params.append('projectIds', filter.value);
                    break;
                case 'dateRange':
                    if (filter.value.startDate) params.append('startDate', filter.value.startDate);
                    if (filter.value.endDate) params.append('endDate', filter.value.endDate);
                    break;
            }
        });

        return params.toString();
    };

    // Load report
    self.loadReport = function() {
        if (!self.selectedDealerId()) {
            toastr.error('Lütfen bir şube seçin.');
            return;
        }

        self.isLoading(true);
        self.errorMessage('');
        self.report(null);

        var url = '/api/reports/dealer-report-card/' + self.selectedDealerId();
        var params = self.buildFilterParams();
        if (params) url += '?' + params;

        fetch(url, { credentials: 'include' })
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
        self.selectedCustomerId('');
        self.selectedDealerId('');
        self.activeFilters([]);
        self.projects([]);
        self.report(null);
        self.errorMessage('');
    };

    // Export to Excel
    self.exportToExcel = function() {
        if (!self.selectedDealerId()) return;

        self.isExporting(true);
        var url = '/api/reports/dealer-report-card/' + self.selectedDealerId() + '/export';
        var params = self.buildFilterParams();
        if (params) url += '?' + params;

        fetch(url, { credentials: 'include' })
            .then(function(response) {
                if (!response.ok) throw new Error('Excel dosyası oluşturulamadı');
                return response.blob();
            })
            .then(function(blob) {
                var a = document.createElement('a');
                var objectUrl = URL.createObjectURL(blob);
                a.href = objectUrl;
                a.download = 'SubeKarnesi_' + (self.report() ? self.report().dealerName.replace(/ /g, '_') : 'rapor') + '.xlsx';
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

    // View evaluation detail
    self.viewDetail = function(evaluation) {
        fetch('/api/reports/evaluations/' + evaluation.evaluationId, { credentials: 'include' })
            .then(function(response) { return response.json(); })
            .then(function(data) {
                self.selectedDetail(data);
                var modal = new bootstrap.Modal(document.getElementById('detailModal'));
                modal.show();
            })
            .catch(function(error) {
                console.error('Error:', error);
                toastr.error('Detay yüklenirken bir hata oluştu.');
            });
    };

    // Export to PDF via PdfService
    self.exportToPdf = function() {
        if (!self.selectedDealerId()) return;

        self.isExporting(true);

        var url = '/api/reports/dealer-report-card/' + self.selectedDealerId() + '/export-pdf';
        var params = self.buildFilterParams();
        if (params) url += '?' + params;

        fetch(url, { credentials: 'include' })
            .then(function(response) {
                if (!response.ok) throw new Error('PDF olusturulamadi');
                return response.blob();
            })
            .then(function(blob) {
                var a = document.createElement('a');
                var objectUrl = URL.createObjectURL(blob);
                a.href = objectUrl;
                a.download = 'SubeKarnesi_' + (self.report() ? self.report().dealerName.replace(/ /g, '_') : 'rapor') + '.pdf';
                document.body.appendChild(a);
                a.click();
                URL.revokeObjectURL(objectUrl);
                a.remove();
            })
            .catch(function(error) {
                console.error('PDF export error:', error);
                toastr.error('PDF export basarisiz: ' + error.message);
            })
            .finally(function() {
                self.isExporting(false);
            });
    };

    // Initialize
    self.loadCustomers();
    self.loadDealerList();
}

// Apply bindings
document.addEventListener('DOMContentLoaded', function() {
    var element = document.getElementById('dealer-report-card-app');
    if (element) {
        ko.applyBindings(new DealerReportCardViewModel(), element);
    }
});
