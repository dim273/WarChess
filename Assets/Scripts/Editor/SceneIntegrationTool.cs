using System;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Unity.Cinemachine;
using WarChess.Composition;
using WarChess.UnityViews;

namespace WarChess.EditorTools
{
    /// <summary>显式执行的场景迁移工具；不会在导入或启动编辑器时自动修改资产。</summary>
    public static class SceneIntegrationTool
    {
        private static readonly string[] Targets = {
            "Assets/Scenes/Game.unity", "Assets/Scenes/Menu.unity",
            "Assets/Prefab/Role/Role_A.prefab", "Assets/Prefab/Role/Role_B.prefab", "Assets/Prefab/Enemy.prefab" };

        public static void Run()
        {
            try
            {
                // 原文件及 GUID 一起备份到 Assets 外，避免备份被当作游戏资产导入。
                string backup = "Backups/SceneIntegration-" + DateTime.Now.ToString("yyyyMMdd-HHmmss");
                foreach (string path in Targets)
                {
                    string dest = Path.Combine(backup, path);
                    Directory.CreateDirectory(Path.GetDirectoryName(dest));
                    File.Copy(path, dest);
                    if (File.Exists(path + ".meta")) File.Copy(path + ".meta", dest + ".meta");
                }
                Debug.Log("INTEGRATION BACKUP " + Path.GetFullPath(backup));
                MigrateUnit(Targets[2], false, 4, 1, 5, .2f);
                MigrateUnit(Targets[3], false, 10, 2, 4, .5f);
                MigrateUnit(Targets[4], true, 2, 3, 3, .1f);
                if (!AssetDatabase.IsValidFolder("Assets/Scripts/Generated"))
                    AssetDatabase.CreateFolder("Assets/Scripts", "Generated");
                const string gridPath = "Assets/Scripts/Generated/GridCell.prefab";
                if (AssetDatabase.LoadAssetAtPath<GameObject>(gridPath) == null)
                {
                    var grid = PrefabUtility.LoadPrefabContents("Assets/Prefab/GridSystemVisualSingle.prefab");
                    try
                    {
                        Clean(grid);
                        var view = Ensure<GridCellView>(grid);
                        Set(view, "meshRenderer", grid.GetComponentInChildren<MeshRenderer>(true));
                        PrefabUtility.SaveAsPrefabAsset(grid, gridPath);
                    }
                    finally { PrefabUtility.UnloadPrefabContents(grid); }
                }
                SetupGame();
                SetupMenu();
                WarChess.Tests.CoreRegressionTests.RunAll(Debug.Log);
                AssetDatabase.SaveAssets();
                EditorSceneManager.OpenScene(Targets[0]);
                Debug.Log("INTEGRATION SUCCESS: Game + Menu; backup=" + backup);
            }
            catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
        }

        private static void MigrateUnit(string path, bool enemy, int ap, int move, int attack, float hit)
        {
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                bool isNew = root.GetComponent<UnitView>() == null;
                Clean(root);
                var view = Ensure<UnitView>(root);
                if (isNew)
                {
                    Set(view, "team", enemy ? 1 : 0); Set(view, "maxActionPoints", ap);
                    Set(view, "moveRange", move); Set(view, "attackRange", attack);
                    Set(view, "attackHitDelay", hit); Set(view, "invertForward", !enemy);
                }
                Set(view, "animator", root.GetComponentInChildren<Animator>(true));
                Set(view, "selectionCollider", root.GetComponentInChildren<Collider>(true));
                var selected = root.GetComponentsInChildren<Transform>(true).FirstOrDefault(x => x.name == "SelectedVisual");
                if (selected != null) Set(view, "selectedVisual", selected.gameObject);
                var fill = root.GetComponentsInChildren<Image>(true).FirstOrDefault(x => x.name.Equals("healthBar", StringComparison.OrdinalIgnoreCase));
                if (fill == null) throw new InvalidOperationException("Health image not found: " + path);
                Set(view, "healthFill", fill);
                Set(view, "actionPointsText", root.GetComponentsInChildren<TextMeshProUGUI>(true).First(x => x.name == "PointsText"));
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }

        private static void SetupGame()
        {
            var scene = EditorSceneManager.OpenScene(Targets[0]);
            // OpenScene 会卸载前一场景的未使用资产，必须在打开目标场景之后重新加载引用。
            var cell = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Scripts/Generated/GridCell.prefab").GetComponent<GridCellView>();
            if (cell == null) throw new InvalidOperationException("Generated GridCellView could not be loaded");
            foreach (var root in scene.GetRootGameObjects()) Clean(root);
            var canvas = Find("Canvas");
            var board = Ensure<BoardView>(Find("GridSystemVisual"));
            // 旧 GridSystem 不使用管理对象位置；现有地图按世界原点、2 米网格摆放。
            // 新 BoardView 使用自身 Transform，因此清除旧管理对象的任意偏移。
            board.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            board.transform.localScale = Vector3.one;
            Set(board, "cellPrefab", cell); Set(board, "cellContainer", board.transform);
            var input = Ensure<BattleInputView>(Find("RoleActionSystem"));
            Set(input, "inputCamera", Camera.main); Set(input, "boardView", board);
            Set(input, "unitLayerMask", LayerMask.GetMask("Role")); Set(input, "boardLayerMask", LayerMask.GetMask("Ground"));
            var hud = Ensure<BattleHudView>(canvas);
            var container = Find("ActionButtonContainer").transform;
            string[] names = { "MoveButton", "AttackButton", "DefendButton" };
            string[] fields = { "moveButton", "attackButton", "defendButton" };
            string[] labels = { "移动", "攻击", "防御" };
            for (int i = 0; i < names.Length; i++)
            {
                var child = container.Find(names[i]);
                var go = child != null ? child.gameObject : (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefab/UI/ActionButton.prefab"), container);
                go.name = names[i];
                // 仅对新建的三个 UI 按钮断开旧模板连接，避免运行时加载已缺脚本的旧模板。
                if (PrefabUtility.IsPartOfPrefabInstance(go)) PrefabUtility.UnpackPrefabInstance(go, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                Clean(go);
                var button = go.GetComponent<Button>(); Clear(button);
                go.GetComponentInChildren<TextMeshProUGUI>(true).text = labels[i];
                Set(hud, fields[i], button);
            }
            var end = Find("TurnButton").GetComponent<Button>(); Clear(end); Set(hud, "endTurnButton", end);
            Set(hud, "turnText", Find("TurnInfo").GetComponent<TextMeshProUGUI>());
            Set(hud, "actionPointsText", Find("ActionPointsTMP").GetComponent<TextMeshProUGUI>());
            Set(hud, "selectedUnitText", Label(canvas.transform, "SelectedUnitText", new Vector2(240, -30)));
            Set(hud, "statusText", Label(canvas.transform, "BattleStatusText", new Vector2(240, -65)));
            var result = Label(canvas.transform, "BattleResultText", new Vector2(240, -105));
            Set(hud, "resultText", result); Set(hud, "resultPanel", result.gameObject); result.gameObject.SetActive(false);
            var busy = Find("ActionBusyUI"); Set(hud, "busyOverlay", busy); busy.SetActive(false);
            Find("IsEnemyTurnUI").SetActive(false);
            var controls = Ensure<SceneControlsView>(canvas);
            var pause = Find("Menu"); Set(controls, "pausePanel", pause); pause.SetActive(false);
            Bind("StopButton", controls.Pause); Bind("ContinueButton", controls.Resume);
            Bind("RestartButton", controls.Restart); Bind("BackToMenuButton", controls.BackToMenu); Bind("ExitButton", controls.Quit);
            var camera = Ensure<BattleCameraView>(Find("CameraController"));
            Set(camera, "cvc", All<CinemachineVirtualCameraBase>().Single());
            var scope = All<BattleInstaller>().SingleOrDefault() ?? Ensure<BattleInstaller>(new GameObject("BattleScope"));
            Set(scope, "boardView", board); Set(scope, "inputView", input); Set(scope, "hudView", hud); Set(scope, "sceneControls", controls);
            var units = All<UnitView>().OrderBy(x => x.name).ThenBy(x => x.transform.position.x).ThenBy(x => x.transform.position.z).ToArray();
            if (units.Length != 22) throw new InvalidOperationException("Expected 22 units, found " + units.Length);
            for (int i = 0; i < units.Length; i++) Set(units[i], "unitId", units[i].name + "-" + i.ToString("D2"));
            // 禁用的备用敌人保留挂载，但不擅自启用，也不能注册到本场战斗。
            units = units.Where(x => x.gameObject.activeInHierarchy).ToArray();
            foreach (var unit in units) Debug.Log("INTEGRATION ACTIVE " + unit.name);
            var so = new SerializedObject(scope); var list = so.FindProperty("unitViews"); list.arraySize = units.Length;
            for (int i = 0; i < units.Length; i++) list.GetArrayElementAtIndex(i).objectReferenceValue = units[i];
            so.ApplyModifiedPropertiesWithoutUndo();
            board.ConfigureGeometry(so.FindProperty("cellSize").floatValue);
            // 保存前执行与运行时完全相同的出生点/障碍/重复占位校验。
            using (var model = (WarChess.Domain.BattleModel)typeof(BattleInstaller)
                .GetMethod("CreateBattle", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                .Invoke(scope, null))
                Debug.Log("INTEGRATION spawn validation passed: " + model.Units.Count());
            ValidateMissing();
            if (new SerializedObject(board).FindProperty("cellPrefab").objectReferenceValue == null)
                throw new InvalidOperationException("Board cell prefab reference was lost before save");
            EditorSceneManager.SaveScene(scene);
            Debug.Log("INTEGRATION Game saved: " + units.Length + " active units (22 mounted)");
        }

        private static void SetupMenu()
        {
            var scene = EditorSceneManager.OpenScene(Targets[1]);
            foreach (var root in scene.GetRootGameObjects()) Clean(root);
            var canvas = Find("Canvas"); var view = Ensure<MenuView>(canvas); var controls = Ensure<SceneControlsView>(canvas);
            var help = Find("HelpPanel"); var about = Find("AboutPanel");
            Set(controls, "helpPanel", help); Set(controls, "aboutPanel", about);
            var start = Find("ButtonStart").GetComponent<Button>(); Clear(start); Set(view, "newGameButton", start);
            var exits = All<Button>().Where(x => x.name == "ButtonExit").ToArray();
            var quit = exits.Single(x => !x.transform.IsChildOf(help.transform) && !x.transform.IsChildOf(about.transform));
            Clear(quit); Set(view, "quitButton", quit);
            Bind("ButtonHelp", controls.ShowHelp); Bind("ButtonAbout", controls.ShowAbout);
            Bind(exits.Single(x => x.transform.IsChildOf(help.transform)), controls.HideHelp);
            Bind(exits.Single(x => x.transform.IsChildOf(about.transform)), controls.HideAbout);
            help.SetActive(false); about.SetActive(false);
            var scope = All<MenuInstaller>().SingleOrDefault() ?? Ensure<MenuInstaller>(new GameObject("MenuScope"));
            Set(scope, "menuView", view); Set(scope, "sceneControls", controls);
            ValidateMissing(); EditorSceneManager.SaveScene(scene); Debug.Log("INTEGRATION Menu saved");
        }

        private static TextMeshProUGUI Label(Transform parent, string name, Vector2 position)
        {
            var found = parent.Find(name);
            if (found != null) return found.GetComponent<TextMeshProUGUI>();
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI)); go.transform.SetParent(parent, false);
            var label = go.GetComponent<TextMeshProUGUI>();
            label.font = All<TextMeshProUGUI>().First(x => x != label && x.font != null).font;
            label.fontSize = 24; label.color = Color.white; label.raycastTarget = false;
            var rect = label.rectTransform; rect.anchorMin = rect.anchorMax = new Vector2(0, 1);
            rect.sizeDelta = new Vector2(460, 35); rect.anchoredPosition = position;
            return label;
        }
        private static T[] All<T>() where T : Component => UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects().SelectMany(x => x.GetComponentsInChildren<T>(true)).ToArray();
        private static GameObject Find(string name)
        {
            var matches = All<Transform>().Where(x => x.name == name).ToArray();
            var roots = matches.Where(x => x.parent == null).ToArray();
            return (roots.Length == 1 ? roots[0] : matches.Single()).gameObject;
        }
        private static T Ensure<T>(GameObject go) where T : Component => go.GetComponent<T>() ?? go.AddComponent<T>();
        private static void Clean(GameObject root)
        {
            foreach (var t in root.GetComponentsInChildren<Transform>(true)) GameObjectUtility.RemoveMonoBehavioursWithMissingScript(t.gameObject);
        }
        private static void ValidateMissing()
        {
            if (All<Transform>().Any(t => GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject) > 0))
                throw new InvalidOperationException("Scene still has missing scripts");
        }
        private static void Clear(Button button)
        {
            for (int i = button.onClick.GetPersistentEventCount() - 1; i >= 0; i--) UnityEventTools.RemovePersistentListener(button.onClick, i);
            EditorUtility.SetDirty(button);
        }
        private static void Bind(string name, UnityAction action) => Bind(Find(name).GetComponent<Button>(), action);
        private static void Bind(Button button, UnityAction action) { Clear(button); UnityEventTools.AddPersistentListener(button.onClick, action); }
        private static void Set(UnityEngine.Object target, string field, object value)
        {
            var so = new SerializedObject(target); var p = so.FindProperty(field);
            if (p == null) throw new InvalidOperationException(target.name + "." + field + " not found");
            if (value is int i) p.intValue = i;
            else if (value is float f) p.floatValue = f;
            else if (value is bool b) p.boolValue = b;
            else if (value is string s) p.stringValue = s;
            else p.objectReferenceValue = (UnityEngine.Object)value;
            so.ApplyModifiedPropertiesWithoutUndo(); EditorUtility.SetDirty(target);
        }
    }
}
