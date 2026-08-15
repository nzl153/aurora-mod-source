using BaseLib.Utils;
using AuroraMod.AuroraCode.Cards;
using AuroraMod.AuroraCode.Helpers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace AuroraMod.AuroraCode.Cards.Common;

/// <summary>
/// 21 校验循环 / Validation Loop（普通，D 指令连锁）。扫描 3，然后抽 1。升级：扫描 3→4，抽牌不变。
/// （扫描实为「查抽牌堆顶 N」非看全手牌，回调到 3/4：基础恢复、升级比原 5 弱一点。）
/// 结算（扫描→正常抽 1）：扫描走 <see cref="AuroraScanHelper.ScanAsync"/>（查看抽牌堆顶 N、可移 0~全部入弃牌堆底、
/// 不触发弃牌 hook、不算抽/弃、不洗牌），随后是普通原生抽牌 <see cref="CardPileCmd.Draw"/>（可正常触发洗牌）。
/// 不按实际移走数量改变抽牌数；扫描 0 张仍抽 1。
/// </summary>
public class AuroraValidationLoop() : AuroraCard(1, CardType.Skill, CardRarity.Common, TargetType.Self)
{
    protected override string ArtName => "validation_loop";

    protected override IEnumerable<AuroraMechanic> MechanicTips => [AuroraMechanic.Scan];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("ScanCount", 3m),
        new DynamicVar("DrawCount", 1m),
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var player = Owner;
        if (player == null)
        {
            return;
        }

        // 1. 扫描（同步选择 + 实际换堆由 ScanHelper 负责；不触发弃牌效果）。
        await AuroraScanHelper.ScanAsync(choiceContext, player, (int)DynamicVars["ScanCount"].BaseValue, this);

        // 2. 扫描后正常抽牌（可触发洗牌；牌堆全空时自然抽不到，不造替代牌）。
        await CardPileCmd.Draw(choiceContext, DynamicVars["DrawCount"].BaseValue, player);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["ScanCount"].UpgradeValueBy(1m);   // 3 → 4
    }
}
