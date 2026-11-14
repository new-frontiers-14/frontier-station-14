# UNEON: GitHub → VPS

## Daily workflow

1. Change files locally in a task branch and test them.
2. Commit and push to origin (unemo-tional/frontier-station-14).
3. Open a PR **inside this repository**, base master, and merge it after review.
4. Wait for **UNEON - Build Linux** to succeed and publish a new **Release**.
5. On the VPS, as root, run:

```bash
uneon-update
```

Creating a PR does not deploy it. Only master is packaged for deployment.
The VPS keeps running its old version until you explicitly run the command.
The update ends the current round and disconnects players.

## One-time installation (Ubuntu 24.04 x86_64)

Run in the VPS SSH terminal as root:

```bash
curl -fL --retry 3 https://raw.githubusercontent.com/unemo-tional/frontier-station-14/master/Tools/uneon/install.sh -o /root/uneon-install.sh
bash /root/uneon-install.sh
```

This installs .NET 9 runtime, a dedicated uneon system account, the systemd
service, and the updater. Existing /opt/uneon/config/server.toml is preserved.
It does not start the game or enable a firewall.

Open inbound **TCP 1212 and UDP 1212** in the hosting provider firewall.
If UFW is already enabled, allow both there as well.
Do not enable a new firewall before permitting your SSH port.

When the first Release is ready, run uneon-update.

## Commands

```bash
uneon-update
systemctl status uneon --no-pager
journalctl -u uneon -n 80 --no-pager
journalctl -u uneon -f
systemctl stop uneon
systemctl start uneon
df -h /
```

Connect through the SS14 launcher to ss14://YOUR_PUBLIC_IP:1212.

## Layout

- /opt/uneon/current: symlink to the installed immutable build.
- /opt/uneon/releases: downloaded/extracted builds, owned by root.
- /opt/uneon/config/server.toml: persistent settings.
- /opt/uneon/data: persistent databases and server data.
- /opt/uneon/backups: stopped-server backups before updates.
- /usr/local/sbin/uneon-update: installed updater.
- /etc/systemd/system/uneon.service: service definition.

The updater downloads the public repository latest Release, pins all asset URLs
to that release tag, validates SHA-256 and ZIP paths, checks disk space, and
extracts the build before stopping the service. It then saves data/config,
switches the build symlink and checks the local status endpoint.

If startup health checks fail and there was a previous version, it stops the
new build, preserves its data for diagnosis, restores the previous data and
code, and restarts the old service if it was running. Check logs after a failed
update. Health means the process/status endpoint responds; test joining the
game after every update too. A first-install failure leaves the service stopped.

Backups and old releases are kept, not pruned automatically. Monitor disk
space, especially on a 25 GB VPS. This local backup does not replace an offsite
backup. SHA-256 checks detect transfer corruption, not a compromised publisher.

This bootstrap targets a public GitHub repository and the default port 1212.
If you change the port, update the updater health URL and firewall rules too.
No personal GitHub token or VPS password is needed in Actions.
Re-run the installer explicitly when you want to update the installed updater.
The runtime follows the current project's net9.0 target.

## Local checkout after setup changes on GitHub

```powershell
git status
git pull --ff-only origin master
```

If the same newly added setup files are untracked locally, preserve/move your
copies before pulling; do not blindly delete unrelated work.

## Build behavior

The workflow runs on pushes/PR merges to master and on manual dispatch from
master. The build uses the existing Content.Packaging hybrid mode, bundles
Content.Client.zip, and adds build.json with the matching engine version and
commit identifier. It publishes only after package validation. Older queued
commits do not replace the latest master Release. Releases are named
uneon-RUN_ID-ATTEMPT. Artifacts remain available as a secondary download.

Other inherited Frontier test workflows are separate checks. Their failures
must be investigated; this packaging workflow does not claim that all tests
passed.
