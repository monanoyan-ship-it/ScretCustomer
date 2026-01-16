// Checklist Editor ViewModel - Popup
// Model tanımları (checklist.js'den kopyalandı)

// SubCriteria Model - Alt Kriter/Öneri
var SubCriteriaModel = function (data) {
    let base = this;
    data = data || {};

    base.id = ko.observable(data.id || null);
    base.description = ko.observable(data.description || '');
    base.order = ko.observable(data.order || 0);
    base.weightPoints = ko.observable(data.weightPoints || 1);
    base.isActive = ko.observable(data.isActive !== false);
};

// Question Model - Direkt Checklist'e bağlı
var QuestionModel = function (data, loadAttachmentsFn) {
    let base = this;
    data = data || {};

    base.id = ko.observable(data.id || null);
    base.text = ko.observable(data.text || '');
    base.order = ko.observable(data.order || 0);
    base.isRequired = ko.observable(data.isRequired !== false);

    // Puanlama alanları
    base.scoringType = ko.observable(data.scoringType || 'Scored');
    var initialWeightPoints = data.weightPoints !== undefined ? data.weightPoints :
        (data.scoringType === 'Unscored' ? 0 : 10);
    base.weightPoints = ko.observable(initialWeightPoints);
    base.maxPoints = ko.observable(data.maxPoints || 5);
    base.penaltyType = ko.observable(data.penaltyType || 'None');
    base.recommendedNote = ko.observable(data.recommendedNote || '');
    base.helpText = ko.observable(data.helpText || '');
    base.groupName = ko.observable(data.groupName || '');

    // SubCriteria ayarları (Online Anket için)
    base.selectionTypeId = ko.observable(data.selectionTypeId || 2);
    base.showScoreInput = ko.observable(data.showScoreInput !== false);

    // scoringType değiştiğinde weightPoints'i otomatik ayarla
    base.scoringType.subscribe(function(newValue) {
        if (newValue === 'Unscored') {
            base.weightPoints(0);
        } else if (base.weightPoints() === 0) {
            base.weightPoints(10);
        }
    });

    // Alt Kriterler/Öneriler
    let subCriteria = (data.subCriteria || []).map(function (sc) {
        return new SubCriteriaModel(sc);
    });
    base.subCriteria = ko.observableArray(subCriteria);

    // Alt kriter toggle
    base._showSubCriteria = ko.observable(subCriteria.length > 0);

    // Yeni alt kriter ekle
    base.addSubCriteria = function () {
        base.subCriteria.push(new SubCriteriaModel({
            order: base.subCriteria().length + 1
        }));
        base._showSubCriteria(true);
    };

    // Alt kriter sil
    base.removeSubCriteria = function (subCriteria) {
        base.subCriteria.remove(subCriteria);
    };

    // Mevcut dosya eklerini yükle
    base._attachments = ko.observableArray([]);
    base._isUploadingFile = ko.observable(false);
    if (base.id() && loadAttachmentsFn) {
        loadAttachmentsFn(base);
    }
};

// Checklist Model - Sorular direkt checklist'e bağlı
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
    base.validFrom = ko.observable(data.validFrom ? data.validFrom.split('T')[0] : '');
    base.validUntil = ko.observable(data.validUntil ? data.validUntil.split('T')[0] : '');
    base.customerId = ko.observable(data.customerId || null);
    base.customerOrganizationId = ko.observable(data.customerOrganizationId || null);

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

// Main Editor ViewModel
function ChecklistEditorViewModel() {
    var self = this;
    var config = window.editorConfig || {};

    // State
    self.isLoading = ko.observable(!config.isNew);
    self.isSaving = ko.observable(false);
    self.wizardStep = ko.observable(1);
    self.checklist = ko.observable(null);

    // Soru grupları (autocomplete için)
    self.questionGroups = ko.observableArray([]);
    self.filteredGroups = ko.observableArray([]);
    self.showGroupDropdown = ko.observable(false);
    self._groupFilterTimeout = null;

    // Mevcut checklist'in sorularından grup isimlerini al
    self.getGroupsFromQuestions = function () {
        var groups = [];
        if (self.checklist()) {
            self.checklist().questions().forEach(function (q) {
                var gn = q.groupName();
                if (gn && groups.indexOf(gn) === -1) {
                    groups.push(gn);
                }
            });
        }
        return groups.sort();
    };

    // Autocomplete - Grup önerilerini göster
    self.showGroupSuggestions = function (question, event) {
        var allGroups = self.getGroupsFromQuestions();
        self.filteredGroups(allGroups);
        self.showGroupDropdown(allGroups.length > 0);
    };

    // Autocomplete - Grup önerilerini filtrele
    self.filterGroupSuggestions = function (question, event) {
        if (self._groupFilterTimeout) {
            clearTimeout(self._groupFilterTimeout);
        }
        self._groupFilterTimeout = setTimeout(function () {
            var searchVal = (event.target.value || '').toLowerCase();
            var allGroups = self.getGroupsFromQuestions();

            if (searchVal) {
                var filtered = allGroups.filter(function (g) {
                    return g.toLowerCase().indexOf(searchVal) >= 0;
                });
                self.filteredGroups(filtered);
            } else {
                self.filteredGroups(allGroups);
            }

            self.showGroupDropdown(self.filteredGroups().length > 0);
        }, 100);
    };

    // Autocomplete - Dropdown'u gizle
    self.hideGroupSuggestions = function (question, event) {
        setTimeout(function () {
            self.showGroupDropdown(false);
        }, 200);
    };

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

    // Dosya yükleme
    self.uploadQuestionAttachment = function (question, fileInput) {
        if (!fileInput.files || fileInput.files.length === 0) return;
        if (!question.id()) {
            toastr.warning('Dosya eklemek icin once soruyu kaydetmeniz gerekiyor.');
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
                    toastr.success('Dosya basariyla yuklendi.');
                } else {
                    toastr.error(result.message || 'Dosya yuklenemedi.');
                }
            })
            .catch(function (error) {
                console.error('Upload error:', error);
                toastr.error('Dosya yuklenirken bir hata olustu.');
            })
            .finally(function () {
                question._isUploadingFile(false);
                fileInput.value = '';
            });
    };

    // Dosya silme
    self.removeQuestionAttachment = function (attachment, question) {
        if (!confirm('Bu dosyayi silmek istediginize emin misiniz?')) return;

        fetch('/api/question-attachments/' + attachment.id, {
            method: 'DELETE',
            credentials: 'include'
        })
            .then(function (response) { return response.json(); })
            .then(function (result) {
                question._attachments.remove(attachment);
                toastr.success('Dosya silindi.');
            })
            .catch(function (error) {
                console.error('Delete error:', error);
                toastr.error('Dosya silinirken bir hata olustu.');
            });
    };

    // Load checklist
    self.loadChecklist = function () {
        if (config.isNew) {
            self.checklist(new ChecklistModel());
            return;
        }

        // Clone veya Edit modunda yükle
        var loadId = config.isClone ? config.cloneId : config.checklistId;

        fetch('/api/checklists/' + loadId, { credentials: 'include' })
            .then(function (r) { return r.json(); })
            .then(function (data) {
                if (config.isClone) {
                    // Clone: ID'leri temizle, ismi değiştir
                    data.id = null;
                    data.name = data.name + ' (Kopya)';
                    data.code = '';
                    data.validFrom = null;
                    data.validUntil = null;
                    data.version = 1;
                    // Question ve SubCriteria ID'lerini temizle
                    (data.questions || []).forEach(function (q) {
                        q.id = null;
                        (q.subCriteria || []).forEach(function (sc) {
                            sc.id = null;
                        });
                    });
                    self.checklist(new ChecklistModel(data)); // Clone'da attachment yükleme yapma
                } else {
                    self.checklist(new ChecklistModel(data, self.loadQuestionAttachments));
                }
            })
            .catch(function (error) {
                console.error('Load checklist error:', error);
                toastr.error('Kontrol listesi yuklenirken bir hata olustu.');
            })
            .finally(function () {
                self.isLoading(false);
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

    // Save checklist
    self.saveChecklist = function () {
        if (!self.checklist()) return;

        var checklist = self.checklist();
        var name = checklist.name();

        if (!name) {
            toastr.error('Kontrol listesi adi gerekli.');
            self.wizardStep(1);
            return;
        }

        var data = ko.toJS(checklist);

        // _ ile baslayan internal alanlari temizle
        data.questions.forEach(function (q) {
            delete q._attachments;
            delete q._isUploadingFile;
            delete q._showSubCriteria;
            delete q.addSubCriteria;
            delete q.removeSubCriteria;
            if (q.subCriteria && q.subCriteria.length === 0) {
                q.subCriteria = null;
            }
        });
        delete data.addQuestion;
        delete data.removeQuestion;

        // Boş string date alanlarını null'a çevir
        if (!data.validFrom || data.validFrom === '' || data.validFrom === 'null') {
            data.validFrom = null;
        }
        if (!data.validUntil || data.validUntil === '' || data.validUntil === 'null') {
            data.validUntil = null;
        }

        self.isSaving(true);

        var isNew = config.isNew;
        var url = isNew ? '/api/checklists' : '/api/checklists/' + config.checklistId;
        var method = isNew ? 'POST' : 'PUT';

        fetch(url, {
            method: method,
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(data),
            credentials: 'include'
        })
        .then(function (r) {
            if (!r.ok) {
                return r.json().then(function(err) { throw err; });
            }
            return r.json();
        })
        .then(function (result) {
            toastr.success('Kontrol listesi basariyla kaydedildi.');

            // Notify opener window to update the specific item (no full refresh)
            if (window.opener && window.opener.updateOrAddChecklist) {
                window.opener.updateOrAddChecklist(result, isNew || config.isClone);
            }

            // Parent'a data gitti, hemen kapat
            window.close();
        })
        .catch(function (error) {
            console.error('Save error:', error);
            var errorMsg = 'Kontrol listesi kaydedilirken bir hata olustu.';
            if (error.message) {
                errorMsg += ' ' + error.message;
            } else if (error.errors) {
                errorMsg += ' ' + JSON.stringify(error.errors);
            }
            toastr.error(errorMsg);
        })
        .finally(function () {
            self.isSaving(false);
        });
    };

    // Initialize
    self.loadChecklist();
}

// Apply bindings
$(document).ready(function () {
    ko.applyBindings(new ChecklistEditorViewModel(), document.getElementById('checklist-editor'));
});
