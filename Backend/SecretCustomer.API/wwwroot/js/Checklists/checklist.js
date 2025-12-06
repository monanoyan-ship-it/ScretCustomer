// Checklist ViewModel
function ChecklistViewModel() {
    var self = this;

    self.checklists = ko.observableArray([]);
    self.isLoading = ko.observable(true);
    self.errorMessage = ko.observable('');

    self.loadChecklists = function() {
        self.isLoading(true);
        self.errorMessage('');

        apiService.get('/checklists')
            .then(function(data) {
                self.checklists(data);
            })
            .catch(function(error) {
                console.error('Checklists error:', error);
                self.errorMessage('Kontrol listeleri yüklenirken bir hata oluştu.');
            })
            .finally(function() {
                self.isLoading(false);
            });
    };

    self.viewChecklist = function(checklist) {
        window.location.hash = '#/checklists/' + checklist.id;
    };

    self.deleteChecklist = function(checklist) {
        if (!confirm('Bu kontrol listesini silmek istediğinizden emin misiniz?')) {
            return;
        }

        apiService.delete('/checklists/' + checklist.id)
            .then(function() {
                self.checklists.remove(checklist);
                alert('Kontrol listesi başarıyla silindi.');
            })
            .catch(function(error) {
                console.error('Delete error:', error);
                alert('Kontrol listesi silinirken bir hata oluştu.');
            });
    };

    // Initialize
    self.loadChecklists();
}
