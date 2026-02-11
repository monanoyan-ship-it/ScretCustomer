#!/usr/bin/env python3
"""Localize hardcoded Turkish text in Assignments.cshtml"""
import os

filepath = os.path.join(os.path.dirname(os.path.dirname(os.path.dirname(__file__))),
    'Backend', 'SecretCustomer.API', 'Views', 'TrainingVideos', 'Assignments.cshtml')

with open(filepath, 'r', encoding='utf-8') as f:
    content = f.read()

replacements = [
    # Header
    ('<h2><i class="bi bi-people"></i> Egitim Atamalari</h2>', '<h2><i class="bi bi-people"></i> @Html.T("Assignment.Title", "Egitim Atamalari")</h2>'),
    ('<i class="bi bi-arrow-left"></i> Videolara Don', '<i class="bi bi-arrow-left"></i> @Html.T("Training.BackToVideos", "Videolara Don")'),
    ('<i class="bi bi-list-check me-1"></i>Atamalar', '<i class="bi bi-list-check me-1"></i>@Html.T("Training.Assignments", "Atamalar")'),
    ('<i class="bi bi-people me-1"></i>Tum Katilimcilar', '<i class="bi bi-people me-1"></i>@Html.T("Training.AllParticipants", "Tum Katilimcilar")'),

    # Filter options
    ('<option value="">Filtre Secin...</option>', '<option value="">@Html.T("Common.SelectFilter", "Filtre Secin...")</option>'),
    ('<option value="searchTerm">Arama</option>', '<option value="searchTerm">@Html.T("Common.Search", "Arama")</option>'),
    ('<option value="video">Video</option>', '<option value="video">@Html.T("Training.Video", "Video")</option>'),
    ('<option value="participantType">Katilimci Tipi</option>', '<option value="participantType">@Html.T("Training.ParticipantType", "Katilimci Tipi")</option>'),
    ("optionsCaption: 'Video secin...'", """optionsCaption: '@Html.T("Training.SelectVideo", "Video secin...")'"""),
    ('<option value="">Tip secin...</option>', '<option value="">@Html.T("Common.SelectType", "Tip secin...")</option>'),
    ('<option value="internal">Ic</option>', '<option value="internal">@Html.T("Training.Internal", "Ic")</option>'),
    ('<option value="external">Dis</option>', '<option value="external">@Html.T("Training.External", "Dis")</option>'),
    ('<option value="start">Baslangic Tarihi</option>', '<option value="start">@Html.T("Common.StartDate", "Baslangic Tarihi")</option>'),
    ('<option value="due">Bitis Tarihi</option>', '<option value="due">@Html.T("Common.DueDate", "Bitis Tarihi")</option>'),

    # Date range buttons in filter area
    ('btn-outline-secondary">Bugun</button>', 'btn-outline-secondary">@Html.T("Common.Today", "Bugun")</button>'),
    ('btn-outline-secondary">Yarin</button>', 'btn-outline-secondary">@Html.T("Common.Tomorrow", "Yarin")</button>'),
    ('btn-outline-secondary">Bu Hafta</button>', 'btn-outline-secondary">@Html.T("Common.ThisWeek", "Bu Hafta")</button>'),
    ('btn-outline-secondary">Gelecek Hafta</button>', 'btn-outline-secondary">@Html.T("Common.NextWeek", "Gelecek Hafta")</button>'),
    ('btn-outline-secondary">Bu Ay</button>', 'btn-outline-secondary">@Html.T("Common.ThisMonth", "Bu Ay")</button>'),
    ('btn-outline-secondary">Gelecek Ay</button>', 'btn-outline-secondary">@Html.T("Common.NextMonth", "Gelecek Ay")</button>'),
    ('btn-outline-secondary">Gelecek 3 Ay</button>', 'btn-outline-secondary">@Html.T("Common.Next3Months", "Gelecek 3 Ay")</button>'),
    ('btn-outline-secondary">Bu Yil</button>', 'btn-outline-secondary">@Html.T("Common.ThisYear", "Bu Yil")</button>'),
    ('btn-outline-secondary">Gecen Yil</button>', 'btn-outline-secondary">@Html.T("Common.LastYear", "Gecen Yil")</button>'),

    # Clear/Loading
    ('<i class="bi bi-x-circle"></i> Temizle', '<i class="bi bi-x-circle"></i> @Html.T("Common.Clear", "Temizle")'),
    ('<p class="mt-2 text-muted">Yukleniyor...</p>', '<p class="mt-2 text-muted">@Html.T("Common.Loading", "Yukleniyor...")</p>'),
    ('<p class="mt-2 text-muted">Katilimcilar yukleniyor...</p>', '<p class="mt-2 text-muted">@Html.T("Training.LoadingParticipants", "Katilimcilar yukleniyor...")</p>'),

    # Table headers
    ('<th>Video</th>', '<th>@Html.T("Training.Video", "Video")</th>'),
    ('<th class="text-center">Tip</th>', '<th class="text-center">@Html.T("Common.Type", "Tip")</th>'),
    ('<th class="text-center">Ilerleme</th>', '<th class="text-center">@Html.T("Training.Progress", "Ilerleme")</th>'),
    ('<th class="text-end" style="width: 150px;">Islemler</th>', '<th class="text-end" style="width: 150px;">@Html.T("Common.Actions", "Islemler")</th>'),
    ('<th>Musteri</th>', '<th>@Html.T("Common.Customer", "Musteri")</th>'),
    ('<th>Kullanici</th>', '<th>@Html.T("Common.User", "Kullanici")</th>'),
    ('<th class="text-center">Izlenen</th>', '<th class="text-center">@Html.T("Training.Watched", "Izlenen")</th>'),
    ('<th class="text-center">Tamamlama</th>', '<th class="text-center">@Html.T("Training.Completion", "Tamamlama")</th>'),
    ('<th class="text-center">Son Email</th>', '<th class="text-center">@Html.T("Training.LastEmail", "Son Email")</th>'),
    ('<th>Ad Soyad</th>', '<th>@Html.T("Common.FullName", "Ad Soyad")</th>'),
    ('<th>Email</th>', '<th>@Html.T("Common.Email", "Email")</th>'),
    ('<th style="width: 100px;">Tur</th>', '<th style="width: 100px;">@Html.T("Common.Type", "Tur")</th>'),
    ('<th>Checklist</th>', '<th>@Html.T("Common.Checklist", "Checklist")</th>'),
    ('<th>Grup/Soru</th>', '<th>@Html.T("Training.GroupQuestion", "Grup/Soru")</th>'),

    # Type badges
    ('class="badge bg-secondary">Dis</span>', 'class="badge bg-secondary">@Html.T("Training.External", "Dis")</span>'),
    ('class="badge bg-info">Ic</span>', 'class="badge bg-info">@Html.T("Training.Internal", "Ic")</span>'),
    ('class="badge bg-info ms-1">Dis</span>', 'class="badge bg-info ms-1">@Html.T("Training.External", "Dis")</span>'),
    ('class="badge bg-secondary ms-1">Dis</span>', 'class="badge bg-secondary ms-1">@Html.T("Training.External", "Dis")</span>'),

    # Status badges
    ('class="badge bg-warning">Bekliyor</span>', 'class="badge bg-warning">@Html.T("Training.Waiting", "Bekliyor")</span>'),
    ('class="badge bg-info">Izliyor</span>', 'class="badge bg-info">@Html.T("Training.Watching", "Izliyor")</span>'),
    ('class="badge bg-success">Tamamladi</span>', 'class="badge bg-success">@Html.T("Training.Completed", "Tamamladi")</span>'),
    ('class="badge bg-warning">Bekleme</span>', 'class="badge bg-warning">@Html.T("Training.Waiting", "Bekleme")</span>'),

    # data-bind text with kisi
    ("+ ' kisi'", """+ ' @Html.T("Common.Person", "kisi")'"""),

    # Button titles
    ('title="Duzenle"', 'title="@Html.T("Common.Edit", "Duzenle")"'),
    ('title="Katilimcilari Gor"', 'title="@Html.T("Training.ViewParticipants", "Katilimcilari Gor")"'),
    ('title="Email Yonetimi"', 'title="@Html.T("Training.EmailManagement", "Email Yonetimi")"'),
    ('title="Tumunu Sec"', 'title="@Html.T("Common.SelectAll", "Tumunu Sec")"'),

    # Empty states
    ('Henuz egitim atamasi bulunmuyor.', '@Html.T("Training.NoAssignmentsYet", "Henuz egitim atamasi bulunmuyor.")'),
    ('<i class="bi bi-plus-circle"></i> Ilk Atamayi Olustur', '<i class="bi bi-plus-circle"></i> @Html.T("Training.CreateFirstAssignment", "Ilk Atamayi Olustur")'),
    ('Filtrelerinize uygun katilimci bulunamadi.', '@Html.T("Training.NoMatchingParticipants", "Filtrelerinize uygun katilimci bulunamadi.")'),
    ('Henuz katilimci yok', '@Html.T("Training.NoParticipantsYet", "Henuz katilimci yok")'),
    ('Henuz ic katilimci yok.', '@Html.T("Training.NoInternalParticipantsYet", "Henuz ic katilimci yok.")'),
    ('Henuz dis katilimci yok.', '@Html.T("Training.NoExternalParticipantsYet", "Henuz dis katilimci yok.")'),
    ('Filtre kriterlerine uyan katilimci yok', '@Html.T("Training.NoMatchingParticipants", "Filtre kriterlerine uyan katilimci yok")'),

    # Wizard steps
    ('rounded-pill me-2">1</span> Video ve Ayarlar', 'rounded-pill me-2">1</span> @Html.T("Training.VideoAndSettings", "Video ve Ayarlar")'),
    ('bg-primary">2</span> Filtreler', 'bg-primary">2</span> @Html.T("Common.Filters", "Filtreler")'),
    ('bg-primary">4</span> Ozet', 'bg-primary">4</span> @Html.T("Training.Summary", "Ozet")'),

    # Step 1
    ('<h6 class="border-bottom pb-2 mb-3"><i class="bi bi-play-circle me-2"></i>Video Secimi</h6>', '<h6 class="border-bottom pb-2 mb-3"><i class="bi bi-play-circle me-2"></i>@Html.T("Training.VideoSelection", "Video Secimi")</h6>'),
    ('<label class="form-label">Egitim Videosu <span class="text-danger">*</span></label>', '<label class="form-label">@Html.T("Training.TrainingVideo", "Egitim Videosu") <span class="text-danger">*</span></label>'),
    ('placeholder="Video ara..."', 'placeholder="@Html.T("Training.SearchVideo", "Video ara...")"'),
    ('<label class="form-label">Baslangic Tarihi <span class="text-danger">*</span></label>', '<label class="form-label">@Html.T("Common.StartDate", "Baslangic Tarihi") <span class="text-danger">*</span></label>'),
    ('<label class="form-label">Bitis Tarihi <span class="text-danger">*</span></label>', '<label class="form-label">@Html.T("Common.DueDate", "Bitis Tarihi") <span class="text-danger">*</span></label>'),
    ('<h6 class="border-bottom pb-2 mb-3 mt-3"><i class="bi bi-gear me-2"></i>Izleme Ayarlari</h6>', '<h6 class="border-bottom pb-2 mb-3 mt-3"><i class="bi bi-gear me-2"></i>@Html.T("Training.WatchSettings", "Izleme Ayarlari")</h6>'),
    ('<label class="form-label">Min Izleme Sayisi</label>', '<label class="form-label">@Html.T("Training.MinWatchCount", "Min Izleme Sayisi")</label>'),
    ('<label class="form-label">Max Izleme Sayisi</label>', '<label class="form-label">@Html.T("Training.MaxWatchCount", "Max Izleme Sayisi")</label>'),
    ('placeholder="Sinirsiz"', 'placeholder="@Html.T("Training.Unlimited", "Sinirsiz")"'),
    ('<small class="text-muted">Bos = sinirsiz</small>', '<small class="text-muted">@Html.T("Training.EmptyMeansUnlimited", "Bos = sinirsiz")</small>'),
    ('Video hizini degistirmeye izin ver', '@Html.T("Training.AllowSpeedChange", "Video hizini degistirmeye izin ver")'),
    ('Videoyu ileri/geri sarmaya izin ver', '@Html.T("Training.AllowSeeking", "Videoyu ileri/geri sarmaya izin ver")'),
    ('<h6 class="border-bottom pb-2 mb-3"><i class="bi bi-envelope me-2"></i>Email Ayarlari</h6>', '<h6 class="border-bottom pb-2 mb-3"><i class="bi bi-envelope me-2"></i>@Html.T("Training.EmailSettings", "Email Ayarlari")</h6>'),
    ('Katilimcilara email gonder', '@Html.T("Training.SendEmailToParticipants", "Katilimcilara email gonder")'),
    ('<label class="form-label">Email Sablonu</label>', '<label class="form-label">@Html.T("Training.EmailTemplate", "Email Sablonu")</label>'),
    ("optionsCaption: 'Varsayilan sablon'", """optionsCaption: '@Html.T("Training.DefaultTemplate", "Varsayilan sablon")'"""),
    ('<h6 class="border-bottom pb-2 mb-3"><i class="bi bi-list-check me-2"></i>Video Kapsami</h6>', '<h6 class="border-bottom pb-2 mb-3"><i class="bi bi-list-check me-2"></i>@Html.T("Training.VideoScope", "Video Kapsami")</h6>'),
    ('Bu video asagidaki konularda egitim icin hazirlanmistir.', '@Html.T("Training.VideoScopeDescription", "Bu video asagidaki konularda egitim icin hazirlanmistir.")'),
    ('<span class="badge bg-primary"><i class="bi bi-journal-check"></i> Checklist</span>', '<span class="badge bg-primary"><i class="bi bi-journal-check"></i> @Html.T("Common.Checklist", "Checklist")</span>'),
    ('<span class="badge bg-success"><i class="bi bi-collection"></i> Grup</span>', '<span class="badge bg-success"><i class="bi bi-collection"></i> @Html.T("Common.Group", "Grup")</span>'),
    ('<span class="badge bg-warning text-dark"><i class="bi bi-question-circle"></i> Soru</span>', '<span class="badge bg-warning text-dark"><i class="bi bi-question-circle"></i> @Html.T("Common.Question", "Soru")</span>'),
    ('<span class="text-muted">Tum checklist</span>', '<span class="text-muted">@Html.T("Training.AllChecklist", "Tum checklist")</span>'),

    # Navigation buttons
    ('Devam <i class="bi bi-arrow-right"></i>', '@Html.T("Common.Continue", "Devam") <i class="bi bi-arrow-right"></i>'),
    ('Personelleri Listele <i class="bi bi-arrow-right"></i>', '@Html.T("Training.ListPersonnel", "Personelleri Listele") <i class="bi bi-arrow-right"></i>'),
    ('Ozete Git <i class="bi bi-arrow-right"></i>', '@Html.T("Training.GoToSummary", "Ozete Git") <i class="bi bi-arrow-right"></i>'),

    # Step 2
    ('<label class="form-label">Musteri</label>', '<label class="form-label">@Html.T("Common.Customer", "Musteri")</label>'),
    ("optionsCaption: 'Tum musteriler...'", """optionsCaption: '@Html.T("Common.AllCustomers", "Tum musteriler...")'"""),
    ('<label class="form-label">Organizasyon</label>', '<label class="form-label">@Html.T("Common.Organization", "Organizasyon")</label>'),
    ("optionsCaption: 'Tum organizasyonlar...'", """optionsCaption: '@Html.T("Common.AllOrganizations", "Tum organizasyonlar...")'"""),
    ("optionsCaption: 'Tum projeler...'", """optionsCaption: '@Html.T("Common.AllProjects", "Tum projeler...")'"""),
    ('placeholder="Baslangic"', 'placeholder="@Html.T("Common.StartDate", "Baslangic")"'),
    ('placeholder="Bitis"', 'placeholder="@Html.T("Common.EndDate", "Bitis")"'),

    # Quick date dropdowns
    ("? dateRangeLabels[formDateRangeType()] : 'Hizli Sec'", """? dateRangeLabels[formDateRangeType()] : '@Html.T("Common.QuickSelect", "Hizli Sec")'"""),
    ('<li><h6 class="dropdown-header">Gun</h6></li>', '<li><h6 class="dropdown-header">@Html.T("Common.Day", "Gun")</h6></li>'),
    ('<li><h6 class="dropdown-header">Hafta</h6></li>', '<li><h6 class="dropdown-header">@Html.T("Common.Week", "Hafta")</h6></li>'),
    ('<li><h6 class="dropdown-header">Ay</h6></li>', '<li><h6 class="dropdown-header">@Html.T("Common.Month", "Ay")</h6></li>'),
    ('<li><h6 class="dropdown-header">Yil</h6></li>', '<li><h6 class="dropdown-header">@Html.T("Common.Year", "Yil")</h6></li>'),
    ('">Bugun</a></li>', '">@Html.T("Common.Today", "Bugun")</a></li>'),
    ('">Dun</a></li>', '">@Html.T("Common.Yesterday", "Dun")</a></li>'),
    ('">Bu Hafta</a></li>', '">@Html.T("Common.ThisWeek", "Bu Hafta")</a></li>'),
    ('">Gecen Hafta</a></li>', '">@Html.T("Common.LastWeek", "Gecen Hafta")</a></li>'),
    ('">Bu Ay</a></li>', '">@Html.T("Common.ThisMonth", "Bu Ay")</a></li>'),
    ('">Gecen Ay</a></li>', '">@Html.T("Common.LastMonth", "Gecen Ay")</a></li>'),
    ('">Son 3 Ay</a></li>', '">@Html.T("Common.Last3Months", "Son 3 Ay")</a></li>'),
    ('">Son 6 Ay</a></li>', '">@Html.T("Common.Last6Months", "Son 6 Ay")</a></li>'),
    ('">Bu Yil</a></li>', '">@Html.T("Common.ThisYear", "Bu Yil")</a></li>'),
    ('">Gecen Yil</a></li>', '">@Html.T("Common.LastYear", "Gecen Yil")</a></li>'),

    # Score range
    ('<span class="input-group-text">Min</span>', '<span class="input-group-text">@Html.T("Common.Min", "Min")</span>'),
    ('<span class="input-group-text">Max</span>', '<span class="input-group-text">@Html.T("Common.Max", "Max")</span>'),
    ('Bu puan araligindaki personeller listelenecek', '@Html.T("Training.ScoreRangeDescription", "Bu puan araligindaki personeller listelenecek")'),

    # Step 3
    ('placeholder="Isim ile ara..."', 'placeholder="@Html.T("Common.SearchByName", "Isim ile ara...")"'),
    ("+ ' secildi'", """+ ' @Html.T("Common.Selected", "secildi")'"""),
    ("+ ' gosteriliyor'", """+ ' @Html.T("Common.Showing", "gosteriliyor")'"""),
    ('<i class="bi bi-check-all"></i> Max puan altini sec', '<i class="bi bi-check-all"></i> @Html.T("Training.SelectBelowThreshold", "Max puan altini sec")'),
    ('Bu kriterlere uyan personel bulunamadi', '@Html.T("Training.NoMatchingPersonnel", "Bu kriterlere uyan personel bulunamadi")'),

    # Step 4 - Summary
    ('<i class="bi bi-info-circle me-2"></i>Temel Bilgiler', '<i class="bi bi-info-circle me-2"></i>@Html.T("Training.BasicInfo", "Temel Bilgiler")'),
    ('<th>Video:</th>', '<th>@Html.T("Training.Video", "Video"):</th>'),
    ('<th>Baslangic:</th>', '<th>@Html.T("Common.StartDate", "Baslangic"):</th>'),
    ('<th>Bitis:</th>', '<th>@Html.T("Common.EndDate", "Bitis"):</th>'),
    ('<i class="bi bi-gear me-2"></i>Izleme Ayarlari', '<i class="bi bi-gear me-2"></i>@Html.T("Training.WatchSettings", "Izleme Ayarlari")'),
    ('<th style="width: 40%;">Min Izleme:</th>', '<th style="width: 40%;">@Html.T("Training.MinWatch", "Min Izleme"):</th>'),
    ('<th>Max Izleme:</th>', '<th>@Html.T("Training.MaxWatch", "Max Izleme"):</th>'),
    ("+ ' kere'", """+ ' @Html.T("Common.Times", "kere")'"""),
    ("'Sinirsiz'", """'@Html.T("Training.Unlimited", "Sinirsiz")'"""),
    ('<th>Hiz Degisimi:</th>', '<th>@Html.T("Training.SpeedChange", "Hiz Degisimi"):</th>'),
    ('class="badge bg-success">Izin var</span>', 'class="badge bg-success">@Html.T("Training.Allowed", "Izin var")</span>'),
    ('class="badge bg-danger">Izin yok</span>', 'class="badge bg-danger">@Html.T("Training.NotAllowed", "Izin yok")</span>'),
    ('<i class="bi bi-envelope me-2"></i>Email Ayarlari', '<i class="bi bi-envelope me-2"></i>@Html.T("Training.EmailSettings", "Email Ayarlari")'),
    ('<th style="width: 40%;">Email Gonder:</th>', '<th style="width: 40%;">@Html.T("Training.SendEmail", "Email Gonder"):</th>'),
    ('class="badge bg-secondary">Hayir</span>', 'class="badge bg-secondary">@Html.T("Common.No", "Hayir")</span>'),
    ('<th>Sablon:</th>', '<th>@Html.T("Training.Template", "Sablon"):</th>'),
    ("|| 'Varsayilan'", """|| '@Html.T("Training.Default", "Varsayilan")'"""),
    ('<span class="text-muted">kisi</span>', '<span class="text-muted">@Html.T("Common.Person", "kisi")</span>'),
    ('kisi daha', '@Html.T("Training.MorePersons", "kisi daha")'),
    ('Lutfen yukaridaki bilgileri kontrol edin. "Atamayi Olustur" butonuna tikladiginizda islem geri alinamaz.', '@Html.T("Training.CreateWarning", "Lutfen yukaridaki bilgileri kontrol edin. Atamayi Olustur butonuna tikladiginizda islem geri alinamaz.")'),
    ('<span data-bind="visible: !isSaving()"><i class="bi bi-check-lg me-1"></i> Atamayi Olustur</span>', '<span data-bind="visible: !isSaving()"><i class="bi bi-check-lg me-1"></i> @Html.T("Training.CreateAssignment", "Atamayi Olustur")</span>'),
    ('<span data-bind="visible: isSaving"><span class="spinner-border spinner-border-sm"></span> Olusturuluyor...</span>', '<span data-bind="visible: isSaving"><span class="spinner-border spinner-border-sm"></span> @Html.T("Training.Creating", "Olusturuluyor...")</span>'),

    # Participants modal
    ('<h5 class="modal-title"><i class="bi bi-people me-2"></i>Katilimcilar</h5>', '<h5 class="modal-title"><i class="bi bi-people me-2"></i>@Html.T("Training.Participants", "Katilimcilar")</h5>'),

    # Email management modal
    ('<i class="bi bi-envelope me-2"></i>Email Yonetimi', '<i class="bi bi-envelope me-2"></i>@Html.T("Training.EmailManagement", "Email Yonetimi")'),
    ('<h6 class="border-bottom pb-2 mb-3"><i class="bi bi-funnel me-2"></i>Filtreler</h6>', '<h6 class="border-bottom pb-2 mb-3"><i class="bi bi-funnel me-2"></i>@Html.T("Common.Filters", "Filtreler")</h6>'),
    ('<label class="form-label">Email Durumu</label>', '<label class="form-label">@Html.T("Training.EmailStatus", "Email Durumu")</label>'),
    ('<option value="">Tumu</option>', '<option value="">@Html.T("Common.All", "Tumu")</option>'),
    ('<option value="true">Email Gitmis</option>', '<option value="true">@Html.T("Training.EmailSent", "Email Gitmis")</option>'),
    ('<option value="false">Email Gitmemis</option>', '<option value="false">@Html.T("Training.EmailNotSent", "Email Gitmemis")</option>'),
    ('<label class="form-label">Izleme Durumu</label>', '<label class="form-label">@Html.T("Training.WatchStatus", "Izleme Durumu")</label>'),
    ('<option value="true">Baslamis</option>', '<option value="true">@Html.T("Training.Started", "Baslamis")</option>'),
    ('<option value="false">Baslamadi</option>', '<option value="false">@Html.T("Training.NotStarted", "Baslamadi")</option>'),
    ('<option value="true">Tamamladi</option>', '<option value="true">@Html.T("Training.Completed", "Tamamladi")</option>'),
    ('<option value="false">Tamamlamadi</option>', '<option value="false">@Html.T("Training.NotCompleted", "Tamamlamadi")</option>'),
    ('<label class="form-label">Email Turu</label>', '<label class="form-label">@Html.T("Training.EmailType", "Email Turu")</label>'),
    ('<option value="1">Ilk Bildirim</option>', '<option value="1">@Html.T("Training.FirstNotification", "Ilk Bildirim")</option>'),
    ('<option value="2">Hatirlatma</option>', '<option value="2">@Html.T("Training.Reminder", "Hatirlatma")</option>'),
    ('<i class="bi bi-check-all"></i> Tumunu Sec', '<i class="bi bi-check-all"></i> @Html.T("Common.SelectAll", "Tumunu Sec")'),
    ("+ ' secili'", """+ ' @Html.T("Common.Selected", "secili")'"""),
    ('<i class="bi bi-send me-1"></i> Email Gonder (<span', '<i class="bi bi-send me-1"></i> @Html.T("Training.SendEmail", "Email Gonder") (<span'),
    ('<span data-bind="visible: isSendingEmails"><span class="spinner-border spinner-border-sm"></span> Gonderiliyor...</span>', '<span data-bind="visible: isSendingEmails"><span class="spinner-border spinner-border-sm"></span> @Html.T("Training.Sending", "Gonderiliyor...")</span>'),

    # Edit modal
    ('<i class="bi bi-gear me-1"></i>Genel', '<i class="bi bi-gear me-1"></i>@Html.T("Common.General", "Genel")'),
    ('<i class="bi bi-people me-1"></i>Katilimcilar', '<i class="bi bi-people me-1"></i>@Html.T("Training.Participants", "Katilimcilar")'),
    ('<label class="form-label">Baslik <span class="text-danger">*</span></label>', '<label class="form-label">@Html.T("Common.Title", "Baslik") <span class="text-danger">*</span></label>'),
    ("optionsCaption: 'Sablon secin...'", """optionsCaption: '@Html.T("Training.SelectTemplate", "Sablon secin...")'"""),
    ('<label class="form-label">Min. Izleme Sayisi</label>', '<label class="form-label">@Html.T("Training.MinWatchCount", "Min. Izleme Sayisi")</label>'),
    ('<label class="form-label">Max. Izleme Sayisi</label>', '<label class="form-label">@Html.T("Training.MaxWatchCount", "Max. Izleme Sayisi")</label>'),
    ('<label class="form-check-label">Hiz degistirmeye izin ver</label>', '<label class="form-check-label">@Html.T("Training.AllowSpeedChange", "Hiz degistirmeye izin ver")</label>'),
    ('<label class="form-check-label">Ileri/geri sarmaya izin ver</label>', '<label class="form-check-label">@Html.T("Training.AllowSeeking", "Ileri/geri sarmaya izin ver")</label>'),
    ('<i class="bi bi-person me-1"></i>Ic Katilimcilar', '<i class="bi bi-person me-1"></i>@Html.T("Training.InternalParticipants", "Ic Katilimcilar")'),
    ('<i class="bi bi-person-badge me-1"></i>Dis Katilimcilar', '<i class="bi bi-person-badge me-1"></i>@Html.T("Training.ExternalParticipants", "Dis Katilimcilar")'),
    ('placeholder="Video kapsaminda dinlemesi olan personel ara..."', 'placeholder="@Html.T("Training.SearchPersonnelInScope", "Video kapsaminda dinlemesi olan personel ara...")"'),
    ('<small class="text-success">Eklenecek:</small>', '<small class="text-success">@Html.T("Training.ToBeAdded", "Eklenecek"):</small>'),
    ('placeholder="Email *"', 'placeholder="@Html.T("Common.Email", "Email") *"'),
    ('placeholder="Ad"', 'placeholder="@Html.T("Common.FirstName", "Ad")"'),
    ('placeholder="Soyad"', 'placeholder="@Html.T("Common.LastName", "Soyad")"'),

    # Status in data-bind text
    ("text: isCompleted ? 'Tamamladi' : (statusId === 2 ? 'Izliyor' : 'Bekliyor')", """text: isCompleted ? '@Html.T("Training.Completed", "Tamamladi")' : (statusId === 2 ? '@Html.T("Training.Watching", "Izliyor")' : '@Html.T("Training.Waiting", "Bekliyor")')"""),

    # Participant status options
    ('<option value="1">Bekliyor</option>', '<option value="1">@Html.T("Training.Waiting", "Bekliyor")</option>'),
    ('<option value="2">Izliyor</option>', '<option value="2">@Html.T("Training.Watching", "Izliyor")</option>'),
    ('<option value="3">Tamamladi</option>', '<option value="3">@Html.T("Training.Completed", "Tamamladi")</option>'),

    # Edit labels (need specific match due to multiple similar)
    ('<label class="form-label">Tamamlama</label>', '<label class="form-label">@Html.T("Training.Completion", "Tamamlama")</label>'),

    # Remaining items
    ('<i class="bi bi-people me-2"></i>Katilimcilar', '<i class="bi bi-people me-2"></i>@Html.T("Training.Participants", "Katilimcilar")'),
    ('Video kapsamindaki checklistlerde degerlendirmesi olan', '@Html.T("Training.CustomersWithEvaluations", "Video kapsamindaki checklistlerde degerlendirmesi olan")'),
    ('Video kapsamindaki', '@Html.T("Training.InVideoScope", "Video kapsamindaki")'),
]

count = 0
for old, new in replacements:
    if old in content:
        content = content.replace(old, new)
        count += 1

with open(filepath, 'w', encoding='utf-8') as f:
    f.write(content)

print(f'Replaced {count}/{len(replacements)} patterns')
