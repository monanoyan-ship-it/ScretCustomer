// EmailTemplate Editor ViewModel - Popup
function EmailTemplateEditorViewModel() {
    var self = this;
    var config = window.editorConfig || {};

    // State
    self.isLoading = ko.observable(!config.isNew);
    self.isSaving = ko.observable(false);
    self.templateTypes = ko.observableArray([]);
    self.placeholderCategories = ko.observableArray([]);
    self.previewSubject = ko.observable('');
    self.previewBody = ko.observable('');
    self.previewModal = null;

    // Template data
    self.template = {
        name: ko.observable(''),
        description: ko.observable(''),
        subject: ko.observable(''),
        body: ko.observable(''),
        templateTypeId: ko.observable(1),
        isActive: ko.observable(true),
        isDefault: ko.observable(false),
        customerId: ko.observable(null)
    };

    // Initialize Summernote
    self.initEditor = function() {
        $('#bodyEditor').summernote({
            lang: 'tr-TR',
            height: 300,
            placeholder: 'Email içeriğini buraya yazın...',
            toolbar: [
                ['style', ['style']],
                ['font', ['bold', 'italic', 'underline', 'strikethrough', 'clear']],
                ['fontsize', ['fontsize']],
                ['color', ['color']],
                ['para', ['ul', 'ol', 'paragraph']],
                ['table', ['table']],
                ['insert', ['link', 'picture', 'hr']],
                ['view', ['codeview', 'help']]
            ],
            callbacks: {
                onChange: function(contents) {
                    self.template.body(contents);
                }
            }
        });

        // Set initial content if editing
        var body = self.template.body();
        if (body) {
            $('#bodyEditor').summernote('code', body);
        }
    };

    // Insert placeholder at cursor
    self.insertPlaceholder = function(placeholder) {
        $('#bodyEditor').summernote('insertText', placeholder.code);
        toastr.info(placeholder.label + ' eklendi');
    };

    // Load template for editing
    self.loadTemplate = function() {
        if (config.isNew) {
            self.initEditor();
            return;
        }

        fetch('/api/email-templates/' + config.templateId, { credentials: 'include' })
            .then(function(r) { return r.json(); })
            .then(function(data) {
                self.template.name(data.name);
                self.template.description(data.description || '');
                self.template.subject(data.subject);
                self.template.body(data.body || '');
                self.template.templateTypeId(data.templateTypeId);
                self.template.isActive(data.isActive);
                self.template.isDefault(data.isDefault);
                self.template.customerId(data.customerId);
            })
            .finally(function() {
                self.isLoading(false);
                // Initialize editor after data is loaded
                setTimeout(function() {
                    self.initEditor();
                }, 100);
            });
    };

    // Load template types
    self.loadTypes = function() {
        fetch('/api/email-templates/types', { credentials: 'include' })
            .then(function(r) { return r.json(); })
            .then(function(data) {
                self.templateTypes(data);
            });
    };

    // Load placeholders
    self.loadPlaceholders = function() {
        fetch('/api/email-templates/placeholders', { credentials: 'include' })
            .then(function(r) { return r.json(); })
            .then(function(data) {
                self.placeholderCategories(data);
            });
    };

    // Show preview
    self.showPreview = function() {
        fetch('/api/email-templates/preview', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({
                subject: self.template.subject(),
                body: self.template.body()
            }),
            credentials: 'include'
        })
        .then(function(r) { return r.json(); })
        .then(function(data) {
            self.previewSubject(data.subject);
            self.previewBody(data.body);
            if (!self.previewModal) {
                self.previewModal = new bootstrap.Modal(document.getElementById('previewModal'));
            }
            self.previewModal.show();
        });
    };

    // Save template
    self.saveTemplate = function() {
        var name = self.template.name();
        var subject = self.template.subject();
        var body = self.template.body();

        if (!name) {
            toastr.error('Şablon adı gerekli.');
            return;
        }
        if (!subject) {
            toastr.error('Email konusu gerekli.');
            return;
        }
        if (!body) {
            toastr.error('Email içeriği gerekli.');
            return;
        }

        self.isSaving(true);

        var data = {
            name: name,
            description: self.template.description(),
            subject: subject,
            body: body,
            templateTypeId: parseInt(self.template.templateTypeId()),
            isActive: self.template.isActive(),
            isDefault: self.template.isDefault(),
            customerId: self.template.customerId()
        };

        var isNew = config.isNew;
        var url = isNew ? '/api/email-templates' : '/api/email-templates/' + config.templateId;
        var method = isNew ? 'POST' : 'PUT';

        fetch(url, {
            method: method,
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(data),
            credentials: 'include'
        })
        .then(function(r) { return r.json(); })
        .then(function(result) {
            if (result.success) {
                toastr.success(result.message);
                // Notify opener window to refresh
                if (window.opener && window.opener.refreshTemplates) {
                    window.opener.refreshTemplates();
                }
                // Close after short delay
                setTimeout(function() {
                    window.close();
                }, 1000);
            } else {
                toastr.error(result.message);
            }
        })
        .catch(function(err) {
            toastr.error('Bir hata oluştu.');
        })
        .finally(function() {
            self.isSaving(false);
        });
    };

    // Initialize
    self.loadTypes();
    self.loadPlaceholders();
    self.loadTemplate();
}

// Apply bindings
$(document).ready(function() {
    ko.applyBindings(new EmailTemplateEditorViewModel(), document.getElementById('email-template-editor'));
});
