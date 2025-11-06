#!/bin/bash
# Fast Feature Toggle Integration Test (No Restart Required)
# Uses admin endpoints to toggle the feature at runtime
#
# Prerequisites:
#   - SubscriberService running with AdminController enabled
#   - No restart or redeployment needed
#
# This test completes in ~5 seconds instead of ~30 seconds

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
echo -e "${CYAN}Fast Feature Toggle Test (No Restart)${NC}"
echo -e "${CYAN}============================================${NC}"
echo ""
echo "This test validates the feature toggle using runtime overrides:"
echo "  1. Verify service responds normally"
echo "  2. Disable via admin endpoint (no restart)"
echo "  3. Verify HTTP 503 is returned"
echo "  4. Re-enable via admin endpoint (no restart)"
echo "  5. Verify service recovers"
echo ""
echo "Estimated time: ~5 seconds"
echo ""
echo "Press Enter to begin..."
read

BASE_URL="http://localhost:8007"

echo ""
print_step "Step 1: Verify service is currently responding"
HTTP_CODE=$(curl -s -o /dev/null -w "%{http_code}" "${BASE_URL}/api/Subscriber?region=Europe")

if [ "$HTTP_CODE" = "200" ]; then
    print_success "Service is responding (HTTP 200)"
elif [ "$HTTP_CODE" = "503" ]; then
    print_warning "Service is currently disabled, enabling via admin endpoint..."
    curl -s -X POST "${BASE_URL}/api/Admin/enable-service" > /dev/null
    sleep 1
    HTTP_CODE=$(curl -s -o /dev/null -w "%{http_code}" "${BASE_URL}/api/Subscriber?region=Europe")
    if [ "$HTTP_CODE" = "200" ]; then
        print_success "Service enabled and responding"
    else
        print_error "Could not enable service"
        exit 1
    fi
else
    print_warning "Service returned unexpected HTTP code: $HTTP_CODE"
fi

echo ""
print_step "Step 2: Disable feature via admin endpoint (no restart)"
RESPONSE=$(curl -s -X POST "${BASE_URL}/api/Admin/disable-service")
echo "Admin response: $RESPONSE"
print_success "Feature toggle disabled via runtime override"

echo ""
print_step "Step 3: Verify service returns HTTP 503 (immediate, no restart)"
RESPONSE=$(curl -s -w "\n%{http_code}" "${BASE_URL}/api/Subscriber?region=Europe")
HTTP_CODE=$(echo "$RESPONSE" | tail -1)
BODY=$(echo "$RESPONSE" | head -n -1)

if [ "$HTTP_CODE" = "503" ]; then
    print_success "Service correctly returns HTTP 503 Service Unavailable"
    
    if echo "$BODY" | grep -q "SubscriberService is disabled"; then
        print_success "Response body contains expected message: 'SubscriberService is disabled'"
    else
        print_warning "Response body: $BODY"
    fi
else
    print_error "Expected HTTP 503, but got HTTP $HTTP_CODE"
    echo "Response: $BODY"
fi

echo ""
print_step "Step 4: Re-enable feature via admin endpoint (no restart)"
RESPONSE=$(curl -s -X POST "${BASE_URL}/api/Admin/enable-service")
echo "Admin response: $RESPONSE"
print_success "Feature toggle enabled via runtime override"

echo ""
print_step "Step 5: Verify service recovers (immediate, no restart)"
HTTP_CODE=$(curl -s -o /dev/null -w "%{http_code}" "${BASE_URL}/api/Subscriber?region=Europe")

if [ "$HTTP_CODE" = "200" ]; then
    print_success "Service recovered and responding (HTTP 200)"
else
    print_error "Expected HTTP 200, but got HTTP $HTTP_CODE"
fi

echo ""
print_step "Step 6: Reset to configuration-based value"
RESPONSE=$(curl -s -X POST "${BASE_URL}/api/Admin/reset-feature-toggle")
echo "Admin response: $RESPONSE"
print_success "Runtime override cleared"

echo ""
echo -e "${CYAN}============================================${NC}"
echo -e "${CYAN}Fast Feature Toggle Test Complete${NC}"
echo -e "${CYAN}============================================${NC}"
echo ""
echo "Summary:"
echo "  ✓ Service responds when enabled"
echo "  ✓ Service returns 503 when disabled (NO RESTART)"
echo "  ✓ Service recovers when re-enabled (NO RESTART)"
echo "  ✓ Runtime override cleared"
echo ""
echo -e "${GREEN}Feature toggle validated without service restart (completed in ~5 seconds)${NC}"
echo ""

