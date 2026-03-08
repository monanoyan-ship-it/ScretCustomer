using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SecretCustomer.Core.Enums;
using SecretCustomer.Core.Interfaces.Services;

namespace SecretCustomer.API.Controllers.Api;

[Route("api/enums")]
[ApiController]
[Authorize]
public class EnumsApiController : ControllerBase
{
    private readonly ILocalizationService _localizationService;

    public EnumsApiController(ILocalizationService localizationService)
    {
        _localizationService = localizationService;
    }

    /// <summary>
    /// Tum enum tanimlarini dondurur
    /// Frontend bu veriyi cache'leyerek kullanabilir
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        return Ok(new
        {
            userRoles = await MapTypeItemsAsync(UserRoles.All),
            customerPersonnelRoles = await MapTypeItemsAsync(CustomerPersonnelRoles.All),
            assignmentStatuses = await MapTypeItemsAsync(AssignmentStatuses.All),
            evaluationStatuses = await MapTypeItemsAsync(EvaluationStatuses.All),
            projectStatuses = await MapTypeItemsAsync(ProjectStatuses.All),
            questionScoringTypes = await MapTypeItemsAsync(QuestionScoringTypes.All),
            penaltyTypes = await MapTypeItemsAsync(PenaltyTypes.All),
            periodStatuses = await MapTypeItemsAsync(PeriodStatuses.All),
            approvalStatuses = await MapTypeItemsAsync(ApprovalStatuses.All),
            months = await MapTypeItemsAsync(Months.All),
            daysOfWeek = await MapTypeItemsAsync(DaysOfWeek.All),
            evaluationNotificationFrequency = await MapTypeItemsAsync(EvaluationNotificationFrequencies.All),
            checklistTypes = await MapTypeItemsAsync(ChecklistTypes.All),
            scoringMethods = await MapTypeItemsAsync(ScoringMethods.All),
            scoringTypes = await MapTypeItemsAsync(ScoringTypes.All),
            projectTypes = await MapTypeItemsAsync(ProjectTypes.All)
        });
    }

    /// <summary>
    /// Kullanici rollerini dondurur
    /// </summary>
    [HttpGet("user-roles")]
    public async Task<IActionResult> GetUserRoles()
    {
        return Ok(await MapTypeItemsAsync(UserRoles.All));
    }

    /// <summary>
    /// Musteri personeli rollerini dondurur
    /// </summary>
    [HttpGet("customer-personnel-roles")]
    public async Task<IActionResult> GetCustomerPersonnelRoles()
    {
        return Ok(await MapTypeItemsAsync(CustomerPersonnelRoles.All));
    }

    /// <summary>
    /// Atama durumlarini dondurur
    /// </summary>
    [HttpGet("assignment-statuses")]
    public async Task<IActionResult> GetAssignmentStatuses()
    {
        return Ok(await MapTypeItemsAsync(AssignmentStatuses.All));
    }

    /// <summary>
    /// Degerlendirme durumlarini dondurur
    /// </summary>
    [HttpGet("evaluation-statuses")]
    public async Task<IActionResult> GetEvaluationStatuses()
    {
        return Ok(await MapTypeItemsAsync(EvaluationStatuses.All));
    }

    /// <summary>
    /// Proje durumlarini dondurur
    /// </summary>
    [HttpGet("project-statuses")]
    public async Task<IActionResult> GetProjectStatuses()
    {
        return Ok(await MapTypeItemsAsync(ProjectStatuses.All));
    }

    /// <summary>
    /// Soru puanlama tiplerini dondurur
    /// </summary>
    [HttpGet("scoring-types")]
    public async Task<IActionResult> GetScoringTypes()
    {
        return Ok(await MapTypeItemsAsync(QuestionScoringTypes.All));
    }

    /// <summary>
    /// Ceza tiplerini dondurur
    /// </summary>
    [HttpGet("penalty-types")]
    public async Task<IActionResult> GetPenaltyTypes()
    {
        return Ok(await MapTypeItemsAsync(PenaltyTypes.All));
    }

    /// <summary>
    /// Donem durumlarini dondurur
    /// </summary>
    [HttpGet("period-statuses")]
    public async Task<IActionResult> GetPeriodStatuses()
    {
        return Ok(await MapTypeItemsAsync(PeriodStatuses.All));
    }

    /// <summary>
    /// Onay durumlarini dondurur
    /// </summary>
    [HttpGet("approval-statuses")]
    public async Task<IActionResult> GetApprovalStatuses()
    {
        return Ok(await MapTypeItemsAsync(ApprovalStatuses.All));
    }

    /// <summary>
    /// Aylari dondurur
    /// </summary>
    [HttpGet("months")]
    public async Task<IActionResult> GetMonths()
    {
        return Ok(await MapTypeItemsAsync(Months.All));
    }

    /// <summary>
    /// Haftanin gunlerini dondurur
    /// </summary>
    [HttpGet("days-of-week")]
    public async Task<IActionResult> GetDaysOfWeek()
    {
        return Ok(await MapTypeItemsAsync(DaysOfWeek.All));
    }

    private async Task<IEnumerable<object>> MapTypeItemsAsync(IEnumerable<TypeItem> items)
    {
        var result = new List<object>();
        foreach (var item in items)
        {
            var displayName = await _localizationService.GetResourceAsync(
                item.NameResourceKey,
                languageId: null,
                defaultValue: item.NameResourceKey.Split('.').LastOrDefault() ?? item.SystemName);

            result.Add(new
            {
                id = item.Id,
                systemName = item.SystemName,
                nameKey = item.NameResourceKey,
                displayName = displayName,
                description = item.Description,
                icon = item.Icon,
                cssClass = item.CssClass,
                displayOrder = item.DisplayOrder,
                isDefault = item.IsDefault
            });
        }
        return result;
    }
}
