using BaseLib.Utils;
using AuroraMod.AuroraCode.Cards;
using AuroraMod.AuroraCode.DynamicVars;
using AuroraMod.AuroraCode.Helpers;
using AuroraMod.AuroraCode.Powers;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.ValueProps;

namespace AuroraMod.AuroraCode.Cards.Uncommon;

/// <summary>
/// C-U06 编队突击 / Formation Assault（罕见，C 悬浮模块）。造成 7 伤害，打出前每有 1 枚模块 +4；若同时有攻击与护盾模块，伤害后散 1 热。
/// 升级：基础伤害 7→10。
/// 每模块 2→4——给罕见位一张真正的模块兑现小终结技（满 3 槽 19/22 伤），补前期输出。
/// 只按打出前「实际已部署模块数量」计算，不看空槽/容量/强化值。
/// 结算：读打出前模块数量与构型快照 → 伤害=基础+数量×每模块，合并为一段 powered 攻击（整段统一吃力量/易伤/过载×1.25/取整/锁定+2）→
/// 若混合构型且战斗仍在进行则 <see cref="HeatPower.VentUpToAsync"/>(1) 散 1 热。不触发任何模块；伤害只看部署数量不看强化值（防双重指数）。
/// 规格原写 VentAsync（实为散尽全部热量）→ 改用 VentUpToAsync(1) 精确散 1。
/// </summary>
public class AuroraFormationAssault() : AuroraCard(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
{
    protected override string ArtName => "formation_assault";

    protected override IEnumerable<AuroraMechanic> MechanicTips =>
        [AuroraMechanic.AttackModule, AuroraMechanic.ShieldModule, AuroraMechanic.Heat];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new AuroraScalingDamageVar(7, ValueProp.Move, c =>
            AuroraModuleController.Count(c.Owner?.Creature) * (int)c.DynamicVars["DamagePerModule"].BaseValue),
        new DynamicVar("DamagePerModule", 4m),
        new DynamicVar("VentAmount", 1m),
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var creature = Owner?.Creature;
        if (creature == null)
        {
            return;
        }

        // 打出前快照：模块数量与构型。
        var moduleCount = AuroraModuleController.Count(creature);
        var wasMixed = AuroraModuleController.CountOf(creature, ModuleKind.Attack) > 0
                       && AuroraModuleController.CountOf(creature, ModuleKind.Shield) > 0;

        // 数量加值先并入本段基础伤害，整段统一吃乘区。
        var dmg = (int)DynamicVars.Damage.BaseValue
                  + moduleCount * (int)DynamicVars["DamagePerModule"].BaseValue;
        await CommonActions.CardAttack(this, cardPlay, cardPlay.Target, dmg, ValueProp.Move).Execute(choiceContext);

        // 混合构型（同时有攻击与护盾模块）且战斗仍在进行 → 散 1 热。
        if (wasMixed && CombatManager.Instance.IsInProgress)
        {
            await HeatPower.VentUpToAsync(choiceContext, creature, (int)DynamicVars["VentAmount"].BaseValue, this);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(3m);   // 7 → 10
    }
}
