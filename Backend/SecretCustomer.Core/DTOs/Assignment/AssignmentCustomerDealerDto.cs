namespace SecretCustomer.Core.DTOs.Assignment;

/// <summary>
/// Atamaya bağlı şube bilgisi
/// </summary>
public class AssignmentCustomerDealerDto
{
    public int Id { get; set; }
    public int AssignmentId { get; set; }
    public int CustomerDealerId { get; set; }
    public string CustomerDealerName { get; set; } = string.Empty;
    public string? CustomerDealerCode { get; set; }
    public string? City { get; set; }
    public string? District { get; set; }
    public int SortOrder { get; set; }

    /// <summary>
    /// Bu şube için ziyaret yapıldı mı?
    /// </summary>
    public bool HasEvaluation { get; set; }

    /// <summary>
    /// Ziyaret yapıldıysa değerlendirme ID
    /// </summary>
    public int? EvaluationId { get; set; }

    /// <summary>
    /// Ziyaret yapıldıysa puan
    /// </summary>
    public decimal? EvaluationScore { get; set; }

    /// <summary>
    /// Ziyaret tarihi
    /// </summary>
    public DateTime? EvaluationDate { get; set; }
}

/// <summary>
/// Atamaya şube ekleme DTO
/// </summary>
public class AddDealerToAssignmentDto
{
    public int CustomerDealerId { get; set; }
}
