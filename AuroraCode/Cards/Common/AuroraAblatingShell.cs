using BaseLib.Utils;
using AuroraMod.AuroraCode.Cards;
using AuroraMod.AuroraCode.Helpers;
using AuroraMod.AuroraCode.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.ValueProps;

namespace AuroraMod.AuroraCode.Cards.Common;

/// <summary>
/// 10 烧蚀护壳 / Ablating Shell（普通，A 过热暴走）。获得 7 格挡；若打出前在冷区或温区，改为获得 10 格挡并随后积 2 热。
/// 升级：普通 7→9，充能 10→13，积热不变。
/// 普通 8/10→7/9、充能 12/15→10/13——压低自足防御地基（自伤/积热/条件判定不变）。
/// 结算（区段快照→格挡→积热）：只读一次打出前区段；按区段决定最终格挡数，只执行一次 GainBlock（不做 8+4 两段）；
/// 冷/温区 +2 热（冷/温最高 6，+2 最多到 8，本牌绝不直接过热），过载/临界只保留基础格挡不调热。
/// </summary>
public class AuroraAblatingShell() : AuroraCard(1, CardType.Skill, CardRarity.Common, TargetType.Self)
{
    protected override string ArtName => "ablating_shell";

    protected override IEnumerable<AuroraMechanic> MechanicTips => [AuroraMechanic.Heat];

    private const int HeatGain = 2;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new BlockVar(7, ValueProp.Move),
        new DynamicVar("ChargedBlock", 10m),
        new PowerVar<HeatPower>(HeatGain),
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var creature = Owner?.Creature;
        if (creature == null)
        {
            return;
        }

        // 1. 打出前只读一次区段。
        var zone = HeatPower.GetZone(creature);
        var charged = zone is HeatPower.HeatZone.Cold or HeatPower.HeatZone.Warm;

        // 2. 按区段选定最终格挡数，只调用一次 GainBlock。
        var block = charged
            ? (int)DynamicVars["ChargedBlock"].BaseValue
            : (int)DynamicVars.Block.BaseValue;
        await CreatureCmd.GainBlock(creature, block, ValueProp.Move, cardPlay);

        // 3. 仅冷/温区随后积热（不追溯改本次格挡；过载/临界不调热）。
        if (charged)
        {
            await HeatPower.AddHeatAsync(choiceContext, creature, HeatGain, this);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Block.UpgradeValueBy(2m);               // 7 → 9
        DynamicVars["ChargedBlock"].UpgradeValueBy(3m);     // 10 → 13
    }
}
