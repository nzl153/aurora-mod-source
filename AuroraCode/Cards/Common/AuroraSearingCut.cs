using BaseLib.Utils;
using AuroraMod.AuroraCode.Cards;
using AuroraMod.AuroraCode.Helpers;
using AuroraMod.AuroraCode.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.ValueProps;

namespace AuroraMod.AuroraCode.Cards.Common;

/// <summary>
/// 08 灼热切割 / Searing Cut（普通，A）。造成 7 伤；若目标将攻击，获得 4 格挡；随后积 2 热。升级 7/4→10/5，积热不变。
/// 顺序（§4.1）：结算开始只读一次目标 IntendsToAttack → 1 段 powered attack → 按快照获得格挡 → AddHeat(+2)。
/// 格挡在积热之前，故 9 热打出时新格挡可抵挡随之而来的过热伤害；目标被击杀仍按快照获得格挡并积热。
/// </summary>
public class AuroraSearingCut() : AuroraCard(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
{
    protected override string ArtName => "searing_cut";

    protected override IEnumerable<AuroraMechanic> MechanicTips => [AuroraMechanic.Heat];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(7, ValueProp.Move),
        new BlockVar(4, ValueProp.Move),
        new PowerVar<HeatPower>(2m),
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var creature = Owner?.Creature;

        // 1. 结算开始只读一次目标攻击意图（Attack / DeathBlow 视为「将攻击」；非怪物或非攻击意图为假）。
        var willAttack = cardPlay.Target?.Monster?.IntendsToAttack ?? false;

        // 2. 单段 powered attack（力量/易伤/过载/锁定各只结算一次）。
        var damage = (int)DynamicVars.Damage.BaseValue;
        await CommonActions.CardAttack(this, cardPlay, cardPlay.Target, damage, ValueProp.Move).Execute(choiceContext);

        // 3. 按打出前快照获得条件格挡（目标即使被击杀也照给）。
        if (willAttack && creature != null)
        {
            await CreatureCmd.GainBlock(creature, (int)DynamicVars.Block.BaseValue, ValueProp.Move, cardPlay);
        }

        // 4. 随后积 2 热（在格挡之后，故 9 热打出时新格挡可抵挡过热伤害）。
        if (creature != null)
        {
            await HeatPower.AddHeatAsync(choiceContext, creature, 2, this);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(3m);   // 7 → 10
        DynamicVars.Block.UpgradeValueBy(1m);    // 4 → 5
    }
}
