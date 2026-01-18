# Trace Flow Visualization

## Before Fix ❌

```
PublisherService                    ArticleService
    |                                      |
    | Start Trace A                        | Start Trace B (NEW!)
    | TraceId: abc123                      | TraceId: xyz789
    |                                      |
    v                                      v
[PublishArticle]  ----RabbitMQ---->  [ConsumeArticle]
 SpanId: 1111                         SpanId: 2222
 TraceId: abc123                      TraceId: xyz789
                                      (BROKEN! Different trace!)
```

**Problem:** Two separate traces. Cannot see the connection between publish and consume.

---

## After Fix ✅

```
PublisherService                    ArticleService
    |                                      |
    | Start Trace                          | Continue Same Trace
    | TraceId: abc123                      | TraceId: abc123 (SAME!)
    | SpanId: 1111                         | ParentSpanId: 1111
    |                                      | SpanId: 2222
    v                                      v
[PublishArticle]  --Headers:----->  [ConsumeArticle]
 TraceId: abc123   traceparent       TraceId: abc123
 SpanId: 1111      = abc123:1111     ParentSpan: 1111
                                     SpanId: 2222
                                          |
                                          v
                                     [GetArticle]
                                      TraceId: abc123
                                      ParentSpan: 2222
                                      SpanId: 3333
```

**Result:** ONE continuous trace with parent-child relationships!

---

## Zipkin Trace View

```
Trace: abc123 (Total: 45ms)

├─ PublishArticle                    [5ms]  PublisherService
│  └─ ConsumeArticle                [20ms]  ArticleService
│     └─ ArticleAppService.GetArticle [15ms]  ArticleService
│        Tags:
│        - article.id: 42
│        - article.region: Europe
│        - cache.hit: L2-Redis
│        - cache.compressed_size: 200
│        - compression.ratio: 3.2
```

**Click any span to see:**
- Duration
- Service name
- Tags (metadata)
- Parent/child relationships

---

## The Magic: W3C Trace Context

### Format
```
traceparent: 00-{traceId}-{spanId}-{flags}
Example:     00-abc123def456-111222333-01
             ││  │           │         └─ Flags (sampled)
             ││  │           └─────────── Parent SpanId
             ││  └─────────────────────── TraceId (32 hex chars)
             │└────────────────────────── Version
```

### In Your Code

**Inject (Publisher):**
```csharp
properties.Headers["traceparent"] = 
    $"00-{activity.TraceId}-{activity.SpanId}-01";
```

**Extract (Consumer):**
```csharp
var traceparent = GetHeader("traceparent");
var parts = traceparent.Split('-');
var traceId = ActivityTraceId.CreateFromString(parts[1]);
var parentSpanId = ActivitySpanId.CreateFromString(parts[2]);
var parentContext = new ActivityContext(traceId, parentSpanId, ...);
```

**Continue (Consumer):**
```csharp
using var activity = ActivitySource.StartActivity(
    "ConsumeArticle",
    ActivityKind.Consumer,
    parentContext  // This makes it a CHILD span
);
```

---

## Why This Matters

### Without Proper Tracing
```
User: "Why is publishing slow?"
You: "Uh... let me check logs... maybe RabbitMQ? Database?"
     *searches 8 services manually*
```

### With Proper Tracing
```
User: "Why is publishing slow?"
You: *Opens Zipkin, searches TraceId*
     "ArticleService's Redis is taking 15ms instead of 2ms.
      Cache warming job might be overloading it."
     *Fixed in 5 minutes*
```

**Tracing = X-ray vision for distributed systems.**

---

## Quick Reference

### When Publishing a Message
```csharp
using var activity = ActivitySource.StartActivity("MyOperation");
message.Headers["traceparent"] = 
    $"00-{activity.TraceId}-{activity.SpanId}-01";
await Publish(message);
```

### When Consuming a Message
```csharp
var traceparent = message.Headers["traceparent"];
var parentContext = ParseTraceParent(traceparent);

using var activity = ActivitySource.StartActivity(
    "MyOperation",
    ActivityKind.Consumer,
    parentContext
);
// Process message...
```

### When Adding Internal Spans
```csharp
using var activity = ActivitySource.StartActivity("DatabaseQuery");
activity?.SetTag("query.id", id);
activity?.SetTag("query.region", region);
var result = await _db.GetAsync(id);
activity?.SetTag("query.found", result != null);
```

---

## Common Mistakes

### ❌ Starting New Trace in Consumer
```csharp
using var activity = ActivitySource.StartActivity("Consume");
// This creates NEW trace, not child!
```

### ✅ Continuing Parent Trace
```csharp
var parentContext = ExtractFromHeaders(message);
using var activity = ActivitySource.StartActivity(
    "Consume",
    ActivityKind.Consumer,
    parentContext  // Key!
);
```

### ❌ Forgetting to Inject
```csharp
var message = new { data = "..." };
await Publish(message);
// No headers = Broken trace!
```

### ✅ Always Inject
```csharp
var message = new { data = "..." };
message.Headers["traceparent"] = CurrentTraceContext();
await Publish(message);
```

---

## Summary

**3 Steps to Unbroken Traces:**

1. **Producer:** Inject `traceparent` into message headers
2. **Consumer:** Extract `traceparent` and parse into `ActivityContext`
3. **Consumer:** Start activity with parent context

**That's it. Now your traces flow across services.** ✅

