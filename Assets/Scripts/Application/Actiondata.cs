using System;
using System.Collections.Generic;
using WarChess.Domain;

namespace WarChess.Application
{
    // 某个行动在当前状态下的预览结果，包含可选目标以及行动对应的路径
    public sealed class ActionPreview
    {
        private readonly Dictionary<GridCoord, IReadOnlyList<GridCoord>> _paths;

        public bool IsAllowed { get; }
        public string FailureReason { get; }
        public IReadOnlyList<GridCoord> Target { get; }

        private ActionPreview(
            bool isAllowed,
            string failureReason,
            IReadOnlyList<GridCoord> target,
            Dictionary<GridCoord, IReadOnlyList<GridCoord>> paths)
        {
            IsAllowed = isAllowed;
            FailureReason = failureReason ?? string.Empty;
            Target = target ?? Array.Empty<GridCoord>();
            _paths = paths ?? new Dictionary<GridCoord, IReadOnlyList<GridCoord>>();
        }

        public static ActionPreview Failure(string reason)
        {
            // Presenter 可直接显示失败原因
            return new ActionPreview(false, reason, Array.Empty<GridCoord>(), 
                new Dictionary<GridCoord, IReadOnlyList<GridCoord>>());
        }

        public static ActionPreview Success(IReadOnlyList<GridCoord> target, 
            Dictionary<GridCoord, IReadOnlyList<GridCoord>> paths = null)
        {
            return new ActionPreview(true, string.Empty, target, paths);
        }

        public bool Contains(GridCoord coord)
        {
            for (int i = 0; i < Target.Count; i++)
            {
                if (Target[i] == coord)
                {
                    return true;
                }
            }
            return false;
        }

        public IReadOnlyList<GridCoord> GetPath(GridCoord target)
        {
            return _paths.TryGetValue(target, out IReadOnlyList<GridCoord> path) ? 
                path : Array.Empty<GridCoord>();
        }
    }

    // 一次行动请求，执行什么行动、目标格在哪里
    public readonly struct ActionRequest
    {
        public UnitId ActorId { get; }
        public UnitActionType ActionType { get; }
        public GridCoord Target { get; }

        public ActionRequest(UnitId actorId, UnitActionType actionType, GridCoord target)
        {
            ActorId = actorId;
            ActionType = actionType;
            Target = target;
        }
    }

    // 行动执行后的不可变结果
    public sealed class ActionResult
    {
        public bool Succeeded { get; }
        public string FailureReason { get; }
        public ActionRequest Request { get; }
        public IReadOnlyList<GridCoord> Path { get; }
        public UnitId? TargetUnitId { get; }
        public int Damage { get;  }
        public bool TargetDied { get; }

        private ActionResult(
            bool success,
            string failureReason,
            ActionRequest request,
            IReadOnlyList<GridCoord> path,
            UnitId? targetUnitId,
            int damage,
            bool targetDied)
        {
            Succeeded = success;
            FailureReason = failureReason ?? string.Empty;
            Request = request;
            Path = path ?? Array.Empty<GridCoord>();
            TargetUnitId = targetUnitId;
            Damage = damage;
            TargetDied = targetDied;
        }

        public static ActionResult Failure(ActionRequest request, string reason)
        {
            return new ActionResult(false, reason, request, null, null, 0, false);
        }

        public static ActionResult Success(ActionRequest request,
            IReadOnlyList<GridCoord> path = null,
            UnitId? targetUnitId = null,
            int damage = 0,
            bool targetDied = false)
        {
            return new ActionResult(true, string.Empty, request, 
                path, targetUnitId, damage, targetDied);
        }


    }

    // AI 选出的行动请求及其评分
    public readonly struct AiDecision
    {
        public ActionRequest Request { get; }
        public int Score { get; }
        public AiDecision(ActionRequest request, int score)
        {
            Request = request;
            Score = score;
        }
    }
}
