using UnityEngine;
using WarChess.Domain;

namespace WarChess.Configuration
{
    /// <summary>
    /// 角色的设计期配置资产。运行时通过工厂方法转换为不依赖 Unity 的 UnitDefinition。
    /// </summary>
    [CreateAssetMenu(
        fileName = "UnitDefinition",
        menuName = "WarChess/New Architecture/Unit Definition")]
    public sealed class UnitDefinitionAsset : ScriptableObject
    {
        [SerializeField, Min(1)] private int maxHealth = 100;
        [SerializeField, Min(1)] private int maxActionPoints = 2;
        [SerializeField, Min(0)] private int moveRange = 4;
        [SerializeField, Min(0)] private int attackRange = 3;
        [SerializeField, Min(0)] private int attackDamage = 40;
        [SerializeField]
        private UnitActionType[] actions =
        {
            UnitActionType.Move,
            UnitActionType.Attack,
            UnitActionType.Defend
        };

        public UnitDefinition CreateRuntimeDefinition(
            UnitId id,
            Team team,
            GridCoord initialPosition)
        {
            // ScriptableObject 只提供只读配置，不承担任何战斗运行时状态。
            return new UnitDefinition(
                id,
                team,
                initialPosition,
                maxHealth,
                maxActionPoints,
                moveRange,
                attackRange,
                attackDamage,
                actions);
        }
    }
}
