# 开发路线图

- 基线：[MVP 规格文档](spec/遗迹回响_影子共生_单机MVP开发规格文档.md)
- 当前阶段：**M0 完成，M1 代码落地待实机验证**
- 用法：每个里程碑独立可验收；任务可直接拆成 GitHub Issue。

状态图例：`[x]` 已完成 · `[~]` 代码已落地、待 Unity 验证 · `[ ]` 未开始

---

## M0 · 工程地基（已完成）

目标：让任何人都能克隆、打开、编译、跑测试、提交。

- [x] 团结引擎 Tuanjie 1.10.4 / Unity 2022.3.62t16 工程骨架（`Assets / Packages / ProjectSettings`）
- [x] Assembly Definition：Runtime / Editor / Tests 三层
- [x] `.gitignore`（Unity 生成目录）、`.gitattributes`（YAML 合并）
- [x] 目录结构与命名空间约定
- [x] README / 架构 / 开发指南 / 实现状态 / ADR
- [x] GitHub Actions：仓库结构校验 + 可选 Unity EditMode 测试
- [x] Issue / PR 模板
- [ ] 在 Tuanjie 1.10.4 中首次导入并提交引擎生成的 `.meta`（首次打开后执行）
- [ ] 在 Tuanjie 1.10.4 中跑通全部 EditMode 测试（当前环境仍在安装编辑器）

**验收**：新机器按 README 可在 10 分钟内打开工程；Test Runner 全绿；CI 的 validate 任务通过。

---

## M1 · 录制 / 回放技术切片（代码已落地）

目标：验证“玩家录 3 秒 → 影子原样回放 → 遗物改变影子行为”的核心循环。

### 录制

- [x] `RecordFrame` / `RecordBuffer`：3 秒滑动窗口、时间戳单调性
- [x] `PlayerRecorder`：LateUpdate 每帧追加、`Time.unscaledTime`
- [x] 角色录制窗口 +0.5s（追猎者）
- [x] `R` 清空缓存；空缓存 Q 不扣能量
- [x] 休憩 / 奖励界面冻结录制接口
- [ ] 运行时验证：高速移动 / 闪避 / 连续攻击下的轨迹还原度
- [ ] 录制开关的可视化提示（HUD 已显示时长 / 帧数）

### 回放

- [x] `PlaybackCursor`：正向、变速、循环、反向、重复时间戳
- [x] `ShadowEcho`：Transform 赋值、HP、攻击、闪避无敌、回池
- [x] `ShadowPool`：上限 10、Factory 注入、ReleaseAll
- [x] 双生回响：一次召唤 2 个，偏移显示
- [x] 残影存续：循环回放
- [x] 共生反噬：影子误伤本体
- [ ] 运行时验证：120 FPS / 30 FPS 下回放位置误差
- [ ] 影子轨迹预览虚线（规格可选增强）
- [ ] 影子死亡特效 / 召唤音效

**验收**：录一段“移动 + 攻击 + 闪避”，Q 后影子在相同时间点出现在相同位置并命中相同目标；误差在 1 帧内。

---

## M2 · 战斗、敌人与 Boss

目标：把灰盒假人升级为完整战斗层。

### 敌人

- [x] `EnemyBase`：HP / 伤害 / 目标优先级 / 死亡事件
- [x] 近战杂兵追击与攻击
- [x] 蚀影怪 `PreferShadow`，击杀影子扣 15 能量
- [ ] 远程杂兵 + 投射物对象池（规格 8.2）
- [ ] `EnemyData` ScriptableObject 配置
- [ ] 敌人受击 / 死亡 / 攻击前摇表现
- [ ] 敌人对象池（规格 12）

### Boss

- [ ] `BossController`：两阶段（50% 血量切换）
- [ ] 一阶段【扭曲力场】：`ShadowEcho.PlaybackSpeedMultiplier = 0.5`
- [ ] 二阶段随机时序反转：`SetPlaybackDirection(true/false)` + 持续时间
- [ ] Boss 专属攻击 / 弹幕 / 召唤小怪
- [ ] Boss 死亡掉落遗迹残片 + 开传送门

### 战斗规则

- [x] `IDamageable` / `DamageInfo` / 伤害来源
- [x] 闪避无敌帧
- [x] 暴击 + 标记（【掠影打击】）
- [x] 替影承伤 25% 转移
- [ ] 伤害数字 / 屏幕震动 / 命中停顿
- [ ] 玩家受击无敌帧（当前只有闪避无敌）

**验收**：5 层 Boss 均可击杀；扭曲力场下影子回放半速且时间戳不漂移；蚀影怪击杀影子扣能量。

---

## M3 · 地牢、房间与 5 层流程

目标：从“波次循环”升级为规格 7 的完整一局。

- [x] `DungeonGenerator`：5 层数据、战斗 / 休憩 / 事件 / Boss 节点
- [x] 同种子确定性 + 遗物池顺序无关
- [x] `RunStateMachine`：合法状态转换
- [x] `RunContext`：种子、层数、局内失效
- [ ] `GameFlow`：主菜单 → 选角色 → 5 层 → 结算
- [ ] 房间 Prefab / Tilemap 拼接与相机切换
- [ ] 战斗房清怪 → 锁房 → 三选一 → 开门
- [ ] 分支房二选一：休憩（回 30% HP、冻结录制）/ 事件
- [ ] 事件 A：献祭 30% HP 获得稀有扭曲回响（HP 不足禁用）
- [ ] 事件 B：献祭 1 件遗物，重新三选一（无遗物禁用）
- [ ] 传送门 → 下一层；第 5 层通关结算
- [ ] 房间卸载回收实体到对象池

**验收**：从主菜单开始，可完整通关 5 层；死亡后回据点；同种子两次跑图布局一致。

---

## M4 · 遗物、角色与局外成长

- [x] 9 件 MVP 遗物效果
- [x] 8 件携带上限、去重、三选一抽取
- [x] 2 个角色模板（追猎者 / 守御者）
- [x] 局外解锁只影响内容池，不影响局内数值
- [ ] 遗物 `.asset` 化 + 图标
- [ ] 稀有度 / 扭曲回响池权重
- [ ] 事件 A 的“稀有扭曲回响”筛选
- [ ] 据点：残片消费、遗物 / 角色解锁 UI
- [ ] 遗物图鉴 / 描述本地化

**验收**：所有遗物在实机中效果与描述一致；解锁只改变可用池；8 件上限有明确 UI 提示。

---

## M5 · UGUI、存档与结算

- [x] `SaveData` / `SaveSystem`：JSON、备份、损坏回退
- [x] `MetaProgress`：残片、解锁、自动保存
- [ ] UGUI：HP、能量、遗物栏（悬浮描述）、三选一、事件弹窗、结算
- [ ] 主菜单 / 角色选择 / 暂停菜单
- [ ] 结算：本局结果、残片、返回据点
- [ ] 提示文本：能量不足、缓存为空、遗物上限
- [ ] 存档版本迁移与云存档预留

**验收**：规格 10 的 7 项 UI 全部可用；死亡 / 退出 / 重开不丢局外进度。

---

## M6 · 验收、性能与打包

- [ ] 对照规格 15 的 8 条验收标准逐条打勾
- [ ] 120 FPS 目标机性能采样（影子 ×10、敌人 ×30、投射物 ×50）
- [ ] GC 分配检查：录制 / 回放每帧 0 新分配（目标）
- [ ] 对象池覆盖率：影子 / 敌人 / 投射物
- [ ] Windows x64 构建、分辨率 / 输入配置
- [ ] 30 分钟稳定性测试
- [ ] 已知问题清单与发布说明

**验收**：规格 15 全部满足；构建包可独立运行；无阻塞性 Bug。

---

## 规格追溯表

| 规格章节 | 对应实现 | 状态 |
| --- | --- | --- |
| 1 项目概述 & 约束 | `GameConfig` / 架构文档 / 设计约束 | ✅ 约束已编码 |
| 2 全局流程 & 状态机 | `RunStateMachine` / `RunContext` | 🟡 状态机完成，GameFlow 待 M3 |
| 3 玩家本体 | `PlayerController` / `PlayerStatus` | 🟡 核心完成，表现待 M2 |
| 4 录制 & 影子回放 | `RecordBuffer` / `PlaybackCursor` / `ShadowEcho` / `ShadowPool` | 🟡 代码完成，待实机验证 |
| 5 回响能量 | `EchoEnergy` | ✅ |
| 6 遗物 | `RelicData` / `RelicManager` / `RelicStats` / 9 效果 | ✅ 效果完成，资产 / UI 待 M4 |
| 7 地牢 & 房间 | `DungeonGenerator` / `DungeonLayout` | 🟡 数据完成，房间表现待 M3 |
| 8 敌人 & Boss | `EnemyBase` | 🟡 近战 / 蚀影怪完成，远程 / Boss 待 M2 |
| 9 战斗 & 伤害 | `IDamageable` / `DamageInfo` | 🟡 核心完成，反馈待 M2 |
| 10 UI | `EchoLabHud`（IMGUI） | 🟡 调试 HUD 完成，UGUI 待 M5 |
| 11 存档 | `SaveSystem` / `SaveData` / `MetaProgress` | ✅ 核心完成，UI 待 M5 |
| 12 资源管理 | `ShadowPool` | 🟡 影子池完成，敌人 / 投射物待 M2 |
| 13 配置表 | `CharacterData` / `RelicData` | 🟡 部分完成，EnemyData / RoomConfig 待 M2 |
| 14 异常边界 | 见实现状态文档 | 🟡 大部分已覆盖 |
| 15 验收标准 | 见 M6 | ⏳ |
| 16 风险清单 | 架构文档 / 开发指南 | ✅ 已落成约束 |
| 17 后续路线 | 本文档 | ✅ |

---

## 建议的下一批 Issue（按优先级）

1. **在 Unity 2022 中跑通编译与 EditMode 测试**（M0 收尾，最高优先级）
2. 录制 / 回放实机误差测量（M1 验收）
3. 远程杂兵 + 投射物池（M2）
4. `BossController` 两阶段 + 扭曲力场（M2）
5. `GameFlow` 五层房间流程（M3）
6. 事件房 A/B 逻辑（M3）
7. UGUI HUD 替换 IMGUI（M5）
8. 首次打开后提交 `.meta` 与 `Main.unity`（M0）
