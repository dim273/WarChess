using System;
using WarChess.Domain;

namespace WarChess.Application
{
    /// <summary>确定性 AI：优先击杀/攻击，其次靠近玩家，最后防御；只提交合法且有意义的行动。</summary>
    public sealed class AiDecisionService
    {
        private readonly IActionService _actions;
        public AiDecisionService(IActionService actions) { _actions = actions; }

        public bool TryDecide(BattleModel battle, out AiDecision decision)
        {
            decision = default;
            if (battle.Phase != BattlePhase.EnemyThinking || battle.Turn.ActiveTeam != Team.Enemy) return false;
            int best = int.MinValue;
            var enemies = battle.GetLivingUnits(Team.Enemy);
            enemies.Sort((a, b) => string.CompareOrdinal(a.Id.Value, b.Id.Value));
            foreach (UnitModel actor in enemies)
            {
                foreach (UnitActionType action in new[] { UnitActionType.Attack, UnitActionType.Move, UnitActionType.Defend })
                {
                    ActionPreview preview = _actions.GetPreview(battle, actor.Id, action);
                    if (!preview.IsAllowed) continue;
                    foreach (GridCoord target in preview.Target)
                    {
                        int score;
                        if (action == UnitActionType.Attack)
                        {
                            battle.Board.TryGetUnitAt(target, out UnitId id);
                            UnitModel victim = battle.GetUnit(id);
                            int damage = victim.IsDefending ? actor.AttackDamage / 2 + actor.AttackDamage % 2 : actor.AttackDamage;
                            if (damage <= 0) continue;
                            score = 1000 + Math.Min(damage, victim.Health) + (damage >= victim.Health ? 1000 : 0);
                        }
                        else if (action == UnitActionType.Move)
                        {
                            int before = DistanceToPlayer(battle, actor.Position);
                            int after = DistanceToPlayer(battle, target);
                            if (after >= before) continue; // 不来回走动浪费 AP；无进展时防御或结束。
                            score = 100 + before - after;
                        }
                        else score = 1;
                        if (score <= best) continue;
                        best = score;
                        decision = new AiDecision(new ActionRequest(actor.Id, action, target), score);
                    }
                }
            }
            return best != int.MinValue;
        }

        private static int DistanceToPlayer(BattleModel battle, GridCoord position)
        {
            int best = int.MaxValue;
            foreach (UnitModel player in battle.GetLivingUnits(Team.Player))
                best = Math.Min(best, position.ManhattanDistance(player.Position));
            return best;
        }
    }
}
