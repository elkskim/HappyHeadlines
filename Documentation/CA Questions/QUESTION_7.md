# Question 7: Specifikationer (Specifications)

## Part 1: What is a Functional Specification?

### The Bjarne Stroustrup Wisdom

> "The most important single aspect of software development is to be clear about what you are trying to build."

**Specification-first approach:** Understand WHAT before HOW.

---

### The Contract Metaphor

A specification is a **contract** between:
- **Server** (the function) - offers a service
- **Client** (the caller) - uses the service

The contract has two parts:

| Part | Description | Expressed As |
|------|-------------|--------------|
| **Pre-condition** | What client MUST fulfil before calling | Predicate on inputs |
| **Post-condition** | What server guarantees after execution | Predicate on result |

---

### Formal vs Informal

| Aspect | Informal Spec | Formal Spec |
|--------|--------------|-------------|
| **Format** | Plain text, natural language | Predicate logic, pre/post-conditions |
| **Precision** | Ambiguous, open to interpretation | Mathematically precise |
| **Outsourcing** | "That's not what I meant!" | "It doesn't satisfy the post-condition" |
| **Testing** | Hard to derive test cases | Test cases derived directly from spec |
| **Maintenance** | Easy to write, hard to verify | Harder to write, easy to verify |

---

## Part 2: Informal vs Formal Specification Examples

### Informal Specification (Plain Text)

```csharp
interface IArticleService
{
    // Returns the article with the given ID, or null if not found
    Article? GetArticle(int id);
    
    // Returns all articles from the specified region
    IEnumerable<Article> GetArticlesByRegion(string region);
    
    // Compresses input string to byte array
    byte[] Compress(string input);
}
```

**Problems with informal specs:**
- What if `id` is negative? Behavior undefined.
- What if `region` is null or empty? Undefined.
- What algorithm for compression? Unspecified.
- No testable criteria.

---

### Formal Specification (Pre/Post-Conditions)

```csharp
interface IArticleService
{
    /* Pre: id > 0
       Post: (result = null AND ¬∃article in DB: article.Id = id)
             OR (result.Id = id AND result ∈ DB) */
    Article? GetArticle(int id);
    
    /* Pre: region ≠ null AND region.Length > 0
       Post: ∀article in result: article.Region = region
             AND ∀article in DB: article.Region = region => article ∈ result */
    IEnumerable<Article> GetArticlesByRegion(string region);
    
    /* Pre: input ≠ null
       Post: Decompress(result) = input 
             AND result.Length ≤ input.Length (compression achieved) */
    byte[] Compress(string input);
}
```

**What makes this formal:**
- Pre-conditions define valid inputs
- Post-conditions define exact behavior using predicates
- Universal (∀) and existential (∃) quantifiers remove ambiguity
- Test cases can be derived directly from spec

---

## Part 3: HappyHeadlines Formal Specification Example

### The ICompressionService Interface

```csharp
/// <summary>
/// Compresses and decompresses data for cache storage.
/// </summary>
public interface ICompressionService
{
    /* Pre: input ≠ null
       Post: Decompress(result) = input */
    byte[] Compress(string input);
    
    /* Pre: compressed ≠ null AND compressed was produced by Compress()
       Post: result = original string before compression */
    string Decompress(byte[] compressed);
    
    /* Pre: compressedSize > 0
       Post: result = originalSize / compressedSize
             AND result > 1.0 implies compression was beneficial */
    double CalculateCompressionRatio(int originalSize, int compressedSize);
}
```

**Deriving Tests from Post-Conditions:**

```csharp
[Fact]
public void Compress_Decompress_ReturnsOriginalString()
{
    // This test DIRECTLY from post-condition: Decompress(Compress(x)) = x
    var original = "Hello, World!";
    var compressed = _service.Compress(original);
    var result = _service.Decompress(compressed);
    
    Assert.Equal(original, result); // Post-condition verified
}

[Fact]
public void CalculateCompressionRatio_ReturnsCorrectValue()
{
    // Post-condition: result = originalSize / compressedSize
    var ratio = _service.CalculateCompressionRatio(1000, 250);
    
    Assert.Equal(4.0, ratio); // 1000/250 = 4.0
}
```

---

### Pre-Condition Violations → Exceptions

```csharp
[Fact]
public void Compress_NullInput_ThrowsArgumentNullException()
{
    // Pre-condition: input ≠ null
    // When pre-condition violated → throw exception
    Assert.Throws<ArgumentNullException>(() => _service.Compress(null));
}
```

**Rule from the note:** "Throw exceptions when pre-condition is not met"

---

### The Data Contract (Article Model)

```csharp
public class Article
{
    public int Id { get; set; }
    public string Title { get; set; }
    public string Content { get; set; }
    public string Author { get; set; }
    public DateTime Created { get; set; }
    public string Region { get; set; } = "Global";
}
```

**Formal because:**
- Exact field names
- Exact types
- Default values specified
- JSON serialization is deterministic

---

### Message Contract (RabbitMQ)

```csharp
// From ArticleConsumer.cs
var article = JsonSerializer.Deserialize<Article>(json);
```

**The implicit contract:**
- Message body is JSON
- JSON matches Article class structure
- If it doesn't match → deserialization fails → clear error

---

## Part 4: Why Formal Specs Matter for Outsourcing

### Benefits of Formal Specification

1. **Precise Contract**
   - When outsourcing to a colleague or another company
   - Implementation requirements are perfectly clear
   - No "that's not what I meant" disputes

2. **Separation of Concerns**
   - When specifying: 100% focused on WHAT to build
   - When implementing: 100% focused on HOW to build
   - Clean mental separation

3. **Test Cases Derived Directly**
   - Post-condition → test assertion
   - Pre-condition → exception test
   - No guessing what to test

4. **If You Can't Specify It...**
   > "If you have troubles making a formal specification, it might be because you really do not know what to build."

---

## Part 5: Deriving Implementation from Specification

### Example: GetArticlesByRegion

**Formal Spec:**
```
Pre: region ≠ null AND region.Length > 0
Post: ∀article in result: article.Region = region
      AND ∀article in DB: article.Region = region => article ∈ result
```

**Implementation derived from spec:**

```csharp
public IEnumerable<Article> GetArticlesByRegion(string region)
{
    // Pre-condition check (throw if violated)
    if (string.IsNullOrEmpty(region))
        throw new ArgumentException("Region cannot be null or empty");
    
    // Post-condition: ∀article in result: article.Region = region
    // This implies: filter by region
    // Post-condition: ∀article in DB where Region = region => article ∈ result
    // This implies: return ALL matching articles
    
    return _context.Articles
        .Where(a => a.Region == region)  // Satisfies both post-conditions
        .ToList();
}
```

**The universal quantifier (∀) in the post-condition implies iteration/filtering in implementation.**

---

## Part 6: Informal vs Formal Side-by-Side

#### Informal Specification (Email/Slack)

> "Hey, we need an article service. It should store articles with titles, content, and author info. Make sure it caches stuff for performance. Use compression maybe? Thanks!"

**Problems:**
- What's the cache strategy? (TTL? LRU? Distributed?)
- "Compression maybe" - which algorithm? When?
- "Author info" - just name? Or email? ID?
- No testable acceptance criteria

---

#### Formal Specification (Pre/Post-Conditions)

```csharp
public interface IArticleAppService
{
    /* Pre: id > 0 AND region ≠ null
       Post: (result = null AND ¬∃article in DB: article.Id = id)
             OR (result.Id = id AND result ∈ Cache OR result ∈ DB)
       Cache policy: L1 (memory, 5min) → L2 (Redis, 14d) → L3 (database) */
    Task<Article?> GetArticleAsync(int id, string region, CancellationToken ct = default);
    
    /* Pre: article ≠ null AND article.Title ≠ null AND region ≠ null
       Post: result.Id > 0 AND result ∈ DB AND result ∈ Redis */
    Task<Article> CreateArticleAsync(Article article, string region, CancellationToken ct = default);
}

// Test derived from post-condition
[Fact]
public async Task GetArticle_ReturnsFromCache_WhenCached()
{
    // Post-condition: result ∈ Cache OR result ∈ DB
    // Test: when in cache, should return from cache (not DB)
    _mockCache.Setup(c => c.GetArticle(1, "Europe"))
        .Returns(new Article { Id = 1, Title = "Test" });
    
    var result = await _service.GetArticleAsync(1, "Europe", default);
    
    Assert.Equal("Test", result.Title);
    _mockDb.Verify(d => d.FindAsync(1), Times.Never); // Did NOT hit DB
}
```

**Advantages:**
- ✅ Caching behavior is explicit (L1 → L2 → L3)
- ✅ TTLs are documented (5min, 14d)
- ✅ Test proves implementation is correct
- ✅ Vendor passes test = vendor gets paid

---

## Comparison Summary

| Criteria | Informal | Formal |
|----------|----------|--------|
| **Speed to write** | Fast ⚡ | Slower 🐢 |
| **Precision** | Vague | Exact |
| **Disputes** | "That's not what I meant" | "It doesn't pass the test" |
| **Maintenance cost** | Low initially, high later | Higher initially, low later |
| **Outsourcing safety** | Risky | Safe |
| **Legal defensibility** | Weak | Strong |
| **Automated verification** | Impossible | Unit tests, integration tests |

---

## HappyHeadlines Specifications Used

### 1. Interface Contracts
```
IArticleAppService      → Article CRUD with caching
ICompressionService     → Data compression for cache
IResilienceService      → Fault-tolerant external calls
IFeatureToggleService   → Feature flag management
```

### 2. Data Contracts
```
Article         → News article structure
Comment         → User comment structure  
Profanity       → Banned word definition
Subscriber      → Newsletter subscriber
```

### 3. Message Contracts
```
articles.exchange (Fanout)  → Broadcast new articles
comments.exchange           → Comment notifications
subscriber.exchange         → Subscription events
```

### 4. API Contracts (REST)
```
GET /api/Article/{region}   → List articles
POST /api/Article           → Create article
GET /api/Comment/{id}       → Get comments for article
```

---

## Key Takeaways

1. **Formal specs are contracts** - Both legal and technical
2. **Interfaces are outsourcing-ready** - Give vendor the interface, get implementation back
3. **Tests prove compliance** - No arguing about "what you meant"
4. **Microservices need formal specs** - Services communicate via contracts, not conversations
5. **Investment pays off** - More work upfront, fewer problems later

---

## 15-Minute Presentation Structure

| Time | Slide | Content |
|------|-------|---------|
| 0-2 | Contract Metaphor | Server/Client, Pre/Post-conditions |
| 2-4 | Informal vs Formal | Table comparison, ambiguity issues |
| 4-7 | Formal Examples | ICompressionService with ∀ and ∃ predicates |
| 7-9 | Deriving Tests | Post-condition → Assert, Pre-condition → Exception |
| 9-11 | Deriving Implementation | Universal quantifier → iteration example |
| 11-13 | HappyHeadlines Demo | Show actual interfaces + tests |
| 13-15 | Outsourcing Benefits | Why formal specs win for DLS |

