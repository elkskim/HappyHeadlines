# Question 12: Virtualization

---

## Part 1: Forklar forskellen på virtualisering gennem virtuelle maskiner og containers

### What is Virtualization?

**Definition:** Running multiple isolated environments on a single physical machine.

**The Problem It Solves:**
```
Without virtualization:
┌─────────────────────────────────────┐
│         Physical Server             │
│  One OS, one application            │
│  90% idle most of the time          │
│  New app = buy new server           │
└─────────────────────────────────────┘
Waste of money, hardware, and electricity.
```

---

### Virtual Machines (VMs)

**What it is:** Full simulation of a computer within a computer.

```
┌─────────────────────────────────────────────┐
│            Physical Server                  │
├─────────────────────────────────────────────┤
│            Hypervisor (VMware, Hyper-V)     │
├──────────────┬──────────────┬───────────────┤
│     VM 1     │     VM 2     │     VM 3      │
├──────────────┼──────────────┼───────────────┤
│ Guest OS     │ Guest OS     │ Guest OS      │
│ (Windows)    │ (Ubuntu)     │ (CentOS)      │
├──────────────┼──────────────┼───────────────┤
│ Libs/Deps    │ Libs/Deps    │ Libs/Deps     │
├──────────────┼──────────────┼───────────────┤
│ App A        │ App B        │ App C         │
└──────────────┴──────────────┴───────────────┘
```

**Key characteristics:**
- Each VM has its own **full operating system**
- Hypervisor manages hardware access
- Complete isolation (each VM thinks it's a real machine)
- Heavy: GBs of disk, minutes to boot

---

### Containers

**What it is:** Isolated process with its own filesystem, but shares the host kernel.

```
┌─────────────────────────────────────────────┐
│            Physical Server                  │
├─────────────────────────────────────────────┤
│              Host OS (Linux)                │
├─────────────────────────────────────────────┤
│          Container Runtime (Docker)         │
├──────────────┬──────────────┬───────────────┤
│ Container 1  │ Container 2  │ Container 3   │
├──────────────┼──────────────┼───────────────┤
│ Libs/Deps    │ Libs/Deps    │ Libs/Deps     │
├──────────────┼──────────────┼───────────────┤
│ App A        │ App B        │ App C         │
└──────────────┴──────────────┴───────────────┘
        ↑ No separate OS per container!
```

**Key characteristics:**
- **Shares host kernel** (no separate OS)
- Lightweight: MBs of disk, seconds to start
- Uses Linux namespaces and cgroups for isolation
- "Just enough" isolation for most use cases

---

### The Key Differences

| Aspect | Virtual Machines | Containers |
|--------|------------------|------------|
| **Isolation** | Full hardware-level | Process-level |
| **OS** | Each VM has full OS | Shares host kernel |
| **Size** | GBs (includes OS) | MBs (just app + deps) |
| **Boot time** | Minutes | Seconds |
| **Resource overhead** | High (full OS per VM) | Low (shared kernel) |
| **Density** | ~10-50 VMs per host | ~100-1000 containers |
| **Security** | Stronger isolation | Weaker (shared kernel) |
| **Portability** | Portable (as images) | Very portable (smaller) |

---

### Visual Comparison

```
VIRTUAL MACHINE:                    CONTAINER:
┌─────────────────┐                 ┌─────────────────┐
│      App        │                 │      App        │
├─────────────────┤                 ├─────────────────┤
│   Libraries     │                 │   Libraries     │
├─────────────────┤                 └─────────────────┘
│   Guest OS      │ ← EXTRA                 ↑
│   (2-20 GB)     │                 Shares host kernel
├─────────────────┤                 
│   Hypervisor    │                 ┌─────────────────┐
├─────────────────┤                 │ Docker Engine   │
│   Host OS       │                 ├─────────────────┤
├─────────────────┤                 │   Host OS       │
│   Hardware      │                 ├─────────────────┤
└─────────────────┘                 │   Hardware      │
                                    └─────────────────┘
```

---

### When to Use What?

**Use VMs when:**
- Need different operating systems (Windows + Linux)
- Strong security isolation required
- Running untrusted workloads
- Legacy applications

**Use Containers when:**
- Microservices architecture
- Fast scaling needed
- CI/CD pipelines
- Same OS is acceptable
- Development/production parity

---

## Part 2: Demonstrer hvordan Docker kan bruges til at skabe et konsistent udviklings- og produktionsmiljø

### The "Works on My Machine" Problem

```
Developer's Laptop          Production Server
┌──────────────────┐        ┌──────────────────┐
│ .NET 9.0.1       │        │ .NET 9.0.0       │  ← Version mismatch
│ Windows 11       │        │ Ubuntu 22.04     │  ← Different OS
│ SQL Server 2022  │        │ SQL Server 2017  │  ← Different DB
│ Custom env vars  │        │ Missing env vars │  ← Config drift
└──────────────────┘        └──────────────────┘
        ✓                          ✗
   "Works for me!"            "500 Error"
```

**Docker's solution:** Package the entire environment with the app.

---

### HappyHeadlines Dockerfile Example

```dockerfile
# ArticleService/Dockerfile

# Build stage - uses full SDK
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy csproj and restore
COPY ["ArticleService/ArticleService.csproj", "ArticleService/"]
COPY ["ArticleDatabase/ArticleDatabase.csproj", "ArticleDatabase/"]
COPY ["Monitoring/Monitoring.csproj", "Monitoring/"]
RUN dotnet restore "ArticleService/ArticleService.csproj"

# Copy source and build
COPY ["ArticleService/", "ArticleService/"]
COPY ["ArticleDatabase/", "ArticleDatabase/"]
COPY ["Monitoring/", "Monitoring/"]
WORKDIR "/src/ArticleService"
RUN dotnet build "ArticleService.csproj" -c Release -o /app/build

# Publish stage
RUN dotnet publish "ArticleService.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage - uses slim runtime
FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "ArticleService.dll", "migrate"]
```

**What this guarantees:**
- ✅ .NET 9.0 exactly (same version everywhere)
- ✅ Same Linux base (aspnet:9.0 image)
- ✅ Same build process (defined in Dockerfile)
- ✅ Same runtime (aspnet runtime image)

---

### Multi-Stage Build Pattern

```
Stage 1: BUILD                     Stage 2: RUNTIME
┌────────────────────────┐         ┌────────────────────────┐
│ FROM sdk:9.0           │         │ FROM aspnet:9.0        │
│                        │         │                        │
│ - Full SDK (~800 MB)   │         │ - Runtime only (~200MB)│
│ - Compilers            │         │ - Just DLLs needed     │
│ - Build tools          │   ──→   │ - Minimal attack       │
│ - Source code          │ COPY    │   surface              │
│                        │         │                        │
│ Output: /app/publish   │         │ Small, secure, fast    │
└────────────────────────┘         └────────────────────────┘
```

---

### Docker Compose for Local Development

```yaml
# docker-compose.yml (simplified)
networks:
  HHL:
    driver: bridge

services:
  # Database (same in dev and prod)
  global-article-db:
    image: mcr.microsoft.com/mssql/server:2017-latest-ubuntu
    environment:
      SA_PASSWORD: "Pazzw0rd2025"
      ACCEPT_EULA: "Y"
      MSSQL_PID: Express
    ports:
      - "1433:1433"
    networks:
      - HHL

  # ArticleService
  article-service:
    build:
      context: .
      dockerfile: ArticleService/Dockerfile
    environment:
      - ConnectionStrings__GlobalArticleDb=Server=global-article-db;...
      - ASPNETCORE_ENVIRONMENT=Development
    ports:
      - "5001:8080"
    depends_on:
      - global-article-db
    networks:
      - HHL
```

**Developer workflow:**
```bash
docker-compose up  # Everything starts, configured correctly
# Works the same on any machine with Docker
```

---

### Docker Swarm for Production

```yaml
# docker-compose.swarm.yml (overlay on base compose)
networks:
  HHL:
    driver: overlay  # ← Swarm networking

services:
  article-service:
    deploy:
      replicas: 3              # ← 3 instances
      endpoint_mode: vip       # ← Load balancing
      update_config:
        parallelism: 1         # ← Rolling updates
        delay: 10s
      restart_policy:
        condition: on-failure  # ← Auto-recovery
```

**Production deployment:**
```bash
docker stack deploy -c docker-compose.yml -c docker-compose.swarm.yml happyheadlines
```

---

### The Consistency Flow

```
┌──────────────────────────────────────────────────────────────────┐
│                        DOCKERFILE                                │
│   (Single source of truth for environment)                       │
└─────────────────────────────┬────────────────────────────────────┘
                              │
                              ▼
                    docker build -t app:v1.0
                              │
                              ▼
                    ┌─────────────────┐
                    │  Docker Image   │
                    │  (Immutable)    │
                    └────────┬────────┘
                             │
        ┌────────────────────┼────────────────────┐
        ▼                    ▼                    ▼
┌──────────────┐    ┌──────────────┐    ┌──────────────┐
│  Developer   │    │   Testing    │    │  Production  │
│   Laptop     │    │   Server     │    │   Cluster    │
│              │    │              │    │              │
│ Same image   │    │ Same image   │    │ Same image   │
│ Same behavior│    │ Same behavior│    │ Same behavior│
└──────────────┘    └──────────────┘    └──────────────┘
```

**Key insight:** The image is immutable. What runs in dev IS what runs in prod.

---

### HappyHeadlines Build Script

```bash
# Scripts/build-all-services.sh
#!/bin/bash
set -e

SERVICES=(
    "Monitoring"
    "ArticleService"
    "CommentService"
    "DraftService"
    "ProfanityService"
    "PublisherService"
    "NewsletterService"
    "SubscriberService"
)

for service in "${SERVICES[@]}"; do
    echo "Building $service..."
    docker build -t ${service,,}-service:latest -f ./$service/Dockerfile .
    if [ $? -eq 0 ]; then
        echo "✓ $service built successfully"
    else
        echo "✗ $service build failed"
        exit 1
    fi
done
```

**Result:** 8 services, all built consistently, same command everywhere.

---

## Part 3: Diskuter fordele og ulemper ved at bruge Docker i en stor skalerbar applikation

### Advantages of Docker for Large-Scale Applications

#### 1. Consistent Environments

```
Without Docker:
"It works on my machine" → Days debugging environment differences

With Docker:
Same image everywhere → "It works" means it actually works
```

**HappyHeadlines example:** All 8 services use identical .NET 9.0 runtime, guaranteed by Dockerfile.

---

#### 2. Rapid Scaling

```yaml
# Scale from 3 to 10 instances instantly
docker service scale happyheadlines_article-service=10
```

**Time to scale:**
- VM: Minutes to hours (provision, install OS, configure)
- Container: Seconds (just start another instance)

---

#### 3. Resource Efficiency

```
VM Approach:                    Container Approach:
┌────────────────────┐          ┌────────────────────┐
│ 8 VMs × 2GB OS     │          │ 8 containers       │
│ = 16GB just for OS │          │ = ~800MB total     │
│                    │          │                    │
│ 8 × 30sec boot     │          │ 8 × 2sec start     │
│ = 4 min startup    │          │ = 16sec startup    │
└────────────────────┘          └────────────────────┘
```

**HappyHeadlines:** 8 services + 8 databases + monitoring = many containers, still runs on one laptop.

---

#### 4. Isolation Between Services

```
┌─────────────────┐  ┌─────────────────┐  ┌─────────────────┐
│ ArticleService  │  │ CommentService  │  │ ProfanityService│
│                 │  │                 │  │                 │
│ .NET 9.0        │  │ .NET 9.0        │  │ .NET 9.0        │
│ Port 8080       │  │ Port 8080       │  │ Port 8080       │
│ Own filesystem  │  │ Own filesystem  │  │ Own filesystem  │
└─────────────────┘  └─────────────────┘  └─────────────────┘
        ↑                    ↑                    ↑
        └────────── Each thinks it's alone ──────┘
```

No port conflicts, no dependency conflicts, no shared state leaks.

---

#### 5. Rolling Updates (Zero Downtime)

```yaml
# docker-compose.swarm.yml
update_config:
  parallelism: 1   # One at a time
  delay: 10s       # Wait between updates
```

**Deployment flow:**
```
Instance 1: v1.0 → (updating) → v1.1
Instance 2: v1.0 → serving traffic ← users don't notice
Instance 3: v1.0 → serving traffic

Instance 1: v1.1 ← serving traffic
Instance 2: v1.0 → (updating) → v1.1
Instance 3: v1.0 → serving traffic ← still serving

... zero downtime achieved
```

---

#### 6. Infrastructure as Code

```dockerfile
# The Dockerfile IS the documentation
FROM mcr.microsoft.com/dotnet/aspnet:9.0  # ← Base image
WORKDIR /app                               # ← Working directory
COPY --from=build /app/publish .           # ← What's included
ENTRYPOINT ["dotnet", "ArticleService.dll"]# ← How to run
```

- Version controlled
- Reviewable
- Repeatable
- Self-documenting

---

### Disadvantages of Docker for Large-Scale Applications

#### 1. Operational Complexity

```
Monolith:                       Microservices in Docker:
┌─────────────────┐             ┌───────────────────────────────┐
│ 1 deployment    │             │ 8 services × (build, deploy,  │
│ 1 log file      │             │   monitor, network, volumes)  │
│ 1 thing to      │             │                               │
│   monitor       │             │ Docker Compose/Swarm config   │
└─────────────────┘             │ Orchestration                 │
                                │ Service discovery             │
                                │ Network troubleshooting       │
                                └───────────────────────────────┘
```

**Real issue:** More moving parts = more things that can break.

---

#### 2. Networking Complexity

```
Question: Why can't ServiceA reach ServiceB?

Checklist:
□ Are they on the same network?
□ Is the port exposed?
□ Is the service name correct?
□ Is DNS resolving?
□ Is the service actually running?
□ Is the health check passing?
□ Is there a firewall rule?
```

**HappyHeadlines:** Uses Docker network `HHL` to connect services, but debugging network issues is harder than localhost.

---

#### 3. State Management Challenges

```
Container Philosophy: Containers are ephemeral (can be destroyed anytime)
Reality: Databases have state that must persist

Solution:
┌──────────────────────────────────────┐
│            Docker Volume             │
│  (Persists beyond container life)    │
└───────────────────┬──────────────────┘
                    │ mounted into
                    ▼
┌──────────────────────────────────────┐
│       Database Container             │
│   /var/opt/mssql → volume            │
└──────────────────────────────────────┘
```

```yaml
# HappyHeadlines volume configuration
volumes:
  mssql-global:      # Persists article data
  mssql-comment:     # Persists comment data
  mssql-profanity:   # Persists profanity data
  # ... 8 volumes total
```

**Challenge:** Volume management, backups, data migration.

---

#### 4. Security Considerations

```
VM Isolation:                    Container Isolation:
┌─────────────────┐              ┌─────────────────┐
│ Full OS barrier │              │ Shared kernel   │
│ Hypervisor      │              │ namespaces      │
│ Hardware-level  │              │ cgroups         │
└─────────────────┘              └─────────────────┘
    Very strong                     Good, not perfect
```

**Risks:**
- Kernel vulnerability affects all containers
- Misconfigured container can access host
- Image vulnerabilities (supply chain)

**Mitigations:**
- Run as non-root user
- Use minimal base images (aspnet:9.0, not ubuntu)
- Scan images for vulnerabilities
- Don't run privileged containers

---

#### 5. Learning Curve

```
Skills needed:
┌────────────────────────────────────────┐
│ Dockerfiles                            │
│ Docker Compose                         │
│ Docker Swarm / Kubernetes              │
│ Networking (overlay, bridge)           │
│ Volumes                                │
│ Health checks                          │
│ Logging aggregation                    │
│ Distributed tracing                    │
│ Service discovery                      │
│ Secrets management                     │
└────────────────────────────────────────┘
           ↑
    Steep learning curve
```

---

#### 6. Build Time Overhead

```
Local development without Docker:
  dotnet run               # ~5 seconds

Local development with Docker:
  docker build ...         # ~30-60 seconds (first time)
  docker-compose up        # ~10 seconds

Cache helps, but still slower iteration loop.
```

**Mitigation:** Development mode mounts source code, only use full Docker for integration testing.

---

### Trade-off Summary

| Factor | Docker Advantage | Docker Disadvantage |
|--------|------------------|---------------------|
| **Consistency** | ✅ Same everywhere | |
| **Scaling** | ✅ Seconds to scale | |
| **Resources** | ✅ Lightweight | |
| **Isolation** | ✅ Service isolation | ❌ Not as strong as VMs |
| **Complexity** | | ❌ More moving parts |
| **Networking** | | ❌ Harder to debug |
| **State** | | ❌ Volume management |
| **Learning** | | ❌ Steep curve |
| **Build time** | | ❌ Slower iteration |

---

### HappyHeadlines: Worth It?

**Yes, because:**

1. **8 microservices** → Need consistent deployment
2. **Scaling requirement** → Docker Swarm with replicas
3. **Team development** → Everyone gets same environment
4. **CI/CD** → Automated builds and deploys
5. **Multiple databases** → Easy to spin up per-service DBs

**The trade-offs are manageable because:**
- Scripts automate complexity (`build-all-services.sh`, `deploy-swarm.sh`)
- Docker Compose abstracts networking
- Volumes handle persistence
- Monitoring (Seq, Zipkin) addresses observability

---

### The Bottom Line

```
Docker is like power tools:

- Takes time to learn
- More setup than hand tools
- But once set up:
  - Faster
  - More consistent
  - More scalable
  - Worth the investment for large projects
```

**For HappyHeadlines:**
```
Without Docker:
  "How do I set up 8 services, 8 databases, RabbitMQ, Seq, Zipkin?"
  → Hours of documentation, still fails

With Docker:
  docker-compose up
  → Everything works, every time
```

