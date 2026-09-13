using System;
using R3;
using System.Collections.Generic;
using WarChess.Domain;

namespace WarChess.Presentation
{
    // 战斗输入端口
    public interface IBattleInputView
    {
        Observable<UnitId> UnitClicked { get; }
        Observable<GridCoord> CellClicked { get; }
        void SetInputEnabled(bool enabled);
        IDisposable Schedule(float delaySeconds, Action callback);
    }

    // 棋盘显示端口
    public interface IBoardView
    {
        void Initialize(int width, int height, float cellSize);
        void ClearHighlights();
        void ShowHighlights(IReadOnlyList<GridCoord> cells, BoardHighlight highlight);
    }

    // 单位显示端口
    public interface IUnitView
    {
        UnitDefinition Definition { get; }
        void Initialize(GridCoord position);
        void CancelAnimation();
        void SetSelected(bool selected);
        void SetHealth(int currentHealth, int maxHealth);
        void SetActionPoints(int currentActionPoints, int maxActionPoints);
        void PlayMove(IReadOnlyList<GridCoord> path, Action completed);
        void PlayAttack(GridCoord target, Action hitMoment, Action completed);
        void PlayDefend(Action completed);
        void PlayHit(int damage, bool died);
    }

    // 战斗结果显示端口
    public interface IBattleHudView
    {
        Observable<UnitActionType> ActionSelected { get; }
        Observable<Unit> EndTurnRequested { get; }

        void SetTurn(int turnNumber, Team activeTeam);
        void SetSelectedUnit(string unitId, int actionPoints, int maxActionPoints);
        void SetActionState(UnitActionType selectedAction, bool canMove, bool canAttack, bool canDefend);
        void SetBusy(bool busy);
        void SetStatus(string message);
        void ShowBattleResult(Team winningTeam);
    }
}
