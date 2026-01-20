// EmailTemplates Index ViewModel
function EmailTemplatesViewModel() {
    var self = this;

    // State
    self.isLoading = ko.observable(true);
    self.templates = ko.observableArray([]);
    self.templateTypes = ko.observableArray([]);
    self.filterTypeId = ko.observable('');
    self.filterStatus = ko.observable('');

    // Test email state
    self.testTemplateId = ko.observable(null);
    self.testTemplateName = ko.observable('');
    self.testEmail = ko.observable('');
    self.isSendingTest = ko.observable(false);
    self.testModal = null;
    self.useRealData = ko.observable(false);
    self.testProjects = ko.observableArray([]);
    self.testPersonnel = ko.observableArray([]);
    self.selectedProjectId = ko.observable('');
    self.selectedPersonnelId = ko.observable('');

    // Preview state
    self.previewSubject = ko.observable('');
    self.previewBody = ko.observable('');
    self.previewModal = null;

    // Filtered templates
    self.filteredTemplates = ko.computed(function() {
        var list = self.templates();
        var typeId = self.filterTypeId();
        var status = self.filterStatus();

        if (typeId) {
            list = list.filter(function(t) { return t.templateTypeId == typeId; });
        }
        if (status === 'active') {
            list = list.filter(function(t) { return t.isActive; });
        } else if (status === 'inactive') {
            list = list.filter(function(t) { return !t.isActive; });
        }

        return list;
    });

    // Get type icon
    self.getTypeIcon = function(typeId) {
        var type = self.templateTypes().find(function(t) { return t.id === typeId; });
        return type ? type.icon : 'bi bi-envelope';
    };

    // Create new template - open popup
    self.createTemplate = function() {
        var popup = window.open('/EmailTemplates/Editor', 'email_template_new', 'width=1300,height=800,scrollbars=yes,resizable=yes');
        if (popup) popup.focus();
    };

    // Edit template - open popup
    self.editTemplate = function(template) {
        var popup = window.open('/EmailTemplates/Editor/' + template.id, 'email_template_' + template.id, 'width=1300,height=800,scrollbars=yes,resizable=yes');
        if (popup) popup.focus();
    };

    // Load templates
    self.loadTemplates = function() {
        self.isLoading(true);
        fetch('/api/email-templates', { credentials: 'include' })
            .then(function(r) { return r.json(); })
            .then(function(data) {
                self.templates(data);
            })
            .finally(function() {
                self.isLoading(false);
            });
    };

    // Load types
    self.loadTypes = function() {
        fetch('/api/email-templates/types', { credentials: 'include' })
            .then(function(r) { return r.json(); })
            .then(function(data) {
                self.templateTypes(data);
            });
    };

    // Delete template
    self.deleteTemplate = function(template) {
        showConfirmModal({
            title: T('Common.Delete', 'Sil'),
            message: T('EmailTemplate.ConfirmDelete', 'Bu şablonu silmek istediğinize emin misiniz?'),
            confirmText: T('Common.Delete', 'Sil'),
            confirmClass: 'btn-danger',
            onConfirm: function() {
                fetch('/api/email-templates/' + template.id, {
                    method: 'DELETE',
                    credentials: 'include'
                })
                .then(function(r) { return r.json(); })
                .then(function(result) {
                    if (result.success) {
                        toastr.success(result.message);
                        self.loadTemplates();
                    } else {
                        toastr.error(result.message);
                    }
                });
            }
        });
    };

    // Duplicate template
    self.duplicateTemplate = function(template) {
        fetch('/api/email-templates/' + template.id + '/duplicate', {
            method: 'POST',
            credentials: 'include'
        })
        .then(function(r) { return r.json(); })
        .then(function(result) {
            if (result.success) {
                toastr.success(result.message);
                self.loadTemplates();
            } else {
                toastr.error(result.message);
            }
        });
    };

    // Clear filters
    self.clearFilters = function() {
        self.filterTypeId('');
        self.filterStatus('');
    };

    // Show preview modal
    self.showPreview = function(template) {
        fetch('/api/email-templates/preview', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ subject: template.subject, body: template.body }),
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

    // Show test email modal
    self.showTestModal = function(template) {
        self.testTemplateId(template.id);
        self.testTemplateName(template.name);
        self.testEmail('');
        self.useRealData(false);
        self.selectedProjectId('');
        self.selectedPersonnelId('');
        self.testPersonnel([]);

        // Load projects if not loaded yet
        if (self.testProjects().length === 0) {
            self.loadTestProjects();
        }

        if (!self.testModal) {
            self.testModal = new bootstrap.Modal(document.getElementById('testEmailModal'));
        }
        self.testModal.show();
    };

    // Load test projects
    self.loadTestProjects = function() {
        fetch('/api/email-templates/test-projects', { credentials: 'include' })
            .then(function(r) { return r.json(); })
            .then(function(data) {
                self.testProjects(data);
            });
    };

    // Load test personnel for selected project
    self.loadTestPersonnel = function() {
        var projectId = self.selectedProjectId();
        if (!projectId) {
            self.testPersonnel([]);
            self.selectedPersonnelId('');
            return;
        }

        fetch('/api/email-templates/test-personnel/' + projectId, { credentials: 'include' })
            .then(function(r) { return r.json(); })
            .then(function(data) {
                self.testPersonnel(data);
                self.selectedPersonnelId('');
            });
    };

    // Watch project selection
    self.selectedProjectId.subscribe(function() {
        self.loadTestPersonnel();
    });

    // Send test email
    self.sendTestEmail = function() {
        var email = self.testEmail();
        if (!email) {
            toastr.warning('Lütfen email adresi girin.');
            return;
        }

        var dto = { toEmail: email };

        // Add real data if selected
        if (self.useRealData()) {
            var projectId = self.selectedProjectId();
            var personnelId = self.selectedPersonnelId();

            if (!projectId || !personnelId) {
                toastr.warning('Gerçek veri kullanmak için proje ve personel seçin.');
                return;
            }

            dto.projectId = parseInt(projectId);
            dto.personnelId = parseInt(personnelId);
            dto.baseUrl = window.location.origin;
        }

        self.isSendingTest(true);

        fetch('/api/email-templates/' + self.testTemplateId() + '/send-test', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(dto),
            credentials: 'include'
        })
        .then(function(r) { return r.json(); })
        .then(function(result) {
            if (result.success) {
                toastr.success(result.message);
                self.testModal.hide();
            } else {
                toastr.error(result.message);
            }
        })
        .catch(function() {
            toastr.error('Email gönderilirken hata oluştu.');
        })
        .finally(function() {
            self.isSendingTest(false);
        });
    };

    // Init
    self.loadTypes();
    self.loadTemplates();
}

// Global function for popup to call after save
function refreshTemplates() {
    if (window.vm) {
        window.vm.loadTemplates();
    }
}

// Apply bindings
var vm = null;
$(document).ready(function() {
    vm = new EmailTemplatesViewModel();
    ko.applyBindings(vm, document.getElementById('email-templates-app'));
});
