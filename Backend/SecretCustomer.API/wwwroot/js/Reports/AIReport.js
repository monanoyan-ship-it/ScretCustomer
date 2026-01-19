// AI Report ViewModel
function AIReportViewModel() {
    var self = this;

    // Müşteri listesi
    self.customers = ko.observableArray([]);

    // Yıl seçenekleri
    var currentYear = new Date().getFullYear();
    self.years = ko.observableArray([currentYear, currentYear - 1, currentYear - 2]);
    self.compareYears = ko.observableArray([currentYear - 1, currentYear - 2, currentYear - 3]);

    // Seçili değerler
    self.selectedCustomerId = ko.observable();
    self.selectedYear = ko.observable(currentYear);
    self.compareYear = ko.observable(currentYear - 1);

    // Hedef puanlar
    self.previousYearTarget = ko.observable(85);
    self.currentYearTarget = ko.observable(90);
    self.stretchTarget = ko.observable(95);

    // Durum
    self.isGenerating = ko.observable(false);
    self.errorMessage = ko.observable('');
    self.lastReport = ko.observable('');
    self.renderedReport = ko.observable('');
    self.showJsonPreview = ko.observable(false);
    self.jsonPreview = ko.observable('');

    // İstatistikler
    self.processingTime = ko.observable('');
    self.tokensUsed = ko.observable('');

    // Rapor oluşturulabilir mi?
    self.canGenerate = ko.computed(function() {
        return self.selectedCustomerId() && self.selectedYear();
    });

    // Müşterileri yükle
    self.loadCustomers = function() {
        ApiService.get('/customers?pageSize=1000')
            .then(function(data) {
                self.customers(data.items || []);
            })
            .catch(function(error) {
                console.error('Müşteriler yüklenirken hata:', error);
                toastr.error(T('AIReport.CustomerLoadError', 'Müşteriler yüklenirken hata oluştu.'));
            });
    };

    // Rapor oluştur
    self.generateReport = function() {
        if (!self.canGenerate()) return;

        self.isGenerating(true);
        self.errorMessage('');
        self.lastReport('');
        self.showJsonPreview(false);

        var request = {
            customerId: parseInt(self.selectedCustomerId()),
            year: parseInt(self.selectedYear()),
            compareYear: self.compareYear() ? parseInt(self.compareYear()) : null,
            language: 'tr',
            targetScores: {
                previousYearTarget: parseFloat(self.previousYearTarget()) || 85,
                currentYearTarget: parseFloat(self.currentYearTarget()) || 90,
                stretchTarget: parseFloat(self.stretchTarget()) || 95
            }
        };

        ApiService.post('/reports/ai/generate', request)
            .then(function(response) {
                self.isGenerating(false);

                if (response.success) {
                    self.lastReport(response.reportContent);
                    self.renderedReport(marked.parse(response.reportContent || ''));
                    self.processingTime((response.processingTimeMs / 1000).toFixed(1) + ' ' + T('AIReport.Seconds', 'saniye'));
                    self.tokensUsed(response.tokensUsed ? response.tokensUsed.toLocaleString() : T('AIReport.Unknown', 'Bilinmiyor'));
                    toastr.success(T('AIReport.GenerateSuccess', 'Rapor başarıyla oluşturuldu!'));
                } else {
                    self.errorMessage(response.errorMessage || T('AIReport.GenerateError', 'Rapor oluşturulamadı.'));
                    toastr.error(response.errorMessage || T('AIReport.GenerateError', 'Rapor oluşturulamadı.'));
                }
            })
            .catch(function(error) {
                self.isGenerating(false);
                self.errorMessage(T('AIReport.GenerateError', 'Rapor oluşturulurken hata oluştu.') + ': ' + error);
                toastr.error(T('AIReport.GenerateError', 'Rapor oluşturulurken hata oluştu.'));
            });
    };

    // Veri önizleme
    self.previewData = function() {
        if (!self.canGenerate()) return;

        self.isGenerating(true);
        self.errorMessage('');
        self.lastReport('');
        self.showJsonPreview(false);

        var request = {
            customerId: parseInt(self.selectedCustomerId()),
            year: parseInt(self.selectedYear()),
            compareYear: self.compareYear() ? parseInt(self.compareYear()) : null,
            language: 'tr',
            targetScores: {
                previousYearTarget: parseFloat(self.previousYearTarget()) || 85,
                currentYearTarget: parseFloat(self.currentYearTarget()) || 90,
                stretchTarget: parseFloat(self.stretchTarget()) || 95
            }
        };

        ApiService.post('/reports/ai/collect-data', request)
            .then(function(data) {
                self.isGenerating(false);
                self.showJsonPreview(true);
                self.jsonPreview(JSON.stringify(data, null, 2));
                toastr.info(T('AIReport.PreviewReady', 'Veri önizleme hazır.'));
            })
            .catch(function(error) {
                self.isGenerating(false);
                self.errorMessage(T('AIReport.DataCollectError', 'Veri toplanırken hata oluştu.') + ': ' + error);
                toastr.error(T('AIReport.DataCollectError', 'Veri toplanırken hata oluştu.'));
            });
    };

    // Panoya kopyala
    self.copyToClipboard = function() {
        if (!self.lastReport()) return;

        navigator.clipboard.writeText(self.lastReport()).then(function() {
            toastr.success(T('AIReport.CopySuccess', 'Rapor panoya kopyalandı!'));
        }).catch(function(err) {
            toastr.error(T('AIReport.CopyError', 'Kopyalama başarısız.') + ': ' + err);
        });
    };

    // Markdown olarak indir
    self.downloadAsMarkdown = function() {
        if (!self.lastReport()) return;

        var customer = self.customers().find(function(c) { return c.id == self.selectedCustomerId(); });
        var customerName = customer ? customer.companyName : 'Rapor';
        var fileName = customerName + '_' + self.selectedYear() + '_AI_Rapor.md';

        var blob = new Blob([self.lastReport()], { type: 'text/markdown;charset=utf-8' });
        var link = document.createElement('a');
        link.href = URL.createObjectURL(blob);
        link.download = fileName;
        link.click();
        URL.revokeObjectURL(link.href);

        toastr.success(T('AIReport.DownloadSuccess', 'Rapor indirildi.') + ': ' + fileName);
    };

    // Init
    self.init = function() {
        self.loadCustomers();
    };

    self.init();
}

// Translation keys
var TRANSLATION_KEYS = [
    'AIReport.CustomerLoadError',
    'AIReport.GenerateSuccess',
    'AIReport.GenerateError',
    'AIReport.PreviewReady',
    'AIReport.DataCollectError',
    'AIReport.CopySuccess',
    'AIReport.CopyError',
    'AIReport.DownloadSuccess',
    'AIReport.Seconds',
    'AIReport.Unknown'
];

// Apply bindings when DOM is ready
$(document).ready(function() {
    Localization.loadKeys(TRANSLATION_KEYS).then(function() {
        var viewModel = new AIReportViewModel();
        ko.applyBindings(viewModel, document.getElementById('ai-report-app'));
    });
});
