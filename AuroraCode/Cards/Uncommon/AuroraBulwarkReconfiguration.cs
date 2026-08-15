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

namespace AuroraMod.AuroraCode.Cards.Uncommon;

/// <summary>
/// C-U03 堡垒重构 / Bulwark Reconfiguration（罕见，C 悬浮模块）。获得 8 格挡，部署 1 枚护盾模块；若成功使新模块触发 1 次；
/// 若打出前处于冷区，再触发 1 次。升级：格挡 8→11。
/// 结算：读打出前区段快照 WasCold → 基础格挡 → DeployAsync 护盾模块 → 成功则 TriggerInstance(新模块) 一次；WasCold 再一次。
/// 只触发「本次新部署的那一枚」（全体护盾触发是联防协议的职责）。满槽替换取消仍保留基础格挡、无触发。
/// 与哨戒阵列共存时顺序：哨戒强化→哨戒触发→本卡触发。
/// </summary>
public class AuroraBulwarkReconfiguration() : AuroraCard(2, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    protected override string ArtName => "bulwark_reconfiguration";

    /// <summary>金框：处于冷区时额外效果可触发（工坊反馈 #1，沿用原版 Dismantle/Spite 的金框语义）。</summary>
    protected override bool ShouldGlowGoldInternal => AuroraGlow.InZone(this, HeatPower.HeatZone.Cold);

    protected override IEnumerable<AuroraMechanic> MechanicTips => [AuroraMechanic.ShieldModule, AuroraMechanic.Heat];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new BlockVar(8, ValueProp.Move),
        new DynamicVar("TriggerCount", 1m),
        new DynamicVar("ColdExtraTrigger", 1m),
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var creature = Owner?.Creature;
        if (creature == null)
        {
            return;
        }

        // 1. 打出前读区段快照。
        var wasCold = HeatPower.GetZone(creature) == HeatPower.HeatZone.Cold;

        // 2. 基础格挡（先结算，替换取消仍保留）。
        await CreatureCmd.GainBlock(creature, (int)DynamicVars.Block.BaseValue, ValueProp.Move, cardPlay);

        // 3. 部署护盾模块（满槽走同步替换）；只有成功才触发新模块。
        var deployed = await AuroraModuleController.DeployAsync(choiceContext, creature, ModuleKind.Shield, this);
        if (deployed == null)
        {
            return;
        }

        // 4. 触发新模块 TriggerCount 次；冷区再触发 ColdExtraTrigger 次（只触发这一枚，不触发其他护盾模块）。
        var triggers = (int)DynamicVars["TriggerCount"].BaseValue;
        for (var i = 0; i < triggers; i++)
        {
            await AuroraModuleController.TriggerInstanceAsync(choiceContext, deployed);
        }

        if (wasCold)
        {
            var extra = (int)DynamicVars["ColdExtraTrigger"].BaseValue;
            for (var i = 0; i < extra; i++)
            {
                await AuroraModuleController.TriggerInstanceAsync(choiceContext, deployed);
            }
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Block.UpgradeValueBy(3m);   // 8 → 11
    }
}
