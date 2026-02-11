// Personnel Report Card (Temsilci Karnesi) JavaScript
function PersonnelReportCardViewModel() {
    var self = this;

    // State
    self.isLoading = ko.observable(false);
    self.isExporting = ko.observable(false);
    self.errorMessage = ko.observable('');
    self.report = ko.observable(null);
    self.selectedDetail = ko.observable(null);

    // ===== HİYERARŞİK SEÇİM (Personele ulaşmak için) =====
    self.customers = ko.observableArray([]);
    self.organizations = ko.observableArray([]);
    self.personnelList = ko.observableArray([]);

    self.selectedCustomerId = ko.observable('');
    self.selectedOrganizationId = ko.observable('');
    self.selectedPersonnelId = ko.observable('');

    // ===== FİLTRE SİSTEMİ (Chip-based pattern) =====
    self.projects = ko.observableArray([]);
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
                var project = self.projects().find(function(p) { return p.id === projectId; });
                if (!project) return;

                // Aynı proje zaten eklenmişse ekleme
                var alreadyExists = self.activeFilters().some(function(f) {
                    return f.type === 'project' && f.value === projectId;
                });
                if (alreadyExists) {
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
    };

    // Remove filter
    self.removeFilter = function(filter) {
        self.activeFilters.remove(filter);
    };

    // Clear all filters
    self.clearAllFilters = function() {
        self.activeFilters([]);
    };

    // ===== HİYERARŞİK SEÇİM FONKSİYONLARI =====

    // Computed: Filtered organizations based on customer selection
    self.filteredOrganizations = ko.computed(function() {
        var customerId = self.selectedCustomerId();
        if (!customerId) return self.organizations();
        return self.organizations().filter(function(o) {
            return o.customerId == customerId;
        });
    });

    // Computed: Filtered personnel based on customer and organization selection
    self.filteredPersonnelList = ko.computed(function() {
        var customerId = self.selectedCustomerId();
        var organizationId = self.selectedOrganizationId();
        var list = self.personnelList();

        if (customerId) {
            list = list.filter(function(p) {
                return p.customerId == customerId;
            });
        }

        if (organizationId) {
            list = list.filter(function(p) {
                return p.organizationId == organizationId;
            });
        }

        return list;
    });

    // Load customers with evaluations
    self.loadCustomers = function() {
        fetch('/api/reports/report-card/customers', { credentials: 'include' })
            .then(function(response) {
                if (!response.ok) throw new Error('Musteri listesi yuklenemedi');
                return response.json();
            })
            .then(function(data) {
                self.customers(data || []);
            })
            .catch(function(error) {
                console.error('Error loading customers:', error);
            });
    };

    // Load organizations with evaluations
    self.loadOrganizations = function(customerId) {
        var url = '/api/reports/report-card/organizations';
        if (customerId) {
            url += '?customerId=' + customerId;
        }

        fetch(url, { credentials: 'include' })
            .then(function(response) {
                if (!response.ok) throw new Error('Organizasyon listesi yuklenemedi');
                return response.json();
            })
            .then(function(data) {
                self.organizations(data || []);
            })
            .catch(function(error) {
                console.error('Error loading organizations:', error);
            });
    };

    // Load personnel list
    self.loadPersonnelList = function(customerId, organizationId) {
        var url = '/api/reports/personnel-list';
        var params = [];

        if (customerId) {
            params.push('customerId=' + customerId);
        }
        if (organizationId) {
            params.push('organizationId=' + organizationId);
        }

        if (params.length > 0) {
            url += '?' + params.join('&');
        }

        fetch(url, { credentials: 'include' })
            .then(function(response) {
                if (!response.ok) throw new Error('Personel listesi yuklenemedi');
                return response.json();
            })
            .then(function(data) {
                self.personnelList(data || []);

                // Pre-select if initial personnel ID is provided
                if (typeof initialPersonnelId !== 'undefined' && initialPersonnelId) {
                    self.selectedPersonnelId(initialPersonnelId);
                    self.loadProjectsForPersonnel(initialPersonnelId);
                }
            })
            .catch(function(error) {
                console.error('Error loading personnel list:', error);
                toastr.error('Personel listesi yuklenirken bir hata olustu.');
            });
    };

    // Load projects for selected personnel
    self.loadProjectsForPersonnel = function(personnelId) {
        if (!personnelId) {
            self.projects([]);
            return;
        }

        fetch('/api/reports/personnel-projects/' + personnelId, { credentials: 'include' })
            .then(function(response) {
                if (!response.ok) throw new Error('Proje listesi yuklenemedi');
                return response.json();
            })
            .then(function(data) {
                self.projects(data || []);
            })
            .catch(function(error) {
                console.error('Error loading projects for personnel:', error);
                self.projects([]);
            });
    };

    // Customer change handler
    self.onCustomerChange = function() {
        var customerId = self.selectedCustomerId();

        // Reset downstream selections
        self.selectedOrganizationId('');
        self.selectedPersonnelId('');
        self.projects([]);
        self.activeFilters([]);
        self.report(null);

        // Reload organizations
        self.loadOrganizations(customerId);
    };

    // Organization change handler
    self.onOrganizationChange = function() {
        // Reset personnel selection
        self.selectedPersonnelId('');
        self.projects([]);
        self.activeFilters([]);
        self.report(null);
    };

    // Personnel change handler - projeleri yükle
    self.onPersonnelChange = function() {
        var personnelId = self.selectedPersonnelId();
        self.activeFilters([]);
        self.report(null);
        self.loadProjectsForPersonnel(personnelId);
    };

    // ===== RAPOR FONKSİYONLARI =====

    // Build filter params from activeFilters (URLSearchParams pattern)
    self.buildFilterParams = function() {
        var params = new URLSearchParams();

        self.activeFilters().forEach(function(filter) {
            switch (filter.type) {
                case 'project':
                    params.append('projectIds', filter.value);
                    break;
                case 'dateRange':
                    if (filter.value.startDate) {
                        params.append('startDate', filter.value.startDate);
                    }
                    if (filter.value.endDate) {
                        params.append('endDate', filter.value.endDate);
                    }
                    break;
            }
        });

        return params.toString();
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
        var params = self.buildFilterParams();

        if (params) {
            url += '?' + params;
        }

        console.log('PersonnelReportCard URL:', url);
        console.log('Active filters:', JSON.stringify(self.activeFilters()));

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

    // Clear filters and reset
    self.clearFilters = function() {
        // Hiyerarşi sıfırlama
        self.selectedCustomerId('');
        self.selectedOrganizationId('');
        self.selectedPersonnelId('');

        // Filtre sıfırlama
        self.activeFilters([]);
        self.projects([]);

        self.report(null);
        self.errorMessage('');

        // Reload organizations
        self.loadOrganizations();
    };

    // Export to Excel
    self.exportToExcel = function() {
        if (!self.selectedPersonnelId()) return;

        self.isExporting(true);

        var url = '/api/reports/personnel-report-card/' + self.selectedPersonnelId() + '/export';
        var params = self.buildFilterParams();

        if (params) {
            url += '?' + params;
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
                toastr.error('Detay yuklenirken bir hata olustu.');
            });
    };

    // Export to PDF via PdfService
    self.exportToPdf = function() {
        if (!self.selectedPersonnelId()) return;

        self.isExporting(true);

        var url = '/api/reports/personnel-report-card/' + self.selectedPersonnelId() + '/export-pdf';
        var params = self.buildFilterParams();
        if (params) {
            url += '?' + params;
        }

        fetch(url, { credentials: 'include' })
            .then(function(response) {
                if (!response.ok) throw new Error('PDF olusturulamadi');
                return response.blob();
            })
            .then(function(blob) {
                var a = document.createElement('a');
                var objectUrl = URL.createObjectURL(blob);
                a.href = objectUrl;
                a.download = 'TemsilciKarnesi_' + (self.report() ? self.report().personnelName.replace(/ /g, '_') : 'rapor') + '.pdf';
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
    self.loadOrganizations();
    self.loadPersonnelList();
}

// Apply bindings when DOM is ready
document.addEventListener('DOMContentLoaded', function() {
    var element = document.getElementById('personnel-report-card-app');
    if (element) {
        ko.applyBindings(new PersonnelReportCardViewModel(), element);
    }
});
