using MegaCrit.Sts2.Core.Entities.Players;
using ShoujoKagekiAijoKaren.src.Core.DisableRelicSystem;
using System.Threading.Tasks;

namespace ShoujoKagekiAijoKaren.src.Core.Commands;

/// <summary>
/// 禁用遗物命令
/// 从右向左查找并禁用指定数量的非BOSS遗物，将其替换为锁定遗物
/// </summary>
public static class DisableRelicCmd
{
    /// <summary>
    /// 从右向左禁用最右侧的指定数量非先古遗物
    /// </summary>
    /// <param name="player">玩家</param>
    /// <param name="count">要禁用的遗物数量</param>
    /// <returns>实际禁用的遗物数量</returns>
    public static async Task<int> DisableRelic(Player player, int count)
    {
        if (player == null || count <= 0) return 0;

        int disabledCount = 0;
        int searchEndPosition = player.Relics.Count;

        // 调试日志：打印当前遗物列表状态
        for (int i = 0; i < player.Relics.Count; i++)
        {
            var r = player.Relics[i];
            MainFile.Logger.Info($"[DisableRelicCmd] Relic[{i}] = {r.Id.Entry}, Lockable={DisableRelicManager.IsRelicLockable(r)}, Rarity={r.Rarity}");
        }

        while (disabledCount < count)
        {
            // 从右向左查找可禁用的遗物位置
            int position = DisableRelicManager.GetDisableableRelicPosition(player, searchEndPosition);
            MainFile.Logger.Info($"[DisableRelicCmd] Search endPosition={searchEndPosition}, found position={position}");

            // 没有可禁用的遗物了
            if (position < 0)
            {
                MainFile.Logger.Warn($"[DisableRelicCmd] Could only disable {disabledCount}/{count} relics for player {player.NetId} (not enough eligible relics)");
                break;
            }

            // 禁用该位置的遗物
            if (DisableRelicManager.DisableRelicAtPosition(player, position))
            {
                disabledCount++;
                // 下次搜索到当前位置为止（因为列表长度不变，只是替换了遗物）
                searchEndPosition = position;
            }
            else
            {
                // 禁用失败，跳过这个位置继续搜索
                searchEndPosition = position;
            }
        }

        return disabledCount;
    }

    /// <summary>
    /// 获取可以禁用的遗物数量
    /// 用于卡牌描述中显示当前可禁用遗物的数量
    /// </summary>
    /// <param name="player">玩家</param>
    /// <returns>可禁用的遗物数量</returns>
    public static int GetDisableableRelicCount(Player player)
    {
        return DisableRelicManager.GetDisableableRelicCount(player);
    }

    /// <summary>
    /// 检查是否还有可以禁用的遗物
    /// </summary>
    /// <param name="player">玩家</param>
    /// <returns>是否有可禁用的遗物</returns>
    public static bool HasDisableableRelic(Player player)
    {
        return DisableRelicManager.HasDisableableRelic(player);
    }
}
