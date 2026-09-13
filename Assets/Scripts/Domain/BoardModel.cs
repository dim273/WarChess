using System;
using System.Collections.Generic;

namespace WarChess.Domain
{
    // 棋盘模型，统一维护可通行性与格子占用关系，保证一个格子最多存在一个角色
    public sealed class BoardModel
    {
        private readonly bool[,] _walkable;
        private readonly Dictionary<GridCoord, UnitId> _occupants = 
            new Dictionary<GridCoord, UnitId>();

        public int Width { get; }
        public int Height { get; }

        public BoardModel(int width, int height)
        {
            if (width <= 0 || height <= 0) 
                throw new ArgumentOutOfRangeException(nameof(width) + nameof(height));
            
            Width = width;
            Height = height;

            _walkable = new bool[width, height];
            for (int i = 0; i < width; i++)
            {
                for (int j = 0; j < height; j++)
                {
                    _walkable[i, j] = true;
                }
            }    
        }

        public bool IsInside(GridCoord coord)
        {
            return coord.X >= 0 && coord.X < Width && coord.Z >= 0 && coord.Z < Height;
        }

        public bool IsWalkable(GridCoord coord)
        { 
            return IsInside(coord) && _walkable[coord.X, coord.Z];
        }

        public void SetWalkable(GridCoord coord, bool walkable)
        {
            EnsureInside(coord);
            if (!walkable && _occupants.ContainsKey(coord))
            {
                throw new InvalidOperationException($"Cannot block occupied cell {coord}.");
            }
            _walkable[coord.X, coord.Z] = walkable;
        }

        public bool IsOccupied(GridCoord coord)
        {
            return _occupants.ContainsKey(coord);
        }

        public bool TryGetUnitAt(GridCoord coord, out UnitId unitId)
        {
            return _occupants.TryGetValue(coord, out unitId);
        }

        public void AddUnit(UnitModel unit)
        {
            if (unit == null) throw new ArgumentNullException(nameof(unit));
            if (!unit.IsAlive) throw new InvalidOperationException("Cannot register a dead unit.");
            EnsureAvailable(unit.Position);
            _occupants.Add(unit.Position, unit.Id);
        }

        public void MoveUnit(UnitModel unit, GridCoord destination)
        {
            if (unit == null) throw new ArgumentNullException(nameof(unit));
            if (!unit.IsAlive) throw new InvalidOperationException("Cannot move a dead unit.");
            EnsureAvailable(destination);

            if (!_occupants.TryGetValue(unit.Position, out UnitId current) || current != unit.Id)
            {
                throw new InvalidOperationException($"Unit {unit.Id} is not registered at {unit.Position}.");
            }

            // 先更新占用表，再同步角色坐标
            _occupants.Remove(unit.Position);
            _occupants.Add(destination, unit.Id);
            unit.MoveTo(destination);
        }

        public void RemoveUnit(UnitModel unit)
        {
            if (unit == null) return;
            if (_occupants.TryGetValue(unit.Position, out UnitId current) && current == unit.Id)
            {
                _occupants.Remove(unit.Position);
            }
        }

        private void EnsureAvailable(GridCoord coord)
        {
            EnsureInside(coord);
            if (!IsWalkable(coord))
            {
                throw new InvalidOperationException($"Cell {coord} is blocked.");
            }

            if (IsOccupied(coord))
            {
                throw new InvalidOperationException($"Cell {coord} is occupied.");
            }
        }
        private void EnsureInside(GridCoord coord)
        {
            if (!IsInside(coord))
            {
                throw new ArgumentOutOfRangeException(nameof(coord), $"Coordinate {coord} is outside the board.");
            }
        }
    }
}
