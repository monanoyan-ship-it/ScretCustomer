// ViewModel Constructor
function ProjectEditViewModel(data) {
    var self = this;
    data = data || {};

    self.id = data.id || null;
    self.name = ko.observable(data.name || '');
    self.description = ko.observable(data.description || '');
    self.checklistId = ko.observable(data.checklistId || '');
    self.startDate = ko.observable(data.startDate ? data.startDate.split('T')[0] : '');
    self.endDate = ko.observable(data.endDate ? data.endDate.split('T')[0] : '');
    self.isActive = ko.observable(data.isActive !== undefined ? data.isActive : true);

    self.toDTO = function() {
        return {
            name: self.name(),
            description: self.description(),
            checklistId: self.checklistId(),
            startDate: self.startDate(),
            endDate: self.endDate(),
            isActive: self.isActive()
        };
    };
}

// Main ViewModel
function ProjectsViewModel() {
    var self = this;

    // State
    self.isLoading = ko.observable(false);
    self.isSaving = ko.observable(false);
    self.errorMessage = ko.observable('');
    self.successMessage = ko.observable('');
    self.includeInactive = ko.observable(false);

    // Data
    self.projects = ko.observableArray([]);
    self.availableChecklists = ko.observableArray([]);
    self.editingProject = ko.observable(null);

    // Modal state
    self.isModalOpen = ko.observable(false);

    // Load projects
    self.loadProjects = function() {
        self.isLoading(true);
        self.errorMessage('');

        var url = '/api/projects';
        if (self.includeInactive()) {
            url += '?includeInactive=true';
        }

        fetch(url, { credentials: 'include' })
            .then(res => {
                if (!res.ok) throw new Error('Yükleme başarısız');
                return res.json();
            })
            .then(data => {
                data.forEach(function(project) {
                    project.completionPercentage = ko.computed(function() {
                        var total = project.totalAssignments || 0;
                        var completed = project.completedAssignments || 0;
                        return total > 0 ? (completed * 100.0 / total).toFixed(0) : 0;
                    });
                });
                self.projects(data);
            })
            .catch(error => {
                console.error('Error:', error);
                self.errorMessage('Projeler yüklenirken bir hata oluştu.');
            })
            .finally(() => {
                self.isLoading(false);
            });
    };

    // Load available checklists
    self.loadChecklists = function() {
        fetch('/api/checklists', { credentials: 'include' })
            .then(res => res.json())
            .then(data => {
                self.availableChecklists(data.filter(c => c.isActive));
            })
            .catch(error => {
                console.error('Error loading checklists:', error);
            });
    };

    // Create new project
    self.createNew = function() {
        self.editingProject(new ProjectEditViewModel());
        self.isModalOpen(true);
    };

    // Edit existing project
    self.editProject = function(project) {
        console.log('Fetching project:', project.id);

        fetch('/api/projects/' + project.id, { credentials: 'include' })
            .then(response => {
                console.log('Response status:', response.status);
                if (!response.ok) {
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
                            console.error('Could not parse JSON, using text:', text);
                            throw new Error('API Error: ' + response.status);
                        }
                    });
                }
                return response.json();
            })
            .then(data => {
                console.log('Project data:', data);
                self.editingProject(new ProjectEditViewModel(data));
                self.isModalOpen(true);
            })
            .catch(error => {
                console.error('Edit error:', error);
                self.errorMessage('Proje yüklenirken bir hata oluştu: ' + error.message);
            });
    };

    // Save project
    self.saveProject = function() {
        var project = self.editingProject();

        // Validation
        if (!project.name() || project.name().trim() === '') {
            alert('Proje adı zorunludur!');
            return;
        }

        if (!project.checklistId()) {
            alert('Kontrol listesi seçmelisiniz!');
            return;
        }

        if (!project.startDate() || !project.endDate()) {
            alert('Başlangıç ve bitiş tarihleri zorunludur!');
            return;
        }

        var dto = project.toDTO();

        self.isSaving(true);
        var endpoint = project.id ? '/api/projects/' + project.id : '/api/projects';
        var method = project.id ? 'PUT' : 'POST';

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
            self.successMessage('Proje başarıyla kaydedildi.');
            self.closeModal();
            self.loadProjects();
        })
        .catch(error => {
            console.error('Error:', error);
            self.errorMessage('Proje kaydedilirken bir hata oluştu.');
        })
        .finally(() => {
            self.isSaving(false);
        });
    };

    // Delete project
    self.deleteProject = function(project) {
        deleteConfirmation.show('Bu projeyi silmek istediğinizden emin misiniz?', function() {

        fetch('/api/projects/' + project.id, {
            method: 'DELETE',
            credentials: 'include'
        })
        .then(response => {
            if (!response.ok) throw new Error('Silme başarısız');
            self.successMessage('Proje başarıyla silindi.');
            self.projects.remove(project);
        })
        .catch(error => {
            console.error('Error:', error);
            self.errorMessage('Proje silinirken bir hata oluştu.');
        });
        });
    };

    // Close modal
    self.closeModal = function() {
        self.isModalOpen(false);
        self.editingProject(null);
    };

    // Initialize
    self.loadProjects();
    self.loadChecklists();
}

// Apply bindings
$(document).ready(function() {
    ko.applyBindings(new ProjectsViewModel(), document.getElementById('projects-app'));
});
