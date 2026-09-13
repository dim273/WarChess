using System;
using UnityEngine;
using VContainer;
using VContainer.Unity;
using WarChess.Application;
using WarChess.Presentation;
using WarChess.UnityViews;

namespace WarChess.Composition
{
    /// <summary>菜单独立作用域；不要把场景级 Presenter 注册到 DontDestroyOnLoad 根容器。</summary>
    public sealed class MenuInstaller : LifetimeScope
    {
        [SerializeField] private MenuView menuView;
        [SerializeField] private string gameSceneName = "Game";

        [SerializeField] private SceneControlsView sceneControls;

        protected override void Configure(IContainerBuilder builder)
        {
            if (menuView == null || string.IsNullOrWhiteSpace(gameSceneName))
                throw new InvalidOperationException("MenuInstaller requires MenuView and a scene name.");
            if (sceneControls != null) builder.RegisterComponent(sceneControls);
            builder.RegisterComponent(menuView).As<IMenuView>();
            builder.Register<ISceneService, UnitySceneService>(Lifetime.Scoped);
            builder.RegisterEntryPoint<MenuPresenter>().WithParameter("gameSceneName", gameSceneName);
        }
    }
}
