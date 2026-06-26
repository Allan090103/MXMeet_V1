using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Firebase.Firestore;
using MXMeet.Auth;
using MXMeet.Database;
using MXMeet.Models;
using UnityEngine;
using XRMultiplayer;

namespace MXMeet.Lobby
{
    /// <summary>
    /// MXMeet discussion lobby adapter. VRMeetUp owns networking, Relay, NGO, and Vivox;
    /// this controller owns MXMeet-specific Firestore records and MR guest markers.
    /// </summary>
    public class LobbyController_V2 : MonoBehaviour
    {
        public static LobbyController_V2 Instance { get; private set; }

        [Header("Avatar Spawning")]
        public GameObject guestAvatarPrefab;
        public Transform spawnAnchor;

        public event Action<DiscussionLobbyModel> OnLobbyCreated;
        public event Action OnLobbyJoined;
        public event Action OnLobbyEnded;
        public event Action<string> OnError;
        public event Action<string> OnStatusUpdate;
        public event Action<string, GameObject> OnGuestAvatarSpawned;
        public event Action<string> OnGuestLeft;

        public DiscussionLobbyModel CurrentLobby { get; private set; }
        public bool IsHost { get; private set; }
        public string LobbyCode => XRINetworkGameManager.ConnectedRoomCode;
        public string UnityLobbyCode => LobbyCode;
        public string LobbyName => XRINetworkGameManager.ConnectedRoomName.Value;

        private FirebaseFirestore _db;
        private string _memberId;
        private string _joinTimeIso;
        private bool _hostRecordsSaved;
        private bool _joinRecordsSaved;
        private XRINetworkGameManager _hookedNetworkManager;

        private readonly Dictionary<ulong, GameObject> _spawnedAvatars = new();

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            if (FirebaseManager.Instance != null && FirebaseManager.Instance.IsReady)
                _db = FirebaseManager.Instance.DB;
            else if (FirebaseManager.Instance != null)
                FirebaseManager.Instance.OnFirebaseReady += OnFirebaseReady;

            XRINetworkGameManager.Connected.Subscribe(OnNGOConnectionChanged);
            HookNetworkEvents();
        }

        private void Update()
        {
            if (_hookedNetworkManager == null)
                HookNetworkEvents();
        }

        private void OnDestroy()
        {
            if (FirebaseManager.Instance != null)
                FirebaseManager.Instance.OnFirebaseReady -= OnFirebaseReady;

            XRINetworkGameManager.Connected.Unsubscribe(OnNGOConnectionChanged);
            UnhookNetworkEvents();
        }

        private void OnFirebaseReady()
        {
            _db = FirebaseManager.Instance.DB;
        }

        private void HookNetworkEvents()
        {
            XRINetworkGameManager manager = XRINetworkGameManager.Instance;
            if (manager == null)
            {
                Debug.LogWarning("[LobbyController_V2] XRINetworkGameManager not ready yet.");
                return;
            }

            if (_hookedNetworkManager == manager) return;

            UnhookNetworkEvents();
            manager.playerStateChanged += OnPlayerStateChanged;
            manager.connectionFailedAction += OnConnectionFailed;
            _hookedNetworkManager = manager;
        }

        private void UnhookNetworkEvents()
        {
            if (_hookedNetworkManager == null) return;
            _hookedNetworkManager.playerStateChanged -= OnPlayerStateChanged;
            _hookedNetworkManager.connectionFailedAction -= OnConnectionFailed;
            _hookedNetworkManager = null;
        }

        public async void CreateLobby()
        {
            if (!TryGetUser(out string userId, out string username)) return;
            if (!EnsureFirebaseReady()) return;
            XRINetworkGameManager manager = ResolveNetworkManager();
            if (manager == null) { OnError?.Invoke("VRMeetUp network manager is not ready."); return; }
            if (!await EnsureVRMeetUpAuthenticated(manager)) return;

            try
            {
                string lobbyName = $"{username}'s Lobby";
                CurrentLobby = new DiscussionLobbyModel(userId, lobbyName);
                IsHost = true;
                _hostRecordsSaved = false;
                _joinRecordsSaved = false;

                await _db.Collection("discussion_lobbies")
                    .Document(CurrentLobby.lobbyId)
                    .SetAsync(CurrentLobby.ToDictionary());

                XRINetworkGameManager.LocalPlayerName.Value = username;
                OnStatusUpdate?.Invoke("Creating lobby...");
                manager.CreateNewLobby(lobbyName, false, XRINetworkGameManager.maxPlayers);
            }
            catch (Exception e)
            {
                Debug.LogError($"[LobbyController_V2] CreateLobby error: {e}");
                OnError?.Invoke("Failed to create lobby.");
            }
        }

        public async void JoinLobby(string roomCode)
        {
            if (!TryGetUser(out string userId, out string username)) return;
            if (!EnsureFirebaseReady()) return;
            XRINetworkGameManager manager = ResolveNetworkManager();
            if (manager == null) { OnError?.Invoke("VRMeetUp network manager is not ready."); return; }
            if (!await EnsureVRMeetUpAuthenticated(manager)) return;

            roomCode = (roomCode ?? string.Empty).Trim().ToUpperInvariant();
            if (string.IsNullOrEmpty(roomCode)) { OnError?.Invoke("Please enter a lobby code."); return; }

            try
            {
                OnStatusUpdate?.Invoke($"Checking lobby {roomCode}...");
                CurrentLobby = await FindLobbyByRoomCode(roomCode);
                if (CurrentLobby == null)
                {
                    OnError?.Invoke("Lobby not found or already ended.");
                    return;
                }

                IsHost = false;
                _memberId = null;
                _joinTimeIso = null;
                _joinRecordsSaved = false;

                XRINetworkGameManager.LocalPlayerName.Value = username;
                OnStatusUpdate?.Invoke($"Joining lobby {roomCode}...");
                manager.JoinLobbyByCode(roomCode);
            }
            catch (Exception e)
            {
                Debug.LogError($"[LobbyController_V2] JoinLobby error: {e}");
                OnError?.Invoke("Failed to join lobby.");
            }
        }

        public async void LeaveLobby()
        {
            try
            {
                await SaveLeaveRecords();
                XRINetworkGameManager.Instance?.Disconnect();
            }
            catch (Exception e)
            {
                Debug.LogError($"[LobbyController_V2] LeaveLobby error: {e}");
            }
            finally
            {
                CleanupSpawnedAvatars();
                CleanupLobby();
                OnLobbyEnded?.Invoke();
            }
        }

        public async void EndLobby()
        {
            try
            {
                if (CurrentLobby != null && EnsureFirebaseReady())
                {
                    string endedAt = DateTime.UtcNow.ToString("o");
                    await _db.Collection("discussion_lobbies")
                        .Document(CurrentLobby.lobbyId)
                        .UpdateAsync(new Dictionary<string, object>
                        {
                            { "status", "ended" },
                            { "endedAt", endedAt }
                        });

                    await EndActiveMembers(endedAt);
                    await AddMeetupLog(CurrentLobby.lobbyId, CurrentUserId(), "Lobby Ended", CurrentDurationSeconds());
                }

                XRINetworkGameManager.Instance?.Disconnect();
            }
            catch (Exception e)
            {
                Debug.LogError($"[LobbyController_V2] EndLobby error: {e}");
            }
            finally
            {
                CleanupSpawnedAvatars();
                CleanupLobby();
                OnLobbyEnded?.Invoke();
            }
        }

        public void SetSelfMuted(bool muted)
        {
            var voice = FindFirstObjectByType<VoiceChatManager>();
            if (voice == null)
            {
                OnError?.Invoke("Voice chat manager is not available.");
                return;
            }

            voice.ToggleSelfMute(true, muted);
        }

        private async void OnNGOConnectionChanged(bool connected)
        {
            if (!connected)
            {
                Debug.Log("[LobbyController_V2] NGO disconnected.");
                return;
            }

            if (!EnsureFirebaseReady()) return;

            try
            {
                if (IsHost)
                {
                    await SaveHostConnectionRecords();
                    OnLobbyCreated?.Invoke(CurrentLobby);
                }
                else
                {
                    await SaveJoinRecords();
                    OnLobbyJoined?.Invoke();
                }

                OnStatusUpdate?.Invoke($"Connected to {XRINetworkGameManager.ConnectedRoomName.Value}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[LobbyController_V2] Connection record error: {e}");
                OnError?.Invoke("Connected, but failed to save lobby records.");
            }
        }

        private async Task SaveHostConnectionRecords()
        {
            if (CurrentLobby == null || _hostRecordsSaved) return;

            string roomCode = XRINetworkGameManager.ConnectedRoomCode;
            CurrentLobby.roomCode = roomCode;
            CurrentLobby.status = "active";

            await _db.Collection("discussion_lobbies")
                .Document(CurrentLobby.lobbyId)
                .UpdateAsync(new Dictionary<string, object>
                {
                    { "roomCode", roomCode },
                    { "status", "active" }
                });

            await AddMember(CurrentLobby.lobbyId, CurrentUserId(), CurrentUsername("Host"), "host");
            await AddMeetupLog(CurrentLobby.lobbyId, CurrentUserId(), "Lobby Created");
            _hostRecordsSaved = true;
        }

        private async Task SaveJoinRecords()
        {
            if (_joinRecordsSaved) return;

            if (CurrentLobby == null)
                CurrentLobby = await FindLobbyByRoomCode(XRINetworkGameManager.ConnectedRoomCode);

            if (CurrentLobby == null)
                throw new InvalidOperationException("MXMeet lobby record was not found for the connected room.");

            await AddMember(CurrentLobby.lobbyId, CurrentUserId(), CurrentUsername("Guest"), "guest");
            await AddMeetupLog(CurrentLobby.lobbyId, CurrentUserId(), "User Joined");
            _joinRecordsSaved = true;
        }

        private async Task SaveLeaveRecords()
        {
            if (CurrentLobby == null || !EnsureFirebaseReady()) return;

            string leftAt = DateTime.UtcNow.ToString("o");
            int duration = CurrentDurationSeconds();

            if (!string.IsNullOrEmpty(_memberId))
            {
                await _db.Collection("lobby_members")
                    .Document(_memberId)
                    .UpdateAsync(new Dictionary<string, object>
                    {
                        { "leftAt", leftAt },
                        { "status", "left" }
                    });
            }

            await AddMeetupLog(CurrentLobby.lobbyId, CurrentUserId(), "User Left", duration);
        }

        private async Task AddMember(string lobbyId, string userId, string username, string role)
        {
            var member = new LobbyMemberModel(lobbyId, userId, username, role);
            _memberId = member.memberId;
            _joinTimeIso = member.joinedAt;

            await _db.Collection("lobby_members")
                .Document(member.memberId)
                .SetAsync(member.ToDictionary());
        }

        private async Task AddMeetupLog(string lobbyId, string userId, string action, int duration = 0)
        {
            var log = new MeetupLogModel(lobbyId, userId, action, duration);
            await _db.Collection("meetup_logs")
                .Document(log.logId)
                .SetAsync(log.ToDictionary());
        }

        private async Task EndActiveMembers(string leftAt)
        {
            if (CurrentLobby == null) return;

            QuerySnapshot members = await _db.Collection("lobby_members")
                .WhereEqualTo("lobbyId", CurrentLobby.lobbyId)
                .WhereEqualTo("status", "active")
                .GetSnapshotAsync();

            foreach (DocumentSnapshot doc in members.Documents)
            {
                await doc.Reference.UpdateAsync(new Dictionary<string, object>
                {
                    { "leftAt", leftAt },
                    { "status", "left" }
                });
            }
        }

        private async Task<DiscussionLobbyModel> FindLobbyByRoomCode(string roomCode)
        {
            QuerySnapshot snap = await _db.Collection("discussion_lobbies")
                .WhereEqualTo("roomCode", roomCode)
                .GetSnapshotAsync();

            foreach (DocumentSnapshot doc in snap.Documents)
            {
                var lobby = doc.ConvertTo<DiscussionLobbyModel>();
                if (lobby.status == "waiting" || lobby.status == "active")
                    return lobby;
            }

            return null;
        }

        private void OnPlayerStateChanged(ulong clientId, bool joined)
        {
            if (joined)
            {
                if (clientId != XRINetworkGameManager.LocalId)
                    SpawnGuestAvatar(clientId);
                return;
            }

            if (_spawnedAvatars.TryGetValue(clientId, out GameObject avatar))
            {
                string username = "Guest";
                if (XRINetworkGameManager.Instance.GetPlayerByID(clientId, out XRINetworkPlayer player))
                    username = player.playerName;

                Destroy(avatar);
                _spawnedAvatars.Remove(clientId);
                OnGuestLeft?.Invoke(username);
            }
        }

        private void SpawnGuestAvatar(ulong clientId)
        {
            if (guestAvatarPrefab == null || _spawnedAvatars.ContainsKey(clientId)) return;

            Vector3 spawnPos;
            if (spawnAnchor != null)
            {
                spawnPos = spawnAnchor.position;
            }
            else
            {
                Transform cam = Camera.main?.transform;
                spawnPos = cam != null
                    ? cam.position + cam.forward * 1.2f + Vector3.right * (_spawnedAvatars.Count * 0.6f)
                    : Vector3.zero;
            }

            GameObject avatarObj = Instantiate(guestAvatarPrefab, spawnPos, Quaternion.identity);
            _spawnedAvatars[clientId] = avatarObj;

            string playerName = "Guest";
            if (XRINetworkGameManager.Instance.GetPlayerByID(clientId, out XRINetworkPlayer player))
            {
                playerName = player.playerName;
                var label = avatarObj.GetComponentInChildren<TMPro.TextMeshProUGUI>();
                if (label != null) label.text = playerName;
            }

            OnGuestAvatarSpawned?.Invoke(playerName, avatarObj);
        }

        private void OnConnectionFailed(string reason)
        {
            OnError?.Invoke($"Connection failed: {reason}");
        }

        private bool TryGetUser(out string userId, out string username)
        {
            userId = CurrentUserId();
            username = CurrentUsername("Guest");
            if (!string.IsNullOrEmpty(userId)) return true;

            OnError?.Invoke("Not logged in.");
            return false;
        }

        private bool EnsureFirebaseReady()
        {
            if (_db != null) return true;

            if (FirebaseManager.Instance != null && FirebaseManager.Instance.IsReady)
            {
                _db = FirebaseManager.Instance.DB;
                return true;
            }

            OnError?.Invoke("Firebase is not ready.");
            return false;
        }

        private string CurrentUserId() => AuthController.Instance?.CurrentUser?.userId;

        private string CurrentUsername(string fallback) =>
            AuthController.Instance?.CurrentUser?.username ?? fallback;

        private int CurrentDurationSeconds()
        {
            if (string.IsNullOrEmpty(_joinTimeIso)) return 0;
            return (int)(DateTime.UtcNow - DateTime.Parse(_joinTimeIso)).TotalSeconds;
        }

        private void CleanupSpawnedAvatars()
        {
            foreach (var avatar in _spawnedAvatars.Values.Where(avatar => avatar != null))
                Destroy(avatar);

            _spawnedAvatars.Clear();
        }

        private void CleanupLobby()
        {
            CurrentLobby = null;
            IsHost = false;
            _memberId = null;
            _joinTimeIso = null;
            _hostRecordsSaved = false;
            _joinRecordsSaved = false;
        }

        private XRINetworkGameManager ResolveNetworkManager()
        {
            HookNetworkEvents();
            if (_hookedNetworkManager != null) return _hookedNetworkManager;

#if UNITY_EDITOR
            MXMeet.EditorBootstrap.EnsureVRMeetUpManagersForEditorTesting();
            HookNetworkEvents();
#endif

            return _hookedNetworkManager ?? XRINetworkGameManager.Instance;
        }

        private async Task<bool> EnsureVRMeetUpAuthenticated(XRINetworkGameManager manager)
        {
            if (manager == null) return false;
            if (manager.IsAuthenticated()) return true;

            try
            {
                OnStatusUpdate?.Invoke("Authenticating VRMeetUp services...");
                bool authenticated = await manager.Authenticate();
                if (authenticated) return true;

                OnError?.Invoke("VRMeetUp authentication failed. Check Unity Gaming Services settings.");
                return false;
            }
            catch (Exception e)
            {
                Debug.LogError($"[LobbyController_V2] VRMeetUp auth error: {e}");
                OnError?.Invoke($"VRMeetUp authentication failed: {e.Message}");
                return false;
            }
        }
    }
}
