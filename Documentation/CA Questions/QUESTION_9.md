# Question 9: Organisatorisk Skalering (Organizational Scaling)

## Part 1: Challenges When Teams Grow

### The Problem: Small Team → Large Organization

| Aspect | Small Team (5-8) | Large Organization (50+) |
|--------|------------------|--------------------------|
| **Communication** | Face-to-face, informal | Formal meetings, documentation |
| **Decision making** | Quick, consensus | Slow, hierarchical |
| **Code ownership** | Everyone knows everything | Specialized silos |
| **Coordination** | Natural, ad-hoc | Requires process/tools |
| **Onboarding** | Pair with anyone | Formal training needed |

---

### Key Challenges

#### 1. Communication Overhead (Brooks's Law)

> "Adding manpower to a late software project makes it later."

**Why?**
- Communication paths = n(n-1)/2
- 5 people = 10 paths
- 20 people = 190 paths
- 50 people = 1,225 paths

**More people = exponentially more coordination overhead.**

---

#### 2. Conway's Law

> "Organizations design systems that mirror their communication structure."

**Example from HappyHeadlines:**
```
Team Structure              →    System Architecture
┌─────────────────┐              ┌─────────────────┐
│ Article Team    │      →       │ ArticleService  │
├─────────────────┤              ├─────────────────┤
│ Comment Team    │      →       │ CommentService  │
├─────────────────┤              ├─────────────────┤
│ Newsletter Team │      →       │ NewsletterService│
└─────────────────┘              └─────────────────┘
```

**Implication:** If you want microservices, you need independent teams.

---

#### 3. Knowledge Silos

| Small Team | Large Organization |
|------------|-------------------|
| "Ask anyone" | "Who owns this?" |
| Shared context | Tribal knowledge |
| Bus factor = team size | Bus factor = 1 per service |

**Risk:** Key person leaves → knowledge lost.

---

#### 4. Consistency vs Autonomy

| Approach | Pros | Cons |
|----------|------|------|
| **Strict standards** | Consistency, quality | Slow, bureaucratic |
| **Team autonomy** | Fast, innovative | Inconsistent, tech debt |

**Balance needed:** Core standards + team freedom on implementation details.

---

## Part 2: Kotter's 8-Step Model of Change

### Why Kotter? 

Scaling an organization IS a change process. Kotter (1996) studied 100 organizations going through change and identified 8 steps for success.

---

### The 8 Steps

```
┌─────────────────────────────────────────────────────────────┐
│  8. Anchor Change in Culture                                │
├─────────────────────────────────────────────────────────────┤
│  7. Consolidate Gains                                       │
├─────────────────────────────────────────────────────────────┤
│  6. Create Short-Term Wins                                  │
├─────────────────────────────────────────────────────────────┤
│  5. Remove Obstacles & Empower                              │
├─────────────────────────────────────────────────────────────┤
│  4. Communicate the Vision                                  │
├─────────────────────────────────────────────────────────────┤
│  3. Develop Vision & Strategy                               │
├─────────────────────────────────────────────────────────────┤
│  2. Form Guiding Coalition                                  │
├─────────────────────────────────────────────────────────────┤
│  1. Create Sense of Urgency                                 │
└─────────────────────────────────────────────────────────────┘
```

---

### Step-by-Step Breakdown

#### Step 1: Create a Sense of Urgency

**What:** Make people understand WHY change is needed NOW.

**How:**
- Identify threats if we DON'T change
- Show opportunities we're missing
- Honest dialogue about current pain points

**HappyHeadlines Example:**
> "We're a team of 5 managing 8 services. We can't scale. New features take 3x longer because everyone is context-switching. If we don't restructure, we'll miss market opportunities."

---

#### Step 2: Form a Guiding Coalition

**What:** Assemble a team with power to lead the change.

**How:**
- Find change champions across teams
- Include influential people from different levels
- Ensure coalition has authority to make decisions

**HappyHeadlines Example:**
| Role | Person | Why Included |
|------|--------|--------------|
| Tech Lead | Senior dev | Technical credibility |
| Product Owner | PM | Business perspective |
| Team Lead | Each team | Ground-level buy-in |
| Ops/DevOps | Platform eng | Infrastructure impact |

---

#### Step 3: Develop Vision & Strategy

**What:** Clear picture of the future state.

**How:**
- Define core values that won't change
- Describe the end state clearly
- Strategy = how we get there

**HappyHeadlines Example:**
> **Vision:** "5 autonomous teams, each owning 1-2 services end-to-end, deploying independently, with shared standards for monitoring and messaging."
>
> **Strategy:** Conway's Law in reverse - structure teams to match desired architecture.

---

#### Step 4: Communicate the Vision

**What:** Repeat the vision until everyone can recite it.

**How:**
- Communicate often and through multiple channels
- Connect vision to daily work
- Address concerns honestly

**HappyHeadlines Example:**
- All-hands meeting explaining the change
- Slack channel for questions
- Vision posted in README.md
- Mention in every sprint retro

---

#### Step 5: Remove Obstacles & Empower

**What:** Clear the path for people to act on the vision.

**How:**
- Identify structural barriers
- Deal with resisters
- Reward early adopters

**HappyHeadlines Example:**

| Obstacle | Solution |
|----------|----------|
| Shared database | Split into service-owned DBs |
| Monolithic CI/CD | Per-service pipelines |
| "That's not my job" | Clear ownership docs |
| Fear of change | Pair programming across teams |

---

#### Step 6: Create Short-Term Wins

**What:** Visible improvements early → builds momentum.

**How:**
- Pick easy wins first
- Make success visible
- Reward contributors

**HappyHeadlines Example:**
- Week 2: Article Team deploys independently ✅
- Week 4: First cross-team code review completed ✅
- Week 6: Platform Team sets up shared monitoring ✅

**Celebrate each win publicly!**

---

#### Step 7: Consolidate Gains

**What:** Build on success, don't declare victory too early.

**How:**
- Analyze what worked
- Apply learnings to next teams
- Keep pushing for more change

**HappyHeadlines Example:**
- Article Team pilot successful → Roll out to Comment Team
- Document lessons learned
- Refine the process based on feedback

---

#### Step 8: Anchor Change in Culture

**What:** Make the new way "just how we do things."

**How:**
- Tell success stories
- Embed in onboarding
- Leaders model the behavior

**HappyHeadlines Example:**
- New hire onboarding: "Each team owns their service"
- Architecture Decision Records (ADRs) document why
- Retros include "how does this align with our team structure?"

---

### Kotter Model: Pros & Cons

| Advantages | Disadvantages |
|------------|---------------|
| ✅ Clear step-by-step process | ❌ Skipping steps causes problems |
| ✅ Emphasizes employee buy-in | ❌ Time-consuming (months) |
| ✅ Focus on preparation before change | ❌ Top-down, limited co-creation |
| ✅ Easy to communicate | ❌ Can frustrate if rushed |

---

## Part 3: Risk Analysis - Scrum → Scaled Framework

### Scenario: Switching from Scrum to SAFe/Spotify/LeSS

#### Risk Analysis Template

| Risk | Probability | Impact | Mitigation |
|------|-------------|--------|------------|
| **Training costs** | High | Medium | Budget for certifications, workshops |
| **Productivity dip** | High | High | Gradual rollout, pilot team first |
| **Resistance to change** | Medium | High | Change champions, clear communication |
| **Framework mismatch** | Medium | Critical | Assess culture fit before choosing |
| **Over-engineering process** | Medium | Medium | Start light, add only what's needed |
| **Loss of agility** | Medium | High | Preserve retrospectives, adapt framework |

---

### Detailed Risk Breakdown

#### Risk 1: Productivity Dip During Transition

**Cause:** Learning new ceremonies, roles, artifacts  
**Duration:** 3-6 months typically  
**Mitigation:**
- Pilot with one team first
- Keep delivering during transition
- Don't change everything at once

---

#### Risk 2: Framework Mismatch

**Cause:** Choosing SAFe when you need Spotify, or vice versa

| Framework | Best For | Avoid If |
|-----------|----------|----------|
| **SAFe** | Large enterprises, compliance-heavy | Small/medium orgs, startup culture |
| **Spotify** | Tech companies, autonomous teams | Hierarchical cultures |
| **LeSS** | Product-focused, fewer teams | Many products, many stakeholders |
| **Nexus** | 3-9 teams on one product | More than 9 teams |

**Mitigation:** Culture assessment before choosing.

---

#### Risk 3: Resistance to Change

**Symptoms:**
- "We've always done it this way"
- Passive-aggressive compliance
- Shadow processes (old way in secret)

**Mitigation:**
- Involve people in the decision
- Show the "why" (pain points being solved)
- Change champions in each team
- Celebrate early wins

---

#### Risk 4: Over-Engineering Process

**Symptoms:**
- More time in meetings than coding
- 47 Jira ticket types
- "Process police" role emerges

**Mitigation:**
- Start with minimum viable process
- Add ceremonies/artifacts only when needed
- Regular process retrospectives
- "If it's not helping, stop doing it"

---

## Part 4: Go/No-Go Decision Framework

### Using Risk Analysis for Decision

#### Step 1: Score Each Risk

| Risk | Probability (1-5) | Impact (1-5) | Score (P × I) |
|------|-------------------|--------------|---------------|
| Productivity dip | 4 | 4 | 16 |
| Resistance | 3 | 4 | 12 |
| Framework mismatch | 2 | 5 | 10 |
| Over-engineering | 3 | 3 | 9 |
| Training costs | 5 | 2 | 10 |
| **Total Risk Score** | | | **57** |

---

#### Step 2: Define Thresholds

| Score Range | Decision |
|-------------|----------|
| 0-30 | **GO** - Low risk, proceed |
| 31-50 | **CONDITIONAL GO** - Proceed with mitigations |
| 51-70 | **PILOT FIRST** - Test with one team |
| 71+ | **NO-GO** - Too risky, reconsider |

---

#### Step 3: Apply to Our Example

**Score: 57 → PILOT FIRST**

**Decision:** 
1. Select one team for pilot (3-month trial)
2. Define success metrics upfront
3. Keep other teams on current process
4. Review after pilot, then decide full rollout

---

### Go/No-Go Checklist

| Criterion | ✅ or ❌ |
|-----------|---------|
| Clear problem statement (why change?) | |
| Leadership buy-in | |
| Budget for training/tools | |
| Pilot team identified | |
| Success metrics defined | |
| Rollback plan if it fails | |
| Change champions assigned | |
| Timeline realistic (6+ months) | |

**Rule:** If more than 2 ❌, it's a NO-GO.

---

## HappyHeadlines Context

### Current State (Small Team)
- One team owns everything
- Direct communication
- Shared code ownership
- Fast decisions

### If Scaling to 5 Teams

**Proposed Structure (Conway's Law applied):**

| Team | Owns | Services |
|------|------|----------|
| Article Team | Content publishing | ArticleService, PublisherService |
| Engagement Team | User interaction | CommentService, ProfanityService |
| Subscriber Team | Subscriptions | SubscriberService, NewsletterService |
| Platform Team | Infrastructure | Monitoring, databases, RabbitMQ |
| Draft Team | Content creation | DraftService |

**Risks specific to HappyHeadlines:**
- Breaking shared Monitoring library
- Database schema changes across teams
- RabbitMQ message contract changes

---

## 15-Minute Presentation Structure

| Time | Topic | Content |
|------|-------|---------|
| 0-2 | Growth Challenges | Brooks's Law, communication paths |
| 2-3 | Conway's Law | Team structure mirrors architecture |
| 3-5 | Kotter Introduction | Why use a change model? 8 steps overview |
| 5-8 | Kotter Steps 1-4 | Urgency → Coalition → Vision → Communicate |
| 8-10 | Kotter Steps 5-8 | Remove obstacles → Wins → Consolidate → Anchor |
| 10-12 | Risk Analysis | Scoring framework + Go/No-Go thresholds |
| 12-14 | HappyHeadlines Example | How we'd apply Kotter to scale to 5 teams |
| 14-15 | Pros/Cons + Wrap-up | When Kotter works, when it doesn't |

---

## Sources & References

### Kotter's 8-Step Model
- **Kotter, J.P. (1996).** *Leading Change.* Harvard Business School Press.
- **Kotter, J.P. (2012).** *Leading Change, With a New Preface by the Author.* Harvard Business Review Press. ISBN: 978-1422186435
- **Rose, K. (2002).** "Leading Change: A Model by John Kotter." *ESI Horizons*, Vol. 4, No. 3.

### Risk Analysis Frameworks
- **PMI (2017).** *A Guide to the Project Management Body of Knowledge (PMBOK® Guide)* – Sixth Edition. Project Management Institute.
  - Chapter 11: Project Risk Management
  - Probability × Impact Matrix (P×I scoring method)
  
- **Boehm, B.W. (1991).** "Software Risk Management: Principles and Practices." *IEEE Software*, 8(1), pp. 32-41.
  - Risk exposure = Probability × Loss

- **ISO 31000:2018.** *Risk Management – Guidelines.* International Organization for Standardization.

### Go/No-Go Decision Frameworks
- **Cooper, R.G. (2008).** "Perspective: The Stage-Gate® Idea-to-Launch Process—Update, What's New, and NexGen Systems." *Journal of Product Innovation Management*, 25(3), pp. 213-232.
  - Stage-Gate model with Go/Kill decision points

- **NASA (2007).** *NASA Systems Engineering Handbook.* NASA/SP-2007-6105.
  - Decision Gate Reviews (Go/No-Go criteria)

### Scaling Frameworks Comparison
- **SAFe:** Leffingwell, D. (2018). *SAFe 4.5 Reference Guide.* Addison-Wesley.
- **Spotify Model:** Kniberg, H. & Ivarsson, A. (2012). "Scaling Agile @ Spotify." Spotify Labs whitepaper.
- **LeSS:** Larman, C. & Vodde, B. (2016). *Large-Scale Scrum: More with LeSS.* Addison-Wesley.
- **Nexus:** Schwaber, K. (2015). *The Nexus Guide.* Scrum.org.

### Brooks's Law & Conway's Law
- **Brooks, F.P. (1975).** *The Mythical Man-Month: Essays on Software Engineering.* Addison-Wesley.
- **Conway, M.E. (1968).** "How Do Committees Invent?" *Datamation*, 14(4), pp. 28-31.

