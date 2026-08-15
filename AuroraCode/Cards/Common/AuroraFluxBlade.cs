using BaseLib.Utils;
using AuroraMod.AuroraCode.Cards;
using AuroraMod.AuroraCode.Helpers;
using AuroraMod.AuroraCode.Powers;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.ValueProps;

namespace AuroraMod.AuroraCode.Cards.Common;

/// <summary>
/// 13 通量剑 / Flux Blade（普通，枢纽 稳定器）。造成 7 伤害；随后使热量向温区移动 1（冷区积热、过载区散热、温区不变）；<b>若打出前已处于温区，获得 2 剑势</b>。
/// 升级：伤害 7→10。温区身份：让它成为明确的温区/B 接口，而不是只比打击多 1 伤害。
/// 结算（打出前读一次区段 → 单段 powered 攻击 → 仅首次调热/温区剑势）：冷区 AddHeat(+1)、过载/临界 VentUpTo(1)、温区改为获 MomentumGain 剑势；
/// 温区剑势按打出前区段判定、不因攻击后其他效果改变；仅 IsFirstInSeries 执行；击杀目标后仍执行。Echo 每次伤害、仅首次调热/给势。
/// </summary>
public class AuroraFluxBlade() : AuroraCard(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
{
    protected override string ArtName => "flux_blade";

    protected override IEnumerable<AuroraMechanic> MechanicTips => [AuroraMechanic.Heat, AuroraMechanic.ZoneChange, AuroraMechanic.Momentum];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(8, ValueProp.Move),
        new DynamicVar("HeatStep", 1m),
        new DynamicVar("MomentumGain", 2m),
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var creature = Owner?.Creature;
        if (creature == null)
        {
            return;
        }

        // 打出前读一次区段（用于调热方向）。
        var isFirst = cardPlay.IsFirstInSeries;
        var zone = HeatPower.GetZone(creature);

        // 1. 单段 powered 攻击。
        var damage = (int)DynamicVars.Damage.BaseValue;
        await CommonActions.CardAttack(this, cardPlay, cardPlay.Target, damage, ValueProp.Move).Execute(choiceContext);

        // 2. 仅首次结算调热（伤害之后）；击杀目标也执行。
        if (!isFirst)
        {
            return;
        }

        var step = (int)DynamicVars["HeatStep"].BaseValue;
        switch (zone)
        {
            case HeatPower.HeatZone.Cold:
                await HeatPower.AddHeatAsync(choiceContext, creature, step, this);
                break;
            case HeatPower.HeatZone.Overload:
            case HeatPower.HeatZone.Critical:
                await HeatPower.VentUpToAsync(choiceContext, creature, step, this);
                break;
            case HeatPower.HeatZone.Warm:
                // 温区：不改变热量，改为获得剑势（温区身份，按打出前区段判定）。
                await AuroraMomentumService.GainAsync(choiceContext, creature, (int)DynamicVars["MomentumGain"].BaseValue, this);
                break;
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(3m);   // 7 → 10
    }
}
