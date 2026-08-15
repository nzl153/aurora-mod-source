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
/// C-U05 热灌调校 / Thermal Infusion（罕见，C 悬浮模块）。获得 7 格挡；若打出前至少 1 枚模块，使所有模块各 +1 强化，然后积 2 热。
/// 升级：格挡 7→10。
/// 结算：读打出前模块列表快照 → 基础格挡 → 无模块则结束不积热 → 有模块则 EnhanceAll(+1，每枚一次不乘数量) → 积 2 热。
/// 强化不触发模块（主动触发是同步/联防/交叉火力的职责）。积热在全部强化之后；跨红线只登记延迟过热，不清零。
/// </summary>
public class AuroraThermalInfusion() : AuroraCard(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    protected override string ArtName => "thermal_infusion";

    protected override IEnumerable<AuroraMechanic> MechanicTips =>
        [AuroraMechanic.AttackModule, AuroraMechanic.ShieldModule, AuroraMechanic.ModuleEnhancement, AuroraMechanic.Heat];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new BlockVar(7, ValueProp.Move),
        new DynamicVar("EnhancementAmount", 1m),
        new DynamicVar("HeatGain", 2m),
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var creature = Owner?.Creature;
        if (creature == null)
        {
            return;
        }

        // 1. 打出前读模块数量快照。
        var hadModule = AuroraModuleController.Count(creature) > 0;

        // 2. 基础格挡。
        await CreatureCmd.GainBlock(creature, (int)DynamicVars.Block.BaseValue, ValueProp.Move, cardPlay);

        // 3. 无模块结束（不积无意义热量）。
        if (!hadModule)
        {
            return;
        }

        // 4. 所有模块各 +1 强化（每枚一次，不乘数量）→ 积 2 热。
        await AuroraModuleController.EnhanceAllAsync(choiceContext, creature, (int)DynamicVars["EnhancementAmount"].BaseValue, this);
        await HeatPower.AddHeatAsync(choiceContext, creature, (int)DynamicVars["HeatGain"].BaseValue, this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Block.UpgradeValueBy(3m);   // 7 → 10
    }
}
