using System;
using R3;
using System.Collections.Generic;

namespace WarChess.Domain
{
    // 单个角色的运行时状态，处理生命、行动点、位置和防御等业务规则
    public sealed class UnitModel : IDisposable
    {
        private readonly HashSet<UnitActionType> _actions;

        public UnitId Id { get; }
        public Team Team { get; }
        public GridCoord Position { get; private set; }

        public int MaxHealth { get; }
        private readonly ReactiveProperty<int> _health;
        public ReadOnlyReactiveProperty<int> HealthChanged => _health;
        public int Health => _health.Value;
        public int MaxActionPoints { get; }
        private readonly ReactiveProperty<int> _actionPoints;
        public ReadOnlyReactiveProperty<int> ActionPointsChanged => _actionPoints;
        public int ActionPoints => _actionPoints.Value;
        public int MoveRange { get; }
        public int AttackRange { get; }
        public int AttackDamage { get; }

        public bool IsDefending { get; private set; }
        public bool IsAlive => Health > 0;
        public float NormalizedHealth => MaxHealth == 0 ? 0f : (float)Health / MaxHealth;

        public UnitModel(UnitDefinition definition)
        {
            if (definition == null) 
                throw new ArgumentNullException(nameof(definition));

            Id = definition.Id;
            Team = definition.Team;
            Position = definition.InitialPosition;
            MaxHealth = definition.MaxHealth;
            _health = new ReactiveProperty<int>(MaxHealth);
            MaxActionPoints = definition.MaxActionPoints;
            _actionPoints = new ReactiveProperty<int>(MaxActionPoints);
            MoveRange = definition.MoveRange;
            AttackRange = definition.AttackRange;
            AttackDamage = definition.AttackDamage;
            _actions = new HashSet<UnitActionType>(definition.Actions);
        }

        public bool HasAction(UnitActionType actionType) => _actions.Contains(actionType);
        public bool CanSpendActionPoints(int amount)
        {
            return amount >= 0 && ActionPoints >= amount;
        }

        public void SpendActionPoints(int amount)
        {
            if (!CanSpendActionPoints(amount))
            {
                throw new InvalidOperationException($"Cannot spend {amount} action points. Current: {ActionPoints}");
            }
            _actionPoints.Value -= amount;
        }

        public void BeginTurn()
        {
            // 回合开始恢复行动点，并清除只持续一回合的防御状态
            if (!IsAlive) return;
            _actionPoints.Value = MaxActionPoints;
            IsDefending = false;
        }

        public void MoveTo(GridCoord newPosition)
        {
            if (!IsAlive) 
                throw new InvalidOperationException("Cannot move a dead unit.");
            Position = newPosition;
        }

        public int ApplyDamage(int amount)
        {
            if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount));
            if (!IsAlive) return 0;

            // 返回实际扣除的生命值
            int effectiveDamage = Math.Min(Health, amount);
            _health.Value -= effectiveDamage;
            return effectiveDamage;
        }

        /// <summary>由 BattleModel 持有；场景结束时释放状态源。</summary>
        public void Dispose()
        {
            _health.Dispose();
            _actionPoints.Dispose();
        }

        public void SetDefending()
        {
            if (!IsAlive) throw new InvalidOperationException("Cannot defend with a dead unit.");
            IsDefending = true;
        }

    }
}
