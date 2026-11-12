using System.Collections.Generic;
using BaseLib.Abstracts;
using BaseLib.Utils.Attributes;
using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Characters;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using ShoujoKagekiAijoKaren.src.Core.Models.Cards.basic;
using ShoujoKagekiAijoKaren.src.Core.Models.PotionPools;
using ShoujoKagekiAijoKaren.src.Models.CardPools;
using ShoujoKagekiAijoKaren.src.Models.Cards;
using ShoujoKagekiAijoKaren.src.Models.RelicPools;
using ShoujoKagekiAijoKaren.src.Models.Relics;

namespace ShoujoKagekiAijoKaren.src.Models.Characters;

[CustomID("KAREN")]
public sealed class Karen : CustomCharacterModel, ShoujoKageki.Core.V1.IShineRewardFallback
{
    public CardPoolModel ShineRewardFallback => ModelDb.CardPool<KarenCardPool>();
    public const string energyColorName = "karen";
    public const string OriginalCharacterSelectSfx = "event:/sfx/characters/ironclad/ironclad_select";
    public const string OriginalCharacterAttackSfx = "event:/sfx/characters/ironclad/ironclad_attack";
    public const string OriginalCharacterCastSfx = "event:/sfx/characters/ironclad/ironclad_cast";
    public const string OriginalCharacterDeathSfx = "event:/sfx/characters/ironclad/ironclad_die";
    public const string OriginalCharacterTransitionSfx = "event:/sfx/ui/wipe_ironclad";

    public static string CHAR_ID = StringHelper.Slugify(nameof(Karen));

    public override CharacterGender Gender => CharacterGender.Feminine;

    protected override CharacterModel? UnlocksAfterRunAs => null;

    public override Color NameColor => new Color("FB5458");

    public override int StartingHp => 72;

    public override int StartingGold => 99;

    public override CardPoolModel CardPool => ModelDb.CardPool<KarenCardPool>();

    public override PotionPoolModel PotionPool => ModelDb.PotionPool<KarenPotionPool>();

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

    public override string CharacterSelectSfx => OriginalCharacterSelectSfx;

    public override string CharacterTransitionSfx => OriginalCharacterTransitionSfx;

    public override List<string> GetArchitectAttackVfx() =>
        [VfxCmd.bluntPath, VfxCmd.heavyBluntPath, VfxCmd.slashPath, VfxCmd.bloodyImpactPath, VfxCmd.rockShatterPath];
}
