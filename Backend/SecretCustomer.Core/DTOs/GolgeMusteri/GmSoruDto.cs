using System.ComponentModel.DataAnnotations;

namespace SecretCustomer.Core.DTOs.GolgeMusteri;

public class GmSoruDto
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public string? CustomerName { get; set; }
    public int GmHedefFirmaId { get; set; }
    public string? HedefFirmaAdi { get; set; }
    public string SoruMetni { get; set; } = string.Empty;
    public string? BeklenenCevap { get; set; }
    public int AranmaSayisi { get; set; }
    public bool IsKuponlu { get; set; }
    public int SiraNo { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateGmSoruDto
{
    [Required(ErrorMessage = "Müşteri seçimi zorunludur")]
    public int CustomerId { get; set; }

    [Required(ErrorMessage = "Hedef firma seçimi zorunludur")]
    public int GmHedefFirmaId { get; set; }

    [Required(ErrorMessage = "Soru metni zorunludur")]
    [StringLength(2000, ErrorMessage = "Soru metni en fazla 2000 karakter olabilir")]
    public string SoruMetni { get; set; } = string.Empty;

    [StringLength(2000, ErrorMessage = "Beklenen cevap en fazla 2000 karakter olabilir")]
    public string? BeklenenCevap { get; set; }

    [Range(1, 100, ErrorMessage = "Aranma sayısı 1-100 arasında olmalıdır")]
    public int AranmaSayisi { get; set; } = 1;

    public bool IsKuponlu { get; set; } = false;
    public int SiraNo { get; set; } = 0;
    public bool IsActive { get; set; } = true;
}

public class UpdateGmSoruDto
{
    [Required(ErrorMessage = "Soru metni zorunludur")]
    [StringLength(2000, ErrorMessage = "Soru metni en fazla 2000 karakter olabilir")]
    public string SoruMetni { get; set; } = string.Empty;

    [StringLength(2000, ErrorMessage = "Beklenen cevap en fazla 2000 karakter olabilir")]
    public string? BeklenenCevap { get; set; }

    [Range(1, 100, ErrorMessage = "Aranma sayısı 1-100 arasında olmalıdır")]
    public int AranmaSayisi { get; set; } = 1;

    public bool IsKuponlu { get; set; } = false;
    public int SiraNo { get; set; } = 0;
    public bool IsActive { get; set; } = true;
}
