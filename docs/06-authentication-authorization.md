# Authentication & Authorization Sistemi

## Özet
JWT (JSON Web Token) tabanlı kimlik doğrulama ve yetkilendirme sistemi.

## API Endpoints

- `POST /api/auth/login` - Kullanıcı girişi
- `POST /api/auth/register` - Yeni kullanıcı kaydı

## JWT Configuration

**appsettings.json:**
```json
{
  "JwtSettings": {
    "SecretKey": "YourSuperSecretKeyForJWT_MinLength32Characters!",
    "Issuer": "SecretCustomerAPI",
    "Audience": "SecretCustomerClient",
    "ExpirationMinutes": 1440
  }
}
```

## Login Request/Response

**Request:**
```json
{
  "username": "admin",
  "password": "password123"
}
```

**Response:**
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "userId": "guid",
  "username": "admin",
  "fullName": "Admin User",
  "role": "Admin",
  "branchId": null
}
```

## Register Request

```json
{
  "username": "newuser",
  "email": "user@example.com",
  "password": "password123",
  "firstName": "John",
  "lastName": "Doe",
  "role": "Evaluator",
  "branchId": "guid"
}
```

## Password Hashing
- **Library**: BCrypt.Net-Next
- **Algorithm**: BCrypt
- **Auto-salt**: Yes

## JWT Claims
- `NameIdentifier`: User ID
- `Name`: Username
- `Email`: User email
- `Role`: User role (Admin, TeamLeader, Evaluator, CustomerRepresentative)
- `BranchId`: Branch ID (if applicable)

## Usage in Controllers

```csharp
[Authorize] // Require authentication
[Authorize(Roles = "Admin")] // Require specific role
```

## Token Expiration
- Default: 1440 minutes (24 hours)
- Configurable via appsettings.json

---
**Backend Modülleri Tamamlandı!**
- ✓ Kontrol Listesi
- ✓ Atama Mantığı
- ✓ Dashboard & Raporlama
- ✓ Authentication & Authorization
