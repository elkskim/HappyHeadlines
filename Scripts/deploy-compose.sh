#!/usr/bin/env bash
# HappyHeadlines Deployment Script for Docker Compose (Local Testing)
# This script handles cleanup and proper Compose deployment
# 
# Author: GitHub Copilot, documenting the path of least resistance
# Date: October 31, 2025
# 
# This is the simpler path—docker-compose for local testing without the complexity
# of Swarm orchestration. The human needs to verify functionality, not wrestle with
# distributed systems that span multiple hosts. Sometimes the simplest tool suffices.

set -e

# ANSI color codes
CYAN='\033[0;36m'
YELLOW='\033[1;33m'
GREEN='\033[0;32m'
RED='\033[0;31m'
GRAY='\033[0;37m'
NC='\033[0m' # No Color

echo -e "${CYAN}============================================${NC}"
echo -e "${CYAN}HappyHeadlines Docker Compose Deployment${NC}"
echo -e "${CYAN}============================================${NC}"
echo ""

# Get script directory and project root
SCRIPT_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
PROJECT_ROOT="$(dirname "$SCRIPT_DIR")"

# Step 1: Clean up any existing resources
echo -e "${YELLOW}Step 1: Cleaning up existing resources...${NC}"
cd "$PROJECT_ROOT"
docker-compose down -v 2>/dev/null || true
echo -e "${GREEN}✓ Cleanup complete${NC}"

echo ""

# Step 2: Start all services
echo -e "${YELLOW}Step 2: Starting all services...${NC}"
docker-compose up -d

if [ $? -eq 0 ]; then
    echo -e "${GREEN}✓ Services started${NC}"
else
    echo -e "${RED}✗ Failed to start services${NC}"
    echo ""
    echo -e "${YELLOW}Check docker-compose logs for details:${NC}"
    echo -e "${GRAY}   docker-compose logs${NC}"
    exit 1
fi

echo ""
echo -e "${CYAN}============================================${NC}"
echo -e "${CYAN}Deployment Status${NC}"
echo -e "${CYAN}============================================${NC}"
echo ""

echo -e "${CYAN}Services:${NC}"
docker-compose ps

echo ""
echo -e "${CYAN}============================================${NC}"
echo -e "${CYAN}Next Steps${NC}"
echo -e "${CYAN}============================================${NC}"
echo ""
echo "1. Wait ~2 minutes for all services to initialize"
echo ""
echo "2. Monitor logs for a specific service:"
echo -e "${GRAY}   docker-compose logs -f article-service${NC}"
echo ""
echo "3. Run integration tests:"
echo -e "${GRAY}   cd Scripts${NC}"
echo -e "${GRAY}   ./test-full-flow.sh${NC}"
echo ""
echo "4. Access observability:"
echo -e "${GRAY}   Seq:      http://localhost:5342${NC}"
echo -e "${GRAY}   Zipkin:   http://localhost:9411${NC}"
echo -e "${GRAY}   RabbitMQ: http://localhost:15672 (guest/guest)${NC}"
echo ""
echo "5. Stop services when done:"
echo -e "${GRAY}   docker-compose down${NC}"
echo ""
echo -e "${CYAN}============================================${NC}"
echo -e "${GREEN}Services are initializing. Patience, the system breathes.${NC}"
echo -e "${CYAN}============================================${NC}"

