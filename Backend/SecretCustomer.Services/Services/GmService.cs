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
        entity.DurumId = GmAtamaDurumlari.Ids.Tamamlandi;

        await _context.SaveChangesAsync();
        await _auditLog.LogInfoAsync($"GM Atama tamamlandı (UserId: {userId})", "GolgeMusteri");

        return true;
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
            DurumId = x.DurumId,
            DurumText = GmAtamaDurumlari.GetById(x.DurumId)?.Description ?? "Bilinmiyor",
            DurumCss = GmAtamaDurumlari.GetById(x.DurumId)?.CssClass ?? "bg-secondary"
        };
    }
}
