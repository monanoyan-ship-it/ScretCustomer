// Visit Details Management ViewModel
// Sektör ve Alan Tanımları Yönetimi

function VisitDetailsViewModel() {
    var self = this;

    // ===== State =====
    self.isLoadingSectors = ko.observable(false);
    self.isLoadingFields = ko.observable(false);
    self.isSaving = ko.observable(false);
    self.errorMessage = ko.observable('');
    self.successMessage = ko.observable('');
    self.modalErrorMessage = ko.observable('');

    // ===== Modal State =====
    self.isSectorModalOpen = ko.observable(false);
    self.isFieldModalOpen = ko.observable(false);
    self.editingSector = ko.observable(null);
    self.editingField = ko.observable(null);

    // ===== Data =====
    self.sectors = ko.observableArray([]);
    self.fields = ko.observableArray([]);
    self.selectedSectorIdForFields = ko.observable(null);

    // ===== Options =====
    self.sectorOptions = ko.computed(function() {
        return self.sectors().filter(function(s) { return s.isActive; });
    });

    self.fieldTypeOptions = [
        { value: 0, name: T('VisitDetails.FieldType.Int', 'Tam Sayı') },
        { value: 1, name: T('VisitDetails.FieldType.Decimal', 'Ondalık Sayı') },
        { value: 2, name: T('VisitDetails.FieldType.Bool', 'Evet/Hayır') },
        { value: 3, name: T('VisitDetails.FieldType.String', 'Metin') },
        { value: 4, name: T('VisitDetails.FieldType.DateTime', 'Tarih/Saat') },
        { value: 5, name: T('VisitDetails.FieldType.Rating', 'Puan (Rating)') }
    ];

    self.categoryOptions = [
        { value: 0, name: T('VisitDetails.Category.Time', 'Zaman') },
        { value: 1, name: T('VisitDetails.Category.Staff', 'Personel') },
        { value: 2, name: T('VisitDetails.Category.Facility', 'Tesis') },
        { value: 3, name: T('VisitDetails.Category.General', 'Genel') },
        { value: 4, name: T('VisitDetails.Category.Sector', 'Sektöre Özel') }
    ];

    // ===== Computed =====
    self.filteredFields = ko.computed(function() {
        var sectorId = self.selectedSectorIdForFields();
        if (!sectorId) {
            return self.fields();
        }
        return self.fields().filter(function(f) {
            return f.sectorId === sectorId || !f.sectorId;
        });
    });

    // ===== Helper Functions =====
    self.getFieldTypeName = function(fieldType) {
        var option = self.fieldTypeOptions.find(function(o) { return o.value === fieldType; });
        return option ? option.name : fieldType;
    };

    self.getCategoryName = function(category) {
        var option = self.categoryOptions.find(function(o) { return o.value === category; });
        return option ? option.name : category;
    };

    self.clearMessages = function() {
        setTimeout(function() {
            self.successMessage('');
        }, 3000);
    };

    // ===== Sector CRUD =====
    self.loadSectors = function() {
        self.isLoadingSectors(true);
        fetch('/api/visit-details/sectors')
            .then(function(r) {
                if (!r.ok) throw new Error(T('VisitDetails.LoadError', 'Veriler yüklenirken hata oluştu.'));
                return r.json();
            })
            .then(function(data) {
                self.sectors(data);
            })
            .catch(function(err) {
                toastr.error(err.message);
            })
            .finally(function() {
                self.isLoadingSectors(false);
            });
    };

    self.createSector = function() {
        self.modalErrorMessage('');
        self.editingSector({
            id: null,
            code: ko.observable(''),
            name: ko.observable(''),
            description: ko.observable(''),
            iconClass: ko.observable('bi-folder'),
            sortOrder: ko.observable(self.sectors().length + 1),
            isActive: ko.observable(true)
        });
        self.isSectorModalOpen(true);
    };

    self.editSector = function(sector) {
        self.modalErrorMessage('');
        self.editingSector({
            id: sector.id,
            code: ko.observable(sector.code),
            name: ko.observable(sector.name),
            description: ko.observable(sector.description || ''),
            iconClass: ko.observable(sector.iconClass || 'bi-folder'),
            sortOrder: ko.observable(sector.sortOrder),
            isActive: ko.observable(sector.isActive)
        });
        self.isSectorModalOpen(true);
    };

    self.closeSectorModal = function() {
        self.isSectorModalOpen(false);
        self.editingSector(null);
    };

    self.saveSector = function() {
        var sector = self.editingSector();
        if (!sector) return;

        var code = sector.code();
        var name = sector.name();

        if (!code || !name) {
            toastr.error(T('VisitDetails.CodeNameRequired', 'Kod ve Ad alanları zorunludur.'));
            return;
        }

        var isNew = !sector.id;
        var url = isNew ? '/api/visit-details/sectors' : '/api/visit-details/sectors/' + sector.id;
        var method = isNew ? 'POST' : 'PUT';

        var payload = {
            code: code.toUpperCase(),
            name: name,
            description: sector.description(),
            iconClass: sector.iconClass(),
            sortOrder: parseInt(sector.sortOrder()) || 0,
            isActive: sector.isActive()
        };

        self.isSaving(true);
        fetch(url, {
            method: method,
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(payload)
        })
        .then(function(r) {
            if (!r.ok) {
                return r.json().then(function(err) {
                    throw new Error(err.message || T('VisitDetails.SaveError', 'Kaydetme hatası.'));
                });
            }
            return r.json();
        })
        .then(function() {
            self.closeSectorModal();
            self.loadSectors();
            toastr.success(isNew ? T('VisitDetails.SectorCreated', 'Sektör oluşturuldu.') : T('VisitDetails.SectorUpdated', 'Sektör güncellendi.'));
            self.clearMessages();
        })
        .catch(function(err) {
            toastr.error(err.message);
        })
        .finally(function() {
            self.isSaving(false);
        });
    };

    self.deleteSector = function(sector) {
        showDeleteConfirm(sector.name, function() {
            fetch('/api/visit-details/sectors/' + sector.id, { method: 'DELETE' })
                .then(function(r) {
                    if (!r.ok) {
                        return r.json().then(function(err) {
                            throw new Error(err.message || T('VisitDetails.DeleteError', 'Silme hatası.'));
                        });
                    }
                    self.loadSectors();
                    toastr.success(T('VisitDetails.SectorDeleted', 'Sektör silindi.'));
                    self.clearMessages();
                })
                .catch(function(err) {
                    toastr.error(err.message);
                });
        });
    };

    self.viewSectorFields = function(sector) {
        // Switch to fields tab and filter by sector
        self.selectedSectorIdForFields(sector.id);
        var fieldsTab = document.getElementById('fields-tab');
        if (fieldsTab) {
            var tab = new bootstrap.Tab(fieldsTab);
            tab.show();
        }
    };

    // ===== Field Definition CRUD =====
    self.loadFields = function() {
        self.isLoadingFields(true);
        fetch('/api/visit-details/fields')
            .then(function(r) {
                if (!r.ok) throw new Error(T('VisitDetails.LoadError', 'Veriler yüklenirken hata oluştu.'));
                return r.json();
            })
            .then(function(data) {
                self.fields(data);
            })
            .catch(function(err) {
                toastr.error(err.message);
            })
            .finally(function() {
                self.isLoadingFields(false);
            });
    };

    self.createField = function() {
        self.modalErrorMessage('');
        self.editingField({
            id: null,
            sectorId: ko.observable(self.selectedSectorIdForFields()),
            code: ko.observable(''),
            name: ko.observable(''),
            description: ko.observable(''),
            fieldType: ko.observable(0),
            category: ko.observable(3),
            isRequired: ko.observable(false),
            maxRating: ko.observable(5),
            maxLength: ko.observable(500),
            minValue: ko.observable(null),
            maxValue: ko.observable(null),
            placeholder: ko.observable(''),
            helpText: ko.observable(''),
            sortOrder: ko.observable(self.filteredFields().length + 1),
            isActive: ko.observable(true)
        });
        self.isFieldModalOpen(true);
    };

    self.editField = function(field) {
        self.modalErrorMessage('');
        self.editingField({
            id: field.id,
            sectorId: ko.observable(field.sectorId),
            code: ko.observable(field.code),
            name: ko.observable(field.name),
            description: ko.observable(field.description || ''),
            fieldType: ko.observable(field.fieldType),
            category: ko.observable(field.category),
            isRequired: ko.observable(field.isRequired),
            maxRating: ko.observable(field.maxRating || 5),
            maxLength: ko.observable(field.maxLength || 500),
            minValue: ko.observable(field.minValue),
            maxValue: ko.observable(field.maxValue),
            placeholder: ko.observable(field.placeholder || ''),
            helpText: ko.observable(field.helpText || ''),
            sortOrder: ko.observable(field.sortOrder),
            isActive: ko.observable(field.isActive)
        });
        self.isFieldModalOpen(true);
    };

    self.closeFieldModal = function() {
        self.isFieldModalOpen(false);
        self.editingField(null);
    };

    self.saveField = function() {
        var field = self.editingField();
        if (!field) return;

        var code = field.code();
        var name = field.name();

        if (!code || !name) {
            toastr.error(T('VisitDetails.CodeNameRequired', 'Kod ve Ad alanları zorunludur.'));
            return;
        }

        var isNew = !field.id;
        var url = isNew ? '/api/visit-details/fields' : '/api/visit-details/fields/' + field.id;
        var method = isNew ? 'POST' : 'PUT';

        var payload = {
            sectorId: field.sectorId() || null,
            code: code.toLowerCase().replace(/\s+/g, '_'),
            name: name,
            description: field.description(),
            fieldType: parseInt(field.fieldType()),
            category: parseInt(field.category()),
            isRequired: field.isRequired(),
            maxRating: field.fieldType() == 5 ? parseInt(field.maxRating()) : null,
            maxLength: field.fieldType() == 3 ? parseInt(field.maxLength()) : null,
            minValue: (field.fieldType() == 0 || field.fieldType() == 1) && field.minValue() ? parseFloat(field.minValue()) : null,
            maxValue: (field.fieldType() == 0 || field.fieldType() == 1) && field.maxValue() ? parseFloat(field.maxValue()) : null,
            placeholder: field.placeholder(),
            helpText: field.helpText(),
            sortOrder: parseInt(field.sortOrder()) || 0,
            isActive: field.isActive()
        };

        self.isSaving(true);
        fetch(url, {
            method: method,
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(payload)
        })
        .then(function(r) {
            if (!r.ok) {
                return r.json().then(function(err) {
                    throw new Error(err.message || T('VisitDetails.SaveError', 'Kaydetme hatası.'));
                });
            }
            return r.json();
        })
        .then(function() {
            self.closeFieldModal();
            self.loadFields();
            toastr.success(isNew ? T('VisitDetails.FieldCreated', 'Alan tanımı oluşturuldu.') : T('VisitDetails.FieldUpdated', 'Alan tanımı güncellendi.'));
            self.clearMessages();
        })
        .catch(function(err) {
            toastr.error(err.message);
        })
        .finally(function() {
            self.isSaving(false);
        });
    };

    self.deleteField = function(field) {
        showDeleteConfirm(field.name, function() {
            fetch('/api/visit-details/fields/' + field.id, { method: 'DELETE' })
                .then(function(r) {
                    if (!r.ok) {
                        return r.json().then(function(err) {
                            throw new Error(err.message || T('VisitDetails.DeleteError', 'Silme hatası.'));
                        });
                    }
                    self.loadFields();
                    toastr.success(T('VisitDetails.FieldDeleted', 'Alan tanımı silindi.'));
                    self.clearMessages();
                })
                .catch(function(err) {
                    toastr.error(err.message);
                });
        });
    };

    // ===== Initialize =====
    self.loadSectors();
    self.loadFields();
}

// Translation keys
var TRANSLATION_KEYS = [
    'VisitDetails.FieldType.Int',
    'VisitDetails.FieldType.Decimal',
    'VisitDetails.FieldType.Bool',
    'VisitDetails.FieldType.String',
    'VisitDetails.FieldType.DateTime',
    'VisitDetails.FieldType.Rating',
    'VisitDetails.Category.Time',
    'VisitDetails.Category.Staff',
    'VisitDetails.Category.Facility',
    'VisitDetails.Category.General',
    'VisitDetails.Category.Sector',
    'VisitDetails.LoadError',
    'VisitDetails.CodeNameRequired',
    'VisitDetails.SaveError',
    'VisitDetails.SectorCreated',
    'VisitDetails.SectorUpdated',
    'VisitDetails.DeleteError',
    'VisitDetails.SectorDeleted',
    'VisitDetails.FieldCreated',
    'VisitDetails.FieldUpdated',
    'VisitDetails.FieldDeleted'
];

// Apply bindings to specific element
$(document).ready(function() {
    Localization.loadKeys(TRANSLATION_KEYS).then(function() {
        var container = document.getElementById('visit-details-app');
        if (container) {
            ko.applyBindings(new VisitDetailsViewModel(), container);
        }
    });
});
