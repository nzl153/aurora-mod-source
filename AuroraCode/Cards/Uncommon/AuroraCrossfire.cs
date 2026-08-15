using BaseLib.Utils;
using AuroraMod.AuroraCode.Cards;
using AuroraMod.AuroraCode.Helpers;
using AuroraMod.AuroraCode.Powers;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.ValueProps;

namespace AuroraMod.AuroraCode.Cards.Uncommon;

/// <summary>
/// 46 交叉火力 / Crossfire（罕见，C；双轴 A+C）。对所有敌人造成 6 点伤害；使所有攻击模块触发 1 次；然后积 1 热。升级群伤 6 → 8。
/// 顺序固定：powered AoE → <see cref="AuroraModuleController.TriggerAsync"/>(Attack) → 积 1 热。
/// 主 AoE 吃力量/易伤/过载；模块触发仍走中心 Unpowered 路径（不吃这些乘区、不推进连锁、按模块规则消费本人锁定），本牌不复制模块选敌/伤害/锁定逻辑。
/// </summary>
public class AuroraCrossfire() : AuroraCard(1, CardType.Attack, CardRarity.Uncommon, TargetType.AllEnemies)
{
    protected override string ArtName => "crossfire";

    protected override IEnumerable<AuroraMechanic> MechanicTips => [AuroraMechanic.AttackModule, AuroraMechanic.Heat];

    public override AuroraStrikeVfxKind StrikeVfx => AuroraStrikeVfxKind.Flurry;   // 群体横扫：紫刃齐射

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(6, ValueProp.Move),
        new PowerVar<HeatPower>(1m),
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var creature = Owner?.Creature;

        // 1) 主 powered 群攻（AllEnemies 由 CardAttack 自动分发到全体）。
        var damage = (int)DynamicVars.Damage.BaseValue;
        await CommonActions.CardAttack(this, cardPlay, cardPlay.Target, damage, ValueProp.Move).Execute(choiceContext);

        if (creature == null)
        {
            return;
        }

        // 2) 触发所有攻击模块各 1 次（护盾模块不触发）；3) 积 1 热。
        await AuroraModuleController.TriggerAsync(choiceContext, creature, ModuleKind.Attack);
        await HeatPower.AddHeatAsync(choiceContext, creature, 1, this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(2m);   // 6 → 8
    }
}
