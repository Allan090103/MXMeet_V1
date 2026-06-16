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

        private void Start()
        {
            confirmButton.onClick.AddListener(OnConfirmClicked);
            skipButton.onClick.AddListener(OnSkipClicked);

            // Wire skin buttons
            foreach (Button btn in skinButtons)
            {
                string skinName = btn.name; // button name = skin type
                btn.onClick.AddListener(() => SelectSkin(skinName, btn.gameObject));
            }

            // Wire colour swatches
            foreach (ColorSwatch swatch in colorSwatches)
            {
                ColorSwatch s = swatch; // capture
                s.button.onClick.AddListener(() => SelectColor(s.hexColor));
            }

            AvatarController.Instance.OnAvatarSaved     += HandleAvatarSaved;
            AvatarController.Instance.OnAvatarSaveFailed += HandleAvatarSaveFailed;

            // Load existing avatar if any
            LoadExistingAvatar();
            SetError("");
        }

        private void OnDestroy()
        {
            if (AvatarController.Instance == null) return;
            AvatarController.Instance.OnAvatarSaved     -= HandleAvatarSaved;
            AvatarController.Instance.OnAvatarSaveFailed -= HandleAvatarSaveFailed;
        }

        // ── Skin Selection ────────────────────────────────────────────────
        private void SelectSkin(string skinName, GameObject buttonObj)
        {
            _selectedSkin = skinName;
            // Highlight selected button
            foreach (Button b in skinButtons)
                b.GetComponent<Image>().color = b.gameObject == buttonObj ? Color.yellow : Color.white;

            // Update preview
            if (avatarPreviewObject != null)
            {
                var model = new AvatarModel("preview", skinName, _selectedColor);
                AvatarController.Instance.ApplyAvatarToGameObject(avatarPreviewObject, model);
            }
        }

        // ── Colour Selection ──────────────────────────────────────────────
        private void SelectColor(string hexColor)
        {
            _selectedColor = hexColor;
            if (avatarPreviewObject != null)
            {
                var model = new AvatarModel("preview", _selectedSkin, hexColor);
                AvatarController.Instance.ApplyAvatarToGameObject(avatarPreviewObject, model);
            }
        }

        // ── Confirm / Skip ────────────────────────────────────────────────
        private void OnConfirmClicked()
        {
            SetLoading(true);
            AvatarController.Instance.SaveAvatar(_selectedSkin, _selectedColor);
        }

        private void OnSkipClicked()
        {
            // Apply default avatar and proceed
            AvatarController.Instance.SaveAvatar("default", "#FFFFFF");
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
            AvatarModel existing = await AvatarController.Instance.LoadAvatar();
            if (existing != null)
            {
                _selectedSkin  = existing.skinType;
                _selectedColor = existing.colorCode;
                if (avatarPreviewObject != null)
                    AvatarController.Instance.ApplyAvatarToGameObject(avatarPreviewObject, existing);
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
            confirmButton.interactable = !loading;
            skipButton.interactable    = !loading;
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
