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
/// 27 熔毁斩 / Meltdown Slash（罕见，A）。造成 9 伤；若打出前在过载区，改为造成 2 次，随后引爆。升级每段 9→11。
/// 顺序（§4.5）：开始只读一次区段 → 冷/温区 1 段 8/10；过载/临界区 2 段独立 8/10 后调标准 IgniteAsync。
/// 两段各自吃力量/易伤/过载×1.25/宕机/向下取整/消费本人锁定（中心补丁各段 +2），绝不合并为 16/20 单段。
/// 引爆走标准过热流程（过热计数+1→监听→10/12/14/16 可格挡自伤→清零→下回合宕机）；不因首段击杀而免除引爆。
/// 1 费罕见位数值上调（对照原版 680 张卡解包统计：奥萝拉 1 费罕见攻击均值 7.2 / 中位 7，
/// 原版 9.4 / 8；格挡 6.1 / 6 对原版 8.4 / 7——该档是全卡池唯一明显洼地，而罕见奖励是玩家整局看得最多的三选一）。
/// </summary>
public class AuroraMeltdownSlash() : AuroraCard(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
{
    protected override string ArtName => "meltdown_slash";

    protected override IEnumerable<AuroraMechanic> MechanicTips => [AuroraMechanic.Heat];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(9, ValueProp.Move),
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var creature = Owner?.Creature;

        // 开始结算只读一次区段；异常临界区按过载分支兜底。冷/温区单段，不引爆、不调热。
        var zone = creature != null ? HeatPower.GetZone(creature) : HeatPower.HeatZone.Cold;
        var overloaded = zone is HeatPower.HeatZone.Overload or HeatPower.HeatZone.Critical;
        var damage = (int)DynamicVars.Damage.BaseValue;

        // 第一段（两分支都打）。
        await CommonActions.CardAttack(this, cardPlay, cardPlay.Target, damage, ValueProp.Move).Execute(choiceContext);

        if (!overloaded)
        {
            return;
        }

        // 过载/临界：独立第二段（同目标；首段击杀不转移目标、不免除引爆）。
        await CommonActions.CardAttack(this, cardPlay, cardPlay.Target, damage, ValueProp.Move).Execute(choiceContext);

        // 两段后引爆。战斗已被引擎正式终止时不在结算外强写过热状态。
        if (creature != null && CombatManager.Instance?.IsInProgress == true)
        {
            await HeatPower.IgniteAsync(choiceContext, creature, this);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(2m);   // 9 → 11
    }
}
