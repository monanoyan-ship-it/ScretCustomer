// Checklist ViewModel
function ChecklistViewModel() {
    var self = this;

    self.checklists = ko.observableArray([]);
    self.isLoading = ko.observable(true);
    self.errorMessage = ko.observable('');
    self.successMessage = ko.observable('');
    self.isModalOpen = ko.observable(false);
    self.isViewModalOpen = ko.observable(false);
    self.editingChecklist = ko.observable(null);
    self.viewingChecklist = ko.observable(null);
    self.isSaving = ko.observable(false);

    self.loadChecklists = function() {
        self.isLoading(true);
        self.errorMessage('');

        apiService.get('/checklists')
            .then(function(data) {
                self.checklists(data);
            })
            .catch(function(error) {
                console.error('Checklists error:', error);
                self.errorMessage('Kontrol listeleri yüklenirken bir hata oluştu.');
            })
            .finally(function() {
                self.isLoading(false);
            });
    };

    self.createNew = function() {
        self.editingChecklist({
            id: null,
            name: ko.observable(''),
            description: ko.observable(''),
            isScored: ko.observable(false),
            isActive: ko.observable(true),
            version: ko.observable(1),
            sections: ko.observableArray([])
        });
        self.isModalOpen(true);
    };

    self.viewChecklist = function(checklist) {
        self.viewingChecklist(checklist);
        self.isViewModalOpen(true);
    };

    self.editChecklist = function(checklist) {
        // Convert API data to observables for editing
        var sections = (checklist.sections || []).map(function(section) {
            return {
                id: section.id,
                name: ko.observable(section.name),
                order: ko.observable(section.order),
                questions: ko.observableArray((section.questions || []).map(function(q) {
                    return {
                        id: q.id,
                        text: ko.observable(q.text),
                        type: ko.observable(q.type),
                        points: ko.observable(q.points),
                        order: ko.observable(q.order),
                        isRequired: ko.observable(q.isRequired),
                        allowNA: ko.observable(q.allowNA),
                        options: ko.observable(q.optionsJson || '')
                    };
                }))
            };
        });

        self.editingChecklist({
            id: checklist.id,
            name: ko.observable(checklist.name),
            description: ko.observable(checklist.description || ''),
            isScored: ko.observable(checklist.isScored),
            isActive: ko.observable(checklist.isActive),
            version: ko.observable(checklist.version || 1),
            sections: ko.observableArray(sections)
        });
        self.isModalOpen(true);
    };

    self.cloneChecklist = function(checklist) {
        // Clone the checklist by loading it as new (without ID)
        var sections = (checklist.sections || []).map(function(section) {
            return {
                id: null,
                name: ko.observable(section.name),
                order: ko.observable(section.order),
                questions: ko.observableArray((section.questions || []).map(function(q) {
                    return {
                        id: null,
                        text: ko.observable(q.text),
                        type: ko.observable(q.type),
                        points: ko.observable(q.points),
                        order: ko.observable(q.order),
                        isRequired: ko.observable(q.isRequired),
                        allowNA: ko.observable(q.allowNA),
                        options: ko.observable(q.optionsJson || '')
                    };
                }))
            };
        });

        self.editingChecklist({
            id: null,
            name: ko.observable(checklist.name + ' (Kopya)'),
            description: ko.observable(checklist.description || ''),
            isScored: ko.observable(checklist.isScored),
            isActive: ko.observable(true),
            version: ko.observable(1),
            sections: ko.observableArray(sections)
        });
        self.isModalOpen(true);
    };

    self.addSection = function() {
        if (!self.editingChecklist()) return;
        self.editingChecklist().sections.push({
            id: null,
            name: ko.observable(''),
            order: ko.observable(self.editingChecklist().sections().length + 1),
            questions: ko.observableArray([])
        });
    };

    self.removeSection = function(section) {
        if (!self.editingChecklist()) return;
        self.editingChecklist().sections.remove(section);
    };

    self.addQuestion = function(section) {
        section.questions.push({
            id: null,
            text: ko.observable(''),
            type: ko.observable('YesNo'),
            points: ko.observable(1),
            order: ko.observable(section.questions().length + 1),
            isRequired: ko.observable(true),
            allowNA: ko.observable(false),
            options: ko.observable('')
        });
    };

    self.removeQuestion = function(question, section) {
        section.questions.remove(question);
    };

    self.deleteChecklist = function(checklist) {
        deleteConfirmation.show(
            'Bu kontrol listesini silmek istediğinizden emin misiniz? Bu işlem geri alınamaz.',
            function() {
                apiService.delete('/checklists/' + checklist.id)
                    .then(function() {
                        self.checklists.remove(checklist);
                        self.successMessage('Kontrol listesi başarıyla silindi.');
                    })
                    .catch(function(error) {
                        console.error('Delete error:', error);
                        self.errorMessage('Kontrol listesi silinirken bir hata oluştu.');
                    });
            }
        );
    };

    self.saveChecklist = function() {
        if (!self.editingChecklist()) return;

        var checklist = self.editingChecklist();
        
        // Prepare data for API
        var data = {
            name: checklist.name(),
            description: checklist.description(),
            isScored: checklist.isScored(),
            isActive: checklist.isActive(),
            version: checklist.version(),
            sections: checklist.sections().map(function(section, sIndex) {
                return {
                    id: section.id,
                    name: section.name(),
                    order: section.order(),
                    description: '',
                    questions: section.questions().map(function(q, qIndex) {
                        return {
                            id: q.id,
                            text: q.text(),
                            type: q.type(),
                            points: q.points(),
                            order: q.order(),
                            isRequired: q.isRequired(),
                            allowNA: q.allowNA(),
                            optionsJson: q.options()
                        };
                    })
                };
            })
        };

        self.isSaving(true);
        self.errorMessage('');

        var promise = checklist.id 
            ? apiService.put('/checklists/' + checklist.id, data)
            : apiService.post('/checklists', data);

        promise
            .then(function(savedChecklist) {
                self.successMessage('Kontrol listesi başarıyla kaydedildi.');
                self.closeModal();
                self.loadChecklists();
            })
            .catch(function(error) {
                console.error('Save error:', error);
                self.errorMessage('Kontrol listesi kaydedilirken bir hata oluştu: ' + (error.message || ''));
            })
            .finally(function() {
                self.isSaving(false);
            });
    };

    self.closeModal = function() {
        self.isModalOpen(false);
        self.editingChecklist(null);
    };

    self.closeViewModal = function() {
        self.isViewModalOpen(false);
        self.viewingChecklist(null);
    };

    // Initialize
    self.loadChecklists();
}

// Apply bindings when DOM is ready
document.addEventListener('DOMContentLoaded', function() {
    ko.applyBindings(new ChecklistViewModel(), document.getElementById('checklists-app'));
});
