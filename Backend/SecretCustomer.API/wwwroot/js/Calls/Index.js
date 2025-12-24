(function() {
    function CallsViewModel() {
        var self = this;

        // ==================== STATE ====================
        self.isLoading = ko.observable(false);
        self.isSaving = ko.observable(false);
        self.errorMessage = ko.observable('');
        self.successMessage = ko.observable('');
        self.modalErrorMessage = ko.observable('');

        // ==================== DATA ====================
        self.calls = ko.observableArray([]);
        self.projects = ko.observableArray([]);
        self.customers = ko.observableArray([]);
        self.branches = ko.observableArray([]);
        self.users = ko.observableArray([]);
        self.summary = ko.observable({
            totalCalls: 0,
            todayCalls: 0,
            pendingCalls: 0,
            completedCalls: 0,
            missedCalls: 0,
            callbackRequired: 0
        });

        // ==================== PAGINATION ====================
        self.currentPage = ko.observable(1);
        self.pageSize = ko.observable(20);
        self.totalCount = ko.observable(0);
        self.totalPages = ko.computed(function() {
            return Math.ceil(self.totalCount() / self.pageSize());
        });

        self.visiblePages = ko.computed(function() {
            var pages = [];
            var total = self.totalPages();
            var current = self.currentPage();
            var start = Math.max(1, current - 2);
            var end = Math.min(total, current + 2);
            for (var i = start; i <= end; i++) {
                pages.push(i);
            }
            return pages;
        });

        // ==================== FILTERS ====================
        self.filter = {
            projectId: ko.observable(''),
            customerId: ko.observable(''),
            callType: ko.observable(''),
            status: ko.observable(''),
            priority: ko.observable(''),
            startDate: ko.observable(''),
            endDate: ko.observable(''),
            searchTerm: ko.observable(''),
            requiresCallback: ko.observable(false),
            hasRecording: ko.observable(false)
        };

        // Subscribe to filter changes
        Object.keys(self.filter).forEach(function(key) {
            self.filter[key].subscribe(function() {
                self.currentPage(1);
                self.loadCalls();
            });
        });

        // ==================== MODAL STATE ====================
        self.isFormModalOpen = ko.observable(false);
        self.isDetailModalOpen = ko.observable(false);
        self.isCompleteModalOpen = ko.observable(false);

        self.editingCall = ko.observable(null);
        self.viewingCall = ko.observable(null);
        self.completingCallId = ko.observable(null);

        self.completeData = {
            notes: ko.observable(''),
            outcome: ko.observable(''),
            requiresCallback: ko.observable(false),
            callbackDate: ko.observable(''),
            callbackNote: ko.observable('')
        };

        // ==================== BADGE CLASSES ====================
        self.getCallTypeBadgeClass = function(type) {
            switch(type) {
                case 'Inbound': return 'bg-primary';
                case 'Outbound': return 'bg-info';
                case 'Internal': return 'bg-secondary';
                case 'Conference': return 'bg-warning text-dark';
                case 'MysteryCall': return 'bg-dark';
                default: return 'bg-secondary';
            }
        };

        self.getStatusBadgeClass = function(status) {
            switch(status) {
                case 'Scheduled': return 'bg-info';
                case 'Pending': return 'bg-warning text-dark';
                case 'InProgress': return 'bg-primary';
                case 'Completed': return 'bg-success';
                case 'Missed': return 'bg-danger';
                case 'Cancelled': return 'bg-secondary';
                case 'Failed': return 'bg-dark';
                case 'Evaluated': return 'bg-success';
                default: return 'bg-secondary';
            }
        };

        self.getPriorityBadgeClass = function(priority) {
            switch(priority) {
                case 'Low': return 'bg-secondary';
                case 'Normal': return 'bg-info';
                case 'High': return 'bg-warning text-dark';
                case 'Urgent': return 'bg-danger';
                default: return 'bg-secondary';
            }
        };

        // ==================== DATA LOADING ====================
        self.loadCalls = function() {
            self.isLoading(true);
            var params = new URLSearchParams({
                page: self.currentPage(),
                pageSize: self.pageSize()
            });

            if (self.filter.projectId()) params.append('projectId', self.filter.projectId());
            if (self.filter.customerId()) params.append('customerId', self.filter.customerId());
            if (self.filter.callType()) params.append('callType', self.filter.callType());
            if (self.filter.status()) params.append('status', self.filter.status());
            if (self.filter.priority()) params.append('priority', self.filter.priority());
            if (self.filter.startDate()) params.append('startDate', self.filter.startDate());
            if (self.filter.endDate()) params.append('endDate', self.filter.endDate());
            if (self.filter.searchTerm()) params.append('searchTerm', self.filter.searchTerm());
            if (self.filter.requiresCallback()) params.append('requiresCallback', 'true');
            if (self.filter.hasRecording()) params.append('hasRecording', 'true');

            fetch('/api/calls?' + params.toString())
                .then(function(response) { return response.json(); })
                .then(function(data) {
                    self.calls(data.items || []);
                    self.totalCount(data.totalCount || 0);
                })
                .catch(function(error) {
                    self.errorMessage('Veriler yüklenirken hata oluştu.');
                })
                .finally(function() {
                    self.isLoading(false);
                });
        };

        self.loadSummary = function() {
            fetch('/api/calls/summary')
                .then(function(response) { return response.json(); })
                .then(function(data) {
                    self.summary(data);
                });
        };

        self.loadProjects = function() {
            fetch('/api/projects')
                .then(function(response) { return response.json(); })
                .then(function(data) {
                    self.projects(data.items || data || []);
                });
        };

        self.loadCustomers = function() {
            fetch('/api/customers')
                .then(function(response) { return response.json(); })
                .then(function(data) {
                    self.customers(data.items || data || []);
                });
        };

        self.loadBranches = function() {
            fetch('/api/branches')
                .then(function(response) { return response.json(); })
                .then(function(data) {
                    self.branches(data.items || data || []);
                });
        };

        self.loadUsers = function() {
            fetch('/api/users')
                .then(function(response) { return response.json(); })
                .then(function(data) {
                    self.users(data.items || data || []);
                });
        };

        // ==================== CREATE/EDIT MODAL ====================
        self.createNew = function() {
            self.modalErrorMessage('');
            self.editingCall({
                id: ko.observable(null),
                callType: ko.observable('Inbound'),
                status: ko.observable('Pending'),
                priority: ko.observable('Normal'),
                subject: ko.observable(''),
                description: ko.observable(''),
                callerPhone: ko.observable(''),
                callerName: ko.observable(''),
                calledPhone: ko.observable(''),
                scheduledDate: ko.observable(''),
                notes: ko.observable(''),
                outcome: ko.observable(''),
                projectId: ko.observable(''),
                customerId: ko.observable(''),
                branchId: ko.observable(''),
                assignedUserId: ko.observable(''),
                tags: ko.observable(''),
                requiresCallback: ko.observable(false),
                callbackDate: ko.observable(''),
                callbackNote: ko.observable('')
            });
            self.isFormModalOpen(true);
        };

        self.editCall = function(call) {
            // Load full call data for editing
            self.modalErrorMessage('');
            fetch('/api/calls/' + call.id)
                .then(function(response) { return response.json(); })
                .then(function(data) {
                    self.editingCall({
                        id: ko.observable(data.id),
                        callType: ko.observable(data.callType),
                        status: ko.observable(data.status),
                        priority: ko.observable(data.priority),
                        subject: ko.observable(data.subject || ''),
                        description: ko.observable(data.description || ''),
                        callerPhone: ko.observable(data.callerPhone || ''),
                        callerName: ko.observable(data.callerName || ''),
                        calledPhone: ko.observable(data.calledPhone || ''),
                        scheduledDate: ko.observable(data.scheduledDate ? data.scheduledDate.substring(0, 16) : ''),
                        notes: ko.observable(data.notes || ''),
                        outcome: ko.observable(data.outcome || ''),
                        projectId: ko.observable(data.projectId || ''),
                        customerId: ko.observable(data.customerId || ''),
                        branchId: ko.observable(data.branchId || ''),
                        assignedUserId: ko.observable(data.assignedUserId || ''),
                        tags: ko.observable(data.tags || ''),
                        requiresCallback: ko.observable(data.requiresCallback || false),
                        callbackDate: ko.observable(data.callbackDate ? data.callbackDate.substring(0, 16) : ''),
                        callbackNote: ko.observable(data.callbackNote || '')
                    });
                    self.isFormModalOpen(true);
                });
        };

        self.closeFormModal = function() {
            self.isFormModalOpen(false);
            self.editingCall(null);
        };

        self.saveCall = function() {
            var call = self.editingCall();
            var isNew = !call.id();
            var url = isNew ? '/api/calls' : '/api/calls/' + call.id();
            var method = isNew ? 'POST' : 'PUT';

            var data = {
                callType: call.callType(),
                priority: call.priority(),
                subject: call.subject(),
                description: call.description(),
                callerPhone: call.callerPhone(),
                callerName: call.callerName(),
                calledPhone: call.calledPhone(),
                scheduledDate: call.scheduledDate() || null,
                projectId: call.projectId() || null,
                customerId: call.customerId() || null,
                branchId: call.branchId() || null,
                assignedUserId: call.assignedUserId() || null,
                tags: call.tags()
            };

            // Add edit-only fields
            if (!isNew) {
                data.status = call.status();
                data.notes = call.notes();
                data.outcome = call.outcome();
                data.requiresCallback = call.requiresCallback();
                data.callbackDate = call.callbackDate() || null;
                data.callbackNote = call.callbackNote();
            }

            self.isSaving(true);
            fetch(url, {
                method: method,
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(data)
            })
            .then(function(response) {
                if (response.ok) {
                    self.closeFormModal();
                    self.loadCalls();
                    self.loadSummary();
                    self.successMessage(isNew ? 'Çağrı başarıyla oluşturuldu.' : 'Çağrı başarıyla güncellendi.');
                    setTimeout(function() { self.successMessage(''); }, 3000);
                } else {
                    return response.json().then(function(err) {
                        self.modalErrorMessage(err.message || 'Bir hata oluştu.');
                    });
                }
            })
            .catch(function(error) {
                self.modalErrorMessage('İşlem sırasında hata oluştu.');
            })
            .finally(function() {
                self.isSaving(false);
            });
        };

        // ==================== DETAIL MODAL ====================
        self.showDetail = function(call) {
            fetch('/api/calls/' + call.id)
                .then(function(response) { return response.json(); })
                .then(function(data) {
                    self.viewingCall(data);
                    self.isDetailModalOpen(true);
                });
        };

        self.closeDetailModal = function() {
            self.isDetailModalOpen(false);
            self.viewingCall(null);
        };

        // ==================== COMPLETE MODAL ====================
        self.showCompleteModal = function(call) {
            self.completingCallId(call.id);
            self.completeData.notes('');
            self.completeData.outcome('');
            self.completeData.requiresCallback(false);
            self.completeData.callbackDate('');
            self.completeData.callbackNote('');
            self.isCompleteModalOpen(true);
        };

        self.closeCompleteModal = function() {
            self.isCompleteModalOpen(false);
            self.completingCallId(null);
        };

        self.completeCall = function() {
            var data = {
                notes: self.completeData.notes(),
                outcome: self.completeData.outcome(),
                requiresCallback: self.completeData.requiresCallback(),
                callbackDate: self.completeData.callbackDate() || null,
                callbackNote: self.completeData.callbackNote()
            };

            fetch('/api/calls/' + self.completingCallId() + '/complete', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(data)
            })
            .then(function(response) {
                if (response.ok) {
                    self.closeCompleteModal();
                    self.closeDetailModal();
                    self.loadCalls();
                    self.loadSummary();
                    self.successMessage('Çağrı tamamlandı.');
                    setTimeout(function() { self.successMessage(''); }, 3000);
                } else {
                    self.errorMessage('İşlem başarısız.');
                }
            });
        };

        // ==================== CALL ACTIONS ====================
        self.startCall = function(call) {
            fetch('/api/calls/' + call.id + '/start', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({})
            })
            .then(function(response) {
                if (response.ok) {
                    self.loadCalls();
                    self.loadSummary();
                    self.successMessage('Çağrı başlatıldı.');
                    setTimeout(function() { self.successMessage(''); }, 3000);
                } else {
                    self.errorMessage('İşlem başarısız.');
                }
            });
        };

        self.completeCallback = function(call) {
            fetch('/api/calls/' + call.id + '/callback-complete', {
                method: 'POST'
            })
            .then(function(response) {
                if (response.ok) {
                    // Reload detail if open
                    if (self.viewingCall() && self.viewingCall().id === call.id) {
                        self.showDetail(call);
                    }
                    self.loadCalls();
                    self.loadSummary();
                    self.successMessage('Geri arama tamamlandı.');
                    setTimeout(function() { self.successMessage(''); }, 3000);
                } else {
                    self.errorMessage('İşlem başarısız.');
                }
            });
        };

        self.deleteCall = function(call) {
            showDeleteConfirm('Bu çağrı', function() {
                fetch('/api/calls/' + call.id, { method: 'DELETE' })
                    .then(function(response) {
                        if (response.ok) {
                            self.loadCalls();
                            self.loadSummary();
                            toastr.success('Çağrı silindi.');
                        } else {
                            toastr.error('Silme işlemi başarısız.');
                        }
                    });
            });
        };

        // ==================== PAGINATION ====================
        self.previousPage = function() {
            if (self.currentPage() > 1) {
                self.currentPage(self.currentPage() - 1);
                self.loadCalls();
            }
        };

        self.nextPage = function() {
            if (self.currentPage() < self.totalPages()) {
                self.currentPage(self.currentPage() + 1);
                self.loadCalls();
            }
        };

        self.goToPage = function(page) {
            self.currentPage(page);
            self.loadCalls();
        };

        // ==================== FILTERS ====================
        self.clearFilters = function() {
            self.filter.projectId('');
            self.filter.customerId('');
            self.filter.callType('');
            self.filter.status('');
            self.filter.priority('');
            self.filter.startDate('');
            self.filter.endDate('');
            self.filter.searchTerm('');
            self.filter.requiresCallback(false);
            self.filter.hasRecording(false);
        };

        // ==================== INITIALIZE ====================
        self.init = function() {
            self.loadCalls();
            self.loadSummary();
            self.loadProjects();
            self.loadCustomers();
            self.loadBranches();
            self.loadUsers();
        };

        self.init();
    }

    // CORRECT: Bind to specific element
    ko.applyBindings(new CallsViewModel(), document.getElementById('calls-app'));
})();
