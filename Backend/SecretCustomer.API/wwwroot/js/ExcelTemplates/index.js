// Excel Templates ViewModel
function ExcelTemplatesViewModel() {
    var self = this;

    // Observables
    self.templates = ko.observableArray([]);
    self.entityTypes = ko.observableArray([]);
    self.entityAttributesCache = {}; // Cache for attribute data
    self.isLoading = ko.observable(false);
    self.isModalOpen = ko.observable(false);
    self.isImportModalOpen = ko.observable(false);
    self.errorMessage = ko.observable('');
    self.modalErrorMessage = ko.observable('');
    self.successMessage = ko.observable('');
    self.editingTemplate = ko.observable(null);
    self.importingTemplate = ko.observable(null);
    self.selectedFile = ko.observable(null);

    // Template Model
    function Template(data) {
        var template = this;
        template.id = ko.observable(data ? data.id : null);
        template.name = ko.observable(data ? data.name : '');
        template.description = ko.observable(data ? data.description : '');
        template.entityType = ko.observable(data ? data.entityType : '');
        template.sheetName = ko.observable(data ? data.sheetName : 'Sheet1');
        template.hasHeader = ko.observable(data ? data.hasHeader : true);
        template.isActive = ko.observable(data ? data.isActive : true);
        template.columns = ko.observableArray(data && data.columns ? data.columns.map(c => new Column(c, template)) : []);

        // Load attributes when entity type changes
        template.entityType.subscribe(function(newEntityType) {
            if (newEntityType) {
                self.loadEntityAttributes(newEntityType);
            }
        });

        // Load attributes immediately if we have an entity type
        if (template.entityType()) {
            self.loadEntityAttributes(template.entityType());
        }

        template.addColumn = function() {
            if (!template.entityType()) {
                toastr.warning('Lütfen önce Varlık Tipi seçin');
                return;
            }
            var order = template.columns().length + 1;
            template.columns.push(new Column({ order: order }, template));
        };

        template.removeColumn = function(column) {
            template.columns.remove(column);
            // Update order
            template.columns().forEach(function(col, idx) {
                col.order(idx + 1);
            });
        };
    }

    // Column Model
    function Column(data, parentTemplate) {
        var column = this;
        column.id = ko.observable(data ? data.id : null);
        column.columnName = ko.observable(data ? data.columnName : '');
        column.propertyName = ko.observable(data ? data.propertyName : '');
        column.columnType = ko.observable(data ? data.columnType : 'Text');
        column.order = ko.observable(data ? data.order : 1);
        column.isRequired = ko.observable(data ? data.isRequired : false);
        column.sampleValue = ko.observable(data ? data.sampleValue : '');
        column.description = ko.observable(data ? data.description : '');
        column.dropdownOptionsStr = ko.observable(
            data && data.dropdownOptions ? data.dropdownOptions.join(', ') : ''
        );

        // Auto-detect column type and other fields from property type or attributes
        column.propertyName.subscribe(function(newPropertyName) {
            if (!newPropertyName || !parentTemplate) return;

            var entityType = parentTemplate.entityType();
            if (!entityType) return;

            // First, try to get attribute data
            var attributeData = self.entityAttributesCache[entityType];
            if (attributeData) {
                var attributeColumn = attributeData.find(a => a.propertyName === newPropertyName);
                if (attributeColumn) {
                    // Fill all fields from attribute - always update regardless of value
                    column.columnName(attributeColumn.columnName || '');
                    column.columnType(attributeColumn.columnType || 'Text');
                    column.isRequired(attributeColumn.isRequired || false);
                    column.description(attributeColumn.description || '');
                    column.sampleValue(attributeColumn.sampleValue || '');
                    column.dropdownOptionsStr(
                        attributeColumn.dropdownOptions ? attributeColumn.dropdownOptions.join(', ') : ''
                    );
                    return; // Exit early if we found attribute data
                }
            }

            // Fallback to old behavior if no attribute data
            var entity = self.entityTypes().find(e => e.entityName === entityType);
            if (!entity) return;

            var property = entity.properties.find(p => p.propertyName === newPropertyName);
            if (!property) return;

            // Map database type to Excel column type
            var typeMapping = {
                'string': 'Text',
                'int': 'Number',
                'long': 'Number',
                'decimal': 'Number',
                'double': 'Number',
                'bool': 'Boolean',
                'DateTime': 'Date',
                'Guid': 'Text'
            };

            var excelType = typeMapping[property.propertyType] || 'Text';

            // Special case for email detection
            if (property.propertyType === 'string' &&
                (newPropertyName.toLowerCase().includes('email') ||
                 newPropertyName.toLowerCase().includes('mail'))) {
                excelType = 'Email';
            }

            // Special case for phone detection
            if (property.propertyType === 'string' &&
                (newPropertyName.toLowerCase().includes('phone') ||
                 newPropertyName.toLowerCase().includes('tel') ||
                 newPropertyName.toLowerCase().includes('telefon'))) {
                excelType = 'Phone';
            }

            column.columnType(excelType);

            // Always set a friendly column name from property name
            var friendlyName = newPropertyName
                .replace(/([A-Z])/g, ' $1')
                .trim()
                .replace(/^./, str => str.toUpperCase());
            column.columnName(friendlyName);
        });
    }

    // Helper function to get properties for entity
    self.getPropertiesForEntity = function(entityName) {
        if (!entityName) return [];
        var entity = self.entityTypes().find(e => e.entityName === entityName);
        return entity ? entity.properties : [];
    };

    // Load entity attributes from backend
    self.loadEntityAttributes = function(entityType) {
        if (!entityType) return;

        // Check if already cached
        if (self.entityAttributesCache[entityType]) {
            return;
        }

        // Try to fetch attribute data for this entity
        fetch(`/api/excel-templates/attributes/${entityType}`)
            .then(res => {
                if (!res.ok) {
                    // No attributes defined, that's okay
                    return null;
                }
                return res.json();
            })
            .then(data => {
                if (data) {
                    self.entityAttributesCache[entityType] = data;
                }
            })
            .catch(err => {
                // Silently fail - attributes are optional
            });
    };

    // Load Templates
    self.loadTemplates = function() {
        self.isLoading(true);
        fetch('/api/excel-templates')
            .then(res => res.json())
            .then(data => {
                self.templates(data);
                self.isLoading(false);
            })
            .catch(err => {
                console.error('Error loading templates:', err);
                toastr.error('Şablonlar yüklenirken hata oluştu');
                self.isLoading(false);
            });
    };

    // Load Entity Schema
    self.loadEntitySchema = function() {
        fetch('/api/excel-templates/schema')
            .then(res => res.json())
            .then(data => {
                self.entityTypes(data);
            })
            .catch(err => {
                console.error('Error loading schema:', err);
                toastr.error('Şema yüklenirken hata oluştu');
            });
    };

    // Create New Template
    self.createNew = function() {
        self.editingTemplate(new Template());        self.isModalOpen(true);
    };

    // Edit Template
    self.editTemplate = function(template) {
        // Load full template with columns
        fetch(`/api/excel-templates/${template.id}`)
            .then(res => res.json())
            .then(data => {
                self.editingTemplate(new Template(data));                self.isModalOpen(true);
            })
            .catch(err => {
                console.error('Error loading template:', err);
                toastr.error('Şablon yüklenirken hata oluştu');
            });
    };

    // Save Template
    self.saveTemplate = function() {
        var template = self.editingTemplate();
        if (!template) return;

        var columns = template.columns().map(c => ({
            id: c.id(),
            columnName: c.columnName(),
            propertyName: c.propertyName(),
            columnType: c.columnType(),
            order: c.order(),
            isRequired: c.isRequired(),
            sampleValue: c.sampleValue() || null,
            description: c.description() || null,
            dropdownOptions: c.dropdownOptionsStr() ?
                c.dropdownOptionsStr().split(',').map(s => s.trim()) : null
        }));

        var payload = {
            name: template.name(),
            description: template.description(),
            entityType: template.entityType(),
            sheetName: template.sheetName(),
            hasHeader: template.hasHeader(),
            isActive: template.isActive(),
            columns: columns
        };

        var url = template.id() ?
            `/api/excel-templates/${template.id()}` :
            '/api/excel-templates';

        var method = template.id() ? 'PUT' : 'POST';

        fetch(url, {
            method: method,
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(payload)
        })
        .then(res => {
            if (!res.ok) throw new Error('Kaydetme işlemi başarısız');
            return res.json();
        })
        .then(data => {
            toastr.success(template.id() ? 'Şablon güncellendi' : 'Şablon oluşturuldu');
            self.closeModal();
            self.loadTemplates();
        })
        .catch(err => {
            console.error('Error saving template:', err);
            toastr.error('Şablon kaydedilirken hata oluştu: ' + err.message);
        });
    };

    // Delete Template
    self.deleteTemplate = function(template) {
        showDeleteConfirm(template.name + ' şablonu', function() {
            fetch(`/api/excel-templates/${template.id}`, {
                method: 'DELETE'
            })
            .then(res => {
                if (!res.ok) throw new Error('Silme işlemi başarısız');
                toastr.success('Şablon silindi');
                self.loadTemplates();
            })
            .catch(err => {
                console.error('Error deleting template:', err);
                toastr.error('Şablon silinirken bir hata oluştu.');
            });
        });
    };

    // Export Template (Download Sample Excel)
    self.exportTemplate = function(template) {
        window.location.href = `/api/excel-templates/${template.id}/export`;
    };

    // Show Import Modal
    self.showImportModal = function(template) {
        self.importingTemplate(template);
        self.selectedFile(null);        self.isImportModalOpen(true);
    };

    // Handle File Select
    self.handleFileSelect = function(data, event) {
        var file = event.target.files[0];
        self.selectedFile(file);
    };

    // Process Import
    self.processImport = function() {
        var file = self.selectedFile();
        var template = self.importingTemplate();

        if (!file || !template) return;

        var formData = new FormData();
        formData.append('file', file);

        fetch(`/api/excel-templates/${template.id}/import`, {
            method: 'POST',
            body: formData
        })
        .then(res => {
            if (!res.ok) throw new Error('İçe aktarma başarısız');
            return res.json();
        })
        .then(result => {
            self.closeImportModal();

            // Show results via toastr
            if (result.invalidRows > 0) {
                var errorMessage = 'Hatalı satırlar: ';
                result.rows.filter(r => !r.isValid).slice(0, 3).forEach(r => {
                    errorMessage += `Satır ${r.rowNumber}: ${r.errors.join(', ')}; `;
                });
                if (result.invalidRows > 3) {
                    errorMessage += `ve ${result.invalidRows - 3} hata daha...`;
                }
                toastr.warning(errorMessage, `${result.invalidRows} hatalı satır`);
            }

            if (result.validRows > 0) {
                toastr.success(`Toplam ${result.totalRows} satırdan ${result.validRows} tanesi başarıyla işlendi`);
            } else {
                toastr.error('Hiçbir satır işlenemedi');
            }
        })
        .catch(err => {
            console.error('Error importing:', err);
            toastr.error('Excel içe aktarılırken hata oluştu: ' + err.message);
        });
    };

    // Close Modal
    self.closeModal = function() {
        self.isModalOpen(false);
        self.editingTemplate(null);
    };

    // Close Import Modal
    self.closeImportModal = function() {
        self.isImportModalOpen(false);
        self.importingTemplate(null);
        self.selectedFile(null);
    };

    // Initialize
    self.loadEntitySchema();
    self.loadTemplates();
}

// Apply bindings
$(document).ready(function() {
    if (document.getElementById('excel-templates-app')) {
        ko.applyBindings(new ExcelTemplatesViewModel(), document.getElementById('excel-templates-app'));
    }
});
