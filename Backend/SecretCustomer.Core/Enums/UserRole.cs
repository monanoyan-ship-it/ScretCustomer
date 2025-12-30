namespace SecretCustomer.Core.Enums;

public enum UserRole
{
    Admin = 1,
    QualitySpecialist = 2,  // Kalite Uzmanı - Değerlendirme, dönem açma, personel yükleme
    FieldWorker = 3         // Saha Çalışanı - Sahada checklist puanlar
}
