using MXMeet.Auth;
using MXMeet.Database;
using MXMeet.VRMeetUp;
using UnityEngine;

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

            if (Object.FindFirstObjectByType<VRMeetUpIntegrationController>() == null)
            {
                var vrGo = new GameObject("VRMeetUpIntegrationController");
                vrGo.AddComponent<VRMeetUpIntegrationController>();
                Object.DontDestroyOnLoad(vrGo);
            }
        }
    }
}
