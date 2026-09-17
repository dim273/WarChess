# 当前 Game/Menu 场景挂载说明
2026-09-17 更新：Game/Menu 已通过 Unity Editor 资产接口实际完成挂载并保存。22 个角色实例已挂 UnitView；BattleInstaller 仅注册当前启用的角色，禁用的备用敌人不自动启用。最新映射、备份及验证结果见 [SceneIntegrationReport.md](SceneIntegrationReport.md)。技术背景见 [TechnicalGuide.md](TechnicalGuide.md)。下文保留为手动重建说明，不需要在现有场景重复挂载。

## 0. 先完成依赖检查

先补齐 R3 核心 NuGet 依赖并检查 Console。R3.Unity 和核心 R3 是两步安装，不能只看 Package Manager。具体缺失项、官方链接和 Cinemachine/Unity 6.5 兼容风险见技术文档第 1 节。

不要在有脚本编译错误时继续挂组件。建议先复制 Game/Menu 场景作为迁移备份，在副本中操作；本次没有替你创建或保存场景副本。不要删除旧 Scripts1 文件夹，先只替换场景组件。

## 1. Game：先摘除旧战斗链

场景里目前还在使用下列旧脚本。替换时移除对应组件，保留 GameObject、Canvas、按钮、TMP、Animator、Renderer、Collider 和场景引用。**不要让新旧逻辑同时控制同一角色。** 只取消 enabled 仍不能阻止部分脚本的 Awake，所以迁移完应移除旧组件。

| 现有对象/组件 | 处理 |
| --- | --- |
| RoleManager / RoleActionSystem / TurnSystem / EnemyAI | 移除同名旧战斗组件 |
| LevelGrid / PathFinding（Pathfinding）/ GridSystemVisual / MouseWorld / TestGrid | 移除旧网格、寻路、输入与测试组件 |
| RoleActionSystemUI 的 ActionSystemUI | 移除，保留 ActionButtonContainer |
| TurnSystemUI 的 TurnSystemUI | 移除，保留 TurnButton、TurnInfo、IsEnemyTurnUI |
| ActionBusyUI 的 ActionBusyUI | 移除旧脚本，保留用作 Busy 显示的物体 |
| Canvas 的 UIManager | 换成 SceneControlsView，并清理旧按钮 OnClick |
| CameraController 的 CameraController | 换成 BattleCameraView，保留原 Transform |
| 角色/敌人上的旧脚本 | 按第 4 节处理 |

不要仅仅关闭整个 System/Canvas 父物体，否则新输入和 UI 也可能一起停止。

## 2. 建立场景入口与棋盘

### BattleInstaller

在 Game 根级创建空物体 `BattleScope`，添加 **BattleInstaller**。它已经继承 LifetimeScope，不需要在同一物体再添加一个 LifetimeScope。

- Auto Run 保持开启，Parent 留空，不放入 DontDestroyOnLoad。
- Width=35，Height=28，Cell Size=2。
- Obstacle Layer Mask 只选 **Obstacle（第 8 层）**。它会从现有障碍 Collider 采样；不选择 Ground/Role。
- Blocked Cells 可用于明确补充不可通行的格子。坐标必须在 [0,34]×[0,27]。
- AI Think Delay 可保持 0.4。
- Board View、Input View、Hud View、Unit Views 按下面拖入。
- 使用第 6 节暂停菜单时，将 Scene Controls 也拖入；没有该组件则留空。

### BoardView

可在原 `GridSystemVisual` 物体添加 **BoardView**。其 Transform 设置为原棋盘原点、单位缩放、原来的棋盘朝向；当前场景按世界 XZ、原点 (0,0,0)、格宽 2 使用。

- Cell Container：可指定该物体下一个空子物体；留空则使用自身。
- Surface Offset：0.02，避免与地面重叠闪烁。
- Move/Attack/Defend Material：从旧 GridSystemVisual 中对应蓝/红/白高亮材质转移。
- Cell Prefab：必须是具有 **GridCellView** 的预制体，不能只拖旧 GridSystemVisualSingle 组件。

建议复制现有 `Assets/Prefab/GridSystemVisualSingle.prefab` 作为新高亮预制体，在副本根上移除 **GridSystemVisualSingle**，添加 **GridCellView**：

- Mesh Renderer 拖子物体 `Quad` 的 MeshRenderer。
- Reference Cell Size=2。现有 Quad 在子级，旋转 90°、缩放 1.8×1.8；保留这些值。
- 不需要给高亮格加 Collider。
- 复制后的资产可保存在 Scripts 内你自选的资源子目录，或另行安排资源目录；本次没有生成/改写该 prefab。
- 将新 prefab 根上的 GridCellView 拖入 BoardView.Cell Prefab。

初始时会创建 35×28 个可复用高亮格。棋盘模型是服务权威数据，高亮只是显示。

## 3. 输入与 HUD

### BattleInputView

在常驻启用的空物体（例如原 RoleActionSystem 物体）上添加 **BattleInputView**：

- Input Camera：现有 **Main Camera** 的 Camera，不是 CinemachineVirtualCamera。
- Unit Layer Mask：只选 **Role（6）**。
- Board Layer Mask：只选 **Ground（7）**。
- Board View：由 VContainer 注入，Inspector 若保留此字段也应指向同一个 BoardView。
- 地面必须有 Ground 层的 Collider；单位点击 Collider 在 Role 层。
- 保留 EventSystem、StandaloneInputModule；本实现使用旧 Input API，Active Input Handling 需为 Input Manager 或 Both。
- 组件所在物体不要用于 Busy 遮罩，不要随暂停面板关闭。

### BattleHudView

在一直启用的 **Canvas** 上添加 **BattleHudView**，并把它拖入 BattleInstaller.Hud View。

| BattleHudView 字段 | 当前场景对象 |
| --- | --- |
| End Turn Button | TurnSystemUI 下的 **TurnButton**，原旧脚本引用 fileID 971932808 |
| Turn Text | **TurnInfo** 的 TMP，原引用 fileID 649817075 |
| Action Points Text | **ActionPointsTMP**，原引用 fileID 194715290 |
| Move/Attack/Defend Button | 在 **ActionButtonContainer** 中准备的三个静态 Button |
| Busy Overlay | 保留的 **ActionBusyUI** 显示对象（先移除旧脚本）；可留空 |
| Selected Unit Text / Status Text | 当前未找到专用绑定，新增独立 TMP 或先留空 |
| Result Panel / Result Text | 当前未找到新架构结果面板，新增后绑定；建议配置以便看见胜负 |

原 ActionSystemUI 在运行时动态创建按钮；它被移除后不会自动生成按钮。可把原行动按钮 prefab 拖入 ActionButtonContainer 三次，改名 MoveButton/AttackButton/DefendButton，移除每个实例上的旧 **ActionButtonUI**，保留 Button、Image、TMP，分别写好按钮文字。

**清空这些按钮的旧 OnClick 事件。** BattleHudView 会自行注册监听器，不要再从 Inspector 额外绑定一次行动/结束回合，否则可能重复执行。

**IsEnemyTurnUI** 是旧敌方回合提示，不是任意行动 Busy 的等价物。移除旧 TurnSystemUI 后可先设为不激活；新 Turn Text 已显示 Enemy，Busy Overlay 用于行动播放。不要把它设成 HUD 自身或 HUD 父级，否则显隐会影响整个 HUD。

## 4. 现有角色和敌人

当前场景中有 **Role_A、Role_B、Enemy、Enemy (1)…Enemy (19)**。每一个场景实例根物体添加 **UnitView**，全部 22 个实例拖入 BattleInstaller.Unit Views；列表中不能缺失、重复或包含不激活物体。

### 移除旧组件，保留显示对象

- 根角色：Role、MoveAction、DefenseAction、AttackAction、HealthSystem、RoleAnimator。
- Role_A 另有 RoleAAttackHelp；新代码不调用它，旧 FX 不接入时可以一起移除。
- Enemy 另有 EnemyRagdollSpawner，依赖旧 HealthSystem，移除。
- 子级 Canvas：移除 RoleWorldUI，保留 Canvas、血条 Image、PointsText。
- SelectedVisual：移除 RoleSelectedVisual，保留其 Renderer/GameObject。
- **LookAtCamera 可以保留**：它只负责血条面向 Main Camera，不读取旧战斗单例。
- 保留 Animator、模型、角色碰撞体、Cinemachine 所用场景对象。

### UnitView 必填/关键字段

| 字段 | Role_A | Role_B | Enemy 系列 |
| --- | --- | --- | --- |
| Unit Id | player-a | player-b | enemy-00 到 enemy-19，逐实例不同 |
| Team | Player | Player | Enemy |
| Use Scene Position | 勾选 | 勾选 | 勾选 |
| Invert Forward | 勾选（旧玩家模型朝 -Z） | 勾选 | 不勾选 |
| Animator | 原 RoleAnimator 的 animator 引用 | 同左 | 同左 |
| Selected Visual | 子物体 SelectedVisual | 同左 | 没有则留空 |
| Health Fill | 子 Canvas 下 healthBar 的前景 Image | 同左 | 同左 |
| Action Points Text | 子 Canvas 下 PointsText 的 TMP | 同左 | 同左 |
| Selection Collider | 根角色选择用 Collider | 同左 | 同左 |

使用 Scene Position 时，Role_A 的位置 (10,0,6) 对应格 (5,3)，Role_B 的 (10,0,10) 对应格 (5,5)；不会把所有角色放在默认格 (0,0)。若关闭该选项，则必须逐个填写 Initial X/Z，且不能重叠或落在障碍上。

当前新 UnitView 的 Rules 默认是 HP=100、AP=2、MoveRange=4、AttackRange=3、Damage=40，不会自动读取被删除的旧组件。若需要尽量保留原预制体的范围/AP，删除旧组件前记录其数值，本次读取到：

| 原 prefab | Max Health | Max Action Points | Max Move Distance | Max Attack Distance | RoleAnimator.attackTime |
| --- | --- | --- | --- | --- | --- |
| Role_A | 100 | 4 | 1 | 5 | 0.2 |
| Role_B | 100 | 10 | 2 | 4 | 0.5 |
| Enemy | 100 | 2 | 3 | 3 | 0.1 |

这些值是 prefab 配置，不保证覆盖场景实例全部 overrides。迁移时以当前实例 Inspector 为准。攻击伤害原来属于攻击效果脚本，新架构由 UnitView/UnitDefinitionAsset.Attack Damage 统一定义；不要让旧弹体再次调用 Role.Damage。

可选做法：为三类单位创建 UnitDefinitionAsset，在 UnitView.Definition Asset 绑定它。绑定后资产数据优先于 UnitView.Rules，避免误以为修改 Inspector 数值但实际未生效。

### Animator 和表现

- Walking=Walking；Attack Parameter=Attacking。
- 当前控制器没有 Hit/Defending/Dead trigger，留默认名会安全跳过；不必强行添加不存在的状态。
- 没有死亡状态时，代码会隐藏角色显示和角色 UI、禁用 Collider。
- 不要由 Animator Root Motion 再推动角色；UnitView.Awake 会关闭 applyRootMotion。
- Attack Hit Delay/Attack Duration 是表现时序，可以先参照上表设置命中延迟，再按动画长度调整时长。
- 当前不自动复制旧攻击 FX/布娃娃；它们是后续表现适配项，不影响权威伤害和胜负规则。

## 5. 相机

在原 **CameraController** 物体上添加 **BattleCameraView**，移除旧 CameraController。

- Cvc：当前 Cinemachine 3.1.7 可拖入 **CinemachineCamera**；场景未迁移时也支持原 **CinemachineVirtualCamera**。不要拖 Main Camera。
- 新版相机的 Position Control 使用 **Follow（CinemachineFollow）**，Tracking Target 指向该 CameraController 物体；脚本通过 Follow Offset 的 Y 值缩放。旧相机继续使用原 Body/Transposer，不必为本次修复重建场景。
- Move Speed=10、Rotation Speed=100、Zoom Speed=5，可沿用现有值。
- 保留 Main Camera 上 CinemachineBrain，以及虚拟相机原 Follow/LookAt/Body 设置。
- 保留该对象原位置/父子层级，不需要将脚本挂到 Main Camera。
- 操作仍为 WASD 移动、Q/E 旋转、滚轮缩放；暂停时停止。
- 该组件没有注入依赖，不必登记到 BattleInstaller。

如果项目因 Cinemachine 与 Unity 版本不兼容而无法编译，先处理技术文档中的包问题；不要仅删除 CameraView 以假装整个旧相机系统已经兼容。

## 6. Game 的暂停菜单与导航

在 Canvas 添加 **SceneControlsView**，绑定 Pause Panel=现有 **Menu** 父对象（包含 MenuPanel 背景和 ButtonList 按钮），并把该组件拖入 BattleInstaller.Scene Controls。SceneControlsView 本身不要挂在会被关闭的 Menu 内。

删除旧 UIManager 的持久按钮事件，再设置：

| 现有 Button | 新 OnClick |
| --- | --- |
| StopButton | SceneControlsView.Pause |
| ContinueButton | SceneControlsView.Resume |
| RestartButton | SceneControlsView.Restart |
| BackToMenuButton | SceneControlsView.BackToMenu |
| ExitButton | SceneControlsView.Quit |

Menu 父对象初始不激活，MenuPanel 子对象保持启用。暂停面板背景应拦截 UI 射线。Pause 同时暂停 AI、移动与攻击表现；Resume 恢复。重开/回主菜单会恢复 Time.timeScale=1。

## 7. Menu 场景

1. 在 Canvas 添加 **MenuView**，New Game Button 绑定 **ButtonStart**，Quit Button 绑定主菜单自己的 **ButtonExit**（不要误用 HelpPanel/AboutPanel 内的关闭按钮）。
2. 清空 ButtonStart 和主退出 ButtonExit 原来调用 UIManager 的 OnClick；MenuView 自动绑定。
3. 根级空物体 `MenuScope` 添加 **MenuInstaller**，Menu View 拖入上面的组件，Game Scene Name=`Game`，Auto Run 开启、Parent 留空。
4. Canvas 可添加 **SceneControlsView**，Help Panel=HelpPanel，About Panel=AboutPanel；拖入 MenuInstaller.Scene Controls。
5. 帮助按钮 ButtonHelp → ShowHelp；HelpPanel 内 ButtonExit → HideHelp。
6. 关于按钮 ButtonAbout → ShowAbout；AboutPanel 内 ButtonExit → HideAbout。
7. 清理旧 UIManager 组件及其持久按钮事件；不要重复注册开始/退出事件。
8. 保留 EventSystem 和 Canvas 渲染设置。确认 Build Settings/Build Profiles 中仍启用名为 Menu、Game 的场景。

## 8. 挂完后的验收顺序

先不急着继续扩展代码，依次确认：

1. Console 无编译/依赖错误；运行核心检查菜单，输出 23 passed、0 failed。
2. 从 Menu 点击开始，Game 初始化无空引用、重复 UnitId、越界或障碍出生报错。
3. 初始选中玩家，血条/AP 正确；点击两个玩家能切换；移动后占位和高亮一致。
4. 选择 Defend 后点击同一角色，自身防御成功而不是重新切回 Move。
5. 障碍挡移动和攻击；攻击只结算一次，AP 只扣一次。
6. 结束回合后敌方依次行动，AP 用完交还玩家，不重复恢复双方 AP。
7. 单位死亡后不再拦截选择/占位；最后一方死亡后关闭行动与结束回合按钮。
8. 动画中暂停/恢复；暂停中不能操作 HUD 行动；重新进入菜单或重开不残留 Busy/暂停状态。
9. 测试禁用正在移动的 UnitView，不应永远卡在 Busy；注意该测试是运行期检查，不要保存 Play Mode 状态。
10. 连续 Menu→Game→Menu 往返至少三次，无重复按钮响应、旧场景回调或 Console 错误。

如果出现错误，下一次对话请提供：第一条完整 Console 堆栈、BattleInstaller 的 Inspector、报错单位的 UnitView 配置，以及是否已经完成 R3 核心依赖安装。
