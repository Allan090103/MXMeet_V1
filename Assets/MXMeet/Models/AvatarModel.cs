using System;
using Firebase.Firestore;

namespace MXMeet.Models
{
    [Serializable]
    [FirestoreData]
    public partial class AvatarModel
    {
        [FirestoreProperty] public string avatarId  { get; set; }
        [FirestoreProperty] public string userId    { get; set; }
        [FirestoreProperty] public string skinType  { get; set; }
        [FirestoreProperty] public string colorCode { get; set; }
        [FirestoreProperty] public string updatedAt { get; set; }

        public string avatarID
        {
            get => avatarId;
            set => avatarId = value;
        }

        public string userID
        {
            get => userId;
            set => userId = value;
        }

        public AvatarModel() { }

        public AvatarModel(string userId, string skinType, string colorCode)
        {
            this.avatarId  = userId;
            this.userId    = userId;
            this.skinType  = skinType;
            this.colorCode = colorCode;
            this.updatedAt = DateTime.UtcNow.ToString("o");
        }
    }
}
