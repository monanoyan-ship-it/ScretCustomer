// Checklist ViewModel - Model Pattern ile Gelismis Kontrol Listesi Yonetimi

// Question Model
var QuestionModel = function (data, loadAttachmentsFn) {
    let base = this;
    data = data || {};

    base.id = ko.observable(data.id || null);
    base.text = ko.observable(data.text || '');
    base.type = ko.observable(data.type || 'Likert');
    base.points = ko.observable(data.points || 1);
    base.order = ko.observable(data.order || 0);
    base.isRequired = ko.observable(data.isRequired !== false);
    base.allowNA = ko.observable(data.allowNA || false);
    base.options = ko.observable(data.options ? (typeof data.options === 'string' ? data.options : JSON.stringify(data.options)) : '');
    base.scoringType = ko.observable(data.scoringType || 'Scored');
    base.weightPoints = ko.observable(data.weightPoints || 1);
    base.maxPoints = ko.observable(data.maxPoints || 100);
    base.penaltyType = ko.observable(data.penaltyType || 'None');
    base.penaltyValue = ko.observable(data.penaltyValue || 0);
    base.recommendedNote = ko.observable(data.recommendedNote || '');
    base.helpText = ko.observable(data.helpText || '');

    // Mevcut dosya eklerini yükle (API'ye gönderilmeyecek)
    base._attachments = ko.observableArray([]);
    base._isUploadingFile = ko.observable(false);
    if (base.id() && loadAttachmentsFn) {
        loadAttachmentsFn(base);
    }
};

// Section Model
var SectionModel = function (data, loadAttachmentsFn) {
    let base = this;
    data = data || {};

    base.id = ko.observable(data.id || null);
    base.name = ko.observable(data.name || '');
    base.description = ko.observable(data.description || '');
    base.order = ko.observable(data.order || 0);
    base.groupType = ko.observable(data.groupType || 'Scored');
    base.weightPoints = ko.observable(data.weightPoints || 1);
    base.maxPoints = ko.observable(data.maxPoints || 100);
    base.isActive = ko.observable(data.isActive !== false);

    // Questions
    let questions = (data.questions || []).map(function (q) {
        return new QuestionModel(q, loadAttachmentsFn);
    });
    base.questions = ko.observableArray(questions);

    // Yeni soru ekle
    base.addQuestion = function () {
        base.questions.push(new QuestionModel({
            order: base.questions().length + 1
        }));
    };

    // Soru sil
    base.removeQuestion = function (question) {
        base.questions.remove(question);
    };
};

// Checklist Model
var ChecklistModel = function (data, loadAttachmentsFn) {
    let base = this;
    data = data || {};

    base.id = ko.observable(data.id || null);
    base.name = ko.observable(data.name || '');
    base.description = ko.observable(data.description || '');
    base.isScored = ko.observable(data.isScored !== false);
    base.isActive = ko.observable(data.isActive !== false);
    base.version = ko.observable(data.version || 1);
    base.code = ko.observable(data.code || '');
    base.templateName = ko.observable(data.templateName || '');
    base.checklistType = ko.observable(data.checklistType || 'CallPerformance');
    base.scoringMethod = ko.observable(data.scoringMethod || 'Maximum');
    base.maxTotalPoints = ko.observable(data.maxTotalPoints || 100);
    base.estimatedDurationMinutes = ko.observable(data.estimatedDurationMinutes || 30);
    base.validFrom = ko.observable(data.validFrom ? data.validFrom.split('T')[0] : '');
    base.validUntil = ko.observable(data.validUntil ? data.validUntil.split('T')[0] : '');

    // Sections
    let sections = (data.sections || []).map(function (s) {
        return new SectionModel(s, loadAttachmentsFn);
    });
    base.sections = ko.observableArray(sections);

    // Yeni section ekle
    base.addSection = function () {
        base.sections.push(new SectionModel({
            order: base.sections().length + 1
        }));
    };

    // Section sil
    base.removeSection = function (section) {
        base.sections.remove(section);
    };
};

// Main ViewModel
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

    // Dosya ekleri yükleme fonksiyonu
    self.loadQuestionAttachments = function (question) {
        if (!question.id()) return;

        fetch('/api/question-attachments/question/' + question.id(), {
            credentials: 'include'
        })
            .then(function (response) { return response.json(); })
            .then(function (attachments) {
                question._attachments(attachments);
            })
            .catch(function (error) {
                console.error('Load attachments error:', error);
            });
    };

    self.loadChecklists = function () {
        self.isLoading(true);
        self.errorMessage('');

        apiService.get('/checklists')
            .then(function (data) {
                self.checklists(data);
            })
            .catch(function (error) {
                console.error('Checklists error:', error);
                self.errorMessage('Kontrol listeleri yuklenirken bir hata olustu.');
            })
            .finally(function () {
                self.isLoading(false);
            });
    };

    self.createNew = function () {
        self.modalErrorMessage('');
        self.wizardStep(1);
        self.editingChecklist(new ChecklistModel());
        self.isModalOpen(true);
    };

    self.viewChecklist = function (checklist) {
        self.viewingChecklist(checklist);
        self.isViewModalOpen(true);
    };

    self.editChecklist = function (checklist) {
        self.modalErrorMessage('');
        self.wizardStep(1);
        self.isLoading(true);

        // DETAY API'SINI CAGIR - Listeden gelen veri eksik olabilir
        apiService.get('/checklists/' + checklist.id)
            .then(function (fullChecklist) {
                self.editingChecklist(new ChecklistModel(fullChecklist, self.loadQuestionAttachments));
                self.isModalOpen(true);
            })
            .catch(function (error) {
                console.error('Load checklist error:', error);
                self.errorMessage('Kontrol listesi yuklenirken bir hata olustu.');
            })
            .finally(function () {
                self.isLoading(false);
            });
    };

    self.cloneChecklist = function (checklist) {
        self.modalErrorMessage('');
        self.wizardStep(1);
        self.isLoading(true);

        // DETAY API'SINI CAGIR - Listeden gelen veri eksik olabilir
        apiService.get('/checklists/' + checklist.id)
            .then(function (fullChecklist) {
                // Clone: ID'leri sil, ismi degistir
                var cloneData = JSON.parse(JSON.stringify(fullChecklist));
                cloneData.id = null;
                cloneData.name = fullChecklist.name + ' (Kopya)';
                cloneData.code = '';
                cloneData.validFrom = '';
                cloneData.validUntil = '';
                cloneData.version = 1;
                // Section ve Question ID'lerini temizle
                (cloneData.sections || []).forEach(function (s) {
                    s.id = null;
                    (s.questions || []).forEach(function (q) {
                        q.id = null;
                    });
                });

                self.editingChecklist(new ChecklistModel(cloneData));
                self.isModalOpen(true);
            })
            .catch(function (error) {
                console.error('Load checklist error:', error);
                self.errorMessage('Kontrol listesi yuklenirken bir hata olustu.');
            })
            .finally(function () {
                self.isLoading(false);
            });
    };

    self.addSection = function () {
        if (!self.editingChecklist()) return;
        self.editingChecklist().addSection();
    };

    self.removeSection = function (section) {
        if (!self.editingChecklist()) return;
        self.editingChecklist().removeSection(section);
    };

    self.addQuestion = function (section) {
        section.addQuestion();
    };

    self.removeQuestion = function (question, section) {
        section.removeQuestion(question);
    };

    // Dosya yükleme fonksiyonları
    self.uploadQuestionAttachment = function (question, fileInput) {
        if (!fileInput.files || fileInput.files.length === 0) return;
        if (!question.id()) {
            toastr.warning('Dosya eklemek için önce soruyu kaydetmeniz gerekiyor.');
            return;
        }

        var file = fileInput.files[0];
        var formData = new FormData();
        formData.append('file', file);

        question._isUploadingFile(true);

        fetch('/api/question-attachments/question/' + question.id(), {
            method: 'POST',
            credentials: 'include',
            body: formData
        })
            .then(function (response) { return response.json(); })
            .then(function (result) {
                if (result.success && result.attachment) {
                    question._attachments.push(result.attachment);
                    self.successMessage('Dosya başarıyla yüklendi.');
                } else {
                    self.modalErrorMessage(result.message || 'Dosya yüklenemedi.');
                }
            })
            .catch(function (error) {
                console.error('Upload error:', error);
                self.modalErrorMessage('Dosya yüklenirken bir hata oluştu.');
            })
            .finally(function () {
                question._isUploadingFile(false);
                fileInput.value = '';
            });
    };

    self.removeQuestionAttachment = function (attachment, question) {
        showDeleteConfirm('Bu dosya', function () {
            fetch('/api/question-attachments/' + attachment.id, {
                method: 'DELETE',
                credentials: 'include'
            })
                .then(function (response) { return response.json(); })
                .then(function (result) {
                    question._attachments.remove(attachment);
                    self.successMessage('Dosya silindi.');
                })
                .catch(function (error) {
                    console.error('Delete error:', error);
                    self.modalErrorMessage('Dosya silinirken bir hata oluştu.');
                });
        });
    };

    self.deleteChecklist = function (checklist) {
        var checklistName = typeof checklist.name === 'function' ? checklist.name() : checklist.name;
        showDeleteConfirm(checklistName + ' kontrol listesi', function () {
            apiService.delete('/checklists/' + checklist.id)
                .then(function () {
                    self.checklists.remove(checklist);
                    self.successMessage('Kontrol listesi basariyla silindi.');
                })
                .catch(function (error) {
                    console.error('Delete error:', error);
                    self.errorMessage('Kontrol listesi silinirken bir hata olustu.');
                });
        });
    };

    // Wizard navigation
    self.nextStep = function () {
        if (self.wizardStep() < 4) {
            self.wizardStep(self.wizardStep() + 1);
        }
    };

    self.prevStep = function () {
        if (self.wizardStep() > 1) {
            self.wizardStep(self.wizardStep() - 1);
        }
    };

    // Helper functions for summary
    self.getTotalQuestions = function () {
        if (!self.editingChecklist()) return 0;
        var total = 0;
        self.editingChecklist().sections().forEach(function (s) {
            total += s.questions().length;
        });
        return total;
    };

    self.getYellowCardCount = function () {
        if (!self.editingChecklist()) return 0;
        var count = 0;
        self.editingChecklist().sections().forEach(function (s) {
            s.questions().forEach(function (q) {
                if (q.penaltyType() === 'YellowCard') count++;
            });
        });
        return count;
    };

    self.getRedCardCount = function () {
        if (!self.editingChecklist()) return 0;
        var count = 0;
        self.editingChecklist().sections().forEach(function (s) {
            s.questions().forEach(function (q) {
                if (q.penaltyType() === 'RedCard') count++;
            });
        });
        return count;
    };

    self.saveChecklist = function () {
        if (!self.editingChecklist()) return;

        var checklist = self.editingChecklist();
        var data = ko.toJS(checklist);

        // _ ile baslayan internal alanlari temizle ve options'i parse et
        data.sections.forEach(function (s) {
            s.questions.forEach(function (q) {
                delete q._attachments;
                delete q._isUploadingFile;
                // Options: boş veya string ise düzelt
                if (!q.options || q.options === '') {
                    q.options = null;
                } else if (typeof q.options === 'string') {
                    try {
                        q.options = JSON.parse(q.options);
                    } catch (e) {
                        q.options = null;
                    }
                }
            });
            delete s.addQuestion;
            delete s.removeQuestion;
        });
        delete data.addSection;
        delete data.removeSection;

        // Boş string date alanlarını null'a çevir (backend DateTime? bekliyor)
        // undefined, null, boş string, "null" string hepsini null yap
        if (!data.validFrom || data.validFrom === '' || data.validFrom === 'null') {
            data.validFrom = null;
        }
        if (!data.validUntil || data.validUntil === '' || data.validUntil === 'null') {
            data.validUntil = null;
        }

        // DEBUG: API'ye giden veriyi logla
        console.log('Checklist data to save:', JSON.stringify(data, null, 2));

        self.isSaving(true);
        self.modalErrorMessage('');

        var promise = data.id
            ? apiService.put('/checklists/' + data.id, data)
            : apiService.post('/checklists', data);

        promise
            .then(function (savedChecklist) {
                self.successMessage('Kontrol listesi basariyla kaydedildi.');
                self.closeModal();
                self.loadChecklists();
            })
            .catch(function (error) {
                console.error('Save error:', error);
                if (error.errors) {
                    console.error('Validation errors:', JSON.stringify(error.errors, null, 2));
                }
                self.modalErrorMessage('Kontrol listesi kaydedilirken bir hata olustu: ' + (error.message || JSON.stringify(error.errors || '')));
            })
            .finally(function () {
                self.isSaving(false);
            });
    };

    self.closeModal = function () {
        self.isModalOpen(false);
        self.editingChecklist(null);
        self.wizardStep(1);
    };

    self.closeViewModal = function () {
        self.isViewModalOpen(false);
        self.viewingChecklist(null);
    };

    // Initialize
    self.loadChecklists();
}

// Apply bindings when DOM is ready
document.addEventListener('DOMContentLoaded', function () {
    ko.applyBindings(new ChecklistViewModel(), document.getElementById('checklists-app'));
});
