# Exam Questions Summary

## Question 1: C4 Model (304 lines)

### Part 1: The Four Levels
- Level 1: System Context (big picture, external view)
- Level 2: Container (services, databases, deployable units)
- Level 3: Component (internal structure of one service)
- Level 4: Code (class diagrams, implementation)

### Part 2: Bridging Communication Gaps
- How C4 solves the problem of different stakeholders needing different detail levels
- Progressive disclosure from simple to complex
- Each stakeholder gets appropriate level

### Part 3: C4 in HappyHeadlines
- Context diagram: Readers and Publishers using the system
- Container diagram: All microservices, RabbitMQ, Redis, databases
- Component diagram: ArticleService internal structure
- Code example: ArticleAppService implementation

---

## Question 2: AKF Scale Cube (354 lines)

### Part 1: The Three Dimensions
- **X-axis:** Horizontal duplication (cloning)
- **Y-axis:** Functional decomposition (microservices)
- **Z-axis:** Data partitioning (sharding)

### Part 2: X-Axis Trade-offs & Challenges
- Benefits: Simple, high availability, linear scaling
- Trade-offs: Shared bottlenecks, cost, diminishing returns
- Challenges: Stateless design, cache coherence, connection pools, debugging

### Part 3: ArticleService X-Axis Scaling
- Docker Swarm with 3 replicas
- Stateless design with external Redis and databases
- How requests are handled across instances
- Solving challenges (cache coherence, connection pooling, observability)

---

## Question 3: Fault Isolation (493 lines)

### Part 1: What and Why
- Definition: Preventing cascading failures
- Comparison: With vs without isolation
- Why it's important: Failures inevitable, graceful degradation better than outage

### Part 2: Implementation Techniques
- Circuit Breaker Pattern (open/closed/half-open states)
- Fallback Strategy (fail-closed, fail-open, cached response)
- HTTP Timeouts (prevent hanging)
- Retry with Backoff (handle transient failures)
- Async Messaging (decouple services)

### Part 3: CommentService ↔ ProfanityService
- Complete implementation with Polly policies
- Circuit breaker configuration (3 failures, 30s break)
- Fallback strategy (block comments when service down)
- Four scenarios: Normal, glitch, service down, recovery
- Logging and observability

---

## Question 4: Design to be Monitored (280 lines)

### Part 1: Core Principles
- **Metrics:** Numbers (cache hit ratios, response times)
- **Logging:** Events (what happened, structured logs)
- **Tracing:** Request flows (distributed traces across services)

### Part 2: Y-Axis Scaling Problem
- Microservices make monitoring harder
- Distributed traces essential
- No single dashboard
- Hard to find root causes

### Part 3: PublisherService and ArticleService
- Shared MonitorService initialization
- Tracing: PublishArticle span propagates through RabbitMQ
- Metrics: Cache hit ratio tracking
- Logging: Retry attempts with context
- Tools: Seq for logs, Zipkin for traces

---

## Question 5: Green Architecture Framework (311 lines)

### Part 1: Principles and Goals
- Energy efficiency = Cost efficiency
- Measure everything
- Small changes × scale = big impact
- Closer data = less energy

### Part 2: Relevance to Large Systems
- Scale amplifies everything (1M requests × savings)
- Microservices = more network traffic
- Developer decisions affect millions of requests

### Part 3: HappyHeadlines Implementation
- **Tactic #1:** Multi-tier caching (L1 Memory, L2 Redis, L3 Database)
  - 78% reduction in network energy
- **Tactic #2:** Brotli compression on Redis
  - 67% reduction in Redis traffic
- **Tactic #3:** Cache warming (background service)
  - Higher hit rates
- **Tactic #4:** Regional databases
  - Data locality reduces latency and energy

---

## File Structure

All questions follow the same format:

```
# Question N: [Topic]

## Part 1: [Describe/Define]
...theoretical foundation...

## Part 2: [Explain/Discuss]
...deeper analysis/problems/why it matters...

## Part 3: [Demonstrate]
...actual implementation in HappyHeadlines...
```

## Quick Reference

| Question | Topic | Lines | Key Implementation |
|----------|-------|-------|-------------------|
| 1 | C4 Model | 304 | Container & Component diagrams |
| 2 | AKF Scale Cube | 354 | Docker Swarm 3 replicas |
| 3 | Fault Isolation | 493 | Circuit breaker + fallback |
| 4 | Design to be Monitored | 280 | Seq + Zipkin + metrics |
| 5 | Green Architecture | 311 | Multi-tier caching + compression |

**Total:** 1,742 lines of comprehensive exam preparation material

