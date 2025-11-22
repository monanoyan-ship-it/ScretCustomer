// Checklists ViewModel
function ChecklistsViewModel() {
    const self = this;

    self.checklists = ko.observableArray([]);
    self.isLoading = ko.observable(false);
    self.isSaving = ko.observable(false);
    self.isEditMode = ko.observable(false);

    // Modal instances
    let checklistModal = null;
    let viewModal = null;

    // Current checklist being edited/created
    self.currentChecklist = ko.observable({
        id: null,
        name: ko.observable(''),
        description: ko.observable(''),
        version: ko.observable(1),
        sections: ko.observableArray([])
    });

    // Checklist being viewed
    self.viewingChecklist = ko.observable(null);

    // Initialize Bootstrap modals
    document.addEventListener('DOMContentLoaded', function() {
        const checklistModalEl = document.getElementById('checklistModal');
        const viewModalEl = document.getElementById('checklistViewModal');

        if (checklistModalEl) {
            checklistModal = new bootstrap.Modal(checklistModalEl);
        }
        if (viewModalEl) {
            viewModal = new bootstrap.Modal(viewModalEl);
        }
    });

    // Load all checklists
    self.loadChecklists = async function() {
        self.isLoading(true);
        try {
            const data = await apiService.get('/checklist');
            self.checklists(data);
        } catch (error) {
            console.error('Error loading checklists:', error);
            alert('Kontrol listeleri yüklenirken hata oluştu: ' + error.message);
        } finally {
            self.isLoading(false);
        }
    };

    // Show create modal
    self.showCreateModal = function() {
        self.isEditMode(false);
        self.currentChecklist({
            id: null,
            name: ko.observable(''),
            description: ko.observable(''),
            version: ko.observable(1),
            sections: ko.observableArray([])
        });

        if (checklistModal) {
            checklistModal.show();
        }
    };

    // View checklist details
    self.viewChecklist = function(checklist) {
        self.viewingChecklist(checklist);
        if (viewModal) {
            viewModal.show();
        }
    };

    // Edit checklist
    self.editChecklist = function(checklist) {
        self.isEditMode(true);

        // Deep copy the checklist with observable properties
        const sections = checklist.sections.map(section => ({
            id: section.id,
            name: ko.observable(section.name),
            order: section.order,
            questions: ko.observableArray(section.questions.map(question => ({
                id: question.id,
                text: ko.observable(question.text),
                questionType: ko.observable(question.questionType),
                points: ko.observable(question.points),
                allowNA: ko.observable(question.allowNA),
                options: ko.observable(question.options || ''),
                order: question.order
            })))
        }));

        self.currentChecklist({
            id: checklist.id,
            name: ko.observable(checklist.name),
            description: ko.observable(checklist.description),
            version: ko.observable(checklist.version),
            sections: ko.observableArray(sections)
        });

        if (checklistModal) {
            checklistModal.show();
        }
    };

    // Clone checklist
    self.cloneChecklist = async function(checklist) {
        if (!confirm(`"${checklist.name}" kontrol listesini klonlamak istediğinize emin misiniz?`)) {
            return;
        }

        try {
            await apiService.post(`/checklist/${checklist.id}/clone`);
            alert('Kontrol listesi başarıyla klonlandı');
            await self.loadChecklists(); // Reload list
        } catch (error) {
            console.error('Error cloning checklist:', error);
            alert('Klonlama sırasında hata oluştu: ' + error.message);
        }
    };

    // Add new section
    self.addSection = function() {
        const newSection = {
            id: null,
            name: ko.observable(''),
            order: self.currentChecklist().sections().length + 1,
            questions: ko.observableArray([])
        };
        self.currentChecklist().sections.push(newSection);
    };

    // Remove section
    self.removeSection = function(section) {
        if (confirm('Bu bölümü silmek istediğinize emin misiniz?')) {
            self.currentChecklist().sections.remove(section);
        }
    };

    // Add question to section
    self.addQuestion = function(section) {
        const newQuestion = {
            id: null,
            text: ko.observable(''),
            questionType: ko.observable('YesNo'),
            points: ko.observable(1),
            allowNA: ko.observable(false),
            options: ko.observable(''),
            order: section.questions().length + 1
        };
        section.questions.push(newQuestion);
    };

    // Remove question from section
    self.removeQuestion = function(section, question) {
        if (confirm('Bu soruyu silmek istediğinize emin misiniz?')) {
            section.questions.remove(question);
        }
    };

    // Save checklist (create or update)
    self.saveChecklist = async function() {
        self.isSaving(true);

        try {
            const current = self.currentChecklist();

            // Validate
            if (!current.name() || current.name().trim() === '') {
                alert('Lütfen kontrol listesi adını girin.');
                self.isSaving(false);
                return;
            }

            if (current.sections().length === 0) {
                alert('Lütfen en az bir bölüm ekleyin.');
                self.isSaving(false);
                return;
            }

            // Check if all sections have questions
            for (let section of current.sections()) {
                if (section.questions().length === 0) {
                    alert(`"${section.name()}" bölümünde en az bir soru olmalıdır.`);
                    self.isSaving(false);
                    return;
                }
            }

            // Prepare DTO
            const dto = {
                name: current.name(),
                description: current.description(),
                version: current.version(),
                sections: current.sections().map((section, sIndex) => ({
                    id: section.id,
                    name: section.name(),
                    order: sIndex + 1,
                    questions: section.questions().map((question, qIndex) => ({
                        id: question.id,
                        text: question.text(),
                        questionType: question.questionType(),
                        points: parseFloat(question.points()),
                        allowNA: question.allowNA(),
                        options: question.options() || null,
                        order: qIndex + 1
                    }))
                }))
            };

            // Create or update
            if (self.isEditMode() && current.id) {
                await apiService.put(`/checklist/${current.id}`, dto);
                alert('Kontrol listesi başarıyla güncellendi');
            } else {
                await apiService.post('/checklist', dto);
                alert('Kontrol listesi başarıyla oluşturuldu');
            }

            // Close modal and reload
            if (checklistModal) {
                checklistModal.hide();
            }
            await self.loadChecklists();

        } catch (error) {
            console.error('Error saving checklist:', error);
            alert('Kaydetme sırasında hata oluştu: ' + error.message);
        } finally {
            self.isSaving(false);
        }
    };

    // Load checklists on initialization
    self.loadChecklists();
}
