using System;

namespace MXMeet.Models
{
    [Serializable]
    public partial class AvatarModel
    {
        public string avatarID;
        public string userID;
        public string skinType;
        public string colorCode;
        public string updatedAt;

        public AvatarModel() { }

        public AvatarModel(string userID, string skinType, string colorCode)
        {
            this.avatarID  = Guid.NewGuid().ToString();
            this.userID    = userID;
            this.skinType  = skinType;
            this.colorCode = colorCode;
            this.updatedAt = DateTime.UtcNow.ToString("o");
        }
    }
}
