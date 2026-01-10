function LanguagesViewModel() {
    var self = this;

    // State
    self.languages = ko.observableArray([]);
    self.availableXmlFiles = ko.observableArray([]);
    self.isLoading = ko.observable(true);
    self.isSaving = ko.observable(false);
    self.isEditing = ko.observable(false);
    self.editingId = ko.observable(null);
    self.modalErrorMessage = ko.observable('');
    self.isClearingCache = ko.observable(false);
    self.isImporting = ko.observable(false);

    // Form
    self.form = {
        name: ko.observable(''),
        languageCulture: ko.observable(''),
        uniqueSeoCode: ko.observable(''),
        flagImageFileName: ko.observable(''),
        rtl: ko.observable(false),
        isDefault: ko.observable(false),
        isActive: ko.observable(true),
        displayOrder: ko.observable(0)
    };

    // Resources
    self.selectedLanguage = ko.observable(null);
    self.resources = ko.observableArray([]);
    self.resourceSearch = ko.observable('');
    self.resourcePrefix = ko.observable('');
    self.editingResourceId = ko.observable(null);
    self.resourceForm = {
        name: ko.observable(''),
        value: ko.observable('')
    };

    // Filtered resources
    self.filteredResources = ko.computed(function () {
        var search = (self.resourceSearch() || '').toLowerCase();
        var prefix = self.resourcePrefix() || '';

        return self.resources().filter(function (r) {
            var matchesSearch = !search || r.resourceName.toLowerCase().indexOf(search) > -1 || r.resourceValue.toLowerCase().indexOf(search) > -1;
            var matchesPrefix = !prefix || r.resourceName.startsWith(prefix);
            return matchesSearch && matchesPrefix;
        });
    });

    // Load languages
    self.loadLanguages = function () {
        self.isLoading(true);
        $.get('/api/localization/languages?onlyActive=false')
            .done(function (data) {
                // Load resource counts for each language
                var promises = data.map(function (lang) {
                    return $.get('/api/localization/resources/' + lang.id).then(function (resources) {
                        lang.resourceCount = resources.length;
                        return lang;
                    });
                });

                Promise.all(promises).then(function (langsWithCounts) {
                    self.languages(langsWithCounts);
                    self.isLoading(false);
                });
            })
            .fail(function () {
                toastr.error(T('Language.LoadError', 'Diller yüklenirken hata oluştu.'));
                self.isLoading(false);
            });
    };

    // Load available XML files
    self.loadAvailableXmlFiles = function () {
        $.get('/api/localization/available-xml-files')
            .done(function (data) {
                self.availableXmlFiles(data);
            });
    };

    // Clear localization cache
    self.clearCache = function () {
        self.isClearingCache(true);
        $.post('/api/localization/cache/clear')
            .done(function () {
                toastr.success(T('Language.CacheClearedSuccess', 'Localization cache temizlendi.'));
            })
            .fail(function () {
                toastr.error(T('Language.CacheClearError', 'Cache temizlenirken hata oluştu.'));
            })
            .always(function () {
                self.isClearingCache(false);
            });
    };

    // Show create modal
    self.showCreateModal = function () {
        self.isEditing(false);
        self.editingId(null);
        self.resetForm();
        $('#languageModal').modal('show');
    };

    // Show edit modal
    self.showEditModal = function (language) {
        self.isEditing(true);
        self.editingId(language.id);
        self.form.name(language.name);
        self.form.languageCulture(language.languageCulture);
        self.form.uniqueSeoCode(language.uniqueSeoCode);
        self.form.flagImageFileName(language.flagImageFileName || '');
        self.form.rtl(language.rtl);
        self.form.isDefault(language.isDefault);
        self.form.isActive(language.isActive);
        self.form.displayOrder(language.displayOrder);
        $('#languageModal').modal('show');
    };

    // Reset form
    self.resetForm = function () {
        self.form.name('');
        self.form.languageCulture('');
        self.form.uniqueSeoCode('');
        self.form.flagImageFileName('');
        self.form.rtl(false);
        self.form.isDefault(false);
        self.form.isActive(true);
        self.form.displayOrder(0);
    };

    // Save language
    self.saveLanguage = function () {
        if (!self.form.name() || !self.form.languageCulture() || !self.form.uniqueSeoCode()) {
            toastr.warning(T('Validation.Required', 'Lütfen zorunlu alanları doldurun.'));
            return;
        }

        self.isSaving(true);

        var data = {
            name: self.form.name(),
            languageCulture: self.form.languageCulture(),
            uniqueSeoCode: self.form.uniqueSeoCode(),
            flagImageFileName: self.form.flagImageFileName() || null,
            rtl: self.form.rtl(),
            isDefault: self.form.isDefault(),
            isActive: self.form.isActive(),
            displayOrder: parseInt(self.form.displayOrder()) || 0
        };

        var url = '/api/localization/languages';
        var method = 'POST';

        if (self.isEditing()) {
            url += '/' + self.editingId();
            method = 'PUT';
        }

        $.ajax({
            url: url,
            method: method,
            contentType: 'application/json',
            data: JSON.stringify(data)
        })
            .done(function (response) {
                var savedLang = response.language || response;
                savedLang.resourceCount = savedLang.resourceCount || 0;

                if (self.isEditing()) {
                    // Güncelleme: array'de bul ve güncelle
                    var list = self.languages();
                    for (var i = 0; i < list.length; i++) {
                        if (list[i].id === savedLang.id) {
                            self.languages.splice(i, 1, savedLang);
                            break;
                        }
                    }
                } else {
                    // Yeni kayıt: array'e ekle
                    self.languages.push(savedLang);
                }
                toastr.success(response.message || T('Language.SaveSuccess', 'Dil kaydedildi.'));
                $('#languageModal').modal('hide');
            })
            .fail(function (xhr) {
                toastr.error(xhr.responseJSON?.message || T('Language.SaveError', 'Kayıt sırasında hata oluştu.'));
            })
            .always(function () {
                self.isSaving(false);
            });
    };

    // Set as default
    self.setAsDefault = function (language) {
        if (language.isDefault) {
            toastr.info(T('Language.AlreadyDefault', 'Bu dil zaten varsayılan.'));
            return;
        }

        showConfirmModal({
            title: T('Language.SetDefaultTitle', 'Varsayılan Dil'),
            message: language.name + ' ' + T('Language.SetDefaultMessage', 'Bu dili varsayılan yapmak istiyor musunuz?'),
            type: 'warning',
            confirmText: T('Language.SetDefault', 'Varsayılan Yap'),
            confirmIcon: 'bi-star-fill',
            onConfirm: function() {
                $.ajax({
                    url: '/api/localization/languages/' + language.id,
                    method: 'PUT',
                    contentType: 'application/json',
                    data: JSON.stringify({
                        name: language.name,
                        languageCulture: language.languageCulture,
                        uniqueSeoCode: language.uniqueSeoCode,
                        flagImageFileName: language.flagImageFileName,
                        rtl: language.rtl,
                        isDefault: true,
                        isActive: language.isActive,
                        displayOrder: language.displayOrder
                    })
                })
                    .done(function (response) {
                        toastr.success(language.name + ' ' + T('Language.SetDefaultSuccess', 'varsayılan dil olarak ayarlandı.'));
                        self.loadLanguages();
                    })
                    .fail(function (xhr) {
                        toastr.error(xhr.responseJSON?.message || T('Message.Error', 'İşlem başarısız.'));
                    });
            }
        });
    };

    // Delete language
    self.deleteLanguage = function (language) {
        if (language.isDefault) {
            toastr.warning(T('Language.CannotDeleteDefault', 'Varsayılan dil silinemez.'));
            return;
        }

        showDeleteConfirm(language.name + ' ' + T('Language.DeleteConfirmMessage', 'dili ve tüm çevirileri'), function() {
            $.ajax({
                url: '/api/localization/languages/' + language.id,
                method: 'DELETE'
            })
                .done(function (response) {
                    // Array'den sil
                    self.languages.remove(function(l) { return l.id === language.id; });
                    toastr.success(response.message || T('Language.DeleteSuccess', 'Dil silindi.'));
                })
                .fail(function (xhr) {
                    toastr.error(xhr.responseJSON?.message || T('Language.DeleteError', 'Silme sırasında hata oluştu.'));
                });
        });
    };

    // Import resources from XML
    self.importResources = function (language) {
        showConfirmModal({
            title: T('Language.ImportXmlTitle', 'XML İçe Aktarma'),
            message: language.name + ' için resources.' + language.uniqueSeoCode + '.xml ' + T('Language.ImportConfirmMessage', 'dosyasından çevirileri içe aktarmak istiyor musunuz?'),
            type: 'info',
            confirmText: T('Language.Import', 'İçe Aktar'),
            confirmIcon: 'bi-file-earmark-arrow-down',
            onConfirm: function() {
                self.isImporting(true);
                $.ajax({
                    url: '/api/localization/import/' + language.id + '/default',
                    method: 'POST'
                })
                .done(function (response) {
                    toastr.success(response.message || (response.count + ' ' + T('Language.ImportSuccess', 'çeviri içe aktarıldı.')));
                    self.loadLanguages();
                })
                .fail(function (xhr) {
                    toastr.error(xhr.responseJSON?.message || T('Language.ImportError', 'İçe aktarma sırasında hata oluştu.'));
                })
                .always(function () {
                    self.isImporting(false);
                });
            }
        });
    };

    // Export resources to XML
    self.exportResources = function (language) {
        window.location.href = '/api/localization/export/' + language.id + '/xml';
    };

    // Show resources modal
    self.showResourcesModal = function (language) {
        self.selectedLanguage(language);
        self.resources([]);
        self.resourceSearch('');
        self.resourcePrefix('');

        $.get('/api/localization/resources/' + language.id)
            .done(function (data) {
                self.resources(data);
                $('#resourcesModal').modal('show');
            })
            .fail(function () {
                toastr.error(T('Language.ResourcesLoadError', 'Kaynaklar yüklenirken hata oluştu.'));
            });
    };

    // Show add resource modal
    self.showAddResourceModal = function () {
        self.editingResourceId(null);
        self.resourceForm.name('');
        self.resourceForm.value('');
        $('#resourceModal').modal('show');
    };

    // Edit resource
    self.editResource = function (resource) {
        self.editingResourceId(resource.id);
        self.resourceForm.name(resource.resourceName);
        self.resourceForm.value(resource.resourceValue);
        $('#resourceModal').modal('show');
    };

    // Save resource
    self.saveResource = function () {
        if (!self.resourceForm.name() || !self.resourceForm.value()) {
            toastr.warning(T('Validation.AllFieldsRequired', 'Lütfen tüm alanları doldurun.'));
            return;
        }

        var langId = self.selectedLanguage().id;

        $.ajax({
            url: '/api/localization/resources/' + langId,
            method: 'POST',
            contentType: 'application/json',
            data: JSON.stringify({
                resourceName: self.resourceForm.name(),
                resourceValue: self.resourceForm.value()
            })
        })
            .done(function (response) {
                toastr.success(T('Language.ResourceSaved', 'Kaynak kaydedildi.'));
                $('#resourceModal').modal('hide');
                // Refresh resources
                self.showResourcesModal(self.selectedLanguage());
                self.loadLanguages(); // Update counts
            })
            .fail(function (xhr) {
                toastr.error(xhr.responseJSON?.message || T('Language.SaveError', 'Kayıt sırasında hata oluştu.'));
            });
    };

    // Delete resource
    self.deleteResource = function (resource) {
        showDeleteConfirm(resource.resourceName + ' ' + T('Language.Resource', 'kaynağı'), function() {
            $.ajax({
                url: '/api/localization/resources/' + resource.id,
                method: 'DELETE'
            })
                .done(function (response) {
                    toastr.success(T('Language.ResourceDeleted', 'Kaynak silindi.'));
                    // Refresh resources
                    self.showResourcesModal(self.selectedLanguage());
                    self.loadLanguages(); // Update counts
                })
                .fail(function (xhr) {
                    toastr.error(xhr.responseJSON?.message || T('Language.DeleteError', 'Silme sırasında hata oluştu.'));
                });
        });
    };

    // Seed default languages
    self.isSeeding = ko.observable(false);
    self.seedDefaultLanguages = function () {
        if (self.isSeeding()) return; // Çift tıklama koruması

        self.isSeeding(true);
        $.ajax({
            url: '/api/localization/languages/seed-defaults',
            method: 'POST'
        })
            .done(function (response) {
                toastr.success(response.message || T('Language.SeedSuccess', 'Varsayılan diller oluşturuldu.'));
                self.loadLanguages();
            })
            .fail(function (xhr) {
                toastr.error(xhr.responseJSON?.message || T('Language.SeedError', 'Diller oluşturulurken hata oluştu.'));
            })
            .always(function () {
                self.isSeeding(false);
            });
    };

    // Initialize
    self.init = function () {
        // Önce EnumsService'i yükle, sonra diğer verileri çek
        EnumsService.load().then(function() {
            self.loadLanguages();
            self.loadAvailableXmlFiles();
        });
    };

    self.init();
}

// Translation keys used in this module
var TRANSLATION_KEYS = [
    // Language module keys
    'Language.LoadError',
    'Language.SaveSuccess',
    'Language.SaveError',
    'Language.AlreadyDefault',
    'Language.SetDefaultTitle',
    'Language.SetDefaultMessage',
    'Language.SetDefault',
    'Language.SetDefaultSuccess',
    'Language.CannotDeleteDefault',
    'Language.DeleteConfirmMessage',
    'Language.DeleteSuccess',
    'Language.DeleteError',
    'Language.ImportXmlTitle',
    'Language.ImportConfirmMessage',
    'Language.Import',
    'Language.ImportSuccess',
    'Language.ImportError',
    'Language.ResourcesLoadError',
    'Language.ResourceSaved',
    'Language.Resource',
    'Language.ResourceDeleted',
    'Language.SeedSuccess',
    'Language.SeedError',
    // Validation keys
    'Validation.Required',
    'Validation.AllFieldsRequired',
    // Common keys
    'Message.Error',
    // Confirm modal keys (used by confirm-modal.js)
    'Confirm.Title',
    'Confirm.Message',
    'Confirm.DeleteTitle',
    'Confirm.DeleteMessage',
    'Confirm.YesDelete',
    'Confirm.CancelTitle',
    'Confirm.CancelMessage',
    'Confirm.YesCancel',
    'Common.Confirm',
    'Common.OK'
];

// Initialize when document is ready
$(document).ready(function () {
    // Load translations first, then initialize ViewModel
    Localization.loadKeys(TRANSLATION_KEYS).then(function() {
        ko.applyBindings(new LanguagesViewModel(), document.getElementById('languages-app'));
    });
});
