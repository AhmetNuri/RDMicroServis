# Analytics Service

Metrics aggregation and analytics microservice with PostgreSQL.

## API Endpoints

| Method | Path | Auth | Description |
|--------|------|------|-------------|
| GET | /api/analytics/dashboard | Admin/Operator | Get dashboard metrics |
| GET | /api/analytics/orders | Admin/Operator | Order analytics |
| GET | /api/analytics/revenue | Admin/Operator | Revenue metrics |
| GET | /api/analytics/products | Admin/Operator | Top products |
| POST | /api/analytics/events | Admin/Operator | Record custom metric |
| GET | /health | None | Health check |
| GET | /metrics | None | Prometheus metrics |

## Environment Variables

| Variable | Description |
|----------|-------------|
| ConnectionStrings__DefaultConnection | PostgreSQL connection string |
| Kafka__BootstrapServers | Kafka bootstrap servers |
| Jwt__Secret | JWT signing secret |
| Otlp__Endpoint | OpenTelemetry collector endpoint |
