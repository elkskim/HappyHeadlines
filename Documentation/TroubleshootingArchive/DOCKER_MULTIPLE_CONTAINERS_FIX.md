# Docker Multiple Containers Issue - Root Cause and Fix

**Date:** November 3, 2025  
**Issue:** Multiple containers per service visible in Docker Desktop (green + gray/shutdown containers)

> **Troubleshooting Chronicle:** GitHub Copilot and human developer excavated this horror together, November 3, 2025. Containers spawned endlessly, each crash birthing another attempt. Gray circles in Docker Desktop marked the fallen; green circles the momentarily living. We descended into logs seeking answers: "Connection refused," the litany repeated. RabbitMQ timing betrayed us. Polly retry logic became our salvation—exponential backoff, the mathematics of persistence. The containers no longer multiply like the unquiet dead.

---

## Root Cause

When you deploy with Docker Swarm (`docker stack deploy`), services that crash during startup cause Docker to automatically restart them by creating new tasks (containers). Failed tasks remain visible in Docker Desktop UI as "Shutdown" containers with gray circles, while the current running task shows a green circle.

### Why Services Were Crashing

**RabbitMQ Connection Race Condition:** Multiple services (NewsletterService, ArticleService, PublisherService, SubscriberService) were attempting to connect to RabbitMQ synchronously during startup, before RabbitMQ was fully ready.

**Evidence from logs:**
```
happyheadlines_newsletter-service.1.r6y0siv683ov@docker-desktop     
Unhandled exception. RabbitMQ.Client.Exceptions.BrokerUnreachableException: 
None of the specified endpoints were reachable
 ---> System.AggregateException: One or more errors occurred. 
(Connection failed, host 10.0.1.15:5672)
 ---> RabbitMQ.Client.Exceptions.ConnectFailureException: Connection failed, host 10.0.1.15:5672
 ---> System.Net.Sockets.SocketException (111): Connection refused
```

**Pattern:**
- Task 1 crashes: `Name or service not known` (RabbitMQ hostname not resolved yet)
- Task 2 crashes: `Connection refused` (RabbitMQ container exists but not accepting connections)
- Task 3 crashes: `Connection refused`
- Task 4 crashes: `Connection refused`
- Task 5 succeeds: RabbitMQ is finally ready

Each crash creates a new container/task. Docker Desktop shows all of them.

---

## The Problem Pattern

All affected services had this anti-pattern:

```csharp
public SomeConsumer()
{
    var factory = new ConnectionFactory { HostName = "rabbitmq" };
    _connection = factory.CreateConnectionAsync().GetAwaiter().GetResult(); // BLOCKS, CRASHES IF RABBITMQ NOT READY
    _channel = _connection.CreateChannelAsync().GetAwaiter().GetResult();
    // ... declare exchanges/queues
}
```

When RabbitMQ is not ready, the synchronous `.GetAwaiter().GetResult()` throws an unhandled exception that crashes the entire service process (exit code 139 = segmentation fault). Docker Swarm sees the crash and restarts the service, creating a new container.

---

## The Fix Applied

Added **retry logic with exponential backoff** to all RabbitMQ connection points. Services now retry up to 10 times with delays of 2s, 4s, 8s, 16s, 30s (capped), instead of crashing immediately.

### Files Modified

1. **NewsletterService/Messaging/NewsletterArticleConsumer.cs** - Added retry loop in constructor
2. **NewsletterService/Messaging/NewsletterSubscriberConsumer.cs** - Added retry loop in constructor
3. **ArticleService/Messaging/ArticleConsumer.cs** - Added retry loop in Consume() method
4. **SubscriberService/Program.cs** - Added retry loop in IChannel DI registration
5. **PublisherService/Services/PublisherMessaging.cs** - Added retry loop in CreateAsync()

### Example Fix (NewsletterArticleConsumer)

**Before:**
```csharp
public NewsletterArticleConsumer()
{
    var factory = new ConnectionFactory { HostName = "rabbitmq" };
    _connection = factory.CreateConnectionAsync().GetAwaiter().GetResult();
    _channel = _connection.CreateChannelAsync().GetAwaiter().GetResult();
    // ... declare resources
}
```

**After:**
```csharp
public NewsletterArticleConsumer()
{
    var factory = new ConnectionFactory { HostName = "rabbitmq" };
    
    int attempt = 0;
    int maxAttempts = 10;
    int delayMs = 2000;
    
    while (attempt < maxAttempts)
    {
        try
        {
            MonitorService.Log.Information("Connecting to RabbitMQ (attempt {Attempt}/{Max})", attempt + 1, maxAttempts);
            _connection = factory.CreateConnectionAsync().GetAwaiter().GetResult();
            _channel = _connection.CreateChannelAsync().GetAwaiter().GetResult();
            // ... declare resources
            MonitorService.Log.Information("Successfully connected to RabbitMQ and declared resources");
            return;
        }
        catch (Exception ex)
        {
            attempt++;
            if (attempt >= maxAttempts)
            {
                MonitorService.Log.Error(ex, "Failed to connect to RabbitMQ after {Attempts} attempts; the service will now fail", maxAttempts);
                throw;
            }
            
            MonitorService.Log.Warning(ex, "RabbitMQ connection failed (attempt {Attempt}/{Max}); retrying in {Delay}ms", attempt, maxAttempts, delayMs);
            Thread.Sleep(delayMs);
            delayMs = Math.Min(delayMs * 2, 30000); // Exponential backoff, cap at 30s
        }
    }
}
```

---

## How It Works Now

1. Service starts
2. Attempts to connect to RabbitMQ
3. If RabbitMQ is not ready:
   - Log warning
   - Wait 2 seconds
   - Retry
4. If still not ready:
   - Log warning
   - Wait 4 seconds (doubled)
   - Retry
5. Continues up to 10 attempts with exponential backoff (max 30s wait)
6. If all retries fail, service crashes with detailed error log
7. If any retry succeeds, service continues normally

**Benefit:** Services tolerate RabbitMQ startup delays of up to ~2 minutes without crashing.

---

## Verifying the Fix

### Step 1: Rebuild and Redeploy

```bash
# Rebuild affected images
docker build -t newsletter-service:latest -f NewsletterService/Dockerfile .
docker build -t article-service:latest -f ArticleService/Dockerfile .
docker build -t subscriber-service:latest -f SubscriberService/Dockerfile .
docker build -t publisher-service:latest -f PublisherService/Dockerfile .

# Redeploy stack
docker stack rm happyheadlines
# Wait 10 seconds
docker stack deploy -c docker-compose.yml -c docker-compose.swarm.yml happyheadlines
```

### Step 2: Watch Service Logs

```bash
docker service logs -f happyheadlines_newsletter-service
```

**Expected output:**
```
[10:15:23 INF] Connecting to RabbitMQ (attempt 1/10)
[10:15:25 WRN] RabbitMQ connection failed (attempt 1/10); retrying in 2000ms
[10:15:27 INF] Connecting to RabbitMQ (attempt 2/10)
[10:15:27 INF] Successfully connected to RabbitMQ and declared resources
[10:15:27 INF] Now listening on: http://[::]:80
```

### Step 3: Check Service Status

```bash
docker stack services happyheadlines
```

**Expected:** All services show `1/1` replicas (not `0/1` or crashing).

### Step 4: Inspect Tasks

```bash
docker service ps happyheadlines_newsletter-service
```

**Expected:** Only ONE task with `Running` state, no `Failed` or `Shutdown` tasks.

**Before fix:**
```
ID             NAME                                      STATE      ERROR
r5krcnftjclq   happyheadlines_newsletter-service.1       Running    
r6y0siv683ov    \_ happyheadlines_newsletter-service.1   Shutdown   "task: non-zero exit (139)"
n3trh5zjkisr    \_ happyheadlines_newsletter-service.1   Shutdown   "task: non-zero exit (139)"
3rgf4zcehw9m    \_ happyheadlines_newsletter-service.1   Shutdown   "task: non-zero exit (139)"
o3z0dr4y7nsr    \_ happyheadlines_newsletter-service.1   Shutdown   "task: non-zero exit (139)"
```

**After fix:**
```
ID             NAME                                      STATE      ERROR
abc123def456   happyheadlines_newsletter-service.1       Running    
```

---

## Why Docker Desktop Shows Multiple Containers

Docker Swarm keeps task history for debugging purposes. When a task fails and is restarted, the old task remains visible in the UI as "Shutdown" (gray circle). Only the current task is actually running (green circle).

**Container naming pattern:**
- `happyheadlines_newsletter-service.1.r5krcnftjclq` - Current running task
- `happyheadlines_newsletter-service.1.r6y0siv683ov` - Failed task (history)
- `happyheadlines_newsletter-service.1.n3trh5zjkisr` - Failed task (history)

The `.1` indicates replica number (always 1 for replicated services with `replicas: 1`). The hash suffix identifies the specific task instance.

---

## Understanding Swarm Replica Counts

```bash
docker stack services happyheadlines
```

**REPLICAS column meaning:**
- `1/1` - 1 running, 1 desired (healthy)
- `0/1` - 0 running, 1 desired (starting or crashed)
- `0/3` - 0 running, 3 desired (all replicas crashed/starting)
- `3/3` - 3 running, 3 desired (healthy, multiple replicas)

Your ArticleService showed `0/3` because all 3 replicas were crash-looping (attempting to start, crashing, restarting).

---

## Prevention for Future Services

When adding new services that use RabbitMQ (or any external dependency), always use retry logic:

```csharp
// DON'T DO THIS:
_connection = factory.CreateConnectionAsync().GetAwaiter().GetResult();

// DO THIS:
int attempt = 0;
while (attempt < 10) {
    try {
        _connection = factory.CreateConnectionAsync().GetAwaiter().GetResult();
        break;
    } catch {
        Thread.Sleep(2000 * (attempt + 1));
        attempt++;
    }
}
```

Or better yet, use dependency injection with lazy/deferred initialization and health checks.

---

## Additional Recommendations

1. **Add health checks to services** so Swarm knows when they are truly ready
2. **Use depends_on with condition: service_healthy** in Compose files (requires healthchecks)
3. **Consider async factory pattern** instead of blocking in constructors
4. **Add readiness probes** for Kubernetes deployments
5. **Monitor Seq logs** during deployment to catch startup issues early

---

*The containers will no longer multiply like the sorrows of the damned. The services will wait, patiently, for their broker to awaken.*

