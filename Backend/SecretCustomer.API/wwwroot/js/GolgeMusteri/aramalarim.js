function AramalarimViewModel() {
    var self = this;

    // Tab
    var role = window.userRole || '';
    self.showTabs = role === 'QualitySpecialist' || role === 'Admin' || role === 'Inspector';
    self.activeTab = ko.observable('aramalarim');

    self.atamalar = ko.observableArray([]);
    self.isLoading = ko.observable(true);
    self.isSaving = ko.observable(false);
    self.completingAtama = ko.observable(null);

    // Aramalar tab (tamamlanan aramalar)
    self.tamamlananAramalar = ko.observableArray([]);
    self.isAramalarLoading = ko.observable(false);
    self.aramalarLoaded = false;
    self.selectedAtama = ko.observable(null);

    // Tab 2 Filter UI
    self.tab2SelectedFilterType = ko.observable('');
    self.tab2TempFilter = {
        donemId: ko.observable(''),
        firma: ko.observable(''),
        startDate: ko.observable(''),
        endDate: ko.observable(''),
        dateRangeType: ko.observable('')
    };
    self.tab2ActiveFilters = ko.observableArray([]);

    self.canAddTab2Filter = ko.computed(function () {
        var type = self.tab2SelectedFilterType();
        if (!type) return false;
        if (type === 'donem') return self.tab2TempFilter.donemId();
        if (type === 'firma') return self.tab2TempFilter.firma();
        if (type === 'dateRange') return self.tab2TempFilter.startDate() || self.tab2TempFilter.endDate() || self.tab2TempFilter.dateRangeType();
        return false;
    });

    self.setTab2DateRange = function (rangeType) {
        var range = self.calculateDateRange(rangeType);
        self.tab2TempFilter.startDate(range.start);
        self.tab2TempFilter.endDate(range.end);
        self.tab2TempFilter.dateRangeType(rangeType);
    };

    self.addTab2Filter = function () {
        var type = self.tab2SelectedFilterType();
        if (!type) return;

        var filter = { type: type };
        var label = '';
        var displayValue = '';

        if (type === 'donem') {
            filter.value = self.tab2TempFilter.donemId();
            var donem = self.donemler().find(function (d) { return d.id == filter.value; });
            label = 'Dönem';
            displayValue = donem ? donem.ad : filter.value;
        } else if (type === 'firma') {
            filter.value = self.tab2TempFilter.firma();
            label = 'Firma';
            displayValue = filter.value;
        } else if (type === 'dateRange') {
            filter.dateRangeType = self.tab2TempFilter.dateRangeType();
            filter.startDate = self.tab2TempFilter.startDate();
            filter.endDate = self.tab2TempFilter.endDate();
            label = 'Tarih';
            if (filter.dateRangeType && self.dateRangeLabels[filter.dateRangeType]) {
                displayValue = self.dateRangeLabels[filter.dateRangeType];
            } else {
                displayValue = (filter.startDate || '...') + ' - ' + (filter.endDate || '...');
            }
        }

        self.tab2ActiveFilters.push({
            type: type,
            value: filter.value,
            startDate: filter.startDate,
            endDate: filter.endDate,
            dateRangeType: filter.dateRangeType,
            label: label,
            displayValue: displayValue
        });

        self.resetTab2TempFilter();
        self.tab2SelectedFilterType('');
        self.loadTamamlananAramalar();
    };

    self.resetTab2TempFilter = function () {
        self.tab2TempFilter.donemId('');
        self.tab2TempFilter.firma('');
        self.tab2TempFilter.startDate('');
        self.tab2TempFilter.endDate('');
        self.tab2TempFilter.dateRangeType('');
    };

    self.removeTab2Filter = function (filter) {
        self.tab2ActiveFilters.remove(filter);
        self.loadTamamlananAramalar();
    };

    self.clearTab2Filters = function () {
        self.tab2ActiveFilters.removeAll();
        self.loadTamamlananAramalar();
    };

    // Kuponlarım
    self.kuponBekleyenler = ko.observableArray([]);
    self.isKuponlarLoading = ko.observable(false);
    self.kuponlarLoaded = false;
    self.kuponAtama = ko.observable(null);
    self.isKuponSaving = ko.observable(false);
    self.kuponForm = {
        kuponKodu: ko.observable('')
    };

    // Dinlemelerim
    self.dinlemelerim = ko.observableArray([]);
    self.isDinlemelerLoading = ko.observable(false);

    // Tab change handler
    self.activeTab.subscribe(function (tab) {
        if (tab === 'kuponlarim' && !self.kuponlarLoaded) {
            self.loadKuponBekleyenler();
        }
        if (tab === 'aramalar' && !self.aramalarLoaded) {
            self.loadTamamlananAramalar();
        }
    });

    // Filter data
    self.donemler = ko.observableArray([]);

    // Filter UI (internalEvaluations.js pattern)
    self.selectedFilterType = ko.observable('');
    self.tempFilter = {
        donemId: ko.observable(''),
        durumId: ko.observable(''),
        firma: ko.observable(''),
        startDate: ko.observable(''),
        endDate: ko.observable(''),
        dateRangeType: ko.observable('')
    };

    // Active filters
    self.activeFilters = ko.observableArray([]);

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

    self.durumLabels = {
        '1': 'Beklemede',
        '2': 'Tamamlandı'
    };

    self.completeForm = {
        gerceklesmeTarihi: ko.observable(''),
        aramaSaati: ko.observable(''),
        not: ko.observable(''),
        kuponKodu: ko.observable(''),
        gorusulenTemsilci: ko.observable('')
    };

    // Temsilci (personnel) autocomplete
    self.availablePersonnel = ko.observableArray([]);
    self.isTemsilciDropdownVisible = ko.observable(false);
    self.personnelLoadedForCustomer = null;

    self.filteredPersonnel = ko.computed(function () {
        var search = (self.completeForm.gorusulenTemsilci() || '').toLowerCase().trim();
        if (!search) return [];
        return self.availablePersonnel().filter(function (p) {
            return p.name.toLowerCase().indexOf(search) >= 0;
        }).slice(0, 15);
    });

    self.loadPersonnelForCustomer = function (customerId) {
        if (!customerId || self.personnelLoadedForCustomer === customerId) return;
        $.get('/api/customer-personnel/by-customer/' + customerId)
            .done(function (data) {
                var personnel = (data || []).map(function (p) {
                    return {
                        id: p.id,
                        name: p.fullName || ((p.firstName || '') + ' ' + (p.lastName || '')).trim(),
                        title: p.title || ''
                    };
                });
                self.availablePersonnel(personnel);
                self.personnelLoadedForCustomer = customerId;
            });
    };

    self.selectTemsilci = function (personnel) {
        self.completeForm.gorusulenTemsilci(personnel.name);
        self.isTemsilciDropdownVisible(false);
    };

    self.onTemsilciInputFocus = function () {
        self.isTemsilciDropdownVisible(true);
        return true;
    };

    self.onTemsilciInputBlur = function () {
        setTimeout(function () { self.isTemsilciDropdownVisible(false); }, 200);
        return true;
    };

    self.bekleyenler = ko.computed(function () {
        return self.atamalar().filter(function (a) { return a.durumId === 1; });
    });

    self.tamamlananlar = ko.computed(function () {
        return self.atamalar().filter(function (a) { return a.durumId === 2; });
    });

    // Can add filter
    self.canAddFilter = ko.computed(function () {
        var type = self.selectedFilterType();
        if (!type) return false;
        if (type === 'donem') return self.tempFilter.donemId();
        if (type === 'durum') return self.tempFilter.durumId();
        if (type === 'firma') return self.tempFilter.firma();
        if (type === 'dateRange') return self.tempFilter.startDate() || self.tempFilter.endDate() || self.tempFilter.dateRangeType();
        return false;
    });

    // Calculate date range from type
    self.calculateDateRange = function (rangeType) {
        var today = new Date();
        var start, end;

        if (rangeType === 'today') {
            start = end = formatLocalDate(today);
        } else if (rangeType === 'yesterday') {
            var yesterday = new Date(today);
            yesterday.setDate(yesterday.getDate() - 1);
            start = end = formatLocalDate(yesterday);
        } else if (rangeType === 'thisWeek') {
            var dayOfWeek = today.getDay();
            var diff = today.getDate() - dayOfWeek + (dayOfWeek === 0 ? -6 : 1);
            var weekStart = new Date(today);
            weekStart.setDate(diff);
            start = formatLocalDate(weekStart);
            end = formatLocalDate(today);
        } else if (rangeType === 'lastWeek') {
            var dayOfWeek = today.getDay();
            var diff = today.getDate() - dayOfWeek + (dayOfWeek === 0 ? -6 : 1);
            var lastWeekEnd = new Date(today);
            lastWeekEnd.setDate(diff - 1);
            var lastWeekStart = new Date(lastWeekEnd);
            lastWeekStart.setDate(lastWeekEnd.getDate() - 6);
            start = formatLocalDate(lastWeekStart);
            end = formatLocalDate(lastWeekEnd);
        } else if (rangeType === 'thisMonth') {
            start = formatLocalDate(new Date(today.getFullYear(), today.getMonth(), 1));
            end = formatLocalDate(today);
        } else if (rangeType === 'lastMonth') {
            start = formatLocalDate(new Date(today.getFullYear(), today.getMonth() - 1, 1));
            end = formatLocalDate(new Date(today.getFullYear(), today.getMonth(), 0));
        } else if (rangeType === 'last3Months') {
            start = formatLocalDate(new Date(today.getFullYear(), today.getMonth() - 2, 1));
            end = formatLocalDate(today);
        } else if (rangeType === 'last6Months') {
            start = formatLocalDate(new Date(today.getFullYear(), today.getMonth() - 5, 1));
            end = formatLocalDate(today);
        } else if (rangeType === 'thisYear') {
            start = formatLocalDate(new Date(today.getFullYear(), 0, 1));
            end = formatLocalDate(today);
        } else if (rangeType === 'lastYear') {
            start = formatLocalDate(new Date(today.getFullYear() - 1, 0, 1));
            end = formatLocalDate(new Date(today.getFullYear() - 1, 11, 31));
        }

        return { start: start, end: end };
    };

    // Date range helper for UI
    self.setDateRange = function (rangeType) {
        var range = self.calculateDateRange(rangeType);
        self.tempFilter.startDate(range.start);
        self.tempFilter.endDate(range.end);
        self.tempFilter.dateRangeType(rangeType);
    };

    // Add filter
    self.addFilter = function () {
        var type = self.selectedFilterType();
        if (!type) return;

        var filter = { type: type };
        var label = '';
        var displayValue = '';

        if (type === 'donem') {
            filter.value = self.tempFilter.donemId();
            var donem = self.donemler().find(function (d) { return d.id == filter.value; });
            label = 'Dönem';
            displayValue = donem ? donem.ad : filter.value;
        } else if (type === 'durum') {
            filter.value = self.tempFilter.durumId();
            label = 'Durum';
            displayValue = self.durumLabels[filter.value] || filter.value;
        } else if (type === 'firma') {
            filter.value = self.tempFilter.firma();
            label = 'Firma';
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

        self.activeFilters.push({
            type: type,
            value: filter.value,
            startDate: filter.startDate,
            endDate: filter.endDate,
            dateRangeType: filter.dateRangeType,
            label: label,
            displayValue: displayValue
        });

        self.resetTempFilter();
        self.selectedFilterType('');
        self.search();
    };

    self.resetTempFilter = function () {
        self.tempFilter.donemId('');
        self.tempFilter.durumId('');
        self.tempFilter.firma('');
        self.tempFilter.startDate('');
        self.tempFilter.endDate('');
        self.tempFilter.dateRangeType('');
    };

    self.removeFilter = function (filter) {
        self.activeFilters.remove(filter);
        self.search();
    };

    self.clearFilters = function () {
        self.activeFilters.removeAll();
        self.search();
    };

    // Search
    self.search = function () {
        self.loadAtamalar();
    };

    // Build query params from active filters (URLSearchParams.append pattern)
    self.buildQueryParams = function () {
        var params = new URLSearchParams();

        var startDate = null;
        var endDate = null;

        self.activeFilters().forEach(function (f) {
            switch (f.type) {
                case 'donem':
                    params.append('donemIds', f.value);
                    break;
                case 'durum':
                    params.append('durumIds', f.value);
                    break;
                case 'firma':
                    params.append('firmaArama', f.value);
                    break;
                case 'dateRange':
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

        if (startDate) params.append('startDate', startDate);
        if (endDate) params.append('endDate', endDate);

        return params;
    };

    self.loadDonemler = function () {
        $.get('/api/gm/aramalarim/donemler')
            .done(function (data) { self.donemler(data); });
    };

    self.loadAtamalar = function () {
        self.isLoading(true);

        var params = self.buildQueryParams();
        var queryString = params.toString();
        var url = '/api/gm/aramalarim' + (queryString ? '?' + queryString : '');

        $.get(url)
            .done(function (data) { self.atamalar(data); })
            .fail(function () { toastr.error('Aramalar yüklenirken hata oluştu.'); })
            .always(function () { self.isLoading(false); });
    };

    // Kuponlarım
    self.loadKuponBekleyenler = function () {
        self.isKuponlarLoading(true);
        $.get('/api/gm/aramalarim/kupon-bekleyenler')
            .done(function (data) {
                self.kuponBekleyenler(data);
                self.kuponlarLoaded = true;
            })
            .fail(function () { toastr.error('Kupon bekleyenler yüklenirken hata oluştu.'); })
            .always(function () { self.isKuponlarLoading(false); });
    };

    self.showKuponKoduModal = function (atama) {
        self.kuponAtama(atama);
        self.kuponForm.kuponKodu('');
        $('#kuponKoduModal').modal('show');
    };

    self.saveKuponKodu = function () {
        if (!self.kuponAtama()) return;
        if (!self.kuponForm.kuponKodu()) {
            toastr.warning('Kupon kodu zorunludur.');
            return;
        }

        self.isKuponSaving(true);
        $.ajax({
            url: '/api/gm/aramalarim/' + self.kuponAtama().id + '/kupon-kodu',
            type: 'PUT',
            contentType: 'application/json',
            data: JSON.stringify({
                kuponKodu: self.kuponForm.kuponKodu()
            })
        })
        .done(function () {
            toastr.success('Kupon kodu kaydedildi. Atama aramalarınıza eklendi.');
            $('#kuponKoduModal').modal('hide');
            self.kuponAtama(null);
            self.loadKuponBekleyenler();
            self.loadAtamalar();
        })
        .fail(function (xhr) {
            toastr.error(xhr.responseJSON?.message || 'Kupon kodu kaydedilemedi.');
        })
        .always(function () {
            self.isKuponSaving(false);
        });
    };

    // Tamamlanan aramalar (Aramalar tab)
    self.buildTab2QueryParams = function () {
        var params = new URLSearchParams();
        var startDate = null;
        var endDate = null;

        self.tab2ActiveFilters().forEach(function (f) {
            switch (f.type) {
                case 'donem':
                    params.append('donemIds', f.value);
                    break;
                case 'firma':
                    params.append('firmaArama', f.value);
                    break;
                case 'dateRange':
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

        if (startDate) params.append('startDate', startDate);
        if (endDate) params.append('endDate', endDate);

        return params;
    };

    self.loadTamamlananAramalar = function () {
        self.isAramalarLoading(true);

        var params = self.buildTab2QueryParams();
        var queryString = params.toString();
        var url = '/api/gm/aramalarim/tamamlanan' + (queryString ? '?' + queryString : '');

        $.get(url)
            .done(function (data) {
                self.tamamlananAramalar(data);
                self.aramalarLoaded = true;
            })
            .fail(function () { toastr.error('Tamamlanan aramalar yüklenirken hata oluştu.'); })
            .always(function () { self.isAramalarLoading(false); });
    };

    // Dinlemelerim
    self.loadDinlemelerim = function () {
        self.isDinlemelerLoading(true);
        $.get('/api/gm/aramalarim/dinlemelerim')
            .done(function (data) { self.dinlemelerim(data); })
            .fail(function () { toastr.error('Dinlemeler yüklenirken hata oluştu.'); })
            .always(function () { self.isDinlemelerLoading(false); });
    };

    // Dinleme popup aç (yeni veya taslak)
    self.openDinlemePopup = function (dinleme) {
        var url = '/GolgeMusteri/PopupGmDinleme?gmAtamaId=' + dinleme.gmAtamaId;
        window.open(url, 'gmDinleme_' + dinleme.id, 'width=1200,height=900,scrollbars=yes,resizable=yes');
    };

    // Dinleme popup aç (tamamlanmış - edit/görüntüleme)
    self.openDinlemePopupEdit = function (dinleme) {
        var url = '/GolgeMusteri/PopupGmDinleme?dinlemeId=' + dinleme.id;
        window.open(url, 'gmDinleme_' + dinleme.id, 'width=1200,height=900,scrollbars=yes,resizable=yes');
    };

    self.showAtamaDetailModal = function (atama) {
        self.selectedAtama(atama);
        $('#atamaDetailModal').modal('show');
    };

    // Güncelleme
    self.updatingAtama = ko.observable(null);
    self.isUpdating = ko.observable(false);
    self.updateForm = {
        gerceklesmeTarihi: ko.observable(''),
        aramaSaati: ko.observable(''),
        not: ko.observable(''),
        gorusulenTemsilci: ko.observable('')
    };

    // Güncelle temsilci autocomplete
    self.updateFilteredPersonnel = ko.computed(function () {
        var search = (self.updateForm.gorusulenTemsilci() || '').toLowerCase().trim();
        if (!search) return [];
        return self.availablePersonnel().filter(function (p) {
            return p.name.toLowerCase().indexOf(search) >= 0;
        }).slice(0, 15);
    });

    self.isUpdateTemsilciDropdownVisible = ko.observable(false);

    self.selectUpdateTemsilci = function (personnel) {
        self.updateForm.gorusulenTemsilci(personnel.name);
        self.isUpdateTemsilciDropdownVisible(false);
    };

    self.onUpdateTemsilciFocus = function () {
        self.isUpdateTemsilciDropdownVisible(true);
        return true;
    };

    self.onUpdateTemsilciBlur = function () {
        setTimeout(function () { self.isUpdateTemsilciDropdownVisible(false); }, 200);
        return true;
    };

    self.showUpdateModal = function (atama) {
        self.updatingAtama(atama);
        self.updateForm.gerceklesmeTarihi(atama.gerceklesmeTarihi ? formatLocalDate(new Date(atama.gerceklesmeTarihi)) : '');
        self.updateForm.aramaSaati(atama.aramaSaati || '');
        self.updateForm.not(atama.not || '');
        self.updateForm.gorusulenTemsilci(atama.gorusulenTemsilci || '');
        if (atama.customerId) {
            self.loadPersonnelForCustomer(atama.customerId);
        }
        $('#updateModal').modal('show');
    };

    self.updateAtama = function () {
        if (!self.updatingAtama()) return;
        if (!self.updateForm.gerceklesmeTarihi() || !self.updateForm.aramaSaati()) {
            toastr.warning('Tarih ve saat zorunludur.');
            return;
        }

        self.isUpdating(true);
        $.ajax({
            url: '/api/gm/aramalarim/' + self.updatingAtama().id + '/guncelle',
            type: 'PUT',
            contentType: 'application/json',
            data: JSON.stringify({
                gerceklesmeTarihi: self.updateForm.gerceklesmeTarihi(),
                aramaSaati: self.updateForm.aramaSaati(),
                not: self.updateForm.not() || null,
                gorusulenTemsilci: self.updateForm.gorusulenTemsilci() || null
            })
        })
        .done(function () {
            toastr.success('Arama güncellendi.');
            $('#updateModal').modal('hide');
            self.updatingAtama(null);
            self.loadTamamlananAramalar();
        })
        .fail(function (xhr) {
            toastr.error(xhr.responseJSON?.message || 'Güncelleme başarısız.');
        })
        .always(function () {
            self.isUpdating(false);
        });
    };

    self.showCompleteModal = function (atama) {
        self.completingAtama(atama);
        var today = formatLocalDate(new Date());
        var now = new Date().toTimeString().substring(0, 5);
        self.completeForm.gerceklesmeTarihi(today);
        self.completeForm.aramaSaati(now);
        self.completeForm.not('');
        self.completeForm.kuponKodu('');
        self.completeForm.gorusulenTemsilci('');
        // Load personnel for autocomplete
        if (atama.customerId) {
            self.loadPersonnelForCustomer(atama.customerId);
        }
        $('#completeModal').modal('show');
    };

    self.completeAtama = function () {
        if (!self.completingAtama()) return;
        if (!self.completeForm.gerceklesmeTarihi() || !self.completeForm.aramaSaati()) {
            toastr.warning('Tarih ve saat zorunludur.');
            return;
        }

        self.isSaving(true);
        $.ajax({
            url: '/api/gm/aramalarim/' + self.completingAtama().id + '/tamamla',
            type: 'POST',
            contentType: 'application/json',
            data: JSON.stringify({
                gerceklesmeTarihi: self.completeForm.gerceklesmeTarihi(),
                aramaSaati: self.completeForm.aramaSaati(),
                not: self.completeForm.not() || null,
                kuponKodu: self.completingAtama() ? self.completingAtama().kuponKodu : null,
                gorusulenTemsilci: self.completeForm.gorusulenTemsilci() || null
            })
        })
        .done(function () {
            toastr.success('Arama tamamlandı.');
            $('#completeModal').modal('hide');
            self.completingAtama(null);
            self.loadAtamalar();
        })
        .fail(function (xhr) {
            toastr.error(xhr.responseJSON?.message || 'Tamamlama başarısız.');
        })
        .always(function () {
            self.isSaving(false);
        });
    };

    // Init
    self.loadDonemler();
    self.loadAtamalar();
    self.loadDinlemelerim();
}

$(function () {
    ko.applyBindings(new AramalarimViewModel(), document.getElementById('aramalarim-app'));
});
