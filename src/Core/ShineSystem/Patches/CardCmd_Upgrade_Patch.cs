using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using ShoujoKagekiAijoKaren.src.KarenMod.ShineSystem;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace ShoujoKagekiAijoKaren.src.Core.ShineSystem.Patches;

/// <summary>
/// 卡牌升级补丁 - 在卡牌升级后恢复闪耀值
/// 闪耀值如果本来就是满的，会显示金色高亮
/// </summary>
[HarmonyPatch]
public static class CardCmd_Upgrade_Patch
{
    
}
