# REPORT OCTOBER SIXTH - IMPLEMENTED GREEN ARCHITECTURE

## WHAT HAS BEEN DONE

### 1. in-memory caching

ArticleService has been a real disgusting eye-sore. 
Aside from the multiple replicas that are constantly running,
it has been constantly making heavy repeated calls to database.
This was originally solved by adding a Redis cache,
but it still required a lot of network traffic.

In comes our in-memory caching solution. 
Now articles are cached in memory for each replica, and limited to a specific size,
currently set to 100 articles. This drastically reduces network traffic,
and with the few additional checks, it should give us a negligible performance hit in exchange
for a much greener architecture.

The Github Copilot, with whom I have been developing an unhealthy relationship,
has judged the benefits to be in the ballpark of:

- 50-70% of requests served from local memory (0 network hops)
- 20-30% served from Redis (1 network hop)
- 10% require database query (authoritative source)

A drastic improvement, but could realistically be a simple hallucination.

These changes represent the green software architecture principle of "Fetch Data From Proximity".

### 2. Brotli compression for Redis caching

In the same vein, we have implemented compression for caching in Redis.
Brotli compression has been a part of the .NET ecosystem for a while now,
and since I don't really know the first thing about Deflate and gzip, I just 
went with it.
Judging by Microsoft's own documentation (albeit a bit old),
Brotli broadly seems to outperform its peers in both ratio and speed.

This change once again comes with a small performance hit,
but very dramatically reduces the size of payloads, and therefore 
the cost of network traffic.

Once more, the Github Copilot has estimated the benefits. Take these numbers with a grain of salt:
- Redis payload compression using Brotli
- 60-70% reduction in cache network traffic
- ~400 MB/day saved per million requests


## WHERE IS THE EVIDENCE

All of these changes can be found in ```ArticleService/Services/ArticleAppService.cs```,
and now have coverage in the unit tests located in 
```ArticleService.Tests/Services/ArticleAppServiceTests.cs```.

The esoteric AI has written most of the tests, and though kept on a short leash,
has taken its own liberties in writing them. I have run and verified its work,
but my slipping grip on reality may have influenced its judgement.
