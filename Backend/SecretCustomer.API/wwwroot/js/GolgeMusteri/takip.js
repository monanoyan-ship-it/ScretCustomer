function TakipViewModel() {
    var self = this;

    self.donemler = ko.observableArray([]);
    self.users = ko.observableArray([]);
    self.atamalar = ko.observableArray([]);
    self.dinlemeler = ko.observableArray([]);
    self.isAramalarLoading = ko.observable(false);
    self.isDinlemelerLoading = ko.observable(false);
    self.selectedDonemId = ko.observable(null);
    self.selectedUserId = ko.observable(null);
    self.selectedAramaDurumId = ko.observable('');
    self.selectedDinlemeDurumId = ko.observable('');
    self.activeTab = ko.observable('aramalar');

    // Arama özet
    self.tamamlananSayisi = ko.computed(function () {
        return self.atamalar().filter(function (a) { return a.durumId === 2; }).length;
    });
    self.bekleyenSayisi = ko.computed(function () {
        return self.atamalar().filter(function (a) { return a.durumId === 1; }).length;
    });
    self.tamamlanmaOrani = ko.computed(function () {
        var total = self.atamalar().length;
        if (!total) return 0;
        return Math.round(self.tamamlananSayisi() / total * 100);
    });

    // Dinleme özet
    self.dinlemeTamamlananSayisi = ko.computed(function () {
        return self.dinlemeler().filter(function (d) { return d.durumId === 3; }).length;
    });
    self.dinlemeBekleyenSayisi = ko.computed(function () {
        return self.dinlemeler().filter(function (d) { return d.durumId === 1; }).length;
    });
    self.dinlemeTamamlanmaOrani = ko.computed(function () {
        var total = self.dinlemeler().length;
        if (!total) return 0;
        return Math.round(self.dinlemeTamamlananSayisi() / total * 100);
    });

    // Dönem veya personel değişince her ikisini de yükle
    self.selectedDonemId.subscribe(function () { self.loadAtamalar(); self.loadDinlemeler(); });
    self.selectedUserId.subscribe(function () { self.loadAtamalar(); self.loadDinlemeler(); });
    self.selectedAramaDurumId.subscribe(function () { self.loadAtamalar(); });
    self.selectedDinlemeDurumId.subscribe(function () { self.loadDinlemeler(); });

    self.loadDonemler = function () {
        $.get('/api/gm/donemler')
            .done(function (data) { self.donemler(data); });
    };

    self.loadUsers = function () {
        $.get('/api/users')
            .done(function (data) {
                var list = Array.isArray(data) ? data : (data.items || []);
                self.users(list);
            });
    };

    self.loadAtamalar = function () {
        if (!self.selectedDonemId()) {
            self.atamalar([]);
            return;
        }
        self.isAramalarLoading(true);
        var params = ['donemId=' + self.selectedDonemId()];
        if (self.selectedUserId()) params.push('userId=' + self.selectedUserId());
        if (self.selectedAramaDurumId()) params.push('durumId=' + self.selectedAramaDurumId());

        $.get('/api/gm/atamalar?' + params.join('&'))
            .done(function (data) { self.atamalar(data); })
            .fail(function () { toastr.error('Atamalar yüklenirken hata oluştu.'); })
            .always(function () { self.isAramalarLoading(false); });
    };

    self.loadDinlemeler = function () {
        if (!self.selectedDonemId()) {
            self.dinlemeler([]);
            return;
        }
        self.isDinlemelerLoading(true);
        var params = ['donemId=' + self.selectedDonemId()];
        if (self.selectedUserId()) params.push('dinleyenUserId=' + self.selectedUserId());
        if (self.selectedDinlemeDurumId()) params.push('durumId=' + self.selectedDinlemeDurumId());

        $.get('/api/gm/dinleme-takip?' + params.join('&'))
            .done(function (data) { self.dinlemeler(data); })
            .fail(function () { toastr.error('Dinlemeler yüklenirken hata oluştu.'); })
            .always(function () { self.isDinlemelerLoading(false); });
    };

    // Detay modal
    self.selectedAtama = ko.observable(null);

    self.showAtamaDetail = function (atama) {
        self.selectedAtama(atama);
        $('#atamaDetailModal').modal('show');
    };

    // Düzenleme modal
    self.editingAtama = ko.observable(null);
    self.isEditSaving = ko.observable(false);
    self.editForm = {
        userId: ko.observable(null),
        planTarihi: ko.observable('')
    };

    self.showEditModal = function (atama) {
        self.editingAtama(atama);
        self.editForm.userId(atama.userId);
        self.editForm.planTarihi(atama.planTarihi ? atama.planTarihi.substring(0, 10) : '');
        $('#editAtamaModal').modal('show');
    };

    self.saveAtamaEdit = function () {
        var atama = self.editingAtama();
        if (!atama) return;

        self.isEditSaving(true);
        $.ajax({
            url: '/api/gm/atamalar/' + atama.id,
            type: 'PUT',
            contentType: 'application/json',
            data: JSON.stringify({
                userId: self.editForm.userId() ? parseInt(self.editForm.userId()) : null,
                planTarihi: self.editForm.planTarihi() || null
            })
        })
        .done(function (result) {
            // Listedeki öğeyi güncelle
            var items = self.atamalar();
            for (var i = 0; i < items.length; i++) {
                if (items[i].id === atama.id) {
                    items[i] = result;
                    break;
                }
            }
            self.atamalar(items);
            toastr.success('Atama güncellendi.');
            $('#editAtamaModal').modal('hide');
        })
        .fail(function (xhr) {
            toastr.error(xhr.responseJSON?.message || 'Güncelleme başarısız.');
        })
        .always(function () {
            self.isEditSaving(false);
        });
    };

    // Init
    self.loadDonemler();
    self.loadUsers();
}

$(function () {
    ko.applyBindings(new TakipViewModel(), document.getElementById('takip-app'));
});
