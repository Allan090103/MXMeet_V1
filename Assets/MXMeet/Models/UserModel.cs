using System;

namespace MXMeet.Models
{
    [Serializable]
    public partial class UserModel
    {
        public string userID;
        public string username;
        public string createdAt;

        public UserModel() { }

        public UserModel(string userID, string username)
        {
            this.userID    = userID;
            this.username  = username;
            this.createdAt = DateTime.UtcNow.ToString("o");
        }
    }
}
