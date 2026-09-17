# 场景挂载交接（2026-09-17）

本次已实际修改并保存 Game/Menu 场景、Role_A/Role_B/Enemy 三个预制体；不是仅提供挂载步骤。模型、原有 UI 布局、相机跟随对象、材质及角色启用状态保留。Unity 6000.5.8f1，Cinemachine 3.1.7，R3 1.3.1，VContainer 1.19.0。

## 当前挂载

| 场景对象 | 组件和引用 |
| --- | --- |
| Game / 原有 ` BattleScope`（名称有前导空格） | BattleInstaller；绑定 Board/Input/Hud/SceneControls；35×28，格宽 2，Obstacle 层 8 |
| GridSystemVisual | BoardView；原点已归零；三种现有材质保留；Generated/GridCell.prefab；自身作为高亮容器 |
| RoleActionSystem | BattleInputView；Main Camera，BoardView，Role 层 6 / Ground 层 7 |
| 根 Canvas | BattleHudView + SceneControlsView；暂停对象是整个 Menu，不是单独的 MenuPanel 背景 |
| ActionButtonContainer | MoveButton / AttackButton / DefendButton；沿用原按钮样式，解除对缺脚本旧模板的引用 |
| TurnButton / TurnInfo / ActionPointsTMP | 分别绑定结束回合、回合文本、行动点文本 |
| Canvas 新增文本 | SelectedUnitText、BattleStatusText、BattleResultText；结果文本初始隐藏 |
| ActionBusyUI | 绑定 Busy Overlay，初始隐藏；旧 IsEnemyTurnUI 隐藏 |
| CameraController | BattleCameraView.Cvc 绑定现有 CinemachineVirtualCameraBase；不重建相机 |
| Role_A / Role_B / Enemy 预制体 | UnitView；Animator、血条、行动点文本、碰撞体，以及玩家选择圈 |
| Menu / 根 Canvas | MenuView + SceneControlsView；开始、退出、帮助和关于按钮 |
| Menu / MenuScope | MenuInstaller；MenuView、SceneControls、Game 场景名 |

22 个场景角色实例均挂载 UnitView 并分配唯一 ID。当前仅 6 个启用角色注册进战斗：Role_A、Role_B、Enemy、Enemy (1)、Enemy (2)、Enemy (3)。其余 16 个备用敌人保持禁用。以后启用更多敌人时，必须同步补充 BattleInstaller.Unit Views，且保证出生点不重叠、不在障碍中。

Role_A：HP 100 / AP 4 / 移动 1 / 攻击范围 5；Role_B：100 / 10 / 2 / 4；Enemy：100 / 2 / 3 / 3。默认伤害 40，沿用当前新代码规则。没有把旧特效/布娃娃系统重新移植到 UnitView。

## 按钮所有权

- 行动、结束回合、菜单开始/退出由 View 在 Awake 注册、OnDestroy 注销；Inspector 不重复添加同一个业务回调。
- 暂停、继续、重开、返回菜单、帮助/关于使用已保存的 SceneControlsView 持久按钮回调。
- 暂停绑定 Canvas/Menu 父对象，其中同时包含 MenuPanel 背景与按钮列表。
- 不要再添加第二个 LifetimeScope 或重新挂回旧战斗管理脚本。

## 备份和回滚

完整迁移前原件位于 `Backups/SceneIntegration-20260917-192704/Assets/`，包含两个场景、三个单位预制体及对应 .meta。后续同名前缀目录是调试各阶段的增量快照，不是最初状态。恢复时先关闭 Unity，将需要恢复的明确文件及 .meta 从首份备份复制回对应路径。不要删除整个 Assets 或 Library。

本次从目标场景和预制体移除了因旧源代码已删除而产生的 Missing Script 组件；这些序列化旧配置仍可从备份恢复。未批量清理未使用的旧素材、旧攻击特效和其他预制体。

## 自动化工具

- `Assets/Scripts/Editor/SceneIntegrationTool.cs`：显式批处理入口 `WarChess.EditorTools.SceneIntegrationTool.Run`。会备份并重写上述挂载、角色 ID、指定按钮回调和棋盘原点，只针对本地图；不是通用迁移器。后续手动改布局或配置后不要随意重跑。
- `Assets/Scripts/Editor/SceneIntegrationSmoke.cs`：入口 `WarChess.EditorTools.SceneIntegrationSmoke.Run`；自动进入 Play Mode、菜单开始游戏、检查单位/网格、暂停继续并返回菜单，不保存运行时场景。不带 `-quit`，测试自行退出。
- 核心检查入口 `WarChess.Tests.CoreRegressionTests.RunAll`；编辑器菜单 WarChess/Validation/Run Core Regression Tests。

## 验证与边界

- 已完成实际 Unity 编辑器编译、场景保存、无 Missing Script 场景检查，以及 6 个出生点的边界/障碍/重复占位校验。
- 核心回归检查：23 通过、0 失败。
- 最终 Play Mode 冒烟通过：Menu 的开始按钮进入 Game，6 个启用单位及 980 个格子初始化，暂停/继续按钮生效，再返回 Menu。挂载进程退出码 0；冒烟进程退出码 0，未记录运行时 Error/Exception。
- 仍有 1 条加载旧素材时的 `The referenced script (Unknown) ... is missing` 警告；目标场景层级 Missing Script 检查通过，但这不代表整个资源库没有失效脚本引用。本次未扩大到清理所有旧素材。
- Unity 在实际运行时自动更新了 `Assets/TextMesh Pro/Fonts/斗鱼追光体 SDF Dynamic.asset` 的字体序列化及新增字符缓存；这是本次除场景/预制体/Scripts 外唯一保留的资产改动，不是主动替换字体。
- 最终运行验收记录：`Logs/SceneIntegration-SmokeAcceptance.log`；挂载和核心检查记录：`Logs/SceneIntegration-Acceptance.log`。
- 批处理使用无图形模式，不等于画面验收，也未生成 Player 构建。需要在有界面的 Unity 内检查中文字体、血条朝向、按钮布局、鼠标射线及移动/攻击动画。旧 LookAtCamera 源文件已缺失，本次清除了该失效组件，血条不会因此自动恢复跟随相机旋转；若需要可独立补充纯表现朝向组件。
- 首次运行检查发现并修正了三个配置问题：注册了禁用敌人、旧棋盘管理对象偏移导致出生坐标越界、打开场景前缓存的高亮预制体引用失效。未修改战斗规则来绕过校验。

## 后续对话提供的信息

请先阅读本文件，再阅读 TechnicalGuide.md 中的架构部分。说明操作的是 Game 还是 Menu、Console 第一条完整错误、相关 Inspector 截图及复现步骤。早期 SceneSetup/TechnicalGuide 的 2026-09-11 包版本、尚未挂载说明属于历史记录，以本文件为准。
