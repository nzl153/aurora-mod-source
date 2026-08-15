using System;
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

namespace AuroraMod.AuroraCode.Cards.Rare;

/// <summary>
/// A-R03 极限断裂 / Limit Fracture（稀有，A 过热；单体终结技）。造成 28 伤害；本场每因自己的牌或过热实际失去 1 生命，
/// 本牌额外造成 2 伤害（最多额外 40）；随后积 5 热。升级：基础 28→34、额外上限 40→50（每点失血倍率恒为 2）。
/// 把「每点失血 +1、上限 +20」改为「每点失血 ×2、上限 +40/50」——A 过热流派真正的单体 Boss 终结技，
/// 提高伤害天花板但<b>不降费、不降门槛</b>（仍 3 费，仍要求先真实付出自损代价）。
/// 结算：读 <see cref="AuroraSelfHarmTrackerPower"/> 累计的实际自损 → RiskBonus=min(累计×2, 上限) → 基础+RiskBonus 合并为一段 powered
/// 攻击（整段只吃一次力量/易伤/过载/超频/锁定+2）→ 战斗仍在进行积 5 热。风险加值不拆段；只看实际掉血；击杀结束战斗不积热(胜利宽恕)。
/// 计数口径（tracker 既有语义，本轮未改）：只累计生命条真实下降（经格挡/减伤/灰烬后的净值），被格挡而没掉血的过热伤害不计；
/// 最大生命损失不写入 tracker，故 Overclock 每层 2 点最大生命代价<b>不计入</b>本牌加值。
/// </summary>
public class AuroraLimitFracture() : AuroraCard(3, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy)
{
    protected override string ArtName => "limit_fracture";

    protected override IEnumerable<AuroraMechanic> MechanicTips => [AuroraMechanic.Heat];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new AuroraScalingDamageVar(28, ValueProp.Move, c =>
            Math.Min(AuroraSelfHarmTrackerPower.Get(c.Owner?.Creature) * (int)c.DynamicVars["HpScaling"].BaseValue,
                     (int)c.DynamicVars["RiskBonusCap"].BaseValue)),
        new DynamicVar("HpScaling", 2m),
        new DynamicVar("RiskBonusCap", 40m),
        new PowerVar<HeatPower>(5),
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var creature = Owner?.Creature;
        if (creature == null)
        {
            return;
        }

        // 读本场实际自损累计 → 每点 ×HpScaling → 封顶 RiskBonusCap → 并入单段基础伤害（不拆段）。
        var loss = AuroraSelfHarmTrackerPower.Get(creature);
        var risk = Math.Min(loss * (int)DynamicVars["HpScaling"].BaseValue,
                            (int)DynamicVars["RiskBonusCap"].BaseValue);
        var dmg = (int)DynamicVars.Damage.BaseValue + risk;

        await CommonActions.CardAttack(this, cardPlay, cardPlay.Target, dmg, ValueProp.Move).Execute(choiceContext);

        // 战斗仍在进行才积 5 热（胜利宽恕：击杀最后敌人则不积）。
        if (CombatManager.Instance.IsInProgress)
        {
            await HeatPower.AddHeatAsync(choiceContext, creature, 5, this);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(6m);              // 28 → 34
        DynamicVars["RiskBonusCap"].UpgradeValueBy(10m);    // 40 → 50（倍率恒为 2，不随升级变化）
    }
}
