using System;
using System.Collections.Generic;
using WarChess.Domain;

namespace WarChess.Application
{
    /// <summary>策略只处理特定行动；回合、阵营和行动点的共同校验集中在 ActionService。</summary>
    public interface IActionHandler
    {
        UnitActionType Type { get; }
        int Cost { get; }
        ActionPreview Preview(BattleModel battle, UnitModel actor);
        ActionResult Execute(BattleModel battle, UnitModel actor, ActionRequest request, ActionPreview preview);
    }

    public interface IActionService
    {
        int GetCost(UnitActionType action);
        ActionPreview GetPreview(BattleModel battle, UnitId actor, UnitActionType action);
        ActionResult TryExecute(BattleModel battle, ActionRequest request);
    }

    public sealed class MoveActionHandler : IActionHandler
    {
        private readonly IPathfindingService _paths;
        public UnitActionType Type => UnitActionType.Move;
        public int Cost => 1;
        public MoveActionHandler(IPathfindingService paths) { _paths = paths; }

        public ActionPreview Preview(BattleModel battle, UnitModel actor)
        {
            var paths = _paths.FindReachable(battle.Board, actor.Position, actor.MoveRange);
            return ActionPreview.Success(new List<GridCoord>(paths.Keys).AsReadOnly(), paths);
        }

        public ActionResult Execute(BattleModel battle, UnitModel actor, ActionRequest request, ActionPreview preview)
        {
            battle.Board.MoveUnit(actor, request.Target);
            return ActionResult.Success(request, preview.GetPath(request.Target));
        }
    }

    public sealed class AttackActionHandler : IActionHandler
    {
        private readonly ILineOfSightService _sight;
        public UnitActionType Type => UnitActionType.Attack;
        public int Cost => 1;
        public AttackActionHandler(ILineOfSightService sight) { _sight = sight; }

        public ActionPreview Preview(BattleModel battle, UnitModel actor)
        {
            var targets = new List<GridCoord>();
            foreach (UnitModel target in battle.Units)
            {
                if (target.IsAlive && target.Team != actor.Team &&
                    actor.Position.ManhattanDistance(target.Position) <= actor.AttackRange &&
                    _sight.HasLineOfSight(battle.Board, actor.Position, target.Position))
                    targets.Add(target.Position);
            }
            return ActionPreview.Success(targets.AsReadOnly());
        }

        public ActionResult Execute(BattleModel battle, UnitModel actor, ActionRequest request, ActionPreview preview)
        {
            battle.Board.TryGetUnitAt(request.Target, out UnitId targetId);
            UnitModel target = battle.GetUnit(targetId);
            // 防御减伤 50%，奇数向上取整；防御持续到防御者下一次回合开始。
            int damage = target.IsDefending ? actor.AttackDamage / 2 + actor.AttackDamage % 2 : actor.AttackDamage;
            int applied = target.ApplyDamage(damage);
            battle.RemoveDeadUnitFromBoard(target);
            return ActionResult.Success(request, targetUnitId: target.Id, damage: applied, targetDied: !target.IsAlive);
        }
    }

    public sealed class DefendActionHandler : IActionHandler
    {
        public UnitActionType Type => UnitActionType.Defend;
        public int Cost => 1;
        public ActionPreview Preview(BattleModel battle, UnitModel actor)
        {
            return actor.IsDefending ? ActionPreview.Failure("Already defending.") :
                ActionPreview.Success(new[] { actor.Position });
        }
        public ActionResult Execute(BattleModel battle, UnitModel actor, ActionRequest request, ActionPreview preview)
        {
            actor.SetDefending();
            return ActionResult.Success(request);
        }
    }

    /// <summary>所有行动的唯一业务入口。每次执行重新预览，绝不信任 UI 的旧高亮。</summary>
    public sealed class ActionService : IActionService
    {
        private readonly Dictionary<UnitActionType, IActionHandler> _handlers = new Dictionary<UnitActionType, IActionHandler>();
        private readonly VictoryService _victory;
        private bool _executing;

        public ActionService(IEnumerable<IActionHandler> handlers, VictoryService victory)
        {
            _victory = victory;
            foreach (IActionHandler handler in handlers) _handlers.Add(handler.Type, handler);
        }

        public int GetCost(UnitActionType action) => _handlers.TryGetValue(action, out var handler) ? handler.Cost : int.MaxValue;

        public ActionPreview GetPreview(BattleModel battle, UnitId actorId, UnitActionType action)
        {
            if (_executing) return ActionPreview.Failure("Action is being resolved.");
            if (battle.Phase != BattlePhase.PlayerInput && battle.Phase != BattlePhase.EnemyThinking)
                return ActionPreview.Failure("Battle is not accepting actions.");
            if (!battle.TryGetUnit(actorId, out UnitModel actor) || !actor.IsAlive)
                return ActionPreview.Failure("Unit is unavailable.");
            if (actor.Team != battle.Turn.ActiveTeam) return ActionPreview.Failure("Not this unit's turn.");
            if (!_handlers.TryGetValue(action, out IActionHandler handler) || !actor.HasAction(action))
                return ActionPreview.Failure("Action is unavailable.");
            if (!actor.CanSpendActionPoints(handler.Cost)) return ActionPreview.Failure("Not enough action points.");
            return handler.Preview(battle, actor);
        }

        public ActionResult TryExecute(BattleModel battle, ActionRequest request)
        {
            ActionPreview preview = GetPreview(battle, request.ActorId, request.ActionType);
            if (!preview.IsAllowed) return ActionResult.Failure(request, preview.FailureReason);
            if (!preview.Contains(request.Target)) return ActionResult.Failure(request, "Target is not legal.");
            // R3 状态通知是同步的；防止订阅者在结算过程中重入并重复消耗行动点。
            _executing = true;
            try
            {
                UnitModel actor = battle.GetUnit(request.ActorId);
                IActionHandler handler = _handlers[request.ActionType];
                ActionResult result = handler.Execute(battle, actor, request, preview);
                actor.SpendActionPoints(handler.Cost);
                _victory.Evaluate(battle);
                return result;
            }
            finally { _executing = false; }
        }
    }
}
