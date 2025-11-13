# Structurizr Lite - Quick Reference

## Current Status
- **Container**: `structurizr-lite` (ID: 1e450edf71f1)
- **URL**: http://localhost:8080
- **Workspace file**: `Documentation/Architecture/workspace.dsl`

## Available Views

### 1. System Context (Layer 1) ✨ **UPDATED**
**Path**: Diagrams → SystemContext

Shows:
- 3 user types (News Reader, News Publisher, Newsletter Subscriber)
- HappyHeadlines system boundary
- 5 external dependencies with protocols:
  - RabbitMQ (AMQP) - Message routing
  - PostgreSQL (TCP/5432) - Regional databases
  - Redis (TCP/6379) - Distributed caching with Brotli compression
  - Seq (HTTP) - Structured logging
  - Zipkin (HTTP) - Distributed tracing

### 2. Container View (Layer 2)
**Path**: Diagrams → Containers

Shows all microservices and databases:
- PublisherService, ArticleService (×3 regions), CommentService, ProfanityService
- DraftService, SubscriberService, NewsletterService, Monitoring

### 3. Domain-Specific Container Views
- **ArticleDomain**: ArticleService (deployed in 3 regional instances), ArticleDatabase (3 regional databases), Publisher, Monitoring
- **CommentDomain**: CommentService + ProfanityService with circuit breaker
- **NewsletterDomain**: SubscriberService (feature toggle) + NewsletterService

### 4. Component Views (Layer 3)
- **ArticleServiceComponents**: Internal structure shared across all regional instances (compression, caching, API)
- **SubscriberServiceComponents**: Feature toggle middleware
- **CommentServiceComponents**: Circuit breaker pattern with Polly

### 5. Dynamic Views (Sequence-like flows)
- **ArticlePublicationFlow**: Publisher → Regional Queue → ArticleService → Cache → Newsletter
- **CommentProfanityFlow**: Reader → Comment → Profanity Check (circuit breaker) → Cache
- **SubscriptionFlow**: Subscriber → Feature Toggle → SubscriberService → Newsletter


## How to Reload After Changes

### Option 1: Browser Hard Refresh
```bash
# In browser: Ctrl+F5 or Ctrl+Shift+R
```

### Option 2: Restart Container
```bash
bash Scripts/reload-structurizr.sh
```

### Option 3: Manual Restart
```bash
docker restart structurizr-lite
```

## Troubleshooting

### Diagrams not updating?
1. Check file was saved: `ls -la Documentation/Architecture/workspace.dsl`
2. Check container is running: `docker ps | grep structurizr`
3. Check for errors: `docker logs structurizr-lite | grep -i error`
4. Hard refresh browser (Ctrl+F5)
5. Restart container: `docker restart structurizr-lite`

### Can't access http://localhost:8080?
```bash
docker ps | grep structurizr-lite
# Should show: 0.0.0.0:8080->8080/tcp

# If not running:
bash Scripts/reload-structurizr.sh
```

## Architectural Highlights

**Consolidated Regional Services**: 
- ArticleService shown as single entity that deploys in 3 regional instances (Africa, Asia, Europe)
- ArticleDatabase shown as single entity representing 3 regional databases
- Regional behavior configured via environment variables per instance

**Key Patterns Visible**:
- Regional isolation through configuration, not duplication
- Feature toggles (SubscriberService)
- Fault tolerance via circuit breakers (CommentService → ProfanityService)
- Distributed caching with Brotli compression (ArticleService)
- Async messaging patterns (RabbitMQ)

---

*The diagrams await your inspection at http://localhost:8080. The workspace.dsl file is the source of truth; Structurizr Lite renders it into visual comprehension.*

