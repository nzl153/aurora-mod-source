using BaseLib.Utils;
using AuroraMod.AuroraCode.Cards;
using AuroraMod.AuroraCode.Helpers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.ValueProps;

namespace AuroraMod.AuroraCode.Cards.Common;

/// <summary>
/// 18 战地维护 / Field Maintenance（普通，C 悬浮部件）。获得 6 格挡；若有模块，使「已获得强化量最少」的一枚获得 1 强化。
/// 升级：格挡 6→8，强化 1→2。
/// 结算（格挡→检查模块→强化一枚）：走 <see cref="AuroraModuleController.EnhanceLeastEnhancedAsync"/>——按 Value-BaseValue
/// 选真实强化量最少者、并列按稳定部署顺序（不按当前 Value，否则攻击模块基础 4 会被误判为比护盾 5「更弱」）。
/// 无模块仍是 6/8 格挡不空牌；两槽 / 三槽都只强化一枚，收益不随槽数复制。
/// </summary>
public class AuroraFieldMaintenance() : AuroraCard(1, CardType.Skill, CardRarity.Common, TargetType.Self)
{
    protected override string ArtName => "field_maintenance";

    protected override IEnumerable<AuroraMechanic> MechanicTips =>
        [AuroraMechanic.AttackModule, AuroraMechanic.ShieldModule, AuroraMechanic.ModuleEnhancement];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new BlockVar(6, ValueProp.Move),
        new DynamicVar("ModuleEnhancement", 1m),
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var creature = Owner?.Creature;
        if (creature == null)
        {
            return;
        }

        // 1. 基础格挡 6/8。
        await CreatureCmd.GainBlock(creature, (int)DynamicVars.Block.BaseValue, ValueProp.Move, cardPlay);

        // 2. 有模块则强化「强化量最少」的一枚（无模块自然返回 null，不空牌）。
        var amount = (int)DynamicVars["ModuleEnhancement"].BaseValue;
        await AuroraModuleController.EnhanceLeastEnhancedAsync(choiceContext, creature, amount, this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Block.UpgradeValueBy(2m);                 // 6 → 8
        DynamicVars["ModuleEnhancement"].UpgradeValueBy(1m);  // 1 → 2
    }
}
