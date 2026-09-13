using WarChess.Domain;

namespace WarChess.Application
{
    /// <summary>胜负只依据存活单位；死亡单位仍保留身份，供受击动画查找。</summary>
    public sealed class VictoryService
    {
        public void Evaluate(BattleModel battle)
        {
            if (battle.Phase == BattlePhase.Completed) return;
            bool player = battle.GetLivingUnits(Team.Player).Count > 0;
            bool enemy = battle.GetLivingUnits(Team.Enemy).Count > 0;
            if (player && !enemy) battle.Complete(Team.Player);
            else if (enemy && !player) battle.Complete(Team.Enemy);
        }
    }

    public sealed class TurnService
    {
        public void EndTurn(BattleModel battle)
        {
            // 动画中、结算后不能切回合；只恢复即将行动的一方。
            if (battle.Phase != BattlePhase.PlayerInput && battle.Phase != BattlePhase.EnemyThinking) return;
            battle.Turn.Advance();
            foreach (UnitModel unit in battle.GetLivingUnits(battle.Turn.ActiveTeam)) unit.BeginTurn();
            battle.SetPhase(battle.Turn.ActiveTeam == Team.Player ? BattlePhase.PlayerInput : BattlePhase.EnemyThinking);
        }
    }
}
