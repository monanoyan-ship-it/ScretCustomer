function HedefFirmalarViewModel() {
    var self = this;

    self.firmalar = ko.observableArray([]);
    self.customers = ko.observableArray([]);
    self.isLoading = ko.observable(true);
    self.isSaving = ko.observable(false);
    self.isEditing = ko.observable(false);
    self.editingId = ko.observable(null);
    self.selectedCustomerId = ko.observable(null);

    self.form = {
        customerId: ko.observable(null),
        firmaAdi: ko.observable(''),
        telefonNo: ko.observable(''),
        aciklama: ko.observable(''),
        isActive: ko.observable(true)
    };

    // Müşteri değişince yeniden yükle
    self.selectedCustomerId.subscribe(function () {
        self.loadFirmalar();
    });

    self.loadCustomers = function () {
        $.get('/api/customers')
            .done(function (data) {
                var list = Array.isArray(data) ? data : (data.items || []);
                self.customers(list);
            });
    };

    self.loadFirmalar = function () {
        self.isLoading(true);
        var url = '/api/gm/hedef-firmalar';
        if (self.selectedCustomerId()) {
            url += '?customerId=' + self.selectedCustomerId();
        }
        $.get(url)
            .done(function (data) {
                self.firmalar(data);
            })
            .fail(function () {
                toastr.error('Firmalar yüklenirken hata oluştu.');
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
        $('#firmaModal').modal('show');
    };

    self.showEditModal = function (firma) {
        self.isEditing(true);
        self.editingId(firma.id);
        self.form.customerId(firma.customerId);
        self.form.firmaAdi(firma.firmaAdi);
        self.form.telefonNo(firma.telefonNo || '');
        self.form.aciklama(firma.aciklama || '');
        self.form.isActive(firma.isActive);
        $('#firmaModal').modal('show');
    };

    self.resetForm = function () {
        self.form.customerId(null);
        self.form.firmaAdi('');
        self.form.telefonNo('');
        self.form.aciklama('');
        self.form.isActive(true);
    };

    self.saveFirma = function () {
        if (!self.form.firmaAdi()) {
            toastr.warning('Firma adı zorunludur.');
            return;
        }

        self.isSaving(true);

        if (self.isEditing()) {
            $.ajax({
                url: '/api/gm/hedef-firmalar/' + self.editingId(),
                type: 'PUT',
                contentType: 'application/json',
                data: JSON.stringify({
                    firmaAdi: self.form.firmaAdi(),
                    telefonNo: self.form.telefonNo() || null,
                    aciklama: self.form.aciklama() || null,
                    isActive: self.form.isActive()
                })
            })
            .done(function () {
                toastr.success('Firma güncellendi.');
                $('#firmaModal').modal('hide');
                self.loadFirmalar();
            })
            .fail(function (xhr) {
                toastr.error(xhr.responseJSON?.message || 'Güncelleme başarısız.');
            })
            .always(function () {
                self.isSaving(false);
            });
        } else {
            if (!self.form.customerId()) {
                toastr.warning('Müşteri seçimi zorunludur.');
                self.isSaving(false);
                return;
            }
            $.ajax({
                url: '/api/gm/hedef-firmalar',
                type: 'POST',
                contentType: 'application/json',
                data: JSON.stringify({
                    customerId: self.form.customerId(),
                    firmaAdi: self.form.firmaAdi(),
                    telefonNo: self.form.telefonNo() || null,
                    aciklama: self.form.aciklama() || null,
                    isActive: self.form.isActive()
                })
            })
            .done(function () {
                toastr.success('Firma oluşturuldu.');
                $('#firmaModal').modal('hide');
                self.loadFirmalar();
            })
            .fail(function (xhr) {
                toastr.error(xhr.responseJSON?.message || 'Oluşturma başarısız.');
            })
            .always(function () {
                self.isSaving(false);
            });
        }
    };

    self.deleteFirma = function (firma) {
        showDeleteConfirm(firma.firmaAdi, function () {
            $.ajax({
                url: '/api/gm/hedef-firmalar/' + firma.id,
                type: 'DELETE'
            })
            .done(function () {
                toastr.success('Firma silindi.');
                self.loadFirmalar();
            })
            .fail(function (xhr) {
                toastr.error(xhr.responseJSON?.message || 'Silme başarısız.');
            });
        });
    };

    // Init
    self.loadCustomers();
    self.loadFirmalar();
}

$(function () {
    ko.applyBindings(new HedefFirmalarViewModel(), document.getElementById('hedef-firmalar-app'));
});
