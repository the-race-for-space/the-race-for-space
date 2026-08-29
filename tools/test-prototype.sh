#!/usr/bin/env bash
set -euo pipefail

repositoryRoot="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
projectPath="$repositoryRoot/src/TheRaceForSpace/TheRaceForSpace.csproj"

if ! command -v git >/dev/null 2>&1; then
    echo "git is required but was not found in PATH."
    exit 1
fi

if ! command -v dotnet >/dev/null 2>&1; then
    echo "dotnet is required but was not found in PATH."
    exit 1
fi

targetBranch="${1:-$(git -C "$repositoryRoot" branch --show-current)}"
if [[ -z "$targetBranch" ]]; then
    echo "Usage: bash tools/test-prototype.sh <branch>"
    exit 1
fi

# Refuse to switch branches when local work exists so a test deploy cannot
# accidentally hide or overwrite edits the developer has not committed yet.
if [[ -n "$(git -C "$repositoryRoot" status --porcelain)" ]]; then
    echo "The repository has uncommitted changes. Commit or stash them before switching test versions."
    git -C "$repositoryRoot" status --short
    exit 1
fi

echo "Fetching latest branches from GitHub..."
git -C "$repositoryRoot" fetch origin

if git -C "$repositoryRoot" show-ref --verify --quiet "refs/heads/$targetBranch"; then
    git -C "$repositoryRoot" switch "$targetBranch"
elif git -C "$repositoryRoot" show-ref --verify --quiet "refs/remotes/origin/$targetBranch"; then
    git -C "$repositoryRoot" switch --track "origin/$targetBranch"
else
    echo "Branch '$targetBranch' was not found locally or on origin."
    exit 1
fi

echo "Updating $targetBranch..."
git -C "$repositoryRoot" pull --ff-only origin "$targetBranch"

if [[ -z "${KSP_ROOT:-}" ]]; then
    linuxSteamCandidates=(
        "$HOME/.local/share/Steam/steamapps/common/Kerbal Space Program"
        "$HOME/.steam/steam/steamapps/common/Kerbal Space Program"
    )

    for candidate in "${linuxSteamCandidates[@]}"; do
        if [[ -f "$candidate/KSP_Data/Managed/Assembly-CSharp.dll" ]]; then
            export KSP_ROOT="$candidate"
            break
        fi
    done
fi

if [[ -z "${KSP_ROOT:-}" ]]; then
    echo "KSP_ROOT is not set and no standard Linux Steam KSP installation was found."
    echo "Set it first, for example:"
    echo '  export KSP_ROOT="$HOME/.local/share/Steam/steamapps/common/Kerbal Space Program"'
    exit 1
fi

if [[ ! -f "$KSP_ROOT/KSP_Data/Managed/Assembly-CSharp.dll" ]]; then
    echo "KSP managed assemblies were not found under:"
    echo "  $KSP_ROOT/KSP_Data/Managed"
    exit 1
fi

echo "Building and deploying $targetBranch..."
dotnet build "$projectPath" -c Debug -p:DeployToKsp=true

deployedAssembly="$KSP_ROOT/GameData/TheRaceForSpace/Plugins/TheRaceForSpace.dll"
if [[ ! -f "$deployedAssembly" ]]; then
    echo "Build completed but the deployed DLL was not found at:"
    echo "  $deployedAssembly"
    exit 1
fi

echo
echo "Prototype ready to test."
echo "Branch: $targetBranch"
echo "KSP:    $KSP_ROOT"
echo "DLL:    $deployedAssembly"
