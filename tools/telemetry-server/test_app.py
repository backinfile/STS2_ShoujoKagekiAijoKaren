import copy
import hashlib
import json
from pathlib import Path
import tempfile
import threading
import time
import unittest
from urllib.error import HTTPError
from urllib.request import Request, urlopen

from app import Invalid, Server, Store, parse_history


def fixture():
    card = lambda cid: {"id": "CARD." + cid}
    pid = 76561199000000001
    return {"schema_version": 1, "platform_type": "steam", "game_mode": "standard",
            "win": True, "was_abandoned": False, "seed": "COLLECTOR-SELFTEST",
            "start_time": int(time.time()) - 1000, "run_time": 900, "ascension": 10,
            "build_id": "collector-test", "players": [{"id": pid, "character": "CHARACTER.KAREN",
            "deck": [card("KAREN_STRIKE"), card("KAREN_STRIKE"), card("KAREN_NEW_DAY")],
            "karen_shine_exhaust_cards": [card("KAREN_DROP_FUEL")]}],
            "map_point_history": [[{"map_point_type": "monster", "rooms": [{"room_type": "monster"}],
            "player_stats": [{"player_id": pid, "current_hp": 50, "max_hp": 72,
            "cards_gained": [card("KAREN_NEW_DAY")], "card_choices": [
                {"card": card("KAREN_NEW_DAY"), "was_picked": True},
                {"card": card("KAREN_BASEBALL"), "was_picked": False},
                {"card": card("KAREN_WAKE_UP")}]}]}]]}


class CollectorTests(unittest.TestCase):
    def setUp(self):
        self.tmp = tempfile.TemporaryDirectory()
        self.store = Store(Path(self.tmp.name) / "history.sqlite3")
        self.data = fixture()

    def tearDown(self):
        self.tmp.cleanup()

    def test_ingest_duplicates_conflicts_and_large_ids(self):
        status, result = self.store.upload(self.data)
        self.assertEqual(status, 201)
        self.assertEqual(result["quality"]["missing_was_picked"], 1)
        self.assertEqual(self.store.upload(self.data)[0], 200)
        changed = copy.deepcopy(self.data)
        changed["run_time"] += 1
        self.assertEqual(self.store.upload(changed)[0], 409)
        detail = self.store.detail(result["run_id"])
        self.assertEqual(json.loads(detail["facts"])["seats"][0]["id"], "76561199000000001")
        self.assertEqual(self.store.runs({})["total"], 1)

    def test_missing_results_are_unknown_and_native_choices_not_reward(self):
        self.store.upload(self.data)
        stats = self.store.stats({})
        cards = {c["id"]: c for c in stats["cards"]}
        self.assertEqual(cards["KAREN_STRIKE"]["held"], 1)
        self.assertEqual(cards["KAREN_DROP_FUEL"]["held"], 1)
        self.assertEqual(cards["KAREN_NEW_DAY"]["recorded_picked"], 1)
        self.assertEqual(cards["KAREN_BASEBALL"]["recorded_picked"], 0)
        self.assertEqual(cards["KAREN_WAKE_UP"]["unknown_picked"], 1)
        self.assertIsNone(cards["KAREN_NEW_DAY"]["pick_rate"])
        self.assertEqual(cards["KAREN_DROP_FUEL"]["win_rate"], 1)

    def test_explicit_free_reward_denominator(self):
        choices = self.data["map_point_history"][0][0]["player_stats"][0]["card_choices"]
        for choice in choices[:2]:
            choice["choice_kind"] = "free_reward"
        self.store.upload(self.data)
        cards = {c["id"]: c for c in self.store.stats({})["cards"]}
        self.assertEqual(cards["KAREN_NEW_DAY"]["pick_rate"], 1)
        self.assertEqual(cards["KAREN_BASEBALL"]["pick_rate"], 0)
        self.assertIsNone(cards["KAREN_WAKE_UP"]["pick_rate"])

    def test_early_death_preserved_and_abandon_only_excluded_in_stats(self):
        self.data["win"] = False
        self.store.upload(self.data)
        self.assertEqual(self.store.stats({})["seats"], 1)
        self.data["seed"] += "-abandon"
        self.data["was_abandoned"] = True
        self.store.upload(self.data)
        self.assertEqual(self.store.runs({})["total"], 2)
        self.assertEqual(self.store.stats({})["seats"], 1)
        self.assertEqual(self.store.stats({"sample": ["all"]})["seats"], 2)

    def test_coop_uses_karen_seats_not_team_size(self):
        second = copy.deepcopy(self.data["players"][0])
        second["id"] += 1
        self.data["players"].append(second)
        self.store.upload(self.data)
        self.assertEqual(self.store.stats({})["seats"], 2)
        self.assertEqual(self.store.runs({})["total"], 1)

    def test_parser_strips_shine_only_and_rejects_duplicate_keys(self):
        self.data["players"][0]["deck"][0]["props"] = {"ints": [
            {"name": "karen_shine_current", "value": 3}, {"name": "other", "value": 9}]}
        clean = parse_history(json.dumps(self.data).encode())
        self.assertEqual(clean["players"][0]["deck"][0]["props"]["ints"], [{"name": "other", "value": 9}])
        for body in (b'{"a":1,"a":2}', b'{"x":NaN}', b'not json'):
            with self.assertRaises(Invalid):
                parse_history(body)

    def test_backup_restores_records(self):
        self.store.upload(self.data)
        backup = Path(self.tmp.name) / "backup.sqlite3"
        self.store.backup(backup)
        self.assertEqual(Store(backup).runs({})["total"], 1)

    def test_scoped_token_binding_expiry_replay_and_pending_budget(self):
        body = json.dumps(self.data).encode()
        manifest = {"sha256": hashlib.sha256(body).hexdigest(), "bytes": len(body)}
        status, result = self.store.session(manifest, "ip-one")
        self.assertEqual(status, 201)
        token = result["upload_token"]
        self.assertFalse(self.store.authorize(token, body, "ip-two"))
        self.assertFalse(self.store.authorize(token, body + b" ", "ip-one"))
        self.assertTrue(self.store.authorize(token, body, "ip-one"))
        self.assertFalse(self.store.authorize(token, body, "ip-one"))
        token = self.store.session(manifest, "ip-one")[1]["upload_token"]
        with self.store.connect() as db:
            db.execute("UPDATE upload_sessions SET expires=0")
        self.assertFalse(self.store.authorize(token, body, "ip-one"))
        for _ in range(4):
            self.assertEqual(self.store.session(manifest, "ip-three")[0], 201)
        self.assertEqual(self.store.session(manifest, "ip-three")[0], 429)

    def test_http_auth_validation_upload_read_and_raw(self):
        with Server(("127.0.0.1", 0), self.store, "r" * 32) as server:
            thread = threading.Thread(target=server.serve_forever, daemon=True)
            thread.start()
            base = f"http://127.0.0.1:{server.server_port}"
            def request(path, data=None, headers=None):
                try:
                    with urlopen(Request(base + path, data=data, headers=headers or {}), timeout=5) as response:
                        return response.status, response.read()
                except HTTPError as error:
                    return error.code, error.read()
            try:
                self.assertEqual(request("/api/runs")[0], 404)
                self.assertEqual(request("/api/upload", b"{}")[0], 401)
                def upload(body):
                    manifest = json.dumps({"sha256": hashlib.sha256(body).hexdigest(), "bytes": len(body)}).encode()
                    status, response = request("/api/upload-session", manifest, {"Content-Type": "application/json"})
                    self.assertEqual(status, 201)
                    token = json.loads(response)["upload_token"]
                    return request("/api/upload", body, {"Content-Type": "application/json", "Authorization": "Bearer " + token})
                self.assertEqual(upload(b"{}")[0], 400)
                status, raw = upload(json.dumps(self.data).encode())
                self.assertEqual(status, 201)
                rid = json.loads(raw)["run_id"]
                read = {"X-Observatory-Access": "r" * 32}
                status, raw = request("/api/runs/" + rid, headers=read)
                self.assertEqual(status, 200)
                self.assertEqual(json.loads(raw)["history"]["players"][0]["id"], "76561199000000001")
                status, raw = request("/api/runs/" + rid + "/raw", headers=read)
                self.assertEqual(json.loads(raw)["players"][0]["id"], 76561199000000001)
                self.assertEqual(request("/../app.py", headers=read)[0], 404)
            finally:
                server.shutdown()
                thread.join(timeout=5)


if __name__ == "__main__":
    unittest.main()
