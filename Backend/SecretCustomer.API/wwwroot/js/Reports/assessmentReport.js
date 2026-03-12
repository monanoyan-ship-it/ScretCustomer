// Translation Keys
var TRANSLATION_KEYS = [
    'Common.Loading',
    'FeedbackRole.Self',
    'FeedbackRole.Manager',
    'FeedbackRole.Peer',
    'FeedbackRole.Subordinate',
    'Assessment.Report.LoadError',
    'Assessment.Report.NoData'
];

function AssessmentReportViewModel() {
    var self = this;

    // Filters
    self.projects = ko.observableArray([]);
    self.periods = ko.observableArray([]);
    self.personnel = ko.observableArray([]);

    self.selectedProjectId = ko.observable(null);
    self.selectedPeriodId = ko.observable(null);
    self.selectedPersonnelId = ko.observable(null);

    // State
    self.isLoading = ko.observable(false);
    self.isSummaryLoading = ko.observable(false);

    // Data
    self.personReport = ko.observable(null);
    self.periodSummary = ko.observable(null);

    // Feedback role names for competency table header
    self.feedbackRoleNames = ko.computed(function () {
        var report = self.personReport();
        if (!report || !report.roleScores || report.roleScores.length === 0) return [];
        return report.roleScores.map(function (rs) { return rs.roleName; });
    });

    // Load projects (only PersonnelAssessment type)
    self.loadProjects = function () {
        fetch('/api/projects')
            .then(function (res) { return res.json(); })
            .then(function (data) {
                var items = (data.items || data || []).filter(function (p) {
                    return p.checklistType === 'PersonnelAssessment' || p.projectTypeName === 'PersonnelAssessment';
                });
                self.projects(items.map(function (p) {
                    return { id: p.id, name: p.name || p.projectName };
                }));
            })
            .catch(function () {
                self.projects([]);
            });
    };

    // When project changes, load periods and personnel
    self.selectedProjectId.subscribe(function (projectId) {
        self.periods([]);
        self.personnel([]);
        self.selectedPeriodId(null);
        self.selectedPersonnelId(null);
        self.personReport(null);
        self.periodSummary(null);

        if (!projectId) return;

        // Load assignment + periods for this project
        fetch('/api/assignments?projectIds=' + projectId)
            .then(function (res) { return res.json(); })
            .then(function (data) {
                var assignments = data.items || data || [];
                if (assignments.length === 0) return;

                // Collect periods from all assignments
                var allPeriods = [];
                var allPersonnel = new Map();

                assignments.forEach(function (a) {
                    if (a.periods) {
                        a.periods.forEach(function (p) {
                            allPeriods.push({ id: p.id, name: p.name || p.periodName });
                        });
                    }
                });

                self.periods(allPeriods);

                // Load participants for personnel list
                if (allPeriods.length > 0) {
                    self.loadPersonnelForPeriod(allPeriods[0].id);
                }
            })
            .catch(function () {});
    });

    // When period changes, reload personnel
    self.selectedPeriodId.subscribe(function (periodId) {
        self.personReport(null);
        self.periodSummary(null);
        if (periodId) {
            self.loadPersonnelForPeriod(periodId);
        }
    });

    // Load personnel from assessment participants
    self.loadPersonnelForPeriod = function (periodId) {
        fetch('/api/assessment/participants/' + periodId)
            .then(function (res) { return res.json(); })
            .then(function (participants) {
                var personnelList = (participants || []).map(function (p) {
                    var name = p.customerPersonnel
                        ? ((p.customerPersonnel.firstName || '') + ' ' + (p.customerPersonnel.lastName || '')).trim()
                        : ('PersonelID: ' + p.customerPersonnelId);
                    return { id: p.customerPersonnelId, name: name };
                });
                // Deduplicate
                var seen = {};
                var unique = [];
                personnelList.forEach(function (p) {
                    if (!seen[p.id]) {
                        seen[p.id] = true;
                        unique.push(p);
                    }
                });
                self.personnel(unique.sort(function (a, b) { return a.name.localeCompare(b.name); }));
            })
            .catch(function () {
                self.personnel([]);
            });
    };

    // Load person report
    self.loadPersonReport = function () {
        var projectId = self.selectedProjectId();
        var personnelId = self.selectedPersonnelId();
        if (!projectId || !personnelId) return;

        self.isLoading(true);
        self.personReport(null);

        var url = '/api/assessment/reports/person/' + projectId + '/' + personnelId;
        var periodId = self.selectedPeriodId();
        if (periodId) url += '?assignmentPeriodId=' + periodId;

        fetch(url)
            .then(function (res) {
                if (!res.ok) throw new Error('Load failed');
                return res.json();
            })
            .then(function (data) {
                self.personReport(data);
            })
            .catch(function (error) {
                console.error('Error loading person report:', error);
                toastr.error(T('Assessment.Report.LoadError', 'Rapor yüklenirken hata oluştu.'));
            })
            .finally(function () {
                self.isLoading(false);
            });
    };

    // Load period summary
    self.loadPeriodSummary = function () {
        var projectId = self.selectedProjectId();
        var periodId = self.selectedPeriodId();
        if (!projectId || !periodId) return;

        if (self.periodSummary()) return; // Already loaded

        self.isSummaryLoading(true);

        fetch('/api/assessment/reports/period-summary/' + projectId + '/' + periodId)
            .then(function (res) {
                if (!res.ok) throw new Error('Load failed');
                return res.json();
            })
            .then(function (data) {
                self.periodSummary(data);
            })
            .catch(function (error) {
                console.error('Error loading period summary:', error);
                toastr.error(T('Assessment.Report.LoadError', 'Rapor yüklenirken hata oluştu.'));
            })
            .finally(function () {
                self.isSummaryLoading(false);
            });
    };

    // View person report from summary table
    self.viewPersonFromSummary = function (row) {
        self.selectedPersonnelId(row.customerPersonnelId);

        // Switch to person tab
        var personTab = document.getElementById('person-tab');
        if (personTab) {
            var tab = new bootstrap.Tab(personTab);
            tab.show();
        }

        self.loadPersonReport();
    };

    // Initialize
    self.init = function () {
        self.loadProjects();
    };

    self.init();
}

// Apply bindings with localization
$(document).ready(function () {
    Localization.loadKeys(TRANSLATION_KEYS).then(function () {
        var app = document.getElementById('assessment-report-app');
        if (app) {
            ko.applyBindings(new AssessmentReportViewModel(), app);
        }
    });
});
