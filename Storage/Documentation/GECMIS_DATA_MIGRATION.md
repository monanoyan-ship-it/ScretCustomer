# Geçmiş Data Migration Kılavuzu

> **Oluşturulma Tarihi:** 2026-01-20
> **Amaç:** Eski veritabanından yeni sisteme veri aktarımı

---

## Kullanıcı Talebi (Orijinal)

> "Bizim database imize tam entegre eski datayı çekmek için bir excel kalıbı oluşturulacak. Ama şuradan şuraya öyle bir liste olacak ki o listeden firmaları firmalardaki yöneticileri o firmadaki takım liderlerini altındaki personelleri checklistleri Projeleri projelere tanımlı dönemleri proje atamalarını o proje atamalarının dinlemeleri dinlemelerin aldığı puanları notları açıklamaları puan hesaplamasını yani tüm süreci eski database den alıp sana atmamızı sağlayacak. Ya da ben sana oradan alabileceğim her şeyi bir klasöre atıcam sen oradan tahminen bir kayıt oluşturma excel i oluşturacaksın (csv de olur burada excel şart değil) hangi yol sana uyarsa."

---

## Veri Hiyerarşisi

```
User (Şirket Personeli - Değerlendirmeyi Yapanlar)
├── RoleId: Admin/QualitySpecialist/FieldWorker
├── Assignments (Atandığı işler)
└── Evaluations (Yaptığı değerlendirmeler)

Customer (Firma - Değerlendirilen Müşteri)
├── CustomerOrganization (Organizasyon/Şube)
│   └── CustomerPersonnelOrganization (Personel-Org İlişkisi)
├── CustomerPersonnel (Firma Personeli - Değerlendirilenler)
│   ├── RoleId: Manager/Supervisor/Operator
│   └── SupervisorId (Takım Lideri bağlantısı)
├── Checklist (Kontrol Listesi)
│   ├── Question (Soru/Kriter)
│   │   └── QuestionSubCriteria (Alt Kriter/Öneri)
│   └── Projects (Projeler)
└── Project (Proje)
    └── Assignment (Atama → User'a atanır)
        ├── AssignmentPeriod (Dönem)
        └── Evaluation (Değerlendirme/Dinleme)
            └── Answer (Cevap)
                └── AnswerSubCriteriaSelection (Seçilen Alt Kriterler)
```

### User vs CustomerPersonnel Farkı

| Entity | Açıklama | Örnek |
|--------|----------|-------|
| **User** | SİZİN şirket personeliniz - değerlendirme YAPANLAR | Kalite uzmanı, saha çalışanı, admin |
| **CustomerPersonnel** | MÜŞTERİ firmanın personeli - değerlendirme YAPILANLAR | Çağrı merkezi ajanı, şube müdürü |

---

## CSV Dosya Yapısı ve Import Sırası

### **AŞAMA 1: Temel Veriler (Bağımsız)**

#### 1.1. `00_users.csv` - Şirket Personeli (Değerlendirme Yapanlar)
```csv
Username,Email,FirstName,LastName,RoleId,IsActive,PhoneNumber
admin,admin@sirket.com,Sistem,Admin,1,true,
ahmet.yilmaz,ahmet@sirket.com,Ahmet,Yılmaz,2,true,0532 111 2233
mehmet.kaya,mehmet@sirket.com,Mehmet,Kaya,2,true,0533 222 3344
fatma.demir,fatma@sirket.com,Fatma,Demir,3,true,0534 333 4455
```

| Alan | Tip | Zorunlu | Açıklama |
|------|-----|---------|----------|
| Username | Text | **Evet** | Kullanıcı adı (login için) |
| Email | Text | **Evet** | E-posta (benzersiz) |
| FirstName | Text | **Evet** | Ad |
| LastName | Text | **Evet** | Soyad |
| RoleId | Number | **Evet** | Rol: 1=Admin, 2=QualitySpecialist, 3=FieldWorker |
| IsActive | Boolean | Hayır | Aktif mi? (varsayılan: true) |
| PhoneNumber | Text | Hayır | Telefon |

**Rol Açıklamaları:**
- **1 = Admin**: Sistem yöneticisi, tüm yetkilere sahip
- **2 = QualitySpecialist**: Kalite uzmanı, çağrı dinleme ve değerlendirme yapar
- **3 = FieldWorker**: Saha çalışanı, bayi ziyareti ve fiziksel denetim yapar

---

#### 1.2. `01_customers.csv` - Firmalar
```csv
Code,CompanyName,TaxNumber,Phone,Email,Address,City,IsActive,ContractStartDate,ContractEndDate,Notes,TargetCount,DailyQuota,WeeklyQuota,MonthlyQuota
MUS-001,ABC Perakende A.Ş.,1234567890,0212 123 4567,info@abc.com,"Maslak, İstanbul",İstanbul,true,2024-01-01,2025-12-31,Önemli müşteri,100,5,20,80
MUS-002,XYZ Holding,9876543210,0216 456 7890,info@xyz.com,"Kadıköy, İstanbul",İstanbul,true,2024-06-01,,Yeni müşteri,50,,,
```

| Alan | Tip | Zorunlu | Açıklama |
|------|-----|---------|----------|
| Code | Text | Hayır | Müşteri kodu (MUS-001) |
| CompanyName | Text | **Evet** | Firma adı |
| TaxNumber | Text | **Evet** | Vergi numarası |
| Phone | Text | Hayır | Telefon |
| Email | Text | Hayır | E-posta |
| Address | Text | Hayır | Adres |
| City | Text | Hayır | Şehir |
| IsActive | Boolean | Hayır | Aktif mi? (varsayılan: true) |
| ContractStartDate | Date | Hayır | Sözleşme başlangıç (YYYY-MM-DD) |
| ContractEndDate | Date | Hayır | Sözleşme bitiş |
| Notes | Text | Hayır | Notlar |
| TargetCount | Number | Hayır | Hedef değerlendirme sayısı |
| DailyQuota | Number | Hayır | Günlük kota |
| WeeklyQuota | Number | Hayır | Haftalık kota |
| MonthlyQuota | Number | Hayır | Aylık kota |

---

### **AŞAMA 2: Organizasyon Yapısı**

#### 2.1. `02_organizations.csv` - Organizasyonlar/Şubeler
```csv
CustomerCode,Name,Code,Description,Level,Order,IsActive,ParentCode
MUS-001,Concentrix,CONC-001,Ana operasyon merkezi,0,1,true,
MUS-001,Concentrix Kadıköy,CONC-002,Kadıköy şubesi,1,1,true,CONC-001
MUS-001,Concentrix Maslak,CONC-003,Maslak şubesi,1,2,true,CONC-001
```

| Alan | Tip | Zorunlu | Açıklama |
|------|-----|---------|----------|
| CustomerCode | Text | **Evet** | Firma kodu (ilişki) |
| Name | Text | **Evet** | Organizasyon adı |
| Code | Text | Hayır | Organizasyon kodu |
| Description | Text | Hayır | Açıklama |
| Level | Number | Hayır | Hiyerarşi seviyesi (0=kök) |
| Order | Number | Hayır | Sıralama |
| IsActive | Boolean | Hayır | Aktif mi? |
| ParentCode | Text | Hayır | Üst organizasyon kodu (hiyerarşi) |

---

### **AŞAMA 3: Personel**

#### 3.1. `03_personnel.csv` - Müşteri Personeli
```csv
CustomerCode,Username,Email,FirstName,LastName,PhoneNumber,Department,Title,RoleId,IsActive,Notes
MUS-001,ahmet.yilmaz,ahmet@abc.com,Ahmet,Yılmaz,0555 111 2233,Kalite,Kalite Müdürü,1,true,Yönetici
MUS-001,mehmet.demir,mehmet@abc.com,Mehmet,Demir,0555 222 3344,Kalite,Takım Lideri,2,true,Supervisor
MUS-001,ali.kaya,ali@abc.com,Ali,Kaya,0555 333 4455,Operasyon,Agent,3,true,Operatör
```

| Alan | Tip | Zorunlu | Açıklama |
|------|-----|---------|----------|
| CustomerCode | Text | **Evet** | Firma kodu |
| Username | Text | **Evet** | Kullanıcı adı |
| Email | Text | **Evet** | E-posta |
| FirstName | Text | **Evet** | Ad |
| LastName | Text | **Evet** | Soyad |
| PhoneNumber | Text | Hayır | Telefon |
| Department | Text | Hayır | Departman |
| Title | Text | Hayır | Ünvan |
| RoleId | Number | **Evet** | Rol: 1=Manager, 2=Supervisor, 3=Operator |
| IsActive | Boolean | Hayır | Aktif mi? |
| Notes | Text | Hayır | Notlar |

#### 3.2. `04_personnel_organizations.csv` - Personel-Organizasyon İlişkisi
```csv
PersonnelEmail,OrganizationCode,SupervisorEmail,Notes
ali.kaya@abc.com,CONC-002,mehmet.demir@abc.com,Ana organizasyon
ali.kaya@abc.com,CONC-003,,Yedek organizasyon
```

| Alan | Tip | Zorunlu | Açıklama |
|------|-----|---------|----------|
| PersonnelEmail | Text | **Evet** | Personel e-postası |
| OrganizationCode | Text | **Evet** | Organizasyon kodu |
| SupervisorEmail | Text | Hayır | Supervisor e-postası |
| Notes | Text | Hayır | Notlar |

---

### **AŞAMA 4: Kontrol Listeleri**

#### 4.1. `05_checklists.csv` - Kontrol Listeleri
```csv
Code,Name,Description,IsScored,IsActive,Version,ChecklistTypeId,ScoringMethodId,MaxTotalPoints,CustomerCode,ValidFrom,ValidUntil
KL-001,Çağrı Performans Değerlendirmesi,Müşteri temsilcisi çağrı kalitesi,true,true,1,1,1,100,MUS-001,2024-01-01,
KL-002,Şube Denetim Formu,Fiziksel şube denetimi,true,true,1,2,1,100,,2024-01-01,
```

| Alan | Tip | Zorunlu | Açıklama |
|------|-----|---------|----------|
| Code | Text | Hayır | Kontrol listesi kodu |
| Name | Text | **Evet** | Kontrol listesi adı |
| Description | Text | **Evet** | Açıklama |
| IsScored | Boolean | Hayır | Puanlı mı? |
| IsActive | Boolean | Hayır | Aktif mi? |
| Version | Number | Hayır | Versiyon |
| ChecklistTypeId | Number | Hayır | Tip: 1=CallPerformance, 2=PhysicalAudit, 3=MysteryShopping, 4=OnlineEvaluation, 5=Survey |
| ScoringMethodId | Number | Hayır | Puanlama: 1=Maximum, 2=Average, 3=WeightedAverage, 4=Sum |
| MaxTotalPoints | Number | Hayır | Maksimum puan |
| CustomerCode | Text | Hayır | Firma kodu (boşsa genel) |
| ValidFrom | Date | Hayır | Geçerlilik başlangıç |
| ValidUntil | Date | Hayır | Geçerlilik bitiş |

#### 4.2. `06_questions.csv` - Sorular/Kriterler
```csv
ChecklistCode,Text,Order,ScoringTypeId,WeightPoints,MaxPoints,PenaltyTypeId,AllowNA,IsRequired,RecommendedNote,HelpText,GroupName
KL-001,Açılış standartlarına uyum,1,1,10,5,0,false,true,,Müşteriyi karşılama şekli,Açılış
KL-001,Aktif dinleme,2,1,10,5,0,false,true,,Müşteriyi dinleme kalitesi,İletişim
KL-001,Küfür/Hakaret,3,3,0,1,2,false,true,Kırmızı kart,Küfür tespit edilirse,Cezalar
```

| Alan | Tip | Zorunlu | Açıklama |
|------|-----|---------|----------|
| ChecklistCode | Text | **Evet** | Kontrol listesi kodu |
| Text | Text | **Evet** | Soru metni |
| Order | Number | **Evet** | Sıra |
| ScoringTypeId | Number | Hayır | 1=Scored, 2=Unscored, 3=Penalty |
| WeightPoints | Number | Hayır | Ağırlık puanı |
| MaxPoints | Number | Hayır | Maksimum puan (Likert için) |
| PenaltyTypeId | Number | Hayır | 0=None, 1=YellowCard, 2=RedCard |
| AllowNA | Boolean | Hayır | N/A izni |
| IsRequired | Boolean | Hayır | Zorunlu mu? |
| RecommendedNote | Text | Hayır | Önerilen açıklama |
| HelpText | Text | Hayır | Yardımcı metin |
| GroupName | Text | Hayır | Grup adı (raporlama için) |

#### 4.3. `07_subcriteria.csv` - Alt Kriterler/Öneriler
```csv
ChecklistCode,QuestionOrder,Description,Order,WeightPoints,IsActive
KL-001,1,İsim ile hitap etmedi,1,1,true
KL-001,1,Selamlaşma yapmadı,2,1,true
KL-001,2,Sözünü kesti,1,2,true
```

| Alan | Tip | Zorunlu | Açıklama |
|------|-----|---------|----------|
| ChecklistCode | Text | **Evet** | Kontrol listesi kodu |
| QuestionOrder | Number | **Evet** | Soru sırası (ilişki için) |
| Description | Text | **Evet** | Alt kriter açıklaması |
| Order | Number | **Evet** | Sıra |
| WeightPoints | Number | Hayır | Düşürülecek puan |
| IsActive | Boolean | Hayır | Aktif mi? |

---

### **AŞAMA 5: Projeler ve Atamalar**

#### 5.1. `08_projects.csv` - Projeler
```csv
Code,Name,Description,ChecklistCode,ProjectTypeId,StatusId,AssignmentTypeId,StartDate,EndDate,IsActive,CustomerCode,TargetCount,MinimumScoreThreshold,Priority
PRJ-001,2024 Q1 Çağrı Kalitesi,İlk çeyrek çağrı denetimi,KL-001,2,3,4,2024-01-01,2024-03-31,true,MUS-001,300,70,Medium
PRJ-002,Şube Denetim 2024,Yıllık şube denetimi,KL-002,3,3,1,2024-01-01,2024-12-31,true,MUS-001,100,80,High
```

| Alan | Tip | Zorunlu | Açıklama |
|------|-----|---------|----------|
| Code | Text | Hayır | Proje kodu |
| Name | Text | **Evet** | Proje adı |
| Description | Text | Hayır | Açıklama |
| ChecklistCode | Text | **Evet** | Kontrol listesi kodu |
| ProjectTypeId | Number | **Evet** | 1=MysteryShopping, 2=CallAuditing, 3=PhysicalAudit, 4=OnlineSurvey |
| StatusId | Number | Hayır | 1=Draft, 2=Planned, 3=Active, 4=Paused, 5=Completed |
| AssignmentTypeId | Number | **Evet** | 1=InternalBranch, 2=InternalUser, 3=ExternalCustomer, 4=CustomerPersonnel |
| StartDate | Date | **Evet** | Başlangıç tarihi |
| EndDate | Date | **Evet** | Bitiş tarihi |
| IsActive | Boolean | Hayır | Aktif mi? |
| CustomerCode | Text | Hayır | Firma kodu |
| TargetCount | Number | Hayır | Hedef sayı |
| MinimumScoreThreshold | Number | Hayır | Minimum puan eşiği |
| Priority | Text | Hayır | Low/Medium/High/Critical |

#### 5.2. `09_assignments.csv` - Atamalar (User'lara)
```csv
ProjectCode,TypeId,AssignedUserEmail,AssignedPersonnelEmail,ExternalEmail,ExternalName,DueDate,IsCompleted,CompletedAt
PRJ-001,2,ahmet.yilmaz@sirket.com,,,,2024-03-31,false,
PRJ-001,2,mehmet.kaya@sirket.com,,,,2024-03-31,false,
PRJ-002,3,,,dis.musteri@example.com,Ayşe Yıldız,2024-06-30,true,2024-06-15
```

| Alan | Tip | Zorunlu | Açıklama |
|------|-----|---------|----------|
| ProjectCode | Text | **Evet** | Proje kodu |
| TypeId | Number | Hayır | 1=InternalBranch, **2=InternalUser (en yaygın)**, 3=ExternalCustomer, 4=CustomerPersonnel |
| AssignedUserEmail | Text | **Koşullu** | **Atanan şirket personeli (User) - TypeId=2 ise zorunlu** |
| AssignedPersonnelEmail | Text | Hayır | Atanan müşteri personeli (TypeId=4 ise) |
| ExternalEmail | Text | Hayır | Dış müşteri e-postası (TypeId=3 ise) |
| ExternalName | Text | Hayır | Dış müşteri adı |
| DueDate | Date | **Evet** | Teslim tarihi |
| IsCompleted | Boolean | Hayır | Tamamlandı mı? |
| CompletedAt | Date | Hayır | Tamamlanma tarihi |

**ÖNEMLİ:** Çağrı dinleme projelerinde genellikle `TypeId=2` (InternalUser) kullanılır ve `AssignedUserEmail` şirket personelinizin (kalite uzmanının) e-postası olur.

#### 5.3. `10_periods.csv` - Dönemler
```csv
ProjectCode,AssignmentIndex,Name,StartDate,EndDate,StatusId,TargetCount,Notes
PRJ-001,1,Ocak 2024,2024-01-01,2024-01-31,2,25,İlk dönem
PRJ-001,1,Şubat 2024,2024-02-01,2024-02-29,2,25,
PRJ-001,1,Mart 2024,2024-03-01,2024-03-31,1,25,Son dönem
```

| Alan | Tip | Zorunlu | Açıklama |
|------|-----|---------|----------|
| ProjectCode | Text | **Evet** | Proje kodu |
| AssignmentIndex | Number | **Evet** | Atama sırası (aynı projede) |
| Name | Text | **Evet** | Dönem adı |
| StartDate | Date | **Evet** | Başlangıç |
| EndDate | Date | **Evet** | Bitiş |
| StatusId | Number | Hayır | 1=Open, 2=Closed |
| TargetCount | Number | **Evet** | Hedef sayı |
| Notes | Text | Hayır | Notlar |

---

### **AŞAMA 6: Değerlendirmeler (En Kritik Veri)**

#### 6.1. `11_evaluations.csv` - Değerlendirmeler/Dinlemeler
```csv
ProjectCode,AssignmentIndex,PeriodName,StatusId,TotalScore,MaxScore,ScorePercentage,StartedAt,CompletedAt,Notes,EvaluationComment,CallId,CallDate,CallTime,Duration,EvaluatorEmail,EvaluatedPersonnelEmail,EvaluatedOrganizationCode,YellowCardCount,RedCardCount,ControlDate,ControlTime
PRJ-001,1,Ocak 2024,4,85,100,85,2024-01-15 09:00,2024-01-15 09:30,İyi performans,Genel olarak başarılı,CALL-12345,2024-01-14,14:30,12:26,ahmet.yilmaz@sirket.com,ali.kaya@abc.com,CONC-002,0,0,2024-01-15,09:15
PRJ-001,1,Ocak 2024,4,72,100,72,2024-01-16 10:00,2024-01-16 10:45,Geliştirilmeli,,CALL-12346,2024-01-15,11:00,08:45,ahmet.yilmaz@sirket.com,ali.kaya@abc.com,CONC-002,1,0,2024-01-16,10:30
```

| Alan | Tip | Zorunlu | Açıklama |
|------|-----|---------|----------|
| ProjectCode | Text | **Evet** | Proje kodu |
| AssignmentIndex | Number | **Evet** | Atama sırası |
| PeriodName | Text | Hayır | Dönem adı (yoksa null) |
| StatusId | Number | Hayır | 1=Pending, 2=InProgress, **4=Completed**, 5=Draft |
| TotalScore | Number | Hayır | Toplam puan |
| MaxScore | Number | Hayır | Maksimum puan |
| ScorePercentage | Number | Hayır | Yüzde |
| StartedAt | DateTime | Hayır | Başlangıç zamanı |
| CompletedAt | DateTime | Hayır | Tamamlanma zamanı |
| Notes | Text | Hayır | Notlar |
| EvaluationComment | Text | Hayır | Denetim yorumu |
| CallId | Text | Hayır | Çağrı ID |
| CallDate | Date | Hayır | Çağrı tarihi |
| CallTime | Text | Hayır | Çağrı saati (HH:mm) |
| Duration | Text | Hayır | Süre (mm:ss) |
| EvaluatorEmail | Text | Hayır | **Değerlendirmeyi YAPAN (User - şirket personeli)** |
| EvaluatedPersonnelEmail | Text | Hayır | **Değerlendirilen kişi (CustomerPersonnel - müşteri personeli)** |
| EvaluatedOrganizationCode | Text | Hayır | Değerlendirilen organizasyon |
| YellowCardCount | Number | Hayır | Sarı kart sayısı |
| RedCardCount | Number | Hayır | Kırmızı kart sayısı |
| ControlDate | Date | Hayır | Kontrol tarihi |
| ControlTime | Text | Hayır | Kontrol saati |

**Örnek Akış:**
- `ahmet.yilmaz@sirket.com` (User - Kalite Uzmanı) → `ali.kaya@abc.com` (CustomerPersonnel - Çağrı Merkezi Ajanı) değerlendirmesi yapmış

#### 6.2. `12_answers.csv` - Cevaplar
```csv
ProjectCode,AssignmentIndex,EvaluationIndex,QuestionOrder,AnswerText,AnswerNumeric,IsNA,EarnedPoints,GivenPoints,Notes,RecommendationNotes,IsPenaltyApplied,AppliedPenaltyTypeId
PRJ-001,1,1,1,,5,false,10,5,İyi açılış yaptı,,false,0
PRJ-001,1,1,2,,4,false,8,4,Dinleme iyi ama zaman zaman sözünü kesti,Aktif dinleme eğitimi önerilir,false,0
PRJ-001,1,1,3,,0,false,0,0,,Küfür tespit edilmedi,false,0
```

| Alan | Tip | Zorunlu | Açıklama |
|------|-----|---------|----------|
| ProjectCode | Text | **Evet** | Proje kodu |
| AssignmentIndex | Number | **Evet** | Atama sırası |
| EvaluationIndex | Number | **Evet** | Değerlendirme sırası (aynı atamada) |
| QuestionOrder | Number | **Evet** | Soru sırası |
| AnswerText | Text | Hayır | Metin cevabı |
| AnswerNumeric | Number | Hayır | Sayısal cevap |
| IsNA | Boolean | Hayır | N/A mı? |
| EarnedPoints | Number | Hayır | Kazanılan puan |
| GivenPoints | Number | Hayır | Verilen ham puan |
| Notes | Text | Hayır | Notlar |
| RecommendationNotes | Text | Hayır | Öneri notu |
| IsPenaltyApplied | Boolean | Hayır | Ceza uygulandı mı? |
| AppliedPenaltyTypeId | Number | Hayır | 0=None, 1=YellowCard, 2=RedCard |

#### 6.3. `13_answer_subcriteria.csv` - Seçilen Alt Kriterler
```csv
ProjectCode,AssignmentIndex,EvaluationIndex,QuestionOrder,SubCriteriaOrder
PRJ-001,1,2,2,1
```

| Alan | Tip | Zorunlu | Açıklama |
|------|-----|---------|----------|
| ProjectCode | Text | **Evet** | Proje kodu |
| AssignmentIndex | Number | **Evet** | Atama sırası |
| EvaluationIndex | Number | **Evet** | Değerlendirme sırası |
| QuestionOrder | Number | **Evet** | Soru sırası |
| SubCriteriaOrder | Number | **Evet** | Alt kriter sırası |

---

## Import Sırası (Bağımlılık Sırası)

```
1.  00_users.csv              (bağımsız - ŞİRKET PERSONELİ, atama yapılacak)
2.  01_customers.csv          (bağımsız)
3.  02_organizations.csv      (Customer'a bağlı)
4.  03_personnel.csv          (Customer'a bağlı - MÜŞTERİ PERSONELİ)
5.  04_personnel_organizations.csv (Personnel + Organization'a bağlı)
6.  05_checklists.csv         (opsiyonel Customer'a bağlı)
7.  06_questions.csv          (Checklist'e bağlı)
8.  07_subcriteria.csv        (Question'a bağlı)
9.  08_projects.csv           (Checklist + Customer'a bağlı)
10. 09_assignments.csv        (Project + User'a bağlı - ATAMAYI YAPAN)
11. 10_periods.csv            (Assignment'a bağlı)
12. 11_evaluations.csv        (Assignment + Period + User'a bağlı)
13. 12_answers.csv            (Evaluation + Question'a bağlı)
14. 13_answer_subcriteria.csv (Answer + SubCriteria'ya bağlı)
```

### Özet Akış

```
User (Kalite Uzmanı)
  → Assignment (Atama alır)
    → Evaluation (Değerlendirme yapar)
      → CustomerPersonnel (Müşteri personelini değerlendirir)
```

---

## Puan Hesaplama Mantığı

### Örnek: Ağırlıklı Puan Hesabı

```
Soru 1: WeightPoints=10, MaxPoints=5, Verilen=5 → EarnedPoints = (5/5) * 10 = 10
Soru 2: WeightPoints=10, MaxPoints=5, Verilen=4 → EarnedPoints = (4/5) * 10 = 8
Soru 3: WeightPoints=10, MaxPoints=5, Verilen=3 → EarnedPoints = (3/5) * 10 = 6

TotalScore = 10 + 8 + 6 = 24
MaxScore = 10 + 10 + 10 = 30
ScorePercentage = (24/30) * 100 = 80%
```

### Ceza Puanları
- **YellowCard**: Uyarı (puanı düşürmez, raporda görünür)
- **RedCard**: Değerlendirme başarısız sayılır (0 puan)

---

## Alternatif Yol: Klasör Yapısı

Eski veritabanından export edilen dosyaları şu klasör yapısında da gönderebilirsiniz:

```
migration_data/
├── users/                          # ŞİRKET PERSONELİ (değerlendirme yapanlar)
│   ├── ahmet.yilmaz.json
│   └── mehmet.kaya.json
├── firmalar/                       # MÜŞTERİLER
│   ├── MUS-001_ABC_Perakende.json
│   └── MUS-002_XYZ_Holding.json
├── organizasyonlar/
│   └── ...
├── personeller/                    # MÜŞTERİ PERSONELİ (değerlendirilenler)
│   └── ...
├── checklistler/
│   ├── KL-001.json (sorular dahil)
│   └── ...
├── projeler/
│   └── ...
├── atamalar/                       # User'lara yapılan atamalar
│   └── ...
├── donemler/
│   └── ...
└── degerlendirmeler/
    ├── PRJ-001_evaluations.json (cevaplar dahil)
    └── ...
```

Bu durumda her JSON dosyası ilgili entity'nin tüm alanlarını ve alt ilişkilerini içerebilir.

---

## Notlar

1. **Tarih Formatı**: YYYY-MM-DD (ISO 8601)
2. **DateTime Formatı**: YYYY-MM-DD HH:mm:ss
3. **Boolean**: true/false veya 1/0
4. **Boş Değerler**: Boş bırakın veya null yazın
5. **Encoding**: UTF-8 (Türkçe karakterler için önemli)
6. **CSV Ayırıcı**: Virgül (,) - içeride virgül varsa çift tırnak kullanın

---

## İletişim

Migration işlemi için sorularınız olursa lütfen iletişime geçin.
