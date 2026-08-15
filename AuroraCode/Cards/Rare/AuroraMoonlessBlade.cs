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

namespace AuroraMod.AuroraCode.Cards.Rare;

/// <summary>
/// B-R02 无月 / Moonless Blade（稀有，B 剑势；保留）。造成 10 + 每 1 势×2 伤害，最多计 20 势；不清空剑势。升级每势 2→3。
///
/// 读取上限 10→20——原上限使本卡在 12 势之后<b>再也不涨</b>（封顶 56），
/// 而一刀两断无上限（20 势 141 / 40 势 266），导致注释里声称的「与一刀两断成真实抉择」<b>实际不成立</b>：
/// 任何真正的剑势卡组里无月都是纯下位选择。提到 20 后（封顶 87）本卡在中高势段重新有竞争力，
/// 且因<b>保留剑势 + Retain 可重复</b>，两回合累计 174 &gt; 一刀两断一次性 141——
/// 分工回归设计原意：无月=稳定可重复中爆，一刀两断=一次性无上限斩杀。仍保留上限，不让它变成第二张无上限倾泻。
///
/// 结算：伤害 = 10 + 每势×min(当前剑势, MomentumCap)，合并为单段 powered 攻击；不清空/不消耗剑势。
/// 升级只提每势值、读取上限不变。剑势被动底薪由伤害中心另行叠加，本卡不手写（防同一资源双重计算）。
/// Echo 按当时剑势正常重复。
/// </summary>
public class AuroraMoonlessBlade() : AuroraCard(2, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy)
{
    protected override string ArtName => "moonless_blade";

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Retain];

    protected override IEnumerable<AuroraMechanic> MechanicTips => [AuroraMechanic.Momentum];

    public override AuroraStrikeVfxKind StrikeVfx => AuroraStrikeVfxKind.Ultimate;   // 招牌终结技：大招紫刀光

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new AuroraScalingDamageVar(10, ValueProp.Move, c =>
            Math.Min(MomentumPower.Get(c.Owner?.Creature), (int)c.DynamicVars["MomentumCap"].BaseValue)
            * (int)c.DynamicVars["PerMomentum"].BaseValue),
        new DynamicVar("PerMomentum", 2m),
        new DynamicVar("MomentumCap", 20m),
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var creature = Owner?.Creature;
        if (creature == null)
        {
            return;
        }

        var momentum = Math.Min(AuroraMomentumService.Get(creature), (int)DynamicVars["MomentumCap"].BaseValue);
        var dmg = (int)DynamicVars.Damage.BaseValue + momentum * (int)DynamicVars["PerMomentum"].BaseValue;
        await CommonActions.CardAttack(this, cardPlay, cardPlay.Target, dmg, ValueProp.Move).Execute(choiceContext);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["PerMomentum"].UpgradeValueBy(1m);   // 2 → 3（读取上限不变，恒为 20）
    }
}
