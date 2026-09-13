using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class UIManager : Singleton<UIManager>
{
    [Header("Config")]
    [SerializeField] private GameObject helpPanel;
    [SerializeField] private GameObject aboutPanel;
    [SerializeField] private GameObject stopPanel;

    public bool gameStop {  get; private set; }

    public void NewGameButton()
    {
        SceneManager.LoadScene("Game");
    }

    public void MenuButton()
    {
        SceneManager.LoadScene("Menu");
    }

    public void ExitButton()
    {
        Application.Quit();
    }

    public void OpenHelpPanel() => helpPanel.SetActive(true);
    public void CloseHelpPanel() => helpPanel.SetActive(false);
    public void OpneAboutPanel() => aboutPanel.SetActive(true);
    public void CloseAboutPanel() => aboutPanel.SetActive(false);
    public void ContinueButton()
    {
        stopPanel.SetActive(false);
        gameStop = false;
    }
    public void StopButton()
    {
        stopPanel.SetActive(true);
        gameStop = true;
    }
}
