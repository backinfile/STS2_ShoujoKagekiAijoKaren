using System.Collections.Generic;
using System.Runtime.InteropServices;
using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Characters;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.PotionPools;
using ShoujoKagekiAijoKaren.src.Core.Models.Cards.basic;
using ShoujoKagekiAijoKaren.src.Models.CardPools;
using ShoujoKagekiAijoKaren.src.Models.Cards;
using ShoujoKagekiAijoKaren.src.Models.RelicPools;
using ShoujoKagekiAijoKaren.src.Models.Relics;

namespace ShoujoKagekiAijoKaren.src.Models.Characters;

public sealed class Karen : CharacterModel
{
    public const string energyColorName = "karen";

    public override CharacterGender Gender => CharacterGender.Feminine;

    protected override CharacterModel? UnlocksAfterRunAs => null;

    public override Color NameColor => new Color("FB5458");

    public override int StartingHp => 72;

    public override int StartingGold => 99;

    public override CardPoolModel CardPool => ModelDb.CardPool<KarenCardPool>();

    public override PotionPoolModel PotionPool => ModelDb.PotionPool<IroncladPotionPool>();

    public override RelicPoolModel RelicPool => ModelDb.RelicPool<KarenRelicPool>();

    public override IEnumerable<CardModel> StartingDeck =>
    [
        ModelDb.Card<KarenStrike>(),
        ModelDb.Card<KarenStrike>(),
        ModelDb.Card<KarenStrike>(),
        ModelDb.Card<KarenStrike>(),
        ModelDb.Card<KarenShineStrike>(),
        ModelDb.Card<KarenDefend>(),
        ModelDb.Card<KarenDefend>(),
        ModelDb.Card<KarenDefend>(),
        ModelDb.Card<KarenDefend>(),
        ModelDb.Card<KarenFall>(),
    ];

    public override IReadOnlyList<RelicModel> StartingRelics =>
    [
        ModelDb.Relic<KarenHairpinRelic>()
    ];

    public override float AttackAnimDelay => 0.15f;

    public override float CastAnimDelay => 0.25f;

    public override Color EnergyLabelOutlineColor => new Color("A0383AFF");

    public override Color DialogueColor => new Color("8B2A2D");

    public override Color MapDrawingColor => new Color("FB5458");

    public override Color RemoteTargetingLineColor => new Color("FF7A7DFF");

    public override Color RemoteTargetingLineOutline => new Color("A0383AFF");

    public override List<string> GetArchitectAttackVfx()
    {
        var num = 5;
        var list = new List<string>(num);
        CollectionsMarshal.SetCount(list, num);
        var span = CollectionsMarshal.AsSpan(list);
        var num2 = 0;
        span[num2] = VfxCmd.bluntPath;
        num2++;
        span[num2] = VfxCmd.heavyBluntPath;
        num2++;
        span[num2] = VfxCmd.slashPath;
        num2++;
        span[num2] = VfxCmd.bloodyImpactPath;
        num2++;
        span[num2] = VfxCmd.rockShatterPath;
        return list;
    }
}
