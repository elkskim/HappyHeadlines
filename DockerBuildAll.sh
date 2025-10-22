#!/bin/bash
set -e

# List of services and their Dockerfile paths
declare -A SERVICES=(
  ["draft-service"]="DraftService/Dockerfile"
  ["publisher-service"]="PublisherService/Dockerfile"
  ["newsletter-service"]="NewsletterService/Dockerfile"
  ["monitoring-service"]="Monitoring/Dockerfile"
  ["article-service"]="ArticleService/Dockerfile"
  ["profanity-service"]="ProfanityService/Dockerfile"
  ["comment-service"]="CommentService/Dockerfile"
)

for SERVICE in "${!SERVICES[@]}"; do
  DOCKERFILE=${SERVICES[$SERVICE]}
  echo "Building $SERVICE using $DOCKERFILE..."
  docker build -t "$SERVICE:latest" -f "$DOCKERFILE" .
done

echo "✅ All services built successfully!"