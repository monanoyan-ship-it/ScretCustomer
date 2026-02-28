using System.ComponentModel.DataAnnotations;

namespace SecretCustomer.Core.DTOs.GolgeMusteri;

public class GmHedefFirmaDto
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public string? CustomerName { get; set; }
    public string FirmaAdi { get; set; } = string.Empty;
    public string? TelefonNo { get; set; }
    public string? Aciklama { get; set; }
    public bool IsActive { get; set; }
    public int SoruSayisi { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateGmHedefFirmaDto
{
    [Required(ErrorMessage = "Müşteri seçimi zorunludur")]
    public int CustomerId { get; set; }

    [Required(ErrorMessage = "Firma adı zorunludur")]
    [StringLength(300, ErrorMessage = "Firma adı en fazla 300 karakter olabilir")]
    public string FirmaAdi { get; set; } = string.Empty;

    [StringLength(50, ErrorMessage = "Telefon en fazla 50 karakter olabilir")]
    public string? TelefonNo { get; set; }

    [StringLength(1000, ErrorMessage = "Açıklama en fazla 1000 karakter olabilir")]
    public string? Aciklama { get; set; }

    public bool IsActive { get; set; } = true;
}

public class UpdateGmHedefFirmaDto
{
    [Required(ErrorMessage = "Müşteri seçimi zorunludur")]
    public int CustomerId { get; set; }

    [Required(ErrorMessage = "Firma adı zorunludur")]
    [StringLength(300, ErrorMessage = "Firma adı en fazla 300 karakter olabilir")]
    public string FirmaAdi { get; set; } = string.Empty;

    [StringLength(50, ErrorMessage = "Telefon en fazla 50 karakter olabilir")]
    public string? TelefonNo { get; set; }

    [StringLength(1000, ErrorMessage = "Açıklama en fazla 1000 karakter olabilir")]
    public string? Aciklama { get; set; }

    public bool IsActive { get; set; } = true;
}
