using System;
using System.Collections.Generic;
using UnityEngine;
using VContainer;
using VContainer.Unity;
using WarChess.Domain;
using WarChess.Application;
using WarChess.Presentation;
using WarChess.UnityViews;

namespace WarChess.Composition
{
    /// <summary>战斗场景作用域。唯一允许知道完整对象图的位置，不提供全局 Instance。</summary>
    public sealed class BattleInstaller : LifetimeScope
    {
        [Header("Board")]
        [SerializeField] private int width = 35;
        [SerializeField] private int height = 28;
        [SerializeField] private float cellSize = 2f;
        [SerializeField] private Vector2Int[] blockedCells = Array.Empty<Vector2Int>();
        [Tooltip("从现有障碍碰撞体采样；只选 Obstacle 层，不要包含地板或角色。")]
        [SerializeField] private LayerMask obstacleLayerMask = 1 << 8;

        [Header("Scene views")]
        [SerializeField] private BoardView boardView;
        [SerializeField] private BattleInputView inputView;
        [SerializeField] private BattleHudView hudView;
        [SerializeField] private UnitView[] unitViews = Array.Empty<UnitView>();
        [SerializeField, Min(0f)] private float aiThinkDelay = 0.4f;

        [SerializeField] private SceneControlsView sceneControls;

        protected override void Configure(IContainerBuilder builder)
        {
            if (width <= 0 || height <= 0 || cellSize <= 0f)
                throw new InvalidOperationException("Invalid board dimensions.");
            if (boardView == null || inputView == null || hudView == null)
                throw new InvalidOperationException("BattleInstaller requires Board/Input/Hud views.");
            if (unitViews == null || unitViews.Length == 0)
                throw new InvalidOperationException("BattleInstaller requires UnitViews.");
            var uniqueViews = new HashSet<UnitView>();
            foreach (UnitView view in unitViews)
                if (view == null || !uniqueViews.Add(view) || !view.gameObject.activeInHierarchy)
                    throw new InvalidOperationException("UnitViews must be active, non-null and unique.");

            builder.Register<ISceneService, UnitySceneService>(Lifetime.Scoped);
            if (sceneControls != null) builder.RegisterComponent(sceneControls);
            boardView.ConfigureGeometry(cellSize);
            builder.RegisterComponent(boardView).As<IBoardView>().AsSelf();
            builder.RegisterComponent(inputView).As<IBattleInputView>();
            builder.RegisterComponent(hudView).As<IBattleHudView>();
            // 同类型组件使用不同键，否则 RegisterComponent 的回调只会解析最后一个单位。
            foreach (UnitView view in unitViews) builder.RegisterComponent(view).Keyed(view);
            builder.Register<IPathfindingService, PathfindingService>(Lifetime.Scoped);
            builder.Register<ILineOfSightService, GridLineOfSightService>(Lifetime.Scoped);
            builder.Register<IActionHandler, MoveActionHandler>(Lifetime.Scoped);
            builder.Register<IActionHandler, AttackActionHandler>(Lifetime.Scoped);
            builder.Register<IActionHandler, DefendActionHandler>(Lifetime.Scoped);
            builder.Register<VictoryService>(Lifetime.Scoped);
            builder.Register<TurnService>(Lifetime.Scoped);
            builder.Register<IActionService, ActionService>(Lifetime.Scoped);
            builder.Register<AiDecisionService>(Lifetime.Scoped);
            builder.Register<BoardPresenter>(Lifetime.Scoped);
            builder.Register<BattleModel>(_ => CreateBattle(), Lifetime.Scoped);
            builder.Register<IEnumerable<UnitPresenter>>(resolver =>
            {
                BattleModel battle = resolver.Resolve<BattleModel>();
                var presenters = new List<UnitPresenter>(unitViews.Length);
                foreach (UnitView view in unitViews)
                    presenters.Add(new UnitPresenter(battle.GetUnit(view.UnitId), view));
                return presenters;
            }, Lifetime.Scoped);
            builder.Register<BattlePresenter>(Lifetime.Scoped).WithParameter("aiThinkDelay", aiThinkDelay);
            builder.RegisterEntryPoint<BattleSceneEntryPoint>()
                .WithParameter("width", width).WithParameter("height", height).WithParameter("cellSize", cellSize);
        }

        private BattleModel CreateBattle()
        {
            // 这里处于组成根的工厂边界，不把 Physics 和场景读取带入 Service。
            var board = new BoardModel(width, height);
            var battle = new BattleModel(board);
            try
            {
                if (obstacleLayerMask.value != 0)
                {
                    Physics.SyncTransforms();
                    for (int x = 0; x < width; x++)
                    for (int z = 0; z < height; z++)
                    {
                        var coord = new GridCoord(x, z);
                        Vector3 center = boardView.GridToWorld(coord) + boardView.transform.up;
                        if (Physics.CheckBox(center, new Vector3(cellSize * 0.45f, 0.9f, cellSize * 0.45f),
                            boardView.transform.rotation, obstacleLayerMask, QueryTriggerInteraction.Ignore))
                            board.SetWalkable(coord, false);
                    }
                }
                foreach (Vector2Int coord in blockedCells ?? Array.Empty<Vector2Int>())
                    board.SetWalkable(new GridCoord(coord.x, coord.y), false);
                foreach (UnitView view in unitViews)
                {
                    // 工厂可能先于组件注入回调求值；显式传坐标依赖，不依赖 Awake 顺序。
                    view.BindBoard(boardView);
                    var unit = new UnitModel(view.Definition);
                    try { battle.AddUnit(unit); }
                    catch { unit.Dispose(); throw; }
                }
                if (battle.GetLivingUnits(Team.Player).Count == 0 || battle.GetLivingUnits(Team.Enemy).Count == 0)
                    throw new InvalidOperationException("Both Player and Enemy units are required.");
                return battle;
            }
            catch { battle.Dispose(); throw; }
        }
    }

    /// <summary>等待场景 Awake 完成再初始化显示，避免 HUD.Awake 覆盖 Presenter 已设置的状态。</summary>
    public sealed class BattleSceneEntryPoint : IStartable, IDisposable
    {
        private readonly IBoardView _board;
        private readonly BattlePresenter _presenter;
        private readonly int _width, _height;
        private readonly float _cellSize;
        public BattleSceneEntryPoint(IBoardView board, BattlePresenter presenter, int width, int height, float cellSize)
        {
            _board = board; _presenter = presenter;
            _width = width; _height = height; _cellSize = cellSize;
        }
        public void Start()
        {
            _board.Initialize(_width, _height, _cellSize);
            _presenter.Start();
        }
        public void Dispose() => _presenter.Dispose();
    }
}
