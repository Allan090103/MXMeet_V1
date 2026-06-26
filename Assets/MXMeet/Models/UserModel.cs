using System;
using Firebase.Firestore;

namespace MXMeet.Models
{
    [Serializable]
    [FirestoreData]
    public partial class UserModel
    {
        [FirestoreProperty] public string userId    { get; set; }
        [FirestoreProperty] public string username  { get; set; }
        [FirestoreProperty] public string email     { get; set; }
        [FirestoreProperty] public string createdAt { get; set; }

        public string userID
        {
            get => userId;
            set => userId = value;
        }

        public UserModel() { }

        public UserModel(string userId, string username)
            : this(userId, username, string.Empty)
        {
        }

        public UserModel(string userId, string username, string email)
        {
            this.userId    = userId;
            this.username  = username;
            this.email     = email;
            this.createdAt = DateTime.UtcNow.ToString("o");
        }
    }
}
