# 04 BaseLib

`BaseLib` 是统一添加新内容行为的基础mod，类似于塔1的 `basemod` 和 `stslib`。

项目地址：https://github.com/Alchyr/BaseLib-StS2

> 注意：由于目前（2026.3.14）`BaseLib` 尚处于开发阶段，如果只打patch不添加新内容可以不使用。

## 下载

1. 前往 https://github.com/Alchyr/BaseLib-StS2/releases 下载 `dll`、`pck` 和 `json` 三个文件
2. 把它们放在 `mods` 文件夹里，记住你下载的版本

## 引用 BaseLib

在 `csproj` 文件中添加引用：

```xml
<ItemGroup>
  <!-- 本地引用，注意路径是否正确 -->
  <Reference Include="BaseLib">
    <HintPath>$(Sts2Dir)/mods/BaseLib/BaseLib.dll</HintPath>
    <Private>false</Private>
  </Reference>

  <!-- 或使用 NuGet 获取，注意版本是否一致 -->
  <!-- <PackageReference Include="Alchyr.Sts2.BaseLib" Version="*" /> -->
</ItemGroup>
```

在 `{modid}.json` 中添加依赖：

```json
"dependencies": ["BaseLib"],
```

## 添加新卡牌

使用 BaseLib 添加卡牌更简单：

1. 添加 `Pool` attribute 指定卡池
2. 继承 `CustomCardModel` 而不是 `CardModel`
3. 卡牌会自动注册，不需要手动调用

```csharp
[Pool(typeof(ColorlessCardPool))]
public class TestCard2 : CustomCardModel
{
    // 卡牌实现
}
```

**注意**：通过 BaseLib 添加的卡牌，其 id 会变成 `{命名空间第一段大写}-{原卡牌id}`。

例如 `namespace Test.Scripts;` 取 `TEST`，卡牌 id `TEST_CARD2` 最后变成 `TEST-TEST_CARD2`。

本地化文本：

```json
{
  "TEST-TEST_CARD2.title": "测试卡牌2",
  "TEST-TEST_CARD2.description": "造成{Damage:diff()}点伤害。"
}
```

### 自定义卡框

可以 override 自定义卡框：

```csharp
public override Texture2D? CustomFrame => GD.Load<Texture2D>("res://images/icon_1024.png");
```

## 自定义模组配置

### 前置条件

需要先放一张图片到 `{modId}\mod_image.png` 作为 mod 图标，尺寸任意，否则会由于报错不显示配置。

### 创建配置类

```csharp
[ModInitializer("Init")]
public class Entry
{
    public static void Init()
    {
        ModConfigRegistry.Register("test", new ModConfig());
    }
}

public class ModConfig : SimpleModConfig
{
    public static bool Test1 { get; set; } = true;
    public static bool Test2 { get; set; } = false;
    public static bool Test3 { get; set; } = true;
}
```

目前只支持 `bool` 类型的配置项。

## 其他功能

以下功能待补充：

- 添加新遗物
- 添加新怪物
- 添加新能力
- 添加新事件
- 添加新药水
- 添加新附魔
- 添加先古卡
- 添加先古之民
