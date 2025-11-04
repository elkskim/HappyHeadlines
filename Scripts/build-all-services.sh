#!/bin/bash

# The void calls; we answer with steel and discipline.
# Build all services in the HappyHeadlines project with proper dependencies.

set -e

echo "==================================="
echo "Building ALL HappyHeadlines Services"
echo "==================================="
echo ""

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
    echo ""
done

echo "==================================="
echo "All services built successfully!"
echo "==================================="
echo ""
echo "To deploy to swarm:"
echo "  ./Scripts/deploy-swarm.ps1"

