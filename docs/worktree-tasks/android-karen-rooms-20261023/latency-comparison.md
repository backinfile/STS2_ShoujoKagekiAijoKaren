# Room relay path measurements (2026-10-23)

## Method

Same Windows computer, real .NET ClientWebSocket adapter (`tools/room-tests`), two room participants and the deployed botyun relay. Each sample sends one native packet client -> relay -> host -> relay -> client. There are 12 measured samples per successful route, after a separate 256 KiB binary packet integrity check. Values include application scheduling/polling; they are application round trips, not ICMP ping or one-way card-play latency. The probe column includes new DNS/TCP/TLS/WebSocket connection and the ready message.

All routes used normal TLS validation. Following the user's later explicit choice, the client default now uses direct IP HTTPS; the domain Worker relay has been retired. Local domain resolution returned 198.18.7.54, indicating a local virtual DNS/proxy path; these measurements describe this computer's current path and do not establish mobile/carrier latency.

| Path | Min | Median | Slowest of 12 | Cold probe | Protocol checks |
| --- | ---: | ---: | ---: | ---: | --- |
| Direct origin IP HTTPS :42765 | 46 ms | 47 ms | 63 ms | 156 ms | 11/11 passed |
| Long domain -> Worker TLS socket -> origin IP HTTPS | 906 ms | 912 ms | 916 ms | 7922 ms | 11/11 passed |
| Quick Tunnel QUIC | 934 ms | 938 ms | 1412 ms | 2299 ms | 11/11 passed on retry |
| Quick Tunnel HTTP2 | 998 ms | 1000 ms | 1564 ms | 2029 ms | 11/11 passed |

HTTP2 tunnel connected through lax13; QUIC through lax12. Worker responses observed SJC. QUIC's first attempt passed the probe but failed during the subsequent client's TLS establishment with unexpected EOF; the successful retry does not erase that failure. Quick Tunnels were stopped after measurement. They are temporary test fixtures, not the production domain mapping.

The three Cloudflare routes are close in this small sample. Worker had the lowest median here; this is not evidence of a durable performance advantage. The major observed difference is direct origin access versus the current Cloudflare route. No throughput/load test, mobile network test, long-session soak, or full game compatibility matrix was performed.

## Reproduction and evidence

Run `dotnet run -c Release --project tools/room-tests/RoomSocketTests.csproj -- <wss-endpoint> --latency` against an authorized test relay. It checks probe, create/join identity, copied native bytes, transfer mode/channel, a 256 KiB binary message, incompatible version rejection, closed-room rejection, host loss, cancellation and public cleartext rejection.

Local evidence: `artifacts/rooms-latency-ip-direct.log`, `artifacts/rooms-latency-worker.log`, `artifacts/rooms-latency-tunnel-quic.log` (failed first attempt), `artifacts/rooms-latency-tunnel-quic-retry.log`, `artifacts/rooms-latency-tunnel-http2.log`. Artifacts are ignored by Git; this document preserves the results.

## Changes required by these tests

- Worker global `fetch()` rejected direct IP subrequests with 1003. The final worker uses `cloudflare:sockets` with `secureTransport: on`, a fixed origin, bounded HTTP upgrade parsing and pinned `ws` 8.22.0 for RFC 6455 frames. Only `/rooms` and `/healthz` are exposed.
- Set the accepted front WebSocket's `binaryType` to `arraybuffer` before `accept()`. Current compatibility dates default to Blob; otherwise text greeting succeeds but native binary forwarding fails. The end-to-end native-byte test was red before this fix and green afterwards. Temporary debug logs were removed.
- Increase the client's availability probe deadline from 6 to 15 seconds: this machine's first TLS connection alone measured about 5.7 seconds. Settings now label the displayed value as connection time, distinct from steady message round-trip latency.
- Bound total Nginx connections (512) and handshake rate (20/s, burst 40), rather than applying an 8-connection cap to Cloudflare's shared egress address.

Primary references: [Workers IP fetch restriction](https://developers.cloudflare.com/workers/platform/known-issues/#fetch-to-ip-addresses), [TLS sockets](https://developers.cloudflare.com/workers/runtime-apis/tcp-sockets/), [binaryType default and migration](https://developers.cloudflare.com/workers/runtime-apis/websockets/#binary-messages).
