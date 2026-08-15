using BaseLib.Utils;
using AuroraMod.AuroraCode.Cards;
using AuroraMod.AuroraCode.Helpers;
using AuroraMod.AuroraCode.Powers;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace AuroraMod.AuroraCode.Cards.Rare;

/// <summary>
/// C-R03 自律兵装 / Autonomous Arsenal（稀有，C 悬浮模块；能力）。回合结束时若模块槽已满，使所有模块额外触发 1 次并积 1 热。升级费用 2→1。
/// 结算：经 <see cref="AuroraAutonomousArsenalPower"/>（Amount=额外轮数，多张各加 1 轮 + 1 热）。满槽判定读本人有效容量——辅助肩架扩槽后须填满第 3 槽。
/// 回合末积热若越 10，纳入同一回合末统一过热结算（延迟过热）。C 的满槽持续引擎，持续积热=真实维护成本。
/// </summary>
public class AuroraAutonomousArsenal() : AuroraCard(2, CardType.Power, CardRarity.Rare, TargetType.Self)
{
    protected override string ArtName => "autonomous_arsenal";

    protected override IEnumerable<AuroraMechanic> MechanicTips =>
        [AuroraMechanic.AttackModule, AuroraMechanic.ShieldModule, AuroraMechanic.Heat];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var creature = Owner?.Creature;
        if (creature == null)
        {
            return;
        }

        // 每张贡献 1 层（=1 额外触发轮 + 1 积热），多张累加。
        await AuroraPowerCmd.Apply<AuroraAutonomousArsenalPower>(choiceContext, creature, 1, creature, this);
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);   // 2 → 1
    }
}
