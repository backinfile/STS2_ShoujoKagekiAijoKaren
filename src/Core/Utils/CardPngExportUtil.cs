using Godot;
using HarmonyLib;
using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Cards;
using ShoujoKagekiAijoKaren.src.Models.CardPools;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace ShoujoKagekiAijoKaren.src.Core.Utils
{
    /// <summary>
    /// 精简版卡牌 PNG 导出工具。
    /// 基于 SubViewport 离屏渲染，支持导出卡牌本体、升级版，以及 CardHoverTip 引用卡牌的图，并生成 JSON 索引。
    /// </summary>
    public static class CardPngExportUtil
    {
        // ---- 场景路径（原版游戏路径） ----
        private const string CardScenePath = "res://scenes/cards/card.tscn";
        private const string HoverTipScenePath = "res://scenes/ui/hover_tip.tscn";
        private const string CardHoverTipScenePath = "res://scenes/ui/card_hover_tip.tscn";
        private const string HoverTipDebuffMaterialPath = "res://materials/ui/hover_tip_debuff.tres";

        // ---- 尺寸常量 ----
        private const float CardHalfExtentX = 190f;
        private const float CardHalfExtentY = 240f;
        private const float FramePad = 6f;
        private const float HoverTipWidth = 360f;
        private const float HoverTipGap = 5f;
        private const int HoverColumnSeparation = 0;
        private const int RefCardHoverGap = 4;
        private const float RefCardScale = 0.75f;

        // ---- 帧等待常量 ----
        private const int FramesAfterHostAdded = 2;
        private const int FramesAfterVisualApply = 2;
        private const int FramesAfterHoverLayout = 2;
        private const int FramesAfterHoverResize = 2;
        private const int FramesAfterRender = 5;
        private const int FramesBeforeTeardown = 1;

        // ---- JSON 序列化选项 ----
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        };

        // ==================== 数据模型 ====================

        public sealed class CardExportInfo
        {
            [JsonPropertyName("id")]
            public string Id { get; set; } = "";

            [JsonPropertyName("name")]
            public string Name { get; set; } = "";

            [JsonPropertyName("cost")]
            public string Cost { get; set; } = "";

            [JsonPropertyName("rarity")]
            public string Rarity { get; set; } = "";

            [JsonPropertyName("type")]
            public string Type { get; set; } = "";

            [JsonPropertyName("tips")]
            public List<TipInfo> Tips { get; set; } = new();

            [JsonPropertyName("images")]
            public ImageInfo Images { get; set; } = new();
        }

        public sealed class TipInfo
        {
            [JsonPropertyName("type")]
            public string Type { get; set; } = "";

            [JsonPropertyName("title")]
            public string? Title { get; set; }

            [JsonPropertyName("description")]
            public string? Description { get; set; }

            [JsonPropertyName("cardId")]
            public string? CardId { get; set; }

            [JsonPropertyName("image")]
            public string? Image { get; set; }

            [JsonPropertyName("upgradedImage")]
            public string? UpgradedImage { get; set; }
        }

        public sealed class ImageInfo
        {
            [JsonPropertyName("base")]
            public string Base { get; set; } = "";

            [JsonPropertyName("upgraded")]
            public string? Upgraded { get; set; }
        }

        public sealed class ExportManifest
        {
            [JsonPropertyName("cards")]
            public List<CardExportInfo> Cards { get; set; } = new();
        }

        public sealed class GameInfoExport
        {
            [JsonPropertyName("cards")]
            public List<IdNamePair> Cards { get; set; } = new();

            [JsonPropertyName("relics")]
            public List<IdNamePair> Relics { get; set; } = new();

            [JsonPropertyName("potions")]
            public List<IdNamePair> Potions { get; set; } = new();

            [JsonPropertyName("monsters")]
            public List<IdNamePair> Monsters { get; set; } = new();

            [JsonPropertyName("encounters")]
            public List<IdNamePair> Encounters { get; set; } = new();

            [JsonPropertyName("events")]
            public List<IdNamePair> Events { get; set; } = new();

            [JsonPropertyName("characters")]
            public List<IdNamePair> Characters { get; set; } = new();
        }

        public sealed class IdNamePair
        {
            [JsonPropertyName("id")]
            public string Id { get; set; } = "";

            [JsonPropertyName("name")]
            public string Name { get; set; } = "";
        }

        public sealed class ExportResult
        {
            public bool Success { get; init; }
            public string OutputDirectory { get; init; } = "";
            public int CardCount { get; init; }
            public int ReferenceCardCount { get; init; }
            public int SavedImages { get; init; }
            public int FailedImages { get; init; }
            public string? Error { get; init; }
        }

        public sealed class JsonExportResult
        {
            public bool Success { get; init; }
            public string OutputDirectory { get; init; } = "";
            public int CardCount { get; init; }
            public string? Error { get; init; }
        }

        // ==================== 公共 API ====================

        /// <summary>
        /// 导出所有 Karen 卡牌：生成 base、upgraded 图，以及 CardHoverTip 引用卡牌的图，并输出 JSON 索引。
        /// </summary>
        /// <param name="outputDirectory">输出目录，默认 user://KarenCardExports/</param>
        /// <param name="scale">渲染缩放，默认 1</param>
        /// <param name="log">可选日志回调</param>
        public static async Task<ExportResult> ExportAllKarenCardsAsync(
            string? outputDirectory = null,
            float scale = 1f,
            Action<string>? log = null)
        {
            if (!CanExport(out var err))
            {
                log?.Invoke($"[ExportAll] 失败: {err}");
                return new ExportResult
                {
                    Success = false,
                    Error = err,
                };
            }

            var outDir = ProjectSettings.GlobalizePath((outputDirectory ?? "user://KarenCardExports/").Trim());
            Directory.CreateDirectory(outDir);

            var karenCards = ModelDb.AllCards
                .Where(c => c.Pool is KarenCardPool)
                .OrderBy(c => c.Id.Entry, StringComparer.OrdinalIgnoreCase)
                .ToList();

            log?.Invoke($"[ExportAll] 开始导出 {karenCards.Count} 张 Karen 卡牌到: {outDir}");

            var manifest = new ExportManifest();
            var exportedRefCards = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var savedImages = 0;
            var failedImages = 0;

            // 第 1 轮：导出所有 Karen 卡牌的 base 和 upgraded
            for (var i = 0; i < karenCards.Count; i++)
            {
                var card = karenCards[i];
                log?.Invoke($"[ExportAll] ({i + 1}/{karenCards.Count}) {card.Id.Entry}");

                try
                {
                    var info = CollectCardInfo(card);

                    // Base
                    var basePath = Path.Combine(outDir, info.Images.Base);
                    if (await ExportSingleAsync(card, basePath, scale, includeHoverTips: false))
                    {
                        log?.Invoke($"  保存 {info.Images.Base}");
                        savedImages++;
                    }
                    else
                    {
                        log?.Invoke($"  失败 {info.Images.Base}");
                        failedImages++;
                    }

                    await WaitFrames(1);

                    // Upgraded
                    if (card.IsUpgradable)
                    {
                        var upgraded = card.ToMutable();
                        upgraded.UpgradeInternal();

                        var upPath = Path.Combine(outDir, info.Images.Upgraded!);
                        if (await ExportSingleAsync(upgraded, upPath, scale, includeHoverTips: false))
                        {
                            log?.Invoke($"  保存 {info.Images.Upgraded}");
                            savedImages++;
                        }
                        else
                        {
                            log?.Invoke($"  失败 {info.Images.Upgraded}");
                            failedImages++;
                        }

                        await WaitFrames(1);
                    }

                    manifest.Cards.Add(info);
                }
                catch (Exception ex)
                {
                    failedImages++;
                    log?.Invoke($"[ExportAll] 错误: {card.Id.Entry} - {ex.Message}");
                }

                await WaitFrames(2);
            }

            // 第 2 轮：导出所有 CardHoverTip 引用的卡牌（去重）
            log?.Invoke("[ExportAll] 开始导出引用卡牌...");
            foreach (var card in karenCards)
            {
                foreach (var tip in card.HoverTips)
                {
                    if (tip is not CardHoverTip cardTip || cardTip.Card == null) continue;

                    var refId = cardTip.Card.Id.Entry;
                    if (exportedRefCards.Contains(refId)) continue;

                    var refFileName = $"{SanitizeFileName(refId)}_base.png";
                    var refPath = Path.Combine(outDir, refFileName);

                    if (await ExportSingleAsync(cardTip.Card, refPath, scale, includeHoverTips: false))
                    {
                        log?.Invoke($"  保存引用 {refFileName}");
                        savedImages++;
                        exportedRefCards.Add(refId);
                    }
                    else
                    {
                        log?.Invoke($"  失败引用 {refFileName}");
                        failedImages++;
                    }

                    await WaitFrames(1);

                    if (!cardTip.Card.IsUpgradable) continue;

                    var refUpgradedFileName = $"{SanitizeFileName(refId)}_upgraded.png";
                    var refUpgradedPath = Path.Combine(outDir, refUpgradedFileName);
                    var upgraded = ModelDb.GetById<CardModel>(cardTip.Card.Id).ToMutable();
                    upgraded.UpgradeInternal();

                    if (await ExportSingleAsync(upgraded, refUpgradedPath, scale, includeHoverTips: false))
                    {
                        log?.Invoke($"  保存引用 {refUpgradedFileName}");
                        savedImages++;
                    }
                    else
                    {
                        log?.Invoke($"  失败引用 {refUpgradedFileName}");
                        failedImages++;
                    }

                    await WaitFrames(1);
                }
            }

            // 写入卡牌 JSON
            var jsonPath = Path.Combine(outDir, "cards.json");
            var json = JsonSerializer.Serialize(manifest, JsonOptions);
            await File.WriteAllTextAsync(jsonPath, json);

            // 写入游戏信息 JSON（卡牌 / 遗物 / 怪物 / 药水）
            try
            {
                var gameInfo = new GameInfoExport
                {
                    Cards = ModelDb.AllCards
                        .OrderBy(c => c.Id.Entry, StringComparer.OrdinalIgnoreCase)
                        .Select(c => new IdNamePair { Id = c.Id.Entry, Name = c.Title ?? c.Id.Entry })
                        .ToList(),
                    Relics = ModelDb.AllRelics
                        .OrderBy(r => r.Id.Entry, StringComparer.OrdinalIgnoreCase)
                        .Select(r => new IdNamePair { Id = r.Id.Entry, Name = r.Title?.GetFormattedText() ?? r.Id.Entry })
                        .ToList(),
                    Potions = ModelDb.AllPotions
                        .OrderBy(p => p.Id.Entry, StringComparer.OrdinalIgnoreCase)
                        .Select(p => new IdNamePair { Id = p.Id.Entry, Name = p.Title?.GetFormattedText() ?? p.Id.Entry })
                        .ToList(),
                    Monsters = ModelDb.Monsters
                        .OrderBy(m => m.Id.Entry, StringComparer.OrdinalIgnoreCase)
                        .Select(m => new IdNamePair { Id = m.Id.Entry, Name = m.Title?.GetFormattedText() ?? m.Id.Entry })
                        .ToList(),
                    Encounters = ModelDb.AllEncounters
                        .OrderBy(e => e.Id.Entry, StringComparer.OrdinalIgnoreCase)
                        .Select(e => new IdNamePair { Id = e.Id.Entry, Name = e.Title?.GetFormattedText() ?? e.Id.Entry })
                        .ToList(),
                    Events = ModelDb.AllEvents
                        .Concat(ModelDb.AllAncients)
                        .GroupBy(e => e.Id.Entry, StringComparer.OrdinalIgnoreCase)
                        .Select(g => g.First())
                        .OrderBy(e => e.Id.Entry, StringComparer.OrdinalIgnoreCase)
                        .Select(e => new IdNamePair { Id = e.Id.Entry, Name = e.Title?.GetFormattedText() ?? e.Id.Entry })
                        .ToList(),
                    Characters = ModelDb.AllCharacters
                        .OrderBy(c => c.Id.Entry, StringComparer.OrdinalIgnoreCase)
                        .Select(c => new IdNamePair { Id = c.Id.Entry, Name = c.Title?.GetFormattedText() ?? c.Id.Entry })
                        .ToList(),
                };

                var infoJsonPath = Path.Combine(outDir, "game_info.json");
                var infoJson = JsonSerializer.Serialize(gameInfo, JsonOptions);
                await File.WriteAllTextAsync(infoJsonPath, infoJson);
                log?.Invoke($"[ExportAll] 游戏信息已导出: {infoJsonPath}");
            }
            catch (Exception ex)
            {
                log?.Invoke($"[ExportAll] 游戏信息导出失败: {ex.Message}");
            }

            log?.Invoke($"[ExportAll] 完成。Karen 卡牌: {manifest.Cards.Count}, 引用卡牌: {exportedRefCards.Count}, 图片成功: {savedImages}, 失败: {failedImages}, JSON: {jsonPath}");
            return new ExportResult
            {
                Success = failedImages == 0,
                OutputDirectory = outDir,
                CardCount = manifest.Cards.Count,
                ReferenceCardCount = exportedRefCards.Count,
                SavedImages = savedImages,
                FailedImages = failedImages,
                Error = failedImages == 0 ? null : $"有 {failedImages} 张图片导出失败，请查看日志。",
            };
        }

        /// <summary>
        /// 收集卡牌元数据。
        /// </summary>
        public static async Task<JsonExportResult> ExportJsonOnlyAsync(
            string? outputDirectory = null,
            Action<string>? log = null)
        {
            if (!CanExport(out var err))
            {
                log?.Invoke($"[ExportJsonOnly] 失败: {err}");
                return new JsonExportResult
                {
                    Success = false,
                    Error = err,
                };
            }

            var outDir = ProjectSettings.GlobalizePath((outputDirectory ?? "user://KarenCardExports/").Trim());
            Directory.CreateDirectory(outDir);

            var manifest = new ExportManifest
            {
                Cards = ModelDb.AllCards
                    .Where(c => c.Pool is KarenCardPool)
                    .OrderBy(c => c.Id.Entry, StringComparer.OrdinalIgnoreCase)
                    .Select(CollectCardInfo)
                    .ToList(),
            };

            var jsonPath = Path.Combine(outDir, "cards.json");
            var json = JsonSerializer.Serialize(manifest, JsonOptions);
            await File.WriteAllTextAsync(jsonPath, json);
            log?.Invoke($"[ExportJsonOnly] 卡牌索引已导出: {jsonPath}");

            var gameInfo = BuildGameInfo();
            var infoJsonPath = Path.Combine(outDir, "game_info.json");
            var infoJson = JsonSerializer.Serialize(gameInfo, JsonOptions);
            await File.WriteAllTextAsync(infoJsonPath, infoJson);
            log?.Invoke($"[ExportJsonOnly] 游戏信息已导出: {infoJsonPath}");

            return new JsonExportResult
            {
                Success = true,
                OutputDirectory = outDir,
                CardCount = manifest.Cards.Count,
            };
        }

        private static CardExportInfo CollectCardInfo(CardModel card)
        {
            var baseName = SanitizeFileName(card.Id.Entry);
            var info = new CardExportInfo
            {
                Id = card.Id.Entry,
                Name = card.Title?.ToString() ?? card.Id.Entry,
                Cost = GetCostString(card),
                Rarity = GetRarityString(card.Rarity),
                Type = GetTypeString(card.Type),
                Images = new ImageInfo
                {
                    Base = $"{baseName}_base.png",
                    Upgraded = card.IsUpgradable ? $"{baseName}_upgraded.png" : null,
                },
            };

            foreach (var tip in card.HoverTips)
            {
                switch (tip)
                {
                    case HoverTip hoverTip:
                        info.Tips.Add(new TipInfo
                        {
                            Type = "text",
                            Title = hoverTip.Title?.ToString(),
                            Description = hoverTip.Description,
                        });
                        break;
                    case CardHoverTip cardTip:
                        var refId = cardTip.Card?.Id.Entry;
                        info.Tips.Add(new TipInfo
                        {
                            Type = "card",
                            CardId = refId,
                            Image = refId != null ? $"{SanitizeFileName(refId)}_base.png" : null,
                            UpgradedImage = refId != null && cardTip.Card != null && cardTip.Card.IsUpgradable ? $"{SanitizeFileName(refId)}_upgraded.png" : null,
                        });
                        break;
                }
            }

            return info;
        }

        private static GameInfoExport BuildGameInfo()
        {
            return new GameInfoExport
            {
                Cards = ModelDb.AllCards
                    .OrderBy(c => c.Id.Entry, StringComparer.OrdinalIgnoreCase)
                    .Select(c => new IdNamePair { Id = c.Id.Entry, Name = c.Title ?? c.Id.Entry })
                    .ToList(),
                Relics = ModelDb.AllRelics
                    .OrderBy(r => r.Id.Entry, StringComparer.OrdinalIgnoreCase)
                    .Select(r => new IdNamePair { Id = r.Id.Entry, Name = r.Title?.GetFormattedText() ?? r.Id.Entry })
                    .ToList(),
                Potions = ModelDb.AllPotions
                    .OrderBy(p => p.Id.Entry, StringComparer.OrdinalIgnoreCase)
                    .Select(p => new IdNamePair { Id = p.Id.Entry, Name = p.Title?.GetFormattedText() ?? p.Id.Entry })
                    .ToList(),
                Monsters = ModelDb.Monsters
                    .OrderBy(m => m.Id.Entry, StringComparer.OrdinalIgnoreCase)
                    .Select(m => new IdNamePair { Id = m.Id.Entry, Name = m.Title?.GetFormattedText() ?? m.Id.Entry })
                    .ToList(),
                Encounters = ModelDb.AllEncounters
                    .OrderBy(e => e.Id.Entry, StringComparer.OrdinalIgnoreCase)
                    .Select(e => new IdNamePair { Id = e.Id.Entry, Name = e.Title?.GetFormattedText() ?? e.Id.Entry })
                    .ToList(),
                Events = ModelDb.AllEvents
                    .Concat(ModelDb.AllAncients)
                    .GroupBy(e => e.Id.Entry, StringComparer.OrdinalIgnoreCase)
                    .Select(g => g.First())
                    .OrderBy(e => e.Id.Entry, StringComparer.OrdinalIgnoreCase)
                    .Select(e => new IdNamePair { Id = e.Id.Entry, Name = e.Title?.GetFormattedText() ?? e.Id.Entry })
                    .ToList(),
                Characters = ModelDb.AllCharacters
                    .OrderBy(c => c.Id.Entry, StringComparer.OrdinalIgnoreCase)
                    .Select(c => new IdNamePair { Id = c.Id.Entry, Name = c.Title?.GetFormattedText() ?? c.Id.Entry })
                    .ToList(),
            };
        }

        /// <summary>
        /// 获取卡牌费用字符串。
        /// </summary>
        private static string GetCostString(CardModel card)
        {
            var hasX = (bool?)AccessTools.Property(typeof(CardModel), "HasEnergyCostX")?.GetValue(card);
            if (hasX == true)
                return "X";

            var resolved = card.EnergyCost?.GetResolved() ?? 0;
            if (resolved < 0)
                return "";

            return resolved.ToString();
        }

        /// <summary>
        /// 稀有度转中文。
        /// </summary>
        private static string GetRarityString(CardRarity rarity) => rarity switch
        {
            CardRarity.Basic => "基础",
            CardRarity.Common => "普通",
            CardRarity.Uncommon => "罕见",
            CardRarity.Rare => "稀有",
            CardRarity.Ancient => "远古",
            CardRarity.Event => "事件",
            CardRarity.Token => "衍生",
            CardRarity.Status => "状态",
            CardRarity.Curse => "诅咒",
            CardRarity.Quest => "任务",
            _ => rarity.ToString(),
        };

        /// <summary>
        /// 类型转中文。
        /// </summary>
        private static string GetTypeString(CardType type) => type switch
        {
            CardType.Attack => "攻击",
            CardType.Skill => "技能",
            CardType.Power => "能力",
            CardType.Status => "状态",
            CardType.Curse => "诅咒",
            _ => type.ToString(),
        };

        /// <summary>
        /// 导出单张卡牌为 PNG。
        /// </summary>
        public static async Task<bool> ExportSingleAsync(CardModel card, string outputPath, float scale = 1f, bool includeHoverTips = true)
        {
            if (!CanExport(out var err))
            {
                GD.PushError($"[CardPngExportUtil] {err}");
                return false;
            }

            var absPath = ProjectSettings.GlobalizePath(outputPath);
            scale = Mathf.Max(0.25f, scale);

            var host = new Control { Name = "CardExportHost", Position = new Vector2(-5000, -5000) };
            var ok = false;

            try
            {
                var built = BuildViewport(card, scale, includeHoverTips);
                host.AddChild(built.Viewport);
                NGame.Instance.AddChild(host);

                await WaitFrames(FramesAfterHostAdded);

                if (GodotObject.IsInstanceValid(built.MainCard))
                    RefreshCardVisuals(built.MainCard);

                await WaitFrames(FramesAfterVisualApply);

                if (built.HoverRow != null && GodotObject.IsInstanceValid(built.HoverRow))
                {
                    built.HoverRow.QueueSort();
                    await WaitFrames(FramesAfterHoverLayout);
                    ResizeViewportToHoverRow(built);
                    await WaitFrames(FramesAfterHoverResize);
                }

                if (!GodotObject.IsInstanceValid(built.Viewport))
                    return false;

                built.Viewport.RenderTargetUpdateMode = SubViewport.UpdateMode.Once;
                await WaitFrames(FramesAfterRender);

                if (!GodotObject.IsInstanceValid(built.Viewport))
                    return false;

                var tex = built.Viewport.GetTexture();
                if (tex == null)
                {
                    GD.PushError("[CardPngExportUtil] viewport texture was null.");
                    return false;
                }

                using var image = tex.GetImage()?.Duplicate() as Image;
                if (image == null)
                {
                    GD.PushError("[CardPngExportUtil] viewport image was null.");
                    return false;
                }

                var saveErr = image.SavePng(absPath);
                ok = saveErr == Error.Ok;
                if (!ok)
                    GD.PushError($"[CardPngExportUtil] SavePng failed: {saveErr}");
            }
            catch (Exception ex)
            {
                GD.PushError($"[CardPngExportUtil] {ex}");
            }
            finally
            {
                await WaitFrames(FramesBeforeTeardown);
                if (GodotObject.IsInstanceValid(host))
                    TeardownHost(host);
            }

            return ok;
        }

        /// <summary>
        /// 检查当前环境是否可以导出。
        /// </summary>
        public static bool CanExport(out string error)
        {
            if (NGame.Instance == null)
            {
                error = "游戏未加载，请先进入主菜单或战斗。";
                return false;
            }
            error = "";
            return true;
        }

        // ==================== 私有实现 ====================

        private static BuiltViewport BuildViewport(CardModel card, float scale, bool includeHoverTips)
        {
            var (cardW, cardH) = ComputeCardSize(scale);
            var vp = new SubViewport
            {
                TransparentBg = true,
                RenderTargetUpdateMode = SubViewport.UpdateMode.Always,
            };
            var root = new Control { Name = "ExportRoot" };

            if (!includeHoverTips)
            {
                var frame = Mathf.RoundToInt(FramePad);
                var vpW = cardW + frame * 2;
                var vpH = cardH + frame * 2;
                vp.Size = new Vector2I(vpW, vpH);
                root.CustomMinimumSize = new Vector2(vpW, vpH);
                root.Size = new Vector2(vpW, vpH);

                var mainNCard = CreateNCard(card, scale, new Vector2(frame, frame));
                root.AddChild(mainNCard);
                vp.AddChild(root);

                return new BuiltViewport(vp, root, mainNCard, null, []);
            }

            // 含 HoverTips 模式：横向排列 [参考卡牌列 | 主卡牌 | 文本提示列]
            var pad = Mathf.RoundToInt(FramePad);
            var cardSlotW = cardW;
            var initW = cardSlotW + 800;
            var initH = cardH + pad * 2 + 120;
            vp.Size = new Vector2I(initW, initH);
            root.CustomMinimumSize = new Vector2(initW, initH);
            root.Size = new Vector2(initW, initH);

            var row = new HBoxContainer
            {
                Position = new Vector2(pad, pad),
                Name = "HoverRow",
            };
            row.AddThemeConstantOverride("separation", HoverColumnSeparation);
            root.AddChild(row);

            var refCardsColumn = new VBoxContainer { Name = "RefCardsColumn" };
            refCardsColumn.AddThemeConstantOverride("separation", RefCardHoverGap);
            row.AddChild(refCardsColumn);

            var cardSlot = new Control
            {
                CustomMinimumSize = new Vector2(cardSlotW, cardH),
                Name = "CardSlot",
            };
            row.AddChild(cardSlot);
            var nCard = CreateNCard(card, scale, Vector2.Zero);
            cardSlot.AddChild(nCard);

            var textTipsColumn = new VBoxContainer { Name = "TextTipsColumn" };
            textTipsColumn.AddThemeConstantOverride("separation", Mathf.RoundToInt(HoverTipGap));
            row.AddChild(textTipsColumn);

            var refCardNodes = new List<NCard>();
            PopulateHoverTips(refCardsColumn, textTipsColumn, card, refCardNodes);

            vp.AddChild(root);
            return new BuiltViewport(vp, root, nCard, row, refCardNodes);
        }

        private static NCard CreateNCard(CardModel card, float scale, Vector2 topLeft)
        {
            var scene = ResourceLoader.Load<PackedScene>(CardScenePath);
            var nCard = scene.Instantiate<NCard>();
            nCard.OnInstantiated();
            nCard.Model = card;
            nCard.Visible = true;

            nCard.Scale = Vector2.One * scale;
            var minLocal = new Vector2(-CardHalfExtentX, -CardHalfExtentY);
            nCard.Position = new Vector2(
                Mathf.Round(topLeft.X - minLocal.X * scale),
                Mathf.Round(topLeft.Y - minLocal.Y * scale));
            return nCard;
        }

        private static void RefreshCardVisuals(NCard nCard)
        {
            nCard.UpdateVisuals(PileType.None, CardPreviewMode.Normal);
            if (nCard.Model is { IsUpgraded: true })
                nCard.ShowUpgradePreview();
        }

        private static void PopulateHoverTips(VBoxContainer refCardsColumn, VBoxContainer textTipsColumn, CardModel card, List<NCard> refCardNodes)
        {
            var tips = card.HoverTips;
            foreach (var tip in tips)
            {
                switch (tip)
                {
                    case HoverTip hoverTip:
                        AddTextTip(textTipsColumn, hoverTip);
                        break;
                    case CardHoverTip cardTip:
                        AddCardTip(refCardsColumn, cardTip, refCardNodes);
                        break;
                }
            }
        }

        private static void AddTextTip(VBoxContainer column, HoverTip tip)
        {
            var scene = ResourceLoader.Load<PackedScene>(HoverTipScenePath);
            var control = scene.Instantiate<Control>();
            column.AddChild(control);

            var title = control.GetNode<MegaLabel>("%Title");
            if (tip.Title == null)
                title.Visible = false;
            else
                title.SetTextAutoSize(tip.Title);

            var desc = control.GetNode<MegaRichTextLabel>("%Description");
            desc.Text = tip.Description;
            desc.AutowrapMode = tip.ShouldOverrideTextOverflow
                ? TextServer.AutowrapMode.Off
                : TextServer.AutowrapMode.WordSmart;

            var icon = control.GetNode<TextureRect>("%Icon");
            if (icon != null)
                icon.Texture = tip.Icon;

            if (tip.IsDebuff)
            {
                var bg = control.GetNode<CanvasItem>("%Bg");
                if (bg != null)
                    bg.Material = ResourceLoader.Load<Material>(HoverTipDebuffMaterialPath);
            }

            control.CustomMinimumSize = new Vector2(HoverTipWidth, 0f);
            control.ResetSize();
        }

        private static void AddCardTip(VBoxContainer column, CardHoverTip tip, List<NCard> refCardNodes)
        {
            var scene = ResourceLoader.Load<PackedScene>(CardHoverTipScenePath);
            var control = scene.Instantiate<Control>();
            column.AddChild(control);

            var (paddedW, paddedH) = ComputeCardSize(RefCardScale);
            control.CustomMinimumSize = new Vector2(paddedW, paddedH);
            control.ResetSize();

            var node = control.GetNode<NCard>("%Card");
            node.Model = tip.Card;
            node.SetAnchorsPreset(Control.LayoutPreset.TopLeft);
            node.Scale = Vector2.One * RefCardScale;
            var minLocal = new Vector2(-CardHalfExtentX, -CardHalfExtentY);
            node.Position = new Vector2(
                Mathf.Round(-minLocal.X * RefCardScale),
                Mathf.Round(-minLocal.Y * RefCardScale));
            refCardNodes.Add(node);
        }

        private static void ResizeViewportToHoverRow(BuiltViewport built)
        {
            if (built.HoverRow == null || !GodotObject.IsInstanceValid(built.HoverRow))
                return;
            if (!GodotObject.IsInstanceValid(built.Viewport) || !GodotObject.IsInstanceValid(built.Root))
                return;

            var pad = Mathf.RoundToInt(FramePad);
            var sz = built.HoverRow.GetCombinedMinimumSize();
            var w = Mathf.CeilToInt(pad * 2 + sz.X);
            var h = Mathf.CeilToInt(pad * 2 + sz.Y);
            built.Viewport.Size = new Vector2I(w, h);
            built.Root.CustomMinimumSize = new Vector2(w, h);
            built.Root.Size = new Vector2(w, h);
        }

        private static async Task WaitFrames(int count)
        {
            var tree = NGame.Instance?.GetTree();
            if (tree == null) return;
            for (var i = 0; i < count; i++)
                await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
        }

        private static void TeardownHost(Control host)
        {
            if (!GodotObject.IsInstanceValid(host))
                return;

            host.GetParent()?.RemoveChild(host);

            var postOrder = new List<Node>();
            CollectPostOrder(host);

            foreach (var node in postOrder.Where(GodotObject.IsInstanceValid))
            {
                if (node is NCard nCard)
                    nCard.QueueFree();
                else
                    node.QueueFree();
            }

            return;

            void CollectPostOrder(Node n)
            {
                foreach (var c in n.GetChildren())
                    CollectPostOrder(c);
                postOrder.Add(n);
            }
        }

        private static (int w, int h) ComputeCardSize(float scale)
        {
            var w = Mathf.CeilToInt(2f * CardHalfExtentX * scale);
            var h = Mathf.CeilToInt(2f * CardHalfExtentY * scale);
            return (w, h);
        }

        private static string SanitizeFileName(string entry)
        {
            var invalid = Path.GetInvalidFileNameChars();
            var s = new string(entry.Select(c => invalid.Contains(c) ? '_' : c).ToArray());
            return string.IsNullOrEmpty(s) ? "card" : s;
        }

        private sealed class BuiltViewport(
            SubViewport viewport,
            Control root,
            NCard mainCard,
            HBoxContainer? hoverRow,
            List<NCard> refCards)
        {
            public SubViewport Viewport { get; } = viewport;
            public Control Root { get; } = root;
            public NCard MainCard { get; } = mainCard;
            public HBoxContainer? HoverRow { get; } = hoverRow;
            public List<NCard> RefCards { get; } = refCards;
        }
    }
}
