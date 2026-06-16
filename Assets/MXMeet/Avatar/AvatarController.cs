using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Firebase.Firestore;
using MXMeet.Auth;
using MXMeet.Database;
using MXMeet.Models;
using UnityEngine;

namespace MXMeet.Avatar
{
    /// <summary>
    /// Manages avatar skin and colour selection.
    /// Saves and loads avatar settings from Firestore under "avatars/{userID}".
    /// </summary>
    public class AvatarController : MonoBehaviour
    {
        public static AvatarController Instance { get; private set; }

        public event Action<AvatarModel> OnAvatarSaved;
        public event Action<string>      OnAvatarSaveFailed;

        private FirebaseFirestore _db;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            if (FirebaseManager.Instance.IsReady) _db = FirebaseManager.Instance.DB;
            else FirebaseManager.Instance.OnFirebaseReady += () => _db = FirebaseManager.Instance.DB;
        }

        // ── Save Avatar ───────────────────────────────────────────────────
        /// <summary>Saves selected skin and colour to Firestore and updates CurrentAvatar.</summary>
        public async void SaveAvatar(string skinType, string colorCode)
        {
            string userID = AuthController.Instance.CurrentUser?.userID;
            if (string.IsNullOrEmpty(userID)) { OnAvatarSaveFailed?.Invoke("Not logged in."); return; }

            try
            {
                var avatar = new AvatarModel(userID, skinType, colorCode);

                await _db.Collection("avatars")
                         .Document(userID)
                         .SetAsync(avatar.ToDictionary(), SetOptions.MergeAll);

                AuthController.Instance.UpdateCurrentAvatar(avatar);

                Debug.Log($"[AvatarController] Avatar saved: skin={skinType}, color={colorCode}");
                OnAvatarSaved?.Invoke(avatar);
            }
            catch (Exception e)
            {
                Debug.LogError($"[AvatarController] Save error: {e.Message}");
                OnAvatarSaveFailed?.Invoke("Failed to save avatar. Please try again.");
            }
        }

        // ── Load Avatar ───────────────────────────────────────────────────
        /// <summary>Loads avatar settings for the current user from Firestore.</summary>
        public async Task<AvatarModel> LoadAvatar()
        {
            string userID = AuthController.Instance.CurrentUser?.userID;
            if (string.IsNullOrEmpty(userID)) return null;

            try
            {
                DocumentSnapshot doc = await _db.Collection("avatars").Document(userID).GetSnapshotAsync();
                return doc.Exists ? doc.ConvertTo<AvatarModel>() : null;
            }
            catch (Exception e)
            {
                Debug.LogError($"[AvatarController] Load error: {e.Message}");
                return null;
            }
        }

        // ── Apply Avatar to GameObject ─────────────────────────────────────
        /// <summary>Applies skin and colour to a given avatar GameObject (renderer).</summary>
        public void ApplyAvatarToGameObject(GameObject avatarObject, AvatarModel avatar)
        {
            if (avatarObject == null || avatar == null) return;

            // Apply colour
            Renderer rend = avatarObject.GetComponentInChildren<Renderer>();
            if (rend != null && ColorUtility.TryParseHtmlString(avatar.colorCode, out Color col))
                rend.material.color = col;

            // Apply skin — assumes child GameObjects named after skin types
            foreach (Transform child in avatarObject.transform)
                child.gameObject.SetActive(child.name == avatar.skinType);

            Debug.Log($"[AvatarController] Applied avatar: skin={avatar.skinType}, color={avatar.colorCode}");
        }
    }
}

// ── Extension ────────────────────────────────────────────────────────────────
namespace MXMeet.Models
{
    public partial class AvatarModel
    {
        public Dictionary<string, object> ToDictionary()
            => new Dictionary<string, object>
            {
                { "avatarID",   avatarID   },
                { "userID",     userID     },
                { "skinType",   skinType   },
                { "colorCode",  colorCode  },
                { "updatedAt",  updatedAt  },
            };
    }
}
