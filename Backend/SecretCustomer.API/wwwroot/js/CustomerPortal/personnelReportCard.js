// CustomerPortal Personnel Report Card (Temsilci Karnesi) ViewModel
function CustomerPersonnelReportCardViewModel() {
    var self = this;

    // State
    self.isLoading = ko.observable(false);
    self.isExporting = ko.observable(false);
    self.errorMessage = ko.observable('');
    self.report = ko.observable(null);

    // Details modal
    self.isDetailsModalOpen = ko.observable(false);
    self.isDetailsLoading = ko.observable(false);
    self.detailsData = ko.observable(null);
    self.isExportingDetail = ko.observable(false);

    // ===== HİYERARŞİK SEÇİM (Personele ulaşmak için) =====
    self.organizations = ko.observableArray([]);
    self.personnelList = ko.observableArray([]);
    self.projects = ko.observableArray([]);

    self.selectedOrganizationId = ko.observable('');
    self.selectedPersonnelId = ko.observable('');

    // ===== FİLTRE SİSTEMİ (Chip-based pattern) =====
    self.activeFilters = ko.observableArray([]);
    self.selectedFilterType = ko.observable('');

    // Temp filter values
    self.tempFilter = {
        projectId: ko.observable(null),
        startDate: ko.observable(''),
        endDate: ko.observable(''),
        dateRangeType: ko.observable('')
    };

    // Filter labels
    self.filterLabels = {
        project: 'Proje',
        dateRange: 'Tarih'
    };

    // Date range options
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

    // Can add filter
    self.canAddFilter = ko.computed(function() {
        var type = self.selectedFilterType();
        if (!type) return false;

        switch (type) {
            case 'project': return self.tempFilter.projectId();
            case 'dateRange': return self.tempFilter.startDate() || self.tempFilter.endDate();
            default: return false;
        }
    });

    // Calculate date range from type
    self.calculateDateRange = function(rangeType) {
        var today = new Date();
        var start, end;

        var formatDate = function(date) {
            return date.toISOString().split('T')[0];
        };

        switch (rangeType) {
            case 'today':
                start = end = today;
                break;
            case 'yesterday':
                start = end = new Date(today.getFullYear(), today.getMonth(), today.getDate() - 1);
                break;
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
            case 'thisMonth':
                start = new Date(today.getFullYear(), today.getMonth(), 1);
                end = today;
                break;
            case 'lastMonth':
                start = new Date(today.getFullYear(), today.getMonth() - 1, 1);
                end = new Date(today.getFullYear(), today.getMonth(), 0);
                break;
            case 'last3Months':
                start = new Date(today.getFullYear(), today.getMonth() - 2, 1);
                end = today;
                break;
            case 'last6Months':
                start = new Date(today.getFullYear(), today.getMonth() - 5, 1);
                end = today;
                break;
            case 'thisYear':
                start = new Date(today.getFullYear(), 0, 1);
                end = today;
                break;
            case 'lastYear':
                start = new Date(today.getFullYear() - 1, 0, 1);
                end = new Date(today.getFullYear() - 1, 11, 31);
                break;
            default:
                return null;
        }

        return {
            start: formatDate(start),
            end: formatDate(end)
        };
    };

    // Set temp date range
    self.setTempDateRange = function(rangeType) {
        var range = self.calculateDateRange(rangeType);
        if (range) {
            self.tempFilter.startDate(range.start);
            self.tempFilter.endDate(range.end);
            self.tempFilter.dateRangeType(rangeType);
        }
    };

    // Add filter
    self.addFilter = function() {
        var type = self.selectedFilterType();
        if (!type) return;

        var filter = {
            type: type,
            label: self.filterLabels[type],
            value: null,
            displayValue: ''
        };

        switch (type) {
            case 'project':
                var projectId = self.tempFilter.projectId();
                var project = self.projects().find(function(p) { return p.id == projectId; });
                if (!project) return;

                // Aynı proje zaten eklenmişse ekleme
                var alreadyExists = self.activeFilters().some(function(f) {
                    return f.type === 'project' && f.value == projectId;
                });
                if (alreadyExists) {
                    toastr.warning('Bu proje zaten filtrelere eklenmiş.');
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

                filter.value = {
                    startDate: startDate,
                    endDate: endDate,
                    dateRangeType: dateRangeType
                };

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

            default:
                return;
        }

        self.activeFilters.push(filter);
        self.selectedFilterType('');
        self.loadReport(); // Filtre eklenince otomatik ara
    };

    // Remove filter
    self.removeFilter = function(filter) {
        self.activeFilters.remove(filter);
        self.loadReport(); // Filtre kaldırılınca otomatik ara
    };

    // Clear all filters
    self.clearAllFilters = function() {
        self.activeFilters.removeAll();
        self.loadReport();
    };

    // Build query params from active filters
    self.buildQueryParams = function() {
        var params = [];
        var projectIds = [];
        var dateRangeIndex = 0;

        self.activeFilters().forEach(function(f) {
            if (f.type === 'project') {
                projectIds.push(f.value);
            } else if (f.type === 'dateRange') {
                // DateRanges pattern - çoğul tarih aralığı desteği
                if (f.value.startDate) {
                    params.push('dateRanges[' + dateRangeIndex + '].startDate=' + f.value.startDate);
                }
                if (f.value.endDate) {
                    params.push('dateRanges[' + dateRangeIndex + '].endDate=' + f.value.endDate);
                }
                dateRangeIndex++;
            }
        });

        projectIds.forEach(function(id) { params.push('projectIds=' + id); });

        return params.length > 0 ? '?' + params.join('&') : '';
    };

    // Load organizations
    self.loadOrganizations = function() {
        customerApiFetch('/api/customer/portal/organizations')
            .then(function(response) {
                if (!response.ok) throw new Error('Organizasyon listesi yüklenemedi');
                return response.json();
            })
            .then(function(data) {
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

    // Load personnel list
    self.loadPersonnelList = function() {
        customerApiFetch('/api/customer/portal/reports/personnel-list')
            .then(function(response) {
                if (!response.ok) throw new Error('Personel listesi yüklenemedi');
                return response.json();
            })
            .then(function(data) {
                self.personnelList(data || []);
            })
            .catch(function(error) {
                console.error('Error loading personnel list:', error);
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

    // Organization change handler
    self.onOrganizationChange = function() {
        self.selectedPersonnelId('');
        self.report(null);
        self.activeFilters.removeAll();
    };

    // Personnel change handler
    self.onPersonnelChange = function() {
        self.report(null);
        self.activeFilters.removeAll();
    };

    // Load report
    self.loadReport = function() {
        if (!self.selectedPersonnelId()) {
            return;
        }

        self.isLoading(true);
        self.errorMessage('');
        self.report(null);

        var url = '/api/customer/portal/reports/personnel-report-card/' + self.selectedPersonnelId() + self.buildQueryParams();

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

    // Clear all (selection + filters)
    self.clearFilters = function() {
        self.selectedOrganizationId('');
        self.selectedPersonnelId('');
        self.activeFilters.removeAll();
        self.report(null);
        self.errorMessage('');
    };

    // Export to Excel
    self.exportToExcel = function() {
        if (!self.selectedPersonnelId()) return;

        self.isExporting(true);

        var url = '/api/customer/portal/reports/personnel-report-card/' + self.selectedPersonnelId() + '/export' + self.buildQueryParams();

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

    // Details modal functions
    self.showDetails = function(evaluationId) {
        self.isDetailsModalOpen(true);
        self.isDetailsLoading(true);
        self.detailsData(null);

        customerApiFetch('/api/customer/portal/evaluations/' + evaluationId)
            .then(function(response) {
                if (!response.ok) throw new Error('Detay yuklenemedi');
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
        var filename = 'Dinleme_Detay_' + (data.callId || data.id) + '.xlsx';

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

    // Print report as PDF
    self.printReport = function() {
        var header = document.querySelector('.d-flex.justify-content-between.align-items-center.mb-4');
        var selection = document.querySelector('.card.shadow-sm.mb-4');

        if (header) header.style.display = 'none';
        if (selection) selection.style.display = 'none';

        window.print();

        if (header) header.style.display = '';
        if (selection) selection.style.display = '';
    };

    // Initialize
    self.loadOrganizations();
    self.loadPersonnelList();
    self.loadProjects();
}

// Apply bindings when DOM is ready
$(document).ready(function() {
    ko.applyBindings(new CustomerPersonnelReportCardViewModel(), document.getElementById('personnel-report-card-app'));
});
