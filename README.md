# 遗迹回响：影子共生 · Ruins Echo

> 2D 俯视角动作肉鸽 · Unity 2022 LTS + C# 单机 MVP
>
> 录制自己最近 3 秒的动作，消耗回响能量召唤影子，让影子原样回放你的操作。

[规格文档](docs/spec/遗迹回响_影子共生_单机MVP开发规格文档.md) ·
[架构设计](docs/architecture.md) ·
[开发路线](docs/roadmap.md) ·
[开发指南](docs/development.md) ·
[实现状态](docs/implementation-status.md)

---

## 当前状态

仓库处于 **M0 工程地基 + M1 核心链路技术切片**。目标不是现在就交付完整 5 层游戏，而是先把最高风险、最有差异化的“录制 → 影子回放 → 遗物改写行为”做成可运行、可测试的骨架，再按路线图填充内容。

| 模块 | 状态 |
| --- | --- |
| Unity 2022 LTS 工程骨架 / Assembly Definition / EditMode 测试程序集 | ✅ 已建立 |
| 3 秒滑动窗口录制（UnscaledTime） | ✅ 已实现 |
| 影子回放游标（变速 / 循环 / 反向 / 重复时间戳） | ✅ 已实现 + 测试代码 |
| 影子对象池 / 影子生命 / 回放攻击 | ✅ 已实现 |
| 回响能量 | ✅ 已实现 |
| 9 件 MVP 遗物与聚合属性管线 | ✅ 已实现 + 测试代码 |
| 种子驱动的 5 层地牢数据生成 | ✅ 已实现 + 测试代码 |
| 局外 JSON 存档 / 残片 / 解锁 | ✅ 已实现 + 测试代码 |
| 局内状态机 | ✅ 已实现 + 测试代码 |
| Echo Lab 灰盒技术切片（移动 / 普攻 / 闪避 / 录制 / 召唤 / 遗物三选一） | ✅ 代码已落地，待 Unity 2022 实机验证 |
| 完整 5 层房间流程 / Boss / 远程敌人 / UGUI / 正式场景与 Prefab | ⏳ 见路线图 M2–M5 |

> `✅` 表示代码已落地，不等于已通过实机验收。当前编写环境没有安装 Unity 2022 编辑器，运行时代码已用 Unity 2021.3 引用程序集做过编译校验；**尚未在 Unity 2022 中实际运行**。首次打开工程后，请先执行 EditMode 测试与一次 Play 验证，见[开发指南](docs/development.md)。

---

## 快速开始

### 环境

- Unity Hub
- Unity **2022.3.62f1**（或任意 2022.3 LTS；ProjectVersion 以 2022.3.62f1 为基线）
- Windows 10/11，键盘 + 鼠标

### 打开

1. Unity Hub → Add → 选择本仓库根目录 `ruins-echo/`。
2. 用 Unity 2022.3 打开，等待首次导入与包解析。
3. 可选：执行 `Tools → Ruins Echo → Create Main Scene`，生成 `Assets/Scenes/Main.unity` 并加入 Build Settings。
   - 也可以直接在当前空场景按 **Play**：`EchoLabBootstrap` 会通过 `RuntimeInitializeOnLoadMethod` 自动搭建灰盒场景。
4. 按 **Play**。

### 技术切片操作

| 按键 | 功能 |
| --- | --- |
| `WASD` | 移动 |
| 鼠标 | 瞄准 |
| 鼠标左键 | 普攻（前摇 0.15s / 后摇 0.2s） |
| `Space` | 闪避冲刺 + 无敌帧 |
| `Q` | 召唤影子，回放最近 3 秒动作 |
| `R` | 清空动作缓存 |
| `1 / 2 / 3` | 战斗结束后选择遗物 |
| `Enter` | 死亡后重开 |

技术切片会不断刷波次：清空当前波 → 三选一遗物 → 下一波，用来验证遗物对影子行为的改写。

---

## 项目结构

```text
ruins-echo/
├─ Assets/
│  └─ RuinsEcho/
│     ├─ Runtime/                 # 运行时代码（RuinsEcho.Runtime.asmdef）
│     │  ├─ Core/                 # 常量、状态机、种子随机、角色模板
│     │  ├─ Recording/            # 录制缓存、回放游标、影子、录制器
│     │  ├─ Player/               # 玩家控制、生命、回响能量
│     │  ├─ Relics/               # 遗物数据、聚合属性、9 件 MVP 遗物
│     │  ├─ Dungeon/              # 5 层地牢数据与种子生成
│     │  ├─ Enemies/              # 敌人基类与目标选择
│     │  ├─ Combat/               # 伤害接口与事件
│     │  ├─ Save/                 # 局外 JSON 存档
│     │  ├─ Spawning/             # 影子对象池
│     │  └─ Presentation/         # Echo Lab 灰盒场景与调试 HUD
│     ├─ Editor/                  # 场景生成、工程校验
│     └─ Tests/EditMode/          # NUnit EditMode 测试
├─ Packages/manifest.json
├─ ProjectSettings/ProjectVersion.txt
├─ docs/
│  ├─ spec/                       # 规格文档快照
│  ├─ architecture.md
│  ├─ roadmap.md
│  ├─ development.md
│  ├─ implementation-status.md
│  └─ decisions/                  # ADR
└─ .github/                       # CI、Issue / PR 模板
```

---

## 设计要点

### 1. 回放使用 Transform，不用 Rigidbody2D

影子每帧把 `Transform.position / rotation` 直接赋值为录制帧的值。碰撞体只参与 `Physics2D.OverlapCircle` 伤害查询，不参与移动物理，避免回放轨迹漂移。

### 2. 时间戳全部使用 `Time.unscaledTime`

录制窗口、回放时间轴、Boss 慢动作都基于 UnscaledTime；`Time.timeScale` 只影响常规游戏逻辑，不会破坏回放时序。

### 3. 种子驱动，完全可复现

使用自研 `DeterministicRandom`（SplitMix64），而不是 `UnityEngine.Random.State`。同一种子 + 同一遗物池，房间、敌人分布、遗物三选一完全一致，且不随 Unity 版本变化。见 [ADR-0001](docs/decisions/0001-deterministic-rng.md)。

### 4. 遗物是“规则修改器”，不是数值堆叠

`RelicData` 持有 `IRelicEffect` 列表；所有效果把修正写入 `RelicStats` 聚合快照。玩家、影子、敌人只读快照，新增遗物不需要改动战斗代码。

### 5. 局内 / 局外严格隔离

- 局内：HP、能量、录制帧、影子、遗物列表、种子 → 死亡即销毁。
- 局外：`fragments`、`unlockedRelicIds`、`unlockedCharacterIds` → `SaveData.json` 持久化。
- 局内对象没有写入存档的路径。

---

## 测试

Unity 菜单：`Window → General → Test Runner → EditMode → Run All`。

当前覆盖：

- 录制缓存 3 秒裁剪、时间戳单调性
- 回放游标：正向、变速、循环、反向、空缓存
- 回响能量：恢复、上限、消耗、扣除
- 9 件遗物的聚合修正、8 件上限、移除重算
- 种子地牢：5 层、确定性、遗物池顺序无关
- 存档：JSON 往返、损坏回退、默认解锁
- 状态机：合法 / 非法转换、死亡、RunContext 失效

CI 配置见 `.github/workflows/`；Unity 测试任务默认跳过，需在仓库 Variables 中设置 `UNITY_CI_ENABLED=true` 并配置 `UNITY_LICENSE` secret。详见[开发指南](docs/development.md)。

---

## 已知限制（技术切片）

- 使用 IMGUI 调试 HUD，不是最终 UGUI；正式 UI 在 M5。
- 没有 Tilemap 房间、Prefab、正式场景；Echo Lab 在运行时程序化生成。
- 只实现了近战杂兵与蚀影怪目标偏好；远程杂兵、Boss、扭曲力场、传送门尚未接入。
- 完整 5 层流程、事件房、休憩房、结算尚未接入。
- 敌 / 投射物对象池尚未实现（影子池已完成）。

这些都在 [路线图](docs/roadmap.md) 中拆成了可执行任务。

---

## 许可证

当前未指定开源许可证（默认保留所有权利）。如需开源，请在合并外部贡献前添加 `LICENSE`。
