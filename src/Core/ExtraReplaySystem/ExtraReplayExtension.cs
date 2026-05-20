using BaseLib.Utils;
using MegaCrit.Sts2.Core.Models;

namespace ShoujoKagekiAijoKaren.src.Core.ExtraReplaySystem;

/// <summary>
/// 为 CardModel 动态附加"下一张打出时额外重播次数"
/// 使用 SpireField，无需修改原始类
/// </summary>
public static class ExtraReplayExtension
{
    private static readonly SpireField<CardModel, int> _extraReplayCountForNextPlay = new(() => 0);

    public static int GetExtraReplayCountForNextPlay(this CardModel card)
    {
        return _extraReplayCountForNextPlay.Get(card);
    }

    public static void SetExtraReplayCountForNextPlay(this CardModel card, int value)
    {
        _extraReplayCountForNextPlay.Set(card, value);
    }

    public static void AddExtraReplayCountForNextPlay(this CardModel card, int amount)
    {
        var current = _extraReplayCountForNextPlay.Get(card);
        _extraReplayCountForNextPlay.Set(card, current + amount);
    }
}
