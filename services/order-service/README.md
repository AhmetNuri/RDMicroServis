# Order Service

Order management microservice with MSSQL, Outbox Pattern, and full-text search.

## API Endpoints

| Method | Path | Auth | Description |
|--------|------|------|-------------|
| POST | /api/orders | Bearer | Create order |
| GET | /api/orders/{id} | Bearer | Get order by ID |
| GET | /api/orders | Bearer | Get current user's orders |
| GET | /api/orders/user/{userId} | Bearer | Get user's orders |
| PUT | /api/orders/{id}/status | Admin/Operator | Update order status |
| POST | /api/orders/{id}/cancel | Bearer | Cancel order |
| GET | /api/orders/search | Bearer | Search orders |
| GET | /health | None | Health check |
| GET | /metrics | None | Prometheus metrics |

## Environment Variables

| Variable | Description |
|----------|-------------|
| ConnectionStrings__DefaultConnection | MSSQL connection string |
| Jwt__Secret | JWT signing secret |
| Kafka__BootstrapServers | Kafka bootstrap servers |
| Otlp__Endpoint | OpenTelemetry collector endpoint |

## Outbox Pattern

Orders create an outbox message atomically. The OutboxPublisher background service polls for unprocessed messages and publishes them to Kafka topics matching the event type (order.created, order.cancelled, etc.).
