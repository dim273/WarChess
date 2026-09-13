namespace WarChess.Domain
{
    // 保存当前回合编号和行动阵营
    public sealed class TurnModel
    {
        public int Number { get; private set; } = 1;
        public Team ActiveTeam { get; private set; } = Team.Player;

        public void Advance()
        {
            // 完成敌方回合回到玩家的时候，才会进入新的回合
            ActiveTeam = ActiveTeam == Team.Player ? Team.Enemy : Team.Player;
            if (ActiveTeam == Team.Player)
            {
                Number++;
            }
        }
    }

}