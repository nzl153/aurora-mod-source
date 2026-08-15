using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using AuroraMod.AuroraCode.Characters;
using AuroraMod.AuroraCode.Powers;
using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace AuroraMod.AuroraCode.Potions;

/// <summary>
/// P-01 过载催化剂 / Overload Catalyst（罕见药水，A 过热）—— 仅热量低于 7 时可用；把热量提升至 7。
/// 实现：<see cref="PassesCustomUsabilityCheck"/> 门控（热 &lt; 7 才亮按钮）；OnUse 调标准 <see cref="HeatPower.AddHeatAsync"/>(7-当前)，
/// 禁止直接 Set；只一次 Heat 变化（0→7 视作一次换区）。本药水只送入过载区、不越过热阈值；不移除/重置已登记 Pending；不算打出牌、不推连锁。
/// </summary>
[Pool(typeof(AuroraPotionPool))]
public class OverloadCatalyst : CustomPotionModel
{
    public override PotionRarity Rarity => PotionRarity.Uncommon;
    public override PotionUsage Usage => PotionUsage.CombatOnly;
    public override TargetType TargetType => TargetType.None;

    private const int TargetHeat = 7;
    private const string ImgDir = "res://Aurora/Images/Potions/";

    public override string CustomPackedImagePath =>
        ResourceLoader.Exists($"{ImgDir}overload_catalyst.png") ? $"{ImgDir}overload_catalyst.png" : null;

    public override bool PassesCustomUsabilityCheck
    {
        get
        {
            var creature = Owner?.Creature;
            return creature != null && HeatPower.GetHeat(creature) < TargetHeat;
        }
    }

    protected override async Task OnUse(PlayerChoiceContext choiceContext, Creature target)
    {
        var creature = Owner?.Creature;
        if (creature == null || !CombatManager.Instance.IsInProgress)
        {
            return;
        }

        // OnUse 内再读一次热量（防竞态；此处不再退药，仅确保不误积到 7 以上）。
        var delta = TargetHeat - HeatPower.GetHeat(creature);
        if (delta > 0)
        {
            await HeatPower.AddHeatAsync(choiceContext, creature, delta, null);
        }
    }
}
