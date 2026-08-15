using BaseLib.Utils;
using AuroraMod.AuroraCode.Cards;
using AuroraMod.AuroraCode.Helpers;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.ValueProps;

namespace AuroraMod.AuroraCode.Cards.Common;

/// <summary>
/// 17 目标涂装 / Target Painter（普通，C）。造成 7 伤，随后施加 2 层锁定。升级伤害 7→10，锁定不变。
/// 顺序（§4.3）：1 段 powered attack → 伤害后经 <see cref="AuroraLockService.ApplyAsync"/> 施加 2 层本人锁定（单敌本人上限 6）。
/// 伤害在前，故本段不能消费刚施加的锁定；已有本人锁定仍被本段正常消费 1 层。人工制品照常阻挡，被挡不补偿；
/// 目标被击杀不向尸体/他敌/本地替代目标施加。不在卡类手写锁定 +2 伤害。
/// </summary>
public class AuroraTargetPainter() : AuroraCard(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
{
    protected override string ArtName => "target_painter";

    protected override IEnumerable<AuroraMechanic> MechanicTips => [AuroraMechanic.Lock];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(8, ValueProp.Move),
        new DynamicVar("LockStacks", 2m),
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var creature = Owner?.Creature;

        // 1. 单段 powered attack（本段消费已有本人锁定，不消费即将施加的新锁定）。
        var damage = (int)DynamicVars.Damage.BaseValue;
        await CommonActions.CardAttack(this, cardPlay, cardPlay.Target, damage, ValueProp.Move).Execute(choiceContext);

        // 2. 伤害后施加 2 层本人锁定（目标仍是合法存活对象时；死亡则不施加）。
        var target = cardPlay.Target;
        if (creature != null && target != null && target.IsAlive)
        {
            var stacks = (int)DynamicVars["LockStacks"].BaseValue;
            await AuroraLockService.ApplyAsync(choiceContext, target, creature, stacks, this);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(3m);   // 7 → 10
    }
}
