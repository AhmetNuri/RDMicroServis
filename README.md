# 🛒 RDMicroServis — Distributed Mini Commerce Platform

A production-grade, distributed e-commerce platform built with a polyglot microservices architecture. Each service is independently deployable, uses the right database for its workload, and communicates asynchronously through Kafka and RabbitMQ.

---

## 🏗️ Architecture Overview

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                          CLIENT LAYER                                        │
│                                                                               │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐  ┌─────────────────┐ │
│  │  shop-web    │  │ landing-web  │  │ admin-portal │  │operations-panel │ │
│  │  (Next.js)   │  │  (Next.js)   │  │  (Angular)   │  │   (Angular)     │ │
│  │  :3001       │  │  :3002       │  │  :4201       │  │   :4202         │ │
│  └──────┬───────┘  └──────┬───────┘  └──────┬───────┘  └───────┬─────────┘ │
└─────────┼────────────────┼────────────────┼───────────────────┼───────────┘
          │                │                │                   │
          └────────────────┴────────────────┴───────────────────┘
                                    │  HTTP/REST
┌───────────────────────────────────▼─────────────────────────────────────────┐
│                         BACKEND SERVICES LAYER                               │
│                                                                               │
│  ┌────────────┐  ┌────────────┐  ┌──────────────────┐  ┌──────────────┐   │
│  │auth-service│  │order-service│  │notification-svc  │  │audit-service │   │
│  │  (.NET 9)  │  │  (.NET 9)  │  │    (.NET 9)       │  │  (.NET 9)    │   │
│  │  :8001     │  │  :8002     │  │    :8003          │  │  :8004       │   │
│  └─────┬──────┘  └─────┬──────┘  └────────┬─────────┘  └──────┬───────┘   │
│        │               │                   │                    │            │
│  ┌─────┴──────┐  ┌─────┴──────┐  ┌────────┴─────────┐  ┌──────┴───────┐   │
│  │analytics-  │  │search-     │  │product-service   │  │payment-svc   │   │
│  │service     │  │indexer-svc │  │  (Node.js/TS)    │  │  (Laravel)   │   │
│  │(.NET 9)    │  │(.NET 9)    │  │  :8007           │  │  :8008       │   │
│  │:8005       │  │:8006       │  └────────┬─────────┘  └──────┬───────┘   │
│  └─────┬──────┘  └─────┬──────┘           │                    │            │
│        │               │           ┌──────┴───────┐            │            │
│        │               │           │inventory-svc │            │            │
│        │               │           │(Node.js/TS)  │            │            │
│        │               │           │:8009         │            │            │
│        │               │           └──────┬───────┘            │            │
└────────┼───────────────┼──────────────────┼────────────────────┼────────────┘
         │               │                  │                     │
         └───────────────┴──────────────────┴─────────────────────┘
                                    │  Events / Messaging
┌───────────────────────────────────▼─────────────────────────────────────────┐
│                      MESSAGING & CACHE LAYER                                 │
│                                                                               │
│  ┌─────────────────────┐  ┌────────────────────┐  ┌──────────────────────┐ │
│  │  Apache Kafka       │  │    RabbitMQ         │  │       Redis          │ │
│  │  (confluentinc      │  │  (3.13-management)  │  │    (7-alpine)        │ │
│  │   cp-kafka:7.7.0)   │  │  :15672 / :15673   │  │    :16379            │ │
│  │  :19092 (ext)       │  └────────────────────┘  └──────────────────────┘ │
│  └─────────────────────┘                                                     │
└─────────────────────────────────────────────────────────────────────────────┘
         │
┌────────▼────────────────────────────────────────────────────────────────────┐
│                         DATA LAYER                                           │
│                                                                               │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐   │
│  │ postgres-auth│  │postgres-     │  │postgres-     │  │ mssql-order  │   │
│  │ (auth svc)   │  │analytics     │  │payment       │  │ (order svc)  │   │
│  │  :55432      │  │  :55433      │  │  :55434      │  │  :11433      │   │
│  └──────────────┘  └──────────────┘  └──────────────┘  └──────────────┘   │
│                                                                               │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐   │
│  │mongo-        │  │mongo-product │  │mongo-        │  │elasticsearch │   │
│  │notification  │  │(product svc) │  │inventory     │  │(audit/search)│   │
│  │  :27017      │  │  :27018      │  │  :27019      │  │  :19200      │   │
│  └──────────────┘  └──────────────┘  └──────────────┘  └──────────────┘   │
└─────────────────────────────────────────────────────────────────────────────┘
         │
┌────────▼────────────────────────────────────────────────────────────────────┐
│                      OBSERVABILITY LAYER                                     │
│                                                                               │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐   │
│  │  Prometheus  │  │   Grafana    │  │     Loki     │  │    Tempo     │   │
│  │   :19090     │  │   :13000     │  │    :13100    │  │    :14317    │   │
│  └──────────────┘  └──────────────┘  └──────────────┘  └──────────────┘   │
│                                                                               │
│  ┌──────────────────────────┐  ┌─────────────────────────────────────────┐ │
│  │    OTel Collector        │  │              Kibana                     │ │
│  │  gRPC :14317 HTTP :14318 │  │              :15601                     │ │
│  └──────────────────────────┘  └─────────────────────────────────────────┘ │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## 🧰 Technology Stack

| Layer            | Technology                              | Version   |
|------------------|-----------------------------------------|-----------|
| Backend API      | ASP.NET Core (auth, order, notification, audit, analytics, search-indexer) | .NET 9 |
| Backend API      | Node.js / TypeScript (product, inventory) | Node 20 LTS |
| Backend API      | Laravel (payment)                       | Laravel 11 / PHP 8.3 |
| Frontend         | Next.js (shop-web, landing-web)         | Next.js 14 |
| Frontend         | Angular (admin-portal, operations-panel) | Angular 17 |
| Database         | PostgreSQL (auth, analytics, payment)   | 16-alpine |
| Database         | MSSQL Server (order)                    | 2022-latest |
| Database         | MongoDB (notification, product, inventory) | 7 |
| Search           | Elasticsearch                           | 8.13.4 |
| Cache            | Redis                                   | 7-alpine |
| Message Broker   | Apache Kafka + Zookeeper                | Confluent 7.7.0 |
| Message Queue    | RabbitMQ                                | 3.13-management |
| Metrics          | Prometheus                              | v2.51.2 |
| Dashboards       | Grafana                                 | 10.4.2 |
| Log Aggregation  | Loki                                    | 2.9.7 |
| Distributed Tracing | Grafana Tempo                        | 2.4.1 |
| Telemetry        | OpenTelemetry Collector (contrib)       | 0.99.0 |
| Log UI           | Kibana                                  | 8.13.4 |
| Containerization | Docker + Docker Compose                 | v3.9 spec |

---

## 📦 Services & Port Reference

### Backend Services

| Service               | Tech         | Host Port | Container Port | Database              |
|-----------------------|--------------|-----------|----------------|-----------------------|
| auth-service          | .NET 9       | 8001      | 8080           | PostgreSQL (authdb)   |
| order-service         | .NET 9       | 8002      | 8080           | MSSQL (orderdb)       |
| notification-service  | .NET 9       | 8003      | 8080           | MongoDB (notificationdb) |
| audit-service         | .NET 9       | 8004      | 8080           | PostgreSQL + Elasticsearch |
| analytics-service     | .NET 9       | 8005      | 8080           | PostgreSQL (analyticsdb) |
| search-indexer-service| .NET 9       | 8006      | 8080           | Elasticsearch         |
| product-service       | Node.js/TS   | 8007      | 8080           | MongoDB (productdb)   |
| payment-service       | Laravel/PHP  | 8008      | 8080           | PostgreSQL (paymentdb)|
| inventory-service     | Node.js/TS   | 8009      | 8080           | MongoDB (inventorydb) |

### Frontend Applications

| Application       | Tech       | Host Port | Container Port |
|-------------------|------------|-----------|----------------|
| shop-web          | Next.js 14 | 3001      | 3000           |
| landing-web       | Next.js 14 | 3002      | 3000           |
| admin-portal      | Angular 17 | 4201      | 80             |
| operations-panel  | Angular 17 | 4202      | 80             |

### Infrastructure Services

| Service            | Image                          | Host Port(s)        |
|--------------------|-------------------------------|---------------------|
| postgres-auth      | postgres:16-alpine             | 55432               |
| postgres-analytics | postgres:16-alpine             | 55433               |
| postgres-payment   | postgres:16-alpine             | 55434               |
| mssql-order        | mssql/server:2022-latest       | 11433               |
| mongo-notification | mongo:7                        | 27017               |
| mongo-product      | mongo:7                        | 27018               |
| mongo-inventory    | mongo:7                        | 27019               |
| elasticsearch      | elasticsearch:8.13.4           | 19200               |
| redis              | redis:7-alpine                 | 16379               |
| zookeeper          | cp-zookeeper:7.7.0             | 12181               |
| kafka              | cp-kafka:7.7.0                 | 19092               |
| rabbitmq           | rabbitmq:3.13-management       | 15672 (amqp), 15673 (mgmt) |

### Observability Services

| Service         | Image                               | Host Port(s)          |
|-----------------|-------------------------------------|-----------------------|
| prometheus      | prom/prometheus:v2.51.2             | 19090                 |
| grafana         | grafana/grafana:10.4.2              | 13000                 |
| loki            | grafana/loki:2.9.7                  | 13100                 |
| tempo           | grafana/tempo:2.4.1                 | 14317, 3200           |
| otel-collector  | otel-collector-contrib:0.99.0       | 14317 (gRPC), 14318 (HTTP) |
| kibana          | kibana:8.13.4                       | 15601                 |

---

## 🚀 Quick Start

### Prerequisites
- Docker Desktop ≥ 4.28 (or Docker Engine + Compose plugin v2)
- Git
- 16 GB RAM recommended (all services combined)

### Step 1 — Clone

```bash
git clone https://github.com/AhmetNuri/RDMicroServis.git
cd RDMicroServis
```

### Step 2 — Configure environment

```bash
cp .env .env.local   # optional: keep .env as-is for local dev
# Edit .env if you need custom passwords or ports
```

> ⚠️ `.env` is committed for development convenience. **Never commit `.env` to a shared/production repo.**

### Step 3 — Start everything

```bash
# 1. Start infrastructure (databases, brokers, cache)
docker compose -f compose.base.yml up -d

# 2. Wait for infra to be healthy, then start observability
docker compose -f compose.observability.yml up -d

# 3. Start backend services and frontends (after building service images)
docker compose -f compose.apps.yml up -d
```

#### Verify health

```bash
# Check all containers are running
docker compose -f compose.base.yml ps
docker compose -f compose.observability.yml ps
docker compose -f compose.apps.yml ps

# Quick health check on auth service
curl http://localhost:8001/health

# Open Grafana
open http://localhost:13000  # admin / Grafana@Str0ng!Pass
```

#### Stop everything

```bash
docker compose -f compose.apps.yml down
docker compose -f compose.observability.yml down
docker compose -f compose.base.yml down
```

#### Tear down with volumes

```bash
docker compose -f compose.base.yml down -v
docker compose -f compose.observability.yml down -v
```

---

## 🗂️ Repository Structure

```
RDMicroServis/
├── compose.base.yml              # Infrastructure: DBs, Kafka, Redis, RabbitMQ
├── compose.apps.yml              # Application services + frontends
├── compose.observability.yml     # Prometheus, Grafana, Loki, Tempo, OTel, Kibana
├── .env                          # Environment variables (dev defaults)
├── .gitignore
│
├── services/
│   ├── auth-service/             # .NET 9 — JWT auth, user management
│   ├── order-service/            # .NET 9 — Order lifecycle, MSSQL
│   ├── notification-service/     # .NET 9 — Email/push via Kafka + RabbitMQ
│   ├── audit-service/            # .NET 9 — Audit log → Elasticsearch
│   ├── analytics-service/        # .NET 9 — Events → PostgreSQL analytics
│   ├── search-indexer-service/   # .NET 9 — Kafka consumer → Elasticsearch
│   ├── product-service/          # Node.js/TS — Product catalog, MongoDB
│   ├── payment-service/          # Laravel/PHP — Payment processing, PostgreSQL
│   └── inventory-service/        # Node.js/TS — Stock management, MongoDB
│
├── frontend/
│   ├── shop-web/                 # Next.js 14 — Customer storefront
│   ├── landing-web/              # Next.js 14 — Marketing landing page
│   ├── admin-portal/             # Angular 17 — Admin dashboard
│   └── operations-panel/         # Angular 17 — Ops & logistics dashboard
│
└── observability/
    ├── prometheus/
    │   └── prometheus.yml
    ├── grafana/
    │   └── provisioning/
    │       └── datasources/
    │           └── datasources.yml
    ├── loki/
    │   └── loki-config.yml
    ├── tempo/
    │   └── tempo-config.yml
    └── otel-collector/
        └── otel-collector.yml
```

---

## 🔭 Observability

All .NET services export OpenTelemetry traces/metrics/logs to the OTel Collector. Node.js and Laravel services connect via OTLP HTTP/gRPC.

| Tool            | URL                              | Credentials            |
|-----------------|----------------------------------|------------------------|
| Grafana         | http://localhost:13000           | admin / Grafana@Str0ng!Pass |
| Prometheus      | http://localhost:19090           | —                      |
| Kibana          | http://localhost:15601           | —                      |
| RabbitMQ Mgmt   | http://localhost:15673           | rabbituser / Rabbit@Str0ng!Pass |

**Grafana Datasources** (auto-provisioned):
- **Prometheus** — metrics (default)
- **Loki** — logs (with TraceID → Tempo deep links)
- **Tempo** — distributed traces (with service map via Prometheus)

---

## 🗺️ Development Roadmap

### Phase 1 — Platform Foundation ✅ *(current)*
- Docker Compose for all infrastructure, app services, and observability
- Environment configuration and network topology
- Observability stack with auto-provisioned Grafana datasources

### Phase 2 — Auth Service
- ASP.NET Core 8 with JWT + Refresh Tokens
- PostgreSQL via EF Core
- OTLP telemetry, `/health`, `/metrics`

### Phase 3 — Product & Inventory Services
- Node.js/TypeScript with Express
- MongoDB via Mongoose
- Kafka producer (product events → search indexer)

### Phase 4 — Order Service
- ASP.NET Core 8 with MSSQL via EF Core
- Saga orchestration via Kafka
- Kafka events: `order.created`, `order.paid`, `order.shipped`

### Phase 5 — Payment Service
- Laravel 11 with PostgreSQL
- Stripe/mock payment gateway integration
- Kafka consumer for order events

### Phase 6 — Notification Service
- ASP.NET Core 8 consuming Kafka + RabbitMQ
- Email (SMTP) and push notification channels
- MongoDB for notification history

### Phase 7 — Audit & Search Indexer Services
- Audit: structured event log to PostgreSQL + Elasticsearch
- Search Indexer: Kafka consumer indexing products/orders into Elasticsearch

### Phase 8 — Analytics Service
- ASP.NET Core 8 Kafka consumer
- Aggregate events into PostgreSQL analytics schema
- Grafana dashboard for business KPIs

### Phase 9 — Frontend Applications
- shop-web: Next.js 14 App Router + Tailwind CSS
- landing-web: Next.js 14 static marketing site
- admin-portal: Angular 17 with Angular Material
- operations-panel: Angular 17 for logistics and inventory ops

---

## 🛠️ Development Guide

### Building a single service

```bash
# Build and run just the auth service (infra must be up)
docker compose -f compose.apps.yml up auth-service --build
```

### Running a .NET service locally

```bash
cd services/auth-service
dotnet restore
dotnet run
# Connects to localhost ports defined in .env
```

### Running a Node.js service locally

```bash
cd services/product-service
npm install
npm run dev
```

### Running the Laravel service locally

```bash
cd services/payment-service
composer install
cp .env.example .env
php artisan key:generate
php artisan migrate
php artisan serve --port=8008
```

### Viewing logs

```bash
# All services
docker compose -f compose.apps.yml logs -f

# Single service
docker compose -f compose.apps.yml logs -f auth-service

# Infrastructure
docker compose -f compose.base.yml logs -f kafka
```

### Kafka topic management

```bash
# List topics
docker exec kafka kafka-topics --bootstrap-server localhost:9092 --list

# Consume from a topic
docker exec kafka kafka-console-consumer \
  --bootstrap-server localhost:9092 \
  --topic order.created \
  --from-beginning
```

### Redis CLI

```bash
docker exec -it redis redis-cli -a Redis@Str0ng!Pass
```

### MongoDB Shell

```bash
# Connect to product service DB
docker exec -it mongo-product mongosh \
  "mongodb://productuser:Product@Str0ng!Pass@localhost:27017/productdb"
```

---

## 🔒 Security Notes

- All service-to-service traffic runs on the internal `commerce-network` Docker bridge
- Passwords in `.env` are for **development only** — use secrets management (Vault, AWS Secrets Manager, etc.) in production
- JWT secret should be rotated per environment
- Elasticsearch has `xpack.security.enabled=false` for dev — enable for production
- MSSQL SA account should be replaced with a least-privilege account in production

---

## 📄 License

MIT