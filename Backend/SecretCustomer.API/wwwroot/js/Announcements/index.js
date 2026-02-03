// Announcements Index ViewModel
function AnnouncementsViewModel() {
    var self = this;

    // State
    self.announcements = ko.observableArray([]);
    self.isLoading = ko.observable(true);
    self.isSaving = ko.observable(false);
    self.isEditing = ko.observable(false);
    self.editingId = ko.observable(null);

    // Filters
    self.filterTypeId = ko.observable('');
    self.filterStatus = ko.observable('');
    self.filterSearch = ko.observable('');

    // Form
    self.form = {
        title: ko.observable(''),
        content: ko.observable(''),
        summary: ko.observable(''),
        typeId: ko.observable('0'),
        priority: ko.observable('3'),
        publishDate: ko.observable(''),
        expiryDate: ko.observable(''),
        isActive: ko.observable(true),
        isPinned: ko.observable(false),
        targetRoles: ko.observable('')
    };

    // Modal reference
    self.modal = null;

    // Filtered list
    self.filteredAnnouncements = ko.computed(function () {
        var list = self.announcements();
        var typeId = self.filterTypeId();
        var status = self.filterStatus();
        var search = (self.filterSearch() || '').toLowerCase();

        if (typeId !== '') {
            list = list.filter(function (a) { return a.typeId == typeId; });
        }
        if (status === 'active') {
            list = list.filter(function (a) { return a.isActive; });
        } else if (status === 'inactive') {
            list = list.filter(function (a) { return !a.isActive; });
        }
        if (search) {
            list = list.filter(function (a) {
                return (a.title || '').toLowerCase().indexOf(search) > -1 ||
                       (a.summary || '').toLowerCase().indexOf(search) > -1;
            });
        }

        return list;
    });

    // Type helpers
    var typeMap = {
        0: { name: T('AnnouncementType.Info', 'Bilgi'), badge: 'bg-info' },
        1: { name: T('AnnouncementType.Warning', 'Uyarı'), badge: 'bg-warning text-dark' },
        2: { name: T('AnnouncementType.Success', 'Başarı'), badge: 'bg-success' },
        3: { name: T('AnnouncementType.Important', 'Önemli'), badge: 'bg-danger' },
        4: { name: T('AnnouncementType.News', 'Haber'), badge: 'bg-primary' },
        5: { name: T('AnnouncementType.System', 'Sistem'), badge: 'bg-secondary' }
    };

    self.getTypeName = function (typeId) {
        var t = typeMap[typeId];
        return t ? t.name : 'Bilinmeyen';
    };

    self.getTypeBadgeClass = function (typeId) {
        var t = typeMap[typeId];
        return t ? t.badge : 'bg-secondary';
    };

    self.formatDate = function (dateStr) {
        if (!dateStr) return '-';
        var d = new Date(dateStr);
        return d.toLocaleDateString('tr-TR') + ' ' + d.toLocaleTimeString('tr-TR', { hour: '2-digit', minute: '2-digit' });
    };

    // Format datetime for input[type=datetime-local]
    function toDateTimeLocal(dateStr) {
        if (!dateStr) return '';
        var d = new Date(dateStr);
        var pad = function (n) { return n < 10 ? '0' + n : n; };
        return d.getFullYear() + '-' + pad(d.getMonth() + 1) + '-' + pad(d.getDate()) +
               'T' + pad(d.getHours()) + ':' + pad(d.getMinutes());
    }

    // Load announcements
    self.loadAnnouncements = function () {
        self.isLoading(true);
        fetch('/api/announcements/admin', { credentials: 'include' })
            .then(function (r) { return r.json(); })
            .then(function (data) {
                self.announcements(data);
            })
            .catch(function () {
                toastr.error(T('Announcement.LoadError', 'Duyurular yüklenirken hata oluştu.'));
            })
            .finally(function () {
                self.isLoading(false);
            });
    };

    // Reset form
    self.resetForm = function () {
        self.form.title('');
        self.form.content('');
        self.form.summary('');
        self.form.typeId('0');
        self.form.priority('3');
        self.form.publishDate('');
        self.form.expiryDate('');
        self.form.isActive(true);
        self.form.isPinned(false);
        self.form.targetRoles('');
    };

    // Open create modal
    self.openCreateModal = function () {
        self.isEditing(false);
        self.editingId(null);
        self.resetForm();

        // Set default publish date to now
        self.form.publishDate(toDateTimeLocal(new Date().toISOString()));

        self.getModal().show();
    };

    // Open edit modal
    self.openEditModal = function (announcement) {
        self.isEditing(true);
        self.editingId(announcement.id);

        self.form.title(announcement.title);
        self.form.content(announcement.content);
        self.form.summary(announcement.summary || '');
        self.form.typeId(String(announcement.typeId));
        self.form.priority(String(announcement.priority));
        self.form.publishDate(toDateTimeLocal(announcement.publishDate));
        self.form.expiryDate(toDateTimeLocal(announcement.expiryDate));
        self.form.isActive(announcement.isActive);
        self.form.isPinned(announcement.isPinned);
        self.form.targetRoles(announcement.targetRoles || '');

        self.getModal().show();
    };

    // Get or create modal instance
    self.getModal = function () {
        if (!self.modal) {
            self.modal = new bootstrap.Modal(document.getElementById('announcementModal'));
        }
        return self.modal;
    };

    // Save (create or update)
    self.saveAnnouncement = function () {
        var title = self.form.title().trim();
        var content = self.form.content().trim();

        if (!title) {
            toastr.warning(T('Announcement.TitleRequired', 'Başlık zorunludur.'));
            return;
        }
        if (!content) {
            toastr.warning(T('Announcement.ContentRequired', 'İçerik zorunludur.'));
            return;
        }

        var dto = {
            title: title,
            content: content,
            summary: self.form.summary().trim() || null,
            typeId: parseInt(self.form.typeId()),
            priority: parseInt(self.form.priority()),
            publishDate: self.form.publishDate() ? new Date(self.form.publishDate()).toISOString() : null,
            expiryDate: self.form.expiryDate() ? new Date(self.form.expiryDate()).toISOString() : null,
            isActive: self.form.isActive(),
            isPinned: self.form.isPinned(),
            targetRoles: self.form.targetRoles().trim() || null
        };

        var url = '/api/announcements';
        var method = 'POST';

        if (self.isEditing()) {
            url = '/api/announcements/' + self.editingId();
            method = 'PUT';
        }

        self.isSaving(true);

        fetch(url, {
            method: method,
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(dto),
            credentials: 'include'
        })
        .then(function (r) {
            if (!r.ok) throw new Error('Request failed');
            return r.json();
        })
        .then(function () {
            toastr.success(self.isEditing()
                ? T('Announcement.UpdateSuccess', 'Duyuru güncellendi.')
                : T('Announcement.CreateSuccess', 'Duyuru oluşturuldu.'));
            self.getModal().hide();
            self.loadAnnouncements();
        })
        .catch(function () {
            toastr.error(self.isEditing()
                ? T('Announcement.UpdateError', 'Duyuru güncellenirken hata oluştu.')
                : T('Announcement.CreateError', 'Duyuru oluşturulurken hata oluştu.'));
        })
        .finally(function () {
            self.isSaving(false);
        });
    };

    // Delete
    self.deleteAnnouncement = function (announcement) {
        showDeleteConfirm(announcement.title, function () {
            fetch('/api/announcements/' + announcement.id, {
                method: 'DELETE',
                credentials: 'include'
            })
            .then(function (r) {
                if (!r.ok) throw new Error('Request failed');
                return r.json();
            })
            .then(function () {
                toastr.success(T('Announcement.DeleteSuccess', 'Duyuru silindi.'));
                self.loadAnnouncements();
            })
            .catch(function () {
                toastr.error(T('Announcement.DeleteError', 'Duyuru silinirken hata oluştu.'));
            });
        });
    };

    // Clear filters
    self.clearFilters = function () {
        self.filterTypeId('');
        self.filterStatus('');
        self.filterSearch('');
    };

    // Init
    self.loadAnnouncements();
}

// Translation keys
var TRANSLATION_KEYS = [
    'AnnouncementType.Info',
    'AnnouncementType.Warning',
    'AnnouncementType.Success',
    'AnnouncementType.Important',
    'AnnouncementType.News',
    'AnnouncementType.System',
    'Announcement.LoadError',
    'Announcement.TitleRequired',
    'Announcement.ContentRequired',
    'Announcement.CreateSuccess',
    'Announcement.UpdateSuccess',
    'Announcement.CreateError',
    'Announcement.UpdateError',
    'Announcement.DeleteSuccess',
    'Announcement.DeleteError'
];

// Apply bindings when document is ready
$(document).ready(function () {
    Localization.loadKeys(TRANSLATION_KEYS).then(function () {
        ko.applyBindings(new AnnouncementsViewModel(), document.getElementById('announcements-app'));
    });
});
