"""
Auto-replace hardcoded Turkish strings in JS files with T() calls.
Also updates TRANSLATION_KEYS arrays and adds keys to XML files.

Usage: python loc_replace_js.py [--dry-run]
"""
import sys, io, os, re
from collections import OrderedDict

sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8')

DRY_RUN = '--dry-run' in sys.argv

BASE_JS = r'C:\Users\Ahmet\source\repos\monanoyan-ship-it\ScretCustomer\Backend\SecretCustomer.API\wwwroot\js'
BASE_XML = r'C:\Users\Ahmet\source\repos\monanoyan-ship-it\ScretCustomer\Backend\SecretCustomer.API\App_Data\Localization'
TR_CHARS = 'şçöüğıİŞÇÖÜĞ'
TR_PATTERN = re.compile(f'[{TR_CHARS}]')

# ============================================================
# FALSE POSITIVES - Skip these strings
# ============================================================
SKIP_PATTERNS = [
    'class=', 'style=', 'data-id=', 'data-question-id',
    'notification-item', 'text-muted', 'text-center', 'text-info',
    'fw-semibold', 'CustomerManager"', 'px-2">', 'mt-1">',
]

def is_false_positive(text):
    t = text.strip()
    if len(t) < 2:
        return True
    if t == '"':
        return True
    # HTML/CSS fragments
    for p in SKIP_PATTERNS:
        if p in t:
            return True
    # Strings that are just whitespace + punctuation
    cleaned = re.sub(r'[^\w]', '', t)
    if not cleaned:
        return True
    return False

# ============================================================
# EXPLICIT KEY MAPPING - Known strings -> specific keys
# ============================================================
KEY_MAP = {
    # === DATE RANGES (shared across 15+ files) ===
    'Bugün': ('DateRange.Today', 'Today'),
    'Dün': ('DateRange.Yesterday', 'Yesterday'),
    'Bu Hafta': ('DateRange.ThisWeek', 'This Week'),
    'Geçen Hafta': ('DateRange.LastWeek', 'Last Week'),
    'Bu Ay': ('DateRange.ThisMonth', 'This Month'),
    'Geçen Ay': ('DateRange.LastMonth', 'Last Month'),
    'Son 3 Ay': ('DateRange.Last3Months', 'Last 3 Months'),
    'Son 6 Ay': ('DateRange.Last6Months', 'Last 6 Months'),
    'Bu Yıl': ('DateRange.ThisYear', 'This Year'),
    'Geçen Yıl': ('DateRange.LastYear', 'Last Year'),
    'Son 7 Gün': ('DateRange.Last7Days', 'Last 7 Days'),
    'Son 30 Gün': ('DateRange.Last30Days', 'Last 30 Days'),
    # === STATUS LABELS ===
    'Tamamlandı': ('Status.Completed', 'Completed'),
    'Başlamadı': ('Status.NotStarted', 'Not Started'),
    'Duraklatıldı': ('Status.Paused', 'Paused'),
    'İptal Edildi': ('Status.Cancelled', 'Cancelled'),
    'Planlandı': ('Status.Planned', 'Planned'),
    'Gönderildi': ('Status.Submitted', 'Submitted'),
    'Süresi Geçmiş': ('Status.Overdue', 'Overdue'),
    'Süresi Doldu': ('Status.Expired', 'Expired'),
    'Başarılı': ('Status.Successful', 'Successful'),
    'Başarısız': ('Status.Failed', 'Failed'),
    # === COMMON UI LABELS ===
    'Müşteri': ('Common.Customer', 'Customer'),
    'Müşteri/Org.': ('Common.CustomerOrg', 'Customer/Org.'),
    'Şube/Org.': ('Common.DealerOrg', 'Dealer/Org.'),
    'Kırmızı Kart': ('Common.RedCard', 'Red Card'),
    'Sarı Kart': ('Common.YellowCard', 'Yellow Card'),
    'Düşük Puan': ('Common.LowScore', 'Low Score'),
    'Değerlendirme Sayısı': ('Common.EvaluationCount', 'Evaluation Count'),
    'Değerlendirme Tarihi': ('Common.EvaluationDate', 'Evaluation Date'),
    'Değerlendirme Tipi': ('Common.EvaluationType', 'Evaluation Type'),
    'Çağrı Tarihi': ('Common.CallDate', 'Call Date'),
    'Çağrı ID': ('Common.CallId', 'Call ID'),
    'Çağrı No': ('Common.CallNo', 'Call No'),
    'Firma Adı': ('Common.CompanyName', 'Company Name'),
    'Kullanıcı Adı': ('Common.Username', 'Username'),
    'Organizasyon Adı': ('Common.OrganizationName', 'Organization Name'),
    'Bayi Adı': ('Common.DealerName', 'Dealer Name'),
    'Bitiş Tarihi': ('Common.EndDate', 'End Date'),
    'Kayıt Tarihi': ('Common.RecordDate', 'Record Date'),
    'Tarih Aralığı': ('Common.DateRange', 'Date Range'),
    'Değerlendiren': ('Common.Evaluator', 'Evaluator'),
    'Değerlendirici': ('Common.Evaluator2', 'Evaluator'),
    'Yönetici': ('Common.Manager', 'Manager'),
    'Not/Öneri': ('Common.NoteRecommendation', 'Note/Recommendation'),
    'Yanıt': ('Common.Response', 'Response'),
    'Yanıt Sayısı': ('Common.ResponseCount', 'Response Count'),
    'Şehir': ('Common.City', 'City'),
    'İç Değerlendirme': ('Common.InternalEvaluation', 'Internal Evaluation'),
    'Dış Değerlendirme': ('Common.ExternalEvaluation', 'External Evaluation'),
    'Taslak Değerlendirme': ('Common.DraftEvaluation', 'Draft Evaluation'),
    'İptal': ('Common.Cancel', 'Cancel'),
    # === TIME AGO ===
    'Az önce': ('TimeAgo.JustNow', 'Just now'),
    ' dk önce': ('TimeAgo.MinutesAgo', ' min ago'),
    ' dakika önce': ('TimeAgo.MinutesAgoLong', ' minutes ago'),
    ' saat önce': ('TimeAgo.HoursAgo', ' hours ago'),
    ' gün önce': ('TimeAgo.DaysAgo', ' days ago'),
    # === PRIORITY ===
    'Yüksek': ('Priority.High', 'High'),
    'Düşük': ('Priority.Low', 'Low'),
    # === ROLES ===
    'Kalite Uzmanı': ('Role.QualitySpecialist', 'Quality Specialist'),
    'Saha Çalışanı': ('Role.FieldWorker', 'Field Worker'),
    'Süpervizör': ('Role.Supervisor', 'Supervisor'),
    'Operatör': ('Role.Operator', 'Operator'),
    'Firma Yöneticisi': ('Role.CustomerManager', 'Customer Manager'),
    'Proje Yöneticisi': ('Role.ProjectManager', 'Project Manager'),
    'Gözlemci': ('Role.Observer', 'Observer'),
    # === FREQUENCY ===
    'Günlük': ('Frequency.Daily', 'Daily'),
    'Haftalık': ('Frequency.Weekly', 'Weekly'),
    'Aylık': ('Frequency.Monthly', 'Monthly'),
    # === MONTHS ===
    'Şubat': ('Month.February', 'February'),
    'Ağustos': ('Month.August', 'August'),
    # === PROJECT TYPES ===
    'Gizli Müşteri': ('ProjectType.MysteryShopping', 'Mystery Shopping'),
    'Çağrı Denetimi': ('ProjectType.CallAuditing', 'Call Auditing'),
    'Müşteri Memnuniyeti': ('ProjectType.CustomerSatisfaction', 'Customer Satisfaction'),
    'Eğitim Değerlendirme': ('ProjectType.TrainingEvaluation', 'Training Evaluation'),
    # === COMMON MESSAGES ===
    'Bir hata oluştu': ('Common.ErrorOccurred', 'An error occurred'),
    'Bir hata oluştu.': ('Common.ErrorOccurredDot', 'An error occurred.'),
    'Beklenmeyen bir hata oluştu.': ('Common.UnexpectedError', 'An unexpected error occurred.'),
    'Bu işlem için yetkiniz bulunmamaktadır.': ('Common.Unauthorized', 'You do not have permission for this action.'),
    'Kopyalandı': ('Common.Copied', 'Copied'),
    'Kopyalama başarısız': ('Common.CopyFailed', 'Copy failed'),
    'Panoya kopyalandı': ('Common.CopiedToClipboard', 'Copied to clipboard'),
    'Yükleme başarısız': ('Common.LoadFailed', 'Load failed'),
    'Bu kaydı silmek istediğinizden emin misiniz?': ('Common.DeleteConfirm', 'Are you sure you want to delete this record?'),
    'Talep gönderilemedi': ('Common.RequestFailed', 'Failed to send request'),
    'Dosya indirme hatası': ('Common.FileDownloadError', 'File download error'),
    'Tooltip yüklenemedi': ('Common.TooltipLoadFailed', 'Failed to load tooltip'),
    # === COMMON DETAIL/DATA MESSAGES ===
    'Detay yüklenemedi': ('Common.DetailLoadFailed', 'Failed to load details'),
    'Detay yüklenemedi.': ('Common.DetailLoadFailedDot', 'Failed to load details.'),
    'Detay yüklenirken bir hata oluştu.': ('Common.DetailLoadError', 'An error occurred while loading details.'),
    'Detay yüklenirken hata oluştu': ('Common.DetailLoadError2', 'Error loading details'),
    'Detay yüklenirken hata oluştu.': ('Common.DetailLoadError3', 'Error loading details.'),
    'Veriler yüklenirken bir hata oluştu.': ('Common.DataLoadError', 'An error occurred while loading data.'),
    'Veriler yüklenirken hata oluştu': ('Common.DataLoadError2', 'Error loading data'),
    'Veriler yüklenirken hata oluştu.': ('Common.DataLoadError3', 'Error loading data.'),
    # === EXCEL/PDF/WORD EXPORT ===
    'Excel dosyası indirildi': ('Common.ExcelDownloaded', 'Excel file downloaded'),
    'Excel dosyası oluşturulamadı': ('Common.ExcelCreateFailed', 'Failed to create Excel file'),
    'Excel dosyası oluşturulurken bir hata oluştu.': ('Common.ExcelCreateError', 'An error occurred while creating Excel file.'),
    'Excel oluşturulurken hata oluştu': ('Common.ExcelGenerateError', 'Error generating Excel'),
    'Excel export sırasında hata oluştu': ('Common.ExcelExportError', 'Error during Excel export'),
    'Excel export başarısız.': ('Common.ExcelExportFailed', 'Excel export failed.'),
    'Excel export başarısız: ': ('Common.ExcelExportFailedDetail', 'Excel export failed: '),
    'Export başarısız': ('Common.ExportFailed', 'Export failed'),
    'Export sırasında hata oluştu': ('Common.ExportError', 'Error during export'),
    'Detaylı Excel dosyası indirildi': ('Common.DetailedExcelDownloaded', 'Detailed Excel file downloaded'),
    'Detaylı Excel export sırasında hata oluştu': ('Common.DetailedExcelExportError', 'Error during detailed Excel export'),
    'Excel indirme hazırlanıyor...': ('Common.ExcelPreparing', 'Preparing Excel download...'),
    'PDF oluşturuluyor...': ('Common.PdfGenerating', 'Generating PDF...'),
    'PDF oluşturulamadı': ('Common.PdfCreateFailed', 'Failed to create PDF'),
    'PDF oluşturulurken bir hata oluştu.': ('Common.PdfCreateError', 'An error occurred while creating PDF.'),
    'PDF dosyası oluşturulamadı': ('Common.PdfFileCreateFailed', 'Failed to create PDF file'),
    'PDF dosyası oluşturulurken bir hata oluştu.': ('Common.PdfFileCreateError', 'An error occurred while creating PDF file.'),
    'Word dosyası oluşturulamadı': ('Common.WordCreateFailed', 'Failed to create Word file'),
    'Word dosyası oluşturulurken bir hata oluştu.': ('Common.WordCreateError', 'An error occurred while creating Word file.'),
    # === VALIDATION ===
    'Kullanıcı adı sadece İngilizce harf, rakam, alt çizgi, nokta ve tire içerebilir.': ('Validation.UsernameFormat', 'Username can only contain English letters, numbers, underscores, dots, and hyphens.'),
    'Şifre en az 6 karakter olmalıdır.': ('Validation.PasswordMinLength', 'Password must be at least 6 characters.'),
    'Şifre en az 6 karakter olmalıdır!': ('Validation.PasswordMinLengthExcl', 'Password must be at least 6 characters!'),
    'Şifreler eşleşmiyor.': ('Validation.PasswordMismatch', 'Passwords do not match.'),
    'Şifreler eşleşmiyor!': ('Validation.PasswordMismatchExcl', 'Passwords do not match!'),
    'Tüm alanları doldurun.': ('Validation.FillAllFields', 'Please fill in all fields.'),
    'Şifre başarıyla değiştirildi.': ('Validation.PasswordChanged', 'Password changed successfully.'),
    'Şifre değiştirilemedi.': ('Validation.PasswordChangeFailed', 'Failed to change password.'),
    'Şifre zorunludur.': ('Validation.PasswordRequired', 'Password is required.'),
    # === REPORT ===
    'Rapor oluşturulurken hata oluştu': ('Common.ReportGenerateError', 'Error generating report'),
    'Rapor yüklenemedi': ('Common.ReportLoadFailed', 'Failed to load report'),
    'Rapor yüklenemedi.': ('Common.ReportLoadFailedDot', 'Failed to load report.'),
    'Rapor yüklenirken bir hata oluştu.': ('Common.ReportLoadError', 'An error occurred while loading report.'),
    'Soru puan dağılımı yüklenirken hata oluştu.': ('Report.ScoreDistributionError', 'Error loading question score distribution.'),
    'Puan detayı yüklenirken hata oluştu.': ('Report.ScoreDetailError', 'Error loading score details.'),
    'Sonuç detayı yüklenirken hata oluştu.': ('Report.ResultDetailError', 'Error loading result details.'),
    'Sonuçlar yüklenirken hata oluştu.': ('Report.ResultsLoadError', 'Error loading results.'),
    'Yanıt detayı yüklenirken hata oluştu.': ('Report.ResponseDetailError', 'Error loading response details.'),
    'Yanıtlar yüklenirken hata oluştu.': ('Report.ResponsesLoadError', 'Error loading responses.'),
    'Bitiş tarihi, başlangıç tarihinden önce olamaz.': ('Report.EndDateBeforeStart', 'End date cannot be before start date.'),
    'Bu proje zaten eklenmiş.': ('Report.ProjectAlreadyAdded', 'This project has already been added.'),
    'Bu proje zaten filtrelere eklenmiş.': ('Report.ProjectAlreadyInFilter', 'This project is already added to filters.'),
    'Proje listesi yüklenemedi': ('Report.ProjectListLoadFailed', 'Failed to load project list'),
    'Müşteri listesi yüklenemedi': ('Report.CustomerListLoadFailed', 'Failed to load customer list'),
    'Organizasyon listesi yüklenemedi': ('Report.OrgListLoadFailed', 'Failed to load organization list'),
    'Personel listesi yüklenemedi': ('Report.PersonnelListLoadFailed', 'Failed to load personnel list'),
    'Şube listesi yüklenemedi': ('Report.DealerListLoadFailed', 'Failed to load dealer list'),
    'Şube listesi yüklenirken bir hata oluştu.': ('Report.DealerListLoadError', 'An error occurred while loading dealer list.'),
    'Lütfen bir şube seçin.': ('Report.SelectDealer', 'Please select a dealer.'),
    'Lütfen bir proje seçin.': ('Report.SelectProject', 'Please select a project.'),
    'Karne yüklenemedi': ('Report.ReportCardLoadFailed', 'Failed to load report card'),
    'Karne yüklenirken bir hata oluştu.': ('Report.ReportCardLoadError', 'An error occurred while loading report card.'),
    'Değerlendirme detayı yüklenemedi.': ('Report.EvalDetailLoadFailed', 'Failed to load evaluation detail.'),
    'Değerlendirme detayı yüklenirken hata oluştu.': ('Report.EvalDetailLoadError', 'Error loading evaluation detail.'),
    'Liste yüklenemedi': ('Common.ListLoadFailed', 'Failed to load list'),
    'Liste yüklenirken hata oluştu': ('Common.ListLoadError', 'Error loading list'),
    'Alt kriterler yüklenemedi': ('Report.SubCriteriaLoadFailed', 'Failed to load sub-criteria'),
    'Top sorular yüklenemedi': ('Report.TopQuestionsLoadFailed', 'Failed to load top questions'),
    'Müşteriler yüklenirken hata: ': ('Report.CustomersLoadErrorDetail', 'Error loading customers: '),
    'Projeler yüklenirken hata oluştu.': ('Report.ProjectsLoadError', 'Error loading projects.'),
    # === REPORT CARD / DASHBOARD ===
    'Başarılı (≥ ': ('Dashboard.SuccessThreshold', 'Successful (≥ '),
    'Başarısız (< ': ('Dashboard.FailThreshold', 'Failed (< '),
    'Uyarı': ('Dashboard.Warning', 'Warning'),
    'Uyarı ( ': ('Dashboard.WarningThreshold', 'Warning ( '),
    'Dashboard verileri yüklenirken bir hata oluştu.': ('Dashboard.LoadError', 'An error occurred while loading dashboard data.'),
    'Grafik bulunamadı': ('Dashboard.ChartNotFound', 'Chart not found'),
    'Grafik canvas bulunamadı': ('Dashboard.ChartCanvasNotFound', 'Chart canvas not found'),
    # === EVALUATION ===
    'Değerlendirme tamamlandı': ('Evaluation.Completed', 'Evaluation completed'),
    'Form yüklenemedi': ('Evaluation.FormLoadFailed', 'Failed to load form'),
    'Form yüklenirken bir hata oluştu: ': ('Evaluation.FormLoadError', 'An error occurred while loading form: '),
    'Geçersiz parametre': ('Evaluation.InvalidParameter', 'Invalid parameter'),
    'Kaydetme hatası': ('Evaluation.SaveError', 'Save error'),
    'Kaydetme hatası: ': ('Evaluation.SaveErrorDetail', 'Save error: '),
    'Personel seçimi zorunludur': ('Evaluation.PersonnelRequired', 'Personnel selection is required'),
    'Süre zorunludur (SS:DD:SN formatında giriniz)': ('Evaluation.DurationRequired', 'Duration is required (enter in HH:MM:SS format)'),
    'Çağrı Saati zorunludur (SS:DD formatında giriniz)': ('Evaluation.CallTimeRequired', 'Call time is required (enter in HH:MM format)'),
    'Çağrı Tarihi zorunludur': ('Evaluation.CallDateRequired', 'Call date is required'),
    'Zorunlu sorular cevaplanmalıdır: ': ('Evaluation.RequiredQuestionsUnanswered', 'Required questions must be answered: '),
    'Değerlendirme ID bulunamadı': ('Evaluation.IdNotFound', 'Evaluation ID not found'),
    'Değerlendirme bilgisi bulunamadı': ('Evaluation.InfoNotFound', 'Evaluation information not found'),
    'Değerlendirme detayı indirildi': ('Evaluation.DetailDownloaded', 'Evaluation detail downloaded'),
    'Değerlendirmeler yüklenemedi': ('Evaluation.ListLoadFailed', 'Failed to load evaluations'),
    'Değerlendirme kaydetme fonksiyonu geliştirilecek.': ('Evaluation.SaveNotImplemented', 'Save function will be developed.'),
    'Bu işlem için yetkiniz bulunmamaktadır.': ('Common.Unauthorized', 'You do not have permission for this action.'),
    'Sadece taslak durumundaki değerlendirmeler silinebilir.': ('Evaluation.OnlyDraftCanBeDeleted', 'Only draft evaluations can be deleted.'),
    'Değerlendirme tipi filtresi zaten eklenmiş. Önce mevcut olanı kaldırın.': ('Evaluation.TypeFilterAlreadyAdded', 'Evaluation type filter already added. Remove the existing one first.'),
    'Taslak başarıyla silindi.': ('Evaluation.DraftDeleted', 'Draft deleted successfully.'),
    'Taslak silinirken bir hata oluştu.': ('Evaluation.DraftDeleteError', 'An error occurred while deleting draft.'),
    'Silme işlemi başarısız': ('Common.DeleteFailed', 'Delete failed'),
    # === NOTIFICATION ===
    'Okundu İşaretle': ('Notification.MarkAsRead', 'Mark as Read'),
    'Tümünü Okundu İşaretle': ('Notification.MarkAllAsRead', 'Mark All as Read'),
    'Tüm bildirimleri okundu olarak işaretlemek istiyor musunuz?': ('Notification.MarkAllConfirm', 'Do you want to mark all notifications as read?'),
    ' bildirim okundu olarak işaretlendi': ('Notification.MarkedAsRead', ' notification(s) marked as read'),
    'Bildirim okundu işaretlenemedi: ': ('Notification.MarkReadFailed', 'Failed to mark notification as read: '),
    'Bildirim sayısı yüklenemedi: ': ('Notification.CountLoadFailed', 'Failed to load notification count: '),
    'Bildirimler okundu işaretlenemedi: ': ('Notification.MarkAllReadFailed', 'Failed to mark notifications as read: '),
    'Bildirimler yüklenemedi: ': ('Notification.LoadFailed', 'Failed to load notifications: '),
    'SignalR bağlantı hatası: ': ('Notification.SignalRConnectionError', 'SignalR connection error: '),
    'SignalR kütüphanesi yüklenmedi': ('Notification.SignalRNotLoaded', 'SignalR library not loaded'),
    'SignalR yeniden bağlandı': ('Notification.SignalRReconnected', 'SignalR reconnected'),
    'SignalR yeniden bağlanıyor...': ('Notification.SignalRReconnecting', 'SignalR reconnecting...'),
    # === PROJECT ===
    'Proje adı zorunludur!': ('Project.NameRequired', 'Project name is required!'),
    'Kontrol listesi seçmelisiniz!': ('Project.ChecklistRequired', 'You must select a checklist!'),
    'Başlangıç ve bitiş tarihleri zorunludur!': ('Project.DatesRequired', 'Start and end dates are required!'),
    'Proje oluşturuldu.': ('Project.Created', 'Project created.'),
    'Proje başarıyla silindi.': ('Project.Deleted', 'Project deleted successfully.'),
    'Proje başlatıldı.': ('Project.Started', 'Project started.'),
    'Proje duraklatıldı.': ('Project.PausedMsg', 'Project paused.'),
    'Proje tamamlandı.': ('Project.CompletedMsg', 'Project completed.'),
    'Proje yüklenemedi': ('Project.LoadFailed', 'Failed to load project'),
    'Proje yüklenirken bir hata oluştu.': ('Project.LoadError', 'An error occurred while loading project.'),
    'Projeler yüklenirken bir hata oluştu.': ('Project.ListLoadError', 'An error occurred while loading projects.'),
    'Projeler yüklenemedi': ('Project.ListLoadFailed', 'Failed to load projects'),
    'Organizasyonlar yüklenemedi': ('Project.OrganizationsLoadFailed', 'Failed to load organizations'),
    'Personel listesi yüklenirken hata oluştu.': ('Project.PersonnelLoadError', 'Error loading personnel list.'),
    'Proje kaydedilirken bir hata oluştu: ': ('Project.SaveError', 'An error occurred while saving project: '),
    'Proje başlatılırken bir hata oluştu.': ('Project.StartError', 'An error occurred while starting project.'),
    'Proje duraklatılırken bir hata oluştu.': ('Project.PauseError', 'An error occurred while pausing project.'),
    'Proje tamamlanırken bir hata oluştu.': ('Project.CompleteError', 'An error occurred while completing project.'),
    'Proje iptal edilirken bir hata oluştu.': ('Project.CancelError', 'An error occurred while cancelling project.'),
    'Proje silinirken bir hata oluştu.': ('Project.DeleteError', 'An error occurred while deleting project.'),
    'Proje detayı yüklenirken bir hata oluştu.': ('Project.DetailLoadError', 'An error occurred while loading project details.'),
    'Proje detayı yüklenirken hata oluştu.': ('Project.DetailLoadError2', 'Error loading project details.'),
    'Proje kodu oluşturulurken bir hata oluştu.': ('Project.CodeGenerateError', 'An error occurred while generating project code.'),
    'Kod oluşturulamadı': ('Project.CodeGenerateFailed', 'Failed to generate code'),
    'Kayıt başarısız': ('Project.SaveFailed', 'Save failed'),
    'Silme başarısız': ('Project.DeleteFailed', 'Delete failed'),
    'Projeyi başlatmak istediğinizden emin misiniz?': ('Project.StartConfirm', 'Are you sure you want to start this project?'),
    'Projeyi duraklatmak istediğinizden emin misiniz?': ('Project.PauseConfirm', 'Are you sure you want to pause this project?'),
    'Projeyi tamamlamak istediğinizden emin misiniz?': ('Project.CompleteConfirm', 'Are you sure you want to complete this project?'),
    'Projeyi iptal etmek istediğinizden emin misiniz?': ('Project.CancelConfirm', 'Are you sure you want to cancel this project?'),
    'Email şablonu seçilmemiş. Proje ayarlarından şablon seçin.': ('Project.NoEmailTemplate', 'No email template selected. Select a template from project settings.'),
    'Proje için email şablonu seçilmemiş. Proje ayarlarından şablon seçin.': ('Project.NoEmailTemplateForProject', 'No email template selected for project.'),
    'Davetiye gönderilirken bir hata oluştu.': ('Project.InviteSendError', 'An error occurred while sending invitation.'),
    'Hatırlatma gönderilecek davetiye bulunamadı.': ('Project.NoInvitesForReminder', 'No invitations found to send reminders.'),
    'Gönderim sırasında hata oluştu.': ('Project.SendError', 'Error during sending.'),
    'Lütfen bir dosya seçin (CSV veya Excel).': ('Project.SelectCsvOrExcel', 'Please select a file (CSV or Excel).'),
    'Sadece CSV ve Excel (.xlsx, .xls) dosyaları desteklenir.': ('Project.OnlyCsvExcelSupported', 'Only CSV and Excel files are supported.'),
    'Dosyalar yüklenemedi': ('Project.FilesLoadFailed', 'Failed to load files'),
    'Dosya yükleme sırasında hata oluştu.': ('Project.FileUploadError', 'Error during file upload.'),
    'Dosya yüklenirken bir hata oluştu.': ('Common.FileUploadError', 'An error occurred while uploading file.'),
    'Dosya yüklenirken bir hata oluştu: ': ('Project.FileUploadErrorDetail', 'An error occurred while uploading file: '),
    'Dosya silinirken bir hata oluştu.': ('Common.FileDeleteError', 'An error occurred while deleting file.'),
    ' dosya yüklendi.': ('Project.FilesUploaded', ' file(s) uploaded.'),
    'Başlat': ('Project.Start', 'Start'),
    'Proje Başlat': ('Project.StartProject', 'Start Project'),
    'Proje İptal': ('Project.CancelProject', 'Cancel Project'),
    'İptal Et': ('Project.CancelAction', 'Cancel'),
    'Lütfen email adresi girin.': ('Common.EnterEmail', 'Please enter an email address.'),
    # === ASSIGNMENT ===
    'Bugün Son Tarih': ('Assignment.DueToday', 'Due Today'),
    'Yarın Son Tarih': ('Assignment.DueTomorrow', 'Due Tomorrow'),
    '7 Gün İçinde': ('Assignment.DueIn7Days', 'Due in 7 Days'),
    '30 Gün İçinde': ('Assignment.DueIn30Days', 'Due in 30 Days'),
    'Bugün Bitecek': ('Project.DueToday', 'Due Today'),
    '7 Gün İçinde Bitecek': ('Project.DueIn7Days', 'Due in 7 Days'),
    '30 Gün İçinde Bitecek': ('Project.DueIn30Days', 'Due in 30 Days'),
    'Bu Çeyrek Bitecek': ('Project.DueThisQuarter', 'Due This Quarter'),
    # === CUSTOMER / ORGANIZATION ===
    'Organizasyon adı zorunludur.': ('Organization.NameRequired', 'Organization name is required.'),
    'Organizasyon oluşturuldu.': ('Organization.Created', 'Organization created.'),
    'Organizasyon kaydedilirken hata oluştu.': ('Organization.SaveError', 'Error saving organization.'),
    'Organizasyon silinirken hata oluştu.': ('Organization.DeleteError', 'Error deleting organization.'),
    'Organizasyon yüklenirken hata oluştu.': ('Organization.LoadError', 'Error loading organization.'),
    'Organizasyonlar yüklenirken bir hata oluştu.': ('Organization.ListLoadError', 'An error occurred while loading organizations.'),
    'Organizasyonlar yüklenirken hata oluştu.': ('Organization.ListLoadError2', 'Error loading organizations.'),
    'Personeller yüklenirken bir hata oluştu.': ('Organization.PersonnelLoadError', 'An error occurred while loading personnel.'),
    'Personeller yüklenirken hata oluştu.': ('Organization.PersonnelLoadError2', 'Error loading personnel.'),
    'Müşteriler yüklenirken bir hata oluştu.': ('Organization.CustomersLoadError', 'An error occurred while loading customers.'),
    'Lütfen önce bir müşteri seçin.': ('Organization.SelectCustomerFirst', 'Please select a customer first.'),
    'Rol seçimi zorunludur.': ('Organization.RoleRequired', 'Role selection is required.'),
    'Kullanıcı adı, e-posta, ad ve soyad zorunludur.': ('Organization.RequiredFields', 'Username, email, first and last name are required.'),
    'Bu e-posta adresi zaten kullanılıyor.': ('Organization.EmailInUse', 'This email address is already in use.'),
    'Bu kullanıcı adı zaten kullanılıyor.': ('Organization.UsernameInUse', 'This username is already in use.'),
    'Personel başarıyla oluşturuldu.': ('Organization.PersonnelCreated', 'Personnel created successfully.'),
    'Personel kaydedilirken bir hata oluştu: ': ('Organization.PersonnelSaveError', 'An error occurred while saving personnel: '),
    'Ekipten Çıkar': ('Organization.RemoveFromTeam', 'Remove from Team'),
    'Ekipten çıkarma sırasında hata oluştu.': ('Organization.RemoveFromTeamError', 'Error during team removal.'),
    'Organizasyondan Çıkar': ('Organization.RemoveFromOrg', 'Remove from Organization'),
    'Organizasyondan çıkarma sırasında hata oluştu.': ('Organization.RemoveFromOrgError', 'Error during organization removal.'),
    'Evet, Çıkar': ('Organization.YesRemove', 'Yes, Remove'),
    ' ekip üyesi': ('Organization.TeamMemberSuffix', ' team member(s)'),
    ' ekipten çıkarıldı.': ('Organization.RemovedFromTeam', ' removed from team.'),
    ' organizasyondan çıkarıldı.': ('Organization.RemovedFromOrg', ' removed from organization.'),
    ' kişisini ': ('Organization.PersonSuffix', ' person(s) '),
    'Operatör ekibe eklendi.': ('CustomerOrg.OperatorAddedToTeam', 'Operator added to team.'),
    'Operatör eklenirken hata oluştu.': ('CustomerOrg.OperatorAddError', 'Error adding operator.'),
    'Operatör oluşturuldu ve organizasyona eklendi.': ('CustomerOrg.OperatorCreatedAndAdded', 'Operator created and added to organization.'),
    'Operatör oluşturulurken hata oluştu.': ('CustomerOrg.OperatorCreateError', 'Error creating operator.'),
    'Operatör organizasyona eklendi.': ('CustomerOrg.OperatorAddedToOrg', 'Operator added to organization.'),
    'Süpervizör oluşturuldu ve organizasyona eklendi.': ('CustomerOrg.SupervisorCreatedAndAdded', 'Supervisor created and added to organization.'),
    'Süpervizör oluşturulurken hata oluştu.': ('CustomerOrg.SupervisorCreateError', 'Error creating supervisor.'),
    'Personel bağımsız yapıldı.': ('CustomerOrg.PersonnelMadeIndependent', 'Personnel made independent.'),
    'Personel eklenirken hata oluştu.': ('CustomerOrg.PersonnelAddError', 'Error adding personnel.'),
    'Personel organizasyondan çıkarıldı.': ('CustomerOrg.PersonnelRemovedFromOrg', 'Personnel removed from organization.'),
    'Ekip devredildi ve personel çıkarıldı.': ('CustomerOrg.TeamTransferredAndRemoved', 'Team transferred and personnel removed.'),
    'Yönetici oluşturuldu.': ('CustomerOrg.ManagerCreated', 'Manager created.'),
    'Yönetici oluşturulurken hata oluştu.': ('CustomerOrg.ManagerCreateError', 'Error creating manager.'),
    'Yönetici silindi.': ('CustomerOrg.ManagerDeleted', 'Manager deleted.'),
    'Yönetici silinirken hata oluştu.': ('CustomerOrg.ManagerDeleteError', 'Error deleting manager.'),
    'Yönetici silinirken hata: ': ('CustomerOrg.ManagerDeleteErrorDetail', 'Error deleting manager: '),
    'İşlem sırasında hata oluştu.': ('CustomerOrg.OperationError', 'Error during operation.'),
    'Müşteri detayları yüklenirken hata oluştu.': ('Customer.DetailLoadError', 'Error loading customer details.'),
    # === DEALER ===
    'Bayi adı zorunludur.': ('Dealer.NameRequired', 'Dealer name is required.'),
    'Bayi başarıyla oluşturuldu.': ('Dealer.Created', 'Dealer created successfully.'),
    'Bayi başarıyla silindi.': ('Dealer.Deleted', 'Dealer deleted successfully.'),
    'Bayi kaydedilirken bir hata oluştu.': ('Dealer.SaveError', 'An error occurred while saving dealer.'),
    'Bayi listesi yüklenirken bir hata oluştu.': ('Dealer.ListLoadError', 'An error occurred while loading dealer list.'),
    'Bayi silinirken bir hata oluştu.': ('Dealer.DeleteError', 'An error occurred while deleting dealer.'),
    'Müşteri ID bulunamadı.': ('Customer.IdNotFound', 'Customer ID not found.'),
    # === CUSTOMER PERSONNEL ===
    'Ad, soyad, kullanıcı adı ve e-posta zorunludur.': ('CustomerPersonnel.RequiredFields', 'First name, last name, username and email are required.'),
    'Personel oluşturuldu.': ('CustomerPersonnel.Created', 'Personnel created.'),
    'Yeni personel için şifre zorunludur.': ('CustomerPersonnel.PasswordRequired', 'Password is required for new personnel.'),
    'Şifre sıfırlandı.': ('CustomerPersonnel.PasswordReset', 'Password has been reset.'),
    'Şifre sıfırlanırken bir hata oluştu.': ('CustomerPersonnel.PasswordResetError', 'An error occurred while resetting password.'),
    'Personel kaydedilirken bir hata oluştu.': ('Personnel.SaveError', 'An error occurred while saving personnel.'),
    'Personel silinirken bir hata oluştu.': ('Personnel.DeleteError', 'An error occurred while deleting personnel.'),
    # === PERSONNEL ===
    'Ad ve Soyad alanları zorunludur.': ('Personnel.NameRequired', 'First and Last name fields are required.'),
    'E-posta alanı zorunludur.': ('Personnel.EmailRequired', 'Email field is required.'),
    'Kullanıcı adı zorunludur.': ('Personnel.UsernameRequired', 'Username is required.'),
    'Personel başarıyla güncellendi.': ('Personnel.Updated', 'Personnel updated successfully.'),
    'Personel başarıyla silindi.': ('Personnel.Deleted', 'Personnel deleted successfully.'),
    'Personel listesi yüklenirken bir hata oluştu.': ('Personnel.ListLoadError', 'An error occurred while loading personnel list.'),
    # === USERS ===
    'Kullanıcı adı zorunludur!': ('User.UsernameRequired', 'Username is required!'),
    'Müşteri seçimi zorunludur!': ('User.CustomerRequired', 'Customer selection is required!'),
    'Kullanıcı başarıyla kaydedildi.': ('User.Saved', 'User saved successfully.'),
    'Kullanıcı silindi.': ('User.Deleted', 'User deleted.'),
    'Müşteri personeli başarıyla kaydedildi.': ('User.CustomerPersonnelSaved', 'Customer personnel saved successfully.'),
    'Müşteri personeli silindi.': ('User.CustomerPersonnelDeleted', 'Customer personnel deleted.'),
    'Silme hatası.': ('User.DeleteError', 'Delete error.'),
    'Arama sırasında hata oluştu': ('User.SearchError', 'Error during search'),
    # === PERMISSIONS ===
    'Lütfen bir yetki seçin.': ('Permission.SelectPermission', 'Please select a permission.'),
    'Kullanıcı yetkileri yüklenemedi': ('Permission.LoadFailed', 'Failed to load user permissions'),
    'Kullanıcı yetkisi başarıyla eklendi.': ('Permission.Added', 'User permission added successfully.'),
    'Kullanıcı yetkisi başarıyla kaldırıldı.': ('Permission.Removed', 'User permission removed successfully.'),
    'Rol yetkileri başarıyla güncellendi.': ('Permission.RoleUpdated', 'Role permissions updated successfully.'),
    'Yetki eklenirken bir hata oluştu.': ('Permission.AddError', 'An error occurred while adding permission.'),
    'Yetki kaldırılırken bir hata oluştu.': ('Permission.RemoveError', 'An error occurred while removing permission.'),
    'Yetkiler kaydedilirken bir hata oluştu.': ('Permission.SaveError', 'An error occurred while saving permissions.'),
    'Yetkiler senkronize edilirken hata oluştu.': ('Permission.SyncError', 'Error synchronizing permissions.'),
    'Senkronizasyon başarısız': ('Permission.SyncFailed', 'Synchronization failed'),
    'İşlem başarısız': ('Permission.OperationFailed', 'Operation failed'),
    'Bu özel yetki': ('Permission.CustomPermission', 'This custom permission'),
    # === SMTP/SETTINGS ===
    'Bağlantı testi sırasında hata oluştu.': ('Smtp.ConnectionTestError', 'Error during connection test.'),
    'Kaydetme sırasında hata oluştu.': ('Smtp.SaveError', 'Error during save.'),
    'Lütfen bir email adresi girin.': ('Smtp.EnterEmail', 'Please enter an email address.'),
    'Lütfen bir profil seçin.': ('Smtp.SelectProfile', 'Please select a profile.'),
    'Profiller yüklenirken hata oluştu.': ('Smtp.ProfilesLoadError', 'Error loading profiles.'),
    'Silme sırasında hata oluştu.': ('Smtp.DeleteError', 'Error during delete.'),
    'Test maili gönderilirken hata oluştu.': ('Smtp.TestEmailError', 'Error sending test email.'),
    'Varsayılan yapma sırasında hata oluştu.': ('Smtp.DefaultSetError', 'Error setting as default.'),
    'Ayar güncellendi.': ('Settings.Updated', 'Setting updated.'),
    ' ayarı': ('Settings.SettingSuffix', ' setting'),
    # === EMAIL TEMPLATES ===
    'Email gönderilirken hata oluştu.': ('EmailTemplate.SendError', 'Error sending email.'),
    'Email içeriği gerekli.': ('EmailTemplate.ContentRequired', 'Email content is required.'),
    'Email içeriğini buraya yazın...': ('EmailTemplate.ContentPlaceholder', 'Write email content here...'),
    'Şablon adı gerekli.': ('EmailTemplate.NameRequired', 'Template name is required.'),
    'Gerçek veri kullanmak için proje ve personel seçin.': ('EmailTemplate.SelectProjectAndPersonnel', 'Select project and personnel to use real data.'),
    # === EXCEL TEMPLATES ===
    'Lütfen önce Varlık Tipi seçin': ('ExcelTemplate.SelectEntityType', 'Please select an Entity Type first'),
    'Excel içe aktarılırken hata oluştu: ': ('ExcelTemplate.ImportError', 'Error importing Excel: '),
    'Hatalı satırlar: ': ('ExcelTemplate.ErrorRows', 'Error rows: '),
    'Hiçbir satır işlenemedi': ('ExcelTemplate.NoRowsProcessed', 'No rows could be processed'),
    'İçe aktarma başarısız': ('ExcelTemplate.ImportFailed', 'Import failed'),
    'Kaydetme işlemi başarısız': ('ExcelTemplate.SaveFailed', 'Save failed'),
    'Şablon güncellendi': ('ExcelTemplate.Updated', 'Template updated'),
    'Şablon kaydedilirken hata oluştu: ': ('ExcelTemplate.SaveError', 'Error saving template: '),
    'Şablon silindi': ('ExcelTemplate.Deleted', 'Template deleted'),
    'Şablon silinirken bir hata oluştu.': ('ExcelTemplate.DeleteError', 'An error occurred while deleting template.'),
    'Şablon yüklenirken hata oluştu': ('ExcelTemplate.LoadError', 'Error loading template'),
    'Şablonlar yüklenirken hata oluştu': ('ExcelTemplate.ListLoadError', 'Error loading templates'),
    'Şema yüklenirken hata oluştu': ('ExcelTemplate.SchemaLoadError', 'Error loading schema'),
    ' şablonu': ('ExcelTemplate.TemplateSuffix', ' template'),
    # === CHECKLIST ===
    'Kontrol listesi yüklenirken bir hata oluştu.': ('Checklist.LoadError', 'An error occurred while loading checklist.'),
    'Dosya başarıyla yüklendi.': ('Checklist.FileUploaded', 'File uploaded successfully.'),
    'Dosya eklemek için önce soruyu kaydetmeniz gerekiyor.': ('Checklist.SaveQuestionFirst', 'You need to save the question first to add files.'),
    'Dosya yüklenemedi.': ('Checklist.FileUploadFailed', 'Failed to upload file.'),
    'Kontrol listesi güncellendi.': ('Checklist.Updated', 'Checklist updated.'),
    'Puan girişini göster': ('Checklist.ShowScoreInput', 'Show score input'),
    # === SURVEY ===
    'Anket doğrulanamadı': ('Survey.ValidationFailed', 'Survey validation failed'),
    'Anket yüklenirken bir hata oluştu.': ('Survey.LoadError', 'An error occurred while loading survey.'),
    'Geçersiz anket daveti.': ('Survey.InvalidInvitation', 'Invalid survey invitation.'),
    'Geçersiz anket linki. Token bulunamadı.': ('Survey.InvalidLink', 'Invalid survey link. Token not found.'),
    'Gönderim başarısız': ('Survey.SubmitFailed', 'Submit failed'),
    # === TRAINING VIDEOS ===
    'Atama detayları yüklenemedi': ('TrainingVideo.AssignmentLoadFailed', 'Failed to load assignment details'),
    'Atama güncellendi': ('TrainingVideo.AssignmentUpdated', 'Assignment updated'),
    'Başlık gerekli': ('TrainingVideo.TitleRequired', 'Title required'),
    'Geçerli bir email adresi girin': ('TrainingVideo.EnterValidEmail', 'Enter a valid email address'),
    'Güncelleme sırasında hata oluştu': ('TrainingVideo.UpdateError', 'Error during update'),
    # === CUSTOMER PORTAL ===
    'Firma ayarları başarıyla güncellendi.': ('CustomerPortal.ProfileUpdated', 'Company settings updated successfully.'),
    'Firma ayarları güncellenemedi': ('CustomerPortal.ProfileUpdateFailed', 'Failed to update company settings'),
    'Firma ayarları güncellenirken bir hata oluştu.': ('CustomerPortal.ProfileUpdateError', 'An error occurred while updating company settings.'),
    'Firma ayarları yüklenemedi': ('CustomerPortal.ProfileLoadFailed', 'Failed to load company settings'),
    'Profil yüklenemedi': ('CustomerPortal.ProfileLoadFailed2', 'Failed to load profile'),
    ' sonrası': ('Common.AfterSuffix', ' after'),
    ' öncesi': ('Common.BeforeSuffix', ' before'),
    # === FILTER ===
    'Filtre adı zorunludur': ('Filter.NameRequired', 'Filter name is required'),
    'Filtre kaydedilirken hata oluştu': ('Filter.SaveError', 'Error saving filter'),
    'Filtre uygulandı: ': ('Filter.Applied', 'Filter applied: '),
    'Filtre uygulanırken hata oluştu': ('Filter.ApplyError', 'Error applying filter'),
    'Kaydetmek için önce filtre ekleyin': ('Filter.AddFilterFirst', 'Add a filter first to save'),
    'Varsayılan filtre ayarlandı': ('Filter.DefaultSet', 'Default filter set'),
    'Varsayılan filtre ayarlanırken hata oluştu': ('Filter.DefaultSetError', 'Error setting default filter'),
    'Varsayılan filtre kaldırıldı': ('Filter.DefaultRemoved', 'Default filter removed'),
    'Varsayılan filtre kaldırılırken hata oluştu': ('Filter.DefaultRemoveError', 'Error removing default filter'),
    ' filtresi kaldırıldı (farklı müşteriye ait)': ('Filter.RemovedDifferentCustomer', ' filter removed (belongs to different customer)'),
    # === LISTENING/VISIT REPORTS ===
    'Çağrı Denetleme Raporu indirildi': ('Listening.ReportDownloaded', 'Call Audit Report downloaded'),
    'Müşteri Değerlendirme Raporu indirildi': ('Listening.CustomerReportDownloaded', 'Customer Evaluation Report downloaded'),
    'Müşteri Ziyaret Değerlendirme Raporu indirildi': ('Visit.CustomerReportDownloaded', 'Customer Visit Evaluation Report downloaded'),
    # === AUDIT LOG ===
    'Loglar yüklenirken hata oluştu': ('AuditLog.LoadError', 'Error loading logs'),
    'Kaç günden eski loglar silinsin?': ('AuditLog.DeleteOldPrompt', 'Delete logs older than how many days?'),
    # === CONFIRM MESSAGES WITH HTML ===
    '</strong> bayisini silmek istediğinize emin misiniz?': ('Dealer.DeleteConfirmMsg', '</strong> Do you want to delete this dealer?'),
    '</strong> organizasyonunu silmek istediğinize emin misiniz?': ('CustomerOrg.DeleteOrgConfirm', '</strong> Do you want to delete this organization?'),
    '</strong> personelini organizasyondan çıkarmak istediğinize emin misiniz?': ('CustomerOrg.RemovePersonnelConfirm', '</strong> Do you want to remove this personnel from organization?'),
    '</strong> personelini silmek istediğinize emin misiniz?': ('CustomerPersonnel.DeleteConfirm', '</strong> Do you want to delete this personnel?'),
    '</strong> videosunu silmek istediğinize emin misiniz?': ('TrainingVideo.DeleteConfirm', '</strong> Do you want to delete this video?'),
    '</strong> yöneticisini silmek istediğinize emin misiniz?': ('CustomerOrg.DeleteManagerConfirm', '</strong> Do you want to delete this manager?'),
    '</strong> kopyalandı<br>': ('Organization.CopiedMsg', '</strong> copied<br>'),
    ' - Değerlendirmeler': ('EvaluationReport.TitleSuffix', ' - Evaluations'),
}

# ============================================================
# MODULE MAPPING
# ============================================================
MODULE_MAP = {
    'Assignments': 'Assignment', 'AuditLogs': 'AuditLog', 'Checklists': 'Checklist',
    'CustomerOrganizations': 'CustomerOrganization', 'CustomerPersonnel': 'CustomerPersonnel',
    'CustomerPortal': 'CustomerPortal', 'Customers': 'Customer', 'Dashboard': 'Dashboard',
    'EmailTemplates': 'EmailTemplate', 'Evaluations': 'Evaluation',
    'EvaluationReport': 'EvaluationReport', 'ExcelTemplates': 'ExcelTemplate',
    'FieldWorker': 'FieldWorker', 'InternalAssignments': 'InternalAssignment',
    'Languages': 'Language', 'Listenings': 'Listening', 'MyAssignments': 'MyAssignment',
    'Notifications': 'Notification', 'Permissions': 'Permission', 'Personnel': 'Personnel',
    'Projects': 'Project', 'Reports': 'Report', 'Settings': 'Settings', 'Shared': 'Shared',
    'Survey': 'Survey', 'TrainingQuiz': 'TrainingQuiz', 'TrainingVideos': 'TrainingVideo',
    'Users': 'User', 'Visits': 'Visit',
}

# ============================================================
# AUTO KEY GENERATION for strings not in KEY_MAP
# ============================================================
_auto_counters = {}

def auto_generate_key(module, text):
    """Generate a key for strings not in KEY_MAP."""
    # Clean text for key generation
    clean = text.strip().rstrip('.!?:')
    # Take first 3-4 significant words
    words = re.sub(r'[^\w\sğüşıöçĞÜŞİÖÇ]', '', clean).split()[:4]
    if not words:
        return None

    # Simple Turkish -> English word mapping for key names
    tr_en = {
        'yüklenirken': 'Load', 'yüklenemedi': 'LoadFailed', 'yükleniyor': 'Loading',
        'hata': 'Error', 'oluştu': '', 'başarıyla': 'Success', 'başarısız': 'Failed',
        'silinirken': 'Delete', 'silindi': 'Deleted', 'silmek': 'Delete',
        'kaydedilirken': 'Save', 'kaydedildi': 'Saved', 'kaydetme': 'Save',
        'güncellenirken': 'Update', 'güncellendi': 'Updated', 'güncelleme': 'Update',
        'oluşturuldu': 'Created', 'oluşturulurken': 'Create',
        'zorunludur': 'Required', 'zorunlu': 'Required', 'gerekli': 'Required',
        'istediğinize': 'Confirm', 'emin': '', 'misiniz': '',
        'bir': '', 've': '', 'için': '', 'olan': '', 'olan': '',
    }

    suffix_parts = []
    for w in words:
        low = w.lower()
        if low in tr_en:
            mapped = tr_en[low]
            if mapped:
                suffix_parts.append(mapped)
        else:
            # Capitalize first letter
            suffix_parts.append(w[:1].upper() + w[1:])

    suffix = ''.join(suffix_parts)[:40]
    if not suffix:
        suffix = 'Text'

    key = f'{module}.{suffix}'

    # Ensure uniqueness
    if key in _auto_counters:
        _auto_counters[key] += 1
        key = f'{key}{_auto_counters[key]}'
    else:
        _auto_counters[key] = 0

    return key

# ============================================================
# PROCESSING
# ============================================================
str_pattern = re.compile(r"""(['"])([^'"]*?[""" + TR_CHARS + r""""][^'"]*?)\1""")
all_new_keys = OrderedDict()
file_changes = {}

def get_module(filepath):
    rel = os.path.relpath(filepath, BASE_JS)
    parts = rel.split(os.sep)
    return MODULE_MAP.get(parts[0], parts[0]) if parts else 'Common'

def process_file(filepath):
    with open(filepath, 'r', encoding='utf-8-sig') as f:
        content = f.read()

    lines = content.split('\n')
    module = get_module(filepath)
    new_keys = []
    modified = False

    for i, line in enumerate(lines):
        stripped = line.strip()
        if not stripped or stripped.startswith('//') or stripped.startswith('/*') or stripped.startswith('*'):
            continue
        if not TR_PATTERN.search(line):
            continue

        for match in str_pattern.finditer(line):
            quote = match.group(1)
            text = match.group(2)
            pos = match.start()

            if is_false_positive(text):
                continue

            # Skip if inside T() call
            before = line[:pos]
            if re.search(r"T\(\s*['\"][^'\"]*['\"]\s*,\s*$", before):
                continue
            if re.search(r"T\(\s*$", before.rstrip()):
                continue

            # Get key from mapping or auto-generate
            if text in KEY_MAP:
                key, en_val = KEY_MAP[text]
            else:
                key = auto_generate_key(module, text)
                if not key:
                    continue
                en_val = text  # Use TR as fallback for EN

            # Do replacement
            old_str = f"{quote}{text}{quote}"
            new_str = f"T('{key}', {quote}{text}{quote})"
            new_line = line.replace(old_str, new_str, 1)
            if new_line != line:
                lines[i] = new_line
                line = new_line
                modified = True
                new_keys.append(key)
                all_new_keys[key] = (text, en_val)

            break  # One replacement per line

    if modified:
        # Update TRANSLATION_KEYS
        lines = update_translation_keys(lines, new_keys)
        if not DRY_RUN:
            with open(filepath, 'w', encoding='utf-8-sig') as f:
                f.write('\n'.join(lines))
        rel = os.path.relpath(filepath, BASE_JS)
        file_changes[rel] = new_keys

    return new_keys

def update_translation_keys(lines, new_keys):
    if not new_keys:
        return lines

    tk_start = -1
    tk_end = -1
    for i, line in enumerate(lines):
        if 'TRANSLATION_KEYS' in line and '[' in line:
            tk_start = i
        if tk_start >= 0 and tk_end < 0 and '];' in line:
            tk_end = i
            break

    if tk_start < 0:
        # No TRANSLATION_KEYS found - add before $(document).ready
        ready_line = -1
        for i, line in enumerate(lines):
            if '$(document).ready' in line or "document.addEventListener('DOMContentLoaded'" in line:
                ready_line = i
                break

        unique_keys = sorted(set(new_keys))
        tk_block = '\nvar TRANSLATION_KEYS = [\n'
        for k in unique_keys:
            tk_block += f"    '{k}',\n"
        tk_block += '];\n'

        if ready_line >= 0:
            lines.insert(ready_line, tk_block)
        else:
            lines.append(tk_block)
        return lines

    # Extract existing keys
    existing_keys = set()
    for i in range(tk_start, tk_end + 1):
        for m in re.finditer(r"'([^']+)'", lines[i]):
            existing_keys.add(m.group(1))

    keys_to_add = [k for k in sorted(set(new_keys)) if k not in existing_keys]
    if not keys_to_add:
        return lines

    insert_lines = [f"    '{k}'," for k in keys_to_add]
    insert_text = '\n'.join(insert_lines)
    lines[tk_end] = insert_text + '\n' + lines[tk_end]

    return lines

# ============================================================
# XML UPDATE
# ============================================================
def update_xml_files():
    for xml_file, lang_idx in [('resources.tr.xml', 0), ('resources.en.xml', 1), ('resources.de.xml', 1), ('resources.es.xml', 1)]:
        xml_path = os.path.join(BASE_XML, xml_file)
        if not os.path.exists(xml_path):
            continue

        with open(xml_path, 'r', encoding='utf-8-sig') as f:
            content = f.read()

        added = 0
        snippets = []
        for key in sorted(all_new_keys.keys()):
            if f'Name="{key}"' in content:
                continue
            tr_val, en_val = all_new_keys[key]
            val = tr_val if lang_idx == 0 else en_val
            val = val.replace('&', '&amp;').replace('<', '&lt;').replace('>', '&gt;')
            snippets.append(f'  <LocaleResource Name="{key}">\n    <Value>{val}</Value>\n  </LocaleResource>')
            added += 1

        if snippets and not DRY_RUN:
            insert_block = '\n'.join(snippets)
            content = content.replace('</Language>', f'{insert_block}\n</Language>')
            with open(xml_path, 'w', encoding='utf-8-sig') as f:
                f.write(content)

        print(f'  {xml_file}: {added} keys {"would be " if DRY_RUN else ""}added')

# ============================================================
# MAIN
# ============================================================
print(f'{"[DRY RUN] " if DRY_RUN else ""}Processing JS files...\n')

js_files = []
for root, dirs, files in os.walk(BASE_JS):
    dirs[:] = [d for d in dirs if d not in ('lib', 'node_modules')]
    for f in files:
        if f.endswith('.js'):
            js_files.append(os.path.join(root, f))

total = 0
for filepath in sorted(js_files):
    keys = process_file(filepath)
    if keys:
        rel = os.path.relpath(filepath, BASE_JS)
        print(f'  {rel}: {len(keys)} replacements')
        total += len(keys)

print(f'\nTotal: {total} replacements in {len(file_changes)} files')
print(f'Unique keys: {len(all_new_keys)}')

if all_new_keys:
    print(f'\nUpdating XML files...')
    update_xml_files()

# Show auto-generated keys for review
auto_keys = {k: v for k, v in all_new_keys.items() if k not in [km[0] for km in KEY_MAP.values()]}
if auto_keys:
    print(f'\n=== AUTO-GENERATED KEYS ({len(auto_keys)}) ===')
    for key, (tr, en) in auto_keys.items():
        print(f'  {key}: {tr}')

print('\nDone!')
