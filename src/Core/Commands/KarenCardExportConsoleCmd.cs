using MegaCrit.Sts2.Core.DevConsole;
using MegaCrit.Sts2.Core.DevConsole.ConsoleCommands;
using MegaCrit.Sts2.Core.Entities.Players;
using ShoujoKagekiAijoKaren.src.Core.Utils;
using System;
using System.Globalization;
using System.Linq;

namespace ShoujoKagekiAijoKaren.src.Core.Commands
{
    /// <summary>
    /// 调试控制台命令：导出 Karen 所有卡牌的卡图，同时导出游戏内所有卡牌、遗物、怪物和药水的 ID 与中文名映射。
    /// 用法：<c>karen exportcards [path] [scale]</c>
    /// </summary>
    public sealed class KarenCardExportConsoleCmd : AbstractConsoleCmd
    {
        private static readonly string[] Actions = ["exportcards"];

        public override string CmdName => "karen";
        public override string Args => "exportcards [path] [scale]";
        public override string Description => "Export all Karen card images and game info (cards/relics/monsters/potions). Default path: user://KarenCardExports/, default scale: 1.";
        public override bool IsNetworked => false;

        public override CompletionResult GetArgumentCompletions(Player? player, string[] args)
        {
            if (args.Length <= 1)
            {
                var partial = args.Length == 0 ? string.Empty : args[0];
                return CompleteArgument(Actions, [], partial, CompletionType.Subcommand);
            }
            return base.GetArgumentCompletions(player, args);
        }

        public override CmdResult Process(Player? issuingPlayer, string[] args)
        {
            if (args.Length == 0 || !args[0].Equals("exportcards", StringComparison.OrdinalIgnoreCase))
                return new(false, "Usage: karen exportcards [path] [scale]");

            var path = "user://KarenCardExports/";
            var scale = 1f;

            if (args.Length >= 2)
                path = args[1];

            if (args.Length >= 3 && float.TryParse(args[2], NumberStyles.Float, CultureInfo.InvariantCulture, out var s))
                scale = s;

            if (!CardPngExportUtil.CanExport(out var err))
                return new(false, $"Cannot export: {err}");

            _ = RunExportAsync(path, scale);
            return new(true, $"Karen card export started (includes game info). Path: {path}, Scale: {scale}. Check game log for progress.");
        }

        private static async System.Threading.Tasks.Task RunExportAsync(string path, float scale)
        {
            await CardPngExportUtil.ExportAllKarenCardsAsync(path, scale, log: msg =>
            {
                MainFile.Logger.Info(msg);
            });
        }
    }
}
