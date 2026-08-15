using BaseLib.Utils;
using AuroraMod.AuroraCode.Cards;
using AuroraMod.AuroraCode.DynamicVars;
using AuroraMod.AuroraCode.Helpers;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.ValueProps;

namespace AuroraMod.AuroraCode.Cards.Uncommon;

/// <summary>
/// C-U?? 装甲冲撞 / Armor Slam（罕见，C 悬浮模块泄压口）。造成等同于当前格挡的伤害。升级：费用 1→0。
///
/// 【为什么加这张】模块流是全卡池唯一完全脱离增伤体系的路——模块伤害走 Unpowered，
/// 不吃过载 ×1.25、不吃剑势底薪（手册 §2C 的防三轴指数闸门，不可动）。副作用是护盾模块产出没有天花板、
/// 攻击模块产出被锁死在低位，玩家自然滑向纯防御，最后卡在「死不了也打不死」。
/// 本卡是这条流派的<b>泄压口</b>：把模块攒出来的格挡转成一次<b>正常管线的 powered 攻击</b>，
/// 于是模块流第一次接上了热量与剑势主轴——而闸门本身一点没动（模块伤害仍是 Unpowered）。
///
/// 结算：读当前格挡 → 单段 powered 攻击。<b>不消耗格挡</b>（与原版全身撞击一致），
/// 故不触碰手册 §6 那四个影响己方输出的钩子，无结算顺序陷阱。
/// 卡面预览走 <see cref="AuroraScalingDamageVar"/>（第 7 张接预览的卡），0 格挡时如实显示 0。
/// </summary>
public class AuroraArmorSlam() : AuroraCard(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
{
    protected override string ArtName => "armor_slam";

    protected override IEnumerable<AuroraMechanic> MechanicTips => [AuroraMechanic.ShieldModule];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        // 基础 0 + 当前格挡。canonical（图鉴/主菜单模板卡）下 Owner 会抛异常，
        // AuroraScalingDamageVar 内部已按手册 §6 做了 CombatState==null 判空 + try/catch 兜底。
        new AuroraScalingDamageVar(0, ValueProp.Move, c => c.Owner?.Creature?.Block ?? 0),
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var creature = Owner?.Creature;
        if (creature == null)
        {
            return;
        }

        // 读当前格挡即为伤害。不消耗格挡：撞完护板还在。
        var damage = (int)DynamicVars.Damage.BaseValue + creature.Block;
        await CommonActions.CardAttack(this, cardPlay, cardPlay.Target, damage, ValueProp.Move).Execute(choiceContext);
    }

    protected override void OnUpgrade()
    {
        // 必须用 EnergyCost.UpgradeBy，不是 UpgradeStarCostBy——星费是另一套，本卡没有。
        EnergyCost.UpgradeBy(-1);   // 1 → 0
    }
}
