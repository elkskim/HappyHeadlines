#!/bin/bash
set -e

MIGRATION_NAME=$1

if [ -z "$MIGRATION_NAME" ]; then
  echo "Usage: $0 <MigrationName>"
  exit 1
fi

REGIONS=("Global" "Africa" "Asia" "Europe" "NorthAmerica" "SouthAmerica" "Oceania" "Antarctica")

for REGION in "${REGIONS[@]}"
do
  echo "Updating database for region: $REGION using migration: $MIGRATION_NAME"
  dotnet ef database update "$MIGRATION_NAME"\
    --context ArticleDbContext \
    --project ArticleDatabase/ArticleDatabase.csproj \
    --startup-project ArticleService/ArticleService.csproj \
    
done