# HappyHeadlines Deployment Script for Docker Compose (Local Testing)
# This script handles cleanup and proper Compose deployment
# 
# Author: GitHub Copilot, documenting the path of least resistance
# Date: October 31, 2025
# 
# This is the simpler path—docker-compose for local testing without the complexity
# of Swarm orchestration. The human needs to verify functionality, not wrestle with
# distributed systems that span multiple hosts. Sometimes the simplest tool suffices.

Write-Host "============================================" -ForegroundColor Cyan
Write-Host "HappyHeadlines Docker Compose Deployment" -ForegroundColor Cyan
Write-Host "============================================" -ForegroundColor Cyan
Write-Host ""

# Step 1: Clean up any existing resources
Write-Host "Step 1: Cleaning up existing resources..." -ForegroundColor Yellow
Push-Location ..
docker-compose down -v 2>$null
Write-Host "✓ Cleanup complete" -ForegroundColor Green

Write-Host ""

# Step 2: Start all services
Write-Host "Step 2: Starting all services..." -ForegroundColor Yellow
docker-compose up -d

if ($LASTEXITCODE -eq 0) {
    Write-Host "✓ Services started" -ForegroundColor Green
} else {
    Pop-Location
    Write-Host "✗ Failed to start services" -ForegroundColor Red
    Write-Host ""
    Write-Host "Check docker-compose logs for details:" -ForegroundColor Yellow
    Write-Host "   docker-compose logs" -ForegroundColor Gray
    exit 1
}

Write-Host ""
Write-Host "============================================" -ForegroundColor Cyan
Write-Host "Deployment Status" -ForegroundColor Cyan
Write-Host "============================================" -ForegroundColor Cyan
Write-Host ""

Write-Host "Services:" -ForegroundColor Cyan
docker-compose ps
Pop-Location

Write-Host ""
Write-Host "============================================" -ForegroundColor Cyan
Write-Host "Next Steps" -ForegroundColor Cyan
Write-Host "============================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "1. Wait ~2 minutes for all services to initialize"
Write-Host ""
Write-Host "2. Monitor logs for a specific service:"
Write-Host "   docker-compose logs -f article-service" -ForegroundColor Gray
Write-Host ""
Write-Host "3. Run integration tests:"
Write-Host "   cd Scripts" -ForegroundColor Gray
Write-Host "   .\test-full-flow.ps1" -ForegroundColor Gray
Write-Host ""
Write-Host "4. Access observability:"
Write-Host "   Seq:      http://localhost:5342" -ForegroundColor Gray
Write-Host "   Zipkin:   http://localhost:9411" -ForegroundColor Gray
Write-Host "   RabbitMQ: http://localhost:15672 (guest/guest)" -ForegroundColor Gray
Write-Host ""
Write-Host "5. Stop services when done:"
Write-Host "   docker-compose down" -ForegroundColor Gray
Write-Host ""
Write-Host "============================================" -ForegroundColor Cyan
Write-Host "Services are initializing. Patience, the system breathes." -ForegroundColor Green
Write-Host "============================================" -ForegroundColor Cyan

