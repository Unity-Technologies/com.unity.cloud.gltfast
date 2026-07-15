#!/bin/bash

set -euo pipefail

DOTNET_DIR="${HOME}/.dotnet"

# Collect unique major.minor versions to install
declare -A versions

# Primary version from global.json (e.g. SDK "10.0.201" -> "10.0")
global_ver=$(node -e "const v=require('./Tools/CI/global.json').sdk.version; console.log(v.split('.').slice(0,2).join('.'))")

if [[ -n "$global_ver" ]]; then
  versions["$global_ver"]=1
fi

# Additional versions from csproj TargetFramework(s)
while IFS= read -r -d '' csproj; do
  for fw in $(grep -oP '<TargetFrameworks?>\K[^<]+' "$csproj"); do
    IFS=';' read -ra frameworks <<< "$fw"
    for f in "${frameworks[@]}"; do
      ver=$(echo "$f" | grep -oP 'net\K[0-9]+\.[0-9]+')
      if [[ -n "$ver" ]]; then
        versions["$ver"]=1
      fi
    done
  done
done < <(find Tools/CI -name '*.csproj' -print0)

# Install all collected .NET SDK versions
rm -rf "$DOTNET_DIR"
for ver in "${!versions[@]}"; do
  echo "Installing .NET SDK channel $ver..."
  curl -sSL https://dot.net/v1/dotnet-install.sh \
    | bash /dev/stdin --channel "$ver" --arch x64 --install-dir "$DOTNET_DIR"
done
export PATH="$DOTNET_DIR:$PATH"

dotnet run -c Release --project Tools/CI/Gltfast.Cookbook.csproj
