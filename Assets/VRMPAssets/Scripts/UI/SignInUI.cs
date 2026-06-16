using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections;
using System.Linq;
using CustomNetwork;

namespace XRMultiplayer
{
    public class SignInUI : MonoBehaviour
    {
        [Header("Authentication Panels")]
        [SerializeField] GameObject[] m_AuthenticationSubPanels;

        [Header("Sign In Panel")]
        [SerializeField] TMP_InputField m_UsernameInputField;
        [SerializeField] TMP_InputField m_PasswordInputField;
        [SerializeField] Button m_SignInButton;
        [SerializeField] Button m_GoToRegistrationButton;

        [Header("Registration Panel")]
        [SerializeField] TMP_InputField m_NewUsernameInputField;
        [SerializeField] TMP_InputField m_NewPasswordInputField;
        [SerializeField] TMP_InputField m_ConfirmPasswordInputField;
        [SerializeField] Button m_RegisterButton;
        [SerializeField] Button m_BackToSignInButton;

        [Header("Connection Success Panel Buttons")]
        [SerializeField] Button m_ConnectionSuccessPanelBackButton;

        [Header("Connection Failure Panel Buttons")]
        [SerializeField] Button m_ConnectionFailurePanelBackButton;

        [Header("No Connection Panel Buttons")]
        [SerializeField] Button m_NoConnectionPanelBackButton;

        [Header("Registration Success Panel Buttons")]
        [SerializeField] Button m_RegistrationSuccessPanelBackButton;

        [Header("Status Texts")]
        [SerializeField] TMP_Text m_ConnectionSuccessText;
        [SerializeField] TMP_Text m_ConnectionFailedText;
        [SerializeField] TMP_Text m_RegistrationSuccessText;

        [Header("Warning Toast")]
        [SerializeField] GameObject m_ConnectionWarningToast;
        [SerializeField] GameObject m_RegistrationWarningToast;
        [SerializeField] TMP_Text m_ConnectionWarningText;
        [SerializeField] TMP_Text m_RegistrationWarningText;

        [Header("Dependencies")]
        [SerializeField] AuthManagerNew m_AuthenticationManager;

        // Panel IDs
        private const int PANEL_SIGN_IN = 0;
        private const int PANEL_REGISTRATION = 1;
        private const int PANEL_CONNECTION = 2;
        private const int PANEL_CONNECTION_SUCCESS = 3;
        private const int PANEL_CONNECTION_FAILED = 4;
        private const int PANEL_NO_CONNECTION = 5;
        private const int PANEL_REGISTRATION_SUCCESS = 6;

        private void Awake()
        {
            // Ensure Authentication Manager is assigned
            if (m_AuthenticationManager == null)
            {
                m_AuthenticationManager = FindFirstObjectByType<AuthManagerNew>();
            }

            // Setup button listeners
            SetupButtonListeners();
            SetupAuthenticationEvents();
        }

        private void Start()
        {
            // Initial setup
            CheckInternetConnection();
        }

        private void SetupButtonListeners()
        {
            // Sign In Panel Buttons
            m_SignInButton.onClick.AddListener(AttemptSignIn);
            m_GoToRegistrationButton.onClick.AddListener(() => ToggleAuthenticationSubPanel(PANEL_REGISTRATION));

            // Registration Panel Buttons
            m_RegisterButton.onClick.AddListener(AttemptRegistration);
            m_BackToSignInButton.onClick.AddListener(() => ToggleAuthenticationSubPanel(PANEL_SIGN_IN));

            // Connection Success Panel Buttons
            m_ConnectionSuccessPanelBackButton.onClick.AddListener(() => ToggleAuthenticationSubPanel(PANEL_SIGN_IN));

            // Connection Failure Panel Buttons
            m_ConnectionFailurePanelBackButton.onClick.AddListener(() => ToggleAuthenticationSubPanel(PANEL_SIGN_IN));

            // No Connection Panel Buttons
            m_NoConnectionPanelBackButton.onClick.AddListener(() => ToggleAuthenticationSubPanel(PANEL_SIGN_IN));

            // Registration Success Panel Buttons
            m_RegistrationSuccessPanelBackButton.onClick.AddListener(() => ToggleAuthenticationSubPanel(PANEL_SIGN_IN));
        }


        private void SetupAuthenticationEvents()
        {
            // Subscribe to authentication events
            AuthManagerNew.OnSignInSuccess += HandleSignInSuccess;
            AuthManagerNew.OnSignInFailed += HandleSignInFailed;
            //AuthManager.OnRegistrationSuccess += HandleRegistrationSuccess;
            AuthManagerNew.OnRegistrationFailed += HandleRegistrationFailed;
        }

        private void OnDestroy()
        {
            // Unsubscribe from events to prevent memory leaks
            AuthManagerNew.OnSignInSuccess -= HandleSignInSuccess;
            AuthManagerNew.OnSignInFailed -= HandleSignInFailed;
            //AuthManager.OnRegistrationSuccess -= HandleRegistrationSuccess;
            AuthManagerNew.OnRegistrationFailed -= HandleRegistrationFailed;
        }

        public async void CheckInternetConnection()
        {
            if (Application.internetReachability == NetworkReachability.NotReachable)
            {
                ToggleAuthenticationSubPanel(PANEL_NO_CONNECTION);
                return;
            }

            // Check authentication status
            if (!AuthManagerNew.IsAuthenticated())
            {
                await m_AuthenticationManager.Authenticate();
            }

            // Show sign-in panel by default
            ToggleAuthenticationSubPanel(PANEL_SIGN_IN);
        }

        public void ToggleAuthenticationSubPanel(int panelId)
        {
            // Deactivate all panels
            for (int i = 0; i < m_AuthenticationSubPanels.Length; i++)
            {
                m_AuthenticationSubPanels[i].SetActive(i == panelId);
            }
        }

        public void AttemptSignIn()
        {
            string username = m_UsernameInputField.text;
            string password = m_PasswordInputField.text;

            // Clear previous warnings
            ClearSignInWarnings();

            // Validate inputs
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                ShowSignInWarning("Please enter both username and password");
                return;
            }

            // Show connection panel
            ToggleAuthenticationSubPanel(PANEL_CONNECTION);

            // Attempt sign in
            StartCoroutine(SignInCoroutine(username, password));
        }

        #region SignIn Warning Toast

        // Method to show sign in warning
        public void ShowSignInWarning(string message)
        {
            // Set the warning text
            m_ConnectionWarningText.text = $"<b>Warning:</b> {message}";

            // Activate the toast within the current panel
            m_ConnectionWarningToast.SetActive(true);

            // Optional: Auto-hide the toast after a few seconds
            StartCoroutine(HideSignInWarningAfterDelay());
        }

        private IEnumerator HideSignInWarningAfterDelay()
        {
            yield return new WaitForSeconds(3f); // Toast will show for 3 seconds
            HideSignInWarning();
        }

        public void HideSignInWarning()
        {
            m_ConnectionWarningToast.SetActive(false);
            m_ConnectionWarningText.text = string.Empty;
        }

        // Method to clear previous warnings
        private void ClearSignInWarnings()
        {
            HideSignInWarning();
        }
        #endregion

        private IEnumerator SignInCoroutine(string username, string password)
        {
            var signInTask = m_AuthenticationManager.SignInWithUsernamePassword(username, password);

            yield return new WaitUntil(() => signInTask.IsCompleted);

            try
            {
                bool success = signInTask.Result;
                if (success)
                {
                    m_ConnectionSuccessText.text = $"Signed in as {username}";
                    ToggleAuthenticationSubPanel(PANEL_CONNECTION_SUCCESS);
                }
                else
                {
                    ShowConnectionFailure("Sign in failed");
                }
            }
            catch (Exception ex)
            {
                ShowConnectionFailure(ex.Message);
            }
        }

        public void AttemptRegistration()
        {
            string username = m_NewUsernameInputField.text;
            string password = m_NewPasswordInputField.text;
            string confirmPassword = m_ConfirmPasswordInputField.text;

            // Clear previous warnings
            ClearRegistrationWarnings();

            // Validate username
            #region Username Validation

            if (string.IsNullOrEmpty(username))
            {
                ShowRegistrationWarning("Username cannot be empty");
                return;
            }

            if (username.Length < 3 || username.Length > 20)
            {
                ShowRegistrationWarning("Username must be between 3 and 20 characters");
                return;
            }
            #endregion

            // Validate password
            #region Password Validation

            if (string.IsNullOrEmpty(password))
            {
                ShowRegistrationWarning("Password cannot be empty");
                return;
            }

            if (password.Length < 8)
            {
                ShowRegistrationWarning("Password must be at least 8 characters long");
                return;
            }

            // Advanced password validation
            if (!ContainsUppercase(password))
            {
                ShowRegistrationWarning("Password must contain at least one uppercase letter");
                return;
            }

            if (!ContainsLowercase(password))
            {
                ShowRegistrationWarning("Password must contain at least one lowercase letter");
                return;
            }

            if (!ContainsNumber(password))
            {
                ShowRegistrationWarning("Password must contain at least one number");
                return;
            }

            if (!ContainsSpecialCharacter(password))
            {
                ShowRegistrationWarning("Password must contain at least one special character");
                return;
            }

            // Confirm password
            if (password != confirmPassword)
            {
                ShowRegistrationWarning("Passwords do not match");
                return;
            }
            #endregion

            // Show connection panel if all validations pass
            ToggleAuthenticationSubPanel(PANEL_CONNECTION);

            // Attempt registration
            StartCoroutine(RegistrationCoroutine(username, password));
        }

        // Helper methods for password validation
        #region Password Validation Method

        private bool ContainsUppercase(string password)
        {
            return password.Any(char.IsUpper);
        }

        private bool ContainsLowercase(string password)
        {
            return password.Any(char.IsLower);
        }

        private bool ContainsNumber(string password)
        {
            return password.Any(char.IsDigit);
        }

        private bool ContainsSpecialCharacter(string password)
        {
            return password.Any(ch => !char.IsLetterOrDigit(ch));
        }
        #endregion

        #region Registration Warning Toast
        // Method to show registration warning
        public void ShowRegistrationWarning(string message)
        {
            // Set the warning text
            m_RegistrationWarningText.text = $"<b>Warning:</b> {message}";

            // Activate the toast within the current panel
            m_RegistrationWarningToast.SetActive(true);

            // Optional: Auto-hide the toast after a few seconds
            StartCoroutine(HideRegistrationWarningAfterDelay());
        }

        private IEnumerator HideRegistrationWarningAfterDelay()
        {
            yield return new WaitForSeconds(3f); // Toast will show for 3 seconds
            HideRegistrationWarning();
        }

        public void HideRegistrationWarning()
        {
            m_RegistrationWarningToast.SetActive(false);
            m_RegistrationWarningText.text = string.Empty;
        }

        // Method to clear previous warnings
        private void ClearRegistrationWarnings()
        {
            HideRegistrationWarning();
        }
        #endregion

        private IEnumerator RegistrationCoroutine(string username, string password)
        {
            var registrationTask = m_AuthenticationManager.RegisterUser(username, password);

            yield return new WaitUntil(() => registrationTask.IsCompleted);

            try
            {
                bool success = registrationTask.Result;
                if (success)
                {
                    m_RegistrationSuccessText.text = $"<b>Successfully Registered:</b> {username}";
                    ToggleAuthenticationSubPanel(PANEL_REGISTRATION_SUCCESS);
                }
                // Do not add an else block here. If success is false, HandleRegistrationFailed was already invoked by AuthManagerNew, which sets the exact error message!
            }
            catch (Exception ex)
            {
                ShowConnectionFailure(ex.Message);
            }
        }

        public void HandleSignInSuccess()
        {
            // This method will be called when sign-in is successful via event
            m_ConnectionSuccessText.text = "Sign In Successful";
            ToggleAuthenticationSubPanel(PANEL_CONNECTION_SUCCESS);
        }

        private void HandleSignInFailed(string reason)
        {
            ShowConnectionFailure(reason);
        }

        private void HandleRegistrationFailed(string reason)
        {
            ShowConnectionFailure(reason);
        }

        public void ShowConnectionFailure(string reason)
        {
            ToggleAuthenticationSubPanel(PANEL_CONNECTION_FAILED);
            m_ConnectionFailedText.text = $"<b>Error:</b> {reason}";
        }

        // Optional: Methods to set username or clear fields
        public void SetUsername(string username)
        {
            if (!string.IsNullOrEmpty(username))
            {
                m_UsernameInputField.text = username;
            }
        }

        public void ClearFields()
        {
            m_UsernameInputField.text = string.Empty;
            m_PasswordInputField.text = string.Empty;
            m_NewUsernameInputField.text = string.Empty;
            m_NewPasswordInputField.text = string.Empty;
            m_ConfirmPasswordInputField.text = string.Empty;
        }
    }
}