# AI 生成立绘纹理清理与本地工具

记录日期：2026-09-29。本文整理本次少女歌剧九人立绘的实际处理过程，以及本机已经安装、验证可用的工具。处理发生于 2026-09-28；环境信息于记录日重新核验。

## 1. 本次采用的方案与结论

先检查源 PNG 的 100% 局部及缩略图，再用 **GPT Image 2 Artifact Cleaner** 对神乐光制作原图、低强度 `0.25`、中强度 `0.50` 对比。最终选择 **0.25**，批量处理七张生成立绘，保留原尺寸、原始透明通道和朝右的姿势，重新输出无损 PNG 九宫格。

这里把用户描述的“摩尔纹”作为待排查现象。本次目视检查没有发现明确、稳定的固定周期网格，主要看到局部细碎假纹理、色块不平整和偏硬的边缘，因此没有直接使用 FFT 陷波或固定两像素去网格滤波。不能据此把所有 AI 图的细纹都归为同一种问题。

低档已减弱部分细碎纹理；中档的额外收益不明显，因此选择更保守的一档。这是本批图的目视选择，不代表对其他图片也最佳。清理未承诺消除全部纹理，VAE 重建也会改变 RGB 细节。

## 2. 本地目录与已准备好的工具

以下两个根目录均为真实本地路径：

```text
素材根目录：output\imagegen\starlight-nine
工具根目录：output\imagegen\starlight-nine\texture-review
```

下表中路径相对于“工具根目录”。这些文件已经存在，正常复用不需要重新安装或重新下载模型。

| 工具／文件 | 本地相对位置 | 用途 |
| --- | --- | --- |
| 清理器源码 | `artifact-cleaner/` | 已克隆、审阅并实测 |
| 独立 Python 环境 | `artifact-cleaner/.venv/Scripts/python.exe` | 运行清理和对比脚本，避免依赖系统 Python |
| 上游依赖锁 | `artifact-cleaner/uv.lock` | 重建环境时复用 |
| 残差网络权重 | `artifact-cleaner/weights/latent_residual.pt` | 已下载，约 1.9 MB |
| FLUX.2-VAE 配置 | `flux2-vae/config.json` | 从本地加载模型 |
| FLUX.2-VAE 权重 | `flux2-vae/diffusion_pytorch_model.safetensors` | 已下载，249,521,340 字节 |
| 保留透明通道的包装脚本 | `clean_textures.py` | 两档试样、七图批处理及 alpha 验证 |
| 对比与九宫格脚本 | `make_comparisons.py` | 原尺寸局部对比、抗混叠缩略图、九宫格 |
| 本次检查记录 | `README.md` | 简要过程与选档结论 |

清理器源码版本：`885543891103456f87b49afa809c15a1c25cb78a`。

重新核验的环境：

| 项目 | 版本／状态 |
| --- | --- |
| Python | 3.14.3 |
| PyTorch | 2.13.0+cu126 |
| diffusers | 0.39.0 |
| Pillow | 12.3.0 |
| NumPy | 2.5.1 |
| GPU | NVIDIA GeForce RTX 4060，8 GB 显存 |
| CUDA | `torch.cuda.is_available()` 返回 `True` |

本机另有 `<user-home>\.local\bin\uv.exe`；依赖下载缓存位于 `<user-home>\AppData\Local\uv\cache`。首次安装 GPU 依赖下载约耗时 12 分钟，已完成；后续直接使用现有虚拟环境即可。

## 3. 实际处理步骤

### 步骤一：保留源图，区分源图纹理与缩放混叠

1. 保留 `characters/` 中的输入，另建 `characters-cleaned/` 保存输出。
2. 对神乐光检查脸部、发丝、衣料、衣褶线、金属边缘和透明轮廓。
3. 同时看原尺寸 100% 裁切、局部 200% 像素放大以及抗混叠缩略图。像素放大只用于观察，不作为交付图。
4. 如果问题仅出现在缩略图，先修正缩放与导出流程；只有源图确有问题时，才对源图清理。

检查图：`texture-review/hikari-source-diagnosis.png`。本次预览采用 Lanczos 缩小，最终使用 PNG，不额外锐化，避免有损压缩干扰判断。

### 步骤二：审阅清理器并适配透明 PNG

清理器流程为：

```text
RGB → FLUX.2-VAE 编码 → 潜变量 + strength × 残差网络输出 → VAE 解码 → RGB
```

上游命令行读取图片时会转换为 RGB，直接使用会丢失透明通道。因此本次没有直接用上游 CLI 输出最终透明立绘，而是通过 `clean_textures.py`：

1. 以 RGBA 读取输入，独立保留原始 alpha 数组。
2. 只把 RGB 交给清理器。
3. 将清理后的 RGB 与原 alpha 原样拼回 RGBA。
4. 保存 PNG，重新打开后用 `np.array_equal` 验证 alpha 逐像素一致，同时验证输出数组尺寸一致。

`strength`（上游称 alpha）是清理强度；**图片 alpha** 是透明通道，两者不是同一含义。

实际执行设置：CUDA、BF16、VAE tiling 开启、`torch.compile` 关闭，模型从本地目录加载。残差权重用 `torch.load(..., weights_only=True)` 读取。

### 步骤三：做低／中强度对比

以神乐光为样本，分别生成 `0.25` 和 `0.50` 两档。

对比图上方是相同尺寸的整人缩略图，下方分别是脸部和衣料的 100% 原尺寸裁切。重点检查：

- 色块中的碎纹、假亮点、硬边是否改善。
- 发丝和睫毛是否消失，眼睛是否变软。
- 衣褶线和武器边缘是否被抹平。
- 透明边缘是否异常，朝向、姿势是否保持。

本次选择低档 `0.25`，没有为了追求完全无纹理而继续提高强度。

### 步骤四：批量处理与输出

批处理名单固定为：神乐光、天堂真矢、星见纯那、露崎真昼、西条克洛迪娜、石动双叶、花柳香子，共七张生成图。

爱城华恋、大场奈奈来自原仓库画稿，作为原画对照保留，没有通过清理器重建。九宫格包含这两张原画与七张处理后的生成图。

实测七张生成图全部保持 `1024×1536`，alpha 检查全部通过；每张处理约 2.48–2.73 秒，不含安装、模型加载和排版时间。记录见 `texture-review/batch-results.json`。

## 4. 直接复用的 PowerShell 命令

这些命令使用现有虚拟环境，不需要激活环境或重新安装依赖。

```powershell
$reviewDir = 'output\imagegen\starlight-nine\texture-review'
$pythonExe = Join-Path $reviewDir 'artifact-cleaner\.venv\Scripts\python.exe'

# 可选：确认 GPU 可用
& $pythonExe -c "import torch; print(torch.cuda.is_available()); print(torch.cuda.get_device_name(0) if torch.cuda.is_available() else 'CPU')"

# 1. 神乐光试样：固定输出 0.25 和 0.50 两档
& $pythonExe (Join-Path $reviewDir 'clean_textures.py') pilot

# 2. 制作原图／低档／中档对比图
& $pythonExe (Join-Path $reviewDir 'make_comparisons.py')

# 3. 检查对比图后，按选定强度处理七张生成图
& $pythonExe (Join-Path $reviewDir 'clean_textures.py') batch --strength 0.25

# 4. 更新当前九宫格及 PNG 预览
& $pythonExe (Join-Path $reviewDir 'make_comparisons.py') --grid
```

当前脚本的边界：

- `pilot` 固定读 `characters/hikari.png`，固定试 `0.25`、`0.50`；传入 `--strength` 不会改变这两个试样档位。
- `batch` 固定读取上述七个英文文件名，不是任意目录批处理器。
- 脚本按所在位置推导素材根目录，未提供 `--input`／`--output` 参数。
- 重跑会覆盖同名试样、清理结果、JSON 记录和当前九宫格，源图不覆盖。比较多套参数时，先把已有输出另存。
- `--grid` 需要 `characters-cleaned/` 内九张文件齐全；华恋和奈奈已在该目录准备好，清理脚本本身不复制这两张。
- `make_comparisons.py` 使用 Windows 字体 `<windows-directory>\Fonts\msyh.ttc`。
- 当前环境、模型已经就绪；若以后用于其他素材，先复制并调整包装脚本的根目录、样本名称、批处理名单和对比裁切坐标，再重复试样选档，不要直接覆盖本批输入。

## 5. 输出文件与检查记录

以下路径相对于素材根目录：

| 路径 | 内容 |
| --- | --- |
| `characters/` | 输入立绘；华恋后来已换为从 PSD 恢复的完整披风版本 |
| `characters-cleaned/` | 七张清理图及两张原画对照 |
| `texture-review/pilot/hikari-0.25.png` | 神乐光低档试样 |
| `texture-review/pilot/hikari-0.50.png` | 神乐光中档试样 |
| `texture-review/hikari-original-low-medium.png` | 三列整人及原尺寸局部对比 |
| `texture-review/pilot-results.json` | 试样耗时、尺寸和 alpha 验证 |
| `texture-review/batch-results.json` | 七图批处理验证 |
| `starlight-nine-3x3-cleaned.png` | 可由脚本重建的当前高清九宫格 |
| `starlight-nine-3x3-cleaned-preview.png` | 可由脚本重建的当前预览 |
| `starlight-nine-3x3-cape-fixed.png` | 恢复华恋披风后的高清交付快照 |
| `starlight-nine-3x3-cape-fixed-preview.png` | 恢复华恋披风后的预览快照 |
| `karen-layer-review/` | 华恋披风修复前备份、PSD 图层检查及修复记录 |

`cape-fixed` 是当时另存的交付快照，运行 `--grid` 不会自动更新这两个文件。

## 6. 本次发现的两个独立问题

### 透明区隐藏颜色不等于实有背景

神乐光 PNG 的 alpha 范围是 `0–254`。工具曾把透明区内的 RGB 光晕显示出来，导致最初误判为未去背景。正确按 alpha 合成后，该光晕不作为背景出现。检查 PNG 时要同时检查颜色与透明通道，不能只看忽略 alpha 的 RGB 预览；本次也没有擅自把半透明像素全部改为不透明。

### 华恋肩部披风遗漏属于 PSD 导出问题

原先读取了 PSD 的默认合成预览，遗漏隐藏图层中的完整披风。后来使用 `psd-tools` 查看图层，直接从隐藏图层 6 导出完整原画，按原图层坐标放回 `3508×4961` 画布，并更新输入、对照图和九宫格。没有用去纹理模型或生成模型补画披风。

完整原画导出位于：

```text
output\imagegen\hikari\references\karen-complete-original.png
```

`psd-tools 1.20.0` 安装在系统 Python 3.14 环境，用于 PSD 检查与导出；它不是清理器的依赖，也不在上述清理虚拟环境的依赖清单中。

## 7. 参考资料与适用边界

- [GPT Image 2 Artifact Cleaner 中文说明](https://github.com/Larryvrh/gpt-image-2-artifact-cleaner/blob/main/README.zh-CN.md)：本次实际采用的工具。作者针对 GPT Image 2 伪影训练；其他来源的图片要先试样验证。本会话的生成工具没有返回确切底层模型名，因此不能只凭工具名称认定模型一致。
- [FLUX.2-VAE](https://huggingface.co/black-forest-labs/FLUX.2-VAE)：已下载到本地。VAE 编解码会改变细节；即使保留透明通道，也不能声称整图逐像素不变。
- [OpenCV 周期噪声去除](https://docs.opencv.org/doc/doxygen/html/d2/d0b/tutorial_periodic_noise_removing_filter.html)：规则周期纹理的备选方向，本次未实施。
- [ComfyUI-DeGrid](https://github.com/lunaaispace-eng/ComfyUI-DeGrid)：侧对话提到的其他模型网格处理方案，本次未安装、未使用，不能直接套用到这批图片。

本地清理器采用 PolyForm Noncommercial 许可，FLUX.2-VAE 有独立许可；需要分发工具或用于其他用途时，以各自原始许可文件为准。本文记录实际采用的方法，不承诺对所有“摩尔纹”有效。
