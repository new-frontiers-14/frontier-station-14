"""UNEON subscription bridge. Discord credentials stay on the VPS, never in SS14."""
from __future__ import annotations

import hashlib
import hmac
import json
import os
import re
import secrets
import sqlite3
import threading
import time
import uuid
from contextlib import closing
from http.server import BaseHTTPRequestHandler, ThreadingHTTPServer
from pathlib import Path
from urllib.error import HTTPError, URLError
from urllib.request import Request, urlopen


class Rejected(Exception):
    pass


class Unavailable(Exception):
    pass


def account_id(value: str) -> str:
    return str(uuid.UUID(value))


class Store:
    def __init__(self, path: str, clock=time.time):
        self.path, self.clock = path, clock
        Path(path).parent.mkdir(parents=True, exist_ok=True)
        with closing(self.connect()) as db, db:
            db.executescript("""
                CREATE TABLE IF NOT EXISTS links (
                    ss14 TEXT PRIMARY KEY, discord TEXT NOT NULL UNIQUE);
                CREATE TABLE IF NOT EXISTS codes (
                    digest TEXT PRIMARY KEY, ss14 TEXT NOT NULL UNIQUE,
                    expires REAL NOT NULL);
                CREATE TABLE IF NOT EXISTS discord_codes (
                    digest TEXT PRIMARY KEY, discord TEXT NOT NULL UNIQUE,
                    expires REAL NOT NULL);
            """)

    def connect(self):
        return sqlite3.connect(self.path, timeout=10)

    def issue(self, ss14: str) -> str:
        ss14 = account_id(ss14)
        code = secrets.token_hex(8).upper()
        with closing(self.connect()) as db, db:
            db.execute("DELETE FROM codes WHERE ss14=? OR expires<=?", (ss14, self.clock()))
            db.execute("INSERT INTO codes VALUES (?, ?, ?)",
                       (hashlib.sha256(code.encode()).hexdigest(), ss14, self.clock() + 600))
        return code

    def link(self, code: str, discord: str):
        code = code.strip().upper()
        if not re.fullmatch(r"[0-9A-F]{16}", code) or not re.fullmatch(r"[0-9]{1,20}", discord):
            raise Rejected("Invalid or expired code")
        digest = hashlib.sha256(code.encode()).hexdigest()
        with closing(self.connect()) as db, db:
            db.execute("BEGIN IMMEDIATE")
            row = db.execute("SELECT ss14, expires FROM codes WHERE digest=?", (digest,)).fetchone()
            if not row or row[1] <= self.clock():
                raise Rejected("Invalid or expired code")
            # No silent transfers between Discord accounts. Unlink inside SS14 first.
            existing = db.execute("SELECT ss14, discord FROM links WHERE ss14=? OR discord=?",
                                  (row[0], discord)).fetchall()
            if any(pair != (row[0], discord) for pair in existing):
                raise Rejected("Account already linked; unlink in SS14 first")
            db.execute("INSERT OR IGNORE INTO links VALUES (?, ?)", (row[0], discord))
            db.execute("DELETE FROM codes WHERE digest=?", (digest,))

    def lookup(self, ss14: str):
        with closing(self.connect()) as db:
            row = db.execute("SELECT discord FROM links WHERE ss14=?", (account_id(ss14),)).fetchone()
            return row[0] if row else None

    def issue_discord(self, discord: str) -> str:
        if not re.fullmatch(r"[0-9]{1,20}", discord):
            raise ValueError("Invalid Discord ID")
        code = secrets.token_hex(8).upper()
        with closing(self.connect()) as db, db:
            db.execute("DELETE FROM discord_codes WHERE discord=? OR expires<=?", (discord, self.clock()))
            db.execute("INSERT INTO discord_codes VALUES (?, ?, ?)",
                       (hashlib.sha256(code.encode()).hexdigest(), discord, self.clock() + 600))
        return code

    def redeem_discord(self, code: str, ss14: str):
        ss14 = account_id(ss14)
        code = code.strip().upper()
        if not re.fullmatch(r"[0-9A-F]{16}", code):
            raise Rejected("Invalid or expired code")
        digest = hashlib.sha256(code.encode()).hexdigest()
        with closing(self.connect()) as db, db:
            db.execute("BEGIN IMMEDIATE")
            row = db.execute("SELECT discord, expires FROM discord_codes WHERE digest=?", (digest,)).fetchone()
            if not row or row[1] <= self.clock():
                raise Rejected("Invalid or expired code")
            pair = (ss14, row[0])
            existing = db.execute("SELECT ss14, discord FROM links WHERE ss14=? OR discord=?", pair).fetchall()
            if any(link != pair for link in existing):
                raise Rejected("Account already linked; unlink in SS14 first")
            db.execute("INSERT OR IGNORE INTO links VALUES (?, ?)", pair)
            db.execute("DELETE FROM discord_codes WHERE digest=?", (digest,))

    def unlink(self, ss14: str):
        with closing(self.connect()) as db, db:
            db.execute("DELETE FROM discord_codes WHERE discord IN (SELECT discord FROM links WHERE ss14=?)",
                       (account_id(ss14),))
            db.execute("DELETE FROM links WHERE ss14=?", (account_id(ss14),))
            db.execute("DELETE FROM codes WHERE ss14=?", (account_id(ss14),))


class RoleProvider:
    def __init__(self, token: str, guild: str, opener=urlopen):
        if not token or not re.fullmatch(r"[0-9]{1,20}", guild):
            raise ValueError("Discord token and guild ID are required")
        self.token, self.guild, self.opener = token, guild, opener
        self.lock = threading.Lock()
        self.retry_at = 0.0

    def roles(self, discord: str) -> list[str]:
        # Serialize requests and honor Discord's retry delay, without blocking SS14.
        with self.lock:
            if time.monotonic() < self.retry_at:
                raise Unavailable("Discord rate limit")
            request = Request(
                f"https://discord.com/api/v10/guilds/{self.guild}/members/{discord}",
                headers={"Authorization": f"Bot {self.token}", "User-Agent": "UNEONSubscriptions/1.0"})
            try:
                with self.opener(request, timeout=10) as response:
                    data = json.load(response)
                    if response.headers.get("X-RateLimit-Remaining") == "0":
                        self.retry_at = time.monotonic() + float(response.headers.get("X-RateLimit-Reset-After", "1"))
                roles = data["roles"]
                if not isinstance(roles, list) or not all(isinstance(r, str) for r in roles):
                    raise ValueError("Invalid member response")
                return roles
            except HTTPError as exc:
                # Only Unknown Member is a definitive absence. Unknown Guild is a configuration error.
                with exc:
                    try:
                        error = json.load(exc)
                    except (ValueError, OSError):
                        error = {}
                if exc.code == 404 and error.get("code") == 10007:
                    return []
                if exc.code == 429:
                    self.retry_at = time.monotonic() + max(1, float(error.get("retry_after", 60)))
                raise Unavailable("Discord request failed") from None
            except (URLError, TimeoutError, OSError, ValueError, KeyError):
                raise Unavailable("Discord unavailable or invalid response") from None


def validate_tiers(tiers):
    if not isinstance(tiers, list) or not tiers:
        raise ValueError("Configure at least one tier")
    levels = set()
    for tier in tiers:
        level = tier.get("level")
        if type(level) is not int or level <= 0 or level in levels:
            raise ValueError("Tier levels must be distinct positive integers")
        levels.add(level)
        if not re.fullmatch(r"[0-9]{1,20}", tier.get("role_id", "")):
            raise ValueError("Role IDs must be Discord snowflakes")
        permissions = tier.get("permissions")
        if not isinstance(permissions, list) or not all(isinstance(p, str) and re.fullmatch(r"[a-z0-9_.-]{1,80}", p) for p in permissions):
            raise ValueError("Invalid permissions")
    return tiers


class Bridge:
    def __init__(self, store: Store, provider, tiers, clock=time.time):
        self.store, self.provider = store, provider
        self.tiers, self.clock = validate_tiers(tiers), clock

    def entitlements(self, ss14):
        ss14 = account_id(ss14)
        discord = self.store.lookup(ss14)
        roles = self.provider.roles(discord) if discord else []
        matching = [t for t in self.tiers if t["role_id"] in roles]
        best = max(matching, key=lambda t: t["level"], default=None)
        # The highest matching tier wins; its permission list must explicitly include inherited benefits.
        return {"user_id": ss14, "linked": discord is not None,
                "tier": best["level"] if best else 0,
                "permissions": sorted(set(best["permissions"])) if best else [],
                "verified_at": int(self.clock())}


def make_handler(bridge: Bridge, api_key: str):
    if len(api_key) < 32:
        raise ValueError("API key must contain at least 32 characters")

    class Handler(BaseHTTPRequestHandler):
        def log_message(self, *args):
            pass  # Do not log codes, account IDs, keys or Discord payloads.

        def send_json(self, status, data):
            payload = json.dumps(data).encode()
            self.send_response(status)
            self.send_header("Content-Type", "application/json")
            self.send_header("Cache-Control", "no-store")
            self.send_header("Content-Length", str(len(payload)))
            self.end_headers()
            self.wfile.write(payload)

        def do_GET(self):
            self.handle_api()

        def do_POST(self):
            self.handle_api()

        def read_body(self):
            transfer = self.headers.get("Transfer-Encoding")
            lengths = self.headers.get_all("Content-Length", [])
            if transfer:
                if transfer.lower().strip() != "chunked" or lengths:
                    raise ValueError("Unsupported or ambiguous framing")
                body = bytearray()
                while True:
                    line = self.rfile.readline(130)
                    if not line.endswith(b"\r\n") or len(line) > 128:
                        raise ValueError("Invalid chunk header")
                    token = line[:-2].split(b";", 1)[0]
                    if not re.fullmatch(rb"[0-9a-fA-F]+", token):
                        raise ValueError("Invalid chunk size")
                    size = int(token, 16)
                    if size == 0:
                        trailer_size = 0
                        while True:
                            trailer = self.rfile.readline(1026)
                            trailer_size += len(trailer)
                            if not trailer.endswith(b"\r\n") or trailer_size > 1024:
                                raise ValueError("Invalid trailers")
                            if trailer == b"\r\n":
                                if not body:
                                    raise ValueError("Empty body")
                                return bytes(body)
                    if len(body) + size > 1024:
                        raise ValueError("Body too large")
                    chunk = self.rfile.read(size)
                    if len(chunk) != size or self.rfile.read(2) != b"\r\n":
                        raise ValueError("Incomplete chunk")
                    body.extend(chunk)
            if len(lengths) != 1 or not lengths[0].isdigit():
                raise ValueError("Content-Length required")
            size = int(lengths[0])
            if not 1 <= size <= 1024:
                raise ValueError("Invalid body size")
            body = self.rfile.read(size)
            if len(body) != size:
                raise ValueError("Incomplete body")
            return body

        def handle_api(self):
            self.connection.settimeout(15)
            if not hmac.compare_digest(self.headers.get("Authorization", ""), f"Bearer {api_key}"):
                self.send_json(401, {"error": "unauthorized"})
                return
            try:
                if self.command == "GET" and self.path.startswith("/v1/entitlements/"):
                    self.send_json(200, bridge.entitlements(self.path.removeprefix("/v1/entitlements/")))
                    return
                if self.command != "POST" or self.path not in ("/v1/link", "/v1/link-code", "/v1/unlink"):
                    self.send_json(404, {"error": "not_found"})
                    return
                data = json.loads(self.read_body())
                ss14 = account_id(data["user_id"])
                if self.path == "/v1/link":
                    bridge.store.redeem_discord(data["code"], ss14)
                    self.send_json(200, {"linked": True})
                elif self.path == "/v1/link-code":
                    self.send_json(200, {"code": bridge.store.issue(ss14), "expires_in": 600})
                else:
                    bridge.store.unlink(ss14)
                    self.send_json(200, {"unlinked": True})
            except (ValueError, KeyError, TypeError):
                self.send_json(400, {"error": "invalid_request"})
            except Rejected:
                self.send_json(409, {"error": "invalid_or_conflicting_code"})
            except Unavailable:
                self.send_json(503, {"error": "upstream_unavailable"})
            except sqlite3.Error:
                self.send_json(503, {"error": "storage_unavailable"})

    return Handler


def read_discord_token() -> str:
    """Match the existing monitor's systemd credential mechanism."""
    directory = os.environ.get("CREDENTIALS_DIRECTORY")
    if directory:
        path = Path(directory) / "discord_token"
        if path.is_file():
            token = path.read_text(encoding="utf-8").strip()
            if token:
                return token
    token = os.environ.get("DISCORD_TOKEN", "").strip()
    if not token:
        raise ValueError("Discord token missing: configure systemd credential discord_token")
    return token


def read_tiers():
    # The parent /etc directory may be root-only, even if the JSON file is group-readable.
    # systemd reads the source as root and exposes a private copy to the service user.
    directory = os.environ.get("CREDENTIALS_DIRECTORY")
    if directory:
        path = Path(directory) / "subscription_tiers"
        if path.is_file():
            return json.loads(path.read_text(encoding="utf-8"))
    return json.loads(Path(os.environ["UNEON_TIERS_FILE"]).read_text(encoding="utf-8"))


def from_env():
    os.umask(0o077)
    key = os.environ["UNEON_API_KEY"]
    tiers = read_tiers()
    bridge = Bridge(Store(os.environ.get("UNEON_DB", "/var/lib/uneon-subscriptions/links.db")),
                    RoleProvider(read_discord_token(), os.environ["DISCORD_GUILD_ID"]), tiers)
    return bridge, key


if __name__ == "__main__":
    bridge, key = from_env()
    # Bind locally: expose through an HTTPS reverse proxy or a private tunnel only.
    ThreadingHTTPServer(("127.0.0.1", int(os.environ.get("UNEON_PORT", "8787"))),
                        make_handler(bridge, key)).serve_forever()
