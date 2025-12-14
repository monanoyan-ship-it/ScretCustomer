// Banka Gizli Müşteri Ziyaretleri (GBF) ViewModel
function BankVisitsViewModel() {
    var self = this;

    // Observables
    self.visits = ko.observableArray([]);
    self.customers = ko.observableArray([]);
    self.branches = ko.observableArray([]);
    self.isLoading = ko.observable(false);
    self.isSaving = ko.observable(false);
    self.errorMessage = ko.observable('');
    self.successMessage = ko.observable('');
    self.totalCount = ko.observable(0);

    // Summary
    self.summary = {
        totalVisits: ko.observable(0),
        completedScenarios: ko.observable(0),
        avgSatisfaction: ko.observable('0'),
        avgWaitTime: ko.observable('0'),
        greetingRate: ko.observable('%0'),
        farewellRate: ko.observable('%0')
    };

    // Statistics
    self.statistics = ko.observable({
        totalVisits: 0,
        completedScenarios: 0,
        scenarioCompletionRate: 0,
        averageWaitTime: 0,
        averageServiceTime: 0,
        averageTotalTime: 0,
        averageStaffRating: 0,
        averageOverallSatisfaction: 0,
        averageRecommendationScore: 0,
        greetingRate: 0,
        farewellRate: 0
    });

    // Filter
    self.filter = {
        customerId: ko.observable(''),
        branchId: ko.observable(''),
        scenario: ko.observable(''),
        status: ko.observable(''),
        fromDate: ko.observable(''),
        toDate: ko.observable(''),
        minSatisfaction: ko.observable(''),
        maxSatisfaction: ko.observable('')
    };

    // Modal States
    self.isDetailModalOpen = ko.observable(false);
    self.isEditModalOpen = ko.observable(false);
    self.isStatsModalOpen = ko.observable(false);
    self.viewingVisit = ko.observable(null);
    self.editingVisit = ko.observable(null);

    // Initialize
    self.init = function() {
        self.loadCustomers();
        self.loadBranches();
        self.loadVisits();
        self.loadStatistics();

        // Filter subscriptions
        self.filter.customerId.subscribe(self.loadVisits);
        self.filter.branchId.subscribe(self.loadVisits);
        self.filter.scenario.subscribe(self.loadVisits);
        self.filter.status.subscribe(self.loadVisits);
        self.filter.fromDate.subscribe(self.loadVisits);
        self.filter.toDate.subscribe(self.loadVisits);
        self.filter.minSatisfaction.subscribe(self.loadVisits);
        self.filter.maxSatisfaction.subscribe(self.loadVisits);
    };

    // Load data
    self.loadCustomers = function() {
        $.get('/api/customers')
            .done(function(data) {
                self.customers(data);
            });
    };

    self.loadBranches = function() {
        $.get('/api/branches')
            .done(function(data) {
                self.branches(data);
            });
    };

    self.loadVisits = function() {
        self.isLoading(true);
        self.errorMessage('');

        var params = {};
        if (self.filter.customerId()) params.customerId = self.filter.customerId();
        if (self.filter.branchId()) params.branchId = self.filter.branchId();
        if (self.filter.scenario()) params.scenario = self.filter.scenario();
        if (self.filter.status()) params.status = self.filter.status();
        if (self.filter.fromDate()) params.fromDate = self.filter.fromDate();
        if (self.filter.toDate()) params.toDate = self.filter.toDate();
        if (self.filter.minSatisfaction()) params.minSatisfactionRating = self.filter.minSatisfaction();
        if (self.filter.maxSatisfaction()) params.maxSatisfactionRating = self.filter.maxSatisfaction();

        $.get('/api/bank-visits', params)
            .done(function(data) {
                self.visits(data);
                self.totalCount(data.length);
                self.updateSummary(data);
            })
            .fail(function(xhr) {
                self.errorMessage(T('BankVisit.LoadError', 'Veriler yüklenirken hata oluştu.'));
            })
            .always(function() {
                self.isLoading(false);
            });
    };

    self.loadStatistics = function() {
        $.get('/api/bank-visits/statistics')
            .done(function(data) {
                self.statistics(data);
            });
    };

    self.updateSummary = function(data) {
        self.summary.totalVisits(data.length);
        self.summary.completedScenarios(data.filter(v => v.scenarioCompleted).length);

        var satisfactionRatings = data.filter(v => v.overallSatisfactionRating).map(v => v.overallSatisfactionRating);
        var avgSat = satisfactionRatings.length > 0 ? (satisfactionRatings.reduce((a, b) => a + b, 0) / satisfactionRatings.length).toFixed(1) : '0';
        self.summary.avgSatisfaction(avgSat);

        var waitTimes = data.filter(v => v.queueWaitMinutes).map(v => v.queueWaitMinutes);
        var avgWait = waitTimes.length > 0 ? Math.round(waitTimes.reduce((a, b) => a + b, 0) / waitTimes.length) : 0;
        self.summary.avgWaitTime(avgWait);
    };

    // Clear filters
    self.clearFilters = function() {
        self.filter.customerId('');
        self.filter.branchId('');
        self.filter.scenario('');
        self.filter.status('');
        self.filter.fromDate('');
        self.filter.toDate('');
        self.filter.minSatisfaction('');
        self.filter.maxSatisfaction('');
    };

    // Show detail modal
    self.showDetail = function(visit) {
        $.get('/api/bank-visits/' + visit.id)
            .done(function(data) {
                self.viewingVisit(data);
                self.isDetailModalOpen(true);
            })
            .fail(function() {
                self.errorMessage(T('BankVisit.DetailLoadError', 'Detay yüklenirken hata oluştu.'));
            });
    };

    self.closeDetailModal = function() {
        self.isDetailModalOpen(false);
        self.viewingVisit(null);
    };

    // Edit visit
    self.editVisit = function(visit) {
        $.get('/api/bank-visits/' + visit.id)
            .done(function(data) {
                // Convert to observables
                var editableVisit = {
                    id: ko.observable(data.id),
                    customerVisitId: ko.observable(data.customerVisitId),
                    scenario: ko.observable(data.scenario),
                    scenarioDescription: ko.observable(data.scenarioDescription),
                    scenarioCompleted: ko.observable(data.scenarioCompleted),
                    productOffered: ko.observable(data.productOffered),
                    crossSellOffered: ko.observable(data.crossSellOffered),
                    entryTime: ko.observable(self.formatDateTimeLocal(data.entryTime)),
                    exitTime: ko.observable(self.formatDateTimeLocal(data.exitTime)),
                    queueWaitMinutes: ko.observable(data.queueWaitMinutes),
                    serviceDurationMinutes: ko.observable(data.serviceDurationMinutes),
                    queueTicketTaken: ko.observable(data.queueTicketTaken),
                    queueNumber: ko.observable(data.queueNumber),
                    staffName: ko.observable(data.staffName),
                    staffHasNameTag: ko.observable(data.staffHasNameTag),
                    greetingReceived: ko.observable(data.greetingReceived),
                    farewellReceived: ko.observable(data.farewellReceived),
                    staffAppearanceRating: ko.observable(data.staffAppearanceRating),
                    staffKnowledgeRating: ko.observable(data.staffKnowledgeRating),
                    staffAttentivenessRating: ko.observable(data.staffAttentivenessRating),
                    staffCommunicationRating: ko.observable(data.staffCommunicationRating),
                    staffCountObserved: ko.observable(data.staffCountObserved),
                    busyCountersCount: ko.observable(data.busyCountersCount),
                    totalCountersCount: ko.observable(data.totalCountersCount),
                    entranceAreaRating: ko.observable(data.entranceAreaRating),
                    atmAreaRating: ko.observable(data.atmAreaRating),
                    waitingAreaRating: ko.observable(data.waitingAreaRating),
                    counterAreaRating: ko.observable(data.counterAreaRating),
                    managerAreaRating: ko.observable(data.managerAreaRating),
                    cleanlinessRating: ko.observable(data.cleanlinessRating),
                    lightingRating: ko.observable(data.lightingRating),
                    airConditioningRating: ko.observable(data.airConditioningRating),
                    signageRating: ko.observable(data.signageRating),
                    brochuresAvailable: ko.observable(data.brochuresAvailable),
                    queueSystemAvailable: ko.observable(data.queueSystemAvailable),
                    disabledAccessAvailable: ko.observable(data.disabledAccessAvailable),
                    securityPersonnelPresent: ko.observable(data.securityPersonnelPresent),
                    atmCount: ko.observable(data.atmCount),
                    workingAtmCount: ko.observable(data.workingAtmCount),
                    atmCleanlinessRating: ko.observable(data.atmCleanlinessRating),
                    atmUsabilityRating: ko.observable(data.atmUsabilityRating),
                    overallSatisfactionRating: ko.observable(data.overallSatisfactionRating),
                    recommendationScore: ko.observable(data.recommendationScore),
                    wouldVisitAgain: ko.observable(data.wouldVisitAgain),
                    strengths: ko.observable(data.strengths),
                    improvementAreas: ko.observable(data.improvementAreas),
                    additionalNotes: ko.observable(data.additionalNotes)
                };
                self.editingVisit(editableVisit);
                self.isEditModalOpen(true);
            })
            .fail(function() {
                self.errorMessage(T('BankVisit.EditLoadError', 'Düzenleme için veri yüklenirken hata oluştu.'));
            });
    };

    self.closeEditModal = function() {
        self.isEditModalOpen(false);
        self.editingVisit(null);
    };

    self.saveVisit = function() {
        var visit = self.editingVisit();
        if (!visit) return;

        self.isSaving(true);

        var data = {
            customerVisitId: visit.customerVisitId(),
            scenario: parseInt(visit.scenario()),
            scenarioDescription: visit.scenarioDescription(),
            scenarioCompleted: visit.scenarioCompleted(),
            productOffered: visit.productOffered(),
            crossSellOffered: visit.crossSellOffered(),
            entryTime: visit.entryTime() ? new Date(visit.entryTime()).toISOString() : null,
            exitTime: visit.exitTime() ? new Date(visit.exitTime()).toISOString() : null,
            queueWaitMinutes: visit.queueWaitMinutes() ? parseInt(visit.queueWaitMinutes()) : null,
            serviceDurationMinutes: visit.serviceDurationMinutes() ? parseInt(visit.serviceDurationMinutes()) : null,
            queueTicketTaken: visit.queueTicketTaken(),
            queueNumber: visit.queueNumber(),
            staffName: visit.staffName(),
            staffHasNameTag: visit.staffHasNameTag(),
            greetingReceived: visit.greetingReceived(),
            farewellReceived: visit.farewellReceived(),
            staffAppearanceRating: visit.staffAppearanceRating() ? parseInt(visit.staffAppearanceRating()) : null,
            staffKnowledgeRating: visit.staffKnowledgeRating() ? parseInt(visit.staffKnowledgeRating()) : null,
            staffAttentivenessRating: visit.staffAttentivenessRating() ? parseInt(visit.staffAttentivenessRating()) : null,
            staffCommunicationRating: visit.staffCommunicationRating() ? parseInt(visit.staffCommunicationRating()) : null,
            staffCountObserved: visit.staffCountObserved() ? parseInt(visit.staffCountObserved()) : null,
            busyCountersCount: visit.busyCountersCount() ? parseInt(visit.busyCountersCount()) : null,
            totalCountersCount: visit.totalCountersCount() ? parseInt(visit.totalCountersCount()) : null,
            entranceAreaRating: visit.entranceAreaRating() ? parseInt(visit.entranceAreaRating()) : null,
            atmAreaRating: visit.atmAreaRating() ? parseInt(visit.atmAreaRating()) : null,
            waitingAreaRating: visit.waitingAreaRating() ? parseInt(visit.waitingAreaRating()) : null,
            counterAreaRating: visit.counterAreaRating() ? parseInt(visit.counterAreaRating()) : null,
            managerAreaRating: visit.managerAreaRating() ? parseInt(visit.managerAreaRating()) : null,
            cleanlinessRating: visit.cleanlinessRating() ? parseInt(visit.cleanlinessRating()) : null,
            lightingRating: visit.lightingRating() ? parseInt(visit.lightingRating()) : null,
            airConditioningRating: visit.airConditioningRating() ? parseInt(visit.airConditioningRating()) : null,
            signageRating: visit.signageRating() ? parseInt(visit.signageRating()) : null,
            brochuresAvailable: visit.brochuresAvailable(),
            queueSystemAvailable: visit.queueSystemAvailable(),
            disabledAccessAvailable: visit.disabledAccessAvailable(),
            securityPersonnelPresent: visit.securityPersonnelPresent(),
            atmCount: visit.atmCount() ? parseInt(visit.atmCount()) : null,
            workingAtmCount: visit.workingAtmCount() ? parseInt(visit.workingAtmCount()) : null,
            atmCleanlinessRating: visit.atmCleanlinessRating() ? parseInt(visit.atmCleanlinessRating()) : null,
            atmUsabilityRating: visit.atmUsabilityRating() ? parseInt(visit.atmUsabilityRating()) : null,
            overallSatisfactionRating: visit.overallSatisfactionRating() ? parseInt(visit.overallSatisfactionRating()) : null,
            recommendationScore: visit.recommendationScore() ? parseInt(visit.recommendationScore()) : null,
            wouldVisitAgain: visit.wouldVisitAgain(),
            strengths: visit.strengths(),
            improvementAreas: visit.improvementAreas(),
            additionalNotes: visit.additionalNotes()
        };

        $.ajax({
            url: '/api/bank-visits/' + visit.id(),
            method: 'PUT',
            contentType: 'application/json',
            data: JSON.stringify(data)
        })
        .done(function() {
            self.successMessage(T('BankVisit.SaveSuccess', 'Ziyaret kaydedildi.'));
            self.closeEditModal();
            self.loadVisits();
            self.loadStatistics();
            setTimeout(function() { self.successMessage(''); }, 3000);
        })
        .fail(function(xhr) {
            var msg = xhr.responseJSON?.message || 'Kaydetme işlemi başarısız.';
            self.errorMessage(msg);
        })
        .always(function() {
            self.isSaving(false);
        });
    };

    // Delete visit
    self.deleteVisit = function(visit) {
        showDeleteConfirm(T('BankVisit.ThisRecord', 'Bu banka ziyareti kaydı'), function() {
            $.ajax({
                url: '/api/bank-visits/' + visit.id,
                method: 'DELETE'
            })
            .done(function() {
                toastr.success(T('BankVisit.DeleteSuccess', 'Ziyaret silindi.'));
                self.loadVisits();
                self.loadStatistics();
            })
            .fail(function(xhr) {
                var msg = xhr.responseJSON?.message || 'Silme işlemi başarısız.';
                toastr.error(msg);
            });
        });
    };

    // Statistics Modal
    self.showStatistics = function() {
        self.loadStatistics();
        self.isStatsModalOpen(true);
    };

    self.closeStatsModal = function() {
        self.isStatsModalOpen(false);
    };

    // Export Excel
    self.exportExcel = function() {
        var params = new URLSearchParams();
        if (self.filter.customerId()) params.append('customerId', self.filter.customerId());
        if (self.filter.branchId()) params.append('branchId', self.filter.branchId());
        if (self.filter.fromDate()) params.append('fromDate', self.filter.fromDate());
        if (self.filter.toDate()) params.append('toDate', self.filter.toDate());

        window.location.href = '/api/bank-visits/export?' + params.toString();
    };

    // Helper functions
    self.formatDateTimeLocal = function(dateStr) {
        if (!dateStr) return null;
        var date = new Date(dateStr);
        var pad = function(n) { return n < 10 ? '0' + n : n; };
        return date.getFullYear() + '-' + pad(date.getMonth() + 1) + '-' + pad(date.getDate()) +
            'T' + pad(date.getHours()) + ':' + pad(date.getMinutes());
    };

    self.getSatisfactionBadgeClass = function(rating) {
        if (!rating) return 'bg-secondary';
        if (rating >= 8) return 'bg-success';
        if (rating >= 6) return 'bg-info';
        if (rating >= 4) return 'bg-warning';
        return 'bg-danger';
    };

    // Initialize
    self.init();
}

// Apply bindings when document is ready
$(document).ready(function() {
    var vm = new BankVisitsViewModel();
    ko.applyBindings(vm, document.getElementById('bank-visits-app'));
});
