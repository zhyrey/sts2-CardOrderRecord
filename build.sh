#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
CONFIGURATION="${CONFIGURATION:-Debug}"
APP_ID="2868840"
GAME_NAME="Slay the Spire 2"

steamapps_candidates=()

add_steamapps() {
  local path="${1:-}"
  [[ -z "$path" ]] && return 0

  path="${path%/}"
  if [[ "$(basename "$path")" != "steamapps" ]]; then
    path="$path/steamapps"
  fi

  [[ -d "$path" ]] || return 0
  path="$(cd "$path" && pwd)"

  local existing
  for existing in "${steamapps_candidates[@]}"; do
    [[ "$existing" == "$path" ]] && return 0
  done

  steamapps_candidates+=("$path")
}

add_steam_root() {
  local path="${1:-}"
  [[ -z "$path" ]] && return 0

  add_steamapps "$path"

  local library_folders="$path/steamapps/libraryfolders.vdf"
  [[ -f "$library_folders" ]] || return 0

  while IFS= read -r line; do
    if [[ "$line" =~ \"path\"[[:space:]]*\"([^\"]+)\" ]]; then
      add_steamapps "${BASH_REMATCH[1]//\\\\/\\}"
    fi
  done < "$library_folders"
}

install_dir_from_manifest() {
  local manifest="$1"
  local line
  while IFS= read -r line; do
    if [[ "$line" =~ \"installdir\"[[:space:]]*\"([^\"]+)\" ]]; then
      printf '%s\n' "${BASH_REMATCH[1]}"
      return 0
    fi
  done < "$manifest"

  printf '%s\n' "$GAME_NAME"
}

find_sts2_dir() {
  if [[ -n "${Sts2Dir:-}" ]]; then
    printf '%s\n' "$Sts2Dir"
    return 0
  fi

  add_steamapps "${SteamLibraryPath:-}"
  add_steam_root "${STEAM_DIR:-}"
  add_steam_root "${STEAM_HOME:-}"
  add_steam_root "$HOME/.local/share/Steam"
  add_steam_root "$HOME/.steam/steam"

  local steamapps manifest install_dir candidate
  for steamapps in "${steamapps_candidates[@]}"; do
    manifest="$steamapps/appmanifest_$APP_ID.acf"
    [[ -f "$manifest" ]] || continue

    install_dir="$(install_dir_from_manifest "$manifest")"
    candidate="$steamapps/common/$install_dir"
    if [[ -d "$candidate" ]]; then
      printf '%s\n' "$candidate"
      return 0
    fi
  done

  for steamapps in "${steamapps_candidates[@]}"; do
    candidate="$steamapps/common/$GAME_NAME"
    if [[ -d "$candidate" ]]; then
      printf '%s\n' "$candidate"
      return 0
    fi
  done

  return 1
}

sts2_dir="$(find_sts2_dir)" || {
  echo "Could not find $GAME_NAME. Set Sts2Dir or SteamLibraryPath." >&2
  exit 1
}

echo "Detected $GAME_NAME at $sts2_dir"
build_args=("build" "$SCRIPT_DIR/CardOrderRecord.csproj" "-c" "$CONFIGURATION" "/p:Sts2Dir=$sts2_dir")
if [[ "${SKIP_MOD_COPY:-}" == "true" ]]; then
  build_args+=("/p:SkipModCopy=true")
fi

dotnet "${build_args[@]}"
