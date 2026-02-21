# Claude Code Proje Kuralları

> **Kurallar ClaudeManager'da. Her oturumda guide API'yi oku.**

## ClaudeManager Entegrasyonu (ZORUNLU!)

- **Proje ID:** 12
- **Proje adı:** ScretCustomer
- **cwd:** `c:/Users/Ahmet/source/repos/monanoyan-ship-it/ScretCustomer` (forward slash, `$(pwd)` KULLANMA)

**Her oturum başında** guide'ı oku ve dönen kuralları uygula:

```bash
curl -s "http://127.0.0.1:41847/api/guide?cwd=c:/Users/Ahmet/source/repos/monanoyan-ship-it/ScretCustomer"
```

Bu API tüm pattern'leri, roadmap durumunu ve kullanılabilir endpoint'leri tek seferde döner. **Tüm kurallara uy!**

### 1. Pattern'ler (Kurallar / Hatalar / Tercihler)

Pattern tipleri: `rule`, `mistake`, `preference`

```bash
# Oku
curl -s http://127.0.0.1:41847/api/projects/12/patterns

# Yeni kaydet
curl -X POST http://127.0.0.1:41847/api/patterns -H "Content-Type: application/json" \
  -d '{"project_id":12,"type":"rule|mistake|preference","title":"BASLIK","description":"ACIKLAMA"}'

# Güncelle
curl -X PUT http://127.0.0.1:41847/api/patterns/PATTERN_ID -H "Content-Type: application/json" \
  -d '{"title":"YENI_BASLIK","description":"YENI_ACIKLAMA"}'

# Sil
curl -X DELETE http://127.0.0.1:41847/api/patterns/PATTERN_ID
```

**Ne zaman kaydet:**
- Kullanıcı bir hata yaptığını söylerse → `mistake`
- Kullanıcı bir tercih belirtirse → `preference`
- Tekrarlanan bir pattern fark edersen → `rule`

### 2. Yol Haritası / Planlama

```bash
curl -s http://127.0.0.1:41847/api/projects/12/roadmap
curl -s http://127.0.0.1:41847/api/projects/12/roadmap/stats

# Yeni faz
curl -X POST http://127.0.0.1:41847/api/projects/12/phases \
  -H "Content-Type: application/json" -d '{"phase_no":"1","title":"FAZ ADI"}'

# Faza görev ekle
curl -X POST http://127.0.0.1:41847/api/phases/FAZ_ID/tasks \
  -H "Content-Type: application/json" -d '{"task_no":"1.1","title":"GOREV","detail":"DETAY","risks":"RISKLER"}'

# Görev durumu güncelle
curl -X PUT http://127.0.0.1:41847/api/tasks/GOREV_ID \
  -H "Content-Type: application/json" -d '{"status":"in_progress|completed|cancelled"}'

# XML'den toplu import
curl -X POST http://127.0.0.1:41847/api/projects/12/roadmap/import \
  -H "Content-Type: application/json" -d '{"xml":"<Faz no=\"1\" ad=\"Faz Adi\" durum=\"planned\"><Gorev no=\"1.1\" durum=\"planned\"><Ad>Gorev adi</Ad><Detay>Aciklama</Detay><Riskler>Risk</Riskler></Gorev></Faz>"}'
```

### 3. Günlük / Notlar / Arama

```bash
# Günlük
curl -s http://127.0.0.1:41847/api/projects/12/journal
curl -X POST http://127.0.0.1:41847/api/projects/12/journal -H "Content-Type: application/json" \
  -d '{"title":"BASLIK","content":"ICERIK","category":"genel|teknik|karar|arastirma"}'

# Notlar
curl -s http://127.0.0.1:41847/api/projects/12/notes
curl -X POST http://127.0.0.1:41847/api/projects/12/notes -H "Content-Type: application/json" \
  -d '{"title":"BASLIK","content":"ICERIK","category":"teknik"}'

# Arama
curl -s "http://127.0.0.1:41847/api/search?q=ARAMA_TERIMI&project=12"
```

Dashboard: http://127.0.0.1:41847/

---

> Proje yapısı, kurallar, hatalar, tercihler → hepsi ClaudeManager'da. Buraya duplike bilgi YAZMA.
