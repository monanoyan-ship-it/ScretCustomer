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

    // Details modal
    self.isDetailsModalOpen = ko.observable(false);
    self.isDetailsLoading = ko.observable(false);
    self.detailsData = ko.observable(null);
    self.isExportingDetail = ko.observable(false);

    // Pagination
    self.currentPage = ko.observable(1);
    self.pageSize = ko.observable(10);

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

        fetch('/api/evaluations/my-evaluations', {
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
        fetch('/api/customer/portal/reports/my-report-card', {
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

        Promise.all([
            fetch('/api/evaluations/my-evaluations', { credentials: 'include' }).then(function(r) { return r.ok ? r.json() : []; }),
            fetch('/api/customer/portal/reports/my-report-card', { credentials: 'include' }).then(function(r) { return r.ok ? r.json() : null; })
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
    self.getScoreBadgeClass = function(score) {
        if (score === null || score === undefined) return 'bg-secondary';
        if (score >= 80) return 'bg-success';
        if (score >= 60) return 'bg-warning text-dark';
        return 'bg-danger';
    };

    // Helper: Get score text class
    self.getScoreTextClass = function(score) {
        if (score === null || score === undefined) return 'text-secondary';
        if (score >= 80) return 'text-success';
        if (score >= 60) return 'text-warning';
        return 'text-danger';
    };

    // Helper: Get score class (for detail modal)
    self.getScoreClass = function(score) {
        if (score >= 80) return 'text-success';
        if (score >= 60) return 'text-warning';
        if (score > 0) return 'text-danger';
        return 'text-muted';
    };

    // Helper: Get progress bar class (for detail modal)
    self.getProgressBarClass = function(score) {
        if (score >= 80) return 'bg-success';
        if (score >= 60) return 'bg-warning';
        if (score > 0) return 'bg-danger';
        return 'bg-secondary';
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
        var url = '/api/customer/portal/reports/my-report-card/export';

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
        var url = '/api/customer/portal/reports/my-report-card/export-word';

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
        var url = '/api/customer/portal/reports/my-report-card/export-pdf';

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
    self.loadAll();
}

// Apply bindings when document is ready
$(document).ready(function() {
    ko.applyBindings(new MyPerformanceViewModel(), document.getElementById('my-performance-app'));
});
