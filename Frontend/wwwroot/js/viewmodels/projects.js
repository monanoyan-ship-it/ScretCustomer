// Projects ViewModel
function ProjectsViewModel() {
    const self = this;

    // Observables
    self.projects = ko.observableArray([]);
    self.checklists = ko.observableArray([]);
    self.isLoading = ko.observable(false);
    self.includeInactive = ko.observable(false);

    // Modal state
    self.showModal = ko.observable(false);
    self.isEditMode = ko.observable(false);
    self.currentProject = ko.observable(null);

    // Form fields
    self.projectName = ko.observable('');
    self.projectDescription = ko.observable('');
    self.selectedChecklistId = ko.observable('');
    self.assignmentType = ko.observable('Internal');
    self.startDate = ko.observable('');
    self.endDate = ko.observable('');

    // Computed
    self.filteredProjects = ko.computed(() => {
        if (self.includeInactive()) {
            return self.projects();
        }
        return self.projects().filter(p => p.isActive);
    });

    self.completionPercentage = (project) => {
        if (project.totalAssignments === 0) return 0;
        return Math.round((project.completedAssignments / project.totalAssignments) * 100);
    };

    self.getStatusBadgeClass = (project) => {
        if (!project.isActive) return 'bg-secondary';
        const now = new Date();
        const endDate = new Date(project.endDate);
        if (endDate < now) return 'bg-warning';
        return 'bg-success';
    };

    self.getStatusText = (project) => {
        if (!project.isActive) return 'Kapalı';
        const now = new Date();
        const endDate = new Date(project.endDate);
        if (endDate < now) return 'Süresi Dolmuş';
        return 'Aktif';
    };

    // Load data
    self.loadProjects = async () => {
        self.isLoading(true);
        try {
            const data = await apiService.get(`/project?includeInactive=${self.includeInactive()}`);
            self.projects(data);
        } catch (error) {
            console.error('Error loading projects:', error);
            alert('Projeler yüklenirken hata oluştu: ' + error.message);
        } finally {
            self.isLoading(false);
        }
    };

    self.loadChecklists = async () => {
        try {
            const data = await apiService.get('/checklist');
            self.checklists(data.filter(c => c.isActive));
        } catch (error) {
            console.error('Error loading checklists:', error);
        }
    };

    // Modal functions
    self.showCreateModal = () => {
        self.isEditMode(false);
        self.currentProject(null);
        self.clearForm();
        self.showModal(true);
    };

    self.showEditModal = (project) => {
        self.isEditMode(true);
        self.currentProject(project);
        self.projectName(project.name);
        self.projectDescription(project.description || '');
        self.selectedChecklistId(project.checklistId);
        self.assignmentType(project.assignmentType);
        self.startDate(project.startDate.split('T')[0]);
        self.endDate(project.endDate.split('T')[0]);
        self.showModal(true);
    };

    self.closeModal = () => {
        self.showModal(false);
        self.clearForm();
    };

    self.clearForm = () => {
        self.projectName('');
        self.projectDescription('');
        self.selectedChecklistId('');
        self.assignmentType('Internal');
        self.startDate('');
        self.endDate('');
    };

    // CRUD operations
    self.saveProject = async () => {
        // Validation
        if (!self.projectName() || !self.selectedChecklistId() || !self.startDate() || !self.endDate()) {
            alert('Lütfen tüm zorunlu alanları doldurun.');
            return;
        }

        if (new Date(self.startDate()) >= new Date(self.endDate())) {
            alert('Bitiş tarihi başlangıç tarihinden sonra olmalıdır.');
            return;
        }

        const dto = {
            name: self.projectName(),
            description: self.projectDescription() || null,
            checklistId: self.selectedChecklistId(),
            assignmentType: self.assignmentType(),
            startDate: self.startDate(),
            endDate: self.endDate()
        };

        try {
            if (self.isEditMode()) {
                // Update
                await apiService.put(`/project/${self.currentProject().id}`, dto);
                alert('Proje başarıyla güncellendi.');
            } else {
                // Create
                await apiService.post('/project', dto);
                alert('Proje başarıyla oluşturuldu.');
            }

            self.closeModal();
            await self.loadProjects();
        } catch (error) {
            console.error('Error saving project:', error);
            alert('Proje kaydedilirken hata oluştu: ' + error.message);
        }
    };

    self.deleteProject = async (project) => {
        if (!confirm(`"${project.name}" projesini silmek istediğinizden emin misiniz?`)) {
            return;
        }

        try {
            await apiService.delete(`/project/${project.id}`);
            alert('Proje başarıyla silindi.');
            await self.loadProjects();
        } catch (error) {
            console.error('Error deleting project:', error);
            alert('Proje silinirken hata oluştu: ' + error.message);
        }
    };

    self.closeProject = async (project) => {
        if (!confirm(`"${project.name}" projesini kapatmak istediğinizden emin misiniz?`)) {
            return;
        }

        try {
            await apiService.post(`/project/${project.id}/close`);
            alert('Proje başarıyla kapatıldı.');
            await self.loadProjects();
        } catch (error) {
            console.error('Error closing project:', error);
            alert('Proje kapatılırken hata oluştu: ' + error.message);
        }
    };

    self.viewProjectDetails = (project) => {
        // Navigate to assignments page filtered by this project
        // This could be implemented in the future
        alert(`Proje detayları: ${project.name}\nToplam Atama: ${project.totalAssignments}\nTamamlanan: ${project.completedAssignments}`);
    };

    // Toggle include inactive
    self.toggleIncludeInactive = () => {
        self.includeInactive(!self.includeInactive());
        self.loadProjects();
    };

    // Format date for display
    self.formatDate = (dateString) => {
        const date = new Date(dateString);
        return date.toLocaleDateString('tr-TR');
    };

    // Initialize
    self.init = async () => {
        await self.loadChecklists();
        await self.loadProjects();
    };

    self.init();
}
