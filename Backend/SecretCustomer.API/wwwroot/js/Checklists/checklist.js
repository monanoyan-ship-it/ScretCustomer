// Checklist ViewModel - 4 Adimli Wizard ile Gelismis Kontrol Listesi Yonetimi
function ChecklistViewModel() {
    var self = this;

    self.checklists = ko.observableArray([]);
    self.isLoading = ko.observable(true);
    self.errorMessage = ko.observable('');
    self.successMessage = ko.observable('');
    self.modalErrorMessage = ko.observable('');
    self.isModalOpen = ko.observable(false);
    self.isViewModalOpen = ko.observable(false);
    self.editingChecklist = ko.observable(null);
    self.viewingChecklist = ko.observable(null);
    self.isSaving = ko.observable(false);
    self.wizardStep = ko.observable(1);

    self.loadChecklists = function() {
        self.isLoading(true);
        self.errorMessage('');

        apiService.get('/checklists')
            .then(function(data) {
                self.checklists(data);
            })
            .catch(function(error) {
                console.error('Checklists error:', error);
                self.errorMessage('Kontrol listeleri yuklenirken bir hata olustu.');
            })
            .finally(function() {
                self.isLoading(false);
            });
    };

    self.createNew = function() {
        self.modalErrorMessage('');
        self.wizardStep(1);
        self.editingChecklist({
            id: null,
            name: ko.observable(''),
            description: ko.observable(''),
            isScored: ko.observable(true),
            isActive: ko.observable(true),
            version: ko.observable(1),
            // Yeni alanlar
            code: ko.observable(''),
            templateName: ko.observable(''),
            checklistType: ko.observable('CallPerformance'),
            scoringMethod: ko.observable('Maximum'),
            maxTotalPoints: ko.observable(100),
            estimatedDurationMinutes: ko.observable(30),
            validFrom: ko.observable(''),
            validUntil: ko.observable(''),
            sections: ko.observableArray([])
        });
        self.isModalOpen(true);
    };

    self.viewChecklist = function(checklist) {
        self.viewingChecklist(checklist);
        self.isViewModalOpen(true);
    };

    self.editChecklist = function(checklist) {
        self.modalErrorMessage('');
        self.wizardStep(1);
        // Convert API data to observables for editing
        var sections = (checklist.sections || []).map(function(section) {
            return {
                id: section.id,
                name: ko.observable(section.name),
                order: ko.observable(section.order),
                groupType: ko.observable(section.groupType || 'Scored'),
                weightPoints: ko.observable(section.weightPoints || 1),
                maxPoints: ko.observable(section.maxPoints || 100),
                isActive: ko.observable(section.isActive !== false),
                questions: ko.observableArray((section.questions || []).map(function(q) {
                    var question = {
                        id: q.id,
                        text: ko.observable(q.text),
                        type: ko.observable(q.type),
                        points: ko.observable(q.points),
                        order: ko.observable(q.order),
                        isRequired: ko.observable(q.isRequired),
                        allowNA: ko.observable(q.allowNA),
                        options: ko.observable(q.optionsJson || ''),
                        // Yeni alanlar
                        scoringType: ko.observable(q.scoringType || 'Scored'),
                        weightPoints: ko.observable(q.weightPoints || 1),
                        maxPoints: ko.observable(q.maxPoints || 100),
                        penaltyType: ko.observable(q.penaltyType || 'None'),
                        penaltyValue: ko.observable(q.penaltyValue || 0),
                        recommendedNote: ko.observable(q.recommendedNote || ''),
                        helpText: ko.observable(q.helpText || ''),
                        // Dosya ekleri
                        attachments: ko.observableArray([]),
                        isUploadingFile: ko.observable(false)
                    };
                    // Mevcut dosya eklerini yükle
                    if (q.id) {
                        self.loadQuestionAttachments(question);
                    }
                    return question;
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
            // Yeni alanlar
            code: ko.observable(checklist.code || ''),
            templateName: ko.observable(checklist.templateName || ''),
            checklistType: ko.observable(checklist.checklistType || 'CallPerformance'),
            scoringMethod: ko.observable(checklist.scoringMethod || 'Maximum'),
            maxTotalPoints: ko.observable(checklist.maxTotalPoints || 100),
            estimatedDurationMinutes: ko.observable(checklist.estimatedDurationMinutes || 30),
            validFrom: ko.observable(checklist.validFrom ? checklist.validFrom.split('T')[0] : ''),
            validUntil: ko.observable(checklist.validUntil ? checklist.validUntil.split('T')[0] : ''),
            sections: ko.observableArray(sections)
        });
        self.isModalOpen(true);
    };

    self.cloneChecklist = function(checklist) {
        self.modalErrorMessage('');
        self.wizardStep(1);
        // Clone the checklist by loading it as new (without ID)
        var sections = (checklist.sections || []).map(function(section) {
            return {
                id: null,
                name: ko.observable(section.name),
                order: ko.observable(section.order),
                groupType: ko.observable(section.groupType || 'Scored'),
                weightPoints: ko.observable(section.weightPoints || 1),
                maxPoints: ko.observable(section.maxPoints || 100),
                isActive: ko.observable(section.isActive !== false),
                questions: ko.observableArray((section.questions || []).map(function(q) {
                    return {
                        id: null,
                        text: ko.observable(q.text),
                        type: ko.observable(q.type),
                        points: ko.observable(q.points),
                        order: ko.observable(q.order),
                        isRequired: ko.observable(q.isRequired),
                        allowNA: ko.observable(q.allowNA),
                        options: ko.observable(q.optionsJson || ''),
                        scoringType: ko.observable(q.scoringType || 'Scored'),
                        weightPoints: ko.observable(q.weightPoints || 1),
                        maxPoints: ko.observable(q.maxPoints || 100),
                        penaltyType: ko.observable(q.penaltyType || 'None'),
                        penaltyValue: ko.observable(q.penaltyValue || 0),
                        recommendedNote: ko.observable(q.recommendedNote || ''),
                        helpText: ko.observable(q.helpText || ''),
                        // Dosya ekleri - klonlamada boş
                        attachments: ko.observableArray([]),
                        isUploadingFile: ko.observable(false)
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
            code: ko.observable(''),
            templateName: ko.observable(checklist.templateName || ''),
            checklistType: ko.observable(checklist.checklistType || 'CallPerformance'),
            scoringMethod: ko.observable(checklist.scoringMethod || 'Maximum'),
            maxTotalPoints: ko.observable(checklist.maxTotalPoints || 100),
            estimatedDurationMinutes: ko.observable(checklist.estimatedDurationMinutes || 30),
            validFrom: ko.observable(''),
            validUntil: ko.observable(''),
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
            groupType: ko.observable('Scored'),
            weightPoints: ko.observable(1),
            maxPoints: ko.observable(100),
            isActive: ko.observable(true),
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
            type: ko.observable('Likert'),
            points: ko.observable(1),
            order: ko.observable(section.questions().length + 1),
            isRequired: ko.observable(true),
            allowNA: ko.observable(false),
            options: ko.observable(''),
            // Yeni alanlar
            scoringType: ko.observable('Scored'),
            weightPoints: ko.observable(1),
            maxPoints: ko.observable(10),
            penaltyType: ko.observable('None'),
            penaltyValue: ko.observable(0),
            recommendedNote: ko.observable(''),
            helpText: ko.observable(''),
            // Dosya ekleri
            attachments: ko.observableArray([]),
            isUploadingFile: ko.observable(false)
        });
    };

    // Dosya yükleme fonksiyonları
    self.uploadQuestionAttachment = function(question, fileInput) {
        if (!fileInput.files || fileInput.files.length === 0) return;
        if (!question.id) {
            toastr.warning('Dosya eklemek için önce soruyu kaydetmeniz gerekiyor.');
            return;
        }

        var file = fileInput.files[0];
        var formData = new FormData();
        formData.append('file', file);

        question.isUploadingFile(true);

        fetch('/api/question-attachments/question/' + question.id, {
            method: 'POST',
            credentials: 'include',
            body: formData
        })
        .then(function(response) { return response.json(); })
        .then(function(result) {
            if (result.success && result.attachment) {
                question.attachments.push(result.attachment);
                self.successMessage('Dosya başarıyla yüklendi.');
            } else {
                self.modalErrorMessage(result.message || 'Dosya yüklenemedi.');
            }
        })
        .catch(function(error) {
            console.error('Upload error:', error);
            self.modalErrorMessage('Dosya yüklenirken bir hata oluştu.');
        })
        .finally(function() {
            question.isUploadingFile(false);
            fileInput.value = '';
        });
    };

    self.removeQuestionAttachment = function(attachment, question) {
        showDeleteConfirm('Bu dosya', function() {
            fetch('/api/question-attachments/' + attachment.id, {
                method: 'DELETE',
                credentials: 'include'
            })
            .then(function(response) { return response.json(); })
            .then(function(result) {
                question.attachments.remove(attachment);
                self.successMessage('Dosya silindi.');
            })
            .catch(function(error) {
                console.error('Delete error:', error);
                self.modalErrorMessage('Dosya silinirken bir hata oluştu.');
            });
        });
    };

    self.loadQuestionAttachments = function(question) {
        if (!question.id) return;

        fetch('/api/question-attachments/question/' + question.id, {
            credentials: 'include'
        })
        .then(function(response) { return response.json(); })
        .then(function(attachments) {
            question.attachments(attachments);
        })
        .catch(function(error) {
            console.error('Load attachments error:', error);
        });
    };

    self.removeQuestion = function(question, section) {
        section.questions.remove(question);
    };

    self.deleteChecklist = function(checklist) {
        showDeleteConfirm(checklist.name() + ' kontrol listesi', function() {
            apiService.delete('/checklists/' + checklist.id)
                .then(function() {
                    self.checklists.remove(checklist);
                    self.successMessage('Kontrol listesi basariyla silindi.');
                })
                .catch(function(error) {
                    console.error('Delete error:', error);
                    self.errorMessage('Kontrol listesi silinirken bir hata olustu.');
                });
        });
    };

    // Wizard navigation
    self.nextStep = function() {
        if (self.wizardStep() < 4) {
            self.wizardStep(self.wizardStep() + 1);
        }
    };

    self.prevStep = function() {
        if (self.wizardStep() > 1) {
            self.wizardStep(self.wizardStep() - 1);
        }
    };

    // Helper functions for summary
    self.getTotalQuestions = function() {
        if (!self.editingChecklist()) return 0;
        var total = 0;
        self.editingChecklist().sections().forEach(function(s) {
            total += s.questions().length;
        });
        return total;
    };

    self.getYellowCardCount = function() {
        if (!self.editingChecklist()) return 0;
        var count = 0;
        self.editingChecklist().sections().forEach(function(s) {
            s.questions().forEach(function(q) {
                if (q.penaltyType() === 'YellowCard') count++;
            });
        });
        return count;
    };

    self.getRedCardCount = function() {
        if (!self.editingChecklist()) return 0;
        var count = 0;
        self.editingChecklist().sections().forEach(function(s) {
            s.questions().forEach(function(q) {
                if (q.penaltyType() === 'RedCard') count++;
            });
        });
        return count;
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
            // Yeni alanlar
            code: checklist.code(),
            templateName: checklist.templateName(),
            checklistType: checklist.checklistType(),
            scoringMethod: checklist.scoringMethod(),
            maxTotalPoints: parseFloat(checklist.maxTotalPoints()) || 100,
            estimatedDurationMinutes: parseInt(checklist.estimatedDurationMinutes()) || null,
            validFrom: checklist.validFrom() || null,
            validUntil: checklist.validUntil() || null,
            sections: checklist.sections().map(function(section, sIndex) {
                return {
                    id: section.id,
                    name: section.name(),
                    order: section.order(),
                    description: '',
                    groupType: section.groupType(),
                    weightPoints: parseFloat(section.weightPoints()) || 1,
                    maxPoints: parseFloat(section.maxPoints()) || 100,
                    isActive: section.isActive(),
                    questions: section.questions().map(function(q, qIndex) {
                        return {
                            id: q.id,
                            text: q.text(),
                            type: q.type(),
                            points: parseInt(q.points()) || 0,
                            order: q.order(),
                            isRequired: q.isRequired(),
                            allowNA: q.allowNA(),
                            optionsJson: q.options(),
                            // Yeni alanlar
                            scoringType: q.scoringType(),
                            weightPoints: parseFloat(q.weightPoints()) || 1,
                            maxPoints: parseFloat(q.maxPoints()) || 100,
                            penaltyType: q.penaltyType(),
                            penaltyValue: parseFloat(q.penaltyValue()) || 0,
                            recommendedNote: q.recommendedNote(),
                            helpText: q.helpText()
                        };
                    })
                };
            })
        };

        self.isSaving(true);
        self.modalErrorMessage('');

        var promise = checklist.id
            ? apiService.put('/checklists/' + checklist.id, data)
            : apiService.post('/checklists', data);

        promise
            .then(function(savedChecklist) {
                self.successMessage('Kontrol listesi basariyla kaydedildi.');
                self.closeModal();
                self.loadChecklists();
            })
            .catch(function(error) {
                console.error('Save error:', error);
                self.modalErrorMessage('Kontrol listesi kaydedilirken bir hata olustu: ' + (error.message || ''));
            })
            .finally(function() {
                self.isSaving(false);
            });
    };

    self.closeModal = function() {
        self.isModalOpen(false);
        self.editingChecklist(null);
        self.wizardStep(1);
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
