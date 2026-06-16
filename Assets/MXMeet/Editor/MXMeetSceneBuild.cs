using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;
using MXMeet.Auth;
using MXMeet.Avatar;
using MXMeet.Database;
using MXMeet.Lobby;
using MXMeet.UI;
using MXMeet.VRMeetUp;
using CustomNetwork;

namespace MXMeet.Editor
{
    public static class MXMeetSceneBuild
    {
        const string SceneBase   = "Assets/Scenes/MXMeet/";
        const string PrefabFolder = "Assets/MXMeet/Prefabs";

        // ══════════════════════════════════════════════════════════════
        // ENTRY POINT
        // ══════════════════════════════════════════════════════════════
        [MenuItem("MXMeet/Build All Scenes (Full Setup)")]
        public static void BuildAll()
        {
            if (!AssetDatabase.IsValidFolder(PrefabFolder))
                AssetDatabase.CreateFolder("Assets/MXMeet", "Prefabs");

            BuildBootstrap();
            BuildLogin();
            BuildAvatarSelection();
            BuildMainMenu();
            BuildLobby();
            BuildMRView();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("MXMeet", "All 6 scenes built and wired up!", "OK");
        }

        // ══════════════════════════════════════════════════════════════
        // SCENE BUILDERS
        // ══════════════════════════════════════════════════════════════

        static void BuildBootstrap()
        {
            var scene = Open("Bootstrap");

            // Persistent managers
            Make<FirebaseManager>("FirebaseManager");
            Make<AuthController>("AuthController");
            Make<AvatarController>("AvatarController");
            var vrCtrl = Make<VRMeetUpIntegrationController>("VRMeetUpIntegrationController");

            // AuthManagerNew needs two child display GameObjects
            var authMgrGO = new GameObject("AuthManagerNew");
            var authMgr   = authMgrGO.AddComponent<AuthManagerNew>();
            var signIn    = Child(authMgrGO, "SignInDisplay");
            var lobby     = Child(authMgrGO, "LobbyDisplay");
            SetField(authMgr, "signInDisplay", signIn);
            SetField(authMgr, "lobbyDisplay",  lobby);

            // AppBootstrap + minimal loading canvas
            var bootstrapGO = new GameObject("AppBootstrap");
            var bootstrap   = bootstrapGO.AddComponent<AppBootstrap>();
            bootstrap.loginScene    = "Login";
            bootstrap.mainMenuScene = "MainMenu";

            var cv = WorldCanvas("BootstrapCanvas");
            var bar = Img(cv.transform, "LoadingBar", Color.cyan);
            bar.type        = Image.Type.Filled;
            bar.fillMethod  = Image.FillMethod.Horizontal;
            bar.fillAmount  = 0f;
            Anchor(bar, new Vector2(0, -120), new Vector2(800, 28));
            var status = Txt(cv.transform, "StatusText", "Starting MXMeet…", 26);
            Anchor(status, new Vector2(0, -70), new Vector2(700, 50));

            bootstrap.loadingBar  = bar;
            bootstrap.statusText  = status;
            Dirty(bootstrap);

#if META_XR_SDK
            // OVRManager — required by OVRPassthroughLayer to initialise the OVR subsystem
            var mgrGO = new GameObject("OVRManager");
            var ovrMgr = mgrGO.AddComponent<OVRManager>();
            ovrMgr.trackingOriginType = OVRManager.TrackingOrigin.FloorLevel;

            // OVRPassthroughLayer — needed by VRMeetUpIntegrationController at runtime
            var ptGO    = new GameObject("OVRPassthroughLayer");
            var ptLayer = ptGO.AddComponent<OVRPassthroughLayer>();
            ptLayer.enabled = false; // EnablePassthrough() turns it on at runtime
            vrCtrl.passthroughLayer = ptLayer;
            Dirty(vrCtrl);
            Debug.Log("[MXMeet] OVRManager + OVRPassthroughLayer added and wired.");
#endif

            AddXROrigin();
            ES();
            Save(scene, "Bootstrap");
        }

        static void BuildLogin()
        {
            var scene = Open("Login");
            var cv    = WorldCanvas("LoginCanvas");
            var ui    = cv.gameObject.AddComponent<LoginUI>();

            // ── Login panel ────────────────────────────────────────────
            var lp = Panel(cv.transform, "LoginPanel");
            var lT = Txt(lp.transform, "Title", "MXMeet", 56, Accent);                    Apos(lp, "Title",           0,  320); lT.fontStyle = FontStyles.Bold;
            Txt(lp.transform, "Subtitle", "MIXED REALITY PLATFORM", 15, new Color(0.4f,0.85f,1f,0.6f)); Apos(lp, "Subtitle", 0, 272);
            Divider(lp.transform, new Vector2(0, 245), 520);
            var lEmail = Field(lp.transform, "LoginEmailField",    "Email");               Apos(lp, "LoginEmailField", 0,  150);
            var lPass  = Field(lp.transform, "LoginPasswordField", "Password", pass: true);Apos(lp, "LoginPasswordField", 0, 60);
            var lBtn   = Btn(lp.transform,   "LoginButton",        "Log In",   BtnBlue);   Apos(lp, "LoginButton",     0,  -55);
            var s2r    = Btn(lp.transform,   "SwitchToRegister",   "Create Account", BtnDark); Apos(lp, "SwitchToRegister", 0, -135);

            // ── Register panel ─────────────────────────────────────────
            var rp = Panel(cv.transform, "RegisterPanel", false);
            var rT = Txt(rp.transform, "Title", "Create Account", 42, Accent);             Apos(rp, "Title",              0,  330); rT.fontStyle = FontStyles.Bold;
            Divider(rp.transform, new Vector2(0, 298), 520);
            var rUser  = Field(rp.transform, "UsernameField", "Username");                 Apos(rp, "UsernameField",      0,  200);
            var rEmail = Field(rp.transform, "EmailField",    "Email");                    Apos(rp, "EmailField",         0,  118);
            var rPass  = Field(rp.transform, "PasswordField", "Password", pass: true);     Apos(rp, "PasswordField",      0,   36);
            var rBtn   = Btn(rp.transform,   "RegisterButton","Register", BtnGreen);       Apos(rp, "RegisterButton",     0,  -68);
            var s2l    = Btn(rp.transform,   "SwitchToLogin", "Back to Login", BtnDark);   Apos(rp, "SwitchToLogin",      0, -150);

            // ── Feedback ───────────────────────────────────────────────
            var err  = ErrTxt(cv.transform, "ErrorText");       Anchor(err, new Vector2(0,-390), new Vector2(700,40));
            var load = LoadInd(cv.transform);

            ui.loginPanel            = lp;
            ui.registerPanel         = rp;
            ui.loginEmailField       = lEmail;
            ui.loginPasswordField    = lPass;
            ui.loginButton           = lBtn;
            ui.registerButton        = rBtn;
            ui.registerUsernameField = rUser;
            ui.registerEmailField    = rEmail;
            ui.registerPasswordField = rPass;
            ui.switchToRegisterButton = s2r;
            ui.switchToLoginButton   = s2l;
            ui.errorText             = err;
            ui.loadingIndicator      = load;
            ui.avatarSelectionScene  = "AvatarSelection";
            Dirty(ui);

            AddXROrigin();
            ES(); Save(scene, "Login");
        }

        static void BuildAvatarSelection()
        {
            var scene = Open("AvatarSelection");
            var cv    = WorldCanvas("AvatarSelectionCanvas");
            var ui    = cv.gameObject.AddComponent<AvatarSelectionUI>();

            var preview = new GameObject("AvatarPreview");
            preview.transform.position = new Vector3(0, 1.5f, 3.5f);

            var aT = Txt(cv.transform, "Title", "Avatar", 56, Accent);
            aT.fontStyle = FontStyles.Bold;
            Anchor(FindGO(cv,"Title"), new Vector2(0, 375), new Vector2(600, 70));
            Txt(cv.transform, "Subtitle", "CUSTOMISE YOUR IDENTITY", 14, new Color(0.4f,0.85f,1f,0.6f));
            Anchor(FindGO(cv,"Subtitle"), new Vector2(0, 330), new Vector2(600, 24));
            Divider(cv.transform, new Vector2(0, 303), 500);

            var secLbl1 = Txt(cv.transform, "LblSkin", "SELECT SKIN", 13, new Color(0.4f,0.85f,1f,0.55f));
            Anchor(FindGO(cv,"LblSkin"), new Vector2(0, 272), new Vector2(500, 20)); secLbl1.fontStyle = FontStyles.Bold;

            // Skin buttons (names become the skin type key)
            string[] skins = { "default", "robot", "astronaut" };
            Color[]  sCols = { BtnBlue, new Color(0.52f, 0.18f, 0.82f, 1f), new Color(0.82f, 0.48f, 0.04f, 1f) };
            var skinBtns = new Button[3];
            for (int i = 0; i < 3; i++)
            {
                skinBtns[i] = Btn(cv.transform, skins[i], skins[i], sCols[i]);
                Anchor(skinBtns[i], new Vector2(-310 + i * 310, 210), new Vector2(270, 62));
            }

            Divider(cv.transform, new Vector2(0, 168), 500);
            var secLbl2 = Txt(cv.transform, "LblColor", "CHOOSE COLOUR", 13, new Color(0.4f,0.85f,1f,0.55f));
            Anchor(FindGO(cv,"LblColor"), new Vector2(0, 148), new Vector2(500, 20)); secLbl2.fontStyle = FontStyles.Bold;

            // Colour swatches
            string[] hexes = { "#FFFFFF", "#FF4444", "#4488FF" };
            Color[]  cCols = { Color.white, new Color(1f,0.27f,0.27f,1f), new Color(0.27f,0.53f,1f,1f) };
            var swatches = new ColorSwatch[3];
            for (int i = 0; i < 3; i++)
            {
                var sb = Btn(cv.transform, "Swatch" + i, "", cCols[i]);
                Anchor(sb, new Vector2(-150 + i * 150, 75), new Vector2(110, 110));
                swatches[i] = new ColorSwatch { button = sb, hexColor = hexes[i] };
            }

            Divider(cv.transform, new Vector2(0, 4), 500);

            var confirm = Btn(cv.transform, "ConfirmButton", "Save Avatar", BtnGreen); Anchor(confirm, new Vector2(-175, -72), new Vector2(310, 62));
            var skip    = Btn(cv.transform, "SkipButton",    "Skip",        BtnDark);  Anchor(skip,    new Vector2( 175, -72), new Vector2(210, 62));

            var err  = ErrTxt(cv.transform, "ErrorText");   Anchor(err,  new Vector2(0, -155), new Vector2(700, 36));
            var load = LoadInd(cv.transform);

            ui.avatarPreviewObject = preview;
            ui.skinButtons         = skinBtns;
            ui.colorSwatches       = swatches;
            ui.confirmButton       = confirm;
            ui.skipButton          = skip;
            ui.errorText           = err;
            ui.loadingIndicator    = load;
            ui.mainMenuScene       = "MainMenu";
            Dirty(ui);

            AddXROrigin();
            ES(); Save(scene, "AvatarSelection");
        }

        static void BuildMainMenu()
        {
            var scene = Open("MainMenu");
            var cv    = WorldCanvas("MainMenuCanvas");
            var ui    = cv.gameObject.AddComponent<MainMenuUI>();

            var mT = Txt(cv.transform, "Title", "MXMeet", 60, Accent);
            mT.fontStyle = FontStyles.Bold;
            Anchor(FindGO(cv,"Title"), new Vector2(0, 355), new Vector2(500, 75));
            Txt(cv.transform, "Subtitle", "MIXED REALITY PLATFORM", 15, new Color(0.4f,0.85f,1f,0.6f));
            Anchor(FindGO(cv,"Subtitle"), new Vector2(0, 308), new Vector2(500, 25));
            Divider(cv.transform, new Vector2(0, 278), 460);

            string[] labels = { "My Avatar", "Discussion Lobby", "VRMeetUp (MR)", "Logout" };
            Color[]  cols   = { BtnBlue, BtnGreen, new Color(0.45f,0.1f,0.85f,1f), BtnRed };
            var btns = new Button[4];
            for (int i = 0; i < 4; i++)
            {
                btns[i] = Btn(cv.transform, labels[i].Replace(" ","") + "Btn", labels[i], cols[i]);
                Anchor(btns[i], new Vector2(0, 200 - i * 115), new Vector2(400, 72));
            }

            ui.avatarButton  = btns[0];
            ui.lobbyButton   = btns[1];
            ui.mrButton      = btns[2];
            ui.logoutButton  = btns[3];
            Dirty(ui);

            AddXROrigin();
            ES(); Save(scene, "MainMenu");
        }

        static void BuildLobby()
        {
            var scene = Open("Lobby");

            var ctrlGO = new GameObject("LobbyController");
            ctrlGO.AddComponent<LobbyController_V2>();

            var cv = WorldCanvas("LobbyCanvas");
            var ui = cv.gameObject.AddComponent<LobbyUI>();

            // ── Choice Panel ───────────────────────────────────────────
            var cp = Panel(cv.transform, "ChoicePanel");
            var cpT = Txt(cp.transform, "Title", "Discussion Lobby", 44, Accent); Apos(cp, "Title", 0, 300); cpT.fontStyle = FontStyles.Bold;
            Txt(cp.transform, "Sub", "REAL-WORLD COLLABORATION", 14, new Color(0.4f,0.85f,1f,0.6f)); Apos(cp, "Sub", 0, 256);
            Divider(cp.transform, new Vector2(0, 228), 480);
            var createBtn = Btn(cp.transform, "CreateLobbyButton", "Host a Lobby",  BtnGreen); Apos(cp,"CreateLobbyButton", 0, 100);
            var joinBtn   = Btn(cp.transform, "JoinLobbyButton",   "Join a Lobby",  BtnBlue);  Apos(cp,"JoinLobbyButton",   0,   5);
            var backBtn   = Btn(cp.transform, "BackToMenuButton",  "Back",          BtnDark);  Apos(cp,"BackToMenuButton",  0, -90);

            // ── Host Panel ─────────────────────────────────────────────
            var hp = Panel(cv.transform, "HostPanel", false);
            var hpT = Txt(hp.transform, "Title", "Hosting", 38, Accent); Apos(hp,"Title", 0, 310); hpT.fontStyle = FontStyles.Bold;
            Divider(hp.transform, new Vector2(0, 278), 500);
            var codeText  = Txt(hp.transform, "LobbyCodeText",  "CODE: ---", 32, TextCyan);     Apos(hp,"LobbyCodeText",  0, 220);
            var guestText = Txt(hp.transform, "GuestListText",  "Waiting for guests...", 20, new Color(0.6f,0.8f,1f,0.8f)); Apos(hp,"GuestListText", 0, 100);
            var endBtn    = Btn(hp.transform, "EndLobbyButton", "End Lobby", BtnRed);            Apos(hp,"EndLobbyButton", 0,-240);

            // ── Guest Join Panel ───────────────────────────────────────
            var gjp = Panel(cv.transform, "GuestJoinPanel", false);
            var gjpT = Txt(gjp.transform, "Title", "Join Lobby", 44, Accent); Apos(gjp,"Title", 0, 270); gjpT.fontStyle = FontStyles.Bold;
            Divider(gjp.transform, new Vector2(0, 238), 480);
            var codeIn  = Field(gjp.transform, "LobbyCodeInput", "Enter room code..."); Apos(gjp,"LobbyCodeInput", 0, 100);
            var confBtn = Btn(gjp.transform, "ConfirmJoinButton","Join",   BtnGreen);   Apos(gjp,"ConfirmJoinButton", -170, -40);
            var cancBtn = Btn(gjp.transform, "CancelJoinButton", "Cancel", BtnDark);   Apos(gjp,"CancelJoinButton",   170, -40);

            // ── In-Lobby HUD ───────────────────────────────────────────
            var hudGO = new GameObject("InLobbyHUD");
            hudGO.transform.SetParent(cv.transform, false);
            hudGO.AddComponent<Image>().color = new Color(0.03f, 0.05f, 0.10f, 0.96f);
            Anchor(hudGO, new Vector2(0,-395), new Vector2(960, 72));
            hudGO.SetActive(false);

            // Cyan top border on HUD
            var hudTop = new GameObject("_HUDLine"); hudTop.transform.SetParent(hudGO.transform, false);
            hudTop.AddComponent<Image>().color = Accent;
            var hlt = hudTop.GetComponent<RectTransform>();
            hlt.anchorMin = new Vector2(0,1); hlt.anchorMax = Vector2.one;
            hlt.offsetMin = new Vector2(0,-2); hlt.offsetMax = Vector2.zero;

            var muteBtn  = Btn(hudGO.transform, "MuteButton",       "Mute",  BtnDark); Anchor(muteBtn,  new Vector2(-240, 0), new Vector2(280, 56));
            var muteText = muteBtn.GetComponentInChildren<TextMeshProUGUI>();
            var leaveBtn = Btn(hudGO.transform, "LeaveLobbyButton", "Leave", BtnRed);  Anchor(leaveBtn, new Vector2( 240, 0), new Vector2(220, 56));

            // ── Feedback ───────────────────────────────────────────────
            var err  = ErrTxt(cv.transform, "ErrorText");  Anchor(err, new Vector2(0,-445), new Vector2(700,36));
            var load = LoadInd(cv.transform);

            ui.choicePanel       = cp;
            ui.createLobbyButton = createBtn;
            ui.joinLobbyButton   = joinBtn;
            ui.backToMenuButton  = backBtn;
            ui.hostPanel         = hp;
            ui.lobbyCodeText     = codeText;
            ui.guestListText     = guestText;
            ui.endLobbyButton    = endBtn;
            ui.guestJoinPanel    = gjp;
            ui.lobbyCodeInput    = codeIn;
            ui.confirmJoinButton = confBtn;
            ui.cancelJoinButton  = cancBtn;
            ui.inLobbyHUD        = hudGO;
            ui.muteButton        = muteBtn;
            ui.muteButtonText    = muteText;
            ui.leaveLobbyButton  = leaveBtn;
            ui.errorText         = err;
            ui.loadingIndicator  = load;
            ui.mainMenuScene     = "MainMenu";
            Dirty(ui);

            AddXROrigin();
            ES(); Save(scene, "Lobby");
        }

        static void BuildMRView()
        {
            var scene = Open("MRView");
            var cv    = WorldCanvas("MRViewCanvas");
            var ui    = cv.gameObject.AddComponent<VRMeetUpViewUI_V2>();

            // ── Setup Panel ────────────────────────────────────────────
            var sp = Panel(cv.transform, "SetupPanel");
            var spT = Txt(sp.transform, "Title", "VRMeetUp MR", 48, Accent); Apos(sp,"Title", 0, 320); spT.fontStyle = FontStyles.Bold;
            Txt(sp.transform, "Sub", "MIXED REALITY SESSION", 14, new Color(0.4f,0.85f,1f,0.6f)); Apos(sp,"Sub", 0, 275);
            Divider(sp.transform, new Vector2(0, 248), 500);
            var statusTxt = Txt(sp.transform, "StatusText", "Initialising passthrough...", 20, TextCyan); Apos(sp,"StatusText", 0, 195);
            var placeBtn  = Btn(sp.transform, "PlaceAnchorButton", "Place Anchor",  BtnBlue);  Apos(sp,"PlaceAnchorButton", 0,  110);
            var quickBtn  = Btn(sp.transform, "QuickJoinButton",   "Quick Join",    BtnGreen); Apos(sp,"QuickJoinButton",  -210,  10);
            var byCodeBtn = Btn(sp.transform, "JoinByCodeButton",  "Join by Code",  BtnDark);  Apos(sp,"JoinByCodeButton",  210,  10);

            // Room code sub-panel
            var rcPanel = new GameObject("RoomCodePanel");
            rcPanel.transform.SetParent(sp.transform, false);
            rcPanel.AddComponent<Image>().color = new Color(0.04f, 0.06f, 0.12f, 0.98f);
            Anchor(rcPanel, new Vector2(0,-120), new Vector2(640, 200));
            rcPanel.SetActive(false);
            // cyan border on room code panel
            var rcLine = new GameObject("_Border"); rcLine.transform.SetParent(rcPanel.transform, false);
            rcLine.AddComponent<Image>().color = new Color(0f,0.85f,1f,0.5f);
            var rclrt = rcLine.GetComponent<RectTransform>();
            rclrt.anchorMin = new Vector2(0,1); rclrt.anchorMax = Vector2.one;
            rclrt.offsetMin = new Vector2(0,-2); rclrt.offsetMax = Vector2.zero;
            var rcInput = Field(rcPanel.transform, "RoomCodeInput", "Enter room code..."); Apos(rcPanel,"RoomCodeInput", 0, 40);
            Btn(rcPanel.transform, "ConfirmCodeButton", "Connect", BtnGreen);              Apos(rcPanel,"ConfirmCodeButton", 0, -52);

            // ── HUD Panel ──────────────────────────────────────────────
            var hud  = Panel(cv.transform, "HUDPanel", false);
            var rnTxt = Txt(hud.transform, "RoomNameText", "ROOM: ---", 32, Accent);        Apos(hud,"RoomNameText", 0, 300); rnTxt.fontStyle = FontStyles.Bold;
            var rcTxt = Txt(hud.transform, "RoomCodeText", "CODE: ---", 20, TextCyan);      Apos(hud,"RoomCodeText", 0, 258);
            Divider(hud.transform, new Vector2(0, 228), 500);
            var exitBtn = Btn(hud.transform, "ExitButton", "Exit MR", BtnRed);              Apos(hud,"ExitButton", 0, -310);

            // ── Feedback ───────────────────────────────────────────────
            var err  = ErrTxt(cv.transform, "ErrorText");  Anchor(err, new Vector2(0,-425), new Vector2(700,36));
            var load = LoadInd(cv.transform);

            ui.setupPanel          = sp;
            ui.placeAnchorButton   = placeBtn;
            ui.quickJoinButton     = quickBtn;
            ui.joinByCodeButton    = byCodeBtn;
            ui.roomCodeInput       = rcInput;
            ui.roomCodePanel       = rcPanel;
            ui.statusText          = statusTxt;
            ui.hudPanel            = hud;
            ui.roomNameText        = rnTxt;
            ui.roomCodeText        = rcTxt;
            ui.exitButton          = exitBtn;
            ui.errorText           = err;
            ui.loadingIndicator    = load;
            ui.mainMenuScene       = "MainMenu";
            Dirty(ui);

            AddXROrigin();
            ES(); Save(scene, "MRView");
        }

        // ══════════════════════════════════════════════════════════════
        // NOTEITEM PREFAB
        // ══════════════════════════════════════════════════════════════

        // ══════════════════════════════════════════════════════════════
        // XR CAMERA RIG
        // ══════════════════════════════════════════════════════════════
        static void AddXROrigin()
        {
            const string xrOriginPath = "Assets/Samples/XR Interaction Toolkit/3.0.7/Starter Assets/Prefabs/XR Origin (XR Rig).prefab";
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(xrOriginPath);
            if (prefab != null)
            {
                PrefabUtility.InstantiatePrefab(prefab);
                Debug.Log("[MXMeet] Added XR Origin (XR Rig) to scene.");
            }
            else
            {
                // Fallback: plain camera so scenes render in Editor play mode
                var camGO = new GameObject("Main Camera");
                camGO.tag = "MainCamera";
                var cam = camGO.AddComponent<Camera>();
                cam.clearFlags      = CameraClearFlags.SolidColor;
                cam.backgroundColor = Color.black;
                camGO.transform.position = new Vector3(0, 1.7f, 0);
                Debug.LogWarning("[MXMeet] XR Origin prefab not found — added fallback Main Camera.");
            }
        }

        // ══════════════════════════════════════════════════════════════
        // HELPERS — Factory
        // ══════════════════════════════════════════════════════════════

        // ── Futuristic Color Palette ──────────────────────────────────
        static readonly Color BgDeep    = new Color(0.03f, 0.04f, 0.09f, 0.98f);
        static readonly Color PanelBg   = new Color(0.05f, 0.07f, 0.14f, 0.96f);
        static readonly Color Accent    = new Color(0.0f,  0.85f, 1.0f,  1.0f);
        static readonly Color BtnBlue   = new Color(0.04f, 0.42f, 0.82f, 1.0f);
        static readonly Color BtnGreen  = new Color(0.0f,  0.72f, 0.42f, 1.0f);
        static readonly Color BtnRed    = new Color(0.82f, 0.10f, 0.22f, 1.0f);
        static readonly Color BtnDark   = new Color(0.10f, 0.13f, 0.22f, 1.0f);
        static readonly Color TextWhite = new Color(0.92f, 0.96f, 1.0f,  1.0f);
        static readonly Color TextCyan  = new Color(0.4f,  0.85f, 1.0f,  1.0f);

        // Legacy aliases so existing call-sites compile unchanged
        static readonly Color Teal = new Color(0.04f, 0.42f, 0.82f, 1.0f);
        static readonly Color Grey = new Color(0.10f, 0.13f, 0.22f, 1.0f);

        static UnityEngine.SceneManagement.Scene Open(string name)
        {
            var scene = EditorSceneManager.OpenScene(SceneBase + name + ".unity", OpenSceneMode.Single);
            foreach (var go in scene.GetRootGameObjects())
                Object.DestroyImmediate(go);
            return scene;
        }

        static void Save(UnityEngine.SceneManagement.Scene s, string name) =>
            EditorSceneManager.SaveScene(s, SceneBase + name + ".unity");

        static T Make<T>(string name) where T : Component
        {
            var go = new GameObject(name);
            return go.AddComponent<T>();
        }

        static GameObject Child(GameObject parent, string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent.transform);
            return go;
        }

        static Canvas WorldCanvas(string name)
        {
            var go = new GameObject(name);
            var cv = go.AddComponent<Canvas>();
            cv.renderMode = RenderMode.WorldSpace;
            go.AddComponent<CanvasScaler>();
            go.AddComponent<GraphicRaycaster>();
            var rt = go.GetComponent<RectTransform>();
            rt.position   = new Vector3(0, 1.5f, 2f);
            rt.localScale = Vector3.one * 0.001f;
            rt.sizeDelta  = new Vector2(1200, 900);

            // Deep space background
            var bg = new GameObject("_Background");
            bg.transform.SetParent(go.transform, false);
            bg.AddComponent<Image>().color = BgDeep;
            var brt = bg.GetComponent<RectTransform>();
            brt.anchorMin = Vector2.zero; brt.anchorMax = Vector2.one;
            brt.offsetMin = Vector2.zero; brt.offsetMax = Vector2.zero;

            // Cyan frame lines — top
            FrameLine(go.transform, new Vector2(0,1), new Vector2(1,1), new Vector2(0,-2), new Vector2(0,0));
            // bottom
            FrameLine(go.transform, new Vector2(0,0), new Vector2(1,0), new Vector2(0,0), new Vector2(0,2));
            // left
            FrameLine(go.transform, new Vector2(0,0), new Vector2(0,1), new Vector2(0,0), new Vector2(2,0));
            // right
            FrameLine(go.transform, new Vector2(1,0), new Vector2(1,1), new Vector2(-2,0), new Vector2(0,0));

            return cv;
        }

        static void FrameLine(Transform parent, Vector2 ancMin, Vector2 ancMax, Vector2 offMin, Vector2 offMax)
        {
            var go = new GameObject("_Frame");
            go.transform.SetParent(parent, false);
            go.AddComponent<Image>().color = new Color(0f, 0.85f, 1f, 0.35f);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = ancMin; rt.anchorMax = ancMax;
            rt.offsetMin = offMin; rt.offsetMax = offMax;
        }

        static GameObject Panel(Transform parent, string name, bool active = true)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<Image>().color = PanelBg;
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;

            // Cyan top-border accent
            var top = new GameObject("_TopBorder");
            top.transform.SetParent(go.transform, false);
            top.AddComponent<Image>().color = Accent;
            var trt = top.GetComponent<RectTransform>();
            trt.anchorMin = new Vector2(0,1); trt.anchorMax = Vector2.one;
            trt.offsetMin = new Vector2(0,-3); trt.offsetMax = Vector2.zero;

            go.SetActive(active);
            return go;
        }

        static Button Btn(Transform parent, string name, string label, Color? col = null)
        {
            var baseCol = col ?? BtnBlue;
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.color = baseCol;
            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;
            go.GetComponent<RectTransform>().sizeDelta = new Vector2(300, 62);

            // Left accent bar
            var bar = new GameObject("_AccentBar");
            bar.transform.SetParent(go.transform, false);
            bar.AddComponent<Image>().color = new Color(1f, 1f, 1f, 0.25f);
            var brt = bar.GetComponent<RectTransform>();
            brt.anchorMin = Vector2.zero; brt.anchorMax = new Vector2(0f, 1f);
            brt.offsetMin = Vector2.zero; brt.offsetMax = new Vector2(4f, 0f);

            // Top highlight shimmer
            var hl = new GameObject("_Highlight");
            hl.transform.SetParent(go.transform, false);
            hl.AddComponent<Image>().color = new Color(1f, 1f, 1f, 0.08f);
            var hrt = hl.GetComponent<RectTransform>();
            hrt.anchorMin = new Vector2(0f, 0.7f); hrt.anchorMax = Vector2.one;
            hrt.offsetMin = hrt.offsetMax = Vector2.zero;

            // Label
            var tgo = new GameObject("Text");
            tgo.transform.SetParent(go.transform, false);
            var tmp = tgo.AddComponent<TextMeshProUGUI>();
            tmp.text       = label;
            tmp.fontSize   = 22;
            tmp.color      = TextWhite;
            tmp.alignment  = TextAlignmentOptions.Center;
            tmp.fontStyle  = FontStyles.Bold;
            var trt = tgo.GetComponent<RectTransform>();
            trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
            trt.offsetMin = new Vector2(8, 0); trt.offsetMax = Vector2.zero;
            return btn;
        }

        static TMP_InputField Field(Transform parent, string name, string ph, bool pass = false)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<Image>().color = new Color(0.07f, 0.09f, 0.16f, 1f);
            go.GetComponent<RectTransform>().sizeDelta = new Vector2(520, 62);

            // Bottom cyan border
            var border = new GameObject("_Border");
            border.transform.SetParent(go.transform, false);
            border.AddComponent<Image>().color = new Color(0f, 0.85f, 1f, 0.7f);
            var brt = border.GetComponent<RectTransform>();
            brt.anchorMin = Vector2.zero; brt.anchorMax = new Vector2(1f, 0f);
            brt.offsetMin = Vector2.zero; brt.offsetMax = new Vector2(0f, 2f);

            var area = new GameObject("Text Area");
            area.transform.SetParent(go.transform, false);
            area.AddComponent<RectMask2D>();
            var art = area.GetComponent<RectTransform>();
            art.anchorMin = Vector2.zero; art.anchorMax = Vector2.one;
            art.offsetMin = new Vector2(14, 6); art.offsetMax = new Vector2(-10, -6);

            var phGO  = new GameObject("Placeholder");
            phGO.transform.SetParent(area.transform, false);
            var phTMP = phGO.AddComponent<TextMeshProUGUI>();
            phTMP.text = ph; phTMP.color = new Color(0.35f, 0.55f, 0.75f, 1f); phTMP.fontSize = 20;
            FullRect(phGO);

            var tGO  = new GameObject("Text");
            tGO.transform.SetParent(area.transform, false);
            var tTMP = tGO.AddComponent<TextMeshProUGUI>();
            tTMP.color = TextWhite; tTMP.fontSize = 20;
            FullRect(tGO);

            var f = go.AddComponent<TMP_InputField>();
            f.textViewport  = art;
            f.textComponent = tTMP;
            f.placeholder   = phTMP;
            if (pass) f.contentType = TMP_InputField.ContentType.Password;
            return f;
        }

        static TextMeshProUGUI Txt(Transform parent, string name, string text, float size = 28, Color? col = null)
        {
            var go  = new GameObject(name);
            go.transform.SetParent(parent, false);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text      = text;
            tmp.fontSize  = size;
            tmp.color     = col ?? TextWhite;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontStyle = size >= 36 ? FontStyles.Bold : FontStyles.Normal;
            go.GetComponent<RectTransform>().sizeDelta = new Vector2(700, 55);
            return tmp;
        }

        // Horizontal divider line in cyan
        static void Divider(Transform parent, Vector2 pos, float width = 700)
        {
            var go = new GameObject("_Divider");
            go.transform.SetParent(parent, false);
            go.AddComponent<Image>().color = new Color(0f, 0.85f, 1f, 0.3f);
            var rt = go.GetComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta        = new Vector2(width, 1);
        }

        static TextMeshProUGUI ErrTxt(Transform parent, string name)
        {
            var t = Txt(parent, name, "", 19, new Color(1f, 0.3f, 0.4f, 1f));
            t.gameObject.SetActive(false);
            return t;
        }

        static Image Img(Transform parent, string name, Color col)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.color = col;
            return img;
        }

        static GameObject LoadInd(Transform parent)
        {
            var go = new GameObject("LoadingIndicator");
            go.transform.SetParent(parent, false);

            // Outer ring
            var ring = go.AddComponent<Image>();
            ring.color = new Color(0f, 0.85f, 1f, 0.15f);
            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(80, 80);

            // Inner dot
            var dot = new GameObject("_Dot");
            dot.transform.SetParent(go.transform, false);
            dot.AddComponent<Image>().color = Accent;
            var drt = dot.GetComponent<RectTransform>();
            drt.anchoredPosition = Vector2.zero;
            drt.sizeDelta        = new Vector2(20, 20);

            // Label
            var lbl = new GameObject("_Label");
            lbl.transform.SetParent(go.transform, false);
            var ltmp = lbl.AddComponent<TextMeshProUGUI>();
            ltmp.text      = "Loading...";
            ltmp.fontSize  = 18;
            ltmp.color     = TextCyan;
            ltmp.alignment = TextAlignmentOptions.Center;
            var lrt = lbl.GetComponent<RectTransform>();
            lrt.anchoredPosition = new Vector2(0, -55);
            lrt.sizeDelta        = new Vector2(200, 30);

            go.SetActive(false);
            return go;
        }

        static void ES()
        {
            if (Object.FindFirstObjectByType<EventSystem>() == null)
            {
                var go = new GameObject("EventSystem");
                go.AddComponent<EventSystem>();
                go.AddComponent<StandaloneInputModule>();
            }
        }

        // ── Layout helpers ───────────────────────────────────────────

        static void Anchor(Object c, Vector2 pos, Vector2 size)
        {
            var rt = c is Component comp
                ? comp.GetComponent<RectTransform>()
                : (c as GameObject)?.GetComponent<RectTransform>();
            if (rt == null) return;
            rt.anchoredPosition = pos;
            rt.sizeDelta        = size;
        }

        static void Anchor(GameObject go, Vector2 pos, Vector2 size)
        {
            var rt = go.GetComponent<RectTransform>();
            if (rt == null) rt = go.AddComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta        = size;
        }

        // Position a named child inside a panel
        static void Apos(GameObject panel, string childName, float x, float y)
        {
            var child = panel.transform.Find(childName);
            if (child == null) return;
            child.GetComponent<RectTransform>().anchoredPosition = new Vector2(x, y);
        }

        static void FullRect(GameObject go)
        {
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        }

        static GameObject FindGO(Canvas cv, string name)
        {
            var t = cv.transform.Find(name);
            return t != null ? t.gameObject : null;
        }

        static void SetField(Object target, string field, Object value)
        {
            var so = new SerializedObject(target);
            var p  = so.FindProperty(field);
            if (p != null) { p.objectReferenceValue = value; so.ApplyModifiedPropertiesWithoutUndo(); }
        }

        static void Dirty(Object o) => EditorUtility.SetDirty(o);
    }
}
