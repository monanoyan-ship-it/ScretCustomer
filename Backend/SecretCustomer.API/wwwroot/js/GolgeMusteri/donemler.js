function DonemlerViewModel() {
    var self = this;

    self.donemler = ko.observableArray([]);
    self.customers = ko.observableArray([]);
    self.availableUsers = ko.observableArray([]);
    self.availableSorular = ko.observableArray([]);
    self.isLoading = ko.observable(true);
    self.isSaving = ko.observable(false);
    self.isEditing = ko.observable(false);
    self.editingId = ko.observable(null);
    self.selectedCustomerId = ko.observable(null);
    self.selectedDonem = ko.observable(null);
    self.donemDetail = ko.observable(null);

    // Dönem alt yönetim
    self.newPersonelUserId = ko.observable(null);
    self.newSoruId = ko.observable(null);
    self.newSoruAranma = ko.observable(1);
    self.newKuponText = ko.observable('');

    self.form = {
        customerId: ko.observable(null),
        ad: ko.observable(''),
        baslangicTarihi: ko.observable(''),
        bitisTarihi: ko.observable('')
    };

    self.selectedCustomerId.subscribe(function () {
        self.loadDonemler();
    });

    self.loadCustomers = function () {
        $.get('/api/customers')
            .done(function (data) {
                var list = Array.isArray(data) ? data : (data.items || []);
                self.customers(list);
            });
    };

    self.loadDonemler = function () {
        self.isLoading(true);
        var url = '/api/gm/donemler';
        if (self.selectedCustomerId()) url += '?customerId=' + self.selectedCustomerId();

        $.get(url)
            .done(function (data) { self.donemler(data); })
            .fail(function () { toastr.error('Dönemler yüklenirken hata oluştu.'); })
            .always(function () { self.isLoading(false); });
    };

    self.openDonem = function (donem) {
        self.selectedDonem(donem);
        self.loadDonemDetail(donem.id);
    };

    self.closeDonem = function () {
        self.selectedDonem(null);
        self.donemDetail(null);
        self.loadDonemler();
    };

    self.loadDonemDetail = function (donemId) {
        $.get('/api/gm/donemler/' + donemId)
            .done(function (data) {
                self.donemDetail(data);
                // Kullanıcılar ve soruları da yükle
                if (data.customerId) {
                    self.loadAvailableUsers();
                    self.loadAvailableSorular(data.customerId);
                }
            })
            .fail(function () { toastr.error('Dönem detayı yüklenirken hata.'); });
    };

    self.loadAvailableUsers = function () {
        $.get('/api/users')
            .done(function (data) {
                var list = Array.isArray(data) ? data : (data.items || []);
                self.availableUsers(list);
            });
    };

    self.loadAvailableSorular = function (customerId) {
        $.get('/api/gm/sorular?customerId=' + customerId)
            .done(function (data) {
                self.availableSorular(data.filter(function (s) { return s.isActive; }));
            });
    };

    // CRUD
    self.showCreateModal = function () {
        self.isEditing(false);
        self.editingId(null);
        self.form.customerId(self.selectedCustomerId());
        self.form.ad('');
        self.form.baslangicTarihi('');
        self.form.bitisTarihi('');
        $('#donemModal').modal('show');
    };

    self.showEditModal = function (donem) {
        self.isEditing(true);
        self.editingId(donem.id);
        self.form.customerId(donem.customerId);
        self.form.ad(donem.ad);
        self.form.baslangicTarihi(donem.baslangicTarihi ? donem.baslangicTarihi.substring(0, 10) : '');
        self.form.bitisTarihi(donem.bitisTarihi ? donem.bitisTarihi.substring(0, 10) : '');
        $('#donemModal').modal('show');
    };

    self.saveDonem = function () {
        if (!self.form.ad() || !self.form.baslangicTarihi() || !self.form.bitisTarihi()) {
            toastr.warning('Tüm alanları doldurun.');
            return;
        }
        self.isSaving(true);

        var data = {
            ad: self.form.ad(),
            baslangicTarihi: self.form.baslangicTarihi(),
            bitisTarihi: self.form.bitisTarihi()
        };

        if (self.isEditing()) {
            $.ajax({
                url: '/api/gm/donemler/' + self.editingId(),
                type: 'PUT',
                contentType: 'application/json',
                data: JSON.stringify(data)
            })
            .done(function () {
                toastr.success('Dönem güncellendi.');
                $('#donemModal').modal('hide');
                self.loadDonemler();
            })
            .fail(function (xhr) { toastr.error(xhr.responseJSON?.message || 'Güncelleme başarısız.'); })
            .always(function () { self.isSaving(false); });
        } else {
            data.customerId = self.form.customerId();
            if (!data.customerId) {
                toastr.warning('Müşteri seçimi zorunludur.');
                self.isSaving(false);
                return;
            }
            $.ajax({
                url: '/api/gm/donemler',
                type: 'POST',
                contentType: 'application/json',
                data: JSON.stringify(data)
            })
            .done(function () {
                toastr.success('Dönem oluşturuldu.');
                $('#donemModal').modal('hide');
                self.loadDonemler();
            })
            .fail(function (xhr) { toastr.error(xhr.responseJSON?.message || 'Oluşturma başarısız.'); })
            .always(function () { self.isSaving(false); });
        }
    };

    self.deleteDonem = function (donem) {
        showDeleteConfirm(donem.donemAdi, function () {
            $.ajax({ url: '/api/gm/donemler/' + donem.id, type: 'DELETE' })
                .done(function () { toastr.success('Dönem silindi.'); self.loadDonemler(); })
                .fail(function (xhr) { toastr.error(xhr.responseJSON?.message || 'Silme başarısız.'); });
        });
    };

    // Personel
    self.addPersonel = function () {
        if (!self.newPersonelUserId() || !self.selectedDonem()) return;
        $.ajax({
            url: '/api/gm/donemler/' + self.selectedDonem().id + '/personel',
            type: 'POST',
            contentType: 'application/json',
            data: JSON.stringify({ userId: self.newPersonelUserId() })
        })
        .done(function () {
            toastr.success('Personel eklendi.');
            self.newPersonelUserId(null);
            self.loadDonemDetail(self.selectedDonem().id);
        })
        .fail(function (xhr) { toastr.error(xhr.responseJSON?.message || 'Ekleme başarısız.'); });
    };

    self.removePersonel = function (personel) {
        $.ajax({ url: '/api/gm/donem-personel/' + personel.id, type: 'DELETE' })
            .done(function () { toastr.success('Personel çıkarıldı.'); self.loadDonemDetail(self.selectedDonem().id); })
            .fail(function (xhr) { toastr.error(xhr.responseJSON?.message || 'Çıkarma başarısız.'); });
    };

    // Soru
    self.addSoru = function () {
        if (!self.newSoruId() || !self.selectedDonem()) return;
        $.ajax({
            url: '/api/gm/donemler/' + self.selectedDonem().id + '/soru',
            type: 'POST',
            contentType: 'application/json',
            data: JSON.stringify({ soruId: self.newSoruId(), aranmaSayisi: parseInt(self.newSoruAranma()) || 1 })
        })
        .done(function () {
            toastr.success('Soru eklendi.');
            self.newSoruId(null);
            self.newSoruAranma(1);
            self.loadDonemDetail(self.selectedDonem().id);
        })
        .fail(function (xhr) { toastr.error(xhr.responseJSON?.message || 'Ekleme başarısız.'); });
    };

    self.removeSoru = function (soru) {
        $.ajax({ url: '/api/gm/donem-soru/' + soru.id, type: 'DELETE' })
            .done(function () { toastr.success('Soru çıkarıldı.'); self.loadDonemDetail(self.selectedDonem().id); })
            .fail(function (xhr) { toastr.error(xhr.responseJSON?.message || 'Çıkarma başarısız.'); });
    };

    // Kupon
    self.addKuponlar = function () {
        if (!self.newKuponText() || !self.selectedDonem()) return;
        var kodlar = self.newKuponText().split('\n').map(function (s) { return s.trim(); }).filter(function (s) { return s; });
        if (!kodlar.length) return;

        $.ajax({
            url: '/api/gm/donemler/' + self.selectedDonem().id + '/kuponlar',
            type: 'POST',
            contentType: 'application/json',
            data: JSON.stringify({ kuponKodlari: kodlar })
        })
        .done(function () {
            toastr.success(kodlar.length + ' kupon eklendi.');
            self.newKuponText('');
            self.loadDonemDetail(self.selectedDonem().id);
        })
        .fail(function (xhr) { toastr.error(xhr.responseJSON?.message || 'Ekleme başarısız.'); });
    };

    self.removeKupon = function (kupon) {
        $.ajax({ url: '/api/gm/donem-kupon/' + kupon.id, type: 'DELETE' })
            .done(function () { toastr.success('Kupon çıkarıldı.'); self.loadDonemDetail(self.selectedDonem().id); })
            .fail(function (xhr) { toastr.error(xhr.responseJSON?.message || 'Çıkarma başarısız.'); });
    };

    // Aktif Et & Tamamla
    self.aktifEt = function () {
        if (!self.selectedDonem()) return;
        showConfirmModal({
            title: 'Dönem Aktifleştirme',
            message: 'Dönemi aktif etmek istediğinize emin misiniz? Bu işlem atamaları oluşturacaktır.',
            type: 'warning',
            confirmText: 'Evet, Aktif Et',
            confirmIcon: 'bi-play-circle',
            onConfirm: function () {
                $.ajax({
                    url: '/api/gm/donemler/' + self.selectedDonem().id + '/aktif-et',
                    type: 'POST'
                })
                .done(function (data) {
                    toastr.success(data.message || 'Dönem aktif edildi.');
                    self.closeDonem();
                })
                .fail(function (xhr) { toastr.error(xhr.responseJSON?.message || 'Aktif etme başarısız.'); });
            }
        });
    };

    self.tamamla = function () {
        if (!self.selectedDonem()) return;
        showConfirmModal({
            title: 'Dönem Tamamlama',
            message: 'Dönemi tamamlamak istediğinize emin misiniz?',
            type: 'success',
            confirmText: 'Evet, Tamamla',
            confirmIcon: 'bi-check-circle',
            onConfirm: function () {
                $.ajax({
                    url: '/api/gm/donemler/' + self.selectedDonem().id + '/tamamla',
                    type: 'POST'
                })
                .done(function () {
                    toastr.success('Dönem tamamlandı.');
                    self.closeDonem();
                })
                .fail(function (xhr) { toastr.error(xhr.responseJSON?.message || 'Tamamlama başarısız.'); });
            }
        });
    };

    // Init
    self.loadCustomers();
    self.loadDonemler();
}

$(function () {
    ko.applyBindings(new DonemlerViewModel(), document.getElementById('donemler-app'));
});
