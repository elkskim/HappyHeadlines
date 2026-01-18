# Question 10: Availability

---

## Part 1: Forklar begrebet availability og hvordan det relaterer sig til skaleringsprincipper og skaleringskuben

### What is Availability?

**Definition:** The percentage of time a system is operational and accessible.

```
Availability = Uptime / (Uptime + Downtime) × 100%
```

**The "Nines" Language:**

| Availability | Downtime/Year | Name |
|--------------|---------------|------|
| 99% | 3.65 days | Two nines |
| 99.9% | 8.76 hours | Three nines |
| 99.99% | 52.6 minutes | Four nines |
| 99.999% | 5.26 minutes | Five nines |

**Goal:** Maximize uptime, minimize downtime from failures, updates, and traffic spikes.

---

### How Availability Relates to Scaling Principles

**Core Problem:** A single point of failure guarantees eventual downtime.

**Scaling addresses availability through redundancy:**

1. **No single points of failure** → If one thing breaks, another takes over
2. **Capacity headroom** → System doesn't fail under high load
3. **Graceful degradation** → Partial service beats total outage

---

### How Availability Relates to the Scale Cube

**X-Axis (Cloning) → Redundancy:**
```
┌─────────────────────────────────────────────┐
│           Load Balancer (VIP)               │
│                    |                        │
│    ┌───────────────┼───────────────┐        │
│    ↓               ↓               ↓        │
│ Instance 1    Instance 2    Instance 3      │
│    ✅             ❌             ✅         │
│                (crashed)                    │
│                                             │
│ Result: System still available (66%)        │
└─────────────────────────────────────────────┘
```

- 3 replicas = if 1 fails, 2 still serve traffic
- Load balancer routes around dead instances
- **Availability impact:** Eliminates single-point-of-failure

**Y-Axis (Microservices) → Blast Radius Containment:**
```
┌─────────────────────────────────────────────┐
│ CommentService ❌    ArticleService ✅      │
│                                             │
│ Result: Can't comment, BUT can read articles│
│         System partially available          │
└─────────────────────────────────────────────┘
```

- Services fail independently
- One service down ≠ entire system down
- **Availability impact:** Degraded mode possible

**Z-Axis (Sharding) → Regional Isolation:**
```
┌─────────────────────────────────────────────┐
│ Europe DB ❌    Asia DB ✅    Africa DB ✅  │
│                                             │
│ Result: Europe users affected, others fine  │
│         83% of users unaffected             │
└─────────────────────────────────────────────┘
```

- Data partitioned by region
- One region's failure is isolated
- **Availability impact:** Limits blast radius geographically

---

## Part 2: Vis eksempler på mekanismer til forbedring af availability i et system der presses af stigende traffik

### Mechanism 1: Horizontal Scaling with Docker Swarm

**What it does:** Adds more instances to handle increased load

**HappyHeadlines Example:**

```yaml
# docker-compose.swarm.yml
services:
  article-service:
    deploy:
      replicas: 3            # ← 3 instances handle traffic
      endpoint_mode: vip     # ← Load balancer distributes requests
      restart_policy:
        condition: on-failure # ← Auto-restart crashed containers
      update_config:
        parallelism: 1       # ← Rolling update (zero downtime)
        delay: 10s
```

**How it improves availability under traffic pressure:**
- 3 instances → 3× capacity before bottleneck
- Traffic spike? Add more replicas (scale out)
- One instance crashes? Other 2 continue serving
- Rolling updates → No downtime during deployments

---

### Mechanism 2: Circuit Breaker Pattern

**What it does:** Prevents cascading failures when downstream services fail

**HappyHeadlines Example:**

```csharp
// CommentService/Services/ResilienceService.cs
var circuitbreak = Policy
    .Handle<HttpRequestException>()
    .Or<BrokenCircuitException>()
    .CircuitBreakerAsync(
        3,                           // Break after 3 failures
        TimeSpan.FromSeconds(30),    // Stay open 30s
        onBreak: (ex, delay) => 
            MonitorService.Log.Error(ex, "Circuit OPENED - ProfanityService failing"),
        onReset: () => 
            MonitorService.Log.Information("Circuit CLOSED - Recovered")
    );

var fallback = Policy<ProfanityCheckResult>
    .Handle<HttpRequestException>()
    .Or<BrokenCircuitException>()
    .FallbackAsync(
        new ProfanityCheckResult(true, true)  // Fail closed
    );

_policy = fallback.WrapAsync(circuitbreak);
```

**How it improves availability under traffic pressure:**

| Without Circuit Breaker | With Circuit Breaker |
|-------------------------|----------------------|
| ProfanityService slow → All threads wait | Fast-fail after 3 failures |
| Thread pool exhausted | Threads freed immediately |
| CommentService dies | CommentService stays up |
| Cascade to ArticleService | Blast radius contained |

---

### Mechanism 3: Retry with Exponential Backoff

**What it does:** Automatically retries transient failures without overwhelming services

**HappyHeadlines Example:**

```csharp
// CommentService/Program.cs
static IAsyncPolicy<HttpResponseMessage> GetHttpRetryPolicy()
{
    return HttpPolicyExtensions
        .HandleTransientHttpError()
        .WaitAndRetryAsync(
            retryCount: 5,
            sleepDurationProvider: retryAttempt => 
                TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),  // 2s, 4s, 8s, 16s, 32s
            onRetry: (outcome, timespan, retryAttempt, context) =>
            {
                Console.WriteLine($"HTTP retry {retryAttempt} after {timespan.TotalSeconds}s");
            });
}
```

**How it improves availability under traffic pressure:**
- Transient network glitch? Retry succeeds
- Service restarting? Wait and retry works
- Exponential backoff → Doesn't hammer struggling service
- Combined with circuit breaker → Stops retrying on persistent failure

---

### Mechanism 4: Async Messaging (RabbitMQ)

**What it does:** Decouples producers from consumers, enabling temporal isolation

**HappyHeadlines Example:**

```csharp
// PublisherService publishes to RabbitMQ
await channel.BasicPublishAsync(
    exchange: "articles.exchange",
    routingKey: region,
    body: messageBytes
);
// Returns immediately - doesn't wait for ArticleService
```

**How it improves availability under traffic pressure:**

| Synchronous HTTP | Async Messaging |
|------------------|-----------------|
| ArticleService slow → PublisherService blocks | Returns immediately |
| ArticleService down → PublisherService fails | Message queued for later |
| Traffic spike → Both services overloaded | Messages buffered in queue |
| Lost request if crash | Message persisted, delivered later |

---

### Mechanism 5: Database Connection Resilience

**What it does:** Handles transient database failures automatically

**HappyHeadlines Example:**

```csharp
// CommentService/Program.cs
builder.Services.AddDbContext<CommentDbContext>(options =>
{
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("Comment"),
        sql =>
        {
            sql.EnableRetryOnFailure(
                maxRetryCount: 5,
                maxRetryDelay: TimeSpan.FromSeconds(10),
                errorNumbersToAdd: null);
        });
});
```

**How it improves availability under traffic pressure:**
- Database briefly overloaded? Retry succeeds
- Connection pool temporarily exhausted? Waits and retries
- Network blip? Recovers automatically
- No manual intervention needed

---

### Mechanism 6: Caching with Redis

**What it does:** Reduces database load and provides faster responses

**HappyHeadlines Example:**

```csharp
// CommentService uses Redis caching
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("Redis") ?? "redis:6379";
});
```

**How it improves availability under traffic pressure:**
- Read requests served from cache → Database not hit
- 1000 requests for same data → 1 DB query, 999 cache hits
- Database down briefly → Cached data still served
- Response time: 500ms → 5ms

---

## Part 3: Diskuter hvordan visse arkitektoniske valg kan have en negativ indvirkning på systemets availability

### Anti-Pattern 1: Synchronous Call Chains

**The Problem:**
```
User → Gateway → ArticleService → CommentService → ProfanityService → Database
                        ↓
        Each hop is a point of failure
        Latency adds up: 50ms + 50ms + 50ms + 50ms = 200ms minimum
        Any one fails → Entire request fails
```

**Availability Impact:**
- 4 services, each 99.9% available
- Combined availability: 0.999⁴ = **99.6%**
- More services → Lower combined availability

**HappyHeadlines Mitigation:**
- Async messaging where possible (PublisherService → ArticleService)
- Circuit breakers prevent cascade (CommentService → ProfanityService)
- Fallback strategies maintain partial function

---

### Anti-Pattern 2: Shared Database

**The Problem:**
```
┌─────────────────────────────────────────────┐
│ ArticleService ─┐                           │
│                 ├──→ Single Database ← SPOF │
│ CommentService ─┘                           │
└─────────────────────────────────────────────┘
```

**Availability Impact:**
- Database down → All services down
- Database migration → All services affected
- Database overloaded → All services slow

**HappyHeadlines Approach:**
- Each service has own database (Y-axis separation)
- Regional databases (Z-axis partitioning)
- Redis cache reduces database pressure

---

### Anti-Pattern 3: Tight Coupling Without Fallbacks

**The Problem:**
```csharp
// BAD: No fallback, no timeout, no circuit breaker
var response = await _httpClient.GetAsync("http://profanity-service/api/check");
var result = await response.Content.ReadFromJsonAsync<CheckResult>();
// If ProfanityService is down, this hangs or throws, blocking everything
```

**Availability Impact:**
- Downstream service slow → Thread blocked
- Thread pool exhausted → Service stops responding
- No graceful degradation

**HappyHeadlines Solution:**
```csharp
// GOOD: Circuit breaker + fallback
_policy = fallback.WrapAsync(circuitBreaker);
return await _policy.ExecuteAsync(async () => { ... });
```

---

### Anti-Pattern 4: No Redundancy (Single Instance)

**The Problem:**
```yaml
# BAD: Only one instance
services:
  article-service:
    # No replica configuration - single point of failure
```

**Availability Impact:**
- Instance crashes → Complete outage
- Deployment → Downtime
- High traffic → Overwhelmed, slow or unresponsive

**HappyHeadlines Solution:**
```yaml
# GOOD: Multiple replicas with health management
services:
  article-service:
    deploy:
      replicas: 3
      restart_policy:
        condition: on-failure
```

---

### Anti-Pattern 5: Blocking Startup on Dependencies

**The Problem:**
```csharp
// BAD: Synchronous wait for database
public static void Main()
{
    Thread.Sleep(30000);  // Wait 30s for database
    context.Database.Migrate();  // Blocks until done
    // Docker thinks container is unhealthy, restarts it
}
```

**Availability Impact:**
- Slow dependency startup → Container restart loops
- Docker health check fails → Never becomes healthy
- Cascading restart storms

**HappyHeadlines Solution:**
```csharp
// GOOD: Async initialization with retry policy (Polly)
await Policy
    .Handle<Exception>()
    .WaitAndRetryAsync(5, attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)))
    .ExecuteAsync(async () => await context.Database.MigrateAsync());
```

---

### Summary: Availability Trade-offs

| Decision | Improves Availability | Costs |
|----------|----------------------|-------|
| More replicas | ✅ Redundancy | 💰 More resources |
| Circuit breakers | ✅ Prevents cascades | 🔧 Complexity |
| Async messaging | ✅ Temporal isolation | ⏱️ Eventual consistency |
| Microservices | ✅ Independent failures | 🔧 Operational overhead |
| Regional sharding | ✅ Geographic isolation | 💰 More databases |
| Caching | ✅ Reduces load | 🔄 Cache invalidation |

**The Core Truth:** Availability requires intentional architectural decisions. Default choices (single instance, synchronous calls, shared database) will always lead to lower availability. You must design for failure.

