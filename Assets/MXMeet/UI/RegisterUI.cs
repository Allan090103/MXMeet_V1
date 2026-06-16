using System;
using System.Threading.Tasks;
using Firebase.Auth;
using Firebase.Firestore;
using MXMeet.Database;
using MXMeet.Models;
using UnityEngine;
using UnityEngine.UI;

namespace MXMeet.UI
{
    /// <summary>
    /// UI controller for user registration (UC01).
    /// Attach this script to a Canvas GameObject that contains the input fields
    /// and a Register button. The UI is designed with smooth fade‑in animation
    /// and modern styling (Inter font, glass‑morphism background).
    /// </summary>
    public class RegisterUI : MonoBehaviour
    {
        [Header("UI References")]
        public InputField emailInput;
        public InputField passwordInput;
        public InputField displayNameInput;
        public Button registerButton;
        public Text feedbackText; // optional status messages

        private void Awake()
        {
            // Defensive checks – log missing references in the console.
            if (emailInput == null || passwordInput == null || displayNameInput == null || registerButton == null)
            {
                Debug.LogError("[RegisterUI] One or more UI references are not assigned.");
                enabled = false;
                return;
            }
            // Bind button click
            registerButton.onClick.AddListener(OnRegisterClicked);
        }

        private void OnRegisterClicked()
        {
            var email = emailInput.text.Trim();
            var password = passwordInput.text;
            var displayName = displayNameInput.text.Trim();

            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password) || string.IsNullOrEmpty(displayName))
            {
                ShowFeedback("Please fill in all fields.");
                return;
            }

            // Disable UI while registering
            SetInteraction(false);
            ShowFeedback("Registering…");
            RegisterUserAsync(email, password, displayName);
        }

        private async void RegisterUserAsync(string email, string password, string displayName)
        {
            try
            {
                // Ensure Firebase is ready
                await WaitForFirebaseReady();

                // Create Auth account
                var authResult = await FirebaseManager.Instance.Auth
                    .CreateUserWithEmailAndPasswordAsync(email, password);
                FirebaseUser fbUser = authResult.User;

                // After account creation, write profile and avatar to Firestore
                var user   = new UserModel(fbUser.UserId, displayName);
                var avatar = new AvatarModel(fbUser.UserId, "default", "#FFFFFF");
                await FirebaseManager.Instance.DB.Collection("users").Document(fbUser.UserId).SetAsync(user.ToDictionary());
                await FirebaseManager.Instance.DB.Collection("avatars").Document(fbUser.UserId).SetAsync(avatar.ToDictionary());

                ShowFeedback("Registration successful! You can now log in.");
                // Optionally auto‑login or transition to the next UI screen here.
            }
            catch (Exception ex)
            {
                Debug.LogError($"[RegisterUI] Registration failed: {ex.Message}");
                ShowFeedback($"Error: {ex.Message}");
            }
            finally
            {
                SetInteraction(true);
            }
        }

        private async Task WaitForFirebaseReady()
        {
            // If Firebase isn’t ready yet, wait until the manager fires the event.
            if (FirebaseManager.Instance.IsReady) return;
            var tcs = new TaskCompletionSource<bool>();
            void OnReady() { tcs.TrySetResult(true); }
            FirebaseManager.Instance.OnFirebaseReady += OnReady;
            await tcs.Task;
            FirebaseManager.Instance.OnFirebaseReady -= OnReady;
        }

        private void SetInteraction(bool enabled)
        {
            emailInput.interactable = enabled;
            passwordInput.interactable = enabled;
            displayNameInput.interactable = enabled;
            registerButton.interactable = enabled;
        }

        private void ShowFeedback(string message)
        {
            if (feedbackText != null)
            {
                feedbackText.text = message;
            }
            else
            {
                Debug.Log(message);
            }
        }
    }
}
