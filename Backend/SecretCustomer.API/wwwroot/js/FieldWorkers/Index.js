function FieldWorkerEditViewModel(data) {
    var self = this;
    data = data || {};

    self.id = data.id || null;
    self.firstName = ko.observable(data.firstName || '');
    self.lastName = ko.observable(data.lastName || '');
    self.phoneNumber = ko.observable(data.phoneNumber || '');
    self.email = ko.observable(data.email || '');
    self.address = ko.observable(data.address || '');
    self.notes = ko.observable(data.notes || '');
    self.isActive = ko.observable(data.isActive !== undefined ? data.isActive : true);
}

function FieldWorkersViewModel() {
    var self = this;

    // State
    self.isLoading = ko.observable(false);
    self.isSaving = ko.observable(false);
    self.errorMessage = ko.observable('');
    self.successMessage = ko.observable('');

    // Data
    self.fieldWorkers = ko.observableArray([]);
    self.editingFieldWorker = ko.observable(null);

    // Modal state
    self.isModalOpen = ko.observable(false);

    // Load field workers
    self.loadFieldWorkers = function() {
        self.isLoading(true);
        self.errorMessage('');

        fetch('/api/fieldworkers', { credentials: 'include' })
            .then(response => {
                if (!response.ok) throw new Error('Yükleme başarısız');
                return response.json();
            })
            .then(data => {
                self.fieldWorkers(data);
            })
            .catch(error => {
                console.error('Error:', error);
                self.errorMessage('Saha çalışanları yüklenirken bir hata oluştu.');
            })
            .finally(() => {
                self.isLoading(false);
            });
    };

    // Create new field worker
    self.createNew = function() {
        self.editingFieldWorker(new FieldWorkerEditViewModel());
        self.isModalOpen(true);
    };

    // Edit existing field worker
    self.editFieldWorker = function(fieldWorker) {
        self.editingFieldWorker(new FieldWorkerEditViewModel(fieldWorker));
        self.isModalOpen(true);
    };

    // Save field worker
    self.saveFieldWorker = function() {
        var fw = self.editingFieldWorker();

        // Validation
        if (!fw.firstName() || fw.firstName().trim() === '') {
            alert('Ad alanı zorunludur!');
            return;
        }

        if (!fw.lastName() || fw.lastName().trim() === '') {
            alert('Soyad alanı zorunludur!');
            return;
        }

        if (!fw.phoneNumber() || fw.phoneNumber().trim() === '') {
            alert('Telefon numarası zorunludur!');
            return;
        }

        if (!fw.address() || fw.address().trim() === '') {
            alert('Adres alanı zorunludur!');
            return;
        }

        // Prepare DTO
        var dto = {
            firstName: fw.firstName(),
            lastName: fw.lastName(),
            phoneNumber: fw.phoneNumber(),
            email: fw.email() || null,
            address: fw.address(),
            notes: fw.notes() || null,
            isActive: fw.isActive()
        };

        self.isSaving(true);
        var endpoint = fw.id ? '/api/fieldworkers/' + fw.id : '/api/fieldworkers';
        var method = fw.id ? 'PUT' : 'POST';

        fetch(endpoint, {
            method: method,
            headers: { 'Content-Type': 'application/json' },
            credentials: 'include',
            body: JSON.stringify(dto)
        })
        .then(response => {
            if (!response.ok) {
                return response.json().then(err => {
                    throw new Error(err.message || 'Kayıt başarısız');
                });
            }
            return response.json();
        })
        .then(data => {
            self.successMessage('Saha çalışanı başarıyla kaydedildi.');
            self.closeModal();
            self.loadFieldWorkers();
        })
        .catch(error => {
            console.error('Error:', error);
            self.errorMessage(error.message || 'Saha çalışanı kaydedilirken bir hata oluştu.');
        })
        .finally(() => {
            self.isSaving(false);
        });
    };

    // Delete field worker
    self.deleteFieldWorker = function(fieldWorker) {
        deleteConfirmation.show('Bu saha çalışanını silmek istediğinizden emin misiniz?', function() {

        fetch('/api/fieldworkers/' + fieldWorker.id, {
            method: 'DELETE',
            credentials: 'include'
        })
        .then(response => {
            if (!response.ok) throw new Error('Silme başarısız');
            self.successMessage('Saha çalışanı başarıyla silindi.');
            self.fieldWorkers.remove(fieldWorker);
        })
        .catch(error => {
            console.error('Error:', error);
            self.errorMessage('Saha çalışanı silinirken bir hata oluştu.');
        });
        });
    };

    // Close modal
    self.closeModal = function() {
        self.isModalOpen(false);
        self.editingFieldWorker(null);
    };

    // Initialize
    self.loadFieldWorkers();
}

// Apply bindings
$(document).ready(function() {
    ko.applyBindings(new FieldWorkersViewModel(), document.getElementById('fieldworkers-app'));
});
