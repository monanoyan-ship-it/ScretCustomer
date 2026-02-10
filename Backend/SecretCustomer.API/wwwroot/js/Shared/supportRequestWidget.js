// Support Request Widget ViewModel
function SupportRequestWidgetViewModel() {
    var self = this;

    // Modals
    self.modal = null;
    self.detailModal = null;

    // Observables
    self.activeTab = ko.observable('new');
    self.newMessage = ko.observable('');
    self.pageUrl = ko.observable(window.location.href);
    self.isSending = ko.observable(false);
    self.isLoadingRequests = ko.observable(false);
    self.myRequests = ko.observableArray([]);
    self.unreadCount = ko.observable(0);
    self.selectedRequest = ko.observable(null);

    // Translation helper
    self.T = function(key, defaultValue) {
        return typeof T === 'function' ? T(key, defaultValue || key) : (defaultValue || key);
    };

    // Format date helper
    self.formatDate = function(dateString) {
        if (!dateString) return '';
        var date = new Date(dateString);
        return date.toLocaleDateString('tr-TR') + ' ' + date.toLocaleTimeString('tr-TR', { hour: '2-digit', minute: '2-digit' });
    };

    // Get status text
    self.getStatusText = function(statusId) {
        switch (statusId) {
            case 1: return self.T('Project.Pending', 'Beklemede');
            case 2: return self.T('SupportRequestStatus.InProgress', 'İnceleniyor');
            case 3: return self.T('SupportRequestStatus.Resolved', 'Cevaplandı');
            case 4: return self.T('SupportRequestStatus.Closed', 'Kapatıldı');
            default: return 'Bilinmiyor';
        }
    };

    // Open modal
    self.openModal = function() {
        self.pageUrl(window.location.href);
        self.activeTab('new');

        if (!self.modal) {
            self.modal = new bootstrap.Modal(document.getElementById('supportRequestModal'));
        }
        self.modal.show();

        // Load my requests
        self.loadMyRequests();
    };

    // Load my requests
    self.loadMyRequests = function() {
        self.isLoadingRequests(true);

        ApiService.get('/support-requests/my')
            .then(function(data) {
                var unread = 0;
                var mappedRequests = data.map(function(item) {
                    var hasUnreadResponse = item.hasResponse && !item.isReadByUser;
                    if (hasUnreadResponse) unread++;

                    return {
                        id: item.id,
                        message: item.message,
                        statusId: item.statusId,
                        statusText: self.getStatusText(item.statusId),
                        statusCssClass: item.statusCssClass,
                        hasResponse: item.hasResponse,
                        isReadByUser: item.isReadByUser || false,
                        createdAt: item.createdAt
                    };
                });

                self.myRequests(mappedRequests);
                self.unreadCount(unread);
            })
            .catch(function(error) {
                console.error('Error loading my requests:', error);
            })
            .finally(function() {
                self.isLoadingRequests(false);
            });
    };

    // Send request
    self.sendRequest = function() {
        if (!self.newMessage()) return;

        self.isSending(true);

        ApiService.post('/support-requests', {
            message: self.newMessage(),
            pageUrl: self.pageUrl()
        })
            .then(function(data) {
                toastr.success(self.T('SupportRequest.SendSuccess', 'Destek talebiniz başarıyla gönderildi.'));
                self.newMessage('');
                self.activeTab('my');
                self.loadMyRequests();
            })
            .catch(function(error) {
                console.error('Error sending request:', error);
                toastr.error(self.T('SupportRequest.SendError', 'Destek talebi gönderilirken hata oluştu.'));
            })
            .finally(function() {
                self.isSending(false);
            });
    };

    // View request detail
    self.viewRequest = function(request) {
        ApiService.get('/support-requests/my/' + request.id)
            .then(function(data) {
                self.selectedRequest({
                    id: data.id,
                    message: data.message,
                    adminResponse: data.adminResponse,
                    createdAt: data.createdAt,
                    respondedAt: data.respondedAt
                });

                // Hide main modal, show detail modal
                if (self.modal) self.modal.hide();

                if (!self.detailModal) {
                    self.detailModal = new bootstrap.Modal(document.getElementById('supportRequestDetailModal'));
                }
                self.detailModal.show();

                // Reload to update read status
                self.loadMyRequests();
            })
            .catch(function(error) {
                console.error('Error loading request detail:', error);
                toastr.error(self.T('SupportRequest.LoadError', 'Talep detayı yüklenirken hata oluştu.'));
            });
    };

    // Check unread on init
    self.checkUnread = function() {
        ApiService.get('/support-requests/my/summary')
            .then(function(data) {
                if (data && data.unreadResponseCount > 0) {
                    self.unreadCount(data.unreadResponseCount);
                }
            })
            .catch(function(error) {
                console.error('Error checking unread:', error);
            });
    };

    // Initialize
    self.checkUnread();
}

// Translation keys for widget
var SUPPORT_WIDGET_TRANSLATION_KEYS = [
    'Menu.SupportRequests',
    'SupportRequest.SendRequest',
    'SupportRequest.New',
    'SupportRequest.MyRequests',
    'SupportRequest.Info',
    'SupportRequest.YourMessage',
    'SupportRequest.MessagePlaceholder',
    'SupportRequest.PageUrl',
    'SupportRequest.Send',
    'SupportRequest.NoRequestsYet',
    'DealerRequest.Detail',
    'SupportRequest.AdminResponse',
    'SupportRequest.WaitingResponse',
    'SupportRequest.NewResponse',
    'SupportRequest.SendSuccess',
    'SupportRequest.SendError',
    'SupportRequest.LoadError',
    'Project.Pending',
    'SupportRequestStatus.InProgress',
    'SupportRequestStatus.Resolved',
    'SupportRequestStatus.Closed',
    'Common.Close'
];

// Initialize when DOM ready
$(document).ready(function() {
    var widgetElement = document.getElementById('support-request-widget');
    if (widgetElement) {
        Localization.loadKeys(SUPPORT_WIDGET_TRANSLATION_KEYS).then(function() {
            ko.applyBindings(new SupportRequestWidgetViewModel(), widgetElement);
        });
    }
});
