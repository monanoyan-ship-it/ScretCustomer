using SecretCustomer.Core.DTOs.GolgeMusteri;

namespace SecretCustomer.Core.Interfaces.Services;

public interface IGmService
{
    // HedefFirma
    Task<List<GmHedefFirmaDto>> GetHedefFirmalarAsync(int? customerId = null);
    Task<GmHedefFirmaDto?> GetHedefFirmaByIdAsync(int id);
    Task<GmHedefFirmaDto> CreateHedefFirmaAsync(CreateGmHedefFirmaDto dto);
    Task<GmHedefFirmaDto?> UpdateHedefFirmaAsync(int id, UpdateGmHedefFirmaDto dto);
    Task<bool> DeleteHedefFirmaAsync(int id);

    // DonemSoru (soru yönetimi artık dönem bazlı)
    Task<List<GmDonemSoruDto>> GetDonemSorularAsync(int? customerId = null, int? hedefFirmaId = null, int? donemId = null);
    Task<GmDonemSoruDto> CreateDonemSoruAsync(int donemId, CreateDonemSoruRequest dto);
    Task<GmDonemSoruDto?> UpdateDonemSoruAsync(int donemSoruId, UpdateDonemSoruRequest dto);
    Task<bool> RemoveDonemSoruAsync(int donemSoruId);
    Task<(int imported, int skipped, List<string> errors)> ImportDonemSorularFromExcelAsync(int donemId, int customerId, int hedefFirmaId, Stream excelStream);
    Task<ImportDonemSorularResult> ImportDonemSorularWithMatchingAsync(int donemId, Stream excelStream);
    Task<int> SaveUnmatchedSorularAsync(int donemId, List<SaveUnmatchedSoruItem> items);

    // Donem
    Task<List<GmDonemDto>> GetDonemlerAsync();
    Task<GmDonemDetailDto?> GetDonemDetailAsync(int id);
    Task<GmDonemDto> CreateDonemAsync(CreateGmDonemDto dto, int userId);
    Task<GmDonemDto?> UpdateDonemAsync(int id, UpdateGmDonemDto dto);
    Task<bool> DeleteDonemAsync(int id);

    // Donem alt yönetim
    Task<bool> AddDonemPersonelAsync(int donemId, int userId);
    Task<bool> RemoveDonemPersonelAsync(int donemPersonelId);

    // Kuponlu soru import (aktif dönem için ayrı endpoint'ler)
    Task<(int imported, int skipped, List<string> errors)> ImportKuponluSorularFromExcelAsync(int donemId, int customerId, int hedefFirmaId, Stream excelStream);
    Task<ImportDonemSorularResult> ImportKuponluSorularWithMatchingAsync(int donemId, Stream excelStream);
    Task<int> SaveUnmatchedKuponluSorularAsync(int donemId, List<SaveUnmatchedSoruItem> items);

    // Aktif Et (dağıtım algoritması - kuponlu sorular hariç)
    Task<int> AktifEtAsync(int donemId);

    // Kuponlu soru dağıtımı (aktif dönemde, ataması olmayan kuponlu soruları dağıt)
    Task<int> KuponluDagitAsync(int donemId);

    // Dönem tamamla
    Task<bool> TamamlaAsync(int donemId);

    // Dönem kopyala
    Task<int> CopyDonemAsync(int sourceDonemId, string yeniAd, DateTime baslangic, DateTime bitis, int userId);

    // Takip (atama listesi)
    Task<List<GmAtamaDto>> GetAtamalarAsync(int donemId, int? userId = null, int? durumId = null);

    // Aramalarım (kullanıcı)
    Task<List<GmAtamaDto>> GetAramalarimAsync(int userId, List<int>? donemIds = null, List<int>? durumIds = null, List<string>? firmaArama = null, DateTime? startDate = null, DateTime? endDate = null);
    Task<bool> CompleteAtamaAsync(int atamaId, int userId, CompleteGmAtamaDto dto);

    // Tamamlanan aramalar (QualitySpecialist dinleme listesi)
    Task<object> GetTamamlananAramalarAsync(int userId, List<int>? donemIds = null, DateTime? startDate = null, DateTime? endDate = null);

    // Dinlemelerim (kullanıcıya atanmış dinleme değerlendirmeleri)
    Task<List<GmDinlemeListDto>> GetDinlemelerimAsync(int userId);

    // Dinleme Takip (admin görünümü - dönem bazlı tüm dinleme atamaları)
    Task<List<GmDinlemeListDto>> GetDinlemeTakipAsync(int donemId, int? dinleyenUserId = null, int? durumId = null);

    // Dinleme Ayar (dönem + müşteri → checklist eşleştirmesi)
    Task<List<GmDinlemeAyarDto>> GetDinlemeAyarlarAsync(int donemId);
    Task<GmDinlemeAyarDto> CreateDinlemeAyarAsync(int donemId, CreateGmDinlemeAyarDto dto);
    Task<GmDinlemeAyarDto?> UpdateDinlemeAyarAsync(int id, UpdateGmDinlemeAyarDto dto);
    Task<bool> DeleteDinlemeAyarAsync(int id);

    // Dinleme Popup (form yükleme, taslak kaydetme, gönderme)
    Task<GmDinlemeFormDto?> GetDinlemeFormAsync(int gmAtamaId, int userId);
    Task<GmDinlemeFormDto?> GetDinlemeEditFormAsync(int dinlemeId);
    Task<object> SaveDinlemeDraftAsync(GmDinlemeSubmitDto dto, int userId);
    Task<object> SubmitDinlemeAsync(GmDinlemeSubmitDto dto, int userId);
}

// Request DTOs (interface ile birlikte tanımlanıyor)
public class CreateDonemSoruRequest
{
    public int CustomerId { get; set; }
    public int GmHedefFirmaId { get; set; }
    public string SoruMetni { get; set; } = string.Empty;
    public string? BeklenenCevap { get; set; }
    public int AranmaSayisi { get; set; } = 1;
    public bool IsKuponlu { get; set; } = false;
    public string? KuponKodu { get; set; }
    public int SiraNo { get; set; } = 0;
}

public class UpdateDonemSoruRequest
{
    public string SoruMetni { get; set; } = string.Empty;
    public string? BeklenenCevap { get; set; }
    public int AranmaSayisi { get; set; } = 1;
    public bool IsKuponlu { get; set; } = false;
    public string? KuponKodu { get; set; }
    public int SiraNo { get; set; } = 0;
}

public class ImportDonemSorularResult
{
    public int Imported { get; set; }
    public int Skipped { get; set; }
    public List<string> Errors { get; set; } = new();
    public List<ImportMatchedItem> Matched { get; set; } = new();
    public List<ImportUnmatchedItem> Unmatched { get; set; } = new();
}

public class ImportMatchedItem
{
    public string HedefFirmaAdi { get; set; } = string.Empty;
    public int HedefFirmaId { get; set; }
    public string SoruMetni { get; set; } = string.Empty;
    public string? BeklenenCevap { get; set; }
    public int AranmaSayisi { get; set; }
}

public class ImportUnmatchedItem
{
    public int RowIndex { get; set; }
    public string ExcelHedefFirmaAdi { get; set; } = string.Empty;
    public string SoruMetni { get; set; } = string.Empty;
    public string? BeklenenCevap { get; set; }
    public int AranmaSayisi { get; set; }
}

public class SaveUnmatchedSoruItem
{
    public int GmHedefFirmaId { get; set; }
    public string SoruMetni { get; set; } = string.Empty;
    public string? BeklenenCevap { get; set; }
    public int AranmaSayisi { get; set; } = 1;
    public int CustomerId { get; set; }
}
