// CustomerPortal Suggestions Report ViewModel
function CustomerSuggestionsViewModel() {
    var self = this;

    // State
    self.isLoading = ko.observable(false);
    self.isExporting = ko.observable(false);
    self.isExportingTopQuestions = ko.observable(false);
    self.errorMessage = ko.observable('');

    // Filter options (müşteriye özel)
    self.projects = ko.observableArray([]);

    // Filter UI - Chip-based filtre sistemi
    self.selectedFilterType = ko.observable('');
    self.tempFilter = {
        projectId: ko.observable(''),
        searchText: ko.observable(''),
        startDate: ko.observable(''),
        endDate: ko.observable(''),
        dateRangeType: ko.observable('')
    };

    // Active filters (chip-based)
    self.activeFilters = ko.observableArray([]);

    // Date range labels
    self.dateRangeLabels = {
        'today': 'Bugün',
        'yesterday': 'Dün',
        'thisWeek': 'Bu Hafta',
        'lastWeek': 'Geçen Hafta',
        'thisMonth': 'Bu Ay',
        'lastMonth': 'Geçen Ay'
    };

    // Can add filter check
    self.canAddFilter = ko.computed(function() {
        var type = self.selectedFilterType();
        if (!type) return false;
        if (type === 'project') return self.tempFilter.projectId();
        if (type === 'searchText') return self.tempFilter.searchText();
        if (type === 'dateRange') return self.tempFilter.startDate() || self.tempFilter.endDate() || self.tempFilter.dateRangeType();
        return false;
    });

    // Date range helper
    self.calculateDateRange = function(rangeType) {
        var today = new Date();
        var start, end;

        if (rangeType === 'today') {
            start = end = today.toISOString().split('T')[0];
        } else if (rangeType === 'yesterday') {
            var yesterday = new Date(today);
            yesterday.setDate(yesterday.getDate() - 1);
            start = end = yesterday.toISOString().split('T')[0];
        } else if (rangeType === 'thisWeek') {
            var dayOfWeek = today.getDay();
            var diff = today.getDate() - dayOfWeek + (dayOfWeek === 0 ? -6 : 1);
            var weekStart = new Date(today);
            weekStart.setDate(diff);
            start = weekStart.toISOString().split('T')[0];
            end = today.toISOString().split('T')[0];
        } else if (rangeType === 'lastWeek') {
            var dayOfWeek = today.getDay();
            var diff = today.getDate() - dayOfWeek + (dayOfWeek === 0 ? -6 : 1);
            var thisWeekStart = new Date(today);
            thisWeekStart.setDate(diff);
            var lastWeekStart = new Date(thisWeekStart);
            lastWeekStart.setDate(lastWeekStart.getDate() - 7);
            var lastWeekEnd = new Date(lastWeekStart);
            lastWeekEnd.setDate(lastWeekEnd.getDate() + 6);
            start = lastWeekStart.toISOString().split('T')[0];
            end = lastWeekEnd.toISOString().split('T')[0];
        } else if (rangeType === 'thisMonth') {
            start = new Date(today.getFullYear(), today.getMonth(), 1).toISOString().split('T')[0];
            end = today.toISOString().split('T')[0];
        } else if (rangeType === 'lastMonth') {
            start = new Date(today.getFullYear(), today.getMonth() - 1, 1).toISOString().split('T')[0];
            end = new Date(today.getFullYear(), today.getMonth(), 0).toISOString().split('T')[0];
        }

        return { start: start, end: end };
    };

    // Date range helper for UI (dropdown içi)
    self.setDateRange = function(rangeType) {
        var range = self.calculateDateRange(rangeType);
        self.tempFilter.startDate(range.start);
        self.tempFilter.endDate(range.end);
        self.tempFilter.dateRangeType(rangeType);
    };

    // Quick date range filter - direkt uygula ve ara
    self.setQuickDateRange = function(rangeType) {
        var range = self.calculateDateRange(rangeType);
        var displayValue = self.dateRangeLabels[rangeType] || (range.start + ' - ' + range.end);

        self.activeFilters.push({
            type: 'dateRange',
            value: null,
            startDate: range.start,
            endDate: range.end,
            dateRangeType: rangeType,
            label: 'Tarih',
            displayValue: displayValue
        });

        self.search();
    };

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
            displayValue = project ? project.name : filter.value;
        } else if (type === 'searchText') {
            filter.value = self.tempFilter.searchText();
            label = 'Metin';
            displayValue = filter.value;
        } else if (type === 'dateRange') {
            filter.dateRangeType = self.tempFilter.dateRangeType();
            filter.startDate = self.tempFilter.startDate();
            filter.endDate = self.tempFilter.endDate();
            label = 'Tarih';
            if (filter.dateRangeType && self.dateRangeLabels[filter.dateRangeType]) {
                displayValue = self.dateRangeLabels[filter.dateRangeType];
            } else {
                displayValue = (filter.startDate || '...') + ' - ' + (filter.endDate || '...');
            }
        }

        // Tüm filtre tipleri çoklu değer destekler
        self.activeFilters.push({
            type: type,
            value: filter.value,
            startDate: filter.startDate,
            endDate: filter.endDate,
            dateRangeType: filter.dateRangeType,
            label: label,
            displayValue: displayValue
        });

        // Reset temp
        self.resetTempFilter();
        self.selectedFilterType('');
        self.search(); // Filtre eklenince otomatik ara
    };

    self.resetTempFilter = function() {
        self.tempFilter.projectId('');
        self.tempFilter.searchText('');
        self.tempFilter.startDate('');
        self.tempFilter.endDate('');
        self.tempFilter.dateRangeType('');
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
        self.currentPage(1);
        self.loadReport();
    };

    // Summary
    self.summary = ko.observable({
        totalSuggestions: 0,
        totalEvaluationsWithSuggestions: 0,
        uniquePersonnel: 0
    });

    // Pagination
    self.currentPage = ko.observable(1);
    self.pageSize = ko.observable(50);
    self.totalCount = ko.observable(0);
    self.totalPages = ko.computed(function() {
        return Math.ceil(self.totalCount() / self.pageSize()) || 1;
    });

    // Visible pages for pagination
    self.visiblePages = ko.computed(function() {
        var pages = [];
        var current = self.currentPage();
        var total = self.totalPages();
        var start = Math.max(1, current - 2);
        var end = Math.min(total, current + 2);

        for (var i = start; i <= end; i++) {
            pages.push(i);
        }
        return pages;
    });

    // Data
    self.suggestions = ko.observableArray([]);
    self.topSuggestedQuestions = ko.observableArray([]);

    // Details Modal State
    self.isDetailsModalOpen = ko.observable(false);
    self.isDetailsLoading = ko.observable(false);
    self.detailsData = ko.observable(null);

    // Load filter options
    self.loadFilterOptions = function() {
        // Load projects (müşteriye ait)
        customerApiFetch('/api/customer/portal/projects')
            .then(function(response) { return response.json(); })
            .then(function(data) {
                self.projects(data || []);
            })
            .catch(function(error) {
                console.error('Error loading projects:', error);
            });
    };

    // Build query params from active filters
    self.buildQueryParams = function(includePagination) {
        var params = [];

        // Çoklu değer desteği için array'ler
        var projectIds = [];
        var searchTexts = [];

        self.activeFilters().forEach(function(f) {
            if (f.type === 'project') {
                projectIds.push(f.value);
            } else if (f.type === 'searchText') {
                searchTexts.push(f.value);
            } else if (f.type === 'dateRange') {
                if (f.dateRangeType && self.dateRangeLabels[f.dateRangeType]) {
                    var range = self.calculateDateRange(f.dateRangeType);
                    params.push('startDate=' + range.start);
                    params.push('endDate=' + range.end);
                } else {
                    if (f.startDate) params.push('startDate=' + f.startDate);
                    if (f.endDate) params.push('endDate=' + f.endDate);
                }
            }
        });

        // Çoklu değerleri query string'e ekle
        projectIds.forEach(function(id) { params.push('projectId=' + id); });
        // SearchText için birleştir (veya her birini ayrı gönder)
        if (searchTexts.length > 0) {
            params.push('searchText=' + encodeURIComponent(searchTexts.join(' ')));
        }

        if (includePagination) {
            params.push('page=' + self.currentPage());
            params.push('pageSize=' + self.pageSize());
        }
        return params.length > 0 ? '?' + params.join('&') : '';
    };

    // Load suggestions report
    self.loadReport = function() {
        self.isLoading(true);
        self.errorMessage('');

        var url = '/api/customer/portal/reports/suggestions' + self.buildQueryParams(true);

        // Load main report and top questions in parallel
        Promise.all([
            customerApiFetch(url).then(function(r) {
                if (!r.ok) throw new Error('Rapor yüklenemedi');
                return r.json();
            }),
            customerApiFetch('/api/customer/portal/reports/suggestions/top-questions' + self.buildQueryParams(false)).then(function(r) {
                if (!r.ok) throw new Error('Top sorular yüklenemedi');
                return r.json();
            })
        ])
        .then(function(results) {
            var data = results[0];
            var topQuestions = results[1];

            self.summary(data.summary || {
                totalSuggestions: 0,
                totalEvaluationsWithSuggestions: 0,
                uniquePersonnel: 0
            });
            self.suggestions(data.suggestions || []);
            self.totalCount(data.totalCount || 0);
            self.topSuggestedQuestions(topQuestions || []);
        })
        .catch(function(error) {
            console.error('Suggestions report error:', error);
            toastr.error(error.message || 'Rapor yüklenirken bir hata oluştu.');
        })
        .finally(function() {
            self.isLoading(false);
        });
    };

    // Pagination
    self.goToPage = function(page) {
        if (page >= 1 && page <= self.totalPages()) {
            self.currentPage(page);
            self.loadReport();
        }
    };

    self.previousPage = function() {
        if (self.currentPage() > 1) {
            self.goToPage(self.currentPage() - 1);
        }
    };

    self.nextPage = function() {
        if (self.currentPage() < self.totalPages()) {
            self.goToPage(self.currentPage() + 1);
        }
    };

    // Export to Excel
    self.exportToExcel = function() {
        self.isExporting(true);

        var url = '/api/customer/portal/reports/suggestions/export' + self.buildQueryParams(false);

        customerApiFetch(url)
            .then(function(response) {
                if (!response.ok) throw new Error('Export başarısız');
                return response.blob();
            })
            .then(function(blob) {
                var url = window.URL.createObjectURL(blob);
                var a = document.createElement('a');
                a.href = url;
                a.download = 'OnerilerRaporu_' + new Date().toISOString().split('T')[0] + '.xlsx';
                document.body.appendChild(a);
                a.click();
                window.URL.revokeObjectURL(url);
                a.remove();
            })
            .catch(function(error) {
                console.error('Export error:', error);
                toastr.error('Excel export başarısız: ' + error.message);
            })
            .finally(function() {
                self.isExporting(false);
            });
    };

    // Export Top Questions to Excel
    self.exportTopQuestionsToExcel = function() {
        self.isExportingTopQuestions(true);

        var url = '/api/customer/portal/reports/suggestions/top-questions/export' + self.buildQueryParams(false);

        customerApiFetch(url)
            .then(function(response) {
                if (!response.ok) throw new Error('Export başarısız');
                return response.blob();
            })
            .then(function(blob) {
                var url = window.URL.createObjectURL(blob);
                var a = document.createElement('a');
                a.href = url;
                a.download = 'EnCokOnerilenSorular_' + new Date().toISOString().split('T')[0] + '.xlsx';
                document.body.appendChild(a);
                a.click();
                window.URL.revokeObjectURL(url);
                a.remove();
            })
            .catch(function(error) {
                console.error('Export error:', error);
                toastr.error('Excel export başarısız: ' + error.message);
            })
            .finally(function() {
                self.isExportingTopQuestions(false);
            });
    };

    // Show Evaluation Details Modal
    self.showEvaluationDetails = function(suggestion) {
        self.isDetailsModalOpen(true);
        self.isDetailsLoading(true);
        self.detailsData(null);

        customerApiFetch('/api/customer/portal/evaluations/' + suggestion.evaluationId)
            .then(function(response) {
                if (!response.ok) throw new Error('Detay yüklenemedi');
                return response.json();
            })
            .then(function(data) {
                self.detailsData(data);
            })
            .catch(function(error) {
                console.error('Details load error:', error);
                toastr.error('Değerlendirme detayı yüklenemedi.');
                self.closeDetailsModal();
            })
            .finally(function() {
                self.isDetailsLoading(false);
            });
    };

    // Close Details Modal
    self.closeDetailsModal = function() {
        self.isDetailsModalOpen(false);
        self.detailsData(null);
    };

    // Initialize
    self.loadFilterOptions();
    self.loadReport();
}

// Apply bindings when DOM is ready
$(document).ready(function() {
    ko.applyBindings(new CustomerSuggestionsViewModel(), document.getElementById('suggestions-app'));
});
