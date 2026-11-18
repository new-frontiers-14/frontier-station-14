"""Stage the bridge next to the existing ss14-monitor. Run as root on its VPS.

Does not restart services. Activation commands are printed after successful staging.
"""
import argparse
import ast
from datetime import datetime, timezone
import json
import os
from pathlib import Path
import secrets
import shutil
import subprocess
import sys


GUILD = "1571189014478848020"
ROLE = "1572636208310526132"
MARKER = "# UNEON subscription hook (monitor integration v1)"


def patch_monitor(text):
    ast.parse(text)
    if MARKER in text:
        return text
    needle = "        self.poll_status.start()"
    if text.count(needle) != 1:
        raise ValueError("Unexpected bot.py: cannot safely locate setup_hook polling startup")
    result = text.replace(needle, needle + "\n\n        " + MARKER +
                          "\n        from uneon_subscriptions.host_setup import enable_subscription_link"
                          "\n        await enable_subscription_link(self)")
    ast.parse(result)
    return result


def main():
    import pwd
    import grp

    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--guild", default=GUILD)
    parser.add_argument("--role", default=ROLE)
    args = parser.parse_args()
    if os.geteuid() != 0 or not sys.platform.startswith("linux"):
        raise SystemExit("Run as root on the VPS hosting ss14-monitor")
    if not args.guild.isdigit() or not args.role.isdigit():
        raise SystemExit("Guild and role IDs must be numeric")
    os.umask(0o077)
    user = pwd.getpwnam("ss14-monitor")
    group = grp.getgrnam("ss14-monitor")
    props = subprocess.check_output([
        "systemctl", "show", "ss14-monitor.service", "--property=User",
        "--property=WorkingDirectory", "--property=StateDirectory"
    ], text=True)
    properties = dict(line.split("=", 1) for line in props.splitlines() if "=" in line)
    if properties != {"User": "ss14-monitor", "WorkingDirectory": "/opt/ss14-monitor", "StateDirectory": "ss14-monitor"}:
        raise SystemExit("Monitor settings changed; inspect before installation")
    source = Path(__file__).resolve().parent
    bot = Path("/opt/ss14-monitor/bot.py")
    token = Path("/etc/ss14-monitor/discord_token")
    if not token.is_file():
        raise SystemExit("Existing systemd credential file not found")
    original = bot.read_text(encoding="utf-8")
    patched = patch_monitor(original)
    for name in ("service.py", "discord_extension.py", "host_setup.py", "__init__.py"):
        ast.parse((source / name).read_text(encoding="utf-8"))

    config = Path("/etc/ss14-monitor")
    env = config / "subscriptions.env"
    tiers = config / "subscription-tiers.json"
    bot_env = config / "subscriptions-bot.env"
    state = Path("/var/lib/ss14-monitor/subscriptions")
    state.mkdir(parents=True, exist_ok=True, mode=0o700)
    os.chown(state, user.pw_uid, group.gr_gid)
    os.chmod(state, 0o700)
    if not env.exists():
        env.write_text(
            f"UNEON_API_KEY={secrets.token_urlsafe(48)}\n"
            "UNEON_DB=/var/lib/ss14-monitor/subscriptions/links.db\n"
            "UNEON_TIERS_FILE=/etc/ss14-monitor/subscription-tiers.json\n"
            f"DISCORD_GUILD_ID={args.guild}\nUNEON_PORT=8787\n", encoding="utf-8")
    if not tiers.exists():
        tiers.write_text(json.dumps([
            {"level": 1, "role_id": args.role, "permissions": ["supporter"]}
        ], indent=2) + "\n", encoding="utf-8")
    if not bot_env.exists():
        bot_env.write_text(
            "UNEON_SUBSCRIPTIONS_ENABLED=1\n"
            "UNEON_DB=/var/lib/ss14-monitor/subscriptions/links.db\n"
            f"DISCORD_GUILD_ID={args.guild}\n", encoding="utf-8")
    for path in (env, bot_env):
        os.chmod(path, 0o600)
    os.chown(tiers, 0, group.gr_gid)
    os.chmod(tiers, 0o640)

    package = bot.parent / "uneon_subscriptions"
    package.mkdir(exist_ok=True, mode=0o755)
    os.chmod(package, 0o755)
    for name in ("service.py", "discord_extension.py", "host_setup.py", "__init__.py"):
        destination = package / name
        shutil.copyfile(source / name, destination)
        os.chmod(destination, 0o644)

    stamp = datetime.now(timezone.utc).strftime("%Y%m%dT%H%M%S%fZ")
    backup = bot.with_name(f"bot.py.before-subscriptions-{stamp}")
    if patched != original:
        shutil.copy2(bot, backup)
        # Preserve original permissions and owner; only replace file contents.
        bot.write_text(patched, encoding="utf-8")
        print(f"Monitor backup: {backup}")

    dropin = Path("/etc/systemd/system/ss14-monitor.service.d")
    dropin.mkdir(exist_ok=True)
    (dropin / "uneon-subscriptions.conf").write_text(
        "[Service]\nEnvironmentFile=/etc/ss14-monitor/subscriptions-bot.env\n", encoding="utf-8")
    os.chmod(dropin / "uneon-subscriptions.conf", 0o644)
    Path("/etc/systemd/system/uneon-subscriptions.service").write_text("""[Unit]
Description=UNEON subscriptions API
Wants=network-online.target
After=network-online.target

[Service]
Type=simple
User=ss14-monitor
Group=ss14-monitor
WorkingDirectory=/opt/ss14-monitor
EnvironmentFile=/etc/ss14-monitor/subscriptions.env
LoadCredential=discord_token:/etc/ss14-monitor/discord_token
LoadCredential=subscription_tiers:/etc/ss14-monitor/subscription-tiers.json
ExecStart=/usr/bin/python3 /opt/ss14-monitor/uneon_subscriptions/service.py
Restart=on-failure
RestartSec=10
StateDirectory=ss14-monitor
UMask=0077
NoNewPrivileges=true
PrivateTmp=true
ProtectHome=true
ProtectSystem=strict

[Install]
WantedBy=multi-user.target
""", encoding="utf-8")
    os.chmod("/etc/systemd/system/uneon-subscriptions.service", 0o644)
    print("Staged successfully. No services restarted; API key was not printed.")
    print("Activate on VPS:")
    print("  systemctl daemon-reload")
    print("  systemctl enable --now uneon-subscriptions")
    print("  systemctl restart ss14-monitor")
    print("Check: systemctl is-active uneon-subscriptions ss14-monitor")
    print("Logs: journalctl -u uneon-subscriptions -u ss14-monitor -n 40 --no-pager")
    print("API listens only on 127.0.0.1:8787. Configure HTTPS or SSH tunnel before enabling SS14.")


if __name__ == "__main__":
    main()
