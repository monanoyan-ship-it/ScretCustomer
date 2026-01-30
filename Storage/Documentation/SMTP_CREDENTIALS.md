# Office 365 SMTP OAuth 2.0 Credentials

> **DIKKAT:** Bu dosya hassas bilgiler icerir. Git'e commit etmeyin veya .gitignore'a ekleyin!

## Azure App Registration Bilgileri

| Ayar | Deger |
|------|-------|
| **Uygulama Adi** | SecretCustomer-SMTP |
| **Tenant ID** | `9920645c-ae65-495d-9729-a8ac77612e14` |
| **Client ID** | `ba252731-6552-44a0-a41f-dc1c093d76e2` |
| **Client Secret** | `~u68Q~G.4uNaMnL4V3cvb3qS6G13wKk-Pbx-JbxW` |
| **Secret Suresi** | 30.01.2028 |

## SMTP Ayarlari (Uygulamada kullanilacak)

| Ayar | Deger |
|------|-------|
| **Host** | `smtp.office365.com` |
| **Port** | `587` |
| **Use SSL** | `true` (StartTLS) |
| **Use OAuth** | `true` |
| **Username** | (Mail gonderecek hesabin email adresi) |
| **From Email** | (Mail gonderecek hesabin email adresi) |

## API Izinleri (Admin Consent Gerekli)

- `Mail.Read` - Read mail in all mailboxes
- `Mail.ReadWrite` - Read and write mail in all mailboxes
- `Mail.Send` - Send mail as any user

## Admin Consent Link

Yonetici onay linki:
```
https://login.microsoftonline.com/9920645c-ae65-495d-9729-a8ac77612e14/adminconsent?client_id=ba252731-6552-44a0-a41f-dc1c093d76e2
```

## Veritabaninda Kaydedilecek SystemSettings Kayitlari

```sql
-- SMTP OAuth 2.0 ayarlari
INSERT INTO "SystemSettings" ("Key", "Value", "Description") VALUES
('Smtp.Host', 'smtp.office365.com', 'SMTP sunucu adresi'),
('Smtp.Port', '587', 'SMTP port'),
('Smtp.Username', 'EMAIL_ADRESI_BURAYA', 'SMTP kullanici adi (email)'),
('Smtp.Password', '', 'OAuth kullanildiginda bos birakilir'),
('Smtp.UseSsl', 'true', 'SSL/TLS kullan'),
('Smtp.FromEmail', 'EMAIL_ADRESI_BURAYA', 'Gonderen email adresi'),
('Smtp.FromName', 'Secret Customer', 'Gonderen adi'),
('Smtp.Enabled', 'true', 'SMTP aktif mi'),
('Smtp.UseOAuth', 'true', 'OAuth 2.0 kullan'),
('Smtp.TenantId', '9920645c-ae65-495d-9729-a8ac77612e14', 'Azure Tenant ID'),
('Smtp.ClientId', 'ba252731-6552-44a0-a41f-dc1c093d76e2', 'Azure Client ID'),
('Smtp.ClientSecret', '~u68Q~G.4uNaMnL4V3cvb3qS6G13wKk-Pbx-JbxW', 'Azure Client Secret');
```

## Notlar

1. Admin consent onaylanmadan mail gonderilemez
2. Client Secret 30.01.2028 tarihinde sona erecek, yenilenmelidir
3. Mail gondermek icin kullanilacak email adresi Azure AD'de tanimli olmalidir
