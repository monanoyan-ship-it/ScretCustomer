// Checklist ViewModel - Model Pattern ile Gelismis Kontrol Listesi Yonetimi
// YENİ YAPI: Checklist -> Questions -> SubCriteria (Section kaldırıldı)

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
    base.allowNA = ko.observable(data.allowNA || false);

    // Puanlama alanları
    base.scoringType = ko.observable(data.scoringType || 'Scored');
    base.weightPoints = ko.observable(data.weightPoints || 10); // Varsayılan 10 (5 soru = 50, 10 soru = 100)
    base.maxPoints = ko.observable(data.maxPoints || 5); // Maksimum puan (1=Evet/Hayır, 5=Detaylı)
    base.penaltyType = ko.observable(data.penaltyType || 'None');
    base.recommendedNote = ko.observable(data.recommendedNote || '');
    base.helpText = ko.observable(data.helpText || '');
    base.groupName = ko.observable(data.groupName || '');

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
    // Firma ve Organizasyon
    base.customerId = ko.observable(data.customerId || null);
    base.customerOrganizationId = ko.observable(data.customerOrganizationId || null);

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

    // Firma ve Organizasyon listesi
    self.customers = ko.observableArray([]);
    self.organizations = ko.observableArray([]);
    self.selectedCustomerId = ko.observable(null);
    self._isLoadingChecklist = false; // Düzenleme sırasında org sıfırlamayı engelle

    // Arama/Filtreleme
    self.searchText = ko.observable('');
    self.filterCustomerId = ko.observable(null);
    self.filterOrganizationId = ko.observable(null);
    self.filterOrganizations = ko.observableArray([]);

    // Soru grupları (autocomplete için)
    self.questionGroups = ko.observableArray([]);
    self.filteredGroups = ko.observableArray([]);
    self.showGroupDropdown = ko.observable(false);
    self._groupFilterTimeout = null;

    // Filtre firma değiştiğinde organizasyonları yükle
    self.filterCustomerId.subscribe(function (customerId) {
        if (customerId) {
            apiService.get('/customer-organizations/by-customer/' + customerId)
                .then(function (data) {
                    self.filterOrganizations(data);
                })
                .catch(function () {
                    self.filterOrganizations([]);
                });
        } else {
            self.filterOrganizations([]);
            self.filterOrganizationId(null);
        }
    });

    // Firma değiştiğinde organizasyonları yükle
    self.selectedCustomerId.subscribe(function (customerId) {
        if (customerId) {
            self.loadOrganizations(customerId);
            // Checklist'teki customerId'yi güncelle
            if (self.editingChecklist()) {
                self.editingChecklist().customerId(customerId);
                // Sadece kullanıcı manuel değiştirdiyse organizasyonu sıfırla (düzenleme yüklemesinde değil)
                if (!self._isLoadingChecklist) {
                    self.editingChecklist().customerOrganizationId(null);
                }
            }
        } else {
            self.organizations([]);
            if (self.editingChecklist()) {
                self.editingChecklist().customerId(null);
                if (!self._isLoadingChecklist) {
                    self.editingChecklist().customerOrganizationId(null);
                }
            }
        }
    });

    // Firma listesini yükle
    self.loadCustomers = function () {
        apiService.get('/customers')
            .then(function (data) {
                self.customers(data);
            })
            .catch(function (error) {
                console.error('Load customers error:', error);
            });
    };

    // Organizasyon listesini yükle
    self.loadOrganizations = function (customerId) {
        if (!customerId) {
            self.organizations([]);
            return;
        }
        apiService.get('/customer-organizations/by-customer/' + customerId)
            .then(function (data) {
                self.organizations(data);
            })
            .catch(function (error) {
                console.error('Load organizations error:', error);
                self.organizations([]);
            });
    };

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

    // Autocomplete - Grup önerilerini göster
    self.showGroupSuggestions = function (question, event) {
        // Mevcut sorulardan tüm grupları topla (API'den gelenler + yeni eklenmiş olanlar)
        var allGroups = self.questionGroups().slice(); // API'den gelenler

        // Mevcut soruların gruplarını da ekle (yeni eklenmişler)
        if (self.editingChecklist()) {
            self.editingChecklist().questions().forEach(function (q) {
                var gn = q.groupName();
                if (gn && allGroups.indexOf(gn) === -1) {
                    allGroups.push(gn);
                }
            });
        }

        self.filteredGroups(allGroups);
        self.showGroupDropdown(true);
    };

    // Autocomplete - Grup önerilerini filtrele
    self.filterGroupSuggestions = function (question, event) {
        if (self._groupFilterTimeout) {
            clearTimeout(self._groupFilterTimeout);
        }
        self._groupFilterTimeout = setTimeout(function () {
            var searchVal = (event.target.value || '').toLowerCase();

            // Mevcut sorulardan tüm grupları topla
            var allGroups = self.questionGroups().slice();
            if (self.editingChecklist()) {
                self.editingChecklist().questions().forEach(function (q) {
                    var gn = q.groupName();
                    if (gn && allGroups.indexOf(gn) === -1) {
                        allGroups.push(gn);
                    }
                });
            }

            // Filtrele
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

        // Query parametrelerini oluştur
        var params = [];
        var search = self.searchText();
        var customerId = self.filterCustomerId();
        var orgId = self.filterOrganizationId();

        if (search) params.push('search=' + encodeURIComponent(search));
        if (customerId) params.push('customerId=' + customerId);
        if (orgId) params.push('customerOrganizationId=' + orgId);

        var url = '/checklists' + (params.length > 0 ? '?' + params.join('&') : '');

        apiService.get(url)
            .then(function (data) {
                self.checklists(data);
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
        self.filterCustomerId(null);
        self.filterOrganizationId(null);
        self.loadChecklists();
    };

    self.createNew = function () {
        self.modalErrorMessage('');
        self.wizardStep(1);
        self.selectedCustomerId(null);
        self.organizations([]);
        self.questionGroups([]); // Yeni checklist için grupları temizle
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
        self._isLoadingChecklist = true; // Subscriber'ın org sıfırlamasını engelle

        // Soru gruplarını yükle
        self.loadQuestionGroups(checklist.id);

        // DETAY API'SINI CAGIR - Listeden gelen veri eksik olabilir
        apiService.get('/checklists/' + checklist.id)
            .then(function (fullChecklist) {
                if (fullChecklist.customerId) {
                    // Organizasyonları yükle ve sonra checklist'i oluştur
                    apiService.get('/customer-organizations/by-customer/' + fullChecklist.customerId)
                        .then(function (orgs) {
                            self.organizations(orgs);
                            self.selectedCustomerId(fullChecklist.customerId);
                            self.editingChecklist(new ChecklistModel(fullChecklist, self.loadQuestionAttachments));
                            self.isModalOpen(true);
                            self.isLoading(false);
                            self._isLoadingChecklist = false;
                        })
                        .catch(function () {
                            self.organizations([]);
                            self.selectedCustomerId(fullChecklist.customerId);
                            self.editingChecklist(new ChecklistModel(fullChecklist, self.loadQuestionAttachments));
                            self.isModalOpen(true);
                            self.isLoading(false);
                            self._isLoadingChecklist = false;
                        });
                } else {
                    self.selectedCustomerId(null);
                    self.organizations([]);
                    self.editingChecklist(new ChecklistModel(fullChecklist, self.loadQuestionAttachments));
                    self.isModalOpen(true);
                    self.isLoading(false);
                    self._isLoadingChecklist = false;
                }
            })
            .catch(function (error) {
                console.error('Load checklist error:', error);
                toastr.error('Kontrol listesi yuklenirken bir hata olustu.');
                self.isLoading(false);
                self._isLoadingChecklist = false;
            });
    };

    self.cloneChecklist = function (checklist) {
        self.modalErrorMessage('');
        self.wizardStep(1);
        self.isLoading(true);
        self._isLoadingChecklist = true;

        // Klonlama için grupları yükle (orijinal checklist'ten)
        self.loadQuestionGroups(checklist.id);

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
                // Question ve SubCriteria ID'lerini temizle
                (cloneData.questions || []).forEach(function (q) {
                    q.id = null;
                    // SubCriteria ID'lerini de temizle
                    (q.subCriteria || []).forEach(function (sc) {
                        sc.id = null;
                    });
                });

                // Firma ve organizasyon seçimini ayarla (klonlama sırasında korunur)
                if (fullChecklist.customerId) {
                    apiService.get('/customer-organizations/by-customer/' + fullChecklist.customerId)
                        .then(function (orgs) {
                            self.organizations(orgs);
                            self.selectedCustomerId(fullChecklist.customerId);
                            self.editingChecklist(new ChecklistModel(cloneData));
                            self.isModalOpen(true);
                            self.isLoading(false);
                            self._isLoadingChecklist = false;
                        })
                        .catch(function () {
                            self.organizations([]);
                            self.selectedCustomerId(fullChecklist.customerId);
                            self.editingChecklist(new ChecklistModel(cloneData));
                            self.isModalOpen(true);
                            self.isLoading(false);
                            self._isLoadingChecklist = false;
                        });
                } else {
                    self.selectedCustomerId(null);
                    self.organizations([]);
                    self.editingChecklist(new ChecklistModel(cloneData));
                    self.isModalOpen(true);
                    self.isLoading(false);
                    self._isLoadingChecklist = false;
                }
            })
            .catch(function (error) {
                console.error('Load checklist error:', error);
                toastr.error('Kontrol listesi yuklenirken bir hata olustu.');
                self.isLoading(false);
                self._isLoadingChecklist = false;
            });
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
                    self.checklists.remove(checklist);
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

    // Seçili firma adını getir
    self.getSelectedCustomerName = function () {
        var customerId = self.selectedCustomerId();
        if (!customerId) return null;
        var customer = self.customers().find(function (c) { return c.id === customerId; });
        return customer ? customer.companyName : null;
    };

    // Seçili organizasyon adını getir
    self.getSelectedOrganizationName = function () {
        if (!self.editingChecklist()) return null;
        var orgId = self.editingChecklist().customerOrganizationId();
        if (!orgId) return null;
        var org = self.organizations().find(function (o) { return o.id === orgId; });
        return org ? org.name : null;
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

        // DEBUG: API'ye giden veriyi logla
        console.log('Checklist data to save:', JSON.stringify(data, null, 2));

        self.isSaving(true);
        self.modalErrorMessage('');

        var promise = data.id
            ? apiService.put('/checklists/' + data.id, data)
            : apiService.post('/checklists', data);

        promise
            .then(function (savedChecklist) {
                var isNew = !data.id;
                if (isNew) {
                    // Yeni kayıt: array'e ekle (son eklenen en üstte)
                    self.checklists.unshift(savedChecklist);
                } else {
                    // Güncelleme: array'de bul ve güncelle
                    var list = self.checklists();
                    for (var i = 0; i < list.length; i++) {
                        if (list[i].id === savedChecklist.id) {
                            self.checklists.splice(i, 1, savedChecklist);
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

    // Initialize
    self.init = function() {
        // Önce EnumsService'i yükle, sonra diğer verileri çek
        EnumsService.load().then(function() {
            self.loadChecklists();
            self.loadCustomers();
        });
    };

    self.init();
}

// Apply bindings when DOM is ready
document.addEventListener('DOMContentLoaded', function () {
    ko.applyBindings(new ChecklistViewModel(), document.getElementById('checklists-app'));
});
