using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SecretCustomer.Core.DTOs.Visit;
using SecretCustomer.Core.Entities;
using SecretCustomer.Core.Enums;
using SecretCustomer.Core.Interfaces.Services;
using SecretCustomer.Data;

namespace SecretCustomer.Services.Services;

/// <summary>
/// Ziyaret detay servisi
/// Sektör, alan tanımı ve değer yönetimi
/// </summary>
public class VisitDetailService : IVisitDetailService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<VisitDetailService> _logger;

    public VisitDetailService(ApplicationDbContext context, ILogger<VisitDetailService> logger)
    {
        _context = context;
        _logger = logger;
    }

    // ===== SEKTÖR İŞLEMLERİ =====

    public async Task<IEnumerable<VisitSectorDto>> GetAllSectorsAsync()
    {
        return await _context.VisitSectors
            .Where(s => s.IsActive)
            .OrderBy(s => s.SortOrder)
            .Select(s => new VisitSectorDto
            {
                Id = s.Id,
                Code = s.Code,
                Name = s.Name,
                Description = s.Description,
                IconClass = s.IconClass,
                SortOrder = s.SortOrder,
                IsActive = s.IsActive,
                FieldCount = s.FieldDefinitions.Count(f => f.IsActive)
            })
            .ToListAsync();
    }

    public async Task<VisitSectorDto?> GetSectorByIdAsync(Guid id)
    {
        return await _context.VisitSectors
            .Where(s => s.Id == id)
            .Select(s => new VisitSectorDto
            {
                Id = s.Id,
                Code = s.Code,
                Name = s.Name,
                Description = s.Description,
                IconClass = s.IconClass,
                SortOrder = s.SortOrder,
                IsActive = s.IsActive,
                FieldCount = s.FieldDefinitions.Count(f => f.IsActive)
            })
            .FirstOrDefaultAsync();
    }

    public async Task<VisitSectorDto?> GetSectorByCodeAsync(string code)
    {
        return await _context.VisitSectors
            .Where(s => s.Code == code)
            .Select(s => new VisitSectorDto
            {
                Id = s.Id,
                Code = s.Code,
                Name = s.Name,
                Description = s.Description,
                IconClass = s.IconClass,
                SortOrder = s.SortOrder,
                IsActive = s.IsActive,
                FieldCount = s.FieldDefinitions.Count(f => f.IsActive)
            })
            .FirstOrDefaultAsync();
    }

    public async Task<VisitSectorDto> CreateSectorAsync(SaveVisitSectorDto dto)
    {
        var existing = await _context.VisitSectors.AnyAsync(s => s.Code == dto.Code);
        if (existing)
            throw new InvalidOperationException($"'{dto.Code}' kodlu sektör zaten mevcut.");

        var sector = new VisitSector
        {
            Code = dto.Code,
            Name = dto.Name,
            Description = dto.Description,
            IconClass = dto.IconClass,
            SortOrder = dto.SortOrder,
            IsActive = dto.IsActive
        };

        _context.VisitSectors.Add(sector);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Sector created: {Code}", dto.Code);

        return (await GetSectorByIdAsync(sector.Id))!;
    }

    public async Task<VisitSectorDto> UpdateSectorAsync(Guid id, SaveVisitSectorDto dto)
    {
        var sector = await _context.VisitSectors.FindAsync(id)
            ?? throw new KeyNotFoundException("Sektör bulunamadı.");

        var codeExists = await _context.VisitSectors.AnyAsync(s => s.Code == dto.Code && s.Id != id);
        if (codeExists)
            throw new InvalidOperationException($"'{dto.Code}' kodlu başka bir sektör zaten mevcut.");

        sector.Code = dto.Code;
        sector.Name = dto.Name;
        sector.Description = dto.Description;
        sector.IconClass = dto.IconClass;
        sector.SortOrder = dto.SortOrder;
        sector.IsActive = dto.IsActive;
        sector.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Sector updated: {Id}", id);

        return (await GetSectorByIdAsync(id))!;
    }

    public async Task DeleteSectorAsync(Guid id)
    {
        var sector = await _context.VisitSectors.FindAsync(id)
            ?? throw new KeyNotFoundException("Sektör bulunamadı.");

        var hasFields = await _context.VisitFieldDefinitions.AnyAsync(f => f.SectorId == id);
        if (hasFields)
            throw new InvalidOperationException("Bu sektöre bağlı alan tanımları var. Önce onları silin.");

        sector.IsDeleted = true;
        sector.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Sector deleted: {Id}", id);
    }

    // ===== ALAN TANIMI İŞLEMLERİ =====

    public async Task<IEnumerable<VisitFieldDefinitionDto>> GetAllFieldDefinitionsAsync()
    {
        return await _context.VisitFieldDefinitions
            .Include(f => f.Sector)
            .Where(f => f.IsActive)
            .OrderBy(f => f.SectorId)
            .ThenBy(f => f.Category)
            .ThenBy(f => f.SortOrder)
            .Select(f => MapToFieldDefinitionDto(f))
            .ToListAsync();
    }

    public async Task<IEnumerable<VisitFieldDefinitionDto>> GetFieldDefinitionsBySectorAsync(Guid? sectorId)
    {
        return await _context.VisitFieldDefinitions
            .Include(f => f.Sector)
            .Where(f => f.IsActive && f.SectorId == sectorId)
            .OrderBy(f => f.Category)
            .ThenBy(f => f.SortOrder)
            .Select(f => MapToFieldDefinitionDto(f))
            .ToListAsync();
    }

    public async Task<IEnumerable<VisitFieldDefinitionDto>> GetFieldDefinitionsForVisitAsync(Guid? sectorId)
    {
        // Ortak alanlar (SectorId = null) + sektöre özel alanlar
        return await _context.VisitFieldDefinitions
            .Include(f => f.Sector)
            .Where(f => f.IsActive && (f.SectorId == null || f.SectorId == sectorId))
            .OrderBy(f => f.Category)
            .ThenBy(f => f.SortOrder)
            .Select(f => MapToFieldDefinitionDto(f))
            .ToListAsync();
    }

    public async Task<Dictionary<VisitFieldCategory, List<VisitFieldDefinitionDto>>> GetFieldDefinitionsGroupedAsync(Guid? sectorId)
    {
        var fields = await GetFieldDefinitionsForVisitAsync(sectorId);
        return fields
            .GroupBy(f => f.Category)
            .ToDictionary(g => g.Key, g => g.ToList());
    }

    public async Task<VisitFieldDefinitionDto?> GetFieldDefinitionByIdAsync(Guid id)
    {
        return await _context.VisitFieldDefinitions
            .Include(f => f.Sector)
            .Where(f => f.Id == id)
            .Select(f => MapToFieldDefinitionDto(f))
            .FirstOrDefaultAsync();
    }

    public async Task<VisitFieldDefinitionDto> CreateFieldDefinitionAsync(SaveVisitFieldDefinitionDto dto)
    {
        var existing = await _context.VisitFieldDefinitions
            .AnyAsync(f => f.SectorId == dto.SectorId && f.Code == dto.Code);
        if (existing)
            throw new InvalidOperationException($"'{dto.Code}' kodlu alan bu sektörde zaten mevcut.");

        if (dto.SectorId.HasValue)
        {
            var sectorExists = await _context.VisitSectors.AnyAsync(s => s.Id == dto.SectorId.Value);
            if (!sectorExists)
                throw new KeyNotFoundException("Belirtilen sektör bulunamadı.");
        }

        var field = new VisitFieldDefinition
        {
            SectorId = dto.SectorId,
            Code = dto.Code,
            Name = dto.Name,
            Description = dto.Description,
            FieldType = dto.FieldType,
            Category = dto.Category,
            IsRequired = dto.IsRequired,
            MaxRating = dto.MaxRating,
            MaxLength = dto.MaxLength,
            MinValue = dto.MinValue,
            MaxValue = dto.MaxValue,
            Placeholder = dto.Placeholder,
            HelpText = dto.HelpText,
            SortOrder = dto.SortOrder,
            IsActive = dto.IsActive
        };

        _context.VisitFieldDefinitions.Add(field);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Field definition created: {Code} for sector {SectorId}", dto.Code, dto.SectorId);

        return (await GetFieldDefinitionByIdAsync(field.Id))!;
    }

    public async Task<VisitFieldDefinitionDto> UpdateFieldDefinitionAsync(Guid id, SaveVisitFieldDefinitionDto dto)
    {
        var field = await _context.VisitFieldDefinitions.FindAsync(id)
            ?? throw new KeyNotFoundException("Alan tanımı bulunamadı.");

        var codeExists = await _context.VisitFieldDefinitions
            .AnyAsync(f => f.SectorId == dto.SectorId && f.Code == dto.Code && f.Id != id);
        if (codeExists)
            throw new InvalidOperationException($"'{dto.Code}' kodlu başka bir alan bu sektörde zaten mevcut.");

        field.SectorId = dto.SectorId;
        field.Code = dto.Code;
        field.Name = dto.Name;
        field.Description = dto.Description;
        field.FieldType = dto.FieldType;
        field.Category = dto.Category;
        field.IsRequired = dto.IsRequired;
        field.MaxRating = dto.MaxRating;
        field.MaxLength = dto.MaxLength;
        field.MinValue = dto.MinValue;
        field.MaxValue = dto.MaxValue;
        field.Placeholder = dto.Placeholder;
        field.HelpText = dto.HelpText;
        field.SortOrder = dto.SortOrder;
        field.IsActive = dto.IsActive;
        field.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Field definition updated: {Id}", id);

        return (await GetFieldDefinitionByIdAsync(id))!;
    }

    public async Task DeleteFieldDefinitionAsync(Guid id)
    {
        var field = await _context.VisitFieldDefinitions.FindAsync(id)
            ?? throw new KeyNotFoundException("Alan tanımı bulunamadı.");

        var hasValues = await _context.VisitDetailValues.AnyAsync(v => v.FieldDefinitionId == id);
        if (hasValues)
            throw new InvalidOperationException("Bu alana ait değerler var. Önce onları silin veya alanı pasif yapın.");

        field.IsDeleted = true;
        field.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Field definition deleted: {Id}", id);
    }

    // ===== DEĞER İŞLEMLERİ =====

    public async Task<IEnumerable<VisitDetailValueDto>> GetVisitDetailsAsync(Guid customerVisitId)
    {
        var visitExists = await _context.CustomerVisits.AnyAsync(v => v.Id == customerVisitId);
        if (!visitExists)
            throw new KeyNotFoundException("Ziyaret bulunamadı.");

        return await _context.VisitDetailValues
            .Include(v => v.FieldDefinition)
            .Where(v => v.CustomerVisitId == customerVisitId)
            .OrderBy(v => v.FieldDefinition.Category)
            .ThenBy(v => v.FieldDefinition.SortOrder)
            .Select(v => new VisitDetailValueDto
            {
                Id = v.Id,
                CustomerVisitId = v.CustomerVisitId,
                FieldDefinitionId = v.FieldDefinitionId,
                FieldCode = v.FieldDefinition.Code,
                FieldName = v.FieldDefinition.Name,
                Category = v.FieldDefinition.Category,
                FieldType = v.FieldDefinition.FieldType,
                IsRequired = v.FieldDefinition.IsRequired,
                MaxRating = v.FieldDefinition.MaxRating,
                Value = v.GetValue(),
                IntValue = v.IntValue,
                DecimalValue = v.DecimalValue,
                BoolValue = v.BoolValue,
                StringValue = v.StringValue,
                DateTimeValue = v.DateTimeValue
            })
            .ToListAsync();
    }

    public async Task SaveVisitDetailsAsync(SaveVisitDetailsDto dto)
    {
        var visitExists = await _context.CustomerVisits.AnyAsync(v => v.Id == dto.CustomerVisitId);
        if (!visitExists)
            throw new KeyNotFoundException("Ziyaret bulunamadı.");

        foreach (var input in dto.Values)
        {
            var existing = await _context.VisitDetailValues
                .FirstOrDefaultAsync(v => v.CustomerVisitId == dto.CustomerVisitId && v.FieldDefinitionId == input.FieldDefinitionId);

            if (existing != null)
            {
                existing.SetValue(input.Value);
                existing.UpdatedAt = DateTime.UtcNow;
            }
            else
            {
                var newValue = VisitDetailValue.Create(dto.CustomerVisitId, input.FieldDefinitionId, input.Value);
                _context.VisitDetailValues.Add(newValue);
            }
        }

        await _context.SaveChangesAsync();
        _logger.LogInformation("Visit details saved for {CustomerVisitId}, {Count} values", dto.CustomerVisitId, dto.Values.Count);
    }

    public async Task UpdateFieldValueAsync(Guid customerVisitId, Guid fieldDefinitionId, object? value)
    {
        var visitExists = await _context.CustomerVisits.AnyAsync(v => v.Id == customerVisitId);
        if (!visitExists)
            throw new KeyNotFoundException("Ziyaret bulunamadı.");

        var existing = await _context.VisitDetailValues
            .FirstOrDefaultAsync(v => v.CustomerVisitId == customerVisitId && v.FieldDefinitionId == fieldDefinitionId);

        if (existing != null)
        {
            existing.SetValue(value);
            existing.UpdatedAt = DateTime.UtcNow;
        }
        else
        {
            var newValue = VisitDetailValue.Create(customerVisitId, fieldDefinitionId, value);
            _context.VisitDetailValues.Add(newValue);
        }

        await _context.SaveChangesAsync();
    }

    public async Task<VisitDetailSummaryDto> GetVisitSummaryAsync(Guid customerVisitId)
    {
        var visit = await _context.CustomerVisits
            .FirstOrDefaultAsync(v => v.Id == customerVisitId)
            ?? throw new KeyNotFoundException("Ziyaret bulunamadı.");

        // TODO: CustomerVisit'e SectorId eklendiğinde burayı güncelleyelim
        Guid? sectorId = null;

        var allFields = await GetFieldDefinitionsForVisitAsync(sectorId);
        var values = await _context.VisitDetailValues
            .Where(v => v.CustomerVisitId == customerVisitId)
            .ToListAsync();

        var filledFieldIds = values.Select(v => v.FieldDefinitionId).ToHashSet();
        var filledFields = allFields.Count(f => filledFieldIds.Contains(f.Id));

        var categories = allFields
            .GroupBy(f => f.Category)
            .ToDictionary(
                g => g.Key,
                g => new CategorySummaryDto
                {
                    CategoryName = GetCategoryName(g.Key),
                    TotalFields = g.Count(),
                    FilledFields = g.Count(f => filledFieldIds.Contains(f.Id)),
                    AverageRating = CalculateAverageRating(g, values)
                });

        return new VisitDetailSummaryDto
        {
            CustomerVisitId = customerVisitId,
            SectorId = sectorId,
            SectorName = sectorId.HasValue ? (await GetSectorByIdAsync(sectorId.Value))?.Name ?? "" : "Genel",
            TotalFields = allFields.Count(),
            FilledFields = filledFields,
            CompletionPercentage = allFields.Any() ? (double)filledFields / allFields.Count() * 100 : 0,
            Categories = categories
        };
    }

    // ===== SORGULAMA VE İSTATİSTİK =====

    public async Task<IEnumerable<Guid>> FilterVisitsByFieldValueAsync(Guid fieldDefinitionId, object? value, string @operator = "eq")
    {
        var query = _context.VisitDetailValues
            .Where(v => v.FieldDefinitionId == fieldDefinitionId);

        // Basit eşitlik filtresi
        if (@operator == "eq" && value != null)
        {
            var strValue = value.ToString();
            if (int.TryParse(strValue, out var intVal))
                query = query.Where(v => v.IntValue == intVal);
            else if (bool.TryParse(strValue, out var boolVal))
                query = query.Where(v => v.BoolValue == boolVal);
            else
                query = query.Where(v => v.StringValue == strValue);
        }

        return await query.Select(v => v.CustomerVisitId).Distinct().ToListAsync();
    }

    public async Task<FieldStatisticsDto> GetFieldStatisticsAsync(Guid fieldDefinitionId, Guid? projectId = null, DateTime? fromDate = null, DateTime? toDate = null)
    {
        var field = await _context.VisitFieldDefinitions.FindAsync(fieldDefinitionId)
            ?? throw new KeyNotFoundException("Alan tanımı bulunamadı.");

        var query = _context.VisitDetailValues
            .Include(v => v.CustomerVisit)
            .Where(v => v.FieldDefinitionId == fieldDefinitionId);

        if (projectId.HasValue)
            query = query.Where(v => v.CustomerVisit.ProjectId == projectId.Value);

        if (fromDate.HasValue)
            query = query.Where(v => v.CustomerVisit.PlannedDate >= fromDate.Value);

        if (toDate.HasValue)
            query = query.Where(v => v.CustomerVisit.PlannedDate <= toDate.Value);

        var values = await query.ToListAsync();

        var stats = new FieldStatisticsDto
        {
            FieldDefinitionId = fieldDefinitionId,
            FieldCode = field.Code,
            FieldName = field.Name,
            FieldType = field.FieldType,
            TotalRecords = values.Count
        };

        if (field.FieldType == VisitFieldType.Int || field.FieldType == VisitFieldType.Rating)
        {
            var intValues = values.Where(v => v.IntValue.HasValue).Select(v => v.IntValue!.Value).ToList();
            if (intValues.Any())
            {
                stats.Average = intValues.Average();
                stats.Min = intValues.Min();
                stats.Max = intValues.Max();
                stats.StandardDeviation = CalculateStandardDeviation(intValues.Select(v => (double)v));
                stats.Distribution = intValues.GroupBy(v => v).ToDictionary(g => g.Key, g => g.Count());
            }
        }
        else if (field.FieldType == VisitFieldType.Bool)
        {
            var boolValues = values.Where(v => v.BoolValue.HasValue).ToList();
            stats.TrueCount = boolValues.Count(v => v.BoolValue == true);
            stats.FalseCount = boolValues.Count(v => v.BoolValue == false);
            stats.TruePercentage = boolValues.Any() ? (double)stats.TrueCount / boolValues.Count * 100 : null;
        }

        return stats;
    }

    public async Task<IEnumerable<VisitComparisonDto>> CompareVisitsAsync(IEnumerable<Guid> customerVisitIds)
    {
        var visitIdList = customerVisitIds.ToList();

        var values = await _context.VisitDetailValues
            .Include(v => v.FieldDefinition)
            .Where(v => visitIdList.Contains(v.CustomerVisitId))
            .ToListAsync();

        return values
            .GroupBy(v => v.FieldDefinitionId)
            .Select(g =>
            {
                var first = g.First();
                var numericValues = g.Where(v => v.IntValue.HasValue).Select(v => v.IntValue!.Value).ToList();

                return new VisitComparisonDto
                {
                    FieldDefinitionId = g.Key,
                    FieldCode = first.FieldDefinition.Code,
                    FieldName = first.FieldDefinition.Name,
                    Values = g.Select(v => new VisitComparisonValueDto
                    {
                        CustomerVisitId = v.CustomerVisitId,
                        Value = v.GetValue()
                    }).ToList(),
                    Average = numericValues.Any() ? numericValues.Average() : null,
                    Min = numericValues.Any() ? numericValues.Min() : null,
                    Max = numericValues.Any() ? numericValues.Max() : null
                };
            })
            .OrderBy(c => c.FieldCode)
            .ToList();
    }

    // ===== YARDIMCI METODLAR =====

    private static VisitFieldDefinitionDto MapToFieldDefinitionDto(VisitFieldDefinition f)
    {
        return new VisitFieldDefinitionDto
        {
            Id = f.Id,
            SectorId = f.SectorId,
            SectorName = f.Sector?.Name,
            Code = f.Code,
            Name = f.Name,
            Description = f.Description,
            FieldType = f.FieldType,
            FieldTypeName = GetFieldTypeName(f.FieldType),
            Category = f.Category,
            CategoryName = GetCategoryName(f.Category),
            IsRequired = f.IsRequired,
            MaxRating = f.MaxRating,
            MaxLength = f.MaxLength,
            MinValue = f.MinValue,
            MaxValue = f.MaxValue,
            Placeholder = f.Placeholder,
            HelpText = f.HelpText,
            SortOrder = f.SortOrder,
            IsActive = f.IsActive
        };
    }

    private static string GetFieldTypeName(VisitFieldType type) => type switch
    {
        VisitFieldType.Int => "Tam Sayı",
        VisitFieldType.Decimal => "Ondalıklı Sayı",
        VisitFieldType.Bool => "Evet/Hayır",
        VisitFieldType.String => "Metin",
        VisitFieldType.DateTime => "Tarih/Saat",
        VisitFieldType.Rating => "Puan",
        _ => type.ToString()
    };

    private static string GetCategoryName(VisitFieldCategory category) => category switch
    {
        VisitFieldCategory.Time => "Zaman",
        VisitFieldCategory.Staff => "Personel",
        VisitFieldCategory.Facility => "Tesis",
        VisitFieldCategory.General => "Genel",
        VisitFieldCategory.Sector => "Sektör",
        _ => category.ToString()
    };

    private static double? CalculateAverageRating(IGrouping<VisitFieldCategory, VisitFieldDefinitionDto> group, List<VisitDetailValue> values)
    {
        var ratingFieldIds = group.Where(f => f.FieldType == VisitFieldType.Rating).Select(f => f.Id).ToList();
        var ratingValues = values.Where(v => ratingFieldIds.Contains(v.FieldDefinitionId) && v.IntValue.HasValue).ToList();
        return ratingValues.Any() ? ratingValues.Average(v => v.IntValue!.Value) : null;
    }

    private static double CalculateStandardDeviation(IEnumerable<double> values)
    {
        var list = values.ToList();
        if (list.Count < 2) return 0;

        var avg = list.Average();
        var sumOfSquares = list.Sum(v => Math.Pow(v - avg, 2));
        return Math.Sqrt(sumOfSquares / (list.Count - 1));
    }
}
