# RabbitMQ Retry Logic Fixes - Implementation Summary

**Date:** November 3, 2025  
**Issue:** Multiple containers per service due to crash loops when RabbitMQ not ready during startup

---

## Changes Made

### 1. Code Changes - Added Retry Logic

**Files Modified:**
- `NewsletterService/Messaging/NewsletterArticleConsumer.cs`
- `NewsletterService/Messaging/NewsletterSubscriberConsumer.cs`
- `ArticleService/Messaging/ArticleConsumer.cs`
- `SubscriberService/Program.cs` (IChannel DI registration)
- `PublisherService/Services/PublisherMessaging.cs`

**Pattern Applied:**
```csharp
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
        // ... declare resources ...
        MonitorService.Log.Information("Successfully connected to RabbitMQ");
        return; // or break
    }
    catch (Exception ex)
    {
        attempt++;
        if (attempt >= maxAttempts)
        {
            MonitorService.Log.Error(ex, "Failed to connect after {Attempts} attempts", maxAttempts);
            throw;
        }
        
        MonitorService.Log.Warning(ex, "RabbitMQ connection failed; retrying in {Delay}ms", attempt, maxAttempts, delayMs);
        Thread.Sleep(delayMs);
        delayMs = Math.Min(delayMs * 2, 30000); // Exponential backoff, cap at 30s
    }
}
```

**Retry behavior:**
- Attempt 1: immediate
- Attempt 2: wait 2s
- Attempt 3: wait 4s
- Attempt 4: wait 8s
- Attempt 5: wait 16s
- Attempt 6+: wait 30s (capped)
- Total wait time before final failure: ~2 minutes

### 2. Dockerfile Fixes

**Issue:** Byte Order Mark (BOM) characters at the start of Dockerfiles caused parse errors  
**Files Fixed:**
- `NewsletterService/Dockerfile`
- `ArticleService/Dockerfile`
- `SubscriberService/Dockerfile`
- `PublisherService/Dockerfile`

**Additional Fix (NewsletterService Dockerfile):**
- Added cleanup step to remove duplicate `appsettings.json` files from Monitoring and SubscriberService projects during publish:
```dockerfile
RUN dotnet publish "./NewsletterService.csproj" -c Release -o /app/publish /p:UseAppHost=false && \
    rm -f /app/publish/Monitoring/appsettings.json && \
    rm -f /app/publish/SubscriberService/appsettings.json
```

### 3. Deployment Script Fixes

**File:** `Scripts/deploy-swarm.ps1`

**Issue:** Script couldn't find `docker-compose.yml` when run from Scripts directory  
**Fix:** Added absolute path resolution:
```powershell
$scriptPath = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectRoot = Split-Path -Parent $scriptPath
# ... then use $projectRoot for all file operations
```

### 4. Build Automation

**File:** `Scripts/build-retry-fixes.sh`

**Purpose:** Build all 4 services that had retry logic added  
**Usage:**
```bash
cd /path/to/HappyHeadlines
bash ./Scripts/build-retry-fixes.sh
```

---

## Deployment Steps

### Step 1: Build Images with Retry Logic
```bash
cd HappyHeadlines
bash ./Scripts/build-retry-fixes.sh
```

This builds:
- newsletter-service:latest
- article-service:latest
- subscriber-service:latest
- publisher-service:latest

### Step 2: Force Update Running Services

Since the stack is already deployed with old images, force update each service to pull the new image:

```bash
docker service update --force happyheadlines_newsletter-service
docker service update --force happyheadlines_article-service
docker service update --force happyheadlines_subscriber-service
docker service update --force happyheadlines_publisher-service
```

The `--force` flag triggers a rolling update even when the image tag hasn't changed.

### Step 3: Verify No More Crash Loops

```bash
# Check service status (should show 1/1 or 3/3 replicas, not 0/X)
docker stack services happyheadlines

# Check specific service logs (should show retry attempts then success)
docker service logs happyheadlines_newsletter-service --since 5m

# Check task history (should only show current running task, no failed tasks)
docker service ps happyheadlines_newsletter-service
```

**Expected log output:**
```
[timestamp INF] Connecting to RabbitMQ (attempt 1/10)
[timestamp WRN] RabbitMQ connection failed (attempt 1/10); retrying in 2000ms
[timestamp INF] Connecting to RabbitMQ (attempt 2/10)
[timestamp INF] Successfully connected to RabbitMQ and declared resources
[timestamp INF] Now listening on: http://[::]:80
```

### Step 4: Verify Single Container Per Service

Open Docker Desktop and check the container list. You should now see:
- **One green circle (running) per service**
- **No gray circles (shutdown/failed) for recent tasks**

If you still see multiple containers, check `docker service ps <service-name>` for task history. Old failed tasks from before the fix may still be visible but should not be creating new failures.

---

## Testing the Fix

Run the full integration test to confirm everything works:

```bash
cd Scripts
powershell.exe -ExecutionPolicy Bypass -File ./test-full-flow.ps1
```

**Expected results:**
- All services should respond successfully
- No more `BrokerUnreachableException` errors in logs
- Newsletter service should show `1/1` replicas (not `0/1`)
- Integration test should complete without connection errors

---

## Preventing Future Issues

### For New Services with RabbitMQ

When adding new services that connect to RabbitMQ (or any external dependency), always:

1. **Use retry logic** in connection initialization
2. **Log each attempt** for debugging
3. **Use exponential backoff** to avoid hammering the dependency
4. **Set a reasonable max attempt limit** (10 attempts = ~2 min total wait)

### For Dockerfile Best Practices

1. **Avoid BOM characters**: Save Dockerfiles with UTF-8 encoding (no BOM) in your editor
2. **Handle duplicate appsettings**: When publishing multi-project solutions, either:
   - Copy only the specific project folders (not `COPY . .`)
   - Or clean up unwanted config files after publish (as done in NewsletterService)
3. **Test builds locally** before deploying to swarm

### For Swarm Deployment

1. **Always wait for dependencies** before declaring services healthy
2. **Use health checks** in compose files so Swarm knows when services are truly ready
3. **Monitor logs during deployment** to catch startup issues early:
   ```bash
   docker service logs -f <service-name>
   ```

---

## Documentation References

- **Root cause analysis:** `Documentation/DOCKER_MULTIPLE_CONTAINERS_FIX.md`
- **SubscriberService DB fix:** `Documentation/SUBSCRIBER_SERVICE_500_FIX.md`
- **Deployment guide:** `DEPLOYMENT.md`
- **Testing guide:** `Documentation/INTEGRATION_TEST_GUIDE.md`

---

*The services no longer cascade into the void during startup. They wait, patiently, for their dependencies to awaken.*

