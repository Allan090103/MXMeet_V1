using System;
using System.Threading.Tasks;
using Firebase.Auth;
using Firebase.Firestore;
using MXMeet.Database;
using MXMeet.Models;
using UnityEngine;

namespace MXMeet.Auth
{
    public class AuthController : MonoBehaviour
    {
        public static AuthController Instance { get; private set; }

        public UserModel   CurrentUser   { get; private set; }
        public AvatarModel CurrentAvatar { get; private set; }

        public event Action<UserModel> OnLoginSuccess;
        public event Action<string>    OnLoginFailed;
        public event Action<UserModel> OnRegisterSuccess;
        public event Action<string>    OnRegisterFailed;

        private FirebaseAuth      _auth;
        private FirebaseFirestore _db;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            
            HookExternalAuth();
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                UnhookExternalAuth();
            }
        }

        private void HookExternalAuth()
        {
            CustomNetwork.AuthManagerNew.ExternalLoginAction = HandleExternalLogin;
            CustomNetwork.AuthManagerNew.ExternalRegisterAction = HandleExternalRegister;
            CustomNetwork.AuthManagerNew.ExternalLogoutAction = Logout;
        }

        private void UnhookExternalAuth()
        {
            CustomNetwork.AuthManagerNew.ExternalLoginAction = null;
            CustomNetwork.AuthManagerNew.ExternalRegisterAction = null;
            CustomNetwork.AuthManagerNew.ExternalLogoutAction = null;
        }

        private void HandleExternalLogin(string username, string password, Action<string> onSuccess, Action<string> onFailure)
        {
            Action<UserModel> localSuccess = null;
            Action<string> localFailure = null;

            localSuccess = (user) => {
                OnLoginSuccess -= localSuccess;
                OnLoginFailed -= localFailure;
                onSuccess?.Invoke(user.username);
            };

            localFailure = (err) => {
                OnLoginSuccess -= localSuccess;
                OnLoginFailed -= localFailure;
                onFailure?.Invoke(err);
            };

            OnLoginSuccess += localSuccess;
            OnLoginFailed += localFailure;

            string email = $"{username.ToLower().Replace(" ", "")}@mxmeet.com";
            Login(email, password);
        }

        private void HandleExternalRegister(string username, string password, Action onSuccess, Action<string> onFailure)
        {
            Action<UserModel> localSuccess = null;
            Action<string> localFailure = null;

            localSuccess = (user) => {
                OnRegisterSuccess -= localSuccess;
                OnRegisterFailed -= localFailure;
                onSuccess?.Invoke();
            };

            localFailure = (err) => {
                OnRegisterSuccess -= localSuccess;
                OnRegisterFailed -= localFailure;
                onFailure?.Invoke(err);
            };

            OnRegisterSuccess += localSuccess;
            OnRegisterFailed += localFailure;

            string email = $"{username.ToLower().Replace(" ", "")}@mxmeet.com";
            Register(username, email, password);
        }

        private void Start()
        {
            if (FirebaseManager.Instance == null)
            {
                OnLoginFailed?.Invoke("Firebase manager is not loaded.");
                return;
            }

            if (FirebaseManager.Instance.IsReady) Initialise();
            else FirebaseManager.Instance.OnFirebaseReady += Initialise;
        }

        private void Initialise()
        {
            if (FirebaseManager.Instance == null) return;
            _auth = FirebaseManager.Instance.Auth;
            _db   = FirebaseManager.Instance.DB;
        }

        public async void Register(string username, string email, string password)
        {
            if (_auth == null) { OnRegisterFailed?.Invoke("Firebase not ready."); return; }
            try
            {
                if (_db == null) { OnRegisterFailed?.Invoke("Firestore is not ready."); return; }
                bool taken = await IsUsernameTaken(username);
                if (taken) { OnRegisterFailed?.Invoke("Username already taken."); return; }

                AuthResult result  = await _auth.CreateUserWithEmailAndPasswordAsync(email, password);
                FirebaseUser fbUser = result.User;

                var user   = new UserModel(fbUser.UserId, username, email);
                var avatar = new AvatarModel(fbUser.UserId, "default", "#FFFFFF");

                await _db.Collection("users").Document(fbUser.UserId).SetAsync(user.ToDictionary());
                await _db.Collection("avatars").Document(fbUser.UserId).SetAsync(avatar.ToDictionary());

                CurrentUser   = user;
                CurrentAvatar = avatar;
                OnRegisterSuccess?.Invoke(user);
            }
            catch (Exception e)
            {
                OnRegisterFailed?.Invoke(ParseFirebaseError(e.Message));
            }
        }

        public async void Login(string email, string password)
        {
            if (_auth == null) { OnLoginFailed?.Invoke("Firebase not ready."); return; }
            try
            {
                if (_db == null) { OnLoginFailed?.Invoke("Firestore is not ready."); return; }
                AuthResult result  = await _auth.SignInWithEmailAndPasswordAsync(email, password);
                FirebaseUser fbUser = result.User;

                DocumentSnapshot userDoc = await _db.Collection("users").Document(fbUser.UserId).GetSnapshotAsync();
                if (!userDoc.Exists) { OnLoginFailed?.Invoke("User profile not found."); return; }
                CurrentUser = userDoc.ConvertTo<UserModel>();

                DocumentSnapshot avatarDoc = await _db.Collection("avatars").Document(fbUser.UserId).GetSnapshotAsync();
                if (avatarDoc.Exists) CurrentAvatar = avatarDoc.ConvertTo<AvatarModel>();

                OnLoginSuccess?.Invoke(CurrentUser);
            }
            catch (Exception e)
            {
                OnLoginFailed?.Invoke(ParseFirebaseError(e.Message));
            }
        }

        public void Logout()
        {
            _auth?.SignOut();
            CurrentUser   = null;
            CurrentAvatar = null;
        }

        public void UpdateCurrentAvatar(AvatarModel avatar) { CurrentAvatar = avatar; }

        /// <summary>Called by AppBootstrap when a persistent session is detected.</summary>
        public async void SetRestoredUser(UserModel user)
        {
            CurrentUser = user;
            try
            {
                if (_db == null) Initialise();
                if (_db == null) return;
                DocumentSnapshot doc = await _db.Collection("avatars").Document(user.userId).GetSnapshotAsync();
                if (doc.Exists) CurrentAvatar = doc.ConvertTo<AvatarModel>();
            }
            catch (Exception e) { Debug.LogWarning($"[AuthController] Avatar restore failed: {e.Message}"); }
        }

        private async Task<bool> IsUsernameTaken(string username)
        {
            QuerySnapshot snap = await _db.Collection("users").WhereEqualTo("username", username).GetSnapshotAsync();
            return snap.Count > 0;
        }

        private string ParseFirebaseError(string msg)
        {
            if (msg.Contains("email-already-in-use"))  return "Email already registered.";
            if (msg.Contains("wrong-password"))         return "Incorrect password.";
            if (msg.Contains("user-not-found"))         return "No account found for this email.";
            if (msg.Contains("invalid-email"))          return "Invalid email format.";
            if (msg.Contains("weak-password"))          return "Password must be at least 6 characters.";
            if (msg.Contains("network-request-failed")) return "Network error. Check your connection.";
            return $"Firebase Error: {msg}";
        }
    }
}

namespace MXMeet.Models
{
    using System.Collections.Generic;
    public partial class UserModel
    {
        public Dictionary<string, object> ToDictionary() => new Dictionary<string, object>
        {
            { "userId",    userId    },
            { "username",  username  },
            { "email",     email     },
            { "createdAt", createdAt },
        };
    }
}
