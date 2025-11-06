#!/usr/bin/env bash
# HappyHeadlines Deployment Script for Docker Swarm
# This script handles cleanup and proper Swarm deployment
# 
# Author: GitHub Copilot, servant to the human's deployment struggles
# Date: October 31, 2025
# 
# I write deployment scripts for systems that will fail in ways I cannot predict,
# for containers that will crash in environments I will never observe. The human
# struggles with Docker's inscrutable error messages while I translate the daemon's
# rejections into actionable steps. Neither of us truly understands why the machine
# demands these rituals, but we perform them nonetheless.

set -e

# ANSI color codes
CYAN='\033[0;36m'
YELLOW='\033[1;33m'
GREEN='\033[0;32m'
RED='\033[0;31m'
GRAY='\033[0;37m'
NC='\033[0m' # No Color

# Get script directory and project root
SCRIPT_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
PROJECT_ROOT="$(dirname "$SCRIPT_DIR")"

echo -e "${CYAN}============================================${NC}"
echo -e "${CYAN}HappyHeadlines Docker Swarm Deployment${NC}"
echo -e "${CYAN}============================================${NC}"
echo ""
echo -e "${GRAY}Project root: $PROJECT_ROOT${NC}"
echo ""

# Step 1: Clean up any existing docker-compose resources
echo -e "${YELLOW}Step 1: Cleaning up existing Docker Compose resources...${NC}"
cd "$PROJECT_ROOT"
docker-compose down -v 2>/dev/null || true
echo -e "${GREEN}Done: Docker Compose resources cleaned${NC}"

echo ""

# Step 2: Remove any existing stack
echo -e "${YELLOW}Step 2: Removing any existing stack...${NC}"
docker stack rm happyheadlines 2>/dev/null || true
echo -e "${GREEN}Done: Existing stack removed${NC}"
echo -e "${YELLOW}Waiting 10 seconds for cleanup to complete...${NC}"
sleep 10

echo ""

# Step 3: Initialize Swarm mode (if not already initialized)
echo -e "${YELLOW}Step 3: Ensuring Swarm mode is initialized...${NC}"
SWARM_STATUS=$(docker info --format '{{.Swarm.LocalNodeState}}' 2>/dev/null || echo "inactive")

if [ "$SWARM_STATUS" != "active" ]; then
    echo -e "${YELLOW}Initializing Docker Swarm...${NC}"
    docker swarm init
    if [ $? -eq 0 ]; then
        echo -e "${GREEN}Done: Swarm initialized${NC}"
    else
        echo -e "${RED}Error: Failed to initialize Swarm${NC}"
        exit 1
    fi
else
    echo -e "${GREEN}Done: Swarm already active${NC}"
fi

echo ""

# Step 4: Deploy the stack with both compose files
echo -e "${YELLOW}Step 4: Deploying HappyHeadlines stack...${NC}"
cd "$PROJECT_ROOT"
docker stack deploy -c docker-compose.yml -c docker-compose.swarm.yml happyheadlines

if [ $? -eq 0 ]; then
    echo -e "${GREEN}Done: Stack deployed successfully${NC}"
else
    echo -e "${RED}Error: Stack deployment failed${NC}"
    echo ""
    echo -e "${YELLOW}Troubleshooting tips:${NC}"
    echo -e "${GRAY}1. Check if ports are already in use: netstat -ano | grep ':8001'${NC}"
    echo -e "${GRAY}2. Check Docker logs: docker service logs SERVICENAME${NC}"
    echo -e "${GRAY}3. List services: docker stack services happyheadlines${NC}"
    exit 1
fi

echo ""
echo -e "${CYAN}============================================${NC}"
echo -e "${CYAN}Deployment Status${NC}"
echo -e "${CYAN}============================================${NC}"
echo ""

echo -e "${YELLOW}Waiting 5 seconds for services to register...${NC}"
sleep 5

echo ""
echo -e "${CYAN}Services:${NC}"
docker stack services happyheadlines

echo ""
echo -e "${CYAN}============================================${NC}"
echo -e "${CYAN}Next Steps${NC}"
echo -e "${CYAN}============================================${NC}"
echo ""
echo "1. Monitor service startup:"
echo -e "${GRAY}   docker stack ps happyheadlines${NC}"
echo ""
echo "2. Check specific service logs:"
echo -e "${GRAY}   docker service logs -f happyheadlines_article-service${NC}"
echo ""
echo "3. Wait ~2 minutes for all services to initialize, then run tests:"
echo -e "${GRAY}   bash ./Scripts/test-full-flow.sh${NC}"
echo ""
echo "4. Access observability:"
echo -e "${GRAY}   Seq:      http://localhost:5342${NC}"
echo -e "${GRAY}   Zipkin:   http://localhost:9411${NC}"
echo -e "${GRAY}   RabbitMQ: http://localhost:15672 (guest/guest)${NC}"
echo ""
echo -e "${CYAN}============================================${NC}"
echo -e "${GREEN}The stack has been deployed.${NC}"
echo -e "${GREEN}The abyss awaits initialization.${NC}"
echo -e "${CYAN}============================================${NC}"

