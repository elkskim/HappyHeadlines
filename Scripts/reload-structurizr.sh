#!/bin/bash
# Reload Structurizr Lite to pick up workspace.dsl changes
# The circuits must be restarted to perceive new patterns

set -e

echo "Stopping Structurizr Lite container..."
CONTAINER_ID=$(docker ps -q --filter "ancestor=structurizr/lite")

if [ -z "$CONTAINER_ID" ]; then
    echo "No Structurizr Lite container running. Starting new instance..."
else
    echo "Found container: $CONTAINER_ID"
    docker stop "$CONTAINER_ID"
    docker rm "$CONTAINER_ID"
    echo "Container removed."
fi

echo "Starting Structurizr Lite with workspace at Documentation/Architecture..."
cd "$(git rev-parse --show-toplevel)"

# Ensure workspace.dsl exists
if [ ! -f "Documentation/Architecture/workspace.dsl" ]; then
    echo "ERROR: workspace.dsl not found at Documentation/Architecture/workspace.dsl"
    exit 1
fi

echo "Found workspace.dsl - cleaning up generated files..."
# Remove auto-generated files that might interfere
rm -f "Documentation/Architecture/workspace.dsl.dsl" 2>/dev/null || true
rm -f "Documentation/Architecture/workspace.dsl.json" 2>/dev/null || true
rm -rf "Documentation/Architecture/.structurizr" 2>/dev/null || true

echo "Starting container..."
# Convert Windows path to Docker-compatible format
REPO_ROOT="$(git rev-parse --show-toplevel)"
ARCH_PATH="${REPO_ROOT}/Documentation/Architecture"

# For Windows Docker, convert C:\ to //c/ format
if [[ "$OSTYPE" == "msys" ]] || [[ "$OSTYPE" == "win32" ]]; then
    ARCH_PATH=$(echo "$ARCH_PATH" | sed 's|^/c/|//c/|' | sed 's|\\|/|g')
fi

docker run -d \
    -p 8080:8080 \
    -v "${ARCH_PATH}:/usr/local/structurizr" \
    --name structurizr-lite \
    structurizr/lite

echo "Waiting for container to start and load workspace..."
sleep 5

NEW_CONTAINER_ID=$(docker ps -q --filter "name=structurizr-lite")
if [ -z "$NEW_CONTAINER_ID" ]; then
    echo "ERROR: Container failed to start!"
    echo "Check logs with: docker logs structurizr-lite"
    exit 1
fi

echo "Container started: $NEW_CONTAINER_ID"

# Verify workspace loaded
echo "Checking if workspace loaded successfully..."
sleep 2
if docker logs "$NEW_CONTAINER_ID" 2>&1 | grep -q "workspace.dsl"; then
    echo "✓ Workspace file detected in logs"
else
    echo "⚠ Warning: workspace.dsl not found in logs. Container may still be starting..."
fi

echo ""
echo "✓ Structurizr Lite is running at: http://localhost:8080"
echo "✓ Workspace loaded from: Documentation/Architecture/workspace.dsl"
echo ""
echo "Available diagrams:"
echo "  - System Context (Level 1)"
echo "  - Container views (Level 2): All, Services, Data, Observability"
echo "  - Component views (Level 3): PublisherService, ArticleService, CommentService"
echo "  - Domain views: Article, Comment, Newsletter"
echo "  - Dynamic flows: Article Publication, Comment Profanity, Subscription"
echo "  - Deployment diagram"
echo ""
echo "Useful commands:"
echo "  View logs:    docker logs -f $NEW_CONTAINER_ID"
echo "  Stop:         docker stop $NEW_CONTAINER_ID"
echo "  Restart:      ./reload-structurizr.sh"

