using SecretCustomer.Core.DTOs.PersonnelRequest;
using SecretCustomer.Core.Entities;

namespace SecretCustomer.Core.Interfaces.Services;

public interface IPersonnelRequestService
{
    /// <summary>
    /// Tüm personel taleplerini filtreli getirir
    /// </summary>
    Task<(List<PersonnelRequestDto> Items, int TotalCount)> GetAllAsync(PersonnelRequestFilterDto filter);

    /// <summary>
    /// ID ile talep getirir
    /// </summary>
    Task<PersonnelRequestDto?> GetByIdAsync(int id);

    /// <summary>
    /// Değerlendirmeye ait talep var mı kontrol eder
    /// </summary>
    Task<PersonnelRequest?> GetByEvaluationIdAsync(int evaluationId);

    /// <summary>
    /// Yeni personel talebi oluşturur
    /// </summary>
    Task<PersonnelRequestDto> CreateAsync(CreatePersonnelRequestDto dto, int requestedByUserId);

    /// <summary>
    /// Talebi onaylar ve personel oluşturur
    /// </summary>
    Task<PersonnelRequestDto> ApproveAsync(ApprovePersonnelRequestDto dto, int reviewedByUserId);

    /// <summary>
    /// Talebi reddeder
    /// </summary>
    Task<PersonnelRequestDto> RejectAsync(RejectPersonnelRequestDto dto, int reviewedByUserId);

    /// <summary>
    /// Onay ekranı için süpervizör listesi (aynı organizasyondaki)
    /// </summary>
    Task<List<(int Id, string FullName)>> GetSupervisorsForOrganizationAsync(int customerOrganizationId);

    /// <summary>
    /// Durum sayılarını getirir (Pending, Approved, Rejected)
    /// </summary>
    Task<(int Pending, int Approved, int Rejected)> GetStatusCountsAsync();
}
