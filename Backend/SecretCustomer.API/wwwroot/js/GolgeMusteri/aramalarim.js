function AramalarimViewModel() {
    var self = this;

    self.atamalar = ko.observableArray([]);
    self.isLoading = ko.observable(true);
    self.isSaving = ko.observable(false);
    self.completingAtama = ko.observable(null);

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
        kuponKodu: ko.observable('')
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
        $.get('/api/gm/donemler')
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

    self.showCompleteModal = function (atama) {
        self.completingAtama(atama);
        var today = new Date().toISOString().substring(0, 10);
        var now = new Date().toTimeString().substring(0, 5);
        self.completeForm.gerceklesmeTarihi(today);
        self.completeForm.aramaSaati(now);
        self.completeForm.not('');
        self.completeForm.kuponKodu('');
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
                kuponKodu: self.completeForm.kuponKodu() || null
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
}

$(function () {
    ko.applyBindings(new AramalarimViewModel(), document.getElementById('aramalarim-app'));
});
