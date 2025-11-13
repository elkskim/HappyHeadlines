# Component Diagrams (C4 Layer 3)

## What Are Component Diagrams?

Component diagrams are **Layer 3** of the C4 model. They zoom into a single container (microservice) and show its internal building blocks:

- **Controllers** - REST API endpoints
- **Services** - Business logic
- **Repositories** - Data access
- **Message handlers** - RabbitMQ consumers/publishers
- **Background services** - Hosted services
- **Infrastructure components** - Caching, monitoring, etc.

## Why We Use Them

Component diagrams help developers understand:
1. **Internal architecture** of each microservice
2. **Separation of concerns** (Controller → Service → Repository pattern)
3. **Dependency flow** within the service
4. **Integration points** with external systems (databases, message queues, other services)

## Our Component Diagrams

### 1. PublisherService Components

**Purpose**: Receives articles from publishers and routes them to regional RabbitMQ queues.

**Components**:
- `PublisherController` - REST API endpoint for article submission
- `PublisherMessaging` - Routes articles to regional RabbitMQ exchanges

**Flow**: 
```
Controller → PublisherMessaging → RabbitMQ regional exchanges
```

**Key Pattern**: **Simple routing service** - minimal business logic, just routing

---

### 2. ArticleService Components

**Purpose**: Manages articles across 8 regional databases with caching and event consumption.

**Components**:
- `ArticleController` - REST API for article retrieval
- `ArticleAppService` - Business logic layer
- `ArticleRepository` - Data access with dynamic regional routing
- `CompressionService` - Brotli compression for cache entries
- `ArticleCacheCommander` - Redis cache lifecycle and metrics (Hosted Service)
- `ArticleConsumer` - RabbitMQ message consumer
- `ArticleConsumerHostedService` - Background service managing consumer lifecycle

**Flow (Read)**:
```
Controller → AppService → Repository → ArticleDatabase
                       → CompressionService → CacheCommander → Redis
```

**Flow (Message Consumption)**:
```
RabbitMQ → Consumer → Repository → ArticleDatabase
                   → CacheCommander → Redis
                   → RabbitMQ (newsletter events)
```

**Key Patterns**: 
- **Repository pattern** for data access
- **Background processing** with hosted services
- **Cache-aside pattern** with compression
- **Metrics reporting** to Monitoring service

---

### 3. CommentService Components

**Purpose**: Manages comments with profanity filtering and circuit breaker fault isolation.

**Components**:
- `CommentController` - REST API for comment management
- `CommentCacheCommander` - Redis cache management
- `ResilienceService` - Circuit breaker for ProfanityService (Polly)
- `DbInitializer` - Database migrations and seeding

**Flow**:
```
Controller → CacheCommander → CommentDatabase
          → ResilienceService (Circuit Breaker) → ProfanityService
          → Redis (cache)
```

**Key Patterns**:
- **Circuit breaker pattern** (Polly) for fault isolation
- **Cache-aside pattern**
- **Database initialization** on startup

---

## Exam Relevance

### For Question 1 (C4 Model):
- **Demonstrates Level 3 (Component)** of C4 model
- Shows how we document internal service architecture
- Bridges communication: developers understand component responsibilities

### For Question 3 (Fault Isolation):
- `ResilienceService` component shows **circuit breaker implementation**
- Polly library isolates CommentService from ProfanityService failures

### For Question 4 (Design to be Monitored):
- `ArticleCacheCommander` reports metrics to Monitoring service
- Shows observability components at the component level

### For Question 5 (Green Architecture):
- `CompressionService` reduces network/cache overhead (energy efficiency)
- `ArticleCacheCommander` optimizes cache lifecycle (resource efficiency)

---

## How to View Diagrams

1. Start Structurizr Lite: `bash Scripts/reload-structurizr.sh`
2. Open: http://localhost:8080
3. Select diagram views:
   - **PublisherService-Components**
   - **ArticleService-Components**
   - **CommentService-Components**

---

## Component Naming Conventions

We follow **ASP.NET Core conventions**:
- **Controllers** - Handle HTTP requests (`ArticleController`)
- **Services** - Business logic (`ArticleAppService`, `ResilienceService`)
- **Repositories** - Data access (`ArticleRepository`)
- **Hosted Services** - Background tasks (`ArticleCacheCommander`, `ArticleConsumerHostedService`)
- **Consumers** - Message handlers (`ArticleConsumer`)

This makes our code easy to navigate and understand for new developers.

---

## Technical Note: Component Diagram Scope

In our Structurizr DSL, component-level relationships show **internal interactions** within each service:
- Component → Component (within same container)
- Component relationships to **external containers/systems** are represented at the **Container level** in the other views

This is a Structurizr DSL scoping limitation - components can only directly reference siblings within their parent container. For the complete picture including external dependencies (databases, queues, other services), refer to the **Container views** which show the full integration architecture.

**Example**: `ArticleRepository` → `ArticleDatabase` is shown in Container views, while Component views focus on Controller → Service → Repository flow.

