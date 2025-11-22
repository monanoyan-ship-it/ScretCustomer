// Assignments ViewModel
function AssignmentsViewModel() {
    const self = this;

    // Observable arrays and properties
    self.assignments = ko.observableArray([]);
    self.projects = ko.observableArray([]);
    self.branches = ko.observableArray([]);
    self.checklists = ko.observableArray([]);
    self.evaluators = ko.observableArray([]);

    self.isLoading = ko.observable(false);
    self.isSaving = ko.observable(false);

    // Filters
    self.statusFilter = ko.observable('');
    self.typeFilter = ko.observable('');
    self.searchQuery = ko.observable('');

    // Filtered assignments
    self.filteredAssignments = ko.computed(() => {
        let filtered = self.assignments();

        // Status filter
        if (self.statusFilter()) {
            filtered = filtered.filter(a => a.status === self.statusFilter());
        }

        // Type filter
        if (self.typeFilter()) {
            filtered = filtered.filter(a => a.assignmentType === self.typeFilter());
        }

        // Search query
        if (self.searchQuery() && self.searchQuery().trim() !== '') {
            const query = self.searchQuery().toLowerCase();
            filtered = filtered.filter(a =>
                (a.projectName && a.projectName.toLowerCase().includes(query)) ||
                (a.branchName && a.branchName.toLowerCase().includes(query))
            );
        }

        return filtered;
    });

    // New assignment (single)
    self.newAssignment = {
        projectId: ko.observable(''),
        branchId: ko.observable(''),
        checklistId: ko.observable(''),
        assignmentType: ko.observable(''),
        evaluatorId: ko.observable(''),
        deadline: ko.observable(''),
        notes: ko.observable('')
    };

    // Bulk assignment
    self.bulkAssignment = {
        projectId: ko.observable(''),
        checklistId: ko.observable('')
    };
    self.bulkPreview = ko.observableArray([]);

    // Modal instances
    let singleModal = null;
    let bulkModal = null;

    // Initialize Bootstrap modals
    document.addEventListener('DOMContentLoaded', function() {
        const singleModalEl = document.getElementById('singleAssignmentModal');
        const bulkModalEl = document.getElementById('bulkAssignmentModal');

        if (singleModalEl) {
            singleModal = new bootstrap.Modal(singleModalEl);
        }
        if (bulkModalEl) {
            bulkModal = new bootstrap.Modal(bulkModalEl);
        }
    });

    // Load all assignments
    self.loadAssignments = async function() {
        self.isLoading(true);
        try {
            const data = await apiService.get('/assignment');
            self.assignments(data);
        } catch (error) {
            console.error('Error loading assignments:', error);
            alert('Atamalar yüklenirken hata oluştu: ' + error.message);
        } finally {
            self.isLoading(false);
        }
    };

    // Load lookup data (projects, branches, checklists, evaluators)
    self.loadLookupData = async function() {
        try {
            const [projects, checklists, evaluators] = await Promise.all([
                apiService.get('/project'),
                apiService.get('/checklist'),
                // TODO: Add users endpoint to get evaluators
                Promise.resolve([]) // Placeholder
            ]);

            self.projects(projects);
            self.checklists(checklists);
            self.evaluators(evaluators);

            // TODO: Load branches - needs API endpoint
            // For now, we'll load them when needed based on project

        } catch (error) {
            console.error('Error loading lookup data:', error);
        }
    };

    // Show create single modal
    self.showCreateSingleModal = function() {
        // Reset form
        self.newAssignment.projectId('');
        self.newAssignment.branchId('');
        self.newAssignment.checklistId('');
        self.newAssignment.assignmentType('');
        self.newAssignment.evaluatorId('');
        self.newAssignment.deadline('');
        self.newAssignment.notes('');

        if (singleModal) {
            singleModal.show();
        }
    };

    // Show create bulk modal
    self.showCreateBulkModal = function() {
        // Reset form
        self.bulkAssignment.projectId('');
        self.bulkAssignment.checklistId('');
        self.bulkPreview([]);

        if (bulkModal) {
            bulkModal.show();
        }
    };

    // Create single assignment
    self.createSingleAssignment = async function() {
        self.isSaving(true);

        try {
            // Validation
            if (!self.newAssignment.projectId() || !self.newAssignment.branchId() ||
                !self.newAssignment.checklistId() || !self.newAssignment.assignmentType() ||
                !self.newAssignment.deadline()) {
                alert('Lütfen tüm gerekli alanları doldurun.');
                self.isSaving(false);
                return;
            }

            if (self.newAssignment.assignmentType() === 'Internal' && !self.newAssignment.evaluatorId()) {
                alert('İç değerlendirme için değerlendirici seçmelisiniz.');
                self.isSaving(false);
                return;
            }

            // Prepare DTO
            const dto = {
                projectId: self.newAssignment.projectId(),
                branchId: self.newAssignment.branchId(),
                checklistId: self.newAssignment.checklistId(),
                assignmentType: self.newAssignment.assignmentType(),
                evaluatorId: self.newAssignment.assignmentType() === 'Internal' ?
                    self.newAssignment.evaluatorId() : null,
                deadline: self.newAssignment.deadline(),
                notes: self.newAssignment.notes() || null
            };

            // Create assignment
            await apiService.post('/assignment', dto);

            alert('Atama başarıyla oluşturuldu');

            // Close modal and reload
            if (singleModal) {
                singleModal.hide();
            }
            await self.loadAssignments();

        } catch (error) {
            console.error('Error creating assignment:', error);
            alert('Atama oluşturulurken hata oluştu: ' + error.message);
        } finally {
            self.isSaving(false);
        }
    };

    // Handle file upload for bulk assignment
    self.handleFileUpload = function(data, event) {
        const file = event.target.files[0];
        if (!file) return;

        // TODO: This requires Python service for Excel parsing
        // For now, show a placeholder message
        alert('Excel dosyası işleme özelliği Python servisi tamamlandığında eklenecektir.');

        // Placeholder preview
        // In real implementation, file would be sent to Python service for parsing
        // and the result would populate bulkPreview
    };

    // Create bulk assignments
    self.createBulkAssignments = async function() {
        if (self.bulkPreview().length === 0) {
            alert('Önce Excel dosyası yükleyin.');
            return;
        }

        self.isSaving(true);

        try {
            const dto = {
                projectId: self.bulkAssignment.projectId(),
                checklistId: self.bulkAssignment.checklistId(),
                assignments: self.bulkPreview()
            };

            await apiService.post('/assignment/bulk', dto);

            alert(`${self.bulkPreview().length} atama başarıyla oluşturuldu`);

            // Close modal and reload
            if (bulkModal) {
                bulkModal.hide();
            }
            await self.loadAssignments();

        } catch (error) {
            console.error('Error creating bulk assignments:', error);
            alert('Toplu atama oluşturulurken hata oluştu: ' + error.message);
        } finally {
            self.isSaving(false);
        }
    };

    // View assignment details
    self.viewAssignment = function(assignment) {
        // TODO: Show detail modal or navigate to detail page
        console.log('View assignment:', assignment);
        alert('Atama detay görüntüleme özelliği eklenecek');
    };

    // Edit assignment
    self.editAssignment = function(assignment) {
        // TODO: Show edit modal
        console.log('Edit assignment:', assignment);
        alert('Atama düzenleme özelliği eklenecek');
    };

    // Delete assignment
    self.deleteAssignment = async function(assignment) {
        if (!confirm(`"${assignment.projectName}" - "${assignment.branchName}" atamasını silmek istediğinize emin misiniz?`)) {
            return;
        }

        try {
            await apiService.delete(`/assignment/${assignment.id}`);
            alert('Atama başarıyla silindi');
            await self.loadAssignments();
        } catch (error) {
            console.error('Error deleting assignment:', error);
            alert('Atama silinirken hata oluştu: ' + error.message);
        }
    };

    // Initialize
    self.loadAssignments();
    self.loadLookupData();
}
