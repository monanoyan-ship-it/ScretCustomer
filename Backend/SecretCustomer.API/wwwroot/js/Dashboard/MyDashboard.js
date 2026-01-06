// Müşteri Personeli Dashboard ViewModel
function MyDashboardViewModel() {
    var self = this;

    // State
    self.isLoading = ko.observable(false);

    // Filters
    self.selectedYear = ko.observable(new Date().getFullYear());
    self.selectedMonth = ko.observable(new Date().getMonth() + 1);
    self.availableYears = ko.observableArray([]);
    self.searchText = ko.observable('');

    // Data
    self.allEvaluations = ko.observableArray([]);
    self.summary = ko.observable({ totalCount: 0, averageScore: 0 });
    self.showPersonnelName = ko.observable(false);
    self.isManager = ko.observable(false);
    self.personnelSummary = ko.observableArray([]);
    self.supervisorSummary = ko.observableArray([]);

    // Filter dropdowns (Manager için)
    self.organizations = ko.observableArray([]);
    self.supervisors = ko.observableArray([]);
    self.selectedOrganization = ko.observable('');
    self.selectedSupervisor = ko.observable('');

    // Filtrelenmiş değerlendirmeler
    self.evaluations = ko.computed(function() {
        var list = self.allEvaluations();
        var search = (self.searchText() || '').toLowerCase().trim();
        var supervisor = self.selectedSupervisor();
        var organization = self.selectedOrganization();

        // Organizasyon filtresi (Manager için)
        if (organization && self.isManager()) {
            list = list.filter(function(e) {
                return e.organizationName === organization;
            });
        }

        // Supervisor filtresi (Manager için)
        if (supervisor && self.isManager()) {
            list = list.filter(function(e) {
                return e.supervisorName === supervisor;
            });
        }

        // Personel arama filtresi
        if (search && self.showPersonnelName()) {
            list = list.filter(function(e) {
                return (e.personnelName || '').toLowerCase().indexOf(search) >= 0;
            });
        }

        return list;
    });

    // Filtrelenmiş personel özeti (organization ve supervisor seçimine göre)
    self.filteredPersonnelSummary = ko.computed(function() {
        var list = self.personnelSummary();
        var supervisor = self.selectedSupervisor();
        var organization = self.selectedOrganization();

        if (organization && self.isManager()) {
            list = list.filter(function(p) {
                return p.organizationName === organization;
            });
        }

        if (supervisor && self.isManager()) {
            list = list.filter(function(p) {
                return p.supervisorName === supervisor;
            });
        }

        return list;
    });

    // Yıl listesini oluştur (son 3 yıl)
    var currentYear = new Date().getFullYear();
    for (var y = currentYear; y >= currentYear - 2; y--) {
        self.availableYears.push(y);
    }

    // Yıl veya ay değiştiğinde değerlendirmeleri yeniden yükle
    self.selectedYear.subscribe(function() {
        self.loadEvaluations();
    });
    self.selectedMonth.subscribe(function() {
        self.loadEvaluations();
    });

    // Değerlendirmeleri yükle
    self.loadEvaluations = function() {
        self.isLoading(true);

        var url = '/api/dashboard/my-evaluations?year=' + self.selectedYear() + '&month=' + self.selectedMonth();

        fetch(url, { credentials: 'include' })
            .then(function(response) {
                if (!response.ok) throw new Error('Değerlendirmeler yüklenemedi');
                return response.json();
            })
            .then(function(data) {
                self.allEvaluations(data.evaluations || []);
                self.showPersonnelName(data.showPersonnelName || false);
                self.isManager(data.isManager || false);
                self.personnelSummary(data.personnelSummary || []);
                self.supervisorSummary(data.supervisorSummary || []);
                self.organizations(data.organizations || []);
                self.supervisors(data.supervisors || []);
                self.summary({
                    totalCount: data.totalCount || 0,
                    averageScore: data.averageScore || 0
                });
            })
            .catch(function(error) {
                console.error('My evaluations error:', error);
                self.allEvaluations([]);
                self.showPersonnelName(false);
                self.isManager(false);
                self.personnelSummary([]);
                self.supervisorSummary([]);
                self.organizations([]);
                self.supervisors([]);
                self.summary({ totalCount: 0, averageScore: 0 });
            })
            .finally(function() {
                self.isLoading(false);
            });
    };

    // Tarih formatla
    self.formatDateTime = function(dateStr) {
        if (!dateStr) return '';
        var date = new Date(dateStr);
        return date.toLocaleDateString('tr-TR') + ' ' + date.toLocaleTimeString('tr-TR', { hour: '2-digit', minute: '2-digit' });
    };

    // Puan badge class
    self.getScoreBadgeClass = function(score) {
        if (score >= 80) return 'bg-success';
        if (score >= 60) return 'bg-warning text-dark';
        return 'bg-danger';
    };

    // Personel arama - tıklanınca o personeli filtrele
    self.filterByPersonnel = function(personnel) {
        self.searchText(personnel.personnelName);
    };

    // Supervisor filtresi - tıklanınca o supervisor'u filtrele
    self.filterBySupervisor = function(supervisor) {
        self.selectedSupervisor(supervisor.supervisorName);
        self.searchText('');
    };

    // Filtreyi temizle
    self.clearFilter = function() {
        self.searchText('');
        self.selectedOrganization('');
        self.selectedSupervisor('');
    };

    // Aktif filtre var mı?
    self.hasActiveFilter = ko.computed(function() {
        return self.searchText() || self.selectedOrganization() || self.selectedSupervisor();
    });

    // Initialize
    self.loadEvaluations();
}

// Apply bindings when document is ready
$(document).ready(function() {
    ko.applyBindings(new MyDashboardViewModel(), document.getElementById('my-dashboard-app'));
});
