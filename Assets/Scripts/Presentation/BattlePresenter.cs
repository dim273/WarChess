using System;
using R3;
using VContainer.Unity;
using System.Collections.Generic;
using WarChess.Application;
using WarChess.Domain;
using WarChess.Presentation;

namespace WarChess.Presentation
{
    /// <summary>
    /// 战斗场景的主 Presenter。接收 View 意图、调用 Service，并根据 ActionResult 编排动画与刷新。
    /// </summary>
    public sealed class BattlePresenter : IStartable, IDisposable
    {
        private readonly BattleModel _battle;
        private readonly IActionService _actionService;
        private readonly TurnService _turnService;
        private readonly AiDecisionService _aiDecisionService;
        private readonly IBattleInputView _inputView;
        private readonly IBattleHudView _hudView;
        private readonly BoardPresenter _boardPresenter;
        private readonly Dictionary<UnitId, UnitPresenter> _units;
        private readonly float _aiThinkDelay;

        private readonly CompositeDisposable _subscriptions = new CompositeDisposable();
        private readonly SerialDisposable _pendingAi = new SerialDisposable();
        private UnitId? _selectedUnitId;
        private UnitActionType _selectedAction = UnitActionType.Move;
        private bool _started;
        private bool _disposed;

        public BattlePresenter(
            BattleModel battle,
            IActionService actionService,
            TurnService turnService,
            AiDecisionService aiDecisionService,
            IBattleInputView inputView,
            IBattleHudView hudView,
            BoardPresenter boardPresenter,
            IEnumerable<UnitPresenter> units,
            float aiThinkDelay)
        {
            _battle = battle ?? throw new ArgumentNullException(nameof(battle));
            _actionService = actionService ?? throw new ArgumentNullException(nameof(actionService));
            _turnService = turnService ?? throw new ArgumentNullException(nameof(turnService));
            _aiDecisionService = aiDecisionService ?? throw new ArgumentNullException(nameof(aiDecisionService));
            _inputView = inputView ?? throw new ArgumentNullException(nameof(inputView));
            _hudView = hudView ?? throw new ArgumentNullException(nameof(hudView));
            _boardPresenter = boardPresenter ?? throw new ArgumentNullException(nameof(boardPresenter));
            _aiThinkDelay = Math.Max(0f, aiThinkDelay);
            _units = new Dictionary<UnitId, UnitPresenter>();

            if (units == null) throw new ArgumentNullException(nameof(units));
            foreach (UnitPresenter unit in units)
            {
                _units.Add(unit.Id, unit);
            }
        }

        public void Start()
        {
            if (_started || _disposed) return;
            _started = true;

            // Presenter 是唯一的输入订阅者，View 与 Service 之间不直接通信。
            _inputView.UnitClicked.Subscribe(HandleUnitClicked).AddTo(_subscriptions);
            _inputView.CellClicked.Subscribe(HandleCellClicked).AddTo(_subscriptions);
            _hudView.ActionSelected.Subscribe(HandleActionSelected).AddTo(_subscriptions);
            _hudView.EndTurnRequested.Subscribe(_ => HandleEndTurnRequested()).AddTo(_subscriptions);

            foreach (UnitPresenter unit in _units.Values)
            {
                unit.Initialize();
            }

            SelectFirstLivingPlayer();
            RefreshPresentation();
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            // 先拒绝回调，再取消 AI 定时器/输入订阅/动画，最后由场景作用域释放 Model。
            _pendingAi.Dispose();
            _subscriptions.Dispose();
            foreach (UnitPresenter unit in _units.Values) unit.Dispose();
        }

        private void HandleUnitClicked(UnitId unitId)
        {
            if (!CanAcceptPlayerInput()) return;
            if (!_battle.TryGetUnit(unitId, out UnitModel clicked) || !clicked.IsAlive) return;

            if (clicked.Team == Team.Player)
            {
                // 点击己方角色表示切换选择；点击敌方角色表示对其尝试当前行动。
                if (_selectedUnitId == unitId && _selectedAction == UnitActionType.Defend)
                    TryExecuteSelectedAction(clicked.Position);
                else
                    SelectUnit(unitId);
                return;
            }

            TryExecuteSelectedAction(clicked.Position);
        }

        private void HandleCellClicked(GridCoord coord)
        {
            if (!CanAcceptPlayerInput()) return;
            TryExecuteSelectedAction(coord);
        }

        private void HandleActionSelected(UnitActionType actionType)
        {
            if (!CanAcceptPlayerInput() || !_selectedUnitId.HasValue) return;

            UnitModel selected = _battle.GetUnit(_selectedUnitId.Value);
            if (!selected.HasAction(actionType))
            {
                _hudView.SetStatus("This unit does not have that action.");
                return;
            }

            _selectedAction = actionType;
            RefreshPresentation();
        }

        private void HandleEndTurnRequested()
        {
            if (!CanAcceptPlayerInput()) return;

            _boardPresenter.Clear();
            _turnService.EndTurn(_battle);
            RefreshPresentation();
            if (_battle.Phase == BattlePhase.EnemyThinking)
            {
                // 延迟开始 AI，使回合提示先得到一次可见刷新。
                _pendingAi.Disposable = _inputView.Schedule(_aiThinkDelay, RunNextAiAction);
            }
        }

        private void TryExecuteSelectedAction(GridCoord target)
        {
            if (!_selectedUnitId.HasValue) return;

            // Service 负责最终校验；Presenter 只把当前选择翻译为行动请求。
            var request = new ActionRequest(_selectedUnitId.Value, _selectedAction, target);
            ActionResult result = _actionService.TryExecute(_battle, request);
            if (!result.Succeeded)
            {
                _hudView.SetStatus(result.FailureReason);
                return;
            }

            BeginActionAnimation(result, false);
        }

        private void RunNextAiAction()
        {
            if (_disposed || _battle.Phase != BattlePhase.EnemyThinking) return;

            if (!_aiDecisionService.TryDecide(_battle, out AiDecision decision))
            {
                // 没有合法行动代表敌方回合结束，立即把控制权交还玩家。
                _turnService.EndTurn(_battle);
                SelectFirstLivingPlayer();
                RefreshPresentation();
                return;
            }

            ActionResult result = _actionService.TryExecute(_battle, decision.Request);
            if (!result.Succeeded)
            {
                _turnService.EndTurn(_battle);
                SelectFirstLivingPlayer();
                RefreshPresentation();
                return;
            }

            BeginActionAnimation(result, true);
        }

        private void BeginActionAnimation(ActionResult result, bool isAiAction)
        {
            // Model 已经由 Service 更新；动画期间锁定输入，View 只负责表现该确定结果。
            if (_battle.Phase != BattlePhase.Completed)
            {
                _battle.SetPhase(BattlePhase.Animating);
            }

            _inputView.SetInputEnabled(false);
            _hudView.SetBusy(true);
            _hudView.SetStatus(string.Empty);
            _boardPresenter.Clear();

            UnitPresenter actor = _units[result.Request.ActorId];
            Action completed = () => CompleteAction(result, isAiAction);

            switch (result.Request.ActionType)
            {
                case UnitActionType.Move:
                    actor.View.PlayMove(result.Path, completed);
                    break;

                case UnitActionType.Attack:
                    // 血条由 R3 在结算时更新；hitMoment 仅控制命中/死亡表现。
                    actor.View.PlayAttack(
                        result.Request.Target,
                        () => PlayAttackImpact(result),
                        completed);
                    break;

                case UnitActionType.Defend:
                    actor.View.PlayDefend(completed);
                    break;

                default:
                    completed();
                    break;
            }
        }

        private void PlayAttackImpact(ActionResult result)
        {
            if (_disposed || !result.TargetUnitId.HasValue) return;
            if (_units.TryGetValue(result.TargetUnitId.Value, out UnitPresenter target))
            {
                target.View.PlayHit(result.Damage, result.TargetDied);

            }
        }

        private void CompleteAction(ActionResult result, bool isAiAction)
        {
            if (_disposed) return;
            // 数值由 R3 自动同步；此处只解锁交互并推进下一步。
            _hudView.SetBusy(false);

            if (_battle.Phase == BattlePhase.Completed)
            {
                _inputView.SetInputEnabled(false);
                _boardPresenter.Clear();
                if (_battle.Winner.HasValue)
                {
                    _hudView.ShowBattleResult(_battle.Winner.Value);
                }

                return;
            }

            _battle.SetPhase(
                _battle.Turn.ActiveTeam == Team.Player
                    ? BattlePhase.PlayerInput
                    : BattlePhase.EnemyThinking);

            if (_selectedUnitId.HasValue &&
                _battle.TryGetUnit(_selectedUnitId.Value, out UnitModel selected) &&
                !selected.IsAlive)
            {
                SelectFirstLivingPlayer();
            }

            RefreshPresentation();
            if (isAiAction && _battle.Phase == BattlePhase.EnemyThinking)
            {
                // AI 每次只执行一个行动，完成后重新决策，可自然响应刚发生的死亡和位移。
                _pendingAi.Disposable = _inputView.Schedule(_aiThinkDelay, RunNextAiAction);
            }
        }

        private void SelectUnit(UnitId unitId)
        {
            // 先清除旧选择，再设置新选择，确保同一时刻只有一个选中标记。
            if (_selectedUnitId.HasValue && _units.TryGetValue(_selectedUnitId.Value, out UnitPresenter previous))
            {
                previous.SetSelected(false);
            }

            _selectedUnitId = unitId;
            _selectedAction = UnitActionType.Move;
            _units[unitId].SetSelected(true);
            RefreshPresentation();
        }

        private void SelectFirstLivingPlayer()
        {
            if (_selectedUnitId.HasValue && _units.TryGetValue(_selectedUnitId.Value, out UnitPresenter previous))
            {
                previous.SetSelected(false);
            }

            _selectedUnitId = null;
            List<UnitModel> players = _battle.GetLivingUnits(Team.Player);
            if (players.Count == 0) return;

            _selectedUnitId = players[0].Id;
            _selectedAction = UnitActionType.Move;
            _units[players[0].Id].SetSelected(true);
        }

        private void RefreshPresentation()
        {
            // HUD、输入和棋盘高亮全部由当前 BattleModel 与选择状态重新推导。
            bool playerInput = CanAcceptPlayerInput();
            _inputView.SetInputEnabled(playerInput);
            _hudView.SetTurn(_battle.Turn.Number, _battle.Turn.ActiveTeam);

            if (!_selectedUnitId.HasValue)
            {
                _boardPresenter.Clear();
                _hudView.SetSelectedUnit(string.Empty, 0, 0);
                _hudView.SetActionState(_selectedAction, false, false, false);
                return;
            }

            UnitModel selected = _battle.GetUnit(_selectedUnitId.Value);
            _hudView.SetSelectedUnit(selected.Id.Value, selected.ActionPoints, selected.MaxActionPoints);
            _hudView.SetActionState(
                _selectedAction,
                playerInput && CanUse(selected, UnitActionType.Move),
                playerInput && CanUse(selected, UnitActionType.Attack),
                playerInput && CanUse(selected, UnitActionType.Defend));

            if (playerInput)
            {
                ActionPreview preview = _actionService.GetPreview(_battle, selected.Id, _selectedAction);
                _boardPresenter.Show(preview, _selectedAction);
                _hudView.SetStatus(preview.IsAllowed ? string.Empty : preview.FailureReason);
            }
            else
            {
                _boardPresenter.Clear();
            }
        }

        private bool CanUse(UnitModel unit, UnitActionType actionType)
        {
            return unit.HasAction(actionType) &&
                   !(actionType == UnitActionType.Defend && unit.IsDefending) &&
                   unit.CanSpendActionPoints(_actionService.GetCost(actionType));
        }

        private bool CanAcceptPlayerInput()
        {
            return !_disposed &&
                   _battle.Phase == BattlePhase.PlayerInput &&
                   _battle.Turn.ActiveTeam == Team.Player;
        }
    }
}
