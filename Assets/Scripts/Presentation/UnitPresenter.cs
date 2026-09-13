using System;
using R3;
using WarChess.Domain;

namespace WarChess.Presentation
{
    /// <summary>一对一显示绑定；只订阅数值，不把棋盘坐标直接绑定到 Transform（移动需要动画）。</summary>
    public sealed class UnitPresenter : IDisposable
    {
        private readonly UnitModel _model;
        private readonly IUnitView _view;
        private readonly CompositeDisposable _subscriptions = new CompositeDisposable();
        private bool _started;
        public UnitId Id => _model.Id;
        public UnitModel Model => _model;
        public IUnitView View => _view;
        public UnitPresenter(UnitModel model, IUnitView view)
        {
            _model = model ?? throw new ArgumentNullException(nameof(model));
            _view = view ?? throw new ArgumentNullException(nameof(view));
        }

        public void Initialize()
        {
            if (_started) return;
            _started = true;
            _view.Initialize(_model.Position);
            // ReactiveProperty 订阅时立即发送初值，之后只刷新发生变化的字段。
            _model.HealthChanged.Subscribe(value => _view.SetHealth(value, _model.MaxHealth)).AddTo(_subscriptions);
            _model.ActionPointsChanged.Subscribe(value => _view.SetActionPoints(value, _model.MaxActionPoints)).AddTo(_subscriptions);
        }

        public void SetSelected(bool selected) => _view.SetSelected(selected);
        public void Dispose()
        {
            _subscriptions.Dispose();
            _view.CancelAnimation();
        }
    }
}
