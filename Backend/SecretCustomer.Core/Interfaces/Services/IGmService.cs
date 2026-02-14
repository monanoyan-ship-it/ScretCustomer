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

    // Soru
    Task<List<GmSoruDto>> GetSorularAsync(int? customerId = null, int? hedefFirmaId = null);
    Task<GmSoruDto?> GetSoruByIdAsync(int id);
    Task<GmSoruDto> CreateSoruAsync(CreateGmSoruDto dto);
    Task<GmSoruDto?> UpdateSoruAsync(int id, UpdateGmSoruDto dto);
    Task<bool> DeleteSoruAsync(int id);

    // Donem
    Task<List<GmDonemDto>> GetDonemlerAsync(int? customerId = null);
    Task<GmDonemDetailDto?> GetDonemDetailAsync(int id);
    Task<GmDonemDto> CreateDonemAsync(CreateGmDonemDto dto, int userId);
    Task<GmDonemDto?> UpdateDonemAsync(int id, UpdateGmDonemDto dto);
    Task<bool> DeleteDonemAsync(int id);

    // Donem alt yönetim
    Task<bool> AddDonemPersonelAsync(int donemId, int userId);
    Task<bool> RemoveDonemPersonelAsync(int donemPersonelId);
    Task<bool> AddDonemSoruAsync(int donemId, int soruId, int aranmaSayisi);
    Task<bool> RemoveDonemSoruAsync(int donemSoruId);
    Task<bool> UpdateDonemSoruAsync(int donemSoruId, int aranmaSayisi);
    Task<bool> AddDonemKuponAsync(int donemId, string kuponKodu);
    Task<List<bool>> AddDonemKuponlarAsync(int donemId, List<string> kuponKodlari);
    Task<bool> RemoveDonemKuponAsync(int donemKuponId);

    // Aktif Et (dağıtım algoritması)
    Task<int> AktifEtAsync(int donemId);

    // Dönem tamamla
    Task<bool> TamamlaAsync(int donemId);

    // Dönem kopyala
    Task<int> CopyDonemAsync(int sourceDonemId, string yeniAd, DateTime baslangic, DateTime bitis, int userId);

    // Soru Excel import
    Task<(int imported, int skipped, List<string> errors)> ImportSorularFromExcelAsync(int customerId, int hedefFirmaId, Stream excelStream);

    // Takip (atama listesi)
    Task<List<GmAtamaDto>> GetAtamalarAsync(int donemId, int? userId = null, int? durumId = null);

    // Aramalarım (kullanıcı)
    Task<List<GmAtamaDto>> GetAramalarimAsync(int userId, int? donemId = null);
    Task<bool> CompleteAtamaAsync(int atamaId, int userId, CompleteGmAtamaDto dto);
}
