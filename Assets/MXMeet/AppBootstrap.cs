using System.Collections;
using Firebase;
using Firebase.Auth;
using Firebase.Extensions;
using Firebase.Firestore;
using MXMeet.Database;
using UnityEngine;
using UnityEngine.SceneManagement;
using XRMultiplayer;

namespace MXMeet
{
    /// <summary>
    /// Entry point for MXMeet. Place this on a persistent GameObject in the
    /// first scene (Bootstrap scene).
    ///
    /// Boot sequence:
    ///   1. Initialise Firebase (FirebaseManager)
    ///   2. Check if user is already logged in (Firebase Auth persistent session)
    ///   3. If logged in → load MainMenu
    ///   4. If not logged in → load Login scene
    ///
    /// VRMeetUp's XRINetworkGameManager does its own UGS anonymous auth on Awake.
    /// MXMeet adds Firebase email/password auth ON TOP of that for user profiles.
    /// Both auth systems can coexist — they use different services.
    /// </summary>
    public class AppBootstrap : MonoBehaviour
    {
        [Header("Scenes")]
        public string loginScene    = "Login";
        public string mainMenuScene = "MainMenu";

        [Header("Bootstrap UI")]
        public UnityEngine.UI.Image  loadingBar;
        public TMPro.TextMeshProUGUI statusText;

        private void Start()
        {
            StartCoroutine(Boot());
        }

        private IEnumerator Boot()
        {
            SetStatus("Starting MXMeet...");
            SetProgress(0.1f);
            yield return null;

            // Wait for FirebaseManager to initialise
            SetStatus("Initialising Firebase...");
            float timeout = 10f;
            float elapsed = 0f;
            while (!FirebaseManager.Instance.IsReady && elapsed < timeout)
            {
                elapsed += Time.deltaTime;
                SetProgress(0.1f + (elapsed / timeout) * 0.4f);
                yield return null;
            }

            if (!FirebaseManager.Instance.IsReady)
            {
                SetStatus("Firebase failed to initialise. Please check your internet connection.");
                yield break;
            }

            SetProgress(0.5f);
            SetStatus("Checking session...");
            yield return new WaitForSeconds(0.3f);

            // Check for persistent Firebase Auth session
            FirebaseUser currentUser = FirebaseManager.Instance.Auth.CurrentUser;

            if (currentUser != null)
            {
                SetStatus($"Welcome back!");
                SetProgress(0.8f);
                yield return new WaitForSeconds(0.5f);

                // Reload user data into AuthController
                yield return StartCoroutine(RestoreSession(currentUser));
                SetProgress(1.0f);
                yield return new WaitForSeconds(0.2f);
                SceneManager.LoadScene(mainMenuScene);
            }
            else
            {
                SetProgress(1.0f);
                SetStatus("Please log in.");
                yield return new WaitForSeconds(0.5f);
                SceneManager.LoadScene(loginScene);
            }
        }

        private IEnumerator RestoreSession(FirebaseUser fbUser)
        {
            bool done = false;
            string error = null;

            // Load user profile from Firestore on the main thread
            FirebaseManager.Instance.DB
                .Collection("users")
                .Document(fbUser.UserId)
                .GetSnapshotAsync()
                .ContinueWithOnMainThread(task =>
                {
                    if (task.IsCompletedSuccessfully && task.Result.Exists)
                    {
                        var user = task.Result.ConvertTo<MXMeet.Models.UserModel>();
                        Auth.AuthController.Instance?.SetRestoredUser(user);
                    }
                    else
                    {
                        error = "Could not load profile.";
                    }
                    done = true;
                });

            while (!done) yield return null;

            if (error != null)
            {
                SetStatus($"Session restore failed: {error}");
                yield return new WaitForSeconds(1f);
                SceneManager.LoadScene(loginScene);
            }
        }

        private void SetStatus(string msg)
        {
            if (statusText != null) statusText.text = msg;
            Debug.Log($"[Bootstrap] {msg}");
        }

        private void SetProgress(float value)
        {
            if (loadingBar != null) loadingBar.fillAmount = value;
        }
    }
}
