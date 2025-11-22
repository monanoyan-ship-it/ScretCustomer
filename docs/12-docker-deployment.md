# Docker ile Deployment

Bu doküman, Secret Customer uygulamasının Docker ile nasıl deploy edileceğini açıklar.

## Gereksinimler

- Docker Engine 20.10+
- Docker Compose 2.0+

## Dosya Yapısı

```
ScretCustomer/
├── docker-compose.yml          # Ana orchestration dosyası
├── nginx.conf                  # Frontend nginx yapılandırması
├── .env.example               # Örnek environment dosyası
├── Backend/
│   ├── .dockerignore          # Docker build'den hariç tutulacaklar
│   └── SecretCustomer.API/
│       └── Dockerfile         # API container tanımı
└── Frontend/
    └── wwwroot/              # Statik frontend dosyaları
```

## Hızlı Başlangıç

### 1. Environment Dosyasını Hazırlayın

```bash
cp .env.example .env
```

`.env` dosyasını düzenleyin ve güvenli şifreler ayarlayın:

```env
DB_PASSWORD=YourSecureDBPassword123!
JWT_SECRET=YourSuperSecretKeyForJWT_MinLength32CharactersOrMore!
```

### 2. Uygulamayı Başlatın

```bash
docker-compose up -d
```

Bu komut 3 servisi başlatır:
- PostgreSQL Database (Port: 5432)
- .NET API (Port: 5000)
- Nginx Frontend (Port: 3000)

### 3. Logları Kontrol Edin

```bash
# Tüm servislerin logları
docker-compose logs -f

# Sadece API logları
docker-compose logs -f api

# Sadece Database logları
docker-compose logs -f postgres
```

### 4. Uygulamaya Erişin

- **Frontend**: http://localhost:3000
- **API**: http://localhost:5000
- **API Swagger**: http://localhost:5000/swagger (Development modda)

## Servisler

### PostgreSQL Database

```yaml
Image: postgres:16-alpine
Port: 5432
Volume: postgres_data
Health Check: pg_isready
```

**Environment Variables:**
- `POSTGRES_DB`: SecretCustomerDB
- `POSTGRES_USER`: postgres
- `POSTGRES_PASSWORD`: ${DB_PASSWORD}

### .NET API

```yaml
Build: Backend/SecretCustomer.API/Dockerfile
Port: 5000 (mapped to 8080 inside container)
Depends on: postgres (healthy)
```

**Environment Variables:**
- `ASPNETCORE_ENVIRONMENT`: Production
- `ConnectionStrings__DefaultConnection`: PostgreSQL bağlantı stringi
- `JwtSettings__SecretKey`: JWT secret key
- `JwtSettings__Issuer`: SecretCustomerAPI
- `JwtSettings__Audience`: SecretCustomerClient

### Frontend (Nginx)

```yaml
Image: nginx:alpine
Port: 3000 (mapped to 80 inside container)
Volumes: Frontend/wwwroot, nginx.conf
```

## Yararlı Komutlar

### Container Yönetimi

```bash
# Servisleri durdur
docker-compose down

# Servisleri durdur ve volumeleri sil
docker-compose down -v

# Servisleri yeniden başlat
docker-compose restart

# Tek bir servisi yeniden başlat
docker-compose restart api

# Servislerin durumunu kontrol et
docker-compose ps
```

### Build ve Update

```bash
# Yeniden build et ve başlat
docker-compose up -d --build

# Sadece API'yi yeniden build et
docker-compose build api
docker-compose up -d api

# Tüm imageları yeniden oluştur
docker-compose build --no-cache
```

### Database İşlemleri

```bash
# Database'e bağlan
docker exec -it secretcustomer-db psql -U postgres -d SecretCustomerDB

# Database backup al
docker exec secretcustomer-db pg_dump -U postgres SecretCustomerDB > backup.sql

# Backup'tan restore et
cat backup.sql | docker exec -i secretcustomer-db psql -U postgres -d SecretCustomerDB

# Migration çalıştır (API container içinde)
docker exec secretcustomer-api dotnet ef database update
```

### Debugging

```bash
# API container'a bash ile bağlan
docker exec -it secretcustomer-api /bin/bash

# Container resource kullanımını göster
docker stats

# Container detaylarını incele
docker inspect secretcustomer-api

# Network bağlantılarını kontrol et
docker network inspect secretcustomer-network
```

## Production Deployment

### 1. Security

Production için aşağıdaki ayarları yapın:

**.env dosyası:**
```env
# Güçlü şifreler kullanın
DB_PASSWORD=<RandomSecurePassword>
JWT_SECRET=<RandomSecureSecret>
ASPNETCORE_ENVIRONMENT=Production
```

**docker-compose.yml güncellemeleri:**
```yaml
api:
  environment:
    # HTTPS için sertifika ekleyin
    - ASPNETCORE_URLS=https://+:8081;http://+:8080
    - ASPNETCORE_Kestrel__Certificates__Default__Path=/https/cert.pfx
    - ASPNETCORE_Kestrel__Certificates__Default__Password=${CERT_PASSWORD}
  volumes:
    - ./certificates:/https:ro
```

### 2. Reverse Proxy (Nginx/Traefik)

Production'da frontend nginx'in önüne bir reverse proxy ekleyin:

```nginx
server {
    listen 443 ssl http2;
    server_name your-domain.com;

    ssl_certificate /etc/ssl/certs/cert.pem;
    ssl_certificate_key /etc/ssl/private/key.pem;

    location / {
        proxy_pass http://localhost:3000;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
    }
}
```

### 3. Monitoring

Prometheus ve Grafana ekleyin:

```yaml
# docker-compose.yml'ye ekleyin
services:
  prometheus:
    image: prom/prometheus
    volumes:
      - ./prometheus.yml:/etc/prometheus/prometheus.yml
    ports:
      - "9090:9090"

  grafana:
    image: grafana/grafana
    ports:
      - "3001:3000"
    depends_on:
      - prometheus
```

### 4. Backup Strategy

Otomatik backup için cron job:

```bash
# /etc/cron.daily/backup-secretcustomer
#!/bin/bash
DATE=$(date +%Y%m%d_%H%M%S)
docker exec secretcustomer-db pg_dump -U postgres SecretCustomerDB | \
  gzip > /backups/secretcustomer_$DATE.sql.gz
find /backups -name "secretcustomer_*.sql.gz" -mtime +7 -delete
```

## Troubleshooting

### API başlamıyor

```bash
# Logları kontrol edin
docker-compose logs api

# Database bağlantısını test edin
docker exec secretcustomer-api ping postgres

# Environment variables'ı kontrol edin
docker exec secretcustomer-api env | grep ConnectionStrings
```

### Database bağlantı hatası

```bash
# PostgreSQL'in hazır olup olmadığını kontrol edin
docker exec secretcustomer-db pg_isready

# PostgreSQL loglarını inceleyin
docker-compose logs postgres

# Manuel bağlantı testi
docker exec -it secretcustomer-db psql -U postgres -d SecretCustomerDB
```

### Port çakışması

```bash
# Kullanılan portları kontrol edin
netstat -an | grep -E '5000|5432|3000'

# docker-compose.yml'deki portları değiştirin
```

### Disk alanı problemi

```bash
# Kullanılmayan imageları temizle
docker image prune -a

# Kullanılmayan volumeleri temizle
docker volume prune

# Tüm kullanılmayan kaynakları temizle
docker system prune -a --volumes
```

## Performance Tuning

### PostgreSQL

```yaml
postgres:
  environment:
    - POSTGRES_INITDB_ARGS=--encoding=UTF-8 --lc-collate=C --lc-ctype=C
  command: >
    postgres
    -c shared_buffers=256MB
    -c max_connections=200
    -c work_mem=6MB
```

### API

```yaml
api:
  deploy:
    resources:
      limits:
        cpus: '2'
        memory: 2G
      reservations:
        cpus: '1'
        memory: 512M
```

## Güvenlik En İyi Uygulamaları

1. **.env dosyasını asla commit etmeyin**
   ```bash
   echo ".env" >> .gitignore
   ```

2. **Non-root user kullanın** (Dockerfile'da zaten yapılandırılmış)

3. **Health check'leri kullanın** (docker-compose.yml'de tanımlı)

4. **Secrets için Docker secrets kullanın** (Swarm mode)
   ```yaml
   services:
     api:
       secrets:
         - db_password
         - jwt_secret
   ```

5. **Container'ları güncel tutun**
   ```bash
   docker-compose pull
   docker-compose up -d
   ```

## Sonraki Adımlar

- Kubernetes deployment için manifests hazırlayın
- CI/CD pipeline ekleyin (GitHub Actions, Azure DevOps)
- Distributed tracing ekleyin (Jaeger, Zipkin)
- Service mesh kullanın (Istio, Linkerd)
