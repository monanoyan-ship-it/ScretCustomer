using System.ComponentModel.DataAnnotations;

namespace SecretCustomer.Core.DTOs.GolgeMusteri;

public class GmAtamaDto
{
    public int Id { get; set; }
    public int GmDonemId { get; set; }
    public string? DonemAdi { get; set; }
    public int GmDonemSoruId { get; set; }
    public string? SoruMetni { get; set; }
    public string? BeklenenCevap { get; set; }
    public string? HedefFirmaAdi { get; set; }
    public string? HedefFirmaTelefonNo { get; set; }
    public bool IsKuponlu { get; set; }
    public int UserId { get; set; }
    public string? UserName { get; set; }
    public DateTime? PlanTarihi { get; set; }
    public DateTime? GerceklesmeTarihi { get; set; }
    public string? AramaSaati { get; set; }
    public string? Not { get; set; }
    public string? KuponKodu { get; set; }
    public string? KuponBilgisi { get; set; }
    public string? GorusulenTemsilci { get; set; }
    public int CustomerId { get; set; }
    public int DurumId { get; set; }
    public string? DurumText { get; set; }
    public string? DurumCss { get; set; }
}

public class UpdateGmAtamaDto
{
    public int? UserId { get; set; }
    public DateTime? PlanTarihi { get; set; }
}

public class CompleteGmAtamaDto
{
    [Required(ErrorMessage = "Gerçekleşme tarihi zorunludur")]
    public DateTime GerceklesmeTarihi { get; set; }

    [Required(ErrorMessage = "Arama saati zorunludur")]
    [StringLength(10, ErrorMessage = "Arama saati en fazla 10 karakter olabilir")]
    public string AramaSaati { get; set; } = string.Empty;

    [StringLength(2000, ErrorMessage = "Not en fazla 2000 karakter olabilir")]
    public string? Not { get; set; }

    [StringLength(100, ErrorMessage = "Kupon kodu en fazla 100 karakter olabilir")]
    public string? KuponKodu { get; set; }

    [StringLength(200, ErrorMessage = "Temsilci adı en fazla 200 karakter olabilir")]
    public string? GorusulenTemsilci { get; set; }
}

public class EnterKuponKoduDto
{
    [Required(ErrorMessage = "Kupon kodu zorunludur")]
    [StringLength(100, ErrorMessage = "Kupon kodu en fazla 100 karakter olabilir")]
    public string KuponKodu { get; set; } = string.Empty;

    [StringLength(500, ErrorMessage = "Kupon bilgisi en fazla 500 karakter olabilir")]
    public string? KuponBilgisi { get; set; }
}
