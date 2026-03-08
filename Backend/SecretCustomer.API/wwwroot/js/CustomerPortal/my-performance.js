/**
 * MyPerformance - Temsilcinin Kendi Performans Sayfası
 * CustomerOperator rolü için dinlenen çağrıları, puanları, karnesi ve yorumları gösterir
 */

function MyPerformanceViewModel() {
    var self = this;

    // State
    self.isLoading = ko.observable(true);
    self.isExporting = ko.observable(false);
    self.evaluations = ko.observableArray([]);
    self.reportCard = ko.observable(null);
    self.searchText = ko.observable('');
    self.projects = ko.observableArray([]);

    // Details modal
    self.isDetailsModalOpen = ko.observable(false);
    self.isDetailsLoading = ko.observable(false);
    self.detailsData = ko.observable(null);
    self.isExportingDetail = ko.observable(false);

    // Pagination
    self.currentPage = ko.observable(1);
    self.pageSize = ko.observable(10);

    // Filter system (personnelReportCard.js pattern'inden birebir kopyalandı)
    self.activeFilters = ko.observableArray([]);
    self.selectedFilterType = ko.observable('');

    self.tempFilter = {
        projectId: ko.observable(null),
        startDate: ko.observable(''),
        endDate: ko.observable(''),
        dateRangeType: ko.observable(''),
        evaluationType: ko.observable('')
    };

    self.filterLabels = {
        project: 'Proje',
        dateRange: 'Tarih',
        evaluationType: 'Değerlendirme Tipi'
    };

    self.evaluationTypeLabels = {
        'internal': 'İç Değerlendirme',
        'external': 'Dış Değerlendirme'
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

    // Can add filter
    self.canAddFilter = ko.computed(function() {
        var type = self.selectedFilterType();
        if (!type) return false;
        switch (type) {
            case 'project': return self.tempFilter.projectId();
            case 'dateRange': return self.tempFilter.startDate() || self.tempFilter.endDate();
            case 'evaluationType': return !!self.tempFilter.evaluationType();
            default: return false;
        }
    });

    // Calculate date range
    self.calculateDateRange = function(rangeType) {
        var today = new Date();
        var start, end;
        var formatDate = formatLocalDate;

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

            case 'evaluationType':
                var evalType = self.tempFilter.evaluationType();
                if (!evalType) return;
                if (self.activeFilters().some(function(f) { return f.type === 'evaluationType'; })) {
                    toastr.warning('Değerlendirme tipi filtresi zaten eklenmiş. Önce mevcut olanı kaldırın.');
                    self.tempFilter.evaluationType('');
                    return;
                }
                filter.value = evalType;
                filter.displayValue = self.evaluationTypeLabels[evalType] || evalType;
                self.tempFilter.evaluationType('');
                break;

            default: return;
        }

        self.activeFilters.push(filter);
        self.selectedFilterType('');
        self.loadAll();
    };

    // Remove filter
    self.removeFilter = function(filter) {
        self.activeFilters.remove(filter);
        self.loadAll();
    };

    // Clear all filters
    self.clearAllFilters = function() {
        self.activeFilters.removeAll();
        self.loadAll();
    };

    // Build query params (dateRanges[N] format - her iki endpoint için ortak)
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
            } else if (f.type === 'evaluationType') {
                params.push('evaluationType=' + f.value);
            }
        });

        projectIds.forEach(function(id) { params.push('projectIds=' + id); });
        return params.length > 0 ? '?' + params.join('&') : '';
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

    // Summary computed values (from evaluations)
    self.totalEvaluations = ko.computed(function() {
        return self.evaluations().length;
    });

    self.averageScore = ko.computed(function() {
        var evals = self.evaluations();
        if (evals.length === 0) return 0;
        var sum = evals.reduce(function(acc, e) {
            return acc + (e.scorePercentage || 0);
        }, 0);
        return sum / evals.length;
    });

    self.thisMonthScore = ko.computed(function() {
        var now = new Date();
        var thisMonth = now.getMonth();
        var thisYear = now.getFullYear();

        var monthEvals = self.evaluations().filter(function(e) {
            if (!e.completedAt) return false;
            var date = new Date(e.completedAt);
            return date.getMonth() === thisMonth && date.getFullYear() === thisYear;
        });

        if (monthEvals.length === 0) return 0;
        var sum = monthEvals.reduce(function(acc, e) {
            return acc + (e.scorePercentage || 0);
        }, 0);
        return sum / monthEvals.length;
    });

    self.totalYellowCards = ko.computed(function() {
        return self.evaluations().reduce(function(acc, e) {
            return acc + (e.yellowCardCount || 0);
        }, 0);
    });

    self.totalRedCards = ko.computed(function() {
        return self.evaluations().reduce(function(acc, e) {
            return acc + (e.redCardCount || 0);
        }, 0);
    });

    // Filtered evaluations (search)
    self.filteredEvaluations = ko.computed(function() {
        var search = (self.searchText() || '').toLowerCase();
        var evals = self.evaluations();

        if (search) {
            evals = evals.filter(function(e) {
                return (e.callId && e.callId.toLowerCase().indexOf(search) >= 0) ||
                       (e.projectName && e.projectName.toLowerCase().indexOf(search) >= 0) ||
                       (e.checklistName && e.checklistName.toLowerCase().indexOf(search) >= 0);
            });
        }

        // Apply pagination
        var start = (self.currentPage() - 1) * self.pageSize();
        return evals.slice(start, start + self.pageSize());
    });

    // Pagination computed
    self.totalPages = ko.computed(function() {
        var search = (self.searchText() || '').toLowerCase();
        var evals = self.evaluations();

        if (search) {
            evals = evals.filter(function(e) {
                return (e.callId && e.callId.toLowerCase().indexOf(search) >= 0) ||
                       (e.projectName && e.projectName.toLowerCase().indexOf(search) >= 0) ||
                       (e.checklistName && e.checklistName.toLowerCase().indexOf(search) >= 0);
            });
        }

        return Math.ceil(evals.length / self.pageSize()) || 1;
    });

    self.visiblePages = ko.computed(function() {
        var total = self.totalPages();
        var current = self.currentPage();
        var pages = [];

        var start = Math.max(1, current - 2);
        var end = Math.min(total, current + 2);

        for (var i = start; i <= end; i++) {
            pages.push(i);
        }

        return pages;
    });

    // Load evaluations
    self.loadEvaluations = function() {
        self.currentPage(1);

        fetch('/api/evaluations/my-evaluations' + self.buildQueryParams(), {
            method: 'GET',
            credentials: 'include'
        })
        .then(function(response) {
            if (!response.ok) {
                throw new Error('Network response was not ok');
            }
            return response.json();
        })
        .then(function(data) {
            self.evaluations(data || []);
        })
        .catch(function(error) {
            console.error('Error loading evaluations:', error);
            self.evaluations([]);
        });
    };

    // Load report card
    self.loadReportCard = function() {
        var url = '/api/customer/portal/reports/my-report-card' + self.buildQueryParams();

        fetch(url, {
            method: 'GET',
            credentials: 'include'
        })
        .then(function(response) {
            if (!response.ok) {
                if (response.status === 404) {
                    // No data yet, that's ok
                    return null;
                }
                throw new Error('Network response was not ok');
            }
            return response.json();
        })
        .then(function(data) {
            self.reportCard(data);
        })
        .catch(function(error) {
            console.error('Error loading report card:', error);
            self.reportCard(null);
        });
    };

    // Load all data
    self.loadAll = function() {
        self.isLoading(true);

        var queryParams = self.buildQueryParams();

        Promise.all([
            fetch('/api/evaluations/my-evaluations' + queryParams, { credentials: 'include' }).then(function(r) { return r.ok ? r.json() : []; }),
            fetch('/api/customer/portal/reports/my-report-card' + queryParams, { credentials: 'include' }).then(function(r) { return r.ok ? r.json() : null; })
        ])
        .then(function(results) {
            self.evaluations(results[0] || []);
            self.reportCard(results[1]);
        })
        .catch(function(error) {
            console.error('Error loading data:', error);
            toastr.error('Veriler yuklenirken bir hata olustu.');
        })
        .finally(function() {
            self.isLoading(false);
        });
    };

    // Show detail modal
    self.showDetail = function(evaluation) {
        self.isDetailsModalOpen(true);
        self.isDetailsLoading(true);
        self.detailsData(null);

        customerApiFetch('/api/customer/portal/evaluations/' + evaluation.id)
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
        var filename = 'Dinleme_Detay_' + new Date().toISOString().slice(0, 10) + '.xlsx';

        customerApiDownloadGet('/api/customer/portal/evaluations/' + data.evaluationId + '/export', filename)
            .then(function() { toastr.success('Excel dosyası indirildi'); })
            .catch(function(error) { console.error('Error exporting:', error); toastr.error('Excel oluşturulurken hata oluştu'); })
            .finally(function() { self.isExportingDetail(false); });
    };

    // Helper: Get score badge class
    self.getScoreBadgeClass = function(score, projectTypeId) {
        return ScoreThresholds.getScoreBadgeClass(score, projectTypeId);
    };

    // Helper: Get score text class
    self.getScoreTextClass = function(score, projectTypeId) {
        return ScoreThresholds.getScoreClass(score, projectTypeId);
    };

    // Helper: Get score class (for detail modal)
    self.getScoreClass = function(score, projectTypeId) {
        return ScoreThresholds.getScoreClass(score, projectTypeId);
    };

    // Helper: Get progress bar class (for detail modal)
    self.getProgressBarClass = function(score, projectTypeId) {
        return ScoreThresholds.getProgressBarClass(score, projectTypeId);
    };

    // Helper: Copy to clipboard
    self.copyToClipboard = function(text) {
        if (!text) return;

        if (navigator.clipboard && window.isSecureContext) {
            navigator.clipboard.writeText(text).then(function() {
                toastr.success('Panoya kopyalandi');
            }).catch(function() {
                self.fallbackCopy(text);
            });
        } else {
            self.fallbackCopy(text);
        }
    };

    self.fallbackCopy = function(text) {
        var textarea = document.createElement('textarea');
        textarea.value = text;
        document.body.appendChild(textarea);
        textarea.select();
        document.execCommand('copy');
        document.body.removeChild(textarea);
        toastr.success('Panoya kopyalandi');
    };

    // Dosya adı için TR karakter temizleme
    function sanitizeName(name) {
        if (!name) return '';
        var map = { 'ç': 'c', 'ğ': 'g', 'ı': 'i', 'ö': 'o', 'ş': 's', 'ü': 'u', 'Ç': 'C', 'Ğ': 'G', 'İ': 'I', 'Ö': 'O', 'Ş': 'S', 'Ü': 'U' };
        return name.replace(/[çğıöşüÇĞİÖŞÜ]/g, function(c) { return map[c] || c; }).replace(/[^a-zA-Z0-9_-]/g, '_');
    }

    function getExportFileName(ext) {
        var name = self.reportCard() ? sanitizeName(self.reportCard().personnelName) : '';
        var date = new Date().toISOString().slice(0, 10);
        return 'Karne-' + name + '_' + date + '.' + ext;
    }

    // Export to Excel
    self.exportToExcel = function() {
        self.isExporting(true);
        var url = '/api/customer/portal/reports/my-report-card/export' + self.buildQueryParams();

        fetch(url, { credentials: 'include' })
            .then(function(response) {
                if (!response.ok) throw new Error('Export failed');
                return response.blob();
            })
            .then(function(blob) {
                var downloadUrl = window.URL.createObjectURL(blob);
                var a = document.createElement('a');
                a.href = downloadUrl;
                a.download = getExportFileName('xlsx');
                document.body.appendChild(a);
                a.click();
                document.body.removeChild(a);
                window.URL.revokeObjectURL(downloadUrl);
            })
            .catch(function(error) {
                console.error('Export error:', error);
                toastr.error('Excel dosyasi olusturulurken bir hata olustu.');
            })
            .finally(function() {
                self.isExporting(false);
            });
    };

    // Export to Word
    self.exportToWord = function() {
        self.isExporting(true);
        var url = '/api/customer/portal/reports/my-report-card/export-word' + self.buildQueryParams();

        fetch(url, { credentials: 'include' })
            .then(function(response) {
                if (!response.ok) throw new Error('Export failed');
                return response.blob();
            })
            .then(function(blob) {
                var downloadUrl = window.URL.createObjectURL(blob);
                var a = document.createElement('a');
                a.href = downloadUrl;
                a.download = getExportFileName('docx');
                document.body.appendChild(a);
                a.click();
                document.body.removeChild(a);
                window.URL.revokeObjectURL(downloadUrl);
            })
            .catch(function(error) {
                console.error('Export error:', error);
                toastr.error('Word dosyasi olusturulurken bir hata olustu.');
            })
            .finally(function() {
                self.isExporting(false);
            });
    };

    // Export to PDF
    self.exportToPdf = function() {
        self.isExporting(true);
        var url = '/api/customer/portal/reports/my-report-card/export-pdf' + self.buildQueryParams();

        fetch(url, { credentials: 'include' })
            .then(function(response) {
                if (!response.ok) throw new Error('Export failed');
                return response.blob();
            })
            .then(function(blob) {
                var downloadUrl = window.URL.createObjectURL(blob);
                var a = document.createElement('a');
                a.href = downloadUrl;
                a.download = getExportFileName('pdf');
                document.body.appendChild(a);
                a.click();
                document.body.removeChild(a);
                window.URL.revokeObjectURL(downloadUrl);
            })
            .catch(function(error) {
                console.error('Export error:', error);
                toastr.error('PDF dosyasi olusturulurken bir hata olustu.');
            })
            .finally(function() {
                self.isExporting(false);
            });
    };

    // Initialize
    self.loadProjects();
    self.loadAll();
}

// Apply bindings when document is ready
$(document).ready(function() {
    ko.applyBindings(new MyPerformanceViewModel(), document.getElementById('my-performance-app'));
});
