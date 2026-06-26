using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace MXMeet.Editor
{
    /// <summary>
    /// Run "MXMeet → Fix Scenes for PC Testing" after every "Build All Scenes".
    /// - Replaces StandaloneInputModule with InputSystemUIInputModule on all EventSystems
    /// - Sets all World Space canvases to Screen Space - Overlay
    /// - Adds Back button to MRView scene
    /// </summary>
    public static class MXMeetPCTestingFix
    {
        static readonly string[] Scenes =
        {
            "Assets/Scenes/MXMeet/Bootstrap.unity",
            "Assets/Scenes/MXMeet/Login.unity",
            "Assets/Scenes/MXMeet/AvatarSelection.unity",
            "Assets/Scenes/MXMeet/MainMenu.unity",
            "Assets/Scenes/MXMeet/Lobby.unity",
            "Assets/Scenes/MXMeet/MRView.unity",
        };

        [MenuItem("MXMeet/Fix Scenes for PC Testing")]
        public static void FixAll()
        {
            foreach (var path in Scenes)
                FixScene(path);

            AddXRINetworkGameManagerToBootstrap();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("MXMeet", "All scenes fixed for PC testing!\n\nYou can now press Play from Bootstrap.", "OK");
        }

        static void AddXRINetworkGameManagerToBootstrap()
        {
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/MXMeet/Bootstrap.unity", OpenSceneMode.Single);

            AddPrefabIfMissing(
                "Assets/VRMPAssets/Prefabs/Managers/XRI_Network_Game_Manager.prefab",
                "XRI_Network_Game_Manager");

            AddPrefabIfMissing(
                "Assets/VRMPAssets/Prefabs/Managers/Network Manager VR Mutliplayer.prefab",
                "Network Manager VR Mutliplayer");

            EditorSceneManager.SaveScene(scene);
        }

        static void AddPrefabIfMissing(string prefabPath, string displayName)
        {
            // Check by root object name to avoid type-lookup issues
            foreach (var root in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
            {
                if (root.name == displayName) return; // already present
            }

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
            {
                Debug.LogWarning($"[MXMeetPCTestingFix] Prefab not found: {prefabPath}");
                return;
            }

            PrefabUtility.InstantiatePrefab(prefab);
            Debug.Log($"[MXMeetPCTestingFix] Added {displayName} to Bootstrap scene.");
        }

        static void FixScene(string path)
        {
            var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);

            // ── 1. Fix EventSystem input module ──────────────────────────
            var es = Object.FindFirstObjectByType<EventSystem>();
            if (es != null)
            {
                var old = es.GetComponent<StandaloneInputModule>();
                if (old != null) Object.DestroyImmediate(old);
                if (es.GetComponent<InputSystemUIInputModule>() == null)
                    es.gameObject.AddComponent<InputSystemUIInputModule>();
                EditorUtility.SetDirty(es.gameObject);
            }

            // ── 2. Set all World Space canvases to Screen Space Overlay ──
            foreach (var cv in Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))
            {
                if (cv.renderMode == RenderMode.WorldSpace)
                {
                    cv.renderMode = RenderMode.ScreenSpaceOverlay;
                    EditorUtility.SetDirty(cv);
                }
            }

            // ── 3. MRView: add Back button if missing ─────────────────────
            if (path.Contains("MRView"))
                AddMRViewBackButton();

            EditorSceneManager.SaveScene(scene);
            Debug.Log($"[MXMeetPCTestingFix] Fixed: {path}");
        }

        static void AddMRViewBackButton()
        {
            // Find SetupPanel inside MRViewCanvas
            var setupPanel = GameObject.Find("SetupPanel");
            if (setupPanel == null) { Debug.LogWarning("[MXMeetPCTestingFix] SetupPanel not found in MRView."); return; }

            // Remove old BackButton if it exists
            var existing = setupPanel.transform.Find("BackButton");
            if (existing != null) Object.DestroyImmediate(existing.gameObject);

            // Create button GameObject
            var btnGO = new GameObject("BackButton");
            btnGO.transform.SetParent(setupPanel.transform, false);

            var img = btnGO.AddComponent<Image>();
            img.color = new Color(0.10f, 0.13f, 0.22f, 1.0f);

            var btn = btnGO.AddComponent<Button>();
            btn.targetGraphic = img;

            var rt = btnGO.GetComponent<RectTransform>();
            rt.sizeDelta        = new Vector2(300, 62);
            rt.anchoredPosition = new Vector2(0, -160);

            // Label
            var labelGO = new GameObject("Text");
            labelGO.transform.SetParent(btnGO.transform, false);
            var tmp = labelGO.AddComponent<TextMeshProUGUI>();
            tmp.text      = "← Back to Menu";
            tmp.fontSize  = 22;
            tmp.color     = new Color(0.92f, 0.96f, 1.0f, 1.0f);
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            var lrt = labelGO.GetComponent<RectTransform>();
            lrt.anchorMin = Vector2.zero;
            lrt.anchorMax = Vector2.one;
            lrt.offsetMin = Vector2.zero;
            lrt.offsetMax = Vector2.zero;

            // Wire to VRMeetUpViewUI_V2.backButton field
            var canvas = GameObject.Find("MRViewCanvas");
            if (canvas != null)
            {
                var ui = canvas.GetComponent<MXMeet.UI.VRMeetUpViewUI_V2>();
                if (ui != null)
                {
                    ui.backButton = btn;
                    EditorUtility.SetDirty(ui);
                }
            }

            EditorUtility.SetDirty(btnGO);
        }
    }
}
