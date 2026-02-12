// Support Requests Admin ViewModel
function SupportRequestsViewModel() {
    var self = this;

    // Observables
    self.isLoading = ko.observable(false);
    self.isSending = ko.observable(false);
    self.requests = ko.observableArray([]);
    self.totalCount = ko.observable(0);
    self.statusCounts = ko.observable({
        pending: 0,
        inProgress: 0,
        resolved: 0,
        closed: 0,
        total: 0
    });

    // Selected request for detail modal
    self.selectedRequest = ko.observable(null);
    self.responseText = ko.observable('');

    // Filters
    self.filter = {
        statusId: ko.observable(''),
        senderType: ko.observable(''),
        searchTerm: ko.observable(''),
        page: ko.observable(1),
        pageSize: ko.observable(20)
    };

    // Computed
    self.totalPages = ko.computed(function() {
        return Math.ceil(self.totalCount() / self.filter.pageSize()) || 1;
    });

    // Format date helper
    self.formatDate = function(dateString) {
        if (!dateString) return '';
        var date = new Date(dateString);
        return date.toLocaleDateString('tr-TR') + ' ' + date.toLocaleTimeString('tr-TR', { hour: '2-digit', minute: '2-digit' });
    };

    // Load status counts
    self.loadStatusCounts = function() {
        ApiService.get('/support-requests/counts')
            .then(function(data) {
                self.statusCounts(data);
            })
            .catch(function(error) {
                console.error('Error loading status counts:', error);
            });
    };

    // Load requests
    self.loadRequests = function() {
        self.isLoading(true);

        var params = new URLSearchParams();
        if (self.filter.statusId()) params.append('statusId', self.filter.statusId());
        if (self.filter.senderType()) params.append('senderType', self.filter.senderType());
        if (self.filter.searchTerm()) params.append('searchTerm', self.filter.searchTerm());
        params.append('page', self.filter.page());
        params.append('pageSize', self.filter.pageSize());

        ApiService.get('/support-requests?' + params.toString())
            .then(function(data) {
                self.requests(data.items.map(function(item) {
                    return {
                        id: item.id,
                        message: item.message,
                        pageUrl: item.pageUrl,
                        statusId: item.statusId,
                        statusName: item.statusName,
                        statusIcon: item.statusIcon,
                        statusCssClass: item.statusCssClass,
                        hasResponse: item.hasResponse,
                        createdAt: item.createdAt,
                        respondedAt: item.respondedAt,
                        senderName: item.senderName,
                        senderType: item.senderType,
                        userId: item.userId,
                        customerPersonnelId: item.customerPersonnelId
                    };
                }));
                self.totalCount(data.totalCount);
            })
            .catch(function(error) {
                console.error('Error loading support requests:', error);
                toastr.error(T('SupportRequest.LoadError', 'Destek talepleri yuklenirken hata olustu.'));
            })
            .finally(function() {
                self.isLoading(false);
            });
    };

    // Search
    self.search = function() {
        self.filter.page(1);
        self.loadRequests();
    };

    // Pagination
    self.prevPage = function() {
        if (self.filter.page() > 1) {
            self.filter.page(self.filter.page() - 1);
            self.loadRequests();
        }
    };

    self.nextPage = function() {
        if (self.filter.page() < self.totalPages()) {
            self.filter.page(self.filter.page() + 1);
            self.loadRequests();
        }
    };

    // View request detail
    self.viewRequest = function(request) {
        ApiService.get('/support-requests/' + request.id)
            .then(function(data) {
                self.selectedRequest({
                    id: data.id,
                    message: data.message,
                    pageUrl: data.pageUrl,
                    statusId: ko.observable(data.statusId),
                    statusName: data.statusName,
                    statusIcon: data.statusIcon,
                    statusCssClass: data.statusCssClass,
                    adminResponse: ko.observable(data.adminResponse),
                    respondedAt: data.respondedAt,
                    isReadByUser: data.isReadByUser,
                    createdAt: data.createdAt,
                    senderName: data.senderName,
                    senderEmail: data.senderEmail,
                    senderType: data.senderType,
                    userId: data.userId,
                    customerPersonnelId: data.customerPersonnelId,
                    respondedByUserName: data.respondedByUserName
                });
                self.responseText('');
                var modal = bootstrap.Modal.getOrCreateInstance(document.getElementById('requestDetailModal'));
                modal.show();
            })
            .catch(function(error) {
                console.error('Error loading request detail:', error);
                toastr.error(T('SupportRequest.LoadError', 'Talep detayi yuklenirken hata olustu.'));
            });
    };

    // Send response
    self.sendResponse = function() {
        if (!self.responseText() || !self.selectedRequest()) return;

        self.isSending(true);

        ApiService.post('/support-requests/' + self.selectedRequest().id + '/respond', {
            adminResponse: self.responseText()
        })
            .then(function(data) {
                toastr.success(T('SupportRequest.ResponseSent', 'Cevap basariyla gonderildi.'));
                self.selectedRequest().adminResponse(data.adminResponse);
                self.selectedRequest().statusId(data.statusId);
                self.loadRequests();
                self.loadStatusCounts();
            })
            .catch(function(error) {
                console.error('Error sending response:', error);
                toastr.error(T('SupportRequest.ResponseError', 'Cevap gonderilirken hata olustu.'));
            })
            .finally(function() {
                self.isSending(false);
            });
    };

    // Update status
    self.updateStatus = function(statusId) {
        if (!self.selectedRequest()) return;

        ApiService.put('/support-requests/' + self.selectedRequest().id + '/status', {
            statusId: statusId
        })
            .then(function(data) {
                toastr.success(T('SupportRequest.StatusUpdated', 'Durum guncellendi.'));
                self.selectedRequest().statusId(data.statusId);
                self.loadRequests();
                self.loadStatusCounts();
            })
            .catch(function(error) {
                console.error('Error updating status:', error);
                toastr.error(T('SupportRequest.StatusError', 'Durum guncellenirken hata olustu.'));
            });
    };

    // Refresh
    self.refresh = function() {
        self.loadRequests();
        self.loadStatusCounts();
    };

    // Initialize
    self.loadRequests();
    self.loadStatusCounts();

    // Check URL for specific request ID
    var urlParams = new URLSearchParams(window.location.search);
    var requestId = urlParams.get('id');
    if (requestId) {
        setTimeout(function() {
            self.viewRequest({ id: parseInt(requestId) });
        }, 500);
    }
}

// Translation keys
var TRANSLATION_KEYS = [
    'SupportRequest.LoadError',
    'SupportRequest.ResponseSent',
    'SupportRequest.ResponseError',
    'SupportRequest.StatusUpdated',
    'SupportRequest.StatusError'
];

// Initialize when DOM ready
$(document).ready(function() {
    Localization.loadKeys(TRANSLATION_KEYS).then(function() {
        ko.applyBindings(new SupportRequestsViewModel(), document.getElementById('support-requests-app'));
    });
});
