using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using SecretCustomer.Core.DTOs.GolgeMusteri;
using SecretCustomer.Core.Entities;
using SecretCustomer.Core.Enums;
using SecretCustomer.Core.Interfaces.Services;
using SecretCustomer.Data;

namespace SecretCustomer.Services.Services;

public class GmService : IGmService
{
    private readonly ApplicationDbContext _context;
    private readonly IAuditLogService _auditLog;

    public GmService(ApplicationDbContext context, IAuditLogService auditLog)
    {
        _context = context;
        _auditLog = auditLog;
    }

    // =============================================
    // HEDEF FIRMA
    // =============================================

    public async Task<List<GmHedefFirmaDto>> GetHedefFirmalarAsync(int? customerId = null)
    {
        var query = _context.GmHedefFirmalar
            .Include(x => x.Customer)
            .AsQueryable();

        if (customerId.HasValue)
            query = query.Where(x => x.CustomerId == customerId.Value);

        return await query
            .OrderBy(x => x.FirmaAdi)
            .Select(x => new GmHedefFirmaDto
            {
                Id = x.Id,
                CustomerId = x.CustomerId,
                CustomerName = x.Customer != null ? x.Customer.CompanyName : null,
                FirmaAdi = x.FirmaAdi,
                TelefonNo = x.TelefonNo,
                Aciklama = x.Aciklama,
                IsActive = x.IsActive,
                SoruSayisi = x.DonemSorular.Count(s => !s.IsDeleted),
                CreatedAt = x.CreatedAt
            })
            .ToListAsync();
    }

    public async Task<GmHedefFirmaDto?> GetHedefFirmaByIdAsync(int id)
    {
        return await _context.GmHedefFirmalar
            .Include(x => x.Customer)
            .Where(x => x.Id == id)
            .Select(x => new GmHedefFirmaDto
            {
                Id = x.Id,
                CustomerId = x.CustomerId,
                CustomerName = x.Customer != null ? x.Customer.CompanyName : null,
                FirmaAdi = x.FirmaAdi,
                TelefonNo = x.TelefonNo,
                Aciklama = x.Aciklama,
                IsActive = x.IsActive,
                SoruSayisi = x.DonemSorular.Count(s => !s.IsDeleted),
                CreatedAt = x.CreatedAt
            })
            .FirstOrDefaultAsync();
    }

    public async Task<GmHedefFirmaDto> CreateHedefFirmaAsync(CreateGmHedefFirmaDto dto)
    {
        var entity = new GmHedefFirma
        {
            CustomerId = dto.CustomerId,
            FirmaAdi = dto.FirmaAdi,
            TelefonNo = dto.TelefonNo,
            Aciklama = dto.Aciklama,
            IsActive = dto.IsActive
        };

        _context.GmHedefFirmalar.Add(entity);
        await _context.SaveChangesAsync();

        await _auditLog.LogInfoAsync($"Hedef firma oluşturuldu: {entity.FirmaAdi}", "GolgeMusteri");

        return (await GetHedefFirmaByIdAsync(entity.Id))!;
    }

    public async Task<GmHedefFirmaDto?> UpdateHedefFirmaAsync(int id, UpdateGmHedefFirmaDto dto)
    {
        var entity = await _context.GmHedefFirmalar.FindAsync(id);
        if (entity == null) return null;

        entity.FirmaAdi = dto.FirmaAdi;
        entity.TelefonNo = dto.TelefonNo;
        entity.Aciklama = dto.Aciklama;
        entity.IsActive = dto.IsActive;

        await _context.SaveChangesAsync();
        await _auditLog.LogInfoAsync($"Hedef firma güncellendi: {entity.FirmaAdi}", "GolgeMusteri");

        return await GetHedefFirmaByIdAsync(id);
    }

    public async Task<bool> DeleteHedefFirmaAsync(int id)
    {
        var entity = await _context.GmHedefFirmalar.FindAsync(id);
        if (entity == null) return false;

        entity.IsDeleted = true;
        await _context.SaveChangesAsync();
        await _auditLog.LogInfoAsync($"Hedef firma silindi: {entity.FirmaAdi}", "GolgeMusteri");

        return true;
    }

    // =============================================
    // DÖNEM SORU (birleşik soru yönetimi)
    // =============================================

    public async Task<List<GmDonemSoruDto>> GetDonemSorularAsync(int? customerId = null, int? hedefFirmaId = null, int? donemId = null)
    {
        var query = _context.GmDonemSorular
            .Include(x => x.Customer)
            .Include(x => x.GmHedefFirma)
            .Include(x => x.GmDonem)
            .AsQueryable();

        if (customerId.HasValue)
            query = query.Where(x => x.CustomerId == customerId.Value);

        if (hedefFirmaId.HasValue)
            query = query.Where(x => x.GmHedefFirmaId == hedefFirmaId.Value);

        if (donemId.HasValue)
            query = query.Where(x => x.GmDonemId == donemId.Value);

        return await query
            .OrderBy(x => x.SiraNo)
            .ThenBy(x => x.Id)
            .Select(x => new GmDonemSoruDto
            {
                Id = x.Id,
                CustomerId = x.CustomerId,
                GmHedefFirmaId = x.GmHedefFirmaId,
                SoruMetni = x.SoruMetni,
                HedefFirmaAdi = x.GmHedefFirma != null ? x.GmHedefFirma.FirmaAdi : null,
                MusteriAdi = x.Customer != null ? x.Customer.CompanyName : null,
                BeklenenCevap = x.BeklenenCevap,
                IsKuponlu = x.IsKuponlu,
                AranmaSayisi = x.AranmaSayisi,
                SiraNo = x.SiraNo,
                KuponKodu = x.KuponKodu,
                GmDonemId = x.GmDonemId,
                DonemAdi = x.GmDonem != null ? x.GmDonem.Ad : null
            })
            .ToListAsync();
    }

    public async Task<GmDonemSoruDto> CreateDonemSoruAsync(int donemId, CreateDonemSoruRequest dto)
    {
        var donem = await _context.GmDonemler.FindAsync(donemId);
        if (donem == null)
            throw new InvalidOperationException("Dönem bulunamadı.");
        // Taslak: her tür soru eklenebilir. Aktif: sadece kuponlu soru eklenebilir.
        if (donem.DurumId == GmDonemDurumlari.Ids.Tamamlandi)
            throw new InvalidOperationException("Tamamlanmış dönemlere soru eklenemez.");
        if (donem.DurumId == GmDonemDurumlari.Ids.Aktif && !dto.IsKuponlu)
            throw new InvalidOperationException("Aktif dönemlere sadece kuponlu soru eklenebilir.");

        var entity = new GmDonemSoru
        {
            GmDonemId = donemId,
            CustomerId = dto.CustomerId,
            GmHedefFirmaId = dto.GmHedefFirmaId,
            SoruMetni = dto.SoruMetni,
            BeklenenCevap = dto.BeklenenCevap,
            AranmaSayisi = dto.AranmaSayisi,
            IsKuponlu = dto.IsKuponlu,
            KuponKodu = dto.KuponKodu,
            SiraNo = dto.SiraNo
        };

        _context.GmDonemSorular.Add(entity);
        await _context.SaveChangesAsync();

        await _auditLog.LogInfoAsync($"GM Dönem soru oluşturuldu: {entity.SoruMetni.Substring(0, Math.Min(50, entity.SoruMetni.Length))}", "GolgeMusteri");

        // Reload with includes
        var result = await _context.GmDonemSorular
            .Include(x => x.Customer)
            .Include(x => x.GmHedefFirma)
            .Include(x => x.GmDonem)
            .Where(x => x.Id == entity.Id)
            .Select(x => new GmDonemSoruDto
            {
                Id = x.Id,
                CustomerId = x.CustomerId,
                GmHedefFirmaId = x.GmHedefFirmaId,
                SoruMetni = x.SoruMetni,
                HedefFirmaAdi = x.GmHedefFirma != null ? x.GmHedefFirma.FirmaAdi : null,
                MusteriAdi = x.Customer != null ? x.Customer.CompanyName : null,
                BeklenenCevap = x.BeklenenCevap,
                IsKuponlu = x.IsKuponlu,
                AranmaSayisi = x.AranmaSayisi,
                SiraNo = x.SiraNo,
                KuponKodu = x.KuponKodu,
                GmDonemId = x.GmDonemId,
                DonemAdi = x.GmDonem != null ? x.GmDonem.Ad : null
            })
            .FirstAsync();

        return result;
    }

    public async Task<GmDonemSoruDto?> UpdateDonemSoruAsync(int donemSoruId, UpdateDonemSoruRequest dto)
    {
        var entity = await _context.GmDonemSorular
            .Include(x => x.GmDonem)
            .FirstOrDefaultAsync(x => x.Id == donemSoruId);
        if (entity == null) return null;

        // Taslak: her soru düzenlenebilir. Aktif: sadece kuponlu sorular düzenlenebilir.
        if (entity.GmDonem?.DurumId == GmDonemDurumlari.Ids.Tamamlandi)
            throw new InvalidOperationException("Tamamlanmış dönemlerdeki sorular güncellenemez.");
        if (entity.GmDonem?.DurumId == GmDonemDurumlari.Ids.Aktif && !entity.IsKuponlu)
            throw new InvalidOperationException("Aktif dönemlerde sadece kuponlu sorular güncellenebilir.");

        entity.SoruMetni = dto.SoruMetni;
        entity.BeklenenCevap = dto.BeklenenCevap;
        entity.AranmaSayisi = dto.AranmaSayisi;
        entity.IsKuponlu = dto.IsKuponlu;
        entity.KuponKodu = dto.KuponKodu;
        entity.SiraNo = dto.SiraNo;

        await _context.SaveChangesAsync();
        await _auditLog.LogInfoAsync($"GM Dönem soru güncellendi: {entity.SoruMetni.Substring(0, Math.Min(50, entity.SoruMetni.Length))}", "GolgeMusteri");

        // Reload
        return await _context.GmDonemSorular
            .Include(x => x.Customer)
            .Include(x => x.GmHedefFirma)
            .Include(x => x.GmDonem)
            .Where(x => x.Id == entity.Id)
            .Select(x => new GmDonemSoruDto
            {
                Id = x.Id,
                CustomerId = x.CustomerId,
                GmHedefFirmaId = x.GmHedefFirmaId,
                SoruMetni = x.SoruMetni,
                HedefFirmaAdi = x.GmHedefFirma != null ? x.GmHedefFirma.FirmaAdi : null,
                MusteriAdi = x.Customer != null ? x.Customer.CompanyName : null,
                BeklenenCevap = x.BeklenenCevap,
                IsKuponlu = x.IsKuponlu,
                AranmaSayisi = x.AranmaSayisi,
                SiraNo = x.SiraNo,
                KuponKodu = x.KuponKodu,
                GmDonemId = x.GmDonemId,
                DonemAdi = x.GmDonem != null ? x.GmDonem.Ad : null
            })
            .FirstOrDefaultAsync();
    }

    public async Task<bool> RemoveDonemSoruAsync(int donemSoruId)
    {
        var entity = await _context.GmDonemSorular
            .Include(x => x.GmDonem)
            .FirstOrDefaultAsync(x => x.Id == donemSoruId);
        if (entity == null) return false;

        // Taslak: her soru silinebilir. Aktif: sadece kuponlu + ataması olmayan sorular silinebilir.
        if (entity.GmDonem?.DurumId == GmDonemDurumlari.Ids.Tamamlandi) return false;
        if (entity.GmDonem?.DurumId == GmDonemDurumlari.Ids.Aktif)
        {
            if (!entity.IsKuponlu) return false;
            // Ataması varsa silinemez
            var hasAtama = await _context.GmAtamalar.AnyAsync(a => a.GmDonemSoruId == entity.Id && !a.IsDeleted);
            if (hasAtama) return false;
        }

        entity.IsDeleted = true;
        await _context.SaveChangesAsync();
        return true;
    }

    // =============================================
    // DÖNEM SORU EXCEL IMPORT
    // =============================================

    public async Task<(int imported, int skipped, List<string> errors)> ImportDonemSorularFromExcelAsync(int donemId, int customerId, int hedefFirmaId, Stream excelStream)
    {
        var donem = await _context.GmDonemler.FindAsync(donemId);
        if (donem == null)
            throw new InvalidOperationException("Dönem bulunamadı.");
        if (donem.DurumId != GmDonemDurumlari.Ids.Taslak)
            throw new InvalidOperationException("Sadece taslak dönemlere soru import edilebilir.");

        var errors = new List<string>();
        int imported = 0, skipped = 0;

        using var workbook = new XLWorkbook(excelStream);
        var ws = workbook.Worksheets.First();

        var lastRow = ws.LastRowUsed()?.RowNumber() ?? 0;

        for (int row = 2; row <= lastRow; row++)
        {
            var soruMetni = ws.Cell(row, 1).GetString()?.Trim();
            var beklenenCevap = ws.Cell(row, 2).GetString()?.Trim();
            var aranmaSayisiStr = ws.Cell(row, 3).GetString()?.Trim();

            if (string.IsNullOrWhiteSpace(soruMetni))
            {
                skipped++;
                continue;
            }

            if (soruMetni.Length > 2000)
            {
                errors.Add($"Satır {row}: Soru metni 2000 karakterden uzun.");
                skipped++;
                continue;
            }

            int aranmaSayisi = 1;
            if (!string.IsNullOrWhiteSpace(aranmaSayisiStr))
            {
                if (!int.TryParse(aranmaSayisiStr, out aranmaSayisi) || aranmaSayisi < 1)
                {
                    errors.Add($"Satır {row}: Geçersiz aranma sayısı '{aranmaSayisiStr}'.");
                    aranmaSayisi = 1;
                }
            }

            var entity = new GmDonemSoru
            {
                GmDonemId = donemId,
                CustomerId = customerId,
                GmHedefFirmaId = hedefFirmaId,
                SoruMetni = soruMetni,
                BeklenenCevap = string.IsNullOrWhiteSpace(beklenenCevap) ? null : beklenenCevap,
                AranmaSayisi = aranmaSayisi,
                SiraNo = 0
            };

            _context.GmDonemSorular.Add(entity);
            imported++;
        }

        if (imported > 0)
        {
            await _context.SaveChangesAsync();
            await _auditLog.LogInfoAsync($"GM Dönem soru Excel import: {imported} soru eklendi (DonemId: {donemId}, HedefFirmaId: {hedefFirmaId})", "GolgeMusteri");
        }

        return (imported, skipped, errors);
    }

    public async Task<ImportDonemSorularResult> ImportDonemSorularWithMatchingAsync(int donemId, Stream excelStream)
    {
        var donem = await _context.GmDonemler.FindAsync(donemId);
        if (donem == null)
            throw new InvalidOperationException("Dönem bulunamadı.");
        if (donem.DurumId != GmDonemDurumlari.Ids.Taslak)
            throw new InvalidOperationException("Sadece taslak dönemlere soru import edilebilir.");

        // Tüm aktif hedef firmaları yükle (case-insensitive eşleştirme için)
        var hedefFirmalar = await _context.GmHedefFirmalar
            .Where(x => !x.IsDeleted)
            .ToListAsync();

        // Aynı isimde birden fazla firma olabilir (farklı müşterilerde) - ilk eşleşeni al
        var firmaLookup = new Dictionary<string, GmHedefFirma>(StringComparer.OrdinalIgnoreCase);
        foreach (var f in hedefFirmalar)
        {
            var key = f.FirmaAdi.Trim();
            if (!firmaLookup.ContainsKey(key))
                firmaLookup[key] = f;
        }

        var result = new ImportDonemSorularResult();

        using var workbook = new XLWorkbook(excelStream);
        var ws = workbook.Worksheets.First();
        var lastRow = ws.LastRowUsed()?.RowNumber() ?? 0;

        for (int row = 2; row <= lastRow; row++)
        {
            var hedefFirmaAdi = ws.Cell(row, 1).GetString()?.Trim();
            var soruMetni = ws.Cell(row, 2).GetString()?.Trim();
            var beklenenCevap = ws.Cell(row, 3).GetString()?.Trim();
            var aranmaSayisiStr = ws.Cell(row, 4).GetString()?.Trim();

            if (string.IsNullOrWhiteSpace(soruMetni))
            {
                result.Skipped++;
                continue;
            }

            if (soruMetni.Length > 2000)
            {
                result.Errors.Add($"Satır {row}: Soru metni 2000 karakterden uzun.");
                result.Skipped++;
                continue;
            }

            int aranmaSayisi = 1;
            if (!string.IsNullOrWhiteSpace(aranmaSayisiStr))
            {
                if (!int.TryParse(aranmaSayisiStr, out aranmaSayisi) || aranmaSayisi < 1)
                {
                    result.Errors.Add($"Satır {row}: Geçersiz aranma sayısı '{aranmaSayisiStr}'.");
                    aranmaSayisi = 1;
                }
            }

            // Hedef firma eşleştir
            var firmaKey = (hedefFirmaAdi ?? "").Trim();
            if (firmaKey.Length > 0 && firmaLookup.TryGetValue(firmaKey, out var firma))
            {
                // Eşleşti → kaydet
                var entity = new GmDonemSoru
                {
                    GmDonemId = donemId,
                    CustomerId = firma.CustomerId,
                    GmHedefFirmaId = firma.Id,
                    SoruMetni = soruMetni,
                    BeklenenCevap = string.IsNullOrWhiteSpace(beklenenCevap) ? null : beklenenCevap,
                    AranmaSayisi = aranmaSayisi,
                    SiraNo = 0
                };
                _context.GmDonemSorular.Add(entity);
                result.Imported++;

                result.Matched.Add(new ImportMatchedItem
                {
                    HedefFirmaAdi = firma.FirmaAdi,
                    HedefFirmaId = firma.Id,
                    SoruMetni = soruMetni,
                    BeklenenCevap = string.IsNullOrWhiteSpace(beklenenCevap) ? null : beklenenCevap,
                    AranmaSayisi = aranmaSayisi
                });
            }
            else
            {
                // Eşleşmedi → listeye ekle
                result.Unmatched.Add(new ImportUnmatchedItem
                {
                    RowIndex = row,
                    ExcelHedefFirmaAdi = hedefFirmaAdi ?? "",
                    SoruMetni = soruMetni,
                    BeklenenCevap = string.IsNullOrWhiteSpace(beklenenCevap) ? null : beklenenCevap,
                    AranmaSayisi = aranmaSayisi
                });
            }
        }

        if (result.Imported > 0)
        {
            await _context.SaveChangesAsync();
            await _auditLog.LogInfoAsync($"GM Dönem soru Excel import (eşleştirmeli): {result.Imported} soru eklendi, {result.Unmatched.Count} eşleşmedi (DonemId: {donemId})", "GolgeMusteri");
        }

        return result;
    }

    public async Task<int> SaveUnmatchedSorularAsync(int donemId, List<SaveUnmatchedSoruItem> items)
    {
        var donem = await _context.GmDonemler.FindAsync(donemId);
        if (donem == null)
            throw new InvalidOperationException("Dönem bulunamadı.");
        if (donem.DurumId != GmDonemDurumlari.Ids.Taslak)
            throw new InvalidOperationException("Sadece taslak dönemlere soru eklenebilir.");

        int saved = 0;
        foreach (var item in items)
        {
            if (item.GmHedefFirmaId <= 0 || string.IsNullOrWhiteSpace(item.SoruMetni))
                continue;

            var entity = new GmDonemSoru
            {
                GmDonemId = donemId,
                CustomerId = item.CustomerId,
                GmHedefFirmaId = item.GmHedefFirmaId,
                SoruMetni = item.SoruMetni,
                BeklenenCevap = string.IsNullOrWhiteSpace(item.BeklenenCevap) ? null : item.BeklenenCevap,
                AranmaSayisi = item.AranmaSayisi < 1 ? 1 : item.AranmaSayisi,
                SiraNo = 0
            };
            _context.GmDonemSorular.Add(entity);
            saved++;
        }

        if (saved > 0)
        {
            await _context.SaveChangesAsync();
            await _auditLog.LogInfoAsync($"GM Dönem eşleşmeyen sorular kaydedildi: {saved} soru (DonemId: {donemId})", "GolgeMusteri");
        }

        return saved;
    }

    // =============================================
    // DÖNEM
    // =============================================

    public async Task<List<GmDonemDto>> GetDonemlerAsync()
    {
        var query = _context.GmDonemler
            .Include(x => x.OlusturanUser)
            .AsQueryable();

        return await query
            .OrderByDescending(x => x.BaslangicTarihi)
            .Select(x => new GmDonemDto
            {
                Id = x.Id,
                Ad = x.Ad,
                BaslangicTarihi = x.BaslangicTarihi,
                BitisTarihi = x.BitisTarihi,
                DurumId = x.DurumId,
                DurumText = GmDonemDurumlari.GetById(x.DurumId) != null ? GmDonemDurumlari.GetById(x.DurumId)!.Description : "Bilinmiyor",
                DurumCss = GmDonemDurumlari.GetById(x.DurumId) != null ? GmDonemDurumlari.GetById(x.DurumId)!.CssClass : "bg-secondary",
                OlusturanUserId = x.OlusturanUserId,
                OlusturanUserName = x.OlusturanUser != null ? x.OlusturanUser.FirstName + " " + x.OlusturanUser.LastName : null,
                PersonelSayisi = x.Personeller.Count(p => !p.IsDeleted),
                SoruSayisi = x.Sorular.Count(s => !s.IsDeleted),
                KuponSayisi = x.Kuponlar.Count(k => !k.IsDeleted),
                ToplamAtama = x.Atamalar.Count(a => !a.IsDeleted),
                TamamlananAtama = x.Atamalar.Count(a => !a.IsDeleted && a.DurumId == GmAtamaDurumlari.Ids.Tamamlandi),
                CreatedAt = x.CreatedAt
            })
            .ToListAsync();
    }

    public async Task<GmDonemDetailDto?> GetDonemDetailAsync(int id)
    {
        var donem = await _context.GmDonemler
            .Include(x => x.OlusturanUser)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (donem == null) return null;

        var personeller = await _context.GmDonemPersoneller
            .Include(x => x.User)
            .Where(x => x.GmDonemId == id)
            .Select(x => new GmDonemPersonelDto
            {
                Id = x.Id,
                UserId = x.UserId,
                UserName = x.User != null ? x.User.FirstName + " " + x.User.LastName : null
            })
            .ToListAsync();

        var sorular = await _context.GmDonemSorular
            .Include(x => x.GmHedefFirma)
            .Include(x => x.Customer)
            .Where(x => x.GmDonemId == id)
            .Select(x => new GmDonemSoruDto
            {
                Id = x.Id,
                CustomerId = x.CustomerId,
                GmHedefFirmaId = x.GmHedefFirmaId,
                SoruMetni = x.SoruMetni,
                HedefFirmaAdi = x.GmHedefFirma != null ? x.GmHedefFirma.FirmaAdi : null,
                MusteriAdi = x.Customer != null ? x.Customer.CompanyName : null,
                BeklenenCevap = x.BeklenenCevap,
                IsKuponlu = x.IsKuponlu,
                AranmaSayisi = x.AranmaSayisi,
                SiraNo = x.SiraNo,
                KuponKodu = x.KuponKodu
            })
            .ToListAsync();

        return new GmDonemDetailDto
        {
            Id = donem.Id,
            Ad = donem.Ad,
            BaslangicTarihi = donem.BaslangicTarihi,
            BitisTarihi = donem.BitisTarihi,
            DurumId = donem.DurumId,
            DurumText = GmDonemDurumlari.GetById(donem.DurumId)?.Description ?? "Bilinmiyor",
            DurumCss = GmDonemDurumlari.GetById(donem.DurumId)?.CssClass ?? "bg-secondary",
            OlusturanUserId = donem.OlusturanUserId,
            OlusturanUserName = donem.OlusturanUser != null ? donem.OlusturanUser.FirstName + " " + donem.OlusturanUser.LastName : null,
            Personeller = personeller,
            Sorular = sorular
        };
    }

    public async Task<GmDonemDto> CreateDonemAsync(CreateGmDonemDto dto, int userId)
    {
        var entity = new GmDonem
        {
            Ad = dto.Ad,
            BaslangicTarihi = dto.BaslangicTarihi,
            BitisTarihi = dto.BitisTarihi,
            DurumId = GmDonemDurumlari.Ids.Taslak,
            OlusturanUserId = userId
        };

        _context.GmDonemler.Add(entity);
        await _context.SaveChangesAsync();

        await _auditLog.LogInfoAsync($"GM Dönem oluşturuldu: {entity.Ad}", "GolgeMusteri");

        return (await GetDonemlerAsync()).First(d => d.Id == entity.Id);
    }

    public async Task<GmDonemDto?> UpdateDonemAsync(int id, UpdateGmDonemDto dto)
    {
        var entity = await _context.GmDonemler.FindAsync(id);
        if (entity == null) return null;

        if (entity.DurumId != GmDonemDurumlari.Ids.Taslak)
            throw new InvalidOperationException("Sadece taslak dönemler güncellenebilir.");

        entity.Ad = dto.Ad;
        entity.BaslangicTarihi = dto.BaslangicTarihi;
        entity.BitisTarihi = dto.BitisTarihi;

        await _context.SaveChangesAsync();
        await _auditLog.LogInfoAsync($"GM Dönem güncellendi: {entity.Ad}", "GolgeMusteri");

        return (await GetDonemlerAsync()).FirstOrDefault(d => d.Id == entity.Id);
    }

    public async Task<bool> DeleteDonemAsync(int id)
    {
        var entity = await _context.GmDonemler.FindAsync(id);
        if (entity == null) return false;

        if (entity.DurumId != GmDonemDurumlari.Ids.Taslak)
            throw new InvalidOperationException("Sadece taslak dönemler silinebilir.");

        entity.IsDeleted = true;
        await _context.SaveChangesAsync();
        await _auditLog.LogInfoAsync($"GM Dönem silindi: {entity.Ad}", "GolgeMusteri");

        return true;
    }

    // =============================================
    // DÖNEM KOPYALA
    // =============================================

    public async Task<int> CopyDonemAsync(int sourceDonemId, string yeniAd, DateTime baslangic, DateTime bitis, int userId)
    {
        var source = await _context.GmDonemler
            .Include(d => d.Personeller.Where(p => !p.IsDeleted))
            .Include(d => d.Sorular.Where(s => !s.IsDeleted))
            .FirstOrDefaultAsync(d => d.Id == sourceDonemId && !d.IsDeleted);

        if (source == null)
            throw new InvalidOperationException("Kaynak dönem bulunamadı.");

        var yeniDonem = new GmDonem
        {
            Ad = yeniAd,
            BaslangicTarihi = baslangic,
            BitisTarihi = bitis,
            DurumId = GmDonemDurumlari.Ids.Taslak,
            OlusturanUserId = userId
        };

        _context.GmDonemler.Add(yeniDonem);
        await _context.SaveChangesAsync();

        // Personelleri kopyala
        foreach (var p in source.Personeller)
        {
            _context.GmDonemPersoneller.Add(new GmDonemPersonel
            {
                GmDonemId = yeniDonem.Id,
                UserId = p.UserId
            });
        }

        // Soruları kopyala (tüm alanlarıyla)
        foreach (var s in source.Sorular)
        {
            _context.GmDonemSorular.Add(new GmDonemSoru
            {
                GmDonemId = yeniDonem.Id,
                CustomerId = s.CustomerId,
                GmHedefFirmaId = s.GmHedefFirmaId,
                SoruMetni = s.SoruMetni,
                BeklenenCevap = s.BeklenenCevap,
                AranmaSayisi = s.AranmaSayisi,
                IsKuponlu = s.IsKuponlu,
                SiraNo = s.SiraNo
                // KuponKodu kopyalanmıyor
            });
        }

        await _context.SaveChangesAsync();

        await _auditLog.LogInfoAsync($"GM Dönem kopyalandı: {source.Ad} → {yeniAd}", "GolgeMusteri");

        return yeniDonem.Id;
    }

    // =============================================
    // DÖNEM ALT YÖNETİM
    // =============================================

    public async Task<bool> AddDonemPersonelAsync(int donemId, int userId)
    {
        var donem = await _context.GmDonemler.FindAsync(donemId);
        if (donem == null || donem.DurumId != GmDonemDurumlari.Ids.Taslak) return false;

        // Zaten ekli mi kontrol
        var exists = await _context.GmDonemPersoneller
            .AnyAsync(x => x.GmDonemId == donemId && x.UserId == userId);
        if (exists) return false;

        _context.GmDonemPersoneller.Add(new GmDonemPersonel
        {
            GmDonemId = donemId,
            UserId = userId
        });
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> RemoveDonemPersonelAsync(int donemPersonelId)
    {
        var entity = await _context.GmDonemPersoneller
            .Include(x => x.GmDonem)
            .FirstOrDefaultAsync(x => x.Id == donemPersonelId);
        if (entity == null || entity.GmDonem?.DurumId != GmDonemDurumlari.Ids.Taslak) return false;

        entity.IsDeleted = true;
        await _context.SaveChangesAsync();
        return true;
    }

    // =============================================
    // AKTİF ET (DAĞITIM ALGORİTMASI)
    // =============================================

    public async Task<int> AktifEtAsync(int donemId)
    {
        var donem = await _context.GmDonemler
            .Include(x => x.Personeller.Where(p => !p.IsDeleted))
            .Include(x => x.Sorular.Where(s => !s.IsDeleted))
            .FirstOrDefaultAsync(x => x.Id == donemId);

        if (donem == null)
            throw new InvalidOperationException("Dönem bulunamadı.");

        if (donem.DurumId != GmDonemDurumlari.Ids.Taslak)
            throw new InvalidOperationException("Sadece taslak dönemler aktif edilebilir.");

        if (!donem.Personeller.Any())
            throw new InvalidOperationException("Döneme en az bir personel eklenmeli.");

        if (!donem.Sorular.Any())
            throw new InvalidOperationException("Döneme en az bir soru eklenmeli.");

        var personeller = donem.Personeller.ToList();
        // Kuponlu sorular aktif etten hariç - sonradan ayrıca dağıtılacak
        var sorular = donem.Sorular.Where(s => !s.IsKuponlu).ToList();

        // İş günlerini hesapla
        var isGunleri = GetIsGunleri(donem.BaslangicTarihi, donem.BitisTarihi);
        if (!isGunleri.Any())
            throw new InvalidOperationException("Dönem aralığında iş günü bulunamadı.");

        // Normal soru yoksa sadece kuponlu var, atamasız aktif et
        if (!sorular.Any())
        {
            donem.DurumId = GmDonemDurumlari.Ids.Aktif;
            await _context.SaveChangesAsync();
            await _auditLog.LogInfoAsync($"GM Dönem aktif edildi: {donem.Ad}. Normal soru yok, kuponlu sorular sonradan dağıtılacak.", "GolgeMusteri");
            return 0;
        }

        // Tüm atamaları üret
        var atamalar = new List<GmAtama>();
        var personelIndex = 0;
        var gunIndex = 0;

        foreach (var donemSoru in sorular)
        {
            var aranma = donemSoru.AranmaSayisi;
            for (int i = 0; i < aranma; i++)
            {
                var personel = personeller[personelIndex % personeller.Count];
                var planTarihi = isGunleri[gunIndex % isGunleri.Count];

                atamalar.Add(new GmAtama
                {
                    GmDonemId = donemId,
                    GmDonemSoruId = donemSoru.Id,
                    UserId = personel.UserId,
                    PlanTarihi = planTarihi,
                    DurumId = GmAtamaDurumlari.Ids.Beklemede,
                    KuponKodu = donemSoru.KuponKodu
                });

                personelIndex++;
                gunIndex++;
            }
        }

        _context.GmAtamalar.AddRange(atamalar);

        // Durumu aktif yap
        donem.DurumId = GmDonemDurumlari.Ids.Aktif;
        await _context.SaveChangesAsync();

        await _auditLog.LogInfoAsync($"GM Dönem aktif edildi: {donem.Ad}. {atamalar.Count} atama oluşturuldu.", "GolgeMusteri");

        return atamalar.Count;
    }

    // =============================================
    // KUPONLU SORU EXCEL IMPORT (AKTİF DÖNEM İÇİN)
    // Excel formatı: Soru (A), Beklenen Cevap (B), Aranma Sayısı (C), Kupon Kodu (D)
    // =============================================

    public async Task<(int imported, int skipped, List<string> errors)> ImportKuponluSorularFromExcelAsync(int donemId, int customerId, int hedefFirmaId, Stream excelStream)
    {
        var donem = await _context.GmDonemler.FindAsync(donemId);
        if (donem == null)
            throw new InvalidOperationException("Dönem bulunamadı.");
        if (donem.DurumId != GmDonemDurumlari.Ids.Aktif)
            throw new InvalidOperationException("Kuponlu soru import sadece aktif dönemlerde yapılabilir.");

        var errors = new List<string>();
        int imported = 0, skipped = 0;

        using var workbook = new XLWorkbook(excelStream);
        var ws = workbook.Worksheets.First();
        var lastRow = ws.LastRowUsed()?.RowNumber() ?? 0;

        for (int row = 2; row <= lastRow; row++)
        {
            var soruMetni = ws.Cell(row, 1).GetString()?.Trim();
            var beklenenCevap = ws.Cell(row, 2).GetString()?.Trim();
            var aranmaSayisiStr = ws.Cell(row, 3).GetString()?.Trim();
            var kuponKodu = ws.Cell(row, 4).GetString()?.Trim();

            if (string.IsNullOrWhiteSpace(soruMetni))
            {
                skipped++;
                continue;
            }

            if (soruMetni.Length > 2000)
            {
                errors.Add($"Satır {row}: Soru metni 2000 karakterden uzun.");
                skipped++;
                continue;
            }

            int aranmaSayisi = 1;
            if (!string.IsNullOrWhiteSpace(aranmaSayisiStr))
            {
                if (!int.TryParse(aranmaSayisiStr, out aranmaSayisi) || aranmaSayisi < 1)
                {
                    errors.Add($"Satır {row}: Geçersiz aranma sayısı '{aranmaSayisiStr}'.");
                    aranmaSayisi = 1;
                }
            }

            var entity = new GmDonemSoru
            {
                GmDonemId = donemId,
                CustomerId = customerId,
                GmHedefFirmaId = hedefFirmaId,
                SoruMetni = soruMetni,
                BeklenenCevap = string.IsNullOrWhiteSpace(beklenenCevap) ? null : beklenenCevap,
                AranmaSayisi = aranmaSayisi,
                IsKuponlu = true,
                KuponKodu = string.IsNullOrWhiteSpace(kuponKodu) ? null : kuponKodu,
                SiraNo = 0
            };

            _context.GmDonemSorular.Add(entity);
            imported++;
        }

        if (imported > 0)
        {
            await _context.SaveChangesAsync();
            await _auditLog.LogInfoAsync($"GM Kuponlu soru Excel import: {imported} soru eklendi (DonemId: {donemId}, HedefFirmaId: {hedefFirmaId})", "GolgeMusteri");
        }

        return (imported, skipped, errors);
    }

    /// <summary>
    /// Kuponlu soru import (firma eşleştirmeli, aktif dönem).
    /// Excel formatı: Hedef Firma (A), Soru (B), Beklenen Cevap (C), Aranma Sayısı (D), Kupon Kodu (E)
    /// </summary>
    public async Task<ImportDonemSorularResult> ImportKuponluSorularWithMatchingAsync(int donemId, Stream excelStream)
    {
        var donem = await _context.GmDonemler.FindAsync(donemId);
        if (donem == null)
            throw new InvalidOperationException("Dönem bulunamadı.");
        if (donem.DurumId != GmDonemDurumlari.Ids.Aktif)
            throw new InvalidOperationException("Kuponlu soru import sadece aktif dönemlerde yapılabilir.");

        var hedefFirmalar = await _context.GmHedefFirmalar
            .Where(x => !x.IsDeleted)
            .ToListAsync();

        var firmaLookup = new Dictionary<string, GmHedefFirma>(StringComparer.OrdinalIgnoreCase);
        foreach (var f in hedefFirmalar)
        {
            var key = f.FirmaAdi.Trim();
            if (!firmaLookup.ContainsKey(key))
                firmaLookup[key] = f;
        }

        var result = new ImportDonemSorularResult();

        using var workbook = new XLWorkbook(excelStream);
        var ws = workbook.Worksheets.First();
        var lastRow = ws.LastRowUsed()?.RowNumber() ?? 0;

        for (int row = 2; row <= lastRow; row++)
        {
            var hedefFirmaAdi = ws.Cell(row, 1).GetString()?.Trim();
            var soruMetni = ws.Cell(row, 2).GetString()?.Trim();
            var beklenenCevap = ws.Cell(row, 3).GetString()?.Trim();
            var aranmaSayisiStr = ws.Cell(row, 4).GetString()?.Trim();
            var kuponKodu = ws.Cell(row, 5).GetString()?.Trim();

            if (string.IsNullOrWhiteSpace(soruMetni))
            {
                result.Skipped++;
                continue;
            }

            if (soruMetni.Length > 2000)
            {
                result.Errors.Add($"Satır {row}: Soru metni 2000 karakterden uzun.");
                result.Skipped++;
                continue;
            }

            int aranmaSayisi = 1;
            if (!string.IsNullOrWhiteSpace(aranmaSayisiStr))
            {
                if (!int.TryParse(aranmaSayisiStr, out aranmaSayisi) || aranmaSayisi < 1)
                {
                    result.Errors.Add($"Satır {row}: Geçersiz aranma sayısı '{aranmaSayisiStr}'.");
                    aranmaSayisi = 1;
                }
            }

            var firmaKey = (hedefFirmaAdi ?? "").Trim();
            if (firmaKey.Length > 0 && firmaLookup.TryGetValue(firmaKey, out var firma))
            {
                var entity = new GmDonemSoru
                {
                    GmDonemId = donemId,
                    CustomerId = firma.CustomerId,
                    GmHedefFirmaId = firma.Id,
                    SoruMetni = soruMetni,
                    BeklenenCevap = string.IsNullOrWhiteSpace(beklenenCevap) ? null : beklenenCevap,
                    AranmaSayisi = aranmaSayisi,
                    IsKuponlu = true,
                    KuponKodu = string.IsNullOrWhiteSpace(kuponKodu) ? null : kuponKodu,
                    SiraNo = 0
                };
                _context.GmDonemSorular.Add(entity);
                result.Imported++;

                result.Matched.Add(new ImportMatchedItem
                {
                    HedefFirmaAdi = firma.FirmaAdi,
                    HedefFirmaId = firma.Id,
                    SoruMetni = soruMetni,
                    BeklenenCevap = string.IsNullOrWhiteSpace(beklenenCevap) ? null : beklenenCevap,
                    AranmaSayisi = aranmaSayisi
                });
            }
            else
            {
                result.Unmatched.Add(new ImportUnmatchedItem
                {
                    RowIndex = row,
                    ExcelHedefFirmaAdi = hedefFirmaAdi ?? "",
                    SoruMetni = soruMetni,
                    BeklenenCevap = string.IsNullOrWhiteSpace(beklenenCevap) ? null : beklenenCevap,
                    AranmaSayisi = aranmaSayisi
                });
            }
        }

        if (result.Imported > 0)
        {
            await _context.SaveChangesAsync();
            await _auditLog.LogInfoAsync($"GM Kuponlu soru Excel import (eşleştirmeli): {result.Imported} soru eklendi, {result.Unmatched.Count} eşleşmedi (DonemId: {donemId})", "GolgeMusteri");
        }

        return result;
    }

    /// <summary>
    /// Kuponlu import'ta eşleşmeyen soruları kaydet (aktif dönem).
    /// </summary>
    public async Task<int> SaveUnmatchedKuponluSorularAsync(int donemId, List<SaveUnmatchedSoruItem> items)
    {
        var donem = await _context.GmDonemler.FindAsync(donemId);
        if (donem == null)
            throw new InvalidOperationException("Dönem bulunamadı.");
        if (donem.DurumId != GmDonemDurumlari.Ids.Aktif)
            throw new InvalidOperationException("Bu işlem sadece aktif dönemlerde yapılabilir.");

        int saved = 0;
        foreach (var item in items)
        {
            if (item.GmHedefFirmaId <= 0 || string.IsNullOrWhiteSpace(item.SoruMetni))
                continue;

            var entity = new GmDonemSoru
            {
                GmDonemId = donemId,
                CustomerId = item.CustomerId,
                GmHedefFirmaId = item.GmHedefFirmaId,
                SoruMetni = item.SoruMetni,
                BeklenenCevap = string.IsNullOrWhiteSpace(item.BeklenenCevap) ? null : item.BeklenenCevap,
                AranmaSayisi = item.AranmaSayisi < 1 ? 1 : item.AranmaSayisi,
                IsKuponlu = true,
                SiraNo = 0
            };
            _context.GmDonemSorular.Add(entity);
            saved++;
        }

        if (saved > 0)
        {
            await _context.SaveChangesAsync();
            await _auditLog.LogInfoAsync($"GM Kuponlu eşleşmeyen sorular kaydedildi: {saved} soru (DonemId: {donemId})", "GolgeMusteri");
        }

        return saved;
    }

    public async Task<int> KuponluDagitAsync(int donemId)
    {
        var donem = await _context.GmDonemler
            .Include(x => x.Personeller.Where(p => !p.IsDeleted))
            .Include(x => x.Sorular.Where(s => !s.IsDeleted))
            .FirstOrDefaultAsync(x => x.Id == donemId);

        if (donem == null)
            throw new InvalidOperationException("Dönem bulunamadı.");

        if (donem.DurumId != GmDonemDurumlari.Ids.Aktif)
            throw new InvalidOperationException("Kuponlu dağıtım sadece aktif dönemlerde yapılabilir.");

        if (!donem.Personeller.Any())
            throw new InvalidOperationException("Döneme en az bir personel eklenmeli.");

        // Zaten ataması olan kuponlu soru ID'lerini bul
        var atamasiOlanSoruIds = await _context.GmAtamalar
            .Where(a => a.GmDonemId == donemId && !a.IsDeleted)
            .Select(a => a.GmDonemSoruId)
            .Distinct()
            .ToListAsync();

        // Henüz ataması olmayan kuponlu soruları al
        var kuponluSorular = donem.Sorular
            .Where(s => s.IsKuponlu && !atamasiOlanSoruIds.Contains(s.Id))
            .OrderBy(s => s.SiraNo)
            .ToList();

        if (!kuponluSorular.Any())
            throw new InvalidOperationException("Dağıtılacak kuponlu soru bulunamadı.");

        var personeller = donem.Personeller.ToList();
        var isGunleri = GetIsGunleri(donem.BaslangicTarihi, donem.BitisTarihi);
        if (!isGunleri.Any())
            throw new InvalidOperationException("Dönem aralığında iş günü bulunamadı.");

        var atamalar = new List<GmAtama>();
        var personelIndex = 0;
        var gunIndex = 0;

        foreach (var donemSoru in kuponluSorular)
        {
            var aranma = donemSoru.AranmaSayisi;
            for (int i = 0; i < aranma; i++)
            {
                var personel = personeller[personelIndex % personeller.Count];
                var planTarihi = isGunleri[gunIndex % isGunleri.Count];

                atamalar.Add(new GmAtama
                {
                    GmDonemId = donemId,
                    GmDonemSoruId = donemSoru.Id,
                    UserId = personel.UserId,
                    PlanTarihi = planTarihi,
                    DurumId = GmAtamaDurumlari.Ids.Beklemede,
                    KuponKodu = donemSoru.KuponKodu
                });

                personelIndex++;
                gunIndex++;
            }
        }

        _context.GmAtamalar.AddRange(atamalar);
        await _context.SaveChangesAsync();

        await _auditLog.LogInfoAsync($"GM Kuponlu dağıtım: {atamalar.Count} atama oluşturuldu ({kuponluSorular.Count} soru). Dönem: {donem.Ad}", "GolgeMusteri");

        return atamalar.Count;
    }

    public async Task<bool> TamamlaAsync(int donemId)
    {
        var donem = await _context.GmDonemler.FindAsync(donemId);
        if (donem == null || donem.DurumId != GmDonemDurumlari.Ids.Aktif) return false;

        donem.DurumId = GmDonemDurumlari.Ids.Tamamlandi;
        await _context.SaveChangesAsync();

        await _auditLog.LogInfoAsync("GM Dönem tamamlandı.", "GolgeMusteri");

        return true;
    }

    // =============================================
    // TAKİP (ATAMA LİSTESİ)
    // =============================================

    public async Task<List<GmAtamaDto>> GetAtamalarAsync(int donemId, int? userId = null, int? durumId = null)
    {
        var query = _context.GmAtamalar
            .Include(x => x.GmDonem)
            .Include(x => x.GmDonemSoru)
                .ThenInclude(ds => ds!.GmHedefFirma)
            .Include(x => x.User)
            .Where(x => x.GmDonemId == donemId);

        if (userId.HasValue)
            query = query.Where(x => x.UserId == userId.Value);

        if (durumId.HasValue)
            query = query.Where(x => x.DurumId == durumId.Value);

        return await query
            .OrderBy(x => x.PlanTarihi)
            .ThenBy(x => x.User!.FirstName)
            .Select(x => MapToAtamaDto(x))
            .ToListAsync();
    }

    // =============================================
    // ARAMALARIM (KULLANICI)
    // =============================================

    public async Task<List<GmAtamaDto>> GetAramalarimAsync(int userId, List<int>? donemIds = null, List<int>? durumIds = null, List<string>? firmaArama = null, DateTime? startDate = null, DateTime? endDate = null)
    {
        var query = _context.GmAtamalar
            .Include(x => x.GmDonem)
            .Include(x => x.GmDonemSoru)
                .ThenInclude(ds => ds!.GmHedefFirma)
            .Include(x => x.User)
            .Where(x => x.UserId == userId);

        // Dönem filtresi: belirli dönem seçildiyse onu, yoksa sadece aktif dönemleri getir
        if (donemIds != null && donemIds.Any())
            query = query.Where(x => donemIds.Contains(x.GmDonemId));
        else
            query = query.Where(x => x.GmDonem!.DurumId == GmDonemDurumlari.Ids.Aktif);

        // Durum filtresi
        if (durumIds != null && durumIds.Any())
            query = query.Where(x => durumIds.Contains(x.DurumId));

        // Firma arama
        if (firmaArama != null && firmaArama.Any(f => !string.IsNullOrWhiteSpace(f)))
        {
            var searchTerm = firmaArama.First(f => !string.IsNullOrWhiteSpace(f));
            query = query.Where(x => x.GmDonemSoru!.GmHedefFirma != null &&
                EF.Functions.ILike(x.GmDonemSoru.GmHedefFirma.FirmaAdi, "%" + searchTerm + "%"));
        }

        // Tarih aralığı (plan tarihi veya gerçekleşme tarihi)
        if (startDate.HasValue)
        {
            var startUtc = DateTime.SpecifyKind(startDate.Value.Date, DateTimeKind.Utc);
            query = query.Where(x => (x.GerceklesmeTarihi != null && x.GerceklesmeTarihi >= startUtc) ||
                                     (x.PlanTarihi != null && x.PlanTarihi >= startUtc));
        }
        if (endDate.HasValue)
        {
            var endUtc = DateTime.SpecifyKind(endDate.Value.Date.AddDays(1).AddTicks(-1), DateTimeKind.Utc);
            query = query.Where(x => (x.GerceklesmeTarihi != null && x.GerceklesmeTarihi <= endUtc) ||
                                     (x.PlanTarihi != null && x.PlanTarihi <= endUtc));
        }

        return await query
            .OrderBy(x => x.DurumId)
            .ThenBy(x => x.PlanTarihi)
            .Select(x => MapToAtamaDto(x))
            .ToListAsync();
    }

    public async Task<bool> CompleteAtamaAsync(int atamaId, int userId, CompleteGmAtamaDto dto)
    {
        var entity = await _context.GmAtamalar
            .Include(x => x.GmDonem)
            .FirstOrDefaultAsync(x => x.Id == atamaId && x.UserId == userId);

        if (entity == null) return false;

        if (entity.DurumId == GmAtamaDurumlari.Ids.Tamamlandi)
            throw new InvalidOperationException("Bu atama zaten tamamlanmış.");

        if (entity.GmDonem?.DurumId != GmDonemDurumlari.Ids.Aktif)
            throw new InvalidOperationException("Dönem aktif değil.");

        entity.GerceklesmeTarihi = dto.GerceklesmeTarihi;
        entity.AramaSaati = dto.AramaSaati;
        entity.Not = dto.Not;
        entity.KuponKodu = dto.KuponKodu;
        entity.GorusulenTemsilci = dto.GorusulenTemsilci;
        entity.DurumId = GmAtamaDurumlari.Ids.Tamamlandi;

        await _context.SaveChangesAsync();
        await _auditLog.LogInfoAsync($"GM Atama tamamlandı (UserId: {userId})", "GolgeMusteri");

        // Otomatik dinleme oluştur
        await CreateDinlemeForAtamaAsync(entity);

        return true;
    }

    /// <summary>
    /// Tamamlanan arama için otomatik dinleme değerlendirmesi oluşturur.
    /// Kurallar: arayan kendi aramasını dinleyemez, aynı aramaya 2 dinleme oluşturulmaz, round-robin dağıtım.
    /// </summary>
    private async Task CreateDinlemeForAtamaAsync(GmAtama atama)
    {
        // Aynı atamaya zaten dinleme var mı?
        var existingDinleme = await _context.GmDinlemeEvaluations
            .AnyAsync(d => d.GmAtamaId == atama.Id);
        if (existingDinleme) return;

        // DonemSoru'dan CustomerId'yi al
        var donemSoru = await _context.GmDonemSorular
            .FirstOrDefaultAsync(ds => ds.Id == atama.GmDonemSoruId);
        if (donemSoru == null) return;

        // Bu dönem + müşteri için dinleme ayarı var mı? (checklist eşleştirmesi)
        var ayar = await _context.GmDinlemeAyarlar
            .FirstOrDefaultAsync(a => a.GmDonemId == atama.GmDonemId && a.CustomerId == donemSoru.CustomerId);
        if (ayar == null) return; // Ayar yoksa dinleme oluşturma

        // Dinleyici adayları: aktif QualitySpecialist + Inspector, arayan hariç
        var dinleyiciRolleri = new[] { UserRoles.Ids.QualitySpecialist, UserRoles.Ids.Inspector };
        var dinleyiciler = await _context.Users
            .Where(u => dinleyiciRolleri.Contains(u.RoleId) && u.Id != atama.UserId && u.IsActive)
            .Select(u => u.Id)
            .ToListAsync();

        if (!dinleyiciler.Any()) return;

        // Round-robin: en az dinleme atanan kişiyi seç
        var dinlemeSayilari = await _context.GmDinlemeEvaluations
            .Where(d => dinleyiciler.Contains(d.DinleyenUserId) && d.GmAtama!.GmDonemId == atama.GmDonemId)
            .GroupBy(d => d.DinleyenUserId)
            .Select(g => new { UserId = g.Key, Count = g.Count() })
            .ToListAsync();

        var secilenDinleyici = dinleyiciler
            .OrderBy(id => dinlemeSayilari.FirstOrDefault(d => d.UserId == id)?.Count ?? 0)
            .ThenBy(_ => Guid.NewGuid()) // Eşit sayıda ise rastgele
            .First();

        var dinleme = new GmDinlemeEvaluation
        {
            GmAtamaId = atama.Id,
            ChecklistId = ayar.ChecklistId,
            DinleyenUserId = secilenDinleyici,
            DurumId = GmDinlemeDurumlari.Ids.Beklemede
        };

        _context.GmDinlemeEvaluations.Add(dinleme);
        await _context.SaveChangesAsync();
    }

    // =============================================
    // HELPER
    // =============================================

    private static List<DateTime> GetIsGunleri(DateTime baslangic, DateTime bitis)
    {
        var gunler = new List<DateTime>();
        for (var gun = baslangic.Date; gun <= bitis.Date; gun = gun.AddDays(1))
        {
            if (gun.DayOfWeek != DayOfWeek.Saturday && gun.DayOfWeek != DayOfWeek.Sunday)
                gunler.Add(gun);
        }
        return gunler;
    }

    public async Task<object> GetTamamlananAramalarAsync(int userId, List<int>? donemIds = null, DateTime? startDate = null, DateTime? endDate = null)
    {
        var query = _context.GmAtamalar
            .Include(a => a.GmDonem)
            .Include(a => a.GmDonemSoru)
                .ThenInclude(ds => ds.GmHedefFirma)
            .Include(a => a.User)
            .Where(a => a.DurumId == GmAtamaDurumlari.Ids.Tamamlandi)
            .Where(a => a.UserId == userId);

        if (donemIds?.Any() == true)
        {
            query = query.Where(a => donemIds.Contains(a.GmDonemId));
        }
        else
        {
            // Varsayılan: sadece aktif dönemler
            query = query.Where(a => a.GmDonem.DurumId == GmDonemDurumlari.Ids.Aktif);
        }

        if (startDate.HasValue)
        {
            var start = DateTime.SpecifyKind(startDate.Value.Date, DateTimeKind.Utc);
            query = query.Where(a => a.GerceklesmeTarihi >= start || a.PlanTarihi >= start);
        }
        if (endDate.HasValue)
        {
            var end = DateTime.SpecifyKind(endDate.Value.Date.AddDays(1).AddTicks(-1), DateTimeKind.Utc);
            query = query.Where(a => a.GerceklesmeTarihi <= end || a.PlanTarihi <= end);
        }

        var result = await query
            .OrderByDescending(a => a.GerceklesmeTarihi ?? a.PlanTarihi)
            .Select(a => MapToAtamaDto(a))
            .ToListAsync();

        return result;
    }

    // =============================================
    // DINLEMELERIM
    // =============================================

    public async Task<List<GmDinlemeListDto>> GetDinlemelerimAsync(int userId)
    {
        return await _context.GmDinlemeEvaluations
            .Include(d => d.GmAtama)
                .ThenInclude(a => a!.GmDonemSoru)
                    .ThenInclude(ds => ds!.GmHedefFirma)
            .Include(d => d.GmAtama)
                .ThenInclude(a => a!.GmDonem)
            .Include(d => d.GmAtama)
                .ThenInclude(a => a!.User)
            .Include(d => d.Checklist)
            .Where(d => d.DinleyenUserId == userId)
            .OrderByDescending(d => d.CreatedAt)
            .Select(d => new GmDinlemeListDto
            {
                Id = d.Id,
                GmAtamaId = d.GmAtamaId,
                ChecklistName = d.Checklist != null ? d.Checklist.Name : null,
                DurumId = d.DurumId,
                DurumText = GmDinlemeDurumlari.GetById(d.DurumId) != null ? GmDinlemeDurumlari.GetById(d.DurumId)!.Description : "Bilinmiyor",
                DurumCss = GmDinlemeDurumlari.GetById(d.DurumId) != null ? GmDinlemeDurumlari.GetById(d.DurumId)!.CssClass : "bg-secondary",
                Percentage = d.Percentage,
                DinlemeTarihi = d.DinlemeTarihi,
                HedefFirmaAdi = d.GmAtama != null && d.GmAtama.GmDonemSoru != null && d.GmAtama.GmDonemSoru.GmHedefFirma != null
                    ? d.GmAtama.GmDonemSoru.GmHedefFirma.FirmaAdi : null,
                SoruMetni = d.GmAtama != null && d.GmAtama.GmDonemSoru != null
                    ? d.GmAtama.GmDonemSoru.SoruMetni : null,
                AramaTarihi = d.GmAtama != null ? d.GmAtama.GerceklesmeTarihi : null,
                ArayanAdi = d.GmAtama != null && d.GmAtama.User != null
                    ? d.GmAtama.User.FirstName + " " + d.GmAtama.User.LastName : null,
                DonemAdi = d.GmAtama != null && d.GmAtama.GmDonem != null
                    ? d.GmAtama.GmDonem.Ad : null
            })
            .ToListAsync();
    }

    // =============================================
    // DINLEME TAKIP (ADMIN)
    // =============================================

    public async Task<List<GmDinlemeListDto>> GetDinlemeTakipAsync(int donemId, int? dinleyenUserId = null, int? durumId = null)
    {
        var query = _context.GmDinlemeEvaluations
            .Include(d => d.GmAtama)
                .ThenInclude(a => a!.GmDonemSoru)
                    .ThenInclude(ds => ds!.GmHedefFirma)
            .Include(d => d.GmAtama)
                .ThenInclude(a => a!.GmDonem)
            .Include(d => d.GmAtama)
                .ThenInclude(a => a!.User)
            .Include(d => d.Checklist)
            .Include(d => d.DinleyenUser)
            .Where(d => d.GmAtama!.GmDonemId == donemId)
            .AsQueryable();

        if (dinleyenUserId.HasValue)
            query = query.Where(d => d.DinleyenUserId == dinleyenUserId.Value);
        if (durumId.HasValue)
            query = query.Where(d => d.DurumId == durumId.Value);

        return await query
            .OrderByDescending(d => d.CreatedAt)
            .Select(d => new GmDinlemeListDto
            {
                Id = d.Id,
                GmAtamaId = d.GmAtamaId,
                ChecklistName = d.Checklist != null ? d.Checklist.Name : null,
                DurumId = d.DurumId,
                DurumText = GmDinlemeDurumlari.GetById(d.DurumId) != null ? GmDinlemeDurumlari.GetById(d.DurumId)!.Description : "Bilinmiyor",
                DurumCss = GmDinlemeDurumlari.GetById(d.DurumId) != null ? GmDinlemeDurumlari.GetById(d.DurumId)!.CssClass : "bg-secondary",
                Percentage = d.Percentage,
                DinlemeTarihi = d.DinlemeTarihi,
                HedefFirmaAdi = d.GmAtama != null && d.GmAtama.GmDonemSoru != null && d.GmAtama.GmDonemSoru.GmHedefFirma != null
                    ? d.GmAtama.GmDonemSoru.GmHedefFirma.FirmaAdi : null,
                SoruMetni = d.GmAtama != null && d.GmAtama.GmDonemSoru != null
                    ? d.GmAtama.GmDonemSoru.SoruMetni : null,
                AramaTarihi = d.GmAtama != null ? d.GmAtama.GerceklesmeTarihi : null,
                ArayanAdi = d.GmAtama != null && d.GmAtama.User != null
                    ? d.GmAtama.User.FirstName + " " + d.GmAtama.User.LastName : null,
                DonemAdi = d.GmAtama != null && d.GmAtama.GmDonem != null
                    ? d.GmAtama.GmDonem.Ad : null,
                DinleyenAdi = d.DinleyenUser != null
                    ? d.DinleyenUser.FirstName + " " + d.DinleyenUser.LastName : null
            })
            .ToListAsync();
    }

    // =============================================
    // DINLEME AYAR
    // =============================================

    public async Task<List<GmDinlemeAyarDto>> GetDinlemeAyarlarAsync(int donemId)
    {
        return await _context.GmDinlemeAyarlar
            .Include(x => x.Customer)
            .Include(x => x.Checklist)
            .Where(x => x.GmDonemId == donemId)
            .OrderBy(x => x.Customer != null ? x.Customer.CompanyName : "")
            .Select(x => new GmDinlemeAyarDto
            {
                Id = x.Id,
                GmDonemId = x.GmDonemId,
                CustomerId = x.CustomerId,
                CustomerName = x.Customer != null ? x.Customer.CompanyName : null,
                ChecklistId = x.ChecklistId,
                ChecklistName = x.Checklist != null ? x.Checklist.Name : null
            })
            .ToListAsync();
    }

    public async Task<GmDinlemeAyarDto> CreateDinlemeAyarAsync(int donemId, CreateGmDinlemeAyarDto dto)
    {
        // Aynı dönem + müşteri için zaten varsa hata
        var exists = await _context.GmDinlemeAyarlar
            .AnyAsync(x => x.GmDonemId == donemId && x.CustomerId == dto.CustomerId);
        if (exists)
            throw new InvalidOperationException("Bu müşteri için zaten bir dinleme ayarı mevcut.");

        var entity = new GmDinlemeAyar
        {
            GmDonemId = donemId,
            CustomerId = dto.CustomerId,
            ChecklistId = dto.ChecklistId
        };

        _context.GmDinlemeAyarlar.Add(entity);
        await _context.SaveChangesAsync();

        await _auditLog.LogInfoAsync("Dinleme ayarı oluşturuldu.", "GmDinlemeAyar", entity.Id.ToString());

        // DTO dön
        var created = await _context.GmDinlemeAyarlar
            .Include(x => x.Customer)
            .Include(x => x.Checklist)
            .Where(x => x.Id == entity.Id)
            .Select(x => new GmDinlemeAyarDto
            {
                Id = x.Id,
                GmDonemId = x.GmDonemId,
                CustomerId = x.CustomerId,
                CustomerName = x.Customer != null ? x.Customer.CompanyName : null,
                ChecklistId = x.ChecklistId,
                ChecklistName = x.Checklist != null ? x.Checklist.Name : null
            })
            .FirstAsync();

        return created;
    }

    public async Task<GmDinlemeAyarDto?> UpdateDinlemeAyarAsync(int id, UpdateGmDinlemeAyarDto dto)
    {
        var entity = await _context.GmDinlemeAyarlar.FindAsync(id);
        if (entity == null) return null;

        entity.ChecklistId = dto.ChecklistId;
        await _context.SaveChangesAsync();

        return await _context.GmDinlemeAyarlar
            .Include(x => x.Customer)
            .Include(x => x.Checklist)
            .Where(x => x.Id == id)
            .Select(x => new GmDinlemeAyarDto
            {
                Id = x.Id,
                GmDonemId = x.GmDonemId,
                CustomerId = x.CustomerId,
                CustomerName = x.Customer != null ? x.Customer.CompanyName : null,
                ChecklistId = x.ChecklistId,
                ChecklistName = x.Checklist != null ? x.Checklist.Name : null
            })
            .FirstOrDefaultAsync();
    }

    public async Task<bool> DeleteDinlemeAyarAsync(int id)
    {
        var entity = await _context.GmDinlemeAyarlar.FindAsync(id);
        if (entity == null) return false;

        entity.IsDeleted = true;
        await _context.SaveChangesAsync();

        await _auditLog.LogInfoAsync("Dinleme ayarı silindi.", "GmDinlemeAyar", entity.Id.ToString());
        return true;
    }

    // =============================================
    // DINLEME POPUP (FORM / DRAFT / SUBMIT)
    // =============================================

    public async Task<GmDinlemeFormDto?> GetDinlemeFormAsync(int gmAtamaId, int userId)
    {
        // Atamayı bul (tamamlanmış olmalı)
        var atama = await _context.GmAtamalar
            .Include(a => a.GmDonemSoru)
                .ThenInclude(ds => ds!.GmHedefFirma)
            .Include(a => a.GmDonem)
            .Include(a => a.User)
            .FirstOrDefaultAsync(a => a.Id == gmAtamaId && !a.IsDeleted);

        if (atama == null) return null;

        // Bu atama için kullanıcıya atanmış dinleme var mı?
        var dinleme = await _context.GmDinlemeEvaluations
            .FirstOrDefaultAsync(d => d.GmAtamaId == gmAtamaId && d.DinleyenUserId == userId && !d.IsDeleted);

        if (dinleme == null) return null;

        // Checklist'i yükle
        var checklist = await _context.Checklists
            .Include(c => c.Questions.Where(q => !q.IsDeleted))
                .ThenInclude(q => q.SubCriteria.Where(sc => !sc.IsDeleted && sc.IsActive))
            .FirstOrDefaultAsync(c => c.Id == dinleme.ChecklistId && !c.IsDeleted);

        if (checklist == null) return null;

        return new GmDinlemeFormDto
        {
            DinlemeId = dinleme.Id,
            GmAtamaId = gmAtamaId,
            ChecklistId = checklist.Id,
            ChecklistName = checklist.Name ?? "",
            AtamaInfo = BuildAtamaInfo(atama),
            PenaltyGroups = BuildPenaltyGroupsFromQuestions(checklist.Questions.Where(q => !q.IsDeleted).ToList()),
            ExistingAnswers = new List<GmDinlemeExistingAnswerDto>(),
            Comment = null
        };
    }

    public async Task<GmDinlemeFormDto?> GetDinlemeEditFormAsync(int dinlemeId)
    {
        var dinleme = await _context.GmDinlemeEvaluations
            .Include(d => d.Answers)
                .ThenInclude(a => a.SubCriteriaSelections)
            .Include(d => d.GmAtama)
                .ThenInclude(a => a!.GmDonemSoru)
                    .ThenInclude(ds => ds!.GmHedefFirma)
            .Include(d => d.GmAtama)
                .ThenInclude(a => a!.GmDonem)
            .Include(d => d.GmAtama)
                .ThenInclude(a => a!.User)
            .FirstOrDefaultAsync(d => d.Id == dinlemeId && !d.IsDeleted);

        if (dinleme == null) return null;

        var checklist = await _context.Checklists
            .Include(c => c.Questions.Where(q => !q.IsDeleted))
                .ThenInclude(q => q.SubCriteria.Where(sc => !sc.IsDeleted && sc.IsActive))
            .FirstOrDefaultAsync(c => c.Id == dinleme.ChecklistId && !c.IsDeleted);

        if (checklist == null) return null;

        return new GmDinlemeFormDto
        {
            DinlemeId = dinleme.Id,
            GmAtamaId = dinleme.GmAtamaId,
            ChecklistId = checklist.Id,
            ChecklistName = checklist.Name ?? "",
            AtamaInfo = BuildAtamaInfo(dinleme.GmAtama!),
            PenaltyGroups = BuildPenaltyGroupsFromQuestions(checklist.Questions.Where(q => !q.IsDeleted).ToList()),
            ExistingAnswers = dinleme.Answers.Select(a => new GmDinlemeExistingAnswerDto
            {
                Id = a.Id,
                QuestionId = a.QuestionId,
                AnswerNumeric = a.AnswerNumeric,
                AnswerText = a.AnswerText,
                GivenPoints = a.GivenPoints > 0 ? a.GivenPoints : null,
                Notes = null, // GmDinlemeAnswer'da Notes alanı yok
                IsPenaltyApplied = a.ApplyPenalty,
                AppliedPenaltyType = a.ApplyPenalty ? "Penalty" : null,
                SelectedSubCriteriaIds = a.SubCriteriaSelections.Select(s => s.SubCriteriaId).ToList()
            }).ToList(),
            Comment = dinleme.Comment
        };
    }

    public async Task<object> SaveDinlemeDraftAsync(GmDinlemeSubmitDto dto, int userId)
    {
        return await ProcessDinlemeAsync(dto, userId, GmDinlemeDurumlari.Ids.Taslak);
    }

    public async Task<object> SubmitDinlemeAsync(GmDinlemeSubmitDto dto, int userId)
    {
        return await ProcessDinlemeAsync(dto, userId, GmDinlemeDurumlari.Ids.Tamamlandi);
    }

    private async Task<object> ProcessDinlemeAsync(GmDinlemeSubmitDto dto, int userId, int targetDurumId)
    {
        // Dinleme kaydını bul veya kontrol et
        GmDinlemeEvaluation? dinleme = null;

        if (dto.DinlemeId.HasValue && dto.DinlemeId.Value > 0)
        {
            dinleme = await _context.GmDinlemeEvaluations
                .Include(d => d.Answers)
                    .ThenInclude(a => a.SubCriteriaSelections)
                .FirstOrDefaultAsync(d => d.Id == dto.DinlemeId.Value && !d.IsDeleted);
        }

        if (dinleme == null)
            throw new KeyNotFoundException("Dinleme kaydı bulunamadı.");

        // Yetki kontrolü
        if (dinleme.DinleyenUserId != userId)
            throw new UnauthorizedAccessException("Bu dinleme size atanmamış.");

        // Zaten tamamlandıysa tekrar gönderilemez
        if (dinleme.DurumId == GmDinlemeDurumlari.Ids.Tamamlandi && targetDurumId == GmDinlemeDurumlari.Ids.Tamamlandi)
            throw new InvalidOperationException("Bu dinleme zaten tamamlanmış.");

        // Checklist sorularını yükle (puan hesaplama için)
        var checklist = await _context.Checklists
            .Include(c => c.Questions.Where(q => !q.IsDeleted))
            .FirstOrDefaultAsync(c => c.Id == dinleme.ChecklistId && !c.IsDeleted);

        if (checklist == null)
            throw new KeyNotFoundException("Checklist bulunamadı.");

        var allQuestions = checklist.Questions.Where(q => !q.IsDeleted).ToList();

        // Puan hesapla (Maximum modu)
        var scoreResult = CalculateDinlemeScore(allQuestions, dto.Answers);

        // Mevcut cevapları temizle
        if (dinleme.Answers.Any())
        {
            _context.GmDinlemeAnswers.RemoveRange(dinleme.Answers);
        }

        // Yeni cevapları ekle
        foreach (var answerDto in dto.Answers)
        {
            var question = allQuestions.FirstOrDefault(q => q.Id == answerDto.QuestionId);
            if (question == null) continue;

            var answer = new GmDinlemeAnswer
            {
                GmDinlemeEvaluationId = dinleme.Id,
                QuestionId = answerDto.QuestionId,
                AnswerNumeric = answerDto.AnswerNumeric,
                AnswerText = answerDto.AnswerText,
                GivenPoints = answerDto.GivenPoints ?? (answerDto.AnswerNumeric.HasValue ? answerDto.AnswerNumeric.Value : 0),
                EarnedPoints = CalculateDinlemeEarnedPoints(question, answerDto),
                ApplyPenalty = answerDto.ApplyPenalty
            };

            // Alt kriter seçimleri
            if (answerDto.SelectedSubCriteriaIds?.Any() == true)
            {
                foreach (var scId in answerDto.SelectedSubCriteriaIds)
                {
                    answer.SubCriteriaSelections.Add(new GmDinlemeAnswerSubCriteria
                    {
                        SubCriteriaId = scId
                    });
                }
            }

            dinleme.Answers.Add(answer);
        }

        // Dinleme alanlarını güncelle
        dinleme.DurumId = targetDurumId;
        dinleme.TotalScore = scoreResult.TotalEarned;
        dinleme.MaxScore = scoreResult.MaxPossible;
        dinleme.Percentage = scoreResult.Percentage;
        dinleme.YellowCardCount = scoreResult.YellowCardCount;
        dinleme.RedCardCount = scoreResult.RedCardCount;
        dinleme.Comment = dto.Comment;
        dinleme.UpdatedAt = DateTime.UtcNow;

        if (targetDurumId == GmDinlemeDurumlari.Ids.Tamamlandi)
        {
            dinleme.DinlemeTarihi = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();

        var statusText = targetDurumId == GmDinlemeDurumlari.Ids.Tamamlandi ? "tamamlandı" : "taslak kaydedildi";
        await _auditLog.LogInfoAsync($"Dinleme değerlendirmesi {statusText}.", "GmDinleme", dinleme.Id.ToString());

        return new
        {
            message = targetDurumId == GmDinlemeDurumlari.Ids.Tamamlandi
                ? "Dinleme değerlendirmesi başarıyla tamamlandı."
                : "Taslak başarıyla kaydedildi.",
            dinlemeId = dinleme.Id,
            answers = dinleme.Answers.Select(a => new { a.Id, a.QuestionId }).ToList()
        };
    }

    /// <summary>
    /// Maximum modu puan hesaplama (GmDinleme için)
    /// EvaluationService.CalculateScoreCore mantığının basitleştirilmiş versiyonu
    /// </summary>
    private static (decimal TotalEarned, decimal MaxPossible, decimal Percentage, int YellowCardCount, int RedCardCount)
        CalculateDinlemeScore(List<Question> questions, List<GmDinlemeSubmitAnswerDto> answers)
    {
        decimal totalEarned = 0;
        decimal totalMaxPoints = 0;
        int yellowCardCount = 0;
        int redCardCount = 0;

        var answerDict = answers.ToDictionary(a => a.QuestionId, a => a);

        foreach (var question in questions)
        {
            var answer = answerDict.GetValueOrDefault(question.Id);

            // Puansız soruları atla
            if (question.ScoringTypeId == ScoringTypes.Ids.Unscored)
                continue;

            // Cezalı sorular
            if (question.ScoringTypeId == ScoringTypes.Ids.Penalty)
            {
                if (answer != null && answer.ApplyPenalty)
                {
                    var givenPoints = answer.GivenPoints ?? (answer.AnswerNumeric.HasValue ? answer.AnswerNumeric.Value : 0);
                    if (givenPoints > 0)
                    {
                        if (answer.SelectedPenaltyType == "YellowCard")
                            yellowCardCount++;
                        else if (answer.SelectedPenaltyType == "RedCard")
                            redCardCount++;

                        var maxPoints = question.MaxPoints > 0 ? question.MaxPoints : 2m;
                        var penaltyAmount = (givenPoints / maxPoints) * question.WeightPoints;
                        totalEarned -= penaltyAmount;
                    }
                }
                continue;
            }

            // Normal puanlı sorular (Scored)
            var hasAnswer = answer != null && answer.IsIncluded &&
                (answer.GivenPoints.HasValue || answer.AnswerNumeric.HasValue);

            // Zorunlu olmayan ve cevap verilmemiş → atla
            if (!question.IsRequired && !hasAnswer)
                continue;

            totalMaxPoints += question.WeightPoints;

            if (hasAnswer && answer != null)
            {
                var givenPoints = answer.GivenPoints ?? (answer.AnswerNumeric.HasValue ? (decimal)answer.AnswerNumeric.Value : 0);
                var maxPoints = question.MaxPoints > 0 ? question.MaxPoints : 5m;
                totalEarned += (givenPoints / maxPoints) * question.WeightPoints;
            }
        }

        var percentage = totalMaxPoints > 0 ? Math.Max(0, (totalEarned / totalMaxPoints) * 100) : 0;
        percentage = Math.Round(percentage, 2);

        return (Math.Round(totalEarned, 2), Math.Round(totalMaxPoints, 2), percentage, yellowCardCount, redCardCount);
    }

    private static decimal CalculateDinlemeEarnedPoints(Question question, GmDinlemeSubmitAnswerDto answer)
    {
        if (question.ScoringTypeId == ScoringTypes.Ids.Unscored) return 0;

        var givenPoints = answer.GivenPoints ?? (answer.AnswerNumeric.HasValue ? (decimal)answer.AnswerNumeric.Value : 0);
        if (givenPoints == 0) return 0;

        var maxPoints = question.MaxPoints > 0 ? question.MaxPoints : 5m;

        if (question.ScoringTypeId == ScoringTypes.Ids.Penalty && answer.ApplyPenalty)
        {
            return -((givenPoints / maxPoints) * question.WeightPoints);
        }

        return (givenPoints / maxPoints) * question.WeightPoints;
    }

    private static GmDinlemeAtamaInfoDto BuildAtamaInfo(GmAtama atama)
    {
        return new GmDinlemeAtamaInfoDto
        {
            HedefFirmaAdi = atama.GmDonemSoru?.GmHedefFirma?.FirmaAdi,
            SoruMetni = atama.GmDonemSoru?.SoruMetni,
            BeklenenCevap = atama.GmDonemSoru?.BeklenenCevap,
            ArayanAdi = atama.User != null ? atama.User.FirstName + " " + atama.User.LastName : null,
            AramaTarihi = atama.GerceklesmeTarihi,
            AramaSaati = atama.AramaSaati,
            GorusulenTemsilci = atama.GorusulenTemsilci,
            Not = atama.Not,
            DonemAdi = atama.GmDonem?.Ad
        };
    }

    /// <summary>
    /// Soruları PenaltyType'a göre grupla (EvaluationService.BuildPenaltyGroupsFromQuestions kopyası)
    /// </summary>
    private static List<PenaltyGroupDto> BuildPenaltyGroupsFromQuestions(List<Question> questions)
    {
        if (questions == null || !questions.Any())
            return new List<PenaltyGroupDto>();

        var allGroups = new List<(string Name, string PenaltyType, int MinOrder, List<Question> Questions)>();

        // GroupName'i dolu olanları GroupName'e göre grupla
        var groupedByName = questions
            .Where(q => !string.IsNullOrWhiteSpace(q.GroupName))
            .GroupBy(q => q.GroupName!)
            .ToList();

        foreach (var group in groupedByName)
        {
            var groupQuestions = group.OrderBy(q => q.Order).ToList();
            var dominantPenaltyType = group
                .GroupBy(q => q.PenaltyTypeId)
                .OrderByDescending(g => g.Count())
                .First().Key;
            var penaltyTypeName = PenaltyTypes.GetById(dominantPenaltyType)?.SystemName ?? "None";

            allGroups.Add((group.Key, penaltyTypeName, groupQuestions.Min(q => q.Order), groupQuestions));
        }

        // GroupName'i BOŞ olan normal sorular → "Genel"
        var normalWithoutGroup = questions
            .Where(q => string.IsNullOrWhiteSpace(q.GroupName) && q.PenaltyTypeId == PenaltyTypes.Ids.None)
            .OrderBy(q => q.Order)
            .ToList();
        if (normalWithoutGroup.Any())
            allGroups.Add(("Genel", "None", normalWithoutGroup.Min(q => q.Order), normalWithoutGroup));

        // Sarı Kartlar
        var yellowWithoutGroup = questions
            .Where(q => string.IsNullOrWhiteSpace(q.GroupName) && q.PenaltyTypeId == PenaltyTypes.Ids.YellowCard)
            .OrderBy(q => q.Order)
            .ToList();
        if (yellowWithoutGroup.Any())
            allGroups.Add(("Sarı Kartlar", "YellowCard", yellowWithoutGroup.Min(q => q.Order), yellowWithoutGroup));

        // Kırmızı Kartlar
        var redWithoutGroup = questions
            .Where(q => string.IsNullOrWhiteSpace(q.GroupName) && q.PenaltyTypeId == PenaltyTypes.Ids.RedCard)
            .OrderBy(q => q.Order)
            .ToList();
        if (redWithoutGroup.Any())
            allGroups.Add(("Kırmızı Kartlar", "RedCard", redWithoutGroup.Min(q => q.Order), redWithoutGroup));

        var result = new List<PenaltyGroupDto>();
        var order = 1;
        foreach (var group in allGroups.OrderBy(g => g.MinOrder))
        {
            result.Add(new PenaltyGroupDto
            {
                Id = order,
                Name = group.Name,
                Order = order,
                PenaltyType = group.PenaltyType,
                WeightPoints = group.Questions.Sum(q => q.WeightPoints),
                MaxPoints = group.Questions.Sum(q => q.MaxPoints),
                Questions = group.Questions.Select(q => new EvaluationQuestionDto
                {
                    Id = q.Id,
                    Text = q.Text,
                    Order = q.Order,
                    IsRequired = q.IsRequired,
                    ScoringType = ScoringTypes.GetById(q.ScoringTypeId)?.SystemName ?? "Scored",
                    WeightPoints = q.WeightPoints,
                    MaxPoints = q.MaxPoints,
                    PenaltyType = PenaltyTypes.GetById(q.PenaltyTypeId)?.SystemName ?? "None",
                    RecommendedNote = q.RecommendedNote,
                    HelpText = q.HelpText,
                    AllowComment = q.AllowComment,
                    SubCriteria = q.SubCriteria?
                        .Where(sc => !sc.IsDeleted && sc.IsActive)
                        .OrderBy(sc => sc.Order)
                        .Select(sc => new EvaluationSubCriteriaDto
                        {
                            Id = sc.Id,
                            Description = sc.Description,
                            Order = sc.Order,
                            WeightPoints = sc.WeightPoints,
                            IsActive = sc.IsActive
                        }).ToList()
                }).ToList()
            });
            order++;
        }

        return result;
    }

    private static GmAtamaDto MapToAtamaDto(GmAtama x)
    {
        var donemSoru = x.GmDonemSoru;
        var firma = donemSoru?.GmHedefFirma;

        return new GmAtamaDto
        {
            Id = x.Id,
            GmDonemId = x.GmDonemId,
            DonemAdi = x.GmDonem?.Ad,
            GmDonemSoruId = x.GmDonemSoruId,
            SoruMetni = donemSoru?.SoruMetni,
            BeklenenCevap = donemSoru?.BeklenenCevap,
            HedefFirmaAdi = firma?.FirmaAdi,
            HedefFirmaTelefonNo = firma?.TelefonNo,
            IsKuponlu = donemSoru?.IsKuponlu ?? false,
            UserId = x.UserId,
            UserName = x.User != null ? x.User.FirstName + " " + x.User.LastName : null,
            PlanTarihi = x.PlanTarihi,
            GerceklesmeTarihi = x.GerceklesmeTarihi,
            AramaSaati = x.AramaSaati,
            Not = x.Not,
            KuponKodu = x.KuponKodu ?? donemSoru?.KuponKodu,
            GorusulenTemsilci = x.GorusulenTemsilci,
            CustomerId = donemSoru?.CustomerId ?? 0,
            DurumId = x.DurumId,
            DurumText = GmAtamaDurumlari.GetById(x.DurumId)?.Description ?? "Bilinmiyor",
            DurumCss = GmAtamaDurumlari.GetById(x.DurumId)?.CssClass ?? "bg-secondary"
        };
    }
}
