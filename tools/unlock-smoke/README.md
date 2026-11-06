# 一键解锁定向验证

只操作脚本创建的独立用户目录。使用当前 Android AnyCPU Mod 包和原生游戏进度 API，PC 运行不等于 Android 真机验证。

```powershell
python tools/unlock-smoke/run.py --source-env "<正式版独立环境>" --branch stable --port 15850
python tools/unlock-smoke/run.py --source-env "<测试版独立环境>" --branch beta --port 15850
```

验证真实 Mod 配置按钮回调、原进度完整备份、卡牌/遗物/药水/时间线/角色/进阶解锁、重复操作幂等、胜负/游玩时长/成就不变，以及重新启动游戏后仍解锁。探针和私有桌面不打入 Mod/APK。输出 `artifacts/unlock-smoke/`。
