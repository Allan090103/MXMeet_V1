using MXMeet.Avatar;
using MXMeet.Models;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace MXMeet.UI
{
    /// <summary>
    /// Controls the Avatar Selection screen (UC03).
    ///
    /// Scene setup:
    ///   - AvatarPreview  : GameObject (3D avatar preview in scene)
    ///   - SkinButtons    : array of Buttons, each tagged with skin name
    ///   - ColorButtons   : array of Buttons, each with a ColorSwatch component
    ///   - ConfirmButton, SkipButton : Button
    ///   - ErrorText : TextMeshProUGUI
    /// </summary>
    public class AvatarSelectionUI : MonoBehaviour
    {
        [Header("Preview")]
        public GameObject avatarPreviewObject;

        [Header("Skin Selection")]
        public Button[] skinButtons; // Name each button after the skin e.g. "default", "robot", "astronaut"

        [Header("Colour Selection")]
        public ColorSwatch[] colorSwatches; // custom component, see below

        [Header("Buttons")]
        public Button confirmButton;
        public Button skipButton;

        [Header("Feedback")]
        public TextMeshProUGUI errorText;
        public GameObject      loadingIndicator;

        [Header("Navigation")]
        public string mainMenuScene = "MainMenu";

        private string _selectedSkin  = "default";
        private string _selectedColor = "#FFFFFF";
        private AvatarController _avatarController;

        private void Start()
        {
            if (!EnsureAvatarController())
            {
                SetError("Avatar system is not ready. Please return to the menu and try again.");
                SetButtonsInteractable(false);
                return;
            }

            if (confirmButton != null)
            {
                confirmButton.onClick.RemoveListener(OnConfirmClicked);
                confirmButton.onClick.AddListener(OnConfirmClicked);
            }

            if (skipButton != null)
            {
                skipButton.onClick.RemoveListener(OnSkipClicked);
                skipButton.onClick.AddListener(OnSkipClicked);
            }

            // Wire skin buttons
            if (skinButtons != null)
            {
                foreach (Button btn in skinButtons)
                {
                    if (btn == null) continue;
                    string skinName = btn.name; // button name = skin type
                    btn.onClick.RemoveAllListeners();
                    btn.onClick.AddListener(() => SelectSkin(skinName, btn.gameObject));
                }
            }

            // Wire colour swatches
            if (colorSwatches != null)
            {
                foreach (ColorSwatch swatch in colorSwatches)
                {
                    if (swatch?.button == null) continue;
                    ColorSwatch s = swatch; // capture
                    s.button.onClick.RemoveAllListeners();
                    s.button.onClick.AddListener(() => SelectColor(s.hexColor));
                }
            }

            _avatarController.OnAvatarSaved     += HandleAvatarSaved;
            _avatarController.OnAvatarSaveFailed += HandleAvatarSaveFailed;

            // Load existing avatar if any
            LoadExistingAvatar();
            SetError("");
        }

        private void OnDestroy()
        {
            if (_avatarController == null) return;
            _avatarController.OnAvatarSaved     -= HandleAvatarSaved;
            _avatarController.OnAvatarSaveFailed -= HandleAvatarSaveFailed;
        }

        // ── Skin Selection ────────────────────────────────────────────────
        private void SelectSkin(string skinName, GameObject buttonObj)
        {
            if (!EnsureAvatarController()) return;

            _selectedSkin = skinName;
            // Highlight selected button
            if (skinButtons != null)
            {
                foreach (Button b in skinButtons)
                {
                    if (b == null) continue;
                    Image image = b.GetComponent<Image>();
                    if (image != null) image.color = b.gameObject == buttonObj ? Color.yellow : Color.white;
                }
            }

            // Update preview
            if (avatarPreviewObject != null)
            {
                var model = new AvatarModel("preview", skinName, _selectedColor);
                _avatarController.ApplyAvatarToGameObject(avatarPreviewObject, model);
            }
        }

        // ── Colour Selection ──────────────────────────────────────────────
        private void SelectColor(string hexColor)
        {
            if (!EnsureAvatarController()) return;

            _selectedColor = hexColor;
            if (avatarPreviewObject != null)
            {
                var model = new AvatarModel("preview", _selectedSkin, hexColor);
                _avatarController.ApplyAvatarToGameObject(avatarPreviewObject, model);
            }
        }

        // ── Confirm / Skip ────────────────────────────────────────────────
        private void OnConfirmClicked()
        {
            if (!EnsureAvatarController()) return;

            SetLoading(true);
            _avatarController.SaveAvatar(_selectedSkin, _selectedColor);
        }

        private void OnSkipClicked()
        {
            if (!EnsureAvatarController()) return;

            // Apply default avatar and proceed
            SetLoading(true);
            _avatarController.SaveAvatar("default", "#FFFFFF");
        }

        // ── Event Handlers ────────────────────────────────────────────────
        private void HandleAvatarSaved(AvatarModel avatar)
        {
            SetLoading(false);
            SceneManager.LoadScene(mainMenuScene);
        }

        private void HandleAvatarSaveFailed(string error)
        {
            SetLoading(false);
            SetError(error);
        }

        // ── Load Existing Avatar ──────────────────────────────────────────
        private async void LoadExistingAvatar()
        {
            if (_avatarController == null) return;

            AvatarModel existing = await _avatarController.LoadAvatar();
            if (existing != null)
            {
                _selectedSkin  = existing.skinType;
                _selectedColor = existing.colorCode;
                if (avatarPreviewObject != null)
                    _avatarController.ApplyAvatarToGameObject(avatarPreviewObject, existing);
            }
        }

        // ── Helpers ───────────────────────────────────────────────────────
        private void SetError(string msg)
        {
            if (errorText == null) return;
            errorText.text = msg;
            errorText.gameObject.SetActive(!string.IsNullOrEmpty(msg));
        }

        private void SetLoading(bool loading)
        {
            if (loadingIndicator != null) loadingIndicator.SetActive(loading);
            SetButtonsInteractable(!loading);
        }

        private void SetButtonsInteractable(bool interactable)
        {
            if (confirmButton != null) confirmButton.interactable = interactable;
            if (skipButton != null) skipButton.interactable = interactable;
        }

        private bool EnsureAvatarController()
        {
            if (_avatarController != null) return true;

            _avatarController = AvatarController.Instance;
            if (_avatarController == null)
            {
                AvatarController existing = FindFirstObjectByType<AvatarController>();
                if (existing != null)
                {
                    _avatarController = existing;
                }
            }

            if (_avatarController == null)
            {
                GameObject go = new GameObject("AvatarController");
                _avatarController = go.AddComponent<AvatarController>();
                DontDestroyOnLoad(go);
            }

            if (_avatarController == null)
            {
                SetError("Avatar system is not ready.");
                Debug.LogError("[AvatarSelectionUI] Could not create AvatarController.");
                return false;
            }

            return true;
        }
    }

    /// <summary>Simple component to attach a hex colour string to a UI Button.</summary>
    [System.Serializable]
    public class ColorSwatch
    {
        public Button button;
        public string hexColor; // e.g. "#FF0000"
    }
}
