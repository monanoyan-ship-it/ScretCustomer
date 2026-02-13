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
                SoruSayisi = x.Sorular.Count(s => !s.IsDeleted),
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
                SoruSayisi = x.Sorular.Count(s => !s.IsDeleted),
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
    // SORU
    // =============================================

    public async Task<List<GmSoruDto>> GetSorularAsync(int? customerId = null, int? hedefFirmaId = null)
    {
        var query = _context.GmSorular
            .Include(x => x.Customer)
            .Include(x => x.GmHedefFirma)
            .AsQueryable();

        if (customerId.HasValue)
            query = query.Where(x => x.CustomerId == customerId.Value);

        if (hedefFirmaId.HasValue)
            query = query.Where(x => x.GmHedefFirmaId == hedefFirmaId.Value);

        return await query
            .OrderBy(x => x.SiraNo)
            .ThenBy(x => x.Id)
            .Select(x => new GmSoruDto
            {
                Id = x.Id,
                CustomerId = x.CustomerId,
                CustomerName = x.Customer != null ? x.Customer.CompanyName : null,
                GmHedefFirmaId = x.GmHedefFirmaId,
                HedefFirmaAdi = x.GmHedefFirma != null ? x.GmHedefFirma.FirmaAdi : null,
                SoruMetni = x.SoruMetni,
                BeklenenCevap = x.BeklenenCevap,
                AranmaSayisi = x.AranmaSayisi,
                IsKuponlu = x.IsKuponlu,
                SiraNo = x.SiraNo,
                IsActive = x.IsActive,
                CreatedAt = x.CreatedAt
            })
            .ToListAsync();
    }

    public async Task<GmSoruDto?> GetSoruByIdAsync(int id)
    {
        return await _context.GmSorular
            .Include(x => x.Customer)
            .Include(x => x.GmHedefFirma)
            .Where(x => x.Id == id)
            .Select(x => new GmSoruDto
            {
                Id = x.Id,
                CustomerId = x.CustomerId,
                CustomerName = x.Customer != null ? x.Customer.CompanyName : null,
                GmHedefFirmaId = x.GmHedefFirmaId,
                HedefFirmaAdi = x.GmHedefFirma != null ? x.GmHedefFirma.FirmaAdi : null,
                SoruMetni = x.SoruMetni,
                BeklenenCevap = x.BeklenenCevap,
                AranmaSayisi = x.AranmaSayisi,
                IsKuponlu = x.IsKuponlu,
                SiraNo = x.SiraNo,
                IsActive = x.IsActive,
                CreatedAt = x.CreatedAt
            })
            .FirstOrDefaultAsync();
    }

    public async Task<GmSoruDto> CreateSoruAsync(CreateGmSoruDto dto)
    {
        var entity = new GmSoru
        {
            CustomerId = dto.CustomerId,
            GmHedefFirmaId = dto.GmHedefFirmaId,
            SoruMetni = dto.SoruMetni,
            BeklenenCevap = dto.BeklenenCevap,
            AranmaSayisi = dto.AranmaSayisi,
            IsKuponlu = dto.IsKuponlu,
            SiraNo = dto.SiraNo,
            IsActive = dto.IsActive
        };

        _context.GmSorular.Add(entity);
        await _context.SaveChangesAsync();

        await _auditLog.LogInfoAsync($"GM Soru oluşturuldu: {entity.SoruMetni.Substring(0, Math.Min(50, entity.SoruMetni.Length))}", "GolgeMusteri");

        return (await GetSoruByIdAsync(entity.Id))!;
    }

    public async Task<GmSoruDto?> UpdateSoruAsync(int id, UpdateGmSoruDto dto)
    {
        var entity = await _context.GmSorular.FindAsync(id);
        if (entity == null) return null;

        entity.SoruMetni = dto.SoruMetni;
        entity.BeklenenCevap = dto.BeklenenCevap;
        entity.AranmaSayisi = dto.AranmaSayisi;
        entity.IsKuponlu = dto.IsKuponlu;
        entity.SiraNo = dto.SiraNo;
        entity.IsActive = dto.IsActive;

        await _context.SaveChangesAsync();
        await _auditLog.LogInfoAsync($"GM Soru güncellendi: {entity.SoruMetni.Substring(0, Math.Min(50, entity.SoruMetni.Length))}", "GolgeMusteri");

        return await GetSoruByIdAsync(id);
    }

    public async Task<bool> DeleteSoruAsync(int id)
    {
        var entity = await _context.GmSorular.FindAsync(id);
        if (entity == null) return false;

        entity.IsDeleted = true;
        await _context.SaveChangesAsync();
        await _auditLog.LogInfoAsync("GM Soru silindi", "GolgeMusteri");

        return true;
    }

    // =============================================
    // DÖNEM
    // =============================================

    public async Task<List<GmDonemDto>> GetDonemlerAsync(int? customerId = null)
    {
        var query = _context.GmDonemler
            .Include(x => x.Customer)
            .Include(x => x.OlusturanUser)
            .AsQueryable();

        if (customerId.HasValue)
            query = query.Where(x => x.CustomerId == customerId.Value);

        return await query
            .OrderByDescending(x => x.BaslangicTarihi)
            .Select(x => new GmDonemDto
            {
                Id = x.Id,
                CustomerId = x.CustomerId,
                CustomerName = x.Customer != null ? x.Customer.CompanyName : null,
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
            .Include(x => x.Customer)
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
            .Include(x => x.GmSoru)
                .ThenInclude(s => s!.GmHedefFirma)
            .Where(x => x.GmDonemId == id)
            .Select(x => new GmDonemSoruDto
            {
                Id = x.Id,
                GmSoruId = x.GmSoruId,
                SoruMetni = x.GmSoru != null ? x.GmSoru.SoruMetni : null,
                HedefFirmaAdi = x.GmSoru != null && x.GmSoru.GmHedefFirma != null ? x.GmSoru.GmHedefFirma.FirmaAdi : null,
                BeklenenCevap = x.GmSoru != null ? x.GmSoru.BeklenenCevap : null,
                IsKuponlu = x.GmSoru != null && x.GmSoru.IsKuponlu,
                AranmaSayisi = x.AranmaSayisi
            })
            .ToListAsync();

        var kuponlar = await _context.GmDonemKuponlar
            .Where(x => x.GmDonemId == id)
            .Select(x => new GmDonemKuponDto
            {
                Id = x.Id,
                KuponKodu = x.KuponKodu,
                IsUsed = x.IsUsed
            })
            .ToListAsync();

        return new GmDonemDetailDto
        {
            Id = donem.Id,
            CustomerId = donem.CustomerId,
            CustomerName = donem.Customer?.CompanyName,
            Ad = donem.Ad,
            BaslangicTarihi = donem.BaslangicTarihi,
            BitisTarihi = donem.BitisTarihi,
            DurumId = donem.DurumId,
            DurumText = GmDonemDurumlari.GetById(donem.DurumId)?.Description ?? "Bilinmiyor",
            DurumCss = GmDonemDurumlari.GetById(donem.DurumId)?.CssClass ?? "bg-secondary",
            OlusturanUserId = donem.OlusturanUserId,
            OlusturanUserName = donem.OlusturanUser != null ? donem.OlusturanUser.FirstName + " " + donem.OlusturanUser.LastName : null,
            Personeller = personeller,
            Sorular = sorular,
            Kuponlar = kuponlar
        };
    }

    public async Task<GmDonemDto> CreateDonemAsync(CreateGmDonemDto dto, int userId)
    {
        var entity = new GmDonem
        {
            CustomerId = dto.CustomerId,
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

    public async Task<bool> AddDonemSoruAsync(int donemId, int soruId, int aranmaSayisi)
    {
        var donem = await _context.GmDonemler.FindAsync(donemId);
        if (donem == null || donem.DurumId != GmDonemDurumlari.Ids.Taslak) return false;

        var exists = await _context.GmDonemSorular
            .AnyAsync(x => x.GmDonemId == donemId && x.GmSoruId == soruId);
        if (exists) return false;

        _context.GmDonemSorular.Add(new GmDonemSoru
        {
            GmDonemId = donemId,
            GmSoruId = soruId,
            AranmaSayisi = aranmaSayisi
        });
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> RemoveDonemSoruAsync(int donemSoruId)
    {
        var entity = await _context.GmDonemSorular
            .Include(x => x.GmDonem)
            .FirstOrDefaultAsync(x => x.Id == donemSoruId);
        if (entity == null || entity.GmDonem?.DurumId != GmDonemDurumlari.Ids.Taslak) return false;

        entity.IsDeleted = true;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> UpdateDonemSoruAsync(int donemSoruId, int aranmaSayisi)
    {
        var entity = await _context.GmDonemSorular
            .Include(x => x.GmDonem)
            .FirstOrDefaultAsync(x => x.Id == donemSoruId);
        if (entity == null || entity.GmDonem?.DurumId != GmDonemDurumlari.Ids.Taslak) return false;

        entity.AranmaSayisi = aranmaSayisi;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> AddDonemKuponAsync(int donemId, string kuponKodu)
    {
        var donem = await _context.GmDonemler.FindAsync(donemId);
        if (donem == null || donem.DurumId != GmDonemDurumlari.Ids.Taslak) return false;

        _context.GmDonemKuponlar.Add(new GmDonemKupon
        {
            GmDonemId = donemId,
            KuponKodu = kuponKodu
        });
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<List<bool>> AddDonemKuponlarAsync(int donemId, List<string> kuponKodlari)
    {
        var donem = await _context.GmDonemler.FindAsync(donemId);
        if (donem == null || donem.DurumId != GmDonemDurumlari.Ids.Taslak)
            return kuponKodlari.Select(_ => false).ToList();

        var results = new List<bool>();
        foreach (var kupon in kuponKodlari)
        {
            if (string.IsNullOrWhiteSpace(kupon))
            {
                results.Add(false);
                continue;
            }

            _context.GmDonemKuponlar.Add(new GmDonemKupon
            {
                GmDonemId = donemId,
                KuponKodu = kupon.Trim()
            });
            results.Add(true);
        }

        await _context.SaveChangesAsync();
        return results;
    }

    public async Task<bool> RemoveDonemKuponAsync(int donemKuponId)
    {
        var entity = await _context.GmDonemKuponlar
            .Include(x => x.GmDonem)
            .FirstOrDefaultAsync(x => x.Id == donemKuponId);
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
        var sorular = donem.Sorular.ToList();

        // İş günlerini hesapla
        var isGunleri = GetIsGunleri(donem.BaslangicTarihi, donem.BitisTarihi);
        if (!isGunleri.Any())
            throw new InvalidOperationException("Dönem aralığında iş günü bulunamadı.");

        // Tüm atamaları üret
        var atamalar = new List<GmAtama>();
        var personelIndex = 0;
        var gunIndex = 0;

        foreach (var donemSoru in sorular)
        {
            for (int i = 0; i < donemSoru.AranmaSayisi; i++)
            {
                var personel = personeller[personelIndex % personeller.Count];
                var planTarihi = isGunleri[gunIndex % isGunleri.Count];

                atamalar.Add(new GmAtama
                {
                    GmDonemId = donemId,
                    GmDonemSoruId = donemSoru.Id,
                    UserId = personel.UserId,
                    PlanTarihi = planTarihi,
                    DurumId = GmAtamaDurumlari.Ids.Beklemede
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
                .ThenInclude(ds => ds!.GmSoru)
                    .ThenInclude(s => s!.GmHedefFirma)
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

    public async Task<List<GmAtamaDto>> GetAramalarimAsync(int userId, int? donemId = null)
    {
        var query = _context.GmAtamalar
            .Include(x => x.GmDonem)
            .Include(x => x.GmDonemSoru)
                .ThenInclude(ds => ds!.GmSoru)
                    .ThenInclude(s => s!.GmHedefFirma)
            .Include(x => x.User)
            .Where(x => x.UserId == userId)
            .Where(x => x.GmDonem!.DurumId == GmDonemDurumlari.Ids.Aktif);

        if (donemId.HasValue)
            query = query.Where(x => x.GmDonemId == donemId.Value);

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

    private static GmAtamaDto MapToAtamaDto(GmAtama x)
    {
        var soru = x.GmDonemSoru?.GmSoru;
        var firma = soru?.GmHedefFirma;

        return new GmAtamaDto
        {
            Id = x.Id,
            GmDonemId = x.GmDonemId,
            DonemAdi = x.GmDonem?.Ad,
            GmDonemSoruId = x.GmDonemSoruId,
            SoruMetni = soru?.SoruMetni,
            BeklenenCevap = soru?.BeklenenCevap,
            HedefFirmaAdi = firma?.FirmaAdi,
            HedefFirmaTelefonNo = firma?.TelefonNo,
            IsKuponlu = soru?.IsKuponlu ?? false,
            UserId = x.UserId,
            UserName = x.User != null ? x.User.FirstName + " " + x.User.LastName : null,
            PlanTarihi = x.PlanTarihi,
            GerceklesmeTarihi = x.GerceklesmeTarihi,
            AramaSaati = x.AramaSaati,
            Not = x.Not,
            KuponKodu = x.KuponKodu,
            DurumId = x.DurumId,
            DurumText = GmAtamaDurumlari.GetById(x.DurumId)?.Description ?? "Bilinmiyor",
            DurumCss = GmAtamaDurumlari.GetById(x.DurumId)?.CssClass ?? "bg-secondary"
        };
    }
}
