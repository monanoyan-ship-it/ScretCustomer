using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using SecretCustomer.Core.DTOs.Checklist;
using SecretCustomer.Core.DTOs.Report;
using SecretCustomer.Core.Entities;
using SecretCustomer.Core.Enums;
using SecretCustomer.Core.Interfaces.Repositories;
using SecretCustomer.Core.Interfaces.Services;
using SecretCustomer.Data;

namespace SecretCustomer.Services.Services;

public class ChecklistService : IChecklistService
{
    private readonly IChecklistRepository _checklistRepository;
    private readonly ILocalizationService _localizationService;
    private readonly ApplicationDbContext _context;

    public ChecklistService(IChecklistRepository checklistRepository, ILocalizationService localizationService, ApplicationDbContext context)
    {
        _checklistRepository = checklistRepository;
        _localizationService = localizationService;
        _context = context;
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
        return checklist == null ? null : await MapToDtoAsync(checklist);
    }

    public async Task<IEnumerable<ChecklistDto>> GetAllAsync(bool includeInactive = false)
    {
        var checklists = await _checklistRepository.GetAllAsync(includeInactive);
        var result = new List<ChecklistDto>();
        foreach (var checklist in checklists)
            result.Add(await MapToDtoAsync(checklist));
        return result;
    }

    public async Task<IEnumerable<ChecklistDto>> GetFilteredAsync(string? searchText = null, int? customerId = null, int? customerOrganizationId = null, bool includeInactive = false)
    {
        var checklists = await _checklistRepository.GetFilteredAsync(searchText, customerId, customerOrganizationId, includeInactive);
        var result = new List<ChecklistDto>();
        foreach (var checklist in checklists)
            result.Add(await MapToDtoAsync(checklist));
        return result;
    }

    /// <summary>
    /// Liste görünümü için optimize edilmiş method - Projection kullanır, Questions yüklemez
    /// </summary>
    public async Task<IEnumerable<ChecklistListDto>> GetListAsync(string? searchText = null, int? customerId = null, int? customerOrganizationId = null, bool includeInactive = false)
    {
        var query = _context.Checklists
            .Where(c => !c.IsDeleted)
            .AsQueryable();

        if (!includeInactive)
            query = query.Where(c => c.IsActive);

        if (!string.IsNullOrWhiteSpace(searchText))
        {
            var search = searchText.ToLower();
            query = query.Where(c =>
                c.Name.ToLower().Contains(search) ||
                (c.Description != null && c.Description.ToLower().Contains(search)) ||
                (c.Code != null && c.Code.ToLower().Contains(search)));
        }

        if (customerId.HasValue)
            query = query.Where(c => c.CustomerId == customerId.Value);

        if (customerOrganizationId.HasValue)
            query = query.Where(c => c.CustomerOrganizationId == customerOrganizationId.Value);

        return await query
            .OrderByDescending(c => c.CreatedAt)
            .Select(c => new ChecklistListDto
            {
                Id = c.Id,
                Name = c.Name,
                Description = c.Description,
                IsScored = c.IsScored,
                IsActive = c.IsActive,
                Version = c.Version,
                CreatedAt = c.CreatedAt,
                ChecklistType = ChecklistTypes.GetById(c.ChecklistTypeId) != null ? ChecklistTypes.GetById(c.ChecklistTypeId)!.SystemName : "CallPerformance",
                ScoringMethod = ScoringMethods.GetById(c.ScoringMethodId) != null ? ScoringMethods.GetById(c.ScoringMethodId)!.SystemName : "Maximum",
                Code = c.Code,
                MaxTotalPoints = c.MaxTotalPoints,
                ValidFrom = c.ValidFrom,
                ValidUntil = c.ValidUntil,
                CustomerId = c.CustomerId,
                CustomerName = c.Customer != null ? c.Customer.CompanyName : null,
                CustomerOrganizationId = c.CustomerOrganizationId,
                CustomerOrganizationName = c.CustomerOrganization != null ? c.CustomerOrganization.Name : null,
                QuestionCount = c.Questions.Count(q => !q.IsDeleted)
            })
            .ToListAsync();
    }

    /// <summary>
    /// Liste görünümü için - Filter DTO ile (çoklu filtre destekli)
    /// </summary>
    public async Task<IEnumerable<ChecklistListDto>> GetListAsync(ChecklistFilterDto filter)
    {
        var query = _context.Checklists
            .Where(c => !c.IsDeleted)
            .AsQueryable();

        if (!filter.IncludeInactive)
            query = query.Where(c => c.IsActive);

        // Arama
        if (!string.IsNullOrWhiteSpace(filter.SearchText))
        {
            var search = filter.SearchText.ToLower();
            query = query.Where(c =>
                c.Name.ToLower().Contains(search) ||
                (c.Description != null && c.Description.ToLower().Contains(search)) ||
                (c.Code != null && c.Code.ToLower().Contains(search)));
        }

        // Çoklu CustomerIds filtresi
        if (filter.CustomerIds?.Any() == true)
            query = query.Where(c => c.CustomerId.HasValue && filter.CustomerIds.Contains(c.CustomerId.Value));

        // Çoklu CustomerOrganizationIds filtresi
        if (filter.CustomerOrganizationIds?.Any() == true)
            query = query.Where(c => c.CustomerOrganizationId.HasValue && filter.CustomerOrganizationIds.Contains(c.CustomerOrganizationId.Value));

        return await query
            .OrderByDescending(c => c.CreatedAt)
            .Select(c => new ChecklistListDto
            {
                Id = c.Id,
                Name = c.Name,
                Description = c.Description,
                IsScored = c.IsScored,
                IsActive = c.IsActive,
                Version = c.Version,
                CreatedAt = c.CreatedAt,
                ChecklistType = ChecklistTypes.GetById(c.ChecklistTypeId) != null ? ChecklistTypes.GetById(c.ChecklistTypeId)!.SystemName : "CallPerformance",
                ScoringMethod = ScoringMethods.GetById(c.ScoringMethodId) != null ? ScoringMethods.GetById(c.ScoringMethodId)!.SystemName : "Maximum",
                Code = c.Code,
                MaxTotalPoints = c.MaxTotalPoints,
                ValidFrom = c.ValidFrom,
                ValidUntil = c.ValidUntil,
                CustomerId = c.CustomerId,
                CustomerName = c.Customer != null ? c.Customer.CompanyName : null,
                CustomerOrganizationId = c.CustomerOrganizationId,
                CustomerOrganizationName = c.CustomerOrganization != null ? c.CustomerOrganization.Name : null,
                QuestionCount = c.Questions.Count(q => !q.IsDeleted)
            })
            .ToListAsync();
    }

    public async Task<ChecklistDto> CreateAsync(CreateChecklistDto dto)
    {
        // Soru validasyonu: ShowScoreInput kapalıysa, ya SubCriteria olmalı ya da AllowComment açık olmalı
        for (int i = 0; i < dto.Questions.Count; i++)
        {
            var q = dto.Questions[i];
            if (!q.ShowScoreInput)
            {
                var hasSubCriteria = q.SubCriteria != null && q.SubCriteria.Count > 0;
                if (!hasSubCriteria && !q.AllowComment)
                {
                    var errorMsg = await _localizationService.GetResourceAsync(
                        "Checklist.Question.ShowScoreInputValidation",
                        (int?)null,
                        "Soru {0}: 'Puan girişini göster' kapalıyken en az bir alt kriter eklenmeli veya 'Yorum Yapılabilir' işaretlenmelidir.");
                    throw new InvalidOperationException(string.Format(errorMsg, i + 1));
                }
            }
        }

        var checklist = new Checklist
        {
            Name = dto.Name,
            Description = dto.Description,
            IsScored = dto.IsScored,
            IsActive = true,
            Version = 1,
            // Kontrol listesi ayarları
            ChecklistTypeId = ChecklistTypes.GetBySystemName(dto.ChecklistType)?.Id ?? ChecklistTypes.Ids.CallPerformance,
            ScoringMethodId = ScoringMethods.GetBySystemName(dto.ScoringMethod)?.Id ?? ScoringMethods.Ids.Maximum,
            MaxTotalPoints = dto.MaxTotalPoints,
            Code = dto.Code,
            TemplateName = dto.TemplateName,
            ValidFrom = ToUtc(dto.ValidFrom),
            ValidUntil = ToUtc(dto.ValidUntil),
            // Firma ve Organizasyon
            CustomerId = dto.CustomerId,
            CustomerOrganizationId = dto.CustomerOrganizationId,
            // Görünüm ayarları
            HideGroupNames = dto.HideGroupNames,
            // Sorular - Direkt checklist'e bağlı
            Questions = dto.Questions.Select(q => new Question
            {
                Text = q.Text,
                Order = q.Order,
                ScoringTypeId = ScoringTypes.GetBySystemName(q.ScoringType)?.Id ?? ScoringTypes.Ids.Scored,
                WeightPoints = q.WeightPoints,
                MaxPoints = q.MaxPoints,
                PenaltyTypeId = PenaltyTypes.GetBySystemName(q.PenaltyType)?.Id ?? PenaltyTypes.Ids.None,
                IsRequired = q.IsRequired,
                RecommendedNote = q.RecommendedNote,
                HelpText = q.HelpText,
                GroupName = q.GroupName,
                SelectionTypeId = q.SelectionTypeId,
                ShowScoreInput = q.ShowScoreInput,
                AllowComment = q.AllowComment,
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
        return await MapToDtoAsync(created);
    }

    public async Task<ChecklistDto> UpdateAsync(UpdateChecklistDto dto)
    {
        // Soru validasyonu: ShowScoreInput kapalıysa, ya SubCriteria olmalı ya da AllowComment açık olmalı
        for (int i = 0; i < dto.Questions.Count; i++)
        {
            var q = dto.Questions[i];
            if (!q.ShowScoreInput)
            {
                var hasSubCriteria = q.SubCriteria != null && q.SubCriteria.Count > 0;
                if (!hasSubCriteria && !q.AllowComment)
                {
                    var errorMsg = await _localizationService.GetResourceAsync(
                        "Checklist.Question.ShowScoreInputValidation",
                        (int?)null,
                        "Soru {0}: 'Puan girişini göster' kapalıyken en az bir alt kriter eklenmeli veya 'Yorum Yapılabilir' işaretlenmelidir.");
                    throw new InvalidOperationException(string.Format(errorMsg, i + 1));
                }
            }
        }

        var existing = await _checklistRepository.GetByIdAsync(dto.Id, includeDetails: true);
        if (existing == null)
            throw new KeyNotFoundException($"Checklist with ID {dto.Id} not found");

        existing.Name = dto.Name;
        existing.Description = dto.Description;
        existing.IsScored = dto.IsScored;
        existing.IsActive = dto.IsActive;
        // Kontrol listesi ayarları
        existing.ChecklistTypeId = ChecklistTypes.GetBySystemName(dto.ChecklistType)?.Id ?? ChecklistTypes.Ids.CallPerformance;
        existing.ScoringMethodId = ScoringMethods.GetBySystemName(dto.ScoringMethod)?.Id ?? existing.ScoringMethodId;
        existing.MaxTotalPoints = dto.MaxTotalPoints;
        existing.Code = dto.Code;
        existing.TemplateName = dto.TemplateName;
        existing.ValidFrom = ToUtc(dto.ValidFrom);
        existing.ValidUntil = ToUtc(dto.ValidUntil);
        // Firma ve Organizasyon
        existing.CustomerId = dto.CustomerId;
        existing.CustomerOrganizationId = dto.CustomerOrganizationId;
        // Görünüm ayarları
        existing.HideGroupNames = dto.HideGroupNames;

        // Soruları güncelle
        UpdateQuestions(existing, dto.Questions);

        var updated = await _checklistRepository.UpdateAsync(existing);
        return await MapToDtoAsync(updated);
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
            ChecklistTypeId = original.ChecklistTypeId,
            ScoringMethodId = original.ScoringMethodId,
            MaxTotalPoints = original.MaxTotalPoints,
            Code = original.Code,
            TemplateName = original.TemplateName,
            ValidFrom = original.ValidFrom,
            ValidUntil = original.ValidUntil,
            CustomerId = original.CustomerId,
            CustomerOrganizationId = original.CustomerOrganizationId,
            HideGroupNames = original.HideGroupNames,
            // Soruları kopyala
            Questions = original.Questions.Select(q => new Question
            {
                Text = q.Text,
                Order = q.Order,
                ScoringTypeId = q.ScoringTypeId,
                WeightPoints = q.WeightPoints,
                MaxPoints = q.MaxPoints,
                PenaltyTypeId = q.PenaltyTypeId,
                IsRequired = q.IsRequired,
                RecommendedNote = q.RecommendedNote,
                HelpText = q.HelpText,
                GroupName = q.GroupName,
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
        return await MapToDtoAsync(created);
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
                    question.ScoringTypeId = ScoringTypes.GetBySystemName(questionDto.ScoringType)?.Id ?? question.ScoringTypeId;
                    question.WeightPoints = questionDto.WeightPoints;
                    question.MaxPoints = questionDto.MaxPoints;
                    question.PenaltyTypeId = PenaltyTypes.GetBySystemName(questionDto.PenaltyType)?.Id ?? PenaltyTypes.Ids.None;
                    question.IsRequired = questionDto.IsRequired;
                    question.RecommendedNote = questionDto.RecommendedNote;
                    question.HelpText = questionDto.HelpText;
                    question.GroupName = questionDto.GroupName;
                    // Online anket ayarları
                    question.SelectionTypeId = questionDto.SelectionTypeId;
                    question.ShowScoreInput = questionDto.ShowScoreInput;
                    question.AllowComment = questionDto.AllowComment;

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
                    ScoringTypeId = ScoringTypes.GetBySystemName(questionDto.ScoringType)?.Id ?? ScoringTypes.Ids.Scored,
                    WeightPoints = questionDto.WeightPoints,
                    MaxPoints = questionDto.MaxPoints,
                    PenaltyTypeId = PenaltyTypes.GetBySystemName(questionDto.PenaltyType)?.Id ?? PenaltyTypes.Ids.None,
                    IsRequired = questionDto.IsRequired,
                    RecommendedNote = questionDto.RecommendedNote,
                    HelpText = questionDto.HelpText,
                    GroupName = questionDto.GroupName,
                    // Online anket ayarları
                    SelectionTypeId = questionDto.SelectionTypeId,
                    ShowScoreInput = questionDto.ShowScoreInput,
                    AllowComment = questionDto.AllowComment,
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

    private async Task<ChecklistDto> MapToDtoAsync(Checklist checklist)
    {
        var questions = new List<QuestionDto>();
        foreach (var q in checklist.Questions.OrderBy(q => q.Order))
        {
            questions.Add(new QuestionDto
            {
                Id = q.Id,
                Text = q.Text,
                Order = q.Order,
                ScoringType = ScoringTypes.GetById(q.ScoringTypeId)?.SystemName ?? "Scored",
                ScoringTypeName = await GetScoringTypeNameAsync(q.ScoringTypeId),
                WeightPoints = q.WeightPoints,
                MaxPoints = q.MaxPoints,
                PenaltyType = PenaltyTypes.GetById(q.PenaltyTypeId)?.SystemName ?? "None",
                PenaltyTypeName = await GetPenaltyTypeNameAsync(q.PenaltyTypeId),
                IsRequired = q.IsRequired,
                RecommendedNote = q.RecommendedNote,
                HelpText = q.HelpText,
                GroupName = q.GroupName,
                SelectionTypeId = q.SelectionTypeId,
                ShowScoreInput = q.ShowScoreInput,
                AllowComment = q.AllowComment,
                SubCriteria = q.SubCriteria?.OrderBy(sc => sc.Order).Select(sc => new SubCriteriaDto
                {
                    Id = sc.Id,
                    Description = sc.Description,
                    Order = sc.Order,
                    WeightPoints = sc.WeightPoints,
                    IsActive = sc.IsActive
                }).ToList()
            });
        }

        return new ChecklistDto
        {
            Id = checklist.Id,
            Name = checklist.Name,
            Description = checklist.Description,
            IsScored = checklist.IsScored,
            IsActive = checklist.IsActive,
            Version = checklist.Version,
            CreatedAt = checklist.CreatedAt,
            ChecklistType = ChecklistTypes.GetById(checklist.ChecklistTypeId)?.SystemName ?? "",
            ChecklistTypeName = await GetChecklistTypeNameAsync(checklist.ChecklistTypeId),
            ScoringMethod = ScoringMethods.GetById(checklist.ScoringMethodId)?.SystemName ?? "Maximum",
            ScoringMethodName = await GetScoringMethodNameAsync(checklist.ScoringMethodId),
            MaxTotalPoints = checklist.MaxTotalPoints,
            Code = checklist.Code,
            TemplateName = checklist.TemplateName,
            ValidFrom = checklist.ValidFrom,
            ValidUntil = checklist.ValidUntil,
            CustomerId = checklist.CustomerId,
            CustomerName = checklist.Customer?.CompanyName,
            CustomerOrganizationId = checklist.CustomerOrganizationId,
            CustomerOrganizationName = checklist.CustomerOrganization?.Name,
            HideGroupNames = checklist.HideGroupNames,
            Questions = questions,
            QuestionCount = checklist.Questions.Count
        };
    }

    private async Task<string> GetChecklistTypeNameAsync(int typeId)
    {
        var item = ChecklistTypes.GetById(typeId);
        if (item == null) return "";
        return await _localizationService.GetResourceAsync(item.NameResourceKey, (int?)null, item.Description);
    }

    private async Task<string> GetScoringMethodNameAsync(int methodId)
    {
        var item = ScoringMethods.GetById(methodId);
        if (item == null) return "";
        return await _localizationService.GetResourceAsync(item.NameResourceKey, (int?)null, item.Description);
    }

    private async Task<string> GetScoringTypeNameAsync(int typeId)
    {
        var item = ScoringTypes.GetById(typeId);
        if (item == null) return "";
        return await _localizationService.GetResourceAsync(item.NameResourceKey, (int?)null, item.Description);
    }

    private async Task<string> GetPenaltyTypeNameAsync(int typeId)
    {
        var item = PenaltyTypes.GetById(typeId);
        if (item == null) return "";
        return await _localizationService.GetResourceAsync(item.NameResourceKey, (int?)null, item.Description);
    }

    public async Task<ExcelExportDto?> ExportChecklistToExcelAsync(int id)
    {
        var checklist = await _checklistRepository.GetByIdAsync(id, includeDetails: true);
        if (checklist == null) return null;

        // Yerelleştirilmiş metinleri al
        var sheetName = await _localizationService.GetResourceAsync("Common.Checklist");
        var checklistInfoTitle = await _localizationService.GetResourceAsync("Excel.ChecklistInfo");
        var nameLabel = await _localizationService.GetResourceAsync("Excel.Name");
        var codeLabel = await _localizationService.GetResourceAsync("Common.Code");
        var customerLabel = await _localizationService.GetResourceAsync("Common.Customer");
        var generalLabel = await _localizationService.GetResourceAsync("Excel.General");
        var organizationLabel = await _localizationService.GetResourceAsync("Common.Organization");
        var descriptionLabel = await _localizationService.GetResourceAsync("Excel.Description");
        var questionsTitle = await _localizationService.GetResourceAsync("Excel.Questions");
        var orderLabel = await _localizationService.GetResourceAsync("Excel.QuestionNumber");
        var groupLabel = await _localizationService.GetResourceAsync("Report.GroupName");
        var questionLabel = await _localizationService.GetResourceAsync("Excel.QuestionText");
        var maxPointsLabel = await _localizationService.GetResourceAsync("Excel.MaxPoints");
        var weightLabel = await _localizationService.GetResourceAsync("Excel.Weight");
        var scoringTypeLabel = await _localizationService.GetResourceAsync("Excel.QuestionType");
        var penaltyTypeLabel = await _localizationService.GetResourceAsync("Report.PenaltyType");

        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add(sheetName);

        int row = 1;

        // ===== GENEL BİLGİLER =====
        sheet.Cell(row, 1).Value = checklistInfoTitle;
        sheet.Cell(row, 1).Style.Font.Bold = true;
        sheet.Cell(row, 1).Style.Font.FontSize = 14;
        sheet.Range(row, 1, row, 7).Merge();
        sheet.Range(row, 1, row, 7).Style.Fill.BackgroundColor = XLColor.DarkBlue;
        sheet.Range(row, 1, row, 7).Style.Font.FontColor = XLColor.White;
        row++;

        sheet.Cell(row, 1).Value = nameLabel + ":";
        sheet.Cell(row, 1).Style.Font.Bold = true;
        sheet.Cell(row, 2).Value = checklist.Name;
        sheet.Cell(row, 4).Value = codeLabel + ":";
        sheet.Cell(row, 4).Style.Font.Bold = true;
        sheet.Cell(row, 5).Value = checklist.Code ?? "-";
        row++;

        sheet.Cell(row, 1).Value = customerLabel + ":";
        sheet.Cell(row, 1).Style.Font.Bold = true;
        sheet.Cell(row, 2).Value = checklist.Customer?.CompanyName ?? generalLabel;
        sheet.Cell(row, 4).Value = organizationLabel + ":";
        sheet.Cell(row, 4).Style.Font.Bold = true;
        sheet.Cell(row, 5).Value = checklist.CustomerOrganization?.Name ?? "-";
        row++;

        sheet.Cell(row, 1).Value = descriptionLabel + ":";
        sheet.Cell(row, 1).Style.Font.Bold = true;
        sheet.Cell(row, 2).Value = checklist.Description ?? "-";
        sheet.Range(row, 2, row, 7).Merge();
        row++;

        row++; // Boş satır

        // ===== SORULAR VE ALT KRİTERLER =====
        sheet.Cell(row, 1).Value = questionsTitle;
        sheet.Cell(row, 1).Style.Font.Bold = true;
        sheet.Cell(row, 1).Style.Font.FontSize = 14;
        sheet.Range(row, 1, row, 7).Merge();
        sheet.Range(row, 1, row, 7).Style.Fill.BackgroundColor = XLColor.DarkBlue;
        sheet.Range(row, 1, row, 7).Style.Font.FontColor = XLColor.White;
        row++;

        // Tablo başlıkları
        sheet.Cell(row, 1).Value = orderLabel;
        sheet.Cell(row, 2).Value = groupLabel;
        sheet.Cell(row, 3).Value = questionLabel;
        sheet.Cell(row, 4).Value = maxPointsLabel;
        sheet.Cell(row, 5).Value = weightLabel;
        sheet.Cell(row, 6).Value = scoringTypeLabel;
        sheet.Cell(row, 7).Value = penaltyTypeLabel;
        sheet.Range(row, 1, row, 7).Style.Font.Bold = true;
        sheet.Range(row, 1, row, 7).Style.Fill.BackgroundColor = XLColor.LightGray;
        row++;

        var questions = checklist.Questions?.Where(q => !q.IsDeleted).OrderBy(q => q.Order).ToList() ?? new List<Question>();

        foreach (var q in questions)
        {
            // Soru satırı
            sheet.Cell(row, 1).Value = q.Order;
            sheet.Cell(row, 2).Value = q.GroupName ?? "";
            sheet.Cell(row, 3).Value = q.Text;

            // Puansız sorular için Max Puan ve Ağırlık "-" olacak
            if (q.ScoringTypeId == ScoringTypes.Ids.Unscored)
            {
                sheet.Cell(row, 4).Value = "-";
                sheet.Cell(row, 5).Value = "-";
            }
            else
            {
                sheet.Cell(row, 4).Value = q.MaxPoints;
                sheet.Cell(row, 5).Value = (double)q.WeightPoints;
            }

            sheet.Cell(row, 6).Value = await GetScoringTypeNameAsync(q.ScoringTypeId);
            sheet.Cell(row, 7).Value = q.PenaltyTypeId == PenaltyTypes.Ids.None ? "" : await GetPenaltyTypeNameAsync(q.PenaltyTypeId);
            sheet.Range(row, 1, row, 7).Style.Fill.BackgroundColor = XLColor.LightSteelBlue;
            sheet.Range(row, 1, row, 7).Style.Font.Bold = true;
            row++;

            // Alt kriterler
            var subCriteria = q.SubCriteria?.Where(sc => !sc.IsDeleted).OrderBy(sc => sc.Order).ToList() ?? new List<QuestionSubCriteria>();
            foreach (var sc in subCriteria)
            {
                sheet.Cell(row, 3).Value = $"    └ {sc.Description}";
                sheet.Cell(row, 3).Style.Font.FontColor = XLColor.FromHtml("#333333");
                row++;
            }
        }

        // Sütun genişlikleri
        sheet.Column(1).Width = 8;
        sheet.Column(2).Width = 20;
        sheet.Column(3).Width = 50;
        sheet.Column(4).Width = 12;
        sheet.Column(5).Width = 12;
        sheet.Column(6).Width = 12;
        sheet.Column(7).Width = 12;

        // Excel dosyasını byte array'e çevir
        using var stream = new MemoryStream();
        workbook.SaveAs(stream);

        var fileName = $"Checklist_{checklist.Name.Replace(" ", "_")}_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";

        return new ExcelExportDto
        {
            FileName = fileName,
            FileContent = stream.ToArray(),
            ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
        };
    }
}
