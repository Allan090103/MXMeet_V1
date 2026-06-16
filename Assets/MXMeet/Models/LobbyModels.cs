using System;
using System.Collections.Generic;

namespace MXMeet.Models
{
    [Serializable]
    public class DiscussionLobbyModel
    {
        public string lobbyID;
        public string hostUserID;
        public string createdAt;
        public string endedAt;
        public string status; // "active" or "ended"

        public DiscussionLobbyModel() { }

        public DiscussionLobbyModel(string hostUserID)
        {
            this.lobbyID     = Guid.NewGuid().ToString();
            this.hostUserID  = hostUserID;
            this.createdAt   = DateTime.UtcNow.ToString("o");
            this.endedAt     = null;
            this.status      = "active";
        }

        public Dictionary<string, object> ToDictionary()
        {
            return new Dictionary<string, object>
            {
                { "lobbyID", lobbyID },
                { "hostUserID", hostUserID },
                { "createdAt", createdAt },
                { "endedAt", endedAt },
                { "status", status }
            };
        }
    }

    [Serializable]
    public class LobbyMemberModel
    {
        public string memberID;
        public string lobbyID;
        public string userID;
        public string username;
        public string joinTime;
        public string leaveTime;

        public LobbyMemberModel() { }

        public LobbyMemberModel(string lobbyID, string userID, string username)
        {
            this.memberID  = Guid.NewGuid().ToString();
            this.lobbyID   = lobbyID;
            this.userID    = userID;
            this.username  = username;
            this.joinTime  = DateTime.UtcNow.ToString("o");
            this.leaveTime = null;
        }

        public Dictionary<string, object> ToDictionary()
        {
            return new Dictionary<string, object>
            {
                { "memberID", memberID },
                { "lobbyID", lobbyID },
                { "userID", userID },
                { "username", username },
                { "joinTime", joinTime },
                { "leaveTime", leaveTime }
            };
        }
    }

    [Serializable]
    public class MeetupLogModel
    {
        public string logID;
        public string lobbyID;
        public string userID;
        public string joinTime;
        public string leaveTime;
        public int    duration; // seconds

        public MeetupLogModel() { }

        public MeetupLogModel(string lobbyID, string userID, string joinTime)
        {
            this.logID    = Guid.NewGuid().ToString();
            this.lobbyID  = lobbyID;
            this.userID   = userID;
            this.joinTime = joinTime;
            this.duration = 0;
        }

        public Dictionary<string, object> ToDictionary()
        {
            return new Dictionary<string, object>
            {
                { "logID", logID },
                { "lobbyID", lobbyID },
                { "userID", userID },
                { "joinTime", joinTime },
                { "leaveTime", leaveTime },
                { "duration", duration }
            };
        }
    }
}
