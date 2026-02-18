#!/bin/bash
set -e

MIGRATION_NAME=${1:-"Update"}

echo "Creating EF Core migration: $MIGRATION_NAME..."

cd src/CronBot.Api
dotnet ef migrations add "$MIGRATION_NAME" --project ../CronBot.Infrastructure --startup-project .

echo "Migration created successfully!"

# Generate SQL script
echo "Generating SQL script..."
dotnet ef migrations script --project ../CronBot.Infrastructure --startup-project . -o ../../database/migrations/$(date +%Y%m%d%H%M%S)_$MIGRATION_NAME.sql

echo "SQL script generated!"
