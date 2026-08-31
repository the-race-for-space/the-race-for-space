#!/usr/bin/env bash
set -euo pipefail

repositoryRoot="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
domainTestProject="$repositoryRoot/tests/TheRaceForSpace.Tests/TheRaceForSpace.Tests.csproj"
controllerTestProject="$repositoryRoot/tests/TheRaceForSpace.ControllerTests/TheRaceForSpace.ControllerTests.csproj"

if ! command -v dotnet >/dev/null 2>&1; then
    echo "dotnet is required but was not found in PATH."
    exit 1
fi

echo "Running KSP-independent prototype logic tests..."
dotnet run --project "$domainTestProject" -c Release

echo
echo "Running race controller regression tests..."
dotnet run --project "$controllerTestProject" -c Release
