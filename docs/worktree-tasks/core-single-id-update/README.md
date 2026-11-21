# Core 发行包仅使用新 ID

## 用户要求

> 这是正常现象吗？我不想要这种方式

> 移除旧 ID 入口，要求角色同步升级

## 计划

同步更新 Karen 的 Core 最低依赖和构建校验，使用 Core 2.0.2 构建并验证。保护原检出已有的 Workshop 配置改动。

## 结果与验证

- 两份 Karen 清单均要求 `ShoujoKagekiCore` 2.0.2。桌面与 Android 共用的构建入口从清单读取最低版本，测试确认会拒绝 2.0.1。
- Desktop 的 Stable、Main、Beta 均完成构建。内嵌 Core 通过严格校验，与本次独立 Core 发行包一致。v0.113.1 实机确认 Karen 完成加载并可选择。
- 主检出的 `workshop.json` 已有改动得到保留。
