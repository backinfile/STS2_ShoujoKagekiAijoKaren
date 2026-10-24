# Vipcloud domain migration

## Original user input

修改下几个网站/服务器的部署地址，重新在ssh vipcloud上部署，使用域名17181718.xyz,使用外部浏览器中的cf修改域名配置。vipcloud上使用certbot nginx自动续期ssl证书。使用长二级域名。

Follow-up: $worktree-task


Follow-up:

参考“**可以用 CF 代理。** 当时我优先考虑大文件直传、减少代理超时的影响，所以选了 DNS only；这个选择偏保守。

重新核对项目后，开代理是可行的：

- 上传已经按 **8 MiB 分块**，低于 CF 免费版每次请求 100 MB 的上传上限。
- CF 的 **512 MB 限制是缓存文件大小限制**，不能直接理解为下载大小上限。[官方限制](https://developers.cloudflare.com/cache/concepts/default-cache-behavior/)
- 代理确实有读写超时限制，需要验证慢速上传和下载的表现。[连接限制](https://developers.cloudflare.com/fundamentals/reference/connection-limits/)

**结合你现在的安全需求，我更建议开启 CF 代理，并配套完成：**

1. 使用 **Full (strict)**，继续保留源站 Certbot 证书。
2. 私有接口和文件下载明确绕过缓存。
3. Nginx 正确获取真实客户端 IP，维持登录限流。
4. 源站 HTTPS 仅允许 CF 回源，保留证书验证和 SSH 所需通道。

这样能增加边缘防护。单独打开橙云、源站仍允许任意直连，攻击者依然可以绕过 CF。”做下防护

## Brief task plan

1. Capture main at 5d878a0ba46c068a8996a1a82aeab5bc3c340dc9 (clean), create task branch codex/vipcloud-domain-migration-20261024 at ../worktrees/STS2_ShoujoKagekiAijoKaren/vipcloud-domain-migration-20261024.
2. Inventory Karen room relay/status and run observatory/upload deployments; retain unrelated services and existing data/authentication.
3. Configure two long subdomains in external Chrome Cloudflare UI; use proxied DNS, hostname-scoped Full (strict), and Bypass Cache. Deploy to SSH alias vipcloud with Certbot nginx automatic renewal, trusted real IPs and CF-only origin HTTPS. Preserve SSH/HTTP ACME and keep old clients working through a verified TLS/CF compatibility gateway.
4. Update client endpoints and reproducible deployment configuration. Run only migration-related checks, record evidence, consolidate task commit and request merge approval under $worktree-task.

## Result and changes

Server migration and CF protection are deployed and targeted verification is complete. Task commit `3c0e7f1` and synchronization commit `12ba1bc` are integrated into `main`, retaining the independent privacy cleanup at `134aa7b`.

- Relay: `wss://karen-multiplayer-relay-c7a91e5d.17181718.xyz/rooms`; room status retains its private random path.
- Observatory: `https://karen-run-observatory-b4d82f6a.17181718.xyz`; existing random read/upload routes and access policy preserved. Access credentials, private routes and client endpoint JSON stay in ignored artifacts, outside Git.
- Both apps now run only on vipcloud, with loopback backends, independent limits, monitors and daily observatory backups. Old botyun apps/timers are disabled; old IP TLS gateway remains for existing clients and uses certificate-verified CF hostnames. Its local system resolver is used because public UDP DNS timed out.
- Cloudflare UI: Karen hostname Configuration Rule `29c51affb2484040915aab475e4829dd` selects strict TLS; Cache Rule `9e44713b314e40feacdce29dc8134abb` bypasses all Karen content. Existing PanStack strict/cache rules and proxied DNS were preserved. Other domain hosts are outside these Karen rules.
- Origin UFW: 443 restricted to 15 official CF IPv4 and 7 IPv6 networks; existing SSH and port-80 ACME preserved. Weekly allowlist refresh is enabled. Ordinary service installation no longer reopens 443.
- Certbot production SAN certificate expires 2027-01-22. Standard certbot.timer, nginx authenticator, and deploy validation/reload hook remain active. Staging renewal passed with CF proxy and the final firewall enabled.
- Root cause fixed: changing a shared limit zone's IP key under its old name caused the Nginx master to reject reload despite successful syntax checks. New CF zone names allow graceful reload. The temporary ACME proxy workaround was withdrawn after the real cause was proved.
- Four historical runs and six legacy session rows were migrated; initial snapshot SHA-256 `63b7821c4343ca76cb8a1b565b718e3c1599efcaff4322aec3cf1ae79242883d`. A scoped synthetic test record was removed; every historical row was compared with the migration snapshot and matched. Fresh backup and monitor passed.
- Client room default, monitor, retained legacy Worker source, fixture and deployment instructions use the new hostname. The previously retired live Worker was not republished; its old IP hint remains compatible through the old gateway.
- The updated ignored `artifacts/telemetry-deploy/client-endpoint.json` is prepared in this worktree. Integrating the code should also synchronize that build input into the original checkout after merge approval. No APK or Workshop package was built or published.

### Verification and limits

Final active configuration passed 11 native .NET room checks at each new/legacy entry, 13 external telemetry checks, actual HttpClient duplicate uploads, CF-only origin enforcement, four real-IP/header checks, certificate renewal, data integrity and browser rendering. Final logs and archive hashes are recorded in verification.json and the two room log files. The last telemetry synthetic record was removed and all original rows matched again. The weekly allowlist maintenance script also passed a repeated invocation. No full game single/multiplayer matrix or Android device testing was run; no slow authenticated PanStack upload/download test was performed in this project task. The existing PanStack public page remains reachable through CF.

The local Python direct-network probe intermittently ended TLS early. An independent botyun probe passed 13 checks after matching the native game's absent User-Agent; Python's default User-Agent triggered CF error 1010. Actual .NET HttpClient uploads to both new and legacy endpoints returned 200. No CF protection was disabled to make the test pass. One final old-entry probe timed out before handshake; its focused retry passed all 11 checks with a 1.239 s probe and no new legacy server error. The final new-entry probe was 8.526 s, while the earlier packet RTT median was about 600 ms. These measurements do not establish carrier-wide reachability or stable latency.

### Integration / cleanup

The consolidated task commit is 3c0e7f1. Synchronization retained the independent privacy-cleanup changes, including its updated AGENTS.md rules. No deployment code changed during synchronization; run the newly required staged privacy check before finalizing the synchronization commit.

Integration is complete; the task worktree and branch have been removed. Private deployment snapshots and remote rollback material remain outside Git. Do not restore the old database snapshot over newer accepted uploads; reverse migration must first copy current vipcloud data.
