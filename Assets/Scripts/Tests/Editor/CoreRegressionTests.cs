#if UNITY_EDITOR || WARCHESS_STANDALONE_TESTS
using System;
using System.Collections.Generic;
using R3;
using VContainer;
using WarChess.Domain;
using WarChess.Application;
using WarChess.Presentation;

namespace WarChess.Tests
{
    /// <summary>
    /// 无场景依赖的核心回归检查。Unity 菜单 WarChess/Validation/Run Core Regression Tests 执行。
    /// 独立验证时定义 WARCHESS_STANDALONE_TESTS，使用真实 R3 和 VContainer 程序集。
    /// 不创建/保存场景、预制体，也不改动项目配置。
    /// </summary>
    public static class CoreRegressionTests
    {
#if UNITY_EDITOR
        [UnityEditor.MenuItem("WarChess/Validation/Run Core Regression Tests")]
        public static void RunInEditor() => RunAll(message => UnityEngine.Debug.Log(message));
#endif
#if WARCHESS_STANDALONE_TESTS
        public static int Main()
        {
            try { RunAll(Console.WriteLine); return 0; }
            catch (Exception error) { Console.Error.WriteLine(error); return 1; }
        }
#endif
        public static void RunAll(Action<string> log)
        {
            int passed = 0, failed = 0;
            void Test(string name, Action test)
            {
                try { test(); passed++; log("PASS " + name); }
                catch (Exception error) { failed++; log("FAIL " + name + ": " + error); }
            }
            Test("Reject default UnitId", () => Throws(() => Definition(default, Team.Player, new GridCoord(0, 0))));
            Test("Reject blocked spawn", () =>
            {
                using var b = new BattleModel(new BoardModel(3, 3));
                b.Board.SetWalkable(new GridCoord(0, 0), false);
                using var unit = new UnitModel(Definition(new UnitId("p"), Team.Player, new GridCoord(0, 0)));
                Throws(() => b.AddUnit(unit));
                Check(!b.Board.IsOccupied(unit.Position));
            });
            Test("Reject duplicate occupancy", () =>
            {
                using var b = Battle();
                using var other = new UnitModel(Definition(new UnitId("other"), Team.Enemy, new GridCoord(0, 0)));
                Throws(() => b.AddUnit(other));
                Check(b.Board.TryGetUnitAt(other.Position, out var id) && id == P);
            });
            Test("BFS respects range and occupied cells", () =>
            {
                using var b = Battle();
                b.Board.SetWalkable(new GridCoord(1, 0), false);
                var paths = new PathfindingService().FindReachable(b.Board, new GridCoord(0, 0), 2);
                Check(!paths.ContainsKey(new GridCoord(1, 0)) && !paths.ContainsKey(new GridCoord(0, 0)));
                Check(paths[new GridCoord(1, 1)].Count == 3);
                Check(!paths.ContainsKey(new GridCoord(3, 0)));
            });
            Test("Move updates AP position and occupancy", () =>
            {
                using var b = Battle();
                ActionResult result = Actions().TryExecute(b, Request(P, UnitActionType.Move, 0, 2));
                Check(result.Succeeded && result.Path.Count == 3 && b.GetUnit(P).ActionPoints == 1);
                Check(!b.Board.IsOccupied(new GridCoord(0, 0)) && b.Board.IsOccupied(new GridCoord(0, 2)));
            });
            Test("Invalid move is non-mutating", () =>
            {
                using var b = Battle();
                var actor = b.GetUnit(P);
                Check(!Actions().TryExecute(b, Request(P, UnitActionType.Move, -1, 0)).Succeeded);
                Check(actor.ActionPoints == 2 && actor.Position == new GridCoord(0, 0));
            });
            Test("Reject wrong team and unknown action", () =>
            {
                using var b = Battle();
                var actions = Actions();
                Check(!actions.TryExecute(b, Request(E, UnitActionType.Defend, 3, 0)).Succeeded);
                Check(!actions.TryExecute(b, Request(P, (UnitActionType)999, 0, 0)).Succeeded);
            });
            Test("Reject insufficient AP", () =>
            {
                using var b = Battle();
                b.GetUnit(P).SpendActionPoints(2);
                Check(!Actions().TryExecute(b, Request(P, UnitActionType.Move, 0, 1)).Succeeded);
            });
            Test("Wall blocks attack", () =>
            {
                using var b = Battle();
                b.Board.SetWalkable(new GridCoord(1, 0), false);
                Check(!Actions().TryExecute(b, Request(P, UnitActionType.Attack, 3, 0)).Succeeded);
            });
            Test("LOS corner cannot cut blocked diagonal", () =>
            {
                var board = new BoardModel(3, 3);
                board.SetWalkable(new GridCoord(1, 0), false);
                Check(!new GridLineOfSightService().HasLineOfSight(board, new GridCoord(0, 0), new GridCoord(1, 1)));
            });
            Test("Defend only self and once", () =>
            {
                using var b = Battle();
                var actions = Actions();
                Check(!actions.TryExecute(b, Request(P, UnitActionType.Defend, 0, 1)).Succeeded);
                Check(actions.TryExecute(b, Request(P, UnitActionType.Defend, 0, 0)).Succeeded);
                Check(!actions.TryExecute(b, Request(P, UnitActionType.Defend, 0, 0)).Succeeded);
                Check(b.GetUnit(P).ActionPoints == 1 && b.GetUnit(P).IsDefending);
            });
            Test("Defend halves damage", () =>
            {
                using var b = Battle();
                b.GetUnit(E).SetDefending();
                var result = Actions().TryExecute(b, Request(P, UnitActionType.Attack, 3, 0));
                Check(result.Succeeded && result.Damage == 20 && b.GetUnit(E).Health == 80);
            });
            Test("Death frees grid and ends battle", () =>
            {
                using var b = Battle();
                b.GetUnit(E).ApplyDamage(90);
                var actions = Actions();
                var result = actions.TryExecute(b, Request(P, UnitActionType.Attack, 3, 0));
                Check(result.Succeeded && result.Damage == 10 && result.TargetDied);
                Check(!b.Board.IsOccupied(new GridCoord(3, 0)) && b.Phase == BattlePhase.Completed && b.Winner == Team.Player);
                Check(!actions.TryExecute(b, Request(P, UnitActionType.Move, 0, 1)).Succeeded);
                new TurnService().EndTurn(b);
                Check(b.Turn.ActiveTeam == Team.Player);
            });
            Test("Turn restores only incoming team and expires defend", () =>
            {
                using var b = Battle();
                var turns = new TurnService();
                b.GetUnit(P).SpendActionPoints(1);
                b.GetUnit(P).SetDefending();
                b.GetUnit(E).SpendActionPoints(2);
                turns.EndTurn(b);
                Check(b.GetUnit(E).ActionPoints == 2 && b.GetUnit(P).ActionPoints == 1 && b.GetUnit(P).IsDefending);
                turns.EndTurn(b);
                Check(b.Turn.Number == 2 && b.GetUnit(P).ActionPoints == 2 && !b.GetUnit(P).IsDefending);
            });
            Test("Animating phase rejects actions and turn switch", () =>
            {
                using var b = Battle();
                b.SetPhase(BattlePhase.Animating);
                new TurnService().EndTurn(b);
                Check(b.Turn.ActiveTeam == Team.Player && !Actions().GetPreview(b, P, UnitActionType.Move).IsAllowed);
            });
            Test("AI uses finite AP and chooses attack", () =>
            {
                using var b = Battle();
                new TurnService().EndTurn(b);
                var actions = Actions();
                var ai = new AiDecisionService(actions);
                Check(ai.TryDecide(b, out var first) && first.Request.ActionType == UnitActionType.Attack);
                int count = 0;
                while (ai.TryDecide(b, out var decision))
                {
                    Check(actions.TryExecute(b, decision.Request).Succeeded);
                    Check(++count <= 2);
                }
                Check(count == 2);
            });
            Test("R3 emits initial state changes and unsubscribes", () =>
            {
                using var b = Battle();
                int calls = 0, value = -1;
                var subscription = b.GetUnit(P).HealthChanged.Subscribe(v => { value = v; calls++; });
                b.GetUnit(P).ApplyDamage(10);
                b.GetUnit(P).ApplyDamage(0);
                Check(value == 90 && calls == 2);
                subscription.Dispose();
                b.GetUnit(P).ApplyDamage(1);
                Check(calls == 2);
            });
            Test("R3 cannot reenter action transaction", () =>
            {
                using var b = Battle();
                var actions = Actions();
                bool nestedSucceeded = false;
                using var subscription = b.GetUnit(P).ActionPointsChanged.Skip(1).Subscribe(_ =>
                    nestedSucceeded = actions.TryExecute(b, Request(P, UnitActionType.Defend, 0, 1)).Succeeded);
                Check(actions.TryExecute(b, Request(P, UnitActionType.Move, 0, 1)).Succeeded);
                Check(!nestedSucceeded && b.GetUnit(P).ActionPoints == 1);
            });
            Test("Presenter defends when clicking selected friendly", () =>
            {
                using var fixture = new PresenterFixture();
                fixture.Hud.Actions.OnNext(UnitActionType.Defend);
                fixture.Input.Units.OnNext(P);
                Check(fixture.Battle.GetUnit(P).IsDefending && fixture.Battle.Phase == BattlePhase.PlayerInput);
            });
            Test("Presenter cancels AI timer on disposal", () =>
            {
                using var fixture = new PresenterFixture();
                fixture.Hud.End.OnNext(Unit.Default);
                Check(fixture.Input.Pending != null);
                fixture.Presenter.Dispose();
                Check(fixture.Input.Pending == null);
                fixture.Hud.End.OnNext(Unit.Default);
                Check(fixture.Battle.Turn.ActiveTeam == Team.Enemy);
            });
            Test("Late animation callback is ignored after disposal", () =>
            {
                using var fixture = new PresenterFixture();
                fixture.PlayerView.Defer = true;
                fixture.Input.Cells.OnNext(new GridCoord(0, 1));
                Check(fixture.Battle.Phase == BattlePhase.Animating);
                var completion = fixture.PlayerView.Completion;
                fixture.Presenter.Dispose();
                int calls = fixture.Hud.Calls;
                completion?.Invoke();
                Check(fixture.Hud.Calls == calls);
            });
            Test("Menu starts once and unsubscribes", () =>
            {
                var view = new FakeMenu();
                var scene = new FakeScenes();
                var presenter = new MenuPresenter(view, scene, "Game");
                presenter.Start(); presenter.Start();
                view.Start.OnNext(Unit.Default); view.Start.OnNext(Unit.Default);
                Check(scene.Loads == 1);
                presenter.Dispose();
                view.Quit.OnNext(Unit.Default);
                Check(scene.Quits == 0);
                view.Start.Dispose(); view.Quit.Dispose();
            });
            Test("VContainer resolves strategies and disposes Presenter", () =>
            {
                using var b = Battle();
                var input = new FakeInput();
                var hud = new FakeHud();
                var builder = new ContainerBuilder();
                builder.RegisterInstance(b);
                builder.RegisterInstance<IBattleInputView>(input);
                builder.RegisterInstance<IBattleHudView>(hud);
                builder.RegisterInstance<IBoardView>(new FakeBoard());
                builder.RegisterInstance<IEnumerable<UnitPresenter>>(new[]
                {
                    new UnitPresenter(b.GetUnit(P), new FakeUnit()), new UnitPresenter(b.GetUnit(E), new FakeUnit())
                });
                builder.Register<IPathfindingService, PathfindingService>(Lifetime.Scoped);
                builder.Register<ILineOfSightService, GridLineOfSightService>(Lifetime.Scoped);
                builder.Register<IActionHandler, MoveActionHandler>(Lifetime.Scoped);
                builder.Register<IActionHandler, AttackActionHandler>(Lifetime.Scoped);
                builder.Register<IActionHandler, DefendActionHandler>(Lifetime.Scoped);
                builder.Register<VictoryService>(Lifetime.Scoped);
                builder.Register<IActionService, ActionService>(Lifetime.Scoped);
                builder.Register<TurnService>(Lifetime.Scoped);
                builder.Register<AiDecisionService>(Lifetime.Scoped);
                builder.Register<BoardPresenter>(Lifetime.Scoped);
                builder.Register<BattlePresenter>(Lifetime.Scoped).WithParameter("aiThinkDelay", 0.4f);
                var container = builder.Build();
                container.Resolve<BattlePresenter>().Start();
                hud.Actions.OnNext(UnitActionType.Defend);
                input.Units.OnNext(P);
                Check(b.GetUnit(P).IsDefending);
                container.Dispose();
                int calls = hud.Calls;
                hud.End.OnNext(Unit.Default);
                Check(hud.Calls == calls && b.Turn.ActiveTeam == Team.Player);
                input.Units.Dispose(); input.Cells.Dispose(); hud.Actions.Dispose(); hud.End.Dispose();
            });
            log($"RESULT: {passed} passed, {failed} failed");
            if (failed != 0) throw new InvalidOperationException($"{failed} core regression tests failed.");
        }

        private static readonly UnitId P = new UnitId("player");
        private static readonly UnitId E = new UnitId("enemy");
        private static void Check(bool condition) { if (!condition) throw new Exception("Assertion failed."); }
        private static void Throws(Action action)
        {
            bool thrown = false;
            try { action(); } catch (Exception) { thrown = true; }
            Check(thrown);
        }
        private static UnitDefinition Definition(UnitId id, Team team, GridCoord position) =>
            new UnitDefinition(id, team, position, 100, 2, 4, 4, 40,
                new[] { UnitActionType.Move, UnitActionType.Attack, UnitActionType.Defend });
        private static BattleModel Battle()
        {
            var battle = new BattleModel(new BoardModel(6, 6));
            battle.AddUnit(new UnitModel(Definition(P, Team.Player, new GridCoord(0, 0))));
            battle.AddUnit(new UnitModel(Definition(E, Team.Enemy, new GridCoord(3, 0))));
            return battle;
        }
        private static ActionRequest Request(UnitId actor, UnitActionType action, int x, int z) =>
            new ActionRequest(actor, action, new GridCoord(x, z));
        private static IActionService Actions() => new ActionService(new IActionHandler[]
        {
            new MoveActionHandler(new PathfindingService()),
            new AttackActionHandler(new GridLineOfSightService()), new DefendActionHandler()
        }, new VictoryService());

        private sealed class PresenterFixture : IDisposable
        {
            public readonly BattleModel Battle = CoreRegressionTests.Battle();
            public readonly FakeInput Input = new FakeInput();
            public readonly FakeHud Hud = new FakeHud();
            public readonly FakeUnit PlayerView = new FakeUnit();
            public readonly BattlePresenter Presenter;
            public PresenterFixture()
            {
                var actions = Actions();
                Presenter = new BattlePresenter(Battle, actions, new TurnService(), new AiDecisionService(actions),
                    Input, Hud, new BoardPresenter(new FakeBoard()),
                    new[] { new UnitPresenter(Battle.GetUnit(P), PlayerView), new UnitPresenter(Battle.GetUnit(E), new FakeUnit()) }, 0.4f);
                Presenter.Start();
            }
            public void Dispose()
            {
                Presenter.Dispose(); Battle.Dispose();
                Input.Units.Dispose(); Input.Cells.Dispose(); Hud.Actions.Dispose(); Hud.End.Dispose();
            }
        }
        private sealed class FakeInput : IBattleInputView
        {
            public readonly Subject<UnitId> Units = new Subject<UnitId>();
            public readonly Subject<GridCoord> Cells = new Subject<GridCoord>();
            public Observable<UnitId> UnitClicked => Units;
            public Observable<GridCoord> CellClicked => Cells;
            public Action Pending;
            public void SetInputEnabled(bool enabled) { }
            public IDisposable Schedule(float delay, Action callback)
            {
                Pending = callback;
                return Disposable.Create(() => Pending = null);
            }
        }
        private sealed class FakeHud : IBattleHudView
        {
            public readonly Subject<UnitActionType> Actions = new Subject<UnitActionType>();
            public readonly Subject<Unit> End = new Subject<Unit>();
            public Observable<UnitActionType> ActionSelected => Actions;
            public Observable<Unit> EndTurnRequested => End;
            public int Calls;
            public void SetTurn(int number, Team team) { Calls++; }
            public void SetSelectedUnit(string id, int ap, int max) { Calls++; }
            public void SetActionState(UnitActionType action, bool move, bool attack, bool defend) { Calls++; }
            public void SetBusy(bool busy) { Calls++; }
            public void SetStatus(string message) { Calls++; }
            public void ShowBattleResult(Team team) { Calls++; }
        }
        private sealed class FakeBoard : IBoardView
        {
            public void Initialize(int width, int height, float size) { }
            public void ClearHighlights() { }
            public void ShowHighlights(IReadOnlyList<GridCoord> cells, BoardHighlight highlight) { }
        }
        private sealed class FakeUnit : IUnitView
        {
            public UnitDefinition Definition => null;
            public bool Defer;
            public Action Completion;
            public void Initialize(GridCoord position) { }
            public void CancelAnimation() { }
            public void SetSelected(bool selected) { }
            public void SetHealth(int value, int max) { }
            public void SetActionPoints(int value, int max) { }
            public void PlayMove(IReadOnlyList<GridCoord> path, Action completed)
            {
                Completion = completed;
                if (!Defer) completed();
            }
            public void PlayAttack(GridCoord target, Action hit, Action completed) { hit(); completed(); }
            public void PlayDefend(Action completed) { completed(); }
            public void PlayHit(int damage, bool died) { }
        }
        private sealed class FakeMenu : IMenuView
        {
            public readonly Subject<Unit> Start = new Subject<Unit>();
            public readonly Subject<Unit> Quit = new Subject<Unit>();
            public Observable<Unit> NewGameRequested => Start;
            public Observable<Unit> QuitRequested => Quit;
        }
        private sealed class FakeScenes : ISceneService
        {
            public int Loads, Quits;
            public void LoadScene(string name) { Loads++; }
            public void Quit() { Quits++; }
        }
    }
}
#endif
