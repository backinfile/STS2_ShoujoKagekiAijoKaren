import unittest
from monitor import assess

class MonitorTests(unittest.TestCase):
    def test_healthy_and_thresholds(self):
        status = {"connections": 1, "max_connections": 80, "rooms": 0, "max_rooms": 16, "counters": {}}
        self.assertEqual(assess(status, {}, 160 * 3600, 2 * 1024**3), [])
        status.update(connections=64, rooms=16, rss_bytes=300*1024**2, open_fds=1700, loop_lag_seconds=1,
                      counters={"internal_errors_total":1,"rejected_rate_limited_total":60})
        self.assertEqual(set(assess(status, {}, 3600, 0)), {"certificate_expiring", "disk_low", "connection_capacity",
            "room_capacity", "memory_high", "fds_high", "event_loop_lag", "relay_errors", "abuse_spike"})
        self.assertNotIn("abuse_spike", assess(status, status, 160*3600, 2*1024**3))


class ObservationTests(unittest.IsolatedAsyncioTestCase):
    async def test_closed_endpoint_persists_failure_without_claiming_healthy(self):
        import json, socket, tempfile
        from pathlib import Path
        from monitor import observe
        # Reserve a local port without listening: no production service is affected.
        with socket.socket() as reserved, tempfile.TemporaryDirectory() as temporary:
            reserved.bind(("127.0.0.1", 0))
            port = reserved.getsockname()[1]
            state, summary = Path(temporary)/"state.json", Path(temporary)/"summary.json"
            result = await observe(f"wss://127.0.0.1:{port}/rooms", f"http://127.0.0.1:{port}/status", state, summary)
            self.assertEqual(result, 1)
            report=json.loads(state.read_text())
            self.assertEqual(set(report["alerts"]), {"status_unavailable", "tls_websocket_unavailable"})
            public=json.loads(summary.read_text())
            self.assertFalse(public["ok"])
            self.assertNotIn("status_error", public)
