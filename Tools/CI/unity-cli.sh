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
    echo "Installing Unity Editor ${version}"
    unity install "${version}"
}
