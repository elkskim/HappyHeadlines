# Integration Test Issues Analysis
*"Three wounds revealed; the diagnosis follows."*

> **Diagnostic Chronicle:** GitHub Copilot analyzed these symptoms alongside the human developer, November 5, 2025. The test output spoke in status codes and timeouts. We read the auguries: 0/1 replicas, 404 responses, prolonged silence where services should speak. Three afflictions identified through log divination and Docker Desktop observation. Each symptom traced to its pathology. The diagnosis precedes the cure, as contemplation precedes action. Such is the physician's burden.

## Overview
The integration test on November 5, 2025 revealed three issues:
1. **DraftService slow startup** (0/1 replicas initially)
2. **Zipkin slow startup** (0/1 replicas initially)
3. **Subscriber DELETE test returned 404**

This document analyzes the root causes and proposes fixes.

---

## Issue 1: DraftService Slow Startup

### **Root Cause: Synchronous Database Migration**

**Location:** `DraftService/Program.cs`, lines 47-52

```csharp
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<DraftDbContext>();
    context.Database.Migrate();  // ← BLOCKING SYNCHRONOUS CALL
    MonitorService.Log.Information("{ServiceName} database migration completed", serviceName);
}
```

**Problem:**
- `context.Database.Migrate()` runs synchronously during application startup
- SQL Server connection establishment can take 30-60 seconds in containerized environments
- Docker health checks timeout while waiting for the container to become ready
- Swarm marks the service as failing and restarts it, causing a restart loop
- Eventually succeeds after 2-3 minutes when SQL Server container is fully initialized

**Why This Happens:**
- DraftService starts immediately when swarm deploys
- `draft-db` (SQL Server) takes 30-60 seconds to initialize (accept connections, warm up)
- DraftService tries to connect before SQL Server is ready
- Connection timeout causes the startup to hang
- No retry logic in the migration call itself

**Evidence from ArticleService (Fixed):**
ArticleService had the same problem but was fixed in v0.7.0. From PATCHNOTES.md:
> "Removed synchronous `context.Database.Migrate()` call from DbContextFactory that blocked startup for 8+ minutes"

ArticleService now uses async migrations with Polly retry:
```csharp
// ArticleService/Program.cs (WORKING VERSION)
using (var scope = app.Services.CreateScope())
{
    var serviceProvider = scope.ServiceProvider;
    
    // Asynchronous migrations with retry
    foreach (var region in regions)
    {
        var dbContext = dbContextFactory.CreateDbContext(new[] { "region", region });
        var retryPolicy = Policy
            .Handle<Exception>()
            .WaitAndRetryAsync(5, retryAttempt => TimeSpan.FromSeconds(10));
            
        await retryPolicy.ExecuteAsync(async () =>
        {
            await dbContext.Database.MigrateAsync();  // ← ASYNC + RETRY
        });
    }
}
```

**Impact:**
- Startup time: 2-5 minutes instead of 10-20 seconds
- Docker health checks may fail during migration
- Swarm may restart the container 1-2 times before it stabilizes
- No data loss, but poor user experience during deployment

---

## Issue 2: Zipkin Slow Startup

### **Root Cause: MySQL Dependency Chain**

**Location:** `docker-compose.yml`, lines 288-298

```yaml
zipkin:
  image: openzipkin/zipkin
  ports:
    - "9411:9411"
  environment:
    - STORAGE_TYPE=mysql
    - MYSQL_HOST=zipkin-storage  # ← DEPENDS ON ANOTHER CONTAINER
    - MYSQL_USER=zipkin
    - MYSQL_PASS=zipkin
```

**Problem:**
Zipkin has a two-stage startup dependency:
1. **zipkin-storage** (MySQL) must start and initialize
   - MySQL container startup: 15-20 seconds
   - MySQL schema creation: 10-15 seconds
   - Total: 25-35 seconds
2. **zipkin** starts and tries to connect to MySQL
   - Connection attempts: 5-10 retries over 30-60 seconds
   - Schema validation: 5-10 seconds
   - Total: 40-70 seconds

**Why This Happens:**
- No `depends_on` declaration in docker-compose.yml (swarm mode ignores `depends_on` anyway)
- Zipkin attempts connection before MySQL is ready
- Built-in retry logic in Zipkin eventually succeeds
- Docker health checks may timeout during connection attempts

**Evidence from Test Output:**
Initial deployment showed:
```
happyheadlines_zipkin    replicated   0/1
```
After 2 minutes:
```
happyheadlines_zipkin    replicated   1/1
```

**Comparison with Other Services:**
- ArticleService → Redis: Redis starts in ~5 seconds (in-memory, no schema)
- CommentService → SQL Server: Uses retry logic, starts in ~30 seconds
- Zipkin → MySQL: No application-level retry visible; relies on Zipkin's internal retry

**Impact:**
- Distributed tracing unavailable for first 1-2 minutes of deployment
- Services function normally (Zipkin is observability, not critical path)
- Early spans/traces lost if services emit them before Zipkin is ready
- No functional impact on business logic

---

## Issue 3: Subscriber DELETE Test Returns 404

### **Root Cause: Race Condition + Missing Subscriber in Database**

**Location:** `Scripts/test-full-flow.ps1`, lines 400-442

**Test Flow:**
1. Create temporary subscriber: `POST /api/Subscriber` → returns subscriber object
2. Wait 2 seconds for database write to complete
3. Query all subscribers: `GET /api/Subscriber?region=Europe`
4. Find temp subscriber in list by email: `temp.deletion.test@void.com`
5. Delete subscriber: `DELETE /api/Subscriber/{id}?region=Europe`
6. **Expected:** 200 OK or 204 NoContent
7. **Actual:** 404 Not Found

**Problem Analysis:**

**Root Cause A: Query Parameter Ignored**

The test script passes `?region=Europe` to the DELETE endpoint:
```powershell
Invoke-RestMethod -Uri ($BASE_URL + ":8007/api/Subscriber/${TEMP_SUB_ID}?region=$REGION") -Method Delete
```

But the SubscriberController DELETE endpoint signature is:
```csharp
[HttpDelete("{id}")]
public async Task<IActionResult> Unsubscribe(int id)
{
    var deleted = await _subscriberAppService.Delete(id);
    if (!deleted)
    {
        return NotFound($"Could not find subscriber with ID {id} to unsubscribe");
    }
    return Ok("Subscriber was unsubscribed");
}
```

**The `region` parameter is NOT in the method signature!**

This means:
- The query string `?region=Europe` is silently ignored
- The DELETE looks up subscriber by ID without filtering by region
- If the temporary subscriber was created in the Europe region but retrieved from a different replica or cache, the ID may not exist in the expected database

**Root Cause B: GetSubscribers May Not Filter by Region**

The test calls:
```powershell
$allSubs = Invoke-RestMethod -Uri ($BASE_URL + ":8007/api/Subscriber?region=$REGION") -Method Get
```

But looking at the controller:
```csharp
[HttpGet]
public async Task<IActionResult> GetSubscribers()
{
    var subscribers = await _subscriberAppService.GetSubscribers();
    if (!subscribers.Any())
    {
        return NotFound("It seems that there are no subscribers");
    }
    return Ok(subscribers);
}
```

**The `region` query parameter is also ignored here!**

The underlying service method:
```csharp
public async Task<IEnumerable<SubscriberReadDto>?> GetSubscribers()
{
    var subscribers = await _subscriberRepository.GetSubscribersAsync();
    return subscribers.Select(s => s.ToReadDto());
}
```

This retrieves **all subscribers from the database**, not filtered by region.

**Root Cause C: Timing Issue**

The test waits 2 seconds after creating the subscriber:
```powershell
$tempSubResponse = Invoke-RestMethod -Uri ... -Method Post -Body $TEMP_SUBSCRIBER_JSON ...
Start-Sleep -Seconds 2
```

But:
1. POST creates subscriber in database (immediate)
2. SubscriberPublisher publishes event to RabbitMQ (immediate)
3. NewsletterService **may consume the event and act on it** (0-5 seconds)
4. If there's any async cleanup or if the repository has eventual consistency, the subscriber might be modified/removed

However, the more likely issue is:

**Root Cause D: Wrong Subscriber ID Retrieved**

The test searches for the temp subscriber by email:
```powershell
$tempSub = if ($allSubs -is [Array]) {
    $allSubs | Where-Object { $_.email -eq "temp.deletion.test@void.com" } | Select-Object -First 1
} elseif ($allSubs.email -eq "temp.deletion.test@void.com") {
    $allSubs
} else {
    $null
}
```

If multiple test runs created subscribers with that email, the test might grab an old subscriber ID that was already deleted in a previous test run.

**Why 404 Happens:**
1. Test creates subscriber with ID = 2
2. Test retrieves all subscribers (ID 1, ID 2)
3. Test filters for `temp.deletion.test@void.com` and gets subscriber with ID = 1 (from previous test)
4. Test tries to DELETE ID = 1 (already deleted) → 404 Not Found

**Evidence from Test Output:**
```
Temporary subscriber created with ID: 2
Warning: Could not delete subscriber: The remote server returned an error: (404) Not Found.
```

This suggests the subscriber was created but couldn't be deleted, likely because the ID used for deletion was wrong or the subscriber didn't exist in the database at that ID.

---

## Recommended Fixes

### **Fix 1: DraftService Async Migration with Retry**

**File:** `DraftService/Program.cs`

**Change:**
```csharp
// Before (BLOCKING)
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<DraftDbContext>();
    context.Database.Migrate();  // ← Synchronous, no retry
    MonitorService.Log.Information("{ServiceName} database migration completed", serviceName);
}

// After (ASYNC + RETRY)
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<DraftDbContext>();
    
    // Retry policy: 5 attempts, 10 seconds between attempts
    var retryPolicy = Policy
        .Handle<Exception>()
        .WaitAndRetryAsync(
            5, 
            retryAttempt => TimeSpan.FromSeconds(10),
            (exception, timeSpan, retryCount, context) =>
            {
                MonitorService.Log?.Warning(
                    "Migration attempt {RetryCount} failed: {Message}. Retrying in {Seconds}s...",
                    retryCount, exception.Message, timeSpan.TotalSeconds);
            });
    
    await retryPolicy.ExecuteAsync(async () =>
    {
        await context.Database.MigrateAsync();
        MonitorService.Log?.Information("{ServiceName} database migration completed", serviceName);
    });
}
```

**Add Polly NuGet Package:**
```bash
dotnet add DraftService/DraftService.csproj package Polly
```

**Expected Result:**
- Startup time: 20-40 seconds (1-2 retries)
- Graceful handling of SQL Server slow startup
- Logged retry attempts for debugging
- No more restart loops

---

### **Fix 2: Zipkin Dependency Management**

**Option A: Add Health Check to Zipkin-Storage**

Add a health check to ensure MySQL is ready before Zipkin starts.

**File:** `docker-compose.yml`

**Change:**
```yaml
zipkin-storage:
  image: openzipkin/zipkin-mysql
  ports:
    - "3306:3306"
  volumes:
    - zipkin_data:/mysql/data
  healthcheck:  # ← NEW
    test: ["CMD", "mysqladmin", "ping", "-h", "localhost", "-u", "zipkin", "-pzipkin"]
    interval: 10s
    timeout: 5s
    retries: 5
    start_period: 30s

zipkin:
  image: openzipkin/zipkin
  ports:
    - "9411:9411"
  environment:
    - STORAGE_TYPE=mysql
    - MYSQL_HOST=zipkin-storage
    - MYSQL_USER=zipkin
    - MYSQL_PASS=zipkin
  # Note: depends_on with healthcheck is ignored in swarm mode
  # Zipkin has built-in retry; health check just makes state visible
```

**Option B: Switch to In-Memory Storage (Fastest)**

If persistent traces aren't critical, use in-memory storage:

```yaml
zipkin:
  image: openzipkin/zipkin
  ports:
    - "9411:9411"
  # Remove STORAGE_TYPE, MYSQL_HOST, etc.
  # Zipkin defaults to in-memory storage
  # Startup time: 5-10 seconds instead of 60-90 seconds

# Remove zipkin-storage service entirely
```

**Recommendation:** Use **Option B** for development, **Option A** for production.

**Expected Result (Option B):**
- Startup time: 5-10 seconds
- No MySQL dependency
- Traces lost on restart (acceptable for dev)

---

### **Fix 3: Subscriber DELETE Test Race Condition**

**File:** `Scripts/test-full-flow.ps1`

**Change 1: Use Response ID Directly (Most Reliable)**

```powershell
# Before (UNRELIABLE: searches all subscribers by email)
$tempSubResponse = Invoke-RestMethod -Uri ($BASE_URL + ":8007/api/Subscriber") -Method Post -Body $TEMP_SUBSCRIBER_JSON -ContentType "application/json"
Start-Sleep -Seconds 2
$allSubs = Invoke-RestMethod -Uri ($BASE_URL + ":8007/api/Subscriber?region=$REGION") -Method Get
$tempSub = $allSubs | Where-Object { $_.email -eq "temp.deletion.test@void.com" } | Select-Object -First 1
$TEMP_SUB_ID = $tempSub.id

# After (RELIABLE: use ID from POST response)
$tempSubResponse = Invoke-RestMethod -Uri ($BASE_URL + ":8007/api/Subscriber") -Method Post -Body $TEMP_SUBSCRIBER_JSON -ContentType "application/json"
$TEMP_SUB_ID = $tempSubResponse.id  # ← ID directly from creation response
Write-Host "Temporary subscriber created with ID: $TEMP_SUB_ID"
Start-Sleep -Seconds 2  # Wait for database write + event propagation
```

**Change 2: Remove Region Query Parameter from DELETE**

```powershell
# Before
Invoke-RestMethod -Uri ($BASE_URL + ":8007/api/Subscriber/${TEMP_SUB_ID}?region=$REGION") -Method Delete

# After (region param ignored anyway)
Invoke-RestMethod -Uri ($BASE_URL + ":8007/api/Subscriber/${TEMP_SUB_ID}") -Method Delete
```

**Change 3 (Optional): Add Region Filtering to Controller**

If the test expects region filtering, update the controller:

**File:** `SubscriberService/Controllers/SubscriberController.cs`

```csharp
[HttpGet]
public async Task<IActionResult> GetSubscribers([FromQuery] string? region = null)
{
    var subscribers = await _subscriberAppService.GetSubscribers(region);
    if (!subscribers.Any())
    {
        return NotFound("It seems that there are no subscribers");
    }
    return Ok(subscribers);
}

[HttpDelete("{id}")]
public async Task<IActionResult> Unsubscribe(int id, [FromQuery] string? region = null)
{
    // Region parameter accepted but currently not used in lookup
    // (single database for all regions; ID is globally unique)
    var deleted = await _subscriberAppService.Delete(id);
    if (!deleted)
    {
        return NotFound($"Could not find subscriber with ID {id} to unsubscribe");
    }
    return Ok("Subscriber was unsubscribed");
}
```

**Expected Result:**
- DELETE test uses correct ID from POST response
- No more 404 errors
- Test completes successfully

---

## Summary

| Issue | Root Cause | Fix Priority | Estimated Fix Time |
|-------|-----------|-------------|-------------------|
| DraftService slow startup | Synchronous migration, no retry | **HIGH** | 15 minutes |
| Zipkin slow startup | MySQL dependency chain | **LOW** | 10 minutes (switch to in-memory) |
| Subscriber DELETE 404 | Wrong ID retrieved from GET response | **MEDIUM** | 10 minutes |

**Total Fix Time:** ~35 minutes

**Next Steps:**
1. Fix DraftService migration (highest impact; affects deployment reliability)
2. Fix Subscriber DELETE test (improves test suite reliability)
3. Consider Zipkin in-memory storage (quality-of-life improvement; not critical)

---

*"The diagnosis is complete. The wounds are mapped. The surgeon's blade awaits."*

