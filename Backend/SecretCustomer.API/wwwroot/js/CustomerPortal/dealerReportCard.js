// CustomerPortal Dealer Report Card (Şube Karnesi) ViewModel
function CustomerDealerReportCardViewModel() {
    var self = this;

    // State
    self.isLoading = ko.observable(false);
    self.isLoadingDealers = ko.observable(false);
    self.isExporting = ko.observable(false);
    self.report = ko.observable(null);

    // Details modal
    self.isDetailsModalOpen = ko.observable(false);
    self.isDetailsLoading = ko.observable(false);
    self.detailsData = ko.observable(null);
    self.isExportingDetail = ko.observable(false);

    // Dealer selection
    self.dealerList = ko.observableArray([]);
    self.projects = ko.observableArray([]);

    self.selectedDealerId = ko.observable('');
    self.searchText = ko.observable('');

    // Selected dealer name
    self.selectedDealerName = ko.computed(function() {
        var dealerId = self.selectedDealerId();
        if (!dealerId) return '';
        var dealer = self.dealerList().find(function(d) { return d.id == dealerId; });
        return dealer ? dealer.name : '';
    });

    // Filtered dealer list (search)
    self.filteredDealerList = ko.computed(function() {
        var search = (self.searchText() || '').toLowerCase().trim();
        var list = self.dealerList();

        if (search) {
            list = list.filter(function(d) {
                return (d.name || '').toLowerCase().indexOf(search) > -1 ||
                       (d.code || '').toLowerCase().indexOf(search) > -1 ||
                       (d.city || '').toLowerCase().indexOf(search) > -1;
            });
        }

        return list;
    });

    // Filter system
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
                var dow = today.getDay();
                var diff = today.getDate() - dow + (dow === 0 ? -6 : 1);
                start = new Date(today.getFullYear(), today.getMonth(), diff);
                end = today;
                break;
            case 'lastWeek':
                var dow = today.getDay();
                var diff = today.getDate() - dow + (dow === 0 ? -6 : 1);
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
                var project = self.projects().find(function(p) { return p.id == projectId; });
                if (!project) return;
                if (self.activeFilters().some(function(f) { return f.type === 'project' && f.value == projectId; })) {
                    toastr.warning('Bu proje zaten eklenmiş.');
                    self.tempFilter.projectId(null);
                    return;
                }
                filter.value = projectId;
                filter.displayValue = project.name;
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
        self.loadReport();
    };

    self.removeFilter = function(filter) {
        self.activeFilters.remove(filter);
        self.loadReport();
    };

    self.clearAllFilters = function() {
        self.activeFilters.removeAll();
        self.loadReport();
    };

    self.clearSearch = function() {
        self.searchText('');
    };

    // Build query params
    self.buildQueryParams = function() {
        var params = [];
        var projectIds = [];
        var dateRangeIndex = 0;

        self.activeFilters().forEach(function(f) {
            if (f.type === 'project') {
                projectIds.push(f.value);
            } else if (f.type === 'dateRange') {
                if (f.value.startDate) params.push('dateRanges[' + dateRangeIndex + '].startDate=' + f.value.startDate);
                if (f.value.endDate) params.push('dateRanges[' + dateRangeIndex + '].endDate=' + f.value.endDate);
                dateRangeIndex++;
            }
        });

        projectIds.forEach(function(id) { params.push('projectIds=' + id); });
        return params.length > 0 ? '?' + params.join('&') : '';
    };

    // Load dealer list
    self.loadDealerList = function() {
        self.isLoadingDealers(true);
        customerApiFetch('/api/customer/portal/reports/dealer-list')
            .then(function(response) {
                if (!response.ok) throw new Error('Şube listesi yüklenemedi');
                return response.json();
            })
            .then(function(data) {
                self.dealerList(data || []);
            })
            .catch(function(error) {
                console.error('Error loading dealer list:', error);
            })
            .finally(function() {
                self.isLoadingDealers(false);
            });
    };

    // Load projects
    self.loadProjects = function() {
        customerApiFetch('/api/customer/portal/projects')
            .then(function(response) {
                if (!response.ok) throw new Error('Proje listesi yüklenemedi');
                return response.json();
            })
            .then(function(data) {
                self.projects(data || []);
            })
            .catch(function(error) {
                console.error('Error loading projects:', error);
            });
    };

    // Select dealer from list
    self.selectDealer = function(dealer) {
        self.selectedDealerId(dealer.id);
        self.activeFilters.removeAll();
        self.loadReport();
    };

    // Load report
    self.loadReport = function() {
        if (!self.selectedDealerId()) return;

        self.isLoading(true);
        self.report(null);

        var url = '/api/customer/portal/reports/dealer-report-card/' + self.selectedDealerId() + self.buildQueryParams();

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

    // Export to Excel
    self.exportToExcel = function() {
        if (!self.selectedDealerId()) return;

        self.isExporting(true);
        var url = '/api/customer/portal/reports/dealer-report-card/' + self.selectedDealerId() + '/export' + self.buildQueryParams();

        customerApiFetch(url)
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

    // Export to Word
    self.exportToWord = function() {
        if (!self.selectedDealerId()) return;

        self.isExporting(true);
        var url = '/api/customer/portal/reports/dealer-report-card/' + self.selectedDealerId() + '/export-word' + self.buildQueryParams();

        customerApiFetch(url)
            .then(function(response) {
                if (!response.ok) throw new Error('Word dosyası oluşturulamadı');
                return response.blob();
            })
            .then(function(blob) {
                var a = document.createElement('a');
                var objectUrl = URL.createObjectURL(blob);
                a.href = objectUrl;
                a.download = 'Sube_Karne_' + (self.report() ? self.report().dealerName.replace(/ /g, '_') : 'rapor') + '.docx';
                document.body.appendChild(a);
                a.click();
                document.body.removeChild(a);
                URL.revokeObjectURL(objectUrl);
                toastr.success('Word karnesi indirildi.');
            })
            .catch(function(error) {
                console.error('Export error:', error);
                toastr.error('Word dosyası oluşturulurken bir hata oluştu.');
            })
            .finally(function() {
                self.isExporting(false);
            });
    };

    // Details modal
    self.showDetails = function(evaluationId) {
        self.isDetailsModalOpen(true);
        self.isDetailsLoading(true);
        self.detailsData(null);

        customerApiFetch('/api/customer/portal/evaluations/' + evaluationId)
            .then(function(response) {
                if (!response.ok) throw new Error('Detay yüklenemedi');
                return response.json();
            })
            .then(function(data) {
                self.detailsData(data);
            })
            .catch(function(error) {
                console.error('Details load error:', error);
                toastr.error('Detay yüklenirken bir hata oluştu.');
                self.closeDetailsModal();
            })
            .finally(function() {
                self.isDetailsLoading(false);
            });
    };

    self.closeDetailsModal = function() {
        self.isDetailsModalOpen(false);
        self.detailsData(null);
    };

    self.exportDetailToExcel = function() {
        var data = self.detailsData();
        if (!data) return;

        self.isExportingDetail(true);
        var filename = 'Degerlendirme_Detay_' + new Date().toISOString().slice(0,10) + '.xlsx';

        customerApiDownloadGet('/api/customer/portal/evaluations/' + data.id + '/export', filename)
            .then(function() { toastr.success('Excel dosyası indirildi'); })
            .catch(function(error) { console.error('Error exporting:', error); toastr.error('Excel oluşturulurken hata oluştu'); })
            .finally(function() { self.isExportingDetail(false); });
    };

    self.getScoreClass = function(score) {
        if (score >= 80) return 'text-success';
        if (score >= 60) return 'text-warning';
        if (score > 0) return 'text-danger';
        return 'text-muted';
    };

    self.getProgressBarClass = function(score) {
        if (score >= 80) return 'bg-success';
        if (score >= 60) return 'bg-warning';
        if (score > 0) return 'bg-danger';
        return 'bg-secondary';
    };

    // Print report
    self.printReport = function() {
        var leftPanel = document.querySelector('.col-md-3');
        var filterCard = document.querySelector('.card.shadow-sm.mb-3');

        if (leftPanel) leftPanel.style.display = 'none';
        if (filterCard) filterCard.style.display = 'none';

        window.print();

        if (leftPanel) leftPanel.style.display = '';
        if (filterCard) filterCard.style.display = '';
    };

    // Initialize
    self.loadDealerList();
    self.loadProjects();
}

// Apply bindings
$(document).ready(function() {
    ko.applyBindings(new CustomerDealerReportCardViewModel(), document.getElementById('dealer-report-card-app'));
});
