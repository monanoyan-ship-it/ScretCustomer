function DonemlerViewModel() {
    var self = this;

    self.donemler = ko.observableArray([]);
    self.availableUsers = ko.observableArray([]);
    self.customers = ko.observableArray([]);
    self.hedefFirmalar = ko.observableArray([]);
    self.isLoading = ko.observable(true);
    self.isSaving = ko.observable(false);
    self.isEditing = ko.observable(false);
    self.editingId = ko.observable(null);
    self.selectedDonem = ko.observable(null);
    self.donemDetail = ko.observable(null);

    // Dönem alt yönetim
    self.newPersonelUserId = ko.observable(null);

    self.form = {
        ad: ko.observable(''),
        baslangicTarihi: ko.observable(''),
        bitisTarihi: ko.observable('')
    };

    // =============================================
    // SORU EKLEME/DÜZENLEME
    // =============================================
    self.soruIsEditing = ko.observable(false);
    self.soruEditingId = ko.observable(null);
    self.soruForm = {
        customerId: ko.observable(null),
        gmHedefFirmaId: ko.observable(null),
        soruMetni: ko.observable(''),
        beklenenCevap: ko.observable(''),
        aranmaSayisi: ko.observable(1),
        isKuponlu: ko.observable(false),
        kuponKodu: ko.observable(''),
        siraNo: ko.observable(0)
    };

    // Müşteri değişince hedef firmaları yükle
    self.soruForm.customerId.subscribe(function (customerId) {
        self.soruForm.gmHedefFirmaId(null);
        if (customerId) {
            $.get('/api/gm/hedef-firmalar?customerId=' + customerId)
                .done(function (data) { self.hedefFirmalar(data); });
        } else {
            self.hedefFirmalar([]);
        }
    });

    self.showAddSoruModal = function () {
        self.soruIsEditing(false);
        self.soruEditingId(null);
        self.soruForm.customerId(null);
        self.soruForm.gmHedefFirmaId(null);
        self.soruForm.soruMetni('');
        self.soruForm.beklenenCevap('');
        self.soruForm.aranmaSayisi(1);
        self.soruForm.isKuponlu(false);
        self.soruForm.kuponKodu('');
        self.soruForm.siraNo(0);
        self.hedefFirmalar([]);

        if (!self.customers().length) {
            $.get('/api/customers')
                .done(function (data) {
                    var list = Array.isArray(data) ? data : (data.items || []);
                    self.customers(list);
                });
        }

        $('#soruEkleModal').modal('show');
    };

    self.showEditSoruModal = function (soru) {
        self.soruIsEditing(true);
        self.soruEditingId(soru.id);
        self.soruForm.customerId(soru.customerId);
        self.soruForm.gmHedefFirmaId(soru.gmHedefFirmaId);
        self.soruForm.soruMetni(soru.soruMetni || '');
        self.soruForm.beklenenCevap(soru.beklenenCevap || '');
        self.soruForm.aranmaSayisi(soru.aranmaSayisi);
        self.soruForm.isKuponlu(soru.isKuponlu);
        self.soruForm.kuponKodu(soru.kuponKodu || '');
        self.soruForm.siraNo(soru.siraNo);

        if (!self.customers().length) {
            $.get('/api/customers')
                .done(function (data) {
                    var list = Array.isArray(data) ? data : (data.items || []);
                    self.customers(list);
                });
        }

        $('#soruEkleModal').modal('show');
    };

    self.saveSoruForm = function () {
        if (!self.soruForm.soruMetni()) {
            toastr.warning('Soru metni zorunludur.');
            return;
        }
        self.isSaving(true);

        if (self.soruIsEditing()) {
            // Güncelleme
            $.ajax({
                url: '/api/gm/donem-soru/' + self.soruEditingId(),
                type: 'PUT',
                contentType: 'application/json',
                data: JSON.stringify({
                    soruMetni: self.soruForm.soruMetni(),
                    beklenenCevap: self.soruForm.beklenenCevap() || null,
                    aranmaSayisi: parseInt(self.soruForm.aranmaSayisi()) || 1,
                    isKuponlu: self.soruForm.isKuponlu(),
                    kuponKodu: self.soruForm.kuponKodu() || null,
                    siraNo: parseInt(self.soruForm.siraNo()) || 0
                })
            })
            .done(function () {
                toastr.success('Soru güncellendi.');
                $('#soruEkleModal').modal('hide');
                self.loadDonemDetail(self.selectedDonem().id);
            })
            .fail(function (xhr) { toastr.error(xhr.responseJSON?.message || 'Güncelleme başarısız.'); })
            .always(function () { self.isSaving(false); });
        } else {
            // Yeni ekleme
            if (!self.soruForm.customerId() || !self.soruForm.gmHedefFirmaId()) {
                toastr.warning('Müşteri ve hedef firma seçimi zorunludur.');
                self.isSaving(false);
                return;
            }
            $.ajax({
                url: '/api/gm/donemler/' + self.selectedDonem().id + '/soru',
                type: 'POST',
                contentType: 'application/json',
                data: JSON.stringify({
                    customerId: self.soruForm.customerId(),
                    gmHedefFirmaId: self.soruForm.gmHedefFirmaId(),
                    soruMetni: self.soruForm.soruMetni(),
                    beklenenCevap: self.soruForm.beklenenCevap() || null,
                    aranmaSayisi: parseInt(self.soruForm.aranmaSayisi()) || 1,
                    isKuponlu: self.soruForm.isKuponlu(),
                    kuponKodu: self.soruForm.kuponKodu() || null,
                    siraNo: parseInt(self.soruForm.siraNo()) || 0
                })
            })
            .done(function () {
                toastr.success('Soru eklendi.');
                $('#soruEkleModal').modal('hide');
                self.loadDonemDetail(self.selectedDonem().id);
            })
            .fail(function (xhr) { toastr.error(xhr.responseJSON?.message || 'Ekleme başarısız.'); })
            .always(function () { self.isSaving(false); });
        }
    };

    // =============================================
    // EXCEL IMPORT (DÖNEM BAZLI)
    // =============================================
    self.importCustomers = ko.observableArray([]);
    self.importHedefFirmalar = ko.observableArray([]);
    self.importForm = {
        customerId: ko.observable(null),
        hedefFirmaId: ko.observable(null)
    };
    self.isImporting = ko.observable(false);

    self.importForm.customerId.subscribe(function (customerId) {
        self.importForm.hedefFirmaId(null);
        if (customerId) {
            $.get('/api/gm/hedef-firmalar?customerId=' + customerId)
                .done(function (data) { self.importHedefFirmalar(data); });
        } else {
            self.importHedefFirmalar([]);
        }
    });

    self.showImportModal = function () {
        self.importForm.customerId(null);
        self.importForm.hedefFirmaId(null);
        self.importHedefFirmalar([]);
        $('#importFile').val('');

        if (!self.importCustomers().length) {
            $.get('/api/customers')
                .done(function (data) {
                    var list = Array.isArray(data) ? data : (data.items || []);
                    self.importCustomers(list);
                });
        }

        $('#importSoruModal').modal('show');
    };

    self.submitImport = function () {
        if (!self.importForm.customerId() || !self.importForm.hedefFirmaId()) {
            toastr.warning('Müşteri ve hedef firma seçimi zorunludur.');
            return;
        }
        var fileInput = document.getElementById('importFile');
        if (!fileInput.files.length) {
            toastr.warning('Excel dosyası seçin.');
            return;
        }

        var formData = new FormData();
        formData.append('customerId', self.importForm.customerId());
        formData.append('hedefFirmaId', self.importForm.hedefFirmaId());
        formData.append('file', fileInput.files[0]);

        self.isImporting(true);
        $.ajax({
            url: '/api/gm/donemler/' + self.selectedDonem().id + '/sorular/import',
            type: 'POST',
            data: formData,
            processData: false,
            contentType: false
        })
        .done(function (data) {
            toastr.success(data.message || 'Import tamamlandı.');
            if (data.errors && data.errors.length > 0) {
                data.errors.forEach(function (e) { toastr.warning(e); });
            }
            $('#importSoruModal').modal('hide');
            self.loadDonemDetail(self.selectedDonem().id);
        })
        .fail(function (xhr) { toastr.error(xhr.responseJSON?.message || 'Import başarısız.'); })
        .always(function () { self.isImporting(false); });
    };

    // =============================================
    // EXCEL IMPORT WITH MATCHING (HEDEF FİRMA EŞLEŞTİRMELİ)
    // =============================================
    self.isImportingMatch = ko.observable(false);

    // Sonuç modal
    self.importResultUnmatched = ko.observableArray([]);
    self.importResultMatched = ko.observableArray([]);
    self.importResultHedefFirmalar = ko.observableArray([]);
    self.isSavingUnmatched = ko.observable(false);

    self.showImportMatchModal = function () {
        $('#importMatchFile').val('');
        $('#importMatchModal').modal('show');
    };

    self.submitImportMatch = function () {
        var fileInput = document.getElementById('importMatchFile');
        if (!fileInput.files.length) {
            toastr.warning('Excel dosyası seçin.');
            return;
        }

        var formData = new FormData();
        formData.append('file', fileInput.files[0]);

        self.isImportingMatch(true);
        $.ajax({
            url: '/api/gm/donemler/' + self.selectedDonem().id + '/sorular/import-with-matching',
            type: 'POST',
            data: formData,
            processData: false,
            contentType: false
        })
        .done(function (data) {
            if (data.errors && data.errors.length > 0) {
                data.errors.forEach(function (e) { toastr.warning(e); });
            }

            $('#importMatchModal').modal('hide');

            // Tüm hedef firmaları yükle (eşleşmeyenler için dropdown)
            $.get('/api/gm/hedef-firmalar')
                .done(function (firmalar) {
                    firmalar.forEach(function (f) {
                        f.displayName = f.firmaAdi + (f.customerName ? ' (' + f.customerName + ')' : '');
                    });
                    self.importResultHedefFirmalar(firmalar);
                });

            // Unmatched: her satıra selectedHedefFirmaId observable ekle
            var unmatchedItems = (data.unmatched || []).map(function (item) {
                return {
                    rowIndex: item.rowIndex,
                    excelHedefFirmaAdi: item.excelHedefFirmaAdi,
                    soruMetni: item.soruMetni,
                    beklenenCevap: item.beklenenCevap,
                    aranmaSayisi: item.aranmaSayisi,
                    selectedHedefFirmaId: ko.observable(null)
                };
            });

            self.importResultUnmatched(unmatchedItems);
            self.importResultMatched(data.matched || []);

            var msg = data.imported + ' soru eklendi.';
            if (unmatchedItems.length > 0) {
                msg += ' ' + unmatchedItems.length + ' soru için hedef firma eşleşmedi.';
            }
            toastr.info(msg);

            self.loadDonemDetail(self.selectedDonem().id);
            $('#importResultModal').modal('show');
        })
        .fail(function (xhr) { toastr.error(xhr.responseJSON?.message || 'Import başarısız.'); })
        .always(function () { self.isImportingMatch(false); });
    };

    self.saveUnmatchedSorular = function () {
        var items = [];
        var firmalar = self.importResultHedefFirmalar();
        self.importResultUnmatched().forEach(function (item) {
            if (item.selectedHedefFirmaId()) {
                // Seçilen firmadan customerId'yi bul
                var firma = firmalar.find(function (f) { return f.id === item.selectedHedefFirmaId(); });
                items.push({
                    gmHedefFirmaId: item.selectedHedefFirmaId(),
                    soruMetni: item.soruMetni,
                    beklenenCevap: item.beklenenCevap,
                    aranmaSayisi: item.aranmaSayisi,
                    customerId: firma ? firma.customerId : 0
                });
            }
        });

        if (items.length === 0) {
            toastr.warning('En az bir satır için hedef firma seçin.');
            return;
        }

        self.isSavingUnmatched(true);
        $.ajax({
            url: '/api/gm/donemler/' + self.selectedDonem().id + '/sorular/save-unmatched',
            type: 'POST',
            contentType: 'application/json',
            data: JSON.stringify(items)
        })
        .done(function (data) {
            toastr.success(data.message || 'Sorular kaydedildi.');
            $('#importResultModal').modal('hide');
            self.loadDonemDetail(self.selectedDonem().id);
        })
        .fail(function (xhr) { toastr.error(xhr.responseJSON?.message || 'Kaydetme başarısız.'); })
        .always(function () { self.isSavingUnmatched(false); });
    };

    // =============================================
    // DÖNEM LİSTESİ
    // =============================================

    self.loadDonemler = function () {
        self.isLoading(true);
        $.get('/api/gm/donemler')
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
                self.loadAvailableUsers();
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

    // CRUD
    self.showCreateModal = function () {
        self.isEditing(false);
        self.editingId(null);
        self.form.ad('');
        self.form.baslangicTarihi('');
        self.form.bitisTarihi('');
        $('#donemModal').modal('show');
    };

    self.showEditModal = function (donem) {
        self.isEditing(true);
        self.editingId(donem.id);
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
        showDeleteConfirm(donem.ad, function () {
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

    // Soru çıkar
    self.removeSoru = function (soru) {
        $.ajax({ url: '/api/gm/donem-soru/' + soru.id, type: 'DELETE' })
            .done(function () { toastr.success('Soru çıkarıldı.'); self.loadDonemDetail(self.selectedDonem().id); })
            .fail(function (xhr) { toastr.error(xhr.responseJSON?.message || 'Çıkarma başarısız.'); });
    };

    // Kopyala
    self.copySourceDonemId = ko.observable(null);
    self.copyForm = {
        ad: ko.observable(''),
        baslangicTarihi: ko.observable(''),
        bitisTarihi: ko.observable('')
    };

    self.showCopyDonem = function (donem) {
        self.copySourceDonemId(donem.id);
        self.copyForm.ad(donem.ad + ' (Kopya)');
        self.copyForm.baslangicTarihi('');
        self.copyForm.bitisTarihi('');
        $('#copyDonemModal').modal('show');
    };

    self.saveCopyDonem = function () {
        if (!self.copyForm.ad() || !self.copyForm.baslangicTarihi() || !self.copyForm.bitisTarihi()) {
            toastr.warning('Tüm alanları doldurun.');
            return;
        }
        self.isSaving(true);

        $.ajax({
            url: '/api/gm/donemler/' + self.copySourceDonemId() + '/kopyala',
            type: 'POST',
            contentType: 'application/json',
            data: JSON.stringify({
                ad: self.copyForm.ad(),
                baslangicTarihi: self.copyForm.baslangicTarihi(),
                bitisTarihi: self.copyForm.bitisTarihi()
            })
        })
        .done(function (data) {
            toastr.success(data.message || 'Dönem kopyalandı.');
            $('#copyDonemModal').modal('hide');
            self.loadDonemler();
        })
        .fail(function (xhr) { toastr.error(xhr.responseJSON?.message || 'Kopyalama başarısız.'); })
        .always(function () { self.isSaving(false); });
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
    self.loadDonemler();
}

$(function () {
    ko.applyBindings(new DonemlerViewModel(), document.getElementById('donemler-app'));
});
