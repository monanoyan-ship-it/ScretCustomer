# Personel CSV Import Standartları

## CSV Formatı

```csv
FullName,Username,Email,Password,Role,RoleId,Company,Organization
```

## Kolon Açıklamaları

| Kolon | Açıklama | Örnek |
|-------|----------|-------|
| FullName | Ad Soyad (tam) | Burcu Altınışık |
| Username | Kullanıcı adı (Türkçe karaktersiz, küçük harf) | burcu.altinisik |
| Email | E-posta adresi (benzersiz olmalı) | burcu.altinisik@concentrix.com |
| Password | Şifre | user@123 |
| Role | Rol adı | CustomerSupervisor |
| RoleId | Rol ID | 2 |
| Company | Firma kısa adı (LIKE ile aranır) | Ford |
| Organization | Organizasyon/Takım adı | Concentrix |

## Roller

| RoleId | Role | Açıklama |
|--------|------|----------|
| 1 | CustomerManager | Müşteri Yöneticisi |
| 2 | CustomerSupervisor | Takım Lideri |
| 3 | CustomerOperator | Operatör |

## Username Oluşturma Kuralları

1. Ad ve soyadı al: `Burcu Altınışık`
2. Küçük harfe çevir: `burcu altınışık`
3. Türkçe karakterleri dönüştür:
   - ı → i, ğ → g, ü → u, ş → s, ö → o, ç → c
   - İ → i, Ğ → g, Ü → u, Ş → s, Ö → o, Ç → c
4. Format: `ad.soyad` → `burcu.altinisik`

## Email Kuralları

- **Gerçek e-posta varsa:** Onu kullan (örn: `burcu.altinisik@concentrix.com`)
- **Gerçek e-posta yoksa:** `username@temp.com` formatı kullan
- **ÖNEMLİ:** Her e-posta benzersiz olmalı (unique constraint var)

## Firma Eşleştirmesi

Backend LIKE sorgusu yapıyor:
- CSV'de `Ford` yazarsan → DB'de `Ford Otomotiv` bulunur
- CSV'de `Boyner` yazarsan → DB'de `Boyner` bulunur
- Birden fazla eşleşme olursa frontend kullanıcıya sorar

## Supervisor-Operator Hiyerarşisi

CSV sırası önemli! Supervisor'lar kendi operatörlerinden ÖNCE yazılmalı:

```csv
Burcu Altınışık,burcu.altinisik,...,CustomerSupervisor,2,Ford,Concentrix
Ayten Kanpak,ayten.kanpak,...,CustomerOperator,3,Ford,Concentrix
Aybüke Berna Kaya,aybuke.kaya,...,CustomerOperator,3,Ford,Concentrix
```

Bu sırayla import edilince:
- Burcu Altınışık → Supervisor olarak kaydedilir
- Ayten Kanpak → SupervisorId = Burcu'nun ID'si
- Aybüke Berna Kaya → SupervisorId = Burcu'nun ID'si

Yeni bir Supervisor gelince operatörler ona bağlanır:
```csv
Burak Saygın,burak.saygin,...,CustomerSupervisor,2,Ford,Concentrix
Batuhan Yavuz,batuhan.yavuz,...,CustomerOperator,3,Ford,Concentrix
```

## Excel'den CSV'ye Dönüştürme

### Python Script Örneği

```python
import pandas as pd

def normalize_name(name):
    if pd.isna(name) or not name.strip():
        return None
    return name.strip()

def generate_username(fullname):
    tr_map = {'ı': 'i', 'ğ': 'g', 'ü': 'u', 'ş': 's', 'ö': 'o', 'ç': 'c',
              'İ': 'i', 'Ğ': 'g', 'Ü': 'u', 'Ş': 's', 'Ö': 'o', 'Ç': 'c'}
    name = fullname.lower()
    for tr, en in tr_map.items():
        name = name.replace(tr, en)
    parts = name.split()
    if len(parts) >= 2:
        return f'{parts[0]}.{parts[-1]}'
    return parts[0] if parts else 'user'

# Kullanım
fullname = "Burcu Altınışık"
username = generate_username(fullname)  # burcu.altinisik
email = f"{username}@temp.com"  # veya gerçek email varsa onu kullan
```

## Örnek CSV Satırları

```csv
FullName,Username,Email,Password,Role,RoleId,Company,Organization
Burcu Altınışık,burcu.altinisik,burcu.altinisik@concentrix.com,user@123,CustomerSupervisor,2,Ford,Concentrix
Ayten Kanpak,ayten.kanpak,ayten.kanpak@temp.com,user@123,CustomerOperator,3,Ford,Concentrix
Gülcan Toroman,gulcan.toraman,gulcan.toraman@globalbilgi.com.tr,user@123,CustomerSupervisor,2,Ford,Ford Global
Ravza Acar Caner,ravza.caner,ravza.acarcaner@globalbilgi.com.tr,user@123,CustomerOperator,3,Ford,Ford Global
```

## Import API Endpoint

```
POST /api/import/personnel?updateExisting=false
Content-Type: multipart/form-data
File: personnel.csv
```

- `updateExisting=false` → Mevcut kullanıcıları atla
- `updateExisting=true` → Mevcut kullanıcıları güncelle
