using BaseLib.Utils;
using AuroraMod.AuroraCode.Cards;
using AuroraMod.AuroraCode.Helpers;
using AuroraMod.AuroraCode.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.ValueProps;

namespace AuroraMod.AuroraCode.Cards.Common;

/// <summary>
/// 26 散热护盾 / Vent Shield（普通，枢纽）。先获得 4 格挡，最多散 3 热；每实际散 1 热再获得 2 格挡。升级基础格挡 4→6。
/// 顺序（§4.4）：基础格挡 → <see cref="HeatPower.VentUpToAsync"/> 最多散 3 并取实际值 → 若 vented>0 再获得 2×vented 格挡。
/// 用「最多散 N」窄接口，绝不散尽、绝不先散尽再补热；格挡数按服务实际返回值，不按请求值 3 发奖励。
/// </summary>
public class AuroraVentShield() : AuroraCard(1, CardType.Skill, CardRarity.Common, TargetType.Self)
{
    protected override string ArtName => "vent_shield";

    protected override IEnumerable<AuroraMechanic> MechanicTips => [AuroraMechanic.Heat];

    private const int VentMax = 3;
    private const int BlockPerHeat = 2;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new BlockVar(4, ValueProp.Move),
        new DynamicVar("VentMax", VentMax),
        new DynamicVar("BlockPerHeat", BlockPerHeat),
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var creature = Owner?.Creature;
        if (creature == null)
        {
            return;
        }

        // 1. 基础格挡 4/6。
        await CreatureCmd.GainBlock(creature, (int)DynamicVars.Block.BaseValue, ValueProp.Move, cardPlay);

        // 2. 最多散 3 热，取实际散热量（不散尽、不过热）。
        var vented = await HeatPower.VentUpToAsync(choiceContext, creature, VentMax, this);

        // 3. 按实际散热量追加 2×vented 格挡。
        if (vented > 0)
        {
            await CreatureCmd.GainBlock(creature, BlockPerHeat * vented, ValueProp.Move, cardPlay);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Block.UpgradeValueBy(2m);   // 4 → 6
    }
}
