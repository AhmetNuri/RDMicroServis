# Product Service

Laravel 12 + PHP 8.4 microservice for product management. Uses MongoDB for flexible product metadata storage and full-text search.

## Technology Stack
- PHP 8.4 + Laravel 12
- MongoDB 7 (via mongodb/laravel-mongodb)
- Docker (PHP-FPM + Nginx)

## API Endpoints

| Method | Path | Description | Auth |
|--------|------|-------------|------|
| GET | /api/products | List products (paginated) | No |
| GET | /api/products/search?q=... | MongoDB text search | No |
| GET | /api/products/{id} | Get product by ID | No |
| POST | /api/products | Create product | JWT |
| PUT | /api/products/{id} | Update product | JWT |
| DELETE | /api/products/{id} | Soft-delete product | JWT |
| GET | /api/health | Health check | No |

## Environment Variables

| Variable | Description |
|----------|-------------|
| MONGO_PRODUCT_HOST | MongoDB host |
| MONGO_PRODUCT_PORT | MongoDB port |
| MONGO_PRODUCT_DB | Database name |
| MONGO_PRODUCT_USER | MongoDB username |
| MONGO_PRODUCT_PASSWORD | MongoDB password |
| JWT_SECRET | JWT signing secret (shared with Auth Service) |
| KAFKA_BROKERS | Kafka broker addresses |

## Local Development
```bash
cp .env.example .env
composer install
php artisan serve --port=8007
```

## Docker
```bash
docker build -t product-service .
docker run -p 8007:8080 --env-file .env product-service
```
