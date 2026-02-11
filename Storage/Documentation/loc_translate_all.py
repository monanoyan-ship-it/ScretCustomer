#!/usr/bin/env python3
"""Translate ALL remaining English-text keys in DE and ES XML files to proper German/Spanish"""
import re, os, sys
sys.stdout.reconfigure(encoding='utf-8')

base = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
loc_dir = os.path.join(base, 'Backend', 'SecretCustomer.API', 'App_Data', 'Localization')

# Key -> {de, es} translations (only keys that need fixing)
translations = {
    # === Announcement.* ===
    "Announcement.Content": {"de": "Inhalt", "es": "Contenido"},
    "Announcement.ContentRequired": {"de": "Inhalt ist erforderlich.", "es": "El contenido es obligatorio."},
    "Announcement.Create": {"de": "Neue Ankündigung", "es": "Nuevo Anuncio"},
    "Announcement.CreateError": {"de": "Fehler beim Erstellen der Ankündigung.", "es": "Ocurrió un error al crear el anuncio."},
    "Announcement.CreateFirst": {"de": "Erste Ankündigung erstellen", "es": "Crear Primer Anuncio"},
    "Announcement.CreateSuccess": {"de": "Ankündigung erstellt.", "es": "Anuncio creado."},
    "Announcement.DeleteError": {"de": "Fehler beim Löschen der Ankündigung.", "es": "Ocurrió un error al eliminar el anuncio."},
    "Announcement.DeleteSuccess": {"de": "Ankündigung gelöscht.", "es": "Anuncio eliminado."},
    "Announcement.Edit": {"de": "Ankündigung bearbeiten", "es": "Editar Anuncio"},
    "Announcement.ExpiryDate": {"de": "Ablaufdatum", "es": "Fecha de Vencimiento"},
    "Announcement.ExpiryDateHelp": {"de": "Kein Ablauf wenn leer", "es": "Sin vencimiento si se deja vacío"},
    "Announcement.FieldTitle": {"de": "Titel", "es": "Título"},
    "Announcement.LoadError": {"de": "Fehler beim Laden der Ankündigungen.", "es": "Ocurrió un error al cargar los anuncios."},
    "Announcement.NoRecords": {"de": "Noch keine Ankündigungen.", "es": "Aún no hay anuncios."},
    "Announcement.Pinned": {"de": "Angepinnt", "es": "Fijado"},
    "Announcement.Priority": {"de": "Priorität", "es": "Prioridad"},
    "Announcement.PublishDate": {"de": "Veröffentlichungsdatum", "es": "Fecha de Publicación"},
    "Announcement.SearchPlaceholder": {"de": "Titel suchen...", "es": "Buscar título..."},
    "Announcement.Summary": {"de": "Zusammenfassung", "es": "Resumen"},
    "Announcement.SummaryPlaceholder": {"de": "Kurze Zusammenfassung für Dashboard (optional)", "es": "Resumen corto para el panel (opcional)"},
    "Announcement.TargetRoles": {"de": "Zielrollen", "es": "Roles Destinatarios"},
    "Announcement.TargetRolesHelp": {"de": "Mit Komma trennen: Admin,QualitySpecialist", "es": "Separar con comas: Admin,QualitySpecialist"},
    "Announcement.TargetRolesPlaceholder": {"de": "Leer = Alle", "es": "Vacío = Todos"},
    "Announcement.Title": {"de": "Ankündigungen", "es": "Anuncios"},
    "Announcement.TitleRequired": {"de": "Titel ist erforderlich.", "es": "El título es obligatorio."},
    "Announcement.UpdateError": {"de": "Fehler beim Aktualisieren der Ankündigung.", "es": "Ocurrió un error al actualizar el anuncio."},
    "Announcement.UpdateSuccess": {"de": "Ankündigung aktualisiert.", "es": "Anuncio actualizado."},
    "AnnouncementType.Important": {"de": "Wichtig", "es": "Importante"},
    "AnnouncementType.Info": {"es": "Información"},
    "AnnouncementType.News": {"de": "Neuigkeiten", "es": "Noticias"},
    "AnnouncementType.Success": {"de": "Erfolg", "es": "Éxito"},
    "AnnouncementType.System": {"es": "Sistema"},
    "AnnouncementType.Warning": {"de": "Warnung", "es": "Advertencia"},

    # === Checklist.* ===
    "Checklist.OnlineSurveySettings": {"de": "Online-Umfrage-Einstellungen", "es": "Configuración de Encuesta en Línea"},

    # === Common.* ===
    "Common.Phone": {"de": "Telefon", "es": "Teléfono"},
    "Common.ThisMonth": {"de": "Dieser Monat", "es": "Este Mes"},
    "Common.ThisWeek": {"de": "Diese Woche", "es": "Esta Semana"},
    "Common.ProjectCode": {"de": "Projektcode", "es": "Código de Proyecto"},
    "Common.Company": {"de": "Unternehmen", "es": "Empresa"},
    "Common.Dealer": {"de": "Händler", "es": "Distribuidor"},
    "Common.Representative": {"de": "Vertreter", "es": "Representante"},
    "Common.Address": {"de": "Adresse", "es": "Dirección"},
    "Common.Contact": {"de": "Kontakt", "es": "Contacto"},
    "Common.MinChars": {"de": "Mindestens 6 Zeichen", "es": "Al menos 6 caracteres"},
    "Common.OrCreateNew": {"de": "oder Neu erstellen", "es": "o Crear Nuevo"},
    "Common.Alternative": {"de": "Alternativ", "es": "Alternativa"},
    "Common.Copied": {"de": "Kopiert", "es": "Copiado"},
    "Common.DaySuffix": {"de": "Tag(e)", "es": "día(s)"},
    "Common.Default": {"de": "Standard", "es": "Predeterminado"},
    "Common.Disabled": {"de": "Deaktiviert", "es": "Deshabilitado"},
    "Common.DownloadExcel": {"de": "Excel herunterladen", "es": "Descargar Excel"},
    "Common.DownloadPdf": {"de": "PDF herunterladen", "es": "Descargar PDF"},
    "Common.First100Records": {"de": "Erste 100 Datensätze werden angezeigt", "es": "Mostrando los primeros 100 registros"},
    "Common.Help": {"de": "Hilfe", "es": "Ayuda"},
    "Common.NotSelected": {"de": "Nicht ausgewählt", "es": "No Seleccionado"},
    "Common.Recommended": {"de": "Empfohlen", "es": "Recomendado"},
    "Common.RecordSuffix": {"de": "Datensatz/Datensätze", "es": "registro(s)"},
    "Common.Red": {"de": "Rot", "es": "Rojo"},
    "Common.RedCard": {"de": "Rote Karte", "es": "Tarjeta Roja"},
    "Common.Yellow": {"de": "Gelb", "es": "Amarillo"},
    "Common.YellowCard": {"de": "Gelbe Karte", "es": "Tarjeta Amarilla"},
    "Common.Apply": {"es": "Aplicar"},
    "Common.LastMonth": {"es": "Mes Pasado"},
    "Common.Role": {"es": "Rol"},

    # === Customer.* ===
    "Customer.Phone": {"de": "Telefon", "es": "Teléfono"},
    "Customer.AddRule": {"de": "Regel hinzufügen", "es": "Agregar Regla"},
    "Customer.DayOfMonth": {"de": "Tag des Monats", "es": "Día del Mes"},
    "Customer.DayOfWeek": {"de": "Tag", "es": "Día"},
    "Customer.NoNotificationRules": {"de": "Noch keine Benachrichtigungsregeln hinzugefügt.", "es": "Aún no se han agregado reglas de notificación."},
    "Customer.NotificationRules": {"de": "Benachrichtigungsregeln", "es": "Reglas de Notificación"},
    "Customer.Rule": {"de": "Regel", "es": "Regla"},
    "Customer.SendToPersonnel": {"de": "Auch an Personal-E-Mail senden", "es": "También enviar al correo del personal"},
    "Customer.TokenExpirationDays": {"de": "Link-Gültigkeit (Tage)", "es": "Duración del Enlace (días)"},
    "Customer.ActiveCustomer": {"de": "Aktiver Kunde", "es": "Cliente Activo"},

    # === CustomerOrganization.* ===
    "CustomerOrganization.Current": {"de": "Aktuell", "es": "Actual"},
    "CustomerOrganization.Independent": {"de": "Unabhängig (Kein Vorgesetzter)", "es": "Independiente (Sin Supervisor)"},
    "CustomerOrganization.IndependentOperators": {"de": "Unabhängige Mitarbeiter", "es": "Operadores Independientes"},
    "CustomerOrganization.IndependentShort": {"de": "Unabhängig", "es": "Independiente"},
    "CustomerOrganization.LoadingPersonnel": {"de": "Personal wird geladen...", "es": "Cargando personal..."},
    "CustomerOrganization.MoveHint": {"de": "Verwenden Sie die Schaltfläche in der Personalzeile zum Verschieben", "es": "Use el botón en la fila del personal para mover"},
    "CustomerOrganization.MovePersonnel": {"de": "Personal verschieben", "es": "Mover Personal"},
    "CustomerOrganization.MoveToOrg": {"de": "Zur Organisation verschieben", "es": "Mover a Organización"},
    "CustomerOrganization.MoveWithTeam": {"de": "Teammitglieder gemeinsam verschieben", "es": "Mover miembros del equipo juntos"},
    "CustomerOrganization.NoOrganization": {"de": "Keine Organisation gefunden", "es": "No se encontró organización"},
    "CustomerOrganization.NoPersonnel": {"de": "Kein Personal", "es": "Sin personal"},
    "CustomerOrganization.NoSupervisors": {"de": "Keine Vorgesetzten in dieser Organisation", "es": "No hay supervisores en esta organización"},
    "CustomerOrganization.PersonSuffix": {"de": "Personen", "es": "personas"},
    "CustomerOrganization.RemoveFromOrg": {"de": "Aus Organisation entfernen", "es": "Eliminar de la Organización"},
    "CustomerOrganization.RemoveFromTeam": {"de": "Aus Team entfernen", "es": "Eliminar del Equipo"},
    "CustomerOrganization.SelectTargetOrg": {"de": "Zielorganisation auswählen:", "es": "Seleccionar Organización Destino:"},
    "CustomerOrganization.SelectTargetSupervisor": {"de": "Zielvorgesetzten auswählen:", "es": "Seleccionar Supervisor Destino:"},
    "CustomerOrganization.TeamMembersExist": {"de": "Teammitglieder vorhanden", "es": "existen miembros del equipo"},
    "CustomerOrganization.TeamSuffix": {"de": "Team", "es": "equipo"},
    "CustomerOrganization.UnassignedPersonnel": {"de": "Nicht zugewiesenes Personal", "es": "Personal No Asignado"},

    # === CustomerPersonnel.* ===
    "CustomerPersonnel.EditPersonnel": {"de": "Personal bearbeiten", "es": "Editar Personal"},

    # === CustomerPortal.* ===
    "CustomerPortal.FilterNamePlaceholder": {"de": "z.B.: Berichte dieses Monats", "es": "Ej.: Informes de Este Mes"},
    "CustomerPortal.NoSavedFilters": {"de": "Keine gespeicherten Filter.", "es": "No hay filtros guardados."},
    "CustomerPortal.Organizations.AverageScore": {"de": "Durchschn. Punktzahl", "es": "Punt. Prom."},
    "CustomerPortal.Organizations.NoRecords": {"de": "Keine Organisationen für Ihr Unternehmen gefunden.", "es": "No se encontraron organizaciones para su empresa."},
    "CustomerPortal.Organizations.NotFound": {"de": "Organisation nicht gefunden", "es": "Organización No Encontrada"},
    "CustomerPortal.Organizations.PersonnelCount": {"de": "Personal", "es": "Personal"},
    "CustomerPortal.Organizations.Title": {"de": "Organisationen", "es": "Organizaciones"},
    "CustomerPortal.SaveFilter": {"de": "Filter speichern", "es": "Guardar Filtro"},
    "CustomerPortal.SelectedFilters": {"de": "Ausgewählte Filter", "es": "Filtros Seleccionados"},
    "CustomerPortal.Supervisors.AverageScore": {"de": "Durchschn. Punktzahl", "es": "Punt. Prom."},
    "CustomerPortal.Supervisors.EvaluationCount": {"de": "Bewertungen", "es": "Evaluaciones"},
    "CustomerPortal.Supervisors.NoRecords": {"de": "Keine Vorgesetzten für Ihr Unternehmen gefunden.", "es": "No se encontraron supervisores para su empresa."},
    "CustomerPortal.Supervisors.NotFound": {"de": "Vorgesetzter nicht gefunden", "es": "Supervisor No Encontrado"},
    "CustomerPortal.Supervisors.PersonnelCount": {"de": "Personal", "es": "Personal"},
    "CustomerPortal.Dashboard.DominantType": {"de": "Dominanter Typ", "es": "Tipo Dominante"},
    "CustomerPortal.Dashboard.EnneagramTrend": {"de": "Persönlichkeitsanalyse", "es": "Análisis de Personalidad"},
    "CustomerPortal.Dashboard.NoScoreData": {"de": "Keine Punkteverteilungsdaten gefunden.", "es": "No se encontraron datos de distribución de puntuación."},
    "CustomerPortal.Dashboard.NoTrendData": {"de": "Keine monatlichen Trenddaten gefunden.", "es": "No se encontraron datos de tendencia mensual."},
    "CustomerPortal.Dashboard.ProjectCount": {"de": "Projekt", "es": "Proyecto"},
    "CustomerPortal.Dashboard.ResponseRate": {"de": "Rücklaufquote", "es": "Tasa de Respuesta"},
    "CustomerPortal.Dashboard.SurveyTrend": {"de": "Umfrage-Antworttrend", "es": "Tendencia de Respuestas de Encuesta"},
    "CustomerPortal.Dashboard.TotalInvitations": {"de": "Einladung", "es": "Invitación"},
    "CustomerPortal.Dashboard.TotalResponses": {"de": "Antworten gesamt", "es": "Total de Respuestas"},
    "CustomerPortal.Evaluations.AuditDate": {"de": "Prüfungsdatum", "es": "Fecha de Auditoría"},
    "CustomerPortal.Menu.DealerReportCard": {"de": "Händlerzeugnis", "es": "Tarjeta de Informe del Distribuidor"},
    "CustomerPortal.Menu.ExternalEvaluations": {"es": "Evaluaciones Externas"},
    "CustomerPortal.Menu.InternalEvaluations": {"es": "Evaluaciones Internas"},
    "CustomerPortal.Menu.Organizations": {"es": "Sucursales"},
    "CustomerPortal.Menu.Supervisors": {"es": "Supervisores"},
    "CustomerPortal.MyPerformance.Answer": {"de": "Antwort", "es": "Respuesta"},
    "CustomerPortal.MyPerformance.Answers": {"de": "Antworten", "es": "Respuestas"},
    "CustomerPortal.MyPerformance.Company": {"de": "Unternehmen", "es": "Empresa"},
    "CustomerPortal.MyPerformance.Earned": {"de": "Erreicht", "es": "Ganado"},
    "CustomerPortal.MyPerformance.Evaluated": {"de": "Bewertet", "es": "Evaluado"},
    "CustomerPortal.MyPerformance.Group": {"de": "Gruppe", "es": "Grupo"},
    "CustomerPortal.MyPerformance.Notes": {"de": "Notizen", "es": "Notas"},
    "CustomerPortal.MyPerformance.Organization": {"de": "Organisation", "es": "Organización"},
    "CustomerPortal.MyPerformance.Personnel": {"de": "Vertreter", "es": "Representante"},
    "CustomerPortal.MyPerformance.Question": {"de": "Frage", "es": "Pregunta"},
    "CustomerPortal.MyPerformance.TotalScore": {"de": "Gesamtpunktzahl", "es": "Puntuación Total"},
    "CustomerPortal.MyPerformance.Weight": {"de": "Gewichtung", "es": "Peso"},

    # === Dashboard.* ===
    "Dashboard.EndDateSuffix": {"de": "Ende", "es": "fin"},

    # === DateRange.* ===
    "DateRange.Day": {"de": "Tag", "es": "Día"},
    "DateRange.Last3Months": {"de": "Letzte 3 Monate", "es": "Últimos 3 Meses"},
    "DateRange.Last6Months": {"de": "Letzte 6 Monate", "es": "Últimos 6 Meses"},
    "DateRange.LastMonth": {"de": "Letzter Monat", "es": "Mes Pasado"},
    "DateRange.LastWeek": {"de": "Letzte Woche", "es": "Semana Pasada"},
    "DateRange.LastYear": {"de": "Letztes Jahr", "es": "Año Pasado"},
    "DateRange.Month": {"de": "Monat", "es": "Mes"},
    "DateRange.QuickSelect": {"de": "Schnellauswahl", "es": "Selección Rápida"},
    "DateRange.SelectDate": {"de": "Datum auswählen", "es": "Seleccionar Fecha"},
    "DateRange.ThisMonth": {"de": "Dieser Monat", "es": "Este Mes"},
    "DateRange.ThisYear": {"de": "Dieses Jahr", "es": "Este Año"},
    "DateRange.Week": {"de": "Woche", "es": "Semana"},
    "DateRange.Year": {"de": "Jahr", "es": "Año"},
    "DateRange.Yesterday": {"de": "Gestern", "es": "Ayer"},

    # === Dealer.* ===
    "Dealer.AddressPlaceholder": {"de": "Straße, Allee, Nr.", "es": "Calle, Avenida, No."},
    "Dealer.NotesPlaceholder": {"de": "Notizen zum Händler...", "es": "Notas sobre el distribuidor..."},

    # === EmailTemplate.* ===
    "EmailTemplate.BasicInfo": {"de": "Grundinformationen", "es": "Información Básica"},
    "EmailTemplate.BodyLabel": {"de": "Inhalt (Text)", "es": "Contenido (Cuerpo)"},
    "EmailTemplate.CreateFirst": {"de": "Erste Vorlage erstellen", "es": "Crear Primera Plantilla"},
    "EmailTemplate.DefaultTemplate": {"de": "Standardvorlage", "es": "Plantilla Predeterminada"},
    "EmailTemplate.DescriptionPlaceholder": {"de": "Kurze Beschreibung dieser Vorlage", "es": "Breve descripción sobre esta plantilla"},
    "EmailTemplate.EditTemplate": {"de": "E-Mail-Vorlage bearbeiten", "es": "Editar Plantilla de Correo"},
    "EmailTemplate.EmailContent": {"de": "E-Mail-Inhalt", "es": "Contenido del Correo"},
    "EmailTemplate.InsertVariable": {"de": "Variable einfügen", "es": "Insertar Variable"},
    "EmailTemplate.InsertVariableHint": {"de": "Klicken Sie auf eine Variable, um sie einzufügen", "es": "Haga clic en una variable para insertarla"},
    "EmailTemplate.Loading": {"de": "Vorlagen werden geladen...", "es": "Cargando plantillas..."},
    "EmailTemplate.NewTemplate": {"de": "Neue Vorlage", "es": "Nueva Plantilla"},
    "EmailTemplate.NoTemplates": {"de": "Noch keine E-Mail-Vorlagen gefunden.", "es": "Aún no se encontraron plantillas de correo."},
    "EmailTemplate.PlaceholderHint": {"de": "Sie können Platzhalter verwenden: {ProjectName}, {CompanyName}, usw.", "es": "Puede usar marcadores: {ProjectName}, {CompanyName}, etc."},
    "EmailTemplate.Preview": {"de": "E-Mail-Vorschau", "es": "Vista Previa del Correo"},
    "EmailTemplate.RealDataInfo": {"de": "Echter Umfragelink und QR-Code werden für das ausgewählte Personal generiert. E-Mail wird an die obige Adresse gesendet.", "es": "Se generará enlace de encuesta real y código QR para el personal seleccionado. El correo se enviará a la dirección anterior."},
    "EmailTemplate.RecipientAddress": {"de": "Empfänger-E-Mail-Adresse", "es": "Dirección de Correo del Destinatario"},
    "EmailTemplate.SelectPersonnel": {"de": "Personal auswählen...", "es": "Seleccionar personal..."},
    "EmailTemplate.SelectProject": {"de": "Projekt auswählen...", "es": "Seleccionar proyecto..."},
    "EmailTemplate.SelectProjectFirst": {"de": "Zuerst ein Projekt auswählen", "es": "Seleccione un proyecto primero"},
    "EmailTemplate.Subject": {"de": "Betreff", "es": "Asunto"},
    "EmailTemplate.SubjectLabel": {"de": "Betreff", "es": "Asunto"},
    "EmailTemplate.Template": {"de": "Vorlage", "es": "Plantilla"},
    "EmailTemplate.TemplateLoading": {"de": "Vorlage wird geladen...", "es": "Cargando plantilla..."},
    "EmailTemplate.TemplateName": {"de": "Vorlagenname", "es": "Nombre de Plantilla"},
    "EmailTemplate.TemplateType": {"de": "Vorlagentyp", "es": "Tipo de Plantilla"},
    "EmailTemplate.TestEmail": {"de": "Test-E-Mail", "es": "Correo de Prueba"},
    "EmailTemplate.TestEmailHint": {"de": "Test-E-Mail wird an diese Adresse gesendet", "es": "El correo de prueba se enviará a esta dirección"},
    "EmailTemplate.Title": {"de": "E-Mail-Vorlagen", "es": "Plantillas de Correo"},
    "EmailTemplate.UseRealData": {"de": "Echte Projekt-/Personaldaten verwenden", "es": "Usar Datos Reales de Proyecto/Personal"},
    "EmailTemplate.UseRealDataHint": {"de": "Echter Umfragelink und QR-Code werden generiert", "es": "Se generará enlace de encuesta real y código QR"},

    # === EvalReport.* ===
    "EvalReport.Answer": {"de": "Antwort", "es": "Respuesta"},
    "EvalReport.Answers": {"de": "Antworten", "es": "Respuestas"},
    "EvalReport.AuditComment": {"de": "Prüfungskommentar", "es": "Comentario de Auditoría"},
    "EvalReport.CallId": {"de": "Anruf-ID", "es": "ID de Llamada"},
    "EvalReport.CallInfo": {"de": "Anrufinformationen", "es": "Información de Llamada"},
    "EvalReport.Comment": {"de": "Kommentar", "es": "Comentario"},
    "EvalReport.Duration": {"de": "Dauer", "es": "Duración"},
    "EvalReport.Earned": {"de": "Erreicht", "es": "Ganado"},
    "EvalReport.Evaluated": {"de": "Bewertet", "es": "Evaluado"},
    "EvalReport.Evaluation": {"de": "Bewertung(en)", "es": "evaluación(es)"},
    "EvalReport.EvaluationDetail": {"de": "Bewertungsdetail", "es": "Detalle de Evaluación"},
    "EvalReport.Group": {"de": "Gruppe", "es": "Grupo"},
    "EvalReport.Loading": {"de": "Bericht wird geladen...", "es": "Cargando informe..."},
    "EvalReport.NoEvaluationsInRange": {"de": "Keine Bewertungen in diesem Zeitraum gefunden.", "es": "No se encontraron evaluaciones en este rango de fechas."},
    "EvalReport.Notes": {"de": "Notizen", "es": "Notas"},
    "EvalReport.Question": {"de": "Frage", "es": "Pregunta"},
    "EvalReport.Score": {"de": "Punktzahl", "es": "Puntuación"},
    "EvalReport.Title": {"de": "Bewertungsbericht", "es": "Informe de Evaluación"},
    "EvalReport.TotalPrefix": {"de": "Gesamt"},
    "EvalReport.TotalScore": {"de": "Gesamtpunktzahl", "es": "Puntuación Total"},
    "EvalReport.Weight": {"de": "Gewichtung", "es": "Peso"},

    # === Evaluation.* ===
    "Evaluation.Dealer": {"de": "Händler", "es": "Distribuidor"},
    "Evaluation.EvaluationDate": {"de": "Bewertungsdatum", "es": "Fecha de Evaluación"},
    "Evaluation.VisitId": {"de": "Besuchs-ID", "es": "ID de Visita"},
    "Evaluation.VisitInfo": {"de": "Besuchsinformationen", "es": "Información de Visita"},

    # === ExcelTemplate.* ===
    "ExcelTemplate.Type.Phone": {"de": "Telefon", "es": "Teléfono"},

    # === FieldWorker.* ===
    "FieldWorker.Phone": {"de": "Telefon", "es": "Teléfono"},
    "FieldWorker.ConfirmDeleteVisit": {"de": "Möchten Sie diesen Besuchsentwurf wirklich löschen?", "es": "¿Está seguro de que desea eliminar esta visita borrador?"},
    "FieldWorker.ControlInfo": {"de": "Kontrollinformationen", "es": "Información de Control"},
    "FieldWorker.DeleteVisitError": {"de": "Fehler beim Löschen des Besuchs.", "es": "Ocurrió un error al eliminar la visita."},
    "FieldWorker.NoEvaluationData": {"de": "Keine Bewertungsdaten gefunden.", "es": "No se encontraron datos de evaluación."},
    "FieldWorker.VisitDeleted": {"de": "Besuchsentwurf erfolgreich gelöscht.", "es": "Visita borrador eliminada exitosamente."},

    # === Filter.* ===
    "Filter.ClearDefault": {"de": "Standard löschen", "es": "Limpiar Predeterminado"},
    "Filter.CreatedAt": {"de": "Erstellt am", "es": "Fecha de Creación"},
    "Filter.CustomerOrganization": {"de": "Kunde / Organisation", "es": "Cliente / Organización"},
    "Filter.Default": {"de": "Standard", "es": "Predeterminado"},
    "Filter.DefaultNote": {"de": "Dieser Filter wird automatisch angewendet, wenn die Seite geöffnet wird", "es": "Este filtro se aplicará automáticamente al abrir la página"},
    "Filter.Description": {"de": "Beschreibung (Optional)", "es": "Descripción (Opcional)"},
    "Filter.DescriptionOptional": {"de": "Beschreibung (Optional)", "es": "Descripción (Opcional)"},
    "Filter.DescriptionPlaceholder": {"de": "Erklären Sie, wofür dieser Filter verwendet wird...", "es": "Explique para qué se usa este filtro..."},
    "Filter.FilterName": {"de": "Filtername", "es": "Nombre del Filtro"},
    "Filter.FilterNamePlaceholder": {"de": "z.B.: Januarberichte", "es": "Ej.: Informes de Enero"},
    "Filter.Name": {"de": "Filtername", "es": "Nombre del Filtro"},
    "Filter.NamePlaceholder": {"de": "z.B.: Januarberichte", "es": "Ej.: Informes de Enero"},
    "Filter.NoSavedFilters": {"de": "Noch keine gespeicherten Filter.", "es": "Aún no hay filtros guardados."},
    "Filter.NoSearchResults": {"de": "Keine zur Suche passenden Filter gefunden.", "es": "No se encontraron filtros que coincidan con la búsqueda."},
    "Filter.SaveFilterHint": {"de": "Filter speichern, um sie schnell wiederzuverwenden.", "es": "Guarde filtros para reutilizarlos rápidamente."},
    "Filter.SaveQuery": {"de": "Abfrage speichern", "es": "Guardar Consulta"},
    "Filter.SavedFilters": {"de": "Gespeicherte Filter", "es": "Filtros Guardados"},
    "Filter.SelectCustomer": {"de": "Kunde auswählen...", "es": "Seleccionar Cliente..."},
    "Filter.SelectedFilters": {"de": "Ausgewählte Filter:", "es": "Filtros Seleccionados:"},
    "Filter.SetAsDefault": {"de": "Als Standardfilter festlegen", "es": "Establecer como filtro predeterminado"},
    "Filter.SetAsDefaultNote": {"de": "Dieser Filter wird automatisch angewendet, wenn die Seite geöffnet wird", "es": "Este filtro se aplicará automáticamente al abrir la página"},
    "Filter.SetDefault": {"de": "Als Standard festlegen", "es": "Establecer como Predeterminado"},

    # === Language.* ===
    "Language.ResourceCountInfo": {"de": "hat", "es": "tiene"},
    "Language.TranslationResources": {"de": "Übersetzungsressourcen.", "es": "recursos de traducción."},

    # === Listening.* ===
    "Listening.CustomerOrganization": {"de": "Kunde / Organisation", "es": "Cliente / Organización"},

    # === Listenings.* ===
    "Listenings.CallDateRange": {"de": "Anrufdatum", "es": "Fecha de Llamada"},
    "Listenings.CreatedAtRange": {"de": "Erfassungsdatum", "es": "Fecha de Registro"},

    # === Menu.* ===
    "Menu.DealerReportCard": {"de": "Händlerzeugnis", "es": "Tarjeta de Informe del Distribuidor"},

    # === Notifications.* ===
    "Notifications.MarkAllRead": {"de": "Alle als gelesen markieren", "es": "Marcar Todo como Leído"},
    "Notifications.Title": {"de": "Benachrichtigungen"},
    "Notifications.ViewAll": {"de": "Alle anzeigen", "es": "Ver Todo"},

    # === Organization.* ===
    "Organization.AddOperator": {"de": "Mitarbeiter hinzufügen", "es": "Agregar Operador"},
    "Organization.AddSupervisor": {"de": "Manager/Vorgesetzten hinzufügen", "es": "Agregar Gerente/Supervisor"},
    "Organization.CompanyManagers": {"de": "Unternehmensmanager", "es": "Gerentes de Empresa"},
    "Organization.DelegateWarning": {"de": "hat Teammitglieder. Bitte wählen Sie einen Stellvertreter, der diese Mitglieder übernimmt.", "es": "tiene miembros de equipo. Seleccione un delegado para hacerse cargo."},
    "Organization.EditOrg": {"de": "Organisation bearbeiten", "es": "Editar Organización"},
    "Organization.IndependentOperatorHint": {"de": "Dieser Mitarbeiter wird als unabhängig hinzugefügt", "es": "Este operador será agregado como independiente"},
    "Organization.IndependentOperators": {"de": "Unabhängige Mitarbeiter", "es": "Operadores Independientes"},
    "Organization.MakeIndependent": {"de": "Unabhängig machen", "es": "Hacer Independiente"},
    "Organization.NewManager": {"de": "Neuer Manager", "es": "Nuevo Gerente"},
    "Organization.NewOrg": {"de": "Neue Organisation", "es": "Nueva Organización"},
    "Organization.NewSupervisor": {"de": "Neuer Vorgesetzter", "es": "Nuevo Supervisor"},
    "Organization.NoIndependentOperators": {"de": "Keine unabhängigen Mitarbeiter", "es": "No hay operadores independientes"},
    "Organization.NoManagers": {"de": "Noch keine Manager", "es": "Aún no hay gerentes"},
    "Organization.NoPersonnel": {"de": "Noch kein Personal in dieser Organisation", "es": "Aún no hay personal en esta organización"},
    "Organization.NotFound": {"de": "Organisation nicht gefunden", "es": "Organización no encontrada"},
    "Organization.OrgCode": {"de": "Organisationscode", "es": "Código de Organización"},
    "Organization.OrgName": {"de": "Organisationsname", "es": "Nombre de Organización"},
    "Organization.Organizations": {"de": "Organisationen", "es": "Organizaciones"},
    "Organization.PersonnelStructure": {"de": "Personalstruktur", "es": "Estructura de Personal"},
    "Organization.RemoveFromOrg": {"de": "Aus Organisation entfernen", "es": "Eliminar de la Organización"},
    "Organization.SelectDelegate": {"de": "Stellvertreter auswählen", "es": "Seleccionar Delegado"},
    "Organization.SelectFromPool": {"de": "Aus Pool auswählen", "es": "Seleccionar del Pool"},
    "Organization.SelectNewSupervisor": {"de": "Neuen Vorgesetzten auswählen", "es": "Seleccionar Nuevo Supervisor"},
    "Organization.SelectOrgHint": {"de": "Wählen Sie links eine Organisation aus, um Personal anzuzeigen", "es": "Seleccione una organización de la izquierda para ver el personal"},
    "Organization.Supervisor": {"de": "Vorgesetzter"},
    "Organization.TeamMembersToTransfer": {"de": "Zu übertragende Teammitglieder", "es": "Miembros del Equipo a Transferir"},
    "Organization.TransferAndRemove": {"de": "Übertragen und entfernen", "es": "Transferir y Eliminar"},

    # === Penalty.* ===
    "Penalty.Red": {"de": "Rot", "es": "Rojo"},
    "Penalty.Yellow": {"de": "Gelb", "es": "Amarillo"},

    # === Period.* ===
    "Period.Closed": {"de": "Geschlossen", "es": "Cerrado"},
    "Period.Open": {"de": "Offen", "es": "Abierto"},

    # === Permission.* ===
    "Permission.Granted": {"de": "Erteilt", "es": "Concedido"},
    "Permission.Indefinite": {"de": "Unbefristet", "es": "Indefinido"},
    "Permission.Revoked": {"de": "Widerrufen", "es": "Revocado"},

    # === Personnel.* ===
    "Personnel.Phone": {"de": "Telefon", "es": "Teléfono"},

    # === Priority.* ===
    "Priority.Highest": {"de": "Höchste", "es": "Más Alta"},
    "Priority.Lowest": {"de": "Niedrigste", "es": "Más Baja"},

    # === Profile.* ===
    "Profile.CallSystemUrl": {"de": "Anrufsystem-URL", "es": "URL del Sistema de Llamadas"},
    "Profile.CallSystemUrlHelp": {"de": "URL zur Weiterleitung beim Klick auf die Anruf-ID. Der Platzhalter {CallId} wird durch die tatsächliche Anruf-ID ersetzt.", "es": "URL de redirección al hacer clic en ID de Llamada. El marcador {CallId} será reemplazado con el ID real."},
    "Profile.CompanySettings": {"de": "Unternehmenseinstellungen", "es": "Configuración de Empresa"},
    "Profile.Example": {"de": "Beispiel", "es": "Ejemplo"},
    "Profile.Phone": {"de": "Telefon", "es": "Teléfono"},
    "Profile.SaveCompanySettings": {"de": "Unternehmenseinstellungen speichern", "es": "Guardar Configuración de Empresa"},

    # === Project.* ===
    "Project.Assignment.Phone": {"de": "Telefon", "es": "Teléfono"},
    "Project.AutoReport": {"de": "Automatischer Bericht", "es": "Informe Automático"},
    "Project.EndDateFilter": {"de": "Enddatumsfilter", "es": "Filtro de Fecha Fin"},
    "Project.Identity": {"de": "Identität", "es": "Identidad"},
    "Project.Reporting": {"de": "Berichtswesen", "es": "Informes"},
    "Project.SelectFromSettings": {"de": "(Aus Projekteinstellungen auswählen)", "es": "(Seleccionar desde configuración del proyecto)"},
    "Project.SurveyIdentity.Open": {"de": "Offen", "es": "Abierto"},
    "Project.Template": {"de": "Vorlage", "es": "Plantilla"},

    # === Report.* ===
    "Report.AIGenerating": {"de": "KI erstellt den Bericht, bitte warten...", "es": "La IA está generando el informe, por favor espere..."},
    "Report.AIInfoText": {"de": "Diese Funktion erstellt automatische Berichte mit Google Gemini KI. Die Berichtserstellung kann 10-30 Sekunden dauern.", "es": "Esta función genera informes automáticos usando Google Gemini AI. La generación puede tomar 10-30 segundos."},
    "Report.AIReport": {"de": "KI-Bericht", "es": "Informe IA"},
    "Report.AIEmptyState": {"de": "Konfigurieren Sie die Einstellungen im linken Bereich und klicken Sie auf \"Bericht erstellen\".", "es": "Configure los ajustes desde el panel izquierdo y haga clic en \"Generar Informe\"."},
    "Report.AnswerBasedRecords": {"de": "Antwortbasierte Datensätze", "es": "Registros Basados en Respuestas"},
    "Report.Average": {"de": "Durchschnitt", "es": "Promedio"},
    "Report.BasedOnComments": {"de": "(Basierend auf Kommentaranzahl)", "es": "(Basado en cantidad de comentarios)"},
    "Report.CompareYear": {"de": "Vergleichsjahr (Optional)", "es": "Año de Comparación (Opcional)"},
    "Report.ControlTime": {"de": "Kontrollzeit", "es": "Hora de Control"},
    "Report.CurrentYearTarget": {"de": "Ziel des aktuellen Jahres", "es": "Objetivo del Año Actual"},
    "Report.DataPreview": {"de": "Datenvorschau (JSON)", "es": "Vista Previa de Datos (JSON)"},
    "Report.Dealer": {"de": "Händler", "es": "distribuidor"},
    "Report.DealerHierarchyHint": {"de": "Händler des Kunden werden gefiltert, wenn ein Kunde ausgewählt wird.", "es": "Los distribuidores se filtran al seleccionar el cliente."},
    "Report.DealerReportCard": {"de": "Händlerzeugnis", "es": "Tarjeta de Informe del Distribuidor"},
    "Report.DealerSelection": {"de": "Händlerauswahl", "es": "Selección de Distribuidor"},
    "Report.Dealers": {"de": "Händler", "es": "Distribuidores"},
    "Report.DownloadMd": {"de": "Herunterladen (.md)", "es": "Descargar (.md)"},
    "Report.EvalAbbr": {"de": "Bew."},
    "Report.EvaluationGeneralNotes": {"de": "Allgemeine Bewertungsnotizen", "es": "Notas Generales de Evaluación"},
    "Report.EvaluationSuffix": {"de": "Bewertung(en)", "es": "evaluación(es)"},
    "Report.EvaluationType": {"de": "Bewertungstyp", "es": "Tipo de Evaluación"},
    "Report.ExportPdf": {"de": "PDF exportieren", "es": "Exportar PDF"},
    "Report.ExternalEvaluation": {"de": "Externe Bewertung", "es": "Evaluación Externa"},
    "Report.Generating": {"de": "Wird erstellt...", "es": "Generando..."},
    "Report.Highest": {"de": "Höchste", "es": "Más Alto"},
    "Report.Inspected": {"de": "Geprüft", "es": "Inspeccionado"},
    "Report.InternalEvaluation": {"de": "Interne Bewertung", "es": "Evaluación Interna"},
    "Report.ListeningDetail": {"de": "Anhörungsdetail", "es": "Detalle de Escucha"},
    "Report.Lowest": {"de": "Niedrigste", "es": "Más Bajo"},
    "Report.LowScore": {"de": "Niedrige Punktzahl", "es": "Puntuación Baja"},
    "Report.MonthlyPerformance": {"de": "Monatliche Leistung", "es": "Rendimiento Mensual"},
    "Report.NoDealerFound": {"de": "Kein Händler gefunden", "es": "No se encontró distribuidor"},
    "Report.NoEvaluationsForDealer": {"de": "Keine Bewertungen für diesen Händler gefunden", "es": "No se encontraron evaluaciones para este distribuidor"},
    "Report.NoEvaluationsInRange": {"de": "Keine Bewertungen in diesem Bereich gefunden.", "es": "No se encontraron evaluaciones en este rango."},
    "Report.NoGeneralNotes": {"de": "Keine allgemeinen Notizen gefunden.", "es": "No se encontraron notas generales."},
    "Report.PercentageRanking": {"de": "Prozentuale Rangfolge", "es": "Clasificación Porcentual"},
    "Report.PreviousYearTarget": {"de": "Ziel des Vorjahres", "es": "Objetivo del Año Anterior"},
    "Report.RCard": {"de": "R.Karte", "es": "T.Roja"},
    "Report.ReportOutput": {"de": "Berichtsausgabe", "es": "Resultado del Informe"},
    "Report.ReportSettings": {"de": "Berichtseinstellungen", "es": "Configuración del Informe"},
    "Report.ReportYear": {"de": "Berichtsjahr", "es": "Año del Informe"},
    "Report.Response": {"de": "Antwort(en)", "es": "respuesta(s)"},
    "Report.ScoreDistribution": {"de": "Punkteverteilung", "es": "Distribución de Puntuación"},
    "Report.ScoreExcellent": {"de": "Ausgezeichnet (90+)", "es": "Excelente (90+)"},
    "Report.ScoreGood": {"de": "Gut (80-89)", "es": "Bueno (80-89)"},
    "Report.ScorePoor": {"de": "Schlecht (<60)", "es": "Deficiente (<60)"},
    "Report.SelectCustomer": {"de": "Kunde auswählen...", "es": "Seleccionar cliente..."},
    "Report.SelectDealerFromLeftPanel": {"de": "Wählen Sie links einen Händler aus, um sein Zeugnis anzuzeigen.", "es": "Seleccione un distribuidor del panel izquierdo para ver su tarjeta de informe."},
    "Report.SelectDealerToViewReport": {"de": "Händler auswählen, um Zeugnis anzuzeigen", "es": "Seleccione un distribuidor para ver la tarjeta de informe"},
    "Report.SelectionSuffix": {"de": "Auswahl(en)", "es": "selección(es)"},
    "Report.StretchTarget": {"de": "Stretch-Ziel", "es": "Objetivo Extendido"},
    "Report.SuggestionSuffix": {"de": "Vorschlag/Vorschläge", "es": "sugerencia(s)"},
    "Report.TargetScores": {"de": "Zielpunktzahlen", "es": "Puntuaciones Objetivo"},
    "Report.VisitInfo": {"de": "Besuchsinformationen", "es": "Información de Visita"},
    "Report.YCard": {"de": "G.Karte", "es": "T.Amar."},

    # === Smtp.* ===
    "Smtp.Authentication": {"de": "Authentifizierung", "es": "Autenticación"},
    "Smtp.ConnectionTest": {"de": "Verbindungstest", "es": "Prueba de Conexión"},
    "Smtp.CreateFirst": {"de": "Erstes Profil erstellen", "es": "Crear Primer Perfil"},
    "Smtp.EditProfileTitle": {"de": "SMTP-Profil bearbeiten", "es": "Editar Perfil SMTP"},
    "Smtp.GmailAppPasswordHint": {"de": "Verwenden Sie \"App-Passwort\" für Gmail.", "es": "Use \"Contraseña de Aplicación\" para Gmail."},
    "Smtp.GraphApiHint": {"de": "Sendet E-Mails über Graph API statt SMTP (empfohlen)", "es": "Envía correo vía Graph API en lugar de SMTP (recomendado)"},
    "Smtp.ModernAuth": {"de": "Moderne Authentifizierungsmethode", "es": "Método de autenticación moderno"},
    "Smtp.NewProfile": {"de": "Neues Profil", "es": "Nuevo Perfil"},
    "Smtp.NewProfileTitle": {"de": "Neues SMTP-Profil", "es": "Nuevo Perfil SMTP"},
    "Smtp.NoProfiles": {"de": "Noch keine SMTP-Profile gefunden.", "es": "Aún no se encontraron perfiles SMTP."},
    "Smtp.Password": {"de": "Passwort / App-Passwort", "es": "Contraseña / Contraseña de Aplicación"},
    "Smtp.Profile": {"de": "Profil", "es": "Perfil"},
    "Smtp.ProfileActive": {"de": "Profil aktiv", "es": "Perfil Activo"},
    "Smtp.ProfileManagement": {"de": "SMTP-Profilverwaltung", "es": "Gestión de Perfiles SMTP"},
    "Smtp.ProfileName": {"de": "Profilname", "es": "Nombre del Perfil"},
    "Smtp.RecipientEmail": {"de": "Empfänger-E-Mail-Adresse", "es": "Dirección de Correo del Destinatario"},
    "Smtp.SendTestEmail": {"de": "Test-E-Mail senden", "es": "Enviar Correo de Prueba"},
    "Smtp.Sender": {"de": "Absender", "es": "Remitente"},
    "Smtp.SenderEmail": {"de": "Absender-E-Mail", "es": "Correo del Remitente"},
    "Smtp.SenderName": {"de": "Absendername", "es": "Nombre del Remitente"},
    "Smtp.Server": {"es": "Servidor"},
    "Smtp.ServerSettings": {"de": "Servereinstellungen", "es": "Configuración del Servidor"},
    "Smtp.SetDefault": {"de": "Als Standard festlegen", "es": "Establecer como Predeterminado"},
    "Smtp.SmtpServer": {"es": "Servidor SMTP"},
    "Smtp.Title": {"de": "SMTP-Profile", "es": "Perfiles SMTP"},
    "Smtp.UseGraphApi": {"de": "Microsoft Graph API verwenden", "es": "Usar Microsoft Graph API"},
    "Smtp.UseSsl": {"de": "SSL/TLS verwenden (StartTLS)"},
    "Smtp.Username": {"de": "Benutzername / E-Mail", "es": "Usuario / Correo"},

    # === Survey.* ===
    "Survey.Anonymous": {"de": "Anonym", "es": "Anónimo"},
    "Survey.CommentPlaceholder": {"de": "Kommentar (optional)", "es": "Comentario (opcional)"},
    "Survey.Completed.CloseWindow": {"de": "Vielen Dank für Ihre Teilnahme. Sie können dieses Fenster schließen.", "es": "Gracias por su participación. Puede cerrar esta ventana."},
    "Survey.Completed.Success": {"de": "Ihre Umfrage wurde erfolgreich übermittelt.", "es": "Su encuesta ha sido enviada exitosamente."},
    "Survey.Completed.Thanks": {"de": "Vielen Dank!", "es": "¡Gracias!"},
    "Survey.Completed.Title": {"de": "Umfrage abgeschlossen", "es": "Encuesta Completada"},
    "Survey.ErrorMessage": {"de": "Bitte überprüfen Sie den Ihnen zugesandten Link oder kontaktieren Sie Ihren Administrator.", "es": "Verifique el enlace enviado o contacte a su administrador."},
    "Survey.Loading": {"de": "Umfrage wird geladen...", "es": "Cargando encuesta..."},
    "Survey.QuestionsAnswered": {"de": "Fragen beantwortet", "es": "preguntas respondidas"},
    "Survey.SelectMultiple": {"de": "Auswählen (Mehrfachauswahl möglich):", "es": "Seleccione (se permiten múltiples opciones):"},
    "Survey.SelectOne": {"de": "Eine auswählen:", "es": "Seleccione una:"},
    "Survey.Submit": {"de": "Umfrage absenden", "es": "Enviar Encuesta"},
    "Survey.Submitting": {"de": "Wird übermittelt...", "es": "Enviando..."},
    "Survey.Title": {"de": "Umfrage", "es": "Encuesta"},

    # === TrainingVideo.* ===
    "TrainingVideo.NoQuiz": {"de": "Kein Quiz ausgewählt", "es": "Ningún quiz seleccionado"},

    # === Visits.* ===
    "Visits.ActiveFiltersNote": {"de": "Aktive Filter werden auf den Bericht angewendet:", "es": "Los filtros activos se aplicarán al informe:"},
    "Visits.Cards": {"de": "Karten", "es": "Tarjetas"},
    "Visits.ControlDate": {"de": "Kontrolldatum", "es": "Fecha de Control"},
    "Visits.ControlTime": {"de": "Uhrzeit", "es": "Hora"},
    "Visits.Dealer": {"de": "Händler", "es": "Distribuidor"},
    "Visits.Detail": {"de": "Besuchsdetail", "es": "Detalle de Visita"},
    "Visits.EvaluationSource": {"de": "Bewertungsquelle", "es": "Fuente de Evaluación"},
    "Visits.Evaluator": {"de": "Bewerter", "es": "Evaluador"},
    "Visits.NoAttachments": {"de": "Keine Anhänge für diesen Besuch gefunden.", "es": "No se encontraron archivos adjuntos para esta visita."},
    "Visits.NoRecords": {"de": "Keine Datensätze gefunden.", "es": "No se encontraron registros."},
    "Visits.Period": {"de": "Zeitraum", "es": "Período"},
    "Visits.Personnel": {"de": "Bewertet", "es": "Evaluado"},
    "Visits.Report.DetailedExcel": {"de": "Detailliertes Excel (Fragen-Antworten)", "es": "Excel Detallado (Preguntas-Respuestas)"},
    "Visits.Report.DetailedExcelDesc": {"de": "Detaillierter Bericht mit allen Fragen und Antworten", "es": "Informe detallado con todas las preguntas y respuestas"},
    "Visits.Report.GeneralExcel": {"de": "Allgemeiner Excel-Export", "es": "Exportación Excel General"},
    "Visits.Report.GeneralExcelDesc": {"de": "Zusammenfassung aller Anhörungsdatensätze", "es": "Resumen de todos los registros de escucha"},
    "Visits.Report.MT": {"de": "MT-Bericht", "es": "Informe MT"},
    "Visits.Report.MTDesc": {"de": "Erfolg, Verbesserungsbereiche, Prozessanalyse und Indexerfolg (4 Blätter)", "es": "Éxito, áreas de mejora, análisis de procesos e índice de éxito (4 hojas)"},
    "Visits.Report.ProjectPerformance": {"de": "Projektleistungsbericht", "es": "Informe de Rendimiento del Proyecto"},
    "Visits.Report.ProjectPerformanceDesc": {"de": "Periodenbezogene Projektdurchschnittswerte und Rangfolgen", "es": "Promedios y clasificaciones por período"},
    "Visits.Report.QuestionGroupAverage": {"de": "Fragengruppen-Durchschnittsbericht", "es": "Informe Promedio por Grupo de Preguntas"},
    "Visits.Report.QuestionGroupAverageDesc": {"de": "Durchschnittswerte nach Projekt, Zeitraum und Fragengruppe", "es": "Puntuaciones promedio por proyecto, período y grupo de preguntas"},
    "Visits.Report.VisitAudit": {"de": "Besuchsprüfungsbericht", "es": "Informe de Auditoría de Visitas"},
    "Visits.Report.VisitAuditDesc": {"de": "Projekt, Bewerter, Händler, Personal, Kontrolldatum/-zeit, Kommentare und Punktzahlinformationen", "es": "Proyecto, evaluador, distribuidor, personal, fecha/hora de control, comentarios y puntuación"},
    "Visits.Report.VisitCustomerEvaluation": {"de": "Kundenbesuch-Bewertungsbericht", "es": "Informe de Evaluación de Visita al Cliente"},
    "Visits.Report.VisitCustomerEvaluationDesc": {"de": "Unternehmen, Händler, Personal, Abteilung, Kontrolldatum/-zeit und Punktzahlinformationen", "es": "Empresa, distribuidor, personal, departamento, fecha/hora de control y puntuación"},
    "Visits.Reports": {"de": "Berichte", "es": "Informes"},
    "Visits.ReportsDescription": {"de": "Berichte basierend auf aktuellen Filtern erstellen.", "es": "Generar informes basados en los filtros actuales."},
    "Visits.SavedFilters": {"de": "Gespeicherte Filter", "es": "Filtros Guardados"},
    "Visits.Score": {"de": "Punktzahl", "es": "Puntuación"},
    "Visits.Supervisor": {"de": "Vorgesetzter"},
    "Visits.Title": {"de": "Besuche", "es": "Visitas"},
}

# Language code to XML file mapping
lang_map = {
    "de": "resources.de.xml",
    "es": "resources.es.xml",
}

for lang, xml_name in lang_map.items():
    xml_path = os.path.join(loc_dir, xml_name)
    with open(xml_path, 'r', encoding='utf-8') as f:
        content = f.read()

    updated = 0
    not_found = 0
    skipped = 0
    for key, trans in translations.items():
        if lang not in trans:
            skipped += 1
            continue
        value = trans[lang]
        # Escape XML special chars
        safe_value = value.replace('&', '&amp;').replace('<', '&lt;').replace('>', '&gt;').replace('"', '&quot;')

        # Find and replace existing entry
        pattern = f'(<LocaleResource Name="{re.escape(key)}">\\s*<Value>)[^<]*(</Value>)'
        new_content = re.sub(pattern, lambda m: m.group(1) + safe_value + m.group(2), content, count=1)
        if new_content != content:
            content = new_content
            updated += 1
        else:
            not_found += 1

    with open(xml_path, 'w', encoding='utf-8') as f:
        f.write(content)

    total_applicable = len([k for k, t in translations.items() if lang in t])
    print(f'{xml_name}: Updated {updated}/{total_applicable} keys (skipped {skipped} N/A, {not_found} not found in XML)')

print('\nDone!')
