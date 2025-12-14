/**
 * Client-side Localization System
 * Provides translation functions for JavaScript files
 */
(function(window) {
    'use strict';

    // Default translations (Turkish)
    var defaultTranslations = {
        // Common
        'Common.Save': 'Kaydet',
        'Common.Cancel': 'İptal',
        'Common.Delete': 'Sil',
        'Common.Edit': 'Düzenle',
        'Common.Add': 'Ekle',
        'Common.Close': 'Kapat',
        'Common.Loading': 'Yükleniyor...',
        'Common.Saving': 'Kaydediliyor...',
        'Common.Yes': 'Evet',
        'Common.No': 'Hayır',
        'Common.OK': 'Tamam',
        'Common.Confirm': 'Onayla',
        'Common.Search': 'Ara',
        'Common.Filter': 'Filtre',
        'Common.Clear': 'Temizle',
        'Common.Refresh': 'Yenile',
        'Common.Export': 'Dışa Aktar',
        'Common.Import': 'İçe Aktar',
        'Common.Download': 'İndir',
        'Common.Detail': 'Detay',
        'Common.View': 'Görüntüle',
        'Common.Start': 'Başlat',
        'Common.Complete': 'Tamamla',
        'Common.Previous': 'Önceki',
        'Common.Next': 'Sonraki',
        'Common.All': 'Tümü',
        'Common.Active': 'Aktif',
        'Common.Inactive': 'Pasif',
        'Common.Unknown': 'Bilinmiyor',

        // Confirmation
        'Confirm.Title': 'Onay',
        'Confirm.Message': 'Bu işlemi onaylıyor musunuz?',
        'Confirm.DeleteTitle': 'Silme Onayı',
        'Confirm.DeleteMessage': 'Bu kaydı silmek istediğinizden emin misiniz?',
        'Confirm.YesDelete': 'Evet, Sil',
        'Confirm.CancelTitle': 'İptal Onayı',
        'Confirm.CancelMessage': 'Bu işlemi iptal etmek istediğinizden emin misiniz?',
        'Confirm.YesCancel': 'Evet, İptal Et',

        // Messages
        'Message.SaveSuccess': 'Kayıt başarıyla oluşturuldu.',
        'Message.UpdateSuccess': 'Kayıt başarıyla güncellendi.',
        'Message.DeleteSuccess': 'Kayıt başarıyla silindi.',
        'Message.Error': 'Bir hata oluştu.',
        'Message.LoadError': 'Veriler yüklenirken bir hata oluştu.',
        'Message.SaveError': 'Kayıt sırasında bir hata oluştu.',
        'Message.DeleteError': 'Silme sırasında bir hata oluştu.',
        'Message.ValidationError': 'Lütfen zorunlu alanları doldurun.',
        'Message.NetworkError': 'Ağ hatası oluştu. Lütfen tekrar deneyin.',
        'Message.SessionExpired': 'Oturumunuz sona erdi. Lütfen tekrar giriş yapın.',
        'Message.AccessDenied': 'Bu işlem için yetkiniz bulunmamaktadır.',
        'Message.NotFound': 'Kayıt bulunamadı.',
        'Message.CopySuccess': 'Panoya kopyalandı!',
        'Message.CopyError': 'Kopyalama sırasında hata oluştu.',
        'Message.ExportSuccess': 'Dışa aktarma başarılı.',
        'Message.ExportError': 'Dışa aktarma sırasında hata oluştu.',
        'Message.ImportSuccess': 'İçe aktarma başarılı.',
        'Message.ImportError': 'İçe aktarma sırasında hata oluştu.',

        // Validation
        'Validation.Required': 'Bu alan zorunludur.',
        'Validation.Email': 'Geçerli bir e-posta adresi giriniz.',
        'Validation.MinLength': 'En az {0} karakter olmalıdır.',
        'Validation.MaxLength': 'En fazla {0} karakter olabilir.',
        'Validation.PasswordMismatch': 'Şifreler eşleşmiyor.',
        'Validation.InvalidFormat': 'Geçersiz format.',
        'Validation.SelectRequired': 'Lütfen bir seçim yapın.',

        // Account / Login
        'Account.LoginRequired': 'Kullanıcı adı ve şifre gereklidir.',
        'Account.LoginFailed': 'Giriş başarısız. Lütfen bilgilerinizi kontrol edin.',
        'Account.LoginError': 'Giriş sırasında bir hata oluştu.',
        'Account.LogoutSuccess': 'Başarıyla çıkış yapıldı.',
        'Account.PasswordChanged': 'Şifreniz başarıyla değiştirildi.',
        'Account.PasswordChangeError': 'Şifre değiştirilirken bir hata oluştu.',
        'Account.PasswordResetSuccess': 'Şifre başarıyla sıfırlandı.',
        'Account.PasswordResetError': 'Şifre sıfırlanırken bir hata oluştu.',
        'Account.ProfileUpdated': 'Profil bilgileriniz başarıyla güncellendi.',
        'Account.ProfileUpdateError': 'Profil güncellenirken bir hata oluştu.',

        // Roles
        'Role.Admin': 'Yönetici',
        'Role.TeamLeader': 'Takım Lideri',
        'Role.Evaluator': 'Değerlendirici',
        'Role.FieldWorker': 'Saha Çalışanı',
        'Role.CustomerRepresentative': 'Müşteri Temsilcisi',
        'Role.CustomerManager': 'Müşteri Yöneticisi',
        'Role.CustomerSupervisor': 'Müşteri Süpervizörü',
        'Role.CustomerOperator': 'Müşteri Operatörü',
        'Role.CustomerViewer': 'Müşteri Görüntüleyici',

        // Status
        'Status.Pending': 'Bekleyen',
        'Status.InProgress': 'Devam Ediyor',
        'Status.Completed': 'Tamamlandı',
        'Status.Cancelled': 'İptal Edildi',
        'Status.Expired': 'Süresi Doldu',
        'Status.Draft': 'Taslak',
        'Status.Approved': 'Onaylandı',
        'Status.Rejected': 'Reddedildi',
        'Status.Postponed': 'Ertelendi',
        'Status.Planned': 'Planlandı',
        'Status.Waiting': 'Beklemede',
        'Status.Missed': 'Cevapsız',
        'Status.Callback': 'Geri Arama',
        'Status.Overdue': 'Gecikmiş',
        'Status.Today': 'Bugün',
        'Status.Upcoming': 'Yaklaşan',

        // Assignments
        'Assignment.Title': 'Atamalar',
        'Assignment.MyTasks': 'Görevlerim',
        'Assignment.NoTasks': 'Henüz size atanmış bir görev bulunmuyor.',
        'Assignment.SaveSuccess': 'Atama başarıyla kaydedildi.',
        'Assignment.UpdateSuccess': 'Atama başarıyla güncellendi.',
        'Assignment.DeleteSuccess': 'Atama başarıyla silindi.',
        'Assignment.CancelSuccess': 'Atama başarıyla iptal edildi.',
        'Assignment.ReassignSuccess': 'Atama başarıyla yeniden atandı.',
        'Assignment.BulkCreateSuccess': 'Toplu atama başarıyla oluşturuldu.',
        'Assignment.LoadError': 'Atamalar yüklenirken bir hata oluştu.',
        'Assignment.SaveError': 'Atama kaydedilirken bir hata oluştu.',
        'Assignment.DeleteError': 'Atama silinirken bir hata oluştu.',
        'Assignment.CancelError': 'Atama iptal edilirken bir hata oluştu.',
        'Assignment.ReassignError': 'Yeniden atama yapılırken bir hata oluştu.',
        'Assignment.BulkCreateError': 'Toplu atama oluşturulurken bir hata oluştu.',
        'Assignment.NotFound': 'Atama bulunamadı.',
        'Assignment.CancelConfirmTitle': 'Atama İptali',
        'Assignment.CancelConfirmMessage': 'Bu atamayı iptal etmek istediğinizden emin misiniz?',
        'Assignment.SelectProject': 'Proje seçmelisiniz!',
        'Assignment.SelectChecklist': 'Kontrol listesi seçmelisiniz!',
        'Assignment.DueDateRequired': 'Son tarih zorunludur!',
        'Assignment.LinkCopied': 'Link kopyalandı!',
        'Assignment.QRCodeError': 'QR kod oluşturulurken bir hata oluştu.',

        // Evaluations
        'Evaluation.Title': 'Değerlendirmeler',
        'Evaluation.LoadError': 'Değerlendirmeler yüklenirken bir hata oluştu.',
        'Evaluation.FormLoadError': 'Form yüklenirken bir hata oluştu.',
        'Evaluation.InvalidParams': 'Geçersiz parametreler.',
        'Evaluation.DraftSaved': 'Taslak başarıyla kaydedildi.',
        'Evaluation.DraftSaveError': 'Taslak kaydedilirken bir hata oluştu.',
        'Evaluation.SubmitSuccess': 'Değerlendirme başarıyla tamamlandı.',
        'Evaluation.SubmitError': 'Değerlendirme gönderilirken bir hata oluştu.',
        'Evaluation.AnswerAllRequired': 'Lütfen tüm zorunlu soruları cevaplayın.',
        'Evaluation.DetailLoadError': 'Detay yüklenirken bir hata oluştu.',
        'Evaluation.StartEvaluation': 'Değerlendirmeyi Başlat',
        'Evaluation.ViewEvaluation': 'Değerlendirmeyi Görüntüle',

        // Branches
        'Branch.Title': 'Şubeler',
        'Branch.LoadError': 'Şubeler yüklenirken bir hata oluştu.',
        'Branch.SaveSuccess': 'Şube başarıyla kaydedildi.',
        'Branch.SaveError': 'Şube kaydedilirken bir hata oluştu.',
        'Branch.DeleteSuccess': 'Şube başarıyla silindi.',
        'Branch.DeleteError': 'Şube silinirken bir hata oluştu.',
        'Branch.NameRequired': 'Şube adı zorunludur!',
        'Branch.CodeRequired': 'Şube kodu zorunludur!',
        'Branch.DeleteConfirm': 'Bu şubeyi silmek istediğinizden emin misiniz?',

        // Customers
        'Customer.Title': 'Müşteriler',
        'Customer.LoadError': 'Müşteriler yüklenirken bir hata oluştu.',
        'Customer.SaveSuccess': 'Müşteri başarıyla kaydedildi.',
        'Customer.UpdateSuccess': 'Müşteri başarıyla güncellendi.',
        'Customer.SaveError': 'Müşteri kaydedilirken bir hata oluştu.',
        'Customer.DeleteSuccess': 'Müşteri başarıyla silindi.',
        'Customer.DeleteError': 'Müşteri silinirken bir hata oluştu.',
        'Customer.CompanyNameRequired': 'Şirket adı zorunludur.',
        'Customer.DeleteConfirm': 'Bu müşteriyi silmek istediğinizden emin misiniz? Bu işlem geri alınamaz.',

        // Personnel
        'Personnel.Title': 'Personel',
        'Personnel.LoadError': 'Personeller yüklenirken bir hata oluştu.',
        'Personnel.SaveSuccess': 'Personel başarıyla kaydedildi.',
        'Personnel.UpdateSuccess': 'Personel başarıyla güncellendi.',
        'Personnel.SaveError': 'Personel kaydedilirken bir hata oluştu.',
        'Personnel.DeleteSuccess': 'Personel başarıyla silindi.',
        'Personnel.DeleteError': 'Personel silinirken bir hata oluştu.',
        'Personnel.RequiredFields': 'Kullanıcı adı, e-posta, ad ve soyad zorunludur.',
        'Personnel.PasswordRequired': 'Yeni personel için şifre zorunludur.',
        'Personnel.DeleteConfirm': 'Bu personeli silmek istediğinizden emin misiniz?',
        'Personnel.AllFieldsRequired': 'Tüm alanlar zorunludur.',

        // Users
        'User.Title': 'Kullanıcılar',
        'User.LoadError': 'Kullanıcılar yüklenirken bir hata oluştu.',
        'User.SaveSuccess': 'Kullanıcı başarıyla kaydedildi.',
        'User.SaveError': 'Kullanıcı kaydedilirken bir hata oluştu.',
        'User.DeleteSuccess': 'Kullanıcı başarıyla silindi.',
        'User.DeleteError': 'Kullanıcı silinirken bir hata oluştu.',
        'User.UsernameRequired': 'Kullanıcı adı zorunludur!',
        'User.FirstNameRequired': 'Ad alanı zorunludur!',
        'User.LastNameRequired': 'Soyad alanı zorunludur!',
        'User.EmailRequired': 'E-posta alanı zorunludur!',
        'User.PasswordMinLength': 'Şifre en az 6 karakter olmalıdır!',

        // Profile
        'Profile.Title': 'Profilim',
        'Profile.LoadError': 'Profil yüklenirken bir hata oluştu.',
        'Profile.UpdateSuccess': 'Profil bilgileriniz başarıyla güncellendi.',
        'Profile.UpdateError': 'Profil güncellenirken bir hata oluştu.',
        'Profile.PasswordsNotMatch': 'Yeni şifreler eşleşmiyor.',
        'Profile.PasswordMinLength': 'Yeni şifre en az 6 karakter olmalıdır.',

        // Reports
        'Report.Title': 'Raporlar',
        'Report.LoadError': 'Rapor yüklenirken bir hata oluştu.',
        'Report.ExportError': 'Excel dosyası indirilemedi.',
        'Report.ExportSuccess': 'Excel dosyası indirildi.',

        // Dashboard
        'Dashboard.Title': 'Ana Sayfa',
        'Dashboard.LoadError': 'Dashboard verileri yüklenirken bir hata oluştu.',
        'Dashboard.WelcomeFieldWorker': 'Hoş geldiniz! Atamalarınızı görmek için soldaki menüden Görevlerim seçeneğine tıklayabilirsiniz.',

        // Meetings
        'Meeting.Title': 'Toplantılar',
        'Meeting.LoadError': 'Toplantılar yüklenirken bir hata oluştu.',
        'Meeting.SaveSuccess': 'Toplantı başarıyla kaydedildi.',
        'Meeting.DeleteSuccess': 'Toplantı başarıyla silindi.',
        'Meeting.TypeGeneral': 'Genel',
        'Meeting.TypeProject': 'Proje',
        'Meeting.TypeEvaluation': 'Değerlendirme',
        'Meeting.TypeTraining': 'Eğitim',
        'Meeting.TypeCustomer': 'Müşteri',
        'Meeting.TypeKickoff': 'Kick-off',
        'Meeting.TypeClosing': 'Kapanış',

        // Calls
        'Call.Title': 'Çağrılar',
        'Call.LoadError': 'Çağrılar yüklenirken bir hata oluştu.',
        'Call.SaveSuccess': 'Çağrı başarıyla kaydedildi.',
        'Call.DeleteSuccess': 'Çağrı başarıyla silindi.',
        'Call.TypeIncoming': 'Gelen',
        'Call.TypeOutgoing': 'Giden',
        'Call.TypeInternal': 'Dahili',
        'Call.TypeConference': 'Konferans',
        'Call.TypeMystery': 'Gizli Müşteri',

        // Trainings
        'Training.Title': 'Eğitimler',
        'Training.LoadError': 'Eğitimler yüklenirken bir hata oluştu.',
        'Training.SaveSuccess': 'Eğitim başarıyla kaydedildi.',
        'Training.DeleteSuccess': 'Eğitim başarıyla silindi.',
        'Training.TypeGeneral': 'Genel',
        'Training.TypeTechnical': 'Teknik',
        'Training.TypeSales': 'Satış',

        // Approvals
        'Approval.Title': 'Onaylar',
        'Approval.LoadError': 'Onaylar yüklenirken bir hata oluştu.',
        'Approval.ApproveSuccess': 'Onay başarıyla verildi.',
        'Approval.RejectSuccess': 'Talep reddedildi.',

        // Bank Visits
        'BankVisit.Title': 'Banka Ziyaretleri',
        'BankVisit.LoadError': 'Veriler yüklenirken hata oluştu.',
        'BankVisit.SaveSuccess': 'Ziyaret kaydedildi.',
        'BankVisit.DeleteSuccess': 'Ziyaret silindi.',
        'BankVisit.DetailLoadError': 'Detay yüklenirken hata oluştu.',
        'BankVisit.EditLoadError': 'Düzenleme için veri yüklenirken hata oluştu.',

        // Languages
        'Language.Title': 'Diller',
        'Language.LoadError': 'Diller yüklenirken hata oluştu.',
        'Language.SaveSuccess': 'Dil kaydedildi.',
        'Language.SaveError': 'Kayıt sırasında hata oluştu.',
        'Language.DeleteSuccess': 'Dil silindi.',
        'Language.DeleteError': 'Silme sırasında hata oluştu.',
        'Language.SetDefaultSuccess': 'Varsayılan dil olarak ayarlandı.',
        'Language.CannotDeleteDefault': 'Varsayılan dil silinemez.',
        'Language.AlreadyDefault': 'Bu dil zaten varsayılan.',
        'Language.ImportSuccess': 'çeviri içe aktarıldı.',
        'Language.ImportError': 'İçe aktarma sırasında hata oluştu.',
        'Language.ImportConfirmTitle': 'XML İçe Aktarma',
        'Language.ImportConfirmMessage': 'için XML dosyasından çevirileri içe aktarmak istiyor musunuz?',
        'Language.SetDefaultTitle': 'Varsayılan Dil',
        'Language.SetDefaultMessage': 'dilini varsayılan yapmak istiyor musunuz?',
        'Language.SetDefault': 'Varsayılan Yap',
        'Language.ResourceSaveSuccess': 'Kaynak kaydedildi.',
        'Language.ResourceDeleteSuccess': 'Kaynak silindi.',
        'Language.SeedSuccess': 'Varsayılan diller oluşturuldu.',
        'Language.SeedError': 'Diller oluşturulurken hata oluştu.',

        // Notifications
        'Notification.Title': 'Bildirimler',
        'Notification.LoadError': 'Bildirimler yüklenirken hata oluştu.',
        'Notification.SettingsLoadError': 'Ayarlar yüklenemedi.',
        'Notification.SettingsCreated': 'Varsayılan ayarlar oluşturuldu.',
        'Notification.SettingsCreateError': 'Ayarlar oluşturulamadı.',
        'Notification.SettingsSaveError': 'Ayar kaydedilemedi.',
        'Notification.TypeInfo': 'Bilgi',
        'Notification.TypeSuccess': 'Başarılı İşlem',
        'Notification.TypeWarning': 'Uyarı',
        'Notification.TypeError': 'Hata',
        'Notification.TypeApprovalRequest': 'Onay Talebi',
        'Notification.TypeTaskAssignment': 'Görev Ataması',
        'Notification.TypeMeetingInvite': 'Toplantı Daveti',
        'Notification.TypeTrainingInvite': 'Eğitim Daveti',
        'Notification.TypeReminder': 'Hatırlatma',
        'Notification.TypeSystem': 'Sistem Bildirimi',

        // Penalties
        'Penalty.YellowCard': 'Sarı Kart',
        'Penalty.RedCard': 'Kırmızı Kart',

        // File names
        'File.Evaluations': 'Degerlendirmeler',
        'File.DetailedEvaluations': 'Detayli_Degerlendirmeler',
        'File.PenaltyReport': 'CezaliKLRaporu',
        'File.SuggestionsReport': 'OnerilerRaporu',

        // Impersonation
        'Impersonation.SelectCustomer': 'Müşteri Seçin',
        'Impersonation.SelectCustomerPlaceholder': 'Müşteri Seçin...',
        'Impersonation.ViewingAs': 'olarak görüntülüyor',
        'Impersonation.Exit': 'Çıkış',
        'Impersonation.Start': 'Görüntülemeyi Başlat',
        'Impersonation.Cancel': 'Vazgeç',
        'Impersonation.Description': 'Bu özellik, seçilen müşterinin görünümüne geçmenizi sağlar. Gerçekleştirdiğiniz işlemler kayıt altına alınır.',
        'Impersonation.LoadError': 'Müşteri listesi yüklenemedi.',
        'Impersonation.SelectError': 'Lütfen bir müşteri seçin.'
    };

    // Current translations (can be overridden)
    var translations = Object.assign({}, defaultTranslations);

    /**
     * Get translation for a key
     * @param {string} key - Translation key (e.g., 'Common.Save')
     * @param {string} [defaultValue] - Default value if key not found
     * @param {...*} [args] - Arguments for string formatting
     * @returns {string} - Translated string
     */
    function T(key, defaultValue) {
        var value = translations[key] || defaultValue || key;

        // Handle format arguments (e.g., T('Validation.MinLength', null, 6))
        if (arguments.length > 2) {
            var args = Array.prototype.slice.call(arguments, 2);
            for (var i = 0; i < args.length; i++) {
                value = value.replace('{' + i + '}', args[i]);
            }
        }

        return value;
    }

    /**
     * Set translations (merge with existing)
     * @param {Object} newTranslations - Object with translation key-value pairs
     */
    function setTranslations(newTranslations) {
        if (newTranslations && typeof newTranslations === 'object') {
            translations = Object.assign({}, translations, newTranslations);
        }
    }

    /**
     * Load translations from API
     * @param {string} [languageCode] - Language code (e.g., 'tr', 'en')
     * @returns {Promise}
     */
    function loadTranslations(languageCode) {
        var url = '/api/localization/resources/current';
        if (languageCode) {
            url = '/api/localization/resources/code/' + languageCode;
        }

        return fetch(url, { credentials: 'include' })
            .then(function(response) {
                if (!response.ok) {
                    console.warn('Failed to load translations');
                    return {};
                }
                return response.json();
            })
            .then(function(data) {
                if (data && typeof data === 'object') {
                    setTranslations(data);
                }
                return translations;
            })
            .catch(function(error) {
                console.warn('Error loading translations:', error);
                return translations;
            });
    }

    /**
     * Get current language code
     * @returns {string}
     */
    function getCurrentLanguage() {
        // Try to get from cookie
        var match = document.cookie.match(/Language=([^;]+)/);
        return match ? match[1] : 'tr';
    }

    // Expose to global scope
    window.T = T;
    window.Localization = {
        T: T,
        setTranslations: setTranslations,
        loadTranslations: loadTranslations,
        getCurrentLanguage: getCurrentLanguage,
        translations: translations
    };

})(window);
