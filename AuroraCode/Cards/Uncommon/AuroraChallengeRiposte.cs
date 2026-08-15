using BaseLib.Utils;
using AuroraMod.AuroraCode.Cards;
using AuroraMod.AuroraCode.DynamicVars;
using AuroraMod.AuroraCode.Helpers;
using AuroraMod.AuroraCode.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.ValueProps;

namespace AuroraMod.AuroraCode.Cards.Uncommon;

/// <summary>
/// 34 挑战反斩 / Challenge Riposte（罕见，B 协议兑现）。移除你施加给目标的全部挑战协议；造成 8 伤害，
/// 每实际移除 1 层额外造成 6 伤害；然后每实际移除 1 层最多散去 1 热。升级：基础伤害 8→11，每层额外伤害 6→7。
/// 从「偏防御的格挡兑现」改为<b>进攻兑现</b>——挑战协议本是「主动让敌人更能打你」的风险，
/// 兑现口理应是伤害而非再给盾（防御兑现由另一张协议牌承担）。给罕见位一张前期就能用的小终结技：满 3 层 26/32 伤。
/// 结算（先移除取实际层数 → 合并单段 powered 攻击 → 按实际层数散热）：
/// <b>必须先移除再打</b>——伤害要按实际移除层数计算，故 <see cref="AuroraChallengeProtocolService.ConsumeAllAsync"/> 在攻击之前；
/// 只读一次层数快照（服务返回的实际移除量即真值），只识别本牌所有者亲自施加的协议，队友/其他奥萝拉的层数不可消费。
/// 所有条件加值<b>并入同一段</b> powered 伤害（整段统一吃一次力量/易伤/过载×1.25/宕机惩罚；锁定 +2 与消费由伤害中心处理，不在此手写），
/// 绝不拆成多段。散热量 = 实际移除层数（协议上限 3 → 最多散 3），走既有 VentUpTo 服务；0 热时自然返 0。
/// 无本人协议时仍造成 8/11 基础伤害、不散热，不空牌。不产能量、不抽牌。人工制品/按施加者分层/目标死亡等边界继续走既有服务。
/// </summary>
public class AuroraChallengeRiposte() : AuroraCard(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
{
    protected override string ArtName => "challenge_riposte";

    protected override IEnumerable<AuroraMechanic> MechanicTips =>
        [AuroraMechanic.ChallengeProtocol, AuroraMechanic.Heat];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        // 卡面直接显示「按目标身上本人协议层数算完」的真实伤害（同其余条件伤害卡）。
        // 用带 target 的重载——本牌加值取决于<b>目标</b>身上的协议，而非自身状态；
        // target 可能为 null（未指向敌人时的预览），GetStacks 对空实例返回 0，天然安全。
        new AuroraScalingDamageVar(8, ValueProp.Move, (c, t) =>
            AuroraChallengeProtocolService.GetStacks(t, c.Owner?.Creature)
            * (int)c.DynamicVars["DamagePerStack"].BaseValue),
        new DynamicVar("DamagePerStack", 6m),
        new DynamicVar("VentPerStack", 1m),
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var creature = Owner?.Creature;
        var target = cardPlay.Target;

        // 1. 先移除本人对该目标的全部协议，只读一次实际移除量（服务返回值即真值；只认本牌所有者亲自施加的层数）。
        var consumed = creature != null && target != null
            ? await AuroraChallengeProtocolService.ConsumeAllAsync(choiceContext, target, creature, this)
            : 0;

        // 2. 条件加值并入同一段 powered 攻击（绝不拆段：整段统一吃一次力量/易伤/过载×1.25/宕机；锁定 +2 与消费由伤害中心处理）。
        var damage = (int)DynamicVars.Damage.BaseValue
                     + consumed * (int)DynamicVars["DamagePerStack"].BaseValue;
        await CommonActions.CardAttack(this, cardPlay, target, damage, ValueProp.Move).Execute(choiceContext);

        // 3. 按实际移除层数散热（协议上限 3 → 最多散 3）；无协议则不散热。
        if (creature == null || consumed <= 0)
        {
            return;
        }

        var ventMax = consumed * (int)DynamicVars["VentPerStack"].BaseValue;
        await HeatPower.VentUpToAsync(choiceContext, creature, ventMax, this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(3m);                 // 8 → 11
        DynamicVars["DamagePerStack"].UpgradeValueBy(1m);      // 6 → 7
    }
}
