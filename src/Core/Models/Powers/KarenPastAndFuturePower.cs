using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;
using ShoujoKagekiAijoKaren.src.Core.Audio;
using ShoujoKagekiAijoKaren.src.Core.Models.Powers.tmpStrength;
using System.Threading.Tasks;

namespace ShoujoKagekiAijoKaren.src.Core.Models.Powers;

/// <summary>
/// 过去与未来 Power：从约定牌堆抽牌时获得临时力量
/// 附带专属音频轮播：draw1~draw4 按顺序循环播放，每回合重置，播放中不插队
/// </summary>
public sealed class KarenPastAndFuturePower : KarenBasePower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    // 当前播放到第几段（0~3 对应 draw1~draw4）
    private int _audioIndex = 0;

    // 当前正在播放的 AudioStreamPlayer，用于互斥检查
    private AudioStreamPlayer? _currentPlayer;

    public override async Task OnCardRemovedFromPromisePile(CardModel card)
    {
        if (Owner.Player is Player player)
        {
            Flash();
            await PowerCmd.Apply<KarenPastAndFutureTempStrengthPower>(
                player.Creature, Amount, player.Creature, null);

            // 播放轮播音频（互斥：播放中不插入下一段）
            TryPlayNextAudio();
        }
    }

    /// <summary>
    /// 玩家回合开始时重置音频索引到第 1 段
    /// </summary>
    public override Task AfterSideTurnStart(CombatSide side, CombatState combatState)
    {
        if (side == Owner.Side && Owner.IsPlayer)
        {
            _audioIndex = 0;
        }
        return Task.CompletedTask;
    }

    /// <summary>
    /// 尝试播放下一段音频。如果当前有音频在播放中，则跳过。
    /// </summary>
    private void TryPlayNextAudio()
    {
        if (_audioIndex >= 4) return;
        // 互斥检查：当前有音频在播放中，跳过
        if (_currentPlayer != null && _currentPlayer.Playing)
            return;

        var fileName = $"karen_draw{_audioIndex + 1}.ogg";
        var player = KarenAudioManager.Play(fileName, volume: 1f);
        if (player == null)
            return;

        _currentPlayer = player;

        // 播放完毕后清理引用
        player.Finished += () =>
        {
            if (_currentPlayer == player)
                _currentPlayer = null;
        };

        _audioIndex = _audioIndex + 1;
    }
}
