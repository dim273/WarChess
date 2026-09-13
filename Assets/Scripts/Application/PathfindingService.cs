using System.Collections.Generic;
using WarChess.Domain;

namespace WarChess.Application
{
    public interface IPathfindingService
    {
        Dictionary<GridCoord, IReadOnlyList<GridCoord>> FindReachable(BoardModel board, GridCoord start, int range);
    }

    /// <summary>四方向、每格代价为 1 的广度优先搜索；路径包含起点，不穿越角色或障碍。</summary>
    public sealed class PathfindingService : IPathfindingService
    {
        private static readonly GridCoord[] Directions =
        {
            new GridCoord(1, 0), new GridCoord(-1, 0),
            new GridCoord(0, 1), new GridCoord(0, -1)
        };

        public Dictionary<GridCoord, IReadOnlyList<GridCoord>> FindReachable(BoardModel board, GridCoord start, int range)
        {
            var paths = new Dictionary<GridCoord, IReadOnlyList<GridCoord>>();
            if (!board.IsWalkable(start) || range <= 0) return paths;
            var queue = new Queue<GridCoord>();
            var distance = new Dictionary<GridCoord, int> { [start] = 0 };
            var previous = new Dictionary<GridCoord, GridCoord>();
            queue.Enqueue(start);
            while (queue.Count > 0)
            {
                GridCoord current = queue.Dequeue();
                if (distance[current] >= range) continue;
                foreach (GridCoord direction in Directions)
                {
                    GridCoord next = current + direction;
                    if (distance.ContainsKey(next) || !board.IsWalkable(next) || board.IsOccupied(next)) continue;
                    distance.Add(next, distance[current] + 1);
                    previous.Add(next, current);
                    queue.Enqueue(next);
                    var path = new List<GridCoord> { next };
                    for (GridCoord node = next; node != start;)
                    {
                        node = previous[node];
                        path.Add(node);
                    }
                    path.Reverse();
                    paths.Add(next, path.AsReadOnly());
                }
            }
            return paths;
        }
    }

    public interface ILineOfSightService
    {
        bool HasLineOfSight(BoardModel board, GridCoord from, GridCoord to);
    }

    /// <summary>格子中心连线的 supercover 检查：穿过格角时两侧都检查，避免隔墙角攻击。</summary>
    public sealed class GridLineOfSightService : ILineOfSightService
    {
        public bool HasLineOfSight(BoardModel board, GridCoord from, GridCoord to)
        {
            if (!board.IsWalkable(from) || !board.IsWalkable(to)) return false;
            int dx = System.Math.Abs(to.X - from.X), dz = System.Math.Abs(to.Z - from.Z);
            int sx = System.Math.Sign(to.X - from.X), sz = System.Math.Sign(to.Z - from.Z);
            int x = from.X, z = from.Z, ix = 0, iz = 0;
            while (ix < dx || iz < dz)
            {
                long decision = (1L + 2L * ix) * dz - (1L + 2L * iz) * dx;
                if (decision == 0)
                {
                    if (!board.IsWalkable(new GridCoord(x + sx, z)) ||
                        !board.IsWalkable(new GridCoord(x, z + sz))) return false;
                    x += sx; z += sz; ix++; iz++;
                }
                else if (decision < 0) { x += sx; ix++; }
                else { z += sz; iz++; }
                if (!board.IsWalkable(new GridCoord(x, z))) return false;
            }
            // 单位不遮挡射线；静态障碍由 BoardModel 的可通行性统一表达。
            return true;
        }
    }
}
