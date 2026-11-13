# 建筑师重复胜利结局定向回归

该探针针对v0.113.0真实游戏。先阅读KarenSTS2MCP README，用隔离档案、随机端口、最小化与内部音轨录制进行验证。

```powershell
dotnet build tests/ArchitectEndingProbe/ArchitectEndingProbe.csproj -p:GameDir=<game-dir>
```

将KarenArchitectProbe.dll与KarenArchitectProbe.json放到隔离游戏mods/KarenArchitectProbe/。通过MCP run_command运行karen_architect_check，直接检查游戏及BaseLib实际建立的TheArchitect.DialogueSet。0、1、2、3、10、11次和当前实际胜利计数都必须有可用对话。

仅在一次性隔离局运行karen_architect_check enter，通过真实EventRoom进入建筑师。沿原生选项完成对话与动画，等待game_over.victory=true且continue可用，核对胜利界面和新写入的.run历史。该命令会改变测试局位置。

原实现基线：0、1次各1个候选，2次及以上为0；真实继续按钮触发TheArchitect.WinRun空引用。修复后所有计数各1个候选，第二组IsRepeating=true。

资源打包回归还需核对中英日对话正文不变、重复段全部行及next键采用r标记；PCK中只有三份ancients.json改变，其余资源逐项SHA256一致。
