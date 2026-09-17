using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using VContainer;
using WarChess.Composition;
using WarChess.Domain;
using WarChess.UnityViews;

namespace WarChess.EditorTools
{
    /// <summary>批处理 Play Mode 冒烟测试，不保存运行期间的场景变化。</summary>
    [InitializeOnLoad]
    public static class SceneIntegrationSmoke
    {
        private const string Key = "WarChess.SceneSmoke";
        private static double deadline;
        private static int stage;
        private static bool failed;
        static SceneIntegrationSmoke()
        {
            if (SessionState.GetBool(Key, false)) Attach();
        }
        public static void Run()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/Menu.unity");
            SessionState.SetBool(Key, true);
            Attach();
            EditorApplication.isPlaying = true;
        }
        private static void Attach()
        {
            deadline = EditorApplication.timeSinceStartup + 90;
            EditorApplication.update -= Tick; EditorApplication.update += Tick;
            UnityEngine.Application.logMessageReceived -= Log; UnityEngine.Application.logMessageReceived += Log;
        }
        private static void Log(string message, string stack, LogType type)
        {
            if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) failed = true;
        }
        private static T[] All<T>() where T : Component => SceneManager.GetActiveScene().GetRootGameObjects().SelectMany(x => x.GetComponentsInChildren<T>(true)).ToArray();
        private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
        private static void Tick()
        {
            try
            {
                if (EditorApplication.timeSinceStartup > deadline) throw new TimeoutException("Scene smoke timed out");
                if (!EditorApplication.isPlaying || EditorApplication.isCompiling) return;
                if (failed) throw new Exception("Runtime logged errors; inspect smoke log");
                if (stage == 0 && SceneManager.GetActiveScene().name == "Menu")
                {
                    if (All<MenuInstaller>().Single().Container == null) return;
                    var controls = All<SceneControlsView>().Single();
                    controls.ShowHelp(); controls.HideHelp(); controls.ShowAbout(); controls.HideAbout();
                    All<Button>().Single(x => x.name == "ButtonStart").onClick.Invoke();
                    stage = 1; Debug.Log("SMOKE menu start invoked");
                }
                else if (stage == 1 && SceneManager.GetActiveScene().name == "Game")
                {
                    var scope = All<BattleInstaller>().Single();
                    if (scope.Container == null || All<GridCellView>().Length < 980) return;
                    var battle = scope.Container.Resolve<BattleModel>();
                    Check(battle.Units.Count() == All<UnitView>().Count(x => x.gameObject.activeInHierarchy), "Active unit count");
                    Check(battle.GetLivingUnits(Team.Player).Count == 2, "Player count");
                    Check(battle.GetLivingUnits(Team.Enemy).Count > 0, "Enemy count");
                    All<Button>().Single(x => x.name == "StopButton").onClick.Invoke();
                    Check(Time.timeScale == 0, "Pause");
                    All<Button>().Single(x => x.name == "ContinueButton").onClick.Invoke();
                    Check(Time.timeScale == 1, "Resume");
                    stage = 2; deadline = EditorApplication.timeSinceStartup + 5;
                    Debug.Log("SMOKE game initialized: " + battle.Units.Count() + " active units, 980 cells; pause/resume passed");
                }
                else if (stage == 2 && EditorApplication.timeSinceStartup > deadline - 1)
                {
                    All<Button>().Single(x => x.name == "BackToMenuButton").onClick.Invoke();
                    stage = 3; deadline = EditorApplication.timeSinceStartup + 30;
                }
                else if (stage == 3 && SceneManager.GetActiveScene().name == "Menu")
                {
                    Check(All<MenuInstaller>().Single().Container != null, "Menu reload scope");
                    Check(!failed, "Runtime logged errors; inspect smoke log");
                    Debug.Log("SMOKE SUCCESS: Menu -> Game -> Menu; no runtime errors");
                    Finish(0);
                }
            }
            catch (Exception e) { Debug.LogException(e); Finish(1); }
        }
        private static void Finish(int code)
        {
            SessionState.SetBool(Key, false); EditorApplication.update -= Tick;
            UnityEngine.Application.logMessageReceived -= Log; EditorApplication.Exit(code);
        }
    }
}
