using MXMeet.Auth;
using MXMeet.Avatar;
using MXMeet.Database;
using MXMeet.Lobby;
using MXMeet.VRMeetUp;
using UnityEngine;
#if UNITY_EDITOR
using System.Reflection;
using CustomNetwork;
using UnityEditor;
#endif
using XRMultiplayer;

namespace MXMeet
{
    /// <summary>
    /// Ensures that Firebase and Auth managers are instantiated even when 
    /// testing scenes directly in the Unity Editor (bypassing the Bootstrap scene).
    /// </summary>
    internal static class EditorBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Init()
        {
            if (Object.FindFirstObjectByType<FirebaseManager>() == null)
            {
                var fbGo = new GameObject("FirebaseManager");
                fbGo.AddComponent<FirebaseManager>();
                Object.DontDestroyOnLoad(fbGo);
            }

            if (Object.FindFirstObjectByType<AuthController>() == null)
            {
                var authGo = new GameObject("AuthController");
                authGo.AddComponent<AuthController>();
                Object.DontDestroyOnLoad(authGo);
            }

            if (Object.FindFirstObjectByType<AvatarController>() == null)
            {
                var avatarGo = new GameObject("AvatarController");
                avatarGo.AddComponent<AvatarController>();
                Object.DontDestroyOnLoad(avatarGo);
            }

            if (Object.FindFirstObjectByType<VRMeetUpIntegrationController>() == null)
            {
                var vrGo = new GameObject("VRMeetUpIntegrationController");
                vrGo.AddComponent<VRMeetUpIntegrationController>();
                Object.DontDestroyOnLoad(vrGo);
            }

            if (Object.FindFirstObjectByType<LobbyController_V2>() == null)
            {
                var lobbyGo = new GameObject("LobbyController_V2");
                lobbyGo.AddComponent<LobbyController_V2>();
                Object.DontDestroyOnLoad(lobbyGo);
            }

            EnsureVRMeetUpManagersForEditorTesting();
        }

        internal static void EnsureVRMeetUpManagersForEditorTesting()
        {
#if UNITY_EDITOR
            if (Object.FindFirstObjectByType<Unity.Netcode.NetworkManager>() == null)
            {
                InstantiatePersistentPrefab(
                    "Assets/VRMPAssets/Prefabs/Managers/Network Manager VR Mutliplayer.prefab",
                    "Network Manager VR Mutliplayer");
            }

            EnsureVRMeetUpAuthManagerForEditorTesting();

            if (Object.FindFirstObjectByType<XRINetworkGameManager>() == null)
            {
                InstantiatePersistentPrefab(
                    "Assets/VRMPAssets/Prefabs/Managers/XRI_Network_Game_Manager.prefab",
                    "XRI_Network_Game_Manager");
            }
#endif
        }

#if UNITY_EDITOR
        private static void InstantiatePersistentPrefab(string prefabPath, string instanceName)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
            {
                Debug.LogWarning($"[EditorBootstrap] Missing VRMeetUp prefab: {prefabPath}");
                return;
            }

            GameObject instance = Object.Instantiate(prefab);
            instance.name = instanceName;
            Object.DontDestroyOnLoad(instance);
            Debug.Log($"[EditorBootstrap] Created {instanceName} for direct scene testing.");
        }

        private static void EnsureVRMeetUpAuthManagerForEditorTesting()
        {
            if (Object.FindFirstObjectByType<AuthManagerNew>() != null) return;

            GameObject authObject = new GameObject("AuthManagerNew");
            AuthManagerNew authManager = authObject.AddComponent<AuthManagerNew>();
            Object.DontDestroyOnLoad(authObject);

            GameObject signInDisplay = new GameObject("VRMeetUp Direct Test SignInDisplay");
            GameObject lobbyDisplay = new GameObject("VRMeetUp Direct Test LobbyDisplay");
            Object.DontDestroyOnLoad(signInDisplay);
            Object.DontDestroyOnLoad(lobbyDisplay);

            SetPrivateField(authManager, "signInDisplay", signInDisplay);
            SetPrivateField(authManager, "lobbyDisplay", lobbyDisplay);

            Debug.Log("[EditorBootstrap] Created AuthManagerNew for direct scene testing.");
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            if (field == null)
            {
                Debug.LogWarning($"[EditorBootstrap] Field not found: {fieldName}");
                return;
            }

            field.SetValue(target, value);
        }
#endif
    }
}
