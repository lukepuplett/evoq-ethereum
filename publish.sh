#!/bin/bash

# Exit on error
set -e

# Ensure we have a NuGet API key
if [ -z "$NUGET_API_KEY" ]; then
    echo "Error: NUGET_API_KEY environment variable is not set"
    exit 1
fi

# Find the package file
PACKAGE_FILE=$(find ./artifacts -name "*.nupkg" | head -n 1)

if [ -z "$PACKAGE_FILE" ]; then
    echo "Error: No .nupkg file found in artifacts directory"
    exit 1
fi

# Push to NuGet
echo "Publishing package to NuGet.org..."
dotnet nuget push "$PACKAGE_FILE" --api-key "$NUGET_API_KEY" --source https://api.nuget.org/v3/index.json

echo "Package published successfully!"