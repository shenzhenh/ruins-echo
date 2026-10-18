# ADR-0001：使用自研确定性 RNG，而不是 `UnityEngine.Random.State`

- 状态：已采纳
- 日期：2026-10-18
- 相关：规格 1.2 / 7.3 / 16，`DeterministicRandom`、`DungeonGenerator`

## 背景

规格要求：

> 每一局开局生成一个随机 `UInt32` 种子；相同种子，房间布局、敌人分布、遗物掉落顺序完全一致。

规格同时建议使用 `Random.State` 固定随机序列。`UnityEngine.Random` 是全局状态，保存 / 恢复 `Random.State` 可以实现同种子复现，但有两个问题：

1. **跨版本不保证稳定**：Unity 没有承诺 `UnityEngine.Random` 的算法在版本间不变。升级编辑器可能导致同种子生成不同地图，破坏种子分享 / 每日挑战 / 回放。
2. **全局状态易被污染**：任何第三方脚本或表现层调用 `Random.Range` 都会改变序列；在异步加载、对象池、特效抖动等场景下很难保证生成顺序。

## 决策

实现一个纯 C# 的 `DeterministicRandom`（SplitMix64），所有地牢 / 掉落 / 敌人分布使用它；`UnityEngine.Random` 仅允许用于纯表现抖动（如【动作乱流】的视觉偏移），不得参与任何影响玩法的决策。

接口：

```csharp
var rng = new DeterministicRandom(seed);
uint value = rng.NextUInt();
int index = rng.Range(0, list.Count);
float chance = rng.NextFloat();
rng.Shuffle(list);
```

## 备选方案

| 方案 | 结论 |
| --- | --- |
| `UnityEngine.Random.InitState(seed)` | 简单，但全局状态、跨版本不稳定；否 |
| `System.Random` | 算法在 .NET 版本间也可能变化，且不同 Unity 运行时行为不完全一致；否 |
| PCG32 | 质量好，但实现比 SplitMix64 略复杂；可作为后续替换 |
| SplitMix64 | 实现小、速度快、统计质量足够、无外部依赖；选它 |

## 后果

正面：

- 同种子在 Windows / Mac / 不同 Unity 版本上一致（只要算法不改）。
- 不依赖全局状态；生成器可以并行 / 嵌套使用。
- 可以写固定测试向量，防止误改算法。

负面：

- 需要自己维护算法；用测试向量锁定（`DeterministicRandomTests.KnownSequence_IsStableAcrossVersions`）。
- 不是密码学安全随机；游戏不需要。
- 修改算法会导致旧种子地图变化；如需升级，必须新增版本字段并保留旧算法。

## 约束

- 地牢、敌人、遗物三选一、事件选择只用 `DeterministicRandom`。
- 表现层抖动可以用 `UnityEngine.Random`，但不得影响命中 / 伤害 / 掉落。
- 新增随机调用时，注意调用顺序本身也是种子的一部分；不要在不同帧率下改变生成顺序。
