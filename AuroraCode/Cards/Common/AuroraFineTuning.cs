using BaseLib.Utils;
using AuroraMod.AuroraCode.Cards;
using AuroraMod.AuroraCode.Helpers;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

namespace AuroraMod.AuroraCode.Cards.Common;

/// <summary>
/// 25 微调 / Fine Tuning（普通，枢纽）。0 费技能：二选一——积 1 热或散 1 热；<b>随后扫描 1</b>。升级：扫描 1→2。
/// 删除「只有跨区才扫描」——调热与扫描始终依次发生。0 费但不抽牌，不产生直接循环。
/// 结算：走 <see cref="AuroraHeatChoiceHelper.ChooseAdjustAsync"/> 二选一并执行热量变动（造成的换区仍正常触发相变护层等监听）→ 始终 <see cref="AuroraScanHelper.ScanAsync"/> 扫描 ScanCount。
/// </summary>
public class AuroraFineTuning() : AuroraCard(0, CardType.Skill, CardRarity.Common, TargetType.Self)
{
    protected override string ArtName => "fine_tuning";

    protected override IEnumerable<AuroraMechanic> MechanicTips =>
        [AuroraMechanic.Heat, AuroraMechanic.ZoneChange, AuroraMechanic.Scan];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("ScanCount", 1m),
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var creature = Owner?.Creature;
        var player = Owner?.Creature?.Player;
        if (creature == null || player == null)
        {
            return;
        }

        // 二选一积/散 1 热（换区照常触发监听）；随后始终扫描 ScanCount（不再以是否跨区为条件）。
        await AuroraHeatChoiceHelper.ChooseAdjustAsync(choiceContext, creature, this);
        await AuroraScanHelper.ScanAsync(choiceContext, player, (int)DynamicVars["ScanCount"].BaseValue, this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["ScanCount"].UpgradeValueBy(1m);   // 1 → 2
    }
}
