"""Bounded admission and traffic budgets for a small, directly exposed relay."""
from dataclasses import dataclass, field
import ipaddress
import time


class RoomError(Exception):
    pass


@dataclass(frozen=True)
class Limits:
    per_ip_connections: int = 16
    pending_connections: int = 16
    per_ip_pending: int = 4
    cache_entries: int = 4096
    cache_ttl: float = 1800
    connections_per_minute: int = 20
    creates_per_ten_minutes: int = 6
    identity_creates_per_ten_minutes: int = 3
    joins_per_minute: int = 20
    failed_joins_per_five_minutes: int = 8
    join_cooldown_seconds: float = 120
    messages_per_second: int = 100
    message_burst: int = 200
    bytes_per_second: int = 2 * 1024 * 1024
    byte_burst: int = 4 * 1024 * 1024
    ip_bytes_per_second: int = 8 * 1024 * 1024
    ip_byte_burst: int = 16 * 1024 * 1024
    room_identities: int = 16


class Bucket:
    def __init__(self, capacity, period, now):
        self.capacity = capacity
        self.rate = capacity / period
        self.tokens = float(capacity)
        self.at = now

    def take(self, cost, now):
        self.tokens = min(self.capacity, self.tokens + max(0, now - self.at) * self.rate)
        self.at = max(self.at, now)
        if cost > self.tokens:
            return False
        self.tokens -= cost
        return True


def client_address(socket):
    address = ipaddress.ip_address(socket.remote_address[0])
    # The backend binds only to loopback. Nginx overwrites this dedicated header.
    # Forwarded/X-Forwarded-For and headers from non-loopback peers are never trusted.
    if address.is_loopback and socket.request is not None:
        values = socket.request.headers.get_all("X-Karen-Client-IP")
        if values:
            if len(values) != 1:
                raise RoomError("bad_handshake")
            try:
                address = ipaddress.ip_address(values[0])
            except ValueError:
                raise RoomError("bad_handshake") from None
    if isinstance(address, ipaddress.IPv6Address) and address.ipv4_mapped:
        address = address.ipv4_mapped
    return str(address)


@dataclass
class AddressState:
    at: float
    connect: Bucket
    create: Bucket
    join: Bucket
    failures: Bucket
    wire: Bucket
    connections: int = 0
    pending: int = 0
    blocked_until: float = 0


@dataclass
class Lease:
    state: AddressState
    messages: Bucket
    wire: Bucket
    pending: bool = True
    identity: int | None = None
    closed: bool = False


class AbuseGuard:
    def __init__(self, limits=None, clock=time.monotonic):
        self.limits = limits or Limits()
        self.clock = clock
        self.addresses = {}
        self.identities = {}
        self.active_identities = set()
        self.connections = self.pending = 0
        self.last_prune = 0

    def prune(self, force=False):
        now = self.clock()
        if not force and now - self.last_prune < 60:
            return
        self.last_prune = now
        ttl = self.limits.cache_ttl
        for key, state in list(self.addresses.items()):
            if not state.connections and now - state.at > ttl:
                del self.addresses[key]
        for key, (at, _) in list(self.identities.items()):
            if key not in self.active_identities and now - at > ttl:
                del self.identities[key]

    def open(self, address, max_connections):
        now, limits = self.clock(), self.limits
        self.prune()
        if self.connections >= max_connections:
            raise RoomError("server_full")
        state = self.addresses.get(address)
        if state is None:
            if len(self.addresses) >= limits.cache_entries:
                raise RoomError("rate_limited")
            state = self.addresses[address] = AddressState(now,
                Bucket(limits.connections_per_minute, 60, now),
                Bucket(limits.creates_per_ten_minutes, 600, now),
                Bucket(limits.joins_per_minute, 60, now),
                Bucket(limits.failed_joins_per_five_minutes, 300, now),
                Bucket(limits.ip_byte_burst, limits.ip_byte_burst / limits.ip_bytes_per_second, now))
        state.at = now
        if (state.connections >= limits.per_ip_connections or self.pending >= limits.pending_connections or
                state.pending >= limits.per_ip_pending or not state.connect.take(1, now)):
            raise RoomError("rate_limited")
        state.connections += 1
        state.pending += 1
        self.connections += 1
        self.pending += 1
        return Lease(state, Bucket(limits.message_burst, limits.message_burst / limits.messages_per_second, now),
                     Bucket(limits.byte_burst, limits.byte_burst / limits.bytes_per_second, now))

    def check_identity(self, identity):
        if identity in self.active_identities:
            raise RoomError("already_connected")

    def create(self, lease, identity):
        now, limits = self.clock(), self.limits
        item = self.identities.get(identity)
        if item is None:
            if len(self.identities) >= limits.cache_entries:
                raise RoomError("rate_limited")
            bucket = Bucket(limits.identity_creates_per_ten_minutes, 600, now)
        else:
            bucket = item[1]
        self.identities[identity] = now, bucket
        if not lease.state.create.take(1, now) or not bucket.take(1, now):
            raise RoomError("rate_limited")

    def join(self, lease):
        now = self.clock()
        if lease.state.blocked_until > now:
            raise RoomError("join_cooldown")
        if not lease.state.join.take(1, now):
            raise RoomError("rate_limited")

    def failed_join(self, lease):
        now = self.clock()
        state = lease.state
        state.failures.take(1, now)
        if state.failures.tokens < 1:
            state.blocked_until = now + self.limits.join_cooldown_seconds

    def authenticate(self, lease, identity):
        self.check_identity(identity)
        self.active_identities.add(identity)
        lease.identity = identity
        lease.pending = False
        self.pending -= 1
        lease.state.pending -= 1

    def consume(self, lease, size):
        now = self.clock()
        lease.state.at = now
        if not (lease.messages.take(1, now) and lease.wire.take(size, now) and lease.state.wire.take(size, now)):
            raise RoomError("rate_limited")

    def close(self, lease):
        if lease.closed:
            return
        lease.closed = True
        lease.state.at = self.clock()
        lease.state.connections -= 1
        self.connections -= 1
        if lease.pending:
            self.pending -= 1
            lease.state.pending -= 1
        if lease.identity is not None:
            self.active_identities.discard(lease.identity)
