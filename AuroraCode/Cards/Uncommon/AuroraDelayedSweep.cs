using BaseLib.Utils;
using AuroraMod.AuroraCode.Cards;
using AuroraMod.AuroraCode.DynamicVars;
using AuroraMod.AuroraCode.Helpers;
using AuroraMod.AuroraCode.Powers;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.ValueProps;

namespace AuroraMod.AuroraCode.Cards.Uncommon;

/// <summary>
/// B4 延迟横斩 / Delayed Sweep（罕见，B 剑势；群体）。对所有敌人造成 7 伤害；每 2 点剑势额外 +1，最多 +6。<b>不清空剑势</b>。
/// 升级：基础 7→9，加成上限 +6→+8。
///
/// 本卡原本读「上回合是否打出攻击」，<b>挂着 B 的名分却与剑势毫无关系</b>（旧注释明写不挂 Momentum 提示），
/// 且「主动空一回合不打人」的判据非常别扭。改为 B 唯一的<b>罕见小终端</b>——补上流派曲线里缺失的中段变现口
/// （对照 A 的裂芯斩、C 的编队突击，B 此前罕见位零输出，只能一路攒到抽中稀有兑现口，抽不到则整局白攒）。
///
/// 【为什么不清空】清空是稀有位一刀两断的职责；罕见位只负责「攒的势随时能变现一点」，
/// 与 <see cref="Rare.AuroraMoonlessBlade"/>（同为不清空、单体、读势上限 20）分工：本卡群体、上限更低、费用更省。
/// 群体故每点剑势的收益压到 1/2（无月为 1 势×2 单体）。
///
/// 结算（打出前读一次剑势快照 → 合并为单段 powered AoE）：伤害 = 基础 + min(剑势 / 2, 上限)，
/// 整段统一吃力量/易伤/过载×1.25/取整/锁定+2，绝不拆段；AllEnemies 由 CardAttack 自动分发。
/// 不清空、不消耗、不调热。Echo 额外结算按<b>当时</b>剑势重新计算（剑势未被本卡改变，故通常与首段同值）。
/// 注意：剑势被动底薪（<see cref="MomentumPower.ModifyDamageAdditive"/>）会在伤害中心另行叠加，
/// 本卡<b>不在卡内手写底薪</b>，避免同一资源双重计算。
/// </summary>
public class AuroraDelayedSweep() : AuroraCard(1, CardType.Attack, CardRarity.Uncommon, TargetType.AllEnemies)
{
    protected override string ArtName => "delayed_sweep";

    public override AuroraStrikeVfxKind StrikeVfx => AuroraStrikeVfxKind.Flurry;   // 群体横扫：紫刃齐射

    protected override IEnumerable<AuroraMechanic> MechanicTips => [AuroraMechanic.Momentum];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new AuroraScalingDamageVar(7, ValueProp.Move, c =>
            Math.Min(MomentumPower.Get(c.Owner?.Creature) / (int)c.DynamicVars["MomentumPerDamage"].BaseValue,
                     (int)c.DynamicVars["MomentumDamageCap"].BaseValue)),
        new DynamicVar("MomentumPerDamage", 2m),
        new DynamicVar("MomentumDamageCap", 6m),
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var creature = Owner?.Creature;
        if (creature == null)
        {
            return;
        }

        // 打出前读一次剑势快照；不清空、不消耗。
        var momentum = MomentumPower.Get(creature);
        var bonus = Math.Min(momentum / (int)DynamicVars["MomentumPerDamage"].BaseValue,
                             (int)DynamicVars["MomentumDamageCap"].BaseValue);

        // 合并单段 powered AoE（整段统一吃力量/易伤/过载×1.25/取整/锁定+2）；底薪由伤害中心另行叠加。
        var dmg = (int)DynamicVars.Damage.BaseValue + bonus;
        await CommonActions.CardAttack(this, cardPlay, cardPlay.Target, dmg, ValueProp.Move).Execute(choiceContext);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(2m);                    // 7 → 9
        DynamicVars["MomentumDamageCap"].UpgradeValueBy(2m);      // +6 → +8
    }
}
