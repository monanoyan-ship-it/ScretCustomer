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
    self.isFilterModalOpen = ko.observable(false);
    self.isExportModalOpen = ko.observable(false);
    self.isPreviewModalOpen = ko.observable(false);
    self.errorMessage = ko.observable('');
    self.modalErrorMessage = ko.observable('');
    self.successMessage = ko.observable('');
    self.editingTemplate = ko.observable(null);
    self.importingTemplate = ko.observable(null);
    self.filteringTemplate = ko.observable(null);
    self.exportingTemplate = ko.observable(null);
    self.selectedFile = ko.observable(null);

    // Preview state
    self.previewData = ko.observable(null);
    self.previewLoading = ko.observable(false);

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
        template.groupByPropertyName = ko.observable(data ? data.groupByPropertyName : '');
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
        column.aggregateType = ko.observable(data ? String(data.aggregateTypeId || 0) : '0');
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

            // Check root properties first
            var property = entity.properties.find(p => p.propertyName === newPropertyName);
            if (!property) {
                // Check if it's a navigation path (dot notation)
                // Set friendly name from dot notation
                var friendlyName = newPropertyName
                    .split('.')
                    .map(p => p.replace(/([A-Z])/g, ' $1').trim())
                    .join(' > ')
                    .replace(/^./, str => str.toUpperCase());
                column.columnName(friendlyName);
                return;
            }

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

    // Build flat options list from schema (root properties + navigation paths)
    self.getPropertyOptionsForEntity = function(entityName) {
        if (!entityName) return [];
        var entity = self.entityTypes().find(e => e.entityName === entityName);
        if (!entity) return [];

        var options = [];

        // Root properties
        entity.properties.forEach(function(p) {
            options.push({
                value: p.propertyName,
                label: p.propertyName + ' (' + p.propertyType + ')',
                group: entityName
            });
        });

        // Navigation properties (2 levels deep)
        if (entity.navigations) {
            entity.navigations.forEach(function(nav) {
                var navPrefix = nav.name;
                var navLabel = nav.name + (nav.type === 'collection' ? ' []' : '');

                // Level 1 properties
                nav.properties.forEach(function(p) {
                    options.push({
                        value: navPrefix + '.' + p.propertyName,
                        label: navLabel + '.' + p.propertyName + ' (' + p.propertyType + ')',
                        group: nav.name
                    });
                });

                // Level 2 navigations
                if (nav.navigations) {
                    nav.navigations.forEach(function(subNav) {
                        var subPrefix = navPrefix + '.' + subNav.name;
                        var subLabel = navLabel + '.' + subNav.name + (subNav.type === 'collection' ? ' []' : '');

                        subNav.properties.forEach(function(p) {
                            options.push({
                                value: subPrefix + '.' + p.propertyName,
                                label: subLabel + '.' + p.propertyName + ' (' + p.propertyType + ')',
                                group: nav.name + ' > ' + subNav.name
                            });
                        });
                    });
                }
            });
        }

        return options;
    };

    // Helper function to get properties for entity (backward compat - used in property select)
    self.getPropertiesForEntity = function(entityName) {
        return self.getPropertyOptionsForEntity(entityName);
    };

    // Load entity attributes from backend
    self.loadEntityAttributes = function(entityType) {
        if (!entityType) return;

        // Check if already cached
        if (self.entityAttributesCache[entityType]) {
            return;
        }

        // Try to fetch attribute data for this entity
        fetch('/api/excel-templates/attributes/' + entityType)
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
        self.editingTemplate(new Template());
        self.isModalOpen(true);
    };

    // Edit Template
    self.editTemplate = function(template) {
        // Load full template with columns
        fetch('/api/excel-templates/' + template.id)
            .then(res => res.json())
            .then(data => {
                self.editingTemplate(new Template(data));
                self.isModalOpen(true);
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
            aggregateTypeId: parseInt(c.aggregateType()) || 0,
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
            groupByPropertyName: template.groupByPropertyName() || null,
            columns: columns
        };

        var url = template.id() ?
            '/api/excel-templates/' + template.id() :
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
            fetch('/api/excel-templates/' + template.id, {
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
        window.location.href = '/api/excel-templates/' + template.id + '/export';
    };

    // Export Data - show modal with filter values
    self.exportData = function(template) {
        // Load full template to get filters
        fetch('/api/excel-templates/' + template.id)
            .then(res => res.json())
            .then(data => {
                var operatorLabels = {
                    1: '=', 2: '≠', 3: '∋',
                    4: '>', 5: '<', 6: '≥', 7: '≤'
                };

                var exportObj = {
                    id: data.id,
                    name: data.name,
                    exportFilters: ko.observableArray(
                        (data.filters || []).map(function(f) {
                            return {
                                propertyName: f.propertyName,
                                operatorTypeId: f.operatorTypeId,
                                label: f.label,
                                operatorLabel: operatorLabels[f.operatorTypeId] || '=',
                                value: ko.observable('')
                            };
                        })
                    )
                };

                self.exportingTemplate(exportObj);
                self.isExportModalOpen(true);
            })
            .catch(err => {
                console.error('Error loading template:', err);
                toastr.error('Şablon yüklenirken hata oluştu');
            });
    };

    // Build filter payload from exportingTemplate
    function getExportFilters() {
        var tmpl = self.exportingTemplate();
        if (!tmpl) return [];

        return tmpl.exportFilters()
            .filter(function(f) { return f.value() && f.value().trim(); })
            .map(function(f) {
                return {
                    propertyName: f.propertyName,
                    operatorTypeId: f.operatorTypeId,
                    value: f.value()
                };
            });
    }

    // Do the actual export with filter values
    self.doExportData = function() {
        var tmpl = self.exportingTemplate();
        if (!tmpl) return;

        var filters = getExportFilters();

        // POST with fetch and download the blob
        fetch('/api/excel-templates/' + tmpl.id + '/export-data', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ filters: filters })
        })
        .then(res => {
            if (!res.ok) throw new Error('Export başarısız');
            var disposition = res.headers.get('Content-Disposition');
            var filename = 'export.xlsx';
            if (disposition) {
                var match = disposition.match(/filename[^;=\n]*=((['"]).*?\2|[^;\n]*)/);
                if (match && match[1]) filename = match[1].replace(/['"]/g, '');
            }
            return res.blob().then(blob => ({ blob, filename }));
        })
        .then(function(result) {
            var url = URL.createObjectURL(result.blob);
            var a = document.createElement('a');
            a.href = url;
            a.download = result.filename;
            document.body.appendChild(a);
            a.click();
            document.body.removeChild(a);
            URL.revokeObjectURL(url);
            self.closeExportModal();
            toastr.success('Veri başarıyla indirildi');
        })
        .catch(err => {
            console.error('Error exporting data:', err);
            toastr.error('Veri indirme hatası: ' + err.message);
        });
    };

    // Preview data
    self.doPreviewData = function() {
        var tmpl = self.exportingTemplate();
        if (!tmpl) return;

        var filters = getExportFilters();

        self.previewLoading(true);
        self.previewData(null);

        fetch('/api/excel-templates/' + tmpl.id + '/preview', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ filters: filters })
        })
        .then(res => {
            if (!res.ok) throw new Error('Önizleme başarısız');
            return res.json();
        })
        .then(function(data) {
            self.previewData(data);
            self.isPreviewModalOpen(true);
            self.previewLoading(false);
        })
        .catch(err => {
            console.error('Error previewing data:', err);
            toastr.error('Önizleme hatası: ' + err.message);
            self.previewLoading(false);
        });
    };

    // Close Preview Modal
    self.closePreviewModal = function() {
        self.isPreviewModalOpen(false);
        self.previewData(null);
    };

    // Close Export Modal
    self.closeExportModal = function() {
        self.isExportModalOpen(false);
        self.exportingTemplate(null);
        self.previewData(null);
    };

    // Show Filter Definition Modal
    self.showFilterModal = function(template) {
        // Load full template to get existing filters
        fetch('/api/excel-templates/' + template.id)
            .then(res => res.json())
            .then(data => {
                var filterObj = {
                    id: data.id,
                    name: data.name,
                    filters: ko.observableArray(
                        (data.filters || []).map(function(f) {
                            return {
                                id: ko.observable(f.id),
                                label: ko.observable(f.label),
                                propertyName: ko.observable(f.propertyName),
                                operatorTypeId: ko.observable(String(f.operatorTypeId)),
                                order: ko.observable(f.order)
                            };
                        })
                    ),
                    addFilter: function() {
                        var order = filterObj.filters().length + 1;
                        filterObj.filters.push({
                            id: ko.observable(null),
                            label: ko.observable(''),
                            propertyName: ko.observable(''),
                            operatorTypeId: ko.observable('1'),
                            order: ko.observable(order)
                        });
                    },
                    removeFilter: function(filter) {
                        filterObj.filters.remove(filter);
                        filterObj.filters().forEach(function(f, idx) {
                            f.order(idx + 1);
                        });
                    }
                };

                self.filteringTemplate(filterObj);
                self.isFilterModalOpen(true);
            })
            .catch(err => {
                console.error('Error loading template:', err);
                toastr.error('Şablon yüklenirken hata oluştu');
            });
    };

    // Save Filters
    self.saveFilters = function() {
        var tmpl = self.filteringTemplate();
        if (!tmpl) return;

        var filters = tmpl.filters().map(function(f) {
            return {
                id: f.id() || null,
                label: f.label(),
                propertyName: f.propertyName(),
                operatorTypeId: parseInt(f.operatorTypeId()) || 1,
                order: f.order()
            };
        });

        fetch('/api/excel-templates/' + tmpl.id + '/filters', {
            method: 'PUT',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ filters: filters })
        })
        .then(res => {
            if (!res.ok) throw new Error('Kaydetme başarısız');
            return res.json();
        })
        .then(function() {
            toastr.success('Filtreler kaydedildi');
            self.closeFilterModal();
            self.loadTemplates();
        })
        .catch(err => {
            console.error('Error saving filters:', err);
            toastr.error('Filtreler kaydedilirken hata oluştu: ' + err.message);
        });
    };

    // Close Filter Modal
    self.closeFilterModal = function() {
        self.isFilterModalOpen(false);
        self.filteringTemplate(null);
    };

    // Show Import Modal
    self.showImportModal = function(template) {
        self.importingTemplate(template);
        self.selectedFile(null);
        self.isImportModalOpen(true);
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

        fetch('/api/excel-templates/' + template.id + '/import', {
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
                    errorMessage += 'Satır ' + r.rowNumber + ': ' + r.errors.join(', ') + '; ';
                });
                if (result.invalidRows > 3) {
                    errorMessage += 've ' + (result.invalidRows - 3) + ' hata daha...';
                }
                toastr.warning(errorMessage, result.invalidRows + ' hatalı satır');
            }

            if (result.validRows > 0) {
                toastr.success('Toplam ' + result.totalRows + ' satırdan ' + result.validRows + ' tanesi başarıyla işlendi');
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
