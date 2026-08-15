using System.Threading.Tasks;
using AuroraMod.AuroraCode.Helpers;
using AuroraMod.AuroraCode.Visuals;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.ValueProps;

namespace AuroraMod.AuroraCode.Powers;

/// <summary>
/// 护盾模块 / Shield Module（C 悬浮部件流派）—— 已部署的悬浮肩甲，架构 §6.2。
/// 回合开始时获得 <see cref="AuroraModulePower.Value"/> 点格挡（部署 5，每点强化 +1）。
/// 多实例（Instanced）：两枚护盾模块各自独立给盾。
/// </summary>
public sealed class ShieldModulePower : AuroraModulePower
{
    public const int BaseBlock = 5;

    public override ModuleKind Kind => ModuleKind.Shield;
    public override int BaseValue => BaseBlock;
    protected override string IconName => "shield_module";

    // 被动：己方回合开始时触发。
    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner.Player || Value <= 0)
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

        Flash();
        AuroraModuleVisualBridge.RequestTrigger(Owner, this);
        AuroraAudio.PlaySfx("skill.wav");   // 护盾模块生效音效（与技能牌共用素材，纯表现）。
        await CreatureCmd.GainBlock(Owner, Value, ValueProp.Unpowered, null);
    }
}
