using System.Linq;
using System.Threading.Tasks;
using AuroraMod.AuroraCode.Helpers;
using AuroraMod.AuroraCode.Visuals;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.ValueProps;

namespace AuroraMod.AuroraCode.Powers;

/// <summary>
/// 攻击模块 / Attack Module（C 悬浮部件流派）—— 已部署的悬浮肩甲，架构 §6.2。
/// 回合结束时对一名敌人造成 <see cref="AuroraModulePower.Value"/> 点伤害（部署 4，每点强化 +1）。
/// 模块伤害走 Unpowered：不是打出攻击牌、不吃过载增伤、不推进连锁、不破坏「不攻击」条件（架构 §6.2 末条）。
/// 多实例（Instanced）：两枚攻击模块各自独立结算，不并成一个总伤害。
/// </summary>
public sealed class AttackModulePower : AuroraModulePower
{
    public const int BaseDamage = 4;

    public override ModuleKind Kind => ModuleKind.Attack;
    public override int BaseValue => BaseDamage;
    protected override string IconName => "attack_module";

    // 被动：己方回合结束时触发（每个实例各触发一次）。
    public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, System.Collections.Generic.IEnumerable<Creature> participants)
    {
        if (side != Owner.Side || Value <= 0)
        {
            return;
        }

        // 只在自己确实参与本次回合结算时触发。
        if (participants?.Contains(Owner) != true)
        {
            return;
        }

        await TriggerAsync(choiceContext);
    }

    public override async Task TriggerAsync(PlayerChoiceContext choiceContext)
    {
        if (Value <= 0)
        {
            return;
        }

        var enemies = Owner.CombatState?.HittableEnemies;
        if (enemies is not { Count: > 0 })
        {
            return;
        }

        // 架构 §9.1：优先攻击「模块拥有者本人锁定层数最高」的存活敌人；
        // 唯一最高不耗 RNG，并列最高一次 CombatTargets RNG，无本人锁定则退回全体一次 RNG。
        var target = Helpers.AuroraLockService.SelectAttackModuleTarget(Owner, enemies);
        if (target == null)
        {
            return;
        }

        Flash();
        AuroraModuleVisualBridge.RequestTrigger(Owner, this, target);
        AuroraAudio.PlaySfx("module_laser.wav");   // 攻击模块激光开火音效（纯表现）。
        // 攻击模块是锁定消费的唯一 Unpowered 例外：用作用域标记这段伤害可消费锁定并吃 +2（§9 / 架构 §9.1）。
        using (Patches.AuroraLockDamagePatch.ModuleDamageScope.Enter())
        {
            await CreatureCmd.Damage(choiceContext, target, Value, ValueProp.Unpowered, Owner);
        }
    }
}
