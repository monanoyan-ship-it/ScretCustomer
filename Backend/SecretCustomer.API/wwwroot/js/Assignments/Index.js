// ViewModel Constructor
function AssignmentEditViewModel(data) {
    var self = this;
    data = data || {};

    self.projectId = ko.observable(data.projectId || '');
    self.branchId = ko.observable(data.branchId || '');
    self.checklistId = ko.observable(data.checklistId || '');
    self.assignedUserId = ko.observable(data.assignedUserId || '');
    self.dueDate = ko.observable(data.dueDate ? data.dueDate.split('T')[0] : '');

    self.toDTO = function() {
        return {
            projectId: self.projectId(),
            branchId: self.branchId(),
            checklistId: self.checklistId(),
            assignedUserId: self.assignedUserId() || null,
            dueDate: self.dueDate()
        };
    };
}

// Main ViewModel
function AssignmentsViewModel() {
    var self = this;

    // State
    self.isLoading = ko.observable(false);
    self.isSaving = ko.observable(false);
    self.errorMessage = ko.observable('');
    self.successMessage = ko.observable('');

    // Data
    self.assignments = ko.observableArray([]);
    self.availableProjects = ko.observableArray([]);
    self.availableBranches = ko.observableArray([]);
    self.availableChecklists = ko.observableArray([]);
    self.availableEvaluators = ko.observableArray([]);
    self.editingAssignment = ko.observable(null);

    // Modal state
    self.isModalOpen = ko.observable(false);

    // Load assignments
    self.loadAssignments = function() {
        self.isLoading(true);
        self.errorMessage('');

        fetch('/api/assignments?projectId=00000000-0000-0000-0000-000000000000', {
            credentials: 'include'
        })
        .then(res => {
            if (!res.ok) throw new Error('Yükleme başarısız');
            return res.json();
        })
        .then(data => {
            self.assignments(data);
        })
        .catch(error => {
            console.error('Error:', error);
            self.errorMessage('Atamalar yüklenirken bir hata oluştu.');
        })
        .finally(() => {
            self.isLoading(false);
        });
    };

    // Load available data
    self.loadProjects = function() {
        fetch('/api/projects', { credentials: 'include' })
            .then(res => res.json())
            .then(data => {
                self.availableProjects(data.filter(p => p.isActive));
            })
            .catch(error => console.error('Error loading projects:', error));
    };

    self.loadBranches = function() {
        fetch('/api/branches', { credentials: 'include' })
            .then(res => res.json())
            .then(data => {
                self.availableBranches(data);
            })
            .catch(error => console.error('Error loading branches:', error));
    };

    self.loadChecklists = function() {
        fetch('/api/checklists', { credentials: 'include' })
            .then(res => res.json())
            .then(data => {
                self.availableChecklists(data.filter(c => c.isActive));
            })
            .catch(error => console.error('Error loading checklists:', error));
    };

    self.loadEvaluators = function() {
        fetch('/api/users/role/3', { credentials: 'include' })
            .then(res => res.json())
            .then(data => {
                self.availableEvaluators(data);
            })
            .catch(error => console.error('Error loading evaluators:', error));
    };

    // Create new assignment
    self.createNew = function() {
        self.editingAssignment(new AssignmentEditViewModel());
        self.isModalOpen(true);
    };

    // Save assignment
    self.saveAssignment = function() {
        var assignment = self.editingAssignment();

        // Validation
        if (!assignment.projectId()) {
            alert('Proje seçmelisiniz!');
            return;
        }

        if (!assignment.branchId()) {
            alert('Şube seçmelisiniz!');
            return;
        }

        if (!assignment.checklistId()) {
            alert('Kontrol listesi seçmelisiniz!');
            return;
        }

        if (!assignment.dueDate()) {
            alert('Son tarih zorunludur!');
            return;
        }

        var dto = assignment.toDTO();

        self.isSaving(true);

        fetch('/api/assignments', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            credentials: 'include',
            body: JSON.stringify(dto)
        })
        .then(response => {
            if (!response.ok) throw new Error('Kayıt başarısız');
            return response.json();
        })
        .then(data => {
            self.successMessage('Atama başarıyla oluşturuldu.');
            self.closeModal();
            self.loadAssignments();
        })
        .catch(error => {
            console.error('Error:', error);
            self.errorMessage('Atama oluşturulurken bir hata oluştu.');
        })
        .finally(() => {
            self.isSaving(false);
        });
    };

    // Delete assignment
    self.deleteAssignment = function(assignment) {
        if (!confirm('Bu atamayı silmek istediğinizden emin misiniz?')) return;

        fetch('/api/assignments/' + assignment.id, {
            method: 'DELETE',
            credentials: 'include'
        })
        .then(response => {
            if (!response.ok) throw new Error('Silme başarısız');
            self.successMessage('Atama başarıyla silindi.');
            self.assignments.remove(assignment);
        })
        .catch(error => {
            console.error('Error:', error);
            self.errorMessage('Atama silinirken bir hata oluştu.');
        });
    };

    // Close modal
    self.closeModal = function() {
        self.isModalOpen(false);
        self.editingAssignment(null);
    };

    // Initialize
    self.loadAssignments();
    self.loadProjects();
    self.loadBranches();
    self.loadChecklists();
    self.loadEvaluators();
}

// Apply bindings
$(document).ready(function() {
    ko.applyBindings(new AssignmentsViewModel(), document.getElementById('assignments-app'));
});
