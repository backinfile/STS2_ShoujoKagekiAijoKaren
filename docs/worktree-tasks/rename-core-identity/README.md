# Core 发行标识改名

## 用户要求

> Mod ID、SDK 程序集名和发行包名 也需要修改

> 别忘了兼容旧ID的配置项

> 合并到三个本地 main 并清理本次工作树

## 计划

更新角色清单、程序集引用、桌面内嵌与 Android 打包入口，使 Karen 接入改名后的 Core 2.0.0。

## 结果与验证

- Karen 清单、SDK 引用、桌面内嵌、工坊、开发环境和 Android 入口已切换到 `ShoujoKagekiCore` 2.0.0。
- 构建升级时旧 Core 产物移出上传目录并保留。Android 直装在新 Core 安装后移出旧包，保留存档与设置目录。
- Stable、Main、Beta 桌面构建通过，v0.111.1 隔离实机与 Core 2.0.0、Junna 一起加载。Android Mod 包结构和 13 项 JVM 安装检查通过。Android 真机启动未执行。
