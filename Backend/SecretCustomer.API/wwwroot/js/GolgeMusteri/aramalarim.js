function AramalarimViewModel() {
    var self = this;

    self.atamalar = ko.observableArray([]);
    self.isLoading = ko.observable(true);
    self.isSaving = ko.observable(false);
    self.completingAtama = ko.observable(null);

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

    self.loadAtamalar = function () {
        self.isLoading(true);
        $.get('/api/gm/aramalarim')
            .done(function (data) { self.atamalar(data); })
            .fail(function () { toastr.error('Aramalar yüklenirken hata oluştu.'); })
            .always(function () { self.isLoading(false); });
    };

    self.showCompleteModal = function (atama) {
        self.completingAtama(atama);
        // Bugünün tarihini varsayılan yap
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
    self.loadAtamalar();
}

$(function () {
    ko.applyBindings(new AramalarimViewModel(), document.getElementById('aramalarim-app'));
});
