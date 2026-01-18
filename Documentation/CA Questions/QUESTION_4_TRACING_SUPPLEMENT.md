# Distributed Tracing: Quick Exam Answer

## The Core Problem
**Traces break when crossing service boundaries via messaging.**

## The Solution: W3C Trace Context Propagation

### Step 1: Producer Injects Context
**PublisherService** adds trace info to message headers:

```csharp
// Create trace span
using var activity = MonitorService.ActivitySource.StartActivity("PublishArticle");

// Inject into RabbitMQ headers (W3C format)
properties.Headers["traceparent"] = $"00-{activity.TraceId}-{activity.SpanId}-01";
```

### Step 2: Consumer Extracts Context
**ArticleService** reads headers and continues the trace:

```csharp
// Extract from message headers
var traceparent = GetHeader("traceparent"); // "00-traceId-spanId-flags"
var parts = traceparent.Split('-');
var parentContext = new ActivityContext(
    traceId: parts[1],
    spanId: parts[2],
    ActivityTraceFlags.Recorded,
    isRemote: true
);

// Continue trace as child span
using var activity = MonitorService.ActivitySource.StartActivity(
    "ConsumeArticle",
    ActivityKind.Consumer,
    parentContext
);
```

### Result
✅ **ONE continuous trace** from PublisherService → RabbitMQ → ArticleService → Database

---

## Why Centralized Monitoring is Better Than Messaging

### Our Approach: Direct SDK
```
Service → OpenTelemetry SDK → Zipkin
                ↓
              (async batch)
```

### Alternative: Messaging
```
Service → RabbitMQ → MonitoringService → Zipkin
```

### Why We Don't Need Messaging:

| Concern | SDK Already Handles It |
|---------|------------------------|
| "What if Zipkin is down?" | SDK buffers and retries |
| "Too many network calls?" | SDK batches automatically |
| "Blocking?" | SDK is fully async |
| "Scale?" | Tested at Google/Microsoft scale |

**Bottom line:** OpenTelemetry SDK is built for this. Adding RabbitMQ would be **over-engineering**.

---

## What You'll See in Zipkin

**Before fix:**
- 2 disconnected traces ❌
- Can't see full request flow ❌

**After fix:**
- 1 trace with parent-child spans ✅
- Full journey visible ✅
- Cache hit/miss tagged ✅
- Compression metrics ✅

---

## Exam Answer Template

> **Q: How did you implement distributed tracing?**
>
> "We use OpenTelemetry with W3C Trace Context propagation. When PublisherService sends a message to RabbitMQ, it injects the current trace context into message headers using the traceparent format. ArticleService extracts this context and continues the trace as a child span. This ensures traces aren't broken when crossing service boundaries.
>
> We use a centralized MonitorService class that configures OpenTelemetry for all services, exporting to Zipkin for visualization and Seq for logs. This is the industry-standard approach - the SDK handles batching, retries, and async transmission internally, so we don't need additional messaging infrastructure for monitoring data.
>
> In Zipkin, you can see the complete request flow from article publication through persistence, with timing breakdowns and tags showing cache hits, compression ratios, and database queries."

---

## Key Takeaways

1. **Trace propagation** = Inject context in producer, extract in consumer
2. **W3C standard** = `traceparent: "00-{traceId}-{spanId}-{flags}"`
3. **Centralized config** = MonitorService (simple, reliable)
4. **No messaging needed** = OpenTelemetry SDK handles everything
5. **Tags matter** = Add context (cache.hit, article.id, etc.)

**This is production-grade distributed tracing. You're good to go! ✅**

