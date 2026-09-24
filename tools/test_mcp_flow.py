import json
from http.server import BaseHTTPRequestHandler, ThreadingHTTPServer
from pathlib import Path
import sys
import tempfile
from threading import Thread
import unittest

sys.path.insert(0, str(Path(__file__).resolve().parent))
from mcp_flow import FlowError, check_one, run_flow, run_step, validate_flow


class GameHandler(BaseHTTPRequestHandler):
    screen = "main"
    posts = 0

    def log_message(self, *args):
        pass

    def reply(self, body):
        data = json.dumps(body).encode("utf-8")
        self.send_response(200)
        self.send_header("Content-Type", "application/json")
        self.send_header("Content-Length", str(len(data)))
        self.end_headers()
        self.wfile.write(data)

    def do_GET(self):
        if self.path.startswith("/api/v1/settings"):
            self.reply({"status": "ok", "fullscreen": False})
        else:
            self.reply({"state_type": "menu", "menu_screen": type(self).screen})

    def do_POST(self):
        body = json.loads(self.rfile.read(int(self.headers["Content-Length"])))
        type(self).posts += 1
        if body == {"action": "menu_select", "option": "singleplayer"}:
            type(self).screen = "singleplayer"
        elif body == {"action": "menu_select", "option": "back"}:
            type(self).screen = "main"
        self.reply({"status": "ok"})


class FlowTests(unittest.TestCase):
    def setUp(self):
        GameHandler.screen = "main"
        GameHandler.posts = 0
        self.server = ThreadingHTTPServer(("127.0.0.1", 0), GameHandler)
        self.thread = Thread(target=self.server.serve_forever, daemon=True)
        self.thread.start()
        self.port = self.server.server_port

    def tearDown(self):
        self.server.shutdown()
        self.server.server_close()
        self.thread.join()

    def test_batch_wait_post_assert_and_report(self):
        flow = validate_flow({"targets": {"game": {"port": self.port}}, "steps": [
            {"op": "wait", "target": "game", "expect": [{"path": "/menu_screen", "eq": "main"}]},
            {"op": "post", "target": "game", "body": {"action": "menu_select", "option": "singleplayer"},
             "before": [{"path": "/menu_screen", "eq": "main"}],
             "after": [{"path": "/menu_screen", "eq": "singleplayer"}]},
            {"op": "post", "target": "game", "body": {"action": "menu_select", "option": "back"},
             "after": [{"path": "/menu_screen", "eq": "main"}]},
        ]})
        with tempfile.TemporaryDirectory() as temp:
            report = Path(temp) / "report.jsonl"
            run_flow(flow, report)
            rows = [json.loads(line) for line in report.read_text(encoding="utf-8").splitlines()]
        self.assertEqual([row["status"] for row in rows], ["passed"] * 3)
        self.assertEqual(GameHandler.posts, 2)

    def test_failed_precondition_stops_before_post(self):
        flow = validate_flow({"targets": {"game": {"port": self.port}}, "steps": [
            {"op": "post", "target": "game", "body": {"action": "menu_select", "option": "back"},
             "before": [{"path": "/menu_screen", "eq": "singleplayer"}]}
        ]})
        with tempfile.TemporaryDirectory() as temp:
            report = Path(temp) / "report.jsonl"
            with self.assertRaisesRegex(FlowError, "Precondition failed"):
                run_flow(flow, report)
            row = json.loads(report.read_text(encoding="utf-8"))
        self.assertEqual(row["status"], "failed")
        self.assertEqual(GameHandler.posts, 0)

    def test_post_transport_failure_is_not_retried(self):
        class UncertainClient:
            posts = 0

            def request(self, endpoint, body=None):
                if body is None:
                    return {"menu_screen": "main"}
                self.posts += 1
                raise FlowError("response lost")

        client = UncertainClient()
        with self.assertRaisesRegex(FlowError, "response lost"):
            run_step({"op": "post", "body": {"action": "menu_select", "option": "singleplayer"}},
                     client, "singleplayer")
        self.assertEqual(client.posts, 1)

    def test_json_pointer_and_invalid_flow(self):
        state = {"player": {"hand": [{"id": "A/B"}]}}
        self.assertTrue(check_one(state, {"path": "/player/hand/0/id", "eq": "A/B"})["passed"])
        self.assertTrue(check_one(state, {"path": "/missing", "exists": False})["passed"])
        with self.assertRaisesRegex(FlowError, "action body"):
            validate_flow({"targets": {"game": {"port": self.port}}, "steps": [
                {"op": "post", "target": "game", "body": {"option": "singleplayer"}}
            ]})

    def test_action_argument_comes_from_fresh_state(self):
        class Client:
            sent = None

            def request(self, endpoint, body=None):
                if body is None:
                    return {"player": {"hand": [{"index": 3, "id": "KAREN_STRIKE"}]}}
                self.sent = body
                return {"status": "ok"}

        client = Client()
        run_step({"op": "post", "body": {"action": "play_card"},
                  "before": [{"path": "/player/hand/0/id", "eq": "KAREN_STRIKE"}],
                  "body_from_state": {"card_index": "/player/hand/0/index"}},
                 client, "singleplayer")
        self.assertEqual(client.sent, {"action": "play_card", "card_index": 3})


if __name__ == "__main__":
    unittest.main()
