using BaseLib.Utils;
using AuroraMod.AuroraCode.Cards;
using AuroraMod.AuroraCode.Helpers;
using AuroraMod.AuroraCode.Powers;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace AuroraMod.AuroraCode.Cards.Rare;

/// <summary>
/// 73 抛射核心 / Eject Core（稀有，枢纽；双轴 A+C；<b>消耗</b>）。散尽全部热量；强化值最低的一枚模块获得强化，强化量 = min(实际散热量, 8)。
/// 升级：强化后使所有模块触发 1 次。
/// 顺序：vented = VentAsync → 若 vented&gt;0 且有模块，<b>只</b> EnhanceOneAsync(min(vented,8))（实际散热量仍完整返回散热监听，只封顶转化为强化的部分）→ 升级版 TriggerAsync。
/// 散尽不触发过热/不加过热次数/不生成宕机。无模块仍正常散热；升级版即使本次散热为 0，只要有模块仍触发一次。
/// </summary>
public class AuroraEjectCore() : AuroraCard(1, CardType.Skill, CardRarity.Rare, TargetType.Self)
{
    // 转化为模块强化的量封顶 8（实际散热量仍完整返回给散热监听，不封顶）。
    private const int MaxEnhance = 8;

    protected override string ArtName => "eject_core";

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];   // 加消耗

    protected override IEnumerable<AuroraMechanic> MechanicTips =>
        [AuroraMechanic.Heat, AuroraMechanic.AttackModule, AuroraMechanic.ShieldModule, AuroraMechanic.ModuleEnhancement];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var creature = Owner?.Creature;
        if (creature == null)
        {
            return;
        }

        // 1) 散尽全部热量（不触发过热），拿到实际散去量。
        var vented = await HeatPower.VentAsync(choiceContext, creature, this);

        // 2) 只强化一枚（最低值、并列取最早部署者，不耗 RNG）；强化量 = min(实际散热量, 8)。
        if (vented > 0 && AuroraModuleController.Count(creature) > 0)
        {
            await AuroraModuleController.EnhanceOneAsync(choiceContext, creature, System.Math.Min(vented, MaxEnhance), null, this);
        }

        // 3) 升级版：强化后使所有模块触发 1 次（用强化后的值）；无模块时为 no-op；0 热也照触发。
        if (IsUpgraded)
        {
            await AuroraModuleController.TriggerAsync(choiceContext, creature, null);
        }
    }
}
