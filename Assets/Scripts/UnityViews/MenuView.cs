using System;
using R3;
using UnityEngine;
using UnityEngine.UI;
using WarChess.Presentation;

namespace WarChess.UnityViews
{
    /// <summary>菜单按钮的 Unity View，仅向 MenuPresenter 上报用户意图。</summary>
    public sealed class MenuView : MonoBehaviour, IMenuView
    {
        [SerializeField] private Button newGameButton;
        [SerializeField] private Button quitButton;

        private readonly Subject<Unit> _NewGameRequested = new Subject<Unit>();
        public Observable<Unit> NewGameRequested => _NewGameRequested;
        private readonly Subject<Unit> _QuitRequested = new Subject<Unit>();
        public Observable<Unit> QuitRequested => _QuitRequested;

        private void Awake()
        {
            if (newGameButton != null) newGameButton.onClick.AddListener(RequestNewGame);
            if (quitButton != null) quitButton.onClick.AddListener(RequestQuit);
        }

        private void OnDestroy()
        {
            _NewGameRequested.Dispose();
            _QuitRequested.Dispose();
            // 防止重复加载菜单场景后按钮累积旧监听器。
            if (newGameButton != null) newGameButton.onClick.RemoveListener(RequestNewGame);
            if (quitButton != null) quitButton.onClick.RemoveListener(RequestQuit);
        }

        private void RequestNewGame() => _NewGameRequested.OnNext(Unit.Default);
        private void RequestQuit() => _QuitRequested.OnNext(Unit.Default);
    }
}
