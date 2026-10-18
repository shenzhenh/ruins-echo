# ADR-0002：技术切片使用运行时灰盒 + IMGUI，而不是场景 / Prefab / UGUI

- 状态：已采纳（临时）
- 日期：2026-10-18
- 相关：`EchoLabBootstrap`、`GreyboxFactory`、`EchoLabHud`、路线图 M3 / M5

## 背景

MVP 需要先验证最高风险的机制：

> 录制 3 秒动作 → Q 召唤 → 影子按时间戳回放 → 遗物改变影子行为。

如果一开始就制作 Tilemap 房间、Prefab、UGUI、动画、特效，会把这些风险埋在一堆资产依赖下面；而且在没有美术资源、没有 Unity 2022 编辑器的环境里，无法快速迭代场景。

## 决策

M1 技术切片：

- 不依赖任何 `.unity` / `.prefab` / 图片资源；`EchoLabBootstrap` 在运行时生成相机、地面、玩家、敌人、影子。
- HUD 使用 IMGUI（`OnGUI`），只展示调试必需信息。
- 用纯色 Sprite + 排序层级表达实体；受击用颜色闪烁。
- 房间流程退化为“波次循环 + 三选一”，但录制 / 回放 / 遗物 / 能量 / 存档 / 状态机按正式架构实现。

## 后果

正面：

- 克隆仓库后按 Play 就能验证核心循环，无需场景配置。
- 没有资产 GUID / 序列化兼容问题；代码审查聚焦逻辑。
- 后续替换表现层时，核心模块不需要改。

负面：

- 不是最终画面；不能用于验收规格 10 的 UI。
- `GreyboxFactory` 生成 Sprite 有运行时开销（一次性）。
- IMGUI 不支持正式本地化 / 布局 / 动效。
- 需要在 M3 / M5 明确移除或替换，避免技术债固化。

## 退出条件

- M3：房间 / 敌人 / Boss 接入 Prefab 或 Tilemap，`EchoLabBootstrap` 退化为调试场景。
- M5：UGUI 替换 `EchoLabHud`；IMGUI 仅保留为开发者调试开关。
- 在此之前，不允许把玩法逻辑写进 `Presentation`；核心规则必须在 Runtime 模块内可测试。
