# 《遗迹回响：影子共生》单机 MVP 开发规格文档

- 文档版本：v1.0
- 日期：2026-10-18
- 项目：2D 俯视角动作肉鸽 · Unity C# 单机原型
- 目标读者：后端/客户端开发者、AI 编码助手
- 文档目的：完整定义游戏所有模块、数据结构、接口、状态流转、规则、边界条件，开发者可直接依据这份文档实现 MVP。

> **范围说明**：仅单机 MVP，联机相关逻辑不在本版本开发范围，仅预留未来扩展接口。
>
> **核心一句话**：玩家持续录制自身最近 3 秒动作；消耗回响能量召唤影子分身，影子原样回放录制的动作序列；局内拾取遗物修改影子行为规则；程序生成地牢爬层闯关；角色死亡时本局所有局内数据全部销毁，局外解锁进度持久保存。

---

## 目录

1. 项目概述 & 设计约束
2. 游戏全局流程 & 状态机
3. 玩家本体系统
4. 动作录制与影子回放系统（核心模块）
5. 回响能量系统
6. 遗物系统
7. 地牢、房间生成与关卡流程
8. 敌人与 Boss 系统
9. 战斗与伤害规则
10. UI、提示与视觉反馈
11. 存档（局外永久进度）
12. 资源管理与对象池
13. 配置表设计
14. 异常、边界规则
15. MVP 验收标准
16. 风险清单与开发注意事项
17. 后续迭代路线

---

## 1. 项目概述 & 设计约束

### 1.1 游戏基础信息

| 项目 | 内容 |
| --- | --- |
| 视角 | 2D 俯视角（Top-down 2D） |
| 引擎 | Unity 2022 LTS + C# |
| 目标平台 | Windows PC（MVP 原型） |
| 美术 | 单色占位 Sprite / Tilemap 即可，无需美术资源，程序原型优先 |
| 一局时长预期 | 8–12 分钟 |

### 1.2 硬性设计约束（不可违背）

1. **局内 / 局外数据严格隔离**
   - 局内数据：本局玩家血量、回响能量、动作缓存、影子实例、本局遗物列表、房间敌人、本局种子。**玩家死亡 → 全部局内数据销毁，不保存**。
   - 局外数据：遗迹残片、解锁角色 ID 列表、解锁遗物 ID 列表。持久化 JSON 保存，死亡 / 退出游戏不丢失。
   - ❗局外解锁**仅解锁内容池选项，不提供任何局内固定数值加成**，保证肉鸽随机性。
2. **种子驱动地图生成**
   每一局开局生成一个随机 `UInt32` 种子；相同种子，房间布局、敌人分布、遗物掉落顺序完全一致。单机本地生成地图，无网络依赖。
3. **影子回放禁止使用 Rigidbody2D 驱动位移**
   影子移动直接赋值 Transform.position / rotation。碰撞体仅用于伤害检测，不参与移动物理计算。防止回放轨迹漂移、碰撞干扰导致动作错位。
4. **时间戳统一使用 UnscaledTime**
   所有录制帧时间戳、回放时间轴基于 `Time.unscaledTime`，不受 `Time.timeScale`（Boss 扭曲力场、慢动作）影响。
5. 遗物以**修改影子行为规则为核心**，少量修改本体属性；禁止单纯数值堆砌类遗物。

---

## 2. 游戏全局流程 & 状态机

### 2.1 完整一局流程

主菜单 → 选择角色 → 开始新对局（生成随机种子） → 加载第 1 层地牢 → 房间循环（战斗房 / 分支房） → Boss 房 → 击败 Boss → 开启下一层传送门 → 直到第 5 层 Boss 击杀 → 通关结算。

- 通关结算：发放遗迹残片，保存局外进度，返回主据点。
- 玩家血量归零：死亡结算，本局所有局内数据清空，返回主据点。

### 2.2 GameState 枚举定义

```csharp
public enum GameState
{
    MainMenu,        // 主菜单
    RunPreparation,  // 本局初始化，生成种子，加载地图
    InRoomIdle,      // 房间内，无战斗
    InCombat,        // 战斗中，敌人存活
    RoomReward,      // 战斗结束，遗物三选一界面
    EventDialog,     // 事件房弹窗交互
    BossFight,       // Boss 战专用状态
    Victory,         // 本局胜利
    Death            // 本局失败
}
```

状态切换规则：

- `InCombat`：房间内存在存活敌人。
- 敌人全部死亡 → 切换 `RoomReward`，锁定房间，禁止新敌人刷新。
- `RoomReward` 确认选择遗物 → 返回 `InRoomIdle`。
- 进入事件房 → 切换 `EventDialog`，暂停游戏输入。
- `Victory / Death`：销毁所有局内实体（玩家、影子、敌人、房间对象），执行结算，清理动作缓存与遗物列表。

---

## 3. 玩家本体系统

### 3.1 PC 输入映射

| 按键 | 功能 | 备注 |
| --- | --- | --- |
| W / A / S / D | 玩家移动 | 2D 平面移动 |
| 鼠标 | 瞄准方向 | 角色朝向跟随鼠标世界位置 |
| 鼠标左键 | 普攻 | 按下触发一次攻击判定 |
| 空格 | 闪避冲刺 | 短距离位移，带无敌帧，有冷却 |
| Q | 召唤影子回放 | 消耗回响能量，读取当前动作缓存 |
| R | 清空动作缓存 | 清空全部 RecordFrame，无消耗 |
| ESC | 暂停菜单 | 全局暂停 |

### 3.2 玩家局内基础属性（可被遗物修改）

| 属性 | 基础值 | 说明 |
| --- | --- | --- |
| MaxHP | 100 | 最大血量 |
| HP | 100 | 当前血量，归零触发死亡 |
| MoveSpeed | 4.5 | 每秒移动距离 |
| AttackDamage | 12 | 普攻基础伤害 |
| DodgeDistance | 2.2 | 闪避冲刺距离 |
| DodgeDuration | 0.2f | 闪避位移持续时间 |
| DodgeInvincibleTime | 0.35f | 闪避无敌帧时长 |
| DodgeCooldown | 1.8f | 闪避冷却 |

### 3.3 玩家组件结构

- `PlayerController.cs`：输入、移动、朝向、普攻、闪避逻辑
- `PlayerStatus.cs`：血量、局内属性、接收伤害、死亡事件
- `PlayerRecorder.cs`：动作录制模块（和 PlayerController 绑定）
- `EchoEnergy.cs`：回响能量管理

### 3.4 普攻规则

- 普攻：扇形 / 圆形 2D Trigger 碰撞检测，命中敌人造成伤害。
- 普攻有前摇 0.15s，后摇 0.2s；普攻期间可以移动，不能取消普攻。
- 普攻命中触发命中事件（用于遗物监听：如影子命中减 CD 等）。

### 3.5 闪避规则

- 闪避：瞬时位移，无敌帧，闪避期间不会记录受到的伤害。
- 闪避会被录制进动作缓存：回放时影子会执行一模一样的冲刺。
- 闪避有冷却，冷却期间不可再次释放。

---

## 4. 动作录制与影子回放系统（核心模块）

### 4.1 数据结构定义

```csharp
/// <summary>
/// 每一帧录制快照，保存玩家某一瞬间状态
/// </summary>
public struct RecordFrame
{
    public float unscaledTimestamp;    // Time.unscaledTime
    public Vector2 worldPosition;      // 世界坐标
    public float rotation;             // Z 轴旋转角度（朝向）
    public bool triggerAttack;         // 本帧是否触发普攻
    public bool triggerDodge;          // 本帧是否触发闪避
}
```

```csharp
/// <summary>
/// 动作录制管理器，挂载在 Player 上
/// </summary>
public class PlayerRecorder
{
    public List<RecordFrame> recordBuffer;
    public const float MaxRecordDuration = 3f; // 最大缓存时长 3 秒

    // 每帧调用，追加快照
    public void RecordFrame(PlayerController player);

    // 清理旧帧：超过 3 秒的最早帧删除
    public void TrimOldFrames();

    // R 按键：清空全部缓存
    public void ClearBuffer();

    // 获取当前缓存副本，交给影子回放
    public List<RecordFrame> GetBufferCopy();

    // 判断缓存是否为空
    public bool HasValidBuffer();
}
```

#### 录制逻辑细节

1. 玩家存活时，**每帧持续录制**，录制不可关闭。
2. 每帧新增 `RecordFrame` 加入 `recordBuffer`。
3. 每帧执行 `TrimOldFrames()`：计算最新帧时间戳 - 最旧帧时间戳，如果 > 3 秒，循环移除列表头部旧帧。维持滑动窗口 3 秒。
4. R 按键调用 `ClearBuffer()` 清空整个 List。
5. 缓存为空时按下 Q：UI 提示【没有录制动作，无法召唤影子】，不消耗能量。

### 4.2 影子组件 `ShadowEcho.cs`

影子由**对象池**生成 / 回收，禁止直接 Instantiate / Destroy。

```csharp
public class ShadowEcho : MonoBehaviour
{
    public List<RecordFrame> playbackFrames;
    public float playbackStartTime;
    public float HP;            // 影子血量
    public bool isPlaying;

    // 初始化影子，传入录制帧列表
    public void Setup(List<RecordFrame> frames, float inheritedDamage);

    // 开始回放动作序列
    public void StartPlayback();

    // 每帧驱动回放逻辑
    public void TickPlayback();

    // 影子受到伤害
    public void TakeDamage(float damage);

    // 回放结束 / 影子死亡，回收回对象池
    public void ReturnToPool();
}
```

#### 影子回放规则（重点）

1. `Setup` 时复制玩家本局普攻伤害倍率，继承所有遗物效果。
2. 回放开始：`playbackStartTime = Time.unscaledTime`。
3. 每帧 Tick：计算当前回放时间 `currentPlayTime = Time.unscaledTime - playbackStartTime`。
4. 遍历 `playbackFrames`，找到所有时间戳 ≤ currentPlayTime 且未执行的帧：
   - `transform.position = frame.worldPosition`
   - `transform.rotation = Quaternion.Euler(0, 0, frame.rotation)`
   - 如果 `triggerAttack = true`：影子执行一次普攻判定（和玩家普攻逻辑一致）
   - 如果 `triggerDodge = true`：影子执行闪避位移
5. 回放至最后一帧：默认影子停止，调用 `ReturnToPool()`。遗物可修改为循环回放。
6. 影子拥有独立血量，敌人可攻击影子。影子 HP 归零 → 立刻终止回放，回池。
7. **影子没有 AI，不会自动寻路追踪敌人，只会死板回放录制动作。**

### 4.3 影子对象池 `ShadowObjectPool.cs`

- 预实例化最多 10 个影子实例（MVP 足够）。
- `Get()`：从池取可用影子，激活，调用 Setup。
- `Release()`：关闭 GameObject，重置所有状态，放回池内。
- 目的：减少 GC，避免频繁创建销毁。

### 4.4 Q 键召唤影子完整流程

1. 检测 `PlayerRecorder.HasValidBuffer()`；无缓存 → UI 提示，终止。
2. 读取回响能量，判断能量 ≥ 召唤消耗（默认 20）；不足 → UI 提示。
3. 消耗回响能量。
4. 从对象池获取影子实例。
5. 获取 `recordBuffer` 副本，传入影子 Setup。
6. 影子调用 `StartPlayback()` 开始回放。

---

## 5. 回响能量系统

```csharp
public class EchoEnergy
{
    public float MaxEnergy = 100f;
    public float CurrentEnergy;
    public float EnergyRegenPerSecond = 8f;
    public float BaseSummonCost = 20f;

    public void TickRegen(float deltaTime);
    public bool TryConsumeEnergy(float amount);
}
```

规则：

1. 能量范围 0 ~ MaxEnergy，不能溢出上限。
2. 每帧自动恢复能量（乘遗物修改系数）。
3. `TryConsumeEnergy(amount)`：当前能量 ≥ amount → 扣除，返回 true；否则返回 false。
4. 遗物可以修改：能量恢复速率、召唤影子消耗。

---

## 6. 遗物系统（肉鸽构筑核心）

### 6.1 架构设计

使用 `ScriptableObject` 作为遗物配置资产，搭配接口模式，避免大量 if-else。

```csharp
public interface IRelicEffect
{
    void OnApply(RelicManager manager);
    void OnRemove(RelicManager manager);
}

[CreateAssetMenu]
public class RelicData : ScriptableObject
{
    public string relicId;
    public string name;
    public string description;
    public bool isTwisted; // true = 扭曲回响（带负面）
    public List<IRelicEffect> effects;
}

public class RelicManager : MonoBehaviour
{
    public List<RelicData> activeRelics; // 当前本局携带遗物
    public int MaxRelicCount = 8;

    public bool AddRelic(RelicData relic);
    public void RemoveRelic(RelicData relic);
    public void ClearAllRelics(); // 本局结束清空
}
```

- 一局最多携带 8 个遗物。达到上限后，不能再拾取新遗物。
- 房间清怪完成，弹出遗物三选一（从本局可用遗物池随机抽取 3 个）。
- 遗物分为两类：增益回响、扭曲回响（正面 + 负面代价）。

### 6.2 MVP 内置遗物列表

#### 增益回响（isTwisted = false）

1. **残影存续**：影子回放结束后不销毁，循环重复播放动作序列直到影子血量耗尽。
2. **双生回响**：Q 释放一次，生成 2 个影子；召唤能量消耗 ×2。
3. **替影承伤**：敌人 AI 优先锁定影子；影子可以替本体承受伤害。
4. **回响传导**：影子命中敌人，玩家普攻冷却缩短 0.3 秒。
5. **掠影打击**：影子暴击命中敌人，给目标挂标记；本体对标记目标伤害 +30%。

#### 扭曲回响（isTwisted = true，收益 + 负面）

1. **共生反噬**：影子攻击判定会误伤本体；影子伤害 ×2。
2. **能量狂潮**：回响能量恢复速度 ×1.8；本体受到伤害 ×1.3。
3. **意志侵蚀**：影子最大 HP 大幅提升；玩家最大 HP 减少 40%。
4. **动作乱流**：影子回放位置每帧随机小幅偏移；影子攻击范围扩大 50%。

### 6.3 遗物生效逻辑

所有系统（玩家、影子、敌人 AI）每一回合 / 事件读取 RelicManager 内的 activeRelic 列表，叠加所有效果。
事件驱动：影子命中、玩家受伤、召唤影子等事件触发对应遗物回调。

---

## 7. 地牢、房间生成与关卡流程

### 7.1 关卡结构（MVP 共 5 层）

每层结构：战斗房 → 分支房选择（休憩密室 / 事件房二选一） → Boss 房。
击败 Boss，打开传送门进入下一层。第 5 层 Boss 为最终 Boss。

### 7.2 房间类型定义

1. **战斗房**
   - 根据种子生成敌人列表。
   - 所有敌人死亡 → 房间锁定，进入 `RoomReward` 状态，弹出遗物三选一。
2. **休憩密室（分支房）**
   - 玩家恢复 30% 最大 HP。
   - **本房间内动作缓存冻结，不再录制新动作**（PlayerRecorder 暂停追加帧；离开房间恢复录制）。
3. **事件房（分支房）**
   事件弹窗，二选一确认 / 取消。MVP 内置 2 个事件：
   - 事件 A：献祭当前 30% HP，随机获得一件稀有扭曲回响。HP 不足则无法选择。
   - 事件 B：献祭 1 件已携带遗物，重新生成遗物三选一。没有遗物时不可选。
4. **Boss 房**
   - 单独大房间，生成 Boss + 少量小怪。
   - Boss 自带【扭曲力场】光环：力场内影子回放速度降低 50%；高级 Boss 阶段可随机反转影子动作时序。
   - 击杀 Boss → 传送门开启。

### 7.3 地图生成器 `DungeonGenerator.cs`

- 输入 `uint seed`，使用 `Random.State` 固定随机序列。
- 房间是预制体 Tilemap，按顺序拼接房间节点。
- 输出房间实例、敌人生成位置、物品生成位置。
- 种子固定，所有生成结果固定。

---

## 8. 敌人与 Boss 系统

### 8.1 基础敌人基类 `EnemyBase.cs`

```csharp
public class EnemyBase : MonoBehaviour
{
    public float HP;
    public float Damage;
    public Transform target;
    public EnemyTargetPriority targetPriority;

    public void TickAI();
    public void TakeDamage(float damage);
    public void OnDeath();
}

public enum EnemyTargetPriority
{
    Default,        // 优先玩家本体
    PreferShadow    // 蚀影怪：优先寻找影子
}
```

目标选择规则：

- 如果存在遗物【替影承伤】：所有敌人优先锁定影子。
- 否则按 `targetPriority`：普通怪优先玩家；蚀影怪优先影子。
- 找不到目标时，敌人原地巡逻。

### 8.2 MVP 敌人种类

1. **近战杂兵**：追击目标，近身近战攻击。
2. **远程杂兵**：保持距离，发射投射物。投射物触发伤害检测。
3. **蚀影怪（精英）**：`PreferShadow`。影子被蚀影怪击杀时，玩家回响能量扣除 15 点。

### 8.3 Boss 基础规则

每层 Boss 拥有 2 阶段血量。血量下降到 50% 进入二阶段。

- 一阶段：基础攻击；开启【扭曲力场】，影子回放速度 ×0.5。
- 二阶段：新增攻击；随机触发时序反转：影子回放时间轴反向短暂播放。
- Boss 死亡：掉落遗迹残片，开启传送门。

---

## 9. 战斗与伤害规则

1. 伤害计算：基础伤害 × 所有遗物伤害系数叠加。
2. 伤害事件：对象收到伤害触发回调（用于遗物监听）。
3. 无敌帧：闪避无敌帧期间，忽略所有伤害。
4. 影子伤害判定：影子攻击使用 Trigger2D 重叠检测，和玩家普攻逻辑一致。
5. 影子伤害本体：仅当遗物【共生反噬】生效时开启影子对本体的伤害层。
6. 死亡判定：HP ≤ 0 → 触发死亡事件，销毁实体。

---

## 10. UI、提示与视觉反馈

所有 UI 使用 Unity UGUI。MVP 最小 UI 集：

1. 左上角：玩家 HP 血条
2. 顶部：回响能量条
3. 右下角：遗物图标列表；鼠标悬浮显示遗物名称 + 描述。
4. 遗物三选一弹窗：3 个遗物卡片，点击选择。最多携带 8 件提示。
5. 事件房弹窗：文本描述、代价预览、确认 / 取消按钮。
6. 结算界面：本局结果、获得遗迹残片、返回据点按钮。
7. 提示文本（临时弹出）：能量不足、缓存为空、无法拾取更多遗物。

> 可选增强项（MVP 非必须）：回放轨迹预览虚线（按下 Q 前预览影子将要走的路径）。

---

## 11. 存档（局外永久进度）

### 11.1 SaveData.json 结构

```json
{
  "fragments": 0,
  "unlockedRelicIds": ["relic_01", "relic_02"],
  "unlockedCharacterIds": ["char_hunter"]
}
```

- `fragments`：遗迹残片，通关 / Boss 击杀获得，用于据点解锁新遗物池、角色。
- `unlockedRelicIds`：已解锁遗物 ID 列表，本局遗物池只能从这里抽取。
- `unlockedCharacterIds`：可选择角色模板。

### 11.2 角色模板（MVP 2 个）

1. **追猎者**：基础录制缓存上限 +0.5 秒；影子基础 HP 减少 20%。
2. **守御者**：影子承伤效率提升 25%；回响能量恢复速度 -20%。

### 11.3 存档读写

`SaveSystem.cs`：JSON 序列化 / 反序列化，读写 `SaveData.json` 到 PersistentDataPath。

- 进入游戏加载存档；
- 获得遗迹残片 / 解锁内容时，自动写入存档。

---

## 12. 资源管理与对象池

需要对象池：影子对象池、敌人对象池、投射物对象池。

- 所有频繁生成销毁的实体必须走对象池，减少 GC。
- 房间卸载时，回收所有房间实体回对应对象池。

---

## 13. 配置表设计

MVP 全部配置放在 ScriptableObject，不需要外部 Excel。

- `RelicData`：遗物配置
- `CharacterData`：角色模板配置
- `EnemyData`：敌人属性配置
- `RoomConfig`：房间敌人生成配置

---

## 14. 异常、边界规则

1. 回放中影子被击杀：立刻终止回放，影子回池，不再继续执行剩余动作帧。
2. 玩家死亡：所有局内实体销毁，缓存清空，遗物列表清空。局外存档保留。
3. 缓存为空按下 Q：不扣能量，UI 提示。
4. 遗物达到 8 个上限：三选一界面提示上限，无法拾取。
5. 房间战斗期间禁止进入事件 / 休憩房。
6. 时间缩放（Boss 扭曲力场）不影响录制帧时间戳，回放时序正确。
7. 影子脱离房间边界：不自动销毁，继续回放直到结束 / 被击杀。

---

## 15. MVP 验收标准

全部满足，代表原型完成：

1. WASD 移动，鼠标瞄准，左键普攻，空格闪避；R 清空缓存，Q 召唤影子。
2. 玩家持续录制最多 3 秒动作缓存，影子回放录制动作；影子可被敌人击杀。
3. 击败房间敌人，三选一遗物；遗物效果正确生效，修改影子行为 / 属性。
4. 敌人 AI：普通怪攻击本体，蚀影怪优先攻击影子。
5. 5 层地牢，每层 Boss 带有扭曲力场机制。
6. 玩家死亡，本局所有局内数据销毁；局外进度 JSON 持久化保存。
7. 可完整从头通关一局，结算发放遗迹残片。
8. 所有影子回放基于 Transform 赋值，不使用 Rigidbody 驱动位移。

---

## 16. 风险清单与开发注意事项

1. ⚠️ **影子回放千万不要用 Rigidbody2D 移动**，会造成回放轨迹漂移。碰撞体仅用于伤害检测。
2. ⚠️ 录制帧时间戳必须使用 `Time.unscaledTime`，Boss 时间扭曲会破坏 `Time.time`。
3. ⚠️ 影子大量生成必须使用对象池，否则 GC 卡顿。
4. ⚠️ 遗物使用接口模式，后续新增遗物只需要新建 ScriptableObject，不要写大量 if-else。
5. ⚠️ 种子随机必须固定 `Random.State`，保证同种子地图 / 敌人 / 掉落完全一致。
6. ⚠️ 区分局内 / 局外数据，严禁局内数据写入永久存档。

---

## 17. 后续迭代路线

- **V1.1**：新增商店房间、更多遗物、敌人种类、粒子特效、轨迹预览。
- **V1.2**：美术优化、音效、新手引导关卡。
- **V2.0**：联机模块扩展（复用录制回放核心，种子同步地图）。

---

## 使用说明

- 文档可直接复制保存为 `遗迹回响_影子共生_单机MVP开发规格文档.md`。
- 文件编码推荐 UTF-8，可直接给 AI / 开发者阅读，作为开发依据。
