using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace ShoujoKagekiAijoKaren.src.Core.Patches;

/// <summary>
/// 将 NEnergyCounter._Ready() 中对 _backVfx 和 _frontVfx 的 GetNode 调用
/// 替换为 GetNodeOrNull，避免 KarenEnergyCounter 场景中缺少这两个节点时报错。
/// </summary>
[HarmonyPatch(typeof(NEnergyCounter), "_Ready")]
public class NEnergyCounterReadyPatch
{
    private static readonly MethodInfo _getNodeMethod;
    private static readonly MethodInfo _getNodeOrNullMethod;

    static NEnergyCounterReadyPatch()
    {
        var particlesType = AccessTools.TypeByName("MegaCrit.Sts2.Core.Nodes.Vfx.Utilities.NParticlesContainer")
                              ?? throw new System.TypeLoadException("无法找到 NParticlesContainer 类型");

        var getNodeGeneric = typeof(Node).GetMethods()
            .First(m => m.Name == "GetNode" && m.IsGenericMethodDefinition);
        _getNodeMethod = getNodeGeneric.MakeGenericMethod(particlesType);

        var getNodeOrNullGeneric = typeof(Node).GetMethods()
            .First(m => m.Name == "GetNodeOrNull" && m.IsGenericMethodDefinition);
        _getNodeOrNullMethod = getNodeOrNullGeneric.MakeGenericMethod(particlesType);
    }

    static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        foreach (var code in instructions)
        {
            if (code.opcode == OpCodes.Call && code.operand is MethodInfo mi && mi == _getNodeMethod)
            {
                code.operand = _getNodeOrNullMethod;
            }
            yield return code;
        }
    }
}
