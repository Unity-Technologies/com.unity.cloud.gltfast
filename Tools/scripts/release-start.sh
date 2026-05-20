#!/bin/bash

set -e

VERSION=$1
PKG_FILE="Packages/com.unity.cloud.gltfast/package.json"
CHANGELOG_FILE="${2:-Packages/com.unity.cloud.gltfast/CHANGELOG.md}"
CONSTANTS_FILE="Packages/com.unity.cloud.gltfast/Runtime/Scripts/Export/Constants.cs"

DATE=$(date +%Y-%m-%d)

# Validate inputs
if [ -z "$VERSION" ]; then
    echo "Error: Version number is required"
    echo "Usage: $0 <version> [changelog-file]"
    echo "Example: $0 1.2.3"
    exit 1
fi

# Update version in `package.json`
TMP_FILE=$(mktemp)
jq --arg v "$VERSION" '.version=$v' "$PKG_FILE" > "$TMP_FILE"
mv "$TMP_FILE" "$PKG_FILE"
echo "✓ Updated $PKG_FILE"

# Update static version string in `Constants.cs``
if [ ! -f "$CONSTANTS_FILE" ]; then
    echo "Error: Constants file '$CONSTANTS_FILE' not found"
    exit 1
fi

if ! grep -q "public const string version = " "$CONSTANTS_FILE"; then
    echo "Error: No version variable found in $CONSTANTS_FILE"
    exit 1
fi

TMP_FILE=$(mktemp)

awk -v version="$VERSION" '
    /        public const string version = "(.*)";$/ {
        if (!replaced) {
            print "        public const string version = \"" version "\";";
            replaced = 1
            next
        }
    }
    { print }
' "$CONSTANTS_FILE" > "$TMP_FILE"

# Replace original file
mv "$TMP_FILE" "$CONSTANTS_FILE"

echo "✓ Updated $CONSTANTS_FILE"
