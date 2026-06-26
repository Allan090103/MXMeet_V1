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

        [Tooltip("Spawns the original VRMeetUp UI/world prefabs into the MR anchor area.")]
        public VRMeetUpAppPresenter appPresenter;

        [Header("Editor Preview")]
        [Tooltip("Shows a simulated MR room in Editor/Standalone when Quest passthrough is unavailable.")]
        public bool useMRPreviewInEditor = true;

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

        private MRPreviewEnvironment _previewEnvironment;
        private GameObject _previewAnchorGO;
        private XRINetworkGameManager _callbackSource;
        private Vector3 _lastAnchorPosition;
        private Quaternion _lastAnchorRotation = Quaternion.identity;
        private bool _hasAnchor;
        private Coroutine _showAppSurfaceRoutine;

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
            // Subscribe to VRMeetUp connection events
            XRINetworkGameManager.Connected.Subscribe(OnVRMeetUpConnectionChanged);
        }

        private void OnDestroy()
        {
            XRINetworkGameManager.Connected.Unsubscribe(OnVRMeetUpConnectionChanged);
            UnhookNetworkManagerCallbacks();
        }

        // ═══════════════════════════════════════════════════════════════
        // 1. Enable Passthrough
        // ═══════════════════════════════════════════════════════════════
        /// <summary>Enables Meta XR Passthrough so the MR user sees their real environment.</summary>
        public void EnablePassthrough()
        {
            if (ShouldUseMRPreview())
            {
                EnsurePreviewEnvironment().Show();
                IsPassthroughActive = true;
                Debug.Log("[VRMeetUpIntegration] MR preview environment enabled.");
                OnPassthroughEnabled?.Invoke();
                return;
            }

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
            OnError?.Invoke("Passthrough is only available on Quest. Editor preview is disabled.");
#endif
        }

        /// <summary>Disables passthrough and restores camera skybox.</summary>
        public void DisablePassthrough()
        {
            if (_previewEnvironment != null)
                _previewEnvironment.Hide();

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
            Transform cam = Camera.main?.transform;
            if (cam == null) { OnError?.Invoke("No main camera found."); return; }

            // Place 1.5m in front, slightly below eye level
            Vector3 pos = cam.position + cam.forward * 1.5f;
            pos.y = cam.position.y - 0.3f;

            if (ShouldUseMRPreview())
            {
                _previewAnchorGO = EnsurePreviewEnvironment().PlaceAnchor(pos);
                _lastAnchorPosition = pos;
                _lastAnchorRotation = Quaternion.LookRotation(Vector3.back, Vector3.up);
                _hasAnchor = true;

                if (vrMeetUpWorldRoot != null)
                {
                    vrMeetUpWorldRoot.transform.position = pos;
                    vrMeetUpWorldRoot.SetActive(true);
                }

                Debug.Log("[VRMeetUpIntegration] Preview anchor placed.");
                OnAnchorPlaced?.Invoke();
                return;
            }

            if (spatialAnchorPrefab == null) { OnError?.Invoke("Spatial anchor prefab not assigned."); return; }

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

            _lastAnchorPosition = pos;
            _lastAnchorRotation = Quaternion.LookRotation(-cam.forward, Vector3.up);
            _hasAnchor = true;

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
        public async void ConnectToVRMeetUp()
        {
            if (!TryResolveNetworkGameManager()) return;
            if (!await EnsureVRMeetUpAuthenticated()) return;

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
        public async void ConnectToVRMeetUpByCode(string roomCode)
        {
            if (!TryResolveNetworkGameManager()) return;
            if (!await EnsureVRMeetUpAuthenticated()) return;

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
            if (appPresenter != null) appPresenter.Hide();

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
                if (_showAppSurfaceRoutine == null)
                    _showAppSurfaceRoutine = StartCoroutine(ShowVRMeetUpAppSurfaceWhenReady());
            }
            else
            {
                Debug.Log("[VRMeetUpIntegration] Disconnected from VRMeetUp.");
                if (_showAppSurfaceRoutine != null)
                {
                    StopCoroutine(_showAppSurfaceRoutine);
                    _showAppSurfaceRoutine = null;
                }

                if (appPresenter != null) appPresenter.Hide();
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

        private bool TryResolveNetworkGameManager()
        {
            if (networkGameManager != null) return true;

            networkGameManager = FindFirstObjectByType<XRINetworkGameManager>();
            if (networkGameManager != null)
            {
                HookNetworkManagerCallbacks();
                return true;
            }

#if UNITY_EDITOR
            MXMeet.EditorBootstrap.EnsureVRMeetUpManagersForEditorTesting();
            networkGameManager = FindFirstObjectByType<XRINetworkGameManager>();
            if (networkGameManager != null)
            {
                HookNetworkManagerCallbacks();
                return true;
            }
#endif

            string message = "VRMeetUp network manager is not loaded. Start from Bootstrap or open a scene that contains XRI_Network_Game_Manager.";
            Debug.LogWarning($"[VRMeetUpIntegration] {message}");
            OnError?.Invoke(message);
            return false;
        }

        private void ShowVRMeetUpAppSurface()
        {
            EnsureAppPresenter();

            Vector3 position = _hasAnchor
                ? _lastAnchorPosition
                : (Camera.main != null ? Camera.main.transform.position + Camera.main.transform.forward * 1.5f : Vector3.zero);

            Quaternion rotation = _hasAnchor ? _lastAnchorRotation : Quaternion.identity;
            appPresenter.Show(position, rotation);
        }

        private IEnumerator ShowVRMeetUpAppSurfaceWhenReady()
        {
            while (NetworkManager.Singleton == null ||
                   !NetworkManager.Singleton.IsListening ||
                   XRINetworkGameManager.CurrentConnectionState.Value != XRINetworkGameManager.ConnectionState.Connected)
            {
                yield return null;
            }

            // Let VRMeetUp finish its host/client startup frame before adding app UI prefabs.
            yield return null;
            ShowVRMeetUpAppSurface();
            _showAppSurfaceRoutine = null;
            OnConnectedToVRMeetUp?.Invoke();
        }

        private void EnsureAppPresenter()
        {
            if (appPresenter != null) return;

            appPresenter = FindFirstObjectByType<VRMeetUpAppPresenter>();
            if (appPresenter != null) return;

            GameObject presenterObject = new GameObject("VRMeetUp App Presenter");
            DontDestroyOnLoad(presenterObject);
            appPresenter = presenterObject.AddComponent<VRMeetUpAppPresenter>();
        }

        private void HookNetworkManagerCallbacks()
        {
            if (networkGameManager == null || _callbackSource == networkGameManager) return;

            UnhookNetworkManagerCallbacks();
            networkGameManager.connectionFailedAction += HandleVRMeetUpConnectionFailed;
            networkGameManager.connectionUpdated += HandleVRMeetUpConnectionUpdated;
            _callbackSource = networkGameManager;
        }

        private void UnhookNetworkManagerCallbacks()
        {
            if (_callbackSource == null) return;

            _callbackSource.connectionFailedAction -= HandleVRMeetUpConnectionFailed;
            _callbackSource.connectionUpdated -= HandleVRMeetUpConnectionUpdated;
            _callbackSource = null;
        }

        private void HandleVRMeetUpConnectionFailed(string reason)
        {
            OnError?.Invoke($"VRMeetUp connection failed: {reason}");
        }

        private void HandleVRMeetUpConnectionUpdated(string update)
        {
            Debug.Log($"[VRMeetUpIntegration] {update}");
        }

        private async System.Threading.Tasks.Task<bool> EnsureVRMeetUpAuthenticated()
        {
#if UNITY_EDITOR
            MXMeet.EditorBootstrap.EnsureVRMeetUpManagersForEditorTesting();
#endif

            if (networkGameManager.IsAuthenticated()) return true;

            try
            {
                Debug.Log("[VRMeetUpIntegration] Authenticating Unity Gaming Services for VRMeetUp...");
                bool authenticated = await networkGameManager.Authenticate();
                if (authenticated) return true;

                string message = "VRMeetUp authentication failed. Check Unity Gaming Services project settings.";
                Debug.LogWarning($"[VRMeetUpIntegration] {message}");
                OnError?.Invoke(message);
                return false;
            }
            catch (Exception e)
            {
                string message = $"VRMeetUp authentication failed: {e.Message}";
                Debug.LogWarning($"[VRMeetUpIntegration] {message}");
                OnError?.Invoke(message);
                return false;
            }
        }

        private bool ShouldUseMRPreview()
        {
#if UNITY_EDITOR || UNITY_STANDALONE
            return useMRPreviewInEditor;
#else
            return false;
#endif
        }

        private MRPreviewEnvironment EnsurePreviewEnvironment()
        {
            if (_previewEnvironment != null) return _previewEnvironment;

            GameObject previewObject = new GameObject("MXMeet MR Preview");
            DontDestroyOnLoad(previewObject);
            _previewEnvironment = previewObject.AddComponent<MRPreviewEnvironment>();
            return _previewEnvironment;
        }
    }
}
