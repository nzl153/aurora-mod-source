using BaseLib.Abstracts;
using Godot;

namespace AuroraMod.AuroraCode.Characters;

/// <summary>
/// 奥萝拉专属药水池 —— 只含奥萝拉专属药水（过载催化剂/纳米装配剂，经 <c>[Pool(typeof(AuroraPotionPool))]</c> 反射注入）。
/// <b>共享药水由 PotionFactory 自行拼接</b>（GetPotionOptions = Character.PotionPool ∪ SharedPotionPool），故本池<b>不得</b>再返回 Shared，
/// 否则共享药水双份加权、且半边走 AllPotions 绕过 Epoch 解锁过滤。因此 GenerateAllPotions 保持默认空、类为空壳（同 AuroraRelicPool）。
/// IsShared 默认 false（不并入共享池）。<see cref="Aurora"/>.PotionPool 指向本池。
/// </summary>
public class AuroraPotionPool : CustomPotionPoolModel
{
    public override Color LabOutlineColor => Aurora.Color;
}
