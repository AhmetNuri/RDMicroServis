# Search Indexer Service

Elasticsearch product indexing service with Kafka consumer and edge-ngram partial search.

## API Endpoints

| Method | Path | Auth | Description |
|--------|------|------|-------------|
| GET | /api/search/products | None | Search products |
| GET | /api/search/products/{id} | None | Get product by ID |
| POST | /api/search/products/index | Admin | Index/update a product |
| DELETE | /api/search/products/{id} | Admin | Delete from index |
| GET | /health | None | Health check |
| GET | /metrics | None | Prometheus metrics |

## Environment Variables

| Variable | Description |
|----------|-------------|
| Elasticsearch__Uri | Elasticsearch URI |
| Kafka__BootstrapServers | Kafka bootstrap servers |
| Jwt__Secret | JWT signing secret |
| Otlp__Endpoint | OpenTelemetry collector endpoint |

## Search Features

- **Edge-ngram analyzer**: Partial matching (min=2, max=20 chars) on name and description
- **Boosted fields**: Name field has 3x boost over description
- **Filters**: category, minPrice, maxPrice, isActive=true
- **Kafka topics**: product.created, product.updated, product.deleted
