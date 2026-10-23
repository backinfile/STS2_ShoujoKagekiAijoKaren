# Karen room relay

Native multiplayer packet relay; game rules run on the host. Production: `wss://47.95.213.214:8443/rooms`; read-only public page under a server-generated `/<status-prefix>/` path on the same HTTPS entry. Four ASCII digits including leading zeroes. Active-code collisions are retried. Public JSON only contains `••` plus the last two digits; opaque row IDs are independent of the join code. Codes are invitations, not user authentication. The relay checks a persistent random credential for reconnect identity and rejects incompatible game/Mod versions.

## Small-service budgets

- 16 rooms, four concurrent players each, 80 total sockets; room expires after 48 hours or 30 minutes without successful application traffic.
- Per IP: 16 sockets, four pending greetings, 20 admissions/minute, six creates/10 minutes, 20 joins/minute. Global pending cap 16. Persistent identity: three creates/10 minutes, one active socket. Eight failed joins/5 minutes trigger two-minute cooldown.
- Per socket: 100 messages/sec, burst 200; 2 MiB/sec, burst 4 MiB. Per-IP byte budget 8 MiB/sec, burst 16 MiB. Per-room egress 8 MiB/sec, burst 32 MiB (broadcast counts each recipient).
- Unauthenticated frame limit 4 KiB, greeting timeout 10 seconds; authenticated native packet limit 2 MiB. Identity/address caches bounded at 4096 entries with inactive TTL; member history limited to 16 identities per room.
- Nginx adds independent per-IP/global handshake and connection limits. Only its overwritten `X-Karen-Client-IP` from actual loopback peers is trusted. Backend binds 127.0.0.1:42766; neither forwarded headers nor the retired Worker can supply arbitrary player identity.
- systemd: relay MemoryHigh 256M, MemoryMax 384M, CPUQuota 50%, 32 tasks/2048 files. These bounds support small groups; they do not prevent saturation of the server's network link by a volumetric attack. Shared-NAT players share an IP budget.

## Monitoring and privacy

`/healthz` exposes service/protocol/expiry only. `/<status-prefix>/api/status` is public, read-only, no-store, and supplies masked codes, occupancy, waiting/started, room age and recent successful application traffic. It does not inspect battle/act/floor state. UI polls every two seconds with timeout/backoff, pauses in hidden tabs, and explicitly marks stale information.

`/status` and Prometheus `/metrics` are loopback-only and excluded from Nginx routing. They expose aggregate counts, resource use, loop lag and bounded rejection categories, without codes, credentials or player identities. Access log omits IPs, URL/query, bodies and player identifiers; Nginx error logs may include connection metadata. Own logs rotate daily or above 10M when logrotate runs, retaining seven rotations.

`karen-rooms-monitor.timer` runs every minute: check local status and a real certificate-validating IP TLS WebSocket probe from botyun. This is a server-side check; external/carrier reachability still needs external checks. Alerts cover unavailable service/TLS, certificate <48h, disk <1 GiB, >=80% socket capacity, full rooms, high RSS/FD/loop lag, new internal errors and >=50 rejects since previous observation. Results enter local journal/failed service and a public health summary. No external notification destination is configured.

Inspect with `systemctl status karen-rooms karen-rooms-monitor.timer`, `journalctl -u karen-rooms-monitor.service`, `curl http://127.0.0.1:42766/status`. Detailed latest observation is `/var/lib/karen-roomwatch/health.json` (private); sanitized summary `/run/karen-rooms-health/summary.json`. Certbot IP certificate has a separate four-hour renewal timer and Nginx deploy reload hook. Expiry displayed inside the game: July 2028; the web page/API omit it.

## Validation/deployment

Run `python -m unittest discover -s server/karen-rooms -p 'test_*.py'` with the pinned requirements. Abuse tests run against isolated loopback fixtures, never floods against production. `tools/room-tests` validates the actual C# socket adapter and native packet framing. `tools/room-game-tests/run.py` runs isolated game profiles through MCP, optionally with `--endpoint`, `--desktop-host`, `--record`, `--complete-battle`, and the supplied `--mcp-source`.

Deploy archive includes only install.py's explicit ALLOWED source/assets/units. Upload it and the installer under `/opt/karen-rooms/incoming`, pass archive SHA256. Installer atomically replaces current symlink, validates Nginx, relay readiness, monitor and logrotate, and restores previous files/link on failure. All Linux deployment text must use LF line endings. Preserve the prior valid release until the new deployment is verified. No signing keys, game assets or test plugins belong in the server release.

## Shared HTTPS entry

Karen services expose only TCP 8443 through Nginx. `/rooms` is the WebSocket transport; the random status prefix owns its HTML, static files and read-only API. Root and old status routes return 404. The generated 192-bit status prefix is persisted outside Git in `/opt/karen-rooms/status-path`; random URLs reduce incidental discovery, without authenticating visitors or stopping network saturation.

`/etc/nginx/karen-services-http.d/*.conf` supplies sibling service zones, and `/etc/nginx/karen-services.d/*.conf` supplies sibling locations. Room relay and telemetry remain separate loopback services (42766 and 42767). The telemetry service owns its authentication and independent request/body/concurrency limits. Any location that adds headers must repeat the full required security-header set because Nginx replaces inherited headers. Clients never supply trusted forwarding identity. Aliyun ingress and UFW allow 8443; retired Karen public ports 42765/42767 are closed. Existing unrelated apps and port-80 ACME renewal remain unchanged.
