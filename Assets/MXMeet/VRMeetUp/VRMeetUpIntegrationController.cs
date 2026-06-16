using System;
using System.Collections;
using MXMeet.Auth;
using Unity.Netcode;
using UnityEngine;
using XRMultiplayer; // VRMeetUp's namespace

namespace MXMeet.VRMeetUp
{
    /// <summary>
    /// MXMeet's integration controller for VRMeetUp.
    ///
    /// IMPORTANT: VRMeetUp uses XRINetworkGameManager (singleton) for ALL
    /// lobby and relay management. MXMeet must NOT create its own lobby/relay —
    /// it must call XRINetworkGameManager to join/create lobbies just like
    /// a normal VRMeetUp client would.
    ///
    /// This controller handles:
    ///   1. Setting the MXMeet player name/colour in VRMeetUp before connecting
    ///   2. Connecting to VRMeetUp via XRINetworkGameManager.QuickJoinLobby()
    ///      or JoinLobbyByCode()
    ///   3. Enabling Meta XR Passthrough via OVRPassthroughLayer (requires META_XR_SDK)
    ///   4. Placing a Spatial Anchor to fix VRMeetUp in the real world (requires META_XR_SDK)
    ///   5. Disconnecting cleanly on exit
    ///
    /// NOTE: To enable Meta XR features, install the Meta XR SDK and add
    /// META_XR_SDK to Player Settings > Scripting Define Symbols.
    /// </summary>
    public class VRMeetUpIntegrationController : MonoBehaviour
    {
        public static VRMeetUpIntegrationController Instance { get; private set; }

        // ── Inspector ─────────────────────────────────────────────────────
#if META_XR_SDK
        [Header("Meta XR References")]
        [Tooltip("OVRPassthroughLayer on the OVRCameraRig")]
        public OVRPassthroughLayer passthroughLayer;
#endif

        [Tooltip("Prefab with OVRSpatialAnchor component")]
        public GameObject spatialAnchorPrefab;

        [Tooltip("Root GameObject that holds the VRMeetUp world content. Will be repositioned to the anchor.")]
        public GameObject vrMeetUpWorldRoot;

        [Header("VRMeetUp References")]
        [Tooltip("XRINetworkGameManager in the VRMeetUp scene. Assign in Inspector or will be found automatically.")]
        public XRINetworkGameManager networkGameManager;

        // ── Events ────────────────────────────────────────────────────────
        public event Action          OnPassthroughEnabled;
        public event Action          OnAnchorPlaced;
        public event Action          OnConnectedToVRMeetUp;
        public event Action          OnDisconnectedFromVRMeetUp;
        public event Action<string>  OnError;

        // ── State ─────────────────────────────────────────────────────────
        public bool IsConnected        { get; private set; }
        public bool IsPassthroughActive { get; private set; }

#if META_XR_SDK
        private OVRSpatialAnchor _anchor;
#else
        private GameObject _anchorGO;
#endif

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            // Auto-find XRINetworkGameManager if not assigned
            if (networkGameManager == null)
                networkGameManager = FindFirstObjectByType<XRINetworkGameManager>();

            if (networkGameManager == null)
                Debug.LogWarning("[VRMeetUpIntegration] XRINetworkGameManager not found. Assign it in Inspector.");

            // Subscribe to VRMeetUp connection events
            XRINetworkGameManager.Connected.Subscribe(OnVRMeetUpConnectionChanged);
        }

        private void OnDestroy()
        {
            XRINetworkGameManager.Connected.Unsubscribe(OnVRMeetUpConnectionChanged);
        }

        // ═══════════════════════════════════════════════════════════════
        // 1. Enable Passthrough
        // ═══════════════════════════════════════════════════════════════
        /// <summary>Enables Meta XR Passthrough so the MR user sees their real environment.</summary>
        public void EnablePassthrough()
        {
#if META_XR_SDK
            if (passthroughLayer == null) { OnError?.Invoke("Passthrough layer not assigned in Inspector."); return; }

            passthroughLayer.enabled = true;
            passthroughLayer.gameObject.SetActive(true);

            // Make camera background fully transparent so passthrough shows through
            if (Camera.main != null)
            {
                Camera.main.clearFlags       = CameraClearFlags.SolidColor;
                Camera.main.backgroundColor  = Color.clear;
            }

            IsPassthroughActive = true;
            Debug.Log("[VRMeetUpIntegration] Passthrough enabled.");
            OnPassthroughEnabled?.Invoke();
#else
            Debug.LogWarning("[VRMeetUpIntegration] Meta XR SDK not installed. Add META_XR_SDK to Scripting Define Symbols to enable passthrough.");
            OnError?.Invoke("Meta XR SDK not installed. Passthrough unavailable.");
#endif
        }

        /// <summary>Disables passthrough and restores camera skybox.</summary>
        public void DisablePassthrough()
        {
#if META_XR_SDK
            if (passthroughLayer != null) passthroughLayer.enabled = false;
#endif
            if (Camera.main != null)
            {
                Camera.main.clearFlags      = CameraClearFlags.Skybox;
                Camera.main.backgroundColor = Color.black;
            }
            IsPassthroughActive = false;
        }

        // ═══════════════════════════════════════════════════════════════
        // 2. Place Spatial Anchor
        // ═══════════════════════════════════════════════════════════════
        /// <summary>
        /// Places a Spatial Anchor 1.5m in front of the user and
        /// repositions vrMeetUpWorldRoot to that anchor position.
        /// </summary>
        public void PlaceSpatialAnchor()
        {
            if (spatialAnchorPrefab == null) { OnError?.Invoke("Spatial anchor prefab not assigned."); return; }

            Transform cam = Camera.main?.transform;
            if (cam == null) { OnError?.Invoke("No main camera found."); return; }

            // Place 1.5m in front, slightly below eye level
            Vector3 pos = cam.position + cam.forward * 1.5f;
            pos.y = cam.position.y - 0.3f;

#if META_XR_SDK
            // Remove existing anchor
            if (_anchor != null) Destroy(_anchor.gameObject);

            GameObject anchorGO = Instantiate(spatialAnchorPrefab, pos, Quaternion.identity);
            _anchor = anchorGO.GetComponent<OVRSpatialAnchor>();
#else
            // Remove existing anchor placeholder
            if (_anchorGO != null) Destroy(_anchorGO);

            _anchorGO = Instantiate(spatialAnchorPrefab, pos, Quaternion.identity);
            Debug.LogWarning("[VRMeetUpIntegration] Meta XR SDK not installed — spatial anchor placed without OVR tracking.");
#endif

            // Move VRMeetUp world to anchor position
            if (vrMeetUpWorldRoot != null)
            {
                vrMeetUpWorldRoot.transform.position = pos;
                vrMeetUpWorldRoot.SetActive(true);
            }

            StartCoroutine(WaitForAnchor());
        }

        private IEnumerator WaitForAnchor()
        {
#if META_XR_SDK
            float timeout = 5f, elapsed = 0f;
            while (_anchor != null && !_anchor.Created && elapsed < timeout)
            { elapsed += Time.deltaTime; yield return null; }

            Debug.Log(_anchor != null && _anchor.Created
                ? "[VRMeetUpIntegration] Anchor placed successfully."
                : "[VRMeetUpIntegration] Anchor timed out — proceeding anyway.");
#else
            yield return null;
            Debug.Log("[VRMeetUpIntegration] Anchor placed (no OVR tracking).");
#endif

            OnAnchorPlaced?.Invoke();
        }

        // ═══════════════════════════════════════════════════════════════
        // 3. Connect to VRMeetUp (via XRINetworkGameManager)
        // ═══════════════════════════════════════════════════════════════
        /// <summary>
        /// Sets the MXMeet user's name and colour in VRMeetUp's static variables
        /// then Quick-Joins a VRMeetUp lobby.
        /// This is exactly how a normal VRMeetUp player joins.
        /// </summary>
        public void ConnectToVRMeetUp()
        {
            if (networkGameManager == null) { OnError?.Invoke("XRINetworkGameManager not found."); return; }

            // Set player identity in VRMeetUp BEFORE connecting
            string username = MXMeet.Auth.AuthController.Instance?.CurrentUser?.username ?? "MXMeet User";
            Color  color    = MXMeet.Auth.AuthController.Instance?.CurrentAvatar != null &&
                              ColorUtility.TryParseHtmlString(
                                  MXMeet.Auth.AuthController.Instance.CurrentAvatar.colorCode, out Color c) ? c : Color.white;

            XRINetworkGameManager.LocalPlayerName.Value  = username;
            XRINetworkGameManager.LocalPlayerColor.Value = color;

            // Quick Join — finds an existing room or creates one
            networkGameManager.QuickJoinLobby();
            Debug.Log("[VRMeetUpIntegration] QuickJoin called on XRINetworkGameManager.");
        }

        /// <summary>
        /// Joins a specific VRMeetUp room by code.
        /// The room code is the VRMeetUp lobby code, not an MXMeet code.
        /// </summary>
        public void ConnectToVRMeetUpByCode(string roomCode)
        {
            if (networkGameManager == null) { OnError?.Invoke("XRINetworkGameManager not found."); return; }

            string username = MXMeet.Auth.AuthController.Instance?.CurrentUser?.username ?? "MXMeet User";
            Color  color    = Color.white;
            if (MXMeet.Auth.AuthController.Instance?.CurrentAvatar != null)
                ColorUtility.TryParseHtmlString(MXMeet.Auth.AuthController.Instance.CurrentAvatar.colorCode, out color);

            XRINetworkGameManager.LocalPlayerName.Value  = username;
            XRINetworkGameManager.LocalPlayerColor.Value = color;

            networkGameManager.JoinLobbyByCode(roomCode);
            Debug.Log($"[VRMeetUpIntegration] JoinLobbyByCode({roomCode}) called.");
        }

        // ═══════════════════════════════════════════════════════════════
        // 4. Disconnect
        // ═══════════════════════════════════════════════════════════════
        /// <summary>
        /// Disconnects from VRMeetUp using XRINetworkGameManager.Disconnect()
        /// and cleans up passthrough and anchor.
        /// </summary>
        public void ExitVRMeetUp()
        {
            networkGameManager?.Disconnect();
            DisablePassthrough();

            if (vrMeetUpWorldRoot != null) vrMeetUpWorldRoot.SetActive(false);

#if META_XR_SDK
            if (_anchor != null) { Destroy(_anchor.gameObject); _anchor = null; }
#else
            if (_anchorGO != null) { Destroy(_anchorGO); _anchorGO = null; }
#endif

            IsConnected = false;
            Debug.Log("[VRMeetUpIntegration] Exited VRMeetUp.");
        }

        // ═══════════════════════════════════════════════════════════════
        // 5. VRMeetUp Connection Callback
        // ═══════════════════════════════════════════════════════════════
        private void OnVRMeetUpConnectionChanged(bool connected)
        {
            IsConnected = connected;
            if (connected)
            {
                Debug.Log("[VRMeetUpIntegration] Connected to VRMeetUp.");
                OnConnectedToVRMeetUp?.Invoke();
            }
            else
            {
                Debug.Log("[VRMeetUpIntegration] Disconnected from VRMeetUp.");
                OnDisconnectedFromVRMeetUp?.Invoke();
            }
        }

        // ═══════════════════════════════════════════════════════════════
        // 6. Utilities
        // ═══════════════════════════════════════════════════════════════
        /// <summary>Returns current VRMeetUp room code if connected.</summary>
        public string GetCurrentRoomCode() => XRINetworkGameManager.ConnectedRoomCode;

        /// <summary>Returns current VRMeetUp room name if connected.</summary>
        public string GetCurrentRoomName() => XRINetworkGameManager.ConnectedRoomName.Value;

        /// <summary>Returns true if VRMeetUp authentication is complete.</summary>
        public bool IsAuthenticated() => networkGameManager?.IsAuthenticated() ?? false;
    }
}
