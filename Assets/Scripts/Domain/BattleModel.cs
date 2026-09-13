using System;
using System.Collections.Generic;

namespace WarChess.Domain
{
    // 战斗模型，统一维护棋盘、角色、技能等信息
    public sealed class BattleModel : IDisposable
    {
        private readonly Dictionary<UnitId, UnitModel> _units = new Dictionary<UnitId, UnitModel>();
        
        public BoardModel Board { get; }
        public TurnModel Turn { get; }
        public BattlePhase Phase { get; private set; }
        public Team? Winner { get; private set; }
        public IEnumerable<UnitModel> Units => _units.Values;

        public BattleModel(BoardModel board)
        {
            Board = board ?? throw new ArgumentNullException(nameof(board));
            Turn = new TurnModel();
            Phase = BattlePhase.PlayerInput;
        }

        public void AddUnit(UnitModel unit)
        {
            if (unit == null) throw new ArgumentNullException(nameof(unit));
            if (_units.ContainsKey(unit.Id))
                throw new InvalidOperationException($"Unit with ID {unit.Id} already exists in the battle.");

            // 先登记棋盘占用，成功后再加入单位列表
            Board.AddUnit(unit);
            _units.Add(unit.Id, unit);
        }

        public UnitModel GetUnit(UnitId unitId)
        {
            if (!_units.TryGetValue(unitId, out var unit))
                throw new KeyNotFoundException($"Unit with ID {unitId} not found in the battle.");
            return unit;
        }

        public bool TryGetUnit(UnitId id, out UnitModel unit)
        {
            return _units.TryGetValue(id, out unit);
        }

        public List<UnitModel> GetLivingUnits(Team team)
        {
            var result = new List<UnitModel>();
            foreach (UnitModel unit in _units.Values)
            {
                if (unit.Team == team && unit.IsAlive)
                {
                    result.Add(unit);
                }
            }
            return result;
        }

        public void RemoveDeadUnitFromBoard(UnitModel unit)
        {
            // 死亡角色仍保留在模型字典中
            if (unit != null && !unit.IsAlive)
            {
                Board.RemoveUnit(unit);
            }
        }

        public void SetPhase(BattlePhase phase)
        {
            if (Phase == BattlePhase.Completed && phase != BattlePhase.Completed)
            {
                return;
            }
            Phase = phase;
        }

        public void Dispose()
        {
            foreach (UnitModel unit in _units.Values) unit.Dispose();
        }

        public void Complete(Team winner)
        {
            if (Phase == BattlePhase.Completed) return;
            Winner = winner;
            Phase = BattlePhase.Completed;
        }
    }
}
