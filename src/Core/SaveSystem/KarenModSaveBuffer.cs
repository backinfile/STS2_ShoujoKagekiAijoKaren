namespace ShoujoKagekiAijoKaren.src.Core.SaveSystem;

/// <summary>
/// 存档加载期间的临时缓冲区
///
/// 加载流程：
///   LoadRunSave Postfix → Store() → 等待战斗开始 → Consume() → ShineSaveSystem.RestoreShineData()
///
/// 消费时机：RunManager.SetUpSavedSinglePlayer/SetUpSavedMultiPlayer Postfix 中调用 Consume()，
/// 此时 RunState 和卡组均已就绪，将数据恢复至当前牌组的 CardModel 实例上。
/// </summary>
public static class KarenModSaveBuffer
{
    public static KarenRunSaveData? Pending { get; private set; }

    public static bool HasPending => Pending != null;

    public static void Store(KarenRunSaveData data)
    {
        Pending = data;
    }

    /// <summary>
    /// 取出缓冲数据并清空（消费后不可再次取用）
    /// </summary>
    public static KarenRunSaveData? Consume()
    {
        var data = Pending;
        Pending = null;
        return data;
    }
}
