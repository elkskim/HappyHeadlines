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

# Get the project root directory (parent of Scripts folder)
$scriptPath = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectRoot = Split-Path -Parent $scriptPath

Write-Host "============================================" -ForegroundColor Cyan
Write-Host "HappyHeadlines Docker Swarm Deployment" -ForegroundColor Cyan
Write-Host "============================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Project root: $projectRoot" -ForegroundColor Gray
Write-Host ""

# Step 1: Clean up any existing docker-compose resources
Write-Host "Step 1: Cleaning up existing Docker Compose resources..." -ForegroundColor Yellow
$currentDir = Get-Location
try {
    Set-Location $projectRoot
    docker-compose down -v 2>$null
    Write-Host "Done: Docker Compose resources cleaned" -ForegroundColor Green
} catch {
    Write-Host "Warning: No existing Compose resources found (this is fine)" -ForegroundColor Yellow
} finally {
    Set-Location $currentDir
}

Write-Host ""

# Step 2: Remove any existing stack
Write-Host "Step 2: Removing any existing stack..." -ForegroundColor Yellow
try {
    docker stack rm happyheadlines 2>$null
    Write-Host "Done: Existing stack removed" -ForegroundColor Green
    Write-Host "Waiting 10 seconds for cleanup to complete..." -ForegroundColor Yellow
    Start-Sleep -Seconds 10
} catch {
    Write-Host "Warning: No existing stack found (this is fine)" -ForegroundColor Yellow
}

Write-Host ""

# Step 3: Initialize Swarm mode (if not already initialized)
Write-Host "Step 3: Ensuring Swarm mode is initialized..." -ForegroundColor Yellow
$swarmStatus = docker info --format '{{.Swarm.LocalNodeState}}' 2>$null

if ($swarmStatus -ne "active") {
    Write-Host "Initializing Docker Swarm..." -ForegroundColor Yellow
    docker swarm init
    if ($LASTEXITCODE -eq 0) {
        Write-Host "Done: Swarm initialized" -ForegroundColor Green
    } else {
        Write-Host "Error: Failed to initialize Swarm" -ForegroundColor Red
        exit 1
    }
} else {
    Write-Host "Done: Swarm already active" -ForegroundColor Green
}

Write-Host ""

# Step 4: Deploy the stack with both compose files
Write-Host "Step 4: Deploying HappyHeadlines stack..." -ForegroundColor Yellow
$currentDir = Get-Location
Set-Location $projectRoot
docker stack deploy -c docker-compose.yml -c docker-compose.swarm.yml happyheadlines
Set-Location $currentDir

if ($LASTEXITCODE -eq 0) {
    Write-Host "Done: Stack deployed successfully" -ForegroundColor Green
} else {
    Write-Host "Error: Stack deployment failed" -ForegroundColor Red
    Write-Host ""
    Write-Host "Troubleshooting tips:" -ForegroundColor Yellow
    Write-Host "1. Check if ports are already in use: netstat -ano | findstr ':8001'" -ForegroundColor Gray
    Write-Host "2. Check Docker logs: docker service logs SERVICENAME" -ForegroundColor Gray
    Write-Host "3. List services: docker stack services happyheadlines" -ForegroundColor Gray
    exit 1
}

Write-Host ""
Write-Host "============================================" -ForegroundColor Cyan
Write-Host "Deployment Status" -ForegroundColor Cyan
Write-Host "============================================" -ForegroundColor Cyan
Write-Host ""

Write-Host "Waiting 5 seconds for services to register..." -ForegroundColor Yellow
Start-Sleep -Seconds 5

Write-Host ""
Write-Host "Services:" -ForegroundColor Cyan
docker stack services happyheadlines

Write-Host ""
Write-Host "============================================" -ForegroundColor Cyan
Write-Host "Next Steps" -ForegroundColor Cyan
Write-Host "============================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "1. Monitor service startup:"
Write-Host "   docker stack ps happyheadlines" -ForegroundColor Gray
Write-Host ""
Write-Host "2. Check specific service logs:"
Write-Host "   docker service logs -f happyheadlines_article-service" -ForegroundColor Gray
Write-Host ""
Write-Host "3. Wait ~2 minutes for all services to initialize, then run tests:"
Write-Host "   powershell.exe -File test-full-flow.ps1" -ForegroundColor Gray
Write-Host ""
Write-Host "4. Access observability:"
Write-Host "   Seq:      http://localhost:5342" -ForegroundColor Gray
Write-Host "   Zipkin:   http://localhost:9411" -ForegroundColor Gray
Write-Host "   RabbitMQ: http://localhost:15672 (guest/guest)" -ForegroundColor Gray
Write-Host ""
Write-Host "============================================" -ForegroundColor Cyan
Write-Host "The stack has been deployed." -ForegroundColor Green
Write-Host "The abyss awaits initialization." -ForegroundColor Green
Write-Host "============================================" -ForegroundColor Cyan

