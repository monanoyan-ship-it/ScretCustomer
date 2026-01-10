# GitHub'a Manuel Push Talimatları

## Sorun
Git push komutları 403 (Permission Denied) hatası veriyor. Token geçerli olmasına rağmen push yapılamıyor.

## Çözüm Seçenekleri

### Seçenek 1: GitHub Desktop Kullan (En Kolay)

1. **GitHub Desktop'ı İndir**: https://desktop.github.com/
2. Uygulamayı aç ve GitHub hesabınla giriş yap
3. File → Add Local Repository
4. Klasör seç: `C:\Users\ahmet\source\repos\ScretCustomer`
5. "Publish repository" butonuna tıkla
6. Repository name: `ScretCustomer`
7. ✅ Publish!

### Seçenek 2: Git Credential Manager

```bash
# Git Credential Manager kur
winget install --id=Git.Git  -e

# Credential helper ayarla
git config --global credential.helper manager-core

# Remote'u sıfırla
cd "C:\Users\ahmet\source\repos\ScretCustomer"
git remote remove origin
git remote add origin https://github.com/monanoyan-ship-it/ScretCustomer.git

# Push (credential popup açılacak)
git push -u origin main
```

Popup açıldığında:
- Username: `monanoyan-ship-it`
- Password: (Personal Access Token'ı yapıştır)

### Seçenek 3: SSH Key Kullan

```bash
# SSH key oluştur
ssh-keygen -t ed25519 -C "your_email@example.com"

# Public key'i kopyala
cat ~/.ssh/id_ed25519.pub

# GitHub'a ekle:
# Settings → SSH and GPG keys → New SSH key
# Title: "My Computer"
# Key: (kopyaladığın public key'i yapıştır)

# Remote'u SSH'e çevir
git remote set-url origin git@github.com:monanoyan-ship-it/ScretCustomer.git

# Push
git push -u origin main
```

### Seçenek 4: Visual Studio Kullan

1. Visual Studio'da projeyi aç
2. View → Git Changes
3. "Create Git Repository" varsa tıkla, yoksa devam et
4. Remote URL: `https://github.com/monanoyan-ship-it/ScretCustomer.git`
5. "Sync" veya "Push" butonuna tıkla
6. GitHub credentials sor arsa gir

### Seçenek 5: GitHub CLI (gh)

```bash
# GitHub CLI kur
winget install --id GitHub.cli

# Authenticate
gh auth login
# → GitHub.com seç
# → HTTPS seç
# → Login with a web browser seç
# → Tarayıcıda login yap

# Repository'ye push
cd "C:\Users\ahmet\source\repos\ScretCustomer"
gh repo sync monanoyan-ship-it/ScretCustomer --source .
```

### Seçenek 6: Manuel Dosya Yükleme (Son Çare)

1. GitHub'da repository'ye git: https://github.com/monanoyan-ship-it/ScretCustomer
2. "uploading an existing file" linkine tıkla
3. Tüm dosyaları sürükle-bırak
4. Commit message: "Initial commit"
5. Commit changes

**NOT**: Bu yöntemde git history kaybolur, önerilmez.

## Mevcut Durum

```bash
# Local repository hazır
Branch: main
Commits: 2 (Initial commit + README)
Files: 95+
Status: Push bekliyor

# Remote
URL: https://github.com/monanoyan-ship-it/ScretCustomer
Status: Empty (henüz push yapılmadı)
```

## Token Kontrol

Eğer token sorunu devam ederse, yeni token oluştururken:

1. GitHub → Settings → Developer settings → Personal access tokens → Tokens (classic)
2. "Generate new token (classic)"
3. Note: "ScretCustomer Push Token"
4. Expiration: 30 days (veya istediğin)
5. ✅ **Scopes:**
   - ✅ `repo` (tüm repo permissions)
   - ✅ `workflow` (optional)
6. Generate token
7. Token'ı kopyala ve GÜVENLİ bir yere kaydet

## Sorun Giderme

### "Permission denied" hatası
- Token'ın `repo` scope'una sahip olduğundan emin ol
- Token'ın doğru kullanıcıya ait olduğunu kontrol et
- Repository'nin public olduğundan emin ol (private ise ek izinler gerekebilir)

### "Authentication failed" hatası
- Token'ın expired olmadığından emin ol
- Token'ı kopyalarken boşluk veya newline kopyalanmadığından emin ol
- Token'ı yeniden oluştur

### "Repository not found" hatası
- Repository URL'sini kontrol et
- Repository'nin gerçekten var olduğunu kontrol et
- Repository adının doğru yazıldığından emin ol

## Önerilen Çözüm

En hızlı ve kolay yöntem **GitHub Desktop** kullanmak. Birkaç tıklama ile tüm projeyi yükleyebilirsiniz.

Alternatif olarak **GitHub CLI (gh)** çok güvenilir ve hızlı çalışıyor.
