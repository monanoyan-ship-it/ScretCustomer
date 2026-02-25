// CustomerPortal Performance By Period Report ViewModel
function PerformanceByPeriodViewModel() {
    var self = this;

    // State
    self.isLoading = ko.observable(false);
    self.isExporting = ko.observable(false);

    // Filter options
    self.projects = ko.observableArray([]);
    self.organizations = ko.observableArray([]);

    // Filter UI - Chip-based filtre sistemi
    self.selectedFilterType = ko.observable('');
    self.tempFilter = {
        projectId: ko.observable(''),
        organizationId: ko.observable(''),
        startDate: ko.observable(''),
        endDate: ko.observable('')
    };

    // Active filters (chip-based)
    self.activeFilters = ko.observableArray([]);

    // Can add filter check
    self.canAddFilter = ko.computed(function() {
        var type = self.selectedFilterType();
        if (!type) return false;
        if (type === 'project') return self.tempFilter.projectId();
        if (type === 'organization') return self.tempFilter.organizationId();
        if (type === 'dateRange') return self.tempFilter.startDate() || self.tempFilter.endDate();
        return false;
    });

    // Add filter
    self.addFilter = function() {
        var type = self.selectedFilterType();
        if (!type) return;

        var filter = { type: type };
        var label = '';
        var displayValue = '';

        if (type === 'project') {
            filter.value = self.tempFilter.projectId();
            var project = self.projects().find(function(p) { return p.id == filter.value; });
            label = 'Proje';
            displayValue = project ? (project.code ? project.code + ' - ' + project.name : project.name) : filter.value;
        } else if (type === 'organization') {
            filter.value = self.tempFilter.organizationId();
            var org = self.organizations().find(function(o) { return o.id == filter.value; });
            label = 'Organizasyon';
            displayValue = org ? org.name : filter.value;
        } else if (type === 'dateRange') {
            var startDate = self.tempFilter.startDate();
            var endDate = self.tempFilter.endDate();

            // En az bir tarih girilmiş olmalı
            if (!startDate && !endDate) return;

            // Tarih doğrulama: endDate, startDate'den önce olamaz
            if (startDate && endDate && new Date(endDate) < new Date(startDate)) {
                toastr.warning('Bitiş tarihi, başlangıç tarihinden önce olamaz.');
                return;
            }

            // Mevcut tarih aralığı filtresini kaldır (sadece bir tane olabilir)
            self.activeFilters.remove(function(f) { return f.type === 'dateRange'; });

            filter.startDate = startDate || null;
            filter.endDate = endDate || null;
            label = 'Tarih Aralığı';

            // Display value oluştur
            var parts = [];
            if (startDate) parts.push(self.formatDateDisplay(startDate));
            if (endDate) parts.push(self.formatDateDisplay(endDate));
            displayValue = parts.join(' - ');
        }

        // Tüm filtre tipleri çoklu değer destekler (dateRange hariç)
        self.activeFilters.push({
            type: type,
            value: filter.value,
            startDate: filter.startDate,
            endDate: filter.endDate,
            label: label,
            displayValue: displayValue
        });

        // Reset temp
        self.resetTempFilter();
        self.selectedFilterType('');
        self.search(); // Filtre eklenince otomatik ara
    };

    // Tarih formatlama (YYYY-MM-DD -> DD.MM.YYYY)
    self.formatDateDisplay = function(dateStr) {
        if (!dateStr) return '';
        var parts = dateStr.split('-');
        if (parts.length === 3) {
            return parts[2] + '.' + parts[1] + '.' + parts[0];
        }
        return dateStr;
    };

    self.resetTempFilter = function() {
        self.tempFilter.projectId('');
        self.tempFilter.organizationId('');
        self.tempFilter.startDate('');
        self.tempFilter.endDate('');
    };

    self.removeFilter = function(filter) {
        self.activeFilters.remove(filter);
        self.search(); // Filtre kaldırılınca otomatik ara
    };

    self.clearFilters = function() {
        self.activeFilters.removeAll();
        self.search(); // Tüm filtreler temizlenince otomatik ara
    };

    // Search
    self.search = function() {
        self.loadReport();
    };

    // Report data
    self.periods = ko.observableArray([]);
    self.reportData = ko.observableArray([]);

    // Computed
    self.hasData = ko.computed(function() {
        return self.reportData().length > 0;
    });

    // Load filter options
    self.loadFilterOptions = function() {
        // Load projects
        customerApiFetch('/api/customer/portal/projects')
            .then(function(response) { return response.json(); })
            .then(function(data) {
                self.projects(data || []);
            })
            .catch(function(error) {
                console.error('Error loading projects:', error);
            });

        // Load organizations
        customerApiFetch('/api/customer/portal/organizations')
            .then(function(response) { return response.json(); })
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

    // Build query params from active filters
    self.buildQueryParams = function() {
        var params = [];

        // Çoklu değer desteği için array'ler
        var projectIds = [];
        var organizationIds = [];
        var startDate = null;
        var endDate = null;

        self.activeFilters().forEach(function(f) {
            if (f.type === 'project') {
                projectIds.push(f.value);
            } else if (f.type === 'organization') {
                organizationIds.push(f.value);
            } else if (f.type === 'dateRange') {
                startDate = f.startDate;
                endDate = f.endDate;
            }
        });

        // Çoklu değerleri query string'e ekle (API çoğul parametre bekliyor)
        projectIds.forEach(function(id) { params.push('projectIds=' + id); });
        organizationIds.forEach(function(id) { params.push('organizationIds=' + id); });

        // Tarih aralığı
        if (startDate) params.push('startDate=' + startDate);
        if (endDate) params.push('endDate=' + endDate);

        return params.length > 0 ? '?' + params.join('&') : '';
    };

    // Tarih kısayolları
    self.setDateShortcut = function(shortcut) {
        var today = new Date();
        var startDate, endDate;

        switch (shortcut) {
            case 'thisMonth':
                startDate = new Date(today.getFullYear(), today.getMonth(), 1);
                endDate = new Date(today.getFullYear(), today.getMonth() + 1, 0);
                break;
            case 'lastMonth':
                startDate = new Date(today.getFullYear(), today.getMonth() - 1, 1);
                endDate = new Date(today.getFullYear(), today.getMonth(), 0);
                break;
            case 'last3Months':
                startDate = new Date(today.getFullYear(), today.getMonth() - 2, 1);
                endDate = new Date(today.getFullYear(), today.getMonth() + 1, 0);
                break;
            case 'last6Months':
                startDate = new Date(today.getFullYear(), today.getMonth() - 5, 1);
                endDate = new Date(today.getFullYear(), today.getMonth() + 1, 0);
                break;
            case 'thisYear':
                startDate = new Date(today.getFullYear(), 0, 1);
                endDate = new Date(today.getFullYear(), 11, 31);
                break;
            case 'lastYear':
                startDate = new Date(today.getFullYear() - 1, 0, 1);
                endDate = new Date(today.getFullYear() - 1, 11, 31);
                break;
            default:
                return;
        }

        // Format dates as YYYY-MM-DD
        self.tempFilter.startDate(self.formatDateISO(startDate));
        self.tempFilter.endDate(self.formatDateISO(endDate));
    };

    // Date to ISO format (YYYY-MM-DD)
    self.formatDateISO = function(date) {
        var year = date.getFullYear();
        var month = String(date.getMonth() + 1).padStart(2, '0');
        var day = String(date.getDate()).padStart(2, '0');
        return year + '-' + month + '-' + day;
    };

    // Load report
    self.loadReport = function() {
        self.isLoading(true);

        var url = '/api/customer/portal/reports/performance-by-period' + self.buildQueryParams();

        customerApiFetch(url)
            .then(function(response) {
                if (!response.ok) throw new Error('Rapor yuklenemedi');
                return response.json();
            })
            .then(function(data) {
                self.periods(data.periods || []);
                self.reportData(data.data || []);
            })
            .catch(function(error) {
                console.error('Performance by period report error:', error);
                toastr.error(error.message || 'Rapor yuklenirken bir hata olustu.');
            })
            .finally(function() {
                self.isLoading(false);
            });
    };

    // Export to Excel
    self.exportToExcel = function() {
        self.isExporting(true);

        var url = '/api/customer/portal/reports/performance-by-period/export' + self.buildQueryParams();

        customerApiFetch(url)
            .then(function(response) {
                if (!response.ok) throw new Error('Export basarisiz');
                return response.blob();
            })
            .then(function(blob) {
                var url = window.URL.createObjectURL(blob);
                var a = document.createElement('a');
                a.href = url;
                a.download = 'DonemBazliBasari_' + new Date().toISOString().split('T')[0] + '.xlsx';
                document.body.appendChild(a);
                a.click();
                window.URL.revokeObjectURL(url);
                a.remove();
            })
            .catch(function(error) {
                console.error('Export error:', error);
                toastr.error('Excel export basarisiz: ' + error.message);
            })
            .finally(function() {
                self.isExporting(false);
            });
    };

    // Score cell class helper
    self.getScoreCellClass = function(score, projectTypeId) {
        return ScoreThresholds.getScoreCellClass(score, projectTypeId);
    };

    // Update legend with parametric thresholds
    ScoreThresholds.load().then(function() {
        var t = ScoreThresholds.get();
        var legendSuccess = document.getElementById('legend-success');
        var legendWarning = document.getElementById('legend-warning');
        var legendDanger = document.getElementById('legend-danger');
        if (legendSuccess) legendSuccess.textContent = t.success + '%+';
        if (legendWarning) legendWarning.textContent = t.warning + '-' + (t.success - 1) + '%';
        if (legendDanger) legendDanger.textContent = '<' + t.warning + '%';
    });

    // Initialize
    self.loadFilterOptions();
    self.loadReport();
}

// Apply bindings when DOM is ready
$(document).ready(function() {
    ko.applyBindings(new PerformanceByPeriodViewModel(), document.getElementById('performance-by-period-app'));
});
