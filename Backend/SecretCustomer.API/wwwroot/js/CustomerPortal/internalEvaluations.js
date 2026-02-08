function CustomerInternalEvaluationsViewModel() {
    var self = this;

    // State
    self.isLoading = ko.observable(true);
    self.evaluations = ko.observableArray([]);
    self.total = ko.observable(0);
    self.averageScore = ko.observable(0);
    self.page = ko.observable(1);
    self.pageSize = ko.observable(20);
    self.currentUserRole = ko.observable('');

    self.isManager = ko.computed(function() {
        return self.currentUserRole() === 'CustomerManager' || self.currentUserRole() === 'Admin';
    });

    // Filter data
    self.projects = ko.observableArray([]);
    self.organizations = ko.observableArray([]);

    // Filter UI
    self.selectedFilterType = ko.observable('');
    self.tempFilter = {
        projectId: ko.observable(''),
        evaluatorName: ko.observable(''),
        personnelName: ko.observable(''),
        organizationId: ko.observable(''),
        callId: ko.observable(''),
        startDate: ko.observable(''),
        endDate: ko.observable(''),
        dateRangeType: ko.observable('')
    };

    // Active filters
    self.activeFilters = ko.observableArray([]);

    // Saved filters
    self.savedFilters = ko.observableArray([]);
    self.showSaveFilterModal = ko.observable(false);
    self.showLoadFilterModal = ko.observable(false);
    self.saveFilterName = ko.observable('');
    self.isSavingFilter = ko.observable(false);
    self.isLoadingFilters = ko.observable(false);

    // Details modal
    self.isDetailsModalOpen = ko.observable(false);
    self.isDetailsLoading = ko.observable(false);
    self.detailsData = ko.observable(null);
    self.isExportingDetail = ko.observable(false);

    // Attachments modal
    self.isAttachmentsModalOpen = ko.observable(false);
    self.isAttachmentsLoading = ko.observable(false);
    self.attachments = ko.observableArray([]);

    // Report export
    self.isExporting = ko.observable(false);

    self.totalPages = ko.computed(function() {
        return Math.ceil(self.total() / self.pageSize()) || 1;
    });

    self.canAddFilter = ko.computed(function() {
        var type = self.selectedFilterType();
        if (!type) return false;
        if (type === 'project') return self.tempFilter.projectId();
        if (type === 'evaluator') return self.tempFilter.evaluatorName();
        if (type === 'personnel') return self.tempFilter.personnelName();
        if (type === 'organization') return self.tempFilter.organizationId();
        if (type === 'callId') return self.tempFilter.callId();
        if (type === 'dateRange') return self.tempFilter.startDate() || self.tempFilter.endDate() || self.tempFilter.dateRangeType();
        return false;
    });

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

    // Date range labels
    self.dateRangeLabels = {
        'today': 'Bugün',
        'yesterday': 'Dün',
        'thisWeek': 'Bu Hafta',
        'lastWeek': 'Geçen Hafta',
        'thisMonth': 'Bu Ay',
        'lastMonth': 'Geçen Ay',
        'last3Months': 'Son 3 Ay',
        'last6Months': 'Son 6 Ay',
        'thisYear': 'Bu Yıl',
        'lastYear': 'Geçen Yıl'
    };

    // Calculate date range from type
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
            var lastWeekEnd = new Date(today);
            lastWeekEnd.setDate(diff - 1);
            var lastWeekStart = new Date(lastWeekEnd);
            lastWeekStart.setDate(lastWeekEnd.getDate() - 6);
            start = lastWeekStart.toISOString().split('T')[0];
            end = lastWeekEnd.toISOString().split('T')[0];
        } else if (rangeType === 'thisMonth') {
            start = new Date(today.getFullYear(), today.getMonth(), 1).toISOString().split('T')[0];
            end = today.toISOString().split('T')[0];
        } else if (rangeType === 'lastMonth') {
            start = new Date(today.getFullYear(), today.getMonth() - 1, 1).toISOString().split('T')[0];
            end = new Date(today.getFullYear(), today.getMonth(), 0).toISOString().split('T')[0];
        } else if (rangeType === 'last3Months') {
            start = new Date(today.getFullYear(), today.getMonth() - 2, 1).toISOString().split('T')[0];
            end = today.toISOString().split('T')[0];
        } else if (rangeType === 'last6Months') {
            start = new Date(today.getFullYear(), today.getMonth() - 5, 1).toISOString().split('T')[0];
            end = today.toISOString().split('T')[0];
        } else if (rangeType === 'thisYear') {
            start = new Date(today.getFullYear(), 0, 1).toISOString().split('T')[0];
            end = today.toISOString().split('T')[0];
        } else if (rangeType === 'lastYear') {
            start = new Date(today.getFullYear() - 1, 0, 1).toISOString().split('T')[0];
            end = new Date(today.getFullYear() - 1, 11, 31).toISOString().split('T')[0];
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

    // Quick date range filter - direkt uygula ve ara (hızlı erişim butonları için)
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

    // Quick CallId search
    self.quickCallId = ko.observable('');
    self.searchByCallId = function() {
        var callId = self.quickCallId().trim();
        if (!callId) return;

        // Mevcut callId filtresini kaldır
        self.activeFilters.remove(function(f) { return f.type === 'callId'; });

        self.activeFilters.push({
            type: 'callId',
            value: callId,
            label: 'Çağrı ID',
            displayValue: callId
        });

        self.quickCallId('');
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
            displayValue = project ? (project.code ? project.name + ' (' + project.code + ')' : project.name) : filter.value;
        } else if (type === 'evaluator') {
            filter.value = self.tempFilter.evaluatorName();
            label = 'Dinleyen';
            displayValue = filter.value;
        } else if (type === 'personnel') {
            filter.value = self.tempFilter.personnelName();
            label = 'Dinlenen';
            displayValue = filter.value;
        } else if (type === 'organization') {
            filter.value = self.tempFilter.organizationId();
            var org = self.organizations().find(function(o) { return o.id == filter.value; });
            label = 'Organizasyon';
            displayValue = org ? org.name : filter.value;
        } else if (type === 'callId') {
            filter.value = self.tempFilter.callId();
            label = 'Çağrı ID';
            displayValue = filter.value;
        } else if (type === 'dateRange') {
            filter.dateRangeType = self.tempFilter.dateRangeType();
            filter.startDate = self.tempFilter.startDate();
            filter.endDate = self.tempFilter.endDate();
            label = 'Tarih';
            // If it's a named range, show the name; otherwise show dates
            if (filter.dateRangeType && self.dateRangeLabels[filter.dateRangeType]) {
                displayValue = self.dateRangeLabels[filter.dateRangeType];
            } else {
                displayValue = (filter.startDate || '...') + ' - ' + (filter.endDate || '...');
            }
        }

        // Tüm filtre tipleri çoklu değer destekler (aynı tipten birden fazla eklenebilir)
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
        self.tempFilter.evaluatorName('');
        self.tempFilter.personnelName('');
        self.tempFilter.organizationId('');
        self.tempFilter.callId('');
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
        self.loadEvaluations(1);
    };

    // Build query params from active filters (çoklu değer desteği)
    self.buildQueryParams = function() {
        var params = {};

        // Çoklu değer için array'ler
        var projectIds = [];
        var evaluatorNames = [];
        var personnelNames = [];
        var organizationIds = [];
        var callIds = [];
        var startDate = null;
        var endDate = null;

        self.activeFilters().forEach(function(f) {
            switch (f.type) {
                case 'project':
                    projectIds.push(f.value);
                    break;
                case 'evaluator':
                    evaluatorNames.push(f.value);
                    break;
                case 'personnel':
                    personnelNames.push(f.value);
                    break;
                case 'organization':
                    organizationIds.push(f.value);
                    break;
                case 'callId':
                    callIds.push(f.value);
                    break;
                case 'dateRange':
                    // If it's a named range type, recalculate dates dynamically
                    if (f.dateRangeType && self.dateRangeLabels[f.dateRangeType]) {
                        var range = self.calculateDateRange(f.dateRangeType);
                        startDate = range.start;
                        endDate = range.end;
                    } else {
                        if (f.startDate) startDate = f.startDate;
                        if (f.endDate) endDate = f.endDate;
                    }
                    break;
            }
        });

        // Array'leri params'a ekle - HER ZAMAN ÇOĞUL KULLAN
        if (projectIds.length > 0) params.projectIds = projectIds;
        if (evaluatorNames.length > 0) params.evaluatorNames = evaluatorNames;
        if (personnelNames.length > 0) params.personnelNames = personnelNames;
        if (organizationIds.length > 0) params.organizationIds = organizationIds;
        if (callIds.length > 0) params.callIds = callIds;

        if (startDate) params.startDate = startDate;
        if (endDate) params.endDate = endDate;

        return params;
    };

    // Load evaluations
    self.loadEvaluations = function(page) {
        self.isLoading(true);
        page = page || 1;

        var params = self.buildQueryParams();
        var url = '/api/customer/portal/evaluations/internal?page=' + page + '&pageSize=' + self.pageSize();

        Object.keys(params).forEach(function(key) {
            var value = params[key];
            if (Array.isArray(value)) {
                value.forEach(function(v) {
                    url += '&' + key + '=' + encodeURIComponent(v);
                });
            } else {
                url += '&' + key + '=' + encodeURIComponent(value);
            }
        });

        customerApiFetch(url)
            .then(function(response) {
                if (!response.ok) throw new Error('Dinlemeler yuklenemedi');
                return response.json();
            })
            .then(function(data) {
                self.evaluations(data.items || []);
                self.total(data.total || 0);
                self.averageScore(data.averageScore || 0);
                self.page(data.page || 1);
                self.isLoading(false);
            })
            .catch(function(error) {
                console.error('Internal evaluations load error:', error);
                self.isLoading(false);
            });
    };

    // Load filter options
    self.loadFilterOptions = function() {
        // Load projects (sadece CallAuditing = 2)
        customerApiFetch('/api/customer/portal/projects?projectTypeId=2')
            .then(function(response) { return response.json(); })
            .then(function(data) {
                self.projects(data || []);
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
            });
    };

    // Saved Filters
    self.openSaveFilterModal = function() {
        self.saveFilterName('');
        self.showSaveFilterModal(true);
    };

    self.closeSaveFilterModal = function() {
        self.showSaveFilterModal(false);
    };

    self.saveFilter = function() {
        if (!self.saveFilterName()) return;
        self.isSavingFilter(true);

        // Prepare filters for saving - store dateRangeType for relative dates
        var filtersToSave = self.activeFilters().map(function(f) {
            return {
                type: f.type,
                value: f.value,
                startDate: f.dateRangeType ? null : f.startDate, // Don't save dates if using range type
                endDate: f.dateRangeType ? null : f.endDate,
                dateRangeType: f.dateRangeType, // Save the range type (thisMonth, lastMonth, etc.)
                label: f.label,
                displayValue: f.displayValue
            };
        });

        var filterData = {
            name: self.saveFilterName(),
            page: '/CustomerPortal/InternalEvaluations',
            filters: filtersToSave
        };

        customerApiFetch('/api/customer/portal/saved-filters', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(filterData)
        })
            .then(function(response) {
                if (!response.ok) throw new Error('Kaydetme hatasi');
                return response.json();
            })
            .then(function() {
                self.closeSaveFilterModal();
                self.loadSavedFilters();
                self.isSavingFilter(false);
            })
            .catch(function(error) {
                console.error('Filter save error:', error);
                self.isSavingFilter(false);
            });
    };

    self.openLoadFilterModal = function() {
        self.loadSavedFilters();
        self.showLoadFilterModal(true);
    };

    self.closeLoadFilterModal = function() {
        self.showLoadFilterModal(false);
    };

    self.loadSavedFilters = function() {
        self.isLoadingFilters(true);
        customerApiFetch('/api/customer/portal/saved-filters?page=/CustomerPortal/InternalEvaluations')
            .then(function(response) { return response.json(); })
            .then(function(data) {
                self.savedFilters(data || []);
                self.isLoadingFilters(false);
            })
            .catch(function() {
                self.isLoadingFilters(false);
            });
    };

    self.applySavedFilter = function(savedFilter) {
        self.activeFilters.removeAll();
        (savedFilter.filters || []).forEach(function(f) {
            // Rebuild display value
            var displayValue = f.displayValue;
            var startDate = f.startDate;
            var endDate = f.endDate;

            if (f.type === 'project') {
                var project = self.projects().find(function(p) { return p.id == f.value; });
                if (project) displayValue = project.code ? project.name + ' (' + project.code + ')' : project.name;
            } else if (f.type === 'organization') {
                var org = self.organizations().find(function(o) { return o.id == f.value; });
                if (org) displayValue = org.name;
            } else if (f.type === 'dateRange' && f.dateRangeType) {
                // Recalculate dates for relative ranges
                var range = self.calculateDateRange(f.dateRangeType);
                startDate = range.start;
                endDate = range.end;
                displayValue = self.dateRangeLabels[f.dateRangeType] || displayValue;
            }

            self.activeFilters.push({
                type: f.type,
                value: f.value,
                startDate: startDate,
                endDate: endDate,
                dateRangeType: f.dateRangeType,
                label: f.label,
                displayValue: displayValue
            });
        });
        self.closeLoadFilterModal();
        self.search();
    };

    self.deleteSavedFilter = function(savedFilter) {
        showConfirmModal({
            title: T('Common.Delete', 'Sil'),
            message: T('Filter.ConfirmDelete', 'Bu filtreyi silmek istediğinize emin misiniz?'),
            confirmText: T('Common.Delete', 'Sil'),
            confirmClass: 'btn-danger',
            onConfirm: function() {
                customerApiFetch('/api/customer/portal/saved-filters/' + savedFilter.id, { method: 'DELETE' })
                    .then(function() {
                        self.loadSavedFilters();
                    });
            }
        });
    };

    // Details Modal Functions
    self.detailsError = ko.observable('');

    self.showDetails = function(evaluation) {
        self.isDetailsModalOpen(true);
        self.isDetailsLoading(true);
        self.detailsData(null);
        self.detailsError('');

        customerApiFetch('/api/customer/portal/evaluations/' + evaluation.id)
            .then(function(response) {
                if (!response.ok) {
                    return response.json().then(function(err) {
                        throw new Error(err.message || 'Detay yüklenemedi');
                    }).catch(function() {
                        throw new Error('Detay yüklenemedi');
                    });
                }
                return response.json();
            })
            .then(function(data) {
                self.detailsData(data);
            })
            .catch(function(error) {
                console.error('Details load error:', error);
                self.detailsError(error.message || 'Değerlendirme detayı yüklenirken hata oluştu.');
            })
            .finally(function() {
                self.isDetailsLoading(false);
            });
    };

    self.closeDetailsModal = function() {
        self.isDetailsModalOpen(false);
        self.detailsData(null);
    };

    self.exportToExcel = function() {
        var data = self.detailsData();
        if (!data) return;

        var evalId = data.evaluationId || data.id;
        if (!evalId) {
            toastr.error('Değerlendirme ID bulunamadı');
            return;
        }

        self.isExportingDetail(true);
        var filename = 'Dinleme_Detay_' + self.getTimestamp() + '.xlsx';

        customerApiDownloadGet('/api/customer/portal/evaluations/' + evalId + '/export', filename)
            .then(function() { toastr.success('Excel dosyası indirildi'); })
            .catch(function(error) { console.error('Error exporting:', error); toastr.error('Excel oluşturulurken hata oluştu'); })
            .finally(function() { self.isExportingDetail(false); });
    };

    // Attachments Modal Functions
    self.showAttachments = function(evaluation) {
        self.isAttachmentsModalOpen(true);
        self.isAttachmentsLoading(true);
        self.attachments([]);

        customerApiFetch('/api/customer/portal/evaluations/' + evaluation.id + '/attachments')
            .then(function(response) {
                if (!response.ok) throw new Error('Dosyalar yuklenemedi');
                return response.json();
            })
            .then(function(data) {
                self.attachments(data || []);
            })
            .catch(function(error) {
                console.error('Attachments load error:', error);
            })
            .finally(function() {
                self.isAttachmentsLoading(false);
            });
    };

    self.closeAttachmentsModal = function() {
        self.isAttachmentsModalOpen(false);
        self.attachments([]);
    };

    self.downloadAttachment = function(attachment) {
        window.open('/api/answers/' + attachment.answerId + '/attachment', '_blank');
    };

    // Copy to clipboard (with fallback for HTTP)
    self.copyToClipboard = function(text) {
        if (!text) return;

        // Modern API (HTTPS only)
        if (navigator.clipboard && window.isSecureContext) {
            navigator.clipboard.writeText(text).then(function() {
                toastr.success('Kopyalandı');
            }).catch(function(err) {
                console.error('Copy failed:', err);
                self.fallbackCopyToClipboard(text);
            });
        } else {
            // Fallback for HTTP
            self.fallbackCopyToClipboard(text);
        }
    };

    self.fallbackCopyToClipboard = function(text) {
        var textArea = document.createElement('textarea');
        textArea.value = text;
        textArea.style.position = 'fixed';
        textArea.style.left = '-9999px';
        document.body.appendChild(textArea);
        textArea.focus();
        textArea.select();
        try {
            document.execCommand('copy');
            toastr.success('Kopyalandı');
        } catch (err) {
            console.error('Fallback copy failed:', err);
            toastr.error('Kopyalama başarısız');
        }
        document.body.removeChild(textArea);
    };

    // Export Reports
    self.buildExportFilter = function() {
        var params = self.buildQueryParams();
        return {
            projectId: params.projectId ? parseInt(params.projectId) : null,
            organizationId: params.organizationId ? parseInt(params.organizationId) : null,
            startDate: params.startDate || null,
            endDate: params.endDate || null,
            evaluationSources: ['internal'] // İç dinlemeler
        };
    };

    self.getTimestamp = function() {
        return new Date().toISOString().slice(0, 10).replace(/-/g, '');
    };

    self.exportQuestionGroupAverageReport = function() {
        self.isExporting(true);
        var filename = 'Soru_Grubu_Ortalama_Raporu_' + self.getTimestamp() + '.xlsx';
        customerApiDownloadPost('/api/customer/portal/reports/export/question-group-average', self.buildExportFilter(), filename)
            .then(function() { toastr.success('Rapor indirildi'); })
            .catch(function(error) { console.error('Error exporting report:', error); toastr.error('Rapor oluşturulurken hata oluştu'); })
            .finally(function() { self.isExporting(false); });
    };

    self.exportCustomerEvaluationReport = function() {
        self.isExporting(true);
        var filename = 'Musteri_Degerlendirme_Raporu_' + self.getTimestamp() + '.xlsx';
        customerApiDownloadPost('/api/customer/portal/reports/export/customer-evaluation', self.buildExportFilter(), filename)
            .then(function() { toastr.success('Rapor indirildi'); })
            .catch(function(error) { console.error('Error exporting report:', error); toastr.error('Rapor oluşturulurken hata oluştu'); })
            .finally(function() { self.isExporting(false); });
    };

    self.exportProjectPerformanceReport = function() {
        self.isExporting(true);
        var filename = 'Proje_Performans_Raporu_' + self.getTimestamp() + '.xlsx';
        customerApiDownloadPost('/api/customer/portal/reports/export/project-performance', self.buildExportFilter(), filename)
            .then(function() { toastr.success('Rapor indirildi'); })
            .catch(function(error) { console.error('Error exporting report:', error); toastr.error('Rapor oluşturulurken hata oluştu'); })
            .finally(function() { self.isExporting(false); });
    };

    // ========================
    // DELETE DRAFT
    // ========================
    self.deleteDraft = function(evaluation) {
        if (evaluation.status !== 'Draft') {
            toastr.error('Sadece taslak durumundaki değerlendirmeler silinebilir.');
            return;
        }

        if (!self.isManager()) {
            toastr.error('Bu işlem için yetkiniz bulunmamaktadır.');
            return;
        }

        showDeleteConfirm('Taslak Değerlendirme', function() {
            customerApiFetch('/api/customer/portal/evaluations/internal/' + evaluation.id, {
                method: 'DELETE'
            })
            .then(function(response) {
                if (!response.ok) {
                    return response.json().then(function(data) {
                        throw new Error(data.message || 'Silme işlemi başarısız');
                    });
                }
                return response.json();
            })
            .then(function() {
                self.evaluations.remove(evaluation);
                toastr.success('Taslak başarıyla silindi.');
            })
            .catch(function(error) {
                console.error('Delete error:', error);
                toastr.error(error.message || 'Taslak silinirken bir hata oluştu.');
            });
        });
    };

    // Load current user role
    self.loadCurrentUserRole = function() {
        fetch('/api/auth/me', { credentials: 'include' })
            .then(function(response) { return response.json(); })
            .then(function(data) {
                if (data && data.role) {
                    self.currentUserRole(data.role);
                }
            })
            .catch(function(error) {
                console.error('Error loading user role:', error);
            });
    };

    // Init
    self.loadCurrentUserRole();
    self.loadFilterOptions();
    self.loadEvaluations(1);
}

$(document).ready(function() {
    ko.applyBindings(new CustomerInternalEvaluationsViewModel(), document.getElementById('customer-internal-evaluations-app'));
});
