# Question 8: Design to be Monitored

## Part 1: The Principle - Metrics, Logging, Tracing

### What is "Design to be Monitored"?

Building observability INTO your system from day one - not bolting it on later.

**Key insight:** In a distributed system, you can't debug by stepping through code. You MUST have visibility.

---

### The Three Pillars of Observability

| Pillar | Question Answered | Data Type | Example |
|--------|------------------|-----------|---------|
| **Metrics** | "How much? How many?" | Numbers over time | Cache hit ratio: 73% |
| **Logging** | "What happened? When?" | Text events | "Article 42 saved to DB" |
| **Tracing** | "What's the path? Where's the slowness?" | Request flow across services | Publish → RabbitMQ → Save (250ms) |

---

### Metrics - Numbers

Aggregate numerical data tracked over time.

```csharp
// From CacheMetrics.cs in HappyHeadlines
public async Task RecordHitAsync()
{
    await _redis.StringIncrementAsync($"{_prefix}:hits");
}

public async Task<double> GetHitRatio()
{
    var hits = await GetHitsAsync();
    var misses = await GetMissesAsync();
    return hits / (hits + misses);  // e.g., 0.73 = 73%
}
```

**What metrics tell you:**
- Is the cache working? (hit ratio)
- Is the system overloaded? (request count)
- Are errors increasing? (error rate)

---

### Logging - Events

Discrete events with context.

```csharp
// From HappyHeadlines services
MonitorService.Log.Information(
    "Cache hit for article {Id} (compressed: {Size} bytes)", 
    id, compressedBytes.Length);

MonitorService.Log.Error(ex, 
    "Failed to connect to RabbitMQ after {Attempts} attempts", 
    maxAttempts);
```

**What logs tell you:**
- What happened?
- When did it happen?
- What was the context?
- What went wrong?

**Structured logging** (like Serilog → Seq) makes logs searchable and filterable.

---

### Tracing - Request Flow

Follows a single request across multiple services.

```csharp
// From MonitorService.cs
public static void Initialize(string serviceName)
{
    _activitySource = new ActivitySource(serviceName);
    
    TracerProvider = Sdk.CreateTracerProviderBuilder()
        .AddZipkinExporter(o => o.Endpoint = new Uri("http://zipkin:9411/api/v2/spans"))
        .AddSource(serviceName)
        .Build();
}

// Usage: Start a span
using var activity = MonitorService.ActivitySource.StartActivity("PublishArticle");
// ... operation happens ...
// Timing automatically recorded
```

**What tracing tells you:**
- Which services did this request touch?
- How long did each service take?
- Where is the bottleneck?

---

## Part 2: Why Y-Axis Scaling Breaks Monitoring

### What is Y-Axis Scaling?

**Y-axis = Functional decomposition** = Splitting by service responsibility.

```
Monolith                    Microservices (Y-axis scaled)
┌─────────────┐             ┌──────────────┐
│             │             │ ArticleService│
│   Single    │    →→→      ├──────────────┤
│   Service   │             │ CommentService│
│             │             ├──────────────┤
└─────────────┘             │ PublisherSvc  │
                            └──────────────┘
```

---

### The Monitoring Problem

| Aspect | Monolith | After Y-Axis Scaling |
|--------|----------|---------------------|
| **Logs** | 1 log file | 10 separate log streams |
| **Metrics** | 1 dashboard | 10 dashboards to correlate |
| **Tracing** | Not needed (single process) | CRITICAL (requests cross services) |
| **Debugging** | Step through code | Follow traces across network |

---

### Specific Challenges

#### 1. Requests Cross Service Boundaries

```
User publishes article:
┌─────────────┐     ┌──────────┐     ┌───────────────┐
│ Publisher   │────▶│ RabbitMQ │────▶│ ArticleService│
│ Service     │     │          │     │               │
└─────────────┘     └──────────┘     └───────────────┘
     50ms              20ms               180ms
                                          
     Total: 250ms - WHERE is the slowness?
```

**Without tracing:** You see 250ms but don't know why.  
**With tracing:** You see ArticleService.SaveToDb took 180ms → that's your bottleneck.

#### 2. Logs Are Scattered

```
# Monolith
tail -f /var/log/app.log  # Everything in one place

# Microservices
# Where do I look?
- article-service logs?
- publisher-service logs?
- comment-service logs?
- rabbitmq logs?
```

**Solution:** Centralized logging (Seq, ELK stack)

#### 3. Correlation is Hard

Without a shared trace ID, you can't connect:
- "Article 42 published" (in PublisherService)
- "Article 42 received" (in ArticleService)
- "Article 42 saved" (in ArticleService)

**Solution:** Propagate trace context across services:

```csharp
// From ArticleConsumer.cs - extracting trace context from RabbitMQ message
if (ea.BasicProperties.Headers.TryGetValue("traceparent", out var traceparentObj))
{
    var traceparent = Encoding.UTF8.GetString(traceparentBytes);
    // Parse and continue the trace
    var traceId = ActivityTraceId.CreateFromString(parts[1].AsSpan());
    parentContext = new ActivityContext(traceId, spanId, ActivityTraceFlags.Recorded);
}

using var activity = MonitorService.ActivitySource.StartActivity(
    "ConsumeArticle", 
    ActivityKind.Consumer, 
    parentContext);  // Links to publisher's trace!
```

---

## Part 3: Useful Metrics from Logs and Traces

### The Key Insight

Logs and traces contain RAW data → Extract METRICS from them.

---

### From Logs → Metrics

| Log Event | Extracted Metric |
|-----------|------------------|
| `"Cache hit for article {Id}"` | Cache hit count → hit ratio |
| `"Error: {Exception}"` | Error count → error rate |
| `"Request completed in {Ms}ms"` | Response time distribution |
| `"Retry attempt {N} for RabbitMQ"` | Retry count → connection stability |
| `"Circuit breaker OPEN"` | Circuit breaker state → availability |

**Example: Deriving error rate from logs**

```csharp
// Logs look like this:
MonitorService.Log.Error(ex, "Failed to process comment {Id}", id);

// In Seq, you can query:
// count(where Exception != null) / count(*) = error rate
```

---

### From Traces → Metrics

| Trace Data | Extracted Metric |
|------------|------------------|
| Span duration | Average response time per service |
| Span count per request | Service dependency depth |
| Failed spans | Failure rate per service |
| Time in queue (RabbitMQ) | Message processing latency |

**Example: Identifying bottlenecks**

```
Trace for "Publish Article":
├── PublisherService.Publish     (50ms)
├── RabbitMQ.Queue              (20ms)  
└── ArticleService.Consume       (180ms)
    ├── Decompress               (10ms)
    └── SaveToDatabase           (170ms)  ← BOTTLENECK
```

**Metric derived:** "Database save time" → if > 100ms, alert!

---

### HappyHeadlines: Metrics We Track

```csharp
// Cache metrics (from CacheMetrics.cs)
GET /api/Article/cache
{
    "hitRatio": 0.73,    // 73% of requests served from cache
    "hits": 7300,
    "misses": 2700
}
```

**Why this matters:**
- Hit ratio dropping? Cache might be too small
- Hit ratio at 99%? Maybe cache TTL too long (stale data)
- Misses spiking? New content being published

---

### Useful Metrics Summary

| Category | Metric | Why It Matters |
|----------|--------|----------------|
| **Performance** | Response time (p50, p95, p99) | User experience |
| **Reliability** | Error rate | System health |
| **Efficiency** | Cache hit ratio | Resource utilization |
| **Capacity** | Request rate | Scaling decisions |
| **Resilience** | Circuit breaker trips | Dependency health |
| **Async** | Queue depth | Processing backlog |

---

## 15-Minute Presentation Structure

| Time | Topic | Content |
|------|-------|---------|
| 0-2 | The Principle | "Can't debug distributed systems without observability" |
| 2-5 | Three Pillars | Metrics (numbers), Logging (events), Tracing (flow) |
| 5-8 | Y-Axis Problem | Monolith vs microservices monitoring complexity |
| 8-10 | Demo | Show Zipkin trace / Seq logs from HappyHeadlines |
| 10-13 | Logs → Metrics | How to extract useful numbers from text |
| 13-15 | Practical Metrics | What to monitor in a distributed system |

