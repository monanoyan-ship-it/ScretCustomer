// Checklist ViewModel - Model Pattern ile Gelismis Kontrol Listesi Yonetimi
// YAPI: Checklist -> Questions -> SubCriteria (Sorular GroupName ile gruplandırılır)

// SubCriteria Model - Alt Kriter/Öneri
var SubCriteriaModel = function (data) {
    let base = this;
    data = data || {};

    base.id = ko.observable(data.id !== undefined && data.id !== null ? data.id : 0);
    base.description = ko.observable(data.description || '');
    base.order = ko.observable(data.order || 0);
    base.weightPoints = ko.observable(data.weightPoints !== undefined ? data.weightPoints : 1);
    base.isActive = ko.observable(data.isActive !== false);
};

// Question Model - Direkt Checklist'e bağlı
var QuestionModel = function (data, loadAttachmentsFn) {
    let base = this;
    data = data || {};

    base.id = ko.observable(data.id !== undefined && data.id !== null ? data.id : 0);
    base.text = ko.observable(data.text || '');
    base.order = ko.observable(data.order || 0);
    base.isRequired = ko.observable(data.isRequired !== false);

    // Puanlama alanları
    base.scoringType = ko.observable(data.scoringType || 'Scored');
    // Puansız sorular için weightPoints = 0, diğerleri için varsayılan 10
    var initialWeightPoints = data.weightPoints !== undefined ? data.weightPoints :
        (data.scoringType === 'Unscored' ? 0 : 10);
    base.weightPoints = ko.observable(initialWeightPoints);
    base.maxPoints = ko.observable(data.maxPoints || 5); // Maksimum puan (1=Evet/Hayır, 5=Detaylı)
    base.penaltyType = ko.observable(data.penaltyType || 'None');
    base.recommendedNote = ko.observable(data.recommendedNote || '');
    base.helpText = ko.observable(data.helpText || '');
    base.groupName = ko.observable(data.groupName || '');

    // SubCriteria ayarları (Online Anket için)
    base.selectionTypeId = ko.observable(data.selectionTypeId || 2); // Varsayılan: Multiple (2)
    base.showScoreInput = ko.observable(data.showScoreInput !== false); // Varsayılan: true
    base.allowComment = ko.observable(data.allowComment !== false); // Varsayılan: true

    // scoringType değiştiğinde weightPoints'i otomatik ayarla
    base.scoringType.subscribe(function(newValue) {
        if (newValue === 'Unscored') {
            base.weightPoints(0);
        } else if (base.weightPoints() === 0) {
            // Puansızdan puanlıya geçerken varsayılan değer ata
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

    // Mevcut dosya eklerini yükle (API'ye gönderilmeyecek)
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

    base.id = ko.observable(data.id !== undefined && data.id !== null ? data.id : 0);
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
    // Questions - Direkt checklist'e bağlı
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

// Main ViewModel
function ChecklistViewModel() {
    var self = this;

    self._checklists = ko.observableArray([]);
    self.isLoading = ko.observable(true);
    self.isEditLoading = ko.observable(false); // Modal için ayrı loading
    self.errorMessage = ko.observable('');
    self.successMessage = ko.observable('');
    self.modalErrorMessage = ko.observable('');
    self.isModalOpen = ko.observable(false);
    self.isViewModalOpen = ko.observable(false);
    self.isScoringMethodModalOpen = ko.observable(false); // Puanlama yöntemi seçim modalı
    self.editingChecklist = ko.observable(null);
    self.viewingChecklist = ko.observable(null);
    self.isSaving = ko.observable(false);
    self.wizardStep = ko.observable(1);

    // Sorting - varsayılan: son eklenen en üstte
    self.sorting = TableSorting.createSortState('createdAt', 'desc');

    // Sorted checklists
    self.checklists = ko.computed(function() {
        var items = self._checklists();
        var sortBy = self.sorting.sortBy();
        var sortDir = self.sorting.sortDirection();
        if (!sortBy) return items;
        return TableSorting.clientSort(items, sortBy, sortDir);
    });

    // ========== CHIP-BASED FILTER SYSTEM ==========
    self.selectedFilterType = ko.observable('');
    self.activeFilters = ko.observableArray([]);

    // Temp filter values
    self.tempFilter = {
        searchText: ko.observable(''),
        isActive: ko.observable(null)
    };

    // Filter labels
    self.filterLabels = {
        searchText: 'Arama',
        isActive: 'Durum'
    };

    self.statusLabels = {
        'true': 'Aktif',
        'false': 'Pasif'
    };

    // Legacy filter references
    self.searchText = ko.observable('');

    // Can add filter
    self.canAddFilter = ko.computed(function() {
        var type = self.selectedFilterType();
        if (!type) return false;

        switch (type) {
            case 'searchText': return self.tempFilter.searchText().trim() !== '';
            case 'isActive': return self.tempFilter.isActive() !== null;
            default: return false;
        }
    });

    // Add filter
    self.addFilter = function() {
        var type = self.selectedFilterType();
        if (!type) return;

        var filter = {
            type: type,
            label: self.filterLabels[type],
            value: null,
            displayValue: ''
        };

        switch (type) {
            case 'searchText':
                var searchText = self.tempFilter.searchText().trim();
                if (!searchText) return;
                filter.value = searchText;
                filter.displayValue = searchText;
                self.tempFilter.searchText('');
                break;

            case 'isActive':
                var isActive = self.tempFilter.isActive();
                if (isActive === null) return;
                filter.value = isActive;
                filter.displayValue = self.statusLabels[String(isActive)];
                self.tempFilter.isActive(null);
                break;

            default:
                return;
        }

        self.activeFilters.push(filter);
        self.selectedFilterType('');
        self.loadChecklists();
    };

    // Remove filter
    self.removeFilter = function(filter) {
        self.activeFilters.remove(filter);
        self.loadChecklists();
    };

    // Clear all filters
    self.clearFilters = function() {
        self.activeFilters([]);
        self.searchText('');
        self.loadChecklists();
    };

    // Build filter params
    self.buildFilterParams = function() {
        var params = [];

        self.activeFilters().forEach(function(filter) {
            switch (filter.type) {
                case 'searchText':
                    params.push('searchText=' + encodeURIComponent(filter.value));
                    break;
                case 'isActive':
                    params.push('isActive=' + filter.value);
                    break;
            }
        });

        // Doğrudan input'tan gelen arama (chip eklenmeden)
        var directSearch = (self.searchText() || '').trim();
        if (directSearch && !self.activeFilters().some(function(f) { return f.type === 'searchText'; })) {
            params.push('searchText=' + encodeURIComponent(directSearch));
        }

        return params;
    };

    // Soru grupları (autocomplete için)
    self.questionGroups = ko.observableArray([]);
    self.filteredGroups = ko.observableArray([]);
    self.showGroupDropdown = ko.observable(false);
    self._groupFilterTimeout = null;

    // Soru gruplarını yükle (belirli checklist için)
    self.loadQuestionGroups = function (checklistId) {
        if (!checklistId) {
            self.questionGroups([]);
            return;
        }
        fetch('/api/checklists/' + checklistId + '/question-groups', { credentials: 'include' })
            .then(function (response) { return response.json(); })
            .then(function (data) {
                self.questionGroups(data || []);
            })
            .catch(function (error) {
                console.error('Load question groups error:', error);
                self.questionGroups([]);
            });
    };

    // Mevcut checklist'in sorularından grup isimlerini al
    self.getGroupsFromQuestions = function () {
        var groups = [];
        if (self.editingChecklist()) {
            self.editingChecklist().questions().forEach(function (q) {
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
        // Biraz gecikme ile gizle (click'in çalışması için)
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

    self.loadChecklists = function () {
        self.isLoading(true);
        self.errorMessage('');

        // Build chip-based filter params
        var params = self.buildFilterParams();

        var url = '/checklists' + (params.length > 0 ? '?' + params.join('&') : '');

        apiService.get(url)
            .then(function (data) {
                self._checklists(data);
            })
            .catch(function (error) {
                console.error('Checklists error:', error);
                toastr.error('Kontrol listeleri yuklenirken bir hata olustu.');
            })
            .finally(function () {
                self.isLoading(false);
            });
    };

    // Arama fonksiyonu
    self.search = function () {
        self.loadChecklists();
    };

    // Filtreleri temizle
    self.clearFilters = function () {
        self.searchText('');
        self.loadChecklists();
    };

    self.createNew = function () {
        // Önce puanlama yöntemi seçim modalını aç
        self.isScoringMethodModalOpen(true);
    };

    // Puanlama yöntemi seçildiğinde
    self.selectScoringMethod = function (scoringMethod) {
        self.isScoringMethodModalOpen(false);

        // Seçilen yönteme göre ilgili editör popup'ını aç
        var width = 1200;
        var height = 800;
        var left = (screen.width - width) / 2;
        var top = (screen.height - height) / 2;

        // Her mod için ayrı URL
        var editorUrl = '/Checklists/' + scoringMethod;
        window.open(editorUrl, 'ChecklistEditor',
            'width=' + width + ',height=' + height + ',left=' + left + ',top=' + top + ',resizable=yes,scrollbars=yes');
    };

    // Puanlama yöntemi seçim modalını kapat
    self.closeScoringMethodModal = function () {
        self.isScoringMethodModalOpen(false);
    };

    self.viewChecklist = function (checklist) {
        // Liste verisi sadece questionCount içeriyor, tam veriyi çekmemiz gerekiyor
        self.isEditLoading(true);
        self.isViewModalOpen(true);

        apiService.get('/checklists/' + checklist.id)
            .then(function (fullChecklist) {
                self.viewingChecklist(fullChecklist);
            })
            .catch(function (error) {
                console.error('Load checklist error:', error);
                toastr.error('Kontrol listesi yüklenirken bir hata oluştu.');
                self.isViewModalOpen(false);
            })
            .finally(function () {
                self.isEditLoading(false);
            });
    };

    self.editChecklist = function (checklist) {
        // Popup olarak aç
        var width = 1200;
        var height = 800;
        var left = (screen.width - width) / 2;
        var top = (screen.height - height) / 2;

        // scoringMethod'a göre ilgili editör URL'ini belirle
        var scoringMethod = checklist.scoringMethod || 'Maximum';
        var editorUrl = '/Checklists/' + scoringMethod + '?id=' + checklist.id;

        window.open(editorUrl, 'ChecklistEditor',
            'width=' + width + ',height=' + height + ',left=' + left + ',top=' + top + ',resizable=yes,scrollbars=yes');
    };

    self.cloneChecklist = function (checklist) {
        // Popup olarak aç - clone parametresi ile
        var width = 1200;
        var height = 800;
        var left = (screen.width - width) / 2;
        var top = (screen.height - height) / 2;

        // scoringMethod'a göre ilgili editör URL'ini belirle
        var scoringMethod = checklist.scoringMethod || 'Maximum';
        var editorUrl = '/Checklists/' + scoringMethod + '?clone=' + checklist.id;

        window.open(editorUrl, 'ChecklistEditor',
            'width=' + width + ',height=' + height + ',left=' + left + ',top=' + top + ',resizable=yes,scrollbars=yes');
    };

    self.addQuestion = function () {
        if (!self.editingChecklist()) return;
        self.editingChecklist().addQuestion();

        // Yeni eklenen soruya scroll yap
        setTimeout(function() {
            var questionCards = document.querySelectorAll('.question-card');
            if (questionCards.length > 0) {
                var lastCard = questionCards[questionCards.length - 1];
                lastCard.scrollIntoView({ behavior: 'smooth', block: 'center' });
            }
        }, 100);
    };

    self.removeQuestion = function (question) {
        if (!self.editingChecklist()) return;
        self.editingChecklist().removeQuestion(question);
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
                    toastr.success('Dosya başarıyla yüklendi.');
                } else {
                    toastr.error(result.message || 'Dosya yüklenemedi.');
                }
            })
            .catch(function (error) {
                console.error('Upload error:', error);
                toastr.error('Dosya yüklenirken bir hata oluştu.');
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
                    toastr.success('Dosya silindi.');
                })
                .catch(function (error) {
                    console.error('Delete error:', error);
                    toastr.error('Dosya silinirken bir hata oluştu.');
                });
        });
    };

    self.deleteChecklist = function (checklist) {
        var checklistName = typeof checklist.name === 'function' ? checklist.name() : checklist.name;
        showDeleteConfirm(checklistName + ' kontrol listesi', function () {
            apiService.delete('/checklists/' + checklist.id)
                .then(function () {
                    self._checklists.remove(checklist);
                    toastr.success('Kontrol listesi basariyla silindi.');
                })
                .catch(function (error) {
                    console.error('Delete error:', error);
                    toastr.error('Kontrol listesi silinirken bir hata olustu.');
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
        return self.editingChecklist().questions().length;
    };

    self.getYellowCardCount = function () {
        if (!self.editingChecklist()) return 0;
        var count = 0;
        self.editingChecklist().questions().forEach(function (q) {
            if (q.penaltyType() === 'YellowCard') count++;
        });
        return count;
    };

    self.getRedCardCount = function () {
        if (!self.editingChecklist()) return 0;
        var count = 0;
        self.editingChecklist().questions().forEach(function (q) {
            if (q.penaltyType() === 'RedCard') count++;
        });
        return count;
    };

    // Toplam ağırlık puanı hesapla
    self.getTotalWeightPoints = function () {
        if (!self.editingChecklist()) return 0;
        var total = 0;
        self.editingChecklist().questions().forEach(function (q) {
            total += parseFloat(q.weightPoints()) || 0;
        });
        return total;
    };

    self.saveChecklist = function () {
        if (!self.editingChecklist()) return;

        var checklist = self.editingChecklist();
        var data = ko.toJS(checklist);

        // _ ile baslayan internal alanlari temizle
        data.questions.forEach(function (q) {
            delete q._attachments;
            delete q._isUploadingFile;
            delete q._showSubCriteria;
            delete q.addSubCriteria;
            delete q.removeSubCriteria;
            // SubCriteria: boş array ise null yap
            if (q.subCriteria && q.subCriteria.length === 0) {
                q.subCriteria = null;
            }
        });
        delete data.addQuestion;
        delete data.removeQuestion;

        // Boş string date alanlarını null'a çevir (backend DateTime? bekliyor)
        if (!data.validFrom || data.validFrom === '' || data.validFrom === 'null') {
            data.validFrom = null;
        }
        if (!data.validUntil || data.validUntil === '' || data.validUntil === 'null') {
            data.validUntil = null;
        }

        self.isSaving(true);
        var promise = data.id
            ? apiService.put('/checklists/' + data.id, data)
            : apiService.post('/checklists', data);

        promise
            .then(function (savedChecklist) {
                var isNew = !data.id;
                if (isNew) {
                    // Yeni kayıt: array'e ekle (son eklenen en üstte)
                    self._checklists.unshift(savedChecklist);
                } else {
                    // Güncelleme: array'de bul ve güncelle
                    var list = self._checklists();
                    for (var i = 0; i < list.length; i++) {
                        if (list[i].id === savedChecklist.id) {
                            self._checklists.splice(i, 1, savedChecklist);
                            break;
                        }
                    }
                }
                toastr.success('Kontrol listesi basariyla kaydedildi.');
                self.closeModal();
            })
            .catch(function (error) {
                console.error('Save error:', error);
                if (error.errors) {
                    console.error('Validation errors:', JSON.stringify(error.errors, null, 2));
                }
                toastr.error('Kontrol listesi kaydedilirken bir hata olustu: ' + (error.message || JSON.stringify(error.errors || '')));
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

    // Excel Export
    self.exportChecklistToExcel = function (checklist) {
        var checklistId = checklist.id;
        var checklistName = checklist.name;

        // Dosyayı indir
        window.location.href = '/api/checklists/' + checklistId + '/export/excel';
        toastr.success(checklistName + ' kontrol listesi Excel\'e aktarılıyor...');
    };

    // Initialize
    self.init = function() {
        // Önce EnumsService'i yükle, sonra diğer verileri çek
        EnumsService.load().then(function() {
            self.loadChecklists();
        });

        // Popup'tan çağrılacak global fonksiyon - liste yenilemesi olmadan güncelleme
        window.updateOrAddChecklist = function(checklist, isNew) {
            if (isNew) {
                // Yeni kayıt: array'in başına ekle
                self._checklists.unshift(checklist);
                toastr.info('Yeni kontrol listesi eklendi.');
            } else {
                // Güncelleme: array'de bul ve güncelle
                var list = self._checklists();
                for (var i = 0; i < list.length; i++) {
                    if (list[i].id === checklist.id) {
                        self._checklists.splice(i, 1, checklist);
                        toastr.info('Kontrol listesi güncellendi.');
                        break;
                    }
                }
            }
        };
    };

    self.init();
}

// Apply bindings when DOM is ready
document.addEventListener('DOMContentLoaded', function () {
    ko.applyBindings(new ChecklistViewModel(), document.getElementById('checklists-app'));
});
