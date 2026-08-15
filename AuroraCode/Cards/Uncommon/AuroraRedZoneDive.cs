using BaseLib.Utils;
using AuroraMod.AuroraCode.Cards;
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
/// 28 红区俯冲 / Red-Zone Dive（罕见，A 过热暴走）。造成 16 伤害;若打出前不在过载区,随后将热量提升至 7;
/// 否则最多散 2 热。升级:伤害 16→21。
/// 结算(打出前读区段快照 → 单段 powered 攻击 → 仅首次调热):伤害在调热之前,保证本段按打出时实际区段吃过载增伤;
/// 打出前 Cold/Warm → AddHeat 到 7(delta=max(0,7-heat)),打出前 Overload/Critical → VentUpTo(2)。
/// 调热仅 IsFirstInSeries 执行(击杀后战斗仍继续也执行);本牌不主动触发过热(≤7)。Echo 重复伤害、调热仅一次。
/// </summary>
public class AuroraRedZoneDive() : AuroraCard(2, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
{
    protected override string ArtName => "red_zone_dive";

    protected override IEnumerable<AuroraMechanic> MechanicTips => [AuroraMechanic.Heat, AuroraMechanic.ZoneChange];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(16, ValueProp.Move),
        new DynamicVar("TargetHeat", 7m),
        new DynamicVar("VentMax", 2m),
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var creature = Owner?.Creature;
        if (creature == null)
        {
            return;
        }

        // 打出前读区段快照(决定调热方向);伤害不改变热量,故 delta 用打出前热量即可。
        var isFirst = cardPlay.IsFirstInSeries;
        var zone = HeatPower.GetZone(creature);

        // 1. 单段 powered 攻击(按打出时实际区段吃过载增伤)。
        var damage = (int)DynamicVars.Damage.BaseValue;
        await CommonActions.CardAttack(this, cardPlay, cardPlay.Target, damage, ValueProp.Move).Execute(choiceContext);

        // 2. 仅首次结算调热(伤害之后);击杀目标后仍执行。
        if (!isFirst || !CombatManager.Instance.IsInProgress)
        {
            return;
        }

        if (zone is HeatPower.HeatZone.Cold or HeatPower.HeatZone.Warm)
        {
            var delta = (int)DynamicVars["TargetHeat"].BaseValue - HeatPower.GetHeat(creature);
            if (delta > 0)
            {
                await HeatPower.AddHeatAsync(choiceContext, creature, delta, this);
            }
        }
        else
        {
            await HeatPower.VentUpToAsync(choiceContext, creature, (int)DynamicVars["VentMax"].BaseValue, this);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(5m);   // 16 → 21
    }
}
