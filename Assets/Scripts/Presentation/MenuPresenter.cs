using System;
using R3;
using VContainer.Unity;
using WarChess.Application;

namespace WarChess.Presentation
{
    public interface IMenuView
    {
        Observable<Unit> NewGameRequested { get; }
        Observable<Unit> QuitRequested { get; }
    }

    /// <summary>菜单订阅由 VContainer EntryPoint 启动，随菜单场景的 LifetimeScope 释放。</summary>
    public sealed class MenuPresenter : IStartable, IDisposable
    {
        private readonly IMenuView _view;
        private readonly ISceneService _sceneService;
        private readonly string _gameSceneName;
        private readonly CompositeDisposable _subscriptions = new CompositeDisposable();
        private bool _started;
        private bool _disposed;
        public MenuPresenter(IMenuView view, ISceneService sceneService, string gameSceneName)
        {
            _view = view;
            _sceneService = sceneService;
            _gameSceneName = gameSceneName;
        }
        public void Start()
        {
            if (_started || _disposed) return;
            _started = true;
            _view.NewGameRequested.Take(1).Subscribe(_ => _sceneService.LoadScene(_gameSceneName)).AddTo(_subscriptions);
            _view.QuitRequested.Subscribe(_ => _sceneService.Quit()).AddTo(_subscriptions);
        }
        public void Dispose()
        {
            _disposed = true;
            _subscriptions.Dispose();
        }
    }
}
