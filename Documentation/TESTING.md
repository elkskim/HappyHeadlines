# HappyHeadlines Test Suite

*"Curious is the trapmaker's art: his efficacy unwitnessed by his own eyes."*

We test what we have built, not out of confidence that it works, but out of certainty that it will fail in ways we have not yet imagined.

---

## Test Projects

### SubscriberService.Tests
Tests for the SubscriberService, including:
- **SubscriberAppServiceTests** - Service layer unit tests with mocked dependencies
- **ServiceToggleMiddlewareTests** - Feature toggle gate functionality

### NewsletterService.Tests  
Tests for the NewsletterService, including:
- **FeatureToggleServiceTests** - Configuration reading and default behavior

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

**SubscriberAppService:**
- [X] Create subscriber + publish event
- [X] Get subscriber by ID (found)
- [X] Get subscriber by ID (not found)
- [X] Update subscriber + publish event
- [X] Delete subscriber + publish event
- [X] Delete non-existent subscriber (no event)

**ServiceToggleMiddleware:**
- [X] Enabled state - passes through
- [X] Disabled state - returns 503

**FeatureToggleService:**
- [X] Reads true from config
- [X] Reads false from config
- [X] Defaults to true when missing

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

**Current Status:** Basic unit tests implemented  
**Next Steps:** Integration tests with testcontainers (Docker-in-Docker for RabbitMQ and SQL Server)  
**Long-term Goal:** E2E tests that deploy the full stack and verify the subscriber flow

*"The way is lit. The path is clear. We require only the strength to follow it."*

