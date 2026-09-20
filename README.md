# 奥萝拉 / Aurora

《杀戮尖塔 2》的可玩角色 MOD。一台没有收到停机指令的战争机械——
战斗中不断推高炉温，用濒临过载的机体换取更重的一击。

![选人界面](docs/character-select.png)

> 非官方粉丝作品，与 Mega Crit 无关。需要游戏本体。

**[Steam 创意工坊页面](https://steamcommunity.com/sharedfiles/filedetails/?id=3772711396)**

---

## 这个角色是什么

核心机制是**热量**：一条风险条，而不是第二套能量。

热量进入过载区（7+）后，奥萝拉自身的 Powered Attack（强力攻击）伤害 ×1.25；
模块等 Unpowered（非强力攻击）伤害不受这个倍率影响。
热量达到 10 会锁定一笔过热伤害，在回合末结算；之后散热可以降低当前热量，
但不能取消或降低已经锁定的伤害。

围绕热量有四条主要构筑方向：

| 流派 | 玩法重点 |
|---|---|
| **A · 过热暴走** | 主动冲高热量，以自伤或最大生命为代价换爆发 |
| **B · 剑势** | 积累剑势获得持续增伤，也可通过特定牌集中兑现 |
| **C · 悬浮模块** | 在有限模块槽中部署、强化、替换攻击或护盾模块 |
| **D · 指令连锁** | 围绕每回合手动出牌数量规划出牌顺序 |

完整设计与维护说明见 **[docs/DESIGN.md](docs/DESIGN.md)**。

## 内容量

| | |
|---|---|
| 卡牌 | 98（图鉴显示 94，另 4 张为临时令牌） |
| 专属遗物 | 4 |
| 专属药水 | 2 |
| 专属事件 | 3 |
| 联机专属卡 | 2 |
| 语言 | 简体中文 / English / 日本語 / Русский |

角色有完整的 Spine 骨骼动画（战斗 + 选人界面）、自定义能量球、
自定义命中特效，以及全卡池的关键字悬停说明。

---

## 安装

**推荐**：直接订阅上面的创意工坊页面。

**手动**：把 `Aurora.pck`、`Aurora.dll`、`Aurora.json` 放进游戏目录的 `mods/aurora/`。

依赖：

- 《杀戮尖塔 2》 `v0.107.1` 或更高（正式版与 beta 分支均可）
- [BaseLib](https://steamcommunity.com/sharedfiles/filedetails/?id=3737335127) `v3.3.0` 或更高

> **正式版与 beta 分支都已适配**（2026-08-17，beta `v0.111.0`）。
> 两个分支共用同一份源码，靠条件编译产出两份 dll，Steam 按玩家所在分支自动派发：
>
> ```bash
> dotnet build -p:Sts2Beta=true   # beta 分支
> dotnet build                     # 正式版（默认）
> ```
>
> API 差异清单见 [docs/DESIGN.md](docs/DESIGN.md) §8。

---

## 从源码构建

### 需要什么

- Godot 4.5.1（**mono / .NET 版**）
- .NET SDK 8.0+
- [spine-godot 运行时](https://zh.esotericsoftware.com/spine-godot)
  —— **不包含在本仓库中**，需自行获取并放到 `bin/`
  （`bin/spine_godot_extension.gdextension` 已就位，缺运行时时 Godot 无法打开 Spine 资源）

### 构建顺序

改了本地化或美术资源时，**先导 pck，再 build dll**。
headless 导出会触发 Godot 的 C# 编译，可能覆盖已部署的 dll。

```bash
# 1. 导 pck（游戏必须完全退出）
godot --headless --path . --export-pack "Windows Desktop" Aurora.pck
```

```bash
# 2. build dll
git checkout HEAD -- AuroraMod.csproj AuroraMod.sln
STS2_GAME_DIR="<游戏安装目录>" dotnet build AuroraMod.csproj -c Debug
```

只改 C# 代码时，第 2 步即可。

构建应当是 **0 错误**。警告数量取决于 BaseLib 版本；
出现新的警告时建议逐项确认。

---

## 目录结构

```text
AuroraCode/          C# 逻辑
  Cards/             卡牌，按稀有度分目录
  Powers/            能力（含 Heat / Momentum / Chain 等核心机制）
  Relics/ Potions/ Events/
  Helpers/           模块控制器、机制悬停等共用逻辑
  Patches/           Harmony 补丁（含第三方 mod 兼容层）
  Visuals/           命中特效
Aurora/              Godot 资源（进 pck）
  Images/            卡面、图标、特效贴图
  Spine/             骨骼动画
  localization/      zhs / eng / jpn / rus
  Scenes/ Shaders/ Materials/ Audio/
docs/DESIGN.md       设计与维护文档
```

---

## 授权

**双授权**，详见 [LICENSE](LICENSE)：

- **代码** → MIT
- **美术 / 音频素材** → CC BY-NC-SA 4.0：允许使用和修改，但须署名、不得商用，衍生作品保持同协议

游戏本体的一切内容归 Mega Crit 所有，不在本仓库内。

---

## 说明

这是个人兴趣项目，不接受赞助、不做商业化。
欢迎 fork 来做自己的角色。`AuroraCode/Powers/` 中的热量、模块和连锁机制可作为实现参考。

提 issue 前建议先读 [docs/DESIGN.md](docs/DESIGN.md) §4「刻意为之，不是 bug」。
