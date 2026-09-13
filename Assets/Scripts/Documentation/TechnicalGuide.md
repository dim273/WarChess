# WarChess：R3 + VContainer 技术交接
更新：2026-09-11。适用代码：`Assets/Scripts`。阅读挂载步骤请看同目录的 [SceneSetup.md](SceneSetup.md)。

## 1. 当前结论和边界

- 新代码继续使用 Model–Presenter–Service，并使用 VContainer 1.19.0、R3 1.3.1。
- 本次只改动/新增 `Assets/Scripts` 内代码、测试、文档和相应 .meta；没有保存或修改场景、预制体、Packages、ProjectSettings、Scripts1。
- 现有 Game/Menu 场景仍绑定旧 `Assets/Scripts1` 的组件。代码改好不等于场景已经迁移。
- 核心回归测试：23 项通过、0 项失败。包括真实 R3 与 VContainer，不使用框架桩。
- Scripts 全部运行时 C# 已通过 Unity .NET Standard 2.1 隔离编译。没有进行 Unity Editor 导入验证、Play Mode、玩家构建或视觉验收。
- 状态：代码层验证通过；场景运行验收仍受依赖安装和手动挂载阻塞，不能称为整个项目已运行通过。

### 依赖事实（以后请重新核实）

| 内容 | 本次读取的实际值 |
| --- | --- |
| ProjectVersion | Unity 6000.5.8f1 |
| R3 Unity 适配包 | com.cysharp.r3，1.3.1，Git |
| VContainer | jp.hadashikick.vcontainer，1.19.0，Git |
| UniTask | 2.5.11，已安装，本次未强行引入 |
| Cinemachine | 2.10.5，使用 CinemachineVirtualCamera/Transposer |
| UI | uGUI + TMP，旧 Input Manager API |
| 启用场景 | Assets/Scenes/Menu.unity、Assets/Scenes/Game.unity |
| 层 | Role=6、Ground=7、Obstacle=8 |
| 当前场景单位 | Role_A、Role_B；Enemy、Enemy (1) 至 Enemy (19) |

**R3 的安装分两部分。** 当前能找到 Unity 适配包，但本次没有在项目中找到它引用的 `R3.dll`、`Microsoft.Bcl.TimeProvider.dll`、`Microsoft.Bcl.AsyncInterfaces.dll`。Package Manager 截图只能证明 Unity 适配包存在。请按 [R3 官方 1.3.1 Unity 安装说明](https://github.com/Cysharp/R3/tree/1.3.1#unity)，通过 NuGetForUnity 安装 R3 1.3.1 和解析出的依赖，再检查适配包要求的 AsyncInterfaces 是否也已解析。不要同时手工复制不同目标框架的同名 DLL。这里没有自动安装/升级 Unity 包。

本次真实 R3 NuGet 包仅还原在系统临时验证目录，**没有**导入项目。因此独立测试通过不代表 Unity 当前能找到 R3 核心库。

**另一个既有风险：** 使用 Unity 6000.5.8f1 的编译器编译旧 Cinemachine 2.10.5 的 uGUI Storyboard 分支时，`CinemachineStoryboard.cs:196` 的 `Object.GetInstanceID()` 报 CS0619（当前引擎要求 GetEntityId）。本次没有修复第三方包或替你选择包升级方案。独立编译针对本次实际使用的相机核心 API，未启用 Storyboard/uGUI 分支，未编译 Timeline 集成；不能用该结果宣称 Cinemachine 全包或项目整体兼容。后续若 Unity Console 也报告此错误，应单独处理 Unity/包版本兼容，而不是修改战斗规则来规避。

## 2. 文件结构与职责

```text
Scripts/
  Domain/         棋盘占位、单位状态、回合、坐标、不可变定义
  Application/    行动校验/结算、移动/攻击/防御策略、寻路、视线、AI、回合与胜负
  Presentation/   Battle/Menu/Unit/Board Presenter，以及 View 端口
  UnityViews/     输入、HUD、角色动画、棋盘高亮、相机、场景/面板适配
  Composition/    BattleInstaller、MenuInstaller（均继承 LifetimeScope）
  Configuration/  可选的 UnitDefinitionAsset
  Tests/Editor/   无场景副作用的核心回归菜单检查
  Documentation/  技术交接与实际场景挂载步骤
```

依赖方向：

```text
Unity 输入/按钮 → R3 意图流 → Presenter → Application Service → Domain
                                             ↓
                                      不可变 ActionResult
                                             ↓
                                     Presenter 编排动画

UnitModel 的只读 R3 数值流 → UnitPresenter → Unity 数值显示
Composition → 以上各层（只在组成根装配）
```

- Domain 不依赖 UnityEngine、View、Presenter、VContainer；它使用 R3 核心的响应式数值。
- Service 不访问 Physics、Transform、Animator、单例或容器。
- Presenter 通过接口操作 View，不根据动画反馈重新计算伤害。
- UnityViews 只处理平台 API 和表现。SceneControlsView 是场景导航/面板基础设施的薄适配，不处理战斗规则。
- 没有新增 asmdef，避免在本次迁移中强行改变现有 Assembly-CSharp 编译边界。
- 保留已有 .cs 文件名和 .meta GUID。历史拼写 `Actiondata.cs`、`ViewContarcts.cs` 暂不更名，减少迁移风险。
- 统一使用 `WarChess.*` 命名空间；原配置文件错误的 `WarChess.NewScript.Domain` 引用已修正。

## 3. VContainer 对象图与生命周期

### 战斗场景

`BattleInstaller : LifetimeScope` 是唯一场景组成根；不要再手动 new 它或自己调用 Awake/Start。

1. Configure 校验 Inspector 引用、棋盘尺寸，登记组件和服务。
2. BoardView 先设置坐标尺度，暂不创建高亮格。
3. BattleModel 场景工厂读取障碍、单位配置。Physics 只在这个 Unity 边界采样一次。
4. 通过工厂创建一对一 UnitPresenter 集合；每个实例对应同一个 BattleModel 的 UnitModel。
5. `BattleSceneEntryPoint : IStartable` 在组件 Awake 后先创建棋盘显示，再调用 BattlePresenter.Start。
6. 场景作用域释放时取消订阅、AI 定时器和动画；BattleModel 释放自己拥有的 R3 状态源。

| 对象 | 注册/所有权 |
| --- | --- |
| Board/Input/Hud/SceneControls | RegisterComponent，Unity 场景拥有 GameObject |
| 多个 UnitView | 每个实例 Keyed(view)，避免同类型默认解析仅取最后一个 |
| BattleModel | Scoped 工厂，拥有全部 UnitModel |
| Service、BoardPresenter、BattlePresenter | Scoped，构造器注入 |
| IActionHandler | 三个策略注册，VContainer 注入 IEnumerable&lt;IActionHandler&gt; |
| UnitPresenter 集合 | 场景工厂创建，由 BattlePresenter 统一 Dispose |
| BattleSceneEntryPoint | 唯一战斗启动 EntryPoint |
| 动态高亮格 | BoardView 创建；无业务依赖，无需容器注入 |

`UnitView.BindBoard` 标有 [Inject]。组成根工厂同时显式设置这个依赖，是为了读取初始场景坐标时不依赖组件注入回调先后顺序；赋值本身幂等，不在此创建订阅。

构造器只接收直接依赖。不允许给 Service、Model 或普通 View 注入 IObjectResolver 作为全局服务查询器。容器 Resolve 仅出现在组成根工厂和测试中。

### 菜单场景

`MenuInstaller : LifetimeScope` 注册 MenuView、ISceneService、可选 SceneControlsView，以及 MenuPresenter EntryPoint。菜单和战斗作用域彼此独立，不设父级常驻全局容器，不使用 DontDestroyOnLoad。

### 顺序和释放

BattlePresenter.Dispose 先设置 disposed，再取消定时器和输入订阅，再释放 UnitPresenter。动画即使发出迟到回调，也会在 Presenter 的 disposed 检查处停止。Dispose 可重复调用；不要依赖 Unity 各对象 OnDestroy 的相对次序。

## 4. R3 使用约定

| 流 | 拥有者 | 订阅者 | 释放方式 |
| --- | --- | --- | --- |
| HealthChanged / ActionPointsChanged | UnitModel 私有 ReactiveProperty | UnitPresenter | UnitPresenter.CompositeDisposable |
| UnitClicked / CellClicked | BattleInputView 私有 Subject | BattlePresenter | BattlePresenter.CompositeDisposable |
| ActionSelected / EndTurnRequested | BattleHudView 私有 Subject | BattlePresenter | 同上 |
| NewGameRequested / QuitRequested | MenuView 私有 Subject | MenuPresenter | MenuPresenter.CompositeDisposable |
| AI 延迟 | BattleInputView.Schedule 返回的订阅 | BattlePresenter | SerialDisposable 替换/取消 |

- 对外暴露 ReadOnlyReactiveProperty/Observable，不暴露可写 Subject。
- 保留 Health、ActionPoints 的普通只读数值属性，方便 Service 同步规则计算。
- 新订阅立即取得当前生命/行动点；不再每次刷新 HUD 都遍历角色重写血条。
- 血条在逻辑结算时更新；攻击命中回调只做受击/死亡表现。这是明确的视觉时序，不是伤害重复结算。
- 位置没有直接响应式绑定 Transform；权威坐标先更新，路径动画稍后追上，避免瞬移。
- 延迟明确使用 `UnityTimeProvider.Update`，回调在 Unity 主线程并遵循 Time.timeScale。
- 不使用默认线程池 Timer 来操作 Unity 对象。
- ActionService 的重入保护阻止同步 R3 订阅在行动尚未结算完时再次执行行动。
- 下次新增状态时应先明确拥有者及事务边界，不能为了“响应式”在半完成的棋盘更新中触发新的业务行为。

## 5. 行动、回合与显示契约

### 权威执行

`IActionService.TryExecute(BattleModel, ActionRequest)` 是行动入口：

1. 校验阶段、单位存活、当前阵营、已解锁行动、AP。
2. 重新生成预览并检查目标，拒绝使用已经过期的 UI 高亮。
3. 执行对应策略，更新位置/血量/防御、扣除 AP、检测胜负。
4. 返回 ActionResult，Presenter 锁住输入并播放表现。
5. 动画完成后恢复 PlayerInput/EnemyThinking，或保持不可逆的 Completed。

非法请求不消耗 AP、不移动单位。策略只在合法预览后执行。不要绕开 Service 直接从 View 修改 Model。

### 当前新架构规则（不是旧 Scripts1 的完全等价移植）

- 移动：四方向 BFS，单格代价 1，范围按路径长度；不能穿墙、穿单位。
- 攻击：曼哈顿范围，静态障碍挡视线；精确穿过格角时两侧障碍都会检查；单位不遮挡射线。
- 移动/攻击/防御均为 1 AP。
- 防御只能点自己，不能重复叠加；受伤减半，奇数向上取整，在自身下一次回合开始解除。
- 回合只补满新行动阵营的 AP；完成敌方回合再回玩家时回合数 +1。
- 死亡立即移除棋盘占位，但保留模型身份用于完成受击显示。
- AI 优先击杀/攻击，然后按与最近玩家的距离选择有进展的移动，最后防御；每次行动后重新决策。它是确定性的简单启发式，不保证复杂迷宫绕路最优。
- 进入 Completed 后禁止新行动/换回合，HUD 按钮关闭。

旧 Scripts1 的 DefenseAction 实际是消耗 2 AP 转一圈，没有减伤状态；旧移动使用另一套寻路。新代码已有 IsDefending 等领域字段，本次补齐的是新架构语义，不应声称旧玩法和视觉完全一致。若要与旧版逐项同数值/同效果，应另行确认规则后修改 Handler 和对应测试。

### 表现与配置

- UnitDefinitionAsset 只保存设计期数据，可选。配置不为空时优先于 UnitView 的 Rules；运行期不写回资产。
- UnitView 默认 `Use Scene Position=true`，初始位置从 BoardView 坐标系计算并吸附到格点。ID 仍须逐实例唯一。
- 中途禁用正在动画的 UnitView，会停止协程、对齐最终坐标并补发一次完成回调，避免永久 Busy。
- 原 Animator 只有 Walking(bool)、Attacking(trigger)。Hit/Defending/Dead 为可选 trigger，缺少时跳过，不刷参数错误。
- 没有 Dead trigger 时，死亡角色隐藏 Renderer/Canvas，并禁用所有角色碰撞体。
- 原项目弹道、攻击特效、布娃娃不自动移植；它们不能继续依靠旧 HealthSystem/Role 结算。需要时在 UnitView 的表现边界增加适配，伤害仍只在 Service 结算一次。
- SceneControlsView 通过 Time.timeScale 暂停表现和 AI 时钟，输入/HUD 同时拒绝暂停时的战斗意图；导航恢复 timeScale=1。

## 6. 本次修正的问题

- UnitDefinition 缺分号；UnitModel 方法结束符错误。
- ActionResult 的 Success 属性与工厂同名冲突，改为 Succeeded。
- TargetUnitId 的空值类型错误，改为 UnitId?。
- CellCilcked 接口拼写与实现不一致，统一为 CellClicked。
- 配置命名空间残留与重复 using，UnityEngine.Application 与项目命名空间冲突。
- 缺失的寻路、视线、行动策略、回合/胜负和 AI 服务。
- 单位在障碍上生成、死亡单位移动的占位保护。
- 点击已选中己方单位时防御被“重新选择”吞掉。
- 已完成战斗仍可点击结束回合的 HUD 状态。
- 场景释放后的 AI/动画回调，以及动画对象禁用后永久等待。
- 同类型多个 UnitView 的 VContainer 默认键解析歧义。

## 7. 验证方法与证据

### 可重复的项目内检查

补齐依赖并让 Unity 编译通过后，在 Editor 菜单执行：

`WarChess > Validation > Run Core Regression Tests`

源码：`Assets/Scripts/Tests/Editor/CoreRegressionTests.cs`。

这是无需 NUnit/asmdef 的确定性检查入口，不是 Unity Test Runner 测试套件。失败会记录 FAIL 并抛出异常；不会修改场景资产。正确输出应为 `RESULT: 23 passed, 0 failed`。

覆盖：配置/占位、BFS、非法请求零副作用、阵营/AP、障碍/格角视线、防御/减伤、死亡/胜负、回合恢复、动画锁、AI 终止、R3 初值/取消/重入、Presenter 防御点击/定时器取消/迟到回调、菜单重复订阅，以及 VContainer 策略集合解析和 Presenter 自动释放。

### 本次实际执行

- 独立 .NET 8 编译运行上述同一份测试，引用真实 R3 1.3.1 NuGet 和项目 VContainer.dll：23/23 通过。
- Unity 6000.5.8f1 自带 Roslyn C# 9 + Unity .NET Standard 2.1 引用，编译全部 31 个运行时 C# 文件：通过。
- Editor 回归菜单入口也已独立编译通过；这仍不代表已在 Unity 菜单中实际执行。
- R3.Unity 使用已缓存的真实包源码；Cinemachine 使用包中核心源码，不开启未使用的 Storyboard/uGUI 与 Timeline 集成。不是伪造 API。
- 警告包含 Inspector 序列化字段在独立 C# 编译下的 CS0649、TimeProvider 的 netstandard 引用统一 CS1701，以及第三方 Cinemachine 的过时 API 警告。没有通过禁用错误来伪造通过结果。
- 之前包含 Cinemachine Storyboard 分支的检查失败已在第 1 节记录；未修改第三方源码来消除它。
- 暂存证据目录：`C:/Users/Windows11/AppData/Local/Temp/WarChess-R3-Validation-4cff28b908a24fad9307fed3e3b203bc`。目录可能被系统清理，不应作为长期依赖。

### 尚未执行的必要验收

Unity 整体编译/Console、场景手动装配后 Play Mode、实际动画与血条视觉、暂停恢复、菜单往返、重进场景订阅数量、IL2CPP/玩家构建、性能测试均未完成。没有已连接的 Unity MCP，也没有启动 Editor 自动改动项目。

## 8. 后续扩展与新对话入口

- 加行动：增加 UnitActionType、IActionHandler 实现、容器注册、对应 View 表现和测试。
- 改数值：优先使用 UnitDefinitionAsset；改费用/减伤则修改 Handler 并更新验证。
- 动态生成单位：当前是场景静态名单。以后需要场景级工厂同时创建 Model/View/Presenter，并明确销毁时解除占位和订阅；不能只 Instantiate 一个 UnitView。
- 加存档：保存领域快照，不序列化 ReactiveProperty、Presenter、Unity 引用或容器。
- 加网络：先设计服务器权威的 ActionRequest/ActionResult 边界，不让本地动画产生权威伤害。

下次对话可直接说：

> 请先阅读 Assets/Scripts/Documentation/TechnicalGuide.md 和 SceneSetup.md，再检查当前 Scripts、场景挂载和包依赖是否有变化。当前采用 Model–Presenter–Service、R3 只读状态与意图流、VContainer 场景作用域。不要假设场景已经迁移，也不要把独立编译当作 Unity Play Mode 已通过。我的新需求是：……
