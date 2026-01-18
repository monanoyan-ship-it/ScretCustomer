using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SecretCustomer.Core.Enums;

namespace SecretCustomer.API.Controllers.Api;

[Route("api/enums")]
[ApiController]
[Authorize]
public class EnumsApiController : ControllerBase
{
    /// <summary>
    /// Tum enum tanimlarini dondurur
    /// Frontend bu veriyi cache'leyerek kullanabilir
    /// </summary>
    [HttpGet]
    public IActionResult GetAll()
    {
        return Ok(new
        {
            userRoles = UserRoles.All.Select(MapTypeItem),
            customerPersonnelRoles = CustomerPersonnelRoles.All.Select(MapTypeItem),
            assignmentStatuses = AssignmentStatuses.All.Select(MapTypeItem),
            evaluationStatuses = EvaluationStatuses.All.Select(MapTypeItem),
            projectStatuses = ProjectStatuses.All.Select(MapTypeItem),
            questionScoringTypes = QuestionScoringTypes.All.Select(MapTypeItem),
            penaltyTypes = PenaltyTypes.All.Select(MapTypeItem),
            periodStatuses = PeriodStatuses.All.Select(MapTypeItem),
            approvalStatuses = ApprovalStatuses.All.Select(MapTypeItem),
            months = Months.All.Select(MapTypeItem),
            daysOfWeek = DaysOfWeek.All.Select(MapTypeItem),
            evaluationNotificationFrequency = EvaluationNotificationFrequencies.All.Select(MapTypeItem)
        });
    }

    /// <summary>
    /// Kullanici rollerini dondurur
    /// </summary>
    [HttpGet("user-roles")]
    public IActionResult GetUserRoles()
    {
        return Ok(UserRoles.All.Select(MapTypeItem));
    }

    /// <summary>
    /// Musteri personeli rollerini dondurur
    /// </summary>
    [HttpGet("customer-personnel-roles")]
    public IActionResult GetCustomerPersonnelRoles()
    {
        return Ok(CustomerPersonnelRoles.All.Select(MapTypeItem));
    }

    /// <summary>
    /// Atama durumlarini dondurur
    /// </summary>
    [HttpGet("assignment-statuses")]
    public IActionResult GetAssignmentStatuses()
    {
        return Ok(AssignmentStatuses.All.Select(MapTypeItem));
    }

    /// <summary>
    /// Degerlendirme durumlarini dondurur
    /// </summary>
    [HttpGet("evaluation-statuses")]
    public IActionResult GetEvaluationStatuses()
    {
        return Ok(EvaluationStatuses.All.Select(MapTypeItem));
    }

    /// <summary>
    /// Proje durumlarini dondurur
    /// </summary>
    [HttpGet("project-statuses")]
    public IActionResult GetProjectStatuses()
    {
        return Ok(ProjectStatuses.All.Select(MapTypeItem));
    }

    /// <summary>
    /// Soru puanlama tiplerini dondurur
    /// </summary>
    [HttpGet("scoring-types")]
    public IActionResult GetScoringTypes()
    {
        return Ok(QuestionScoringTypes.All.Select(MapTypeItem));
    }

    /// <summary>
    /// Ceza tiplerini dondurur
    /// </summary>
    [HttpGet("penalty-types")]
    public IActionResult GetPenaltyTypes()
    {
        return Ok(PenaltyTypes.All.Select(MapTypeItem));
    }

    /// <summary>
    /// Donem durumlarini dondurur
    /// </summary>
    [HttpGet("period-statuses")]
    public IActionResult GetPeriodStatuses()
    {
        return Ok(PeriodStatuses.All.Select(MapTypeItem));
    }

    /// <summary>
    /// Onay durumlarini dondurur
    /// </summary>
    [HttpGet("approval-statuses")]
    public IActionResult GetApprovalStatuses()
    {
        return Ok(ApprovalStatuses.All.Select(MapTypeItem));
    }

    /// <summary>
    /// Aylari dondurur
    /// </summary>
    [HttpGet("months")]
    public IActionResult GetMonths()
    {
        return Ok(Months.All.Select(MapTypeItem));
    }

    /// <summary>
    /// Haftanin gunlerini dondurur
    /// </summary>
    [HttpGet("days-of-week")]
    public IActionResult GetDaysOfWeek()
    {
        return Ok(DaysOfWeek.All.Select(MapTypeItem));
    }

    private static object MapTypeItem(TypeItem item)
    {
        return new
        {
            id = item.Id,
            systemName = item.SystemName,
            nameKey = item.NameResourceKey,
            description = item.Description,
            icon = item.Icon,
            cssClass = item.CssClass,
            displayOrder = item.DisplayOrder,
            isDefault = item.IsDefault
        };
    }
}
