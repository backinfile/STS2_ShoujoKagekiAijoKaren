# Why 特效定向验证

仅使用 KarenSTS2MCP HTTP 和游戏内命令；不使用 Computer Use，不修改共享游戏目录。`prepare.py` 创建隔离副本；录制保留真实视口与游戏进程音频，不最小化游戏。

先从工作树运行 `tools/build_dual_branch.ps1`。为每版运行以下流程，把 `<branch>` 换成 `stable` / `beta`，分别使用两个未占用的端口：

```powershell
python tools/vfx/catalog/prepare.py <branch> --output artifacts/why-vfx --source-dev <repository>/artifacts/dev-game --skip-build --port <port>
python -u tools/vfx/why/verify.py <branch> --output artifacts/why-vfx
```

隔离副本需要源环境已有可继续的 Karen 单人存档。工具只终止自己启动的进程，已存在的端口会拒绝部署。可用 `--cases base,zero,...` 限定案例；默认覆盖 `base,zero,upgrade,fast,instant,void,empty,repeat,peek,resize,exit`。测试 fixture 的 restore/编译缓存按版本隔离。

封装实录并复核截图：

```powershell
python tools/vfx/why/package_video.py --source artifacts/why-vfx --output artifacts/why-vfx/delivery
python tools/vfx/why/qa_evidence.py artifacts/why-vfx
```

`package_video.py --clips stable/upgrade,beta/void` 可限定展示片段。基础录像含开/关视觉的诊断操作，不宜作为连续效果展示片段；原始录像和对照截图仍作为验证证据保留。

`verify.py --selection-hold 4` 可延长录像中的真实选牌停留。`compare_reference.py artifacts/why-vfx --branch stable` 将原始参考与 MCP 实机截图并排，并记录粗略的光峰位置/柔光宽度；不加工游戏画面，也不将生成 UI 与原生 UI 的像素差异视为特效差异。低速的边缘流动可能被全画面静帧检测器标为近静态，须同时检查边缘截图差分、帧指纹和运动拼图，不可直接宣称零静帧。

重录流畅性检查前，分别用当前 MCP 源码构建 stable/beta DLL 并部署到隔离副本。使用 `--audit-capture --staged-selection --smooth-recording`：先以真实视口录制 8 秒临时 Gray-code 墙钟，再逐张执行原生选牌。墙钟仅在独立测试插件中；`check_capture.py` 直接从视频逐帧解码，检查时钟停滞、跳跃及时间轴漂移，另拒绝人为制造的 7 秒停帧负例。完成统计要求实际采帧率至少 27fps，重复/队列丢帧不超过 10%。录制版本必须提供 `encoder` 与 `frames_dropped` 等新字段。

`--smooth-recording` 在录像中只保存 JSON 状态，将 MCP PNG 前后截图移到开始前/停止后，避免 `SavePngToBuffer()` 占用游戏主线程。`--staged-selection` 将停留时间分配到真实的逐张选择，不向成片添加动作。`package_video.py --native-viewport` 交付原生 1280×720 视口，通过一次连续输出编码拼接，检查严格递增 PTS、帧间隔和音视频时长差，不插帧。

`WhyProbe.cs` 只负责准备测试手牌和只读观察；原生打牌、选牌和确认均通过 MCP 动作执行。临时 fixture 不包含在正式 Mod 发布包中。每例存储原始截图、模型状态、手牌核查及观察结果；原始录像与封装结果保留来源哈希及音视频 QA。

第六版观察器在原生 Hand 中采样效果，核对仅在选择时出现、位于 dimmer 与卡牌之间、Shader 时间推进、表面尺寸及无输入状态、最终清理。额外测试查看战场、选牌中调整窗口、未确认时切换战斗；base 保存光影开/关对照和间隔 0.65 秒的实际边缘动画截图，检查可见变化及中央区域留空。所有临时可见性/Peek 控制只存在于 fixture，不进入发布包。
