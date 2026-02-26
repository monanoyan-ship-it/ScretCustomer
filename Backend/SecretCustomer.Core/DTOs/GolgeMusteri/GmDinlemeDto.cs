using SecretCustomer.Core.Interfaces.Services;

namespace SecretCustomer.Core.DTOs.GolgeMusteri;

public class GmDinlemeListDto
{
    public int Id { get; set; }
    public int GmAtamaId { get; set; }
    public string? ChecklistName { get; set; }

    // Durum
    public int DurumId { get; set; }
    public string? DurumText { get; set; }
    public string? DurumCss { get; set; }

    // Puanlama
    public decimal Percentage { get; set; }
    public DateTime? DinlemeTarihi { get; set; }

    // Arama bilgileri (GmAtama → GmDonemSoru → GmHedefFirma)
    public string? HedefFirmaAdi { get; set; }
    public string? SoruMetni { get; set; }
    public DateTime? AramaTarihi { get; set; }
    public string? ArayanAdi { get; set; }
    public string? DonemAdi { get; set; }
    public string? DinleyenAdi { get; set; }
}

public class GmDinlemeAyarDto
{
    public int Id { get; set; }
    public int GmDonemId { get; set; }
    public int CustomerId { get; set; }
    public string? CustomerName { get; set; }
    public int ChecklistId { get; set; }
    public string? ChecklistName { get; set; }
}

public class CreateGmDinlemeAyarDto
{
    public int CustomerId { get; set; }
    public int ChecklistId { get; set; }
}

public class UpdateGmDinlemeAyarDto
{
    public int ChecklistId { get; set; }
}

// =============================================
// DINLEME FORM DTOs
// =============================================

/// <summary>
/// Dinleme popup formu için DTO
/// </summary>
public class GmDinlemeFormDto
{
    public int? DinlemeId { get; set; }
    public int GmAtamaId { get; set; }
    public int ChecklistId { get; set; }
    public string ChecklistName { get; set; } = string.Empty;

    // Arama bilgileri (readonly)
    public GmDinlemeAtamaInfoDto AtamaInfo { get; set; } = new();

    // Soru grupları (PenaltyGroupDto - EvaluationService ile aynı tip)
    public List<PenaltyGroupDto> PenaltyGroups { get; set; } = new();

    // Mevcut cevaplar (düzenleme için)
    public List<GmDinlemeExistingAnswerDto> ExistingAnswers { get; set; } = new();

    // Mevcut yorum
    public string? Comment { get; set; }
}

public class GmDinlemeAtamaInfoDto
{
    public string? HedefFirmaAdi { get; set; }
    public string? SoruMetni { get; set; }
    public string? BeklenenCevap { get; set; }
    public string? ArayanAdi { get; set; }
    public DateTime? AramaTarihi { get; set; }
    public string? AramaSaati { get; set; }
    public string? GorusulenTemsilci { get; set; }
    public string? Not { get; set; }
    public string? DonemAdi { get; set; }
}

public class GmDinlemeExistingAnswerDto
{
    public int Id { get; set; }
    public int QuestionId { get; set; }
    public int? AnswerNumeric { get; set; }
    public string? AnswerText { get; set; }
    public decimal? GivenPoints { get; set; }
    public string? Notes { get; set; }
    public bool IsPenaltyApplied { get; set; }
    public string? AppliedPenaltyType { get; set; }
    public List<int>? SelectedSubCriteriaIds { get; set; }
}

/// <summary>
/// Dinleme kaydetme/gönderme DTO'su
/// </summary>
public class GmDinlemeSubmitDto
{
    public int? DinlemeId { get; set; }
    public int GmAtamaId { get; set; }
    public string? Comment { get; set; }
    public List<GmDinlemeSubmitAnswerDto> Answers { get; set; } = new();
}

public class GmDinlemeSubmitAnswerDto
{
    public int QuestionId { get; set; }
    public int? AnswerNumeric { get; set; }
    public string? AnswerText { get; set; }
    public decimal? GivenPoints { get; set; }
    public string? Notes { get; set; }
    public bool ApplyPenalty { get; set; }
    public string? SelectedPenaltyType { get; set; }
    public List<int>? SelectedSubCriteriaIds { get; set; }
    public bool IsIncluded { get; set; } = true;
}
