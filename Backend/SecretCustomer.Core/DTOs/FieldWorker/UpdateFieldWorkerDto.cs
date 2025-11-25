using System.ComponentModel.DataAnnotations;

namespace SecretCustomer.Core.DTOs.FieldWorker;

public class UpdateFieldWorkerDto
{
    public Guid Id { get; set; }

    [Required(ErrorMessage = "Ad alanı zorunludur.")]
    [MaxLength(100, ErrorMessage = "Ad en fazla 100 karakter olabilir.")]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Soyad alanı zorunludur.")]
    [MaxLength(100, ErrorMessage = "Soyad en fazla 100 karakter olabilir.")]
    public string LastName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Telefon numarası zorunludur.")]
    [Phone(ErrorMessage = "Geçerli bir telefon numarası giriniz.")]
    [MaxLength(20, ErrorMessage = "Telefon numarası en fazla 20 karakter olabilir.")]
    public string PhoneNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "Adres alanı zorunludur.")]
    [MaxLength(500, ErrorMessage = "Adres en fazla 500 karakter olabilir.")]
    public string Address { get; set; } = string.Empty;

    [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi giriniz.")]
    [MaxLength(255, ErrorMessage = "E-posta en fazla 255 karakter olabilir.")]
    public string? Email { get; set; }

    public bool IsActive { get; set; }

    [MaxLength(1000, ErrorMessage = "Notlar en fazla 1000 karakter olabilir.")]
    public string? Notes { get; set; }
}
