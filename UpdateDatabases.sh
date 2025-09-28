#!/bin/bash
set -e
REGIONS=("Global" "Africa" "Asia" "Europe" "NorthAmerica" "SouthAmerica" "Oceania" "Antarctica")

for REGION in "${REGIONS[@]}"
do
  echo "Updating database for region: $REGION"
  dotnet ef database update \
    --context ArticleDbContext \
    --project ArticleDatabase/ArticleDatabase.csproj \
    --startup-project ArticleService/ArticleService.csproj \
    -- "$REGION"
done