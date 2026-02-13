using System.ComponentModel.DataAnnotations;

namespace SecretCustomer.Core.DTOs.GolgeMusteri;

public class GmDonemDto
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public string? CustomerName { get; set; }
    public string Ad { get; set; } = string.Empty;
    public DateTime BaslangicTarihi { get; set; }
    public DateTime BitisTarihi { get; set; }
    public int DurumId { get; set; }
    public string? DurumText { get; set; }
    public string? DurumCss { get; set; }
    public int? OlusturanUserId { get; set; }
    public string? OlusturanUserName { get; set; }
    public int PersonelSayisi { get; set; }
    public int SoruSayisi { get; set; }
    public int KuponSayisi { get; set; }
    public int ToplamAtama { get; set; }
    public int TamamlananAtama { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class GmDonemDetailDto
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public string? CustomerName { get; set; }
    public string Ad { get; set; } = string.Empty;
    public DateTime BaslangicTarihi { get; set; }
    public DateTime BitisTarihi { get; set; }
    public int DurumId { get; set; }
    public string? DurumText { get; set; }
    public string? DurumCss { get; set; }
    public int? OlusturanUserId { get; set; }
    public string? OlusturanUserName { get; set; }

    public List<GmDonemPersonelDto> Personeller { get; set; } = new();
    public List<GmDonemSoruDto> Sorular { get; set; } = new();
    public List<GmDonemKuponDto> Kuponlar { get; set; } = new();
}

public class CreateGmDonemDto
{
    [Required(ErrorMessage = "Müşteri seçimi zorunludur")]
    public int CustomerId { get; set; }

    [Required(ErrorMessage = "Dönem adı zorunludur")]
    [StringLength(200, ErrorMessage = "Dönem adı en fazla 200 karakter olabilir")]
    public string Ad { get; set; } = string.Empty;

    [Required(ErrorMessage = "Başlangıç tarihi zorunludur")]
    public DateTime BaslangicTarihi { get; set; }

    [Required(ErrorMessage = "Bitiş tarihi zorunludur")]
    public DateTime BitisTarihi { get; set; }
}

public class UpdateGmDonemDto
{
    [Required(ErrorMessage = "Dönem adı zorunludur")]
    [StringLength(200, ErrorMessage = "Dönem adı en fazla 200 karakter olabilir")]
    public string Ad { get; set; } = string.Empty;

    [Required(ErrorMessage = "Başlangıç tarihi zorunludur")]
    public DateTime BaslangicTarihi { get; set; }

    [Required(ErrorMessage = "Bitiş tarihi zorunludur")]
    public DateTime BitisTarihi { get; set; }
}

public class GmDonemPersonelDto
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string? UserName { get; set; }
}

public class GmDonemSoruDto
{
    public int Id { get; set; }
    public int GmSoruId { get; set; }
    public string? SoruMetni { get; set; }
    public string? HedefFirmaAdi { get; set; }
    public string? BeklenenCevap { get; set; }
    public bool IsKuponlu { get; set; }
    public int AranmaSayisi { get; set; }
}

public class GmDonemKuponDto
{
    public int Id { get; set; }
    public string KuponKodu { get; set; } = string.Empty;
    public bool IsUsed { get; set; }
}
