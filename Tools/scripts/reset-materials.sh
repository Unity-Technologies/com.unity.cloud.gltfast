#!/bin/sh

# Resets changes to materials and render pipeline settings via git restore.
#
# When to use:
# When testing with newer Unity versions URP/HDRP may auto-update many material files and you don't want to commit
# those changes. With this script you can easily discard those changes without going through them manually.

set -e

pwd=$(cd -P -- "$(dirname -- "$0")" && printf '%s\n' "$(pwd -P)")
source "$pwd/common.sh"

reset_materials
