"""Room-code relay for the native STS2 host/client transport. No game rules run here."""
from __future__ import annotations

import argparse
import asyncio
from collections import Counter, deque
from pathlib import Path
import os
from dataclasses import dataclass, field
import hashlib
import json
import logging
import secrets
import signal
import struct
import time

from websockets.asyncio.server import serve
from websockets.exceptions import ConnectionClosed

from security import AbuseGuard, Bucket, Lease, RoomError, client_address

PROTOCOL = 1
SUBPROTOCOL = "karen-rooms-v1"
MAX_FRAME = 2 * 1024 * 1024
HEADER = struct.Struct("!QBB")  # peer ID, native transfer mode, native channel
LOG = logging.getLogger("karen.rooms")


@dataclass(eq=False)
class Peer:
    socket: object
    id: int
    room: Room | None = None
    host: bool = False
    lease: Lease | None = None

    async def send(self, value):
        wire = json.dumps(value, separators=(",", ":")) if isinstance(value, dict) else value
        await asyncio.wait_for(self.socket.send(wire), 5)


@dataclass
class Room:
    code: str
    version: str
    host: Peer
    peers: dict[int, Peer] = field(default_factory=dict)
    seen: set[int] = field(default_factory=set)
    kicked: set[int] = field(default_factory=set)
    closed: bool = False
    created: float = field(default_factory=time.monotonic)
    active_at: float = field(default_factory=time.monotonic)
    view_id: str = field(default_factory=lambda: secrets.token_hex(8))
    egress: Bucket = field(default_factory=lambda: Bucket(16 * MAX_FRAME, 4, time.monotonic()))


class RoomService:
    def __init__(self, max_rooms=16, max_connections=80, room_hours=48, limits=None):
        self.rooms: dict[str, Room] = {}
        self.max_rooms, self.max_connections = max_rooms, max_connections
        self.room_seconds = room_hours * 3600
        self.guard = AbuseGuard(limits)
        self.counters = Counter()
        self.started = time.monotonic()
        self.loop_lag = deque(maxlen=60)
        self.idle_seconds = 1800

    @property
    def connections(self):
        return self.guard.connections

    async def reject(self, socket, error):
        try:
            await asyncio.wait_for(socket.send(json.dumps({"op": "error", "error": error})), 2)
            await socket.close(4000, error)
        except (ConnectionClosed, TimeoutError):
            pass

    async def handle(self, socket):
        peer = lease = None
        join_attempt = False
        try:
            lease = self.guard.open(client_address(socket), self.max_connections)
            self.counters["connections_accepted_total"] += 1
            raw = await asyncio.wait_for(socket.recv(), 10)
            if not isinstance(raw, str) or len(raw) > 4096:
                raise RoomError("bad_handshake")
            hello = json.loads(raw)
            if not isinstance(hello, dict) or hello.get("protocol") != PROTOCOL:
                raise RoomError("protocol_mismatch")
            if hello.get("op") == "probe":
                self.counters["probes_total"] += 1
                await socket.send(json.dumps({"op": "ready", "protocol": PROTOCOL, "expires_on": "2028-07"}))
                await socket.close(1000, "probe_complete")
                return
            credential, version = hello.get("credential"), hello.get("version")
            if not isinstance(credential, str) or len(credential) != 64:
                raise RoomError("bad_handshake")
            try:
                identity = hashlib.sha256(bytes.fromhex(credential)).digest()
            except ValueError:
                raise RoomError("bad_handshake") from None
            if not isinstance(version, str) or not 1 <= len(version) <= 160:
                raise RoomError("bad_handshake")
            peer_id = (int.from_bytes(identity[:8], "big") & ((1 << 63) - 1)) or 2
            self.guard.check_identity(peer_id)
            peer = Peer(socket, peer_id, lease=lease)
            if hello.get("op") == "create":
                if len(self.rooms) >= self.max_rooms:
                    raise RoomError("server_full")
                self.guard.create(lease, peer_id)
                code = f"{secrets.randbelow(10_000):04d}"
                while code in self.rooms:
                    code = f"{secrets.randbelow(10_000):04d}"
                room = Room(code, version, peer)
                room.peers[peer.id], peer.room, peer.host = peer, room, True
                room.seen.add(peer.id)
                self.rooms[code] = room
                self.counters["rooms_created_total"] += 1
            elif hello.get("op") == "join":
                self.guard.join(lease)
                join_attempt = True
                code = hello.get("code")
                if not isinstance(code, str) or len(code) != 4 or not code.isascii() or not code.isdigit():
                    raise RoomError("invalid_code")
                room = self.rooms.get(code)
                if room is None:
                    raise RoomError("room_not_found")
                if room.version != version:
                    raise RoomError("version_mismatch")
                if peer.id in room.peers:
                    raise RoomError("already_connected")
                if peer.id in room.kicked:
                    raise RoomError("kicked")
                if room.closed and peer.id not in room.seen:
                    raise RoomError("room_started")
                if len(room.peers) >= 4 or (peer.id not in room.seen and len(room.seen) >= self.guard.limits.room_identities):
                    raise RoomError("room_full")
                room.peers[peer.id], peer.room = peer, room
                room.seen.add(peer.id)
                join_attempt = False
                room.active_at = time.monotonic()
                self.counters["joins_total"] += 1
            else:
                raise RoomError("bad_handshake")
            self.guard.authenticate(lease, peer_id)
            # Only authenticated peers may send full native packets. The parser's
            # default is 4 KiB; changing this public Sans-I/O limit is pinned/tested.
            socket.protocol.max_size = MAX_FRAME
            room = peer.room
            await peer.send({"op": "welcome", "code": room.code, "id": str(peer.id),
                             "host": str(room.host.id),
                             "peers": [str(p) for p in room.peers if p != peer.id]})
            if not peer.host:
                await room.host.send({"op": "peer_joined", "id": str(peer.id)})
            async for wire in socket:
                if isinstance(wire, str) and len(wire) > 4096:
                    raise RoomError("invalid_message")
                size = len(wire) if isinstance(wire, bytes) else len(wire.encode("utf-8"))
                self.counters["messages_in_total"] += 1
                self.counters["bytes_in_total"] += size
                self.guard.consume(lease, size)
                await self.dispatch(peer, wire)
                room.active_at = time.monotonic()
        except RoomError as error:
            self.counters["rejected_" + str(error) + "_total"] += 1
            if join_attempt and lease is not None:
                self.guard.failed_join(lease)
            await self.reject(socket, str(error))
        except (ValueError, TypeError, KeyError, json.JSONDecodeError):
            self.counters["rejected_invalid_message_total"] += 1
            if join_attempt and lease is not None: self.guard.failed_join(lease)
            await self.reject(socket, "invalid_message")
        except (ConnectionClosed, TimeoutError):
            self.counters["connections_closed_total"] += 1
        except Exception:
            self.counters["internal_errors_total"] += 1
            LOG.exception("relay operation failed")
        finally:
            try:
                if peer and peer.room:
                    await self.leave(peer)
            finally:
                if lease is not None:
                    self.guard.close(lease)

    async def dispatch(self, peer, wire):
        room = peer.room
        if isinstance(wire, bytes):
            if not HEADER.size < len(wire) <= MAX_FRAME:
                raise RoomError("invalid_packet")
            target, mode, channel = HEADER.unpack_from(wire)
            if mode not in (1, 2) or channel > 31:
                raise RoomError("invalid_packet")
            if peer.host:
                if target == peer.id or (target and target not in room.peers):
                    raise RoomError("invalid_target")
                recipients = [p for p in room.peers.values() if p != peer and (target == 0 or p.id == target)]
            else:
                if target not in (0, room.host.id):
                    raise RoomError("invalid_target")
                recipients = [room.host]
            packet = HEADER.pack(peer.id, mode, channel) + wire[HEADER.size:]
            if not room.egress.take(len(packet) * len(recipients), time.monotonic()):
                raise RoomError("rate_limited")
            for recipient in recipients:
                try:
                    await recipient.send(packet)
                    self.counters["messages_out_total"] += 1
                    self.counters["bytes_out_total"] += len(packet)
                except (ConnectionClosed, TimeoutError):
                    # A slow/departed receiver cannot disconnect the sender.
                    await recipient.socket.close(4001, "receiver_unavailable")
        else:
            if len(wire) > 4096:
                raise RoomError("invalid_message")
            control = json.loads(wire)
            if not isinstance(control, dict):
                raise RoomError("invalid_message")
            operation = control.get("op")
            if operation == "set_closed" and peer.host and isinstance(control.get("closed"), bool):
                room.closed = control["closed"]
            elif operation == "kick" and peer.host:
                try:
                    target = int(control.get("id", ""))
                except (ValueError, TypeError):
                    raise RoomError("invalid_message") from None
                if target == peer.id or target not in room.peers:
                    raise RoomError("invalid_target")
                victim = room.peers[target]
                room.kicked.add(target)
                reason = control.get("reason", 0)
                if type(reason) is not int or not 0 <= reason <= 255:
                    raise RoomError("invalid_message")
                await victim.send({"op": "disconnected", "reason": reason})
                await victim.socket.close(4002, "kicked")
            elif operation == "leave":
                await peer.socket.close(1000, "left")
            else:
                raise RoomError("invalid_message")

    async def leave(self, peer):
        room = peer.room
        peer.room = None
        if room.peers.get(peer.id) is not peer:
            return
        del room.peers[peer.id]
        if peer.host:
            self.rooms.pop(room.code, None)
            for other in list(room.peers.values()):
                try:
                    await other.send({"op": "host_left"})
                    await other.socket.close(4003, "host_left")
                except (ConnectionClosed, TimeoutError):
                    pass
        else:
            try:
                await room.host.send({"op": "peer_left", "id": str(peer.id)})
            except (ConnectionClosed, TimeoutError):
                pass

    def snapshot(self):
        rss = None
        fd_count = None
        try:
            for line in Path("/proc/self/status").read_text().splitlines():
                if line.startswith("VmRSS:"):
                    rss = int(line.split()[1]) * 1024
            fd_count = len(os.listdir("/proc/self/fd"))
        except OSError:
            pass
        return {"service": "karen-rooms", "protocol": PROTOCOL,
                "uptime_seconds": round(time.monotonic() - self.started, 2),
                "rooms": len(self.rooms), "connections": self.connections,
                "pending": self.guard.pending, "max_rooms": self.max_rooms,
                "max_connections": self.max_connections, "rss_bytes": rss, "open_fds": fd_count,
                "loop_lag_seconds": max(self.loop_lag, default=0),
                "address_cache_entries": len(self.guard.addresses),
                "identity_cache_entries": len(self.guard.identities),
                "counters": dict(self.counters), "expires_on": "2028-07"}

    def public_snapshot(self):
        now = time.monotonic()
        monitor = None
        try:
            report = json.loads(Path("/run/karen-rooms-health/summary.json").read_text())
            monitor = {"ok": bool(report["ok"]), "checked_at": report["checked_at"],
                       "certificate_seconds": report.get("certificate_seconds")}
        except (OSError, ValueError, KeyError, TypeError):
            pass
        return {"service": "karen-rooms", "server_time": time.time(),
                "uptime_seconds": int(now - self.started), "max_rooms": self.max_rooms,
                "max_players": self.max_rooms * 4, "players": sum(len(r.peers) for r in self.rooms.values()),
                "monitor": monitor,
                "rooms": [{"id": room.view_id, "display_code": "••" + room.code[-2:],
                           "players": len(room.peers), "capacity": 4, "started": room.closed,
                           "age_seconds": int(now - room.created), "idle_seconds": int(now - room.active_at)}
                          for room in sorted(self.rooms.values(), key=lambda r: r.created, reverse=True)]}

    def health(self, connection, request):
        if request.path == "/api/status":
            response = connection.respond(200, json.dumps(self.public_snapshot(), ensure_ascii=False) + "\n")
            del response.headers["Content-Type"]
            response.headers["Content-Type"] = "application/json; charset=utf-8"
            response.headers["Cache-Control"] = "no-store"
            response.headers["X-Content-Type-Options"] = "nosniff"
            return response
        if request.path == "/healthz":
            response = connection.respond(200, json.dumps({"service": "karen-rooms", "protocol": PROTOCOL,
                                                           "expires_on": "2028-07"}) + "\n")
            response.headers["Cache-Control"] = "no-store"
            return response
        if request.path in ("/metrics", "/status"):
            # Proxied public traffic carries this overwritten header and isn't administrative.
            if connection.remote_address[0] not in ("127.0.0.1", "::1") or request.headers.get_all("X-Karen-Client-IP"):
                return connection.respond(404, "Not found\n")
            status = self.snapshot()
            if request.path == "/status":
                response = connection.respond(200, json.dumps(status) + "\n")
                del response.headers["Content-Type"]
                response.headers["Content-Type"] = "application/json"
            else:
                values = {key: value for key, value in status.items() if isinstance(value, (int, float))}
                values.update(status["counters"])
                response = connection.respond(200, "".join(f"karen_rooms_{key} {value}\n" for key, value in sorted(values.items())))
                del response.headers["Content-Type"]
                response.headers["Content-Type"] = "text/plain; version=0.0.4"
            response.headers["Cache-Control"] = "no-store"
            return response
        if request.path != "/rooms":
            return connection.respond(404, "Not found\n")

    async def reap_once(self, now):
        self.guard.prune()
        for room in list(self.rooms.values()):
            if now - room.created > self.room_seconds or now - room.active_at > self.idle_seconds:
                self.counters["rooms_expired_total"] += 1
                await room.host.socket.close(4004, "room_expired")

    async def reap(self):
        next_reap = time.monotonic() + 60
        while True:
            before = time.monotonic()
            await asyncio.sleep(1)
            now = time.monotonic()
            self.loop_lag.append(max(0, now - before - 1))
            if now < next_reap:
                continue
            next_reap = now + 60
            await self.reap_once(now)


async def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--host", default="127.0.0.1")
    parser.add_argument("--port", type=int, default=42766)
    args = parser.parse_args()
    service = RoomService()
    stop = asyncio.get_running_loop().create_future()
    for sig in (signal.SIGTERM, signal.SIGINT):
        try:
            asyncio.get_running_loop().add_signal_handler(sig, lambda: stop.done() or stop.set_result(None))
        except NotImplementedError:  # Windows loopback smoke runner; production uses systemd signals.
            pass
    async with serve(service.handle, args.host, args.port, subprotocols=[SUBPROTOCOL],
                     process_request=service.health, origins=[None], compression=None, max_size=4096, server_header=None,
                     max_queue=1, ping_interval=15, ping_timeout=30, close_timeout=3):
        reaper = asyncio.create_task(service.reap())
        LOG.info("room service listening on %s:%s", args.host, args.port)
        try:
            await stop
        finally:
            reaper.cancel()


if __name__ == "__main__":
    logging.getLogger("websockets").setLevel(logging.WARNING)
    logging.basicConfig(level=logging.INFO, format="%(asctime)s %(levelname)s %(name)s %(message)s")
    asyncio.run(main())
