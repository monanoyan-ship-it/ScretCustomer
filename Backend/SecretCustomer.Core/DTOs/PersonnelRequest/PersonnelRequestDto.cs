namespace SecretCustomer.Core.DTOs.PersonnelRequest;

public class PersonnelRequestDto
{
    public int Id { get; set; }

    // Talep bilgileri
    public int EvaluationId { get; set; }
    public int CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public int CustomerOrganizationId { get; set; }
    public string CustomerOrganizationName { get; set; } = string.Empty;

    // Personel bilgileri
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string FullName => $"{FirstName} {LastName}";
    public string? Title { get; set; }
    public string? Notes { get; set; }

    // Talep eden
    public int RequestedByUserId { get; set; }
    public string RequestedByUserName { get; set; } = string.Empty;
    public DateTime RequestedAt { get; set; }

    // Durum
    public int Status { get; set; }
    public string StatusName { get; set; } = string.Empty;

    // Onay/Red bilgileri
    public int? ReviewedByUserId { get; set; }
    public string? ReviewedByUserName { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public string? RejectReason { get; set; }

    // Oluşturulan personel
    public int? CreatedPersonnelId { get; set; }
    public string? CreatedPersonnelName { get; set; }
}

public class CreatePersonnelRequestDto
{
    public int EvaluationId { get; set; }
    public int CustomerId { get; set; }
    public int CustomerOrganizationId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? Title { get; set; }
    public string? Notes { get; set; }
}

public class ApprovePersonnelRequestDto
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string? Email { get; set; } // Varsayılan: username@temp.com
    public int? CustomerOrganizationId { get; set; } // Belirtilirse request'teki organizasyonu override eder
    public int? SupervisorId { get; set; }
    public string? FirstName { get; set; } // Opsiyonel - verilirse override eder
    public string? LastName { get; set; }  // Opsiyonel - verilirse override eder
}

public class RejectPersonnelRequestDto
{
    public int Id { get; set; }
    public string RejectReason { get; set; } = string.Empty;

    /// <summary>
    /// Eğer belirtilirse, değerlendirme bu personele atanır
    /// Belirtilmezse değerlendirme taslak olarak kalır
    /// </summary>
    public int? CorrectPersonnelId { get; set; }
}

public class PersonnelRequestFilterDto
{
    public int? Status { get; set; }
    public string? SearchTerm { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
