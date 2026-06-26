using MXMeet.Auth;
using MXMeet.Models;
using UnityEngine;
using UnityEngine.UI;

namespace MXMeet.UI
{
    /// <summary>
    /// Legacy standalone registration UI. Kept for scenes that still reference it,
    /// but registration is delegated to AuthController so UI does not call Firebase directly.
    /// </summary>
    public class RegisterUI : MonoBehaviour
    {
        [Header("UI References")]
        public InputField emailInput;
        public InputField passwordInput;
        public InputField displayNameInput;
        public Button registerButton;
        public Text feedbackText;

        private void Awake()
        {
            if (emailInput == null || passwordInput == null || displayNameInput == null || registerButton == null)
            {
                Debug.LogError("[RegisterUI] One or more UI references are not assigned.");
                enabled = false;
                return;
            }

            registerButton.onClick.AddListener(OnRegisterClicked);
        }

        private void OnDestroy()
        {
            Unsubscribe();
        }

        private void OnRegisterClicked()
        {
            string email = emailInput.text.Trim();
            string password = passwordInput.text;
            string displayName = displayNameInput.text.Trim();

            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password) || string.IsNullOrEmpty(displayName))
            {
                ShowFeedback("Please fill in all fields.");
                return;
            }

            if (password.Length < 6)
            {
                ShowFeedback("Password must be at least 6 characters.");
                return;
            }

            if (AuthController.Instance == null)
            {
                ShowFeedback("Authentication is not ready.");
                return;
            }

            SetInteraction(false);
            ShowFeedback("Registering...");
            Unsubscribe();
            AuthController.Instance.OnRegisterSuccess += HandleRegisterSuccess;
            AuthController.Instance.OnRegisterFailed += HandleRegisterFailed;
            AuthController.Instance.Register(displayName, email, password);
        }

        private void HandleRegisterSuccess(UserModel user)
        {
            Unsubscribe();
            ShowFeedback("Registration successful! You can now log in.");
            SetInteraction(true);
        }

        private void HandleRegisterFailed(string error)
        {
            Unsubscribe();
            Debug.LogError($"[RegisterUI] Registration failed: {error}");
            ShowFeedback($"Error: {error}");
            SetInteraction(true);
        }

        private void Unsubscribe()
        {
            if (AuthController.Instance == null) return;
            AuthController.Instance.OnRegisterSuccess -= HandleRegisterSuccess;
            AuthController.Instance.OnRegisterFailed -= HandleRegisterFailed;
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
            if (feedbackText != null) feedbackText.text = message;
            else Debug.Log(message);
        }
    }
}
