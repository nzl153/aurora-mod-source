using BaseLib.Utils;
using AuroraMod.AuroraCode.Cards;
using AuroraMod.AuroraCode.Helpers;
using AuroraMod.AuroraCode.Powers;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.ValueProps;

namespace AuroraMod.AuroraCode.Cards.Uncommon;

/// <summary>
/// 37 相位切割 / Phase Cutter（罕见，枢纽）。造成 9 伤害；按打出前区段：冷区给 1 虚弱，温区施 2 锁定，过载区散 1 热。升级 9→12。
/// 三区段适配接口而非按热量加伤害。结算：读打出前 zoneSnapshot → 单段 powered 攻击 → 仅 IsFirstInSeries 走区段副效果
/// （冷=虚弱、温=锁定经 AuroraLockService、过载/红线=VentUpTo(1) 不取消 Pending）。Echo 只重复伤害；目标死亡不改施他人。
/// 1 费罕见位数值上调（对照原版 680 张卡解包统计：奥萝拉 1 费罕见攻击均值 7.2 / 中位 7，
/// 原版 9.4 / 8；格挡 6.1 / 6 对原版 8.4 / 7——该档是全卡池唯一明显洼地，而罕见奖励是玩家整局看得最多的三选一）。
/// </summary>
public class AuroraPhaseCutter() : AuroraCard(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
{
    protected override string ArtName => "phase_cutter";

    protected override IEnumerable<AuroraMechanic> MechanicTips =>
        [AuroraMechanic.Heat, AuroraMechanic.Lock, AuroraMechanic.ZoneChange];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [HoverTipFactory.FromPower<WeakPower>()];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(9, ValueProp.Move),
        new PowerVar<WeakPower>(1m),
        new DynamicVar("LockStacks", 2m),
        new DynamicVar("VentMax", 1m),
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var creature = Owner?.Creature;
        if (creature == null)
        {
            return;
        }

        var isFirst = cardPlay.IsFirstInSeries;
        var zone = HeatPower.GetZone(creature);   // 打出前快照

        var damage = (int)DynamicVars.Damage.BaseValue;
        await CommonActions.CardAttack(this, cardPlay, cardPlay.Target, damage, ValueProp.Move).Execute(choiceContext);

        if (!isFirst || !CombatManager.Instance.IsInProgress)
        {
            return;   // Echo 只重复伤害
        }

        var target = cardPlay.Target;
        switch (zone)
        {
            case HeatPower.HeatZone.Cold:
                if (target is { IsAlive: true })
                {
                    await AuroraPowerCmd.Apply<WeakPower>(choiceContext, target, DynamicVars["WeakPower"].BaseValue, creature, this);
                }

                break;
            case HeatPower.HeatZone.Warm:
                if (target is { IsAlive: true })
                {
                    await AuroraLockService.ApplyAsync(choiceContext, target, creature, (int)DynamicVars["LockStacks"].BaseValue, this);
                }

                break;
            default:   // Overload（含 10+ 红线）
                await HeatPower.VentUpToAsync(choiceContext, creature, (int)DynamicVars["VentMax"].BaseValue, this);
                break;
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(3m);   // 9 → 12
    }
}
