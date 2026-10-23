import asyncio
import json
import secrets
import struct
import unittest
from unittest.mock import patch
from dataclasses import replace
from security import AbuseGuard, Limits

from websockets.asyncio.client import connect
from websockets.asyncio.server import serve
from websockets.exceptions import ConnectionClosed

from server import RoomService, MAX_FRAME, SUBPROTOCOL, HEADER


class RelayTests(unittest.IsolatedAsyncioTestCase):
    async def asyncSetUp(self):
        self.service = RoomService()
        self.server = await serve(self.service.handle, "127.0.0.1", 0, subprotocols=[SUBPROTOCOL],
                                  process_request=self.service.health, origins=[None], compression=None,
                                  max_size=4096, max_queue=1, close_timeout=.2)
        self.url = f"ws://127.0.0.1:{self.server.sockets[0].getsockname()[1]}/rooms"
        self.clients = []

    async def asyncTearDown(self):
        for socket in self.clients:
            await socket.close()
        self.server.close()
        await self.server.wait_closed()

    async def open(self, op="create", code=None, credential=None, version="test-native-v1", address=None):
        socket = await connect(self.url, subprotocols=[SUBPROTOCOL], compression=None,
                               additional_headers={} if address is None else {"X-Karen-Client-IP": address})
        self.clients.append(socket)
        try:
            await socket.send(json.dumps({"op": op, "protocol": 1, "credential": credential or secrets.token_hex(32),
                                          "version": version, "code": code}))
        except ConnectionClosed:
            pass  # Admission may reject and close before the client sends its greeting.
        return socket, json.loads(await socket.recv())

    async def control(self, socket):
        return json.loads(await asyncio.wait_for(socket.recv(), 2))

    async def test_probe_has_no_room_side_effect(self):
        socket = await connect(self.url, subprotocols=[SUBPROTOCOL])
        self.clients.append(socket)
        await socket.send('{"op":"probe","protocol":1}')
        ready = await self.control(socket)
        self.assertEqual(ready["op"], "ready")
        self.assertEqual(ready["expires_on"], "2028-07")
        self.assertFalse(self.service.rooms)

    async def test_host_and_three_clients_capacity(self):
        host, welcome = await self.open()
        self.assertRegex(welcome["code"], r"^[0-9]{4}$")
        for _ in range(3):
            client, result = await self.open("join", welcome["code"])
            self.assertEqual(result["host"], welcome["id"])
            self.assertEqual((await self.control(host))["id"], result["id"])
        _, rejected = await self.open("join", welcome["code"])
        self.assertEqual(rejected["error"], "room_full")

    async def test_native_packets_both_directions_preserve_mode_channel_and_identity(self):
        host, room = await self.open()
        client, joined = await self.open("join", room["code"])
        await self.control(host)
        for mode in (1, 2):
            await client.send(HEADER.pack(int(room["id"]), mode, 1) + b"native-client-packet")
            received = await asyncio.wait_for(host.recv(), 2)
            self.assertEqual(HEADER.unpack_from(received), (int(joined["id"]), mode, 1))
            self.assertEqual(received[10:], b"native-client-packet")
            await host.send(HEADER.pack(int(joined["id"]), mode, 0) + b"native-host-packet")
            received = await asyncio.wait_for(client.recv(), 2)
            self.assertEqual(HEADER.unpack_from(received), (int(room["id"]), mode, 0))
            self.assertEqual(received[10:], b"native-host-packet")

    async def test_broadcast_reaches_all_room_clients(self):
        host, room = await self.open()
        peers = []
        for _ in range(2):
            client, result = await self.open("join", room["code"])
            await self.control(host)
            peers.append(client)
        await host.send(HEADER.pack(0, 2, 0) + b"broadcast")
        for peer in peers:
            self.assertEqual((await peer.recv())[10:], b"broadcast")

    async def test_wrong_version_is_rejected_before_native_packets(self):
        host, room = await self.open()
        _, result = await self.open("join", room["code"], version="incompatible")
        self.assertEqual(result["error"], "version_mismatch")
        self.assertEqual(len(self.service.rooms[room["code"]].peers), 1)

    async def test_room_codes_validate_ascii_and_unknown(self):
        for code, error in [("123", "invalid_code"), ("锛戯紥锛擄紨", "invalid_code"),
                            ("abcd", "invalid_code"), ("12345678", "invalid_code"), ("8765", "room_not_found")]:
            _, result = await self.open("join", code)
            self.assertEqual(result["error"], error)

    async def test_same_identity_cannot_connect_twice(self):
        credential = secrets.token_hex(32)
        host, room = await self.open(credential=credential)
        _, result = await self.open("join", room["code"], credential=credential)
        self.assertEqual(result["error"], "already_connected")

    async def test_closed_room_rejects_new_players_but_allows_rejoin(self):
        host, room = await self.open()
        credential = secrets.token_hex(32)
        client, joined = await self.open("join", room["code"], credential=credential)
        await self.control(host)
        await client.close()
        self.assertEqual((await self.control(host))["op"], "peer_left")
        await host.send('{"op":"set_closed","closed":true}')
        # Ordering is guaranteed on the host connection; let it consume the control.
        await asyncio.sleep(.02)
        _, failed = await self.open("join", room["code"])
        self.assertEqual(failed["error"], "room_started")
        returning, accepted = await self.open("join", room["code"], credential=credential)
        self.assertEqual(accepted["id"], joined["id"])
        self.assertEqual((await self.control(host))["op"], "peer_joined")

    async def test_host_exit_closes_and_removes_room(self):
        host, room = await self.open()
        client, _ = await self.open("join", room["code"])
        await self.control(host)
        await host.close()
        self.assertEqual((await self.control(client))["op"], "host_left")
        _, failed = await self.open("join", room["code"])
        self.assertEqual(failed["error"], "room_not_found")

    async def test_clients_cannot_send_to_other_rooms_or_spoof_sender(self):
        host, room = await self.open()
        client, _ = await self.open("join", room["code"])
        await self.control(host)
        other_host, other = await self.open()
        await client.send(HEADER.pack(int(other["id"]), 2, 0) + b"forbidden")
        self.assertEqual((await self.control(client))["error"], "invalid_target")
        self.assertEqual((await self.control(host))["op"], "peer_left")
        self.assertTrue(other["code"] in self.service.rooms)

    async def test_kick_prevents_same_identity_rejoining(self):
        host, room = await self.open()
        credential = secrets.token_hex(32)
        client, joined = await self.open("join", room["code"], credential=credential)
        await self.control(host)
        await host.send(json.dumps({"op":"kick","id":joined["id"],"reason":5}))
        self.assertEqual((await self.control(client))["op"], "disconnected")
        await self.control(host)
        _, failed = await self.open("join", room["code"], credential=credential)
        self.assertEqual(failed["error"], "kicked")

    async def test_malformed_envelopes_are_rejected(self):
        for packet in [b"small", HEADER.pack(0, 0, 0) + b"x", HEADER.pack(0, 2, 32) + b"x"]:
            host, _ = await self.open()
            await host.send(packet)
            self.assertEqual((await self.control(host))["error"], "invalid_packet")

    async def test_malformed_control_and_handshake(self):
        host, _ = await self.open()
        await host.send('{"op":"set_closed","closed":"yes"}')
        self.assertEqual((await self.control(host))["error"], "invalid_message")
        _, failed = await self.open(op="unknown")
        self.assertEqual(failed["error"], "bad_handshake")

    async def test_room_capacity_limit_is_bounded(self):
        self.service.max_rooms = 1
        await self.open()
        _, failed = await self.open()
        self.assertEqual(failed["error"], "server_full")

    async def test_per_ip_connections_release_after_disconnect(self):
        self.service.guard = AbuseGuard(replace(Limits(), per_ip_connections=1))
        host, _ = await self.open()
        _, failed = await self.open()
        self.assertEqual(failed["error"], "rate_limited")
        await host.close(); await asyncio.sleep(.02)
        _, accepted = await self.open()
        self.assertEqual(accepted["op"], "welcome")

    async def test_identity_cannot_create_another_room_while_connected(self):
        credential = secrets.token_hex(32)
        await self.open(credential=credential)
        _, failed = await self.open(credential=credential)
        self.assertEqual(failed["error"], "already_connected")
        self.assertEqual(len(self.service.rooms), 1)

    async def test_create_rate_survives_rotated_credentials(self):
        self.service.guard = AbuseGuard(replace(Limits(), creates_per_ten_minutes=1))
        host, _ = await self.open()
        await host.close(); await asyncio.sleep(.02)
        _, failed = await self.open()
        self.assertEqual(failed["error"], "rate_limited")

    async def test_failed_joins_cool_down_without_blocking_other_ip(self):
        self.service.guard = AbuseGuard(replace(Limits(), failed_joins_per_five_minutes=2))
        host, room = await self.open(address="198.51.100.1")
        for _ in range(2):
            _, failed = await self.open("join", room["code"], version="wrong", address="198.51.100.2")
            self.assertEqual(failed["error"], "version_mismatch")
        _, failed = await self.open("join", room["code"], address="198.51.100.2")
        self.assertEqual(failed["error"], "join_cooldown")
        _, accepted = await self.open("join", room["code"], address="198.51.100.3")
        self.assertEqual(accepted["op"], "welcome")
        self.assertEqual((await self.control(host))["op"], "peer_joined")

    async def test_message_flood_disconnects_only_offending_peer(self):
        self.service.guard = AbuseGuard(replace(Limits(), message_burst=2, messages_per_second=.001))
        host, room = await self.open()
        client, _ = await self.open("join", room["code"])
        await self.control(host)
        for _ in range(3): await client.send(HEADER.pack(int(room["id"]), 2, 0) + b"payload")
        self.assertEqual((await self.control(client))["error"], "rate_limited")
        for _ in range(2): self.assertIsInstance(await host.recv(), bytes)
        self.assertEqual((await self.control(host))["op"], "peer_left")
        self.assertIn(room["code"], self.service.rooms)

    async def test_large_message_before_auth_is_closed(self):
        socket = await connect(self.url, subprotocols=[SUBPROTOCOL]); self.clients.append(socket)
        await socket.send(b"x" * 8192)
        with self.assertRaises(ConnectionClosed): await socket.recv()
        self.assertEqual(socket.close_code, 1009)

    async def test_large_native_message_after_auth_still_works(self):
        host, room = await self.open()
        client, _ = await self.open("join", room["code"]); await self.control(host)
        payload = b"n" * (256 * 1024)
        await client.send(HEADER.pack(int(room["id"]), 2, 0) + payload)
        self.assertEqual((await host.recv())[10:], payload)

    async def test_member_history_bounded_but_rejoin_available(self):
        self.service.guard = AbuseGuard(replace(Limits(), room_identities=2))
        host, room = await self.open(); credential = secrets.token_hex(32)
        client, _ = await self.open("join", room["code"], credential=credential); await self.control(host)
        await client.close(); await self.control(host)
        _, failed = await self.open("join", room["code"])
        self.assertEqual(failed["error"], "room_full")
        _, accepted = await self.open("join", room["code"], credential=credential)
        self.assertEqual(accepted["op"], "welcome")

    async def http(self, path, forwarded=False, method="GET"):
        reader, writer = await asyncio.open_connection("127.0.0.1", self.server.sockets[0].getsockname()[1])
        header = "X-Karen-Client-IP: 198.51.100.1\r\n" if forwarded else ""
        writer.write((f"{method} {path} HTTP/1.1\r\nHost: localhost\r\nConnection: close\r\n" + header + "\r\n").encode())
        await writer.drain(); response = await asyncio.wait_for(reader.read(), 2)
        writer.close(); await writer.wait_closed()
        return response.decode()

    async def test_metrics_private_and_contain_no_player_identity(self):
        credential = secrets.token_hex(32)
        _, room = await self.open(credential=credential)
        response = await self.http("/status")
        self.assertTrue(response.startswith("HTTP/1.1 200"))
        snapshot = json.loads(response.split("\r\n\r\n", 1)[1])
        self.assertEqual(snapshot["rooms"], 1)
        self.assertEqual(snapshot["counters"]["rooms_created_total"], 1)
        self.assertNotIn(credential, response); self.assertNotIn(room["code"], response)
        self.assertIn("karen_rooms_rooms 1", await self.http("/metrics"))
        for path in ("/metrics", "/status"):
            self.assertTrue((await self.http(path, True)).startswith("HTTP/1.1 404"))
        self.assertNotIn('"connections"', await self.http("/healthz"))

    async def test_four_digit_leading_zero_and_collision_retry(self):
        with patch("server.secrets.randbelow", side_effect=[7, 7, 8]):
            _, first = await self.open()
            _, second = await self.open()
        self.assertEqual(first["code"], "0007")
        self.assertEqual(second["code"], "0008")
        self.assertEqual(len(self.service.rooms), 2)

    async def test_public_status_masks_on_server_and_tracks_room_lifecycle(self):
        credential = secrets.token_hex(32)
        host, room = await self.open(credential=credential, address="198.51.100.9")
        response = await self.http("/api/status", forwarded=True)
        self.assertIn("Content-Type: application/json; charset=utf-8", response)
        snapshot = json.loads(response.split("\r\n\r\n", 1)[1])
        self.assertNotIn("expires_on", snapshot)
        public_room = snapshot["rooms"][0]
        self.assertEqual(public_room["display_code"], "••" + room["code"][-2:])
        self.assertEqual(set(public_room), {"id", "display_code", "players", "capacity", "started", "age_seconds", "idle_seconds"})
        self.assertNotEqual(public_room["id"], room["code"])
        for secret in (credential, room["id"], "198.51.100.9"):
            self.assertNotIn(secret, response)
        self.assertNotIn('"code":', response)
        self.assertNotIn('"' + room["code"] + '"', response)
        self.assertEqual(public_room["players"], 1)
        client, _ = await self.open("join", room["code"]); await self.control(host)
        await host.send('{"op":"set_closed","closed":true}'); await asyncio.sleep(.02)
        updated = self.service.public_snapshot()
        self.assertEqual(updated["players"], 2)
        self.assertTrue(updated["rooms"][0]["started"])
        self.assertEqual(updated["rooms"][0]["id"], public_room["id"])
        await host.close(); await asyncio.sleep(.02)
        self.assertEqual(self.service.public_snapshot()["rooms"], [])

    async def test_idle_expiry_releases_room_and_connections(self):
        host, room = await self.open()
        existing = self.service.rooms[room["code"]]
        await self.service.reap_once(existing.active_at + 1801)
        await host.wait_closed(); await asyncio.sleep(.02)
        self.assertFalse(self.service.rooms)
        self.assertEqual(self.service.connections, 0)
        self.assertEqual(self.service.counters["rooms_expired_total"], 1)


if __name__ == "__main__":
    unittest.main(verbosity=2)
