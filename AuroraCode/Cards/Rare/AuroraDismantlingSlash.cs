using BaseLib.Utils;
using AuroraMod.AuroraCode.Cards;
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
/// C-R06 拆械斩 / Dismantling Slash（稀有，C 悬浮模块）。造成 8 伤害；可选移除 1 枚模块 → 改为 18 + 该模块每点强化×5 伤害。升级 8→11 / 18→22。
/// 每点强化 4→5——提高「资产变现」的 Boss 兑现上限（基础伤害/选择语义/移除语义不变）。
/// 结算：有模块则可取消地同步选一枚——取消/无模块=基础伤害；选中则先记强化量 E（Value-BaseValue）再移除（不触发/不轮转/不触哨戒或底盘，强化永久丢失），
/// 伤害改 RemoveDamage + E×5。无论是否移除都只有一段 powered。移除立即改变满槽状态。Echo 每次可重选仍存在的模块。资产变现攻击。
/// </summary>
public class AuroraDismantlingSlash() : AuroraCard(1, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy)
{
    protected override string ArtName => "dismantling_slash";

    protected override IEnumerable<AuroraMechanic> MechanicTips =>
        [AuroraMechanic.AttackModule, AuroraMechanic.ShieldModule, AuroraMechanic.ModuleEnhancement];

    private const int PerEnhance = 5;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(8, ValueProp.Move),
        new DynamicVar("RemoveDamage", 18m),
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var creature = Owner?.Creature;
        if (creature == null)
        {
            return;
        }

        int dmg;
        var chosen = await AuroraModuleController.ChooseModuleOptionalAsync(choiceContext, creature, this);
        if (chosen != null)
        {
            var enhancement = chosen.Value - chosen.BaseValue;   // 记录强化量后再移除
            await AuroraModuleController.RecallAsync(chosen);     // 纯移除：不触发/不轮转/不触监听，强化永久丢失
            dmg = (int)DynamicVars["RemoveDamage"].BaseValue + enhancement * PerEnhance;
        }
        else
        {
            dmg = (int)DynamicVars.Damage.BaseValue;
        }

        await CommonActions.CardAttack(this, cardPlay, cardPlay.Target, dmg, ValueProp.Move).Execute(choiceContext);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(3m);           // 8 → 11
        DynamicVars["RemoveDamage"].UpgradeValueBy(4m);  // 18 → 22
    }
}
