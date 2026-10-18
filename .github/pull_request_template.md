## 变更内容

<!-- 一句话说明这个 PR 做了什么；关联 Issue：Closes #123 -->

## 对应规格 / 里程碑

<!-- 例如：规格 4.3；路线图 M1 -->

## 验证方式

<!-- 不要只写“手动测试通过”；写清楚场景、步骤、结果。 -->

- [ ] EditMode 测试：`Window → General → Test Runner → EditMode → Run All`
- [ ] `Tools → Ruins Echo → Validate Project`
- [ ] Play Mode 手测（如有表现层改动）
- [ ] 构建验证（如涉及场景 / 资源 / 包）

## 检查清单

- [ ] 未破坏局内 / 局外数据隔离
- [ ] 影子回放仍未使用 Rigidbody2D 驱动位移
- [ ] 录制 / 回放时间戳仍使用 `Time.unscaledTime`
- [ ] 随机生成仍使用 `DeterministicRandom`，未引入 `UnityEngine.Random` 影响玩法
- [ ] 新增逻辑有测试，或说明了只能手工验证的原因
- [ ] 已更新 `docs/implementation-status.md`（如状态变化）
- [ ] 没有提交 `Library/`、`Temp/`、`Builds/` 等生成目录
- [ ] `.meta` 文件与资源一起提交

## 截图 / GIF（表现层改动）

<!-- 拖入图片即可 -->
