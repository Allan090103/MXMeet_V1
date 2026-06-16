using MXMeet.VRMeetUp;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using XRMultiplayer; // VRMeetUp namespace

namespace MXMeet.UI
{
    /// <summary>
    /// Updated VRMeetUp View UI that uses VRMeetUpIntegrationController
    /// which correctly hooks into VRMeetUp's XRINetworkGameManager.
    ///
    /// Flow:
    ///   OnEnable → EnablePassthrough (auto)
    ///   User taps Place Anchor → PlaceSpatialAnchor()
    ///   User taps Connect / enters room code → ConnectToVRMeetUp()
    ///   HUD shown: room info + Exit button
    /// </summary>
    public class VRMeetUpViewUI_V2 : MonoBehaviour
    {
        [Header("Setup Panel")]
        public GameObject      setupPanel;
        public Button          placeAnchorButton;
        public Button          quickJoinButton;
        public Button          joinByCodeButton;
        public TMP_InputField  roomCodeInput;
        public GameObject      roomCodePanel;
        public TextMeshProUGUI statusText;

        [Header("HUD (shown when connected)")]
        public GameObject      hudPanel;
        public TextMeshProUGUI roomNameText;
        public TextMeshProUGUI roomCodeText;
        public Button          exitButton;

        [Header("Error")]
        public TextMeshProUGUI errorText;
        public GameObject      loadingIndicator;

        [Header("Scenes")]
        public string mainMenuScene = "MainMenu";

        private void Start()
        {
            // Wire buttons
            placeAnchorButton.onClick.AddListener(OnPlaceAnchorClicked);
            quickJoinButton.onClick.AddListener(OnQuickJoinClicked);
            joinByCodeButton.onClick.AddListener(() => roomCodePanel.SetActive(true));
            exitButton.onClick.AddListener(OnExitClicked);

            // Add confirm button inside roomCodePanel
            Button confirmCode = roomCodePanel.GetComponentInChildren<Button>();
            if (confirmCode != null) confirmCode.onClick.AddListener(OnJoinByCodeClicked);

            // Subscribe to VRMeetUpIntegrationController events
            var ctrl = VRMeetUpIntegrationController.Instance;
            if (ctrl != null)
            {
                ctrl.OnPassthroughEnabled        += HandlePassthroughEnabled;
                ctrl.OnAnchorPlaced              += HandleAnchorPlaced;
                ctrl.OnConnectedToVRMeetUp       += HandleConnected;
                ctrl.OnDisconnectedFromVRMeetUp  += HandleDisconnected;
                ctrl.OnError                     += HandleError;
            }

            // Also subscribe to XRINetworkGameManager connection fail
            if (XRINetworkGameManager.Instance != null)
                XRINetworkGameManager.Instance.connectionFailedAction += HandleConnectionFailed;

            // Initial state
            setupPanel.SetActive(true);
            hudPanel.SetActive(false);
            roomCodePanel.SetActive(false);
            placeAnchorButton.interactable = false;
            quickJoinButton.interactable   = false;
            joinByCodeButton.interactable  = false;
            SetStatus("Enabling passthrough...");
            SetError("");
            SetLoading(false);

            // Auto-enable passthrough when screen opens
            VRMeetUpIntegrationController.Instance?.EnablePassthrough();
        }

        private void OnDestroy()
        {
            var ctrl = VRMeetUpIntegrationController.Instance;
            if (ctrl == null) return;
            ctrl.OnPassthroughEnabled       -= HandlePassthroughEnabled;
            ctrl.OnAnchorPlaced             -= HandleAnchorPlaced;
            ctrl.OnConnectedToVRMeetUp      -= HandleConnected;
            ctrl.OnDisconnectedFromVRMeetUp -= HandleDisconnected;
            ctrl.OnError                    -= HandleError;

            if (XRINetworkGameManager.Instance != null)
                XRINetworkGameManager.Instance.connectionFailedAction -= HandleConnectionFailed;
        }

        // ── Button Handlers ───────────────────────────────────────────────
        private void OnPlaceAnchorClicked()
        {
            placeAnchorButton.interactable = false;
            SetStatus("Placing anchor in real environment...");
            VRMeetUpIntegrationController.Instance?.PlaceSpatialAnchor();
        }

        private void OnQuickJoinClicked()
        {
            SetLoading(true);
            SetError("");
            SetStatus("Finding VRMeetUp session...");
            VRMeetUpIntegrationController.Instance?.ConnectToVRMeetUp();
        }

        private void OnJoinByCodeClicked()
        {
            string code = roomCodeInput.text.Trim().ToUpper();
            if (string.IsNullOrEmpty(code)) { SetError("Please enter a room code."); return; }
            roomCodePanel.SetActive(false);
            SetLoading(true);
            SetError("");
            SetStatus($"Joining room {code}...");
            VRMeetUpIntegrationController.Instance?.ConnectToVRMeetUpByCode(code);
        }

        private void OnExitClicked()
        {
            VRMeetUpIntegrationController.Instance?.ExitVRMeetUp();
            SceneManager.LoadScene(mainMenuScene);
        }

        // ── Event Handlers ────────────────────────────────────────────────
        private void HandlePassthroughEnabled()
        {
            SetStatus("Real environment active. Place your anchor.");
            placeAnchorButton.interactable = true;
        }

        private void HandleAnchorPlaced()
        {
            SetStatus("Anchor placed! Connect to a VRMeetUp session.");
            quickJoinButton.interactable  = true;
            joinByCodeButton.interactable = true;
        }

        private void HandleConnected()
        {
            SetLoading(false);
            setupPanel.SetActive(false);
            hudPanel.SetActive(true);
            SetError("");

            // Show room info in HUD
            var ctrl = VRMeetUpIntegrationController.Instance;
            if (ctrl != null)
            {
                if (roomNameText != null) roomNameText.text = $"Room: {ctrl.GetCurrentRoomName()}";
                if (roomCodeText != null) roomCodeText.text = $"Code: {ctrl.GetCurrentRoomCode()}";
            }
        }

        private void HandleDisconnected()
        {
            SetLoading(false);
            hudPanel.SetActive(false);
            setupPanel.SetActive(true);
            quickJoinButton.interactable  = false;
            joinByCodeButton.interactable = false;
            SetStatus("Disconnected. Place anchor to reconnect.");
        }

        private void HandleConnectionFailed(string reason)
        {
            SetLoading(false);
            SetError($"Connection failed: {reason}");
            quickJoinButton.interactable  = true;
            joinByCodeButton.interactable = true;
            SetStatus("Connection failed. Please try again.");
        }

        private void HandleError(string error)
        {
            SetLoading(false);
            SetError(error);
        }

        // ── Helpers ───────────────────────────────────────────────────────
        private void SetStatus(string msg)
        {
            if (statusText != null) statusText.text = msg;
        }

        private void SetError(string msg)
        {
            if (errorText == null) return;
            errorText.text = msg;
            errorText.gameObject.SetActive(!string.IsNullOrEmpty(msg));
        }

        private void SetLoading(bool loading)
        {
            if (loadingIndicator != null) loadingIndicator.SetActive(loading);
            quickJoinButton.interactable  = !loading && VRMeetUpIntegrationController.Instance?.IsPassthroughActive == true;
            joinByCodeButton.interactable = !loading && VRMeetUpIntegrationController.Instance?.IsPassthroughActive == true;
        }
    }
}
