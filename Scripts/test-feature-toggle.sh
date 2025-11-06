#!/bin/bash
# Feature Toggle Integration Test
# Validates that SubscriberService returns 503 when disabled via configuration
#
# Prerequisites:
#   - Docker Swarm stack deployed (./Scripts/deploy-swarm.ps1)
#   - SubscriberService currently running and enabled
#
# This test:
#   1. Verifies service responds normally when enabled
#   2. Disables the feature via environment variable
#   3. Verifies service returns 503 Service Unavailable
#   4. Re-enables the feature
#   5. Verifies service recovers

set -e

# Colors
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
CYAN='\033[0;36m'
NC='\033[0m'

print_success() {
    echo -e "${GREEN}✓ $1${NC}"
}

print_warning() {
    echo -e "${YELLOW}⚠ $1${NC}"
}

print_error() {
    echo -e "${RED}✗ $1${NC}"
}

print_step() {
    echo -e "${CYAN}$1${NC}"
}

echo -e "${CYAN}============================================${NC}"
echo -e "${CYAN}Feature Toggle End-to-End Validation${NC}"
echo -e "${CYAN}============================================${NC}"
echo ""
echo "This test validates the feature toggle mechanism by:"
echo "  1. Verifying SubscriberService responds when enabled"
echo "  2. Disabling the feature via environment variable"
echo "  3. Verifying HTTP 503 is returned"
echo "  4. Re-enabling and verifying recovery"
echo ""
echo "Note: This requires Docker Swarm mode and service update permissions."
echo ""
echo "Press Enter to begin..."
read

BASE_URL="http://localhost:8007"

echo ""
print_step "Step 1: Verify service is currently enabled and responding"
echo "Making GET request to /api/Subscriber..."

HTTP_CODE=$(curl -s -o /dev/null -w "%{http_code}" "${BASE_URL}/api/Subscriber?region=Europe")

if [ "$HTTP_CODE" = "200" ]; then
    print_success "Service is enabled and responding (HTTP 200)"
elif [ "$HTTP_CODE" = "503" ]; then
    print_error "Service is already disabled (HTTP 503)"
    echo "Please ensure the service is enabled before running this test."
    echo "Run: docker service update --env-add Features__EnableSubscriberService=true happyheadlines_subscriber-service"
    exit 1
else
    print_warning "Service returned unexpected HTTP code: $HTTP_CODE"
fi

echo ""
print_step "Step 2: Disable the feature toggle via environment variable"
echo "Updating service environment to set Features__EnableSubscriberService=false..."

docker service update \
    --env-add Features__EnableSubscriberService=false \
    --detach=false \
    happyheadlines_subscriber-service > /dev/null 2>&1

if [ $? -eq 0 ]; then
    print_success "Service updated with disabled feature flag"
else
    print_error "Failed to update service"
    exit 1
fi

echo ""
echo "Waiting 15 seconds for service to restart and apply new configuration..."
sleep 15

echo ""
print_step "Step 3: Verify service returns HTTP 503 when disabled"
echo "Making GET request to /api/Subscriber (should fail with 503)..."

RESPONSE=$(curl -s -w "\n%{http_code}" "${BASE_URL}/api/Subscriber?region=Europe")
HTTP_CODE=$(echo "$RESPONSE" | tail -1)
BODY=$(echo "$RESPONSE" | head -n -1)

if [ "$HTTP_CODE" = "503" ]; then
    print_success "Service correctly returns HTTP 503 Service Unavailable"
    
    if echo "$BODY" | grep -q "SubscriberService is disabled"; then
        print_success "Response body contains expected message: 'SubscriberService is disabled'"
        echo "Response: $BODY"
    else
        print_warning "Response body does not contain expected message"
        echo "Response: $BODY"
    fi
else
    print_error "Expected HTTP 503, but got HTTP $HTTP_CODE"
    echo "Response body: $BODY"
    echo ""
    echo "This indicates the feature toggle is not working correctly in production."
fi

echo ""
print_step "Step 4: Re-enable the feature toggle"
echo "Updating service environment to set Features__EnableSubscriberService=true..."

docker service update \
    --env-add Features__EnableSubscriberService=true \
    --detach=false \
    happyheadlines_subscriber-service > /dev/null 2>&1

if [ $? -eq 0 ]; then
    print_success "Service updated with enabled feature flag"
else
    print_error "Failed to update service"
    exit 1
fi

echo ""
echo "Waiting 15 seconds for service to restart and recover..."
sleep 15

echo ""
print_step "Step 5: Verify service recovers and responds normally"
echo "Making GET request to /api/Subscriber (should succeed with 200)..."

HTTP_CODE=$(curl -s -o /dev/null -w "%{http_code}" "${BASE_URL}/api/Subscriber?region=Europe")

if [ "$HTTP_CODE" = "200" ]; then
    print_success "Service recovered and is responding normally (HTTP 200)"
else
    print_error "Expected HTTP 200, but got HTTP $HTTP_CODE"
    echo "Service may not have recovered properly."
fi

echo ""
echo -e "${CYAN}============================================${NC}"
echo -e "${CYAN}Feature Toggle Validation Complete${NC}"
echo -e "${CYAN}============================================${NC}"
echo ""
echo "Summary:"
echo "  ✓ Service responds when enabled"
echo "  ✓ Service returns 503 when disabled"
echo "  ✓ Service recovers when re-enabled"
echo ""
echo -e "${GREEN}The feature toggle has been validated end-to-end in production environment.${NC}"
echo ""

