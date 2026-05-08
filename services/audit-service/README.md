# Audit Service

Audit trail microservice with dual-write to Elasticsearch and PostgreSQL (tsvector FTS).

## API Endpoints

| Method | Path | Auth | Description |
|--------|------|------|-------------|
| POST | /api/audit/events | Admin/Operator | Record audit event |
| GET | /api/audit/events | Admin/Operator | Search events |
| GET | /api/audit/events/{id} | Admin/Operator | Get event by ID |
| GET | /api/audit/events/user/{userId} | Admin/Operator | Get user events |
| GET | /health | None | Health check |
| GET | /metrics | None | Prometheus metrics |

## Environment Variables

| Variable | Description |
|----------|-------------|
| ConnectionStrings__DefaultConnection | PostgreSQL connection string |
| Elasticsearch__Uri | Elasticsearch URI |
| Kafka__BootstrapServers | Kafka bootstrap servers |
| Jwt__Secret | JWT signing secret |
| Otlp__Endpoint | OpenTelemetry collector endpoint |

## Kafka Topics Consumed

order.created, order.cancelled, payment.completed, payment.failed, inventory.reserved, inventory.released, product.created, product.updated
