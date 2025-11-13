# Structurizr Workspace Loading Fix

## What Was Changed

The `reload-structurizr.sh` script has been updated to properly load your `workspace.dsl` file as the default diagram.

### Key Changes:

1. **Added environment variable**: `-e STRUCTURIZR_WORKSPACE_FILENAME=workspace.dsl`
   - This explicitly tells Structurizr Lite which file to load
   - Without this, it might not recognize `workspace.dsl` as the default

2. **Added workspace file validation**:
   - Checks if `workspace.dsl` exists before starting container
   - Exits with error if file not found

3. **Added verification step**:
   - Checks container logs to confirm workspace loaded
   - Waits longer (5 seconds) for container to fully start
   - Reports success or warning

4. **Improved output messages**:
   - Lists all available diagrams
   - Provides helpful commands for managing container

## How to Use

### Run the script:
```bash
cd Scripts
./reload-structurizr.sh
```

### Access Structurizr:
Open your browser to: **http://localhost:8080**

The workspace should automatically load with these views:
- System Context
- Container views (All, Services, Data, Observability)
- Domain views (Article, Comment, Newsletter)
- Dynamic flows (Article Publication, Comment Profanity, Subscription)
- Deployment diagram

## Troubleshooting

### If workspace still doesn't load:

1. **Check container logs**:
   ```bash
   docker logs structurizr-lite
   ```

2. **Verify file location**:
   ```bash
   ls -la Documentation/Architecture/workspace.dsl
   ```

3. **Try manual container start**:
   ```bash
   docker run -it --rm \
       -p 8080:8080 \
       -v "$(pwd)/Documentation/Architecture:/usr/local/structurizr" \
       -e STRUCTURIZR_WORKSPACE_FILENAME=workspace.dsl \
       structurizr/lite
   ```
   This runs in foreground so you can see any errors immediately.

4. **Check file permissions**:
   Make sure `workspace.dsl` is readable:
   ```bash
   chmod 644 Documentation/Architecture/workspace.dsl
   ```

5. **Validate DSL syntax**:
   If workspace loads but shows errors, there might be syntax issues in your DSL file.
   Check the Structurizr UI for validation errors.

## Why It Works

Structurizr Lite by default looks for:
1. `workspace.json` (higher priority)
2. `workspace.dsl` (if no JSON found)

By setting the `STRUCTURIZR_WORKSPACE_FILENAME` environment variable, we explicitly tell it to load `workspace.dsl` regardless of other files present.

The volume mount `-v "$(pwd)/Documentation/Architecture:/usr/local/structurizr"` ensures the DSL file is accessible inside the container at the expected location.

## Quick Reference

| Action | Command |
|--------|---------|
| Start/Reload | `./Scripts/reload-structurizr.sh` |
| View logs | `docker logs -f structurizr-lite` |
| Stop container | `docker stop structurizr-lite` |
| Remove container | `docker rm structurizr-lite` |
| Access UI | http://localhost:8080 |

## Expected Output

When running the script, you should see:

```
Stopping Structurizr Lite container...
Found container: abc123def456
Container removed.
Starting Structurizr Lite with workspace at Documentation/Architecture...
Found workspace.dsl - starting container...
Waiting for container to start and load workspace...
Container started: xyz789abc012
Checking if workspace loaded successfully...
✓ Workspace file detected in logs

✓ Structurizr Lite is running at: http://localhost:8080
✓ Workspace loaded from: Documentation/Architecture/workspace.dsl

Available diagrams:
  - System Context
  - Container views (All, Services, Data, Observability)
  - Domain views (Article, Comment, Newsletter)
  - Dynamic flows (Article Publication, Comment Profanity, Subscription)
  - Deployment diagram
```

