from dataclasses import replace
from types import SimpleNamespace
import unittest
from websockets.datastructures import Headers
from security import AbuseGuard, Bucket, Limits, RoomError, client_address

class SecurityTests(unittest.TestCase):
    def setUp(self):
        self.now = 10000.0
        self.guard = AbuseGuard(clock=lambda: self.now)
    def test_cache_bound_and_expiry_preserve_live_peer(self):
        self.guard = AbuseGuard(replace(Limits(), cache_entries=2), clock=lambda: self.now)
        active = self.guard.open("a", 80)
        old = self.guard.open("b", 80); self.guard.close(old)
        with self.assertRaises(RoomError): self.guard.open("c", 80)
        self.now += 1801; self.guard.prune(force=True)
        self.assertIn("a", self.guard.addresses)
        self.guard.open("c", 80); self.assertEqual(len(self.guard.addresses), 2)
        self.guard.close(active)
    def test_pending_budget_release_is_idempotent(self):
        self.guard = AbuseGuard(replace(Limits(), pending_connections=1), clock=lambda: self.now)
        lease = self.guard.open("a", 80)
        with self.assertRaises(RoomError): self.guard.open("b", 80)
        self.guard.authenticate(lease, 1)
        pending = self.guard.open("b", 80)
        self.guard.close(pending); self.guard.close(pending); self.guard.close(lease)
        self.assertEqual((self.guard.connections, self.guard.pending), (0, 0))
    def test_identity_create_budget_survives_ip_change(self):
        self.guard = AbuseGuard(replace(Limits(), identity_creates_per_ten_minutes=1), clock=lambda: self.now)
        lease = self.guard.open("a", 80); self.guard.create(lease, 1); self.guard.close(lease)
        lease = self.guard.open("b", 80)
        with self.assertRaises(RoomError): self.guard.create(lease, 1)
        self.now += 601; self.guard.create(lease, 1)
    def test_byte_budget_shared_by_ip_and_recovers(self):
        self.guard = AbuseGuard(replace(Limits(), ip_byte_burst=20, ip_bytes_per_second=1), clock=lambda: self.now)
        a = self.guard.open("same", 80); b = self.guard.open("same", 80)
        self.guard.consume(a, 15)
        with self.assertRaises(RoomError): self.guard.consume(b, 10)
        self.now += 10; self.guard.consume(b, 10)
    def test_clock_rollback_never_immediately_grants_tokens(self):
        bucket = Bucket(2, 2, self.now)
        self.assertTrue(bucket.take(2, self.now)); self.assertFalse(bucket.take(1, self.now - 20))
    def test_public_peer_cannot_spoof_proxy_header(self):
        socket = SimpleNamespace(remote_address=("203.0.113.1", 1), request=SimpleNamespace(headers=Headers({"X-Karen-Client-IP":"198.51.100.1"})))
        self.assertEqual(client_address(socket), "203.0.113.1")
        socket.remote_address = ("127.0.0.1", 1); self.assertEqual(client_address(socket), "198.51.100.1")
        socket.request.headers["X-Karen-Client-IP"] = "198.51.100.2"
        with self.assertRaises(RoomError): client_address(socket)
if __name__ == "__main__": unittest.main()
