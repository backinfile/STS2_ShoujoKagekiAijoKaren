namespace ShoujoKagekiAijoKaren.src.Core.SaveSystem;

/// <summary>
/// 存档加载期间的额外字段缓冲区。
///
/// 加载流程：
///   ReadFile Postfix → Store() → Consume() → 恢复 Karen 闪耀耗尽牌堆数据
///
/// 消费时机：RunManager.SetUpSavedSinglePlayer/SetUpSavedMultiPlayer Postfix 中调用 Consume()，
/// 此时 RunState 和卡组均已就绪，将数据恢复至当前牌组的 CardModel 实例上。
/// </summary>
public static class KarenExtraFieldsSaveBuffer
{
    public static bool HasPending { get; private set; }

    public static void MarkPending()
    {
        HasPending = true;
    }

    /// <summary>
    /// 取出缓冲数据并清空（消费后不可再次取用）
    /// </summary>
    public static bool Consume()
    {
        bool pending = HasPending;
        HasPending = false;
        return pending;
    }
}
