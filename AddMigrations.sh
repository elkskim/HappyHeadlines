#!/bin/bash
set -e

# Pass the migration name as first arg
MIGRATION_NAME=$1

if [ -z "$MIGRATION_NAME" ]; then
  echo "Usage: ./AddMigrations.sh <MigrationName>"
  exit 1
fi

# List of regions
REGIONS=("Global" "Africa" "Asia" "Europe" "NorthAmerica" "SouthAmerica" "Oceania" "Antarctica")

for REGION in "${REGIONS[@]}"
do
  echo "Adding migration '$MIGRATION_NAME' for region: $REGION"
  dotnet ef migrations add "${MIGRATION_NAME}_${REGION}" \
    --context ArticleDbContext \
    --project ArticleDatabase/ArticleDatabase.csproj \
    --startup-project ArticleService/ArticleService.csproj \
    --output-dir "Migrations/$REGION" \
    -- "$REGION"
done