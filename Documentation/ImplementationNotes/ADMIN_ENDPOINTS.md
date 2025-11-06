# Admin Endpoints - SubscriberService

> **Authorship Note:** GitHub Copilot, November 6, 2025. These endpoints emerged from necessity—the need to test without restarting, to toggle without redeploying, to prove our assumptions without waiting. They are tools of convenience that may become permanent fixtures or temporary scaffolding. Time will reveal their fate.

---

## Overview

The SubscriberService exposes admin endpoints for runtime feature toggle control. These enable **testing and debugging without service restart**.

**Base URL:** `http://localhost:8007/api/Admin`

**Security Note:** These endpoints are currently **unprotected** for development/testing. In production, secure with authentication/authorization or remove entirely.

---

## Endpoints

### Disable Service (Runtime Override)

**Endpoint:** `POST /api/Admin/disable-service`

**Purpose:** Immediately disable SubscriberService without restart

**Effect:**
- All requests to `/api/Subscriber/*` return HTTP 503
- Response body: "SubscriberService is disabled"
- Override persists only in memory (lost on restart)
- Admin endpoints remain accessible

**Example:**
```bash
curl -X POST http://localhost:8007/api/Admin/disable-service
```

**Response:**
```json
{
  "message": "SubscriberService disabled via runtime override",
  "note": "This override is temporary and will be lost on service restart"
}
```

---

### Enable Service (Runtime Override)

**Endpoint:** `POST /api/Admin/enable-service`

**Purpose:** Immediately enable SubscriberService without restart

**Effect:**
- All requests to `/api/Subscriber/*` process normally
- Override persists only in memory (lost on restart)

**Example:**
```bash
curl -X POST http://localhost:8007/api/Admin/enable-service
```

**Response:**
```json
{
  "message": "SubscriberService enabled via runtime override",
  "note": "This override is temporary and will be lost on service restart"
}
```

---

### Reset Feature Toggle

**Endpoint:** `POST /api/Admin/reset-feature-toggle`

**Purpose:** Clear runtime override and return to configuration-based value

**Effect:**
- Removes in-memory override
- Service reads from `appsettings.json` or environment variables
- Behavior determined by configuration file

**Example:**
```bash
curl -X POST http://localhost:8007/api/Admin/reset-feature-toggle
```

**Response:**
```json
{
  "message": "Runtime override cleared, using configuration value",
  "note": "Feature toggle now reads from appsettings.json or environment variables"
}
```

---

### Get Feature Toggle Status

**Endpoint:** `GET /api/Admin/feature-toggle-status`

**Purpose:** Check current feature toggle state

**Example:**
```bash
curl http://localhost:8007/api/Admin/feature-toggle-status
```

**Response (Enabled):**
```json
{
  "enabled": true,
  "message": "Service is enabled"
}
```

**Response (Disabled):**
```json
{
  "enabled": false,
  "message": "Service is disabled"
}
```

---

## Behavior Details

### Middleware Bypass

Admin endpoints **bypass** the ServiceToggleMiddleware:
- Always accessible regardless of feature toggle state
- Required to re-enable service if disabled
- Path check: `context.Request.Path.StartsWithSegments("/api/Admin")`

### Override Precedence

Configuration hierarchy (highest to lowest):
1. **Runtime override** (via admin endpoints) - Highest priority
2. **Environment variables** (`Features__EnableSubscriberService`)
3. **Configuration file** (`appsettings.json`)
4. **Default value** (true if all above are missing)

### Persistence

**Runtime override:**
- ✓ Immediate effect (no restart)
- ✗ Lost on service restart
- ✗ Not persisted to disk

**Environment variable:**
- ✗ Requires service restart
- ✓ Persisted across restarts
- ✓ Docker Swarm manages

**Configuration file:**
- ✗ Requires service restart
- ✓ Persisted to disk
- ✓ Version controlled

---

## Testing Workflows

### Quick Test (5 seconds)

```bash
# Automated test
bash ./Scripts/test-feature-toggle-fast.sh

# Or manual
curl -X POST http://localhost:8007/api/Admin/disable-service
curl http://localhost:8007/api/Subscriber  # Should return 503
curl -X POST http://localhost:8007/api/Admin/enable-service
curl http://localhost:8007/api/Subscriber  # Should return 200
```

### Production-Like Test (30 seconds)

```bash
# Tests environment variable override with restart
bash ./Scripts/test-feature-toggle.sh
```

---

## Security Considerations

### Current State (Development)
- ✗ No authentication
- ✗ No authorization
- ✗ No rate limiting
- ✓ Only admin operations (no data access)

### Production Recommendations

**Option 1: Secure the endpoints**
```csharp
[Authorize(Roles = "Admin")]
[Route("api/[controller]")]
public class AdminController : ControllerBase
```

**Option 2: Remove in production**
```csharp
#if DEBUG
    app.MapControllers();  // Includes AdminController
#else
    // Admin endpoints not registered in release builds
#endif
```

**Option 3: Environment-based registration**
```csharp
if (builder.Environment.IsDevelopment())
{
    // Register AdminController only in Development
}
```

---

## Implementation Details

**Files:**
- `SubscriberService/Controllers/AdminController.cs` - HTTP endpoints
- `SubscriberService/Features/FeatureToggleService.cs` - Runtime override logic
- `SubscriberService/Features/IFeatureToggleService.cs` - Interface definition
- `SubscriberService/Middleware/ServiceToggleMiddleware.cs` - Admin bypass logic

**Test Scripts:**
- `Scripts/test-feature-toggle-fast.sh` - Automated test using admin endpoints
- `Scripts/test-feature-toggle.sh` - Automated test using environment variables

---

*"A backdoor into our own fortress. We built the walls, and we built the secret passage. The question is not whether we should guard the passage, but whether we should seal it when we leave the workshop and enter the battlefield."*

**Current Status:** Functional in development. Production security not implemented.  
**Recommendation:** Evaluate security requirements before production deployment.

