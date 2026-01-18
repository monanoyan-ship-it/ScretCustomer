// CustomerPortal StaffTrainings ViewModel
var TRANSLATION_KEYS = [
    'Common.Loading',
    'Common.Error'
];

function StaffTrainingsViewModel() {
    var self = this;

    // State
    self.isLoading = ko.observable(false);
    self.trainings = ko.observableArray([]);
    self.personnelList = ko.observableArray([]);
    self.assignmentList = ko.observableArray([]);

    // Filters
    self.filterPersonnelId = ko.observable('');
    self.filterAssignmentId = ko.observable('');
    self.filterStatusId = ko.observable('');

    // Stats computed
    self.stats = ko.computed(function() {
        var list = self.trainings();
        var personnelIds = {};
        list.forEach(function(t) { personnelIds[t.personnelId] = true; });

        return {
            totalPersonnel: Object.keys(personnelIds).length,
            pending: list.filter(function(t) { return t.statusId === 1; }).length,
            completed: list.filter(function(t) { return t.isCompleted; }).length,
            overdue: list.filter(function(t) { return t.isOverdue && !t.isCompleted; }).length
        };
    });

    // Filtered trainings
    self.filteredTrainings = ko.computed(function() {
        var list = self.trainings();
        var personnelId = self.filterPersonnelId();
        var assignmentId = self.filterAssignmentId();
        var statusId = self.filterStatusId();

        return list.filter(function(t) {
            if (personnelId && t.personnelId !== parseInt(personnelId)) return false;
            if (assignmentId && t.assignmentId !== parseInt(assignmentId)) return false;

            if (statusId === 'overdue') {
                if (!t.isOverdue || t.isCompleted) return false;
            } else if (statusId && t.statusId !== parseInt(statusId)) {
                return false;
            }

            return true;
        });
    });

    // Format date
    self.formatDate = function(dateStr) {
        if (!dateStr) return '-';
        var d = new Date(dateStr);
        return d.toLocaleDateString('tr-TR');
    };

    // Format datetime
    self.formatDateTime = function(dateStr) {
        if (!dateStr) return '-';
        var d = new Date(dateStr);
        return d.toLocaleDateString('tr-TR') + ' ' + d.toLocaleTimeString('tr-TR', { hour: '2-digit', minute: '2-digit' });
    };

    // Get progress percentage
    self.getProgressPercentage = function(training) {
        if (!training.videoDurationSeconds) return 0;
        return Math.min(100, Math.round((training.watchedSeconds / training.videoDurationSeconds) * 100));
    };

    // Clear filters
    self.clearFilters = function() {
        self.filterPersonnelId('');
        self.filterAssignmentId('');
        self.filterStatusId('');
    };

    // Load trainings
    self.loadTrainings = function() {
        self.isLoading(true);

        customerApiFetch('/api/customer/portal/staff-trainings')
            .then(function(r) { return r.json(); })
            .then(function(data) {
                self.trainings(data.trainings || []);

                // Build personnel list from unique personnel
                var personnelMap = {};
                (data.trainings || []).forEach(function(t) {
                    if (!personnelMap[t.personnelId]) {
                        personnelMap[t.personnelId] = {
                            id: t.personnelId,
                            fullName: t.personnelName
                        };
                    }
                });
                self.personnelList(Object.values(personnelMap).sort(function(a, b) {
                    return a.fullName.localeCompare(b.fullName);
                }));

                // Build assignment list from unique assignments
                var assignmentMap = {};
                (data.trainings || []).forEach(function(t) {
                    if (!assignmentMap[t.assignmentId]) {
                        assignmentMap[t.assignmentId] = {
                            id: t.assignmentId,
                            title: t.assignmentTitle + ' - ' + t.videoTitle
                        };
                    }
                });
                self.assignmentList(Object.values(assignmentMap).sort(function(a, b) {
                    return a.title.localeCompare(b.title);
                }));
            })
            .catch(function(err) {
                toastr.error(T('Common.Error', 'Hata olustu'));
                console.error(err);
            })
            .finally(function() {
                self.isLoading(false);
            });
    };

    // Initialize
    self.loadTrainings();
}

// Apply bindings after translations loaded
$(document).ready(function() {
    Localization.loadKeys(TRANSLATION_KEYS).then(function() {
        ko.applyBindings(new StaffTrainingsViewModel(), document.getElementById('staff-trainings-app'));
    });
});
