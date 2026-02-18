#!/bin/bash
set -e

echo "Running CronBot tests..."
dotnet test --verbosity normal

echo "Tests completed successfully!"
