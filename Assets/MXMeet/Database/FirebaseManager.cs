using System;
using System.Threading.Tasks;
using Firebase;
using Firebase.Auth;
using Firebase.Firestore;
using UnityEngine;

namespace MXMeet.Database
{
    /// <summary>
    /// Singleton that initialises Firebase and exposes Auth + Firestore instances.
    /// Attach to a persistent GameObject in your first scene (e.g. "AppManager").
    /// </summary>
    public class FirebaseManager : MonoBehaviour
    {
        public static FirebaseManager Instance { get; private set; }

        public FirebaseAuth    Auth      { get; private set; }
        public FirebaseFirestore DB       { get; private set; }
        public bool            IsReady   { get; private set; }

        // Fired once Firebase is fully initialised
        public event Action OnFirebaseReady;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitialiseFirebase();
        }

        private async void InitialiseFirebase()
        {
            DependencyStatus status = await FirebaseApp.CheckAndFixDependenciesAsync();

            if (status == DependencyStatus.Available)
            {
                Auth    = FirebaseAuth.DefaultInstance;
                DB      = FirebaseFirestore.DefaultInstance;
                IsReady = true;
                Debug.Log("[FirebaseManager] Firebase initialised successfully.");
                OnFirebaseReady?.Invoke();
            }
            else
            {
                Debug.LogError($"[FirebaseManager] Firebase dependency check failed: {status}");
            }
        }
    }
}
