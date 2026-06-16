using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MXMeet.Editor
{
    public static class MXMeetSceneSetup
    {
        private static readonly string[] SceneNames =
        {
            "Bootstrap",
            "Login",
            "AvatarSelection",
            "MainMenu",
            "Lobby",
            "MRView"
        };

        private const string SceneFolder = "Assets/Scenes/MXMeet";

        [MenuItem("MXMeet/Create All Scenes + Build Settings")]
        public static void CreateAllScenes()
        {
            // Create folder
            if (!AssetDatabase.IsValidFolder("Assets/Scenes"))
                AssetDatabase.CreateFolder("Assets", "Scenes");
            if (!AssetDatabase.IsValidFolder(SceneFolder))
                AssetDatabase.CreateFolder("Assets/Scenes", "MXMeet");

            var buildScenes = new List<EditorBuildSettingsScene>();

            foreach (string sceneName in SceneNames)
            {
                string scenePath = $"{SceneFolder}/{sceneName}.unity";

                if (!File.Exists(Path.GetFullPath(scenePath)))
                {
                    var newScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
                    EditorSceneManager.SaveScene(newScene, scenePath);
                    EditorSceneManager.CloseScene(newScene, true);
                    Debug.Log($"[MXMeet] Created: {scenePath}");
                }
                else
                {
                    Debug.Log($"[MXMeet] Already exists: {scenePath}");
                }

                buildScenes.Add(new EditorBuildSettingsScene(scenePath, true));
            }

            EditorBuildSettings.scenes = buildScenes.ToArray();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog(
                "MXMeet Scene Setup",
                "Done!\n\n6 scenes created in Assets/Scenes/MXMeet/\nAll added to Build Settings (indices 0–5).",
                "OK"
            );
        }
    }
}
