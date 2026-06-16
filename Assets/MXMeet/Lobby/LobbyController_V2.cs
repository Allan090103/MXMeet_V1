using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Firebase.Firestore;
using MXMeet.Auth;
using MXMeet.Database;
using MXMeet.Models;
using Unity.Netcode;
using UnityEngine;
using XRMultiplayer; // VRMeetUp namespace

namespace MXMeet.Lobby
{
    /// <summary>
    /// Updated Discussion Lobby Controller that uses VRMeetUp's existing
    /// LobbyManager and XRINetworkGameManager instead of creating its own.
    ///
    /// KEY INSIGHT from reading VRMeetUp source:
    ///   - XRINetworkGameManager.Instance handles ALL lobby/relay/NGO
    ///   - LobbyManager handles Unity Lobby + Relay transport setup
    ///   - VoiceChatManager handles ALL Vivox (auto-connects when NGO connects)
    ///   - XRINetworkPlayer is the networked avatar (already handles name/colour sync)
    ///
    /// MXMeet Discussion Lobby ONLY needs to:
    ///   1. Call XRINetworkGameManager.Instance.CreateNewLobby() for host
    ///   2. Call XRINetworkGameManager.Instance.JoinLobbyByCode() for guest
    ///   3. Save lobby records to Firebase Firestore
    ///   4. Spawn/despawn guest avatar GameObjects in real environment
    ///   5. Call XRINetworkGameManager.Instance.Disconnect() on leave/end
    ///
    /// Vivox voice is handled AUTOMATICALLY by VoiceChatManager when NGO connects.
    /// No separate voice setup needed.
    /// </summary>
    public class LobbyController_V2 : MonoBehaviour
    {
        public static LobbyController_V2 Instance { get; private set; }

        [Header("Avatar Spawning")]
        [Tooltip("Prefab for guest avatar in real environment. Must have XRINetworkPlayer + NetworkObject.")]
        public GameObject guestAvatarPrefab;

        [Tooltip("Where to spawn guest avatars (in front of host). Leave null to auto-calculate.")]
        public Transform  spawnAnchor;

        // Events
        public event Action<DiscussionLobbyModel>    OnLobbyCreated;
        public event Action                          OnLobbyJoined;
        public event Action                          OnLobbyEnded;
        public event Action<string>                  OnError;
        public event Action<string>                  OnStatusUpdate;
        public event Action<string, GameObject>      OnGuestAvatarSpawned;
        public event Action<string>                  OnGuestLeft;

        // State
        public DiscussionLobbyModel CurrentLobby { get; private set; }
        public bool                 IsHost        { get; private set; }
        public string               LobbyCode     => XRINetworkGameManager.ConnectedRoomCode;
        public string               UnityLobbyCode => LobbyCode;
        public string               LobbyName     => XRINetworkGameManager.ConnectedRoomName.Value;

        private FirebaseFirestore _db;
        private string            _memberID;
        private string            _logID;
        private string            _joinTimeISO;

        private readonly Dictionary<ulong, GameObject> _spawnedAvatars = new();

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            // Firebase
            if (FirebaseManager.Instance.IsReady) _db = FirebaseManager.Instance.DB;
            else FirebaseManager.Instance.OnFirebaseReady += () => _db = FirebaseManager.Instance.DB;

            // Subscribe to VRMeetUp connection events
            XRINetworkGameManager.Connected.Subscribe(OnNGOConnectionChanged);
            if (XRINetworkGameManager.Instance != null)
            {
                XRINetworkGameManager.Instance.playerStateChanged     += OnPlayerStateChanged;
                XRINetworkGameManager.Instance.connectionFailedAction += OnConnectionFailed;
            }
            else
            {
                Debug.LogWarning("[LobbyController_V2] XRINetworkGameManager not ready at Start — will hook on first connection.");
            }
        }

        private void OnDestroy()
        {
            XRINetworkGameManager.Connected.Unsubscribe(OnNGOConnectionChanged);
            if (XRINetworkGameManager.Instance != null)
            {
                XRINetworkGameManager.Instance.playerStateChanged    -= OnPlayerStateChanged;
                XRINetworkGameManager.Instance.connectionFailedAction -= OnConnectionFailed;
            }
        }

        // ═══════════════════════════════════════════════════════════════
        // UC06: Create Discussion Lobby (Host)
        // ═══════════════════════════════════════════════════════════════
        /// <summary>
        /// Creates a Discussion Lobby using VRMeetUp's XRINetworkGameManager.
        /// This handles Lobby + Relay + NGO host automatically.
        /// </summary>
        public async void CreateLobby()
        {
            string userID   = AuthController.Instance.CurrentUser?.userID;
            string username = AuthController.Instance.CurrentUser?.username ?? "Host";
            if (string.IsNullOrEmpty(userID)) { OnError?.Invoke("Not logged in."); return; }

            try
            {
                // Set player identity in VRMeetUp
                XRINetworkGameManager.LocalPlayerName.Value = username;

                OnStatusUpdate?.Invoke("Creating lobby...");

                // Create lobby via VRMeetUp's system (handles Relay + NGO host)
                XRINetworkGameManager.Instance.CreateNewLobby(
                    roomName:    $"{username}'s Lobby",
                    isPrivate:   false,
                    playerCount: XRINetworkGameManager.maxPlayers
                );

                // Save to Firestore (done optimistically; update with room code after NGO connects)
                CurrentLobby = new DiscussionLobbyModel(userID);
                await _db.Collection("discussionLobbies")
                         .Document(CurrentLobby.lobbyID)
                         .SetAsync(CurrentLobby.ToDictionary());

                IsHost = true;
                // OnLobbyCreated is fired in OnNGOConnectionChanged when connected == true

                Debug.Log("[LobbyController_V2] CreateNewLobby called.");
            }
            catch (Exception e)
            {
                Debug.LogError($"[LobbyController_V2] CreateLobby error: {e.Message}");
                OnError?.Invoke("Failed to create lobby.");
            }
        }

        // ═══════════════════════════════════════════════════════════════
        // UC07: Join Discussion Lobby (Guest)
        // ═══════════════════════════════════════════════════════════════
        /// <summary>
        /// Joins an existing Discussion Lobby by VRMeetUp room code.
        /// VRMeetUp's JoinLobbyByCode handles Relay + NGO client automatically.
        /// </summary>
        public void JoinLobby(string roomCode)
        {
            string userID   = AuthController.Instance.CurrentUser?.userID;
            string username = AuthController.Instance.CurrentUser?.username ?? "Guest";
            if (string.IsNullOrEmpty(userID)) { OnError?.Invoke("Not logged in."); return; }

            try
            {
                // Set player identity in VRMeetUp
                XRINetworkGameManager.LocalPlayerName.Value = username;

                OnStatusUpdate?.Invoke($"Joining lobby {roomCode}...");

                // Join via VRMeetUp's system
                XRINetworkGameManager.Instance.JoinLobbyByCode(roomCode);

                IsHost = false;
                // Firebase records saved in OnNGOConnectionChanged after connected

                Debug.Log($"[LobbyController_V2] JoinLobbyByCode({roomCode}) called.");
            }
            catch (Exception e)
            {
                Debug.LogError($"[LobbyController_V2] JoinLobby error: {e.Message}");
                OnError?.Invoke("Failed to join lobby.");
            }
        }

        // ═══════════════════════════════════════════════════════════════
        // UC08: Leave Discussion Lobby (Guest)
        // ═══════════════════════════════════════════════════════════════
        public async void LeaveLobby()
        {
            try
            {
                await SaveLeaveRecords();
                XRINetworkGameManager.Instance.Disconnect();
                CleanupLobby();
                OnLobbyEnded?.Invoke();
                Debug.Log("[LobbyController_V2] Left lobby.");
            }
            catch (Exception e)
            {
                Debug.LogError($"[LobbyController_V2] LeaveLobby error: {e.Message}");
                CleanupLobby();
                OnLobbyEnded?.Invoke();
            }
        }

        // ═══════════════════════════════════════════════════════════════
        // End Lobby (Host)
        // ═══════════════════════════════════════════════════════════════
        public async void EndLobby()
        {
            try
            {
                // Update Firestore lobby status
                if (CurrentLobby != null)
                {
                    await _db.Collection("discussionLobbies")
                             .Document(CurrentLobby.lobbyID)
                             .UpdateAsync(new Dictionary<string, object>
                             {
                                 { "status",  "ended"                            },
                                 { "endedAt", DateTime.UtcNow.ToString("o")  }
                             });
                }

                // Despawn all guest avatars
                foreach (var kv in _spawnedAvatars)
                    if (kv.Value != null) Destroy(kv.Value);
                _spawnedAvatars.Clear();

                // Disconnect via VRMeetUp (this also deletes the Unity Lobby)
                XRINetworkGameManager.Instance.Disconnect();

                CleanupLobby();
                OnLobbyEnded?.Invoke();
                Debug.Log("[LobbyController_V2] Lobby ended.");
            }
            catch (Exception e)
            {
                Debug.LogError($"[LobbyController_V2] EndLobby error: {e.Message}");
                CleanupLobby();
                OnLobbyEnded?.Invoke();
            }
        }

        // ═══════════════════════════════════════════════════════════════
        // VRMeetUp Connection Callback
        // ═══════════════════════════════════════════════════════════════
        private async void OnNGOConnectionChanged(bool connected)
        {
            if (connected)
            {
                // Update Firestore lobby with actual Unity room code
                if (CurrentLobby != null && IsHost)
                {
                    await _db.Collection("discussionLobbies")
                             .Document(CurrentLobby.lobbyID)
                             .UpdateAsync(new Dictionary<string, object>
                             {
                                 { "unityRoomCode", XRINetworkGameManager.ConnectedRoomCode }
                             });
                }

                // For guests: find and save lobby member records
                if (!IsHost)
                {
                    await SaveJoinRecords();
                    OnLobbyJoined?.Invoke();
                }
                else
                {
                    OnLobbyCreated?.Invoke(CurrentLobby);
                }

                OnStatusUpdate?.Invoke($"Connected to {XRINetworkGameManager.ConnectedRoomName.Value}");
            }
            else
            {
                // Cleaned up by Leave/End, or external disconnect
                Debug.Log("[LobbyController_V2] NGO disconnected.");
            }
        }

        // ═══════════════════════════════════════════════════════════════
        // Player Join/Leave (NGO callbacks from XRINetworkGameManager)
        // ═══════════════════════════════════════════════════════════════
        private void OnPlayerStateChanged(ulong clientID, bool joined)
        {
            if (joined)
            {
                // Spawn avatar in real environment for non-local players
                if (clientID != XRINetworkGameManager.LocalId)
                    SpawnGuestAvatar(clientID);
            }
            else
            {
                // Remove avatar
                if (_spawnedAvatars.TryGetValue(clientID, out GameObject av))
                {
                    string username = "Guest";
                    if (XRINetworkGameManager.Instance.GetPlayerByID(clientID, out XRINetworkPlayer player))
                    {
                        username = player.playerName;
                    }
                    Destroy(av);
                    _spawnedAvatars.Remove(clientID);
                    Debug.Log($"[LobbyController_V2] Removed avatar for client {clientID}");
                    OnGuestLeft?.Invoke(username);
                }
            }
        }

        private void OnConnectionFailed(string reason)
        {
            OnError?.Invoke($"Connection failed: {reason}");
        }

        // ═══════════════════════════════════════════════════════════════
        // Avatar Spawning
        // ═══════════════════════════════════════════════════════════════
        private void SpawnGuestAvatar(ulong clientID)
        {
            if (guestAvatarPrefab == null) return;
            if (_spawnedAvatars.ContainsKey(clientID)) return;

            // Calculate spawn position
            Vector3 spawnPos;
            if (spawnAnchor != null)
                spawnPos = spawnAnchor.position;
            else
            {
                Transform cam = Camera.main?.transform;
                spawnPos = cam != null
                    ? cam.position + cam.forward * 1.2f + Vector3.right * (_spawnedAvatars.Count * 0.6f)
                    : Vector3.zero;
            }

            GameObject avatarObj = Instantiate(guestAvatarPrefab, spawnPos, Quaternion.identity);
            _spawnedAvatars[clientID] = avatarObj;

            string playerName = "Guest";
            // Try get player name from XRINetworkPlayer and apply to avatar label
            if (XRINetworkGameManager.Instance.GetPlayerByID(clientID, out XRINetworkPlayer player))
            {
                playerName = player.playerName;
                var label = avatarObj.GetComponentInChildren<TMPro.TextMeshProUGUI>();
                if (label != null) label.text = playerName;
            }

            Debug.Log($"[LobbyController_V2] Spawned avatar for client {clientID}");
            OnGuestAvatarSpawned?.Invoke(playerName, avatarObj);
        }

        // ═══════════════════════════════════════════════════════════════
        // Firebase Record Helpers
        // ═══════════════════════════════════════════════════════════════
        private async Task SaveJoinRecords()
        {
            string userID  = AuthController.Instance.CurrentUser?.userID;
            string username = AuthController.Instance.CurrentUser?.username ?? "Guest";
            _joinTimeISO   = DateTime.UtcNow.ToString("o");

            // Find the Firestore lobby matching the Unity room code
            QuerySnapshot snap = await _db.Collection("discussionLobbies")
                                          .WhereEqualTo("unityRoomCode", XRINetworkGameManager.ConnectedRoomCode)
                                          .WhereEqualTo("status", "active")
                                          .GetSnapshotAsync();

            var docsList = snap.Documents.ToList();
            string firestoreLobbyID = docsList.Count > 0 ? docsList[0].Id : "unknown";
            CurrentLobby = docsList.Count > 0 ? docsList[0].ConvertTo<DiscussionLobbyModel>() : null;

            // Save LOBBY_MEMBER
            var member = new LobbyMemberModel(firestoreLobbyID, userID, username);
            _memberID = member.memberID;
            await _db.Collection("lobbyMembers").Document(_memberID).SetAsync(member.ToDictionary());

            // Save MEETUP_LOG
            var log = new MeetupLogModel(firestoreLobbyID, userID, _joinTimeISO);
            _logID = log.logID;
            await _db.Collection("meetupLogs").Document(_logID).SetAsync(log.ToDictionary());
        }

        private async Task SaveLeaveRecords()
        {
            string leaveTime = DateTime.UtcNow.ToString("o");
            int    duration  = string.IsNullOrEmpty(_joinTimeISO) ? 0
                : (int)(DateTime.Parse(leaveTime) - DateTime.Parse(_joinTimeISO)).TotalSeconds;

            if (!string.IsNullOrEmpty(_memberID))
                await _db.Collection("lobbyMembers").Document(_memberID)
                         .UpdateAsync(new Dictionary<string, object> { { "leaveTime", leaveTime } });

            if (!string.IsNullOrEmpty(_logID))
                await _db.Collection("meetupLogs").Document(_logID)
                         .UpdateAsync(new Dictionary<string, object>
                         {
                             { "leaveTime", leaveTime },
                             { "duration",  duration  }
                         });
        }

        private void CleanupLobby()
        {
            CurrentLobby = null;
            IsHost       = false;
            _memberID    = null;
            _logID       = null;
            _joinTimeISO = null;
        }
    }
}
