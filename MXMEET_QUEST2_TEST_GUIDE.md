# MXMeet Quest 2 Test Guide

## Before Building

1. Open Unity Hub and use Unity 2022.3 LTS.
2. Install Android Build Support, Android SDK/NDK Tools, and OpenJDK from Unity Hub.
3. Connect the Meta Quest 2 with USB and allow USB debugging in the headset.
4. In Unity, switch platform to Android:
   - File > Build Settings > Android > Switch Platform
5. Confirm XR is enabled for Android:
   - Edit > Project Settings > XR Plug-in Management > Android
   - Enable Oculus/OpenXR according to the installed Meta XR setup.
6. Confirm Firebase config exists at:
   - `Assets/MXMeet/Database/FirebaseConfig/google-services.json`
7. Confirm internet access is available on the Quest 2. Firebase, Unity Lobby/Relay, and Vivox need network access.

## Build Settings

Use these scenes in Build Settings, in this order:

1. `Assets/Scenes/MXMeet/Bootstrap.unity`
2. `Assets/Scenes/MXMeet/Login.unity`
3. `Assets/Scenes/MXMeet/AvatarSelection.unity`
4. `Assets/Scenes/MXMeet/MainMenu.unity`
5. `Assets/Scenes/MXMeet/MRView.unity`
6. `Assets/Scenes/MXMeet/Lobby.unity`

Recommended Android settings:

- Minimum API Level: Android 10/API 29 or newer
- Target Architecture: ARM64
- Scripting Backend: IL2CPP
- Internet Access: Require
- Package name: match the Firebase Android app package configured in Firebase console

## Meta Quest 2 Runtime Checks

On first launch, allow required headset permissions if prompted:

- Camera/passthrough permission
- Microphone permission for Vivox voice
- Network access

Passthrough and spatial anchors are real Quest features. In Unity Editor, MXMeet uses the simulated MR preview room instead.

## Feature Test Checklist

### 1. Register

1. Launch app.
2. Register with username, email, and password.
3. Expected:
   - User reaches Avatar Selection.
   - Firestore has `users/{uid}`.
   - Firestore has `avatars/{uid}`.

### 2. Login

1. Logout or relaunch.
2. Login with the same email/password.
3. Expected:
   - User reaches Avatar Selection or Main Menu.
   - Existing user/avatar data loads without errors.

### 3. Avatar

1. Select a skin and colour.
2. Confirm.
3. Expected:
   - Firestore `avatars/{uid}` updates `skinType`, `colorCode`, and `updatedAt`.
   - Main Menu opens.

### 4. MR VRMeetUp View

1. Main Menu > Launch VRMeetUp.
2. Passthrough should activate on Quest 2.
3. Press Place Anchor.
4. Press Quick Join.
5. Expected:
   - A VRMeetUp lobby is created or joined.
   - The VRMeetUp app surface appears in the real environment at the anchor.
   - Room name and room code show in the HUD.
   - Exit MR disconnects and returns cleanly.

### 5. Discussion Lobby Host

1. Main Menu > Discussion Lobby.
2. Press Create Lobby.
3. Expected:
   - A VRMeetUp lobby is created.
   - Lobby code appears.
   - Firestore `discussion_lobbies` has an active lobby.
   - Firestore `lobby_members` has the host.
   - Firestore `meetup_logs` has `Lobby Created`.

### 6. Discussion Lobby Guest

1. On a second Quest or second app instance, login as another user.
2. Main Menu > Discussion Lobby > Join Lobby.
3. Enter the host lobby code.
4. Expected:
   - Guest joins the same VRMeetUp session.
   - Firestore `lobby_members` has the guest.
   - Firestore `meetup_logs` has `User Joined`.

### 7. Leave and End

1. Guest presses Leave.
2. Expected:
   - Guest member record changes to `left`.
   - `User Left` log is created.
3. Host presses End Lobby.
4. Expected:
   - Lobby status changes to `ended`.
   - Active members change to `left`.
   - `Lobby Ended` log is created.

## Common Problems

- If Firebase login fails on Quest, check internet and Firebase Android package name.
- If Quick Join fails, check Unity Gaming Services Lobby/Relay setup.
- If voice fails, check Vivox project settings and microphone permission.
- If passthrough does not show, confirm Meta XR/OpenXR setup and that the build is running on Quest, not in Editor.
- If app works in Editor but not Quest, check Android build settings and `google-services.json`.
