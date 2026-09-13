namespace WarChess.Domain
{
    // 角色类型
    public enum Team
    {
        Player,
        Enemy
    }

    // 战斗流程
    public enum BattlePhase
    {
        PlayerInput,
        Animating,
        EnemyThinking,
        Completed
    }

    // 角色可进行的基础行动类型
    public enum UnitActionType
    {
        Move,
        Attack,
        Defend
    }

    // 表现层使用的棋盘高亮语义
    public enum BoardHighlight
    {
        Move,
        Attack,
        Defend
    }
}