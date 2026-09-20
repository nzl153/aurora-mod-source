# 奥萝拉 · 设计与维护文档

这份文档记录核心设计意图、关键约束和维护注意事项。数值或机制改动前，优先确认 §4 和 §7。

---

## 0. 速查

### 卡池

| 稀有度 | 张数 | 说明 |
|---|---:|---|
| 基础 Basic | 6 | 起手牌 |
| 普通 Common | 20 | |
| 罕见 Uncommon | 39 | 含联机卡「战术接力」 |
| 稀有 Rare | 28 | 含联机卡「热能壁垒接力」 |
| 衍生 Token | 5 | 攻击/护盾模块令牌、积热/散热令牌、宕机 |
| **合计** | **98** | `cards.json` 的描述条目数 |

- **98**：所有卡类。
- **94**：图鉴显示数，排除 4 张临时选择令牌；宕机是状态牌，会进入图鉴。
- 费用分布（按 94 张）：0 费 9 / 1 费 67 / 2 费 15 / 3 费 3。
- 类型分布：技能 46 / 攻击 35 / 能力 17 / 状态 1。

### 流派标签覆盖

| 标签 | 数量 |
|---|---:|
| 热量 Heat | 58 |
| 护盾模块 / 攻击模块 | 19 / 19 |
| 剑势 Momentum | 18 |
| 指令连锁 Chain | 17 |
| 模块强化 | 11 |
| 换区 / 锁定 / 宕机 / 挑战协议 / 扫描 / 模块槽位 | 8 / 6 / 5 / 4 / 3 / 1 |

### 关键常量

| 项 | 值 | 定义位置 |
|---|---|---|
| 热量区段 | 冷 0–3 / 温 4–6 / 过载 7+ | `HeatPower.ZoneOf` |
| 过载增伤 | Powered Attack ×1.25 | `HeatPower.OverloadDamageMultiplier` |
| 过热阈值 | 10 | `HeatPower.OverheatThreshold` |
| 过热伤害阶梯 | 10 / 12 / 14 / 16 | `AuroraOverheatCountPower` |
| 剑势被动 | 每 3 点 +1 伤害，上限 +7 | `MomentumPower.DamagePerStacks` / `MaxDamageBonus` |
| 模块槽位 | 基础 2，硬上限 3 | `AuroraModuleCapacityPower` |
| 锁定 | 每层 +2 平伤，最多 6 层 | `LockPower` |
| 挑战协议 | 每层 +10%，最多 3 层 | `ChallengeProtocolPower` |
| 宕机惩罚 | 手牌有宕机则攻击 ×0.75 | `AuroraSystemCrashPenaltyPower` |

---

## 1. 热量

热量是风险条，不是第二套能量。核心关系只有三条：

- 7+ 进入过载区，奥萝拉自身的 Powered Attack 伤害 ×1.25；模块等 Unpowered 伤害不受影响。
- 达到 10 时锁定一笔过热伤害，回合末或引爆时结算。
- 锁定后散热只降低当前热量，不降低已经锁定的伤害。

过热基础伤害按本场次数递增：10 / 12 / 14 / 16（封顶 16）。
红线和重复越线会继续提高本次锁定伤害。击杀最后一个敌人后免除尚未结算的过热代价，避免收尾攻击反杀玩家。

这套机制的目标是让高热量始终同时代表收益和风险；修改时不要让「冲到高热后无代价撤退」成为稳定解法。

---

## 2. 四条构筑方向

### A · 过热暴走

主轴。主动进入红线，以自伤或最大生命换取爆发。
关键卡包括超频、极限断裂、葬炉、炉心淬锋。

### B · 剑势

剑势无上限且不随回合衰减。每 3 点使 Powered Attack 每段 +1，**最多 +7（21 点达到被动上限）**；
计数本身仍可继续增长，供倾泻类卡牌读取。

早期版本的剑势只有「储存后兑现」，积累过程没有即时收益，因此后来加入被动增伤。
现在清空剑势会失去持续增伤，使「立即兑现」与「继续保留」形成真实取舍。

### C · 悬浮模块

基础 2 槽，最多 3 槽。模块是独立 Power 实例，可分别强化、触发和替换。

满槽部署新模块时，被替换模块会在移除前按当前数值触发一次；轮转和主动收回不触发这次退役效果。
实现位于 `AuroraModuleController.DeployAsync`，触发必须发生在 `PowerCmd.Remove` 前，否则模块会失去 Owner。

模块伤害使用 **Unpowered**，因此不吃热量倍率和剑势被动。这是防止热量 × 剑势 × 模块数量产生乘法膨胀的关键约束。

### D · 指令连锁

每回合手动打出第 3 张牌时激活，持续到回合结束。
自动打出、复制和同一次出牌的额外结算不计数；相关判断依赖 `!IsAutoPlay && IsFirstInSeries`。

### 配套机制

- **锁定**：攻击或攻击模块命中时消耗层数，并追加平伤；攻击模块会优先考虑锁定较高的目标。
- **挑战协议**：给敌人施加 buff，使其对奥萝拉造成的伤害提高；部分剑势卡把这份风险转换为收益。

---

## 3. 数值与平衡

平衡调整以原版同「稀有度 × 费用」卡牌为基准，优先比较攻击、格挡等直接数值的分布和中位数。

调整原则：

1. 先确认偏差是否来自单卡、构筑联动还是机制本身。
2. 只修明确的洼地或异常值，不做无依据的整体上调。
3. 改稀有度时遵守 §7 的数量约束。
4. 涉及热量、剑势、模块时同时检查交叉倍率，避免乘法膨胀。

---

## 4. 刻意为之，不是 bug

| 现象 | 原因 |
|---|---|
| 模块伤害不吃过载增伤、不吃剑势被动 | 防止多轴乘法膨胀 |
| 散热不能降低已锁定的过热伤害 | 防止冲到 10 后无代价撤回 |
| 剑势被动封顶 +7，但计数无上限 | 只限制被动增伤，不限制倾泻类卡牌的计数 |
| 挑战反斩先移除协议再攻击 | 避免协议倍率和兑现伤害重复计算 |
| 战斗胜利免除未结算过热代价 | 防止击杀最后一个敌人后被自身机制反杀 |
| 挑衅按实际新增协议层数给剑势 | 满层后不能继续刷收益 |
| 一刀两断按「读快照 → 攻击 → 清空」结算 | 攻击必须读取清空前的剑势 |
| 装甲冲撞不消耗格挡 | 与原版全身撞击语义一致 |
| 冗余装甲只保留一半格挡 | 避免与装甲冲撞形成近似无限增长 |
| 泄势斩追加段封顶 12，且不消耗剑势 | 控制单卡上限，同时保留剑势主系统 |

---

## 5. 维护检查

遇到平衡反馈时，先区分是单卡数值、构筑成型度还是机制本身缺乏即时收益。
多人问题优先检查新代码是否破坏 §6 的同步约束。

影响己方攻击输出的主要隐式状态：

| 钩子 | 读取状态 |
|---|---|
| `AuroraSystemCrashPenaltyPower` | 手牌是否有宕机 |
| `ChallengeProtocolPower` | 目标身上的协议层数 |
| `HeatPower` | 热量区段、Pending、超频 |
| `MomentumPower` | 剑势 |

如果卡牌在攻击前修改这些状态，本次攻击会立即受影响。
「消耗资源后造成伤害」类卡牌要特别检查结算顺序。

---

## 6. 引擎与联机约束

### 联机安全

- 不使用 `Random` / `DateTime` / `Guid` / `Time.Get*` 作为玩法随机源。
- 归属身份使用 `AuroraPerApplier`（NetId），不要用裸 `Creature` 引用作持久字典键。
- 「每场 / 每回合一次」效果必须使用可恢复的状态标记，例如 `AuroraTurnGatePower`。
- 玩家选择使用游戏同步选择接口；失败时不要在本地随机兜底。

### 卡牌预览

联机同步的卡牌字段不包含 `PreviewValue`，因此预览只写 `PreviewValue`，不要为了显示效果改 `BaseValue`。
`AuroraScalingDamageVar` 是当前统一入口。

### canonical 卡

图鉴或主菜单里的 canonical 卡没有有效战斗状态：

- `card.CombatState` / `card.RunState` 可安全得到 null。
- `card.Owner` 可能抛 `CanonicalModelException`。

预览逻辑先检查战斗状态，再读取 Owner 或其他运行时数据。

### 结算顺序

资源清除、状态移除等操作若发生在攻击前，会改变该次攻击读取到的倍率或加值。
一刀两断必须先读取剑势快照，再攻击，最后清空。

---

## 7. 改动铁律

### 1. 稀有度总数保持不变

调整卡牌稀有度时做 1 对 1 对调，避免破坏卡池分布。

### 2. 玩家可见数值优先使用 `DynamicVar`

凡是文案需要显示或可能调整的数值，优先通过 `DynamicVar` 统一来源，避免代码和本地化文案漂移。
过去剑势共鸣、三相指令核都出现过只改一处导致悬停过期的问题。

### 3. 模块伤害保持 Unpowered

这是防止热量、剑势和模块形成乘法膨胀的核心约束。

### 4. 设计文档跟随代码更新

改动常量、阈值或结算顺序后，同步检查 `README.md`、本文件和本地化文案。
**代码是最终真值源。**

---

## 8. 构建与版本适配

### 构建顺序

改本地化或资源时先导 pck，再 build dll；只改 C# 时直接 build dll。

```bash
# 1. 导 pck（游戏必须完全退出）
godot --headless --path <项目路径> --export-pack "Windows Desktop" <输出路径>

# 2. build dll
git checkout HEAD -- AuroraMod.csproj AuroraMod.sln
STS2_GAME_DIR="<游戏安装目录>" dotnet build AuroraMod.csproj -c Debug
```

- Godot 导出退出时的 `Attempt to unregister unexisting extension class 'Spine*'` 为已知无害信息。
- 构建应为 0 错误；新增警告需要确认。
- 游戏运行时 dll 会被锁定，构建前先退出游戏。

### beta 分支

正式版与 beta 共用一份源码，通过 `STS2_BETA` 条件编译：

```bash
dotnet build -p:Sts2Beta=true
dotnet build
```

截至 2026-08-17，beta `v0.111.0` 相对正式版 `v0.107.1` 的主要 API 差异：

| # | 变更 |
|---|---|
| 1 | `CardModel.GetResultPileTypeForCardPlay()` → `GetResultLocationForCardPlay()`，返回 `CardLocation` |
| 2 | `ModifyDamageAdditive` / `Multiplicative` / `Cap` 上移到 `AbstractModel`，末尾新增 `CardPlay?` |
| 3 | `CharacterModel.GenerateAnimator` 新增 `Creature` 参数 |
| 4 | `CreatureCmd.Damage` 删除 6 参数重载，改为末尾带 `CardPlay?` 的 7 参数版 |
| 5 | `CreatureCmd.LoseBlock` 只剩 `(ctx, target, amount, remover)` |
| 6 | `Hook.ModifyDamage` 在 `cardSource` 后插入 `CardPlay?` |
| 7 | `Hook.ModifyDamageInternal` 的 `combatState` 由 `CombatState` 改为 `ICombatState` |

跨版本适配重点：

- Harmony 目标方法或参数类型变化可能在运行时才报错；启动先检查 `HarmonyException`。
- `[HarmonyPatch]` 写死的参数类型数组不会在编译期验证目标方法是否存在。
- 通过节点名寻找 UI 可能静默失效；优先使用稳定引用/字段，节点名查找只作兜底。
- `CardPlay` 在牌库、商店、奖励等预览路径可能为 null。
- 涉及 `SavedProperty` 的改动要实际做一次存档往返测试。

发布同一工坊 ID 的双分支构建时，先给正式版设置 `maxBranch: "public"`，再上传 beta 构建。

---

## 9. 文案规范

角色描述保持原版风格：两行，第一行身份，第二行玩法。

```text
一台没有收到停机指令的战争机械。
战斗中不断推高炉温，用濒临过载的机体换取更重的一击。
```

玩家可见文案避免内部实现词汇，例如「债务、快照、门闩、兜底、序列化」。

条件加值伤害统一使用实时预览值，不再额外写「牌面数字为基础伤害」之类的开发说明。

---

## 10. 已知遗留项

- `AuroraTouchOfOrobasUpgradePatch` 缺少 `starterRelic == null` 守卫；若后续维护该事件，可补一行判空。
