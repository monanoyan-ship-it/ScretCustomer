function SettingsViewModel() {
    var self = this;

    // State
    self.isLoading = ko.observable(true);
    self.isSaving = ko.observable(false);
    self.settings = ko.observableArray([]);
    self.categories = ko.observableArray([]);
    self.selectedCategory = ko.observable(null);
    self.isEditing = ko.observable(false);

    // Form
    self.form = {
        key: ko.observable(''),
        value: ko.observable(''),
        valueTypeId: ko.observable('0'),
        category: ko.observable('General'),
        description: ko.observable('')
    };

    // Value type names
    self.getValueTypeName = function(type) {
        var types = ['String', 'Boolean', 'Integer', 'Decimal', 'JSON', 'DateTime'];
        return types[type] || 'String';
    };

    // Load categories
    self.loadCategories = function() {
        fetch('/api/AppSettingsApi/categories')
            .then(function(r) { return r.json(); })
            .then(function(data) {
                self.categories(data || []);
            });
    };

    // Load settings
    self.loadSettings = function() {
        self.isLoading(true);
        var url = self.selectedCategory()
            ? '/api/AppSettingsApi/category/' + encodeURIComponent(self.selectedCategory())
            : '/api/AppSettingsApi';

        fetch(url)
            .then(function(r) { return r.json(); })
            .then(function(data) {
                self.settings(data || []);
                self.isLoading(false);
            })
            .catch(function(err) {
                console.error('Settings load error:', err);
                self.isLoading(false);
            });
    };

    // Open create modal
    self.openCreateModal = function() {
        self.isEditing(false);
        self.form.key('');
        self.form.value('');
        self.form.valueTypeId('0');
        self.form.category('General');
        self.form.description('');
        bootstrap.Modal.getOrCreateInstance(document.getElementById('settingModal')).show();
    };

    // Open edit modal
    self.openEditModal = function(setting) {
        self.isEditing(true);
        self.form.key(setting.key);
        self.form.value(setting.value);
        self.form.valueTypeId(String(setting.valueTypeId));
        self.form.category(setting.category);
        self.form.description(setting.description || '');
        bootstrap.Modal.getOrCreateInstance(document.getElementById('settingModal')).show();
    };

    // Save setting
    self.saveSetting = function() {
        if (!self.form.key()) {
            toastr.warning('Anahtar gereklidir.');
            return;
        }

        self.isSaving(true);
        var isEdit = self.isEditing();
        var url = isEdit
            ? '/api/AppSettingsApi/' + encodeURIComponent(self.form.key())
            : '/api/AppSettingsApi';

        var data = {
            key: self.form.key(),
            value: self.form.value(),
            valueTypeId: parseInt(self.form.valueTypeId()),
            category: self.form.category() || 'General',
            description: self.form.description()
        };

        fetch(url, {
            method: isEdit ? 'PUT' : 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(data)
        })
        .then(function(r) {
            if (!r.ok) {
                return r.json().then(function(err) { throw new Error(err.message); });
            }
            return r.json();
        })
        .then(function() {
            bootstrap.Modal.getInstance(document.getElementById('settingModal')).hide();
            toastr.success(isEdit ? 'Ayar güncellendi.' : 'Ayar oluşturuldu.');
            self.loadSettings();
            self.loadCategories();
        })
        .catch(function(err) {
            toastr.error(err.message || 'Bir hata oluştu.');
        })
        .finally(function() {
            self.isSaving(false);
        });
    };

    // Delete setting
    self.deleteSetting = function(setting) {
        showDeleteConfirm(setting.key + ' ayarı', function() {
            fetch('/api/AppSettingsApi/' + encodeURIComponent(setting.key), {
                method: 'DELETE'
            })
            .then(function(r) {
                if (!r.ok) {
                    return r.json().then(function(err) { throw new Error(err.message); });
                }
                return r.json();
            })
            .then(function() {
                toastr.success('Ayar silindi.');
                self.loadSettings();
                self.loadCategories();
            })
            .catch(function(err) {
                toastr.error(err.message || 'Bir hata oluştu.');
            });
        });
    };

    // Initialize
    self.loadCategories();
    self.loadSettings();
}

$(document).ready(function() {
    ko.applyBindings(new SettingsViewModel(), document.getElementById('settings-app'));
});
