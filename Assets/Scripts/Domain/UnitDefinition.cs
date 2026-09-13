using System;
using System.Collections.Generic;

namespace WarChess.Domain
{
    // 创建角色运行时模型所需的只读定义，该对象只保存配置
    public sealed class UnitDefinition
    {
        private readonly UnitActionType[] _actions;
        public UnitId Id { get; }
        public Team Team { get; }
        public GridCoord InitialPosition { get; }
        public int MaxHealth { get; }
        public int MaxActionPoints { get; }
        public int MoveRange { get; }
        public int AttackRange { get; }
        public int AttackDamage { get; }
        public IReadOnlyCollection<UnitActionType> Actions => _actions;

        public UnitDefinition(UnitId id, Team team, GridCoord initialPosition, 
            int maxHealth, int maxActionPoints, int moveRange, int attackRange, 
            int attackDamage, IEnumerable<UnitActionType> actions)
        {
            // 拒绝默认标识与非法阵营，避免字典登记后才暴露配置问题。
            if (string.IsNullOrWhiteSpace(id.Value)) throw new ArgumentException("Unit ID is required.", nameof(id));
            if (!Enum.IsDefined(typeof(Team), team)) throw new ArgumentOutOfRangeException(nameof(team));
            // 非法检测
            if (maxHealth <= 0) throw new ArgumentOutOfRangeException(nameof(maxHealth));
            if (maxActionPoints <= 0) throw new ArgumentOutOfRangeException(nameof(maxActionPoints));
            if (moveRange < 0) throw new ArgumentOutOfRangeException(nameof(moveRange));
            if (attackRange < 0) throw new ArgumentOutOfRangeException(nameof(attackRange));
            if (attackDamage < 0) throw new ArgumentOutOfRangeException(nameof(attackDamage));


            Id = id;
            Team = team;
            InitialPosition = initialPosition;
            MaxHealth = maxHealth;
            MaxActionPoints = maxActionPoints;
            MoveRange = moveRange;
            AttackRange = attackRange;
            AttackDamage = attackDamage;
            _actions = actions == null
                ? Array.Empty<UnitActionType>()
                : new List<UnitActionType>(actions).ToArray();
        }
    }
}
