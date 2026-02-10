// Listenings (Dinlemeler) Page ViewModel
function ListeningsViewModel() {
    var self = this;

    // Page name for saved filters (URL path)
    self.pageName = '/Listenings';

    // State
    self.isLoading = ko.observable(false);
    self.isExporting = ko.observable(false);
    self.isLoadingDetail = ko.observable(false);
    self.evaluations = ko.observableArray([]);
    self.totalCount = ko.observable(0);
    self.showDetailModal = ko.observable(false);
    self.selectedEvaluation = ko.observable(null);

    // Reports Modal State
    self.showReportsModal = ko.observable(false);

    // Attachments Modal State
    self.isAttachmentsModalOpen = ko.observable(false);
    self.isAttachmentsLoading = ko.observable(false);
    self.attachments = ko.observableArray([]);

    // Saved Filters State
    self.savedFilters = ko.observableArray([]);
    self.savedFilterSearch = ko.observable('');
    self.showSaveFilterModal = ko.observable(false);
    self.showLoadFilterModal = ko.observable(false);
    self.saveFilterName = ko.observable('');
    self.saveFilterDescription = ko.observable('');
    self.saveFilterIsDefault = ko.observable(false);
    self.isSavingFilter = ko.observable(false);
    self.isLoadingFilters = ko.observable(false);

    // Filtered saved filters (for search)
    self.filteredSavedFilters = ko.computed(function() {
        var search = (self.savedFilterSearch() || '').toLowerCase().trim();
        var filters = self.savedFilters();

        if (!search) return filters;

        return filters.filter(function(f) {
            return (f.name && f.name.toLowerCase().indexOf(search) > -1) ||
                   (f.description && f.description.toLowerCase().indexOf(search) > -1);
        });
    });

    // Lookup data
    self.customers = ko.observableArray([]);
    self.organizationsForFilter = ko.observableArray([]);
    self.allProjects = ko.observableArray([]);
    self.projectsForFilter = ko.observableArray([]);
    self.evaluators = ko.observableArray([]);
    self.dateRanges = ko.observableArray([]);
    self.evaluationSources = ko.observableArray([]);
    self.projectTypes = ko.observableArray([]);

    // Sorting
    self.sortField = ko.observable('id'); // ID = Primary Key, en hızlı sıralama
    self.sortDirection = ko.observable('desc');

    // Filter system
    self.selectedFilterType = ko.observable('');
    self.activeFilters = ko.observableArray([]);

    // Temp filter values (for adding new filter)
    self.tempFilter = {
        customerIdForOrg: ko.observable(null),
        organizationId: ko.observable(null),
        projectId: ko.observable(null),
        projectType: ko.observable(null),
        evaluatorId: ko.observable(null),
        personnelName: ko.observable(''),
        supervisorName: ko.observable(''),
        callId: ko.observable(''),
        status: ko.observable(''),
        evaluationSource: ko.observable(null),
        startDate: ko.observable(''),
        endDate: ko.observable(''),
        selectedDateRangeType: ko.observable(null) // Seçilen hızlı tarih tipi
    };

    // Tarih manuel değiştirildiğinde dateRangeType'ı temizle
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
    self._manualDateChange = true; // Flag to track manual vs programmatic changes

    // Pagination
    self.page = ko.observable(1);
    self.pageSize = ko.observable(25);

    // Filter labels (for display)
    self.filterLabels = {
        customerOrganization: 'Müşteri/Org.',
        project: 'Proje',
        projectType: 'Proje Tipi',
        evaluator: 'Değerlendiren',
        personnel: 'Temsilci',
        supervisor: 'Yönetici',
        callId: 'Çağrı No',
        status: 'Durum',
        evaluationSource: 'Kaynak',
        callDateRange: 'Çağrı Tarihi',
        dateRange: 'Kayıt Tarihi'
    };

    self.statusLabels = {
        'Completed': 'Tamamlandı',
        'InProgress': 'Devam Ediyor',
        'Draft': 'Taslak'
    };

    // Computed
    self.totalPages = ko.computed(function() {
        return Math.ceil(self.totalCount() / self.pageSize()) || 1;
    });

    self.visiblePages = ko.computed(function() {
        var current = self.page();
        var total = self.totalPages();
        var pages = [];
        var start = Math.max(1, current - 2);
        var end = Math.min(total, current + 2);
        for (var i = start; i <= end; i++) {
            pages.push(i);
        }
        return pages;
    });

    self.canAddFilter = ko.computed(function() {
        var type = self.selectedFilterType();
        if (!type) return false;

        switch (type) {
            case 'customerOrganization': return self.tempFilter.customerIdForOrg();
            case 'project': return self.tempFilter.projectId();
            case 'projectType': return self.tempFilter.projectType();
            case 'evaluator': return self.tempFilter.evaluatorId();
            case 'personnel': return self.tempFilter.personnelName().trim() !== '';
            case 'supervisor': return self.tempFilter.supervisorName().trim() !== '';
            case 'callId': return self.tempFilter.callId().trim() !== '';
            case 'status': return self.tempFilter.status() !== '';
            case 'evaluationSource': return self.tempFilter.evaluationSource();
            case 'callDateRange': return self.tempFilter.startDate() || self.tempFilter.endDate();
            case 'dateRange': return self.tempFilter.startDate() || self.tempFilter.endDate();
            default: return false;
        }
    });

    // Init
    self.init = function() {
        self.isLoading(true); // Sayfa açılır açılmaz loading göster
        self.loadLookups();
    };

    // Load lookup data
    self.loadLookups = function() {
        // EnumsService ve lookups'ı paralel yükle
        Promise.all([
            EnumsService.load().catch(function(err) { console.warn('EnumsService load failed:', err); return null; }),
            ApiService.get('/reports/lookups')
        ])
        .then(function(results) {
            var enumsData = results[0];
            var response = results[1];

            self.customers(response.customers || []);
            self.allProjects(response.projects || []);
            self.projectsForFilter(response.projects || []);
            self.evaluators(response.evaluators || []);
            self.dateRanges(response.dateRanges || []);
            self.evaluationSources(response.evaluationSources || []);

            // Proje tiplerini EnumsService'den al
            if (enumsData && enumsData.projectTypes) {
                self.projectTypes(enumsData.projectTypes);
            }

            // Lookups yüklendikten sonra varsayılan filtreyi kontrol et
            self.loadDefaultFilterAndSearch();
        })
        .catch(function(error) {
            console.error('Error loading lookups:', error);
            self.search(); // Hata olsa bile arama yap
        });
    };

    // Load default filter (if any) and search
    self.loadDefaultFilterAndSearch = function() {
        ApiService.get('/saved-filters?pageName=' + self.pageName)
            .then(function(response) {
                // isDefault'u observable yap
                var filters = (response || []).map(function(f) {
                    var isDefaultVal = f.isDefault;
                    f.isDefault = ko.observable(isDefaultVal);
                    return f;
                });
                self.savedFilters(filters);

                var defaultFilter = filters.find(function(f) { return f.isDefault(); });
                if (defaultFilter) {
                    self.applySavedFilter(defaultFilter);
                } else {
                    // Varsayılan filtre yoksa direkt arama yap (filtre olmadan)
                    self.search();
                }
            })
            .catch(function(error) {
                console.error('Error loading saved filters:', error);
                self.search();
            });
    };

    // Load organizations for filter dropdown and filter projects by customer
    self.loadOrganizationsForFilter = function() {
        var customerId = self.tempFilter.customerIdForOrg();
        if (!customerId) {
            self.organizationsForFilter([]);
            self.projectsForFilter(self.allProjects());
            return;
        }

        // Filter projects by customer
        var filteredProjects = self.allProjects().filter(function(p) {
            return p.customerId === customerId;
        });
        self.projectsForFilter(filteredProjects);

        // Remove incompatible filters when customer changes
        self.removeIncompatibleFilters(customerId);

        // Load organizations
        ApiService.get('/reports/organizations/' + customerId)
            .then(function(response) {
                self.organizationsForFilter(response || []);
            })
            .catch(function(error) {
                console.error('Error loading organizations:', error);
            });
    };

    // Remove filters that don't belong to selected customer
    self.removeIncompatibleFilters = function(customerId) {
        var filtersToRemove = [];

        self.activeFilters().forEach(function(filter) {
            switch (filter.type) {
                case 'project':
                    var project = self.allProjects().find(function(p) { return p.id === filter.value; });
                    if (project && project.customerId && project.customerId !== customerId) {
                        filtersToRemove.push(filter);
                    }
                    break;
                // Buraya ileride başka filtre tipleri eklenebilir
            }
        });

        filtersToRemove.forEach(function(filter) {
            self.activeFilters.remove(filter);
            toastr.info(filter.displayValue + ' filtresi kaldırıldı (farklı müşteriye ait)');
        });
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
            case 'customerOrganization':
                var coCustomerId = self.tempFilter.customerIdForOrg();
                var coCustomer = self.customers().find(function(c) { return c.id === coCustomerId; });
                if (!coCustomer) return;

                var coOrgId = self.tempFilter.organizationId();
                var coOrg = coOrgId ? self.organizationsForFilter().find(function(o) { return o.id === coOrgId; }) : null;

                filter.value = { customerId: coCustomerId, organizationId: coOrgId };
                filter.displayValue = coOrg ? coCustomer.companyName + ' / ' + coOrg.name : coCustomer.companyName;

                self.tempFilter.customerIdForOrg(null);
                self.tempFilter.organizationId(null);
                self.organizationsForFilter([]);
                break;

            case 'project':
                var projectId = self.tempFilter.projectId();
                var project = self.allProjects().find(function(p) { return p.id === projectId; });
                if (!project) return;
                filter.value = projectId;
                filter.displayValue = project.name;
                self.tempFilter.projectId(null);
                break;

            case 'projectType':
                var projectTypeId = self.tempFilter.projectType();
                var projectType = self.projectTypes().find(function(pt) { return pt.id === projectTypeId; });
                if (!projectType) return;
                filter.value = projectType.systemName;
                filter.displayValue = T(projectType.nameKey, projectType.systemName);
                self.tempFilter.projectType(null);
                break;

            case 'evaluator':
                var evaluatorId = self.tempFilter.evaluatorId();
                var evaluator = self.evaluators().find(function(e) { return e.id === evaluatorId; });
                if (!evaluator) return;
                filter.value = evaluatorId;
                filter.displayValue = evaluator.name;
                self.tempFilter.evaluatorId(null);
                break;

            case 'personnel':
                var personnelName = self.tempFilter.personnelName().trim();
                if (!personnelName) return;
                filter.value = personnelName;
                filter.displayValue = personnelName;
                self.tempFilter.personnelName('');
                break;

            case 'supervisor':
                var supervisorName = self.tempFilter.supervisorName().trim();
                if (!supervisorName) return;
                filter.value = supervisorName;
                filter.displayValue = supervisorName;
                self.tempFilter.supervisorName('');
                break;

            case 'callId':
                var callId = self.tempFilter.callId().trim();
                if (!callId) return;
                filter.value = callId;
                filter.displayValue = callId;
                self.tempFilter.callId('');
                break;

            case 'status':
                var status = self.tempFilter.status();
                if (!status) return;
                filter.value = status;
                filter.displayValue = self.statusLabels[status] || status;
                self.tempFilter.status('');
                break;

            case 'evaluationSource':
                var sourceId = self.tempFilter.evaluationSource();
                if (!sourceId) return;
                var source = self.evaluationSources().find(function(s) { return s.id === sourceId; });
                if (!source) return;
                filter.value = sourceId;
                filter.displayValue = source.name;
                self.tempFilter.evaluationSource(null);
                break;

            case 'callDateRange':
            case 'dateRange':
                var startDate = self.tempFilter.startDate();
                var endDate = self.tempFilter.endDate();
                var dateRangeType = self.tempFilter.selectedDateRangeType();
                if (!startDate && !endDate) return;

                filter.value = {
                    startDate: startDate,
                    endDate: endDate,
                    dateRangeType: dateRangeType // null ise sabit tarih, değilse dinamik
                };

                // Display value - eğer dateRangeType varsa onun adını göster
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

        // Tüm filtre tipleri çoklu değer destekler (aynı tipten birden fazla eklenebilir)
        self.activeFilters.push(filter);
        self.selectedFilterType('');
        self.search(); // Filtre eklenince otomatik ara
    };

    // Remove filter
    self.removeFilter = function(filter) {
        self.activeFilters.remove(filter);
        self.search(); // Filtre kaldırılınca otomatik ara
    };

    // Clear all filters
    self.clearFilters = function() {
        self.activeFilters([]);
        self.search();
    };

    // Set temp date range (quick select buttons)
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
                startDate = new Date(today.getTime());
                endDate = new Date(today.getTime());
                break;
            case 'yesterday':
                var yesterday = new Date(today.getTime());
                yesterday.setDate(yesterday.getDate() - 1);
                startDate = yesterday;
                endDate = new Date(yesterday.getTime());
                break;
            case 'thisWeek':
                startDate = getMonday(today);
                endDate = new Date(today.getTime());
                break;
            case 'lastWeek':
                var lastWeekEnd = new Date(today.getTime());
                lastWeekEnd.setDate(lastWeekEnd.getDate() - lastWeekEnd.getDay() - (lastWeekEnd.getDay() === 0 ? 0 : 0));
                var lastWeekStart = getMonday(today);
                lastWeekStart.setDate(lastWeekStart.getDate() - 7);
                lastWeekEnd = new Date(lastWeekStart.getTime());
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
            case 'thisQuarter':
                var quarter = Math.floor(today.getMonth() / 3);
                startDate = new Date(today.getFullYear(), quarter * 3, 1);
                endDate = new Date(today.getTime());
                break;
            case 'thisYear':
                startDate = new Date(today.getFullYear(), 0, 1);
                endDate = new Date(today.getTime());
                break;
        }

        // Programmatic change - don't clear dateRangeType
        self._manualDateChange = false;
        if (startDate) self.tempFilter.startDate(formatDate(startDate));
        if (endDate) self.tempFilter.endDate(formatDate(endDate));
        self.tempFilter.selectedDateRangeType(range);
        self._manualDateChange = true;
    };

    // Build params from active filters (çoklu değer desteği)
    self.buildFilterParams = function() {
        var params = {
            page: self.page(),
            pageSize: self.pageSize(),
            sortField: self.sortField(),
            sortDirection: self.sortDirection()
        };

        // Çoklu değer için array'ler
        var customerIds = [];
        var organizationIds = [];
        var projectIds = [];
        var projectTypes = [];
        var evaluatorIds = [];
        var personnelNames = [];
        var supervisorNames = [];
        var callIds = [];
        var statuses = [];
        var evaluationSources = [];
        var dateRanges = [];

        self.activeFilters().forEach(function(filter) {
            switch (filter.type) {
                case 'customerOrganization':
                    if (filter.value.customerId) customerIds.push(filter.value.customerId);
                    if (filter.value.organizationId) organizationIds.push(filter.value.organizationId);
                    break;
                case 'project':
                    projectIds.push(filter.value);
                    break;
                case 'projectType':
                    projectTypes.push(filter.value);
                    break;
                case 'evaluator':
                    evaluatorIds.push(filter.value);
                    break;
                case 'personnel':
                    personnelNames.push(filter.value);
                    break;
                case 'supervisor':
                    supervisorNames.push(filter.value);
                    break;
                case 'callId':
                    callIds.push(filter.value);
                    break;
                case 'status':
                    statuses.push(filter.value);
                    break;
                case 'evaluationSource':
                    evaluationSources.push(filter.value);
                    break;
                case 'callDateRange':
                    dateRanges.push({
                        startDate: filter.value.startDate,
                        endDate: filter.value.endDate,
                        filterType: 'callDate'
                    });
                    break;
                case 'dateRange':
                    dateRanges.push({
                        startDate: filter.value.startDate,
                        endDate: filter.value.endDate,
                        filterType: 'createdAt'
                    });
                    break;
            }
        });

        // Array'leri params'a ekle (boş olmayanları)
        if (customerIds.length > 0) params.customerIds = customerIds;
        if (organizationIds.length > 0) params.organizationIds = organizationIds;
        if (projectIds.length > 0) params.projectIds = projectIds;
        if (projectTypes.length > 0) params.projectTypes = projectTypes;
        if (evaluatorIds.length > 0) params.evaluatorIds = evaluatorIds;
        if (personnelNames.length > 0) params.personnelNames = personnelNames;
        if (supervisorNames.length > 0) params.supervisorNames = supervisorNames;
        if (callIds.length > 0) params.callIds = callIds;
        if (statuses.length > 0) params.statuses = statuses;
        if (evaluationSources.length > 0) params.evaluationSources = evaluationSources;
        if (dateRanges.length > 0) params.dateRanges = dateRanges;

        return params;
    };

    // Search evaluations
    self.search = function() {
        self.page(1);
        self.loadEvaluations();
    };

    // Load evaluations (iki aşamalı: önce veri, sonra count)
    self.loadEvaluations = function() {
        self.isLoading(true);

        var params = self.buildFilterParams();
        params.skipCount = true; // İlk istekte count atla - hızlı yükleme

        ApiService.post('/reports/evaluations', params)
            .then(function(response) {
                self.evaluations(response.items || []);
                // totalCount -1 ise henüz bilinmiyor
                if (response.totalCount >= 0) {
                    self.totalCount(response.totalCount);
                } else {
                    // Background'da count'u al
                    self.loadEvaluationsCount();
                }
            })
            .catch(function(error) {
                console.error('Error loading evaluations:', error);
                toastr.error('Veriler yüklenirken hata oluştu');
            })
            .finally(function() {
                self.isLoading(false);
            });
    };

    // Background'da toplam sayıyı al
    self.loadEvaluationsCount = function() {
        var params = self.buildFilterParams();
        delete params.page;
        delete params.pageSize;
        delete params.sortField;
        delete params.sortDirection;

        ApiService.post('/reports/evaluations/count', params)
            .then(function(response) {
                if (response && response.totalCount !== undefined) {
                    self.totalCount(response.totalCount);
                }
            })
            .catch(function(error) {
                console.error('Error loading count:', error);
                // Count alınamazsa varsayılan değer
                self.totalCount(self.evaluations().length);
            });
    };

    // Sorting
    self.toggleSort = function(field) {
        if (self.sortField() === field) {
            self.sortDirection(self.sortDirection() === 'asc' ? 'desc' : 'asc');
        } else {
            self.sortField(field);
            self.sortDirection('asc');
        }
        self.loadEvaluations();
    };

    self.getSortIcon = function(field) {
        if (self.sortField() !== field) return '';
        return self.sortDirection() === 'asc' ? 'bi-sort-up' : 'bi-sort-down';
    };

    // Pagination
    self.prevPage = function() {
        if (self.page() > 1) {
            self.page(self.page() - 1);
            self.loadEvaluations();
        }
    };

    self.nextPage = function() {
        if (self.page() < self.totalPages()) {
            self.page(self.page() + 1);
            self.loadEvaluations();
        }
    };

    self.goToPage = function(page) {
        self.page(page);
        self.loadEvaluations();
    };

    // View detail
    self.viewDetail = function(evaluation) {
        self.selectedEvaluation(evaluation);
        self.showDetailModal(true);
        self.isLoadingDetail(true);

        ApiService.get('/reports/evaluations/' + evaluation.evaluationId)
            .then(function(response) {
                self.selectedEvaluation(response);
            })
            .catch(function(error) {
                console.error('Error loading evaluation detail:', error);
                toastr.error('Detay yüklenirken hata oluştu');
            })
            .finally(function() {
                self.isLoadingDetail(false);
            });
    };

    self.closeDetailModal = function() {
        self.showDetailModal(false);
        self.selectedEvaluation(null);
    };

    // ==================== REPORTS MODAL ====================

    // Open reports modal
    self.openReportsModal = function() {
        self.showReportsModal(true);
    };

    // Close reports modal
    self.closeReportsModal = function() {
        self.showReportsModal(false);
    };

    // Export to Excel
    self.exportToExcel = function() {
        self.isExporting(true);

        var params = self.buildFilterParams();
        delete params.page;
        delete params.pageSize;

        ApiService.downloadPost('/reports/export/excel', params, 'Dinlemeler.xlsx')
            .then(function() {
                toastr.success('Excel dosyası indirildi');
                self.closeReportsModal();
            })
            .catch(function(error) {
                console.error('Error exporting:', error);
                toastr.error('Excel export sırasında hata oluştu');
            })
            .finally(function() {
                self.isExporting(false);
            });
    };

    // Export Detailed to Excel (with questions/answers)
    self.exportDetailedToExcel = function() {
        self.isExporting(true);

        var params = self.buildFilterParams();
        delete params.page;
        delete params.pageSize;

        ApiService.downloadPost('/reports/export/excel/detailed', params, 'Dinlemeler_Detayli.xlsx')
            .then(function() {
                toastr.success('Detaylı Excel dosyası indirildi');
                self.closeReportsModal();
            })
            .catch(function(error) {
                console.error('Error exporting:', error);
                toastr.error('Detaylı Excel export sırasında hata oluştu');
            })
            .finally(function() {
                self.isExporting(false);
            });
    };

    // Dosya adı için timestamp oluştur (yyyyMMddHHmmss)
    self.getTimestamp = function() {
        var now = new Date();
        var yyyy = now.getFullYear();
        var MM = String(now.getMonth() + 1).padStart(2, '0');
        var dd = String(now.getDate()).padStart(2, '0');
        var HH = String(now.getHours()).padStart(2, '0');
        var mm = String(now.getMinutes()).padStart(2, '0');
        var ss = String(now.getSeconds()).padStart(2, '0');
        return yyyy + MM + dd + HH + mm + ss;
    };

    // Çağrı Denetleme Raporu
    self.exportCallAuditReport = function() {
        self.isExporting(true);

        var params = self.buildFilterParams();
        delete params.page;
        delete params.pageSize;

        var filename = 'Cagri_Denetleme_Raporu_' + self.getTimestamp() + '.xlsx';
        ApiService.downloadPost('/reports/export/call-audit', params, filename)
            .then(function() {
                toastr.success('Çağrı Denetleme Raporu indirildi');
                self.closeReportsModal();
            })
            .catch(function(error) {
                console.error('Error exporting call audit report:', error);
                toastr.error('Rapor oluşturulurken hata oluştu');
            })
            .finally(function() {
                self.isExporting(false);
            });
    };

    // Soru Grubu Ortalama Raporu
    self.exportQuestionGroupAverageReport = function() {
        self.isExporting(true);

        var params = self.buildFilterParams();
        delete params.page;
        delete params.pageSize;

        var filename = 'Soru_Grubu_Ortalama_Raporu_' + self.getTimestamp() + '.xlsx';
        ApiService.downloadPost('/reports/export/question-group-average', params, filename)
            .then(function() {
                toastr.success('Soru Grubu Ortalama Raporu indirildi');
                self.closeReportsModal();
            })
            .catch(function(error) {
                console.error('Error exporting question group average report:', error);
                toastr.error('Rapor oluşturulurken hata oluştu');
            })
            .finally(function() {
                self.isExporting(false);
            });
    };

    // Müşteri Değerlendirme Raporu
    self.exportCustomerEvaluationReport = function() {
        self.isExporting(true);

        var params = self.buildFilterParams();
        delete params.page;
        delete params.pageSize;

        var filename = 'Musteri_Degerlendirme_Raporu_' + self.getTimestamp() + '.xlsx';
        ApiService.downloadPost('/reports/export/customer-evaluation', params, filename)
            .then(function() {
                toastr.success('Müşteri Değerlendirme Raporu indirildi');
                self.closeReportsModal();
            })
            .catch(function(error) {
                console.error('Error exporting customer evaluation report:', error);
                toastr.error('Rapor oluşturulurken hata oluştu');
            })
            .finally(function() {
                self.isExporting(false);
            });
    };

    // Proje Performans Raporu
    self.exportProjectPerformanceReport = function() {
        self.isExporting(true);

        var params = self.buildFilterParams();
        delete params.page;
        delete params.pageSize;

        var filename = 'Proje_Performans_Raporu_' + self.getTimestamp() + '.xlsx';
        ApiService.downloadPost('/reports/export/project-performance', params, filename)
            .then(function() {
                toastr.success('Proje Performans Raporu indirildi');
                self.closeReportsModal();
            })
            .catch(function(error) {
                console.error('Error exporting project performance report:', error);
                toastr.error('Rapor oluşturulurken hata oluştu');
            })
            .finally(function() {
                self.isExporting(false);
            });
    };

    // MT Raporu (4 sheet)
    self.exportMTReport = function() {
        self.isExporting(true);

        var params = self.buildFilterParams();
        delete params.page;
        delete params.pageSize;

        var filename = 'MT_Raporu_' + self.getTimestamp() + '.xlsx';
        ApiService.downloadPost('/reports/export/mt-report', params, filename)
            .then(function() {
                toastr.success('MT Raporu indirildi');
                self.closeReportsModal();
            })
            .catch(function(error) {
                console.error('Error exporting MT report:', error);
                toastr.error('Rapor oluşturulurken hata oluştu');
            })
            .finally(function() {
                self.isExporting(false);
            });
    };

    // Değerlendirme Detay Excel Export
    self.exportDetailToExcel = function() {
        var evaluation = self.selectedEvaluation();
        if (!evaluation || !evaluation.evaluationId) {
            toastr.error('Değerlendirme bilgisi bulunamadı');
            return;
        }

        self.isExporting(true);

        var filename = 'Degerlendirme_Detay_' + evaluation.evaluationId + '_' + self.getTimestamp() + '.xlsx';
        ApiService.downloadGet('/reports/evaluations/' + evaluation.evaluationId + '/export', filename)
            .then(function() {
                toastr.success('Değerlendirme detayı indirildi');
            })
            .catch(function(error) {
                console.error('Error exporting evaluation detail:', error);
                toastr.error('Rapor oluşturulurken hata oluştu');
            })
            .finally(function() {
                self.isExporting(false);
            });
    };

    // Helpers
    self.formatDate = function(dateStr) {
        if (!dateStr) return '-';
        var date = new Date(dateStr);
        return date.toLocaleDateString('tr-TR');
    };

    // Dönem gösterimi: dönem varsa dönem adı, yoksa çağrı tarihinden ay/yıl
    self.formatPeriod = function(periodName, callDate) {
        if (periodName) return periodName;
        if (!callDate) return '-';

        // EnumsService varsa kullan (lokalize edilmiş ay adları için)
        if (typeof EnumsService !== 'undefined' && EnumsService.cache && EnumsService.cache.months) {
            return EnumsService.formatMonthYear(callDate);
        }

        // Fallback: T fonksiyonuyla lokalize et
        var date = new Date(callDate);
        var monthNames = [
            T('Common.Month.January', 'Ocak'),
            T('Common.Month.February', 'Şubat'),
            T('Common.Month.March', 'Mart'),
            T('Common.Month.April', 'Nisan'),
            T('Common.Month.May', 'Mayıs'),
            T('Common.Month.June', 'Haziran'),
            T('Common.Month.July', 'Temmuz'),
            T('Common.Month.August', 'Ağustos'),
            T('Common.Month.September', 'Eylül'),
            T('Common.Month.October', 'Ekim'),
            T('Common.Month.November', 'Kasım'),
            T('Common.Month.December', 'Aralık')
        ];
        return monthNames[date.getMonth()] + ' ' + date.getFullYear();
    };

    self.formatTime = function(timeStr) {
        if (!timeStr) return '';
        if (typeof timeStr === 'string') {
            return timeStr.substring(0, 5);
        }
        return '';
    };

    self.getScoreClass = function(score) {
        if (score === null || score === undefined) return 'bg-secondary';
        if (score >= 90) return 'bg-success';
        if (score >= 70) return 'bg-warning text-dark';
        return 'bg-danger';
    };

    self.getStatusText = function(status) {
        return self.statusLabels[status] || status;
    };

    self.getStatusClass = function(status) {
        var statusClasses = {
            'Completed': 'bg-success',
            'InProgress': 'bg-info',
            'Draft': 'bg-secondary',
            'Pending': 'bg-warning text-dark',
            'Cancelled': 'bg-danger'
        };
        return statusClasses[status] || 'bg-secondary';
    };

    // ==================== SAVED FILTERS ====================

    // Load saved filters from API
    self.loadSavedFilters = function() {
        self.isLoadingFilters(true);
        ApiService.get('/saved-filters?pageName=' + self.pageName)
            .then(function(response) {
                // isDefault'u observable yap
                var filters = (response || []).map(function(f) {
                    f.isDefault = ko.observable(f.isDefault);
                    return f;
                });
                self.savedFilters(filters);
            })
            .catch(function(error) {
                console.error('Error loading saved filters:', error);
            })
            .finally(function() {
                self.isLoadingFilters(false);
            });
    };

    // Open save filter modal
    self.openSaveFilterModal = function() {
        if (self.activeFilters().length === 0) {
            toastr.warning('Kaydetmek için önce filtre ekleyin');
            return;
        }
        self.saveFilterName('');
        self.saveFilterDescription('');
        self.saveFilterIsDefault(false);
        self.showSaveFilterModal(true);
    };

    // Close save filter modal
    self.closeSaveFilterModal = function() {
        self.showSaveFilterModal(false);
    };

    // Get filter summary for display
    self.getFilterSummary = function() {
        return self.activeFilters().map(function(f) {
            return f.label + ': ' + f.displayValue;
        });
    };

    // Save current filters
    self.saveFilter = function() {
        var name = self.saveFilterName().trim();
        if (!name) {
            toastr.warning('Filtre adı zorunludur');
            return;
        }

        // Convert activeFilters to serializable format
        var filterData = {
            filters: self.activeFilters().map(function(f) {
                return {
                    type: f.type,
                    label: f.label,
                    value: f.value,
                    displayValue: f.displayValue
                };
            })
        };

        var dto = {
            pageName: self.pageName,
            name: name,
            description: self.saveFilterDescription().trim() || null,
            filterData: JSON.stringify(filterData),
            isDefault: self.saveFilterIsDefault()
        };

        self.isSavingFilter(true);
        ApiService.post('/saved-filters', dto)
            .then(function(response) {
                toastr.success('Filtre kaydedildi');
                self.closeSaveFilterModal();
                self.loadSavedFilters();
            })
            .catch(function(error) {
                console.error('Error saving filter:', error);
                toastr.error('Filtre kaydedilirken hata oluştu');
            })
            .finally(function() {
                self.isSavingFilter(false);
            });
    };

    // Open load filter modal
    self.openLoadFilterModal = function() {
        self.savedFilterSearch('');
        self.loadSavedFilters();
        self.showLoadFilterModal(true);
    };

    // Close load filter modal
    self.closeLoadFilterModal = function() {
        self.showLoadFilterModal(false);
    };

    // Apply a saved filter
    self.applySavedFilter = function(savedFilter) {
        try {
            var filterData = JSON.parse(savedFilter.filterData);
            var filters = filterData.filters || [];

            // Clear current filters
            self.activeFilters([]);

            // Apply each filter
            filters.forEach(function(f) {
                // dateRange/callDateRange filtresi için dateRangeType varsa tarihleri yeniden hesapla
                if ((f.type === 'dateRange' || f.type === 'callDateRange') && f.value && f.value.dateRangeType) {
                    // Dinamik tarih hesapla
                    self.setTempDateRange(f.value.dateRangeType);
                    var newStartDate = self.tempFilter.startDate();
                    var newEndDate = self.tempFilter.endDate();

                    var rangeInfo = self.dateRanges().find(function(r) { return r.systemName === f.value.dateRangeType; });

                    self.activeFilters.push({
                        type: f.type,
                        label: f.label,
                        value: {
                            startDate: newStartDate,
                            endDate: newEndDate,
                            dateRangeType: f.value.dateRangeType
                        },
                        displayValue: rangeInfo ? rangeInfo.name : f.value.dateRangeType
                    });

                    // Clear temp
                    self.tempFilter.startDate('');
                    self.tempFilter.endDate('');
                    self.tempFilter.selectedDateRangeType(null);
                } else {
                    self.activeFilters.push({
                        type: f.type,
                        label: f.label,
                        value: f.value,
                        displayValue: f.displayValue
                    });
                }
            });

            self.closeLoadFilterModal();
            self.search();
            toastr.success('Filtre uygulandı: ' + savedFilter.name);
        } catch (e) {
            console.error('Error applying filter:', e);
            toastr.error('Filtre uygulanırken hata oluştu');
        }
    };

    // Delete a saved filter
    self.deleteSavedFilter = function(savedFilter) {
        showConfirmModal({
            title: T('Common.Delete', 'Sil'),
            message: T('Filter.ConfirmDelete', 'Bu filtreyi silmek istediğinize emin misiniz?'),
            confirmText: T('Common.Delete', 'Sil'),
            confirmClass: 'btn-danger',
            onConfirm: function() {
                ApiService.delete('/saved-filters/' + savedFilter.id)
                    .then(function() {
                        toastr.success(T('Filter.Deleted', 'Filtre silindi'));
                        self.savedFilters.remove(savedFilter);
                    })
                    .catch(function(error) {
                        console.error('Error deleting filter:', error);
                        toastr.error(T('Filter.DeleteError', 'Filtre silinirken hata oluştu'));
                    });
            }
        });
    };

    // Set a filter as default
    self.setFilterAsDefault = function(savedFilter) {
        ApiService.post('/saved-filters/' + savedFilter.id + '/set-default?pageName=' + self.pageName)
            .then(function() {
                toastr.success('Varsayılan filtre ayarlandı');
                // Tüm filtrelerin isDefault'unu güncelle
                self.savedFilters().forEach(function(f) {
                    f.isDefault(f.id === savedFilter.id);
                });
            })
            .catch(function(error) {
                console.error('Error setting default filter:', error);
                toastr.error('Varsayılan filtre ayarlanırken hata oluştu');
            });
    };

    // Clear default filter
    self.clearFilterDefault = function(savedFilter) {
        ApiService.post('/saved-filters/clear-default?pageName=' + self.pageName)
            .then(function() {
                toastr.success('Varsayılan filtre kaldırıldı');
                // Tüm filtrelerin isDefault'unu false yap
                self.savedFilters().forEach(function(f) {
                    f.isDefault(false);
                });
            })
            .catch(function(error) {
                console.error('Error clearing default filter:', error);
                toastr.error('Varsayılan filtre kaldırılırken hata oluştu');
            });
    };

    // Attachments Modal Functions
    self.showAttachments = function(evaluation) {
        self.isAttachmentsModalOpen(true);
        self.isAttachmentsLoading(true);
        self.attachments([]);

        fetch('/api/evaluations/' + evaluation.evaluationId + '/attachments', { credentials: 'include' })
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
        window.open('/api/evaluations/attachments/' + attachment.id + '/download', '_blank');
    };

    // Dosya boyutunu formatla
    self.formatFileSize = function(bytes) {
        if (!bytes) return '-';
        if (bytes < 1024) return bytes + ' B';
        if (bytes < 1024 * 1024) return (bytes / 1024).toFixed(1) + ' KB';
        if (bytes < 1024 * 1024 * 1024) return (bytes / (1024 * 1024)).toFixed(1) + ' MB';
        return (bytes / (1024 * 1024 * 1024)).toFixed(1) + ' GB';
    };

    // Initialize
    self.init();
}

// Apply bindings when DOM is ready
$(document).ready(function() {
    if (document.getElementById('listenings-app')) {
        ko.applyBindings(new ListeningsViewModel(), document.getElementById('listenings-app'));
    }
});
