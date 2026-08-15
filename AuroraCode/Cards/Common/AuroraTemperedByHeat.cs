using BaseLib.Utils;
using AuroraMod.AuroraCode.Cards;
using AuroraMod.AuroraCode.Helpers;
using AuroraMod.AuroraCode.Powers;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.ValueProps;

namespace AuroraMod.AuroraCode.Cards.Common;

/// <summary>
/// 07 以热淬刃 / Tempered by Heat（普通，A 过热暴走）。失去 2 生命，积 3 热，抽 2。升级：失去 2→1。
/// 结算固定顺序（失去生命 → 积热/过热 → 抽牌）：生命损失走原生 Unblockable|Unpowered（无视格挡、不吃力量/易伤/过载，
/// 来源=本牌可被原生减免识别、为未来 #64 生命损失记录提供合法来源）；积热走延迟过热（越 10 只登记待结算、回合末统一结算，
/// 本牌打出时不当场过热）；掉血未致死且战斗继续才抽牌（原生抽牌，可洗牌，非扫描）。掉血致死则不抽牌。每次真实结算完整执行。
/// </summary>
public class AuroraTemperedByHeat() : AuroraCard(1, CardType.Skill, CardRarity.Common, TargetType.Self)
{
    protected override string ArtName => "tempered_by_heat";

    protected override IEnumerable<AuroraMechanic> MechanicTips => [AuroraMechanic.Heat, AuroraMechanic.SystemCrash];

    private const int HeatGain = 3;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new HpLossVar("HpLoss", 2),
        new PowerVar<HeatPower>(HeatGain),
        new DynamicVar("DrawCount", 2m),
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var creature = Owner?.Creature;
        var player = Owner;
        if (creature == null || player == null)
        {
            return;
        }

        // 1. 失去生命（无视格挡、不吃力量/易伤/过载；走自损服务：套自损作用域供灰烬拦截、记录实际掉血供极限断裂累计）。
        var hpLoss = (int)DynamicVars["HpLoss"].BaseValue;
        if (hpLoss > 0)
        {
            await AuroraSelfHarm.ApplyAsync(choiceContext, creature, hpLoss,
                ValueProp.Unblockable | ValueProp.Unpowered, this);
        }

        // 2. 掉血致死或战斗结束则停止。
        if (creature.IsDead || !CombatManager.Instance.IsInProgress)
        {
            return;
        }

        // 3. 积热（延迟过热：越 10 只登记待结算，回合末统一结算，不当场过热）。
        await HeatPower.AddHeatAsync(choiceContext, creature, HeatGain, this);

        // 4. 掉血未致死且战斗继续 → 正常抽牌（可触发洗牌，非扫描）。
        if (creature.IsDead || !CombatManager.Instance.IsInProgress)
        {
            return;
        }

        await CardPileCmd.Draw(choiceContext, DynamicVars["DrawCount"].BaseValue, player);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["HpLoss"].UpgradeValueBy(-1m);   // 2 → 1
    }
}
