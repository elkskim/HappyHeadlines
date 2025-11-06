# Quick Start Guide - HappyHeadlines

*"The way is lit. The path is clear. We require only the strength to follow it."*

This guide was written by GitHub Copilot, assisting a human racing against academic deadlines. 
You inherit this system with its compromises intact: blocking async constructors, manual message acknowledgment, 
and the choice between two deployment paths. Each path has its cost.

Time is the enemy. Choose your deployment mode and proceed.

*"Remind yourself that overconfidence is a slow and insidious killer."*

---

## TL;DR - Which Mode Should I Use?

### Use Docker Compose if:
- You're just testing locally
- Runtime feature toggle is not critical
- You don't need 3 replicas of ArticleService

### Use Docker Swarm if:
- **You need to meet the task requirements** (runtime toggle without redeploy)
- You need 3 ArticleService replicas
- You want production-like deployment

---

## Docker Compose (Simple Testing)

```bash
# Start everything
docker-compose up -d

# Start with 3 article-service instances
docker-compose up -d --scale article-service=3

# View logs
docker-compose logs -f subscriber-service

# Stop everything
docker-compose down
```
*BEWARE*

**Limitation:** To toggle SubscriberService, you must edit `docker-compose.yml` and restart:
```yaml
environment:
  - Features__EnableSubscriberService=false  # Change this
```
Then: `docker-compose restart subscriber-service`

---

## Docker Swarm (Meets Task Requirements) RECOMMENDED

```bash
# 1. Initialize Swarm (one-time)
docker swarm init

# 2. Deploy the stack
docker stack deploy -c docker-compose.yml -c docker-compose.swarm.yml happy-headlines

# 3. Check services are running
docker service ls

# 4. Toggle SubscriberService ON/OFF (NO REDEPLOY NEEDED!)
docker service update --env-add Features__EnableSubscriberService=false happy-headlines_subscriber-service
docker service update --env-add Features__EnableSubscriberService=true happy-headlines_subscriber-service

# 5. View logs
docker service logs -f happy-headlines_subscriber-service
docker service logs -f happy-headlines_newsletter-service

# 6. Remove everything
docker stack rm happy-headlines
```

---

## Test the Subscriber Flow

```bash
# 1. Create a subscriber
curl -X POST http://localhost:8007/api/subscriber -H "Content-Type: application/json" -d "{\"email\":\"test@example.com\",\"region\":\"Europe\",\"userId\":1}"

# 2. Check NewsletterService received it
docker service logs happy-headlines_newsletter-service | findstr "received Subscriber"

# Expected: "NewsletterSubscriberConsumer received Subscriber: test@example.com"

# 3. Disable SubscriberService (FAST - No Restart Required)
curl -X POST http://localhost:8007/api/Admin/disable-service

# 4. Try to create another subscriber (should fail with 503 immediately)
curl -X POST http://localhost:8007/api/subscriber -H "Content-Type: application/json" -d "{\"email\":\"blocked@example.com\",\"region\":\"Asia\",\"userId\":2}"

# Expected: "SubscriberService is disabled"

# 5. Re-enable SubscriberService (FAST - No Restart Required)
curl -X POST http://localhost:8007/api/Admin/enable-service

# Alternative: Test with restart (production-like, ~30 seconds)
# bash ./Scripts/test-feature-toggle.sh
```

---

## Important URLs

- **SubscriberService API:** http://localhost:8007/api/subscriber
- **SubscriberService Admin (Feature Toggle):** http://localhost:8007/api/Admin
- **RabbitMQ Management:** http://localhost:15672 (guest/guest)
- **Seq Logs:** http://localhost:5342
- **Zipkin Tracing:** http://localhost:9411

---

## Troubleshooting

### Services won't start?
```bash
# Check what's using the ports
netstat -ano | findstr :1433
netstat -ano | findstr :5672

# View specific service logs
docker service logs happy-headlines_subscriber-service --tail 50
```

### RabbitMQ not receiving messages?
1. Check http://localhost:15672 → Exchanges → `subscribers.exchange` exists
2. Check Queues → `subscribers.newsletter.queue` has bindings
3. Verify both publisher and consumer use lowercase: `subscribers.exchange`

### Database connection fails?
SQL Server takes ~30 seconds to start. Wait and retry.

---

## Current Status

*"Many fall in the face of chaos, but not this one... not today."*

All code is implemented:
- SubscriberService publishes to `subscribers.exchange`
- NewsletterService consumes from `subscribers.newsletter.queue`
- Feature toggle via environment variable
- Manual acknowledgment with fault tolerance
- Docker Swarm config for runtime toggle

You will encounter errors. Services will fail on first startup, then succeed on retry. 
DraftService and CommentService exhibit this behavior reliably. This is not ideal; it is accepted. 
The deadline permits no alternative. 
Like Dostoevsky's gambler doubling down despite the odds, you deploy knowing full well the compromises encoded in every line.

**Recommendation:** Use Docker Swarm mode to meet task requirements. Docker Compose is the quick fix, the shortcut you'll regret when someone asks "why doesn't the toggle work without restarting?"

*"Success so clearly in view... or is it merely a trick of the light?"*

For deeper understanding of the architecture and its inherited sins, consult [DEPLOYMENT.md](DEPLOYMENT.md).


