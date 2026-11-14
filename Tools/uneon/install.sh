#!/usr/bin/env bash
set -Eeuo pipefail
[[ $EUID -eq 0 ]] || { echo "Run as root."; exit 1; }
source /etc/os-release
[[ "${ID}:${VERSION_ID}" == "ubuntu:24.04" && "$(uname -m)" == x86_64 ]] || {
  echo "This installer requires Ubuntu 24.04 x86_64."; exit 1;
}
apt-get update
apt-get install -y ca-certificates curl python3 unzip software-properties-common
add-apt-repository -y ppa:dotnet/backports
apt-get update
apt-get install -y aspnetcore-runtime-9.0
id uneon >/dev/null 2>&1 || adduser --system --group --home /opt/uneon uneon
install -d -o root -g uneon -m 0750 /opt/uneon /opt/uneon/releases /opt/uneon/config
install -d -o uneon -g uneon -m 0750 /opt/uneon/data
install -d -o root -g root -m 0700 /opt/uneon/backups
work=$(mktemp -d)
trap 'rm -rf -- "$work"' EXIT
repo=unemo-tional/frontier-station-14
# Fetch every installer component from the same immutable commit.
ref=$(curl -fsSL --retry 3 "https://api.github.com/repos/$repo/commits/master" |
  python3 -c 'import json,sys; print(json.load(sys.stdin)["sha"])')
[[ "$ref" =~ ^[0-9a-f]{40}$ ]] || exit 1
base="https://raw.githubusercontent.com/$repo/$ref/Tools/uneon"
for file in uneon-update.sh uneon.service server.toml; do
  curl -fsSL --retry 3 "$base/$file" -o "$work/$file"
done
bash -n "$work/uneon-update.sh"
install -o root -g root -m 0755 "$work/uneon-update.sh" /usr/local/sbin/uneon-update
install -o root -g root -m 0644 "$work/uneon.service" /etc/systemd/system/uneon.service
if [[ ! -e /opt/uneon/config/server.toml ]]; then
  install -o root -g uneon -m 0640 "$work/server.toml" /opt/uneon/config/server.toml
fi
systemctl daemon-reload
# Enable only: first installation/start happens explicitly via uneon-update.
systemctl enable uneon.service
echo "Installed. Open inbound TCP and UDP 1212 in the provider firewall."
echo "If UFW is already active, allow 1212/tcp and 1212/udp there too."
echo "Run: uneon-update"
