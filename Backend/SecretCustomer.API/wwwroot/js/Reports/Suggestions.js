// Suggestions Report ViewModel
function SuggestionsViewModel() {
    var self = this;

    // Page name for saved filters
    self.pageName = '/Reports/Suggestions';

    // State
    self.isLoading = ko.observable(false);
    self.isExporting = ko.observable(false);
    self.errorMessage = ko.observable('');

    // Lookup data
    self.projects = ko.observableArray([]);
    self.branches = ko.observableArray([]);
    self.checklists = ko.observableArray([]);
    self.dateRanges = ko.observableArray([
        { systemName: 'today', name: 'Bugün' },
        { systemName: 'yesterday', name: 'Dün' },
        { systemName: 'thisWeek', name: 'Bu Hafta' },
        { systemName: 'lastWeek', name: 'Geçen Hafta' },
        { systemName: 'thisMonth', name: 'Bu Ay' },
        { systemName: 'lastMonth', name: 'Geçen Ay' },
        { systemName: 'last7Days', name: 'Son 7 Gün' },
        { systemName: 'last30Days', name: 'Son 30 Gün' }
    ]);

    // ===== Chip-based Filter System =====
    self.selectedFilterType = ko.observable('');
    self.activeFilters = ko.observableArray([]);

    // Temp filter values
    self.tempFilter = {
        projectId: ko.observable(null),
        branchId: ko.observable(null),
        checklistId: ko.observable(null),
        searchText: ko.observable(''),
        startDate: ko.observable(''),
        endDate: ko.observable(''),
        selectedDateRangeType: ko.observable(null)
    };

    // Tarih manuel degistirildiginde dateRangeType'i temizle
    self.tempFilter.startDate.subscribe(function(newVal) {
        if (self._manualDateChange) {
            self.tempFilter.selectedDateRangeType(null);
        }
    });
    self.tempFilter.endDate.subscribe(function(newVal) {
        if (self._manualDateChange) {
            self.tempFilter.selectedDateRangeType(null);
        }
    });
    self._manualDateChange = true;

    // Pagination
    self.currentPage = ko.observable(1);
    self.pageSize = ko.observable(50);
    self.totalCount = ko.observable(0);
    self.totalPages = ko.computed(function() {
        return Math.ceil(self.totalCount() / self.pageSize()) || 1;
    });

    // Filter labels
    self.filterLabels = {
        project: 'Proje',
        branch: 'Şube/Org.',
        checklist: 'Kontrol Listesi',
        search: 'Arama',
        dateRange: 'Tarih'
    };

    // Summary
    self.summary = ko.observable({
        totalSuggestions: 0,
        totalEvaluationsWithSuggestions: 0,
        uniqueEvaluators: 0,
        uniquePersonnel: 0
    });

    // Data
    self.suggestions = ko.observableArray([]);
    self.topSuggestedQuestions = ko.observableArray([]);
    self.evaluationNotes = ko.observableArray([]);
    self.evaluationNotesCount = ko.observable(0);

    // Evaluation Detail Modal
    self.isDetailModalOpen = ko.observable(false);
    self.isDetailLoading = ko.observable(false);
    self.detailData = ko.observable(null);

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

    // Can add filter
    self.canAddFilter = ko.computed(function() {
        var type = self.selectedFilterType();
        if (!type) return false;

        switch (type) {
            case 'project': return !!self.tempFilter.projectId();
            case 'branch': return !!self.tempFilter.branchId();
            case 'checklist': return !!self.tempFilter.checklistId();
            case 'search': return !!self.tempFilter.searchText();
            case 'dateRange': return !!self.tempFilter.startDate() || !!self.tempFilter.endDate();
            default: return false;
        }
    });

    // Set temp date range
    self.setTempDateRange = function(range) {
        var today = new Date();
        today.setHours(0, 0, 0, 0);
        var startDate = null;
        var endDate = null;

        var formatDate = function(date) {
            var year = date.getFullYear();
            var month = String(date.getMonth() + 1).padStart(2, '0');
            var day = String(date.getDate()).padStart(2, '0');
            return year + '-' + month + '-' + day;
        };

        var getMonday = function(d) {
            var date = new Date(d.getTime());
            var day = date.getDay();
            var diff = date.getDate() - day + (day === 0 ? -6 : 1);
            date.setDate(diff);
            return date;
        };

        switch (range) {
            case 'today':
                startDate = endDate = new Date(today.getTime());
                break;
            case 'yesterday':
                var yesterday = new Date(today.getTime());
                yesterday.setDate(yesterday.getDate() - 1);
                startDate = endDate = yesterday;
                break;
            case 'thisWeek':
                startDate = getMonday(today);
                endDate = new Date(today.getTime());
                break;
            case 'lastWeek':
                var lastWeekStart = getMonday(today);
                lastWeekStart.setDate(lastWeekStart.getDate() - 7);
                var lastWeekEnd = new Date(lastWeekStart.getTime());
                lastWeekEnd.setDate(lastWeekEnd.getDate() + 6);
                startDate = lastWeekStart;
                endDate = lastWeekEnd;
                break;
            case 'thisMonth':
                startDate = new Date(today.getFullYear(), today.getMonth(), 1);
                endDate = new Date(today.getTime());
                break;
            case 'lastMonth':
                startDate = new Date(today.getFullYear(), today.getMonth() - 1, 1);
                endDate = new Date(today.getFullYear(), today.getMonth(), 0);
                break;
            case 'last7Days':
                startDate = new Date(today.getTime());
                startDate.setDate(startDate.getDate() - 6);
                endDate = new Date(today.getTime());
                break;
            case 'last30Days':
                startDate = new Date(today.getTime());
                startDate.setDate(startDate.getDate() - 29);
                endDate = new Date(today.getTime());
                break;
        }

        self._manualDateChange = false;
        if (startDate) self.tempFilter.startDate(formatDate(startDate));
        if (endDate) self.tempFilter.endDate(formatDate(endDate));
        self.tempFilter.selectedDateRangeType(range);
        self._manualDateChange = true;
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
                filter.value = projectId;
                filter.displayValue = project.name;
                self.tempFilter.projectId(null);
                break;

            case 'branch':
                var branchId = self.tempFilter.branchId();
                var branch = self.branches().find(function(b) { return b.id === branchId; });
                if (!branch) return;
                filter.value = branchId;
                filter.displayValue = branch.name;
                self.tempFilter.branchId(null);
                break;

            case 'checklist':
                var checklistId = self.tempFilter.checklistId();
                var checklist = self.checklists().find(function(c) { return c.id === checklistId; });
                if (!checklist) return;
                filter.value = checklistId;
                filter.displayValue = checklist.name;
                self.tempFilter.checklistId(null);
                break;

            case 'search':
                var searchText = self.tempFilter.searchText();
                if (!searchText) return;
                filter.value = searchText;
                filter.displayValue = '"' + searchText + '"';
                self.tempFilter.searchText('');
                break;

            case 'dateRange':
                var startDate = self.tempFilter.startDate();
                var endDate = self.tempFilter.endDate();
                var dateRangeType = self.tempFilter.selectedDateRangeType();
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
                self.tempFilter.selectedDateRangeType(null);
                break;

            default:
                return;
        }

        // Tüm filtre tipleri çoklu değer destekler
        self.activeFilters.push(filter);
        self.selectedFilterType('');
        self.search();
    };

    // Remove filter
    self.removeFilter = function(filter) {
        self.activeFilters.remove(filter);
        self.search();
    };

    // Clear all filters
    self.clearFilters = function() {
        self.activeFilters.removeAll();
        self.search();
    };

    // Load filter options
    self.loadFilterOptions = function() {
        // Load projects
        fetch('/api/projects', { credentials: 'include' })
            .then(function(response) { return response.json(); })
            .then(function(data) {
                self.projects(data);
            })
            .catch(function(error) {
                console.error('Error loading projects:', error);
            });

        // Branches modülü kaldırıldı - CustomerOrganizations kullanılıyor
        fetch('/api/customer-organizations', { credentials: 'include' })
            .then(function(response) { return response.json(); })
            .then(function(data) {
                self.branches(data || []);
            })
            .catch(function(error) {
                console.error('Error loading organizations:', error);
                self.branches([]);
            });

        // Load checklists
        fetch('/api/checklists', { credentials: 'include' })
            .then(function(response) { return response.json(); })
            .then(function(data) {
                self.checklists(data);
            })
            .catch(function(error) {
                console.error('Error loading checklists:', error);
            });
    };

    // Build query params from active filters (çoklu değer desteği)
    self.buildQueryParams = function(includePagination) {
        var params = [];

        var projectIds = [];
        var branchIds = [];
        var checklistIds = [];
        var searchTexts = [];
        var dateRanges = [];

        self.activeFilters().forEach(function(filter) {
            switch (filter.type) {
                case 'project':
                    projectIds.push(filter.value);
                    break;
                case 'branch':
                    branchIds.push(filter.value);
                    break;
                case 'checklist':
                    checklistIds.push(filter.value);
                    break;
                case 'search':
                    searchTexts.push(filter.value);
                    break;
                case 'dateRange':
                    dateRanges.push({
                        startDate: filter.value.startDate,
                        endDate: filter.value.endDate
                    });
                    break;
            }
        });

        // Query string oluştur - HER ZAMAN ÇOĞUL KULLAN
        projectIds.forEach(function(id) { params.push('projectIds=' + id); });
        branchIds.forEach(function(id) { params.push('branchIds=' + id); });
        checklistIds.forEach(function(id) { params.push('checklistIds=' + id); });

        if (searchTexts.length > 0) params.push('searchText=' + encodeURIComponent(searchTexts.join(' ')));

        if (dateRanges.length > 0) {
            if (dateRanges[0].startDate) params.push('startDate=' + dateRanges[0].startDate);
            if (dateRanges[0].endDate) params.push('endDate=' + dateRanges[0].endDate);
        }

        if (includePagination) {
            params.push('page=' + self.currentPage());
            params.push('pageSize=' + self.pageSize());
        }

        return params.length > 0 ? '?' + params.join('&') : '';
    };

    // Search
    self.search = function() {
        self.currentPage(1);
        self.loadReport();
    };

    // Load suggestions report
    self.loadReport = function() {
        self.isLoading(true);
        self.errorMessage('');

        var url = '/api/reports/suggestions' + self.buildQueryParams(true);

        // Load main report and top questions in parallel
        Promise.all([
            fetch(url, { credentials: 'include' }).then(function(r) { return r.json(); }),
            fetch('/api/reports/suggestions/top-questions' + self.buildQueryParams(false), { credentials: 'include' }).then(function(r) { return r.json(); })
        ])
        .then(function(results) {
            var data = results[0];
            var topQuestions = results[1];

            self.summary(data.summary || {
                totalSuggestions: 0,
                totalEvaluationsWithSuggestions: 0,
                uniqueEvaluators: 0,
                uniquePersonnel: 0,
                evaluationNotesCount: 0
            });
            self.suggestions(data.suggestions || []);
            self.totalCount(data.totalCount || 0);
            self.topSuggestedQuestions(topQuestions || []);
            self.evaluationNotes(data.evaluationNotes || []);
            self.evaluationNotesCount(data.evaluationNotesCount || 0);
        })
        .catch(function(error) {
            console.error('Suggestions report error:', error);
            toastr.error(error.message || T('Report.LoadErrorMessage', 'Rapor yüklenirken bir hata oluştu.'));
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

        var url = '/api/reports/suggestions/export' + self.buildQueryParams(false);

        fetch(url, { credentials: 'include' })
            .then(function(response) {
                if (!response.ok) throw new Error(T('Report.ExportError', 'Export başarısız'));
                return response.blob();
            })
            .then(function(blob) {
                var url = window.URL.createObjectURL(blob);
                var a = document.createElement('a');
                a.href = url;
                a.download = T('File.SuggestionsReport', 'OnerilerRaporu') + '_' + new Date().toISOString().split('T')[0] + '.xlsx';
                document.body.appendChild(a);
                a.click();
                window.URL.revokeObjectURL(url);
                a.remove();
            })
            .catch(function(error) {
                console.error('Export error:', error);
                toastr.error(T('Report.ExcelExportError', 'Excel export başarısız') + ': ' + error.message);
            })
            .finally(function() {
                self.isExporting(false);
            });
    };

    // Truncate long CallId for display
    self.truncateCallId = function(callId) {
        if (!callId) return '-';
        if (callId.length <= 20) return callId;
        return callId.substring(0, 8) + '...' + callId.substring(callId.length - 8);
    };

    // Copy text to clipboard
    self.copyToClipboard = function(text) {
        if (!text) return;
        if (navigator.clipboard && navigator.clipboard.writeText) {
            navigator.clipboard.writeText(text).then(function() {
                toastr.success(T('Common.CopiedToClipboard', 'Panoya kopyalandı'));
            }).catch(function() {
                self.fallbackCopyToClipboard(text);
            });
        } else {
            self.fallbackCopyToClipboard(text);
        }
    };

    self.fallbackCopyToClipboard = function(text) {
        var textArea = document.createElement('textarea');
        textArea.value = text;
        textArea.style.position = 'fixed';
        textArea.style.left = '-9999px';
        document.body.appendChild(textArea);
        textArea.select();
        try {
            document.execCommand('copy');
            toastr.success(T('Common.CopiedToClipboard', 'Panoya kopyalandı'));
        } catch (err) {
            toastr.error(T('Common.CopyFailed', 'Kopyalama başarısız'));
        }
        document.body.removeChild(textArea);
    };

    // Show evaluation detail in modal
    self.showEvaluationDetail = function(suggestion) {
        if (!suggestion.evaluationId) {
            toastr.warning(T('Evaluation.NotFound', 'Degerlendirme bulunamadi'));
            return;
        }

        self.isDetailModalOpen(true);
        self.isDetailLoading(true);
        self.detailData(null);

        fetch('/api/evaluations/' + suggestion.evaluationId, { credentials: 'include' })
            .then(function(response) {
                if (!response.ok) throw new Error(T('Evaluation.NotFound', 'Degerlendirme bulunamadi'));
                return response.json();
            })
            .then(function(data) {
                self.detailData(data);
            })
            .catch(function(error) {
                console.error('Detail load error:', error);
                self.closeDetailModal();
                toastr.error(T('Evaluation.DetailsLoadError', 'Degerlendirme detaylari yuklenirken hata olustu.'));
            })
            .finally(function() {
                self.isDetailLoading(false);
            });
    };

    // Export detail to Excel
    self.exportDetailToExcel = function() {
        var detail = self.detailData();
        if (!detail || !detail.id) return;

        self.isExporting(true);
        fetch('/api/reports/evaluations/' + detail.id + '/export', { credentials: 'include' })
            .then(function(response) {
                if (!response.ok) throw new Error('Export failed');
                return response.blob();
            })
            .then(function(blob) {
                var url = window.URL.createObjectURL(blob);
                var a = document.createElement('a');
                a.href = url;
                a.download = 'Degerlendirme_Detay_' + detail.id + '.xlsx';
                document.body.appendChild(a);
                a.click();
                window.URL.revokeObjectURL(url);
                a.remove();
            })
            .catch(function(error) {
                console.error('Export error:', error);
                toastr.error(T('Report.ExcelExportError', 'Excel export basarisiz'));
            })
            .finally(function() {
                self.isExporting(false);
            });
    };

    // Close detail modal
    self.closeDetailModal = function() {
        self.isDetailModalOpen(false);
        self.detailData(null);
    };

    // Initialize
    self.loadFilterOptions();
    self.loadReport();
}

// Translation keys
var TRANSLATION_KEYS = [
    'Report.LoadErrorMessage',
    'Report.ExportError',
    'Report.ExcelExportError',
    'File.SuggestionsReport',
    'Evaluation.NotFound',
    'Evaluation.DetailsLoadError',
    'Common.CopiedToClipboard',
    'Common.CopyFailed'
];

// Apply bindings
$(document).ready(function() {
    Localization.loadKeys(TRANSLATION_KEYS).then(function() {
        ko.applyBindings(new SuggestionsViewModel(), document.getElementById('suggestions-app'));
    });
});
