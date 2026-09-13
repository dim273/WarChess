using System;
using R3;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using WarChess.Domain;
using WarChess.Presentation;

namespace WarChess.UnityViews
{
    /// <summary>战斗 HUD 的 Unity View，负责按钮事件和文本/遮罩显示。</summary>
    public sealed class BattleHudView : MonoBehaviour, IBattleHudView
    {
        [Header("Actions")]
        [SerializeField] private Button moveButton;
        [SerializeField] private Button attackButton;
        [SerializeField] private Button defendButton;
        [SerializeField] private Button endTurnButton;

        [Header("Text")]
        [SerializeField] private TextMeshProUGUI turnText;
        [SerializeField] private TextMeshProUGUI selectedUnitText;
        [SerializeField] private TextMeshProUGUI actionPointsText;
        [SerializeField] private TextMeshProUGUI statusText;
        [SerializeField] private TextMeshProUGUI resultText;

        [Header("State")]
        [SerializeField] private GameObject busyOverlay;
        [SerializeField] private GameObject resultPanel;
        [SerializeField] private Color selectedActionColor = new Color(0.35f, 0.85f, 0.35f);

        private Color _moveBaseColor = Color.white;
        private Color _attackBaseColor = Color.white;
        private Color _defendBaseColor = Color.white;
        private bool _playerTurn;
        private bool _busy;
        private bool _completed;

        private readonly Subject<UnitActionType> _ActionSelected = new Subject<UnitActionType>();
        public Observable<UnitActionType> ActionSelected => _ActionSelected;
        private readonly Subject<Unit> _EndTurnRequested = new Subject<Unit>();
        public Observable<Unit> EndTurnRequested => _EndTurnRequested;

        private void Awake()
        {
            // 监听器只注册一次，并在 OnDestroy 中对称移除。
            CacheButtonColors();
            AddListeners();
            if (resultPanel != null) resultPanel.SetActive(false);
            if (busyOverlay != null) busyOverlay.SetActive(false);
        }

        private void OnDestroy()
        {
            _ActionSelected.Dispose();
            _EndTurnRequested.Dispose();
            RemoveListeners();
        }

        public void SetTurn(int turnNumber, Team activeTeam)
        {
            _playerTurn = activeTeam == Team.Player;
            if (turnText != null)
            {
                turnText.text = $"Turn {turnNumber} - {activeTeam}";
            }

            if (endTurnButton != null)
            {
                endTurnButton.interactable = _playerTurn && !_busy && !_completed;
            }
        }

        public void SetSelectedUnit(string unitId, int actionPoints, int maximumActionPoints)
        {
            if (selectedUnitText != null) selectedUnitText.text = unitId;
            if (actionPointsText != null)
            {
                actionPointsText.text = $"AP {actionPoints}/{maximumActionPoints}";
            }
        }

        public void SetActionState(
            UnitActionType selectedAction,
            bool canMove,
            bool canAttack,
            bool canDefend)
        {
            // 按钮可用性由 Presenter 计算；View 只负责应用结果和选中颜色。
            if (moveButton != null) moveButton.interactable = canMove && !_busy;
            if (attackButton != null) attackButton.interactable = canAttack && !_busy;
            if (defendButton != null) defendButton.interactable = canDefend && !_busy;

            SetButtonColor(moveButton, selectedAction == UnitActionType.Move ? selectedActionColor : _moveBaseColor);
            SetButtonColor(attackButton, selectedAction == UnitActionType.Attack ? selectedActionColor : _attackBaseColor);
            SetButtonColor(defendButton, selectedAction == UnitActionType.Defend ? selectedActionColor : _defendBaseColor);
        }

        public void SetBusy(bool busy)
        {
            // Busy 期间同时显示遮罩并关闭所有可能改变战斗状态的按钮。
            _busy = busy;
            if (busyOverlay != null) busyOverlay.SetActive(busy);
            if (moveButton != null && busy) moveButton.interactable = false;
            if (attackButton != null && busy) attackButton.interactable = false;
            if (defendButton != null && busy) defendButton.interactable = false;
            if (endTurnButton != null) endTurnButton.interactable = _playerTurn && !busy && !_completed;
        }

        public void SetStatus(string message)
        {
            if (statusText != null) statusText.text = message ?? string.Empty;
        }

        public void ShowBattleResult(Team winner)
        {
            _completed = true;
            SetActionState(UnitActionType.Move, false, false, false);
            if (endTurnButton != null) endTurnButton.interactable = false;
            if (resultPanel != null) resultPanel.SetActive(true);
            if (resultText != null) resultText.text = $"{winner} wins";
        }

        private void CacheButtonColors()
        {
            if (moveButton != null && moveButton.image != null) _moveBaseColor = moveButton.image.color;
            if (attackButton != null && attackButton.image != null) _attackBaseColor = attackButton.image.color;
            if (defendButton != null && defendButton.image != null) _defendBaseColor = defendButton.image.color;
        }

        private void AddListeners()
        {
            if (moveButton != null) moveButton.onClick.AddListener(SelectMove);
            if (attackButton != null) attackButton.onClick.AddListener(SelectAttack);
            if (defendButton != null) defendButton.onClick.AddListener(SelectDefend);
            if (endTurnButton != null) endTurnButton.onClick.AddListener(RequestEndTurn);
        }

        private void RemoveListeners()
        {
            if (moveButton != null) moveButton.onClick.RemoveListener(SelectMove);
            if (attackButton != null) attackButton.onClick.RemoveListener(SelectAttack);
            if (defendButton != null) defendButton.onClick.RemoveListener(SelectDefend);
            if (endTurnButton != null) endTurnButton.onClick.RemoveListener(RequestEndTurn);
        }

        private void SelectMove() => SelectAction(UnitActionType.Move);
        private void SelectAttack() => SelectAction(UnitActionType.Attack);
        private void SelectDefend() => SelectAction(UnitActionType.Defend);
        private void SelectAction(UnitActionType action)
        {
            if (Time.timeScale > 0f && !_busy && !_completed) _ActionSelected.OnNext(action);
        }
        private void RequestEndTurn()
        {
            if (Time.timeScale > 0f && !_busy && !_completed) _EndTurnRequested.OnNext(Unit.Default);
        }

        private static void SetButtonColor(Button button, Color color)
        {
            if (button != null && button.image != null)
            {
                button.image.color = color;
            }
        }
    }
}
