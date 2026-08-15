using BaseLib.Utils;
using AuroraMod.AuroraCode.Cards;
using AuroraMod.AuroraCode.Helpers;
using AuroraMod.AuroraCode.Powers;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.ValueProps;

namespace AuroraMod.AuroraCode.Cards.Uncommon;

/// <summary>
/// 32 烧蚀装甲 / Ablative Armor（罕见，A 过热暴走）。失去 2 生命,获得 11 格挡,随后积 2 热。升级:格挡 11→14。
/// 格挡 13/16→11/14——压低自足防御地基(失血/积热/顺序均不变)。
/// 结算固定顺序(失血 → 格挡 → 积热/过热):失血走 Unblockable|Unpowered(无视格挡、不吃力量/易伤、来源=本牌可被减免识别);
/// 死亡/战斗结束则停;格挡在积热之前 → 本回合的新盾仍在、可挡到回合末统一结算的过热伤害;积热走延迟过热(越10只登记待结算)。
/// 即使失血被原生减免,只要存活仍获得完整后续。Echo 每次完整重复失血/格挡/积热,不加 IsFirst 守卫。
/// </summary>
public class AuroraAblativeArmor() : AuroraCard(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    protected override string ArtName => "ablative_armor";

    protected override IEnumerable<AuroraMechanic> MechanicTips => [AuroraMechanic.Heat, AuroraMechanic.SystemCrash];

    private const int HeatGain = 2;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new HpLossVar("HpLoss", 2),
        new BlockVar(11, ValueProp.Move),
        new PowerVar<HeatPower>(HeatGain),
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var creature = Owner?.Creature;
        if (creature == null)
        {
            return;
        }

        // 1. 失去生命(无视格挡、不吃力量/易伤;走自损服务:套自损作用域供灰烬拦截、记录实际掉血供极限断裂累计)。
        var hpLoss = (int)DynamicVars["HpLoss"].BaseValue;
        if (hpLoss > 0)
        {
            await AuroraSelfHarm.ApplyAsync(choiceContext, creature, hpLoss,
                ValueProp.Unblockable | ValueProp.Unpowered, this);
        }

        if (creature.IsDead || !CombatManager.Instance.IsInProgress)
        {
            return;
        }

        // 2. 获得格挡(本回合的盾仍在、可挡到回合末统一结算的过热伤害)。
        await CreatureCmd.GainBlock(creature, (int)DynamicVars.Block.BaseValue, ValueProp.Move, cardPlay);

        // 3. 积热(延迟过热:越10只登记待结算,回合末统一结算,不当场过热)。
        await HeatPower.AddHeatAsync(choiceContext, creature, HeatGain, this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Block.UpgradeValueBy(3m);   // 11 → 14
    }
}
