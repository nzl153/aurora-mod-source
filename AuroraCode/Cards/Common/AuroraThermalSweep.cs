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
/// 23 热流横扫 / Thermal Sweep（普通，枢纽，群攻）。对所有敌人造成 5 伤害；冷区随后积 2 热，过载区随后最多散 1 热，温区不变。
/// 升级：伤害 5→7，调热不变。
/// 结算（读区段→群攻→按区段调热一次）：打出前只读一次区段；对全体各 1 段 powered 攻击（各自消费该敌本人锁定）；
/// 全部命中后只按区段调热一次（冷 AddHeat+2 / 过载·临界 VentUpTo(1) / 温区不动），调热不按命中数重复。
/// </summary>
public class AuroraThermalSweep() : AuroraCard(1, CardType.Attack, CardRarity.Common, TargetType.AllEnemies)
{
    protected override string ArtName => "thermal_sweep";

    protected override IEnumerable<AuroraMechanic> MechanicTips => [AuroraMechanic.Heat, AuroraMechanic.ZoneChange];

    public override AuroraStrikeVfxKind StrikeVfx => AuroraStrikeVfxKind.Flurry;   // 群体横扫：紫刃齐射

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(5, ValueProp.Move),
        new DynamicVar("HeatGain", 2m),
        new DynamicVar("VentMax", 1m),
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var creature = Owner?.Creature;

        // 1. 打出前只读一次区段。
        var zone = creature != null ? HeatPower.GetZone(creature) : HeatPower.HeatZone.Cold;

        // 2. 群体 powered 攻击（AllEnemies 由 CardAttack 自动分发；各段各自消费该敌身上本人锁定）。
        var damage = (int)DynamicVars.Damage.BaseValue;
        await CommonActions.CardAttack(this, cardPlay, cardPlay.Target, damage, ValueProp.Move).Execute(choiceContext);

        if (creature == null)
        {
            return;
        }

        // 3. 全部命中后只按区段调热一次（不按敌人数重复）。
        switch (zone)
        {
            case HeatPower.HeatZone.Cold:
                await HeatPower.AddHeatAsync(choiceContext, creature, (int)DynamicVars["HeatGain"].BaseValue, this);
                break;
            case HeatPower.HeatZone.Overload:
            case HeatPower.HeatZone.Critical:
                await HeatPower.VentUpToAsync(choiceContext, creature, (int)DynamicVars["VentMax"].BaseValue, this);
                break;
            // 温区不改变热量。
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(2m);   // 5 → 7
    }
}
