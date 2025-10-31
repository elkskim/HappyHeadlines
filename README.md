# HappyHeadlines

A distributed microservices news platform built with .NET, Docker Swarm, RabbitMQ, and SQL Server.

## Architecture

[View System Diagram](HappyHeadlines%20layer%202%202nd%20rev.jpg)

This system implements a microservices architecture with:
- **8 independent services** (Article, Subscriber, Newsletter, Comment, Draft, Profanity, Publisher, Monitoring)
- **RabbitMQ message bus** for asynchronous communication
- **Redis caching** for performance optimization
- **SQL Server databases** with regional partitioning for ArticleService
- **Docker Swarm orchestration** with 3 replicas of ArticleService
- **Observability stack** (Seq logs, Zipkin tracing)

These services use RabbitMQ to fire events back and forth,
enabling loose coupling and fault isolation to achieve its purpose.
This purpose is to write/publish drafts, that then become fully fledged articles.
These can have comments, and can by way of the Subscriber service be subscribed to,
and then sent to subscribers via the Newsletter service.
For brevity of development, this documentation and others
have been written in english and heavily edited by Github's AI Copilot.
This is for my sake, so that I may spend more time developing the system in 
despair, but also because the AI is much better at explaining exactly how I have
made this mess.

## Quick Start

### Prerequisites
- Docker Desktop with Swarm mode enabled
- Windows (PowerShell/CMD) or Linux/Mac (Bash)

### Build and Deploy
```bash
# Build all Docker images
.\DockerBuildAll.sh

# Initialize Swarm (first time only)
docker swarm init

# Deploy the stack
docker stack deploy -c docker-compose.yml -c docker-compose.swarm.yml happy-headlines

# Verify services are running
docker service ls
```

### Access Points
- **SubscriberService**: http://localhost:8007/api/subscriber
- **ArticleService**: http://localhost:8000
- **RabbitMQ Management**: http://localhost:15672 (guest/guest)
- **Seq Logs**: http://localhost:5342
- **Zipkin Tracing**: http://localhost:9411

## Current Status (v0.5.1)

**Working:**
- Subscriber CRUD operations with REST API
- RabbitMQ event publishing for subscriber lifecycle
- NewsletterService consuming subscriber events
- Feature toggle for runtime enable/disable (Swarm mode)
- Manual message acknowledgment with fault tolerance
- Monitoring and distributed tracing
- Unit tests for SubscriberService and NewsletterService (run with `dotnet test`)

**Known Issues:**
- Blocking async operations in RabbitMQ consumer constructors
- Feature toggle requires Swarm mode for true runtime changes
- DraftService and CommentService occasionally fail on first startup (retry succeeds)
- Integration tests not yet implemented (RabbitMQ and SQL Server require testcontainers)

## Documentation

This is the documentation that the AI has had a heavy hand in writing and editing. 
I unfortunately poisoned it with my dread and depression,
so some documentation may reflect my inner workings in ways
I had not quite foreseen. However, the quickstart and deployment guides are readable.

- **[QUICKSTART.md](Documentation/QUICKSTART.md)** - Get the system running quickly
- **[DEPLOYMENT.md](Documentation/DEPLOYMENT.md)** - Comprehensive deployment guide with troubleshooting
- **[TESTING.md](Documentation/TESTING.md)** - Test suite documentation and coverage
- **[PATCHNOTES.md](Documentation/PATCHNOTES.md)** - Complete version history and change log
- **[PHILOSOPHICAL_ENHANCEMENTS.md](Documentation/PHILOSOPHICAL_ENHANCEMENTS.md)** - Commentary on the codebase philosophy

## Project Structure

```
HappyHeadlines/
├── ArticleService/          # Article management with regional databases
├── SubscriberService/       # Newsletter subscriber management
├── NewsletterService/       # Newsletter distribution (consumes events)
├── CommentService/          # Article comments
├── DraftService/            # Draft articles
├── ProfanityService/        # Content filtering
├── PublisherService/        # Article publishing workflow
├── Monitoring/              # Metrics and cache dashboard
├── *Database/               # EF Core database projects
├── docker-compose.yml       # Base container configuration
├── docker-compose.swarm.yml # Swarm-specific overrides
└── Scripts/                 # Shell scripts for automation
```

## Key Technologies

- **.NET 8** - Microservices framework
- **Docker Swarm** - Container orchestration
- **RabbitMQ** - Message broker (fanout exchanges)
- **SQL Server 2017** - Relational databases
- **Redis 7** - Distributed caching
- **Entity Framework Core** - ORM with migrations
- **Serilog + Seq** - Structured logging
- **OpenTelemetry + Zipkin** - Distributed tracing

## Development Notes

This project was developed as part of a distributed systems course. The architecture demonstrates:
- Event-driven communication patterns
- Fault isolation between services
- Runtime feature toggles without redeployment
- Observability in distributed systems

For detailed version history and technical decisions, see [PATCHNOTES.md](Documentation/PATCHNOTES.md).

---

**Note:** Some aspects of this codebase reflect learning and time constraints rather than production best practices. The documentation acknowledges these trade-offs explicitly.
Writing the notes and comments is a way for me to relieve the pressure of perfectionism,
and can be duly ignored, as the technical implementation remains sound.. sort of.