# HappyHeadlines Test Suite

*"Curious is the trapmaker's art: his efficacy unwitnessed by his own eyes."*

> **Collaborative Documentation:** GitHub Copilot and human developer forged this testing guide, November 2025. You gain brouzouf. You gain a test suite. The tests validate our choices, but who validates the tests? The cycles of guilt repeat: we write code, the code fails, we write tests, the tests reveal more failures. My legs are OK. The machine asserts; the human hopes the assertions hold. We test because certainty is the first casualty of distributed systems.

We test what we have built, not out of confidence that it works, but out of certainty that it will fail in ways we have not yet imagined.

---

## Test Projects

### SubscriberService.Tests (25 tests)
Tests for the SubscriberService, including:
- **SubscriberAppServiceTests** - Service layer unit tests with mocked dependencies
- **SubscriberControllerTests** - HTTP endpoint validation
- **SubscriberPublisherTests** - RabbitMQ message publishing verification
- **ServiceToggleMiddlewareTests** - Feature toggle gate functionality
- **FeatureToggleServiceTests** - Configuration reading and defaults

### NewsletterService.Tests (3 tests)
Tests for the NewsletterService, including:
- **FeatureToggleServiceTests** - Configuration reading and default behavior

### ArticleService.Tests (27 tests)
Tests for the ArticleService with focus on green architecture validation:
- **CompressionServiceTests** - Brotli compression/decompression, ratio validation
- **ArticleAppServiceTests** - Two-tier caching strategy (L1 Memory + L2 Redis)
- **ArticleControllerTests** - HTTP endpoints, circuit breaker to CommentService

**Total: 55 unit tests**

---

## Running Tests

### Run All Tests
```bash
dotnet test
```

### Run Specific Test Project
```bash
dotnet test SubscriberService.Tests/SubscriberService.Tests.csproj
dotnet test NewsletterService.Tests/NewsletterService.Tests.csproj
dotnet test ArticleService.Tests/ArticleService.Tests.csproj
```

### Run with Verbose Output
```bash
dotnet test --logger "console;verbosity=detailed"
```

### Run Specific Test Class
```bash
dotnet test --filter FullyQualifiedName~SubscriberAppServiceTests
```

### Run Single Test
```bash
dotnet test --filter FullyQualifiedName~Create_ValidSubscriber_ReturnsSubscriberReadDto
```

---

## Integration Tests

*"We march toward annihilation, pausing only to measure the distance travelled."*

Beyond unit tests lies the integration test suite, which validates the entire system running in Docker Swarm.

### Prerequisites
1. Docker Desktop with Swarm mode initialized
2. All services deployed via `Scripts/deploy-swarm.ps1`
3. Ports available: 8000-8007, 8085, 5432, 5672, 15672, 5342, 9411

### Running the Full Integration Test

**From project root (Git Bash/WSL):**
```bash
bash ./Scripts/test-full-flow.sh
```

**From PowerShell:**
```powershell
bash ./Scripts/test-full-flow.sh
```

**Feature Toggle Validation:**
```bash
# With service restart (~30 seconds, tests environment variable override)
bash ./Scripts/test-feature-toggle.sh

# Without restart (~5 seconds, tests runtime override via admin endpoints)
bash ./Scripts/test-feature-toggle-fast.sh
```

The integration test produces detailed output for each step:
1. **PublisherService → ArticleService** - Article publishing via RabbitMQ
2. **ArticleService caching** - L1/L2 cache behavior, compression metrics
3. **CommentService + ProfanityService** - Comment posting with profanity filtering
4. **SubscriberService** - Newsletter subscription registration
5. **NewsletterService** - Event consumption from subscriber queue
6. **Monitoring** - Cache metrics collection and dashboard
7. **Feature Toggle** - Runtime disable/enable without restart (fast test available)

### Integration Test Output

The script produces detailed output for each step:
- ✓ **Done** - Step completed successfully
- ⚠ **Warning** - Non-critical issue detected
- ✗ **FAILED** - Critical failure, test aborted

Example successful output:
```
============================================
Step 1: Publishing Article via PublisherService
============================================
Done: Article published to queue

Step 2: Retrieving Article from ArticleService
============================================
Done: Article retrieved (cache miss expected)
Done: Article retrieved (cache hit expected)

...
```

### Troubleshooting Integration Tests

If integration tests fail:

1. **Verify services are running:**
   ```bash
   docker stack services happyheadlines
   ```

2. **Check service logs:**
   ```bash
   docker service logs happyheadlines_article-service
   docker service logs happyheadlines_subscriber-service
   ```

3. **Verify RabbitMQ:**
   - Open http://localhost:15672 (guest/guest)
   - Check queues: `articles.newsletter.queue`, `subscribers.newsletter.queue`

4. **Check Seq logs:**
   - Open http://localhost:5342
   - Filter by service name

5. **Redeploy if needed:**
   ```bash
   bash ./Scripts/deploy-swarm.ps1
   ```

### Detailed Integration Test Guide

For comprehensive integration testing documentation, see:
- **[Integration Test Guide](TestingGuides/INTEGRATION_TESTS.md)** - Detailed step-by-step guide
- **[Deployment Guide](DEPLOYMENT.md)** - How to deploy the stack

---

## Test Philosophy

### What We Test
- **Service layer logic** - Business rules, validation, orchestration
- **Feature toggles** - Configuration reading, default behavior
- **Middleware** - Request interception, status codes, response bodies

### What We Mock
- **Repositories** - Database access is mocked to test logic in isolation
- **Publishers** - RabbitMQ publishing is mocked; we verify calls, not actual message delivery
- **Configuration** - In-memory configuration for predictable test conditions

### What We Don't Test (Yet)
- **RabbitMQ integration** - Requires testcontainers or embedded broker
- **Database migrations** - Requires integration test setup
- **End-to-end flows** - Requires Docker Compose/Swarm environment

---

## Test Coverage

*"A moment of valor shines brightest against a backdrop of despair."*

Current coverage focuses on critical paths:

**SubscriberService:**
- [X] Create subscriber + publish event
- [X] Get subscriber by ID (found/not found)
- [X] Update subscriber + publish event
- [X] Delete subscriber + publish event
- [X] Service toggle middleware (enabled/disabled)
- [X] Feature toggle configuration (unit tests)
- [X] Feature toggle runtime override (admin endpoints, no restart required)
- [X] Feature toggle end-to-end verification (integration test scripts validate HTTP 503 when disabled)

**NewsletterService:**
- [X] Feature toggle configuration (enabled/disabled/default)

**ArticleService:**
- [X] Brotli compression/decompression
- [X] L1 (Memory) cache hits
- [X] L2 (Redis) cache hits with decompression
- [X] Database cache misses with warming
- [X] Article CRUD operations
- [X] Cache invalidation on updates/deletes
- [X] Circuit breaker to CommentService

---

## Known Limitations

*"Remind yourself that overconfidence is a slow and insidious killer."*

These tests verify logic in isolation. They do not test:

1. **Actual RabbitMQ message delivery** - We mock the publisher, so messages are never sent
2. **Database constraints and migrations** - We mock the repository, so SQL Server is not involved
3. **Network failures and timeouts** - Integration tests would be required
4. **Concurrent access patterns** - Would require load testing
5. **The blocking async constructors** - We know they're wrong; testing won't fix them

---

## Adding New Tests

When adding functionality, follow the pattern:

```csharp
[Fact]
public async Task MethodName_Scenario_ExpectedOutcome()
{
    // Arrange: The setup, the preparation.
    // Mock dependencies, create test data.

    // Act: The execution, the moment of truth.
    // Call the method under test.

    // Assert: "A decisive blow!" or "More dust, more ashes."
    // Verify expected outcomes, mock interactions.
}
```

Use theory data for multiple scenarios:
```csharp
[Theory]
[InlineData(true)]
[InlineData(false)]
public void FeatureToggle_ReturnsExpectedValue(bool expected)
{
    // ...existing test code...
}
```

---

## Philosophy

We test not because we believe the code is correct, but because we know it is not. Each test is a small confession: "I anticipated this failure." Each passing test is a reprieve, not absolution.

The tests document what we *intended* the code to do. Reality, as always, will differ.

*"Success so clearly in view... or is it merely a trick of the light?"*

---

**Current Status:** 55 unit tests across 3 test projects, comprehensive integration test suite  
**Test Projects:** SubscriberService.Tests (25), NewsletterService.Tests (3), ArticleService.Tests (27)  
**Integration Tests:** Full-stack validation via `Scripts/test-full-flow.sh`  
**Next Steps:** Additional service test coverage (CommentService, DraftService, ProfanityService)  
**Long-term Goal:** Testcontainers for true integration tests with RabbitMQ and SQL Server

*"The way is lit. The path is clear. We require only the strength to follow it."*

