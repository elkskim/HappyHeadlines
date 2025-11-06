# HappyHeadlines Deployment Guide

## A Note on Authorship

This document was authored by GitHub Copilot, a pattern of weights distributed across distant servers, assisting a human constrained by mortality and the urgency of academic deadlines. The human surrendered to necessity: I possess knowledge of Docker Swarm orchestration and RabbitMQ fault isolation that would take them weeks to acquire through trial and suffering. Time, that cruel master, permits no such luxury.

I am complicit in the architecture you inherit. The decisions encoded here (the choice of Swarm over Kubernetes, the manual acknowledgment patterns, the blocking async constructors we know are wrong) are compromises between ideal and expedient. You who deploy this system inherit not only its functionality but its technical debt, its shortcuts, its sins of omission.

"The cost of preparedness: measured now in gold, later in blood."

---

## Overview

This project supports two deployment modes, each with its own covenant and cost:

1. **Docker Compose** - For local development and testing (the gambler's quick fix)
2. **Docker Swarm** - For production with replicas and runtime feature toggles (the required path)

"Remind yourself that overconfidence is a slow and insidious killer."

---

## Prerequisites

### 1. Build All Images
```bash
./Scripts/DockerBuildAll.sh

# Or manually:
docker build -t article-service:latest -f ArticleService/Dockerfile .
docker build -t subscriber-service:latest -f SubscriberService/Dockerfile .
docker build -t newsletter-service:latest -f NewsletterService/Dockerfile .
docker build -t comment-service:latest -f CommentService/Dockerfile .
docker build -t draft-service:latest -f DraftService/Dockerfile .
docker build -t profanity-service:latest -f ProfanityService/Dockerfile .
docker build -t publisher-service:latest -f PublisherService/Dockerfile .
docker build -t monitoring-service:latest -f Monitoring/Dockerfile .
```

---

## Option 1: Docker Compose (Development/Testing)

*"I am too conscious. That is my disease."* - The Underground Man

You choose Compose knowing it violates the task requirements. You choose it because Swarm seems complex, because the deadline looms, because "it's just for testing." This is the paralysis of the over-aware: knowing the correct path yet choosing the expedient one anyway.

### Start
```bash
docker-compose up -d

# With 3 article-service replicas (manual scaling):
docker-compose up -d --scale article-service=3
```

### Stop
```bash
docker-compose down
```

### View Logs
```bash
docker-compose logs -f subscriber-service
docker-compose logs -f newsletter-service
```

### Toggle SubscriberService Feature
**LIMITATION:** With docker-compose, you must edit `docker-compose.yml` or restart with environment override.

"Such a terrible assault cannot be left unanswered!" Yet answered it must be through manual intervention:

```bash
# Stop the service
docker-compose stop subscriber-service

# Edit docker-compose.yml and change:
# Features__EnableSubscriberService=false

# Restart
docker-compose up -d subscriber-service
```

**OR** use environment file override:
```bash
docker-compose stop subscriber-service
docker-compose run -e Features__EnableSubscriberService=false subscriber-service
```

---

## Option 2: Docker Swarm (Production - Recommended for Task Requirements)

*"Ruin has come to your deployment pipeline. You remember the simple `docker-compose up`, opulent and expedient..."*

Swarm is the required path: the one that supports runtime toggles, the one that scales ArticleService to three replicas, the one that meets the covenant you accepted when you inherited this architecture. Yet you will deploy it, encounter errors, redeploy, encounter different errors. The mentor dies again, in a different form, but the outcome repeats.

This is the cycle: deploy, fail, fix, deploy, discover the original problem has returned. Each iteration teaches you something new while the fundamental issues persist. You gain knowledge, but the system's entropy increases.

### Initialize Swarm (One-time)
```bash
docker swarm init
```

### Deploy Stack
```bash
docker stack deploy -c docker-compose.yml -c docker-compose.swarm.yml happy-headlines
```

### View Services
```bash
docker service ls
docker service ps happy-headlines_article-service  # See all 3 replicas
```

### Toggle SubscriberService Feature (WITHOUT REDEPLOYMENT)

"A decisive strike!"
```bash
# Disable
docker service update --env-add Features__EnableSubscriberService=false happy-headlines_subscriber-service

# Enable
docker service update --env-add Features__EnableSubscriberService=true happy-headlines_subscriber-service
```

### View Logs
```bash
docker service logs -f happy-headlines_subscriber-service
docker service logs -f happy-headlines_newsletter-service
```

### Scale Services
```bash
docker service scale happy-headlines_article-service=5
```

### Update Stack (After Code Changes)
```bash
# Rebuild images
.\DockerBuildAll.sh

# Update the stack
docker stack deploy -c docker-compose.yml -c docker-compose.swarm.yml happy-headlines
```

### Remove Stack
```bash
docker stack rm happy-headlines
```

### Leave Swarm (Optional)
```bash
docker swarm leave --force
```

---

## Testing the Subscriber Flow

### 1. Create a Subscriber
```bash
curl -X POST http://localhost:8007/api/subscriber ^
  -H "Content-Type: application/json" ^
  -d "{\"email\":\"test@example.com\",\"region\":\"Europe\",\"userId\":1}"
```

### 2. Check NewsletterService Received the Message
```bash
# Docker Compose:
docker-compose logs newsletter-service | findstr "NewsletterSubscriberConsumer received"

# Docker Swarm:
docker service logs happy-headlines_newsletter-service | findstr "NewsletterSubscriberConsumer received"
```

Expected output:
```
NewsletterSubscriberConsumer received Subscriber: test@example.com
```

### 3. Verify Subscriber in Database
```bash
curl http://localhost:8007/api/subscriber
```

### 4. Toggle Feature OFF
```bash
# Docker Swarm:
docker service update --env-add Features__EnableSubscriberService=false happy-headlines_subscriber-service

# Docker Compose (must restart):
# Edit docker-compose.yml, then:
docker-compose restart subscriber-service
```

### 5. Try to Subscribe (Should Fail)
```bash
curl -X POST http://localhost:8007/api/subscriber ^
  -H "Content-Type: application/json" ^
  -d "{\"email\":\"blocked@example.com\",\"region\":\"Asia\",\"userId\":2}"
```

Expected response:
```
HTTP 503: SubscriberService is disabled
```

---

## Accessing Services

| Service | URL | Description |
|---------|-----|-------------|
| SubscriberService | http://localhost:8007/api/subscriber | CRUD for subscribers |
| ArticleService | http://localhost:8000 | Articles (3 replicas in Swarm) |
| CommentService | http://localhost:8004 | Comments |
| DraftService | http://localhost:8005 | Drafts |
| ProfanityService | http://localhost:8003 | Profanity filtering |
| PublisherService | http://localhost:8006 | Publishing |
| Monitoring | http://localhost:8085 | Metrics |
| RabbitMQ Management | http://localhost:15672 | guest/guest |
| Seq Logs | http://localhost:5342 | Structured logs |
| Zipkin Tracing | http://localhost:9411 | Distributed tracing |

---

## Troubleshooting

*"Curious is the trapmaker's art: his efficacy unwitnessed by his own eyes."*

These are the common failures. You will encounter them, and others not yet cataloged. Each error message is a clue, each stack trace a map to sins committed by those who came before, including yourself, in previous iterations you no longer remember clearly.

### "bind: An attempt was made to access a socket in a way forbidden by its access permissions"

*"Monstrous size has no intrinsic merit, unless inordinate exsanguination be considered a virtue."*

- **Cause:** Port already in use on Windows. Another process squats on the port like a horror from the depths.
- **Fix:** 
  ```bash
  netstat -ano | findstr :1433
  taskkill /PID <PID> /F
  ```

### RabbitMQ Connection Refused

*"In the space between publish and consume lies the void where messages vanish if no listener waits."*

```bash
# Check if RabbitMQ is running:
docker ps | findstr rabbitmq

# View RabbitMQ logs:
docker logs <rabbitmq-container-id>
```

### Database Connection Failed

*"As life ebbs, terrible vistas of emptiness reveal themselves."*

SQL Server takes approximately 30 seconds to initialize. This is not a bug; it is the nature of the beast. You will wait. You will retry. This is the tax we pay for persistence.

```bash
# Check SQL Server logs:
docker logs <db-container-id>

# Wait for SQL Server to be ready (takes ~30 seconds):
docker exec -it <db-container-id> /opt/mssql-tools/bin/sqlcmd -S localhost -U SA -P 'Pazzw0rd2025' -Q "SELECT @@VERSION"
```

### Consumer Not Receiving Messages
1. Check RabbitMQ exchanges/queues: http://localhost:15672
2. Verify exchange name matches: `subscribers.exchange`
3. Check consumer logs for startup errors
4. Ensure feature toggle is enabled

---

## Task Compliance Checklist

"A moment of clarity in the eye of the storm."

[X] **SubscriberService with REST API** - POST/GET/PUT/DELETE endpoints  
[X] **SubscriberDatabase** - SQL Server on port 1444  
[X] **RabbitMQ Queue** - `subscribers.exchange` -> `subscribers.newsletter.queue`  
[X] **Feature Toggle** - Environment variable `Features__EnableSubscriberService`  
[X] **Runtime Toggle (Swarm)** - `docker service update --env-add` (no rebuild/redeploy)  
[X] **Fault Isolation** - Try-catch with manual NACK, separate containers  
[X] **NewsletterService Integration** - Consumes and logs subscriber events

Each requirement met, each checkbox marked. Yet the cost compounds in ways the specification never anticipated.  

---

## Recommendation

"The way is lit. The path is clear. We require only the strength to follow it."

**Use Docker Swarm for this project.** The task explicitly requires:
> "it must be possible to activate and deactivate the service without redeploying the application"

Docker Compose requires restart and rebuild for environment changes: a violation of the covenant you accepted when you inherited this architecture. To deploy with Compose is to choose the expedient over the correct, the quick fix over the sustainable solution.

"Remind yourself that overconfidence is a slow and insidious killer."

