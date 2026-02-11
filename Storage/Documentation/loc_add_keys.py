"""
Yeni localization key'lerini XML dosyalarina ekler.
Kullanim: python loc_add_keys.py
"""
import sys, io, os, re

sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8')

base_xml = r'C:\Users\Ahmet\source\repos\monanoyan-ship-it\ScretCustomer\Backend\SecretCustomer.API\App_Data\Localization'

# Key -> (TR value, EN value)
NEW_KEYS = {
    # === Common keys ===
    'Common.AllCustomers': ('Tüm Müşteriler', 'All Customers'),
    'Common.Copied': ('Kopyalandı', 'Copied'),
    'Common.DaySuffix': ('gün', 'day(s)'),
    'Common.Fetch': ('Getir', 'Fetch'),
    'Common.First100Records': ('İlk 100 kayıt gösteriliyor', 'Showing first 100 records'),
    'Common.NotSelected': ('Seçilmedi', 'Not Selected'),
    'Common.Question': ('soru', 'question'),
    'Common.RecordSuffix': ('kayıt', 'record(s)'),
    'Common.Red': ('Kırmızı', 'Red'),
    'Common.RedCard': ('Kırmızı Kart', 'Red Card'),
    'Common.SelectCustomer': ('Müşteri Seçin...', 'Select Customer...'),
    'Common.Yellow': ('Sarı', 'Yellow'),
    'Common.YellowCard': ('Sarı Kart', 'Yellow Card'),
    # === Dashboard keys ===
    'Dashboard.EndDateSuffix': ('bitiş', 'end'),
    # === DateRange keys ===
    'DateRange.Day': ('Gün', 'Day'),
    'DateRange.Month': ('Ay', 'Month'),
    'DateRange.QuickSelect': ('Hızlı Seç', 'Quick Select'),
    'DateRange.Week': ('Hafta', 'Week'),
    'DateRange.Year': ('Yıl', 'Year'),
    'DateRange.Yesterday': ('Dün', 'Yesterday'),
    # === Filter keys ===
    'Filter.ClearDefault': ('Varsayılanı Kaldır', 'Clear Default'),
    'Filter.CreatedAt': ('Oluşturulma', 'Created At'),
    'Filter.CustomerOrganization': ('Müşteri / Organizasyon', 'Customer / Organization'),
    'Filter.Default': ('Varsayılan', 'Default'),
    'Filter.DefaultNote': ('Sayfa açıldığında bu filtre otomatik uygulanacak', 'This filter will be automatically applied when the page is opened'),
    'Filter.Description': ('Açıklama (Opsiyonel)', 'Description (Optional)'),
    'Filter.DescriptionOptional': ('Açıklama (Opsiyonel)', 'Description (Optional)'),
    'Filter.DescriptionPlaceholder': ('Bu filtrenin ne için kullanıldığını açıklayın...', 'Explain what this filter is used for...'),
    'Filter.FilterName': ('Filtre Adı', 'Filter Name'),
    'Filter.FilterNamePlaceholder': ('Örn: Ocak Ayı Raporları', 'E.g.: January Reports'),
    'Filter.Name': ('Filtre Adı', 'Filter Name'),
    'Filter.NamePlaceholder': ('Örn: Ocak Ayı Raporları', 'E.g.: January Reports'),
    'Filter.NoSavedFilters': ('Henüz kayıtlı filtre yok.', 'No saved filters yet.'),
    'Filter.NoSearchResults': ('Aramayla eşleşen filtre bulunamadı.', 'No filters matching the search were found.'),
    'Filter.SaveFilterHint': ('Filtreleri kaydederek hızlıca tekrar kullanabilirsiniz.', 'Save filters to quickly reuse them.'),
    'Filter.SaveQuery': ('Sorguyu Kaydet', 'Save Query'),
    'Filter.SavedFilters': ('Kayıtlı Filtreler', 'Saved Filters'),
    'Filter.SelectCustomer': ('Müşteri Seçin...', 'Select Customer...'),
    'Filter.SelectedFilters': ('Seçilen Filtreler:', 'Selected Filters:'),
    'Filter.SetAsDefault': ('Varsayılan filtre olarak ayarla', 'Set as default filter'),
    'Filter.SetAsDefaultNote': ('Sayfa açıldığında bu filtre otomatik uygulanacak', 'This filter will be automatically applied when the page is opened'),
    'Filter.SetDefault': ('Varsayılan Yap', 'Set as Default'),
    # === Listening keys ===
    'Listening.CustomerOrganization': ('Müşteri / Organizasyon', 'Customer / Organization'),
    # === Penalty keys ===
    'Penalty.Red': ('Kırmızı', 'Red'),
    'Penalty.Yellow': ('Sarı', 'Yellow'),
    # === Project keys ===
    'Project.AutoReport': ('Otomatik Rapor', 'Auto Report'),
    'Project.EndDateFilter': ('Bitiş Tarihi Filtresi', 'End Date Filter'),
    'Project.Identity': ('Kimlik', 'Identity'),
    'Project.Reporting': ('Raporlama', 'Reporting'),
    'Project.SelectFromSettings': ('(Proje ayarlarından seçin)', '(Select from project settings)'),
    'Project.SurveyIdentity.Open': ('Açık', 'Open'),
    'Project.Template': ('Şablon', 'Template'),
    # === Report keys ===
    'Report.AIGenerating': ('AI rapor oluşturuyor, lütfen bekleyin...', 'AI is generating the report, please wait...'),
    'Report.AIInfoText': ('Bu özellik Google Gemini AI kullanarak otomatik rapor oluşturur. Rapor oluşturma işlemi 10-30 saniye sürebilir.', 'This feature generates automatic reports using Google Gemini AI. Report generation may take 10-30 seconds.'),
    'Report.AIReport': ('AI Rapor', 'AI Report'),
    'Report.AnswerBasedRecords': ('Cevap Bazlı Kayıtlar', 'Answer-Based Records'),
    'Report.CompareYear': ('Karşılaştırma Yılı (Opsiyonel)', 'Comparison Year (Optional)'),
    'Report.CurrentYearTarget': ('Mevcut Yıl Hedefi', 'Current Year Target'),
    'Report.DataPreview': ('Veri Önizleme (JSON)', 'Data Preview (JSON)'),
    'Report.DownloadMd': ('İndir (.md)', 'Download (.md)'),
    'Report.EvalAbbr': ('değ.', 'eval.'),
    'Report.EvaluationGeneralNotes': ('Değerlendirme Genel Notları', 'Evaluation General Notes'),
    'Report.EvaluationSuffix': ('değerlendirme', 'evaluation(s)'),
    'Report.GeneralNote': ('Genel Not', 'General Note'),
    'Report.Generating': ('Oluşturuluyor...', 'Generating...'),
    'Report.LowScore': ('Düşük Puan', 'Low Score'),
    'Report.NoEvaluationsInRange': ('Bu aralıkta değerlendirme bulunamadı.', 'No evaluations found in this range.'),
    'Report.NoGeneralNotes': ('Genel not bulunamadı.', 'No general notes found.'),
    'Report.PercentageRanking': ('Yüzde Sıralaması', 'Percentage Ranking'),
    'Report.PreviousYearTarget': ('Önceki Yıl Hedefi', 'Previous Year Target'),
    'Report.ReportOutput': ('Rapor Çıktısı', 'Report Output'),
    'Report.ReportSettings': ('Rapor Ayarları', 'Report Settings'),
    'Report.ReportYear': ('Rapor Yılı', 'Report Year'),
    'Report.Response': ('yanıt', 'response(s)'),
    'Report.ScoreDistribution': ('Puan Dağılımı', 'Score Distribution'),
    'Report.ScoreExcellent': ('Mükemmel (90+)', 'Excellent (90+)'),
    'Report.ScoreGood': ('İyi (80-89)', 'Good (80-89)'),
    'Report.ScorePoor': ('Düşük (<60)', 'Poor (<60)'),
    'Report.SelectCustomer': ('Müşteri seçin...', 'Select customer...'),
    'Report.SelectionSuffix': ('seçim', 'selection(s)'),
    'Report.StretchTarget': ('Üst Hedef', 'Stretch Target'),
    'Report.SuggestionSuffix': ('öneri', 'suggestion(s)'),
    'Report.TargetScores': ('Hedef Puanlar', 'Target Scores'),
    'Report.Token': ('Token', 'Token'),
    'Report.Visual': ('Görsel', 'Visual'),
    # === Smtp keys ===
    'Smtp.ProfileManagement': ('SMTP Profil Yönetimi', 'SMTP Profile Management'),
    # === Survey keys ===
    'Survey.Anonymous': ('Anonim', 'Anonymous'),
    'Survey.CommentPlaceholder': ('Yorum (opsiyonel)', 'Comment (optional)'),
    'Survey.Completed.CloseWindow': ('Katılımınız için teşekkür ederiz. Bu pencereyi kapatabilirsiniz.', 'Thank you for your participation. You can close this window.'),
    'Survey.Completed.Success': ('Anketiniz başarıyla gönderildi.', 'Your survey has been submitted successfully.'),
    'Survey.Completed.Thanks': ('Teşekkürler!', 'Thank you!'),
    'Survey.Completed.Title': ('Anket Tamamlandı', 'Survey Completed'),
    'Survey.ErrorMessage': ('Lütfen size gönderilen linki kontrol edin veya yöneticinize başvurun.', 'Please check the link sent to you or contact your administrator.'),
    'Survey.Loading': ('Anket yükleniyor...', 'Loading survey...'),
    'Survey.QuestionsAnswered': ('soru cevaplandı', 'questions answered'),
    'Survey.SelectMultiple': ('Seçin (birden fazla seçilebilir):', 'Select (multiple choices allowed):'),
    'Survey.SelectOne': ('Birini seçin:', 'Select one:'),
    'Survey.Submit': ('Anketi Gönder', 'Submit Survey'),
    'Survey.Submitting': ('Gönderiliyor...', 'Submitting...'),
    'Survey.Title': ('Anket', 'Survey'),
}

for xml_file, lang_idx in [('resources.tr.xml', 0), ('resources.en.xml', 1), ('resources.de.xml', 1), ('resources.es.xml', 1)]:
    xml_path = os.path.join(base_xml, xml_file)
    if not os.path.exists(xml_path):
        continue

    with open(xml_path, 'r', encoding='utf-8-sig') as f:
        content = f.read()

    added = 0
    snippets = []
    for key in sorted(NEW_KEYS.keys()):
        if f'Name="{key}"' in content:
            continue
        val = NEW_KEYS[key][lang_idx]
        snippets.append(f'  <LocaleResource Name="{key}">\n    <Value>{val}</Value>\n  </LocaleResource>')
        added += 1

    if snippets:
        insert_block = '\n'.join(snippets)
        content = content.replace('</Language>', f'{insert_block}\n</Language>')
        with open(xml_path, 'w', encoding='utf-8-sig') as f:
            f.write(content)

    print(f'{xml_file}: {added} keys added')

print('Done!')
