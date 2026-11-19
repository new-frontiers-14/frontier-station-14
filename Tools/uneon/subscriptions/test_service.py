import io
import importlib.util
import http.client
import json
import tempfile
import threading
import unittest
from types import SimpleNamespace
from contextlib import closing
from pathlib import Path
from urllib.error import HTTPError
from urllib.request import Request, urlopen
from unittest.mock import AsyncMock, Mock, patch

from service import Bridge, Rejected, RoleProvider, Store, ThreadingHTTPServer, Unavailable, make_handler, read_discord_token, read_tiers
from install_monitor import patch_monitor


USER = "fd4312aa-0909-43a7-8ca4-18a0c2f120b3"
OTHER = "bf4312aa-0909-43a7-8ca4-18a0c2f120b3"
TIERS = [
    {"level": 1, "role_id": "100", "permissions": ["supporter"]},
    {"level": 2, "role_id": "200", "permissions": ["supporter", "cosmetic.test"]},
]


class FakeDiscord:
    roles_value = ["100"]
    unavailable = False

    def roles(self, discord):
        if self.unavailable:
            raise Unavailable()
        return self.roles_value


class ServiceTests(unittest.TestCase):
    def setUp(self):
        self.tmp = tempfile.TemporaryDirectory()
        self.addCleanup(self.tmp.cleanup)
        self.now = 1000
        self.store = Store(str(Path(self.tmp.name) / "links.db"), lambda: self.now)
        self.discord = FakeDiscord()
        self.bridge = Bridge(self.store, self.discord, TIERS, lambda: self.now)

    def link(self, user=USER, discord="123"):
        self.store.link(self.store.issue(user), discord)

    def test_code_single_use(self):
        code = self.store.issue(USER)
        self.store.link(code.lower(), "123")
        self.assertEqual(self.store.lookup(USER), "123")
        with self.assertRaises(Rejected):
            self.store.link(code, "123")

    def test_code_expiry(self):
        code = self.store.issue(USER)
        self.now += 600
        with self.assertRaises(Rejected):
            self.store.link(code, "123")

    def test_new_code_invalidates_previous(self):
        old = self.store.issue(USER)
        self.store.issue(USER)
        with self.assertRaises(Rejected):
            self.store.link(old, "123")

    def test_code_not_stored_in_plaintext(self):
        code = self.store.issue(USER)
        with closing(self.store.connect()) as db:
            digest = db.execute("SELECT digest FROM codes").fetchone()[0]
        self.assertNotEqual(digest, code)

    def test_cannot_replace_another_discord(self):
        self.link()
        code = self.store.issue(USER)
        with self.assertRaises(Rejected):
            self.store.link(code, "456")
        self.assertEqual(self.store.lookup(USER), "123")

    def test_cannot_reuse_discord_for_second_account(self):
        self.link()
        with self.assertRaises(Rejected):
            self.store.link(self.store.issue(OTHER), "123")

    def test_unlink_removes_pending_code_and_allows_new_link(self):
        self.link()
        code = self.store.issue(USER)
        self.store.unlink(USER)
        self.assertIsNone(self.store.lookup(USER))
        with self.assertRaises(Rejected):
            self.store.link(code, "456")
        self.link(discord="456")
        self.assertEqual(self.store.lookup(USER), "456")

    def test_links_survive_restart(self):
        self.link()
        self.assertEqual(Store(self.store.path).lookup(USER), "123")

    def test_unlinked_no_rights_without_discord_request(self):
        self.discord.unavailable = True
        self.assertEqual(self.bridge.entitlements(USER)["tier"], 0)

    def test_highest_role_wins(self):
        self.link()
        self.discord.roles_value = ["100", "200"]
        state = self.bridge.entitlements(USER)
        self.assertEqual(state["tier"], 2)
        self.assertEqual(state["permissions"], ["cosmetic.test", "supporter"])

    def test_removed_role_revokes_rights(self):
        self.link()
        self.assertEqual(self.bridge.entitlements(USER)["tier"], 1)
        self.discord.roles_value = []
        state = self.bridge.entitlements(USER)
        self.assertEqual(state["tier"], 0)
        self.assertEqual(state["permissions"], [])

    def test_outage_is_not_a_negative_entitlement(self):
        self.link()
        self.discord.unavailable = True
        with self.assertRaises(Unavailable):
            self.bridge.entitlements(USER)

    def test_concurrent_code_consumption_has_one_winner(self):
        code = self.store.issue(USER)
        results = []
        def consume(discord):
            try:
                self.store.link(code, discord)
                results.append(True)
            except Rejected:
                results.append(False)
        threads = [threading.Thread(target=consume, args=(str(n),)) for n in (123, 456)]
        for thread in threads:
            thread.start()
        for thread in threads:
            thread.join()
        self.assertEqual(sorted(results), [False, True])

    def test_invalid_uuid_rejected(self):
        with self.assertRaises(ValueError):
            self.store.issue("player-name")

    def test_duplicate_tier_configuration_rejected(self):
        with self.assertRaises(ValueError):
            Bridge(self.store, self.discord, [TIERS[0], TIERS[0]])

    def test_http_authorization_and_outage(self):
        key = "k" * 40
        server = ThreadingHTTPServer(("127.0.0.1", 0), make_handler(self.bridge, key))
        thread = threading.Thread(target=server.serve_forever, daemon=True)
        thread.start()
        try:
            base = f"http://127.0.0.1:{server.server_port}"
            with self.assertRaises(HTTPError) as error:
                urlopen(base + "/v1/entitlements/" + USER)
            self.assertEqual(error.exception.code, 401)
            error.exception.close()
            request = Request(base + "/v1/link-code", json.dumps({"user_id": USER}).encode(),
                              {"Authorization": "Bearer " + key})
            with urlopen(request) as response:
                code = json.load(response)["code"]
            self.store.link(code, "123")
            request = Request(base + "/v1/entitlements/" + USER,
                              headers={"Authorization": "Bearer " + key})
            with urlopen(request) as response:
                self.assertEqual(json.load(response)["tier"], 1)
            self.discord.unavailable = True
            with self.assertRaises(HTTPError) as error:
                urlopen(request)
            self.assertEqual(error.exception.code, 503)
            error.exception.close()
        finally:
            server.shutdown()
            server.server_close()
            thread.join()

    def test_only_unknown_member_counts_as_absent(self):
        def opener_for(status, code):
            def fail(request, **kwargs):
                raise HTTPError(request.full_url, status, "Discord error", {},
                                io.BytesIO(json.dumps({"code": code}).encode()))
            return fail
        provider = RoleProvider("token", "123", opener_for(404, 10007))
        self.assertEqual(provider.roles("456"), [])
        for status, code in [(404, 10004), (403, 50001), (401, 0), (500, 0)]:
            with self.subTest(status=status):
                with self.assertRaises(Unavailable):
                    RoleProvider("token", "123", opener_for(status, code)).roles("456")

    def test_rate_limit_prevents_immediate_retry(self):
        calls = []
        def fail(request, **kwargs):
            calls.append(1)
            raise HTTPError(request.full_url, 429, "Rate limited", {},
                            io.BytesIO(b'{"retry_after": 60}'))
        provider = RoleProvider("token", "123", fail)
        for _ in range(2):
            with self.assertRaises(Unavailable):
                provider.roles("456")
        self.assertEqual(len(calls), 1)

    def test_systemd_credential_takes_priority_over_environment(self):
        Path(self.tmp.name, "discord_token").write_text("credential-token\n", encoding="utf-8")
        with patch.dict("os.environ", {"CREDENTIALS_DIRECTORY": self.tmp.name,
                                       "DISCORD_TOKEN": "environment-token"}, clear=True):
            self.assertEqual(read_discord_token(), "credential-token")

    def test_environment_fallback_and_missing_token(self):
        with patch.dict("os.environ", {"DISCORD_TOKEN": "environment-token"}, clear=True):
            self.assertEqual(read_discord_token(), "environment-token")
        with patch.dict("os.environ", {}, clear=True):
            with self.assertRaises(ValueError):
                read_discord_token()

    def test_tiers_credential_bypasses_inaccessible_config_source(self):
        Path(self.tmp.name, "subscription_tiers").write_text(json.dumps(TIERS), encoding="utf-8")
        with patch.dict("os.environ", {"CREDENTIALS_DIRECTORY": self.tmp.name,
                                       "UNEON_TIERS_FILE": "/inaccessible/config/tiers.json"}, clear=True):
            self.assertEqual(read_tiers(), TIERS)

    def test_tiers_direct_file_fallback_for_development(self):
        path = Path(self.tmp.name, "tiers.json")
        path.write_text(json.dumps(TIERS), encoding="utf-8")
        with patch.dict("os.environ", {"UNEON_TIERS_FILE": str(path)}, clear=True):
            self.assertEqual(read_tiers(), TIERS)

    def test_discord_first_code_is_single_use_and_persistent(self):
        code = self.store.issue_discord("123")
        restarted = Store(self.store.path, lambda: self.now)
        restarted.redeem_discord(code.lower(), USER)
        self.assertEqual(restarted.lookup(USER), "123")
        with self.assertRaises(Rejected):
            self.store.redeem_discord(code, USER)

    def test_discord_first_code_expires_and_new_code_replaces_it(self):
        old = self.store.issue_discord("123")
        new = self.store.issue_discord("123")
        with self.assertRaises(Rejected):
            self.store.redeem_discord(old, USER)
        self.now += 600
        with self.assertRaises(Rejected):
            self.store.redeem_discord(new, USER)

    def test_discord_first_cannot_replace_existing_ss14_or_discord_link(self):
        self.link()
        with self.assertRaises(Rejected):
            self.store.redeem_discord(self.store.issue_discord("456"), USER)
        with self.assertRaises(Rejected):
            self.store.redeem_discord(self.store.issue_discord("123"), OTHER)
        self.assertEqual(self.store.lookup(USER), "123")

    def test_unlink_cancels_outstanding_discord_code(self):
        self.link()
        code = self.store.issue_discord("123")
        self.store.unlink(USER)
        with self.assertRaises(Rejected):
            self.store.redeem_discord(code, OTHER)

    def test_discord_first_concurrent_redemption_has_one_winner(self):
        code = self.store.issue_discord("123")
        results = []
        def redeem(user):
            try:
                self.store.redeem_discord(code, user)
                results.append(True)
            except Rejected:
                results.append(False)
        threads = [threading.Thread(target=redeem, args=(user,)) for user in (USER, OTHER)]
        for thread in threads:
            thread.start()
        for thread in threads:
            thread.join()
        self.assertEqual(sorted(results), [False, True])

    def api_connection(self):
        server = ThreadingHTTPServer(("127.0.0.1", 0), make_handler(self.bridge, "k" * 40))
        thread = threading.Thread(target=server.serve_forever, daemon=True)
        thread.start()
        def cleanup():
            server.shutdown()
            server.server_close()
            thread.join()
        self.addCleanup(cleanup)
        connection = http.client.HTTPConnection("127.0.0.1", server.server_port, timeout=5)
        self.addCleanup(connection.close)
        return connection

    def test_discord_first_http_link_with_content_length(self):
        code = self.store.issue_discord("123")
        connection = self.api_connection()
        body = json.dumps({"user_id": USER, "code": code}).encode()
        connection.request("POST", "/v1/link", body=body,
                           headers={"Authorization": "Bearer " + "k" * 40})
        response = connection.getresponse()
        self.assertEqual(response.status, 200)
        self.assertEqual(json.load(response), {"linked": True})
        self.assertEqual(self.store.lookup(USER), "123")

    def test_discord_first_http_link_accepts_streamed_dotnet_style_body(self):
        code = self.store.issue_discord("123")
        connection = self.api_connection()
        body = json.dumps({"user_id": USER, "code": code}).encode()
        connection.request("POST", "/v1/link", body=iter([body[:20], body[20:]]),
                           headers={"Authorization": "Bearer " + "k" * 40}, encode_chunked=True)
        response = connection.getresponse()
        self.assertEqual(response.status, 200)
        self.assertEqual(json.load(response), {"linked": True})

    def test_streamed_body_is_bounded(self):
        connection = self.api_connection()
        connection.request("POST", "/v1/link", body=iter([b"x" * 1025]),
                           headers={"Authorization": "Bearer " + "k" * 40}, encode_chunked=True)
        self.assertEqual(connection.getresponse().status, 400)

    def test_ambiguous_body_framing_is_rejected(self):
        connection = self.api_connection()
        connection.putrequest("POST", "/v1/link")
        connection.putheader("Authorization", "Bearer " + "k" * 40)
        connection.putheader("Transfer-Encoding", "chunked")
        connection.putheader("Content-Length", "2")
        connection.endheaders()
        self.assertEqual(connection.getresponse().status, 400)

    def test_discord_first_bad_code_returns_conflict_not_server_error(self):
        connection = self.api_connection()
        connection.request("POST", "/v1/link", body=json.dumps({"user_id": USER, "code": "0" * 16}).encode(),
                           headers={"Authorization": "Bearer " + "k" * 40})
        self.assertEqual(connection.getresponse().status, 409)


class MonitorPatchTests(unittest.TestCase):
    ORIGINAL = '''class Monitor:
    async def setup_hook(self):
        self.poll_status.start()

    def observe_round(self):
        return "unchanged"
'''

    def test_patch_preserves_polling_and_round_logic(self):
        patched = patch_monitor(self.ORIGINAL)
        self.assertIn("self.poll_status.start()", patched)
        self.assertIn('return "unchanged"', patched)
        self.assertEqual(patched.count("await enable_subscription_link(self)"), 1)

    def test_patch_is_idempotent(self):
        once = patch_monitor(self.ORIGINAL)
        self.assertEqual(patch_monitor(once), once)

    def test_unknown_bot_layout_is_rejected(self):
        with self.assertRaises(ValueError):
            patch_monitor("class Unknown: pass")

    def test_ambiguous_bot_layout_is_rejected(self):
        with self.assertRaises(ValueError):
            patch_monitor(self.ORIGINAL + "\nclass Another:\n    def start(self):\n        self.poll_status.start()\n")


class HostSetupTests(unittest.IsolatedAsyncioTestCase):
    def setUp(self):
        fake_discord = SimpleNamespace(Object=lambda **kwargs: SimpleNamespace(**kwargs),
                                       AppCommandType=SimpleNamespace(chat_input=1))
        spec = importlib.util.spec_from_file_location("test_host_setup", Path(__file__).with_name("host_setup.py"))
        self.module = importlib.util.module_from_spec(spec)
        with patch.dict("sys.modules", {"discord": fake_discord}):
            spec.loader.exec_module(self.module)
        self.bot = SimpleNamespace(load_extension=AsyncMock(), unload_extension=AsyncMock(), tree=SimpleNamespace(
            get_commands=Mock(return_value=[SimpleNamespace(name="uneon_link")]),
            fetch_commands=AsyncMock(return_value=[]), sync=AsyncMock()))

    async def enable(self):
        with patch.dict("os.environ", {"UNEON_SUBSCRIPTIONS_ENABLED": "1", "DISCORD_GUILD_ID": "123"}, clear=True):
            await self.module.enable_subscription_link(self.bot)

    async def test_disabled_monitor_does_not_contact_discord(self):
        with patch.dict("os.environ", {}, clear=True):
            await self.module.enable_subscription_link(self.bot)
        self.bot.load_extension.assert_not_awaited()
        self.bot.tree.sync.assert_not_awaited()

    async def test_new_slash_command_is_synced_only_to_configured_guild(self):
        await self.enable()
        self.bot.tree.sync.assert_awaited_once()
        self.assertEqual(self.bot.tree.sync.call_args.kwargs["guild"].id, 123)

    async def test_unknown_remote_commands_are_not_deleted(self):
        self.bot.tree.fetch_commands.return_value = [SimpleNamespace(name="other_command", type=1)]
        await self.enable()
        self.bot.tree.sync.assert_not_awaited()
        self.bot.unload_extension.assert_awaited_once()

    async def test_discord_failure_does_not_prevent_monitor_startup(self):
        self.bot.tree.fetch_commands.side_effect = OSError("offline")
        await self.enable()
        self.bot.unload_extension.assert_awaited_once()

    async def test_existing_link_command_can_be_resynced(self):
        self.bot.tree.fetch_commands.return_value = [SimpleNamespace(name="uneon_link", type=1)]
        await self.enable()
        self.bot.tree.sync.assert_awaited_once()


if __name__ == "__main__":
    unittest.main()
