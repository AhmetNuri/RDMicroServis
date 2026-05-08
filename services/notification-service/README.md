# Notification Service

Real-time notification microservice with MongoDB, SignalR, Kafka, and RabbitMQ.

## API Endpoints

| Method | Path | Auth | Description |
|--------|------|------|-------------|
| GET | /api/notifications/{userId} | Bearer | Get user notifications |
| PUT | /api/notifications/{id}/read | Bearer | Mark as read |
| GET | /api/notifications/{userId}/unread-count | Bearer | Get unread count |
| GET | /health | None | Health check |
| GET | /metrics | None | Prometheus metrics |
| WS | /hubs/notifications | Bearer | SignalR hub |

## Environment Variables

| Variable | Description |
|----------|-------------|
| MongoDB__ConnectionString | MongoDB connection string |
| MongoDB__DatabaseName | MongoDB database name |
| Kafka__BootstrapServers | Kafka bootstrap servers |
| RabbitMQ__Host | RabbitMQ hostname |
| RabbitMQ__Username | RabbitMQ username |
| RabbitMQ__Password | RabbitMQ password |
| Jwt__Secret | JWT signing secret |
| Otlp__Endpoint | OpenTelemetry collector endpoint |

## Message Consumers

- **Kafka**: Subscribes to `notification.send` topic
- **RabbitMQ**: Consumes `send-email` queue with DLQ support (`send-email.dlq`)

## SignalR

Connect to `/hubs/notifications?access_token=<jwt>`. Call `JoinUserGroup(userId)` after connection.
