#!/bin/sh
# Source this, never execute it: Yamato runs each command in a fresh shell, so the PATH the installer sets must land in the caller's.

# TODO: Remove the beta channel once the CLI is stable and available on the default channel.
curl -fsSL https://public-cdn.cloud.unity3d.com/hub/prod/cli/install.sh | UNITY_CLI_CHANNEL=beta bash
export PATH="$HOME/.local/bin:$HOME/.unity/bin:/usr/local/bin:$PATH"
hash -r
unity --version

install_unity_editor()
{
    version="$(sed -n 's/^m_EditorVersion: //p' "$1/ProjectSettings/ProjectVersion.txt")"
    # `unity editors info` queries the release catalog, not local installs, so it succeeds for versions that are absent.
    if unity editors list --installed --format tsv --no-banner \
        | awk -F'\t' -v v="${version}" 'NR > 1 && $1 == v { found = 1 } END { exit !found }'
    then
        echo "Unity Editor ${version} is already installed"
        return 0
    fi
    echo "Installing Unity Editor ${version}"
    unity install "${version}"
}
