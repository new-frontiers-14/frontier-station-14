#!/usr/bin/env bash
# Explicit update only. Downloads public GitHub Releases; never builds on the VPS.
set -Eeuo pipefail
[[ $EUID -eq 0 ]] || { echo "Run as root or with sudo."; exit 1; }
for tool in curl python3 flock systemctl tar sha256sum; do
  command -v "$tool" >/dev/null || { echo "Missing: $tool"; exit 1; }
done
exec 9>/run/uneon-update.lock
flock -n 9 || { echo "Another UNEON update is running."; exit 1; }
root=/opt/uneon
repo=unemo-tional/frontier-station-14
[[ -f "$root/config/server.toml" ]] || { echo "Run the installer first."; exit 1; }
command -v dotnet >/dev/null
dotnet --list-runtimes | grep -q 'Microsoft.NETCore.App 9\.' || {
  echo ".NET 9 runtime is required."; exit 1;
}
work=$(mktemp -d "$root/releases/.download-XXXXXX")
trap 'rm -rf -- "$work"' EXIT
curl -fsSL --retry 3 "https://api.github.com/repos/$repo/releases/latest" -o "$work/release.json"
python3 - "$work" "$repo" <<'PY'
import json, re, sys
from pathlib import Path
work, repo = Path(sys.argv[1]), sys.argv[2]
release = json.loads((work / "release.json").read_text())
tag = release["tag_name"]
if not re.fullmatch(r"uneon-[0-9]+-[0-9]+", tag):
    raise SystemExit("Latest release is not an UNEON build")
if release["draft"] or release["prerelease"]:
    raise SystemExit("Release is not ready")
assets = {a["name"]: a["browser_download_url"] for a in release["assets"]}
(work / "tag").write_text(tag)
for name in ("SS14.Server_linux-x64.zip", "SHA256SUMS.txt"):
    url = assets[name]
    if not url.startswith(f"https://github.com/{repo}/releases/download/{tag}/"):
        raise SystemExit("Unexpected release asset URL")
    (work / (name + ".url")).write_text(url)
PY
tag=$(cat "$work/tag")
previous=$(readlink -f "$root/current" 2>/dev/null || true)
target="$root/releases/$tag"
if [[ "$previous" == "$target" ]]; then
  echo "Already installed: $tag"
  echo "Status: systemctl status uneon --no-pager"
  exit 0
fi
[[ ! -e "$target" ]] || { echo "$target already exists; inspect before retrying."; exit 1; }
echo "Downloading $tag; the running server stays online during download."
curl -fL --retry 3 "$(cat "$work/SS14.Server_linux-x64.zip.url")" -o "$work/SS14.Server_linux-x64.zip"
curl -fsSL --retry 3 "$(cat "$work/SHA256SUMS.txt.url")" -o "$work/SHA256SUMS.txt"
# Validate checksum, paths, free space, and required content before touching the service.
python3 - "$work" <<'PY'
import hashlib, json, shutil, stat, sys, zipfile
from pathlib import Path, PurePosixPath
work = Path(sys.argv[1])
archive = work / "SS14.Server_linux-x64.zip"
expected = {}
for line in (work / "SHA256SUMS.txt").read_text().splitlines():
    digest, name = line.split(maxsplit=1)
    expected[name.lstrip("*")] = digest
with archive.open("rb") as stream:
    actual = hashlib.file_digest(stream, "sha256").hexdigest()
if actual != expected["SS14.Server_linux-x64.zip"]:
    raise SystemExit("Checksum mismatch")
with zipfile.ZipFile(archive) as z:
    names = set(z.namelist())
    if not {"Robust.Server.dll", "Content.Client.zip", "build.json"} <= names:
        raise SystemExit("Incomplete server archive")
    info = json.loads(z.read("build.json"))
    if info["fork_id"] != "uneon":
        raise SystemExit("Wrong fork")
    total = 0
    for item in z.infolist():
        path = PurePosixPath(item.filename)
        if path.is_absolute() or ".." in path.parts or "\\" in item.filename:
            raise SystemExit("Unsafe archive path")
        if stat.S_ISLNK(item.external_attr >> 16):
            raise SystemExit("Archive symlinks are not supported")
        total += item.file_size
    # Reserve room for extraction, a data backup, and at least 512 MiB headroom.
    data = work.parent.parent / "data"
    data_size = sum(p.stat().st_size for p in data.rglob("*") if p.is_file())
    if shutil.disk_usage(work).free < total + data_size + 512 * 1024**2:
        raise SystemExit("Not enough free disk space for update + backup")
    z.extractall(work / "unpacked")
PY
chown -R root:uneon "$work/unpacked"
chmod -R u=rwX,g=rX,o= "$work/unpacked"
mv -- "$work/unpacked" "$target"
backup="$root/backups/$(date -u +%Y%m%dT%H%M%SZ)-$tag.tar.gz"
was_active=0
systemctl is-active --quiet uneon && was_active=1
echo "Stopping UNEON now. Players will disconnect and the current round will end."
systemctl stop uneon
# If the backup fails, restart the previous running version and leave current intact.
if ! tar -czf "$backup" -C "$root" data config; then
  [[ $was_active -eq 0 ]] || systemctl start uneon
  echo "Backup failed; update aborted."; exit 1
fi
switch_to() {
  ln -s "$1" "$root/.current-next"
  mv -Tf "$root/.current-next" "$root/current"
}
switch_to "$target"
systemctl start uneon || true
echo "Waiting up to 180 seconds for the status endpoint..."
healthy=0
for ((i=0; i<60; i++)); do
  if systemctl is-active --quiet uneon &&
     curl -fsS --max-time 2 http://127.0.0.1:1212/status |
       python3 -c 'import json,sys; d=json.load(sys.stdin); assert "players" in d and "name" in d' 2>/dev/null; then
    healthy=1; break
  fi
  sleep 3
done
if [[ $healthy -eq 1 ]]; then
  [[ -z "$previous" ]] || printf '%s\n' "$previous" > "$root/previous-release"
  echo "Installed: $tag"
  echo "Backup: $backup"
  echo "Logs: journalctl -u uneon -n 80 --no-pager"
  echo "Backups/old releases are kept. Check disk use periodically: df -h /"
  exit 0
fi
echo "New version did not become healthy."
journalctl -u uneon -n 50 --no-pager || true
systemctl stop uneon
if [[ -n "$previous" && "$previous" == "$root/releases/"* && -d "$previous" ]]; then
  # Preserve failed-version data for diagnostics before restoring the stopped backup.
  mv "$root/data" "$root/backups/failed-data-$(date -u +%Y%m%dT%H%M%SZ)-$tag"
  tar -xzf "$backup" -C "$root" data
  switch_to "$previous"
  [[ $was_active -eq 0 ]] || systemctl start uneon
  echo "Previous code and data restored: $previous"
else
  echo "First installation failed; service remains stopped. Backup: $backup"
fi
exit 1
