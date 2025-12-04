// ViewModel Constructors


function QuestionEditViewModel(data) {
    var self = this;
    data = data || {};

    self.id = data.id || null;
    self.text = ko.observable(data.text || '');
    self.type = ko.observable(data.type || 'YesNo');
    self.points = ko.observable(data.points || 10);
    self.allowNA = ko.observable(data.allowNA || false);
    self.options = ko.observable(data.options || '');
    self.order = ko.observable(data.order || 0);

    // Convert to DTO
    self.toDTO = function(orderIndex) {
        return {
            id: self.id,
            text: self.text(),
            type: self.type(),
            points: parseInt(self.points()) || 0,
            allowNA: self.allowNA(),
            options: self.options(),
            order: orderIndex !== undefined ? orderIndex + 1 : self.order()
        };
    };
}

function SectionEditViewModel(data) {
    var self = this;
    data = data || {};

    self.id = data.id || null;
    self.name = ko.observable(data.name || '');
    self.order = ko.observable(data.order || 0);
    self.questions = ko.observableArray((data.questions || []).map(function(q) {
        return new QuestionEditViewModel(q);
    }));

    // Convert to DTO
    self.toDTO = function(orderIndex) {
        return {
            id: self.id,
            name: self.name(),
            order: orderIndex !== undefined ? orderIndex + 1 : self.order(),
            questions: self.questions().map(function(q, qIndex) {
                return q.toDTO(qIndex);
            })
        };
    };
}

function ChecklistEditViewModel(data) {
    var self = this;
    data = data || {};

    self.id = data.id || null;
    self.name = ko.observable(data.name || '');
    self.description = ko.observable(data.description || '');
    self.version = ko.observable(data.version || 1);
    self.isActive = ko.observable(data.isActive !== undefined ? data.isActive : true);
    self.sections = ko.observableArray((data.sections || []).map(function(s) {
        return new SectionEditViewModel(s);
    }));

    // Convert to DTO
    self.toDTO = function() {
        return {
            name: self.name(),
            description: self.description(),
            version: self.version(),
            isActive: self.isActive(),
            sections: self.sections().map(function(s, sIndex) {
                return s.toDTO(sIndex);
            })
        };
    };
}

// Main ViewModel
function ChecklistsViewModel() {
    var self = this;

    // State
    self.isLoading = ko.observable(false);
    self.isSaving = ko.observable(false);
    self.errorMessage = ko.observable('');
    self.successMessage = ko.observable('');

    // Data
    self.checklists = ko.observableArray([]);
    self.editingChecklist = ko.observable(null);
    self.viewingChecklist = ko.observable(null);

    // Modal state
    self.isModalOpen = ko.observable(false);
    self.isViewModalOpen = ko.observable(false);

    // Load checklists
    self.loadChecklists = function() {
        self.isLoading(true);
        self.errorMessage('');

        fetch('/api/checklists', { credentials: 'include' })
            .then(response => {
                if (!response.ok) throw new Error('Yükleme başarısız');
                return response.json();
            })
            .then(data => {
                self.checklists(data);
            })
            .catch(error => {
                console.error('Error:', error);
                self.errorMessage('Kontrol listeleri yüklenirken bir hata oluştu.');
            })
            .finally(() => {
                self.isLoading(false);
            });
    };

    // Create new checklist
    self.createNew = function() {
        self.editingChecklist(new ChecklistEditViewModel());
        self.isModalOpen(true);
    };

    // Edit existing checklist
    self.editChecklist = function(checklist) {
        // Load full checklist data from API
        console.log('Fetching checklist:', checklist.id);

        fetch('/api/checklists/' + checklist.id, { credentials: 'include' })
            .then(response => {
                console.log('Response status:', response.status);
                if (!response.ok) {
                    // Read as text first, then try to parse as JSON
                    return response.text().then(text => {
                        console.error('Error response (raw):', text);

                        try {
                            const errorData = JSON.parse(text);
                            console.error('Error response (parsed):', errorData);

                            if (errorData.error) {
                                console.error('Exception:', errorData.error);
                                console.error('Stack trace:', errorData.details);
                            }

                            throw new Error(errorData.error || errorData.message || 'API Error: ' + response.status);
                        } catch (jsonError) {
                            // Not JSON, use text as-is
                            console.error('Could not parse JSON, using text:', text);
                            throw new Error('API Error: ' + response.status);
                        }
                    });
                }
                return response.json();
            })
            .then(data => {
                console.log('Checklist data:', data);

                // Create ViewModel from API data
                self.editingChecklist(new ChecklistEditViewModel(data));
                self.isModalOpen(true);
            })
            .catch(error => {
                console.error('Edit error:', error);
                self.errorMessage('Kontrol listesi yüklenirken bir hata oluştu: ' + error.message);
            });
    };

    // View checklist details
    self.viewChecklist = function(checklist) {
        console.log('Viewing checklist:', checklist.id);

        fetch('/api/checklists/' + checklist.id, { credentials: 'include' })
            .then(response => {
                console.log('View response status:', response.status);
                if (!response.ok) {
                    return response.text().then(text => {
                        console.error('View error response:', text);
                        throw new Error('API Error: ' + response.status);
                    });
                }
                return response.json();
            })
            .then(data => {
                console.log('View checklist data:', data);
                self.viewingChecklist(data);
                self.isViewModalOpen(true);
            })
            .catch(error => {
                console.error('View error:', error);
                self.errorMessage('Kontrol listesi yüklenirken bir hata oluştu: ' + error.message);
            });
    };

    // Add section
    self.addSection = function() {
        var checklist = self.editingChecklist();
        var newSection = new SectionEditViewModel({
            order: checklist.sections().length + 1
        });
        checklist.sections.push(newSection);
    };

    // Remove section
    self.removeSection = function(section) {
        if (!confirm('Bu bölümü silmek istediğinizden emin misiniz?')) return;
        var checklist = self.editingChecklist();
        checklist.sections.remove(section);
    };

    // Add question
    self.addQuestion = function(section) {
        var newQuestion = new QuestionEditViewModel({
            order: section.questions().length + 1
        });
        section.questions.push(newQuestion);
    };

    // Remove question
    self.removeQuestion = function(question) {
        var section = self.editingChecklist().sections().find(s => s.questions().includes(question));
        if (section) {
            section.questions.remove(question);
        }
    };

    // Save checklist
    self.saveChecklist = function() {
        var checklist = self.editingChecklist();

        // Validation
        if (!checklist.name() || checklist.name().trim() === '') {
            alert('Kontrol listesi adı zorunludur!');
            return;
        }

        if (checklist.sections().length === 0) {
            alert('En az bir bölüm eklemelisiniz!');
            return;
        }

        // Convert to DTO using toDTO() method
        var dto = checklist.toDTO();

        self.isSaving(true);
        var endpoint = checklist.id ? '/api/checklists/' + checklist.id : '/api/checklists';
        var method = checklist.id ? 'PUT' : 'POST';

        fetch(endpoint, {
            method: method,
            headers: { 'Content-Type': 'application/json' },
            credentials: 'include',
            body: JSON.stringify(dto)
        })
        .then(response => {
            if (!response.ok) throw new Error('Kayıt başarısız');
            return response.json();
        })
        .then(data => {
            self.successMessage('Kontrol listesi başarıyla kaydedildi.');
            self.closeModal();
            self.loadChecklists();
        })
        .catch(error => {
            console.error('Error:', error);
            self.errorMessage('Kontrol listesi kaydedilirken bir hata oluştu.');
        })
        .finally(() => {
            self.isSaving(false);
        });
    };

    // Clone checklist
    self.cloneChecklist = function(checklist) {
        if (!confirm('Bu kontrol listesini klonlamak istediğinizden emin misiniz?')) return;

        var newName = prompt('Yeni kontrol listesi adı:', checklist.name + ' (Kopya)');
        if (!newName) return;

        fetch('/api/checklists/' + checklist.id + '/clone', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            credentials: 'include',
            body: JSON.stringify(newName)
        })
        .then(response => {
            if (!response.ok) throw new Error('Klonlama başarısız');
            return response.json();
        })
        .then(() => {
            self.successMessage('Kontrol listesi başarıyla klonlandı.');
            self.loadChecklists();
        })
        .catch(error => {
            console.error('Error:', error);
            self.errorMessage('Kontrol listesi klonlanırken bir hata oluştu.');
        });
    };

    // Delete checklist
    self.deleteChecklist = function(checklist) {
        if (!confirm('Bu kontrol listesini silmek istediğinizden emin misiniz?')) return;

        fetch('/api/checklists/' + checklist.id, {
            method: 'DELETE',
            credentials: 'include'
        })
        .then(response => {
            if (!response.ok) throw new Error('Silme başarısız');
            self.successMessage('Kontrol listesi başarıyla silindi.');
            self.checklists.remove(checklist);
        })
        .catch(error => {
            console.error('Error:', error);
            self.errorMessage('Kontrol listesi silinirken bir hata oluştu.');
        });
    };

    // Close modals
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

// Apply bindings
$(document).ready(function() {
    ko.applyBindings(new ChecklistsViewModel(), document.getElementById('checklists-app'));
});
