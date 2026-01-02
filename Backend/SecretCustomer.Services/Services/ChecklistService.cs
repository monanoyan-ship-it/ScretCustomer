using SecretCustomer.Core.DTOs.Checklist;
using SecretCustomer.Core.Entities;
using SecretCustomer.Core.Enums;
using SecretCustomer.Core.Interfaces.Repositories;
using SecretCustomer.Core.Interfaces.Services;

namespace SecretCustomer.Services.Services;

public class ChecklistService : IChecklistService
{
    private readonly IChecklistRepository _checklistRepository;

    public ChecklistService(IChecklistRepository checklistRepository)
    {
        _checklistRepository = checklistRepository;
    }

    // Helper: DateTime'ı UTC'ye çevir (PostgreSQL için gerekli)
    private static DateTime? ToUtc(DateTime? dateTime)
    {
        if (!dateTime.HasValue) return null;
        if (dateTime.Value.Kind == DateTimeKind.Utc) return dateTime;
        return DateTime.SpecifyKind(dateTime.Value, DateTimeKind.Utc);
    }

    public async Task<ChecklistDto?> GetByIdAsync(int id)
    {
        var checklist = await _checklistRepository.GetByIdAsync(id, includeDetails: true);
        return checklist == null ? null : MapToDto(checklist);
    }

    public async Task<IEnumerable<ChecklistDto>> GetAllAsync(bool includeInactive = false)
    {
        var checklists = await _checklistRepository.GetAllAsync(includeInactive);
        return checklists.Select(MapToDto);
    }

    public async Task<IEnumerable<ChecklistDto>> GetFilteredAsync(string? searchText = null, int? customerId = null, int? customerOrganizationId = null, bool includeInactive = false)
    {
        var checklists = await _checklistRepository.GetFilteredAsync(searchText, customerId, customerOrganizationId, includeInactive);
        return checklists.Select(MapToDto);
    }

    public async Task<ChecklistDto> CreateAsync(CreateChecklistDto dto)
    {
        var checklist = new Checklist
        {
            Name = dto.Name,
            Description = dto.Description,
            IsScored = dto.IsScored,
            IsActive = true,
            Version = 1,
            // Kontrol listesi ayarları
            ChecklistType = Enum.TryParse<ChecklistType>(dto.ChecklistType, out var clType) ? clType : ChecklistType.CallPerformance,
            ScoringMethod = Enum.TryParse<ScoringMethod>(dto.ScoringMethod, out var scMethod) ? scMethod : ScoringMethod.Maximum,
            MaxTotalPoints = dto.MaxTotalPoints,
            Code = dto.Code,
            TemplateName = dto.TemplateName,
            ValidFrom = ToUtc(dto.ValidFrom),
            ValidUntil = ToUtc(dto.ValidUntil),
            // Firma ve Organizasyon
            CustomerId = dto.CustomerId,
            CustomerOrganizationId = dto.CustomerOrganizationId,
            // Sorular - Direkt checklist'e bağlı
            Questions = dto.Questions.Select(q => new Question
            {
                Text = q.Text,
                Order = q.Order,
                ScoringType = Enum.TryParse<ScoringType>(q.ScoringType, out var sType) ? sType : ScoringType.Scored,
                WeightPoints = q.WeightPoints,
                MaxPoints = q.MaxPoints,
                PenaltyType = Enum.TryParse<PenaltyType>(q.PenaltyType, out var pType) ? pType : PenaltyType.None,
                AllowNA = q.AllowNA,
                IsRequired = q.IsRequired,
                RecommendedNote = q.RecommendedNote,
                HelpText = q.HelpText,
                // Alt Kriterler
                SubCriteria = q.SubCriteria?.Select(sc => new QuestionSubCriteria
                {
                    Description = sc.Description,
                    Order = sc.Order,
                    WeightPoints = sc.WeightPoints,
                    IsActive = sc.IsActive
                }).ToList() ?? new List<QuestionSubCriteria>()
            }).ToList()
        };

        var created = await _checklistRepository.CreateAsync(checklist);
        return MapToDto(created);
    }

    public async Task<ChecklistDto> UpdateAsync(UpdateChecklistDto dto)
    {
        var existing = await _checklistRepository.GetByIdAsync(dto.Id, includeDetails: true);
        if (existing == null)
            throw new KeyNotFoundException($"Checklist with ID {dto.Id} not found");

        existing.Name = dto.Name;
        existing.Description = dto.Description;
        existing.IsScored = dto.IsScored;
        existing.IsActive = dto.IsActive;
        // Kontrol listesi ayarları
        existing.ChecklistType = Enum.TryParse<ChecklistType>(dto.ChecklistType, out var clType) ? clType : ChecklistType.CallPerformance;
        existing.ScoringMethod = Enum.TryParse<ScoringMethod>(dto.ScoringMethod, out var scMethod) ? scMethod : ScoringMethod.Maximum;
        existing.MaxTotalPoints = dto.MaxTotalPoints;
        existing.Code = dto.Code;
        existing.TemplateName = dto.TemplateName;
        existing.ValidFrom = ToUtc(dto.ValidFrom);
        existing.ValidUntil = ToUtc(dto.ValidUntil);
        // Firma ve Organizasyon
        existing.CustomerId = dto.CustomerId;
        existing.CustomerOrganizationId = dto.CustomerOrganizationId;

        // Soruları güncelle
        UpdateQuestions(existing, dto.Questions);

        var updated = await _checklistRepository.UpdateAsync(existing);
        return MapToDto(updated);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        return await _checklistRepository.DeleteAsync(id);
    }

    public async Task<ChecklistDto> CloneChecklistAsync(int id, string newName)
    {
        var original = await _checklistRepository.GetByIdAsync(id, includeDetails: true);
        if (original == null)
            throw new KeyNotFoundException($"Checklist with ID {id} not found");

        var versionCount = await _checklistRepository.GetVersionCountAsync(newName);

        var cloned = new Checklist
        {
            Name = $"{newName} v{versionCount + 1}",
            Description = original.Description,
            IsScored = original.IsScored,
            IsActive = true,
            Version = versionCount + 1,
            // Kontrol listesi ayarları
            ChecklistType = original.ChecklistType,
            ScoringMethod = original.ScoringMethod,
            MaxTotalPoints = original.MaxTotalPoints,
            Code = original.Code,
            TemplateName = original.TemplateName,
            ValidFrom = original.ValidFrom,
            ValidUntil = original.ValidUntil,
            CustomerId = original.CustomerId,
            CustomerOrganizationId = original.CustomerOrganizationId,
            // Soruları kopyala
            Questions = original.Questions.Select(q => new Question
            {
                Text = q.Text,
                Order = q.Order,
                ScoringType = q.ScoringType,
                WeightPoints = q.WeightPoints,
                MaxPoints = q.MaxPoints,
                PenaltyType = q.PenaltyType,
                AllowNA = q.AllowNA,
                IsRequired = q.IsRequired,
                RecommendedNote = q.RecommendedNote,
                HelpText = q.HelpText,
                // Alt Kriterleri de kopyala
                SubCriteria = q.SubCriteria.Select(sc => new QuestionSubCriteria
                {
                    Description = sc.Description,
                    Order = sc.Order,
                    WeightPoints = sc.WeightPoints,
                    IsActive = sc.IsActive
                }).ToList()
            }).ToList()
        };

        var created = await _checklistRepository.CreateAsync(cloned);
        return MapToDto(created);
    }

    private void UpdateQuestions(Checklist checklist, List<UpdateQuestionDto> questionDtos)
    {
        var existingQuestionIds = checklist.Questions.Select(q => q.Id).ToHashSet();
        var dtoQuestionIds = questionDtos.Where(q => q.Id.HasValue).Select(q => q.Id!.Value).ToHashSet();

        // Silinen soruları işaretle
        var questionsToRemove = checklist.Questions.Where(q => !dtoQuestionIds.Contains(q.Id)).ToList();
        foreach (var question in questionsToRemove)
        {
            question.IsDeleted = true;
        }

        // Güncelle veya ekle
        foreach (var questionDto in questionDtos)
        {
            if (questionDto.Id.HasValue)
            {
                // Mevcut soruyu güncelle
                var question = checklist.Questions.FirstOrDefault(q => q.Id == questionDto.Id.Value);
                if (question != null)
                {
                    question.Text = questionDto.Text;
                    question.Order = questionDto.Order;
                    question.ScoringType = Enum.TryParse<ScoringType>(questionDto.ScoringType, out var sType) ? sType : ScoringType.Scored;
                    question.WeightPoints = questionDto.WeightPoints;
                    question.MaxPoints = questionDto.MaxPoints;
                    question.PenaltyType = Enum.TryParse<PenaltyType>(questionDto.PenaltyType, out var pType) ? pType : PenaltyType.None;
                    question.AllowNA = questionDto.AllowNA;
                    question.IsRequired = questionDto.IsRequired;
                    question.RecommendedNote = questionDto.RecommendedNote;
                    question.HelpText = questionDto.HelpText;

                    // Alt Kriterleri güncelle
                    UpdateSubCriteria(question, questionDto.SubCriteria);
                }
            }
            else
            {
                // Yeni soru ekle
                checklist.Questions.Add(new Question
                {
                    Id = 0,
                    ChecklistId = checklist.Id,
                    Text = questionDto.Text,
                    Order = questionDto.Order,
                    ScoringType = Enum.TryParse<ScoringType>(questionDto.ScoringType, out var sType) ? sType : ScoringType.Scored,
                    WeightPoints = questionDto.WeightPoints,
                    MaxPoints = questionDto.MaxPoints,
                    PenaltyType = Enum.TryParse<PenaltyType>(questionDto.PenaltyType, out var pType) ? pType : PenaltyType.None,
                    AllowNA = questionDto.AllowNA,
                    IsRequired = questionDto.IsRequired,
                    RecommendedNote = questionDto.RecommendedNote,
                    HelpText = questionDto.HelpText,
                    // Alt Kriterler
                    SubCriteria = questionDto.SubCriteria?.Select(sc => new QuestionSubCriteria
                    {
                        Id = 0,
                        Description = sc.Description,
                        Order = sc.Order,
                        WeightPoints = sc.WeightPoints,
                        IsActive = sc.IsActive
                    }).ToList() ?? new List<QuestionSubCriteria>()
                });
            }
        }
    }

    private void UpdateSubCriteria(Question question, List<UpdateSubCriteriaDto>? subCriteriaDtos)
    {
        if (subCriteriaDtos == null)
            return;

        var existingSubCriteriaIds = question.SubCriteria.Select(sc => sc.Id).ToHashSet();
        var dtoSubCriteriaIds = subCriteriaDtos.Where(sc => sc.Id.HasValue).Select(sc => sc.Id!.Value).ToHashSet();

        // Silinen alt kriterleri işaretle
        var subCriteriaToRemove = question.SubCriteria.Where(sc => !dtoSubCriteriaIds.Contains(sc.Id)).ToList();
        foreach (var subCriteria in subCriteriaToRemove)
        {
            subCriteria.IsDeleted = true;
        }

        // Güncelle veya ekle
        foreach (var scDto in subCriteriaDtos)
        {
            if (scDto.Id.HasValue)
            {
                // Mevcut alt kriteri güncelle
                var subCriteria = question.SubCriteria.FirstOrDefault(sc => sc.Id == scDto.Id.Value);
                if (subCriteria != null)
                {
                    subCriteria.Description = scDto.Description;
                    subCriteria.Order = scDto.Order;
                    subCriteria.WeightPoints = scDto.WeightPoints;
                    subCriteria.IsActive = scDto.IsActive;
                }
            }
            else
            {
                // Yeni alt kriter ekle
                question.SubCriteria.Add(new QuestionSubCriteria
                {
                    Id = 0,
                    Description = scDto.Description,
                    Order = scDto.Order,
                    WeightPoints = scDto.WeightPoints,
                    IsActive = scDto.IsActive
                });
            }
        }
    }

    private ChecklistDto MapToDto(Checklist checklist)
    {
        return new ChecklistDto
        {
            Id = checklist.Id,
            Name = checklist.Name,
            Description = checklist.Description,
            IsScored = checklist.IsScored,
            IsActive = checklist.IsActive,
            Version = checklist.Version,
            CreatedAt = checklist.CreatedAt,
            // Kontrol listesi ayarları
            ChecklistType = checklist.ChecklistType.ToString(),
            ChecklistTypeName = GetChecklistTypeName(checklist.ChecklistType),
            ScoringMethod = checklist.ScoringMethod.ToString(),
            ScoringMethodName = GetScoringMethodName(checklist.ScoringMethod),
            MaxTotalPoints = checklist.MaxTotalPoints,
            Code = checklist.Code,
            TemplateName = checklist.TemplateName,
            ValidFrom = checklist.ValidFrom,
            ValidUntil = checklist.ValidUntil,
            // Firma ve Organizasyon
            CustomerId = checklist.CustomerId,
            CustomerName = checklist.Customer?.CompanyName,
            CustomerOrganizationId = checklist.CustomerOrganizationId,
            CustomerOrganizationName = checklist.CustomerOrganization?.Name,
            // Sorular - Direkt checklist'e bağlı
            Questions = checklist.Questions.OrderBy(q => q.Order).Select(q => new QuestionDto
            {
                Id = q.Id,
                Text = q.Text,
                Order = q.Order,
                ScoringType = q.ScoringType.ToString(),
                ScoringTypeName = GetScoringTypeName(q.ScoringType),
                WeightPoints = q.WeightPoints,
                MaxPoints = q.MaxPoints,
                PenaltyType = q.PenaltyType.ToString(),
                PenaltyTypeName = GetPenaltyTypeName(q.PenaltyType),
                AllowNA = q.AllowNA,
                IsRequired = q.IsRequired,
                RecommendedNote = q.RecommendedNote,
                HelpText = q.HelpText,
                // Alt Kriterler
                SubCriteria = q.SubCriteria?.OrderBy(sc => sc.Order).Select(sc => new SubCriteriaDto
                {
                    Id = sc.Id,
                    Description = sc.Description,
                    Order = sc.Order,
                    WeightPoints = sc.WeightPoints,
                    IsActive = sc.IsActive
                }).ToList()
            }).ToList(),
            QuestionCount = checklist.Questions.Count
        };
    }

    private static string GetChecklistTypeName(ChecklistType type) => type switch
    {
        ChecklistType.CallPerformance => "Çağrı Performans",
        ChecklistType.PhysicalAudit => "Fiziksel Denetim",
        ChecklistType.MysteryShopping => "Gizli Müşteri",
        ChecklistType.OnlineEvaluation => "Online Değerlendirme",
        ChecklistType.Survey => "Anket",
        _ => type.ToString()
    };

    private static string GetScoringMethodName(ScoringMethod method) => method switch
    {
        ScoringMethod.Maximum => "Maksimum",
        ScoringMethod.Average => "Ortalama",
        ScoringMethod.WeightedAverage => "Ağırlıklı Ortalama",
        ScoringMethod.Sum => "Toplam",
        _ => method.ToString()
    };

    private static string GetScoringTypeName(ScoringType type) => type switch
    {
        ScoringType.Scored => "Puanlı",
        ScoringType.Unscored => "Puansız",
        ScoringType.Penalty => "Cezalı",
        _ => type.ToString()
    };

    private static string GetPenaltyTypeName(PenaltyType type) => type switch
    {
        PenaltyType.None => "Yok",
        PenaltyType.YellowCard => "Sarı Kart",
        PenaltyType.RedCard => "Kırmızı Kart",
        _ => type.ToString()
    };
}
