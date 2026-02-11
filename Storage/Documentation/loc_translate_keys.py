#!/usr/bin/env python3
"""Translate 148 new localization keys to TR/EN/DE/ES and update XML files"""
import re, os, sys
sys.stdout.reconfigure(encoding='utf-8')

base = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
loc_dir = os.path.join(base, 'Backend', 'SecretCustomer.API', 'App_Data', 'Localization')

# Key -> {tr, en, de, es} translations
translations = {
    "Checklist.CriteriaTotal": {
        "tr": "Kriter Toplam", "en": "Criteria Total", "de": "Kriteriensumme", "es": "Total de Criterios"
    },
    "Checklist.Maximum": {
        "tr": "Maksimum", "en": "Maximum", "de": "Maximum", "es": "Máximo"
    },
    "Checklist.Survey": {
        "tr": "Anket", "en": "Survey", "de": "Umfrage", "es": "Encuesta"
    },
    "Common.Adding": {
        "tr": "Ekleniyor...", "en": "Adding...", "de": "Wird hinzugefügt...", "es": "Añadiendo..."
    },
    "Common.AllCustomers": {
        "tr": "Tüm Müşteriler", "en": "All Customers", "de": "Alle Kunden", "es": "Todos los Clientes"
    },
    "Common.AllOrganizations": {
        "tr": "Tüm Şubeler", "en": "All Organizations", "de": "Alle Organisationen", "es": "Todas las Organizaciones"
    },
    "Common.AllPersonnel": {
        "tr": "Tüm Personel", "en": "All Personnel", "de": "Gesamtes Personal", "es": "Todo el Personal"
    },
    "Common.AllProjects": {
        "tr": "Tüm Projeler", "en": "All Projects", "de": "Alle Projekte", "es": "Todos los Proyectos"
    },
    "Common.Checklist": {
        "tr": "Kontrol Listesi", "en": "Checklist", "de": "Checkliste", "es": "Lista de Verificación"
    },
    "Common.Clear": {
        "tr": "Temizle", "en": "Clear", "de": "Löschen", "es": "Limpiar"
    },
    "Common.Continue": {
        "tr": "Devam", "en": "Continue", "de": "Weiter", "es": "Continuar"
    },
    "Common.Customer": {
        "tr": "Müşteri", "en": "Customer", "de": "Kunde", "es": "Cliente"
    },
    "Common.DueDate": {
        "tr": "Bitiş Tarihi", "en": "Due Date", "de": "Fälligkeitsdatum", "es": "Fecha de Vencimiento"
    },
    "Common.Email": {
        "tr": "E-posta", "en": "Email", "de": "E-Mail", "es": "Correo Electrónico"
    },
    "Common.EmailNameSurname": {
        "tr": "E-posta / Ad Soyad", "en": "Email / Full Name", "de": "E-Mail / Vor- und Nachname", "es": "Correo / Nombre Completo"
    },
    "Common.EndDate": {
        "tr": "Bitiş", "en": "End Date", "de": "Enddatum", "es": "Fecha Fin"
    },
    "Common.Error": {
        "tr": "hata", "en": "error", "de": "Fehler", "es": "error"
    },
    "Common.EvaluationSuffix": {
        "tr": "Değerlendirme", "en": "Evaluation", "de": "Bewertung", "es": "Evaluación"
    },
    "Common.Fetch": {
        "tr": "Getir", "en": "Fetch", "de": "Abrufen", "es": "Obtener"
    },
    "Common.FirstName": {
        "tr": "Ad", "en": "First Name", "de": "Vorname", "es": "Nombre"
    },
    "Common.FullName": {
        "tr": "Ad Soyad", "en": "Full Name", "de": "Vor- und Nachname", "es": "Nombre Completo"
    },
    "Common.General": {
        "tr": "Genel", "en": "General", "de": "Allgemein", "es": "General"
    },
    "Common.Group": {
        "tr": "Grup", "en": "Group", "de": "Gruppe", "es": "Grupo"
    },
    "Common.LastName": {
        "tr": "Soyad", "en": "Last Name", "de": "Nachname", "es": "Apellido"
    },
    "Common.Max": {
        "tr": "Maks", "en": "Max", "de": "Max", "es": "Máx"
    },
    "Common.Min": {
        "tr": "Min", "en": "Min", "de": "Min", "es": "Mín"
    },
    "Common.NextMonth": {
        "tr": "Gelecek Ay", "en": "Next Month", "de": "Nächster Monat", "es": "Próximo Mes"
    },
    "Common.NextWeek": {
        "tr": "Gelecek Hafta", "en": "Next Week", "de": "Nächste Woche", "es": "Próxima Semana"
    },
    "Common.Next3Months": {
        "tr": "Gelecek 3 Ay", "en": "Next 3 Months", "de": "Nächste 3 Monate", "es": "Próximos 3 Meses"
    },
    "Common.No": {
        "tr": "Hayır", "en": "No", "de": "Nein", "es": "No"
    },
    "Common.Organization": {
        "tr": "Organizasyon", "en": "Organization", "de": "Organisation", "es": "Organización"
    },
    "Common.OrganizationOptional": {
        "tr": "Organizasyon (Opsiyonel)", "en": "Organization (Optional)", "de": "Organisation (Optional)", "es": "Organización (Opcional)"
    },
    "Common.Passive": {
        "tr": "Pasif", "en": "Inactive", "de": "Inaktiv", "es": "Inactivo"
    },
    "Common.Person": {
        "tr": "kişi", "en": "person", "de": "Person", "es": "persona"
    },
    "Common.PersonnelNamePlaceholder": {
        "tr": "Personel adı...", "en": "Personnel name...", "de": "Personalname...", "es": "Nombre del personal..."
    },
    "Common.PersonnelSuffix": {
        "tr": "Personel", "en": "Personnel", "de": "Personal", "es": "Personal"
    },
    "Common.Question": {
        "tr": "Soru", "en": "Question", "de": "Frage", "es": "Pregunta"
    },
    "Common.QuickSelect": {
        "tr": "Hızlı Seç", "en": "Quick Select", "de": "Schnellauswahl", "es": "Selección Rápida"
    },
    "Common.Record": {
        "tr": "kayıt", "en": "record", "de": "Datensatz", "es": "registro"
    },
    "Common.SearchByName": {
        "tr": "İsim Ara", "en": "Search by Name", "de": "Nach Name suchen", "es": "Buscar por Nombre"
    },
    "Common.SelectAll": {
        "tr": "Tümünü Seç", "en": "Select All", "de": "Alle auswählen", "es": "Seleccionar Todo"
    },
    "Common.SelectCustomer": {
        "tr": "Müşteri Seçin...", "en": "Select Customer...", "de": "Kunde auswählen...", "es": "Seleccionar Cliente..."
    },
    "Common.SelectFilter": {
        "tr": "Filtre Seçin...", "en": "Select Filter...", "de": "Filter auswählen...", "es": "Seleccionar Filtro..."
    },
    "Common.SelectStatus": {
        "tr": "Durum seçin...", "en": "Select status...", "de": "Status auswählen...", "es": "Seleccionar estado..."
    },
    "Common.SelectType": {
        "tr": "Tip seçin...", "en": "Select type...", "de": "Typ auswählen...", "es": "Seleccionar tipo..."
    },
    "Common.Selected": {
        "tr": "seçildi", "en": "selected", "de": "ausgewählt", "es": "seleccionado"
    },
    "Common.Showing": {
        "tr": "gösteriliyor", "en": "showing", "de": "angezeigt", "es": "mostrando"
    },
    "Common.StartDate": {
        "tr": "Başlangıç Tarihi", "en": "Start Date", "de": "Startdatum", "es": "Fecha de Inicio"
    },
    "Common.Times": {
        "tr": "kere", "en": "times", "de": "mal", "es": "veces"
    },
    "Common.Title": {
        "tr": "Başlık", "en": "Title", "de": "Titel", "es": "Título"
    },
    "Common.Tomorrow": {
        "tr": "Yarın", "en": "Tomorrow", "de": "Morgen", "es": "Mañana"
    },
    "Common.Type": {
        "tr": "Tip", "en": "Type", "de": "Typ", "es": "Tipo"
    },
    "Common.User": {
        "tr": "Kullanıcı", "en": "User", "de": "Benutzer", "es": "Usuario"
    },
    "Dealer.ContactPlaceholder": {
        "tr": "Ahmet Yılmaz", "en": "John Doe", "de": "Max Mustermann", "es": "Juan García"
    },
    "Dealer.NamePlaceholder": {
        "tr": "ABC Bayi", "en": "ABC Dealer", "de": "ABC Händler", "es": "Distribuidor ABC"
    },
    "Dealer.NotSpecified": {
        "tr": "Bayi Belirtilmemiş", "en": "Dealer Not Specified", "de": "Händler nicht angegeben", "es": "Distribuidor No Especificado"
    },
    "Evaluation.NotAnswered": {
        "tr": "Cevaplanmadı", "en": "Not Answered", "de": "Nicht beantwortet", "es": "Sin Respuesta"
    },
    "Evaluation.NotRequired": {
        "tr": "Gerekmedi", "en": "Not Required", "de": "Nicht erforderlich", "es": "No Requerido"
    },
    "Evaluation.Weight": {
        "tr": "Ağırlık", "en": "Weight", "de": "Gewichtung", "es": "Peso"
    },
    "Organization.OrgManager": {
        "tr": "Organizasyon Yöneticisi", "en": "Organization Manager", "de": "Organisationsleiter", "es": "Gerente de Organización"
    },
    "Project.ProjectCode": {
        "tr": "Proje Kodu", "en": "Project Code", "de": "Projektcode", "es": "Código de Proyecto"
    },
    "Project.ProjectType": {
        "tr": "Proje Tipi", "en": "Project Type", "de": "Projekttyp", "es": "Tipo de Proyecto"
    },
    "Report.EvaluationComment": {
        "tr": "Denetim Yorumu", "en": "Evaluation Comment", "de": "Bewertungskommentar", "es": "Comentario de Evaluación"
    },
    "Report.GeneralNote": {
        "tr": "Genel Not", "en": "General Note", "de": "Allgemeine Notiz", "es": "Nota General"
    },
    "Report.Reason": {
        "tr": "Sebep", "en": "Reason", "de": "Grund", "es": "Razón"
    },
    "Report.SelectedTotalEvaluation": {
        "tr": "Seçilen / Toplam değerlendirme", "en": "Selected / Total evaluations", "de": "Ausgewählt / Gesamtbewertungen", "es": "Seleccionadas / Total evaluaciones"
    },
    "Report.SubCriteria": {
        "tr": "Alt Kriterler", "en": "Sub Criteria", "de": "Unterkriterien", "es": "Subcriterios"
    },
    "Report.Success": {
        "tr": "Başarı", "en": "Success", "de": "Erfolg", "es": "Éxito"
    },
    "Report.TopSubCriteria": {
        "tr": "En Çok Seçilen Alt Kriterler", "en": "Most Selected Sub Criteria", "de": "Am häufigsten ausgewählte Unterkriterien", "es": "Subcriterios Más Seleccionados"
    },
    "Report.Warning": {
        "tr": "Uyarı", "en": "Warning", "de": "Warnung", "es": "Advertencia"
    },
    "Smtp.AppPassword": {
        "tr": "Uygulama Şifresi", "en": "App Password", "de": "App-Passwort", "es": "Contraseña de Aplicación"
    },
    "Smtp.CreateIt": {
        "tr": "oluşturun", "en": "create one", "de": "erstellen", "es": "cree una"
    },
    "Smtp.NormalPasswordOk": {
        "tr": "Normal şifre kullanabilirsiniz", "en": "You can use a regular password", "de": "Sie können ein normales Passwort verwenden", "es": "Puede usar una contraseña normal"
    },
    "SurveyResult.DetailedReport": {
        "tr": "Detay Raporu", "en": "Detailed Report", "de": "Detailbericht", "es": "Informe Detallado"
    },
    "SurveyResult.GroupBased": {
        "tr": "Grup Bazlı Puan", "en": "Group Based Score", "de": "Gruppenbasierte Punktzahl", "es": "Puntuación por Grupo"
    },
    "SurveyResult.QuestionBased": {
        "tr": "Soru İstatistik", "en": "Question Statistics", "de": "Fragenstatistik", "es": "Estadísticas de Preguntas"
    },
    "Training.AddEmailsFromLeft": {
        "tr": "Sol taraftan email adreslerini ekleyebilirsiniz", "en": "You can add email addresses from the left panel", "de": "Sie können E-Mail-Adressen vom linken Bereich hinzufügen", "es": "Puede agregar direcciones de correo desde el panel izquierdo"
    },
    "Training.AddNewExternalParticipant": {
        "tr": "Yeni Dış Katılımcı Ekle", "en": "Add New External Participant", "de": "Neuen externen Teilnehmer hinzufügen", "es": "Agregar Nuevo Participante Externo"
    },
    "Training.AddNewParticipant": {
        "tr": "Yeni Katılımcı Ekle", "en": "Add New Participant", "de": "Neuen Teilnehmer hinzufügen", "es": "Agregar Nuevo Participante"
    },
    "Training.AllChecklist": {
        "tr": "Tüm kontrol listesi", "en": "Entire checklist", "de": "Gesamte Checkliste", "es": "Lista completa"
    },
    "Training.AllParticipants": {
        "tr": "Tüm Katılımcılar", "en": "All Participants", "de": "Alle Teilnehmer", "es": "Todos los Participantes"
    },
    "Training.AllowSeeking": {
        "tr": "İleri/geri sarmaya izin ver", "en": "Allow seeking forward/backward", "de": "Vor-/Zurückspulen erlauben", "es": "Permitir avanzar/retroceder"
    },
    "Training.AllowSpeedChange": {
        "tr": "Hız değiştirmeye izin ver", "en": "Allow speed change", "de": "Geschwindigkeitsänderung erlauben", "es": "Permitir cambio de velocidad"
    },
    "Training.Allowed": {
        "tr": "İzin var", "en": "Allowed", "de": "Erlaubt", "es": "Permitido"
    },
    "Training.Assignment": {
        "tr": "Atama", "en": "Assignment", "de": "Zuweisung", "es": "Asignación"
    },
    "Training.AssignmentNamePlaceholder": {
        "tr": "Atama adı...", "en": "Assignment name...", "de": "Zuweisungsname...", "es": "Nombre de asignación..."
    },
    "Training.AssignmentSummary": {
        "tr": "Atama Özeti", "en": "Assignment Summary", "de": "Zuweisungszusammenfassung", "es": "Resumen de Asignación"
    },
    "Training.AssignmentTitlePlaceholder": {
        "tr": "örn: Ocak 2026 Aktif Dinleme Eğitimi", "en": "e.g.: Jan 2026 Active Listening Training", "de": "z.B.: Jan 2026 Aktives Zuhören Training", "es": "ej.: Ene 2026 Capacitación Escucha Activa"
    },
    "Training.Assignments": {
        "tr": "Atamalar", "en": "Assignments", "de": "Zuweisungen", "es": "Asignaciones"
    },
    "Training.AvgScore": {
        "tr": "Ort. Puan", "en": "Avg. Score", "de": "Durchschn. Punktzahl", "es": "Punt. Prom."
    },
    "Training.BackToVideos": {
        "tr": "Videolara Dön", "en": "Back to Videos", "de": "Zurück zu Videos", "es": "Volver a Videos"
    },
    "Training.BasicInfo": {
        "tr": "Temel Bilgiler", "en": "Basic Information", "de": "Grundinformationen", "es": "Información Básica"
    },
    "Training.Completed": {
        "tr": "Tamamladı", "en": "Completed", "de": "Abgeschlossen", "es": "Completado"
    },
    "Training.Completion": {
        "tr": "Tamamlama", "en": "Completion", "de": "Abschluss", "es": "Finalización"
    },
    "Training.CopyLink": {
        "tr": "Link Kopyala", "en": "Copy Link", "de": "Link kopieren", "es": "Copiar Enlace"
    },
    "Training.CreateAssignment": {
        "tr": "Atamayı Oluştur", "en": "Create Assignment", "de": "Zuweisung erstellen", "es": "Crear Asignación"
    },
    "Training.CreateFirstAssignment": {
        "tr": "İlk Atamayı Oluştur", "en": "Create First Assignment", "de": "Erste Zuweisung erstellen", "es": "Crear Primera Asignación"
    },
    "Training.CreateWarning": {
        "tr": "Lütfen yukarıdaki bilgileri kontrol edin. Atamayı Oluştur butonuna tıkladığınızda işlem geri alınamaz.",
        "en": "Please review the information above. This action cannot be undone once you click Create Assignment.",
        "de": "Bitte überprüfen Sie die obigen Informationen. Diese Aktion kann nicht rückgängig gemacht werden.",
        "es": "Por favor revise la información anterior. Esta acción no se puede deshacer."
    },
    "Training.Creating": {
        "tr": "Oluşturuluyor...", "en": "Creating...", "de": "Wird erstellt...", "es": "Creando..."
    },
    "Training.CustomersWithEvaluationsPrefix": {
        "tr": "Video kapsamındaki kontrol listelerinde değerlendirmesi olan",
        "en": "Customers with evaluations in checklists within video scope",
        "de": "Kunden mit Bewertungen in Checklisten im Videoumfang",
        "es": "Clientes con evaluaciones en listas de verificación del alcance del video"
    },
    "Training.Default": {
        "tr": "Varsayılan", "en": "Default", "de": "Standard", "es": "Predeterminado"
    },
    "Training.DefaultTemplate": {
        "tr": "Varsayılan şablon", "en": "Default template", "de": "Standardvorlage", "es": "Plantilla predeterminada"
    },
    "Training.DeleteOrUndo": {
        "tr": "Sil/Geri Al", "en": "Delete/Undo", "de": "Löschen/Rückgängig", "es": "Eliminar/Deshacer"
    },
    "Training.EditAssignment": {
        "tr": "Atama Düzenle", "en": "Edit Assignment", "de": "Zuweisung bearbeiten", "es": "Editar Asignación"
    },
    "Training.EmailFormat": {
        "tr": "Format: email veya email;ad;soyad", "en": "Format: email or email;firstname;lastname", "de": "Format: E-Mail oder E-Mail;Vorname;Nachname", "es": "Formato: email o email;nombre;apellido"
    },
    "Training.EmailManagement": {
        "tr": "Email Yönetimi", "en": "Email Management", "de": "E-Mail-Verwaltung", "es": "Gestión de Correo"
    },
    "Training.EmailNotSent": {
        "tr": "Email Gitmemiş", "en": "Email Not Sent", "de": "E-Mail nicht gesendet", "es": "Correo No Enviado"
    },
    "Training.EmailSent": {
        "tr": "Email Gitmiş", "en": "Email Sent", "de": "E-Mail gesendet", "es": "Correo Enviado"
    },
    "Training.EmailSettings": {
        "tr": "Email Ayarları", "en": "Email Settings", "de": "E-Mail-Einstellungen", "es": "Configuración de Correo"
    },
    "Training.EmailStatus": {
        "tr": "Email Durumu", "en": "Email Status", "de": "E-Mail-Status", "es": "Estado del Correo"
    },
    "Training.EmailTemplate": {
        "tr": "Email Şablonu", "en": "Email Template", "de": "E-Mail-Vorlage", "es": "Plantilla de Correo"
    },
    "Training.EmailType": {
        "tr": "Email Türü", "en": "Email Type", "de": "E-Mail-Typ", "es": "Tipo de Correo"
    },
    "Training.EmptyMeansUnlimited": {
        "tr": "Boş = sınırsız", "en": "Empty = unlimited", "de": "Leer = unbegrenzt", "es": "Vacío = ilimitado"
    },
    "Training.EnterEmailsFromLeft": {
        "tr": "Sol taraftan email adreslerini girin", "en": "Enter email addresses from the left panel", "de": "Geben Sie E-Mail-Adressen im linken Bereich ein", "es": "Ingrese direcciones de correo desde el panel izquierdo"
    },
    "Training.EvaluationDateRange": {
        "tr": "Değerlendirme Tarih Aralığı", "en": "Evaluation Date Range", "de": "Bewertungszeitraum", "es": "Rango de Fechas de Evaluación"
    },
    "Training.ExcelColumns": {
        "tr": "Kolonlar: Email, Ad, Soyad", "en": "Columns: Email, First Name, Last Name", "de": "Spalten: E-Mail, Vorname, Nachname", "es": "Columnas: Email, Nombre, Apellido"
    },
    "Training.ExistingParticipants": {
        "tr": "Mevcut Katılımcılar", "en": "Existing Participants", "de": "Bestehende Teilnehmer", "es": "Participantes Existentes"
    },
    "Training.External": {
        "tr": "Dış", "en": "External", "de": "Extern", "es": "Externo"
    },
    "Training.ExternalAssignmentPlaceholder": {
        "tr": "örn: Ocak 2026 Dış Eğitim", "en": "e.g.: Jan 2026 External Training", "de": "z.B.: Jan 2026 Externe Schulung", "es": "ej.: Ene 2026 Capacitación Externa"
    },
    "Training.ExternalParticipants": {
        "tr": "Dış Katılımcılar", "en": "External Participants", "de": "Externe Teilnehmer", "es": "Participantes Externos"
    },
    "Training.FirstNotification": {
        "tr": "İlk Bildirim", "en": "First Notification", "de": "Erste Benachrichtigung", "es": "Primera Notificación"
    },
    "Training.ForwardBackward": {
        "tr": "İleri/Geri Sarma", "en": "Forward/Backward", "de": "Vor-/Zurückspulen", "es": "Avanzar/Retroceder"
    },
    "Training.GoToSummary": {
        "tr": "Özete Git", "en": "Go to Summary", "de": "Zur Zusammenfassung", "es": "Ir al Resumen"
    },
    "Training.GroupQuestion": {
        "tr": "Grup/Soru", "en": "Group/Question", "de": "Gruppe/Frage", "es": "Grupo/Pregunta"
    },
    "Training.InVideoScopePrefix": {
        "tr": "Video kapsamındaki", "en": "Within video scope", "de": "Im Videoumfang", "es": "En el alcance del video"
    },
    "Training.Internal": {
        "tr": "İç", "en": "Internal", "de": "Intern", "es": "Interno"
    },
    "Training.InternalParticipants": {
        "tr": "İç Katılımcılar", "en": "Internal Participants", "de": "Interne Teilnehmer", "es": "Participantes Internos"
    },
    "Training.LastEmail": {
        "tr": "Son Email", "en": "Last Email", "de": "Letzte E-Mail", "es": "Último Correo"
    },
    "Training.ListPersonnel": {
        "tr": "Personelleri Listele", "en": "List Personnel", "de": "Personal auflisten", "es": "Listar Personal"
    },
    "Training.LoadingParticipants": {
        "tr": "Katılımcılar yükleniyor...", "en": "Loading participants...", "de": "Teilnehmer werden geladen...", "es": "Cargando participantes..."
    },
    "Training.MaxWatch": {
        "tr": "Maks İzleme", "en": "Max Watch", "de": "Max. Ansicht", "es": "Máx Visualización"
    },
    "Training.MaxWatchCount": {
        "tr": "Maks İzleme Sayısı", "en": "Max Watch Count", "de": "Max. Anzahl der Ansichten", "es": "Máx Número de Visualizaciones"
    },
    "Training.MinWatch": {
        "tr": "Min İzleme", "en": "Min Watch", "de": "Min. Ansicht", "es": "Mín Visualización"
    },
    "Training.MinWatchCount": {
        "tr": "Min İzleme Sayısı", "en": "Min Watch Count", "de": "Min. Anzahl der Ansichten", "es": "Mín Número de Visualizaciones"
    },
    "Training.MorePersons": {
        "tr": "kişi daha", "en": "more people", "de": "weitere Personen", "es": "personas más"
    },
    "Training.NewAssignment": {
        "tr": "Yeni Atama", "en": "New Assignment", "de": "Neue Zuweisung", "es": "Nueva Asignación"
    },
    "Training.NoAssignmentsYet": {
        "tr": "Henüz eğitim ataması bulunmuyor.", "en": "No training assignments yet.", "de": "Noch keine Schulungszuweisungen vorhanden.", "es": "Aún no hay asignaciones de capacitación."
    },
    "Training.NoExternalParticipantsYet": {
        "tr": "Henüz dış katılımcı yok.", "en": "No external participants yet.", "de": "Noch keine externen Teilnehmer.", "es": "Aún no hay participantes externos."
    },
    "Training.NoInternalParticipantsYet": {
        "tr": "Henüz iç katılımcı yok.", "en": "No internal participants yet.", "de": "Noch keine internen Teilnehmer.", "es": "Aún no hay participantes internos."
    },
    "Training.NoMatchingParticipants": {
        "tr": "Filtrelerinize uygun katılımcı bulunamadı.", "en": "No participants matching your filters.", "de": "Keine Teilnehmer entsprechen Ihren Filtern.", "es": "No se encontraron participantes con sus filtros."
    },
    "Training.NoMatchingPersonnel": {
        "tr": "Bu kriterlere uyan personel bulunamadı", "en": "No personnel matching these criteria", "de": "Kein Personal entspricht diesen Kriterien", "es": "No se encontró personal con estos criterios"
    },
    "Training.NoParticipantsYet": {
        "tr": "Henüz katılımcı yok", "en": "No participants yet", "de": "Noch keine Teilnehmer", "es": "Aún no hay participantes"
    },
    "Training.NotAllowed": {
        "tr": "İzin yok", "en": "Not Allowed", "de": "Nicht erlaubt", "es": "No Permitido"
    },
    "Training.NotCompleted": {
        "tr": "Tamamlamadı", "en": "Not Completed", "de": "Nicht abgeschlossen", "es": "No Completado"
    },
    "Training.NotStarted": {
        "tr": "Başlamadı", "en": "Not Started", "de": "Nicht gestartet", "es": "No Iniciado"
    },
    "Training.ParticipantType": {
        "tr": "Katılımcı Tipi", "en": "Participant Type", "de": "Teilnehmertyp", "es": "Tipo de Participante"
    },
    "Training.Participants": {
        "tr": "Katılımcılar", "en": "Participants", "de": "Teilnehmer", "es": "Participantes"
    },
    "Training.ParticipantsToAdd": {
        "tr": "Eklenecek Katılımcılar", "en": "Participants to Add", "de": "Hinzuzufügende Teilnehmer", "es": "Participantes a Agregar"
    },
    "Training.PersonFound": {
        "tr": "kişi bulundu", "en": "people found", "de": "Personen gefunden", "es": "personas encontradas"
    },
    "Training.PersonnelFilters": {
        "tr": "Personel Filtreleri", "en": "Personnel Filters", "de": "Personalfilter", "es": "Filtros de Personal"
    },
    "Training.PersonnelListLoading": {
        "tr": "Personel listesi yükleniyor...", "en": "Loading personnel list...", "de": "Personalliste wird geladen...", "es": "Cargando lista de personal..."
    },
    "Training.PersonnelSelection": {
        "tr": "Personel Seçimi", "en": "Personnel Selection", "de": "Personalauswahl", "es": "Selección de Personal"
    },
    "Training.Progress": {
        "tr": "İlerleme", "en": "Progress", "de": "Fortschritt", "es": "Progreso"
    },
    "Training.Reminder": {
        "tr": "Hatırlatma", "en": "Reminder", "de": "Erinnerung", "es": "Recordatorio"
    },
    "Training.ScoreCalculation": {
        "tr": "Puan hesabı:", "en": "Score calculation:", "de": "Punktberechnung:", "es": "Cálculo de puntuación:"
    },
    "Training.ScoreRange": {
        "tr": "Puan Aralığı", "en": "Score Range", "de": "Punktebereich", "es": "Rango de Puntuación"
    },
    "Training.ScoreRangeDescription": {
        "tr": "Bu puan aralığındaki personeller listelenecek", "en": "Personnel within this score range will be listed", "de": "Personal in diesem Punktebereich wird aufgelistet", "es": "Se listará el personal dentro de este rango de puntuación"
    },
    "Training.SearchPersonnelInScope": {
        "tr": "Video kapsamında dinlemesi olan personel ara...", "en": "Search personnel with evaluations in video scope...", "de": "Personal mit Bewertungen im Videoumfang suchen...", "es": "Buscar personal con evaluaciones en alcance del video..."
    },
    "Training.SearchVideo": {
        "tr": "Video ara...", "en": "Search video...", "de": "Video suchen...", "es": "Buscar video..."
    },
    "Training.SelectBelowThreshold": {
        "tr": "Maks puan altını seç", "en": "Select below max score", "de": "Unter Max-Punktzahl auswählen", "es": "Seleccionar debajo del puntaje máximo"
    },
    "Training.SelectTemplate": {
        "tr": "Şablon seçin...", "en": "Select template...", "de": "Vorlage auswählen...", "es": "Seleccionar plantilla..."
    },
    "Training.SelectVideo": {
        "tr": "Video seçin...", "en": "Select video...", "de": "Video auswählen...", "es": "Seleccionar video..."
    },
    "Training.SelectedPersonnel": {
        "tr": "Seçilen Personel", "en": "Selected Personnel", "de": "Ausgewähltes Personal", "es": "Personal Seleccionado"
    },
    "Training.SendEmail": {
        "tr": "Email Gönder", "en": "Send Email", "de": "E-Mail senden", "es": "Enviar Correo"
    },
    "Training.SendEmailToParticipants": {
        "tr": "Katılımcılara email gönder", "en": "Send email to participants", "de": "E-Mail an Teilnehmer senden", "es": "Enviar correo a participantes"
    },
    "Training.Sending": {
        "tr": "Gönderiliyor...", "en": "Sending...", "de": "Wird gesendet...", "es": "Enviando..."
    },
    "Training.SpeedChange": {
        "tr": "Hız Değişimi", "en": "Speed Change", "de": "Geschwindigkeitsänderung", "es": "Cambio de Velocidad"
    },
    "Training.Started": {
        "tr": "Başlamış", "en": "Started", "de": "Gestartet", "es": "Iniciado"
    },
    "Training.Summary": {
        "tr": "Özet", "en": "Summary", "de": "Zusammenfassung", "es": "Resumen"
    },
    "Training.Template": {
        "tr": "Şablon", "en": "Template", "de": "Vorlage", "es": "Plantilla"
    },
    "Training.ToBeAdded": {
        "tr": "Eklenecek", "en": "To be added", "de": "Wird hinzugefügt", "es": "Por agregar"
    },
    "Training.TrainingVideo": {
        "tr": "Eğitim Videosu", "en": "Training Video", "de": "Schulungsvideo", "es": "Video de Capacitación"
    },
    "Training.Unlimited": {
        "tr": "Sınırsız", "en": "Unlimited", "de": "Unbegrenzt", "es": "Ilimitado"
    },
    "Training.Video": {
        "tr": "Video", "en": "Video", "de": "Video", "es": "Video"
    },
    "Training.VideoAndSettings": {
        "tr": "Video ve Ayarlar", "en": "Video and Settings", "de": "Video und Einstellungen", "es": "Video y Configuración"
    },
    "Training.VideoScope": {
        "tr": "Video Kapsamı", "en": "Video Scope", "de": "Videoumfang", "es": "Alcance del Video"
    },
    "Training.VideoScopeDescription": {
        "tr": "Bu video aşağıdaki konularda eğitim için hazırlanmıştır.",
        "en": "This video has been prepared for training on the following topics.",
        "de": "Dieses Video wurde für die Schulung zu folgenden Themen vorbereitet.",
        "es": "Este video fue preparado para capacitación en los siguientes temas."
    },
    "Training.VideoSelection": {
        "tr": "Video Seçimi", "en": "Video Selection", "de": "Videoauswahl", "es": "Selección de Video"
    },
    "Training.ViewParticipants": {
        "tr": "Katılımcıları Gör", "en": "View Participants", "de": "Teilnehmer anzeigen", "es": "Ver Participantes"
    },
    "Training.Waiting": {
        "tr": "Bekliyor", "en": "Waiting", "de": "Wartend", "es": "Esperando"
    },
    "Training.WatchSettings": {
        "tr": "İzleme Ayarları", "en": "Watch Settings", "de": "Ansichtseinstellungen", "es": "Configuración de Visualización"
    },
    "Training.WatchStatus": {
        "tr": "İzleme Durumu", "en": "Watch Status", "de": "Ansichtsstatus", "es": "Estado de Visualización"
    },
    "Training.Watched": {
        "tr": "İzlenen", "en": "Watched", "de": "Angesehen", "es": "Visto"
    },
    "Training.Watching": {
        "tr": "İzliyor", "en": "Watching", "de": "Schaut zu", "es": "Viendo"
    },
}

# Language code to XML file mapping
lang_map = {
    "tr": "resources.tr.xml",
    "en": "resources.en.xml",
    "de": "resources.de.xml",
    "es": "resources.es.xml",
}

for lang, xml_name in lang_map.items():
    xml_path = os.path.join(loc_dir, xml_name)
    with open(xml_path, 'r', encoding='utf-8') as f:
        content = f.read()

    updated = 0
    for key, trans in translations.items():
        if lang not in trans:
            continue
        value = trans[lang]
        # Escape XML
        safe_value = value.replace('&', '&amp;').replace('<', '&lt;').replace('>', '&gt;')

        # Find and replace existing entry
        pattern = f'(<LocaleResource Name="{re.escape(key)}">\n\\s*<Value>)[^<]*(</Value>)'
        new_content = re.sub(pattern, lambda m: m.group(1) + safe_value + m.group(2), content, count=1)
        if new_content != content:
            content = new_content
            updated += 1

    with open(xml_path, 'w', encoding='utf-8') as f:
        f.write(content)

    print(f'{xml_name}: Updated {updated}/{len(translations)} keys')

print('\nDone!')
