using UnityEngine;
using UnityEngine.SceneManagement;
using WarChess.Application;

namespace WarChess.UnityViews
{
    /// <summary>ISceneService 的 Unity 平台实现，隔离 SceneManager 与 Application API。</summary>
    public sealed class UnitySceneService : ISceneService
    {
        public void LoadScene(string sceneName)
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(sceneName);
        }

        public void Quit()
        {
            UnityEngine.Application.Quit();
        }
    }
}
