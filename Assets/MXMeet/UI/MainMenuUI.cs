using MXMeet.Auth;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace MXMeet.UI
{
    /// <summary>
    /// Main menu displayed after login. Provides navigation to Avatar, Lobby, and MR view.
    /// </summary>
    public class MainMenuUI : MonoBehaviour
    {
        public Button avatarButton;
        public Button lobbyButton;
        public Button mrButton;
        public Button logoutButton;

        private void Awake()
        {
            if (avatarButton) avatarButton.onClick.AddListener(() => LoadScene("AvatarSelection"));
            if (lobbyButton) lobbyButton.onClick.AddListener(() => LoadScene("Lobby"));
            if (mrButton) mrButton.onClick.AddListener(() => LoadScene("MRView"));
            if (logoutButton) logoutButton.onClick.AddListener(Logout);
        }

        private void LoadScene(string sceneName)
        {
            SceneManager.LoadScene(sceneName);
        }

        private void Logout()
        {
            AuthController.Instance?.Logout();
            SceneManager.LoadScene("Login");
        }
    }
}
