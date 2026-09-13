using UnityEngine;
using VContainer;
using WarChess.Application;

namespace WarChess.UnityViews
{
    /// <summary>现有暂停/帮助面板的薄适配器；按钮 Inspector 可直接绑定这些方法。</summary>
    public sealed class SceneControlsView : MonoBehaviour
    {
        [SerializeField] private GameObject pausePanel;
        [SerializeField] private GameObject helpPanel;
        [SerializeField] private GameObject aboutPanel;
        private ISceneService _scenes;

        [Inject]
        public void Construct(ISceneService scenes) => _scenes = scenes;

        public void Pause()
        {
            if (pausePanel != null) pausePanel.SetActive(true);
            Time.timeScale = 0f;
        }
        public void Resume()
        {
            if (pausePanel != null) pausePanel.SetActive(false);
            Time.timeScale = 1f;
        }
        public void Restart() => _scenes.LoadScene("Game");
        public void BackToMenu() => _scenes.LoadScene("Menu");
        public void Quit() => _scenes.Quit();
        public void ShowHelp() { if (helpPanel != null) helpPanel.SetActive(true); }
        public void HideHelp() { if (helpPanel != null) helpPanel.SetActive(false); }
        public void ShowAbout() { if (aboutPanel != null) aboutPanel.SetActive(true); }
        public void HideAbout() { if (aboutPanel != null) aboutPanel.SetActive(false); }
        private void OnDestroy() => Time.timeScale = 1f;
    }
}
