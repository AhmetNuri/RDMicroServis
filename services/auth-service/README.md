# Auth Service

JWT authentication microservice for the distributed commerce platform.

## API Endpoints

| Method | Path | Auth | Description |
|--------|------|------|-------------|
| POST | /api/auth/register | None | Register new user |
| POST | /api/auth/login | None | Login and get tokens |
| POST | /api/auth/refresh | None | Refresh access token |
| POST | /api/auth/logout | None | Revoke refresh token |
| GET | /api/auth/me | Bearer | Get current user |
| GET | /api/users | Admin | List all users |
| PUT | /api/users/{id}/roles | Admin | Update user roles |
| GET | /health | None | Health check |
| GET | /metrics | None | Prometheus metrics |

## Environment Variables

| Variable | Default | Description |
|----------|---------|-------------|
| ConnectionStrings__DefaultConnection | - | PostgreSQL connection string |
| Jwt__Secret | - | JWT signing secret (min 32 chars) |
| Jwt__Issuer | auth-service | JWT issuer |
| Jwt__Audience | commerce-platform | JWT audience |
| Otlp__Endpoint | http://otel-collector:4317 | OpenTelemetry collector |
| Cors__Origins | http://localhost:3000 | Allowed CORS origins (comma-separated) |

## Local Development

```bash
# Set environment variables
export ConnectionStrings__DefaultConnection="Host=localhost;Database=authdb;Username=postgres;Password=postgres"
export Jwt__Secret="my-super-secret-key-at-least-32-characters"

# Run
cd src/AuthService
dotnet run
```

## Default Admin Credentials
- Email: admin@commerce.local
- Password: Admin@123456
