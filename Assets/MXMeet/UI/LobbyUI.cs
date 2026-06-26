using MXMeet.Lobby;
using MXMeet.Models;
using MXMeet.VRMeetUp;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace MXMeet.UI
{
    /// <summary>
    /// Controls the Discussion Lobby screen (UC06, UC07, UC08).
    ///
    /// Has three panels:
    ///   1. ChoicePanel   — Host creates lobby OR Guest joins with code
    ///   2. HostPanel     — Shows lobby code, guest list, End Lobby button
    ///   3. GuestPanel    — Join by code input, then in-lobby view with Leave button
    ///   4. InLobbyHUD    — Active once in lobby: Mute + Leave/End
    /// </summary>
    public class LobbyUI : MonoBehaviour
    {
        [Header("Choice Panel")]
        public GameObject      choicePanel;
        public Button          createLobbyButton;
        public Button          joinLobbyButton;
        public Button          backToMenuButton;

        [Header("Host Panel")]
        public GameObject      hostPanel;
        public TextMeshProUGUI lobbyCodeText;
        public TextMeshProUGUI guestListText;
        public Button          endLobbyButton;

        [Header("Guest Join Panel")]
        public GameObject      guestJoinPanel;
        public TMP_InputField  lobbyCodeInput;
        public Button          confirmJoinButton;
        public Button          cancelJoinButton;

        [Header("In-Lobby HUD (shown when active)")]
        public GameObject      inLobbyHUD;
        public Button          muteButton;
        public Button          leaveLobbyButton;
        public TextMeshProUGUI muteButtonText;

        [Header("Feedback")]
        public TextMeshProUGUI statusText;
        public TextMeshProUGUI errorText;
        public GameObject      loadingIndicator;

        [Header("Scenes")]
        public string mainMenuScene = "MainMenu";

        private bool _isMuted = false;

        private void Start()
        {
            if (LobbyController_V2.Instance == null)
            {
                SetError("Lobby system is not ready. Open the app from Bootstrap or Main Menu.");
                SetButtonsInteractable(false);
                return;
            }

            // Choice panel
            if (createLobbyButton != null) createLobbyButton.onClick.AddListener(OnCreateLobbyClicked);
            if (joinLobbyButton != null) joinLobbyButton.onClick.AddListener(() => ShowPanel("guestJoin"));
            if (backToMenuButton != null) backToMenuButton.onClick.AddListener(() => SceneManager.LoadScene(mainMenuScene));

            // Host panel
            if (endLobbyButton != null) endLobbyButton.onClick.AddListener(OnEndLobbyClicked);

            // Guest join panel
            if (confirmJoinButton != null) confirmJoinButton.onClick.AddListener(OnConfirmJoinClicked);
            if (cancelJoinButton != null) cancelJoinButton.onClick.AddListener(() => ShowPanel("choice"));

            // In-lobby HUD
            if (muteButton != null) muteButton.onClick.AddListener(OnMuteClicked);
            if (leaveLobbyButton != null) leaveLobbyButton.onClick.AddListener(OnLeaveLobbyClicked);

            // Subscribe to LobbyController_V2 events
            LobbyController_V2.Instance.OnLobbyCreated      += HandleLobbyCreated;
            LobbyController_V2.Instance.OnLobbyJoined       += HandleLobbyJoined;
            LobbyController_V2.Instance.OnLobbyEnded        += HandleLobbyEnded;
            LobbyController_V2.Instance.OnError             += HandleError;
            LobbyController_V2.Instance.OnStatusUpdate      += HandleStatusUpdate;
            LobbyController_V2.Instance.OnGuestAvatarSpawned += HandleGuestSpawned;
            LobbyController_V2.Instance.OnGuestLeft         += HandleGuestLeft;

            ShowPanel("choice");
            SetError("");

            // Enable passthrough so host sees guest avatars in the real environment
            VRMeetUpIntegrationController.Instance?.EnablePassthrough();
        }

        private void OnDestroy()
        {
            if (LobbyController_V2.Instance == null) return;
            LobbyController_V2.Instance.OnLobbyCreated       -= HandleLobbyCreated;
            LobbyController_V2.Instance.OnLobbyJoined        -= HandleLobbyJoined;
            LobbyController_V2.Instance.OnLobbyEnded         -= HandleLobbyEnded;
            LobbyController_V2.Instance.OnError              -= HandleError;
            LobbyController_V2.Instance.OnStatusUpdate       -= HandleStatusUpdate;
            LobbyController_V2.Instance.OnGuestAvatarSpawned -= HandleGuestSpawned;
            LobbyController_V2.Instance.OnGuestLeft          -= HandleGuestLeft;
        }

        // ── Button Handlers ───────────────────────────────────────────────
        private void OnCreateLobbyClicked()
        {
            SetLoading(true);
            SetError("");
            LobbyController_V2.Instance.CreateLobby();
        }

        private void OnConfirmJoinClicked()
        {
            if (lobbyCodeInput == null) { SetError("Lobby code input is missing."); return; }
            string code = lobbyCodeInput.text.Trim().ToUpper();
            if (string.IsNullOrEmpty(code)) { SetError("Please enter a lobby code."); return; }
            SetLoading(true);
            SetError("");
            LobbyController_V2.Instance.JoinLobby(code);
        }

        private void OnEndLobbyClicked()
        {
            SetLoading(true);
            LobbyController_V2.Instance.EndLobby();
        }

        private void OnLeaveLobbyClicked()
        {
            SetLoading(true);
            LobbyController_V2.Instance.LeaveLobby();
        }

        private void OnMuteClicked()
        {
            _isMuted = !_isMuted;
            if (muteButtonText != null) muteButtonText.text = _isMuted ? "Unmute" : "Mute";
            LobbyController_V2.Instance.SetSelfMuted(_isMuted);
        }

        // ── Event Handlers ────────────────────────────────────────────────
        private void HandleLobbyCreated(DiscussionLobbyModel lobby)
        {
            SetLoading(false);
            if (lobbyCodeText != null) lobbyCodeText.text = $"Lobby Code: {LobbyController_V2.Instance.UnityLobbyCode}";
            if (guestListText != null) guestListText.text = "Waiting for guests...";
            ShowPanel("host");
            if (inLobbyHUD != null) inLobbyHUD.SetActive(true);
            if (leaveLobbyButton != null) leaveLobbyButton.gameObject.SetActive(false); // host uses End Lobby instead
        }

        private void HandleLobbyJoined()
        {
            SetLoading(false);
            ShowPanel("none");
            if (inLobbyHUD != null) inLobbyHUD.SetActive(true);
            if (leaveLobbyButton != null) leaveLobbyButton.gameObject.SetActive(true);
        }

        private void HandleLobbyEnded()
        {
            SetLoading(false);
            if (inLobbyHUD != null) inLobbyHUD.SetActive(false);
            VRMeetUpIntegrationController.Instance?.DisablePassthrough();
            SceneManager.LoadScene(mainMenuScene);
        }

        private void HandleGuestSpawned(string username, GameObject avatarObj)
        {
            if (guestListText == null) return;
            guestListText.text += $"\n- {username} joined";
        }

        private void HandleGuestLeft(string username)
        {
            if (guestListText == null) return;
            guestListText.text += $"\n- {username} left";
        }

        private void HandleError(string error)
        {
            SetLoading(false);
            SetError(error);
        }

        private void HandleStatusUpdate(string status)
        {
            SetStatus(status);
        }

        // ── Panel Manager ─────────────────────────────────────────────────
        private void ShowPanel(string panel)
        {
            if (choicePanel != null) choicePanel.SetActive(panel == "choice");
            if (hostPanel != null) hostPanel.SetActive(panel == "host");
            if (guestJoinPanel != null) guestJoinPanel.SetActive(panel == "guestJoin");
            if (inLobbyHUD != null && panel != "host" && panel != "guestJoin")
                inLobbyHUD.SetActive(false);
        }

        // ── Helpers ───────────────────────────────────────────────────────
        private void SetError(string msg)
        {
            if (errorText == null) return;
            errorText.text = msg;
            errorText.gameObject.SetActive(!string.IsNullOrEmpty(msg));
        }

        private void SetStatus(string msg)
        {
            if (statusText != null) statusText.text = msg;
        }

        private void SetLoading(bool loading)
        {
            if (loadingIndicator != null) loadingIndicator.SetActive(loading);
            SetButtonsInteractable(!loading);
        }

        private void SetButtonsInteractable(bool interactable)
        {
            if (createLobbyButton != null) createLobbyButton.interactable = interactable;
            if (joinLobbyButton != null) joinLobbyButton.interactable = interactable;
            if (confirmJoinButton != null) confirmJoinButton.interactable = interactable;
            if (endLobbyButton != null) endLobbyButton.interactable = interactable;
            if (leaveLobbyButton != null) leaveLobbyButton.interactable = interactable;
            if (muteButton != null) muteButton.interactable = interactable;
        }
    }
}
