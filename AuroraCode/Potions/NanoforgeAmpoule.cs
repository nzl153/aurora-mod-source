using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using AuroraMod.AuroraCode.Characters;
using AuroraMod.AuroraCode.Helpers;
using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace AuroraMod.AuroraCode.Potions;

/// <summary>
/// P-02 纳米装配剂 / Nanoforge Ampoule（稀有药水，C 悬浮模块）—— 选攻击或护盾并部署 1 枚模块，新模块 +2 强化并立即触发 1 次。
/// 实现：同步选型 → 标准 <see cref="AuroraModuleController.DeployAsync"/>（满槽走同步替换、可触发哨戒/满载等部署监听）→ 取新实例 → +2 强化 → 触发 1 次。
/// 不突破容量/3 槽硬上限；模块触发 Unpowered、不吃过载、不推连锁。取消选型或部署失败则无效果返回（潜在冲突：药水消耗时机由框架决定，取消不消耗需框架支持，见反馈）。
/// </summary>
[Pool(typeof(AuroraPotionPool))]
public class NanoforgeAmpoule : CustomPotionModel
{
    public override PotionRarity Rarity => PotionRarity.Rare;
    public override PotionUsage Usage => PotionUsage.CombatOnly;
    public override TargetType TargetType => TargetType.None;

    private const int Enhance = 2;
    private const string ImgDir = "res://Aurora/Images/Potions/";

    public override string CustomPackedImagePath =>
        ResourceLoader.Exists($"{ImgDir}nanoforge_ampoule.png") ? $"{ImgDir}nanoforge_ampoule.png" : null;

    protected override async Task OnUse(PlayerChoiceContext choiceContext, Creature target)
    {
        var creature = Owner?.Creature;
        if (creature == null || !CombatManager.Instance.IsInProgress)
        {
            return;
        }

        var kind = await AuroraModuleController.ChooseModuleTypeAsync(choiceContext, creature, null);
        if (kind == null)
        {
            return;   // 取消选型。
        }

        var newModule = await AuroraModuleController.DeployAsync(choiceContext, creature, kind.Value, null);
        if (newModule == null)
        {
            return;   // 满槽替换取消/部署失败。
        }

        await AuroraModuleController.EnhanceSpecificAsync(choiceContext, newModule, Enhance, null);
        await AuroraModuleController.TriggerInstanceAsync(choiceContext, newModule);
    }
}
