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
/// 09 反应炉突刺 / Reactor Thrust（普通，A 过热暴走）。造成 15 伤害；若随后积 3 热会使你过热，先获得 8 格挡；随后积 3 热。
/// 升级：伤害 15→19，格挡 8→10，积热不变。
/// 结算（过热预测→攻击→条件格挡→积热）：打出前只读一次当前热量做纯预测 willOverheat=heat+3≥10（不预演、不触发副作用）；
/// 单段 powered 攻击；若预测过热则在 AddHeat(+3) 之前获得 8/10 格挡，故能抵挡紧接着的第一次过热伤害；
/// 之后照常积 3 热（达 10 走既有 HeatPower 过热流程）。格挡只护本牌伤害之后的事件，不追溯。
/// </summary>
public class AuroraReactorThrust() : AuroraCard(2, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
{
    protected override string ArtName => "reactor_thrust";

    protected override IEnumerable<AuroraMechanic> MechanicTips => [AuroraMechanic.Heat, AuroraMechanic.SystemCrash];

    private const int HeatGain = 3;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(15, ValueProp.Move),
        new BlockVar(8, ValueProp.Move),
        new PowerVar<HeatPower>(HeatGain),
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var creature = Owner?.Creature;

        // 1. 打出前只读一次当前热量做纯预测（不调积热 API 预演、不触发任何过热副作用）。
        var willOverheat = creature != null && HeatPower.GetHeat(creature) + HeatGain >= HeatPower.OverheatThreshold;

        // 2. 单段 powered 攻击。
        var damage = (int)DynamicVars.Damage.BaseValue;
        await CommonActions.CardAttack(this, cardPlay, cardPlay.Target, damage, ValueProp.Move).Execute(choiceContext);

        if (creature == null)
        {
            return;
        }

        // 3. 预测过热 → 在积热之前先获得格挡（可抵挡随之而来的第一次过热伤害）。
        if (willOverheat)
        {
            await CreatureCmd.GainBlock(creature, (int)DynamicVars.Block.BaseValue, ValueProp.Move, cardPlay);
        }

        // 4. 随后积 3 热（达 10 走既有过热流程）。
        await HeatPower.AddHeatAsync(choiceContext, creature, HeatGain, this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(4m);   // 15 → 19
        DynamicVars.Block.UpgradeValueBy(2m);    // 8 → 10
    }
}
