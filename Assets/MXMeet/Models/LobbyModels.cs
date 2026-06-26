using System;
using System.Collections.Generic;
using Firebase.Firestore;

namespace MXMeet.Models
{
    [Serializable]
    [FirestoreData]
    public class DiscussionLobbyModel
    {
        [FirestoreProperty] public string lobbyId { get; set; }
        [FirestoreProperty] public string hostUserId { get; set; }
        [FirestoreProperty] public string lobbyName { get; set; }
        [FirestoreProperty] public string roomCode { get; set; }
        [FirestoreProperty] public string status { get; set; }
        [FirestoreProperty] public string createdAt { get; set; }
        [FirestoreProperty] public string endedAt { get; set; }

        public string lobbyID
        {
            get => lobbyId;
            set => lobbyId = value;
        }

        public string hostUserID
        {
            get => hostUserId;
            set => hostUserId = value;
        }

        public DiscussionLobbyModel() { }

        public DiscussionLobbyModel(string hostUserId)
            : this(hostUserId, "Discussion Lobby")
        {
        }

        public DiscussionLobbyModel(string hostUserId, string lobbyName, string roomCode = "")
        {
            this.lobbyId = Guid.NewGuid().ToString();
            this.hostUserId = hostUserId;
            this.lobbyName = lobbyName;
            this.roomCode = roomCode;
            this.status = "waiting";
            this.createdAt = DateTime.UtcNow.ToString("o");
            this.endedAt = null;
        }

        public Dictionary<string, object> ToDictionary() => new()
        {
            { "lobbyId", lobbyId },
            { "hostUserId", hostUserId },
            { "lobbyName", lobbyName },
            { "roomCode", roomCode },
            { "status", status },
            { "createdAt", createdAt },
            { "endedAt", endedAt }
        };
    }

    [Serializable]
    [FirestoreData]
    public class LobbyMemberModel
    {
        [FirestoreProperty] public string memberId { get; set; }
        [FirestoreProperty] public string lobbyId { get; set; }
        [FirestoreProperty] public string userId { get; set; }
        [FirestoreProperty] public string username { get; set; }
        [FirestoreProperty] public string role { get; set; }
        [FirestoreProperty] public string joinedAt { get; set; }
        [FirestoreProperty] public string leftAt { get; set; }
        [FirestoreProperty] public string status { get; set; }

        public string memberID
        {
            get => memberId;
            set => memberId = value;
        }

        public string lobbyID
        {
            get => lobbyId;
            set => lobbyId = value;
        }

        public string userID
        {
            get => userId;
            set => userId = value;
        }

        public LobbyMemberModel() { }

        public LobbyMemberModel(string lobbyId, string userId, string username)
            : this(lobbyId, userId, username, "guest")
        {
        }

        public LobbyMemberModel(string lobbyId, string userId, string username, string role)
        {
            this.memberId = Guid.NewGuid().ToString();
            this.lobbyId = lobbyId;
            this.userId = userId;
            this.username = username;
            this.role = role;
            this.joinedAt = DateTime.UtcNow.ToString("o");
            this.leftAt = null;
            this.status = "active";
        }

        public Dictionary<string, object> ToDictionary() => new()
        {
            { "memberId", memberId },
            { "lobbyId", lobbyId },
            { "userId", userId },
            { "username", username },
            { "role", role },
            { "joinedAt", joinedAt },
            { "leftAt", leftAt },
            { "status", status }
        };
    }

    [Serializable]
    [FirestoreData]
    public class MeetupLogModel
    {
        [FirestoreProperty] public string logId { get; set; }
        [FirestoreProperty] public string lobbyId { get; set; }
        [FirestoreProperty] public string userId { get; set; }
        [FirestoreProperty] public string action { get; set; }
        [FirestoreProperty] public string timestamp { get; set; }
        [FirestoreProperty] public int duration { get; set; }

        public string logID
        {
            get => logId;
            set => logId = value;
        }

        public string lobbyID
        {
            get => lobbyId;
            set => lobbyId = value;
        }

        public string userID
        {
            get => userId;
            set => userId = value;
        }

        public MeetupLogModel() { }

        public MeetupLogModel(string lobbyId, string userId, string action, int duration = 0)
        {
            this.logId = Guid.NewGuid().ToString();
            this.lobbyId = lobbyId;
            this.userId = userId;
            this.action = action;
            this.timestamp = DateTime.UtcNow.ToString("o");
            this.duration = duration;
        }

        public Dictionary<string, object> ToDictionary() => new()
        {
            { "logId", logId },
            { "lobbyId", lobbyId },
            { "userId", userId },
            { "action", action },
            { "timestamp", timestamp },
            { "duration", duration }
        };
    }

    [Serializable]
    [FirestoreData]
    public class MREnvironmentModel
    {
        [FirestoreProperty] public string environmentId { get; set; }
        [FirestoreProperty] public string userId { get; set; }
        [FirestoreProperty] public string anchorId { get; set; }
        [FirestoreProperty] public string updatedAt { get; set; }

        public MREnvironmentModel() { }

        public MREnvironmentModel(string userId, string anchorId)
        {
            this.environmentId = Guid.NewGuid().ToString();
            this.userId = userId;
            this.anchorId = anchorId;
            this.updatedAt = DateTime.UtcNow.ToString("o");
        }
    }
}
