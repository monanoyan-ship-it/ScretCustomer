// Customer Personnel ViewModel
function CustomerPersonnelViewModel(customerId) {
    var self = this;

    // Observables
    self.customerId = ko.observable(customerId);
    self.customer = ko.observable(null);
    self.personnel = ko.observableArray([]);
    self.isLoading = ko.observable(false);
    self.errorMessage = ko.observable('');
    self.successMessage = ko.observable('');
    self.showInactive = ko.observable(false);

    // Form observables
    self.isEditing = ko.observable(false);
    self.showForm = ko.observable(false);
    self.currentPersonnel = ko.observable({
        id: null,
        customerId: customerId,
        username: '',
        email: '',
        password: '',
        firstName: '',
        lastName: '',
        phoneNumber: '',
        department: '',
        title: '',
        role: 1, // CustomerManager
        isActive: true,
        notes: ''
    });

    // Personnel roles
    self.personnelRoles = [
        { value: 1, text: 'Müşteri Yöneticisi' },
        { value: 2, text: 'Müşteri Süpervizörü' },
        { value: 3, text: 'Müşteri Operatörü' },
        { value: 4, text: 'Müşteri Görüntüleyici' }
    ];

    // Task assignment roles
    self.taskRoles = [
        { value: 1, text: 'Görev Sahibi' },
        { value: 2, text: 'Görev Yardımcısı' },
        { value: 3, text: 'Gözlemci' },
        { value: 4, text: 'Onaylayıcı' }
    ];

    // Available tasks for assignment
    self.availableTasks = ko.observableArray([]);
    self.selectedPersonnelForTask = ko.observable(null);
    self.showTaskAssignmentModal = ko.observable(false);
    self.selectedTask = ko.observable(null);
    self.selectedTaskRole = ko.observable(1);

    // Computed
    self.filteredPersonnel = ko.computed(function() {
        if (self.showInactive()) {
            return self.personnel();
        }
        return self.personnel().filter(function(p) { return p.isActive; });
    });

    self.getRoleName = function(role) {
        var roleObj = self.personnelRoles.find(function(r) { return r.value === role; });
        return roleObj ? roleObj.text : 'Bilinmeyen';
    };

    // Load customer info
    self.loadCustomer = function() {
        if (!customerId) return;

        customerApiService.getCustomerById(customerId)
            .then(function(data) {
                self.customer(data);
            })
            .catch(function(error) {
                console.error('Error loading customer:', error);
                self.errorMessage('Müşteri bilgileri yüklenirken bir hata oluştu.');
            });
    };

    // Load personnel
    self.loadPersonnel = function() {
        if (!customerId) return;

        self.isLoading(true);
        self.errorMessage('');

        customerApiService.getPersonnelByCustomerId(customerId, self.showInactive())
            .then(function(data) {
                self.personnel(data || []);
            })
            .catch(function(error) {
                console.error('Error loading personnel:', error);
                self.errorMessage('Personeller yüklenirken bir hata oluştu: ' + (error.message || ''));
            })
            .finally(function() {
                self.isLoading(false);
            });
    };

    // Show create form
    self.showCreateForm = function() {
        self.isEditing(false);
        self.currentPersonnel({
            id: null,
            customerId: customerId,
            username: '',
            email: '',
            password: '',
            firstName: '',
            lastName: '',
            phoneNumber: '',
            department: '',
            title: '',
            role: 1,
            isActive: true,
            notes: ''
        });
        self.showForm(true);
    };

    // Show edit form
    self.editPersonnel = function(personnel) {
        self.isEditing(true);
        self.currentPersonnel({
            id: personnel.id,
            customerId: personnel.customerId,
            username: personnel.username,
            email: personnel.email,
            password: '', // Don't populate password
            firstName: personnel.firstName,
            lastName: personnel.lastName,
            phoneNumber: personnel.phoneNumber || '',
            department: personnel.department || '',
            title: personnel.title || '',
            role: personnel.role,
            isActive: personnel.isActive,
            notes: personnel.notes || ''
        });
        self.showForm(true);
    };

    // Save personnel
    self.savePersonnel = function() {
        self.errorMessage('');
        self.successMessage('');

        var personnel = self.currentPersonnel();

        // Validation
        if (!personnel.username || !personnel.email || !personnel.firstName || !personnel.lastName) {
            self.errorMessage('Kullanıcı adı, e-posta, ad ve soyad zorunludur.');
            return;
        }

        if (!self.isEditing() && !personnel.password) {
            self.errorMessage('Yeni personel için şifre zorunludur.');
            return;
        }

        var promise = self.isEditing() 
            ? customerApiService.updatePersonnel(personnel.id, personnel)
            : customerApiService.createPersonnel(personnel);

        promise
            .then(function() {
                self.successMessage(self.isEditing() ? 'Personel başarıyla güncellendi.' : 'Personel başarıyla oluşturuldu.');
                self.showForm(false);
                self.loadPersonnel();
            })
            .catch(function(error) {
                console.error('Error saving personnel:', error);
                self.errorMessage('Personel kaydedilirken bir hata oluştu: ' + (error.message || ''));
            });
    };

    // Cancel form
    self.cancelForm = function() {
        self.showForm(false);
        self.errorMessage('');
        self.successMessage('');
    };

    // Delete personnel
    self.deletePersonnel = function(personnel) {
        if (!confirm('Bu personeli silmek istediğinizden emin misiniz?\n\n' + personnel.fullName)) {
            return;
        }

        customerApiService.deletePersonnel(personnel.id)
            .then(function() {
                self.successMessage('Personel başarıyla silindi.');
                self.loadPersonnel();
            })
            .catch(function(error) {
                console.error('Error deleting personnel:', error);
                self.errorMessage('Personel silinirken bir hata oluştu: ' + (error.message || ''));
            });
    };

    // Show task assignment modal
    self.showTaskAssignment = function(personnel) {
        self.selectedPersonnelForTask(personnel);
        self.showTaskAssignmentModal(true);
        // Load available tasks for this customer
        // TODO: Implement task loading when task API is ready
    };

    // Assign task to personnel
    self.assignTask = function() {
        // TODO: Implement task assignment when task API is ready
        self.successMessage('Görev atama özelliği yakında eklenecek.');
        self.showTaskAssignmentModal(false);
    };

    // Toggle inactive personnel
    self.toggleShowInactive = function() {
        self.showInactive(!self.showInactive());
        self.loadPersonnel();
    };

    // Go back to customers list
    self.goBack = function() {
        window.location.hash = '#/customers';
    };

    // Initialize
    self.loadCustomer();
    self.loadPersonnel();
}
