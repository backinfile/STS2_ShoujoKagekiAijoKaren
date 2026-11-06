# Public room endpoint diagnosis

Domain: `sts2-karen-multiplayer.karen.fan`. SSH target: authorized `botyun` (China Beijing).

## Confirmed observations

- Cloudflare proxied A record added for the dedicated room hostname. Existing unrelated DNS records were retained.
- Two dedicated Aliyun ingress rules added: TCP 42765 and TCP 42767. Backend 42766 remains bound to loopback.
- Nginx TLS listener 42765, HTTP-01 listener 42767, Python room relay loopback 42766; UFW allows the two frontend ports.
- Cloudflare Origin Rules initially mapped ordinary requests to 42765 and HTTP-01 requests to 42767. Public HTTP-01 checks intermittently returned 200 and the correct fixture body, but other requests returned 403 with the `Non-compliance ICP Filing` page pointing to Aliyun.
- Nginx access log recorded a 200 response for a request whose external response was the Aliyun 403 page. Thus a successful Nginx response alone does not validate the public path.
- Certbot nginx plugin honors `--http-01-port 42767` and generated the correct temporary exact challenge location. One production authorization attempt and one staging dry run failed with 403. No new certificate was issued and renewal has not passed.
- A hostname-scoped HTTP-to-HTTPS redirect was subsequently added in Cloudflare, and the ACME Origin Rule now also uses TLS port 42765. HTTPS checks return 520/525.
- Server-side capture showed Cloudflare completing TCP establishment, sending a TLS ClientHello, and Nginx sending its TLS response, followed by multiple immediate inbound RST packets. The room Nginx error log contained no matching application error.
- The TLS listener temporarily uses the existing server-test certificate during bootstrap. The new hostname certificate does not exist. This is not a finished public TLS deployment.

## Interpretation and limits

The HTTP block page confirms ICP-related interception on those requests. The TLS behavior is consistent with network-side interception; SNI identification is a plausible explanation, not a proven implementation detail. Alibaba Cloud does not publish the complete implementation of its filing interception system. Cloud Firewall documentation explains Host/SNI identification and its EIP documentation explicitly describes network security products injecting RST packets.

Changing security-group ports or issuing a certificate cannot itself resolve a filing/access restriction. Public connectivity and automated certificate renewal remain unverified. No Tunnel has been created: the user requested research before deciding on that architecture.

## Primary references

- [Alibaba Cloud filing server check](https://help.aliyun.com/zh/icp-filing/basic-icp-service/user-guide/icp-filing-server-access-information-check): using nonstandard ports does not change filing requirements.
- [Alibaba Cloud EIP connectivity FAQ](https://help.aliyun.com/zh/eip/support/faq-about-internet-connectivity): filing restrictions and forged RST behavior.
- [Alibaba Cloud Host/SNI identification](https://help.aliyun.com/en/cloud-firewall/cloudfirewall/domain-name-resolution-policy): technical capability; this page concerns Cloud Firewall outbound policies and does not prove the exact implementation of ICP interception.
- [Cloudflare Origin Rules](https://developers.cloudflare.com/rules/origin-rules/): proxied records and destination-port override.

## Pending

Choose the public connectivity approach with the user. Continue only the targeted client checks that do not depend on public access. Update the deployed relay expiry (currently the original deployment still reports 2027-07-28) when deploying the finalized new release; source and UI now state July 2028. Do not present the current endpoint as ready for players.

## Direct hostname plus high-port follow-up

The user asked to test hostname plus port without Tunnel. Direct Windows curl using the existing DNS-only server-test hostname on TLS 42765 failed in the TLS handshake. A `--connect-to` test for the new room hostname forced the origin address directly, retaining the hostname and SNI and keeping certificate checks enabled; it also failed at handshake. Direct HTTP on 42767 with Host set to the new hostname plus port returned `403 Forbidden`, `Server: Beaver`, and the Aliyun `Non-compliance ICP Filing` page. These tests did not change DNS and do not depend on Cloudflare orange-cloud port availability.

The endpoint syntax is supported by the client, but current direct public connectivity is still failing. No Tunnel performance measurements exist: route-dependent latency, jitter and long-lived connection reliability would need a targeted measurement after choosing an architecture. Cloudflare references: [proxy ports](https://developers.cloudflare.com/fundamentals/reference/network-ports/), [Tunnel metrics](https://developers.cloudflare.com/tunnel/observability/), [long-lived connection troubleshooting](https://developers.cloudflare.com/cloudflare-one/networks/connectors/cloudflare-tunnel/troubleshoot-tunnels/common-errors/).

## Community research and additional direct controls

Linux.do and V2EX firsthand accounts disagree on simple high-port forwarding. Relevant cases include:

- [Linux.do, September 2024](https://linux.do/t/topic/211052): author tested 5050 and still got the filing page.
- [Linux.do, May 2024](https://linux.do/t/topic/86350): IP:88 worked, domain did not; moving to 18888 also failed.
- [Linux.do, May 2026 solved thread](https://linux.do/t/topic/2227721): author reports success with a named cloudflared Tunnel. This establishes a case, not a guarantee for botyun.
- [Linux.do, May 2026 Tunnel performance report](https://linux.do/t/topic/2164011): author reports poor transfer throughput/timeouts and an SJC http2 tunnel endpoint. This does not quantify this game's latency.
- [V2EX, June 2024 IP HTTPS account](https://www.v2ex.com/t/1045857): author describes an IP certificate backend after hostname TLS resets. Its frontend used the IP directly, so adapting it to our domain-only client would require an additional proxy and remains a design inference.

Additional live controls kept normal TLS verification enabled. HTTP to the exact same IP and port 42767 returned 200 with the numeric Host and 403 with the room hostname Host. TLS to port 42765 with a hostname failed with unexpected EOF. TLS using the IP as the authenticated peer reached certificate verification and failed with the expected IP-address mismatch because the installed certificate contains only server-test. Thus IP HTTPS is a candidate if a valid IP certificate can be installed; it has not yet been established as a complete public room solution.

A candidate retaining the player's hostname is Cloudflare Worker -> HTTPS origin IP:42765, with an authenticated IP certificate. It avoids a cloudflared process but still uses Cloudflare routing, so it is not automatically faster than Tunnel. It needs real edge WebSocket tests, IP certificate issuance and renewal tests, and latency measurements before adoption. No Worker was created for this candidate.

Official constraints: [Workers custom ports](https://developers.cloudflare.com/workers/configuration/compatibility-flags/#allow-specifying-a-custom-port-when-making-a-subrequest-with-the-fetch-api) are supported for direct origins with current compatibility settings; [Workers WebSockets](https://developers.cloudflare.com/workers/examples/websockets/) are supported; [Origin Rules](https://developers.cloudflare.com/rules/origin-rules/) Host/SNI overrides require Enterprise, although destination-port override is free. [Let's Encrypt's March 2026 Certbot announcement](https://letsencrypt.org/2026/03/11/shorter-certs-certbot) confirms short-lived IP certificates and Certbot webroot support from 5.4, with reload deploy hooks needed. At that announcement nginx/apache plugins did not yet support IP issuance; verify the chosen version rather than assume current support.


## Subsequent validated deployment

The historical failures above were superseded by the approved IP HTTPS Worker route; see [latency comparison](latency-comparison.md). A trusted short-lived IP certificate was issued with Certbot webroot HTTP-01 using the reserved ACME prefix on the existing numeric-IP port 80 vhost. This numeric Host was externally reachable. Domain Host/SNI access remained the failing control. No other application route was changed. Certbot 5.8.0 plus nginx plugin is now the system command and the system renewal service's executable; existing certificates remain intact. The room IP certificate has separate renewal data and a four-hour timer. Renewal dry-run including Nginx reload passed.

Ordinary Worker IP fetch was confirmed unsupported (1003); fixed-origin TLS socket WebSocket forwarding passed real .NET protocol/packet checks. The production client endpoint is still `wss://sts2-karen-multiplayer.karen.fan/rooms`, without a port or IP for players. Temporary HTTP2 and QUIC Quick Tunnels were also tested by request; their measurements are recorded separately. Direct hostname plus high port is still not validated as usable.
