using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace AuroraMod.AuroraCode.Powers;

/// <summary>
/// 本回合攻击追踪器—— 隐藏 Power：记录本人「<b>本回合</b>是否打出过攻击牌」。
/// 供 #12 守势蓄锐、充能架势、起始遗物读取。由起始遗物 BeforeCombatStart 挂载（同 ChainPower），回合开始清零。
/// 与 ChainPower 不同：<b>自动打出的攻击也算</b>（不排除 IsAutoPlay），仅用 IsFirstInSeries 防同一次手动打出的多段重复置位。
/// 本引擎无 BeforeCardPlayed 钩子，用 AfterCardPlayed 记录（攻击结算完即置位，随后技能牌读取正确）。只记录本人。
/// <b>权威全进 <see cref="AuroraPower.Amount"/> 位编码</b>（bit0=本回合已打攻击）——存 DynamicVar 重连即丢，
/// 改位编码后联机/重连一致。回合开始清 bit0。
/// 删除「上回合」位与 <c>HasPlayedAttackLastTurn</c>——唯一使用方延迟横斩已改为读剑势，
/// 保留无调用方的 API 只会让后续误以为该语义仍受支持。
/// </summary>
public sealed class AuroraAttackTurnTracker : AuroraPower
{
    public override PowerInstanceType InstanceType => PowerInstanceType.None;
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    protected override bool IsVisibleInternal => false;

    private const int BitThisTurn = 1 << 0;

    private bool PlayedThisTurn => ((int)Amount & BitThisTurn) != 0;

    public static bool HasPlayedAttackThisTurn(Creature creature) =>
        ((int)(creature?.GetPowerAmount<AuroraAttackTurnTracker>() ?? 0) & BitThisTurn) != 0;

    /// <summary>回合开始 / 战斗开始挂载后初始化为「本回合未打出攻击」。</summary>
    public void ResetFlag()
    {
        AssertMutable();
        SetAmount(0);
    }

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay?.Card?.Owner != Owner.Player
            || cardPlay.Card.Type != CardType.Attack
            || !cardPlay.IsFirstInSeries
            || PlayedThisTurn)
        {
            return;
        }

        AssertMutable();
        SetAmount((int)Amount | BitThisTurn);
        await Task.CompletedTask;
    }

    public override Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player == Owner.Player)
        {
            // 本人回合开始时清「本回合已打攻击」标志。权威全在 Amount。
            AssertMutable();
            SetAmount(0);
        }

        return Task.CompletedTask;
    }
}
