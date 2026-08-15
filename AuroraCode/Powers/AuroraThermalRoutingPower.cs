using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

namespace AuroraMod.AuroraCode.Powers;

/// <summary>
/// H-U36 热态路由 Power（枢纽罕见）—— 可见能力。回合开始时按当前热区获得一项效果：
/// 冷区=获 <c>ColdBlock</c> 格挡 / 温区=抽 <c>WarmDraw</c> 牌 / 过载区(含红线)=获 <c>OverloadEnergy</c> 能量。
/// <see cref="Amount"/>=层数，各分支收益线性 ×层数。一次回合开始只读一次区段、只走一个分支。
/// 过载能量在基础能量刷新完成后发放（AfterPlayerTurnStart 在回合起始设置之后）。打出当回合不触发、从下个本人回合起生效。
/// </summary>
public sealed class AuroraThermalRoutingPower : AuroraPower
{
    public override PowerInstanceType InstanceType => PowerInstanceType.None;
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    protected override bool IsVisibleInternal => true;
    protected override string IconName => "thermal_routing";

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("ColdBlock", 4m),
        new DynamicVar("WarmDraw", 1m),
        new DynamicVar("OverloadEnergy", 1m),
        new DynamicVar("OverloadHeat", 1m),
    ];

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner.Player)
        {
            return;
        }

        var stacks = (int)Amount;
        if (stacks <= 0)
        {
            return;
        }

        // 回合开始只读一次区段（过载含 10+ 红线），只走一个分支。
        var zone = HeatPower.GetZone(Owner);
        switch (zone)
        {
            case HeatPower.HeatZone.Cold:
                Flash();
                await CreatureCmd.GainBlock(Owner, (int)DynamicVars["ColdBlock"].BaseValue * stacks, ValueProp.Unpowered, null);
                break;
            case HeatPower.HeatZone.Warm:
                Flash();
                await CardPileCmd.Draw(choiceContext, (int)DynamicVars["WarmDraw"].BaseValue * stacks, player);
                break;
            default:   // Overload（含红线）：先得能量，随后一次性积热。区段快照已在上方读取，积热不改变本次分支。
                Flash();
                await PlayerCmd.GainEnergy((int)DynamicVars["OverloadEnergy"].BaseValue * stacks, player);
                await HeatPower.AddHeatAsync(choiceContext, Owner, (int)DynamicVars["OverloadHeat"].BaseValue * stacks, null);
                break;
        }
    }
}
