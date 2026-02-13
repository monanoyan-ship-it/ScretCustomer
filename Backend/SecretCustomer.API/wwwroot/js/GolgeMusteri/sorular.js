function SorularViewModel() {
    var self = this;

    self.sorular = ko.observableArray([]);
    self.customers = ko.observableArray([]);
    self.hedefFirmalar = ko.observableArray([]);
    self.modalHedefFirmalar = ko.observableArray([]);
    self.isLoading = ko.observable(true);
    self.isSaving = ko.observable(false);
    self.isEditing = ko.observable(false);
    self.editingId = ko.observable(null);
    self.selectedCustomerId = ko.observable(null);
    self.selectedHedefFirmaId = ko.observable(null);

    self.form = {
        customerId: ko.observable(null),
        gmHedefFirmaId: ko.observable(null),
        soruMetni: ko.observable(''),
        beklenenCevap: ko.observable(''),
        aranmaSayisi: ko.observable(1),
        isKuponlu: ko.observable(false),
        siraNo: ko.observable(0),
        isActive: ko.observable(true)
    };

    // Cascading: müşteri değişince firmalar yükle
    self.selectedCustomerId.subscribe(function (customerId) {
        self.selectedHedefFirmaId(null);
        if (customerId) {
            self.loadHedefFirmalar(customerId, self.hedefFirmalar);
        } else {
            self.hedefFirmalar([]);
        }
        self.loadSorular();
    });

    // Modal: müşteri değişince modal firma listesini yükle
    self.form.customerId.subscribe(function (customerId) {
        self.form.gmHedefFirmaId(null);
        if (customerId) {
            self.loadHedefFirmalar(customerId, self.modalHedefFirmalar);
        } else {
            self.modalHedefFirmalar([]);
        }
    });

    self.selectedHedefFirmaId.subscribe(function () {
        self.loadSorular();
    });

    self.loadCustomers = function () {
        $.get('/api/customers')
            .done(function (data) {
                var list = Array.isArray(data) ? data : (data.items || []);
                self.customers(list);
            });
    };

    self.loadHedefFirmalar = function (customerId, targetArray) {
        $.get('/api/gm/hedef-firmalar?customerId=' + customerId)
            .done(function (data) {
                targetArray(data);
            });
    };

    self.loadSorular = function () {
        self.isLoading(true);
        var params = [];
        if (self.selectedCustomerId()) params.push('customerId=' + self.selectedCustomerId());
        if (self.selectedHedefFirmaId()) params.push('hedefFirmaId=' + self.selectedHedefFirmaId());
        var url = '/api/gm/sorular' + (params.length ? '?' + params.join('&') : '');

        $.get(url)
            .done(function (data) {
                self.sorular(data);
            })
            .fail(function () {
                toastr.error('Sorular yüklenirken hata oluştu.');
            })
            .always(function () {
                self.isLoading(false);
            });
    };

    self.showCreateModal = function () {
        self.isEditing(false);
        self.editingId(null);
        self.resetForm();
        if (self.selectedCustomerId()) {
            self.form.customerId(self.selectedCustomerId());
        }
        $('#soruModal').modal('show');
    };

    self.showEditModal = function (soru) {
        self.isEditing(true);
        self.editingId(soru.id);
        self.form.customerId(soru.customerId);
        self.form.gmHedefFirmaId(soru.gmHedefFirmaId);
        self.form.soruMetni(soru.soruMetni);
        self.form.beklenenCevap(soru.beklenenCevap || '');
        self.form.aranmaSayisi(soru.aranmaSayisi);
        self.form.isKuponlu(soru.isKuponlu);
        self.form.siraNo(soru.siraNo);
        self.form.isActive(soru.isActive);
        $('#soruModal').modal('show');
    };

    self.resetForm = function () {
        self.form.customerId(null);
        self.form.gmHedefFirmaId(null);
        self.form.soruMetni('');
        self.form.beklenenCevap('');
        self.form.aranmaSayisi(1);
        self.form.isKuponlu(false);
        self.form.siraNo(0);
        self.form.isActive(true);
    };

    self.saveSoru = function () {
        if (!self.form.soruMetni()) {
            toastr.warning('Soru metni zorunludur.');
            return;
        }
        self.isSaving(true);

        if (self.isEditing()) {
            $.ajax({
                url: '/api/gm/sorular/' + self.editingId(),
                type: 'PUT',
                contentType: 'application/json',
                data: JSON.stringify({
                    soruMetni: self.form.soruMetni(),
                    beklenenCevap: self.form.beklenenCevap() || null,
                    aranmaSayisi: parseInt(self.form.aranmaSayisi()) || 1,
                    isKuponlu: self.form.isKuponlu(),
                    siraNo: parseInt(self.form.siraNo()) || 0,
                    isActive: self.form.isActive()
                })
            })
            .done(function () {
                toastr.success('Soru güncellendi.');
                $('#soruModal').modal('hide');
                self.loadSorular();
            })
            .fail(function (xhr) {
                toastr.error(xhr.responseJSON?.message || 'Güncelleme başarısız.');
            })
            .always(function () {
                self.isSaving(false);
            });
        } else {
            if (!self.form.customerId() || !self.form.gmHedefFirmaId()) {
                toastr.warning('Müşteri ve hedef firma seçimi zorunludur.');
                self.isSaving(false);
                return;
            }
            $.ajax({
                url: '/api/gm/sorular',
                type: 'POST',
                contentType: 'application/json',
                data: JSON.stringify({
                    customerId: self.form.customerId(),
                    gmHedefFirmaId: self.form.gmHedefFirmaId(),
                    soruMetni: self.form.soruMetni(),
                    beklenenCevap: self.form.beklenenCevap() || null,
                    aranmaSayisi: parseInt(self.form.aranmaSayisi()) || 1,
                    isKuponlu: self.form.isKuponlu(),
                    siraNo: parseInt(self.form.siraNo()) || 0,
                    isActive: self.form.isActive()
                })
            })
            .done(function () {
                toastr.success('Soru oluşturuldu.');
                $('#soruModal').modal('hide');
                self.loadSorular();
            })
            .fail(function (xhr) {
                toastr.error(xhr.responseJSON?.message || 'Oluşturma başarısız.');
            })
            .always(function () {
                self.isSaving(false);
            });
        }
    };

    self.deleteSoru = function (soru) {
        if (!confirm('Bu soruyu silmek istediğinize emin misiniz?')) return;

        $.ajax({
            url: '/api/gm/sorular/' + soru.id,
            type: 'DELETE'
        })
        .done(function () {
            toastr.success('Soru silindi.');
            self.loadSorular();
        })
        .fail(function (xhr) {
            toastr.error(xhr.responseJSON?.message || 'Silme başarısız.');
        });
    };

    // Init
    self.loadCustomers();
    self.loadSorular();
}

$(function () {
    ko.applyBindings(new SorularViewModel(), document.getElementById('sorular-app'));
});
