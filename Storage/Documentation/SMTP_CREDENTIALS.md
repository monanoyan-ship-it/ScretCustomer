# Office 365 Email Credentials (Microsoft Graph API)

> **DURUM:** ✅ CALISIYOR (30.01.2025 tarihinde test edildi)

## Azure App Registration Bilgileri

| Ayar | Deger |
|------|-------|
| **Uygulama Adi** | SecretCustomer-SMTP |
| **Tenant ID** | `9920645c-ae65-495d-9729-a8ac77612e14` |
| **Client ID** | `ba252731-6552-44a0-a41f-dc1c093d76e2` |
| **Client Secret** | `~u68Q~G.4uNaMnL4V3cvb3qS6G13wKk-Pbx-JbxW` |
| **Secret Suresi** | 30.01.2028 |

## Kullanilan Yontem

**Microsoft Graph API** kullaniliyor (SMTP degil).

SMTP AUTH, Microsoft 365 tenant seviyesinde devre disi oldugu icin Graph API tercih edildi.

## Uygulama Ayarlari

| Ayar | Deger |
|------|-------|
| **Kullanici Adi / Email** | `akademi@ncacademy.com.tr` |
| **Gonderen Email** | `akademi@ncacademy.com.tr` |
| **Gonderen Adi** | `NCAcademy` |
| **Microsoft Graph API Kullan** | `true` (acik) |
| **OAuth 2.0** | `true` (acik) |

## API Izinleri (Admin Consent Verildi ✅)

### Microsoft Graph
- `Mail.Send` - Send mail as any user ✅
- `User.Read` - Sign in and read user profile ✅

### Office 365 Exchange Online
- `Mail.Read` - Read mail in all mailboxes ✅
- `Mail.ReadWrite` - Read and write mail in all mailboxes ✅
- `Mail.Send` - Send mail as any user ✅

## Admin Consent Link

Yonetici onay linki (gerektiginde):
```
https://login.microsoftonline.com/9920645c-ae65-495d-9729-a8ac77612e14/adminconsent?client_id=ba252731-6552-44a0-a41f-dc1c093d76e2
```

## Onemli Notlar

1. **Graph API kullaniliyor** - SMTP AUTH tenant'ta kapali oldugu icin Graph API tercih edildi
2. **Client Secret 30.01.2028'de sona erecek** - Bu tarihten once yenilenmeli
3. **Admin onaylari verildi** - Newfound Creative Academy icin tum izinler onaylandi
4. **Ayarlar uygulamada yapildi** - http://45.84.191.28/Settings/Smtp adresinden yonetiliyor

## Sorun Giderme

SMTP AUTH hatasi alinirsa (`SmtpClientAuthentication is disabled for the Tenant`):
- Graph API kullanildiginden emin ol (uygulamada "Microsoft Graph API Kullan" acik olmali)
- SMTP yerine Graph API secili olmali

Azure Portal linki:
https://portal.azure.com/#view/Microsoft_AAD_RegisteredApps/ApplicationMenuBlade/~/Overview/appId/ba252731-6552-44a0-a41f-dc1c093d76e2
