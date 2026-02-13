function TakipViewModel() {
    var self = this;

    self.donemler = ko.observableArray([]);
    self.users = ko.observableArray([]);
    self.atamalar = ko.observableArray([]);
    self.isLoading = ko.observable(false);
    self.selectedDonemId = ko.observable(null);
    self.selectedUserId = ko.observable(null);
    self.selectedDurumId = ko.observable('');

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

    self.selectedDonemId.subscribe(function () { self.loadAtamalar(); });
    self.selectedUserId.subscribe(function () { self.loadAtamalar(); });
    self.selectedDurumId.subscribe(function () { self.loadAtamalar(); });

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
        self.isLoading(true);
        var params = ['donemId=' + self.selectedDonemId()];
        if (self.selectedUserId()) params.push('userId=' + self.selectedUserId());
        if (self.selectedDurumId()) params.push('durumId=' + self.selectedDurumId());

        $.get('/api/gm/atamalar?' + params.join('&'))
            .done(function (data) { self.atamalar(data); })
            .fail(function () { toastr.error('Atamalar yüklenirken hata oluştu.'); })
            .always(function () { self.isLoading(false); });
    };

    // Init
    self.loadDonemler();
    self.loadUsers();
}

$(function () {
    ko.applyBindings(new TakipViewModel(), document.getElementById('takip-app'));
});
