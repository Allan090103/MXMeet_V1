using MXMeet.Auth;
using MXMeet.Models;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace MXMeet.UI
{
    /// <summary>
    /// Controls the Login and Registration screen.
    ///
    /// Scene setup:
    ///   - LoginPanel  : GameObject (shown by default)
    ///   - RegisterPanel : GameObject (hidden by default)
    ///   - UsernameField, EmailField, PasswordField : TMP_InputField
    ///   - ErrorText : TextMeshProUGUI
    ///   - LoginButton, RegisterButton, SwitchToRegisterButton, SwitchToLoginButton : Button
    /// </summary>
    public class LoginUI : MonoBehaviour
    {
        [Header("Panels")]
        public GameObject loginPanel;
        public GameObject registerPanel;

        [Header("Login Fields")]
        public TMP_InputField loginEmailField;
        public TMP_InputField loginPasswordField;

        [Header("Register Fields")]
        public TMP_InputField registerUsernameField;
        public TMP_InputField registerEmailField;
        public TMP_InputField registerPasswordField;

        [Header("Buttons")]
        public Button loginButton;
        public Button registerButton;
        public Button switchToRegisterButton;
        public Button switchToLoginButton;

        [Header("Feedback")]
        public TextMeshProUGUI errorText;
        public GameObject      loadingIndicator;

        [Header("Navigation")]
        [Tooltip("Scene to load after successful login")]
        public string avatarSelectionScene = "AvatarSelection";

        private void Start()
        {
            if (AuthController.Instance == null)
            {
                SetError("Authentication system is not ready. Start from Bootstrap.");
                SetLoading(false);
                return;
            }

            // Wire up buttons
            if (loginButton != null) loginButton.onClick.AddListener(OnLoginClicked);
            if (registerButton != null) registerButton.onClick.AddListener(OnRegisterClicked);
            if (switchToRegisterButton != null) switchToRegisterButton.onClick.AddListener(() => ShowPanel(false));
            if (switchToLoginButton != null) switchToLoginButton.onClick.AddListener(() => ShowPanel(true));

            // Subscribe to AuthController events
            AuthController.Instance.OnLoginSuccess    += HandleLoginSuccess;
            AuthController.Instance.OnLoginFailed     += HandleLoginFailed;
            AuthController.Instance.OnRegisterSuccess += HandleRegisterSuccess;
            AuthController.Instance.OnRegisterFailed  += HandleRegisterFailed;

            ShowPanel(true); // show login by default
            SetError("");
            SetLoading(false);
        }

        private void OnDestroy()
        {
            if (AuthController.Instance == null) return;
            AuthController.Instance.OnLoginSuccess    -= HandleLoginSuccess;
            AuthController.Instance.OnLoginFailed     -= HandleLoginFailed;
            AuthController.Instance.OnRegisterSuccess -= HandleRegisterSuccess;
            AuthController.Instance.OnRegisterFailed  -= HandleRegisterFailed;
        }

        // ── Button Handlers ───────────────────────────────────────────────
        private void OnLoginClicked()
        {
            if (loginEmailField == null || loginPasswordField == null)
            {
                SetError("Login fields are not assigned.");
                return;
            }

            SetError("");
            string email    = loginEmailField.text.Trim();
            string password = loginPasswordField.text;

            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                SetError("Please enter your email and password.");
                return;
            }

            SetLoading(true);
            AuthController.Instance?.Login(email, password);
        }

        private void OnRegisterClicked()
        {
            if (registerUsernameField == null || registerEmailField == null || registerPasswordField == null)
            {
                SetError("Registration fields are not assigned.");
                return;
            }

            SetError("");
            string username = registerUsernameField.text.Trim();
            string email    = registerEmailField.text.Trim();
            string password = registerPasswordField.text;

            if (string.IsNullOrEmpty(username))  { SetError("Username cannot be empty."); return; }
            if (string.IsNullOrEmpty(email))      { SetError("Email cannot be empty."); return; }
            if (password.Length < 6)              { SetError("Password must be at least 6 characters."); return; }

            SetLoading(true);
            AuthController.Instance?.Register(username, email, password);
        }

        // ── Event Handlers ────────────────────────────────────────────────
        private void HandleLoginSuccess(UserModel user)
        {
            SetLoading(false);
            Debug.Log($"[LoginUI] Login success: {user.username}");
            SceneManager.LoadScene(avatarSelectionScene);
        }

        private void HandleLoginFailed(string error)
        {
            SetLoading(false);
            SetError(error);
        }

        private void HandleRegisterSuccess(UserModel user)
        {
            SetLoading(false);
            Debug.Log($"[LoginUI] Register success: {user.username}");
            SceneManager.LoadScene(avatarSelectionScene);
        }

        private void HandleRegisterFailed(string error)
        {
            SetLoading(false);
            SetError(error);
        }

        // ── Helpers ───────────────────────────────────────────────────────
        private void ShowPanel(bool showLogin)
        {
            if (loginPanel != null) loginPanel.SetActive(showLogin);
            if (registerPanel != null) registerPanel.SetActive(!showLogin);
            SetError("");
        }

        private void SetError(string message)
        {
            if (errorText == null) return;
            errorText.text      = message;
            errorText.gameObject.SetActive(!string.IsNullOrEmpty(message));
        }

        private void SetLoading(bool loading)
        {
            if (loadingIndicator != null) loadingIndicator.SetActive(loading);
            if (loginButton != null) loginButton.interactable = !loading;
            if (registerButton != null) registerButton.interactable = !loading;
        }
    }
}
