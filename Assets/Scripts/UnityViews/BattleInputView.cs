using System;
using R3;
using VContainer;
using UnityEngine;
using UnityEngine.EventSystems;
using WarChess.Domain;
using WarChess.Presentation;

namespace WarChess.UnityViews
{
    /// <summary>
    /// 旧版 Input Manager 的 Unity 适配器。只产生点击意图，不判断行动是否合法。
    /// </summary>
    public sealed class BattleInputView : MonoBehaviour, IBattleInputView
    {
        [SerializeField] private Camera inputCamera;
        [SerializeField] private BoardView boardView;
        [SerializeField] private LayerMask unitLayerMask;
        [SerializeField] private LayerMask boardLayerMask;

        private bool _inputEnabled;

        private readonly Subject<UnitId> _unitClicked = new Subject<UnitId>();
        public Observable<UnitId> UnitClicked => _unitClicked;
        private readonly Subject<GridCoord> _cellClicked = new Subject<GridCoord>();
        public Observable<GridCoord> CellClicked => _cellClicked;

        [Inject]
        public void Construct(BoardView board) => boardView = board;

        private void OnDestroy()
        {
            _unitClicked.Dispose();
            _cellClicked.Dispose();
        }

        private void Awake()
        {
            if (inputCamera == null)
            {
                inputCamera = Camera.main;
            }
        }

        private void Update()
        {
            // 动画、敌方回合或鼠标位于 UI 上时不向 Presenter 发送输入。
            if (!_inputEnabled || Time.timeScale <= 0f || !Input.GetMouseButtonDown(0)) return;
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;
            if (inputCamera == null || boardView == null) return;

            Ray ray = inputCamera.ScreenPointToRay(Input.mousePosition);
            // 优先检测角色；若未命中角色，再把地面命中点转换成棋盘坐标。
            if (Physics.Raycast(ray, out RaycastHit unitHit, float.MaxValue, unitLayerMask))
            {
                UnitView unitView = unitHit.collider.GetComponentInParent<UnitView>();
                if (unitView != null)
                {
                    _unitClicked.OnNext(unitView.UnitId);
                    return;
                }
            }

            if (Physics.Raycast(ray, out RaycastHit boardHit, float.MaxValue, boardLayerMask))
            {
                _cellClicked.OnNext(boardView.WorldToGrid(boardHit.point));
            }
        }

        public void SetInputEnabled(bool enabled)
        {
            _inputEnabled = enabled;
        }

        public IDisposable Schedule(float delaySeconds, Action callback)
        {
            if (callback == null) throw new ArgumentNullException(nameof(callback));
            // 明确使用 Unity 主线程、受 Time.timeScale 影响的时钟；Presenter 保存并取消订阅。
            return Observable.Timer(TimeSpan.FromSeconds(Mathf.Max(0f, delaySeconds)), UnityTimeProvider.Update)
                .Subscribe(_ => callback());
        }
    }
}
