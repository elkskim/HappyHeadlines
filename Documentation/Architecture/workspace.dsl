workspace "HappyHeadlines" "A distributed microservices-based news platform with fault isolation and feature toggles" {

    !identifiers hierarchical

    model {
        # External Actors
        reader = person "Reader" "Reads articles, posts comments, subscribes to newsletters"
        newsPublisher = person "News Publisher" "Publishes articles to the platform"

        # External Systems
        rabbitmq = softwareSystem "RabbitMQ" "Message broker for async communication" "External"
        mssql = softwareSystem "MS SQL Server" "Relational database cluster (8 regional article databases + 4 centralized service databases)" "External"
        redis = softwareSystem "Redis" "Distributed cache" "External"
        seq = softwareSystem "Seq" "Centralized logging" "External"
        zipkin = softwareSystem "Zipkin" "Distributed tracing" "External"

        # Main System
        happyHeadlines = softwareSystem "HappyHeadlines" "Distributed news platform with regional isolation and feature toggles" {

            # Article Domain (Regional isolation: Africa, Asia, Europe)
            publisherService = container "PublisherService" "Receives articles from publishers and routes to regional queues" "ASP.NET Core 9.0" "ArticleDomain,Service" {
                publisherController = component "PublisherController" "REST API for article submission" "ASP.NET Core Controller"
                publisherMessaging = component "PublisherMessaging" "Routes articles to regional RabbitMQ exchanges" "RabbitMQ Client"
                
                publisherController -> publisherMessaging "Routes article"
            }
            
            articleService = container "ArticleService" "Routes article requests to 8 regional databases. 3 replicas for load balancing. Brotli compression caching." "ASP.NET Core 9.0" "ArticleDomain,Service" {
                articleController = component "ArticleController" "REST API for article retrieval" "ASP.NET Core Controller"
                articleAppService = component "ArticleAppService" "Business logic for article operations" "Service Layer"
                articleRepository = component "ArticleRepository" "Data access with dynamic regional routing" "Repository Pattern"
                compressionService = component "CompressionService" "Brotli compression for cache entries" "Service Layer"
                articleCacheCommander = component "ArticleCacheCommander" "Manages Redis cache lifecycle and metrics" "Hosted Service"
                articleConsumer = component "ArticleConsumer" "Consumes articles from RabbitMQ" "RabbitMQ Consumer"
                articleConsumerHostedService = component "ArticleConsumerHostedService" "Background service for message consumption" "Hosted Service"
                
                articleController -> articleAppService "Calls"
                articleAppService -> articleRepository "Reads/writes"
                articleAppService -> compressionService "Compresses/decompresses"
                articleConsumer -> articleRepository "Persists articles"
                articleConsumerHostedService -> articleConsumer "Manages lifecycle"
            }
            
            articleDb = container "ArticleDatabase" "8 regional instances: Global, Africa, Asia, Europe, N/S America, Oceania, Antarctica. Dynamic routing per request." "MS SQL Server 2017" "Database,ArticleDomain"

            # Comment Domain
            commentService = container "CommentService" "Manages comments with profanity filtering" "ASP.NET Core 9.0" "CommentDomain,Service" {
                commentController = component "CommentController" "REST API for comment management" "ASP.NET Core Controller"
                commentCacheCommander = component "CommentCacheCommander" "Manages Redis cache for comments" "Service Layer"
                resilienceService = component "ResilienceService" "Circuit breaker for ProfanityService (Polly)" "Resilience Layer"
                dbInitializer = component "DbInitializer" "Database migration and seeding" "Infrastructure"
                
                commentController -> commentCacheCommander "Manages comments"
                commentController -> resilienceService "Validates profanity"
            }
            
            commentDb = container "CommentDatabase" "Centralized comment storage across all regions" "MS SQL Server 2017" "Database,CommentDomain"
            profanityService = container "ProfanityService" "Checks text for profanity" "ASP.NET Core 9.0" "CommentDomain,Service"
            profanityDb = container "ProfanityDatabase" "Centralized profanity word storage" "MS SQL Server 2017" "Database,CommentDomain"

            # Draft Domain
            draftService = container "DraftService" "Manages article drafts before publication" "ASP.NET Core 9.0" "DraftDomain,Service"
            draftDb = container "DraftDatabase" "Centralized draft storage" "MS SQL Server 2017" "Database,DraftDomain"

            # Newsletter Domain (Feature Toggle)
            subscriberService = container "SubscriberService" "Manages newsletter subscriptions with runtime feature toggle" "ASP.NET Core 9.0" "NewsletterDomain,Service"
            subscriberDb = container "SubscriberDatabase" "Centralized subscriber storage" "MS SQL Server 2017" "Database,NewsletterDomain"
            newsletterService = container "NewsletterService" "Aggregates content for newsletters" "ASP.NET Core 9.0" "NewsletterDomain,Service"

            # Monitoring Domain
            monitoring = container "Monitoring Service" "Tracks cache metrics and system health" "ASP.NET Core 9.0" "Observability,Service,ArticleDomain"


            # Publisher Service relationships
            publisherService -> rabbitmq "Routes to regional queues" "AMQP"

            # Article Service relationships
            articleService -> rabbitmq "Consumes + publishes events" "AMQP"
            articleService -> articleDb "Reads/writes" "SQL/TDS"
            articleService -> redis "Caches with compression" "Redis Protocol"
            articleService -> monitoring "Reports metrics" "HTTP"

            # Comment Service relationships
            commentService -> commentDb "Reads/writes" "SQL/TDS"
            commentService -> profanityService "Validates (circuit breaker)" "HTTP"
            commentService -> redis "Caches" "Redis Protocol"
            commentService -> monitoring "Reports metrics" "HTTP"

            # Profanity Service relationships
            profanityService -> profanityDb "Reads words" "SQL/TDS"

            # Draft Service relationships
            draftService -> draftDb "Reads/writes" "SQL/TDS"

            # Subscriber Service relationships
            subscriberService -> subscriberDb "Reads/writes" "SQL/TDS"
            subscriberService -> rabbitmq "Publishes events" "AMQP"

            # Newsletter Service relationships
            newsletterService -> rabbitmq "Consumes events" "AMQP"

            # Monitoring Service relationships
            monitoring -> articleService "Collects stats" "HTTP"
            monitoring -> commentService "Collects stats" "HTTP"

            # Component-level relationships for Layer 3 diagrams
            # PublisherService components
            publisherService.publisherMessaging -> rabbitmq "Publishes to regional exchanges" "AMQP"
            
            # ArticleService components
            articleService.articleRepository -> articleDb "SQL queries" "SQL/TDS"
            articleService.articleCacheCommander -> redis "Cache operations" "Redis Protocol"
            articleService.articleConsumer -> rabbitmq "Consumes messages" "AMQP"
            articleService.articleCacheCommander -> monitoring "Reports metrics" "HTTP"
            
            # CommentService components
            commentService.commentCacheCommander -> commentDb "Reads/writes" "SQL/TDS"
            commentService.commentCacheCommander -> redis "Cache operations" "Redis Protocol"
            commentService.resilienceService -> profanityService "HTTP with circuit breaker" "HTTP"

            # Observability relationships
            happyHeadlines -> seq "Logs (Serilog)" "HTTP"
            happyHeadlines -> zipkin "Traces (OpenTelemetry)" "HTTP"

            # Infrastructure relationships - System Context Level
            happyHeadlines -> mssql "Persists data (8 regional + 4 centralized DBs)" "SQL/TDS"
            happyHeadlines -> redis "Caches (Brotli compressed)" "Redis Protocol"
            happyHeadlines -> rabbitmq "Async messaging" "AMQP"

            # Database-level infrastructure
            articleDb -> mssql "8 regional instances" "SQL/TDS"
            commentDb -> mssql "Centralized" "SQL/TDS"
            profanityDb -> mssql "Centralized" "SQL/TDS"
            draftDb -> mssql "Centralized" "SQL/TDS"
            subscriberDb -> mssql "Centralized" "SQL/TDS"
        }

        # User Interactions - System Context Level
        reader -> happyHeadlines "Reads, comments, subscribes" "HTTPS"
        newsPublisher -> happyHeadlines "Publishes articles" "HTTPS"

        live = deploymentEnvironment "Live" {
            deploymentNode "Article Domain" {
                deploymentNode "Services" {
                    containerInstance happyHeadlines.publisherService
                    containerInstance happyHeadlines.articleService
                }
                deploymentNode "Data" {
                    containerInstance happyHeadlines.articleDb
                }
            }

            deploymentNode "Comment Domain" {
                deploymentNode "Services" {
                    containerInstance happyHeadlines.commentService
                    containerInstance happyHeadlines.profanityService
                }
                deploymentNode "Data" {
                    containerInstance happyHeadlines.commentDb
                    containerInstance happyHeadlines.profanityDb
                }
            }

            deploymentNode "Newsletter Domain" {
                deploymentNode "Services" {
                    containerInstance happyHeadlines.subscriberService
                    containerInstance happyHeadlines.newsletterService
                }
                deploymentNode "Data" {
                    containerInstance happyHeadlines.subscriberDb
                }
            }

            deploymentNode "Draft Domain" {
                deploymentNode "Services" {
                    containerInstance happyHeadlines.draftService
                }
                deploymentNode "Data" {
                    containerInstance happyHeadlines.draftDb
                }
            }

            deploymentNode "Shared Infrastructure" {
                softwareSystemInstance rabbitmq
                softwareSystemInstance redis
                softwareSystemInstance seq
                softwareSystemInstance zipkin
                containerInstance happyHeadlines.monitoring
            }
        }

    }

    views {
        # System Context
        systemContext happyHeadlines "SystemContext" {
            include *
            default true
            description "System context for HappyHeadlines with regional isolation, fault tolerance, and runtime feature toggles."
        }

        # Container View - Complete Architecture
        container happyHeadlines "Containers-All" {
            include *
            
            description "Complete container view showing all services, databases, and infrastructure."
        }

        # Container View - Service Mesh
        container happyHeadlines "Containers-Services" {
            include "element.tag==Service"
            include rabbitmq redis seq zipkin
            
            description "Service layer with async messaging (RabbitMQ) and caching (Redis)."
        }

        # Container View - Data Layer
        container happyHeadlines "Containers-Data" {
            include "element.tag==Database"
            include mssql
            
            description "Data architecture: 8 regional article databases + 4 centralized service databases."
        }

        # Container View - Observability
        container happyHeadlines "Containers-Observability" {
            include "element.tag==Service"
            include "element.tag==Observability"
            include seq zipkin
            
            description "Monitoring, logging (Seq/Serilog), and tracing (Zipkin/OpenTelemetry)."
        }

        # Domain Views
        container happyHeadlines "ArticleDomain" {
            include "element.tag==ArticleDomain"
            include "element.tag==Observability"
            include rabbitmq redis
            
            description "3 replicas, 8 regional DBs with dynamic routing."
        }

        container happyHeadlines "CommentDomain" {
            include "element.tag==CommentDomain"
            include rabbitmq redis
            
            description "Comment validation with Polly circuit breaker to ProfanityService."
        }

        container happyHeadlines "NewsletterDomain" {
            include "element.tag==NewsletterDomain"
            include rabbitmq
            
            description "SubscriberService with runtime feature toggle."
        }

        # Component Views (Layer 3)
        component happyHeadlines.publisherService "PublisherService-Components" {
            include *
            description "Internal architecture: Controller receives articles and routes to regional RabbitMQ exchanges via PublisherMessaging component."
        }

        component happyHeadlines.articleService "ArticleService-Components" {
            include *
            description "Internal architecture: Controller → AppService → Repository (8 regional DBs). Background consumer processes RabbitMQ messages. Cache commander manages Brotli-compressed Redis cache and reports metrics."
        }

        component happyHeadlines.commentService "CommentService-Components" {
            include *
            description "Internal architecture: Controller → CacheCommander → Database. ResilienceService implements Polly circuit breaker for ProfanityService HTTP calls."
        }


        # Dynamic Views remain the same
        # Dynamic Views
        dynamic happyHeadlines "ArticlePublicationFlow" {
            happyHeadlines.publisherService -> rabbitmq "1. Routes to regional exchange"
            rabbitmq -> happyHeadlines.articleService "2. Delivers to regional queue"
            happyHeadlines.articleService -> happyHeadlines.articleDb "3. Persists to regional DB"
            happyHeadlines.articleService -> redis "4. Caches compressed"
            happyHeadlines.articleService -> rabbitmq "5. Publishes newsletter event"
            rabbitmq -> happyHeadlines.newsletterService "6. Delivers to newsletter queue"
            
        }

        dynamic happyHeadlines "CommentProfanityFlow" {
            happyHeadlines.commentService -> happyHeadlines.profanityService "1. Validates (circuit breaker)"
            happyHeadlines.profanityService -> happyHeadlines.profanityDb "2. Checks words"
            happyHeadlines.profanityService -> happyHeadlines.commentService "3. Returns result"
            happyHeadlines.commentService -> happyHeadlines.commentDb "4. Persists if clean"
            happyHeadlines.commentService -> redis "5. Caches"
            
        }

        dynamic happyHeadlines "SubscriptionFlow" {
            happyHeadlines.subscriberService -> happyHeadlines.subscriberDb "1. Persists if enabled"
            happyHeadlines.subscriberService -> rabbitmq "2. Publishes event"
            rabbitmq -> happyHeadlines.newsletterService "3. Delivers to queue"
            
        }

        deployment * live {
            include *
            
            description "Production deployment showing domain swimlanes"
        }




        styles {
            element "Person" {
                shape Person
                background #08427b
                color #ffffff
            }
            element "Software System" {
                background #1168bd
                color #ffffff
            }
            element "External" {
                background #999999
                color #ffffff
            }
            element "Container" {
                background #438dd5
                color #ffffff
            }
            element "Database" {
                shape Cylinder
                background #438dd5
                color #ffffff
            }
        }

        themes https://static.structurizr.com/themes/default/theme.json
    }
}