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
        public TextMeshProUGUI errorText;
        public GameObject      loadingIndicator;

        [Header("Scenes")]
        public string mainMenuScene = "MainMenu";

        private bool _isMuted = false;

        private void Start()
        {
            // Choice panel
            createLobbyButton.onClick.AddListener(OnCreateLobbyClicked);
            joinLobbyButton.onClick.AddListener(() => ShowPanel("guestJoin"));
            backToMenuButton.onClick.AddListener(() => SceneManager.LoadScene(mainMenuScene));

            // Host panel
            endLobbyButton.onClick.AddListener(OnEndLobbyClicked);

            // Guest join panel
            confirmJoinButton.onClick.AddListener(OnConfirmJoinClicked);
            cancelJoinButton.onClick.AddListener(() => ShowPanel("choice"));

            // In-lobby HUD
            muteButton.onClick.AddListener(OnMuteClicked);
            leaveLobbyButton.onClick.AddListener(OnLeaveLobbyClicked);

            // Subscribe to LobbyController_V2 events
            LobbyController_V2.Instance.OnLobbyCreated      += HandleLobbyCreated;
            LobbyController_V2.Instance.OnLobbyJoined       += HandleLobbyJoined;
            LobbyController_V2.Instance.OnLobbyEnded        += HandleLobbyEnded;
            LobbyController_V2.Instance.OnError             += HandleError;
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
            muteButtonText.text = _isMuted ? "Unmute" : "Mute";
            // Toggle Vivox mute — using VRMeetUp's VoiceChatManager
            var voice = FindFirstObjectByType<XRMultiplayer.VoiceChatManager>();
            if (voice != null)
                voice.ToggleSelfMute(true, _isMuted);
        }

        // ── Event Handlers ────────────────────────────────────────────────
        private void HandleLobbyCreated(DiscussionLobbyModel lobby)
        {
            SetLoading(false);
            lobbyCodeText.text = $"Lobby Code: {LobbyController_V2.Instance.UnityLobbyCode}";
            guestListText.text = "Waiting for guests...";
            ShowPanel("host");
            inLobbyHUD.SetActive(true);
            leaveLobbyButton.gameObject.SetActive(false); // host uses End Lobby instead
        }

        private void HandleLobbyJoined()
        {
            SetLoading(false);
            ShowPanel("none");
            inLobbyHUD.SetActive(true);
            leaveLobbyButton.gameObject.SetActive(true);
        }

        private void HandleLobbyEnded()
        {
            SetLoading(false);
            inLobbyHUD.SetActive(false);
            VRMeetUpIntegrationController.Instance?.DisablePassthrough();
            SceneManager.LoadScene(mainMenuScene);
        }

        private void HandleGuestSpawned(string username, GameObject avatarObj)
        {
            // Update guest list text on host panel
            guestListText.text += $"\n• {username} joined";
        }

        private void HandleGuestLeft(string username)
        {
            guestListText.text += $"\n• {username} left";
        }

        private void HandleError(string error)
        {
            SetLoading(false);
            SetError(error);
        }

        // ── Panel Manager ─────────────────────────────────────────────────
        private void ShowPanel(string panel)
        {
            choicePanel.SetActive(panel == "choice");
            hostPanel.SetActive(panel == "host");
            guestJoinPanel.SetActive(panel == "guestJoin");
            if (panel != "host" && panel != "guestJoin")
                inLobbyHUD.SetActive(false);
        }

        // ── Helpers ───────────────────────────────────────────────────────
        private void SetError(string msg)
        {
            if (errorText == null) return;
            errorText.text = msg;
            errorText.gameObject.SetActive(!string.IsNullOrEmpty(msg));
        }

        private void SetLoading(bool loading)
        {
            if (loadingIndicator != null) loadingIndicator.SetActive(loading);
            createLobbyButton.interactable  = !loading;
            confirmJoinButton.interactable  = !loading;
            endLobbyButton.interactable     = !loading;
            leaveLobbyButton.interactable   = !loading;
        }
    }
}
