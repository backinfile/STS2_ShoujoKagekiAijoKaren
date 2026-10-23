# Android Karen-only and room-code multiplayer

## Original user input

- 「$worktree-task 1 要求安卓版锁定其他角色，只允许使用karen进行游戏 2 你看看能不能接入下游戏中的联机模式，创建一个服务器，选择一个高位端口部署在ssh botyun上 玩家创建房间的时候生成一个房间号，其他玩家通过房间号进入游戏」

- 「正常版也应该允许切换到我这个服务器」：PC 正式版/测试版提供原生联机与房间服务器切换，PC 角色保持可选。

- 「切换服务器的时候要探测下服务器的可用性」。
- 「注明下服务器到期时间 2027年7月28日 23:59:59」「注明下服务器到期时间 2027年7月28日」：界面标注日期 2027年7月28日。

- 「到期时间修改为 2028年7月 使用域名karen.fan 不要用ip 取一个前缀，在cloudflare上帮我配置上记录。你看看怎么映射到非80 443的端口（这两个端口被阿里云禁用了）」：最新到期时间覆盖旧日期。
- 「codex://threads/01a166a8-b68d-7f60-900c-271874b7d60c 读读这个 取一个长些的域名」：已读取，选用 sts2-karen-multiplayer.karen.fan。
- 「你可以使用外部浏览器在阿里云上修改安全组相关配置」。
- 「申请ssl证书的时候尽量使用certbot，自动化续期」「certbot使用nginx模式不行吗」。

## Brief task plan

- Original checkout: main at d217fcc75ab50b9c822ca3fb2ba7d39668f5bf74; clean. Task branch: codex/android-karen-rooms-20261023. Worktree: ../worktrees/STS2_ShoujoKagekiAijoKaren/android-karen-rooms-20261023.
- Inspect native character selection, multiplayer lobby and transport interfaces, plus the existing Android compatibility layer. Apply Karen-only restrictions to Android builds without deleting progression or changing desktop selection.
- Inspect the authorized botyun SSH host and available high ports. Implement a minimal room-code service and game integration using native multiplayer synchronization, with compatible-version checks and bounded room lifecycle.
- Build and perform targeted local/server checks and isolated native game multiplayer tests where available. Document Android device limitations accurately. Prepare deployment as a distinct service, validate it, then deploy within the user's explicit authorization.
- Consolidate task changes, synchronize main, and request separate merge approval under $worktree-task; retain worktree and branch until separately approved for cleanup.

## Result and changes

Current result: Android Karen-only policy, native room transport, availability probe, IP TLS and hardened shared port 8443 are implemented and deployed. Historical domain/tunnel experiments below are retained as investigation evidence; the current route is direct IP TLS. Final r6 APK is built and its eight exact-package VFX checks pass; release status remains a test candidate because Android ARM64 evidence is missing and beta native startup errors remain open. The task branch is not merged into main.


### Network status

See [network diagnosis](network-diagnosis.md) for the historical public-path failures and the subsequent validated deployment, and [latency comparison](latency-comparison.md) for current results. The long domain now reaches the room service through Worker TLS sockets; IP certificate issuance and renewal dry-run passed. Temporary Quick Tunnels were tested and stopped.

### Build and targeted checks

Android and desktop dual-branch Mod packages build successfully. The Python relay protocol suite has 14 passes and the pure .NET loopback adapter has 10 passes. Stable v0.107.1 targeted native gameplay passed with an Android AnyCPU Karen-only host and desktop Ironclad client: room code join, settings probe fail/success/native switch, character policy, both players playing Defend, synchronized turn transition, screenshots before/after and karen_check_hand. Both hands matched model/visual counts (6/6 and 5/5), no missing or unexpected nodes. Screenshots were inspected. Logs had no runtime errors or checksum mismatch; client disconnect messages at teardown are expected. Evidence: artifacts/rooms-native-stable-08. Beta runtime and ARM64 device tests remain untested; there is no full compatibility matrix claim. No new APK, task commit or merge yet.

Latest follow-ups: 「还有tunnel对性能的影响」「你看看能不能用域名+端口这种方式？」「你全网搜搜解决方案，看看linuxdo等社区」。 The user also updated AGENTS.md: keep changes minimal, run only necessary targeted verification automatically, and request permission for comprehensive testing after implementation. No comprehensive matrix was run.


### Approved IP HTTPS route and comparative tests

Latest user approvals: 「可以」 for domain -> Worker -> IP HTTPS validation; 「你可以直接更新certbot」; 「你可以也试试tunnel方式，测试下各种成功的方法中的延迟」。

The long domain is bound to the dedicated `sts2-karen-multiplayer-relay` Worker, using a trusted IP certificate to the origin TLS port 42765. Client UI/config stays domain-based. Relay release `dfb60ee974c14dd1` reports July 2028; backend stays loopback 42766. Worker protocol and native packet checks passed 11/11. Certbot and certbot-nginx were upgraded to 5.8.0 under `/opt/certbot`; the original system renewal service uses a drop-in to the new executable, retaining existing `/etc/letsencrypt` data. The room certificate uses isolated config/work/log directories and a four-hour systemd renewal check with Nginx reload. Staging issuance, trusted issuance and renewal dry-run passed. Changing only the origin certificate to an unrelated trusted hostname made Worker socket access fail with 502; restoring the IP certificate restored 200. An earlier Python-urllib request was blocked at Cloudflare with 1010, so curl supplied the valid negative control.

See [latency comparison](latency-comparison.md) for four measured paths, exact methodology, failed QUIC connection and sample limits. Temporary Quick Tunnels were used only as test fixtures. No full matrix or Android device tests were added. Android and desktop Mod packages were rebuilt after the probe timeout change; no new APK or merge.

Before release: review native Steam multiplayer save ownership when loading/abandoning and switching transport. Native `TryAbandonMultiplayerRun` uses PrimaryPlatform's ID, while room saves use the persistent room identity; this path has not been runtime validated. Preserve the task worktree and branch; no merge confirmation has been requested for this incomplete release gate.

The temporary HTTP-01 listener on 42767 was disabled after the numeric-IP port 80 webroot path succeeded; its UFW allow rule was removed. The prior Aliyun security-group entry and unused hostname Origin Rules remain as pending deployment cleanup. Room TLS 42765 and loopback 42766 remain active.


### Direct-IP hardening, public status and four-digit codes

Follow-ups: 「小规模联机服务，先补齐限流、防刷和监控，再采用 IP HTTPS 直连」「你制作一个可以实时只读的观看状态和当前房间的轻量网页」「只显示部分房间号」「房间号总长改为4位吧」「清除下你之前的各种尝试产生的临时内容」「你可以用两个pc端，使用这个服务器联机对战一把测测 录制创建房间和加入房间的视频我看看」「如果mcp mod需要修改，则可以先修改下」。

Latest route overrides the historical domain-only requirement: client default is `wss://47.95.213.214:42765/rooms`. The old dedicated Worker returns 410 for relay requests and redirects ordinary page links to the status page. Worker version `ef91cfaf-81fc-4aa5-a0d4-a006aec4b7ce`; no origin socket or ws dependency remains. Other Workers are unchanged.

Relay release `bf6b56cbe138226e` adds bounded per-IP/identity/room budgets, trusted overwritten loopback proxy identity, failed-join cooldown, authenticated packet limits, 16 rooms/80 sockets, private metrics, minute health observation, service resource ceilings and rotating logs. See the service README for exact budgets and monitoring scope. The public page polls every two seconds, shows occupancy/waiting/started/age, and only receives masked four-digit codes (`••78`), with independent opaque room row IDs. A relay cannot observe actual battle rules or run progress.

Verification: 34 relevant Python tests pass. Actual IP TLS native-adapter checks pass 11/11, with 256 KiB payload integrity; 12 round trips min/median/max 46/47/48 ms, probe 177 ms. Public page 200, private `/status` 404, POST public API 405; normal certificate validation throughout. Chrome verified live two-player waiting -> started updates and mask, health summary, plus 390 px responsive layout using the actual page in a local iframe. Android and desktop dual-version Mod packages rebuild with zero errors; no APK produced.

Cleanup: failed local test game copies 01–07 removed after retaining compact diagnostics in `artifacts/rooms-diagnostics.zip`; successful run 08 screenshots/JSON/logs retained while disposable game/fixture/user copies removed. Old Worker dependencies and local preview process removed. On botyun, stopped Quick Tunnel log/PID files and transitional releases 0241ad88df9471a0 / 6a757eb4bac6de0c removed; current release and rollback 9f98518c674e82a3 retained. Worktree, branch, current deployment, protected APK and signing keys remain.

Two desktop clients' live-server battle and recording requested; in progress. Recording uses existing MCP in-game capture on an isolated Windows desktop, with ordinary non-minimized windows; no Computer Use game interaction. Current MCP source builds without changes. No full version/role matrix or device test is implied. Native Steam save ownership gate recorded above remains a release limitation.


### Live public relay, two PC clients and recordings (completed)

Formal version v0.107.1, desktop Karen host + desktop Ironclad client, both isolated test profiles, endpoint `wss://47.95.213.214:42765/rooms`. Run `artifacts/rooms-public-stable-05/result.json` passes: four-digit creation/join, native lobby, both players' legal card actions/turn transition, first combat won and both reward screens observed. Pre/post screenshots were saved and viewed; round-two hand checks are 6/6 and 5/5, no missing/unexpected nodes or pile icons. Both logs contain no ERROR/Exception/desync/checksum failure. All test processes/ports and their room exited after the run.

Captured via current unmodified KarenSTS2MCP source b853512 (compiled against stable game), in ordinary windows on a separate Windows desktop. No Computer Use game actions. Setup fixes were confined to the test runner: supply MCP manifest from mod_manifest.json, avoid minimized startup for video, use supported settings parameters, and consume recording output_path. No gameplay cheat/damage command was used. Host Karen and client Ironclad naturally defeat the first encounter.

Deliverables in `artifacts/rooms-public-stable-05/`: `01-create-room.mp4` 11.9s, `02-join-room.mp4` 7.6s, combined `create-and-join.mp4` 19.5s, `03-online-battle.mp4` 43.7s. Battle recording starts in round two, after the first-round screenshot/hand synchronization checkpoint; it ends at rewards. These are silent 1280x720 H.264/30fps clips, actual capture 29.46/29.04/29.97fps, repeated 7/8/2 frames and zero dropped frames. Video files fully decode; representative frames and both reward screenshots inspected. `verification.json` records stats, package hashes and both result states.

Failed setup diagnostics 01–04 are compacted into `artifacts/rooms-public-setup-diagnostics.zip`; their game copies and redundant clips are removed. Successful run's evidence and final clips remain; disposable game/fixture/user copies are removed. Original copies generated in the MCP recording directory are deleted only when listed in this task's recording metadata and after the final copies are saved. No MCP source changes were needed. No comprehensive matrix, beta runtime, ARM64 device test, new APK, merge or push was performed.


Additional deployment cleanup: both historical Cloudflare Origin Rules (`Karen multiplayer TLS origin 42765`, `Karen Certbot HTTPS origin 42765`) are now Disabled, with the dashboard reporting 0 active Origin Rules. The hostname's useful HTTP-to-HTTPS redirect and Worker status-page redirect remain. The unused Aliyun security-group allow entry for 42767 has not been removed; the listener and UFW allow were already removed, so it has no active service. Other account rules are unchanged. Delivered video copies remain after deleting the task-owned originals; Windows packaged-app path redirection was checked before deletion.


Latest requests: 「安卓限制只能选华恋，继续发布一个最新的apk吧」「服务器到期就不要写在联机web页上了」。 Android-only policy remains; PC roles stay selectable. The public page/API omit expiry; the game retains its expiry hint. r6 APK build and mandated first-use checks are in progress. Room-save identity validation now retargets only this installation's owned room saves and blocks continuing them through native transport; isolated runtime regression in progress.


User follow-ups: 「不要使用根目录吧，你来设置一个长url防止攻击（你分析下有用吗）」「codex://threads/01a166b1-5512-7fb0-9ebd-d703f819663f 读取 这个对话中的部署地址，你看看是不是共用一个端口好点，好统一防止攻击，只暴露一个端口」「你可以和这个对话交流一下，同步需求」。 Read the specified conversation (title: 研究塔2 Mod玩家数据与平衡), and sent a coordination message with user authorization. Proposed common Karen TLS entry port 42765, loopback-only room/data backends, independent per-route limits and two Nginx include seams. Other existing applications and SSH are outside the port consolidation scope. Awaiting the other conversation's route/upload contract before altering its deployment or including a new upload endpoint in APK.

The status page uses a server-generated 192-bit random path stored in `/opt/karen-rooms/status-path` outside Git; the HTML, API and static files share this prefix. Root and former default API paths return 404. Random paths reduce opportunistic discovery, do not authenticate users and do not defend the network link against DoS. Referrer suppression, noindex, sanitized request logging and independent rate budgets remain.


### Shared port 8443

Latest user instruction: 「你来统一修改到8443端口，开放端口，统一进行安全防护，使用子路径分配不同的功能」。 Deployed release `cea1d52d1b45246a`: Aliyun and UFW allow TCP 8443. Room transport is `/rooms`; public read-only status is under a persisted 192-bit prefix, omits expiry and masks codes. Root, old `/api/status`, `/status` and `/metrics` return 404. Separate Nginx include seams keep the telemetry service independent, backend 127.0.0.1:42767, beside room relay 127.0.0.1:42766. Existing applications/SSH/port-80 ACME renewal are preserved. Old Karen 42765/42767 cloud rules and UFW 42765 allow removed.

Actual .NET adapter through valid IP TLS on 8443 passes 11 checks; 12 packet RTT samples min/median/max 62/63/64 ms, availability probe 162 ms. External status HTML, JS, CSS and API pass 200 while root/private routes pass 404. Browser confirms current service and absence of expiry. Nginx, relay, monitor and certificate timers active.

Telemetry coordination supplied a client patch with an embedded shared upload key. Automatic approval rejected copying/embedding the long-lived credential into APK as extractable exposure; the action did not run. Requested a non-secret or server-scoped upload flow from the explicitly authorized peer conversation before final APK. No approval was bypassed.


### Final client and targeted beta verification

The credential blocker was resolved with a non-secret client contract. APK embeds only an HTTPS upload URL. A session request obtains a two-minute, single-use upload token bound to exact UTF-8 content hash, byte length and source IP; no permanent upload key or backend read credential enters the client. The peer reports successful Windows/Ubuntu service tests and public HTTPS contract checks. Inputs remain unverified client contributions. The final client source SHA256 is `2e3d7cd338e117734dcb6123dc88778048c478213641309410a9d5ffa17ee004`. Private endpoint resource stays ignored under artifacts, with no old Firebase fallback.

Shared data service operation: `karen-observatory-monitor.timer` checks health/disk/database/backup age every minute; `karen-observatory-backup.timer` performs daily online SQLite backup with seven retained copies; own logrotate retains seven compressed rotations. Both services bind loopback, run with separate resource limits, and have independent per-route budgets and authentication. Certificate renewal remains a separate four-hour Certbot timer. Old public Worker now returns update endpoint 8443 or 404, version `03762bd7-6439-4099-bba9-5eedbd6cc825`.

Beta v0.111.0 targeted Android AnyCPU host plus desktop Ironclad client passed through the actual 8443 relay in `artifacts/rooms-r6-save-beta-8443-02/result.json`: create/join, failure/success availability probe, Android character lock after unlock attempts, both players acting and reaching second turn, owned room-save canonicalization after session reset/mode switch. Screenshots were saved and viewed; hand checks are 6/6 and 5/5, no missing/unexpected nodes. Logs have no runtime ERROR/Exception/desync/checksum failure in the successful repeat. First beta attempt failed because the test assumed a Defend card in every random starting hand; only the fixture was fixed to use legal immediate cards and enemy entity IDs. That first attempt also logged engine script-registration Invalid Task ID at startup, absent on repeat; do not infer all cold starts are error-free from the passing repeat.

Formal v0.107.1 save regression also passed (`artifacts/rooms-r6-save-stable`), at the former 42765 endpoint before migration; its screenshots and hand results were inspected. Actual 8443 pure adapter passes 11 checks; server regression passes 34. External HTTP/TLS succeeds only at 8443; old/backends time out. Raw TCP connect probes were not valid evidence on this proxy-enabled host because even closed ports were acknowledged; actual application requests and remote listener/firewall state confirm isolation. No full single/multiplayer matrix or ARM64 device check was run.


User follow-up during final checks: 「允许全部授权」。 Continue the current port/deployment/build/targeted-verification scope. Retain the safer short-lived token design; no need to embed long-term credentials or export real run-history data to expand testing.


### Final APK candidate

`artifacts/android-direct/StS2-Karen-v0.107.1-v0.1.3-direct-r6.apk`: application `com.karenmod.sts2hd`, version `0.107.1-karen.0.1.3.6`, versionCode 10018, 2,047,434,377 bytes. SHA256 `3ccfd59f7bbdd1815bf3c8a602c4c605c271b9d0d04b29224d4b5f68f523f0cb`. Signing certificate matches protected r5; v2/v3 signature and alignment passed, 12 installer regressions passed, full APK payload extracted and SHA256-verified on JVM. Only Karen and BaseLib ship, no test plugin or former launcher application classes. The embedded Mod matches the exact tested package.

Mandatory report `artifacts/rooms-r6-release-check.json` has eight passing VFX cases (stable/beta × all/bridge/shine/promise), no timing/functional failures. Each all run covers 31 visual families and three passes; warmup reports 304 resources, 29 batches and 460 draw calls. Shine/promise four-pass before/after/reset screenshots were viewed on both branches; all 48 hand checks passed and formal-version five native card-frame contours matched reference. Stable first maximum frame times: bridge 17.4 ms, shine 16.7109 ms, promise 16.6804 ms; beta all also has no first-use hitch. These PC-hosted measurements do not establish Android performance.

Release gate intentionally remains `pending`: beta bridge/shine/promise startup logs contain 4/1/2 native `Invalid Task ID` messages from `wait_for_task_completion`, during selection/run preload before the measurement commands. Cause is not established; do not silently dismiss or mark these runs log-clean. Log review is retained in `artifacts/rooms-r6-release-log-review.json`. Exact-APK ARM64 fresh-install, upgrade-save preservation, cold first-VFX and background-resume checks are untested. This APK is delivered for device trial, not a fully validated production release. No full gameplay matrix was run.


Desktop stable/beta Mod package rebuilt with final 8443 endpoint and scoped-token upload client, zero build errors; original character selection remains enabled. All source changes are prepared in the isolated task branch; original main remains clean at the captured revision. Deployment and artifacts are retained; no remote Git push or worktree cleanup is implied by this package delivery.


### Approved main integration

User explicitly approved: 「合并到main吧」。 Coordinated the shared main update with the authorized data-service task: it first fast-forwarded main to `5d31a115a4b8f71900c5d54511d795b625134761`, then released the merge window. Synchronized that revision into this task branch. The sole conflict is formatting of the identical `KarenTelemetryEndpoint.json` EmbeddedResource; resolved to one multiline declaration while preserving `KAREN_ANDROID`. Upload client source and game runtime/resource sources are unchanged from the tested r6 task commit. No new APK build or expanded game matrix is required for this configuration-only integration.

Main integration is authorized; retain the task worktree, branch, r6 APK, signing material and test evidence. No push or cleanup is part of this approval.


Integration validation: XML parsed with exactly one endpoint EmbeddedResource and the Android define intact; all four Android/desktop × stable/beta ExportRelease compilation checks pass with zero errors. Git diff confirms no runtime or game-asset changes relative to tested r6; existing beta startup/device limitations remain unchanged. Target main will fast-forward to the synchronized task commit, preserving the already integrated observatory task.
