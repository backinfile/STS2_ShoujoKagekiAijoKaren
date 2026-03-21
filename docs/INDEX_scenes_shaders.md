# 场景文件与着色器索引

---

## 场景文件（.tscn）

> 场景根目录：`D:\claudeProj\sts2\scenes\`

### 核心场景

| 文件路径（绝对路径） | 描述 |
|---|---|
| `D:\claudeProj\sts2\scenes\game.tscn` | 主游戏场景（程序入口点） |
| `D:\claudeProj\sts2\scenes\asset_loader.tscn` | 自动加载的资源加载器场景 |
| `D:\claudeProj\sts2\scenes\one_time_initialization.tscn` | 自动加载的一次性初始化场景 |

### 背景场景

| 文件路径（绝对路径） | 描述 |
|---|---|
| `D:\claudeProj\sts2\scenes\backgrounds\main_menu_bg.tscn` | 主菜单背景 |
| `D:\claudeProj\sts2\scenes\backgrounds\main_menu_bg_alt.tscn` | 主菜单备用背景 |
| `D:\claudeProj\sts2\scenes\backgrounds\ceremonial_beast_boss\` | 仪式野兽BOSS背景 |
| `D:\claudeProj\sts2\scenes\backgrounds\fake_merchant_event_encounter\` | 假冒商人遭遇事件背景 |
| `D:\claudeProj\sts2\scenes\backgrounds\glory\` | 荣耀层背景 |
| `D:\claudeProj\sts2\scenes\backgrounds\hive\` | 蜂巢层背景 |
| `D:\claudeProj\sts2\scenes\backgrounds\kaiser_crab_boss\` | 皇帝蟹BOSS背景 |
| `D:\claudeProj\sts2\scenes\backgrounds\knowledge_demon_boss\` | 知识恶魔BOSS背景 |
| `D:\claudeProj\sts2\scenes\backgrounds\overgrowth\` | 过度生长层背景 |
| `D:\claudeProj\sts2\scenes\backgrounds\little_light_script.gd` | 背景光效GDScript |

### 调试场景

| 文件路径（绝对路径） | 描述 |
|---|---|
| `D:\claudeProj\sts2\scenes\debug\` | 开发者控制台和命令历史场景 |

---

## 着色器文件（.gdshader）

> 着色器根目录：`D:\claudeProj\sts2\shaders\`

### 通用着色器

| 文件路径（绝对路径） | 描述 |
|---|---|
| `D:\claudeProj\sts2\shaders\card_ripple.gdshader` | 卡牌波纹效果 |
| `D:\claudeProj\sts2\shaders\dissolve.gdshader` | 溶解/死亡效果 |
| `D:\claudeProj\sts2\shaders\fade_transition.gdshader` | 屏幕淡入淡出 |
| `D:\claudeProj\sts2\shaders\hsv.gdshader` | 色相/饱和度/明度调整 |
| `D:\claudeProj\sts2\shaders\power.gdshader` | 能力图标着色器 |
| `D:\claudeProj\sts2\shaders\relic.gdshader` | 遗物图标着色器 |
| `D:\claudeProj\sts2\shaders\wiggle.gdshader` | 摇摆动画着色器 |

### 模糊着色器

| 文件路径（绝对路径） | 描述 |
|---|---|
| `D:\claudeProj\sts2\shaders\blur\` | 高斯模糊、画布组遮罩模糊、着色器生成器 |

### 地图绘制着色器

| 文件路径（绝对路径） | 描述 |
|---|---|
| `D:\claudeProj\sts2\shaders\map_drawing\` | 地图标注的线条绘制和擦除着色器 |

### VFX着色器（~60个）

#### 卡牌VFX
| 文件路径（绝对路径） | 描述 |
|---|---|
| `D:\claudeProj\sts2\shaders\vfx\cards\` | 卡牌变换视觉效果 |

#### 通用VFX
| 文件路径（绝对路径） | 描述 |
|---|---|
| `D:\claudeProj\sts2\shaders\vfx\common\` | 粒子、翻页、消散、光线、光环、烟雾、冲击着色器 |

#### 武器/技能VFX
| 文件路径（绝对路径） | 描述 |
|---|---|
| `D:\claudeProj\sts2\shaders\vfx\dagger\` | 匕首喷洒着色器 |
| `D:\claudeProj\sts2\shaders\vfx\fire\` | 火焰材质着色器 |
| `D:\claudeProj\sts2\shaders\vfx\goopy_impact\` | 黏性冲击碎片 |
| `D:\claudeProj\sts2\shaders\vfx\heavy_blunt\` | 重型钝器侵蚀效果 |
| `D:\claudeProj\sts2\shaders\vfx\hyperbeam\` | 超级光束激光 |
| `D:\claudeProj\sts2\shaders\vfx\minion_divebomb\` | 仆从俯冲轰炸光环 |
| `D:\claudeProj\sts2\shaders\vfx\missile\` | 导弹核心着色器 |
| `D:\claudeProj\sts2\shaders\vfx\poison\` | 毒骷髅效果 |
| `D:\claudeProj\sts2\shaders\vfx\potion\` | 药水液体和飞溅着色器 |
| `D:\claudeProj\sts2\shaders\vfx\scream\` | 尖叫扭曲和光环效果 |
| `D:\claudeProj\sts2\shaders\vfx\shiv\` | 飞刃轨迹 |
| `D:\claudeProj\sts2\shaders\vfx\sleeping\` | 睡眠Z字浮动着色器 |
| `D:\claudeProj\sts2\shaders\vfx\sweeping_beam\` | 扫射光束翻页动画 |

#### 屏幕/UI VFX
| 文件路径（绝对路径） | 描述 |
|---|---|
| `D:\claudeProj\sts2\shaders\vfx\distortion\` | 屏幕扭曲效果 |
| `D:\claudeProj\sts2\shaders\vfx\ui\` | UI视觉效果（时间线解锁链条/气态屏幕/低血量边框） |

#### 怪物专属着色器
| 文件路径（绝对路径） | 描述 |
|---|---|
| `D:\claudeProj\sts2\shaders\vfx\monsters\` | 各怪物专属着色器材质（合并者/百节蜈蚣/灵魂鱼/瀑布巨人） |

#### 调试VFX
| 文件路径（绝对路径） | 描述 |
|---|---|
| `D:\claudeProj\sts2\shaders\vfx\debug\` | 调试用视觉效果着色器 |
