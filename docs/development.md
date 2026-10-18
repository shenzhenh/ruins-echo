# 开发指南

## 1. 环境

| 工具 | 版本 |
| --- | --- |
| 引擎（推荐） | **团结引擎 Tuanjie 1.10.4**（基于 Unity 2022.3.62t16） |
| 对照 / CI 引擎 | Unity 2022.3.62f1（国际版；仅用于可选的 game-ci 工作流） |
| 脚本运行时 | .NET Standard 2.1（引擎默认） |
| IDE | Rider / Visual Studio 2022 / VS Code（推荐 Rider） |
| Git | 2.40+ |
| 平台 | Windows 10/11（MVP 目标平台） |

不需要安装任何第三方包；`Packages/manifest.json` 只使用 Unity 官方包与内置模块。Tuanjie 1.10 以 Unity 2022 LTS 为基础，现有 API / 包按 Unity 2022.3 的用法即可。

---

## 2. 首次打开

1. Tuanjie Hub → 添加项目 → 选择 `ruins-echo/`。
2. 用 **团结引擎 1.10.4（2022.3.62t16）** 打开，等待 Package Manager 解析与脚本编译。
3. 打开 `Window → General → Test Runner`，跑一遍 **EditMode** 测试。
4. 可选：`Tools → Ruins Echo → Create Main Scene` 生成 `Assets/Scenes/Main.unity` 并加入 Build Settings。
5. 按 **Play**。如果当前是空场景，`EchoLabBootstrap` 会自动搭建灰盒场景。

> 本仓库已包含手写的 `.meta` 文件，用于固定资源 GUID。**不要删除 `.meta`，也不要手工修改其中的 `guid`**。Unity 首次导入后可能会补充 importer 设置，属于正常变化，提交即可。

### 输入系统

工程使用旧版 Input Manager：

- `WASD` → `Horizontal / Vertical`
- 鼠标位置 / 左键
- `Space / Q / R / 1 / 2 / 3 / Enter`

如果 Player Settings 的 **Active Input Handling** 被改成 `Input System Package (New)`，legacy `Input` 会抛异常。请保持 `Input Manager (Old)` 或 `Both`。当前 manifest 不包含 Input System 包，默认即为旧版。

---

## 3. 运行测试

### 编辑器内

`Window → General → Test Runner → EditMode → Run All`

测试程序集：`Assets/RuinsEcho/Tests/EditMode/RuinsEcho.Tests.EditMode.asmdef`，引用 `RuinsEcho.Runtime`、`UnityEngine.TestRunner`、`UnityEditor.TestRunner`，定义约束 `UNITY_INCLUDE_TESTS`。

### 命令行（Tuanjie 1.10.4）

```powershell
$editor = "C:\Program Files\Tuanjie\Hub\Editor\2022.3.62t16\Editor\Tuanjie.exe"
# 如果安装在其他盘，请换成实际路径；可执行文件名也可能是 Tuanjie.exe
& $editor `
  -batchmode -nographics `
  -projectPath "D:\DeepseekWorkspace\ruins-echo" `
  -runTests -testPlatform EditMode `
  -testResults "D:\DeepseekWorkspace\ruins-echo\TestResults\editmode.xml" `
  -logFile "D:\DeepseekWorkspace\ruins-echo\TestResults\editmode.log" `
  -quit
```

Tuanjie 基于 Unity 2022.3，测试参数与 Unity 一致。退出码 `0` 表示测试进程成功；具体用例结果看 XML / log。`TestResults/` 已在 `.gitignore` 中忽略。

### 当前覆盖

| 测试文件 | 覆盖 |
| --- | --- |
| `RecordBufferTests` | 3 秒裁剪、乱序拒绝、副本独立、动态窗口 |
| `PlaybackCursorTests` | 正向、半速、循环、反向、空缓存 |
| `EchoEnergyTests` | 恢复、上限、消耗、扣除、重开 |
| `RelicStatsTests` | 9 件遗物、上限 8、去重、移除重算 |
| `DungeonGeneratorTests` | 5 层、确定性、不同种子、池顺序无关、最终 Boss |
| `SaveDataTests` | 默认解锁、JSON 往返、落盘、损坏回退 |
| `RunStateMachineTests` | 合法 / 非法转换、死亡、RunContext 失效 |

---

## 4. 代码约定

### 目录 / 命名空间

| 目录 | 命名空间 |
| --- | --- |
| `Runtime/Core` | `RuinsEcho.Core` |
| `Runtime/Recording` | `RuinsEcho.Recording` |
| `Runtime/Player` | `RuinsEcho.Player` |
| `Runtime/Relics` | `RuinsEcho.Relics` |
| `Runtime/Dungeon` | `RuinsEcho.Dungeon` |
| `Runtime/Enemies` | `RuinsEcho.Enemies` |
| `Runtime/Combat` | `RuinsEcho.Combat` |
| `Runtime/Save` | `RuinsEcho.Save` |
| `Runtime/Spawning` | `RuinsEcho.Spawning` |
| `Runtime/Presentation` | `RuinsEcho.Presentation` |
| `Editor` | `RuinsEcho.EditorTools` |
| `Tests/EditMode` | `RuinsEcho.Tests` |

### 硬性规则

1. **影子位移只写 Transform**，禁止 `Rigidbody2D.velocity / MovePosition / AddForce`。
2. **录制 / 回放时间戳只用 `Time.unscaledTime`**；不要混入 `Time.time`。
3. **局内数据禁止写入 `SaveData`**；结算只写残片与解锁 ID。
4. **遗物不写 if-else**：新效果走 `IRelicEffect` + `RelicStats`。
5. **同种子必须可复现**：地牢 / 掉落只用 `DeterministicRandom`，不要用 `UnityEngine.Random`（表现层抖动除外）。
6. **频繁生成实体走对象池**；影子池已实现，敌人 / 投射物在 M2 接入。
7. 新增 `.cs` 必须放在对应 asmdef 目录内；Editor 代码不得进入 Runtime。

### C# 风格

- 私有字段 `_camelCase`；公共字段 / 属性 `PascalCase`。
- 一个文件一个公共类型（效果类可按主题合并，如 `MvpRelicEffects.cs`）。
- 事件用 `event Action<...>`；跨系统事件放 `CombatEvents`，不要到处找引用。
- `[SerializeField]` 只给需要在 Inspector 调的字段；运行时注入用 `Initialize(...)`。
- 注释写“为什么”，不写“是什么”；中文注释可以。
- 不引入 `FindObjectOfType` 做热路径查找；Bootstrap 注入引用。

---

## 5. Git 工作流

### 分支

- `main`：可运行、测试通过。
- `feat/<scope>` / `fix/<scope>` / `docs/<scope>`：短生命周期分支。
- 合并前跑 EditMode 测试 + `Tools → Ruins Echo → Validate Project`。

### 提交信息

使用 Conventional Commits，scope 用模块名：

```text
feat(recording): add reverse playback cursor
fix(shadow): remove duplicate friendly-fire hit
docs(roadmap): split M3 into room-flow tasks
test(relics): cover 8-relic cap
chore(ci): enable editmode workflow
```

中文 subject 也可以：

```text
feat(recording): 增加回放反向游标
```

### PR 检查清单

见 `.github/pull_request_template.md`。至少包含：

- 变更范围 / 对应规格章节
- 测试方式与结果
- 是否触碰局内 / 局外边界
- 截图 / GIF（表现层改动）

---

## 6. Unity YAML 合并（可选）

Unity 场景 / Prefab 是 YAML，直接合并容易冲突。Windows 下配置：

```powershell
# 把 <TUANJIE_EDITOR> 换成实际安装目录，例如：
# C:/Program Files/Tuanjie/Hub/Editor/2022.3.62t16/Editor
git config merge.tool unityyamlmerge
git config mergetool.unityyamlmerge.trustExitCode false
git config mergetool.unityyamlmerge.cmd '"<TUANJIE_EDITOR>/Data/Tools/UnityYAMLMerge.exe" merge -p "$BASE" "$REMOTE" "$LOCAL" "$MERGED"'
```

冲突时执行 `git mergetool`。`.gitattributes` 已把 `*.unity / *.prefab / *.asset` 指向 `unityyamlmerge`。

> 本仓库当前没有场景 / Prefab，因此该配置不是必需；M3 引入房间资产后再启用。

---

## 7. CI

### 仓库结构校验（默认启用）

`.github/workflows/validate.yml`：

- 校验 `Packages/manifest.json`、所有 `.asmdef` 是合法 JSON
- 校验 Unity 版本、关键目录 / 文档存在
- 校验 `.cs` 都有 `.meta`
- 校验没有把 `Library/`、`Temp/` 等生成目录提交进仓库

### Unity EditMode 测试（可选，国际版 Unity）

`.github/workflows/unity-editmode.yml` 默认跳过，使用 game-ci 的 **Unity 2022.3.62f1**（国际版）镜像；团结引擎无法直接跑在 game-ci 的 Unity 镜像里，因此 Tuanjie 用户以本地 Test Runner 为准。启用步骤：

1. 按 [game-ci 文档](https://game.ci/docs/github/activation) 激活 Unity 许可证。
2. 在 GitHub 仓库 `Settings → Secrets and variables → Actions` 添加：
   - Secret `UNITY_LICENSE`（Personal / Pro 的 `.ulf` 内容）
   - 如使用账号密码：`UNITY_EMAIL`、`UNITY_PASSWORD`
3. 在 `Settings → Secrets and variables → Actions → Variables` 添加：
   - `UNITY_CI_ENABLED = true`
4. 手动触发 `unity-editmode` workflow，或 push 到 `main`。

未配置许可证时不要打开该开关，否则 job 会失败。

---

## 8. 构建 Windows 包

1. `Tools → Ruins Echo → Create Main Scene`（确保场景在 Build Settings）。
2. `File → Build Settings`：Platform `Windows / x86_64`，Architecture `x86_64`。
3. `Build` 到 `Builds/Windows/`（已在 `.gitignore` 忽略）。
4. 首次构建建议关闭 `Development Build` 做一次性能对照。

> 技术切片没有 Assets 依赖，构建体积很小；M3 引入 Tilemap / Prefab 后再配置 Addressables（如需要）。

---

## 9. 常见问题

| 现象 | 排查 |
| --- | --- |
| Play 后黑屏 / 没反应 | 当前场景是否有 `EchoLabBootstrap`？没有则任意空场景按 Play 应自动生成。检查 Console 是否有编译错误。 |
| WASD 无反应 | Active Input Handling 是否为旧版 Input Manager；Console 是否报 `InvalidOperationException: You are trying to read Input using the UnityEngine.Input class`。 |
| 攻击打不到敌人 | 敌人 / 玩家 Collider 是否为 Trigger；`Physics2D.queriesHitTriggers` 是否被关掉；场景是否有 Physics2D 设置覆盖。 |
| HUD 中文显示方块 | 运行平台缺少中文字体。Echo Lab 使用 `Microsoft YaHei / SimHei`；Linux / Mac 可把 `EchoLabHud.EnsureStyles` 的字体列表改为系统可用字体。 |
| 测试不被发现 | Test asmdef 是否包含 `TestAssemblies`；是否在 Editor 平台；`UNITY_INCLUDE_TESTS` 是否被自定义宏覆盖。 |
| 场景打开后物体重复 | `EchoLabBootstrap.AutoBoot` 会检测场景中是否已有实例；如果手动放了两个，会重复。保留一个。 |
| 影子轨迹漂移 | 检查是否有代码给影子加了 `Rigidbody2D` 或改写了 `ShadowEcho` 的 Transform 更新顺序。 |

---

## 10. Definition of Done

一个任务算完成，需要同时满足：

- [ ] 对应规格章节的行为实现，边界条件有处理
- [ ] 不破坏“局内 / 局外隔离”“UnscaledTime”“Transform 回放”“种子确定性”四条硬约束
- [ ] 新增逻辑有 EditMode 测试（纯逻辑）或明确说明只能手工验证
- [ ] EditMode 测试全绿
- [ ] `Tools → Ruins Echo → Validate Project` 通过
- [ ] 更新 `docs/implementation-status.md`（如状态变化）
- [ ] PR 描述包含测试方式；表现层改动附截图 / GIF
