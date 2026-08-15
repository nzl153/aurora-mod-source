using System.Collections.Generic;
using System.Threading.Tasks;
using AuroraMod.AuroraCode.Cards;
using AuroraMod.AuroraCode.Helpers;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace AuroraMod.AuroraCode.Cards.Token;

/// <summary>
/// 宕机 / System Crash —— 过热惩罚状态卡。无法打出；回合结束消耗（Ethereal，游戏原生 DoTurnEnd 处理），只惩罚过热后一个回合。
/// 攻击 -25% 惩罚由 <see cref="AuroraMod.AuroraCode.Powers.AuroraSystemCrashPenaltyPower"/> 动态查手牌实现（不叠加，对模块伤害无效）。
/// </summary>
public class AuroraSystemCrash() : AuroraCard(0, CardType.Status, CardRarity.Token, TargetType.Self)
{
    protected override string ArtName => "system_crash";

    protected override IEnumerable<AuroraMechanic> MechanicTips => [AuroraMechanic.SystemCrash];

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Ethereal];

    public override bool CanBeGeneratedInCombat => false;

    public override bool CanBeGeneratedByModifiers => false;

    protected override bool IsPlayable => false;

    protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay) => Task.CompletedTask;
}
