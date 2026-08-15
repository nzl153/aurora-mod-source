using BaseLib.Utils;
using AuroraMod.AuroraCode.Cards;
using AuroraMod.AuroraCode.Helpers;
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
/// 24 反制姿态 / Countermeasure（普通，B 挑战协议·剑势）。造成 8 伤害；若打出前目标有【由你施加】的挑战协议，获得 5 格挡与 2 剑势。
/// 升级：伤害 8→10，格挡 5→7，剑势不变。
/// 伤害 7→8 补普通位输出；兑现口追加剑势——协议正式并入 B，所有协议卡统一产剑势，
/// 让「主动扛伤 → 换剑势 → 一刀倾泻」在普通位就能成立（协议不再是无出口的第 5 套系统）。
/// 结算（打出前读协议归属快照 → 单段 powered 攻击 → 条件格挡 → 条件剑势）：只识别本牌所有者亲自施加的协议
/// （<see cref="AuroraChallengeProtocolService.GetStacks"/>(target, creature)&gt;0），队友协议不算；不消费/不减少/不转移协议。
/// 击杀目标仍按打出前快照给格挡与剑势（<b>但战斗已结束则不发</b>，见结算处 IsInProgress 守卫）。
/// 无协议时仍是 1 费 8/10 伤，不空牌。Echo 每次都造成伤害、条件收益至多一次。
/// </summary>
public class AuroraCountermeasure() : AuroraCard(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
{
    protected override string ArtName => "countermeasure";

    protected override IEnumerable<AuroraMechanic> MechanicTips =>
        [AuroraMechanic.ChallengeProtocol, AuroraMechanic.Momentum];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(8, ValueProp.Move),
        new BlockVar(5, ValueProp.Move),
        // 2→3，与本批剑势产出统一上调。
    new DynamicVar("MomentumGain", 3m),
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var creature = Owner?.Creature;
        if (creature == null)
        {
            return;
        }

        var target = cardPlay.Target;

        // 打出前读一次「目标身上由本人施加的协议」快照。
        var hasOwnProtocol = target != null
            && AuroraChallengeProtocolService.GetStacks(target, creature) > 0;
        var special = cardPlay.IsFirstInSeries && hasOwnProtocol;

        // 1. 单段 powered 攻击（不消费协议）。
        await CommonActions.CardAttack(this, cardPlay, target, (int)DynamicVars.Damage.BaseValue, ValueProp.Move).Execute(choiceContext);

        // 2. 打出前存在本人协议 → 获得格挡与剑势。
        // 【IsInProgress 守卫】多敌场合击杀单个目标、战斗仍在进行 → 照常发奖；
        // 若本段是收尾斩杀导致战斗结束，则不再 Apply/Modify 剑势 Power（本场已无意义，
        // 且与同 mod AuroraArrayExecution 的战后守卫惯例对齐）。
        if (special && CombatManager.Instance.IsInProgress)
        {
            await CreatureCmd.GainBlock(creature, (int)DynamicVars.Block.BaseValue, ValueProp.Move, cardPlay);
            await AuroraMomentumService.GainAsync(choiceContext, creature, (int)DynamicVars["MomentumGain"].BaseValue, this);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(2m);   // 8 → 10
        DynamicVars.Block.UpgradeValueBy(2m);    // 5 → 7
    }
}
