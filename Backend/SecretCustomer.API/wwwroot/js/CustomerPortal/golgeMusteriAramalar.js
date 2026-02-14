function GmAramalarViewModel() {
    var self = this;

    self.aramalar = ko.observableArray([]);
    self.donemler = ko.observableArray([]);
    self.isLoading = ko.observable(true);
    self.selectedDonemId = ko.observable(null);
    self.selectedArama = ko.observable(null);

    self.selectedDonemId.subscribe(function () {
        self.loadAramalar();
    });

    self.loadDonemler = function () {
        customerApiFetch('/api/customer/portal/gm-donemler')
            .then(function (res) { return res.json(); })
            .then(function (data) {
                self.donemler(data);
            })
            .catch(function () {
                toastr.error('Dönemler yüklenirken hata oluştu.');
            });
    };

    self.loadAramalar = function () {
        self.isLoading(true);
        var url = '/api/customer/portal/gm-aramalar';
        if (self.selectedDonemId()) {
            url += '?donemId=' + self.selectedDonemId();
        }

        customerApiFetch(url)
            .then(function (res) { return res.json(); })
            .then(function (data) {
                self.aramalar(data);
            })
            .catch(function () {
                toastr.error('Aramalar yüklenirken hata oluştu.');
            })
            .finally(function () {
                self.isLoading(false);
            });
    };

    self.showDetail = function (arama) {
        self.selectedArama(arama);
        $('#aramaDetailModal').modal('show');
    };

    // Init
    self.loadDonemler();
    self.loadAramalar();
}

$(function () {
    ko.applyBindings(new GmAramalarViewModel(), document.getElementById('gm-aramalar-app'));
});
