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

namespace AuroraMod.AuroraCode.Cards.Uncommon;

/// <summary>
/// 44 联防协议 / Linked Defense（罕见，C）。获得 4 格挡，使所有护盾模块触发 1 次；若打出前在冷区，再触发 1 次。升级基础格挡 4→7。
/// 顺序（§4.6）：只读一次打出前区段 → 基础格挡 4/7 → TriggerAsync(Shield) 一次 → 若快照为冷区再 TriggerAsync(Shield) 一次。
/// 0 模块仍得基础格挡；每枚模块按自己独立 Value 触发（不先求总值再乘模块数）；第三槽自然纳入但每轮仍每枚一次；本牌不调热。
/// </summary>
public class AuroraLinkedDefense() : AuroraCard(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    protected override string ArtName => "linked_defense";

    /// <summary>金框：处于冷区时额外效果可触发（工坊反馈 #1，沿用原版 Dismantle/Spite 的金框语义）。</summary>
    protected override bool ShouldGlowGoldInternal => AuroraGlow.InZone(this, HeatPower.HeatZone.Cold);

    protected override IEnumerable<AuroraMechanic> MechanicTips => [AuroraMechanic.ShieldModule, AuroraMechanic.Heat];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new BlockVar(4, ValueProp.Move),
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var creature = Owner?.Creature;
        if (creature == null)
        {
            return;
        }

        // 1. 只读一次打出前区段。
        var wasCold = HeatPower.GetZone(creature) == HeatPower.HeatZone.Cold;

        // 2. 基础格挡 4/7。
        await CreatureCmd.GainBlock(creature, (int)DynamicVars.Block.BaseValue, ValueProp.Move, cardPlay);

        // 3. 触发全部护盾模块一次（攻击模块不触发；每枚按自己 Value）。
        await AuroraModuleController.TriggerAsync(choiceContext, creature, ModuleKind.Shield);

        // 4. 冷区快照为真则再触发一次（奖励维持冷区；本牌不升/散热，故不会自动离开冷区）。
        if (wasCold)
        {
            await AuroraModuleController.TriggerAsync(choiceContext, creature, ModuleKind.Shield);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Block.UpgradeValueBy(3m);   // 4 → 7
    }
}
