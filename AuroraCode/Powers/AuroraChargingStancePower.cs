using System.Collections.Generic;
using System.Threading.Tasks;
using AuroraMod.AuroraCode.Helpers;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

namespace AuroraMod.AuroraCode.Powers;

/// <summary>
/// 过载引擎 Power（卡面见 <see cref="Cards.Uncommon.AuroraChargingStance"/>）。
/// 本场战斗中，你每打出 1 张攻击牌就积 <see cref="HeatPerAttack"/> 点热量（每层 +1，多张叠加）。
///
/// <b>⚠ 类名/文件名/图标名保持 ChargingStance 不变，这是刻意的</b>：卡牌 ID 与本地化 key 都由类名派生
/// （<c>AURORAMOD-AURORA_CHARGING_STANCE</c>）。Aurora 已发布，改名会让所有进行中的存档找不到这张牌。
/// 内部整洁让位于存档兼容——本卡由旧「蓄能架式」原地重做而来（旧效果：本回合未打出攻击牌则给剑势，
/// 条件与角色主业对立，实战几乎不触发）。
///
/// 计次口径沿用 <see cref="ChainPower.AfterCardPlayed"/> 的既定守卫：只认玩家<b>手动打出</b>的牌
/// （<c>!IsAutoPlay</c>）且同一次打出的多段额外结算只计一次（<c>IsFirstInSeries</c>）。
/// 复制/回响/自动打出不重复积热——否则复制流会把过热推成不可控的雪崩。
/// </summary>
public sealed class AuroraChargingStancePower : AuroraPower
{
    /// <summary>每层每张攻击牌积的热量。</summary>
    public const int HeatPerAttack = 1;

    public override PowerInstanceType InstanceType => PowerInstanceType.None;
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    protected override bool IsVisibleInternal => true;
    protected override string IconName => "charging_stance";

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("HeatPerAttack", HeatPerAttack),
    ];

    /// <summary>叠加层数：每层使每张攻击牌多积 1 点热。</summary>
    public static async Task ApplyAsync(PlayerChoiceContext ctx, Creature creature, int stacks, CardModel source)
    {
        await AuroraPowerCmd.Apply<AuroraChargingStancePower>(ctx, creature, stacks, creature, source, silent: true);
    }

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Amount <= 0 || !CombatManager.Instance.IsInProgress)
        {
            return;
        }

        // 与连锁同一套守卫：必须是本人手动打出、且是这次打出的第一段结算。
        if (cardPlay?.Card?.Owner != Owner.Player || cardPlay.IsAutoPlay || !cardPlay.IsFirstInSeries)
        {
            return;
        }

        if (cardPlay.Card.Type != CardType.Attack)
        {
            return;
        }

        Flash();

        // 积热走统一入口：越线锁定待结算过热、换区通知、热量柱刷新全都自动接上。
        await HeatPower.AddHeatAsync(choiceContext, Owner, (int)Amount * HeatPerAttack, null);
    }
}
